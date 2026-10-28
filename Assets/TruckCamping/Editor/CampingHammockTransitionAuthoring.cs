using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace LWS.TruckCamping.Editor
{
    // R45 actual native exit: original chassis contacts the shortened ramp's
    // raised inner face, while all four wheels remain on the lowered deck.
    // Geometry experiment ONLY. Springs, platform, terrain, vehicle and input
    // remain exact. Actual original entry AND two-way exit tests are mandatory.
    public static class CampingHammockTransitionAuthoring
    {
        const string ReviewedPrefab = "ca37a21bcafe3cc6f202ab2570bc7b0c79b4c3fa3ba729c45d75a0706ee87f47";
        const string ReviewedMeta = "e49c815a758743e10c9aa4ac9878f61d3af1da5adf8aca016dca291ae5001ff1";
        const string RampName = "Actual gently sloped hammock approach";
        // R45 root-relative loaded top endpoints: negative .268..426,
        // positive .417..555. Unloaded center/top about .319/.526.
        // .45 is inside the measured central transition envelope. It is not a
        // claimed new equilibrium or a replacement for real loaded tests.
        public const float InnerTop = .45f, RestSeam = .03f, ToeClearance = .02f;
        public const float MaximumPitch = 12, MaximumHorizontalSpan = 24, ExtensionIncrement = .25f;
        static readonly UTF8Encoding Utf8 = new UTF8Encoding(false, true);
        [Serializable] public sealed class Profile
        {
            public string hierarchy, terrainSamples;
            public long transformId;
            public Vector3 oldPosition, oldScale, position, scale, innerLeft, innerRight, outerLeft, outerRight;
            public Quaternion oldRotation, rotation;
            public float pitch, innerGap, innerTop, outerTop, toeMin, toeMax, horizontalSpan;
            public int extensionSteps;
            public string toePolicy = "MIN_OF_FIVE_ACTUAL_WIDTH_SAMPLES_PLUS_02_EMBEDS_TOE_IN_HIGHER_GROUND";
            public Vector3[] terrainQueryWorldYIgnored;
            public float[] terrainHeightsWorld;
        }
        [Serializable] sealed class NumericalBounds
        {
            public string status = "READ_ONLY_ACTUAL_TERRAIN_AND_FIXED_PITCH_BOUNDS_NOT_GEOMETRY_ACCEPTANCE", stage, frameName;
            public Vector3 frameWorldPosition, frameWorldEuler, frameWorldScale, rampLocalPosition, rampLocalEuler, rampLocalScale, rampBoxCenter, rampBoxSize;
            public Vector3[] actualOriginalToePointsWorld;
            public float[] actualOriginalToeTerrainHeightsWorld;
            public Profile minusTwelve, plusTwelve;
            public float minusTwelveResidual, plusTwelveResidual, targetInnerTop, targetToeClearance;
            public bool originalBoundSatisfied;
        }
        [Serializable] sealed class Change { public long id; public string before, after; }
        [Serializable] sealed class Receipt
        {
            public string status, asset, beforeSha256, afterSha256, metaSha256, beforeFile, afterFile;
            public int ramps, platforms, trees;
            public bool onlyReviewedRampTransformFields, metaExact;
            public Profile[] profiles;
            public Change[] changes;
        }
        static void Require(bool value, string reason)
        { if (!value) throw new InvalidOperationException("Known hammock transition preflight: " + reason + "; custom/unknown content preserved."); }
        static string Hash(byte[] bytes)
        { using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant(); }
        static string Number(float value) => value.ToString("R", CultureInfo.InvariantCulture);
        static Vector3[] Corners(Transform frame, BoxCollider box, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            Require(box.transform.parent == frame, "direct original box frame");
            var matrix = Matrix4x4.TRS(position, rotation, scale); var result = new Vector3[8];
            for (int i = 0; i < 8; i++) result[i] = matrix.MultiplyPoint3x4(box.center + Vector3.Scale(box.size * .5f,
                new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1)));
            return result;
        }
        static float Gap(Transform frame, BoxCollider deck, BoxCollider ramp, float pitch, float length, float centerZ)
        {
            float side = Mathf.Sign(ramp.transform.localPosition.z);
            var corners = Corners(frame, ramp, new Vector3(0, 0, side * centerZ), Quaternion.Euler(side * pitch, 0, 0), new Vector3(10, .25f, length));
            var deckCorners = Corners(frame, deck, deck.transform.localPosition, deck.transform.localRotation, deck.transform.localScale);
            return corners.Min(v => side * v.z) - deckCorners.Max(v => side * v.z);
        }
        static float Length(Transform frame, BoxCollider deck, BoxCollider ramp, float pitch, float centerZ, float horizontalSpan)
        {
            float low = 0, high = horizontalSpan / Mathf.Cos(MaximumPitch * Mathf.Deg2Rad) + .5f;
            for (int i = 0; i < 30; i++) { float mid = (low + high) * .5f; if (Gap(frame, deck, ramp, pitch, mid, centerZ) > RestSeam) low = mid; else high = mid; }
            return (low + high) * .5f;
        }
        static Profile Evaluate(Transform frame, BoxCollider deck, BoxCollider ramp, float pitch, float horizontalSpan, Func<Vector3, float> terrain)
        {
            float side = Mathf.Sign(ramp.transform.localPosition.z);
            var deckCorners = Corners(frame, deck, deck.transform.localPosition, deck.transform.localRotation, deck.transform.localScale);
            float centerZ = deckCorners.Max(v => side * v.z) + RestSeam + horizontalSpan * .5f;
            float length = Length(frame, deck, ramp, pitch, centerZ, horizontalSpan);
            var rotation = Quaternion.Euler(side * pitch, 0, 0); var scale = new Vector3(10, .25f, length);
            var position = new Vector3(0, 0, side * centerZ); var corners = Corners(frame, ramp, position, rotation, scale);
            var top = new[] { corners[2], corners[3], corners[6], corners[7] }.OrderBy(v => side * v.z).ToArray();
            var inner = top.Take(2).OrderBy(v => v.x).ToArray(); var outer = top.Skip(2).OrderBy(v => v.x).ToArray();
            var toePoints = new[] { 0f, .25f, .5f, .75f, 1f }.Select(t => frame.TransformPoint(Vector3.Lerp(outer[0], outer[1], t))).ToArray();
            var heights = toePoints.Select(terrain).ToArray();
            Require(heights.All(v => !float.IsNaN(v) && !float.IsInfinity(v)), "actual terrain toe queries must resolve");
            float y = InnerTop - inner[0].y; position.y = y;
            return new Profile { oldPosition = ramp.transform.localPosition, oldScale = ramp.transform.localScale, oldRotation = ramp.transform.localRotation,
                position = position, rotation = rotation, scale = scale, pitch = pitch,
                innerLeft = inner[0] + Vector3.up * y, innerRight = inner[1] + Vector3.up * y,
                outerLeft = outer[0] + Vector3.up * y, outerRight = outer[1] + Vector3.up * y,
                innerTop = InnerTop, outerTop = outer[0].y + y, toeMin = heights.Min() - frame.position.y, toeMax = heights.Max() - frame.position.y,
                horizontalSpan = horizontalSpan, innerGap = Gap(frame, deck, ramp, pitch, length, centerZ), terrainSamples = string.Join("/", heights.Select(Number)), terrainQueryWorldYIgnored = toePoints, terrainHeightsWorld = heights };
        }
        static float OriginalSpan(Transform frame, BoxCollider ramp)
        {
            var corners = Corners(frame, ramp, ramp.transform.localPosition, ramp.transform.localRotation, ramp.transform.localScale);
            return corners.Max(v => v.z) - corners.Min(v => v.z);
        }
        static NumericalBounds Numerics(Transform frame, BoxCollider deck, BoxCollider ramp, Func<Vector3, float> terrain, string stage)
        {
            float side = Mathf.Sign(ramp.transform.localPosition.z);
            var corners = Corners(frame, ramp, ramp.transform.localPosition, ramp.transform.localRotation, ramp.transform.localScale);
            var top = new[] { corners[2], corners[3], corners[6], corners[7] }.OrderBy(v => side * v.z).Skip(2).OrderBy(v => v.x).ToArray();
            var currentToes = new[] { frame.TransformPoint(top[0]), frame.TransformPoint((top[0] + top[1]) * .5f), frame.TransformPoint(top[1]) };
            float span = OriginalSpan(frame, ramp);
            var low = Evaluate(frame, deck, ramp, -12, span, terrain); var high = Evaluate(frame, deck, ramp, 12, span, terrain);
            float lowResidual = low.outerTop - low.toeMin - ToeClearance, highResidual = high.outerTop - high.toeMin - ToeClearance;
            return new NumericalBounds { stage = stage, frameName = frame.name, frameWorldPosition = frame.position, frameWorldEuler = frame.eulerAngles, frameWorldScale = frame.lossyScale,
                rampLocalPosition = ramp.transform.localPosition, rampLocalEuler = ramp.transform.localEulerAngles, rampLocalScale = ramp.transform.localScale,
                rampBoxCenter = ramp.center, rampBoxSize = ramp.size, actualOriginalToePointsWorld = currentToes, actualOriginalToeTerrainHeightsWorld = currentToes.Select(terrain).ToArray(),
                minusTwelve = low, plusTwelve = high, minusTwelveResidual = lowResidual, plusTwelveResidual = highResidual,
                targetInnerTop = InnerTop, targetToeClearance = ToeClearance, originalBoundSatisfied = lowResidual > 0 && highResidual < 0 };
        }
        static void LogAllActualNumericalBounds(GameObject asset)
        {
            var world = asset.GetComponent<CampingWilderness>(); int count = 0, fitting = 0;
            Require(world != null && world.hammocks.Length == 4, "complete shared world for all-eight numerical observation");
            var errors = new List<string>();
            foreach (var hammock in world.hammocks)
                foreach (var ramp in hammock.GetComponentsInChildren<BoxCollider>(true).Where(c => c.name == RampName))
                {
                    try
                    {
                        var row = Numerics(hammock.transform, hammock.deck, ramp, p => TerrainHeight(world, p), "ALL_EIGHT_BEFORE_ANY_BOUNDED_FIT_REQUIRE");
                        Debug.Log("CAMPING_HAMMOCK_NUMERICAL_BOUNDS " + JsonUtility.ToJson(row)); count++; if (row.originalBoundSatisfied) fitting++;
                    }
                    catch (Exception error) { errors.Add(hammock.transform.localPosition.ToString("R") + "/" + ramp.transform.localPosition.ToString("R") + ":" + error); }
                }
            Debug.Log("CAMPING_HAMMOCK_NUMERICAL_SUMMARY observed=" + count + " originalBoundFits=" + fitting + " errors=" + errors.Count);
            foreach (string error in errors) Debug.Log("CAMPING_HAMMOCK_NUMERICAL_ERROR " + error);
            Require(count == 8 && errors.Count == 0, "all eight actual numerical observations must complete");
        }
        public static Profile FitActualToeProfile(Transform frame, BoxCollider deck, BoxCollider ramp, Func<Vector3, float> actualTerrain)
        {
            Require(frame != null && deck != null && ramp != null && actualTerrain != null, "actual boxes and terrain authority required");
            Require(frame.lossyScale == Vector3.one && Quaternion.Angle(frame.rotation, Quaternion.identity) < .001f, "original unscaled unrotated hammock frame");
            Require((deck.transform.localPosition - new Vector3(0, .9f, 0)).sqrMagnitude < 1e-8f && (deck.transform.localScale - new Vector3(10, .35f, 16)).sqrMagnitude < 1e-8f,
                "original deck reference geometry");
            Require(deck.center.sqrMagnitude < 1e-8f && (deck.size - Vector3.one).sqrMagnitude < 1e-8f &&
                ramp.center.sqrMagnitude < 1e-8f && (ramp.size - Vector3.one).sqrMagnitude < 1e-8f, "original box dimensions");
            Require(Mathf.Abs(ramp.transform.localPosition.x) < 1e-5f && Mathf.Abs(Mathf.Abs(ramp.transform.localPosition.z) - 11) < 1e-5f &&
                Mathf.Abs(ramp.transform.localScale.x - 10) < 1e-5f && Mathf.Abs(ramp.transform.localScale.y - .25f) < 1e-5f && !ramp.isTrigger && ramp.attachedRigidbody == null,
                "original fixed ramp width/thickness/center");
            float low = -MaximumPitch, high = MaximumPitch, span = 0;
            Profile lowProfile = null, highProfile = null; int extension = 0;
            float originalSpan = OriginalSpan(frame, ramp);
            for (extension = 0; originalSpan + extension * ExtensionIncrement <= MaximumHorizontalSpan; extension++)
            {
                span = originalSpan + extension * ExtensionIncrement;
                lowProfile = Evaluate(frame, deck, ramp, low, span, actualTerrain); highProfile = Evaluate(frame, deck, ramp, high, span, actualTerrain);
                if (lowProfile.outerTop - lowProfile.toeMin - ToeClearance > 0 && highProfile.outerTop - highProfile.toeMin - ToeClearance < 0) break;
            }
            Require(lowProfile != null && highProfile != null && lowProfile.outerTop - lowProfile.toeMin - ToeClearance > 0 && highProfile.outerTop - highProfile.toeMin - ToeClearance < 0,
                "actual toe/inner height must fit unchanged12deg pitch within bounded outward extension");
            for (int i = 0; i < 32; i++)
            { float mid = (low + high) * .5f; var p = Evaluate(frame, deck, ramp, mid, span, actualTerrain); if (p.outerTop - p.toeMin - ToeClearance > 0) low = mid; else high = mid; }
            var result = Evaluate(frame, deck, ramp, (low + high) * .5f, span, actualTerrain); result.extensionSteps = extension;
            Require(Mathf.Abs(result.outerTop - result.toeMin - ToeClearance) < 1e-4f && Mathf.Abs(result.innerGap - RestSeam) < 1e-5f &&
                result.horizontalSpan >= originalSpan - 1e-4f && result.horizontalSpan <= MaximumHorizontalSpan && Mathf.Abs(result.pitch) <= MaximumPitch,
                "finite actual terrain embedded toe, measured inner height and unchanged pitch limit"); return result;
        }
        static float TerrainHeight(CampingWilderness world, Vector3 point)
        {
            foreach (var terrain in world.terrains)
            {
                Require(terrain != null && terrain.terrainData != null && terrain.transform.lossyScale == Vector3.one, "actual owned terrain data");
                var local = terrain.transform.InverseTransformPoint(point); var size = terrain.terrainData.size;
                if (local.x >= 0 && local.z >= 0 && local.x <= size.x && local.z <= size.z)
                    return terrain.transform.position.y + terrain.terrainData.GetInterpolatedHeight(local.x / size.x, local.z / size.z);
            }
            throw new InvalidOperationException("No actual terrain at the ramp toe. No synthetic ground fallback.");
        }
        static Profile[] Plan(GameObject asset)
        {
            var world = asset.GetComponent<CampingWilderness>(); Require(world != null && world.hammocks.Length == 4 && world.terrains.Length == 4, "complete shared world");
            var result = new List<Profile>();
            foreach (var hammock in world.hammocks)
            {
                Require(hammock.platform.mass == 35000 && !hammock.platform.isKinematic && hammock.platform.GetComponents<SpringJoint>().Length == 4, "original dynamic authority");
                var ramps = hammock.GetComponentsInChildren<BoxCollider>(true).Where(c => c.name == RampName).ToArray(); Require(ramps.Length == 2, "two original ramps");
                foreach (var ramp in ramps)
                {
                    var profile = FitActualToeProfile(hammock.transform, hammock.deck, ramp, p => TerrainHeight(world, p));
                    Require(Mathf.Abs(ramp.transform.localPosition.y - .4f) < 1e-5f && Mathf.Abs(ramp.transform.localScale.z - 5.953912f) < 2e-5f, "exact prior shortening geometry");
                    Require(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(ramp.transform, out string guid, out long id) && guid == AssetDatabase.AssetPathToGUID(CampingWilderness.SharedPrefab), "original persistent ramp identity");
                    profile.transformId = id; profile.hierarchy = hammock.transform.localPosition.ToString("R") + "/" + Mathf.Sign(ramp.transform.localPosition.z);
                    Debug.Log("CAMPING_HAMMOCK_EXTENDED_PROFILE_PLAN " + JsonUtility.ToJson(profile)); result.Add(profile);
                }
            }
            Require(result.Select(p => p.transformId).Distinct().Count() == 8, "eight unique ramp transforms"); return result.ToArray();
        }
        public static string ObserveActualToePlan()
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(CampingWilderness.SharedPrefab); Require(asset != null, "existing shared prefab; no rebuild");
            Require(Hash(File.ReadAllBytes(CampingWilderness.SharedPrefab)) == ReviewedPrefab && Hash(File.ReadAllBytes(CampingWilderness.SharedPrefab + ".meta")) == ReviewedMeta, "exact R45 shortened prefab/meta");
            LogAllActualNumericalBounds(asset);
            var receipt = new Receipt { status = "READ_ONLY_ACTUAL_TERRAIN_TOE_AND_MEASURED_DECK_TRANSITION_PLAN", asset = CampingWilderness.SharedPrefab, profiles = Plan(asset), ramps = 8, platforms = 4 };
            string json = JsonUtility.ToJson(receipt, true); Debug.Log(json); return json;
        }
        public static void LogActualToePlan() { ObserveActualToePlan(); }
        static Match Document(string text, long id)
        { var doc = Regex.Match(text, @"(?m)^--- !u!4 &" + id + @"\r?$[\s\S]*?(?=^--- !u!|\z)"); Require(doc.Success, "exact native Transform document"); return doc; }
        static string Scalar(string doc, string field, string component, string value)
        {
            string pattern = @"(?m)^  " + field + @": \{x: (?<x>[^,]+), y: (?<y>[^,]+), z: (?<z>[^,}]+)" + (field == "m_LocalRotation" ? @", w: (?<w>[^}]+)" : "") + @"\}\r?$";
            var match = Regex.Match(doc, pattern); Require(match.Success, "native field " + field); var group = match.Groups[component];
            Require(group.Success, "native scalar " + component); return doc.Remove(group.Index, group.Length).Insert(group.Index, value);
        }
        public static string RewriteKnownRampDocument(string document, Vector3 position, float scaleZ, Quaternion rotation, float pitchHint)
        {
            string result = Scalar(document, "m_LocalPosition", "y", Number(position.y)); result = Scalar(result, "m_LocalPosition", "z", Number(position.z)); result = Scalar(result, "m_LocalScale", "z", Number(scaleZ));
            result = Scalar(result, "m_LocalRotation", "x", Number(rotation.x)); result = Scalar(result, "m_LocalRotation", "w", Number(rotation.w));
            return Scalar(result, "m_LocalEulerAnglesHint", "x", Number(pitchHint));
        }
        public static void RepairKnownShortenedRampTransition()
        {
            Require(!EditorApplication.isPlaying && !EditorApplication.isCompiling, "stopped compiled explicit author");
            string path = CampingWilderness.SharedPrefab; byte[] before = File.ReadAllBytes(path), meta = File.ReadAllBytes(path + ".meta");
            Require(Hash(before) == ReviewedPrefab && Hash(meta) == ReviewedMeta, "exact known R45 original hashes; custom/already repaired prefab refused");
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path); Require(asset != null, "original shared prefab"); var profiles = Plan(asset);
            string text = Utf8.GetString(before); var changes = new List<Change>();
            foreach (var profile in profiles)
            {
                var doc = Document(text, profile.transformId); float side = Mathf.Sign(profile.position.z);
                string modified = RewriteKnownRampDocument(doc.Value, profile.position, profile.scale.z, profile.rotation, side * profile.pitch);
                changes.Add(new Change { id = profile.transformId, before = doc.Value, after = modified }); text = text.Remove(doc.Index, doc.Length).Insert(doc.Index, modified);
            }
            string reversed = text; foreach (var change in changes) { var doc = Document(reversed, change.id); Require(doc.Value == change.after, "reviewed output document"); reversed = reversed.Remove(doc.Index, doc.Length).Insert(doc.Index, change.before); }
            Require(Utf8.GetBytes(reversed).SequenceEqual(before), "every non-ramp record and unmodified Transform field must remain byte-exact");
            byte[] after = Utf8.GetBytes(text); string folder = Path.GetFullPath("Logs/TruckSports/Impossible07/CampingHammockTransitionRepair/" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff") + "-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder); string original = Path.Combine(folder, "original.prefab"), output = Path.Combine(folder, "transition.prefab");
            File.WriteAllBytes(original, before); File.WriteAllBytes(original + ".meta", meta); File.WriteAllBytes(output, after);
            var receipt = new Receipt { status = "PREFLIGHT_COMPLETE_BEFORE_MUTATION", asset = path, beforeSha256 = Hash(before), afterSha256 = Hash(after), metaSha256 = Hash(meta), beforeFile = original,
                afterFile = output, profiles = profiles, changes = changes.ToArray(), ramps = 8, platforms = 4, trees = asset.GetComponent<CampingWilderness>().terrains.Sum(t => t.terrainData.treeInstanceCount), onlyReviewedRampTransformFields = true, metaExact = true };
            File.WriteAllText(Path.Combine(folder, "receipt.json"), JsonUtility.ToJson(receipt, true), Utf8);
            Require(File.ReadAllBytes(path).SequenceEqual(before) && File.ReadAllBytes(path + ".meta").SequenceEqual(meta), "unchanged source race guard");
            File.WriteAllBytes(path, after); AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            Require(File.ReadAllBytes(path).SequenceEqual(after) && File.ReadAllBytes(path + ".meta").SequenceEqual(meta), "exact output and preserved meta");
            receipt.status = "EIGHT_RAMP_EXTENDED_GROUND_PROFILE_TRANSFORMS_ONLY_NATIVE_TRANSITION_ACCEPTANCE_PENDING";
            File.WriteAllText(Path.Combine(folder, "receipt.json"), JsonUtility.ToJson(receipt, true), Utf8); Debug.Log(JsonUtility.ToJson(receipt, true));
        }
        public static string VerifyLastRepairPreservation() => VerifyRepairPreservation(null);
        public static string VerifyTransitionDescendsFrom(string exactPreviousSha256)
        {
            Require(!string.IsNullOrEmpty(exactPreviousSha256), "explicit prior artifact SHA required for transition chain");
            return VerifyRepairPreservation(exactPreviousSha256);
        }
        static string VerifyRepairPreservation(string requiredBeforeSha256)
        {
            string folder = Path.GetFullPath("Logs/TruckSports/Impossible07/CampingHammockTransitionRepair");
            var path = Directory.GetFiles(folder, "receipt.json", SearchOption.AllDirectories).OrderBy(File.GetLastWriteTimeUtc).Last();
            var receipt = JsonUtility.FromJson<Receipt>(File.ReadAllText(path, Utf8));
            if (requiredBeforeSha256 != null) Require(receipt.beforeSha256 == requiredBeforeSha256 && Hash(File.ReadAllBytes(receipt.beforeFile)) == requiredBeforeSha256, "current transition must descend from the exact prior shortening artifact");
            Require(receipt.status == "EIGHT_RAMP_EXTENDED_GROUND_PROFILE_TRANSFORMS_ONLY_NATIVE_TRANSITION_ACCEPTANCE_PENDING" && receipt.ramps == 8 && receipt.platforms == 4 && receipt.trees == 2297, "complete exact reviewed repair receipt");
            byte[] current = File.ReadAllBytes(receipt.asset), actual = File.ReadAllBytes(receipt.afterFile), meta = File.ReadAllBytes(receipt.asset + ".meta");
            Require(Hash(actual) == receipt.afterSha256 && Hash(meta) == receipt.metaSha256, "retained exact R52 output and current original metadata");
            // R52's tested ramp artifact remains immutable. A later authorized
            // water stage may change only its nineteen renderer/filter refs.
            // Its own strict reverse proof must return to this exact artifact.
            if (!current.SequenceEqual(actual)) CampingWaterPresentationAuthoring.VerifyWaterDescendsFrom(receipt.afterSha256);
            Require(receipt.beforeSha256 == ReviewedPrefab && Hash(File.ReadAllBytes(receipt.beforeFile)) == ReviewedPrefab && receipt.metaSha256 == ReviewedMeta,
                "strict known shortening source and original metadata");
            Require(receipt.profiles.Length == 8 && receipt.changes.Length == 8 && receipt.profiles.Select(p => p.transformId).Distinct().Count() == 8 &&
                receipt.changes.Select(c => c.id).OrderBy(id => id).SequenceEqual(receipt.profiles.Select(p => p.transformId).OrderBy(id => id)), "only the eight reviewed ramp identities may change");
            foreach (var profile in receipt.profiles)
            {
                Require(profile.scale.x == 10 && profile.scale.y == .25f && Mathf.Abs(profile.pitch) <= MaximumPitch && profile.horizontalSpan >= 5.9399f && profile.horizontalSpan <= MaximumHorizontalSpan,
                    "strict current transition width/thickness/pitch/extension bounds");
                var change = receipt.changes.Single(c => c.id == profile.transformId);
                Require(change.after == RewriteKnownRampDocument(change.before, profile.position, profile.scale.z, profile.rotation, Mathf.Sign(profile.position.z) * profile.pitch),
                    "only approved ramp profile scalars may change within each Transform");
            }
            string reversed = Utf8.GetString(actual);
            foreach (var change in receipt.changes) { var doc = Document(reversed, change.id); Require(doc.Value == change.after, "current ramp document"); reversed = reversed.Remove(doc.Index, doc.Length).Insert(doc.Index, change.before); }
            Require(Utf8.GetBytes(reversed).SequenceEqual(File.ReadAllBytes(receipt.beforeFile)) && meta.SequenceEqual(File.ReadAllBytes(receipt.beforeFile + ".meta")), "all other prefab/meta bytes exact");
            var saved = AssetDatabase.LoadAssetAtPath<GameObject>(receipt.asset);
            var ramps = saved.GetComponentsInChildren<BoxCollider>(true).Where(c => c.name == RampName).ToArray(); Require(ramps.Length == 8, "eight actual imported ramp colliders");
            foreach (var ramp in ramps)
            {
                Require(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(ramp.transform, out string ignored, out long id), "actual persistent ramp");
                var profile = receipt.profiles.Single(p => p.transformId == id);
                Require((ramp.transform.localPosition - profile.position).sqrMagnitude < 1e-8f && (ramp.transform.localScale - profile.scale).sqrMagnitude < 1e-8f &&
                    Mathf.Abs(Quaternion.Dot(ramp.transform.localRotation, profile.rotation)) > .999999f, "actual imported profile frame");
                float side = Mathf.Sign(ramp.transform.localPosition.z); var frame = ramp.transform.parent;
                var corners = Corners(frame, ramp, ramp.transform.localPosition, ramp.transform.localRotation, ramp.transform.localScale);
                var top = new[] { corners[2], corners[3], corners[6], corners[7] }.OrderBy(v => side * v.z).ToArray();
                Require(Mathf.Abs(top[0].y - InnerTop) < 1e-4f && Mathf.Abs(top[3].y - profile.toeMin - ToeClearance) < 1e-4f &&
                    Mathf.Abs(Gap(frame, saved.GetComponent<CampingWilderness>().hammocks.Single(h => h.transform == frame).deck, ramp, profile.pitch, ramp.transform.localScale.z, Mathf.Abs(ramp.transform.localPosition.z)) - RestSeam) < 1e-5f,
                    "actual imported top edges and seam");
            }
            return receipt.status;
        }
    }
}

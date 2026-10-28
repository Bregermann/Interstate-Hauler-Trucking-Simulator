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
    // R42 native contact evidence: both original static ramp tips support the
    // underside of the 35000kg deck, lifting its entrance above the ramps.
    // This explicit repair changes ONLY eight known ramp Transform scale.z
    // scalars in the shared prefab YAML; it never regenerates the world.
    public static class CampingHammockRampAuthoring
    {
        public const float Seam = .03f;
        const string RampName = "Actual gently sloped hammock approach";
        const string ReviewedOriginalSha = "e76ca3b78941f3042ef757fd9397eaa310a27908d9c6d9ead386176328dd6696";
        const string ReviewedMetaSha = "e49c815a758743e10c9aa4ac9878f61d3af1da5adf8aca016dca291ae5001ff1";
        static readonly UTF8Encoding Utf8 = new UTF8Encoding(false, true);
        [Serializable] public sealed class Edit
        {
            public long transformId;
            public string hierarchy, oldZ, newZ;
            public float oldInnerGap, newInnerGap;
        }
        [Serializable] public sealed class Receipt
        {
            public string status, asset, beforeSha256, afterSha256, metaSha256, backup, beforeFile, afterFile;
            public int ramps, platforms, trees;
            public bool allNonRampBytesExact, metaExact;
            public Edit[] edits;
        }
        static void Require(bool valid, string message)
        { if (!valid) throw new InvalidOperationException("Hammock ramp repair preserves customized content: " + message); }
        static string Hash(byte[] bytes)
        { using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant(); }
        static Vector3[] Corners(Transform frame, BoxCollider shape, float zScale)
        {
            Require(shape.transform.parent == frame, "known direct-child box frame required");
            var scale = shape.transform.localScale; scale.z = zScale;
            var matrix = Matrix4x4.TRS(shape.transform.localPosition, shape.transform.localRotation, scale);
            var result = new Vector3[8];
            for (int i = 0; i < 8; i++) result[i] = matrix.MultiplyPoint3x4(shape.center + Vector3.Scale(shape.size * .5f,
                new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1)));
            return result;
        }
        public static float InnerGap(Transform frame, BoxCollider deck, BoxCollider ramp, float zScale)
        {
            float side = Mathf.Sign(ramp.transform.localPosition.z);
            return Corners(frame, ramp, zScale).Min(v => side * v.z) - Corners(frame, deck, deck.transform.localScale.z).Max(v => side * v.z);
        }
        public static float DeriveLength(Transform frame, BoxCollider deck, BoxCollider ramp)
        {
            Require(frame != null && deck != null && ramp != null, "actual boxes required");
            Require((deck.transform.localPosition - new Vector3(0, .9f, 0)).sqrMagnitude < 1e-8f &&
                (deck.transform.localScale - new Vector3(10, .35f, 16)).sqrMagnitude < 1e-8f && Quaternion.Angle(deck.transform.localRotation, Quaternion.identity) < .001f, "original deck frame");
            Require(deck.center.sqrMagnitude < 1e-8f && (deck.size - Vector3.one).sqrMagnitude < 1e-8f, "original deck collider");
            float side = Mathf.Sign(ramp.transform.localPosition.z);
            Require(Mathf.Abs(side) == 1 && (ramp.transform.localPosition - new Vector3(0, .4f, side * 11)).sqrMagnitude < 1e-8f &&
                Mathf.Abs(ramp.transform.localScale.x - 10) < 1e-5f && Mathf.Abs(ramp.transform.localScale.y - .25f) < 1e-5f &&
                Quaternion.Angle(ramp.transform.localRotation, Quaternion.Euler(side * 7, 0, 0)) < .001f, "original ramp frame");
            Require(ramp.center.sqrMagnitude < 1e-8f && (ramp.size - Vector3.one).sqrMagnitude < 1e-8f && !ramp.isTrigger && ramp.enabled && ramp.attachedRigidbody == null, "original static ramp collider");
            float low = 0, high = 7;
            for (int i = 0; i < 32; i++)
            { float middle = (low + high) * .5f; if (InnerGap(frame, deck, ramp, middle) > Seam) low = middle; else high = middle; }
            float result = (low + high) * .5f;
            Require(result > 5.9f && result < 6.1f && Mathf.Abs(InnerGap(frame, deck, ramp, result) - Seam) < 1e-5f, "derived physical seam");
            return result;
        }
        static List<Edit> Plan(GameObject asset, bool installed)
        {
            var world = asset.GetComponent<CampingWilderness>();
            Require(world != null && world.hammocks != null && world.hammocks.Length == 4 && world.terrains != null && world.terrains.Length == 4, "complete shared original world");
            var edits = new List<Edit>();
            for (int i = 0; i < 4; i++)
            {
                var h = world.hammocks[i]; Require(h != null && h.deck != null && h.platform != null, "original hammock references");
                Require(Mathf.Abs(h.transform.localPosition.x - (45 + i * 17)) < 1e-4f && Mathf.Abs(h.transform.localPosition.z + 65) < 1e-4f, "original hammock layout");
                Require(h.platform.gameObject == h.deck.gameObject && !h.platform.isKinematic && h.platform.mass == 35000 &&
                    h.platform.linearDamping == 1 && h.platform.angularDamping == 3 && h.anchors.Length == 4 && h.ropes.Length == 4, "original dynamic platform authority");
                var springs = h.platform.GetComponents<SpringJoint>(); Require(springs.Length == 4, "four original springs");
                for (int j = 0; j < 4; j++)
                {
                    var s = springs[j];
                    Require(s.connectedBody == null && !s.autoConfigureConnectedAnchor && s.minDistance == 0 && s.maxDistance == 4.5f &&
                        s.spring == 220000 && s.damper == 45000 && Mathf.Abs(s.tolerance - .02f) < 1e-6f &&
                        (s.anchor - new Vector3(j % 2 == 0 ? -.45f : .45f, .2f, j < 2 ? -.45f : .45f)).sqrMagnitude < 1e-8f &&
                        (s.connectedAnchor - h.anchors[j].position).sqrMagnitude < 1e-8f, "original spring anchors/parameters");
                }
                var ramps = h.GetComponentsInChildren<BoxCollider>(true).Where(c => c.name == RampName).ToArray();
                Require(ramps.Length == 2, "two original ramps per hammock");
                foreach (var ramp in ramps)
                {
                    Require(ramp.GetComponents<Component>().All(c => c != null && (c is Transform || c is MeshFilter || c is MeshRenderer || c is BoxCollider)), "unknown ramp component");
                    float length = DeriveLength(h.transform, h.deck, ramp);
                    Require(Mathf.Abs(ramp.transform.localScale.z - (installed ? length : 7)) < 1e-5f, installed ? "expected repaired ramp length" : "original unrepaired ramp length");
                    Require(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(ramp.transform, out string guid, out long id) && guid == AssetDatabase.AssetPathToGUID(CampingWilderness.SharedPrefab), "persistent original Transform identity");
                    edits.Add(new Edit { transformId = id, hierarchy = i + "/" + Mathf.Sign(ramp.transform.localPosition.z), newZ = length.ToString("R", CultureInfo.InvariantCulture),
                        oldInnerGap = InnerGap(h.transform, h.deck, ramp, 7), newInnerGap = InnerGap(h.transform, h.deck, ramp, length) });
                }
            }
            Require(edits.Select(e => e.transformId).Distinct().Count() == 8, "eight unique transforms"); return edits;
        }
        public static string ValidateInstalled()
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(CampingWilderness.SharedPrefab); Require(asset != null, "existing shared prefab");
            return JsonUtility.ToJson(new Receipt { status = "EIGHT_NONOVERLAPPING_RAMPS_ORIGINAL_DYNAMIC_PLATFORMS", ramps = Plan(asset, true).Count, platforms = 4,
                trees = asset.GetComponent<CampingWilderness>().terrains.Sum(t => t.terrainData.treeInstanceCount) });
        }
        [Serializable] sealed class HistoricalProof
        {
            public string status = "RETAINED_R44_EIGHT_SCALAR_ARTIFACT_EXACT_CURRENT_STAGE_STRICTLY_VALIDATED", receiptFile, beforeSha256, shortenedSha256, currentSha256, currentStage;
        }
        public static string VerifyRetainedShorteningAndCurrentChain()
        {
            string folder = Path.GetFullPath("Logs/TruckSports/Impossible07/CampingHammockRampRepair");
            string path = Directory.GetFiles(folder, "receipt.json", SearchOption.AllDirectories).OrderBy(File.GetLastWriteTimeUtc).Last();
            var receipt = JsonUtility.FromJson<Receipt>(File.ReadAllText(path, Utf8));
            Require(receipt.status == "ONLY_EIGHT_RAMP_Z_SCALES_CHANGED_ALL_OTHER_PREFAB_META_BYTES_EXACT" && receipt.asset == CampingWilderness.SharedPrefab &&
                receipt.ramps == 8 && receipt.platforms == 4 && receipt.trees == 2297 && receipt.allNonRampBytesExact && receipt.metaExact, "complete retained original R44 proof");
            byte[] before = File.ReadAllBytes(receipt.beforeFile), shortened = File.ReadAllBytes(receipt.afterFile), meta = File.ReadAllBytes(receipt.beforeFile + ".meta");
            Require(Hash(before) == ReviewedOriginalSha && Hash(before) == receipt.beforeSha256 && Hash(meta) == ReviewedMetaSha && Hash(meta) == receipt.metaSha256,
                "retained original R44 bytes/meta");
            Require(Hash(shortened) == receipt.afterSha256 && receipt.afterSha256 == "ca37a21bcafe3cc6f202ab2570bc7b0c79b4c3fa3ba729c45d75a0706ee87f47",
                "retained actual R44 shortened artifact");
            Require(receipt.edits.Length == 8 && receipt.edits.Select(e => e.transformId).Distinct().Count() == 8, "original eight scalar identities");
            string reversed = Utf8.GetString(shortened);
            foreach (var edit in receipt.edits)
            {
                var doc = Regex.Match(reversed, @"(?m)^--- !u!4 &" + edit.transformId + @"\r?$[\s\S]*?(?=^--- !u!|\z)");
                Require(doc.Success, "retained original ramp Transform"); var scale = ScaleMatch(reversed, edit.transformId);
                Require(scale.Groups[3].Value == edit.newZ && Math.Abs(double.Parse(edit.oldZ, CultureInfo.InvariantCulture) - 7) < 1e-5 &&
                    Math.Abs(double.Parse(edit.newZ, CultureInfo.InvariantCulture) - 5.953912) < 2e-5, "exact retained R44 scalar delta");
                int offset = doc.Index + scale.Groups[3].Index; reversed = reversed.Remove(offset, edit.newZ.Length).Insert(offset, edit.oldZ);
            }
            Require(Utf8.GetBytes(reversed).SequenceEqual(before), "all non-ramp R44 artifact bytes exact");
            byte[] current = File.ReadAllBytes(receipt.asset), currentMeta = File.ReadAllBytes(receipt.asset + ".meta"); Require(currentMeta.SequenceEqual(meta), "current original prefab GUID/meta");
            string currentStage;
            if (current.SequenceEqual(shortened)) { ValidateInstalled(); currentStage = "EXACT_R44_SHORTENED_STAGE"; }
            else
            {
                CampingHammockTransitionAuthoring.VerifyTransitionDescendsFrom(receipt.afterSha256);
                currentStage = "STRICT_CURRENT_EXTENDED_TRANSITION_FROM_EXACT_R44_ARTIFACT";
            }
            return JsonUtility.ToJson(new HistoricalProof { receiptFile = path, beforeSha256 = Hash(before), shortenedSha256 = Hash(shortened), currentSha256 = Hash(current), currentStage = currentStage });
        }
        public static string ExtractScaleZ(string text, long id) => ScaleMatch(text, id).Groups[3].Value;
        static Match ScaleMatch(string text, long id)
        {
            var doc = Regex.Match(text, @"(?m)^--- !u!4 &" + id + @"\r?$[\s\S]*?(?=^--- !u!|\z)");
            Require(doc.Success, "original Transform YAML document " + id);
            var scale = Regex.Match(doc.Value, @"(?m)^  m_LocalScale: \{x: ([^,]+), y: ([^,]+), z: ([^}]+)\}\r?$");
            Require(scale.Success, "single native scale scalar " + id);
            // Return a match in the complete text so replacements are byte-local.
            var full = Regex.Match(text.Substring(doc.Index), @"(?m)^  m_LocalScale: \{x: ([^,]+), y: ([^,]+), z: ([^}]+)\}\r?$");
            return full;
        }
        public static void RepairKnownOriginalRampOverlap()
        {
            Require(!EditorApplication.isPlaying && !EditorApplication.isCompiling, "stopped compiled author only");
            string path = CampingWilderness.SharedPrefab; var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path); Require(asset != null, "existing prefab; generation is forbidden");
            byte[] before = File.ReadAllBytes(path), meta = File.ReadAllBytes(path + ".meta"); string oldText = Utf8.GetString(before);
            Require(Hash(before) == ReviewedOriginalSha && Hash(meta) == ReviewedMetaSha, "exact reviewed R42 original prefab/meta hashes; unknown or already repaired asset refused before mutation");
            Require(oldText.StartsWith("%YAML", StringComparison.Ordinal) || oldText.StartsWith("\ufeff%YAML", StringComparison.Ordinal), "native YAML");
            var edits = Plan(asset, false); string text = oldText;
            foreach (var edit in edits)
            {
                var doc = Regex.Match(text, @"(?m)^--- !u!4 &" + edit.transformId + @"\r?$[\s\S]*?(?=^--- !u!|\z)");
                Require(doc.Success, "original transform document");
                var match = ScaleMatch(text, edit.transformId); edit.oldZ = match.Groups[3].Value;
                Require(Math.Abs(double.Parse(edit.oldZ, CultureInfo.InvariantCulture) - 7) < 1e-5, "unmodified old scale scalar");
                int offset = doc.Index + match.Groups[3].Index;
                text = text.Remove(offset, edit.oldZ.Length).Insert(offset, edit.newZ);
            }
            byte[] after = Utf8.GetBytes(text); string reverse = text;
            foreach (var edit in edits)
            {
                var doc = Regex.Match(reverse, @"(?m)^--- !u!4 &" + edit.transformId + @"\r?$[\s\S]*?(?=^--- !u!|\z)");
                var match = ScaleMatch(reverse, edit.transformId); int offset = doc.Index + match.Groups[3].Index;
                Require(match.Groups[3].Value == edit.newZ, "reviewed new scalar"); reverse = reverse.Remove(offset, edit.newZ.Length).Insert(offset, edit.oldZ);
            }
            Require(Utf8.GetBytes(reverse).SequenceEqual(before), "ALL non-ramp bytes must remain exact");
            string backup = Path.GetFullPath("Logs/TruckSports/Impossible07/CampingHammockRampRepair/" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff") + "-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(backup); string beforeFile = Path.Combine(backup, "original.prefab"), afterFile = Path.Combine(backup, "repaired.prefab");
            File.WriteAllBytes(beforeFile, before); File.WriteAllBytes(beforeFile + ".meta", meta); File.WriteAllBytes(afterFile, after);
            var receipt = new Receipt { status = "PREFLIGHT_COMPLETE_BEFORE_MUTATION", asset = path, beforeSha256 = Hash(before), afterSha256 = Hash(after),
                metaSha256 = Hash(meta), backup = backup, beforeFile = beforeFile, afterFile = afterFile, ramps = 8, platforms = 4,
                trees = asset.GetComponent<CampingWilderness>().terrains.Sum(t => t.terrainData.treeInstanceCount), allNonRampBytesExact = true, metaExact = true, edits = edits.ToArray() };
            File.WriteAllText(Path.Combine(backup, "receipt.json"), JsonUtility.ToJson(receipt, true), Utf8);
            Require(File.ReadAllBytes(path).SequenceEqual(before) && File.ReadAllBytes(path + ".meta").SequenceEqual(meta), "source changed after preflight");
            File.WriteAllBytes(path, after); AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            Require(File.ReadAllBytes(path).SequenceEqual(after) && File.ReadAllBytes(path + ".meta").SequenceEqual(meta), "exact output/meta guard");
            ValidateInstalled(); receipt.status = "ONLY_EIGHT_RAMP_Z_SCALES_CHANGED_ALL_OTHER_PREFAB_META_BYTES_EXACT";
            File.WriteAllText(Path.Combine(backup, "receipt.json"), JsonUtility.ToJson(receipt, true), Utf8);
            Debug.Log("CAMPING_HAMMOCK_RAMP_REPAIR " + JsonUtility.ToJson(receipt));
        }
    }
}

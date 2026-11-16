#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using EasyRoads3Dv3;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LWS.TruckDragonKingdom.Editor
{
    public static class DragonKingdomMountainRoadLayoutAuthoring
    {
        public const string Profile = "CircularMountainPass01";
        const string Network = "Assets/EasyRoads3D/Resources/ERRoadNetwork.prefab";
        const string MaterialPath = "Assets/EasyRoads3D/Resources/Materials/roads/road material.mat";
        const string Sdk = "Assets/EasyRoads3D/lib/EasyRoads3Dv3.dll";
        [Serializable] public sealed class Evidence
        {
            public string status = "IN_PROGRESS", error, actualLayoutJson;
            public bool originalSourcesExact, randomStateRestored;
            public List<BuildBudget> buildBudgets = new List<BuildBudget>();
        }
        [Serializable] public sealed class BuildBudget
        {
            public string name;
            public int markerCount, actualShapeNodes, actualExtendedShapeNodes, estimatedLongitudinalStations, conservativeEstimatedVertices, actualSplineSamples, actualMaximumMeshVertices;
            public float resolution, maximumAngleThreshold;
        }
        public static Evidence LastEvidence { get; private set; }
        static void Snapshot(DragonKingdomMountainRoadLayout layout, string phase)
        { LastEvidence.status = phase; LastEvidence.actualLayoutJson = layout == null ? null : JsonUtility.ToJson(layout, true); }

        public static string Digest(byte[] bytes)
        { using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant(); }
        public static string PointsDigest(Vector3[] points) => Digest(Encoding.UTF8.GetBytes(JsonUtility.ToJson(new PointSet { points = points })));
        [Serializable] sealed class PointSet { public Vector3[] points; }

        // Only select measured vendor samples; this does not construct a second
        // curve. Their altitude is an owned road-authoring grade, as in the
        // existing flat approach authoring. Every final segment is checked.
        static Vector3[] SelectActualSamples(Vector3[] actual, Vector3 start, Vector3 end, out float endpointDistance)
        {
            ValidateVendorCurveSamples(actual, "Semantic station selection");
            float Xz(Vector3 a, Vector3 b) => Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));
            int first = Enumerable.Range(0, actual.Length).OrderBy(i => Xz(actual[i], start)).First(), lastIndex = Enumerable.Range(0, actual.Length).OrderBy(i => Xz(actual[i], end)).First();
            endpointDistance = Mathf.Max(Xz(actual[first], start), Xz(actual[lastIndex], end));
            // Public samples are spaced0.35m and can omit an exact marker
            // station. Graph endpoints themselves stay exact. Their nearest
            // actual physical curve station must meet the original0.3m road
            // contact authority; no new interpolation or analytic arc is made.
            if (first >= lastIndex || endpointDistance > .3f) throw new InvalidOperationException("Actual vendor curve does not physically reach the exact semantic endpoints within original0.3m contact acceptance: " + endpointDistance);
            var result = new List<Vector3> { new Vector3(start.x, 0, start.z) }; float since = 0;
            for (int i = first + 1; i < lastIndex; i++)
            {
                since += Xz(actual[i - 1], actual[i]);
                if (since < 2) continue;
                result.Add(new Vector3(actual[i].x, 0, actual[i].z)); since = 0;
            }
            var last = new Vector3(end.x, 0, end.z);
            if (Xz(result[result.Count - 1], last) < .1f) result[result.Count - 1] = last; else result.Add(last);
            return result.ToArray();
        }
        static bool Finite(Vector3 p) => float.IsFinite(p.x) && float.IsFinite(p.y) && float.IsFinite(p.z);
        static float XzDistance(Vector3 a, Vector3 b) => Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));
        static float Length(Vector3[] points) { float d = 0; for (int i = 1; i < points.Length; i++) d += XzDistance(points[i - 1], points[i]); return d; }
        static Vector3[] Grade(Vector3[] actual, float start, float end, float startFlat, float endFlat)
        {
            float length = Length(actual), available = length - startFlat - endFlat;
            if (available <= 0 || Mathf.Abs(end - start) / available > .18f) throw new InvalidOperationException("Owned grade exceeds the original18% actual route limit.");
            var result = actual.ToArray(); float d = 0;
            for (int i = 0; i < result.Length; i++)
            {
                if (i > 0) d += XzDistance(actual[i - 1], actual[i]);
                result[i].y = Mathf.Lerp(start, end, Mathf.Clamp01((d - startFlat) / available));
            }
            result[0].y = start; result[result.Length - 1].y = end; return result;
        }
        static float MinimumRadius(Vector3[] points)
        {
            // Measured8m chords suppress float32 curvature noise in0.35m vendor
            // tessellation. This is preflight evidence, not a maximum-curvature
            // theorem or a replacement for actual contour/physical acceptance.
            var sampled = new List<Vector3> { points[0] }; float d = 0;
            for (int i = 1; i < points.Length; i++) { d += XzDistance(points[i - 1], points[i]); if (d >= 8) { sampled.Add(points[i]); d = 0; } }
            float minimum = float.PositiveInfinity;
            for (int i = 1; i + 1 < sampled.Count; i++)
            {
                var a = sampled[i - 1]; var b = sampled[i]; var c = sampled[i + 1];
                double cross = Math.Abs((b.x - a.x) * (double)(c.z - a.z) - (b.z - a.z) * (double)(c.x - a.x));
                if (cross < .0001) continue;
                float radius = (float)(XzDistance(a, b) * (double)XzDistance(b, c) * XzDistance(c, a) / (2 * cross)); minimum = Mathf.Min(minimum, radius);
            }
            return minimum;
        }
        public const int MaximumVendorSamples = 16000;
        public static void ValidateVendorCurveSamples(Vector3[] points, string context)
        {
            if (points == null || points.Length < 3 || points.Length > MaximumVendorSamples)
                throw new InvalidOperationException(context + ": actual SDK sample count outside3.." + MaximumVendorSamples + ": " + (points == null ? -1 : points.Length));
            if (points.Any(p => !Finite(p)) || Length(points) > 5000)
                throw new InvalidOperationException(context + ": nonfinite or over5km actual SDK curve; refuse further processing.");
        }
        public static Vector3[][] QuarterCircleGuides()
        {
            var start = new Vector3(1100, 190, 1320); var middle = new Vector3(1340, 215, 1080); var end = new Vector3(1100, 240, 840);
            // These four-marker inputs request vendor quarter circles. They are
            // tangent guards, never replacement circle samples or physical mesh.
            return new[] {
                new[] { start + Vector3.left * 100, start, middle, middle + Vector3.back * 100 },
                new[] { middle + Vector3.forward * 100, middle, end, end + Vector3.left * 100 }
            };
        }
        public static int EstimateVendorVertexBudget(Vector3[] points, bool[] circular, int shapeNodes, int extendedShapeNodes, float resolution, float angleThreshold)
        {
            if (points == null || circular == null || points.Length != circular.Length || points.Length < 2 || shapeNodes < 0 || extendedShapeNodes < 0 || !float.IsFinite(resolution) || resolution < .35f || !float.IsFinite(angleThreshold) || angleThreshold <= 0)
                throw new InvalidOperationException("Actual finite supported vendor shape/resolution budget required.");
            long stations = 1;
            for (int i = 0; i + 1 < points.Length; i++)
            {
                if (!Finite(points[i]) || !Finite(points[i + 1])) throw new InvalidOperationException("Nonfinite budget markers refused.");
                // A bounded quarter/connector request permits at most twice its
                // chord for this PREVIEW budget. A malformed vendor radius can
                // still exceed this estimate: native returned counts are checked
                // independently. This never constructs replacement curve points.
                float distance = Vector3.Distance(points[i], points[i + 1]) * (circular[i] ? 2 : 1);
                if (!float.IsFinite(distance) || distance > 5000) throw new InvalidOperationException("Unbounded actual marker leg before vendor Refresh.");
                stations += Mathf.Max(Mathf.CeilToInt(distance / resolution), circular[i] ? Mathf.CeilToInt(360 / angleThreshold) : 1) + 1;
            }
            // Account for both pavement/extended shape channels and duplicated
            // seam vertices conservatively. Empty SDK shape means its two-edge
            // default; no shape mutation is made by this estimate.
            long transverse = Math.Max(2, shapeNodes) + extendedShapeNodes + 2;
            long vertices = stations * transverse * 2;
            if (stations > MaximumVendorSamples || vertices >= 65000)
                throw new InvalidOperationException("Refuse oversized vendor build BEFORE Refresh: stations=" + stations + " conservativeVertices=" + vertices + " actualShape=" + shapeNodes + "/" + extendedShapeNodes);
            return (int)vertices;
        }
        static ERRoad MakeRoad(ERRoadNetwork network, ERModularBase owner, ERRoadType type, string name, Vector3[] points, ERMarkerControlType[] controls)
        {
            if (points == null || controls == null || points.Length != controls.Length || points.Length < 2 || points.Length > 4096 || points.Any(p => !Finite(p)))
                throw new InvalidOperationException("Bounded finite matched vendor markers/controls required: " + name);
            // The points overload builds immediately, before a later resolution
            // assignment can protect its first build. Configure an EMPTY road.
            var road = network.CreateRoad(name, type);
            road.SetTerrainDeformation(false, false); road.SnapToTerrain(false, false); road.SetResolution(.35f, false);
            road.SetRandomBumpiness(0, 0, 5, 15, false); road.SetRandomTilting(0, 0, 5, 15, false);
            road.AddMarkers(points, false);
            for (int i = 0; i < points.Length; i++)
            {
                if (!road.SetMarkerControlType(i, controls[i], false)) throw new InvalidOperationException("SDK rejected requested marker control: " + name + "/" + i);
                road.SetIndent(28, i); road.SetSurrounding(110, i);
            }
            var actual = DragonKingdomWorldAuthoring.ConfigureOwnedNativeRoadGlobals(road, type, owner, 28, 110);
            var budget = new BuildBudget { name = name, markerCount = points.Length, resolution = road.GetResolution(), maximumAngleThreshold = actual.angleTreshold, actualShapeNodes = actual.roadShape.Count, actualExtendedShapeNodes = actual.rt.roadShapeExt.Count };
            LastEvidence.buildBudgets.Add(budget);
            budget.conservativeEstimatedVertices = EstimateVendorVertexBudget(points, controls.Select(c => c == ERMarkerControlType.Circular).ToArray(), budget.actualShapeNodes, budget.actualExtendedShapeNodes, budget.resolution, budget.maximumAngleThreshold);
            budget.estimatedLongitudinalStations = budget.conservativeEstimatedVertices / ((Math.Max(2, budget.actualShapeNodes) + budget.actualExtendedShapeNodes + 2) * 2);
            Debug.Log("OWNED_MOUNTAIN_VENDOR_BUILD_BUDGET " + JsonUtility.ToJson(budget));
            road.Refresh();
            var samples = road.GetSplinePointsCenter(); ValidateVendorCurveSamples(samples, name); budget.actualSplineSamples = samples.Length;
            budget.actualMaximumMeshVertices = road.roadScript.GetComponentsInChildren<MeshFilter>(true).Where(f => f.sharedMesh != null).Select(f => f.sharedMesh.vertexCount).DefaultIfEmpty(0).Max();
            if (budget.actualMaximumMeshVertices >= 65000) throw new InvalidOperationException("Actual returned vendor mesh violates its65k budget: " + name);
            return road;
        }
        static void Append(List<Vector3> destination, Vector3[] source)
        { foreach (var point in source) { if (destination.Count > 0 && (point - destination[destination.Count - 1]).sqrMagnitude < .00000001f) continue; destination.Add(point); } }

        public static ERRoad CreateQualifiedGradeRoad(ERRoadNetwork network,ERModularBase owner,ERRoadType type,string name,Vector3[] points,ERMarkerControlType[] controls,out BuildBudget budget)
        {
            var prior=LastEvidence;var proof=new Evidence();budget=null;
            try{LastEvidence=proof;return MakeRoad(network,owner,type,name,points,controls);}
            finally{budget=proof.buildBudgets.SingleOrDefault();LastEvidence=prior;}
        }
        public static DragonKingdomMountainRoadLayout CreateCandidate(Scene scene, DragonKingdomWorldAuthoring.NativeGradingSourcePlan original, string folder)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || !scene.IsValid() || !scene.isLoaded || SceneManager.GetActiveScene() != scene)
                throw new InvalidOperationException("Stopped owned active authoring scene required.");
            if (!folder.StartsWith("Assets/TruckDragonKingdom/ReviewCandidates/", StringComparison.Ordinal) || folder.Contains("..") || File.Exists(folder + "/Mountain Road Layout.asset"))
                throw new InvalidOperationException("Fresh owned review folder required; never overwrite a designer's saved layout.");
            if (original == null || original.roadId != "caverns-volcano" || original.width != 18 || original.actualGradingMarkers.Length != 17 || original.intervals.Length != 16 || original.intervals.Any(i => i.marker < 5 ? !i.excluded : i.excluded))
                throw new InvalidOperationException("Expected actual legacy portal/interval authority; changed source requires a reviewed recipe.");
            if (UnityEngine.Object.FindObjectsByType<ERModularBase>(FindObjectsSortMode.None).Length != 0) throw new InvalidOperationException("Do not compete with another active vendor authoring network.");
            var sourceHashes = new[] { Network, Network + ".meta", MaterialPath, MaterialPath + ".meta", Sdk, Sdk + ".meta" }.ToDictionary(p => p, p => Digest(File.ReadAllBytes(p)));
            var random = UnityEngine.Random.state; GameObject instance = null; DragonKingdomMountainRoadLayout layout = null;
            LastEvidence = new Evidence();
            try
            {
                instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Network), scene);
                var owner = instance.GetComponent<ERModularBase>(); owner.RoadNetworkInit(); var network = new ERRoadNetwork(owner);
                var type = new ERRoadType { roadTypeName = "Owned circular mountain route", roadWidth = 18, hasMeshCollider = true, roadMaterial = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath) };
                DragonKingdomWorldAuthoring.RegisterOwnedNativeGradingType(type, owner, 28, 110);
                layout = ScriptableObject.CreateInstance<DragonKingdomMountainRoadLayout>(); layout.originalUnderground = original.underground;
                layout.sdkSha256 = sourceHashes[Sdk]; layout.originalGradingPlanSha256 = Digest(Encoding.UTF8.GetBytes(JsonUtility.ToJson(original))); layout.originalRoadSpecificationSha256 = PointsDigest(original.originalRoadPoints);
                layout.preservedPrefix = original.actualGradingMarkers.Take(6).ToArray(); var portal = layout.preservedPrefix[5];
                var heading = original.actualGradingMarkers[6] - portal; heading.y = 0; heading.Normalize();
                var arcStart = new Vector3(1100, 190, 1320);
                var chord = arcStart - portal; chord.y = 0; chord.Normalize();
                // These are authoring tangent guards, not generated curve
                // samples. The supported Circular controller applies from its
                // selected marker to the NEXT marker based on the prior angle
                // (installed scene.html). It must have a following marker;
                // the SDK terminal-circular branch extrapolates instead.
                var connectorExitHeading = 2 * Vector3.Dot(heading, chord) * chord - heading;
                layout.connectorGuides = new[] { portal - heading * 100, portal, arcStart, arcStart + connectorExitHeading * 100 };
                var quarterGuides = QuarterCircleGuides();
                layout.circularGuides = quarterGuides.SelectMany(p => p).ToArray();
                var connectorPlanar = layout.connectorGuides.Select(p => new Vector3(p.x, 0, p.z)).ToArray();
                var controls = new[] { ERMarkerControlType.StraightXZY, ERMarkerControlType.Circular, ERMarkerControlType.StraightXZY, ERMarkerControlType.StraightXZY };
                var connector = MakeRoad(network, owner, type, "Owned mountain guarded circular connector", connectorPlanar, controls);
                layout.actualConnectorControls = Enumerable.Range(0, 4).Select(i => connector.GetMarkerControlType(i).ToString()).ToArray();
                layout.actualConnectorMarkers = connector.GetMarkerPositions(); layout.actualVendorConnector = connector.GetSplinePointsCenter();
                ValidateVendorCurveSamples(layout.actualVendorConnector, "Actual connector");
                layout.connectorMinimumRadius = MinimumRadius(layout.actualVendorConnector);
                layout.circularQuarters = new OwnedRoadVendorQuarter[2];
                for (int q = 0; q < 2; q++)
                {
                    var guides = quarterGuides[q]; var planar = guides.Select(p => new Vector3(p.x, 0, p.z)).ToArray();
                    var road = MakeRoad(network, owner, type, "Owned mountain guarded circular quarter " + (q + 1), planar, controls);
                    var quarter = new OwnedRoadVendorQuarter { guides = guides, actualMarkers = road.GetMarkerPositions(), actualControls = Enumerable.Range(0, 4).Select(i => road.GetMarkerControlType(i).ToString()).ToArray(), actualSpline = road.GetSplinePointsCenter(), actualRadius = road.GetRadius(1) };
                    layout.circularQuarters[q] = quarter;
                    Snapshot(layout, "ACTUAL_VENDOR_QUARTER_" + (q + 1) + "_CAPTURED_REQUIRES_GEOMETRIC_ACCEPTANCE");
                    ValidateVendorCurveSamples(quarter.actualSpline, "Actual quarter " + (q + 1));
                }
                // Capture BOTH native outputs before accepting their geometry.
                layout.actualCircularMarkers = layout.circularQuarters.SelectMany(q => q.actualMarkers).ToArray();
                layout.actualCircularControls = layout.circularQuarters.SelectMany(q => q.actualControls).ToArray();
                layout.mainCircularRadius = layout.circularQuarters.Min(q => q.actualRadius);
                Snapshot(layout, "BOTH_ACTUAL_VENDOR_QUARTERS_CAPTURED_REQUIRES_GEOMETRIC_ACCEPTANCE");
                foreach (var quarter in layout.circularQuarters)
                {
                    if (!float.IsFinite(quarter.actualRadius) || quarter.actualRadius < 238.25f || quarter.actualRadius > 241.75f)
                        throw new InvalidOperationException("Actual vendor quarter radius does not preserve the240m design/full grading envelope: " + quarter.actualRadius);
                    quarter.selectedSamples = SelectActualSamples(quarter.actualSpline, quarter.guides[1], quarter.guides[2], out quarter.maximumSemanticEndpointSampleDistance);
                    float minZ = Mathf.Min(quarter.guides[1].z, quarter.guides[2].z), maxZ = Mathf.Max(quarter.guides[1].z, quarter.guides[2].z);
                    if (quarter.selectedSamples.Any(p => p.x < 1099.7f || p.x > 1340.3f || p.z < minZ - .3f || p.z > maxZ + .3f))
                        throw new InvalidOperationException("Actual SDK quarter leaves its intended semicircle quadrant; no synthetic fallback.");
                }
                float connectorRequired = 9 + 28 + (580 - portal.y) * .5f + 6.25f;
                if (!float.IsFinite(layout.connectorMinimumRadius) || layout.connectorMinimumRadius < connectorRequired) throw new InvalidOperationException("Actual vendor connector curvature is too tight for its true grading footprint: " + layout.connectorMinimumRadius + "/" + connectorRequired);
                var joined = new List<Vector3>(layout.preservedPrefix);
                var connectorSamples = SelectActualSamples(layout.actualVendorConnector, portal, arcStart, out var connectorEndpointDistance);
                var circularSamples = new List<Vector3>(); foreach (var quarter in layout.circularQuarters) Append(circularSamples, quarter.selectedSamples);
                layout.actualVendorCircular = circularSamples.ToArray();
                ValidateVendorCurveSamples(layout.actualVendorCircular, "Composed actual quarters");
                layout.maximumSemanticEndpointSampleDistance = Mathf.Max(connectorEndpointDistance, layout.circularQuarters.Max(q => q.maximumSemanticEndpointSampleDistance));
                Append(joined, Grade(connectorSamples, portal.y, 190, 16, 34));
                // Grade the complete semicircle once: no artificial flat/kink at
                // the shared native station, preserving the original end flats.
                Append(joined, Grade(layout.actualVendorCircular, 190, 240, 34, 34));
                Append(joined, new[] { layout.sourceEndpoint }); layout.authoritativePoints = joined.ToArray();
                float maximumX = layout.actualVendorCircular.Max(p => p.x); layout.mainTerrainRightMargin = 1600 - maximumX - layout.conservativeMainFootprint;
                ValidateLayout(layout);
                var final = MakeRoad(network, owner, type, "Corrected caverns-volcano physical road", layout.authoritativePoints, Enumerable.Repeat(ERMarkerControlType.StraightXZY, layout.authoritativePoints.Length).ToArray());
                if (!final.GetMarkerPositions().SequenceEqual(layout.authoritativePoints)) throw new InvalidOperationException("Final physical bake changed the authoritative route input.");
                layout.physicalInputPointsSha256 = PointsDigest(final.GetMarkerPositions());
                var nativeColliders = final.roadScript.GetComponentsInChildren<MeshCollider>(true).Where(c => c.enabled && !c.isTrigger && c.sharedMesh != null && c.sharedMesh.vertexCount > 2 && (final.roadScript.surfaceMesh == null || !c.transform.IsChildOf(final.roadScript.surfaceMesh.transform))).ToArray();
                Physics.SyncTransforms();
                Physics.SyncTransforms();
                for (int i = 6; i < layout.authoritativePoints.Length; i++)
                {
                    var p = layout.authoritativePoints[i]; var forward = layout.authoritativePoints[Mathf.Min(i + 1, layout.authoritativePoints.Length - 1)] - layout.authoritativePoints[i - 1]; forward.y = 0; forward.Normalize(); var right = Vector3.Cross(Vector3.up, forward);
                    foreach (float across in new[] { -.35f * layout.width, 0, .35f * layout.width })
                    {
                        var at = p + right * across; layout.physicalQueries++;
                        var hits = new List<RaycastHit>(); foreach (var collider in nativeColliders) if (collider.Raycast(new Ray(at + Vector3.up * 20, Vector3.down), out var hit, 40) && hit.normal.y > .25f) hits.Add(hit);
                        if (hits.Count == 0) { layout.physicalMisses++; continue; }
                        layout.maximumPhysicalHeightError = Mathf.Max(layout.maximumPhysicalHeightError, hits.Min(h => Mathf.Abs(h.point.y - p.y)));
                    }
                }
                Snapshot(layout, "ACTUAL_VENDOR_PHYSICAL_CORRESPONDENCE_CAPTURED");
                if (layout.physicalQueries == 0 || layout.physicalMisses != 0 || layout.maximumPhysicalHeightError > .3f) throw new InvalidOperationException("Actual generated pavement does not support the authoritative outdoor graph within existing0.3m physical contact acceptance.");
                Directory.CreateDirectory(folder + "/Models"); AssetDatabase.Refresh();
                var pieces = new List<OwnedRoadPhysicalPiece>(); int pieceIndex = 0;
                foreach (var collider in nativeColliders)
                {
                    if (final.roadScript.surfaceMesh != null && collider.transform.IsChildOf(final.roadScript.surfaceMesh.transform)) continue;
                    var mesh = UnityEngine.Object.Instantiate(collider.sharedMesh); string path = folder + "/Models/Corrected mountain road " + pieceIndex++ + ".asset"; AssetDatabase.CreateAsset(mesh, path);
                    if (!LWS.TruckSports.Editor.SportsEasyRoadsAuthoring.EquivalentPreservedGeometry(collider.sharedMesh, mesh, out var reason)) throw new InvalidOperationException("Native mesh persistence failed: " + reason);
                    pieces.Add(new OwnedRoadPhysicalPiece { mesh = mesh, nativeSource = collider.name, position = collider.transform.position, rotation = collider.transform.rotation, scale = collider.transform.lossyScale });
                }
                if (pieces.Count == 0) throw new InvalidOperationException("Actual vendor physical road mesh missing.");
                layout.physicalPieces = pieces.ToArray(); AssetDatabase.CreateAsset(layout, folder + "/Mountain Road Layout.asset"); EditorUtility.SetDirty(layout); AssetDatabase.SaveAssetIfDirty(layout);
                foreach (var piece in layout.physicalPieces) AssetDatabase.SaveAssetIfDirty(piece.mesh);
                Snapshot(layout, "OWNED_ROUTE_AND_ACTUAL_VENDOR_PHYSICAL_MESHES_SAVED_REQUIRES_TERRAIN_AND_WORLD_ACCEPTANCE");
                return layout;
            }
            catch (Exception error) { if (layout != null) Snapshot(layout, "REJECTED_OWNED_MOUNTAIN_ROUTE_AUTHORING"); LastEvidence.error = error.ToString(); throw; }
            finally
            {
                if (instance != null) UnityEngine.Object.DestroyImmediate(instance); UnityEngine.Random.state = random;
                LastEvidence.randomStateRestored = UnityEngine.Random.state.Equals(random);
                LastEvidence.originalSourcesExact = sourceHashes.All(p => File.Exists(p.Key) && Digest(File.ReadAllBytes(p.Key)) == p.Value);
                if (!LastEvidence.originalSourcesExact || !LastEvidence.randomStateRestored) throw new InvalidOperationException("Owned route authoring modified an original vendor source or incoming Random state.");
                if (layout != null && !EditorUtility.IsPersistent(layout)) UnityEngine.Object.DestroyImmediate(layout);
            }
        }

        public static void ValidateLayout(DragonKingdomMountainRoadLayout layout)
        {
            if (layout == null || layout.schema != 1 || layout.profile != Profile || layout.roadId != "caverns-volcano" || layout.width != 18 || layout.preservedPrefix == null || layout.preservedPrefix.Length != 6 || layout.authoritativePoints == null || layout.authoritativePoints.Length < 10)
                throw new InvalidOperationException("Complete declared owned road layout required.");
            if (!layout.authoritativePoints.Take(6).SequenceEqual(layout.preservedPrefix) || layout.authoritativePoints[layout.authoritativePoints.Length - 1] != layout.sourceEndpoint || layout.sourceEndpoint != new Vector3(1040, 240, 840))
                throw new InvalidOperationException("Do not move original cave prefix or volcano semantic endpoint.");
            layout.maximumGrade = 0; layout.measuredLength = 0;
            for (int i = 1; i < layout.authoritativePoints.Length; i++)
            {
                var a = layout.authoritativePoints[i - 1]; var b = layout.authoritativePoints[i]; float distance = XzDistance(a, b);
                if (!Finite(a) || !Finite(b) || distance < .00001f) throw new InvalidOperationException("Finite nondegenerate actual route required.");
                layout.maximumGrade = Mathf.Max(layout.maximumGrade, Mathf.Abs(a.y - b.y) / distance); layout.measuredLength += Vector3.Distance(a, b);
            }
            if (layout.maximumGrade > .18f || layout.mainTerrainRightMargin < 16) throw new InvalidOperationException("Original grade or declared terrain-boundary clearance failed.");
            if (layout.authoritativePoints.Any(p => p.x <= -1600 || p.x >= 1600 || p.z <= -800 || p.z >= 2400)) throw new InvalidOperationException("Actual road leaves continuous playable terrain.");
            string pointsHash = PointsDigest(layout.authoritativePoints);
            if (EditorUtility.IsPersistent(layout) && (layout.authoritativePointsSha256 != pointsHash || layout.physicalInputPointsSha256 != pointsHash || layout.physicalPieces == null || layout.physicalPieces.Length == 0 || layout.physicalPieces.Any(p => p.mesh == null)))
                throw new InvalidOperationException("Edited points or missing mesh bake would split graph and physical authority; explicitly rebake the edited layout.");
            layout.authoritativePointsSha256 = pointsHash;
        }

        // Wiring contract: call only after the saved layout passed its native
        // provenance/physical gates. All unrelated specs are returned unchanged.
        public static DragonKingdomWorldAuthoring.RoadSpec[] ReplaceOwnedSpecification(DragonKingdomWorldAuthoring.RoadSpec[] legacy, DragonKingdomMountainRoadLayout layout)
        {
            ValidateLayout(layout); var old = legacy.Single(r => r.id == layout.roadId);
            if (old.from != layout.from || old.to != layout.to || old.width != layout.width || PointsDigest(old.points) != layout.originalRoadSpecificationSha256)
                throw new InvalidOperationException("The declared original owned road has changed; review the layout instead of silently overriding it.");
            return legacy.Select(r => r.id == layout.roadId ? new DragonKingdomWorldAuthoring.RoadSpec(r.id, r.from, r.to, r.width, layout.authoritativePoints.ToArray()) : r).ToArray();
        }
    }
}
#endif

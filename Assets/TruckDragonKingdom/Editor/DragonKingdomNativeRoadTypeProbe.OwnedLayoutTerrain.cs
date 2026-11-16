#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EasyRoads3Dv3;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LWS.TruckDragonKingdom.Editor
{
    public static partial class DragonKingdomNativeRoadTypeProbe
    {
        public const int MaximumOwnedTerrainOutliers = 128;
        [Serializable] public sealed class OwnedTerrainPhysicalOutlier
        {
            public int vertex, station, marker, tile, gridX, gridZ, protection00, protection10, protection01, protection11;
            public Vector3 roadPoint, terrainColliderPoint, terrainColliderNormal;
            public float terrainDataY, signedHeightError, absoluteHeightError, terrainColliderY;
            public bool underOriginalCavernBounds, withinOriginalCavernVolume, actualSdkBridgeFlag, originalExcludedMarker, terrainColliderHit, sourceTerrainIsHole, outputTerrainIsHole;
        }
        [Serializable] public sealed class OwnedTerrainGridOutlier
        {
            public int x, z, protection00, protection10, protection01, protection11;
            public Vector3 worldBefore, worldAfter;
            public float baselineStep, outputStep, baselineSlopeDegrees, outputSlopeDegrees, changeMetres;
            public bool underOriginalCavernBounds, sourceTerrainIsHole, outputTerrainIsHole;
        }
        public static bool OwnedTerrainUnderOriginalFootprint(Vector3 point, Bounds original)
            => point.x >= original.min.x && point.x <= original.max.x && point.z >= original.min.z && point.z <= original.max.z;
        public static int OwnedPortalEndMarker(bool[] excluded)
        {
            if (excluded == null || excluded.Length < 2 || !excluded[0]) throw new InvalidOperationException("Actual original contiguous underground prefix required.");
            int end = 0; while (end + 1 < excluded.Length && excluded[end + 1]) end++;
            if (end == excluded.Length - 1 || excluded.Skip(end + 1).Any(value => value)) throw new InvalidOperationException("One original underground prefix followed by exposed route required; refuse guessed portal.");
            return end;
        }
        [Serializable] public sealed class OwnedPortalLevelWitness
        {
            public int marker; public Vector3 point; public float actualSegmentLength, requestedStart = 16, requestedEnd = 16, actualStart, actualEnd;
            public string authority = "EasyRoads3Dv3.ERRoad.SetStartLevelDistance/SetEndLevelDistance(int,float,bool); actual public ERMarkerExt fields after final Refresh";
        }
        public static int OwnedTerrainVertexStation(int vertex, int vertices, int stations)
        {
            if (vertex < 0 || vertex >= vertices || stations < 1 || vertices != stations * 2)
                throw new InvalidOperationException("Actual two-edge SDK pavement/station mapping required; refuse invented station metadata.");
            return vertex / 2;
        }
        static byte[] OwnedHolesBytes(TerrainData data)
        {
            var holes = data.GetHoles(0, 0, data.holesResolution, data.holesResolution); var bytes = new byte[holes.Length]; int index = 0;
            foreach (bool value in holes) bytes[index++] = value ? (byte)1 : (byte)0;
            return bytes; // Raw GetHoles boolean0/1; IsHole is queried separately.
        }
        static float OwnedGridStep(float[,] h, int x, int z)
            => Mathf.Max(x < 512 ? Mathf.Abs(h[z, x + 1] - h[z, x]) * 600 : 0, z < 512 ? Mathf.Abs(h[z + 1, x] - h[z, x]) * 600 : 0);
        static OwnedTerrainGridOutlier OwnedGridWitness(float[,] before, float[,] after, int x, int z, Vector3 origin, Bounds underground)
        {
            var at = origin + new Vector3(x * 3.125f, 0, z * 3.125f); float oldStep = OwnedGridStep(before, x, z), newStep = OwnedGridStep(after, x, z);
            return new OwnedTerrainGridOutlier { x = x, z = z, worldBefore = new Vector3(at.x, origin.y + before[z, x] * 600, at.z), worldAfter = new Vector3(at.x, origin.y + after[z, x] * 600, at.z), baselineStep = oldStep, outputStep = newStep, baselineSlopeDegrees = Mathf.Atan(oldStep / 3.125f) * Mathf.Rad2Deg, outputSlopeDegrees = Mathf.Atan(newStep / 3.125f) * Mathf.Rad2Deg, changeMetres = (after[z, x] - before[z, x]) * 600, underOriginalCavernBounds = OwnedTerrainUnderOriginalFootprint(at, underground) };
        }
        static void OwnedGridProtection(OwnedTerrainGridOutlier item, int[] mask)
        {
            int x = item.x, z = item.z; item.protection00 = mask[z * 513 + x]; item.protection10 = mask[z * 513 + Mathf.Min(512, x + 1)]; item.protection01 = mask[Mathf.Min(512, z + 1) * 513 + x]; item.protection11 = mask[Mathf.Min(512, z + 1) * 513 + Mathf.Min(512, x + 1)];
        }
        static void RetainOwnedGridOutlier(List<OwnedTerrainGridOutlier> items, OwnedTerrainGridOutlier item)
        { items.Add(item); items.Sort((a, b) => b.outputStep.CompareTo(a.outputStep)); if (items.Count > MaximumOwnedTerrainOutliers) items.RemoveAt(items.Count - 1); }
        [Serializable] public sealed class OwnedLayoutTerrainField
        {
            public string source, inputSha256, inputFloat32, inputHolesRawBool8, outputHolesRawBool8, inputHolesSha256, outputHolesSha256, outputSha256, outputFloat32, protectionInt32, afterNetworkDestructionSha256;
            public Vector3 origin, actualTerrainSize;
            public int actualHeightmapResolution, actualHolesResolution, changedHoleCells;
            public int changedVertices, steep65, steep80, originalUndergroundChangedVertices;
            public float maximumAdjacentStep, baselineMaximumAdjacentStep;
            public int baselineSteep65, baselineSteep80, createdSteep65, retainedSteep65, removedSteep65, createdSteep80, retainedSteep80, removedSteep80;
            public List<OwnedTerrainGridOutlier> steepestCells = new List<OwnedTerrainGridOutlier>(), originalUndergroundChanges = new List<OwnedTerrainGridOutlier>();
        }
        [Serializable] public sealed class OwnedLayoutTerrainCase
        {
            public string name, inputPointsSha256, actualContourJson, actualContourSha256, beforeSettingsJson, afterSettingsJson;
            public OwnedLayoutTerrainField[] fields;
            public int actualVendorBuilds, physicalVertices, physicalSamples, physicalOverExistingTolerance;
            public float maximumPhysicalHeightError;
            public float surroundingHeightFactorBefore, surroundingHeightFactorAfter, terrainCellHeightThreshold, terrainCellAngleThreshold, terrainCellSize, terrainMinimumIndent, actualLeftSurroundMinXZ, actualLeftSurroundMaxXZ, actualRightSurroundMinXZ, actualRightSurroundMaxXZ;
            public int originalUndergroundPhysicalVerticesSkipped;
            public List<OwnedTerrainPhysicalOutlier> physicalOutliers = new List<OwnedTerrainPhysicalOutlier>();
            public bool markerInputsExact, ownedNetworkDestroyed, randomCaseStartExact;
            public int portalEndMarker = -1; public List<OwnedPortalLevelWitness> portalLevels = new List<OwnedPortalLevelWitness>();
        }
        [Serializable] public sealed class OwnedLayoutTerrainEvidence
        {
            public string scope = "ORIGINAL_VISTA_TWO_TILE_PUBLIC_SDK_BUILD_ORIGINAL_VS_OWNED_AUTHORED_ROUTE_NOT_WHOLE_WORLD_ACCEPTANCE", status = "IN_PROGRESS", error, savedLayout, originalSourcePlanJson;
            public string sourcePortalVolumeScope = "ACTUAL_ORIGINAL_UNDERGROUND_SOURCE_BOX_AND_CLIPPED_INTERVALS_NOT_A_LIVE_DIGGER_FLOOR";
            public Bounds sourcePortalVolume;
            public bool portalEndBlendControl, identicalActualPavement, actualSdkBridgeFlagsExact;
            public string portalApiWitness = "Assets/EasyRoads3D/html/ERRoad.html:251-255; installed EasyRoads3Dv3.dll public setter takes absolute value, clamps request above half actual totalDistance to .4*totalDistance, stores bridgeEndLevelDistance; no vendor code changed";
            public int pairedUndergroundDifferenceVertices, pairedProtectedCoreDifferenceVertices; public float maximumPairedUndergroundDifference, maximumPairedProtectedCoreDifference;
            public int pairedChangedHoleCells; public List<OwnedTerrainGridOutlier> pairedUndergroundChanges = new List<OwnedTerrainGridOutlier>();
            public bool sameActualInputFields, originalSourcesExact, incomingContextExact, randomStateRestored;
            public List<OwnedLayoutTerrainCase> cases = new List<OwnedLayoutTerrainCase>();
        }
        public static OwnedLayoutTerrainEvidence LastOwnedLayoutTerrainEvidence { get; private set; }
        static void SaveOwnedLayoutTerrain(OwnedLayoutTerrainEvidence proof)
        { Directory.CreateDirectory(OutputDirectory()); File.WriteAllText(Path.Combine(OutputDirectory(), proof.portalEndBlendControl ? "actual-owned-mountain-portal-blend.json" : "actual-owned-mountain-road-terrain.json"), JsonUtility.ToJson(proof, true)); }

        public static void CaptureOwnedMountainRoadTerrainFromCommandLine() => CaptureOwnedMountainRoadTerrain(false);
        public static void CaptureOwnedMountainPortalEndBlendFromCommandLine()
        {
            if (!Environment.GetCommandLineArgs().Contains("-tsDragonOwnedPortalEndBlendControl")) throw new InvalidOperationException("Explicit scratch-only portal end blend flag required; not a canonical world setting.");
            CaptureOwnedMountainRoadTerrain(true);
        }
        static void CaptureOwnedMountainRoadTerrain(bool portalControl)
        {
            // Generation must pass its real geometry/ray/save gates first. This
            // invocation intentionally does not select a prior rejected layout.
            CaptureOwnedMountainRoadLayoutFromCommandLine();
            var layout = AssetDatabase.LoadAssetAtPath<DragonKingdomMountainRoadLayout>(LastOwnedLayoutEvidence.layoutPath);
            DragonKingdomMountainRoadLayoutAuthoring.ValidateLayout(layout);
            const string networkPath = "Assets/EasyRoads3D/Resources/ERRoadNetwork.prefab", materialPath = "Assets/EasyRoads3D/Resources/Materials/roads/road material.mat";
            const string foundation = "Assets/TruckDragonKingdom/ReviewCandidates/RoundedVista01/Terrain/";
            var sources = new[] { foundation + "Kingdom 1 0.asset", foundation + "Kingdom 1 1.asset" };
            var watch = sources.SelectMany(p => AssetDatabase.GetDependencies(p, true)).Concat(sources).Concat(new[] { networkPath, materialPath, "Assets/EasyRoads3D/lib/EasyRoads3Dv3.dll" }).SelectMany(p => new[] { p, p + ".meta" }).Distinct().Where(File.Exists).ToDictionary(p => p, Sha);
            PrepareIsolatedBatchRunner(new Evidence(), watch);
            var incoming = Enumerable.Range(0, SceneManager.sceneCount).Select(SceneManager.GetSceneAt).Select(s => new { s, dirty = s.isDirty, poses = JsonUtility.ToJson(new RunnerPoseSet { poses = RunnerPoses(s) }) }).ToArray(); var prior = SceneManager.GetActiveScene(); var random = UnityEngine.Random.state;
            var proof = new OwnedLayoutTerrainEvidence { savedLayout = AssetDatabase.GetAssetPath(layout), portalEndBlendControl = portalControl }; LastOwnedLayoutTerrainEvidence = proof;
            var original = DragonKingdomWorldAuthoring.InspectNativeMainRoadGradingPlan("caverns-volcano", DragonKingdomWorldAuthoring.CavernUndergroundBounds(), true);
            proof.sourcePortalVolume = original.underground; proof.originalSourcePlanJson = JsonUtility.ToJson(original);
            if (portalControl && layout.authoritativePoints.Length != 646) throw new InvalidOperationException("Exact current646 route required for this bounded endpoint experiment.");
            if (portalControl) proof.scope = "SAME_OWNED646_SAME_RAW_VISTA_PUBLIC_BRIDGE_END_REQUEST16_VS0_ONLY_NOT_DIGGER_WORLD_OR_ROOF_ACCEPTANCE";
            try
            {
                for (int mode = 0; mode < 2; mode++)
                {
                    var row = new OwnedLayoutTerrainCase { name = portalControl ? (mode == 0 ? "ownedPortalEnd16" : "ownedPortalEnd0") : (mode == 0 ? "original17" : "ownedVendorCurve"), inputPointsSha256 = DragonKingdomMountainRoadLayoutAuthoring.PointsDigest(mode == 0 && !portalControl ? original.actualGradingMarkers : layout.authoritativePoints) }; proof.cases.Add(row);
                    Scene scene = default; var clonePaths = new List<string>();
                    try
                    {
                        UnityEngine.Random.state = random; row.randomCaseStartExact = UnityEngine.Random.state.Equals(random);
                        scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive); EditorSceneManager.SetActiveScene(scene);
                        var terrains = new List<Terrain>(); var fields = new List<OwnedLayoutTerrainField>();
                        for (int i = 0; i < 2; i++)
                        {
                            var source = AssetDatabase.LoadAssetAtPath<TerrainData>(sources[i]);
                            if (source == null || source.heightmapResolution != 513 || source.size != new Vector3(1600, 600, 1600)) throw new InvalidOperationException("Original exact Vista two-tile field required.");
                            var clone = UnityEngine.Object.Instantiate(source); const string generated = "Assets/TruckDragonKingdom/Tests/World/Generated"; Directory.CreateDirectory(generated);
                            string path = generated + "/OwnedRoadField-" + Guid.NewGuid().ToString("N") + ".asset"; AssetDatabase.CreateAsset(clone, path); clonePaths.Add(path);
                            var go = Terrain.CreateTerrainGameObject(clone); go.transform.position = new Vector3(0, -20, i * 1600 - 800); terrains.Add(go.GetComponent<Terrain>());
                            fields.Add(new OwnedLayoutTerrainField { source = sources[i], origin = go.transform.position, actualTerrainSize = clone.size, actualHeightmapResolution = clone.heightmapResolution, actualHolesResolution = clone.holesResolution, inputSha256 = Heights(clone), inputHolesSha256 = SingleRoadDigest(OwnedHolesBytes(clone)) });
                        }
                        row.fields = fields.ToArray(); Physics.SyncTransforms();
                        var root = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(networkPath), scene); var owner = root.GetComponent<ERModularBase>();
                        var settings = new SerializedObject(owner); var ray = settings.FindProperty("terrainRaycastHeight"); if (ray == null) throw new InvalidOperationException("Actual installed terrain query field absent.");
                        if (ray.propertyType == SerializedPropertyType.Float) ray.floatValue = 1000; else if (ray.propertyType == SerializedPropertyType.Integer) ray.intValue = 1000; else throw new InvalidOperationException("Unexpected terrain query field representation."); settings.ApplyModifiedPropertiesWithoutUndo();
                        owner.RoadNetworkInit(); var network = new ERRoadNetwork(owner); var type = new ERRoadType { roadTypeName = "Owned mountain controlled grade", roadWidth = 18, hasMeshCollider = true, roadMaterial = AssetDatabase.LoadAssetAtPath<Material>(materialPath) };
                        DragonKingdomWorldAuthoring.RegisterOwnedNativeGradingType(type, owner, 28, 110);
                        var points = mode == 0 && !portalControl ? original.actualGradingMarkers : layout.authoritativePoints;
                        var road = network.CreateRoad(row.name, type, points, Enumerable.Repeat(ERMarkerControlType.StraightXZY, points.Length).ToArray()); road.SetTerrainDeformation(true, false); road.SnapToTerrain(false, false); road.SetResolution(.35f, false);
                        // Both authoring cases use the exact same physical surface
                        // configuration. No random authoring elevation is mixed
                        // into the deliberately changed route comparison.
                        road.SetRandomBumpiness(0, 0, 5, 15, false); road.SetRandomTilting(0, 0, 5, 15, false);
                        for (int i = 0; i < points.Length; i++) { road.SetIndent(28, i); road.SetSurrounding(110, i); }
                        foreach (var interval in original.intervals.Where(v => v.excluded)) { road.SetTerrainDeformation(interval.marker, false); road.SetStartLevelDistance(interval.marker, 16); road.SetEndLevelDistance(interval.marker, 16); }
                        // Isolate exactly one supported endpoint setting; retain
                        // all markers, bridge flags, indent, surround and geometry.
                        if (portalControl)
                        {
                            row.portalEndMarker = OwnedPortalEndMarker(original.intervals.Select(value => value.excluded).ToArray());
                            if (mode == 1) road.SetEndLevelDistance(row.portalEndMarker, 0, false);
                        }
                        DragonKingdomWorldAuthoring.ConfigureOwnedNativeRoadGlobals(road, type, owner, 28, 110); road.Refresh();
                        row.markerInputsExact = road.GetMarkerPositions().SequenceEqual(points); if (!row.markerInputsExact) throw new InvalidOperationException("Actual controlled native road changed its declared marker authority.");
                        if (portalControl)
                        {
                            foreach (var interval in original.intervals.Where(value => value.excluded))
                            {
                                var actual = road.roadScript.markersExt[interval.marker];
                                row.portalLevels.Add(new OwnedPortalLevelWitness { marker = interval.marker, point = points[interval.marker], actualSegmentLength = actual.totalDistance, requestedEnd = mode == 1 && interval.marker == row.portalEndMarker ? 0 : 16, actualStart = actual.bridgeStartLevelDistance, actualEnd = actual.bridgeEndLevelDistance });
                            }
                            if (mode == 1 && row.portalLevels.Single(value => value.marker == row.portalEndMarker).actualEnd != 0) throw new InvalidOperationException("Actual public endpoint-zero control did not persist; no guessed blend effect.");
                        }
                        var contour = CaptureNativeApronContour(road.roadScript); string contourJson = JsonUtility.ToJson(contour); row.actualContourSha256 = SingleRoadDigest(System.Text.Encoding.UTF8.GetBytes(contourJson)); row.actualContourJson = Path.Combine(OutputDirectory(), "owned-road-" + row.name + ".contours.json"); File.WriteAllText(row.actualContourJson, contourJson);
                        row.beforeSettingsJson = JsonUtility.ToJson(road.roadScript); row.surroundingHeightFactorBefore = owner.surroundingHeightFactor; row.terrainCellHeightThreshold = owner.terrainCellHeightThreshold; row.terrainCellAngleThreshold = owner.terrainCellAngleThreshold; row.terrainCellSize = owner.terrainCellSize; row.terrainMinimumIndent = owner.terrainMinIndent;
                        var leftWidths = contour.leftSurround.Zip(contour.leftIndent, (a, b) => Vector3.ProjectOnPlane(a - b, Vector3.up).magnitude).ToArray();
                        var rightWidths = contour.rightSurround.Zip(contour.rightIndent, (a, b) => Vector3.ProjectOnPlane(a - b, Vector3.up).magnitude).ToArray();
                        row.actualLeftSurroundMinXZ = leftWidths.Min(); row.actualLeftSurroundMaxXZ = leftWidths.Max(); row.actualRightSurroundMinXZ = rightWidths.Min(); row.actualRightSurroundMaxXZ = rightWidths.Max();
                        network.BuildRoadNetwork(false, false, false, new[] { road }); row.actualVendorBuilds++; row.afterSettingsJson = JsonUtility.ToJson(road.roadScript); row.surroundingHeightFactorAfter = owner.surroundingHeightFactor;
                        var protections = new int[2][];
                        for (int i = 0; i < 2; i++)
                        {
                            var field = fields[i]; var data = terrains[i].terrainData; var h = data.GetHeights(0, 0, 513, 513); var baseline = AssetDatabase.LoadAssetAtPath<TerrainData>(sources[i]).GetHeights(0, 0, 513, 513);
                            byte[] bytes = new byte[h.Length * 4]; Buffer.BlockCopy(h, 0, bytes, 0, bytes.Length); field.outputSha256 = SingleRoadDigest(bytes); field.outputFloat32 = Path.Combine(OutputDirectory(), "owned-road-" + row.name + "-" + i + ".height.f32"); File.WriteAllBytes(field.outputFloat32, bytes);
                            byte[] baselineBytes = new byte[baseline.Length * 4]; Buffer.BlockCopy(baseline, 0, baselineBytes, 0, baselineBytes.Length);
                            if (SingleRoadDigest(baselineBytes) != field.inputSha256) throw new InvalidOperationException("Actual original baseline bytes changed before diagnostic capture.");
                            field.inputFloat32 = Path.Combine(OutputDirectory(), "owned-road-" + row.name + "-" + i + ".input.height.f32"); File.WriteAllBytes(field.inputFloat32, baselineBytes);
                            var sourceData = AssetDatabase.LoadAssetAtPath<TerrainData>(sources[i]); var sourceHoles = OwnedHolesBytes(sourceData); var outputHoles = OwnedHolesBytes(data);
                            if (SingleRoadDigest(sourceHoles) != field.inputHolesSha256 || sourceHoles.Length != outputHoles.Length) throw new InvalidOperationException("Actual source hole mask changed or native hole resolution disagrees.");
                            field.inputHolesRawBool8 = Path.Combine(OutputDirectory(), "owned-road-" + row.name + "-" + i + ".input.holes.bool8"); field.outputHolesRawBool8 = Path.Combine(OutputDirectory(), "owned-road-" + row.name + "-" + i + ".output.holes.bool8");
                            File.WriteAllBytes(field.inputHolesRawBool8, sourceHoles); File.WriteAllBytes(field.outputHolesRawBool8, outputHoles); field.outputHolesSha256 = SingleRoadDigest(outputHoles);
                            for (int hole = 0; hole < sourceHoles.Length; hole++) if (sourceHoles[hole] != outputHoles[hole]) field.changedHoleCells++;
                            for (int z = 0; z < 513; z++) for (int x = 0; x < 513; x++)
                            {
                                if (!float.IsFinite(h[z, x])) throw new InvalidOperationException("Native grade is nonfinite."); if (h[z, x] != baseline[z, x]) field.changedVertices++;
                                float step = Mathf.Max(x < 512 ? Mathf.Abs(h[z, x + 1] - h[z, x]) * 600 : 0, z < 512 ? Mathf.Abs(h[z + 1, x] - h[z, x]) * 600 : 0);
                                field.maximumAdjacentStep = Mathf.Max(field.maximumAdjacentStep, step); if (x < 512 && z < 512) { if (Mathf.Atan(step / 3.125f) * Mathf.Rad2Deg > 65) field.steep65++; if (Mathf.Atan(step / 3.125f) * Mathf.Rad2Deg > 80) field.steep80++; }
                                var box = original.underground; float worldX = field.origin.x + x * 3.125f, worldZ = field.origin.z + z * 3.125f;
                                if (worldX >= box.min.x && worldX <= box.max.x && worldZ >= box.min.z && worldZ <= box.max.z && h[z, x] != baseline[z, x])
                                {
                                    field.originalUndergroundChangedVertices++;
                                    if (field.originalUndergroundChanges.Count < MaximumOwnedTerrainOutliers) field.originalUndergroundChanges.Add(OwnedGridWitness(baseline, h, x, z, field.origin, box));
                                }
                                float baselineStep = OwnedGridStep(baseline, x, z); field.baselineMaximumAdjacentStep = Mathf.Max(field.baselineMaximumAdjacentStep, baselineStep);
                                if (x < 512 && z < 512)
                                {
                                    float oldSlope = Mathf.Atan(baselineStep / 3.125f) * Mathf.Rad2Deg, newSlope = Mathf.Atan(step / 3.125f) * Mathf.Rad2Deg;
                                    bool old65 = oldSlope > 65, new65 = newSlope > 65, old80 = oldSlope > 80, new80 = newSlope > 80;
                                    if (old65) field.baselineSteep65++; if (old80) field.baselineSteep80++;
                                    if (new65) { if (old65) field.retainedSteep65++; else field.createdSteep65++; RetainOwnedGridOutlier(field.steepestCells, OwnedGridWitness(baseline, h, x, z, field.origin, box)); } else if (old65) field.removedSteep65++;
                                    if (new80) { if (old80) field.retainedSteep80++; else field.createdSteep80++; } else if (old80) field.removedSteep80++;
                                }
                            }
                            var authority = terrains[i].GetComponent<ERTerrain>(); if (authority == null) throw new InvalidOperationException("Actual public terrain protection evidence absent."); var protection = new int[513 * 513];
                            foreach (var change in authority.terrainChanges) { if (change.index < 0 || change.index >= protection.Length) throw new InvalidOperationException("Actual mask index exceeds field."); protection[change.index] = change.value; }
                            protections[i] = protection;
                            foreach (var witness in field.steepestCells.Concat(field.originalUndergroundChanges))
                            {
                                OwnedGridProtection(witness, protection); witness.sourceTerrainIsHole = sourceData.IsHole(Mathf.Min(witness.x, sourceData.holesResolution - 1), Mathf.Min(witness.z, sourceData.holesResolution - 1)); witness.outputTerrainIsHole = data.IsHole(Mathf.Min(witness.x, data.holesResolution - 1), Mathf.Min(witness.z, data.holesResolution - 1));
                            }
                            bytes = new byte[protection.Length * 4]; Buffer.BlockCopy(protection, 0, bytes, 0, bytes.Length); field.protectionInt32 = Path.Combine(OutputDirectory(), "owned-road-" + row.name + "-" + i + ".protection.i32"); File.WriteAllBytes(field.protectionInt32, bytes);
                        }
                        row.physicalVertices = contour.roadMesh.Length;
                        for (int vertex = 0; vertex < contour.roadMesh.Length; vertex++)
                        {
                            var p = contour.roadMesh[vertex];
                            // The original portal prefix is deliberately inside
                            // the source underground exclusion, not exposed grade.
                            var box = original.underground; if (p.x >= box.min.x && p.x <= box.max.x && p.z >= box.min.z && p.z <= box.max.z) { row.originalUndergroundPhysicalVerticesSkipped++; continue; }
                            int tile = p.z < 800 ? 0 : 1; var terrain = terrains[tile]; var local = p - terrain.transform.position; if (local.x < 0 || local.x > 1600 || local.z < 0 || local.z > 1600) throw new InvalidOperationException("Actual generated pavement leaves the measured fields.");
                            float y = terrain.transform.position.y + terrain.terrainData.GetInterpolatedHeight(local.x / 1600, local.z / 1600); float error = Mathf.Abs(y - p.y); row.physicalSamples++; row.maximumPhysicalHeightError = Mathf.Max(row.maximumPhysicalHeightError, error); if (error > .3f) row.physicalOverExistingTolerance++;
                            if (error > .3f)
                            {
                                int station = OwnedTerrainVertexStation(vertex, contour.roadMesh.Length, contour.markers.Length), gx = Mathf.Clamp(Mathf.FloorToInt(local.x / 3.125f), 0, 511), gz = Mathf.Clamp(Mathf.FloorToInt(local.z / 3.125f), 0, 511); var mask = protections[tile];
                                row.physicalOutliers.Add(new OwnedTerrainPhysicalOutlier { vertex = vertex, station = station, marker = contour.markers[station], tile = tile, gridX = gx, gridZ = gz, roadPoint = p, terrainDataY = y, signedHeightError = y - p.y, absoluteHeightError = error, underOriginalCavernBounds = OwnedTerrainUnderOriginalFootprint(p, box), withinOriginalCavernVolume = box.Contains(p), actualSdkBridgeFlag = contour.bridges[station], originalExcludedMarker = original.intervals.Any(v => v.marker == contour.markers[station] && v.excluded), protection00 = mask[gz * 513 + gx], protection10 = mask[gz * 513 + gx + 1], protection01 = mask[(gz + 1) * 513 + gx], protection11 = mask[(gz + 1) * 513 + gx + 1] });
                                row.physicalOutliers.Sort((a, b) => b.absoluteHeightError.CompareTo(a.absoluteHeightError)); if (row.physicalOutliers.Count > MaximumOwnedTerrainOutliers) row.physicalOutliers.RemoveAt(row.physicalOutliers.Count - 1);
                            }
                        }
                        // Read the actual generated TerrainCollider independently;
                        // no Flush/Sync/rebuild or new surface is supplied here.
                        foreach (var witness in row.physicalOutliers)
                        {
                            var sourceData = AssetDatabase.LoadAssetAtPath<TerrainData>(sources[witness.tile]); var outputData = terrains[witness.tile].terrainData;
                            witness.sourceTerrainIsHole = sourceData.IsHole(Mathf.Min(witness.gridX, sourceData.holesResolution - 1), Mathf.Min(witness.gridZ, sourceData.holesResolution - 1)); witness.outputTerrainIsHole = outputData.IsHole(Mathf.Min(witness.gridX, outputData.holesResolution - 1), Mathf.Min(witness.gridZ, outputData.holesResolution - 1));
                            var collider = terrains[witness.tile].GetComponent<TerrainCollider>(); RaycastHit hit = default;
                            witness.terrainColliderHit = collider != null && collider.Raycast(new Ray(witness.roadPoint + Vector3.up * 1000, Vector3.down), out hit, 2000);
                            if (witness.terrainColliderHit) { witness.terrainColliderPoint = hit.point; witness.terrainColliderNormal = hit.normal; witness.terrainColliderY = hit.point.y; }
                        }
                        // Capture all genuine failures before applying any final
                        // acceptance judgment; no relaxed target or clip masks.
                        SaveOwnedLayoutTerrain(proof); UnityEngine.Object.DestroyImmediate(root); row.ownedNetworkDestroyed = root == null;
                        for (int i = 0; i < 2; i++) fields[i].afterNetworkDestructionSha256 = Heights(terrains[i].terrainData);
                    }
                    finally { if (scene.IsValid() && scene.isLoaded) EditorSceneManager.CloseScene(scene, true); if (prior.IsValid()) EditorSceneManager.SetActiveScene(prior); foreach (var path in clonePaths) AssetDatabase.DeleteAsset(path); SaveOwnedLayoutTerrain(proof); }
                }
                proof.sameActualInputFields = proof.cases.Count == 2 && proof.cases[0].fields.Select(f => f.inputSha256).SequenceEqual(proof.cases[1].fields.Select(f => f.inputSha256));
                if (portalControl)
                {
                    var first = proof.cases[0]; var second = proof.cases[1];
                    var a = JsonUtility.FromJson<NativeApronContourFrame>(File.ReadAllText(first.actualContourJson));
                    var b = JsonUtility.FromJson<NativeApronContourFrame>(File.ReadAllText(second.actualContourJson));
                    proof.identicalActualPavement = first.inputPointsSha256 == second.inputPointsSha256 && a.roadMesh.SequenceEqual(b.roadMesh) && a.markers.SequenceEqual(b.markers);
                    proof.actualSdkBridgeFlagsExact = a.bridges.SequenceEqual(b.bridges);
                    if (!proof.identicalActualPavement) throw new InvalidOperationException("One blend control must not change actual accepted pavement vertices/marker IDs. Public SDK sampled bridge flags are recorded separately because they may encode the tested blend span.");
                    for (int tile = 0; tile < 2; tile++)
                    {
                        var before = File.ReadAllBytes(first.fields[tile].outputFloat32); var after = File.ReadAllBytes(second.fields[tile].outputFloat32); var ha = new float[513,513]; var hb = new float[513,513]; Buffer.BlockCopy(before,0,ha,0,before.Length); Buffer.BlockCopy(after,0,hb,0,after.Length);
                        var maskBytes = File.ReadAllBytes(first.fields[tile].protectionInt32); var mask = new int[513*513]; Buffer.BlockCopy(maskBytes,0,mask,0,maskBytes.Length);
                        for (int z = 0; z < 513; z++) for (int x = 0; x < 513; x++)
                        {
                            if (ha[z,x] == hb[z,x]) continue; float delta = Mathf.Abs(ha[z,x]-hb[z,x])*600;
                            var point = first.fields[tile].origin + new Vector3(x*3.125f,0,z*3.125f);
                            if (OwnedTerrainUnderOriginalFootprint(point,original.underground)) { proof.pairedUndergroundDifferenceVertices++; proof.maximumPairedUndergroundDifference = Mathf.Max(proof.maximumPairedUndergroundDifference,delta); if (proof.pairedUndergroundChanges.Count < MaximumOwnedTerrainOutliers) proof.pairedUndergroundChanges.Add(OwnedGridWitness(ha,hb,x,z,first.fields[tile].origin,original.underground)); }
                            if (mask[z*513+x] == 2) { proof.pairedProtectedCoreDifferenceVertices++; proof.maximumPairedProtectedCoreDifference = Mathf.Max(proof.maximumPairedProtectedCoreDifference,delta); }
                        }
                        var holesA = File.ReadAllBytes(first.fields[tile].outputHolesRawBool8); var holesB = File.ReadAllBytes(second.fields[tile].outputHolesRawBool8);
                        if(holesA.Length!=holesB.Length) throw new InvalidOperationException("Paired actual hole-mask resolutions differ.");
                        for(int at=0;at<holesA.Length;at++) if(holesA[at]!=holesB[at]) proof.pairedChangedHoleCells++;
                    }
                }
                if (!proof.sameActualInputFields || proof.cases.Any(c => c.actualVendorBuilds != 1 || c.fields.Sum(f => f.changedVertices) == 0 || c.physicalSamples == 0)) throw new InvalidOperationException("Actual identical raw inputs and one meaningful native build per route required.");
                proof.status = portalControl ? "ACTUAL_OWNED646_PORTAL_END_BLEND_PAIR_CAPTURED_NOT_WORLD_ACCEPTANCE" : "ACTUAL_OWNED_ROUTE_GRADE_COMPARISON_CAPTURED_REQUIRES_CORE_ROOF_AND_WHOLE_WORLD_ACCEPTANCE";
            }
            catch (Exception error) { proof.status = "REJECTED_OWNED_ROUTE_TERRAIN_DIAGNOSTIC"; proof.error = error.ToString(); throw; }
            finally
            {
                UnityEngine.Random.state = random; proof.randomStateRestored = UnityEngine.Random.state.Equals(random); proof.originalSourcesExact = watch.All(p => File.Exists(p.Key) && Sha(p.Key) == p.Value);
                proof.incomingContextExact = SceneManager.sceneCount == incoming.Length && SceneManager.GetActiveScene() == prior && incoming.All(p => p.s.IsValid() && p.s.isLoaded && p.s.isDirty == p.dirty && JsonUtility.ToJson(new RunnerPoseSet { poses = RunnerPoses(p.s) }) == p.poses); SaveOwnedLayoutTerrain(proof);
                if (!proof.randomStateRestored || !proof.originalSourcesExact || !proof.incomingContextExact) throw new InvalidOperationException("Terrain comparison changed original source or incoming context.");
            }
        }
    }
}
#endif

#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LWS.TruckDragonKingdom.Editor
{
    public static partial class DragonKingdomNativeRoadTypeProbe
    {
        [Serializable] public sealed class OwnedLayoutEvidence
        {
            public string scope = "ACTUAL_OWNED_VENDOR_ROUTE_PHYSICAL_BAKE_AND_SAVE_RELOAD_NOT_TERRAIN_OR_WORLD_ACCEPTANCE", status = "IN_PROGRESS", error;
            public string layoutPath, layoutSha256, reloadedPointsSha256;
            public DragonKingdomMountainRoadLayoutAuthoring.Evidence authoring;
            public bool originalSourcesExact, incomingContextExact, randomStateRestored, saveReloadExact, unrelatedSpecsExact, originalCavePrefixExact;
            public int physicalPieces, physicalQueries, physicalMisses, actualVendorBuilds;
            public float maximumPhysicalHeightError, maximumGrade;
        }
        public static OwnedLayoutEvidence LastOwnedLayoutEvidence { get; private set; }
        static void SaveOwnedLayout(OwnedLayoutEvidence proof)
        { Directory.CreateDirectory(OutputDirectory()); File.WriteAllText(Path.Combine(OutputDirectory(), "actual-owned-mountain-road-layout.json"), JsonUtility.ToJson(proof, true)); }

        public static void CaptureOwnedMountainRoadLayoutFromCommandLine()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || Terrain.activeTerrains.Length != 0 || UnityEngine.Object.FindObjectsByType<EasyRoads3Dv3.ERModularBase>(FindObjectsSortMode.None).Length != 0)
                throw new InvalidOperationException("Stopped isolated owned route fixture required.");
            var watched = new[] { "Assets/EasyRoads3D/Resources/ERRoadNetwork.prefab", "Assets/EasyRoads3D/Resources/Materials/roads/road material.mat", "Assets/EasyRoads3D/lib/EasyRoads3Dv3.dll", "Assets/TruckDragonKingdom/Editor/DragonKingdomWorldAuthoring.cs", "Assets/TruckDragonKingdom/Editor/DragonKingdomWorldAuthoring.NativeGradingPlan.cs" }.SelectMany(p => new[] { p, p + ".meta" }).ToDictionary(p => p, Sha);
            PrepareIsolatedBatchRunner(new Evidence(), watched);
            var incoming = Enumerable.Range(0, SceneManager.sceneCount).Select(SceneManager.GetSceneAt).Select(s => new { s, dirty = s.isDirty, poses = JsonUtility.ToJson(new RunnerPoseSet { poses = RunnerPoses(s) }) }).ToArray();
            var prior = SceneManager.GetActiveScene(); var random = UnityEngine.Random.state; Scene scene = default;
            var proof = new OwnedLayoutEvidence(); LastOwnedLayoutEvidence = proof; SaveOwnedLayout(proof);
            try
            {
                scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive); EditorSceneManager.SetActiveScene(scene);
                string folder = "Assets/TruckDragonKingdom/ReviewCandidates/OwnedMountainLayout-" + Guid.NewGuid().ToString("N");
                var original = DragonKingdomWorldAuthoring.InspectNativeMainRoadGradingPlan("caverns-volcano", DragonKingdomWorldAuthoring.CavernUndergroundBounds(), true);
                var layout = DragonKingdomMountainRoadLayoutAuthoring.CreateCandidate(scene, original, folder);
                proof.layoutPath = AssetDatabase.GetAssetPath(layout); proof.layoutSha256 = Sha(proof.layoutPath);
                string savedJson = JsonUtility.ToJson(layout); var meshes = layout.physicalPieces.Select(p => p.mesh).ToArray(); var points = layout.authoritativePoints.ToArray();
                Resources.UnloadAsset(layout); layout = AssetDatabase.LoadAssetAtPath<DragonKingdomMountainRoadLayout>(proof.layoutPath);
                DragonKingdomMountainRoadLayoutAuthoring.ValidateLayout(layout);
                proof.reloadedPointsSha256 = DragonKingdomMountainRoadLayoutAuthoring.PointsDigest(layout.authoritativePoints);
                proof.saveReloadExact = JsonUtility.ToJson(layout) == savedJson && layout.authoritativePoints.SequenceEqual(points) && meshes.SequenceEqual(layout.physicalPieces.Select(p => p.mesh)) && Sha(proof.layoutPath) == proof.layoutSha256;
                var legacy = DragonKingdomWorldAuthoring.Roads(); var replaced = DragonKingdomMountainRoadLayoutAuthoring.ReplaceOwnedSpecification(legacy, layout);
                proof.unrelatedSpecsExact = legacy.Where(r => r.id != layout.roadId).All(r => ReferenceEquals(r, replaced.Single(v => v.id == r.id)));
                proof.originalCavePrefixExact = original.actualGradingMarkers.Take(6).SequenceEqual(layout.authoritativePoints.Take(6));
                proof.physicalPieces = layout.physicalPieces.Length; proof.physicalQueries = layout.physicalQueries; proof.physicalMisses = layout.physicalMisses; proof.maximumPhysicalHeightError = layout.maximumPhysicalHeightError; proof.maximumGrade = layout.maximumGrade;
                if (!proof.saveReloadExact || !proof.unrelatedSpecsExact || !proof.originalCavePrefixExact) throw new InvalidOperationException("Owned layout save/reload or narrow replacement authority failed.");
                proof.status = "ACTUAL_VENDOR_ROUTE_MESH_AND_SAVED_AUTHORITY_PASSED_TERRAIN_AND_WORLD_PENDING";
            }
            catch (Exception error) { proof.status = "REJECTED_OWNED_ROUTE_AUTHORING"; proof.error = error.ToString(); throw; }
            finally
            {
                proof.authoring = DragonKingdomMountainRoadLayoutAuthoring.LastEvidence;
                if (scene.IsValid()) EditorSceneManager.CloseScene(scene, true); if (prior.IsValid()) EditorSceneManager.SetActiveScene(prior); UnityEngine.Random.state = random;
                proof.randomStateRestored = UnityEngine.Random.state.Equals(random); proof.originalSourcesExact = watched.All(p => File.Exists(p.Key) && Sha(p.Key) == p.Value);
                proof.incomingContextExact = SceneManager.sceneCount == incoming.Length && SceneManager.GetActiveScene() == prior && incoming.All(p => p.s.IsValid() && p.s.isLoaded && p.s.isDirty == p.dirty && JsonUtility.ToJson(new RunnerPoseSet { poses = RunnerPoses(p.s) }) == p.poses);
                SaveOwnedLayout(proof);
                if (!proof.randomStateRestored || !proof.originalSourcesExact || !proof.incomingContextExact) throw new InvalidOperationException("Owned route fixture changed original source or incoming scene/random context.");
            }
        }
    }
}
#endif

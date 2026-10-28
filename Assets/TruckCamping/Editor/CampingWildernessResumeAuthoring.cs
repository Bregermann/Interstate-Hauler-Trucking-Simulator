using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LWS.TruckCamping.Editor
{
    public static partial class CampingWildernessAuthoring
    {
        const string FailedStageManifest = "Assets/TruckCamping/Editor/NavigationFailedStageV1.json";
        const string MaterializedStageManifest = "Assets/TruckCamping/Editor/NavigationMaterializedStageV3.json";
        const string NavigationAsset = "Assets/TruckCamping/Models/Camp Haul connected woodland navigation.asset";
        static bool resumingKnownPartial;
        static string selectedStageManifest = FailedStageManifest;
        static bool RepairedTerrainPath(string path)=>path=="Assets/TruckCamping/Models/Camp Haul Vista terrain 0 0.asset" || path=="Assets/TruckCamping/Models/Camp Haul Vista terrain 0 1.asset" || path=="Assets/TruckCamping/Models/Camp Haul Vista terrain 1 0.asset" || path=="Assets/TruckCamping/Models/Camp Haul Vista terrain 1 1.asset";
        static string TerrainContentWithoutScale(TerrainData data)
        {
            string json=EditorJsonUtility.ToJson(data);const string pattern="\"m_Scale\":\\{[^}]*\\}";
            if(Regex.Matches(json,pattern).Count!=1)throw new InvalidOperationException("Unknown TerrainData serialization preserved; cannot prove scale-only repair.");
            return Regex.Replace(json,pattern,"\"m_Scale\":\"verified tile dimensions\"");
        }
        public static void RepairKnownTerrainDimensions(TerrainData data)
        {
            if(data==null || data.heightmapResolution!=257 || data.alphamapResolution!=128 || !data.size.Equals(new Vector3(8000,260,8000)))throw new InvalidOperationException("Unknown/custom terrain dimensions preserved; exact known 257-resolution, 8000m failed stage required.");
            string before=TerrainContentWithoutScale(data);data.size=new Vector3(1000,260,1000);
            if(before!=TerrainContentWithoutScale(data))throw new InvalidOperationException("Terrain content changed outside the authorized horizontal sample pitch; original backup retained.");
            EditorUtility.SetDirty(data);
        }
        [Serializable] sealed class RetainedFile { public string path, sha256; public long bytes; }
        [Serializable] sealed class RetainedStage { public RetainedFile[] files; }
        static string Digest(string path)
        {
            using (var hash = SHA256.Create()) using (var input = File.OpenRead(path)) return string.Concat(hash.ComputeHash(input).Select(x => x.ToString("x2")));
        }
        static RetainedStage ReadKnownStage()
        {
            var stage = JsonUtility.FromJson<RetainedStage>(File.ReadAllText(selectedStageManifest));
            if (stage == null || stage.files == null || stage.files.Length != 127 || stage.files.Select(x => x.path).Distinct().Count() != 127)
                throw new InvalidDataException("Exact retained failed-author stage manifest required.");
            foreach (var file in stage.files)
                if (!file.path.StartsWith("Assets/TruckCamping/", StringComparison.Ordinal) || file.path.Contains("..") || string.IsNullOrEmpty(file.sha256))
                    throw new InvalidDataException("Invalid retained world asset identity.");
            return stage;
        }
        static void SelectExactReviewedStage()
        {
            foreach(string path in new[]{FailedStageManifest,MaterializedStageManifest,CompletedStageManifest,NormalizedStageManifest})
            {
                selectedStageManifest=path;var stage=ReadKnownStage();
                if(stage.files.All(file=>File.Exists(file.path)&&new FileInfo(file.path).Length==file.bytes&&Digest(file.path)==file.sha256))return;
            }
            selectedStageManifest=FailedStageManifest;
            throw new InvalidOperationException("No exact reviewed original/materialized partial world state matched. Custom inputs preserved; no mutation.");
        }
        static void VerifyRetainedInputs(bool allowRepairedNavigation)
        {
            var stage = ReadKnownStage();
            foreach (var file in stage.files)
            {
                                if(allowRepairedNavigation&&!IsCompletedStage(selectedStageManifest))
                {
                    if(file.path==NavigationAsset||file.path==ReviewedRoadAsset||RepairedTerrainPath(file.path))continue;
                    if(CompletedMaterialPath(file.path)){VerifyExactCompletedMaterial(file.path);continue;}
                }
                if (!File.Exists(file.path) || new FileInfo(file.path).Length != file.bytes || Digest(file.path) != file.sha256)
                    throw new InvalidOperationException("Unknown/customized partial world asset preserved; explicit review required: " + file.path);
            }
            // Additional custom assets are not implicitly adopted into this known-stage recovery.
            foreach (string folder in new[] { "Models", "Materials", "Prefabs", "ThirdParty" })
            {
                string path = "Assets/TruckCamping/" + folder;
                if (!Directory.Exists(path)) continue;
                foreach (var file in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
                {
                    string normalized = file.Replace('\\', '/');
                    if (!stage.files.Any(x => x.path == normalized)) throw new InvalidOperationException("Additional custom partial asset preserved: " + normalized);
                }
            }
        }

        [MenuItem("Truck Sports/Impossible Games/Resume EXACT Known Camping Navigation Failure")]
        public static void ResumeKnownPartialSharedWilderness()
        {
            if (EditorApplication.isPlaying || EditorApplication.isCompiling || resumingKnownPartial)
                throw new InvalidOperationException("Stopped compiled explicit author required.");
            for (int i = 0; i < SceneManager.sceneCount; i++) if (SceneManager.GetSceneAt(i).isDirty)
                throw new InvalidOperationException("Save current scene edits before explicit partial-world recovery.");
            if (File.Exists(CampingWilderness.SharedPrefab) || File.Exists(CampingAuthoring.Scene) || File.Exists("Assets/TruckPetPals/Scenes/TruckPetPals.unity"))
                throw new InvalidOperationException("Existing shared prefab or wilderness scene preserved; this recovery is only for the exact pre-prefab failed stage.");
            SelectExactReviewedStage();VerifyRetainedInputs(false);if(IsCompletedStage(selectedStageManifest))VerifyCompletedTerrainMaterialState();else VerifyMissingTerrainCompletionState();
            string backup = "Logs/TruckSports/Impossible07/CampingNavigationResume/Original-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N");
            foreach (var file in ReadKnownStage().files)
            {
                string destination = Path.Combine(backup, file.path); Directory.CreateDirectory(Path.GetDirectoryName(destination)); File.Copy(file.path, destination, false);
            }
            File.Copy(selectedStageManifest, Path.Combine(backup, "known-stage-manifest.json"), false);
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                resumingKnownPartial = true; RequireSharedPrefab();
                Debug.Log("CAMPING_KNOWN_PARTIAL_RESUMED existing source GUIDs retained; original backup=" + backup + "; normal scene authors may now consume the one shared prefab.");
            }
            finally
            {
                resumingKnownPartial = false;
                selectedStageManifest = FailedStageManifest;
                if (setup.Length > 0 && setup.All(x => !string.IsNullOrEmpty(x.path))) EditorSceneManager.RestoreSceneManagerSetup(setup);
                else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }
    }
}

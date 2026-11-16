#if UNITY_EDITOR && VISTA
using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LWS.TruckDragonKingdom.Editor
{
    public static partial class DragonKingdomRegionalWorldIntegration
    {
        [Serializable]public sealed class RoundedCavernProbeEvidence
        {
            public string scope="ISOLATED_ACTUAL_ROUNDED_DIGGER_EXCAVATION_NOT_WHOLE_WORLD_QUALITY_ADOPTION_OR_GAMEPLAY_ACCEPTANCE",tag,status="IN_PROGRESS",error,prefab,preview;
            public bool sourcesExact,criticalPosesExact,saved;public DragonKingdomDiggerCavernAuthoring.Evidence cavern;
            public DragonKingdomRoadSurfaceProbe.NativeFaceNormalsEvidence nativeFaceNormals;
            public bool authoredInnerPaletteBeforeDigger;public DragonKingdomWorldVisualMigration.TerrainResult[] innerWeightPreparation;public AuthoredTerrainPalette innerPalette;public CavernFinishEvidence caveFinish;
        }
        public static RoundedCavernProbeEvidence LastRoundedCavernProbe{get;private set;}
        public static IEnumerator CreateIsolatedRoundedCavernProbe()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||Terrain.activeTerrains.Length!=0||DragonKingdomDiggerCavernAuthoring.IsBusy)throw new InvalidOperationException("Stopped isolated Editor without unrelated terrain or vendor jobs required.");
            string profile=Argument("-tsDragonCavernProfile","");if(profile!="RoundedLobes01"&&profile!="RoundedLobes02")throw new InvalidOperationException("This distinct probe requires an explicit rounded profile, never an ordinary build.");
            bool finish=Environment.GetCommandLineArgs().Contains("-tsDragonCavernProbeFinish");
            bool nativeFaceDiagnostic=Environment.GetCommandLineArgs().Contains("-tsDragonCavernNativeFaceNormalsDiagnostic");if(nativeFaceDiagnostic&&!finish)throw new InvalidOperationException("Native face-normal comparison requires an explicit freshly finished cave candidate.");
            string tag=Argument("-tsDragonCavernProbe","");if(string.IsNullOrEmpty(tag)||tag.Length>40||tag.Any(c=>!char.IsLetterOrDigit(c)&&c!='-'&&c!='_'))throw new ArgumentException("Fresh explicit rounded cavern probe tag required.");
            string foundationTag=Argument("-tsDragonVistaFoundation","RoundedVista01");
            if(string.IsNullOrEmpty(foundationTag)||foundationTag.Length>40||foundationTag.Any(c=>!char.IsLetterOrDigit(c)&&c!='-'&&c!='_'))throw new ArgumentException("Measured foundation tag must stay in the owned review-candidate directory.");
            string foundationFolder=Root+"ReviewCandidates/"+foundationTag,folder=Root+"VisualCandidates/"+tag;
            if(Directory.Exists(folder))throw new InvalidOperationException("Preserve previous cave probes; select a fresh tag.");
            var foundation=JsonUtility.FromJson<FoundationStatus>(File.ReadAllText(Io(foundationFolder+"/Foundation Receipt.json")));
            if(foundation.status!="MEASURED_CANDIDATE_REQUIRES_WORLD_INTEGRATION_AND_VISUAL_REVIEW"||!foundation.sourceBytesPreserved||!foundation.requiresDigger||foundation.validationIssues==null||foundation.validationIssues.Length!=0)throw new InvalidOperationException("Actual measured source-preserved vaulted Vista foundation required.");
            for(int i=0;i<SceneManager.sceneCount;i++){var s=SceneManager.GetSceneAt(i);if(s.path==Preview||s.isDirty||string.IsNullOrEmpty(s.path))throw new InvalidOperationException("Preserve saved incoming scenes before isolated cave qualification.");}
            var proof=LastRoundedCavernProbe=new RoundedCavernProbeEvidence{tag=tag,prefab=folder+"/Cavern Input Probe.prefab",preview=folder+"/Cavern Input Probe.unity"};
            var finishSources=finish?AuthoredPaletteSources().Concat(CavernFinishSources()).SelectMany(p=>AssetDatabase.GetDependencies(p,true)):Array.Empty<string>();
            string[] weightLayers=finish?new[]{"Grass_Light_VistaSample","Grass_Dark_VistaSample","Sediment_VistaSample","RockFace_VistaSample"}.Select((name,index)=>Root+"Materials/TerrainAppearance/Kingdom "+index+" "+name+".terrainlayer").ToArray():Array.Empty<string>();
            if(weightLayers.Any(path=>AssetDatabase.LoadAssetAtPath<TerrainLayer>(path)==null))throw new InvalidOperationException("Existing original four appearance layers required; isolated probe must not create canonical layers.");
            var watched=SourceFiles(foundationFolder).Concat(AssetDatabase.GetDependencies(Prefab,true)).Concat(finishSources).Concat(weightLayers.SelectMany(path=>AssetDatabase.GetDependencies(path,true))).SelectMany(p=>new[]{p,p+".meta"}).Where(p=>File.Exists(Io(p))).Distinct().ToDictionary(p=>p,p=>Hash(File.ReadAllBytes(Io(p))));
            var incoming=EditorSceneManager.GetSceneManagerSetup();Scene scene=default;bool excavationAttempted=false;
            try
            {
                Directory.CreateDirectory(folder+"/Terrain");AssetDatabase.Refresh();scene=EditorSceneManager.OpenScene(Preview,OpenSceneMode.Additive);EditorSceneManager.SetActiveScene(scene);
                var world=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<DragonKingdomWorld>(true)).Single();
                var poses=CriticalTransforms(world).Distinct().ToDictionary(t=>t,t=>Pose(t));string graph=JsonUtility.ToJson(world.definition.graph);
                InstallFoundation(world,folder,foundationFolder);
                if(finish)
                {
                    // Match the existing full-world sequence: the raw Vista
                    // foundation has one layer. The established appearance
                    // adapter derives four normalized weights without changing
                    // heights; the authored palette then preserves those weights.
                    proof.innerWeightPreparation=world.GetComponentsInChildren<Terrain>(true).Select(t=>DragonKingdomWorldVisualMigration.RepairTerrainAppearance(t.terrainData)).ToArray();
                    if(proof.innerWeightPreparation.Length!=4||proof.innerWeightPreparation.Any(p=>p.heightHashBefore!=p.heightHashAfter||p.channels!=4||p.nonFinite!=0||p.zeroSumCells!=0))throw new InvalidOperationException("Original four-layer normalized weight preparation must preserve exact heights.");
                    // Reuse the actual source-authored four-layer palette
                    // BEFORE Init(false) snapshots layers into Digger materials.
                    proof.innerPalette=InstallAuthoredInnerPalette(world,folder);
                    proof.authoredInnerPaletteBeforeDigger=proof.innerPalette.terrains==4&&proof.innerPalette.layers.Length==4&&proof.innerPalette.appearanceOperationsPreservedGeometryAndAlpha;
                    if(!proof.authoredInnerPaletteBeforeDigger)throw new InvalidOperationException("Four exact authored inner layers must preserve geometry/alpha before native Digger initialization.");
                }
                excavationAttempted=true;var excavation=DragonKingdomDiggerCavernAuthoring.ExcavateCandidate(world,tag);while(excavation.MoveNext())yield return excavation.Current;
                if(excavationAttempted&&DragonKingdomDiggerCavernAuthoring.LastEvidence?.dataFolder=="Assets/DiggerData/Scenes/DK-"+tag)proof.cavern=DragonKingdomDiggerCavernAuthoring.LastEvidence;
                if(proof.cavern==null||proof.cavern.status!="MEASURED_CAVERN_REQUIRES_FULL_WORLD_GATES_AND_RENDER_REVIEW"||proof.cavern.roundedProfile==null||!proof.cavern.roundedProfile.nativeRequiredColumnsPassed)throw new InvalidOperationException("Actual rounded native floor/roof/input/source requirements did not pass.");
                if(finish)proof.caveFinish=InstallSharedCavernFinish(world,folder,Argument("-tsDragonCavernOccupancyEvidence",""),Argument("-tsDragonCavernLiveEvidence",""));
                if(nativeFaceDiagnostic)proof.nativeFaceNormals=DragonKingdomRoadSurfaceProbe.CaptureNativeFaceNormalDiagnostic(world,tag);
                proof.criticalPosesExact=poses.All(p=>p.Key!=null&&Pose(p.Key)==p.Value)&&JsonUtility.ToJson(world.definition.graph)==graph;
                if(!proof.criticalPosesExact)throw new InvalidOperationException("Isolated excavation changed original graph/start/extraction/aerial poses.");
                AssetDatabase.SaveAssets();if(PrefabUtility.SaveAsPrefabAsset(world.gameObject,proof.prefab)==null||!EditorSceneManager.SaveScene(scene,proof.preview))throw new InvalidOperationException("Actual qualified cave-only probe did not save.");
                proof.saved=true;proof.status="ACTUAL_ROUNDED_CAVE_COLUMNS_SAVED_REQUIRES_RENDER_ROADS_FULL_LIVE_FLIGHT_AND_WHOLE_WORLD_ACCEPTANCE";
            }
            finally
            {
                if(excavationAttempted&&DragonKingdomDiggerCavernAuthoring.LastEvidence?.dataFolder=="Assets/DiggerData/Scenes/DK-"+tag)proof.cavern=DragonKingdomDiggerCavernAuthoring.LastEvidence;
                if(!proof.saved){proof.status="REJECTED_ISOLATED_CAVERN_PROBE";proof.error="Read actual native exception and Digger column/input receipts; no acceptance inferred.";}
                proof.sourcesExact=watched.All(p=>File.Exists(Io(p.Key))&&Hash(File.ReadAllBytes(Io(p.Key)))==p.Value);
                Directory.CreateDirectory(Output());File.WriteAllText(Path.Combine(Output(),"isolated-rounded-cavern-"+tag+".json"),JsonUtility.ToJson(proof,true));
                if(!DragonKingdomDiggerCavernAuthoring.IsBusy){if(scene.IsValid()&&scene.isLoaded)EditorSceneManager.CloseScene(scene,true);EditorSceneManager.RestoreSceneManagerSetup(incoming);}
                else Debug.LogError("Preserve isolated cave scene while actual Digger jobs remain busy.");
                if(!proof.sourcesExact)throw new InvalidOperationException("Isolated cave qualification changed an original/foundation/dependency source.");
            }
        }
    }
}
#endif

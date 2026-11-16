#if UNITY_EDITOR
using System;
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
        [Serializable]public sealed class Evidence
        {
            public string scope="ACTUAL_PUBLIC_OWNED_TYPE_UPDATE_LOOKUP_AND_REAL_MARKER_CREATION_NO_VENDOR_TERRAIN_BUILD_OR_WORLD_ACCEPTANCE",status="IN_PROGRESS",error,firstId,secondId,actualRoadName;
            public bool emptyBatchRunnerSaved,isolatedBatchRunnerSaved;public string savedBatchRunnerPath;public RunnerInventory[] bootstrapInventory;
            public int registryBefore,registryAfter,actualMarkers;public float actualDefaultIndent,actualDefaultSurrounding,actualRoadIndent,actualRoadSurrounding;
            public float requestedRoadWidth,actualRoadWidthBeforeFinalRefresh,actualRoadWidth,typeRoadWidth,laneWidth,leftShoulderWidth,rightShoulderWidth;public bool actualRoadWidthMatchesRequested;
            public NativeTypeSettings[] publicTypeSettings;
            public bool existingTypesExact,collisionRejected,markerReturnsExact,actualBindingIdentityExact,actualReturnedRoadComponentExact,actualRoadTypeSemanticsExact,registeredTypeReferenceShared,otherOwnedRoadGlobalsExact,sourceBytesExact,terrainHeightsExact,incomingContextExact;public string[] watchedSources;public string actualRoadTypeId,actualRoadTypeName;
        }
        public static Evidence LastEvidence{get;private set;}
        static string Sha(string path){using(var sha=System.Security.Cryptography.SHA256.Create())return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();}
        static string Heights(TerrainData data){var h=data.GetHeights(0,0,513,513);var bytes=new byte[h.Length*4];Buffer.BlockCopy(h,0,bytes,0,bytes.Length);using(var sha=System.Security.Cryptography.SHA256.Create())return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();}
        [Serializable]public sealed class RunnerPose{public int id,parent;public string name;public Vector3 position,scale;public Quaternion rotation;public bool active;}
        [Serializable]public sealed class RunnerInventory{public int handle;public string beforePath,savedPath;public bool wasDirty,wasActive,isolatedBatchSaveAuthorized,objectsExact,dirtyStateExact;public RunnerPose[] poses;}
        static string OutputDirectory(){var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonOutput");return at>=0&&at+1<args.Length?args[at+1]:"Logs/TruckSports/DragonKingdom";}
        static void Persist(Evidence proof){string output=OutputDirectory();Directory.CreateDirectory(output);File.WriteAllText(Path.Combine(output,"actual-owned-native-road-type-probe.json"),JsonUtility.ToJson(proof,true));}
        static RunnerPose[] RunnerPoses(Scene scene)=>scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Select(t=>new RunnerPose{id=t.GetInstanceID(),parent=t.parent==null?0:t.parent.GetInstanceID(),name=t.name,position=t.localPosition,rotation=t.localRotation,scale=t.localScale,active=t.gameObject.activeSelf}).ToArray();
        static void PrepareIsolatedBatchRunner(Evidence proof,System.Collections.Generic.Dictionary<string,string> sources)
        {
            var scenes=Enumerable.Range(0,SceneManager.sceneCount).Select(SceneManager.GetSceneAt).ToArray();var records=scenes.Select(scene=>new RunnerInventory{handle=scene.handle,beforePath=scene.path,wasDirty=scene.isDirty,wasActive=scene==SceneManager.GetActiveScene(),poses=RunnerPoses(scene)}).ToArray();proof.bootstrapInventory=records;Persist(proof);
            bool isolated=Application.isBatchMode&&Application.dataPath.Replace('\\','/').TrimEnd('/').EndsWith("/.sports-validation/TruckSportsPublicValidation/Assets",StringComparison.OrdinalIgnoreCase);
            try
            {
                for(int i=0;i<scenes.Length;i++)
                {
                    var runner=scenes[i];var record=records[i];if(!string.IsNullOrEmpty(runner.path)){record.objectsExact=true;record.dirtyStateExact=true;continue;}
                    record.isolatedBatchSaveAuthorized=isolated;
                    if(!isolated)throw new InvalidOperationException("Preserve untitled user scenes outside the exact isolated validation batch project; no automatic save/close.");
                    const string generated="Assets/TruckDragonKingdom/Tests/World/Generated";Directory.CreateDirectory(generated);string saved=generated+"/NativeTypeRunner-"+Guid.NewGuid().ToString("N")+".unity";
                    if(!EditorSceneManager.SaveScene(runner,saved))throw new InvalidOperationException("Could not save isolated native batch runner to unique generated path.");
                    if(record.wasDirty)EditorSceneManager.MarkSceneDirty(runner);
                    record.savedPath=saved;proof.isolatedBatchRunnerSaved=true;record.objectsExact=JsonUtility.ToJson(new RunnerPoseSet{poses=RunnerPoses(runner)})==JsonUtility.ToJson(new RunnerPoseSet{poses=record.poses});record.dirtyStateExact=runner.isDirty==record.wasDirty;
                    if(!record.objectsExact||!record.dirtyStateExact)throw new InvalidOperationException("Isolated bootstrap save changed actual runner objects/poses/dirty-state.");proof.emptyBatchRunnerSaved=record.poses.Length==0&&!record.wasDirty;proof.savedBatchRunnerPath=saved;
                }
            }
            catch(Exception error){proof.status="REJECTED_NATIVE_PUBLIC_TYPE_BOOTSTRAP";proof.error=error.ToString();throw;}
            finally{proof.sourceBytesExact=sources.All(p=>File.Exists(p.Key)&&Sha(p.Key)==p.Value);proof.incomingContextExact=scenes.Length==SceneManager.sceneCount&&scenes.Select((scene,i)=>new{scene,record=records[i]}).All(p=>p.scene.IsValid()&&p.scene.isLoaded&&p.scene.isDirty==p.record.wasDirty&&(p.scene==SceneManager.GetActiveScene())==p.record.wasActive&&JsonUtility.ToJson(new RunnerPoseSet{poses=RunnerPoses(p.scene)})==JsonUtility.ToJson(new RunnerPoseSet{poses=p.record.poses}));Persist(proof);if(!proof.sourceBytesExact||!proof.incomingContextExact)throw new InvalidOperationException("Bootstrap altered source bytes or prior runner objects/dirty/active context.");}
        }
        [Serializable]sealed class RunnerPoseSet{public RunnerPose[] poses;}
        public static void CaptureFromCommandLine()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||Terrain.activeTerrains.Length!=0||UnityEngine.Object.FindObjectsByType<ERModularBase>(FindObjectsSortMode.None).Length!=0)throw new InvalidOperationException("Stopped isolated native type probe; preserve any unrelated active terrain/network.");
            const string networkPath="Assets/EasyRoads3D/Resources/ERRoadNetwork.prefab",materialPath="Assets/EasyRoads3D/Resources/Materials/roads/road material.mat";
            var proof=new Evidence{watchedSources=new[]{networkPath,networkPath+".meta",materialPath,materialPath+".meta"}};LastEvidence=proof;var source=proof.watchedSources.ToDictionary(p=>p,Sha);
            PrepareIsolatedBatchRunner(proof,source);
            var incoming=Enumerable.Range(0,SceneManager.sceneCount).Select(i=>SceneManager.GetSceneAt(i)).Select(s=>new{s,dirty=s.isDirty,roots=s.GetRootGameObjects().Select(g=>g.GetInstanceID()).ToArray()}).ToArray();var prior=SceneManager.GetActiveScene();Scene scene=default;TerrainData data=null;
            try
            {
                scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);EditorSceneManager.SetActiveScene(scene);data=new TerrainData{heightmapResolution=513,size=new Vector3(1600,600,1600)};var terrainObject=Terrain.CreateTerrainGameObject(data);terrainObject.transform.position=new Vector3(-800,-20,-800);string beforeHeight=Heights(data);
                var root=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(networkPath),scene);var owner=root.GetComponent<ERModularBase>();owner.RoadNetworkInit();var network=new ERRoadNetwork(owner);var defaults=owner.roadTypes.Select(t=>JsonUtility.ToJson(t)).ToArray();proof.registryBefore=defaults.Length;
                var typeSettings=new System.Collections.Generic.List<NativeTypeSettings>{CapturePublicTypeSettings("SDK_OWNER_UNTOUCHED_INITIAL_NATIVE_TYPE",owner.roadTypes[0]),CapturePublicTypeSettings("FRESH_PUBLIC_ERRoadType_CONSTRUCTOR",new ERRoadType())};proof.publicTypeSettings=typeSettings.ToArray();
                foreach(var production in network.GetRoadTypes())typeSettings.Add(CapturePublicTypeSettings("SDK_PUBLIC_PRODUCTION_WRAPPER_"+production.roadTypeName,production));proof.publicTypeSettings=typeSettings.ToArray();
                ERRoadType Type(string name)=>new ERRoadType{roadTypeName=name,roadWidth=18,hasMeshCollider=true,terrainDeformation=true,roadMaterial=AssetDatabase.LoadAssetAtPath<Material>(materialPath)};
                var first=Type("native-probe-first");typeSettings.Add(CapturePublicTypeSettings("CONFIGURED_PUBLIC_FIRST_WRAPPER_BEFORE_REGISTRATION",first));DragonKingdomWorldAuthoring.RegisterOwnedNativeGradingType(first,owner,28,110);var firstBinding=ERRoadType.GetRoadType(first,owner);typeSettings.Add(CapturePublicTypeSettings("ACTUAL_REGISTERED_FIRST_NATIVE_TYPE_BEFORE_ROAD",firstBinding));proof.publicTypeSettings=typeSettings.ToArray();
                var second=Type("native-probe-second");DragonKingdomWorldAuthoring.RegisterOwnedNativeGradingType(second,owner,30,90);var secondBinding=ERRoadType.GetRoadType(second,owner);
                proof.firstId=first.id.ToString("R",System.Globalization.CultureInfo.InvariantCulture);proof.secondId=second.id.ToString("R",System.Globalization.CultureInfo.InvariantCulture);proof.actualBindingIdentityExact=firstBinding!=null&&secondBinding!=null&&!ReferenceEquals(firstBinding,secondBinding)&&firstBinding.roadTypeName==first.roadTypeName&&secondBinding.roadTypeName==second.roadTypeName&&firstBinding.defaultIndent==28&&firstBinding.defaultSurrounding==110&&secondBinding.defaultIndent==30&&secondBinding.defaultSurrounding==90;
                try{DragonKingdomWorldAuthoring.RegisterOwnedNativeGradingType(first,owner,28,110);}catch(InvalidOperationException){proof.collisionRejected=true;}
                var otherRoad=network.CreateRoad("Actual separate second type probe",second,new[]{new Vector3(-80,32,60),new Vector3(80,32,60)},new[]{ERMarkerControlType.StraightXZY,ERMarkerControlType.StraightXZY});var other=otherRoad.roadScript;float otherIndent=other.indent,otherSurrounding=other.surrounding;string otherType=JsonUtility.ToJson(other.rt);
                var road=network.CreateRoad("Actual owned first type probe",first,new[]{new Vector3(-80,24,0),new Vector3(80,24,0)},new[]{ERMarkerControlType.StraightXZY,ERMarkerControlType.StraightXZY});proof.markerReturnsExact=true;
                for(int i=0;i<2;i++)proof.markerReturnsExact&=road.SetIndent(28,i)==28&&road.SetSurrounding(110,i)==110;
                var actual=road.roadScript;proof.requestedRoadWidth=first.roadWidth;proof.actualRoadWidthBeforeFinalRefresh=actual==null?0:actual.roadWidth;proof.actualReturnedRoadComponentExact=actual!=null&&actual.gameObject==road.gameObject&&actual.transform.IsChildOf(owner.transform);if(actual!=null&&actual.rt!=null){proof.actualRoadTypeId=actual.rt.id.ToString("R",System.Globalization.CultureInfo.InvariantCulture);proof.actualRoadTypeName=actual.rt.roadTypeName;proof.registeredTypeReferenceShared=ReferenceEquals(actual.rt,firstBinding);proof.actualDefaultIndent=actual.rt.defaultIndent;proof.actualDefaultSurrounding=actual.rt.defaultSurrounding;}Persist(proof);actual=DragonKingdomWorldAuthoring.ConfigureOwnedNativeRoadGlobals(road,first,owner,28,110);road.Refresh();proof.actualRoadWidth=actual.roadWidth;proof.typeRoadWidth=actual.rt.roadWidth;proof.laneWidth=actual.laneWidth;proof.leftShoulderWidth=actual.leftShoulderWidth;proof.rightShoulderWidth=actual.rightShoulderWidth;proof.actualRoadWidthMatchesRequested=actual.roadWidth==first.roadWidth&&actual.rt.roadWidth==first.roadWidth;proof.actualReturnedRoadComponentExact=actual==road.roadScript&&actual.gameObject==road.gameObject&&actual.transform.IsChildOf(owner.transform);proof.registeredTypeReferenceShared=ReferenceEquals(actual.rt,firstBinding);proof.actualRoadTypeId=actual.rt.id.ToString("R",System.Globalization.CultureInfo.InvariantCulture);proof.actualRoadTypeName=actual.rt.roadTypeName;proof.actualRoadTypeSemanticsExact=actual.rt.id==firstBinding.id&&actual.rt.roadTypeName==firstBinding.roadTypeName;proof.otherOwnedRoadGlobalsExact=other.indent==otherIndent&&other.surrounding==otherSurrounding&&JsonUtility.ToJson(other.rt)==otherType;proof.actualRoadName=actual.name;proof.actualRoadIndent=actual.indent;proof.actualRoadSurrounding=actual.surrounding;proof.actualDefaultIndent=actual.rt.defaultIndent;proof.actualDefaultSurrounding=actual.rt.defaultSurrounding;proof.actualMarkers=road.GetMarkerCount();proof.registryAfter=owner.roadTypes.Count;proof.existingTypesExact=owner.roadTypes.Take(defaults.Length).Select(t=>JsonUtility.ToJson(t)).SequenceEqual(defaults);proof.terrainHeightsExact=Heights(data)==beforeHeight;
                if(!proof.actualBindingIdentityExact||!proof.actualReturnedRoadComponentExact||!proof.actualRoadTypeSemanticsExact||!proof.otherOwnedRoadGlobalsExact||!proof.collisionRejected||!proof.markerReturnsExact||!proof.existingTypesExact||!proof.terrainHeightsExact||proof.registryAfter!=proof.registryBefore+2||proof.actualMarkers!=2||proof.actualRoadIndent!=28||proof.actualRoadSurrounding!=110||proof.actualDefaultIndent!=28||proof.actualDefaultSurrounding!=110)throw new InvalidOperationException("Actual public type/default/marker contract did not produce expected owned native road settings: "+JsonUtility.ToJson(proof));
                typeSettings.Add(CapturePublicTypeSettings("ACTUAL_CREATED_FIRST_ROAD_NATIVE_RT_AFTER_REFRESH",actual.rt));proof.publicTypeSettings=typeSettings.ToArray();
                proof.status="ACTUAL_PUBLIC_TYPE_DEFAULTS_AND_MARKERS_PASS_REQUIRES_REAL_GRADING_AND_WORLD_ACCEPTANCE";
            }
            catch(Exception error){proof.status="REJECTED_NATIVE_PUBLIC_TYPE_PROBE";proof.error=error.ToString();throw;}
            finally
            {
                if(scene.IsValid()&&scene.isLoaded)EditorSceneManager.CloseScene(scene,true);if(data!=null)UnityEngine.Object.DestroyImmediate(data);if(prior.IsValid()&&prior.isLoaded)EditorSceneManager.SetActiveScene(prior);
                proof.sourceBytesExact=source.All(p=>File.Exists(p.Key)&&Sha(p.Key)==p.Value);proof.incomingContextExact=SceneManager.sceneCount==incoming.Length&&SceneManager.GetActiveScene()==prior&&incoming.All(p=>p.s.IsValid()&&p.s.isLoaded&&p.s.isDirty==p.dirty&&p.s.GetRootGameObjects().Select(g=>g.GetInstanceID()).SequenceEqual(p.roots));
                var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonOutput");string output=at>=0&&at+1<args.Length?args[at+1]:"Logs/TruckSports/DragonKingdom";Directory.CreateDirectory(output);File.WriteAllText(Path.Combine(output,"actual-owned-native-road-type-probe.json"),JsonUtility.ToJson(proof,true));if(!proof.sourceBytesExact||!proof.incomingContextExact)throw new InvalidOperationException("Disposable public type probe changed source bytes or incoming scene context.");
            }
        }
    }
}
#endif

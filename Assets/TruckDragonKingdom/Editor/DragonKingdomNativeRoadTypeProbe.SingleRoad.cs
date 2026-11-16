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
        [Serializable] public sealed class SingleRoadField
        {
            public string stage, normalizedFloat32Path, sha256;
            public int changedFromInput;
            public float maximumInputDifference, maximumAdjacentStep;
            public float[] crossSectionAtXZero;
        }
        [Serializable] public sealed class SingleRoadEvidence
        {
            public string scope="ISOLATED_ACTUAL_SINGLE_ROAD_VENDOR_BUILD_DIAGNOSIS_NOT_WORLD_OR_SURROUND_INTERPOLATION_ACCEPTANCE",status="IN_PROGRESS",error;
            public int actualVendorBuilds,resolution=513;
            public Vector3 origin=new Vector3(-800,-20,-800),size=new Vector3(1600,600,1600);
            public float requestedIndent=28,requestedSurrounding=110,requestedResolution=.35f,roadHeight=200,roadWidth=18;
            public string terrainAsset,networkSettingsBeforeBuild,actualRoadSettingsBeforeBuild,inputRecipeSha256,actualInitialHeightfieldSha256;
            public bool markerReturnsExact,existingTypesExact,sourceBytesExact,incomingContextExact;
            public NativeTypeSettings registeredType;
            public Vector3[] spline,leftIndent,rightIndent,leftSurrounding,rightSurrounding;
            public List<SingleRoadField> fields=new List<SingleRoadField>();
        }
        public static SingleRoadEvidence LastSingleRoadEvidence{get;private set;}
        static string SingleRoadDigest(byte[] bytes){using(var sha=System.Security.Cryptography.SHA256.Create())return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();}
        static void SaveSingleRoadField(SingleRoadEvidence proof,TerrainData data,float[,] original,string stage)
        {
            var values=data.GetHeights(0,0,proof.resolution,proof.resolution);var bytes=new byte[values.Length*4];Buffer.BlockCopy(values,0,bytes,0,bytes.Length);
            string path=Path.Combine(OutputDirectory(),"single-road-"+stage+".normalized.f32");File.WriteAllBytes(path,bytes);
            var field=new SingleRoadField{stage=stage,normalizedFloat32Path=path,sha256=SingleRoadDigest(bytes),crossSectionAtXZero=new float[proof.resolution]};
            for(int z=0;z<proof.resolution;z++)for(int x=0;x<proof.resolution;x++)
            {
                float h=values[z,x];if(!float.IsFinite(h))throw new InvalidOperationException("Vendor returned nonfinite height at actual grid vertex "+x+","+z);
                if(h!=original[z,x]){field.changedFromInput++;field.maximumInputDifference=Mathf.Max(field.maximumInputDifference,Mathf.Abs(h-original[z,x])*proof.size.y);}
                if(x>0)field.maximumAdjacentStep=Mathf.Max(field.maximumAdjacentStep,Mathf.Abs(h-values[z,x-1])*proof.size.y);
                if(z>0)field.maximumAdjacentStep=Mathf.Max(field.maximumAdjacentStep,Mathf.Abs(h-values[z-1,x])*proof.size.y);
                if(x==256)field.crossSectionAtXZero[z]=proof.origin.y+h*proof.size.y;
            }
            proof.fields.Add(field);SaveSingleRoadEvidence(proof);
        }
        static void SaveSingleRoadEvidence(SingleRoadEvidence proof){Directory.CreateDirectory(OutputDirectory());File.WriteAllText(Path.Combine(OutputDirectory(),"actual-single-road-native-grading.json"),JsonUtility.ToJson(proof,true));}
        public static void CaptureSingleRoadGradingFromCommandLine()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||Terrain.activeTerrains.Length!=0||UnityEngine.Object.FindObjectsByType<ERModularBase>(FindObjectsSortMode.None).Length!=0)throw new InvalidOperationException("Stopped isolated single-road fixture must preserve unrelated active terrain/networks.");
            const string networkPath="Assets/EasyRoads3D/Resources/ERRoadNetwork.prefab",materialPath="Assets/EasyRoads3D/Resources/Materials/roads/road material.mat";
            var source=new[]{networkPath,networkPath+".meta",materialPath,materialPath+".meta",typeof(ERRoad).Assembly.Location}.Distinct().ToDictionary(p=>p,Sha);
            var bootstrap=new Evidence();PrepareIsolatedBatchRunner(bootstrap,source);
            var incoming=Enumerable.Range(0,SceneManager.sceneCount).Select(SceneManager.GetSceneAt).Select(s=>new{s,dirty=s.isDirty,poses=JsonUtility.ToJson(new RunnerPoseSet{poses=RunnerPoses(s)})}).ToArray();var prior=SceneManager.GetActiveScene();
            var proof=new SingleRoadEvidence();LastSingleRoadEvidence=proof;Scene scene=default;TerrainData data=null;GameObject root=null;
            Directory.CreateDirectory(OutputDirectory());SaveSingleRoadEvidence(proof);
            try
            {
                scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);EditorSceneManager.SetActiveScene(scene);
                data=new TerrainData{heightmapResolution=proof.resolution,size=proof.size};var original=new float[proof.resolution,proof.resolution];
                // Known planar cross-slope:300m at south edge,450m at north edge.
                // All stations are true3.125m TerrainData vertices, never cell midpoints.
                for(int z=0;z<proof.resolution;z++)for(int x=0;x<proof.resolution;x++)original[z,x]=(300f+150f*z/(proof.resolution-1)-proof.origin.y)/proof.size.y;
                var recipeBytes=new byte[original.Length*4];Buffer.BlockCopy(original,0,recipeBytes,0,recipeBytes.Length);proof.inputRecipeSha256=SingleRoadDigest(recipeBytes);
                data.SetHeights(0,0,original);original=data.GetHeights(0,0,proof.resolution,proof.resolution);var actualBytes=new byte[original.Length*4];Buffer.BlockCopy(original,0,actualBytes,0,actualBytes.Length);proof.actualInitialHeightfieldSha256=SingleRoadDigest(actualBytes);
                const string generated="Assets/TruckDragonKingdom/Tests/World/Generated";Directory.CreateDirectory(generated);proof.terrainAsset=generated+"/NativeSingleRoad-"+Guid.NewGuid().ToString("N")+".asset";AssetDatabase.CreateAsset(data,proof.terrainAsset);
                var terrainObject=Terrain.CreateTerrainGameObject(data);terrainObject.transform.position=proof.origin;SaveSingleRoadField(proof,data,original,"beforeNetwork");
                root=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(networkPath),scene);var owner=root.GetComponent<ERModularBase>();
                var settings=new SerializedObject(owner);var rayHeight=settings.FindProperty("terrainRaycastHeight");if(rayHeight==null)throw new InvalidOperationException("Installed terrain ray-query setting absent.");if(rayHeight.propertyType==SerializedPropertyType.Float)rayHeight.floatValue=1000;else if(rayHeight.propertyType==SerializedPropertyType.Integer)rayHeight.intValue=1000;else throw new InvalidOperationException("Terrain ray-query setting must be numeric.");settings.ApplyModifiedPropertiesWithoutUndo();
                owner.RoadNetworkInit();var defaults=owner.roadTypes.Select(t=>JsonUtility.ToJson(t)).ToArray();var network=new ERRoadNetwork(owner);
                var type=new ERRoadType{roadTypeName="Owned single-road actual surround diagnosis",roadWidth=proof.roadWidth,hasMeshCollider=true,terrainDeformation=true,roadMaterial=AssetDatabase.LoadAssetAtPath<Material>(materialPath)};
                DragonKingdomWorldAuthoring.RegisterOwnedNativeGradingType(type,owner,proof.requestedIndent,proof.requestedSurrounding);proof.registeredType=CapturePublicTypeSettings("ACTUAL_SINGLE_ROAD_REGISTERED_NATIVE_TYPE",ERRoadType.GetRoadType(type,owner));
                var road=network.CreateRoad(type.roadTypeName,type,new[]{new Vector3(-250,proof.roadHeight,0),new Vector3(250,proof.roadHeight,0)},new[]{ERMarkerControlType.StraightXZY,ERMarkerControlType.StraightXZY});
                road.SetTerrainDeformation(true,false);road.SnapToTerrain(false,false);road.SetResolution(proof.requestedResolution,false);proof.markerReturnsExact=true;
                for(int i=0;i<2;i++)proof.markerReturnsExact&=road.SetIndent(proof.requestedIndent,i)==proof.requestedIndent&&road.SetSurrounding(proof.requestedSurrounding,i)==proof.requestedSurrounding;
                var actual=DragonKingdomWorldAuthoring.ConfigureOwnedNativeRoadGlobals(road,type,owner,proof.requestedIndent,proof.requestedSurrounding);road.Refresh();
                proof.spline=actual.splinePoints.ToArray();proof.leftIndent=actual.leftIndentVecs.ToArray();proof.rightIndent=actual.rightIndentVecs.ToArray();proof.leftSurrounding=actual.leftSurroundingVecs.ToArray();proof.rightSurrounding=actual.rightSurroundingVecs.ToArray();proof.networkSettingsBeforeBuild=JsonUtility.ToJson(owner);proof.actualRoadSettingsBeforeBuild=JsonUtility.ToJson(actual);
                proof.existingTypesExact=owner.roadTypes.Take(defaults.Length).Select(t=>JsonUtility.ToJson(t)).SequenceEqual(defaults);
                if(!proof.markerReturnsExact||!proof.existingTypesExact||actual.roadWidth!=proof.roadWidth||actual.indent!=28||actual.surrounding!=110||proof.spline.Length==0||proof.leftSurrounding.Length==0)throw new InvalidOperationException("Actual owned road creation/marker/contour configuration failed before Build.");
                SaveSingleRoadField(proof,data,original,"beforeBuild");network.BuildRoadNetwork();proof.actualVendorBuilds++;SaveSingleRoadField(proof,data,original,"afterBuild");
                UnityEngine.Object.DestroyImmediate(root);root=null;SaveSingleRoadField(proof,data,original,"afterNetworkDestroy");
                if(proof.fields.Single(f=>f.stage=="beforeBuild").changedFromInput!=0||proof.fields.Single(f=>f.stage=="afterBuild").changedFromInput==0)throw new InvalidOperationException("Fixture requires unchanged pre-Build field and genuine nonempty actual vendor Build output.");
                proof.status="ACTUAL_SINGLE_VENDOR_BUILD_CAPTURED_REQUIRES_TRANSITION_ANALYSIS_NOT_WORLD_ACCEPTANCE";
            }
            catch(Exception error){proof.status="REJECTED_SINGLE_ROAD_GRADING_DIAGNOSTIC";proof.error=error.ToString();throw;}
            finally
            {
                if(root!=null)UnityEngine.Object.DestroyImmediate(root);if(scene.IsValid()&&scene.isLoaded)EditorSceneManager.CloseScene(scene,true);if(prior.IsValid()&&prior.isLoaded)EditorSceneManager.SetActiveScene(prior);
                if(!string.IsNullOrEmpty(proof.terrainAsset))AssetDatabase.DeleteAsset(proof.terrainAsset);else if(data!=null)UnityEngine.Object.DestroyImmediate(data);
                proof.sourceBytesExact=source.All(p=>File.Exists(p.Key)&&Sha(p.Key)==p.Value);proof.incomingContextExact=SceneManager.sceneCount==incoming.Length&&SceneManager.GetActiveScene()==prior&&incoming.All(p=>p.s.IsValid()&&p.s.isLoaded&&p.s.isDirty==p.dirty&&JsonUtility.ToJson(new RunnerPoseSet{poses=RunnerPoses(p.s)})==p.poses);SaveSingleRoadEvidence(proof);
                if(!proof.sourceBytesExact||!proof.incomingContextExact)throw new InvalidOperationException("Single-road diagnostic changed watched source bytes or incoming scene/context.");
            }
        }
    }
}
#endif

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
        [Serializable] public sealed class SourceCurveStation
        {
            public Vector3 originalWorldVertex,translatedVertex;
            public int column,row,actualIndentQuad=-1,actualMarkerIndex=-1;
            public float actualContourTargetY,actualTerrainY,terrainMinusContour;
            public bool insideActualIndentQuad,actualBridgeElement;
        }
        [Serializable] public sealed class SourceCurveEvidence
        {
            public string scope="ISOLATED_ORIGINAL_ROADSPEC_AND_NATIVE_GRADING_MARKERS_EXCLUSIONS_XZ_TRANSLATION_ONLY_NO_OTHER_ROADS_NOT_WORLD_ACCEPTANCE",status="IN_PROGRESS",error;
            public string sourceAuthority="DragonKingdomWorldAuthoring.Roads():caverns-volcano",sourceAuthoringSha256;
            public Vector3 translationPivot=new Vector3(900,0,1000);
            public Vector3[] originalSourcePoints,translatedSourcePoints,translatedActualMarkers,actualNativeMarkerPositions;
            public DragonKingdomWorldAuthoring.NativeGradingSourcePlan originalNativePlan;
            public bool sourcePointsOnlyTranslatedXZ,actualControlsExact,actualBridgeFlagsCaptured,flagsExactAcrossBuild,actualMarkerPositionsExact;
            public bool[] actualBridgeElements,actualBridgeElementsAfterBuild;public int[] actualMarkerInts,actualMarkerIntsAfterBuild;
            public SourceCurveStation[] stations;
            public SingleRoadEvidence native=new SingleRoadEvidence();
        }
        public static SourceCurveEvidence LastSourceCurveEvidence{get;private set;}
        static void SaveSourceCurve(SourceCurveEvidence proof){Directory.CreateDirectory(OutputDirectory());File.WriteAllText(Path.Combine(OutputDirectory(),"actual-original-source-curve-grading.json"),JsonUtility.ToJson(proof,true));}
        static void SaveSourceCurveField(SourceCurveEvidence proof,TerrainData data,float[,] input,string stage)
        {
            var p=proof.native;var values=data.GetHeights(0,0,p.resolution,p.resolution);var bytes=new byte[values.Length*4];Buffer.BlockCopy(values,0,bytes,0,bytes.Length);string path=Path.Combine(OutputDirectory(),"source-curve-"+stage+".normalized.f32");File.WriteAllBytes(path,bytes);
            var row=new SingleRoadField{stage=stage,normalizedFloat32Path=path,sha256=SingleRoadDigest(bytes),crossSectionAtXZero=new float[p.resolution]};
            for(int z=0;z<p.resolution;z++)for(int x=0;x<p.resolution;x++)
            {
                float h=values[z,x];if(!float.IsFinite(h))throw new InvalidOperationException("Actual source-curve vendor field contains nonfinite vertex.");
                if(h!=input[z,x]){row.changedFromInput++;row.maximumInputDifference=Mathf.Max(row.maximumInputDifference,Mathf.Abs(h-input[z,x])*p.size.y);}
                if(x>0)row.maximumAdjacentStep=Mathf.Max(row.maximumAdjacentStep,Mathf.Abs(h-values[z,x-1])*p.size.y);if(z>0)row.maximumAdjacentStep=Mathf.Max(row.maximumAdjacentStep,Mathf.Abs(h-values[z-1,x])*p.size.y);if(x==256)row.crossSectionAtXZero[z]=p.origin.y+h*p.size.y;
            }
            p.fields.Add(row);SaveSourceCurve(proof);
        }
        static bool SourceCurveTriangleHeight(Vector3 p,Vector3 a,Vector3 b,Vector3 c,out float height)
        {
            double denominator=(b.z-c.z)*(double)(a.x-c.x)+(c.x-b.x)*(double)(a.z-c.z);height=0;if(Math.Abs(denominator)<1e-10)return false;
            double u=((b.z-c.z)*(double)(p.x-c.x)+(c.x-b.x)*(double)(p.z-c.z))/denominator,v=((c.z-a.z)*(double)(p.x-c.x)+(a.x-c.x)*(double)(p.z-c.z))/denominator,w=1-u-v;if(u<0||v<0||w<0)return false;height=(float)(u*a.y+v*b.y+w*c.y);return true;
        }
        static SourceCurveStation[] CaptureSourceCurveStations(SourceCurveEvidence proof,TerrainData data,ERModularRoad actual)
        {
            var rows=new List<SourceCurveStation>();var native=proof.native;float dx=native.size.x/(native.resolution-1),dz=native.size.z/(native.resolution-1);
            foreach(float x in new[]{1250f,1253.125f,1256.25f,1259.375f})foreach(float z in new[]{1162.5f,1165.625f,1168.75f,1171.875f})
            {
                var source=new Vector3(x,0,z);var p=source-proof.translationPivot;int column=Mathf.RoundToInt((p.x-native.origin.x)/dx),row=Mathf.RoundToInt((p.z-native.origin.z)/dz);
                if(native.origin.x+column*dx!=p.x||native.origin.z+row*dz!=p.z||column<0||row<0||column>=native.resolution||row>=native.resolution)throw new InvalidOperationException("Source-curve witness must remain an exact original and translated native grid vertex.");
                var station=new SourceCurveStation{originalWorldVertex=source,translatedVertex=p,column=column,row=row,actualTerrainY=native.origin.y+data.GetHeights(column,row,1,1)[0,0]*native.size.y};
                for(int i=0;i+1<actual.leftIndentVecs.Count&&i+1<actual.rightIndentVecs.Count;i++)
                {
                    float y;if(!SourceCurveTriangleHeight(p,actual.leftIndentVecs[i],actual.leftIndentVecs[i+1],actual.rightIndentVecs[i+1],out y)&&!SourceCurveTriangleHeight(p,actual.leftIndentVecs[i],actual.rightIndentVecs[i+1],actual.rightIndentVecs[i],out y))continue;
                    station.insideActualIndentQuad=true;station.actualIndentQuad=i;station.actualContourTargetY=y;station.terrainMinusContour=station.actualTerrainY-y;
                    if(i>=actual.bridgeElement.Count||i>=actual.markerInts.Count)throw new InvalidOperationException("Actual native per-spline deformation witness arrays do not cover this contour station.");station.actualBridgeElement=actual.bridgeElement[i];station.actualMarkerIndex=actual.markerInts[i];break;
                }
                rows.Add(station);
            }
            return rows.ToArray();
        }
        public static void CaptureOriginalSourceCurveGradingFromCommandLine()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||Terrain.activeTerrains.Length!=0||UnityEngine.Object.FindObjectsByType<ERModularBase>(FindObjectsSortMode.None).Length!=0)throw new InvalidOperationException("Stopped isolated source-curve fixture must preserve unrelated terrain/networks.");
            const string networkPath="Assets/EasyRoads3D/Resources/ERRoadNetwork.prefab",materialPath="Assets/EasyRoads3D/Resources/Materials/roads/road material.mat",authoringPath="Assets/TruckDragonKingdom/Editor/DragonKingdomWorldAuthoring.cs";
            var source=new[]{networkPath,networkPath+".meta",materialPath,materialPath+".meta",authoringPath,typeof(ERRoad).Assembly.Location}.Distinct().ToDictionary(p=>p,Sha);PrepareIsolatedBatchRunner(new Evidence(),source);
            var incoming=Enumerable.Range(0,SceneManager.sceneCount).Select(SceneManager.GetSceneAt).Select(s=>new{s,dirty=s.isDirty,poses=JsonUtility.ToJson(new RunnerPoseSet{poses=RunnerPoses(s)})}).ToArray();var prior=SceneManager.GetActiveScene();var proof=new SourceCurveEvidence{sourceAuthoringSha256=source[authoringPath]};LastSourceCurveEvidence=proof;var native=proof.native;Scene scene=default;TerrainData data=null;GameObject root=null;
            SaveSourceCurve(proof);
            try
            {
                proof.originalNativePlan=DragonKingdomWorldAuthoring.InspectNativeMainRoadGradingPlan("caverns-volcano",DragonKingdomWorldAuthoring.CavernUndergroundBounds(),true);proof.originalSourcePoints=proof.originalNativePlan.originalRoadPoints;proof.translatedSourcePoints=proof.originalSourcePoints.Select(p=>p-proof.translationPivot).ToArray();proof.translatedActualMarkers=proof.originalNativePlan.actualGradingMarkers.Select(p=>p-proof.translationPivot).ToArray();native.roadWidth=proof.originalNativePlan.width;native.scope="ACTUAL_ORIGINAL_SOURCE_GRADING_MARKERS_AND_EXCLUSIONS_VARIABLE_Y_FROM_SOURCE_POINTS_ROADHEIGHT_SCALAR_NOT_APPLICABLE";
                proof.sourcePointsOnlyTranslatedXZ=proof.originalSourcePoints.Select((p,i)=>p.y==proof.translatedSourcePoints[i].y&&(p-proof.translationPivot).Equals(proof.translatedSourcePoints[i])).All(v=>v);if(!proof.sourcePointsOnlyTranslatedXZ)throw new InvalidOperationException("Source road Y/curve must remain exact under XZ translation only.");
                scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);EditorSceneManager.SetActiveScene(scene);data=new TerrainData{heightmapResolution=native.resolution,size=native.size};var input=new float[native.resolution,native.resolution];for(int z=0;z<native.resolution;z++)for(int x=0;x<native.resolution;x++)input[z,x]=(300f+150f*z/(native.resolution-1)-native.origin.y)/native.size.y;
                var bytes=new byte[input.Length*4];Buffer.BlockCopy(input,0,bytes,0,bytes.Length);native.inputRecipeSha256=SingleRoadDigest(bytes);data.SetHeights(0,0,input);input=data.GetHeights(0,0,native.resolution,native.resolution);Buffer.BlockCopy(input,0,bytes,0,bytes.Length);native.actualInitialHeightfieldSha256=SingleRoadDigest(bytes);
                const string generated="Assets/TruckDragonKingdom/Tests/World/Generated";Directory.CreateDirectory(generated);native.terrainAsset=generated+"/NativeSourceCurve-"+Guid.NewGuid().ToString("N")+".asset";AssetDatabase.CreateAsset(data,native.terrainAsset);Terrain.CreateTerrainGameObject(data).transform.position=native.origin;SaveSourceCurveField(proof,data,input,"beforeNetwork");
                root=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(networkPath),scene);var owner=root.GetComponent<ERModularBase>();var settings=new SerializedObject(owner);var rayHeight=settings.FindProperty("terrainRaycastHeight");if(rayHeight==null)throw new InvalidOperationException("Installed terrain ray-query setting absent.");if(rayHeight.propertyType==SerializedPropertyType.Float)rayHeight.floatValue=1000;else if(rayHeight.propertyType==SerializedPropertyType.Integer)rayHeight.intValue=1000;else throw new InvalidOperationException("Terrain ray-query setting must be numeric.");settings.ApplyModifiedPropertiesWithoutUndo();owner.RoadNetworkInit();var defaults=owner.roadTypes.Select(t=>JsonUtility.ToJson(t)).ToArray();var network=new ERRoadNetwork(owner);
                var type=new ERRoadType{roadTypeName="Owned original caverns-volcano source-curve diagnosis",roadWidth=native.roadWidth,hasMeshCollider=true,terrainDeformation=true,roadMaterial=AssetDatabase.LoadAssetAtPath<Material>(materialPath)};DragonKingdomWorldAuthoring.RegisterOwnedNativeGradingType(type,owner,28,110);native.registeredType=CapturePublicTypeSettings("ACTUAL_ORIGINAL_SOURCE_CURVE_REGISTERED_TYPE",ERRoadType.GetRoadType(type,owner));
                var road=network.CreateRoad(type.roadTypeName,type,proof.translatedActualMarkers,Enumerable.Repeat(ERMarkerControlType.StraightXZY,proof.translatedActualMarkers.Length).ToArray());road.SetTerrainDeformation(true,false);road.SnapToTerrain(false,false);road.SetResolution(.35f,false);native.markerReturnsExact=true;proof.actualControlsExact=true;
                for(int i=0;i<proof.translatedActualMarkers.Length;i++){native.markerReturnsExact&=road.SetIndent(28,i)==28&&road.SetSurrounding(110,i)==110;proof.actualControlsExact&=road.GetMarkerControlType(i)==ERMarkerControlType.StraightXZY;}
                foreach(var interval in proof.originalNativePlan.intervals.Where(i=>i.excluded)){road.SetTerrainDeformation(interval.marker,false);road.SetStartLevelDistance(interval.marker,16);road.SetEndLevelDistance(interval.marker,16);}
                var actual=DragonKingdomWorldAuthoring.ConfigureOwnedNativeRoadGlobals(road,type,owner,28,110);road.Refresh();proof.actualNativeMarkerPositions=road.GetMarkerPositions();proof.actualMarkerPositionsExact=proof.actualNativeMarkerPositions.SequenceEqual(proof.translatedActualMarkers);native.spline=actual.splinePoints.ToArray();native.leftIndent=actual.leftIndentVecs.ToArray();native.rightIndent=actual.rightIndentVecs.ToArray();native.leftSurrounding=actual.leftSurroundingVecs.ToArray();native.rightSurrounding=actual.rightSurroundingVecs.ToArray();proof.actualBridgeElements=actual.bridgeElement.ToArray();proof.actualMarkerInts=actual.markerInts.ToArray();proof.actualBridgeFlagsCaptured=proof.actualBridgeElements.Length==native.spline.Length&&proof.actualMarkerInts.Length==native.spline.Length;native.networkSettingsBeforeBuild=JsonUtility.ToJson(owner);native.actualRoadSettingsBeforeBuild=JsonUtility.ToJson(actual);native.existingTypesExact=owner.roadTypes.Take(defaults.Length).Select(t=>JsonUtility.ToJson(t)).SequenceEqual(defaults);
                if(!native.markerReturnsExact||!proof.actualControlsExact||!proof.actualMarkerPositionsExact||!native.existingTypesExact||actual.roadWidth!=native.roadWidth||!proof.actualBridgeFlagsCaptured)throw new InvalidOperationException("Actual source-curve native settings/flags/controls did not retain source contract.");
                SaveSourceCurveField(proof,data,input,"beforeBuild");network.BuildRoadNetwork();native.actualVendorBuilds++;SaveSourceCurveField(proof,data,input,"afterBuild");proof.actualBridgeElementsAfterBuild=actual.bridgeElement.ToArray();proof.actualMarkerIntsAfterBuild=actual.markerInts.ToArray();proof.flagsExactAcrossBuild=proof.actualBridgeElements.SequenceEqual(proof.actualBridgeElementsAfterBuild)&&proof.actualMarkerInts.SequenceEqual(proof.actualMarkerIntsAfterBuild);proof.stations=CaptureSourceCurveStations(proof,data,actual);SaveSourceCurve(proof);UnityEngine.Object.DestroyImmediate(root);root=null;SaveSourceCurveField(proof,data,input,"afterNetworkDestroy");
                if(native.fields.Single(f=>f.stage=="beforeBuild").changedFromInput!=0||native.fields.Single(f=>f.stage=="afterBuild").changedFromInput==0)throw new InvalidOperationException("Source-curve fixture requires exact unchanged pre-Build field and genuine nonempty vendor Build.");proof.status="ACTUAL_ORIGINAL_SOURCE_CURVE_BUILD_CAPTURED_REQUIRES_RASTER_ANALYSIS_NOT_WORLD_ACCEPTANCE";
            }
            catch(Exception error){proof.status="REJECTED_ORIGINAL_SOURCE_CURVE_DIAGNOSTIC";proof.error=error.ToString();throw;}
            finally
            {
                if(root!=null)UnityEngine.Object.DestroyImmediate(root);if(scene.IsValid()&&scene.isLoaded)EditorSceneManager.CloseScene(scene,true);if(prior.IsValid()&&prior.isLoaded)EditorSceneManager.SetActiveScene(prior);if(!string.IsNullOrEmpty(native.terrainAsset))AssetDatabase.DeleteAsset(native.terrainAsset);else if(data!=null)UnityEngine.Object.DestroyImmediate(data);
                native.sourceBytesExact=source.All(p=>File.Exists(p.Key)&&Sha(p.Key)==p.Value);native.incomingContextExact=SceneManager.sceneCount==incoming.Length&&SceneManager.GetActiveScene()==prior&&incoming.All(p=>p.s.IsValid()&&p.s.isLoaded&&p.s.isDirty==p.dirty&&JsonUtility.ToJson(new RunnerPoseSet{poses=RunnerPoses(p.s)})==p.poses);SaveSourceCurve(proof);if(!native.sourceBytesExact||!native.incomingContextExact)throw new InvalidOperationException("Source-curve fixture changed watched sources or incoming scene/context.");
            }
        }
    }
}
#endif

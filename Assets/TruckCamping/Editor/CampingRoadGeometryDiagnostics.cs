using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EasyRoads3Dv3;
using LWS.TruckSports.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace LWS.TruckCamping.Editor
{
 public static partial class CampingWildernessAuthoring
 {
  [Serializable] sealed class RoadMeshFacts
  {
   public string pass,asset,reason;public int vertices,submeshes,positionDifferences,normalDifferences,tangentDifferences,colorDifferences;
   public float maximumPositionDelta,maximumNormalDelta,maximumTangentDelta,maximumColorDelta;
   public Vector3 maximumPositionDeltaPerAxis,firstSourcePosition,firstOriginalPosition,firstHorizontalSourcePosition,firstHorizontalOriginalPosition;
   public int xDifferences,yDifferences,zDifferences,firstDifferentVertex=-1,firstHorizontalDifferentVertex=-1;
   public string firstSourceFloatBits,firstOriginalFloatBits,firstHorizontalSourceFloatBits,firstHorizontalOriginalFloatBits,sourcePositionsFile,originalPositionsFile;
   public bool equivalent,indexLayoutExact,allUvExact;public Vector3 boundsCenter,boundsSize,oldBoundsCenter,oldBoundsSize,sourcePosition,sourceScale;
  }
  [Serializable] sealed class RoadDiagnosticReport {public string status="MEASUREMENT_ONLY_NO_ASSET_REPAIR";public RoadMeshFacts[] meshes;}
  public static void DiagnoseOriginalAndCorrectedRoadGeometry()
  {
   if(EditorApplication.isPlaying||EditorApplication.isCompiling)throw new InvalidOperationException("Stopped compiled diagnostic required.");
   for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Preserve dirty scene before measurement.");
   SelectExactReviewedStage();VerifyRetainedInputs(false);var setup=EditorSceneManager.GetSceneManagerSetup();var rows=new List<RoadMeshFacts>();
   try
   {
    EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
    foreach(int dimension in new[]{8000,1000,1000})
    {
     var root=new GameObject("Transient original terrain/landmark road measurement");var transient=new List<TerrainData>();
     try
     {
      var world=root.AddComponent<CampingWilderness>();world.terrains=new Terrain[4];
      for(int z=0;z<2;z++)for(int x=0;x<2;x++)
      {
       var original=AssetDatabase.LoadAssetAtPath<TerrainData>("Assets/TruckCamping/Models/Camp Haul Vista terrain "+x+" "+z+".asset");if(original==null)throw new InvalidOperationException("Missing exact original terrain.");
       var copy=UnityEngine.Object.Instantiate(original);transient.Add(copy);copy.size=new Vector3(dimension,260,dimension);
       var go=Terrain.CreateTerrainGameObject(copy);go.transform.SetParent(root.transform,false);go.transform.position=new Vector3(-1000+x*1000,0,-1000+z*1000);world.terrains[z*2+x]=go.GetComponent<Terrain>();
      }
      Landmarks(world);Physics.SyncTransforms();MeasureVendorRoad("terrain"+dimension+"-pass"+rows.Count,rows);
     }
     finally{UnityEngine.Object.DestroyImmediate(root);foreach(var copy in transient)UnityEngine.Object.DestroyImmediate(copy);}
    }
    VerifyRetainedInputs(false);string dir="Logs/TruckSports/Impossible07/CampingNavigationDiagnosis";Directory.CreateDirectory(dir);File.WriteAllText(dir+"/original-corrected-road-geometry-axis-v2.json",JsonUtility.ToJson(new RoadDiagnosticReport{meshes=rows.ToArray()},true));Debug.Log("CAMPING_ROAD_MEASUREMENT rows="+rows.Count+" original127BytesUnchanged=true noPersistOrSaveAssets=true");
   }
   finally{selectedStageManifest=FailedStageManifest;if(setup.Length>0&&setup.All(s=>!string.IsNullOrEmpty(s.path)))EditorSceneManager.RestoreSceneManagerSetup(setup);else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);}
  }
  static void MeasureVendorRoad(string pass,List<RoadMeshFacts> rows)
  {
   const string name="Camp Haul scenic woodland loop";var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EasyRoads3D/Resources/ERRoadNetwork.prefab");if(prefab==null)throw new InvalidOperationException("Installed EasyRoads required.");
   var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab);
   try
   {
    var owner=instance.GetComponent<ERModularBase>();owner.RoadNetworkInit();var network=new ERRoadNetwork(owner);var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/TruckCamping/Materials/Camp Haul packed earth trail.mat");
    var type=new ERRoadType{roadTypeName=name,roadWidth=14,hasMeshCollider=true,roadMaterial=material};var markers=Trail.Select(p=>Ground(p,.09f)).ToArray();var road=network.CreateRoad(name,type,markers,Enumerable.Repeat(ERMarkerControlType.StraightXZ,markers.Length).ToArray());
    if(road==null)throw new InvalidOperationException("Vendor returned no original road.");road.SetMeshCollider(true);road.SetTerrainDeformation(false,false);road.SnapToTerrain(false,false);road.SetResolution(.35f,false);road.Refresh();
    var component=road.GetType().GetField("roadScript").GetValue(road) as Component;if(component==null)throw new InvalidOperationException("Missing actual vendor road owner.");
    var colliders=component.GetComponentsInChildren<MeshCollider>(true).Where(c=>c.enabled&&!c.isTrigger&&c.sharedMesh!=null&&c.sharedMesh.vertexCount>2).ToArray();
    for(int i=0;i<colliders.Length;i++)
    {
     var source=colliders[i].sharedMesh;string path="Assets/TruckCamping/Models/"+name+" EasyRoads mesh "+i+".asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(existing==null)throw new InvalidOperationException("Missing known original mesh.");
     var row=new RoadMeshFacts{pass=pass,asset=path,vertices=source.vertexCount,submeshes=source.subMeshCount,boundsCenter=source.bounds.center,boundsSize=source.bounds.size,oldBoundsCenter=existing.bounds.center,oldBoundsSize=existing.bounds.size,sourcePosition=colliders[i].transform.position,sourceScale=colliders[i].transform.lossyScale};
     row.equivalent=SportsEasyRoadsAuthoring.EquivalentPreservedGeometry(source,existing,out row.reason);
     RecordPositionAxes(row,source.vertices,existing.vertices);
     Delta(source.vertices,existing.vertices,out row.positionDifferences,out row.maximumPositionDelta);Delta(source.normals,existing.normals,out row.normalDifferences,out row.maximumNormalDelta);
     Delta4(source.tangents,existing.tangents,out row.tangentDifferences,out row.maximumTangentDelta);Delta4(source.colors.Select(c=>(Vector4)c).ToArray(),existing.colors.Select(c=>(Vector4)c).ToArray(),out row.colorDifferences,out row.maximumColorDelta);
     row.indexLayoutExact=source.indexFormat==existing.indexFormat&&source.subMeshCount==existing.subMeshCount;for(int sub=0;sub<Math.Min(source.subMeshCount,existing.subMeshCount);sub++)row.indexLayoutExact&=source.GetTopology(sub)==existing.GetTopology(sub)&&source.GetIndices(sub).SequenceEqual(existing.GetIndices(sub));
     row.allUvExact=true;var a=new List<Vector4>();var b=new List<Vector4>();for(int channel=0;channel<8;channel++){a.Clear();b.Clear();source.GetUVs(channel,a);existing.GetUVs(channel,b);row.allUvExact&=a.SequenceEqual(b);}rows.Add(row);
    }
   }
   finally{UnityEngine.Object.DestroyImmediate(instance);}
  }
  static string FloatBits(Vector3 value)=>BitConverter.ToUInt32(BitConverter.GetBytes(value.x),0).ToString("X8")+"/"+BitConverter.ToUInt32(BitConverter.GetBytes(value.y),0).ToString("X8")+"/"+BitConverter.ToUInt32(BitConverter.GetBytes(value.z),0).ToString("X8");
  static void RecordPositionAxes(RoadMeshFacts row,Vector3[] source,Vector3[] original)
  {
   for(int i=0;i<Math.Min(source.Length,original.Length);i++)
   {
    var delta=source[i]-original[i];row.maximumPositionDeltaPerAxis=new Vector3(Mathf.Max(row.maximumPositionDeltaPerAxis.x,Mathf.Abs(delta.x)),Mathf.Max(row.maximumPositionDeltaPerAxis.y,Mathf.Abs(delta.y)),Mathf.Max(row.maximumPositionDeltaPerAxis.z,Mathf.Abs(delta.z)));
    if(!source[i].x.Equals(original[i].x))row.xDifferences++;if(!source[i].y.Equals(original[i].y))row.yDifferences++;if(!source[i].z.Equals(original[i].z))row.zDifferences++;
    bool horizontal=!source[i].x.Equals(original[i].x)||!source[i].z.Equals(original[i].z);
    if(horizontal&&row.firstHorizontalDifferentVertex<0){row.firstHorizontalDifferentVertex=i;row.firstHorizontalSourcePosition=source[i];row.firstHorizontalOriginalPosition=original[i];row.firstHorizontalSourceFloatBits=FloatBits(source[i]);row.firstHorizontalOriginalFloatBits=FloatBits(original[i]);}
    if(!source[i].Equals(original[i])&&row.firstDifferentVertex<0){row.firstDifferentVertex=i;row.firstSourcePosition=source[i];row.firstOriginalPosition=original[i];row.firstSourceFloatBits=FloatBits(source[i]);row.firstOriginalFloatBits=FloatBits(original[i]);}
   }
   string dir="Logs/TruckSports/Impossible07/CampingNavigationDiagnosis";Directory.CreateDirectory(dir);
   row.sourcePositionsFile=dir+"/axis-v2-"+row.pass+"-source-positions.float32le";row.originalPositionsFile=dir+"/axis-v2-"+row.pass+"-original-positions.float32le";
   WritePositions(row.sourcePositionsFile,source);WritePositions(row.originalPositionsFile,original);
  }
  static void WritePositions(string path,Vector3[] vertices)
  {
   if(File.Exists(path))throw new IOException("Preserve previous full position evidence: "+path);
   using(var writer=new BinaryWriter(File.Open(path,FileMode.CreateNew,FileAccess.Write)))foreach(var vertex in vertices){writer.Write(vertex.x);writer.Write(vertex.y);writer.Write(vertex.z);}
  }
  static void Delta(Vector3[] a,Vector3[] b,out int changed,out float max){changed=Math.Abs(a.Length-b.Length);max=0;for(int i=0;i<Math.Min(a.Length,b.Length);i++)if(!a[i].Equals(b[i])){changed++;max=Mathf.Max(max,Vector3.Distance(a[i],b[i]));}}
  static void Delta4(Vector4[] a,Vector4[] b,out int changed,out float max){changed=Math.Abs(a.Length-b.Length);max=0;for(int i=0;i<Math.Min(a.Length,b.Length);i++)if(!a[i].Equals(b[i])){changed++;max=Mathf.Max(max,Vector4.Distance(a[i],b[i]));}}
 }
}

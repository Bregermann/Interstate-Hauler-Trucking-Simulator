using System;using System.IO;using System.Linq;using UnityEngine;using UnityEditor;using EasyRoads3Dv3;
namespace LWS.TruckSports.Editor {
// EasyRoads owns course spline geometry/collision generation. Only its baked
// mesh result is retained at runtime; no Taxi road network or terrain is touched.
public static class SportsEasyRoadsAuthoring {
 public static Transform Bake(string folder,string name,Vector3[] markers,float width,Material material,PhysicsMaterial contact,bool preserveExistingMeshes=false){
  if(EditorApplication.isPlaying)throw new InvalidOperationException("Bake sports roads only in a stopped Editor");
  string destination="Assets/"+folder+"/Models";Directory.CreateDirectory(destination);AssetDatabase.Refresh();
  var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EasyRoads3D/Resources/ERRoadNetwork.prefab");if(prefab==null)throw new InvalidOperationException("Installed EasyRoads network prefab required");
  var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab);instance.name=name+" isolated authoring network";var result=new GameObject(name+" · EasyRoads generated course").transform;
  try{var owner=instance.GetComponent<ERModularBase>();owner.RoadNetworkInit();var network=new ERRoadNetwork(owner);var type=new ERRoadType{roadTypeName=name,roadWidth=width,hasMeshCollider=true,roadMaterial=material};
   var controls=Enumerable.Repeat(ERMarkerControlType.StraightXZ,markers.Length).ToArray();var road=network.CreateRoad(name,type,markers,controls);if(road==null)throw new InvalidOperationException("EasyRoads returned no road");road.SetMeshCollider(true);road.SetTerrainDeformation(false,false);road.SnapToTerrain(false,false);road.SetResolution(.35f,false);road.Refresh();
   var component=road.GetType().GetField("roadScript").GetValue(road) as Component;if(component==null)throw new InvalidOperationException("EasyRoads course owner missing");var colliders=component.GetComponentsInChildren<MeshCollider>(true).Where(c=>c.enabled&&!c.isTrigger&&c.sharedMesh!=null&&c.sharedMesh.vertexCount>2).ToArray();if(colliders.Length==0)throw new InvalidOperationException("EasyRoads generated no physical runway");
   for(int i=0;i<colliders.Length;i++){var source=colliders[i];var mesh=Persist(source.sharedMesh,destination+"/"+name+" EasyRoads mesh "+i+".asset",preserveExistingMeshes);var piece=new GameObject("EasyRoads physical surface "+i);piece.transform.SetPositionAndRotation(source.transform.position,source.transform.rotation);piece.transform.localScale=source.transform.lossyScale;piece.transform.SetParent(result,true);piece.AddComponent<MeshFilter>().sharedMesh=mesh;piece.AddComponent<MeshRenderer>().sharedMaterials=Enumerable.Repeat(material,mesh.subMeshCount).ToArray();var collision=piece.AddComponent<MeshCollider>();collision.sharedMesh=mesh;collision.sharedMaterial=contact;}
   Debug.Log("SPORTS_EASYROADS_BAKED "+name+" colliders="+colliders.Length+" markers="+markers.Length+" width="+width);return result;
  }catch{UnityEngine.Object.DestroyImmediate(result.gameObject);throw;}
  finally{UnityEngine.Object.DestroyImmediate(instance);}
 }
 static Mesh Persist(Mesh source,string path,bool preserveExisting){var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(mesh==null){mesh=UnityEngine.Object.Instantiate(source);AssetDatabase.CreateAsset(mesh,path);}else if(preserveExisting){if(!EquivalentPreservedGeometry(source,mesh,out var reason))throw new InvalidOperationException("Existing different EasyRoads mesh preserved: "+path+" · "+reason);}else{mesh.Clear();mesh.indexFormat=source.indexFormat;mesh.vertices=source.vertices;mesh.normals=source.normals;mesh.tangents=source.tangents;mesh.colors=source.colors;mesh.uv=source.uv;mesh.uv2=source.uv2;mesh.subMeshCount=source.subMeshCount;for(int i=0;i<source.subMeshCount;i++)mesh.SetIndices(source.GetIndices(i),source.GetTopology(i),i);mesh.RecalculateBounds();mesh.UploadMeshData(false);EditorUtility.SetDirty(mesh);}return mesh;}
 // Explicit partial-author recovery can retain the exact original asset bytes.
 // Every value which can affect its rendered or physical geometry is checked;
 // this never blesses a custom mesh merely because its bounds look similar.
 public static bool EquivalentPreservedGeometry(Mesh source,Mesh existing,out string reason){
  reason="";if(source==null||existing==null){reason="missing source or existing mesh";return false;}
  if(!source.isReadable||!existing.isReadable){reason="non-readable mesh cannot be verified";return false;}
  if(source.vertexCount!=existing.vertexCount||source.indexFormat!=existing.indexFormat||source.subMeshCount!=existing.subMeshCount){reason="vertex/index/submesh layout";return false;}
  if(!source.vertices.SequenceEqual(existing.vertices)||!source.normals.SequenceEqual(existing.normals)||!source.tangents.SequenceEqual(existing.tangents)||!source.colors.SequenceEqual(existing.colors)||!source.bounds.Equals(existing.bounds)){reason="positions/normals/tangents/colors/bounds";return false;}
  if(!source.bindposes.SequenceEqual(existing.bindposes)||!source.boneWeights.SequenceEqual(existing.boneWeights)||source.blendShapeCount!=0||existing.blendShapeCount!=0){reason="unsupported or changed deformation data";return false;}
  var a=new System.Collections.Generic.List<Vector4>();var b=new System.Collections.Generic.List<Vector4>();
  for(int channel=0;channel<8;channel++){a.Clear();b.Clear();source.GetUVs(channel,a);existing.GetUVs(channel,b);if(!a.SequenceEqual(b)){reason="UV channel "+channel;return false;}}
  for(int i=0;i<source.subMeshCount;i++)if(source.GetTopology(i)!=existing.GetTopology(i)||!source.GetIndices(i).SequenceEqual(existing.GetIndices(i))){reason="submesh "+i+" topology/indices";return false;}
  return true;
 }
}
}

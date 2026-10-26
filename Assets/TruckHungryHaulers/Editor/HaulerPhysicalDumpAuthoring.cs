using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace LWS.TruckHungryHaulers.Editor
{
 // Explicit saved-scene upgrade. Runtime never generates replacement holes.
 public static class HaulerPhysicalDumpAuthoring
 {
  const string Scene="Assets/TruckHungryHaulers/Scenes/TruckHungryHaulers.unity",Folder="Assets/TruckHungryHaulers/Meshes";
  public static void Install()
  {
   EditorSceneManager.OpenScene(Scene);var match=UnityEngine.Object.FindAnyObjectByType<HaulerMatch>();
   if(match.ballSupport!=null){Debug.Log("HAULER_PHYSICAL_DUMP_ALREADY_AUTHORED");return;}
   var court=GameObject.Find("Continuous round toy collection arena");var landscape=GameObject.Find("Finished carnival surrounding landscape");
   if(court==null||landscape==null)throw new InvalidOperationException("Preserve the original authored court and surrounding landscape.");
   var courtCollider=court.GetComponent<Collider>();var landscapeCollider=landscape.GetComponent<Collider>();string courtBefore=EditorJsonUtility.ToJson(courtCollider),terrainBefore=EditorJsonUtility.ToJson(landscapeCollider);
   foreach(var truck in match.trucks)foreach(var wheel in truck.GetComponentsInChildren<NWH.WheelController3D.WheelController>(true))if((wheel.layerMask.value&(1<<2))!=0)throw new InvalidOperationException("NWH wheel ground casts must exclude Ignore Raycast support; preserve their original Default ground.");
   if(!AssetDatabase.IsValidFolder(Folder))AssetDatabase.CreateFolder("Assets/TruckHungryHaulers","Meshes");
   var support=match.gameObject.AddComponent<HaulerBallSupport>();support.match=match;support.originalFloors=new[]{courtCollider,landscapeCollider};support.layouts=new GameObject[4];match.ballSupport=support;
   var courtMaterial=court.GetComponent<Renderer>().sharedMaterial;var physics=courtCollider.sharedMaterial;
   for(int players=1;players<=4;players++)
   {
    var centers=new Vector2[players];for(int i=0;i<players;i++){float angle=i*2*Mathf.PI/players;centers[i]=new Vector2(Mathf.Round(Mathf.Sin(angle)*74000)/1000,Mathf.Round(Mathf.Cos(angle)*74000)/1000);}
    var mesh=Save(CutFloor(centers),"Physical ball support "+players+" player layout");var root=new GameObject("Authored hole-cut ball court "+players+" players");root.layer=2;root.AddComponent<MeshFilter>().sharedMesh=mesh;root.AddComponent<MeshRenderer>().sharedMaterial=courtMaterial;var collider=root.AddComponent<MeshCollider>();collider.sharedMesh=mesh;collider.sharedMaterial=physics;support.layouts[players-1]=root;root.SetActive(players==match.setup.count);
   }
   court.GetComponent<Renderer>().enabled=false; // Original driving collider is untouched.
   var meadow=new GameObject("Authored meadow outside continuous truck court");meadow.layer=2;var meadowMesh=Save(Meadow(),"Visible meadow outside stadium");meadow.AddComponent<MeshFilter>().sharedMesh=meadowMesh;meadow.AddComponent<MeshRenderer>().sharedMaterial=landscape.GetComponent<Renderer>().sharedMaterial;var meadowContact=meadow.AddComponent<MeshCollider>();meadowContact.sharedMesh=meadowMesh;meadowContact.sharedMaterial=landscapeCollider.sharedMaterial;landscape.GetComponent<Renderer>().enabled=false;
   match.hoppers=new HaulerHopper[4];match.holes=new HaulerBankHole[4];var pitShapes=new List<Collider>{meadowContact};
   for(int i=0;i<4;i++)
   {
    match.hoppers[i]=Hopper(match.trucks[i].transform);
    var bank=match.banks[i];var paint=bank.GetComponentInChildren<Renderer>().sharedMaterial;
    foreach(Transform child in bank.Cast<Transform>().ToArray())UnityEngine.Object.DestroyImmediate(child.gameObject);
    var pit=new GameObject("Authored recessed ball-only scoring hole "+i).transform;pit.SetParent(bank,false);pit.rotation=Quaternion.identity;
    var steel=AssetDatabase.LoadAssetAtPath<Material>("Assets/TruckHungryHaulers/Materials/Worn real galvanized scoop steel.mat");
    foreach(int side in new[]{-1,1})
    {
     Box(pit,"Recessed ball-only pit side",new Vector3(side*5.95f,-2.25f,0),new Vector3(.3f,4.5f,12.2f),steel,physics);
     Box(pit,"Recessed ball-only pit end",new Vector3(0,-2.25f,side*5.95f),new Vector3(11.6f,4.5f,.3f),steel,physics);
     Box(pit,"Participant scoring hole lip",new Vector3(side*6.15f,.03f,0),new Vector3(.4f,.06f,12.7f),paint,physics);
     Box(pit,"Participant scoring hole lip",new Vector3(0,.03f,side*6.15f),new Vector3(11.9f,.06f,.4f),paint,physics);
    }
    Box(pit,"Recessed ball-only pit bottom",new Vector3(0,-4.6f,0),new Vector3(12.2f,.2f,12.2f),steel,physics);
    var collector=new GameObject("Actual recessed ball collection trigger");collector.layer=2;collector.transform.SetParent(pit,false);var volume=collector.AddComponent<BoxCollider>();volume.center=new Vector3(0,-2.5f,0);volume.size=new Vector3(11.2f,3,11.2f);volume.isTrigger=true;
    var hole=collector.AddComponent<HaulerBankHole>();hole.match=match;hole.slot=i;hole.pit=pit;hole.collection=volume;match.holes[i]=hole;pitShapes.AddRange(pit.GetComponentsInChildren<Collider>());
   }
   support.pitColliders=pitShapes.ToArray();
   if(courtBefore!=EditorJsonUtility.ToJson(courtCollider)||terrainBefore!=EditorJsonUtility.ToJson(landscapeCollider))throw new InvalidOperationException("Original truck floor/terrain collider changed unexpectedly.");
   EditorUtility.SetDirty(match);EditorUtility.SetDirty(support);EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());AssetDatabase.SaveAssets();Debug.Log("HAULER_PHYSICAL_DUMP_AUTHORED saved4layouts/pits/hoppers; original truck floor and terrain colliders unchanged; Ignore Raycast support excluded by existing NWH ground casts; runtime per-collider isolation only");
  }
  static HaulerHopper Hopper(Transform truck)
  {
   var bed=truck.Find("Visible open rear tractor cargo hopper");if(bed==null)throw new InvalidOperationException("Reuse the original modeled hopper.");var hopper=truck.gameObject.AddComponent<HaulerHopper>();hopper.bed=bed;
   var rear=bed.Cast<Transform>().Single(child=>child.name=="Open hopper raised end rail"&&child.localPosition.z<0);
   foreach(Transform child in bed)
   {
    if(child.name=="Modeled cargo hopper base"||child.name=="Open hopper raised side rail"){var scale=child.localScale;scale.z=7.4f;child.localScale=scale;}
    else if(child!=rear&&child.name=="Open hopper raised end rail"){var p=child.localPosition;p.z=3.7f;child.localPosition=p;}
   }
   var gate=new GameObject("Original modeled hopper rear gate hinge").transform;gate.SetParent(bed,false);gate.localPosition=new Vector3(-2.4f,.11f,-3.7f);rear.SetParent(gate,false);rear.localPosition=new Vector3(2.4f,1,0);hopper.gate=gate;
   hopper.storagePoints=new Vector3[6];for(int i=0;i<6;i++)hopper.storagePoints[i]=new Vector3(i%2==0?-1.15f:1.15f,.18f,-2.3f+i/2*2.3f);hopper.SetOpen(false,true);return hopper;
  }
  static void Box(Transform parent,string name,Vector3 position,Vector3 scale,Material material,PhysicsMaterial contact)
  {
   var box=GameObject.CreatePrimitive(PrimitiveType.Cube);box.name=name;box.layer=2;box.transform.SetParent(parent,false);box.transform.localPosition=position;box.transform.localScale=scale;box.GetComponent<Renderer>().sharedMaterial=material;box.GetComponent<Collider>().sharedMaterial=contact;
  }
  static Mesh Save(Mesh mesh,string name){mesh.name=name;string path=Folder+"/"+name+".asset";if(AssetDatabase.LoadAssetAtPath<Mesh>(path)!=null)throw new InvalidOperationException("Preserve existing authored mesh: "+path);AssetDatabase.CreateAsset(mesh,path);return mesh;}
  static Mesh CutFloor(Vector2[] holes)
  {
   var xs=new SortedSet<float>{-90,90};var zs=new SortedSet<float>{-90,90};foreach(var hole in holes){xs.Add(hole.x-5.8f);xs.Add(hole.x+5.8f);zs.Add(hole.y-5.8f);zs.Add(hole.y+5.8f);}var x=xs.ToArray();var z=zs.ToArray();var vertices=new List<Vector3>();var indices=new List<int>();
   for(int a=0;a<x.Length-1;a++)for(int b=0;b<z.Length-1;b++)
   {
    var middle=new Vector2((x[a]+x[a+1])*.5f,(z[b]+z[b+1])*.5f);if(holes.Any(h=>Mathf.Abs(h.x-middle.x)<5.8f&&Mathf.Abs(h.y-middle.y)<5.8f))continue;
    var polygon=new List<Vector2>{new Vector2(x[a],z[b]),new Vector2(x[a],z[b+1]),new Vector2(x[a+1],z[b+1]),new Vector2(x[a+1],z[b])};
    for(int edge=0;edge<128&&polygon.Count>0;edge++)
    {
     float angle=(edge+.5f)*2*Mathf.PI/128;var normal=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));float limit=90*Mathf.Cos(Mathf.PI/128);var clipped=new List<Vector2>();
     for(int i=0;i<polygon.Count;i++){var from=polygon[i];var to=polygon[(i+1)%polygon.Count];float f=Vector2.Dot(from,normal)-limit,t=Vector2.Dot(to,normal)-limit;if(f<=0)clipped.Add(from);if((f<=0)!=(t<=0))clipped.Add(Vector2.LerpUnclamped(from,to,f/(f-t)));}polygon=clipped;
    }
    int start=vertices.Count;foreach(var point in polygon)vertices.Add(new Vector3(point.x,0,point.y));for(int i=1;i<polygon.Count-1;i++){indices.Add(start);indices.Add(start+i);indices.Add(start+i+1);}
   }
   return Mesh(vertices,indices);
  }
  static Mesh Meadow()
  {
   var vertices=new List<Vector3>();var indices=new List<int>();for(int i=0;i<128;i++)
   {
    float a=i*2*Mathf.PI/128,b=(i+1)*2*Mathf.PI/128;var from=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));var to=new Vector3(Mathf.Cos(b),0,Mathf.Sin(b));int start=vertices.Count;
    vertices.Add(from*90-Vector3.up*1.5f);vertices.Add(from*(300/Mathf.Max(Mathf.Abs(from.x),Mathf.Abs(from.z)))-Vector3.up*1.5f);vertices.Add(to*(300/Mathf.Max(Mathf.Abs(to.x),Mathf.Abs(to.z)))-Vector3.up*1.5f);vertices.Add(to*90-Vector3.up*1.5f);
    indices.Add(start);indices.Add(start+3);indices.Add(start+1);indices.Add(start+1);indices.Add(start+3);indices.Add(start+2);
   }return Mesh(vertices,indices);
  }
  static Mesh Mesh(List<Vector3> vertices,List<int> indices){var mesh=new Mesh();mesh.SetVertices(vertices);mesh.SetTriangles(indices,0);mesh.SetUVs(0,vertices.Select(v=>new Vector2(v.x/180,v.z/180)).ToList());mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();return mesh;}
 }
}

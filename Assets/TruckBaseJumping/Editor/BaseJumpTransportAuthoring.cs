using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using LWS.TruckSports.Editor;
namespace LWS.TruckBaseJumping.Editor {
public static class BaseJumpTransportAuthoring {
 const string Source="Assets/Transport Helicopter/Transport_Chopper.fbx",Folder="TruckBaseJumping";
 struct Vertex {public Vector3 p,n;public Vector2 uv;public static Vertex Mix(Vertex a,Vertex b,float t)=>new Vertex{p=Vector3.Lerp(a.p,b.p,t),n=Vector3.Lerp(a.n,b.n,t).normalized,uv=Vector2.Lerp(a.uv,b.uv,t)};}
 static List<Vertex> Clip(List<Vertex> polygon,int axis,float plane,bool positive){var output=new List<Vertex>();if(polygon.Count==0)return output;var a=polygon[polygon.Count-1];float da=(a.p[axis]-plane)*(positive?1:-1);foreach(var b in polygon){float db=(b.p[axis]-plane)*(positive?1:-1);if((da>=0)!=(db>=0))output.Add(Vertex.Mix(a,b,da/(da-db)));if(db>=0)output.Add(b);a=b;da=db;}return output;}
 // Subtract the cargo opening from a copy; atlas UVs and original normals survive clipping.
 static Mesh OpenCargo(Mesh source){var positions=new List<Vector3>();var normals=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();var p=source.vertices;var n=source.normals;var tex=source.uv;var indices=source.triangles;int[] axes={0,0,1,1,2};float[] planes={-240,240,-550,-100,-350};bool[] insidePositive={true,false,true,false,false};
  Action<List<Vertex>> emit=poly=>{for(int k=1;k+1<poly.Count;k++)foreach(var v in new[]{poly[0],poly[k],poly[k+1]}){triangles.Add(positions.Count);positions.Add(v.p);normals.Add(v.n);uv.Add(v.uv);}};
  for(int i=0;i<indices.Length;i+=3){var remain=new List<Vertex>();for(int k=0;k<3;k++){int j=indices[i+k];remain.Add(new Vertex{p=p[j],n=n[j],uv=tex[j]});}for(int cut=0;cut<axes.Length&&remain.Count>0;cut++){emit(Clip(remain,axes[cut],planes[cut],!insidePositive[cut]));remain=Clip(remain,axes[cut],planes[cut],insidePositive[cut]);}}
  var mesh=new Mesh{name="Transport Helicopter — open cargo body (owned derivative)"};mesh.SetVertices(positions);mesh.SetNormals(normals);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();mesh.RecalculateTangents();return mesh;
 }
 static Transform Marker(Transform parent,string name,Vector3 world){var t=new GameObject(name).transform;t.SetParent(parent);t.position=world;return t;}
 public static void Upgrade(){EditorSceneManager.OpenScene(BaseJumpAuthoring.Scene);var m=UnityEngine.Object.FindAnyObjectByType<BaseJumpMatch>();Install(m);EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());AssetDatabase.SaveAssets();Validate();}
 public static void Install(BaseJumpMatch m){
  var source=AssetDatabase.LoadAssetAtPath<GameObject>(Source);if(source==null)throw new FileNotFoundException("Imported Transport Helicopter3911 is required",Source);
  if(m.helicopter!=null)UnityEngine.Object.DestroyImmediate(m.helicopter.gameObject);foreach(var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))if(t!=null&&(t.name=="Cargo floor"||t.name=="Open rear ramp"||t.name=="Cargo ramp edge stripe"||t.name=="Cargo inner rail"))UnityEngine.Object.DestroyImmediate(t.gameObject);
  var holder=new GameObject("Transport Helicopter 3911 · Sports cargo variant");m.helicopter=holder.transform;holder.transform.position=new Vector3(0,269.55f,-21);holder.transform.localScale=Vector3.one*2;
  var model=(GameObject)PrefabUtility.InstantiatePrefab(source);model.transform.SetParent(holder.transform,false);model.transform.localPosition=Vector3.zero;model.transform.localRotation=Quaternion.identity;
  var filter=model.GetComponent<MeshFilter>();if(filter==null)filter=model.GetComponentsInChildren<MeshFilter>().First(f=>f.sharedMesh.name=="Body");
  Directory.CreateDirectory("Assets/TruckBaseJumping/Art");AssetDatabase.Refresh();var mesh=OpenCargo(filter.sharedMesh);const string path="Assets/TruckBaseJumping/Art/Transport open cargo body.asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(existing==null){AssetDatabase.CreateAsset(mesh,path);existing=mesh;}else{EditorUtility.CopySerialized(mesh,existing);UnityEngine.Object.DestroyImmediate(mesh);}filter.sharedMesh=existing;
  var material=SportsEventAuthoring.Mat(Folder,"Transport helicopter URP atlas",Color.white,.32f);material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Transport Helicopter/Textures/1K_Helicopter_TXTR.tif"));material.SetInt("_Cull",0);EditorUtility.SetDirty(material);foreach(var r in model.GetComponentsInChildren<Renderer>(true))r.sharedMaterial=material;
  var animation=model.GetComponent<Animation>();if(animation!=null)animation.enabled=false;var mc=filter.gameObject.AddComponent<MeshCollider>();mc.sharedMesh=existing;
  var interior=SportsEventAuthoring.Mat(Folder,"Cargo interior",new Color(.19f,.22f,.19f),.35f);var stripe=SportsEventAuthoring.Mat(Folder,"Cargo ramp safety",new Color(.9f,.64f,.1f),.3f);var contact=SportsEventAuthoring.Contact(Folder,"Cargo ramp contact",.6f,.02f);
  // Coordinates are in world space; keep truck-scale staging independent from imported FBX centimetres.
  SportsEventAuthoring.Box("Cargo floor",new Vector3(0,259.55f,-25),new Vector3(9.4f,.25f,28),interior,contact);
  SportsEventAuthoring.Box("Open rear ramp",new Vector3(0,259.55f,-48.5f),new Vector3(9.4f,.25f,19),interior,contact);
  for(int sign=-1;sign<=1;sign+=2){SportsEventAuthoring.Box("Cargo ramp edge stripe",new Vector3(sign*4.5f,259.69f,-48.5f),new Vector3(.13f,.025f,19),stripe);SportsEventAuthoring.Box("Cargo inner rail",new Vector3(sign*4.6f,260.1f,-30),new Vector3(.12f,.9f,24),interior,contact);}
  var rig=m.GetComponent<BaseJumpTransport>();if(rig==null)rig=m.gameObject.AddComponent<BaseJumpTransport>();m.transport=rig;rig.model=model;rig.rotorClip=AssetDatabase.LoadAllAssetsAtPath(Source).OfType<AnimationClip>().First(c=>c.name=="Fly");rig.staging=new Transform[4];for(int i=0;i<4;i++)rig.staging[i]=Marker(holder.transform,"Cargo staging "+i,new Vector3(i%2==0?-2.2f:2.2f,260,i<2?-26:-19));rig.doorway=Marker(holder.transform,"Open hatch alignment",new Vector3(0,260,-42));rig.exit=Marker(holder.transform,"Equal flight exit plane",new Vector3(0,260,-60));if(m.jumpers!=null)foreach(var j in m.jumpers)if(j!=null){j.truck.transform.SetPositionAndRotation(rig.Stage(j.slot),Quaternion.Euler(0,180,0));j.truck.body.position=rig.Stage(j.slot);j.truck.body.rotation=Quaternion.Euler(0,180,0);}m.mainRotor=m.tailRotor=null;EditorUtility.SetDirty(m);EditorUtility.SetDirty(rig);
 }
 public static void Validate(){var m=UnityEngine.Object.FindAnyObjectByType<BaseJumpMatch>();if(m==null||m.transport==null||m.transport.rotorClip==null||m.transport.staging.Length!=4)throw new Exception("Transport cargo references missing");var rig=m.transport;if(rig.model.GetComponentsInChildren<MeshFilter>().Length!=3)throw new Exception("Use actual three-mesh Transport Helicopter");foreach(var p in rig.staging)if(p.position.z<=rig.doorway.position.z||Mathf.Abs(p.position.y-260)>.01f)throw new Exception("Cargo staging must be inside and level");Debug.Log("TRANSPORT_HELICOPTER_SAVED_VALIDATED asset3911 native Fly clip, owned cargo aperture, four staging positions");}
}
}

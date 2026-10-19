using System.Linq;
using UnityEngine;
using UnityEditor;
using LWS.TruckSports.Editor;
namespace LWS.TruckTerrierRacing.Editor {
public static class TerrierRouteAuthoring {
 public static void Install(TerrierMatch m){var c=m.course;if(c.geometry==null)c.geometry=new GameObject("Diagonal triangular Terrier course").transform;var route=c.geometry;route.SetPositionAndRotation(new Vector3(-35,5,0),Quaternion.Euler(1.5f,25,0));
 foreach(var name in new[]{"Meadow surroundings","Continuous racing course","Terrier course installed scenery"}){var g=GameObject.Find(name);if(g!=null&&g.transform.parent!=route)g.transform.SetParent(route,false);}foreach(var t in c.hurdles.Concat(c.ramps).Concat(c.patches).Concat(c.gates).Concat(new[]{c.finish}))if(t.parent!=route)t.SetParent(route,false);
 foreach(var g in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include).Where(t=>t.name=="Continuous outside rail"||t.name=="Course scale reference"||t.name.StartsWith("Stall divider")).ToArray())UnityEngine.Object.DestroyImmediate(g.gameObject);
 var ivory=AssetDatabase.LoadAssetAtPath<Material>("Assets/TruckTerrierRacing/Materials/Racing ivory.mat");var contact=AssetDatabase.LoadAssetAtPath<PhysicsMaterial>("Assets/TruckTerrierRacing/Materials/Barrier contact.asset");c.sideRails=new Transform[6];var rails=route.Find("Tapering course rails");if(rails!=null)UnityEngine.Object.DestroyImmediate(rails.gameObject);rails=new GameObject("Tapering course rails").transform;rails.SetParent(route,false);for(int i=0;i<6;i++)c.sideRails[i]=SportsEventAuthoring.Box("Funnel side board "+i,Vector3.zero,Vector3.one,ivory,contact,rails).transform;
 for(int i=0;i<12;i++){SportsEventAuthoring.Box("Stall divider "+i,new Vector3((i-5.5f)*5.2f-2.6f,.8f,0),new Vector3(.1f,1.6f,6),ivory,contact,route);var label=GameObject.Find("Starting stall "+i);if(label!=null){label.transform.SetParent(route,false);label.transform.localPosition=new Vector3((i-5.5f)*5.2f,.025f,-2);label.transform.localRotation=Quaternion.Euler(90,0,0);}}
 var floor=route.Find("Continuous racing dirt course")??route.Find("Continuous racing course");floor.name="Continuous racing dirt course";c.groundMesh=floor.GetComponent<MeshFilter>();var oldBox=floor.GetComponent<BoxCollider>();if(oldBox!=null)UnityEngine.Object.DestroyImmediate(oldBox);c.groundCollider=floor.GetComponent<MeshCollider>();if(c.groundCollider==null)c.groundCollider=floor.gameObject.AddComponent<MeshCollider>();c.groundCollider.sharedMaterial=AssetDatabase.LoadAssetAtPath<PhysicsMaterial>("Assets/TruckTerrierRacing/Materials/Track contact.asset");floor.localPosition=Vector3.zero;floor.localRotation=Quaternion.identity;floor.localScale=Vector3.one;string path="Assets/TruckTerrierRacing/Materials/Triangular course collision.asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(mesh==null){mesh=new Mesh{name="Triangular course and finish runout"};AssetDatabase.CreateAsset(mesh,path);}c.groundMesh.sharedMesh=mesh;
 var scenery=route.Find("Terrier course installed scenery");if(scenery!=null){int i=0;foreach(Transform tree in scenery){float z=i/2*10-15;tree.localPosition=new Vector3((i%2==0?-1:1)*(c.HalfWidth(z)+10+i%5*4),0,z);i++;}}
 c.Generate(4757,1,false);EditorUtility.SetDirty(mesh);EditorUtility.SetDirty(c);
 }
}
}

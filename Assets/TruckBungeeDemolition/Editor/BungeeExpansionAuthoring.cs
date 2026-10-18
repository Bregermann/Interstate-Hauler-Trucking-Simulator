using System;using System.Linq;using System.Reflection;using UnityEngine;using UnityEditor;using UnityEditor.Events;using Assets.CoreCollapse.Scripts.Structural;using LWS.TruckSports;using LWS.TruckSports.Editor;
namespace LWS.TruckBungeeDemolition.Editor {
// Installed Core-Collapse geometry/health/support remain authoritative; this is venue authoring.
public static class BungeeExpansionAuthoring {
 const string Path="Assets/TruckBungeeDemolition/Prefabs/Ten Additional Reachable Buildings.prefab";
 public static void Install(BungeeMatch m){if(m.bays.All(b=>b.expansionPrefab!=null))return;var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Path)??Create();foreach(var b in m.bays){b.expansionPrefab=prefab;EditorUtility.SetDirty(b);}EditorUtility.SetDirty(m);}
 public static void Rebuild(BungeeMatch m){var prefab=Create();foreach(var b in m.bays){b.expansionPrefab=prefab;EditorUtility.SetDirty(b);}EditorUtility.SetDirty(m);}
 static GameObject Create(){
  var type=typeof(BungeeAuthoring);var flags=BindingFlags.Static|BindingFlags.NonPublic;
  var concrete=AssetDatabase.LoadAssetAtPath<Material>("Assets/TruckBungeeDemolition/Materials/Pale reinforced concrete.mat");var steel=AssetDatabase.LoadAssetAtPath<Material>("Assets/TruckBungeeDemolition/Materials/Industrial crane enamel.mat");var glass=AssetDatabase.LoadAssetAtPath<Material>("Assets/TruckBungeeDemolition/Materials/Architectural blue glazing.mat");var contact=AssetDatabase.LoadAssetAtPath<PhysicsMaterial>("Assets/TruckBungeeDemolition/Materials/Demolition concrete contact.asset");
  type.GetField("concrete",flags).SetValue(null,concrete);type.GetField("steel",flags).SetValue(null,steel);type.GetField("glass",flags).SetValue(null,glass);type.GetField("contact",flags).SetValue(null,contact);type.GetField("wall",flags).SetValue(null,AssetDatabase.LoadAssetAtPath<GameObject>("Assets/CoreCollapse/Demo/Prefabs/PreFrag/Wall.prefab"));
  var fragment=type.GetMethod("Fragments",flags);var module=type.GetMethod("Module",flags);var root=new GameObject("Ten additional destructible buildings");
  for(int i=0;i<10;i++){
   var building=new GameObject("Additional reachable building "+(i+1)).transform;building.SetParent(root.transform,false);float[] headings={-35,-10,15,40,90,115,140,165,190,215};float angle=headings[i]*Mathf.Deg2Rad,radius=32+i%2*3;building.localPosition=new Vector3(Mathf.Cos(angle)*radius,0,-12+Mathf.Sin(angle)*radius);building.localRotation=Quaternion.Euler(0,i*17,0);
   int floors=3+i%2;float height=12;float width=6+i%3;Support below=null;
   var size=new Vector3(width,height,width*.85f);var debris=(GameObject)fragment.Invoke(null,new object[]{"Annex fracture "+i,size});
   for(int level=0;level<floors;level++){
    var part=(BungeeModule)module.Invoke(null,new object[]{building,"Building "+(i+1)+" structural storey "+level,new Vector3(0,height*(level+.5f),0),size,debris,55f+i*5+level*8,25+level*5,level});
    var support=part.GetComponent<Support>();var so=new SerializedObject(support);so.FindProperty("supportCapacity").floatValue=4;so.FindProperty("supportWeight").floatValue=3;var links=so.FindProperty("supportedBy");links.arraySize=below==null?0:1;if(below!=null)links.GetArrayElementAtIndex(0).objectReferenceValue=below;so.ApplyModifiedPropertiesWithoutUndo();
    var field=typeof(Support).GetField("onCollapse",BindingFlags.Instance|BindingFlags.NonPublic);var ev=(Support.OnCollapseEvent)field.GetValue(support)??new Support.OnCollapseEvent();field.SetValue(support,ev);UnityEventTools.AddPersistentListener(ev,part.Collapsed);below=support;
    for(int face=-1;face<=1;face+=2)for(int pane=0;pane<3;pane++){SportsEventAuthoring.Box("Recessed industrial glazing",new Vector3((pane-1)*width*.27f,height*.12f,face*(size.z*.5f+.012f)),new Vector3(width*.21f,height*.48f,.04f),glass,null,part.transform);SportsEventAuthoring.Box("Architectural concrete sill",new Vector3((pane-1)*width*.27f,-height*.18f,face*(size.z*.5f+.02f)),new Vector3(width*.24f,.18f,.12f),concrete,null,part.transform);}
    EditorUtility.SetDirty(support);
   }
  }
  var result=PrefabUtility.SaveAsPrefabAsset(root,Path);UnityEngine.Object.DestroyImmediate(root);return result;
 }
 public static void Validate(BungeeMatch m){foreach(var b in m.bays){if(b.expansionPrefab==null||b.expansionPrefab.transform.childCount!=10)throw new Exception("Exactly ten additional buildings required in each equivalent bay");for(int i=0;i<10;i++){var building=b.expansionPrefab.transform.GetChild(i);Bounds bounds=new Bounds();bool first=true;foreach(var collider in building.GetComponentsInChildren<BoxCollider>()){var world=b.transform.TransformPoint(collider.transform.position);var size=Vector3.Scale(collider.size,collider.transform.lossyScale);var box=new Bounds(world,size);if(first){bounds=box;first=false;}else bounds.Encapsulate(box);}if(Vector3.Distance(bounds.ClosestPoint(b.craneAnchor.position),b.craneAnchor.position)>b.restLength+6)throw new Exception("Unreachable added building "+i);}}}
}
}

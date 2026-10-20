using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using LWS.TruckSports.Editor;
namespace LWS.TruckDockDiving.Editor {
// Additive park dressing outside the established pool/run-up envelope.
// Reuses installed models without changing course collision or scoring.
public static class DockVenueDressing {
 public static void Upgrade(){var scene=EditorSceneManager.OpenScene(DockAuthoring.Scene);Install(Object.FindAnyObjectByType<DockMatch>());EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();}
 public static void Install(DockMatch match){
  if(match.transform.Find("Lakeside competition park")!=null)return;
  var root=new GameObject("Lakeside competition park").transform;root.SetParent(match.transform,false);
  for(int side=-1;side<=1;side+=2){
   for(int i=0;i<13;i++){
    float x=side*(31+(i%3)*5),z=-25+i*14;
    Model(root,"Park shade tree",i%2==0?"Assets/Toon Gas Station/Prefabs/Tree_6A.prefab":"Assets/Toon Gas Station/Prefabs/Tree_7A.prefab",new Vector3(x,-.15f,z),new Vector3(12,13+i%3*2,12),i*137);
    if(i%3==0)Model(root,"Lakeside rock garden","Assets/Toon Gas Station/Prefabs/Rock_Cluster_1A.prefab",new Vector3(side*43,-.15f,z+5),new Vector3(8,3,6),i*73);
   }
   for(int i=0;i<5;i++)Model(root,"Spectator walkway lamp","Assets/Toon Gas Station/Prefabs/Streetlight_1C.prefab",new Vector3(side*26,-.15f,6+i*27),new Vector3(3,8,3),side<0?90:-90);
  }
  Model(root,"Park competition clubhouse","Assets/JC_LP_MegaCity/Models/Buildings/SM_Buildings_Commercial_03.fbx",new Vector3(42,-.15f,143),new Vector3(26,16,24),-90);
  var texture=AssetDatabase.LoadAssetAtPath<Texture>("Assets/NWH/Common/Textures/Ground/GrassAlbedo.png");
  var grass=SportsEventAuthoring.Mat("TruckDockDiving","Textured competition lawn",new Color(.45f,.64f,.39f),.08f);grass.mainTexture=texture;
  // Each existing lawn slab has UVs covering its full face. Match a ~5m tile
  // separately for the wide sides and compact ends, using owned materials.
  foreach(var renderer in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude)){
   if(!renderer.name.StartsWith("Venue ")||!renderer.name.EndsWith("ground"))continue;
   string path="Assets/TruckDockDiving/Materials/"+renderer.name+" lawn.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);if(material==null){material=new Material(grass);AssetDatabase.CreateAsset(material,path);}
   material.mainTexture=texture;material.color=grass.color;material.mainTextureScale=new Vector2(renderer.bounds.size.x/5,renderer.bounds.size.z/5);renderer.sharedMaterial=material;EditorUtility.SetDirty(material);
  }
  EditorUtility.SetDirty(match);
 }
 static void Model(Transform parent,string name,string path,Vector3 position,Vector3 size,float yaw){var anchor=new GameObject(name).transform;anchor.SetParent(parent,false);anchor.SetPositionAndRotation(position,Quaternion.Euler(0,yaw,0));LWS.TruckKeepaway.Editor.KeepawayPresentationAuthoring.Fit(anchor,path,size,0,false,"TruckDockDiving");}
}
}

using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using TMPro;
using LWS.TruckSports;

namespace LWS.TruckRedLightGreenLight.Editor
{
 public static class RedLightCarnivalAuthoring
 {
  const string Root="Carnival ground shot and two-aspect world signals";
  const string ScenePath="Assets/TruckRedLightGreenLight/Scenes/TruckRedLightGreenLight.unity";
  [MenuItem("Truck Sports/Red Light Green Light/Install Carnival ground shot and world signals in open scene")]
  public static void InstallOpenScene()
  {
   var match=UnityEngine.Object.FindAnyObjectByType<RedLightMatch>();if(match==null||match.gameObject.scene.path!=ScenePath)throw new InvalidOperationException("Open the existing Red Light Green Light scene first. This targeted entry never opens or saves scenes.");Install(match,true);
  }
  static Material Material(string name)=>AssetDatabase.LoadAssetAtPath<Material>("Assets/TruckRedLightGreenLight/Materials/"+name+".mat")??throw new InvalidOperationException("Existing owned material absent: "+name);
  static GameObject Shape(PrimitiveType type,string name,Transform parent,Vector3 position,Vector3 scale,Material material,bool collision=false)
  {
   var go=GameObject.CreatePrimitive(type);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localScale=scale;go.GetComponent<Renderer>().sharedMaterial=material;if(!collision)UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());return go;
  }
  // Also called by native tests with undo=false. Uses existing mesh/material/font
  // references only: no asset creation, scene opening, scene saving or global edit.
  public static RedLightCarnivalVenue Install(RedLightMatch match,bool undo)
  {
   var existing=match.GetComponent<RedLightCarnivalVenue>();if(existing!=null)return existing;
   var fixtures=match.gameObject.scene.GetRootGameObjects().SelectMany(go=>go.GetComponentsInChildren<Transform>(true)).Where(t=>t.name=="Owned giant traffic light fixture").ToArray();if(fixtures.Length<2)throw new InvalidOperationException("Existing owned UTS traffic-light foundations are required.");
   if(match.broadcast.broadcastFraming==null)throw new InvalidOperationException("Existing Red Light Cinemachine broadcast framing is required.");
   var metal=Material("PA tower navy steel");var lens=Material("Emissive traffic signal");var font=Resources.Load<SportsBroadcastTheme>("SportsBroadcastTheme").headingFont;
   var root=new GameObject(Root);root.transform.SetParent(match.transform,false);if(undo){Undo.RegisterCreatedObjectUndo(root,"Install Carnival Red Light presentation");Undo.RecordObject(match.broadcast.broadcastFraming,"Mode-only ground shot settings");}
   var venue=undo?Undo.AddComponent<RedLightCarnivalVenue>(match.gameObject):match.gameObject.AddComponent<RedLightCarnivalVenue>();venue.match=match;venue.redLenses=new Renderer[8];venue.greenLenses=new Renderer[8];venue.warnings=new TMP_Text[8];venue.supersededFixtures=fixtures.Select(t=>t.gameObject).ToArray();venue.supersededSignals=match.signals;
   for(int bank=0;bank<4;bank++)for(int side=0;side<2;side++)
   {
    int index=bank*2+side;var tower=new GameObject("Enormous red-green traffic lamp "+(index+1)).transform;tower.SetParent(root.transform,false);tower.localPosition=new Vector3(side==0?-52:52,0,new[]{40f,135f,230f,match.finishZ+12}[bank]);
    var foundation=UnityEngine.Object.Instantiate(fixtures[side].gameObject,tower);foundation.name="Existing UTS mast · owned two-aspect configuration";foundation.SetActive(true);foundation.transform.localPosition=Vector3.zero;foundation.transform.localRotation=Quaternion.identity;
    // Cover every original lamp aspect with dark owned material. There is no
    // active, decorative or accidentally emissive yellow lens in this variant.
    foreach(var renderer in foundation.GetComponentsInChildren<Renderer>(true))renderer.sharedMaterials=renderer.sharedMaterials.Select(_=>metal).ToArray();
    Shape(PrimitiveType.Cube,"Physical two-aspect signal housing",tower,new Vector3(0,16,-1.5f),new Vector3(9,17,3),metal,true);
    venue.redLenses[index]=Shape(PrimitiveType.Sphere,"RED aspect only",tower,new Vector3(0,20,-3.2f),new Vector3(5.5f,5.5f,.8f),lens).GetComponent<Renderer>();venue.greenLenses[index]=Shape(PrimitiveType.Sphere,"GREEN aspect only",tower,new Vector3(0,12,-3.2f),new Vector3(5.5f,5.5f,.8f),lens).GetComponent<Renderer>();
    Shape(PrimitiveType.Cube,"In-world laugh warning contrast board",tower,new Vector3(0,28,-2.9f),new Vector3(30,8,.3f),metal);
    var warning=new GameObject("Laugh warning · world text only").AddComponent<TextMeshPro>();warning.transform.SetParent(tower,false);warning.transform.localPosition=new Vector3(0,28,-3.15f);warning.rectTransform.sizeDelta=new Vector2(29,7.5f);warning.font=font;warning.fontSize=10;warning.alignment=TextAlignmentOptions.Center;warning.color=Color.white;warning.text="THE LAUGH!\nBRAKE NOW!";warning.gameObject.SetActive(false);venue.warnings[index]=warning;
   }
   foreach(var fixture in fixtures){if(undo)Undo.RecordObject(fixture.gameObject,"Replace superseded traffic aspects");fixture.gameObject.SetActive(false);}foreach(var signal in match.signals){if(undo)Undo.RecordObject(signal,"Replace superseded signal lens");signal.enabled=false;}
   var camera=match.broadcast.rig.GetComponent<RedLightGroundCamera>();if(camera==null)camera=undo?Undo.AddComponent<RedLightGroundCamera>(match.broadcast.rig.gameObject):match.broadcast.rig.gameObject.AddComponent<RedLightGroundCamera>();
   var ground=match.gameObject.scene.GetRootGameObjects().SelectMany(go=>go.GetComponentsInChildren<Collider>(true)).Where(c=>c is TerrainCollider||c.name=="Continuous competition runway"||c.name=="Substantial surrounding park").ToArray();if(ground.Length==0)throw new InvalidOperationException("Explicit saved static ground surfaces missing.");camera.Configure(match,ground);venue.groundCamera=camera;
   if(undo){EditorUtility.SetDirty(venue);EditorUtility.SetDirty(camera);EditorUtility.SetDirty(match.broadcast.broadcastFraming);UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(match.gameObject.scene);}return venue;
  }
 }
}

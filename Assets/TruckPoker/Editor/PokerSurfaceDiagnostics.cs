using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using LWS.TruckSports;
namespace LWS.TruckPoker.Editor {
// Explicit same-camera A/B fixture; temporary materials are restored and
// destroyed. Never saves a diagnostic state into the authored restaurant.
public static class PokerSurfaceDiagnostics {
 public static void CaptureNormalComparison(){
  if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Stop the active probe first.");
  EditorSceneManager.OpenScene(PokerAuthoring.Scene);var match=Object.FindAnyObjectByType<PokerMatch>();var canvas=match.menus.GetComponent<Canvas>();var camera=match.broadcast.view;
  bool enabled=camera.enabled,ui=canvas.gameObject.activeSelf;var position=camera.transform.position;var rotation=camera.transform.rotation;var saved=new Dictionary<Renderer,Material[]>();var copies=new Dictionary<Material,Material>();
  try{canvas.gameObject.SetActive(false);camera.enabled=true;camera.transform.position=new Vector3(20,15,-26);camera.transform.LookAt(new Vector3(0,1,0));
   SportsControlledCapture.Save(canvas,new[]{camera},1280,720,"Logs/TruckSports/PresentationSprint/PokerSurfaces/baseline.png");
   var root=match.transform.Find("Modeled midnight casino interior");foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>(true)){var originals=renderer.sharedMaterials;saved[renderer]=originals;var replaced=new Material[originals.Length];for(int i=0;i<originals.Length;i++){if(originals[i]==null)continue;if(!copies.TryGetValue(originals[i],out var copy)){copy=new Material(originals[i]);copy.DisableKeyword("_NORMALMAP");copy.SetFloat("_BumpScale",0);copies.Add(originals[i],copy);}replaced[i]=copy;}renderer.sharedMaterials=replaced;}
   SportsControlledCapture.Save(canvas,new[]{camera},1280,720,"Logs/TruckSports/PresentationSprint/PokerSurfaces/normal-maps-off.png");
  }finally{foreach(var pair in saved)pair.Key.sharedMaterials=pair.Value;foreach(var copy in copies.Values)Object.DestroyImmediate(copy);canvas.gameObject.SetActive(ui);camera.enabled=enabled;camera.transform.SetPositionAndRotation(position,rotation);}
 }
}
}

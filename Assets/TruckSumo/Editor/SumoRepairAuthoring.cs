using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LWS.TruckSumo.Editor {
// Keep hand-edited venue, truck/outfit references and input layout intact.
public static class SumoRepairAuthoring {
 // Called by the existing project-owned Editor jobs after import/compilation.
 // Session flags survive reload; no slow scene operation runs inside an HTTP eval.
 public static void Acceptance(string mode){
  if(EditorApplication.isPlaying)throw new System.InvalidOperationException("An acceptance run is already active.");
  EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
  SessionState.SetBool("LWS.SumoFocused",false);SessionState.SetBool("LWS.SumoProbe",false);SessionState.SetBool("Sumo.RepairProbe",false);
  if(mode=="focused")SessionState.SetBool("LWS.SumoFocused",true);
  else if(mode=="easy"||mode=="normal"||mode=="hard"){
   SessionState.SetInt("LWS.SumoDifficulty",mode=="easy"?0:mode=="normal"?1:2);SessionState.SetBool("LWS.SumoProbe",true);
  }else if(mode=="contact"||mode=="physical"||mode=="shove"){
   SessionState.SetString("Sumo.RepairMode",mode);SessionState.SetBool("Sumo.RepairProbe",true);
  }else throw new System.ArgumentException("Unknown explicit Sumo acceptance mode: "+mode);
  EditorApplication.isPlaying=true;
 }
 [MenuItem("Truck Sports/Sumo/Refresh saved scoring help (preserve venue)")]
 public static void RefreshHelp(){
  if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Exit Play Mode before saving Sumo help.");
  var scene=EditorSceneManager.OpenScene(SumoAuthoring.Scene);var match=Object.FindAnyObjectByType<SumoMatch>();
  if(match==null)throw new System.InvalidOperationException("Saved Sumo match is missing.");
  var text=match.menus.pages[4].transform.Find("Rules and controls").GetComponent<TMPro.TMP_Text>();
  text.text=match.Help;EditorUtility.SetDirty(text);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
 }
}
}

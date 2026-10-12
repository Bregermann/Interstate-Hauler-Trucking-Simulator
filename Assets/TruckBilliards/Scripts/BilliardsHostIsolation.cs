using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using LWS.InterstateHauler;
namespace LWS.TruckBilliards {
// Host development HUD auto-spawns in every development player. Suppress its
// UI/input only while this independent scene is active; preserve host source.
[DefaultExecutionOrder(-500)]
public sealed class BilliardsHostIsolation:MonoBehaviour {
 readonly List<GameObject> suppressed=new List<GameObject>();
 public EventSystem modeEventSystem;
 void Start(){
  foreach(var ui in UnityEngine.Object.FindObjectsByType<LwsDevelopmentUiRoot>(FindObjectsInactive.Include)){
   if(ui.gameObject.activeSelf){suppressed.Add(ui.gameObject);ui.gameObject.SetActive(false);}
  }
  if(modeEventSystem!=null){modeEventSystem.gameObject.SetActive(true);modeEventSystem.enabled=true;EventSystem.current=modeEventSystem;}
  Debug.Log("TB_HOST_HUD_SUPPRESSED "+suppressed.Count);
 }
 void OnDestroy(){foreach(var root in suppressed)if(root!=null)root.SetActive(true);}
}
}

using UnityEngine;
namespace LWS.TruckSubmarine {
 // Original articulated mantle/arms: no appropriate installed squid exists.
 // Wildlife presentation has no colliders, score or vehicle-force authority.
 public sealed class SubmarineSquid:MonoBehaviour {
  public SubmarineMatch match;public Transform model;public Transform[] armJoints;public Vector3 encounterCenter=new Vector3(65,-105,110);[Range(0,1)]public float encounterChance=.125f;
  public bool Encountered{get;private set;}public int AnimatedFrames{get;private set;}bool eligible;Quaternion[] rest;
  public void ResetEncounter(string matchId){uint hash=2166136261;foreach(char c in matchId)hash=(hash^c)*16777619;eligible=(hash%100000)/100000f<encounterChance;Encountered=false;AnimatedFrames=0;if(rest==null){rest=new Quaternion[armJoints.Length];for(int i=0;i<rest.Length;i++)rest[i]=armJoints[i].localRotation;}for(int i=0;i<rest.Length;i++)armJoints[i].localRotation=rest[i];model.gameObject.SetActive(false);}
  public void Tick(float dt){if(!match.Live||match.rules.Phase!=SubmarinePhase.Diving||!eligible)return;if(!Encountered){if(match.rules.Clock<35)return;for(int i=0;i<match.setup.count;i++){var p=match.divers[i].truck.body.position;if(p.y< -65&&(p-encounterCenter).sqrMagnitude<125*125){Encountered=true;model.gameObject.SetActive(true);break;}}}if(!Encountered)return;float t=match.rules.Clock;model.position=encounterCenter+new Vector3(Mathf.Sin(t*.065f)*28,Mathf.Sin(t*.13f)*8,Mathf.Cos(t*.065f)*22);model.rotation=Quaternion.Euler(0,t*3.7f+90,Mathf.Sin(t*.2f)*5);for(int i=0;i<armJoints.Length;i++)armJoints[i].localRotation=rest[i]*Quaternion.Euler(Mathf.Sin(t*1.7f-i*.7f)*18,Mathf.Cos(t*1.1f-i)*12,0);AnimatedFrames++;}
 }
}

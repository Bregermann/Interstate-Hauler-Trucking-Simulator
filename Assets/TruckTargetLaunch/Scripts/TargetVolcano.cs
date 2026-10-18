using UnityEngine;
using Cinemachine;
using LWS.TruckTerrierRacing;
namespace LWS.TruckTargetLaunch {
// Reuses the authored Terrier volcanic environment and its pooled actual-truck fragments.
// Only actual lava contacts/crossings resolve a miss; crossing a scoring ring never does.
public sealed class TargetVolcano:MonoBehaviour {
 public TargetMatch match;public Terrain terrain;public Transform lava;public ParticleSystem[] splashes;public TerrierWreckPool wrecks;public Transform[] debrisSources;public CinemachineImpulseSource impulse;
 public int blastSoundIndex=-1;public Transform[] impactFocus;
 public int LastImpactSlot{get;private set;}=-1;public float LastImpactTime{get;private set;}
 readonly bool[] impacted=new bool[4];public int Impacts{get;private set;}
 public void ResetRound(){Impacts=0;LastImpactSlot=-1;LastImpactTime=0;System.Array.Clear(impacted,0,4);wrecks?.ResetHeat();foreach(var effect in splashes)effect.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);}
 public void Contact(Collider collider){var racer=collider.attachedRigidbody!=null?collider.attachedRigidbody.GetComponent<TargetRacer>():null;if(racer==null||!match.Live||!match.RoundLive)return;var p=racer.truck.body.worldCenterOfMass;var d=p-lava.position;if(new Vector2(d.x,d.z).sqrMagnitude>340*340)return;racer.LavaImpact(new Vector3(p.x,lava.position.y,p.z));}
 static float Bottom(TargetRacer racer,Vector3 p,Quaternion q){var s=racer.truck.shape;var half=s.size*.5f;return p.y+(q*s.center).y-Mathf.Abs((q*Vector3.right).y)*half.x-Mathf.Abs((q*Vector3.up).y)*half.y-Mathf.Abs((q*Vector3.forward).y)*half.z;}
 public void ObserveSweep(TargetRacer racer,Vector3 prior,Quaternion rotation){float before=Bottom(racer,prior,rotation),after=Bottom(racer,racer.truck.body.position,racer.truck.body.rotation);if(before<lava.position.y||after>lava.position.y||before<=after)return;float t=Mathf.InverseLerp(before,after,lava.position.y);var p=Vector3.Lerp(prior,racer.truck.body.position,t);var d=p-lava.position;if(new Vector2(d.x,d.z).sqrMagnitude<=340*340)racer.LavaImpact(new Vector3(p.x,lava.position.y,p.z));}
 public void Explode(TargetRacer racer,Vector3 point){if(impacted[racer.slot])return;impacted[racer.slot]=true;Impacts++;LastImpactSlot=racer.slot;LastImpactTime=match.Clock;impactFocus[racer.slot].position=point;if(blastSoundIndex>=0)match.feedback.Play(blastSoundIndex,0,true);var effect=splashes[racer.slot%splashes.Length];effect.transform.position=point+Vector3.up*.25f;effect.Play();wrecks?.Emit(racer.slot);impulse?.GenerateImpulseAtPositionWithVelocity(point,Vector3.up*.65f);}
 public int ReplayMask(int mask){if(debrisSources==null||wrecks==null)return mask;for(int i=0;i<debrisSources.Length;i++){bool active=false;foreach(var piece in wrecks.pieces)if(piece.owner==i&&piece.body.gameObject.activeSelf){active=true;break;}if(active)mask|=1<<(5+i);}return mask;}
 public void BeginReplay(){wrecks?.BeginReplay();}
 public void Clear(){wrecks?.Clear();if(splashes!=null)foreach(var effect in splashes)effect.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);}
}
}

using System;
using System.Collections.Generic;
using UnityEngine;
using LWS.TruckSports;
using Assets.CoreCollapse.Scripts.Structural;

namespace LWS.TruckBungeeDemolition {
// Core-Collapse owns structural support failure. This adapter owns sport attribution and lifecycle.
public sealed class BungeeBay:MonoBehaviour {
 public BungeeMatch match;public int slot;public SportsEventTruck truck;public Transform craneAnchor,towerMount;public GameObject towerPrefab;public SpringJoint spring;public LineRenderer rope;public AudioSource wind,strain;public ParticleSystem dust;
 public float restLength=48,springStrength=180,damping=12,pumpAcceleration=5.5f,tugCooldown=3.5f;
 public int Score{get;private set;}public int Maximum{get;private set;}public int Collapses{get;private set;}public int Recoveries{get;private set;}public float Cooldown{get;private set;}public float Extension{get;private set;}public float RecoveryTime{get;private set;}
 public bool AcceptScore=>match!=null&&match.Scoring;public bool RopeToolkitIntegrated=>false;
 public readonly List<BungeeDebris> debris=new List<BungeeDebris>(64);public BungeeModule[] modules=Array.Empty<BungeeModule>();GameObject tower;float stuck,tug;Vector3 prior;bool released;
 public void ResetBay(){Clear();dust?.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);Score=Maximum=Collapses=Recoveries=0;Cooldown=RecoveryTime=stuck=tug=0;released=false;tower=Instantiate(towerPrefab,towerMount.position,towerMount.rotation,transform);modules=tower.GetComponentsInChildren<BungeeModule>(true);foreach(var m in modules){m.bay=this;Maximum+=m.budget;}truck.Prepare(craneAnchor.position+new Vector3(-7,-3,-10),Quaternion.Euler(0,35,0),false);truck.body.isKinematic=true;truck.body.maxLinearVelocity=40;truck.body.maxAngularVelocity=7;spring.maxDistance=restLength;spring.minDistance=0;spring.spring=springStrength;spring.damper=damping;prior=truck.body.position;}
 public void Release(){released=true;truck.body.isKinematic=false;truck.body.WakeUp();}
 public void Tick(Vector2 input,bool action,float dt){Cooldown=Mathf.Max(0,Cooldown-dt);if(!released)return;if(RecoveryTime>0)return;
  var b=truck.body;var delta=b.worldCenterOfMass-craneAnchor.position;Extension=Mathf.Max(0,delta.magnitude-restLength);if(action&&Cooldown<=0){tug=.3f;Cooldown=tugCooldown;match.feedback.Play(2,0,true);match.Pulse(slot,.28f,.12f);}tug=Mathf.Max(0,tug-dt);spring.maxDistance=Mathf.MoveTowards(spring.maxDistance,restLength-(tug>0?2.5f:0),dt*10);
  input=Vector2.ClampMagnitude(input,1);var push=new Vector3(input.x,0,input.y)*pumpAcceleration;b.AddForce(push,ForceMode.Acceleration);b.AddTorque(new Vector3(input.y,0,-input.x)*.8f,ForceMode.Acceleration);
  bool immobile=Vector3.Distance(b.position,prior)<.035f&&b.linearVelocity.sqrMagnitude<1;stuck=immobile?stuck+dt:0;prior=b.position;
  if(stuck>6||b.position.y< -8||delta.magnitude>restLength+23||Mathf.Abs(b.position.x-transform.position.x)>65){Recover();}
 }
 public void Recover(){Recoveries++;RecoveryTime=3;stuck=0;truck.Prepare(craneAnchor.position+new Vector3(-9,-restLength+4,-12),Quaternion.identity,false);truck.body.isKinematic=true;released=false;match.feedback.Play(3,0,true);}
 public void RecoveryTick(float dt){if(RecoveryTime<=0)return;RecoveryTime=Mathf.Max(0,RecoveryTime-dt);if(RecoveryTime<=0)Release();}
 public void CollapseVisual(BungeeModule module){if(dust!=null){var emit=new ParticleSystem.EmitParams{position=module.transform.position,velocity=Vector3.up*1.8f,startSize=2.6f,startLifetime=2.8f};dust.Emit(emit,18);}match.feedback.Play(0,8);}
 public void Award(BungeeModule module){if(!AcceptScore||module.awarded)return;module.awarded=true;Score+=module.budget;Collapses++;match.feedback.Play(0,8);match.Pulse(slot,.35f,.12f);}
 public void FinaleStep(int level){foreach(var m in modules)if(m!=null&&m.gameObject.activeInHierarchy&&m.level==level)m.core.ReceiveAllDamage();}
 public void ReelClear(){released=false;truck.Power(false);if(!truck.body.isKinematic)truck.body.linearVelocity=truck.body.angularVelocity=Vector3.zero;truck.body.isKinematic=true;truck.body.detectCollisions=false;truck.body.position=craneAnchor.position+Vector3.down*4;truck.transform.position=truck.body.position;}
 public void Clear(){if(tower!=null){tower.SetActive(false);Destroy(tower);tower=null;}foreach(var d in debris)if(d!=null){d.gameObject.SetActive(false);Destroy(d.gameObject);}debris.Clear();}
 void LateUpdate(){if(rope==null||truck==null)return;const int points=32;rope.positionCount=points;var a=craneAnchor.position;var b=truck.body.position+truck.body.rotation*new Vector3(0,.5f,-.5f);float slack=Mathf.Max(0,restLength-Vector3.Distance(a,b));for(int i=0;i<points;i++){float t=i/(float)(points-1);rope.SetPosition(i,Vector3.Lerp(a,b,t)+Vector3.down*(Mathf.Sin(t*Mathf.PI)*Mathf.Min(5,slack*.15f)));}if(strain!=null){strain.pitch=Mathf.Lerp(.65f,1.2f,Mathf.Clamp01(Extension/8));strain.volume=match.Live&&Application.isFocused?Mathf.Clamp01(Extension/8)*match.settings.sfx*.08f/Mathf.Sqrt(Mathf.Max(1,match.setup.count)):0;if(strain.volume>.001f&&!strain.isPlaying)strain.Play();else if(strain.volume<=.001f&&strain.isPlaying)strain.Stop();}if(wind!=null){wind.volume=match.Live&&Application.isFocused?Mathf.Clamp01(truck.body.linearVelocity.magnitude/35)*match.settings.sfx*.13f/Mathf.Sqrt(Mathf.Max(1,match.setup.count)):0;if(wind.volume>.001f&&!wind.isPlaying)wind.Play();else if(wind.volume<=.001f&&wind.isPlaying)wind.Stop();}}
 void OnDestroy(){Clear();}
}
}

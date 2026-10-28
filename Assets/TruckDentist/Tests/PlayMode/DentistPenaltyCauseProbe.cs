#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
namespace LWS.TruckDentist.Tests
{
 // Test-only actual callbacks/receipts. Never changes scores, damage, collision
 // filters, sockets, physical actors, tool intent or runtime authority.
 public sealed class DentistPenaltyCauseProbe:MonoBehaviour
 {
  public Component match,tooth,tool,arm;public string scenario;public int collisions,jointBreaks;float next,peakImpulse;int lastPoints=int.MinValue;
  const BindingFlags Any=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
  static bool Present(object x)=>x is UnityEngine.Object u?u!=null:x!=null;
  static object Get(object x,string n)=>!Present(x)?null:x.GetType().GetField(n,Any) is FieldInfo f?f.GetValue(x):x.GetType().GetProperty(n,Any).GetValue(x);
  static string Path(Transform t)=>t==null?"none":t.parent==null?t.name:Path(t.parent)+"/"+t.name;
  public void Report(string reason)
  {
   var rules=Get(match,"rules");var body=(Rigidbody)Get(tooth,"body");var wrist=(Rigidbody)Get(arm,"grip");var socket=(Joint)Get(tooth,"Socket");var tip=(Vector3)Get(tool,"Tip");var latest=Get(Get(match,"ScoreJournal"),"Latest");
   Debug.Log("DENTIST_PENALTY_CAUSE scenario="+scenario+" event="+reason+" real="+Time.realtimeSinceStartup.ToString("R")+" fixed="+Time.fixedTime.ToString("R")+" frame="+Time.frameCount+" rulesClock="+Get(rules,"Clock")+" points="+string.Join(",",(int[])Get(rules,"points"))+" mistakes="+string.Join(",",(int[])Get(rules,"mistakes"))+" nativeContacts="+Get(match,"NativeToolContacts")+" nativeExtractions="+Get(match,"NativeExtractions")+" receipt="+(latest==null?"none":Get(latest,"EventId")+"/"+Get(latest,"Reason")+"/"+Get(latest,"Delta"))+" tooth="+Get(tooth,"id")+" socket="+(socket!=null)+" breakForce="+Get(tooth,"nativeBreakForce")+" body="+body.position.ToString("F6")+" bodyV="+body.linearVelocity.ToString("F6")+" bodyQ="+body.rotation.ToString("F6")+" healthyHP="+Get(tooth,"Health")+" broken="+Get(tooth,"Broken")+" extracted="+Get(tooth,"Extracted")+" tool="+Get(tool,"Kind")+" running="+Get(tool,"Running")+" speed="+Get(tool,"TipSpeed")+" tip="+tip.ToString("F6")+" wrist="+wrist.position.ToString("F6")+" wristQ="+wrist.rotation.ToString("F6")+" worldGoal="+Get(arm,"WorldGoal"));
  }
  void FixedUpdate(){if(match==null||tooth==null)return;var points=(int[])Get(Get(match,"rules"),"points");if(points[0]!=lastPoints){lastPoints=points[0];Report("OBSERVED_SCORE_CHANGE");}}
  void Contact(Collision c)
  {
   collisions++;float impulse=c.impulse.magnitude;bool newPeak=impulse>=20&&impulse>peakImpulse*1.25f;if(Time.fixedTime<next&&!newPeak)return;next=Time.fixedTime+.5f;peakImpulse=Mathf.Max(peakImpulse,impulse);
   var collider=c.collider;var tips=(SphereCollider[])Get(tool,"tips");int selected=Convert.ToInt32(Get(tool,"Kind"));var point=c.contactCount>0?c.GetContact(0):default;
   Debug.Log("DENTIST_REAL_TOOTH_COLLISION scenario="+scenario+" fixed="+Time.fixedTime.ToString("R")+" tooth="+Get(tooth,"id")+" other="+Path(collider.transform)+" otherBody="+(collider.attachedRigidbody==null?"none":Path(collider.attachedRigidbody.transform))+" otherKinematic="+(collider.attachedRigidbody!=null&&collider.attachedRigidbody.isKinematic)+" selectedTip="+(selected>=0&&selected<tips.Length&&collider==tips[selected])+" trigger="+collider.isTrigger+" impulse="+c.impulse.ToString("F6")+" estimatedForce="+(c.impulse.magnitude/Mathf.Max(.001f,Time.fixedDeltaTime)).ToString("R")+" relativeV="+c.relativeVelocity.ToString("F6")+" point="+point.point.ToString("F6")+" normal="+point.normal.ToString("F6")+" separation="+point.separation.ToString("R"));Report("REAL_COLLISION");
  }
  void OnCollisionEnter(Collision c){Contact(c);}void OnCollisionStay(Collision c){Contact(c);}
  void OnJointBreak(float force){jointBreaks++;Report("REAL_JOINT_BREAK force="+force.ToString("R"));}
 }
}
#endif

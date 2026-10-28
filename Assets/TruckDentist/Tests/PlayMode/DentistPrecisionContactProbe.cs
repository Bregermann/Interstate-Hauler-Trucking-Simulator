#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
namespace LWS.TruckDentist.Tests
{
 // Native test-only observation. No collision filters, forces, poses, scores,
 // socket limits or running intent are modified by this component.
 public sealed class DentistPrecisionContactProbe:MonoBehaviour
 {
  public Component match,tooth,tool,arm;float next,peak;const BindingFlags Any=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
  static object Get(object x,string n)=>x.GetType().GetField(n,Any) is FieldInfo f?f.GetValue(x):x.GetType().GetProperty(n,Any).GetValue(x);
  static string Path(Transform t)=>t==null?"none":t.parent==null?t.name:Path(t.parent)+"/"+t.name;
  void Start(){var body=(Rigidbody)Get(tooth,"body");foreach(var c in tooth.GetComponentsInChildren<Collider>(true))Debug.Log("DENTIST_ACTUAL_COMPOUND tooth="+Get(tooth,"id")+" path="+Path(c.transform)+" type="+c.GetType().Name+" enabled="+c.enabled+" active="+c.gameObject.activeInHierarchy+" trigger="+c.isTrigger+" belongsToOriginalBody="+(c.attachedRigidbody==body)+" bounds="+c.bounds);}
  void Contact(Collision collision)
  {
   float impulse=collision.impulse.magnitude;bool bigger=impulse>peak*1.25f&&impulse>1;if(Time.fixedTime<next&&!bigger)return;next=Time.fixedTime+.25f;peak=Mathf.Max(peak,impulse);var body=(Rigidbody)Get(tooth,"body");var grip=(Rigidbody)Get(arm,"grip");var tips=(SphereCollider[])Get(tool,"tips");var sphere=tips[Convert.ToInt32(Get(tool,"Kind"))];var p=collision.contactCount>0?collision.GetContact(0):default;var own=p.thisCollider!=null&&p.thisCollider.attachedRigidbody==body?p.thisCollider:p.otherCollider;var other=own==p.thisCollider?p.otherCollider:p.thisCollider;var physical=(Vector3)Get(tool,"PhysicalTip");var rendered=(Vector3)Get(tool,"Tip");var socket=(Joint)Get(tooth,"Socket");var scale=sphere.transform.lossyScale;float radius=sphere.radius*Mathf.Max(Mathf.Abs(scale.x),Mathf.Abs(scale.y),Mathf.Abs(scale.z));var receipt=Get(Get(match,"ScoreJournal"),"Latest");
   Debug.Log("DENTIST_REAL_PRECISION_CONTACT fixed="+Time.fixedTime.ToString("R")+" tooth="+Get(tooth,"id")+" kind="+Get(tool,"Kind")+" running="+Get(tool,"Running")+" toothCollider="+(own!=null?Path(own.transform):"none")+" toolCollider="+(other!=null?Path(other.transform):"none")+" selectedTip="+(other==sphere)+" point="+p.point.ToString("F6")+" normal="+p.normal.ToString("F6")+" separation="+p.separation.ToString("R")+" impulse="+collision.impulse.ToString("F6")+" force="+(impulse/Time.fixedDeltaTime).ToString("R")+" relativeV="+collision.relativeVelocity.ToString("F6")+" physicalTip="+physical.ToString("F6")+" renderedTip="+rendered.ToString("F6")+" tipDifference="+(physical-rendered).ToString("F6")+" physicalSurfaceGap="+(own!=null?(Vector3.Distance(own.ClosestPoint(physical),physical)-radius).ToString("R"):"none")+" gripPhysics="+grip.position.ToString("F6")+" gripRendered="+grip.transform.position.ToString("F6")+" radius="+radius.ToString("R")+" socket="+(socket!=null)+" socketForce="+(socket!=null?socket.currentForce.ToString("F6"):"none")+" socketBreak="+(socket!=null?socket.breakForce.ToString("R"):"none")+" receipt="+(receipt==null?"none":Get(receipt,"EventId")+"/"+Get(receipt,"Reason")+"/"+Get(receipt,"Delta")));
  }
  void OnCollisionEnter(Collision c){Contact(c);}void OnCollisionStay(Collision c){Contact(c);}void OnJointBreak(float force){Debug.Log("DENTIST_REAL_PRECISION_JOINT_BREAK tooth="+Get(tooth,"id")+" force="+force.ToString("R")+" physicalTip="+Get(tool,"PhysicalTip")+" renderedTip="+Get(tool,"Tip"));}
 }
}
#endif

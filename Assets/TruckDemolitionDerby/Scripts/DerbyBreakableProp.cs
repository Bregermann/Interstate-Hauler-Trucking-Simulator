using UnityEngine;using Assets.CoreCollapse.Scripts.Structural;
namespace LWS.TruckDerby {
// Core-Collapse owns health/support/collapse. This adapter reuses the existing prefractured
// mesh clusters in a fixed scene pool so heat resets and recorded hierarchy remain stable.
public sealed class DerbyBreakableProp:Collapser {
 public GameObject intact;public Collider[] solids;public Rigidbody[] fragments;public Vector3[] rest;public Quaternion[] rotation;public DerbyMatch match;public bool Broken{get;private set;}float last=-100;Vector3 incoming;
 public void ResetProp(){currentHp=startingHp;support=GetComponent<Support>();Broken=false;last=-100;intact.SetActive(true);foreach(var c in solids)c.enabled=true;for(int i=0;i<fragments.Length;i++){var b=fragments[i];b.isKinematic=true;b.detectCollisions=false;b.transform.localPosition=rest[i];b.transform.localRotation=rotation[i];b.transform.localScale=Vector3.zero;}}
 void OnCollisionEnter(Collision c){if(Broken||!match.Live||match.Stage!=DerbyStage.Playing||Time.fixedTime-last<.3f||c.rigidbody==null)return;var f=c.rigidbody.GetComponent<DerbyFighter>();if(f==null||f.Eliminated)return;float speed=0;for(int i=0;i<c.contactCount;i++)speed=Mathf.Max(speed,Mathf.Abs(Vector3.Dot(c.relativeVelocity,c.GetContact(i).normal)));if(speed<3)return;last=Time.fixedTime;incoming=-c.relativeVelocity;ReceiveDamage(Mathf.Min(100,.5f*c.rigidbody.mass*speed*speed/600));}
 protected override void Collapse(){if(Broken)return;Broken=true;base.Collapse();intact.SetActive(false);foreach(var c in solids)c.enabled=false;for(int i=0;i<fragments.Length;i++){var b=fragments[i];b.transform.localPosition=rest[i];b.transform.localRotation=rotation[i];b.transform.localScale=Vector3.one;b.detectCollisions=true;b.isKinematic=false;b.linearVelocity=Vector3.ClampMagnitude(incoming*.3f,8)+Vector3.up*.4f;b.angularVelocity=Vector3.Cross(Vector3.up,incoming).normalized*.8f;b.WakeUp();}match.feedback.Play(1,7);}
 public void Freeze(){foreach(var b in fragments){b.isKinematic=true;b.detectCollisions=false;}}
}}

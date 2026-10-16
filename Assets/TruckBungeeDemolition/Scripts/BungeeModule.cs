using System;using UnityEngine;using LWS.TruckSports;using Assets.CoreCollapse.Scripts.Structural;
namespace LWS.TruckBungeeDemolition {
public sealed class BungeeModule:MonoBehaviour {
 public CollapserPrefrag core;public BungeeBay bay;public int level,budget=10;[NonSerialized]public bool awarded;float contactAt=-100;
 public void Collapsed(){bay?.CollapseVisual(this);bay?.Award(this);}
 void OnCollisionEnter(Collision hit){if(bay==null||!bay.AcceptScore||core.HasCollapsed||Time.fixedTime-contactAt<.12f)return;var truck=hit.rigidbody!=null?hit.rigidbody.GetComponent<SportsEventTruck>():null;var fragment=hit.rigidbody!=null?hit.rigidbody.GetComponentInParent<BungeeDebris>():null;if(truck!=bay.truck&&(fragment==null||fragment.bay!=bay||!fragment.playerCaused))return;float speed=0;for(int i=0;i<hit.contactCount;i++)speed=Mathf.Max(speed,Mathf.Abs(Vector3.Dot(hit.relativeVelocity,hit.GetContact(i).normal)));if(speed<(fragment!=null?7:4))return;contactAt=Time.fixedTime;float mass=hit.rigidbody!=null?Mathf.Min(12,hit.rigidbody.mass):0;core.ReceiveDamage(Mathf.Clamp(.5f*mass*speed*speed*.065f,0,110));}
}
}

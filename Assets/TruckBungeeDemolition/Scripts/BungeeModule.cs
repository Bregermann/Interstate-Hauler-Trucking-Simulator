using System;using UnityEngine;using LWS.TruckSports;using Assets.CoreCollapse.Scripts.Structural;
namespace LWS.TruckBungeeDemolition {
public sealed class BungeeModule:MonoBehaviour {
 public float LastIncomingSpeed{get;private set;}public float LastRelativeSpeed{get;private set;}public int ImpactEntries{get;private set;}public int DamageEntries{get;private set;}public bool LastWasTruck{get;private set;}
 public CollapserPrefrag core;public BungeeBay bay;public int level,budget=10;[NonSerialized]public bool awarded;float contactAt=-100;
 public void Collapsed(){bay?.CollapseVisual(this);bay?.Award(this);}
 void OnCollisionEnter(Collision hit){if(bay==null||!bay.AcceptScore||core.HasCollapsed||Time.fixedTime-contactAt<.12f)return;var truck=hit.rigidbody!=null?hit.rigidbody.GetComponent<SportsEventTruck>():null;var fragment=hit.rigidbody!=null?hit.rigidbody.GetComponentInParent<BungeeDebris>():null;if(truck!=bay.truck&&(fragment==null||fragment.bay!=bay||!fragment.playerCaused))return;float speed=0,incoming=0;for(int i=0;i<hit.contactCount;i++){var c=hit.GetContact(i);speed=Mathf.Max(speed,Mathf.Abs(Vector3.Dot(hit.relativeVelocity,c.normal)));var pointVelocity=bay.IncomingVelocity+Vector3.Cross(bay.IncomingAngular,c.point-bay.IncomingCenter);incoming=Mathf.Max(incoming,Mathf.Max(0,Vector3.Dot(pointVelocity,c.normal)));}LastWasTruck=truck==bay.truck;LastIncomingSpeed=LastWasTruck?incoming:0;LastRelativeSpeed=speed;ImpactEntries++;if(truck==bay.truck)speed=incoming;if(speed<(fragment!=null?7:4))return;contactAt=Time.fixedTime;float mass=hit.rigidbody!=null?Mathf.Min(12,hit.rigidbody.mass):0;float before=core.HealthRatio;core.ReceiveDamage(Mathf.Clamp(.5f*mass*speed*speed*.065f,0,110));if(core.HasCollapsed||core.HealthRatio<before-.0001f)DamageEntries++;}
}
}

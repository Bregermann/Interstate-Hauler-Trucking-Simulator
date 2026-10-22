using UnityEngine;
namespace LWS.TruckBaseJumping {
public sealed class BaseSkyBody:MonoBehaviour {
 public BaseSkyField field;public Rigidbody body;public int kind,index;public bool queued;public float recycleAt;public TrailRenderer trail;public ParticleSystem smoke;
 public void BeginSmoke(){if(smoke==null)return;smoke.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);smoke.Play(true);}
 public void DriftSmoke(Vector3 wind){if(smoke==null)return;var force=smoke.forceOverLifetime;force.enabled=true;force.space=ParticleSystemSimulationSpace.World;force.x=wind.x*.035f;force.y=wind.y*.035f;force.z=wind.z*.035f;}
 void OnDisable(){if(smoke!=null)smoke.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);}
 void OnCollisionEnter(Collision c){var jumper=c.rigidbody!=null?c.rigidbody.GetComponent<BaseJumper>():null;if(jumper!=null&&!jumper.Landed&&jumper.Released&&kind!=0)field.Queue(this);}
}
}

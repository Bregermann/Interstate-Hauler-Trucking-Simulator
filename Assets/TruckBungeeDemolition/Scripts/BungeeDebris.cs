using System;using UnityEngine;using LWS.TruckSports;using Assets.CoreCollapse.Scripts.Structural;
namespace LWS.TruckBungeeDemolition {
public sealed class BungeeDebris:MonoBehaviour {
 [NonSerialized]public BungeeBay bay;public bool playerCaused;public Collider[] CollisionShapes{get;private set;}float quiet;bool dormant;Rigidbody[] bodies;
 void Awake(){var match=FindAnyObjectByType<BungeeMatch>();if(match==null)return;float best=float.MaxValue;foreach(var b in match.bays){float d=(b.towerMount.position-transform.position).sqrMagnitude;if(d<best){best=d;bay=b;}}if(bay==null)return;CollisionShapes=GetComponentsInChildren<Collider>();for(int i=0;i<CollisionShapes.Length;i++)for(int j=0;j<i;j++)Physics.IgnoreCollision(CollisionShapes[i],CollisionShapes[j]);foreach(var other in match.bays){if(other==bay)continue;foreach(var shape in CollisionShapes){Physics.IgnoreCollision(shape,other.truck.shape);foreach(var group in other.debris)if(group!=null&&group.CollisionShapes!=null)foreach(var foreign in group.CollisionShapes)if(foreign!=null)Physics.IgnoreCollision(shape,foreign);}}bay.debris.Add(this);transform.SetParent(bay.transform,true);playerCaused=bay.AcceptScore;bodies=GetComponentsInChildren<Rigidbody>();foreach(var body in bodies){body.maxLinearVelocity=35;body.maxAngularVelocity=8;body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;body.linearDamping=.1f;body.angularDamping=.2f;}}
 void FixedUpdate(){if(bodies==null)return;bool settled=true;foreach(var b in bodies)if(!b.IsSleeping()&&(b.linearVelocity.sqrMagnitude>.09f||b.angularVelocity.sqrMagnitude>.09f))settled=false;quiet=settled?quiet+Time.fixedDeltaTime:0;
 // Sleeping rubble remains movable. Making it kinematic created a permanent concrete cage after the first collapse.
 if(quiet>3&&!dormant){foreach(var b in bodies){b.Sleep();b.collisionDetectionMode=CollisionDetectionMode.Discrete;}dormant=true;}
 else if(!settled&&dormant){foreach(var b in bodies)b.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;dormant=false;}
 if(quiet>15&&bay!=null&&bay.debris.Count>96&&Vector3.Distance(transform.position,bay.truck.body.position)>25){gameObject.SetActive(false);Destroy(gameObject);}
 }
 void OnDestroy(){if(bay!=null)bay.debris.Remove(this);}
}
}

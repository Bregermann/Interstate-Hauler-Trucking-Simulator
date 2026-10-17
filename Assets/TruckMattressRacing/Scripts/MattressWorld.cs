using UnityEngine;using LWS.TruckCheeseRolling;
namespace LWS.TruckMattressRacing {
public sealed class MattressWorld:MonoBehaviour {
 public MattressMatch match;public MattressObstacle[] obstacles;public MattressWalker[] walkers;public ParticleSystem[] blasts;public int Explosions{get;private set;}public int PedestrianHits{get;set;}int blast;
 public void ResetWorld(int seed){gameObject.SetActive(true);Explosions=PedestrianHits=0;var rng=new System.Random(seed);for(int i=0;i<obstacles.Length;i++){var o=obstacles[i];float z=48+i*16.5f;float x=match.course.Center(z)+(i%2==0?-1:1)*(o.kind==MattressObstacleKind.Tree?25+(float)rng.NextDouble()*7:7+(float)rng.NextDouble()*17);var p=match.course.Surface(new Vector3(x,0,z));o.ResetObstacle(p,match.course.Rotation(z));}for(int i=0;i<walkers.Length;i++)walkers[i].ResetWalker(82+i*72);}
 public void Explode(Vector3 position){Explosions++;var fx=blasts[blast++%blasts.Length];fx.transform.position=position;fx.Play();foreach(var r in match.racers){if(r.Phase!=MattressAttempt.Racing)continue;var b=r.truck.body;var d=b.worldCenterOfMass-position;float distance=d.magnitude;if(distance>12)continue;d.y=Mathf.Max(1.2f,d.y+2);b.AddForce(d.normalized*Mathf.Lerp(9,1,distance/12),ForceMode.VelocityChange);b.AddTorque(Vector3.Cross(Vector3.up,d.normalized)*Mathf.Lerp(2,.2f,distance/12),ForceMode.VelocityChange);match.Pulse(r.slot,.3f,.16f);}foreach(var o in obstacles)if(o.tree!=null&&Vector3.Distance(o.transform.position,position)<20)o.tree.Blast(position);match.feedback.Play(7,8);}
 public void Freeze(){foreach(var o in obstacles)o.body.isKinematic=true;foreach(var w in walkers)w.StopWalker();foreach(var p in blasts)p.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);}public void Hide(){Freeze();gameObject.SetActive(false);}
 void LateUpdate(){if(!match.Live||match.Phase!=MattressPhase.Race)return;foreach(var o in obstacles)o.Maintain();foreach(var w in walkers)w.Step();}
}
public enum MattressObstacleKind{Tree,Sheep,Loose,Fence}
}

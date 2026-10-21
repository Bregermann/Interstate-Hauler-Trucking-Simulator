using UnityEngine;
using LWS.TruckSports;
namespace LWS.TruckFareFight {
 public sealed class FareFightHazards:MonoBehaviour {
  public FareFightMatch match;public FareFightHazard[] hazards;
  readonly float[] slipperyUntil=new float[4];
  public void ResetMatch(){System.Array.Clear(slipperyUntil,0,4);var random=new System.Random(match.matchSeed^81345);foreach(var hazard in hazards){Vector3 point=Vector3.zero;bool valid=false;for(int attempt=0;attempt<150;attempt++){var edge=match.district.graph.edges[random.Next(match.district.graph.edges.Count)];if(edge.samples.Count<3)continue;point=edge.samples[random.Next(1,edge.samples.Count-1)].position;valid=true;foreach(var stop in match.district.stops)if(Vector3.Distance(stop.position,point)<35){valid=false;break;}for(int i=0;i<match.setup.count;i++)if(Vector3.Distance(match.trucks[i].body.position,point)<40){valid=false;break;}if(valid)break;}hazard.ResetHazard(point,valid);}}
  public void Tick(){for(int i=0;i<match.setup.count;i++)match.trucks[i].tractorMotor.gripScale=match.atmosphere.Grip*(Time.time<slipperyUntil[i]?.35f:1);}
  public void Slip(int slot,Vector3 point){if(!match.Live||match.Intro>0)return;var truck=match.trucks[slot];float speed=truck.body.linearVelocity.magnitude;if(speed<2)return;slipperyUntil[slot]=Mathf.Max(slipperyUntil[slot],Time.time+2.2f);float side=Vector3.Dot(point-truck.body.position,truck.transform.right);truck.body.AddTorque(Vector3.up*Mathf.Sign(side==0?slot%2==0?1:-1:side)*Mathf.Clamp(speed*.07f,.25f,1.2f),ForceMode.VelocityChange);match.Pulse(slot,.1f,.15f);match.feedback.Play(4,0,true);}
 }
}

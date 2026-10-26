using System;
using UnityEngine;

namespace LWS.TruckBaseJumping
{
 [Serializable]
 public sealed class BaseGustSettings
 {
  public bool enabled=true;
  public Vector2 intervalSeconds=new Vector2(8,16),durationSeconds=new Vector2(6,9),addedSpeed=new Vector2(2,4);
  [Min(10)]public float bandWidth=80;
  [Min(5)]public float sweepSpeed=35;
  [Min(.1f)]public float fadeSeconds=.8f;
  [Min(1)]public float maximumTotalSpeed=13;
  public float fadeOutBelowAltitude=25,fullStrengthAltitude=55;
 }
 // One seeded shared band, sampled at world positions. This supplies missing
 // spatial course semantics; existing BaseJumper.Step still integrates forces.
 public sealed class BaseWindGusts
 {
  readonly BaseGustSettings settings;System.Random random;float next,start=-1,end=-1,strength;bool stopped;
  public int Seed{get;private set;}public int Serial{get;private set;}public float Age{get;private set;}
  public bool Active=>!stopped&&settings.enabled&&start>=0&&Age>=start&&Age<end;
  public Vector3 Direction{get;private set;}
  public Vector3 Center=>Direction*((Age-(start+end)*.5f)*Mathf.Max(5,settings.sweepSpeed));
  public float Envelope=>!Active?0:Mathf.Min(Mathf.SmoothStep(0,1,(Age-start)/Mathf.Max(.1f,settings.fadeSeconds)),Mathf.SmoothStep(0,1,(end-Age)/Mathf.Max(.1f,settings.fadeSeconds)));
  public float Strength=>strength*Envelope;
  public BaseWindGusts(BaseGustSettings configuration){settings=configuration;}
  float Range(Vector2 range,float floor){float minimum=Mathf.Max(floor,Mathf.Min(range.x,range.y)),maximum=Mathf.Max(minimum,Mathf.Max(range.x,range.y));return Mathf.Lerp(minimum,maximum,(float)random.NextDouble());}
  public void Reset(int seed){Seed=seed;random=new System.Random(seed);Serial=0;Age=0;start=end=-1;strength=0;Direction=Vector3.zero;stopped=false;next=Range(settings.intervalSeconds,1);}
  public void Tick(float age)
  {
   if(random==null)Reset(Seed);if(age<Age)Reset(Seed);Age=Mathf.Max(0,age);if(stopped||!settings.enabled)return;
   while(Age>=next){start=next;end=start+Range(settings.durationSeconds,1);strength=Range(settings.addedSpeed,0);float angle=(float)random.NextDouble()*Mathf.PI*2;Direction=new Vector3(Mathf.Sin(angle),0,Mathf.Cos(angle));Serial++;next=end+Range(settings.intervalSeconds,1);}
  }
  public Vector3 Delta(Vector3 position)
  {
   if(!Active)return Vector3.zero;float distance=Mathf.Abs(Vector3.Dot(position-Center,Direction));float spatial=1-Mathf.SmoothStep(0,1,distance/Mathf.Max(5,settings.bandWidth*.5f));float height=Mathf.InverseLerp(settings.fadeOutBelowAltitude,Mathf.Max(settings.fadeOutBelowAltitude+.1f,settings.fullStrengthAltitude),position.y);return Direction*(Strength*spatial*height);
  }
  public void Stop(){stopped=true;}
 }
}

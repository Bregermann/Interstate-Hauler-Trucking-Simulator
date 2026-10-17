using UnityEngine;
namespace LWS.TruckDerby {
// Real COM net travel in an observation window, never wheel RPM or accumulated vibration path length.
public sealed class DerbyMotionWatch {
 public float Stopped{get;private set;}public float MeasuredSpeed{get;private set;}public bool Moving{get;private set;}Vector3 sample;float age;bool fastLast;
 public void Reset(Vector3 center){sample=center;age=Stopped=MeasuredSpeed=0;Moving=fastLast=false;}
 public bool Tick(Vector3 center,float dt){if(dt<=0)return Stopped>=5;Stopped+=dt;age+=dt;if(age>=.4f){MeasuredSpeed=Vector3.Distance(center,sample)/age;sample=center;age=0;bool fast=MeasuredSpeed>=.45f;Moving=fast||fastLast&&MeasuredSpeed>=.2f;if(Moving)Stopped=0;fastLast=fast;}return Stopped>=5;}
}
}

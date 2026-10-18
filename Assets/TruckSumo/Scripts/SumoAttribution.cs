using UnityEngine;

namespace LWS.TruckSumo {
// Small fixed contact history, not a collision-response system. No forces here.
// One record per victim/attacker avoids callback-order-dependent last-touch wins.
public sealed class SumoAttribution {
 struct Push {public bool valid;public float time,strength;public Vector3 origin,direction;}
 readonly Push[,] pushes=new Push[4,4];readonly float[] settled=new float[4];int count,heat;
 public const float CreditWindow=4;
 public void Reset(int participants,int heatNumber){count=Mathf.Clamp(participants,2,4);heat=heatNumber;System.Array.Clear(pushes,0,pushes.Length);System.Array.Clear(settled,0,settled.Length);}
 public bool Record(int victim,int attacker,float now,Vector3 position,Vector3 receivingNormal,float impulsePerMass,float otherIncomingSpeed,float closingSpeed,bool sustained){
  if(victim<0||victim>=count||attacker<0||attacker>=count||attacker==victim||otherIncomingSpeed<.75f)return false;
  bool impact=closingSpeed>=1.5f&&impulsePerMass>=.25f;
  if(!impact&&!(sustained&&impulsePerMass>=.015f))return false;
  var direction=Vector3.ProjectOnPlane(receivingNormal,Vector3.up);if(direction.sqrMagnitude<.25f)return false;direction.Normalize();
  var prior=pushes[victim,attacker];bool continuing=prior.valid&&now>=prior.time&&now-prior.time<=.25f&&Vector3.Dot(direction,prior.direction)>.7f;
  pushes[victim,attacker]=new Push{valid=true,time=now,strength=Mathf.Max(impulsePerMass,continuing?prior.strength*.99f:0),origin=continuing?prior.origin:position,direction=direction};settled[victim]=0;return true;
 }
 public void Observe(int victim,float now,float dt,Vector3 position,Vector3 velocity){
  var flat=Vector3.ProjectOnPlane(velocity,Vector3.up);settled[victim]=flat.magnitude<.35f?settled[victim]+dt:0;
  for(int attacker=0;attacker<count;attacker++){var push=pushes[victim,attacker];if(!push.valid)continue;
   if(now<push.time||now-push.time>CreditWindow||settled[victim]>.6f||Vector3.Dot(position-push.origin,push.direction)<-.75f)pushes[victim,attacker]=default;
  }
 }
 public int TakeCredit(int victim,float now,Vector3 position,Vector3 velocity){
  var outward=Vector3.ProjectOnPlane(position,Vector3.up).normalized;var motion=Vector3.ProjectOnPlane(velocity,Vector3.up);int best=-1;float bestValue=0;
  for(int attacker=0;attacker<count;attacker++){
   var push=pushes[victim,attacker];pushes[victim,attacker]=default;if(!push.valid||now<push.time||now-push.time>CreditWindow)continue;
   float alignment=Vector3.Dot(outward,push.direction);
   if(alignment<.25f||Vector3.Dot(motion,outward)<.35f||Vector3.Dot(motion.normalized,push.direction)<.4f||Vector3.Dot(position-push.origin,push.direction)<.001f)continue;
   float value=(Mathf.Min(push.strength,12)+.25f)*alignment/(.5f+now-push.time);
   // Exact shared-impact ties rotate their deterministic priority each heat.
   if(value>bestValue+.0001f||Mathf.Abs(value-bestValue)<=.0001f&&(best<0||(attacker+heat)%count<(best+heat)%count)){best=attacker;bestValue=value;}
  }
  return best;
 }
}
}

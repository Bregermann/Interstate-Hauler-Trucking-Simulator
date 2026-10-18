#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections;
using System.Reflection;
using UnityEngine;
namespace LWS.TruckPedestrianPlowing {
 public sealed partial class PlowValidationProbe {
  IEnumerator RecoveryCases(bool baseline){
   yield return new WaitForSeconds(4);
   m.ProbeHandsOff=true;
   foreach(var pedestrian in m.Crowd)pedestrian.ragdoll.PrepareForPool();
   foreach(var truck in m.trucks)truck.Park(new Vector3(-65,2,65));
   m.setup.count=1;
   var target=System.Array.Find(m.Crowd,p=>p.role==PlowTargetKind.Standard);
   var start=new Vector3(-24,.7f,-24);
   target.ResetTarget(m,target.id,target.role,start+Vector3.forward*13-Vector3.up*.65f,Quaternion.identity,target.characterName,target.panicLine);
   target.agent.enabled=false;
   var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.name="Explicit physical wall recovery fixture";
   wall.transform.position=new Vector3(-24,1.5f,-20.5f);wall.transform.localScale=new Vector3(8,3,1);
   var bot=m.trucks[0];bot.Prepare(start,Quaternion.identity);
   var flags=BindingFlags.Instance|BindingFlags.NonPublic;
   ((int[])typeof(PlowMatch).GetField("target",flags).GetValue(m))[0]=target.id;
   ((float[])typeof(PlowMatch).GetField("rethink",flags).GetValue(m))[0]=Time.time+45;
   Physics.SyncTransforms();m.ProbeHandsOff=false;
   float until=Time.time+35,sample=0;bool reversed=false;float maximumDistance=0,blockedLowThrottle=0;
   while(Time.time<until&&!target.Claimed){
    reversed|=bot.command.y<-.2f;maximumDistance=Mathf.Max(maximumDistance,Vector3.Distance(start,bot.body.position));
    if(bot.body.linearVelocity.sqrMagnitude<.36f&&Mathf.Abs(bot.command.y-.3f)<.001f)blockedLowThrottle+=Time.fixedDeltaTime;
    if(Time.time>=sample){sample=Time.time+.5f;log.AppendLine("WALL_RECOVERY t="+Time.time.ToString("F2")+" position="+bot.body.position.ToString("F3")+" velocity="+bot.body.linearVelocity.ToString("F3")+" input="+bot.command.ToString("F3")+" stuck="+((float[])typeof(PlowMatch).GetField("stuck",flags).GetValue(m))[0].ToString("F3"));}
    yield return new WaitForFixedUpdate();
   }
   log.AppendLine("WALL_RESULT baseline="+baseline+" reverse="+reversed+" maxDistance="+maximumDistance+" blockedLowThrottleSeconds="+blockedLowThrottle+" physicalTargetClaim="+target.Claimed);
   Check(baseline?maximumDistance>0:reversed&&target.Claimed,baseline?"baseline physical wall encounter measured without assuming recovery outcome":"AI physically escapes wall and reaches target through ordinary NWH driving");
   yield return Capture(baseline?"wall-stall-baseline":"wall-recovered");Destroy(wall);m.ProbeHandsOff=true;
  }
 }
}
#endif

using System.Linq;
using UnityEngine;
namespace LWS.TruckMoonMadness
{
    public sealed partial class MoonMatch
    {
#if UNITY_EDITOR
        readonly Vector3[] observedGoals=new Vector3[8],observedWaypoints=new Vector3[8];float nextPhysicalTrace;
#endif
        [System.Diagnostics.Conditional("UNITY_EDITOR")]void ResetPhysicalDiagnostics(){
#if UNITY_EDITOR
            nextPhysicalTrace=0;
#endif
        }
        [System.Diagnostics.Conditional("UNITY_EDITOR")]void ObserveAiGoal(int owner,Vector3 goal,Vector3 waypoint){
#if UNITY_EDITOR
            observedGoals[owner]=goal;observedWaypoints[owner]=waypoint;
#endif
        }
        [System.Diagnostics.Conditional("UNITY_EDITOR")]void TracePhysicalState(){
#if UNITY_EDITOR
            if(Clock<nextPhysicalTrace)return;nextPhysicalTrace=Clock+5;
            for(int i=0;i<8;i++){var t=trucks[i];Debug.Log($"MOON_AI clock={Clock:R} owner={i} cargo={Rules.Carried(i)} score={Rules.Scores[i]} p={t.body.position.ToString("F4")} v={t.body.linearVelocity.ToString("F4")} forward={t.transform.forward.ToString("F4")} signedV={Vector3.Dot(t.body.linearVelocity,t.transform.forward):R} park={t.tractorMotor.vehicle.input.states.handbrake:R} gear={t.tractorMotor.vehicle.powertrain.transmission.Gear} cmd={t.command} goal={observedGoals[i].ToString("F4")} path={observedWaypoints[i].ToString("F4")} grounded={t.tractorMotor.Grounded} up={t.transform.up.ToString("F3")}");}
            for(int f=0;f<flags.Length;f++){var flag=flags[f];int o=Rules.Owners[f];string joint=flag.Joint!=null?flag.Joint.connectedAnchor.ToString("F4"):"NONE";float rack=o>=0?Vector3.Distance(flag.body.position,trucks[o].transform.TransformPoint(new Vector3(0,2.5f,.3f))):-1;Debug.Log($"MOON_FLAG clock={Clock:R} id={f} owner={o} p={flag.body.position.ToString("F4")} v={flag.body.linearVelocity.ToString("F4")} contacts={string.Join(",",Enumerable.Range(0,8).Where(flag.Touching))} joint={joint} held={flag.HeldSeconds:R} inStation={(o>=0&&stations[o].Contains(f))} rackDelta={rack:R}");}
#endif
        }
    }
}

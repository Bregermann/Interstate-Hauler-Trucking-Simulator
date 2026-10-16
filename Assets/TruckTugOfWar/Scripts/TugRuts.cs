using UnityEngine;
namespace LWS.TruckTugOfWar {
// Built-in TrailRenderers retain visual wheel grooves. Small cached wear affects grip equally.
// Weatherade remains the snow/wet-coverage renderer; its shipped RainCoverage has no mud-rut solver.
public sealed class TugRuts:MonoBehaviour {
 public TugMatch match;public TrailRenderer[] trails;readonly float[] wear=new float[128*64];readonly Vector3[] prior=new Vector3[32];readonly bool[] grounded=new bool[32];
 int Cell(Vector3 p)=>Mathf.Clamp((int)((p.z+20)*1.6f),0,63)*128+Mathf.Clamp((int)((p.x+40)*1.6f),0,127);
 public float Depth(Vector3 p)=>wear[Cell(p)];
 public void ResetRuts(){System.Array.Clear(wear,0,wear.Length);System.Array.Clear(grounded,0,grounded.Length);if(trails!=null)foreach(var t in trails){t.emitting=false;t.Clear();}}
 void LateUpdate(){for(int i=0;i<trails.Length;i++){int slot=i/4,w=i%4;var trail=trails[i];if(slot>=match.setup.count||!match.Live||match.Stage!=TugStage.Pull){trail.emitting=false;grounded[i]=false;continue;}var wheel=match.trucks[slot].wheels[w];bool contact=wheel.IsGrounded&&wheel.HitCollider!=null&&wheel.HitNormal.y>.7f;if(!contact){trail.emitting=false;grounded[i]=false;continue;}var p=wheel.HitPoint;float distance=grounded[i]?Vector3.Distance(prior[i],p):0;if(distance>3){trail.emitting=false;grounded[i]=false;continue;}trail.transform.position=p+Vector3.up*.016f;trail.emitting=distance>.012f;wear[Cell(p)]=Mathf.Clamp01(wear[Cell(p)]+distance*.08f);prior[i]=p;grounded[i]=true;}}
}
}

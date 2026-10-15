using UnityEngine;
namespace LWS.TruckTerrierRacing {
public struct TerrierTunnelState {public bool entered,invalid;}
public static class TerrierRules {
 public const int Racers=12,MatchWins=5;public const float TunnelHalfWidth=1.6875f,TunnelHeight=3.2f,TunnelDepth=2.8f;
 // Ordered full-body crossing: a front entry alone, side overlap or airborne bypass earns nothing.
 public static bool Crossing(ref TerrierTunnelState state,Bounds previous,Bounds current,float entrance,out float fraction){fraction=1;bool contained=current.min.x>=-TunnelHalfWidth&&current.max.x<=TunnelHalfWidth&&current.min.y>=-.15f&&current.max.y<=TunnelHeight;float exit=entrance+TunnelDepth;if(current.max.z<entrance){state=default;return false;}if(previous.max.z<entrance&&current.max.z>=entrance){state.entered=contained;state.invalid=!contained;}if(state.entered&&current.min.z<exit&&!contained)state.invalid=true;if(!state.entered||state.invalid||previous.min.z>=exit||current.min.z<exit)return false;fraction=Mathf.Clamp01((exit-previous.min.z)/Mathf.Max(.00001f,current.min.z-previous.min.z));state.entered=false;return true;}
 public static bool FatalWall(Vector3 incoming,Vector3 normal)=>Mathf.Max(0,-Vector3.Dot(incoming,normal))>=10.5f;
 public static int Stall(int slot,int heat)=>(slot+heat*5)%Racers;
 public static int Winner(float[] times){int winner=-1;float best=float.PositiveInfinity;bool tie=false;for(int i=0;i<Racers;i++){float t=times[i];if(t<best-.0001f){best=t;winner=i;tie=false;}else if(!float.IsInfinity(t)&&Mathf.Abs(t-best)<=.0001f)tie=true;}return tie?-1:winner;}
}
}

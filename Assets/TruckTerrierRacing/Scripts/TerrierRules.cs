using UnityEngine;
namespace LWS.TruckTerrierRacing {
public struct TerrierTunnelState {public bool entered,invalid;}
public static class TerrierRules {
 public const int Racers=12,MatchWins=5;public const float TunnelHalfWidth=1.6875f,TunnelHeight=3.2f,TunnelDepth=2.8f;
 // Ordered full-body crossing: a front entry alone, side overlap or airborne bypass earns nothing.
 public static bool Crossing(ref TerrierTunnelState state,Bounds previous,Bounds current,float entrance,out float fraction){var a=new Vector3[8];var b=new Vector3[8];for(int i=0;i<8;i++){var sign=new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1);a[i]=previous.center+Vector3.Scale(previous.extents,sign);b[i]=current.center+Vector3.Scale(current.extents,sign);}return Crossing(ref state,a,b,entrance,out fraction);}
 // Clip the oriented body to the tunnel slab. A rear corner still before the entrance need not fit yet.
 // All corners must eventually cross the rear plane. Samples use actual consecutive physics poses, never triggers.
 public static bool Crossing(ref TerrierTunnelState state,Vector3[] previous,Vector3[] current,float entrance,out float fraction){fraction=1;float oldMax=float.NegativeInfinity,newMax=oldMax,oldMin=float.PositiveInfinity,newMin=oldMin;for(int i=0;i<8;i++){oldMax=Mathf.Max(oldMax,previous[i].z);newMax=Mathf.Max(newMax,current[i].z);oldMin=Mathf.Min(oldMin,previous[i].z);newMin=Mathf.Min(newMin,current[i].z);}if(newMax<entrance){state=default;return false;}float enter=0,leave=1,exit=entrance+TunnelDepth;
 if(oldMax<entrance&&newMax>=entrance){enter=1;for(int i=0;i<8;i++)if(current[i].z>=entrance)enter=Mathf.Min(enter,PlaneFraction(previous[i].z,current[i].z,entrance));state.entered=true;state.invalid=false;}
 bool rearCrosses=oldMin<exit&&newMin>=exit;if(rearCrosses){leave=0;for(int i=0;i<8;i++)if(previous[i].z<exit)leave=Mathf.Max(leave,PlaneFraction(previous[i].z,current[i].z,exit));}
 if(state.entered&&!state.invalid)for(int sample=0;sample<=4;sample++)if(!SlabClear(previous,current,Mathf.Lerp(enter,leave,sample*.25f),entrance,exit)){state.invalid=true;break;}
 if(!state.entered||state.invalid||!rearCrosses)return false;fraction=leave;state.entered=false;return true;}
 static float PlaneFraction(float a,float b,float plane)=>Mathf.Clamp01((plane-a)/Mathf.Max(.000001f,b-a));
 static bool SlabClear(Vector3[] a,Vector3[] b,float time,float front,float back){for(int i=0;i<8;i++){var point=Vector3.Lerp(a[i],b[i],time);if(point.z>=front-.0001f&&point.z<=back+.0001f&&!ClearPoint(point))return false;for(int axis=1;axis<=4;axis*=2){int j=i^axis;if(j<i)continue;var other=Vector3.Lerp(a[j],b[j],time);for(int plane=0;plane<2;plane++){float z=plane==0?front:back;if((point.z-z)*(other.z-z)>0||Mathf.Abs(other.z-point.z)<.000001f)continue;var clipped=Vector3.Lerp(point,other,Mathf.Clamp01((z-point.z)/(other.z-point.z)));if(!ClearPoint(clipped))return false;}}}return true;}
 static bool ClearPoint(Vector3 p)=>p.x>=-TunnelHalfWidth-.08f&&p.x<=TunnelHalfWidth+.08f&&p.y>=-.15f&&p.y<=TunnelHeight+.02f;
 static bool Clear(Bounds b)=>b.min.x>=-TunnelHalfWidth-.08f&&b.max.x<=TunnelHalfWidth+.08f&&b.min.y>=-.15f&&b.max.y<=TunnelHeight+.02f;
 public static bool SafeTunnelSide(Vector3 point,Vector3 normal,float entrance)=>Mathf.Abs(Mathf.Abs(point.x)-TunnelHalfWidth)<.12f&&point.z>=entrance-.12f&&point.z<=entrance+TunnelDepth+.12f&&point.y>=-.2f&&point.y<=TunnelHeight+.08f&&Mathf.Abs(normal.x)>.65f&&point.x*normal.x<0;
 public static bool SafeOpeningLip(Vector3 point,Bounds body,float entrance)=>Mathf.Abs(Mathf.Abs(point.x)-TunnelHalfWidth)<.09f&&Mathf.Abs(point.z-entrance)<.12f&&Clear(body);
 public static bool FatalWall(Vector3 incoming,Vector3 normal)=>Mathf.Max(0,-Vector3.Dot(incoming,normal))>=10.5f;
 public static int Stall(int slot,int heat)=>(slot+heat*5)%Racers;
 public static int Winner(float[] times){int winner=-1;float best=float.PositiveInfinity;bool tie=false;for(int i=0;i<Racers;i++){float t=times[i];if(t<best-.0001f){best=t;winner=i;tie=false;}else if(!float.IsInfinity(t)&&Mathf.Abs(t-best)<=.0001f)tie=true;}return tie?-1:winner;}
}
}

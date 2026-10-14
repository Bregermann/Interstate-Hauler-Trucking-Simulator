using UnityEngine;
namespace LWS.TruckWallball {
// Presentation/AI prediction only. Actual collision events remain authoritative.
public static class WallballFlight {
 public static bool Step(ref Vector3 p,ref Vector3 v,float dt,float gravity,float radius,float wallBounce,float floorBounce,float width,float rear,out bool floor){
  floor=false;v.y-=gravity*dt;p+=v*dt;
  if(Mathf.Abs(p.x)>width+radius||p.z< -rear-radius)return false;
  if(p.z> -radius&&v.z>0){if(Mathf.Abs(p.x)>width-.2f||p.y<.55f||p.y>7.5f)return false;p.z=-radius-(p.z+radius);v.z=-v.z*wallBounce;}
  if(p.y<radius&&v.y<0){p.y=radius+(radius-p.y);v.y=-v.y*floorBounce;floor=true;}return true;
 }
 public static bool Landing(Vector3 p,Vector3 v,WallballMatch m,out Vector3 landing){for(int i=0;i<160;i++){if(!Step(ref p,ref v,.025f,m.ball.gravity,m.ball.radius,m.ball.wallBounce,m.ball.floorBounce,m.halfWidth,m.courtLength,out bool floor))break;if(floor){landing=new Vector3(p.x,.035f,p.z);return true;}}landing=default;return false;}
}
}

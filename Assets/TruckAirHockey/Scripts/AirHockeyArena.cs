using UnityEngine;
namespace LWS.TruckAirHockey {
// Shared rounded-table geometry for navigation, recovery and whole-truck containment.
public static class AirHockeyArena {
 public static Vector3 Project(Vector3 p,float length,float width,float corner,float inset){
  p.x=Mathf.Clamp(p.x,-length+inset,length-inset);p.z=Mathf.Clamp(p.z,-width+inset,width-inset);
  float cx=length-corner,cz=width-corner,dx=Mathf.Abs(p.x)-cx,dz=Mathf.Abs(p.z)-cz,r=Mathf.Max(.05f,corner-inset);
  if(dx>0&&dz>0){var d=new Vector2(dx,dz);if(d.magnitude>r){d=d.normalized*r;p.x=Mathf.Sign(p.x)*(cx+d.x);p.z=Mathf.Sign(p.z)*(cz+d.y);}}
  return p;
 }
 public static bool Contains(Vector3 p,float length,float width,float corner,float inset=0){var q=Project(p,length,width,corner,inset);return (q-p).sqrMagnitude<.000004f;}
 public static Vector3 CornerPuck(float length,float width,float corner,float radius,int x,int z){return new Vector3(x*(length-corner),.22f,z*(width-corner))+new Vector3(x,0,z).normalized*(corner-.2f-radius-.06f);}
}
}

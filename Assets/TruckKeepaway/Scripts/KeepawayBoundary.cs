using UnityEngine;
namespace LWS.TruckKeepaway {
// Inner playing surface; shared by native boards, navigation, spawns and recovery.
public static class KeepawayBoundary {
 public const float Expansion=1.7320508f,HalfX=32*Expansion,HalfZ=18*Expansion,Radius=7*Expansion;
 public static float Area=>4*HalfX*HalfZ-(4-Mathf.PI)*Radius*Radius;
 public static bool Contains(Vector3 p,float clearance=0){float r=Radius-clearance;if(r<=0)return false;float x=Mathf.Abs(p.x),z=Mathf.Abs(p.z);if(x>HalfX-clearance||z>HalfZ-clearance)return false;float dx=Mathf.Max(0,x-(HalfX-Radius)),dz=Mathf.Max(0,z-(HalfZ-Radius));return dx*dx+dz*dz<=r*r;}
 public static Vector3 Inside(Vector3 p,float clearance){p.x=Mathf.Clamp(p.x,-HalfX+clearance,HalfX-clearance);p.z=Mathf.Clamp(p.z,-HalfZ+clearance,HalfZ-clearance);float cx=HalfX-Radius,cz=HalfZ-Radius;var d=new Vector2(Mathf.Max(0,Mathf.Abs(p.x)-cx),Mathf.Max(0,Mathf.Abs(p.z)-cz));float r=Radius-clearance;if(d.sqrMagnitude>r*r){d=d.normalized*r;p.x=Mathf.Sign(p.x)*(cx+d.x);p.z=Mathf.Sign(p.z)*(cz+d.y);}return p;}
}
}

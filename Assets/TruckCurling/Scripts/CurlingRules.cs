using UnityEngine;
namespace LWS.TruckCurling {
// Arcade-sized sheet; stone-distance scoring and line/FGZ semantics follow World Curling R2/R6/R12.
public static class CurlingRules {
 public const int Deliveries=16;public const float Radius=1.6f,HouseRadius=4.6f,Tee=52,Hog=37.5f,Back=58.5f,HalfWidth=7;
 public static bool House(Vector3 p)=>new Vector2(p.x,p.z-Tee).magnitude<=HouseRadius+Radius;
 public static bool SideOrBack(Vector3 p)=>Mathf.Abs(p.x)+Radius>=HalfWidth||p.z-Radius>Back;
 public static bool HogQualified(Vector3 p,bool hitStone)=>p.z-Radius>Hog||hitStone;
 public static bool Guard(Vector3 p,bool guardContact)=>!House(p)&&p.z+Radius<Tee&&(p.z-Radius>Hog||guardContact);
 public static bool Protected(int delivery,int team,int ownerTeam,bool guard)=>delivery<5&&team!=ownerTeam&&guard;
 public static int TeamFor(int delivery,int hammer)=>delivery%2==0?1-hammer:hammer;
 public static int Score(Vector3[] positions,int[] teams,bool[] valid,int count,out int scoring){float a=float.PositiveInfinity,b=float.PositiveInfinity;for(int i=0;i<count;i++)if(valid[i]&&House(positions[i])){float d=new Vector2(positions[i].x,positions[i].z-Tee).magnitude;if(teams[i]==0)a=Mathf.Min(a,d);else b=Mathf.Min(b,d);}scoring=-1;if(float.IsPositiveInfinity(a)&&float.IsPositiveInfinity(b)||Mathf.Abs(a-b)<=.001f)return 0;scoring=a<b?0:1;float opposing=scoring==0?b:a;int points=0;for(int i=0;i<count;i++)if(valid[i]&&teams[i]==scoring&&House(positions[i])&&new Vector2(positions[i].x,positions[i].z-Tee).magnitude<opposing-.001f)points++;return points;}
 public static int HammerAfter(int oldHammer,int scoring,int points)=>points>0?1-scoring:oldHammer;
}
}

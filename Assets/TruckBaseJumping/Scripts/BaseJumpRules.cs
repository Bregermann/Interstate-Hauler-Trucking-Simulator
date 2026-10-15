using System;
using UnityEngine;
namespace LWS.TruckBaseJumping {
public struct BaseLanding {public bool valid,inside;public int errorMillimetres,timeMilliseconds,placement,points;public float verticalSpeed,planarSpeed,up;public int Band=>errorMillimetres/1000;}
public static class BaseJumpRules {
 public static int Compare(BaseLanding a,BaseLanding b){if(a.valid!=b.valid)return a.valid?-1:1;if(!a.valid)return 0;if(a.inside!=b.inside)return a.inside?-1:1;int d=a.Band.CompareTo(b.Band);if(d!=0)return d;d=a.timeMilliseconds.CompareTo(b.timeMilliseconds);if(d!=0)return d;return a.errorMillimetres.CompareTo(b.errorMillimetres);}
 public static void Rank(BaseLanding[] results,int[] order){for(int i=0;i<4;i++)order[i]=i;for(int i=1;i<4;i++){int item=order[i],j=i-1;while(j>=0&&Compare(results[item],results[order[j]])<0){order[j+1]=order[j];j--;}order[j+1]=item;}int previous=0;for(int i=0;i<4;i++){var r=results[order[i]];int place=i>0&&Compare(r,results[order[i-1]])==0?previous:i+1;r.placement=place;r.points=r.valid?5-place:0;results[order[i]]=r;previous=place;}}
 public static int Winner(int[] totals,int[] errors,int[] times){int best=0;bool tied=false;for(int i=1;i<4;i++){int c=totals[i].CompareTo(totals[best]);if(c==0)c=errors[best].CompareTo(errors[i]);if(c==0)c=times[best].CompareTo(times[i]);if(c>0){best=i;tied=false;}else if(c==0)tied=true;}return tied?-1:best;}
 public static string Distance(int millimetres){double total=millimetres/25.4;int feet=(int)Math.Floor(total/12);double inches=Math.Round(total-feet*12,2);if(inches>=12){feet++;inches=0;}return feet+"′ "+inches.ToString("F2")+"″";}
 public static bool Safe(bool deployed,float opening,Vector3 incoming,Vector3 up)=>deployed&&opening>=.8f&&-incoming.y<=8&&new Vector2(incoming.x,incoming.z).magnitude<=12&&Vector3.Dot(up,Vector3.up)>=.75f;
}
}

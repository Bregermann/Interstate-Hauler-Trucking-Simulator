using UnityEngine;
namespace LWS.TruckGiantSwing {
public static class SwingRules {
 // Closed outer boundaries; smaller precision zones win overlapping boundaries.
 public readonly struct Target{public readonly Vector2 center,size;public readonly int score;public readonly bool round;public Target(float x,float z,float w,float h,int points,bool circular){center=new Vector2(x,z);size=new Vector2(w,h);score=points;round=circular;}}
 static readonly Target[,] layouts={{new Target(0,29,28,14,10,false),new Target(-5,46,12,12,25,true),new Target(6,58,12,14,50,false),new Target(3,51,4.6f,4.6f,100,true)},{new Target(0,30,28,14,10,false),new Target(6,44,14,14,25,true),new Target(-5,57,12,16,50,false),new Target(-3,50,5,5,100,true)},{new Target(0,27,30,16,10,false),new Target(-7,44,14,14,25,true),new Target(7,56,14,16,50,false),new Target(1,48,5,5,100,true)}};
 public static Target GetTarget(int layout,int target)=>layouts[Mathf.Clamp(layout,0,2),target];
 public static int Zone(Vector3 point)=>Zone(point,0);public static int Zone(Vector3 point,int layout){var p=new Vector2(point.x,point.z);for(int i=3;i>=0;i--){var t=GetTarget(layout,i);var d=p-t.center;if(t.round?d.sqrMagnitude<=t.size.x*t.size.x*.25f+.0001f:Mathf.Abs(d.x)<=t.size.x*.5f&&Mathf.Abs(d.y)<=t.size.y*.5f)return t.score;}return 0;}
 public static int Leaders(int[] points,int count,int eligible){int mask=0,best=int.MinValue;for(int i=0;i<count;i++)if((eligible&(1<<i))!=0){if(points[i]>best){best=points[i];mask=1<<i;}else if(points[i]==best)mask|=1<<i;}return mask;}
 public static int Count(int mask){int n=0;for(;mask!=0;mask&=mask-1)n++;return n;}
 public const float Length=20,EnergyCap=3500,Preparation=35,Aftertouch=1.6f;
}
}

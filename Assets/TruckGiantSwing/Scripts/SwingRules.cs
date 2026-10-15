using UnityEngine;
namespace LWS.TruckGiantSwing {
public static class SwingRules {
 // Closed outer boundaries; smaller precision zones win overlapping boundaries.
 public static int Zone(Vector3 point){var p=new Vector2(point.x,point.z);if((p-new Vector2(3,51)).sqrMagnitude<=2.3f*2.3f+.0001f)return 100;if(Mathf.Abs(p.x-6)<=6&&Mathf.Abs(p.y-58)<=7)return 50;if((p-new Vector2(-5,46)).sqrMagnitude<=36)return 25;if(Mathf.Abs(p.x)<=14&&Mathf.Abs(p.y-29)<=7)return 10;return 0;}
 public static int Leaders(int[] points,int count,int eligible){int mask=0,best=int.MinValue;for(int i=0;i<count;i++)if((eligible&(1<<i))!=0){if(points[i]>best){best=points[i];mask=1<<i;}else if(points[i]==best)mask|=1<<i;}return mask;}
 public static int Count(int mask){int n=0;for(;mask!=0;mask&=mask-1)n++;return n;}
 public const float Length=20,EnergyCap=3500,Preparation=35,Aftertouch=1.6f;
}
}

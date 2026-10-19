using UnityEngine;
namespace LWS.TruckSnake {
 // Arc-length history, including elevation. O(log n) sampling; no per-tick
 // allocations and no 50-body dynamic joint chain. Discontinuities must reset.
 public sealed class SnakePathHistory {
  public struct Sample {public Vector3 position;public Quaternion rotation;public double distance;}
  readonly Sample[] samples;int first,count;double distance;
  public int Count=>count;public int Capacity=>samples.Length;public double Distance=>distance;
  public double Available=>count<2?0:distance-At(0).distance;
  public SnakePathHistory(int capacity=32768){if(capacity<4)throw new System.ArgumentOutOfRangeException();samples=new Sample[capacity];}
  Sample At(int index)=>samples[(first+index)%samples.Length];
  public void Reset(Vector3 position,Quaternion rotation,float clearStraightBehind=0){first=count=0;distance=0;Vector3 direction=rotation*Vector3.forward;int seed=Mathf.CeilToInt(Mathf.Max(0,clearStraightBehind)/.5f);for(int i=seed;i>=0;i--)Append(position-direction*i*.5f,rotation,0);}
  public bool Append(Vector3 position,Quaternion rotation,float minimumStep=.35f){
   if(!Finite(position))throw new System.ArgumentException("Path sample must be finite.");
   if(count>0){float step=Vector3.Distance(position,At(count-1).position);if(step<Mathf.Max(.0001f,minimumStep))return false;distance+=step;}
   int index;if(count==samples.Length){index=first;first=(first+1)%samples.Length;}else index=(first+count++)%samples.Length;
   samples[index]=new Sample{position=position,rotation=rotation,distance=distance};return true;
  }
  public bool TryBehind(float behind,out Sample sample){
   if(behind<0||float.IsNaN(behind)||count==0||behind>Available+.001){sample=default;return false;}
   double target=distance-behind;int lo=0,hi=count-1;
   while(lo+1<hi){int mid=(lo+hi)/2;if(At(mid).distance<=target)lo=mid;else hi=mid;}
   var a=At(lo);var b=At(hi);float t=b.distance>a.distance?(float)((target-a.distance)/(b.distance-a.distance)):0;
   sample=new Sample{position=Vector3.Lerp(a.position,b.position,t),rotation=Quaternion.Slerp(a.rotation,b.rotation,t),distance=target};return true;
  }
  public void Trim(float retainedMetres){while(count>2&&distance-At(1).distance>retainedMetres){first=(first+1)%samples.Length;count--;}}
  static bool Finite(Vector3 v)=>!(float.IsNaN(v.x)||float.IsNaN(v.y)||float.IsNaN(v.z)||float.IsInfinity(v.x)||float.IsInfinity(v.y)||float.IsInfinity(v.z));
 }
}

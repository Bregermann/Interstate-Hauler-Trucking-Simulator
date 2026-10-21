using System;
using UnityEngine;
namespace LWS.TruckFrogger
{
 public sealed class FrogWorld:MonoBehaviour
 {
  [Serializable]public sealed class Course {public string title;public Transform root,actors;public Vector3 start,finish;public float halfWidth=24;public Vector3[] gates;public Vector2[] water;public FrogMover[] movers;public FrogTraffic traffic;}
  public Course[] courses;public int Current{get;private set;}
  public void Prepare(int course,int seed){Current=course;for(int i=0;i<courses.Length;i++)courses[i].root.gameObject.SetActive(i==course);var c=courses[course];c.traffic?.ResetTraffic(seed);foreach(var mover in c.movers)mover.ResetMotion(seed);Physics.SyncTransforms();}
  public void Step(float age,bool driving,float grip){var c=courses[Current];foreach(var mover in c.movers)mover.Step(age);c.traffic?.Step(driving,grip);}
  public bool Water(Vector3 p){var c=courses[Current];foreach(var range in c.water)if(p.z>range.x&&p.z<range.y&&p.y< -1.8f)return true;return false;}
  public float CrossingEnd(int gate){var c=courses[Current];float z=c.gates[gate].z;return c.traffic==null?z+18:Mathf.Max(z+18,c.traffic.CrossingEnd(z));}
  // Predict moving-solid overlap with the real crossing corridor. Waiting is
  // based on observed physical movers/traffic; it never awards a result.
  public bool SafeToCross(int gate,float now,float seconds)
  {
   var c=courses[Current];float z=c.gates[gate].z;foreach(var mover in c.movers)
   {
    if(Mathf.Abs(mover.origin.z-z)>35)continue;
    if(mover.platform){for(float t=0;t<=seconds;t+=.2f)if(Mathf.Abs(mover.At(now+t).x-c.start.x)>mover.halfWidth-4.5f)return false;}
    else for(float t=0;t<=seconds;t+=.2f){var p=mover.At(now+t);if(Mathf.Abs(p.x-c.start.x)<mover.halfWidth+5&&Mathf.Abs(p.z-z)<20)return false;}
   }
   return c.traffic==null||c.traffic.SafeGap(c.start.x,z,seconds);
  }
 }
}

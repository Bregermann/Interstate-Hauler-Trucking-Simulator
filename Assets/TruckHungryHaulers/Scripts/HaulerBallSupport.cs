using System.Collections.Generic;
using UnityEngine;
namespace LWS.TruckHungryHaulers
{
 // Ball-only authored collision surfaces. Original NWH court/terrain stay intact.
 [DefaultExecutionOrder(30000)]public sealed class HaulerBallSupport:MonoBehaviour
 {
  public HaulerMatch match;public GameObject[] layouts;public Collider[] originalFloors;public Collider[] pitColliders;
  readonly List<Collider> truckShapes=new List<Collider>();readonly HashSet<Collider> boundTrucks=new HashSet<Collider>();Collider[] activeSupport;float refreshAt;
  public void Select(int players)
  {
   for(int i=0;i<layouts.Length;i++)layouts[i].SetActive(i==players-1);
   var support=new List<Collider>(pitColliders);support.AddRange(layouts[players-1].GetComponentsInChildren<Collider>(true));activeSupport=support.ToArray();boundTrucks.Clear();
   foreach(var ball in match.balls)BindBall(ball);
   BindGeneratedTruckShapes();refreshAt=0;
  }
  public void BindBall(HaulerBall ball){foreach(var floor in originalFloors)Physics.IgnoreCollision(ball.shape,floor,true);}
  public void BindGeneratedTruckShapes()
  {
   if(activeSupport==null)return;
   foreach(var truck in match.trucks)
   {
    truck.GetComponentsInChildren(true,truckShapes);
    foreach(var shape in truckShapes)if(shape!=null){if(!shape.enabled||!shape.gameObject.activeInHierarchy){boundTrucks.Remove(shape);continue;}if(boundTrucks.Add(shape))foreach(var support in activeSupport)if(support!=null&&support.enabled&&support.gameObject.activeInHierarchy)Physics.IgnoreCollision(shape,support,true);}
   }
  }
  void FixedUpdate(){if(match!=null&&match.Live&&Time.fixedTime>=refreshAt){refreshAt=Time.fixedTime+.25f;BindGeneratedTruckShapes();}}
 }
}

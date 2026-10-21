using System;
using UnityEngine;
using LWS.InterstateHauler;
namespace LWS.TruckFrogger
{
 // UTS owns traffic steering, wheels and collision avoidance. This adapter
 // freezes/resets equivalent starting conditions for alternating competition.
 public sealed class FrogTraffic:MonoBehaviour
 {
  public GameObject[] prefabs;public LwsTrafficLaneDefinition[] lanes;public Transform fleet;
  CarAIController[] cars;MovePath[] paths;Rigidbody[] bodies;WheelCollider[][] wheels;RigidbodyConstraints[] constraints;Vector3[] origins;Quaternion[] rotations;int[] starts;bool released;
  public int Count=>cars==null?0:cars.Length;
  public void Ensure()
  {
   if(cars!=null)return;var api=new LwsUtsTrafficApi();if(!api.IsAvailable)throw new InvalidOperationException(api.AvailabilitySummary);var list=new System.Collections.Generic.List<CarAIController>();var indices=new System.Collections.Generic.List<int>();var policy=new LwsTrafficSpawnPolicy{targetCruiseSpeedScale=1,maximumTrafficSpeedMetersPerSecond=22};
   foreach(var lane in lanes){var owner=new GameObject("Existing UTS crossing lane "+lane.laneId);owner.transform.SetParent(transform,true);var path=api.CreatePath(owner,lane,prefabs,policy,out var message);if(path==null)throw new InvalidOperationException(message);for(int i=0;i<4;i++){int point=1+i*6;var go=api.SpawnVehicle(prefabs[i%prefabs.Length],path,lane,point,fleet,policy,out message);if(go==null)throw new InvalidOperationException(message);var car=go.GetComponentInChildren<CarAIController>();car.enabled=false;car.INCREASE=3;car.DECREASE=10;list.Add(car);indices.Add(point);}}
   cars=list.ToArray();starts=indices.ToArray();paths=new MovePath[cars.Length];bodies=new Rigidbody[cars.Length];wheels=new WheelCollider[cars.Length][];constraints=new RigidbodyConstraints[cars.Length];origins=new Vector3[cars.Length];rotations=new Quaternion[cars.Length];for(int i=0;i<cars.Length;i++){paths[i]=cars[i].GetComponent<MovePath>();bodies[i]=cars[i].GetComponent<Rigidbody>();wheels[i]=cars[i].GetComponentsInChildren<WheelCollider>();constraints[i]=bodies[i].constraints;origins[i]=bodies[i].position;rotations[i]=bodies[i].rotation;}
  }
  public void ResetTraffic(int seed)
  {
   Ensure();released=false;for(int i=0;i<cars.Length;i++)
   {
    // Fresh vendor controller state prevents private cruise/turn timers from
    // leaking between scored turns. Only a behavior component is renewed;
    // pooled car bodies/render hierarchies and replay references remain stable.
    var old=cars[i];old.enabled=false;Destroy(old);var car=bodies[i].gameObject.AddComponent<CarAIController>();car.enabled=false;car.MOVE_SPEED=lanes[i/4].speedLimitMph*.44704f;car.INCREASE=3;car.DECREASE=10;car.TO_CAR=18;car.TO_SEMAPHORE=18;car.MaxAngle=8;car.allow=VehiclesAllow.Forward;car.tempStop=true;
    foreach(var box in car.GetComponentsInChildren<BoxCollider>())if(!box.isTrigger&&(car.bc==null||box.bounds.size.sqrMagnitude>car.bc.bounds.size.sqrMagnitude))car.bc=box;if(car.bc==null)throw new InvalidOperationException("UTS solid car collider missing");car.bcSize=car.bc.bounds.size;cars[i]=car;
    bodies[i].constraints=RigidbodyConstraints.FreezeAll;bodies[i].linearVelocity=bodies[i].angularVelocity=Vector3.zero;bodies[i].position=origins[i];bodies[i].rotation=rotations[i];bodies[i].transform.SetPositionAndRotation(origins[i],rotations[i]);paths[i].InitStartPosition(0,starts[i],true,true);foreach(var w in wheels[i]){w.enabled=true;w.motorTorque=w.brakeTorque=0;}
   }
  }
  public void Step(bool driving,float grip){if(cars==null)return;for(int i=0;i<cars.Length;i++){cars[i].tempStop=!driving;cars[i].MOVE_SPEED=Mathf.Clamp(lanes[i/4].speedLimitMph*.44704f*grip,7,22);if(driving&&!released){bodies[i].linearVelocity=bodies[i].angularVelocity=Vector3.zero;bodies[i].constraints=constraints[i];cars[i].enabled=true;}}if(driving)released=true;}
  // A UTS road is a two-way loop: the returning lane is 28m beyond the
  // near lane. Both the safety prediction and rear-body gate must include it.
  public float CrossingEnd(float z){float end=z;foreach(var lane in lanes)if(Mathf.Abs(lane.centerline[0].z-z)<1)foreach(var p in lane.centerline)end=Mathf.Max(end,p.z+6);return end;}
  public static bool PredictedCrossingOverlap(Vector3 position,Vector3 velocity,float x,float near,float far,float seconds){for(float t=0;t<=seconds;t+=.15f){var p=position+velocity*t;if(Mathf.Abs(p.x-x)<12&&p.z>near&&p.z<far)return true;}return false;}
  public bool SafeGap(float x,float z,float seconds){if(cars==null)return false;float end=CrossingEnd(z);for(int i=0;i<cars.Length;i++){if(Mathf.Abs(lanes[i/4].centerline[0].z-z)>1)continue;var body=bodies[i];if(PredictedCrossingOverlap(body.position,body.linearVelocity,x,z-7,end,seconds))return false;}return true;}
 }
}

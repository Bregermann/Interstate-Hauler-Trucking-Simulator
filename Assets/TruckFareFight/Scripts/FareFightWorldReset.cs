using System;
using UnityEngine;
namespace LWS.TruckFareFight {
 public sealed class FareFightWorldReset:MonoBehaviour {
  [Serializable]public struct Prop {public Rigidbody body;public Vector3 position;public Quaternion rotation;}
  public Prop[] props;
  public void Restore(){foreach(var prop in props)if(prop.body!=null){prop.body.position=prop.position;prop.body.rotation=prop.rotation;if(!prop.body.isKinematic)prop.body.linearVelocity=prop.body.angularVelocity=Vector3.zero;prop.body.WakeUp();}}
 }
}

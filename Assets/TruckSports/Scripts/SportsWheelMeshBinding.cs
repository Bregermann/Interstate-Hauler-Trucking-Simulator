using UnityEngine;
namespace LWS.TruckSports {
public sealed class SportsWheelMeshBinding:MonoBehaviour {
 public SportsEventTruck truck;public Transform[] meshes;public Vector3[] centers;public Quaternion[] alignment;
 void LateUpdate(){for(int i=0;i<meshes.Length;i++){var physical=truck.wheels[i].wheel.rotatingContainer;if(physical==null)continue;meshes[i].rotation=physical.rotation*alignment[i];meshes[i].position=physical.position-meshes[i].TransformVector(centers[i]);}}
}
}

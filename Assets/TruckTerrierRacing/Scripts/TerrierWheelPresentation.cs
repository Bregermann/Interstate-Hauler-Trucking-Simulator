using UnityEngine;
namespace LWS.TruckTerrierRacing {
// Retarget the existing GR3D meshes to NWH's authoritative wheel pose without another wheel simulation.
public sealed class TerrierWheelPresentation:MonoBehaviour {
 public TerrierRacer racer;public Transform[] meshes;public Vector3[] centers;public Quaternion[] alignment;
 void LateUpdate(){for(int i=0;i<meshes.Length;i++){var pose=racer.truck.wheels[i].wheel.rotatingContainer;if(pose==null)continue;var rotation=pose.rotation*alignment[i];meshes[i].rotation=rotation;meshes[i].position=pose.position-rotation*Vector3.Scale(centers[i],meshes[i].lossyScale);}}
}
}

using UnityEngine;
namespace LWS.TruckTargetLaunch {
public sealed class TargetLavaSurface:MonoBehaviour {
 public TargetVolcano volcano;
 void OnTriggerEnter(Collider other)=>volcano.Contact(other);
}
}

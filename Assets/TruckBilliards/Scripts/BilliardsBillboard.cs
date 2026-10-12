using UnityEngine;
namespace LWS.TruckBilliards {
public sealed class BilliardsBillboard:MonoBehaviour {
 public Camera cameraTarget;
 void LateUpdate(){if(cameraTarget!=null)transform.rotation=cameraTarget.transform.rotation;}
}}

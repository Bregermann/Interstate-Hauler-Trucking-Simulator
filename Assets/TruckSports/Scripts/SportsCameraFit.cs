using UnityEngine;
namespace LWS.TruckSports {
public sealed class SportsCameraFit:MonoBehaviour {public Camera cameraTarget;public float worldWidth=30,worldHeight=20;float aspect;
 void LateUpdate(){if(Mathf.Abs(cameraTarget.aspect-aspect)<.0001f)return;aspect=cameraTarget.aspect;cameraTarget.orthographicSize=Mathf.Max(worldHeight*.5f,worldWidth/(2*aspect));}}
}

using UnityEngine;
using UnityEngine.Rendering;
namespace LWS.TruckBowling {
public sealed class BowlingCabCamera:MonoBehaviour {
 public Camera view;public BowlingLane lane;public Renderer[] exterior,interior;public float Impact;float yaw;
 void OnEnable(){RenderPipelineManager.beginCameraRendering+=Begin;RenderPipelineManager.endCameraRendering+=End;}
 void OnDisable(){RenderPipelineManager.beginCameraRendering-=Begin;RenderPipelineManager.endCameraRendering-=End;Restore();}
 void Begin(ScriptableRenderContext context,Camera camera){foreach(var r in exterior)if(r!=null)r.forceRenderingOff=camera==view;foreach(var r in interior)if(r!=null)r.forceRenderingOff=camera!=view;}
 void End(ScriptableRenderContext context,Camera camera){Restore();}void Restore(){if(exterior!=null)foreach(var r in exterior)if(r!=null)r.forceRenderingOff=false;if(interior!=null)foreach(var r in interior)if(r!=null)r.forceRenderingOff=false;}
 public void ResetView(){yaw=lane.truck.transform.eulerAngles.y;Impact=0;}
 void LateUpdate(){if(lane==null||!view.enabled)return;bool reduced=lane.match.setup.reducedMotion;var body=lane.truck.body;float target=body.rotation.eulerAngles.y;yaw=Mathf.MoveTowardsAngle(yaw,target,(reduced?180:540)*Time.deltaTime);Impact=Mathf.MoveTowards(Impact,0,Time.deltaTime*.12f);float shake=Impact*(reduced?.12f:1)*Mathf.Sin(Time.time*43);transform.position=body.position+Vector3.up*.43f+body.rotation*Vector3.forward*.05f+Vector3.up*shake;transform.rotation=Quaternion.Euler(1,yaw,0);}
}
}
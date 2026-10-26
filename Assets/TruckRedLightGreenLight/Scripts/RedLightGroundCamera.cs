using Cinemachine;
using LWS.TruckSports;
using UnityEngine;

namespace LWS.TruckRedLightGreenLight
{
 // One mode constraint inside the existing Cinemachine evaluation, not a
 // second transform writer after Present or the shared broadcast controller.
 public sealed class RedLightGroundCamera:CinemachineExtension
 {
  public RedLightMatch match;public Collider[] ground;
  [Range(.65f,1.3f)]public float eyeHeight=.95f;
  [Range(60,75)]public float fieldOfView=70;
  SportsBroadcastFraming framing;SportsBroadcastSettings originalSettings;bool originalDefaults,originalOrtho,configured;
  public float GroundHeight{get;private set;}public int FramedRacers{get;private set;}public bool AllBodiesVisible{get;private set;}
  public int AppliedFrame{get;private set;}=-1;
  public bool OwnsGroundShot=>enabled&&match!=null&&match.Running&&!match.Finished&&!match.ReplayControls&&match.drivingViews!=null&&match.drivingViews.NeedsArena&&match.broadcast.view.enabled;
  public void Configure(RedLightMatch owner,Collider[] surfaces)
  {
   RestoreSettings();match=owner;ground=surfaces;framing=owner.broadcast.broadcastFraming;
   if(framing==null)throw new System.InvalidOperationException("Red Light ground shot requires its existing shared Cinemachine framing binding.");
   originalSettings=framing.settings;originalDefaults=framing.useSharedDefaults;originalOrtho=framing.useOrthographic;
   var local=JsonUtility.FromJson<SportsBroadcastSettings>(JsonUtility.ToJson(framing.Configuration));
   // The all-field ground shot intentionally includes distant competitors;
   // the collection's close-subject crop would otherwise lift/crop this shot.
   local.minimumTruckHeight=.005f;local.preferredTruckHeight=.025f;local.maximumDistanceInTruckLengths=50;local.fieldOfView=fieldOfView;
   framing.settings=local;framing.useSharedDefaults=false;framing.useOrthographic=false;configured=true;
  }
  protected override void OnEnable(){base.OnEnable();if(match!=null&&!configured)Configure(match,ground);}
  void RestoreSettings(){if(configured&&framing!=null){framing.settings=originalSettings;framing.useSharedDefaults=originalDefaults;framing.useOrthographic=originalOrtho;}configured=false;}
  void OnDisable()=>RestoreSettings();
  protected override void OnDestroy(){RestoreSettings();base.OnDestroy();}
  float Floor(Vector3 position)
  {
   float floor=0;bool found=false;var ray=new Ray(position+Vector3.up*1000,Vector3.down);
   // Explicit ground colliders only: never jump onto a truck, mast, master,
   // grandstand or another static prop encountered by a general physics ray.
   if(ground!=null)foreach(var surface in ground)if(surface!=null&&surface.enabled&&surface.gameObject.activeInHierarchy&&surface.attachedRigidbody==null&&surface.Raycast(ray,out var hit,2000)){if(!found||hit.point.y>floor)floor=hit.point.y;found=true;}
   return floor;
  }
  static bool Inside(Vector3 point,Vector3 position,Quaternion inverse,float tangent,float aspect)
  {
   var local=inverse*(point-position);return local.z>.1f&&Mathf.Abs(local.x)<local.z*tangent*aspect*.94f&&Mathf.Abs(local.y)<local.z*tangent*.94f;
  }
  protected override void PostPipelineStageCallback(CinemachineVirtualCameraBase vcam,CinemachineCore.Stage stage,ref CameraState state,float deltaTime)
  {
   if(stage!=CinemachineCore.Stage.Finalize||!OwnsGroundShot)return;
   bool first=true;Bounds bounds=default;FramedRacers=0;
   for(int i=0;i<match.setup.count;i++)if(match.rules.outcomes[i]==RedLightOutcome.Racing){var body=match.trucks[i].shape.bounds;if(first){bounds=body;first=false;}else bounds.Encapsulate(body);FramedRacers++;}
   if(first)return;
   float tangent=Mathf.Tan(fieldOfView*Mathf.Deg2Rad*.5f),aspect=Mathf.Max(.5f,match.broadcast.view.aspect);var aim=new Vector3(bounds.center.x,6,match.finishZ+10);Vector3 position=default;Quaternion rotation=Quaternion.identity;AllBodiesVisible=false;
   float distance=Mathf.Max(45,bounds.size.x/(2*tangent*aspect)*1.15f);
   for(int attempt=0;attempt<20;attempt++)
   {
    position=new Vector3(bounds.center.x,0,bounds.min.z-distance);GroundHeight=Floor(position);position.y=GroundHeight+eyeHeight;rotation=Quaternion.LookRotation(aim-position,Vector3.up);var inverse=Quaternion.Inverse(rotation);bool fits=true;
    for(int i=0;i<match.setup.count&&fits;i++)if(match.rules.outcomes[i]==RedLightOutcome.Racing){var body=match.trucks[i].shape.bounds;for(int corner=0;corner<8;corner++){var point=body.center+Vector3.Scale(body.extents,new Vector3((corner&1)==0?-1:1,(corner&2)==0?-1:1,(corner&4)==0?-1:1));if(!Inside(point,position,inverse,tangent,aspect)){fits=false;break;}}}
    if(fits){AllBodiesVisible=true;break;}distance+=8;
   }
   state.RawPosition=position;state.PositionCorrection=Vector3.zero;state.RawOrientation=rotation;state.OrientationCorrection=Quaternion.identity;state.ReferenceLookAt=aim;state.ReferenceUp=Vector3.up;var lens=state.Lens;lens.ModeOverride=LensSettings.OverrideModes.Perspective;lens.FieldOfView=fieldOfView;state.Lens=lens;AppliedFrame=Time.frameCount;
  }
 }
}

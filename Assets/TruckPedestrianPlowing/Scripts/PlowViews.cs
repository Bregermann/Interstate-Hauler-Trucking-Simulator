using UnityEngine;
using UnityEngine.Rendering;
using LWS.TruckSports;
using LWS.TruckBowling;
namespace LWS.TruckPedestrianPlowing {
 [DefaultExecutionOrder(300)]public sealed class PlowViews:MonoBehaviour {
  public PlowMatch match;public SportsRallyViews rally;public Transform[] cabRoots;
  int mode=-1;SportsCameraFit fit;readonly SportsCabLook[] looks=new SportsCabLook[4];readonly Quaternion[] headings=new Quaternion[4];
  Renderer[][] exteriors;Renderer[] hidden;bool[] priorHidden;Camera hiddenCamera;
  void OnEnable(){RenderPipelineManager.beginCameraRendering+=BeginCamera;RenderPipelineManager.endCameraRendering+=EndCamera;}
  void OnDisable(){RenderPipelineManager.beginCameraRendering-=BeginCamera;RenderPipelineManager.endCameraRendering-=EndCamera;RestoreExterior();}
  void BeginCamera(ScriptableRenderContext context,Camera camera){if(mode!=2||match.ReplayControls||exteriors==null)return;for(int v=0;v<rally.views.Length;v++){if(rally.views[v].view!=camera||!camera.enabled)continue;RestoreExterior();hidden=exteriors[Owner(v)];hiddenCamera=camera;for(int i=0;i<hidden.Length;i++){priorHidden[i]=hidden[i].forceRenderingOff;hidden[i].forceRenderingOff=true;}break;}}
  void EndCamera(ScriptableRenderContext context,Camera camera){if(camera==hiddenCamera)RestoreExterior();}
  void RestoreExterior(){if(hidden!=null)for(int i=0;i<hidden.Length;i++)if(hidden[i]!=null)hidden[i].forceRenderingOff=priorHidden[i];hidden=null;hiddenCamera=null;}
  void Awake(){exteriors=new Renderer[match.trucks.Length][];int capacity=0;for(int i=0;i<exteriors.Length;i++){exteriors[i]=match.trucks[i].visuals.GetComponentsInChildren<Renderer>(true);capacity=Mathf.Max(capacity,exteriors[i].Length);}priorHidden=new bool[capacity];fit=rally.broadcast.gameObject.AddComponent<SportsCameraFit>();fit.cameraTarget=rally.broadcast.view;fit.arenaFit=true;fit.topInset=.12f;fit.bottomInset=.2f;for(int i=0;i<4;i++){looks[i]=rally.views[i].GetComponent<SportsCabLook>();cabRoots[i].SetParent(null,true);headings[i]=Quaternion.identity;}}
  public int Owner(int view)=>match.option==0?-1:rally.Humans==0?0:rally.owners[view];
  public void Layout(){mode=match.option;fit.enabled=mode==0;rally.Layout();foreach(var camera in rally.views)camera.view.orthographic=false;rally.broadcast.view.orthographic=mode==0;
   if(mode==0){foreach(var c in rally.views)c.view.enabled=false;rally.broadcast.view.enabled=true;rally.broadcast.view.rect=new Rect(0,0,1,1);match.cameras=new[]{rally.broadcast.view};}
   else if(rally.Humans==0){rally.broadcast.view.enabled=false;rally.views[0].view.enabled=true;rally.views[0].view.rect=new Rect(0,0,1,1);match.cameras=new[]{rally.views[0].view};}
   for(int i=0;i<4;i++){cabRoots[i].gameObject.SetActive(mode==2&&rally.views[i].view.enabled);looks[i].ResetLook();if(rally.views[i].view.enabled&&mode!=0){var forward=Vector3.ProjectOnPlane(match.trucks[Owner(i)].body.rotation*Vector3.forward,Vector3.up);headings[i]=forward.sqrMagnitude>.01f?Quaternion.LookRotation(forward):Quaternion.identity;}}if(rally.weather!=null){rally.weather.vendor.AllowCameras.Clear();foreach(var camera in match.cameras)rally.weather.vendor.AllowCameras.Add(camera);}
  }
  public void ReplayView(Vector3 focus,int angle){fit.enabled=false;rally.broadcast.view.orthographic=false;foreach(var cab in cabRoots)cab.gameObject.SetActive(false);focus.y=Mathf.Max(.8f,focus.y);rally.ReplayView(focus,angle);if(match.cameras.Length!=1||match.cameras[0]!=rally.broadcast.view)match.cameras=new[]{rally.broadcast.view};}
  void LateUpdate(){Tick(Time.deltaTime);}
  void ContainGround(Camera camera){
   var ground=new Plane(Vector3.up,Vector3.zero);var min=new Vector2(float.MaxValue,float.MaxValue);var max=new Vector2(float.MinValue,float.MinValue);
   for(int i=0;i<4;i++){var ray=camera.ViewportPointToRay(new Vector3(i%2,i/2==0?.2f:.88f,0));if(!ground.Raycast(ray,out float distance))return;var point=ray.GetPoint(distance);var xz=new Vector2(point.x,point.z);min=Vector2.Min(min,xz);max=Vector2.Max(max,xz);}
   var bounds=match.arenaBounds;float dx=max.x-min.x>bounds.size.x?bounds.center.x-(min.x+max.x)*.5f:min.x<bounds.min.x?bounds.min.x-min.x:max.x>bounds.max.x?bounds.max.x-max.x:0;
   float dz=max.y-min.y>bounds.size.z?bounds.center.z-(min.y+max.y)*.5f:min.y<bounds.min.z?bounds.min.z-min.y:max.y>bounds.max.z?bounds.max.z-max.y:0;
   camera.transform.position+=new Vector3(dx,0,dz);
  }
  public void Tick(float dt){if(match.ReplayControls)return;if(mode!=match.option)Layout();if(mode==0){var c=rally.broadcast.view;Bounds bounds=default;bool first=true;for(int i=0;i<match.setup.count;i++){if(!match.AllAI&&match.setup.players[i].source==BowlingSource.AI)continue;var p=match.trucks[i].body.position;if(first){bounds=new Bounds(p,Vector3.one*36);first=false;}else bounds.Encapsulate(new Bounds(p,Vector3.one*36));}bounds.size=new Vector3(Mathf.Min(144,bounds.size.x),12,Mathf.Min(144,bounds.size.z));Vector3 focus=bounds.center;focus.y=0;c.transform.SetPositionAndRotation(focus+new Vector3(0,118,-66),Quaternion.Euler(61,0,0));c.orthographic=true;fit.arenaBounds=bounds;fit.Apply();ContainGround(c);return;}
   rally.Follow(dt);int count=rally.Humans==0?1:rally.Humans;
   for(int v=0;v<count;v++){int slot=Owner(v);var t=match.trucks[slot];var camera=rally.views[v].view;if(mode==1&&rally.Humans==0)rally.views[v].Pose(t.body.position+t.transform.forward*5,new Vector3(0,9,-17),70);if(mode!=2)continue;
    var f=Vector3.ProjectOnPlane(t.transform.forward,Vector3.up);if(f.sqrMagnitude>.01f&&Vector3.Dot(t.transform.up,Vector3.up)>.2f)headings[v]=Quaternion.Slerp(headings[v],Quaternion.LookRotation(f),1-Mathf.Exp(-dt*9));
    camera.transform.position=t.body.position+headings[v]*new Vector3(-.17f,1.3f,.65f);cabRoots[v].SetPositionAndRotation(camera.transform.position,headings[v]);var player=match.setup.players[slot];camera.transform.rotation=looks[v].Apply(headings[v],player,slot,v==0,match.Live,SportsLookOptions.Sensitivity,SportsLookOptions.InvertY);camera.fieldOfView=78;camera.GetComponent<SportsEventCamera>().cabSteering.Bind(t);
   }
  }
 }
}

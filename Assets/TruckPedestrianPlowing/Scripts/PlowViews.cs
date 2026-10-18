using UnityEngine;
using LWS.TruckSports;
using LWS.TruckBowling;
namespace LWS.TruckPedestrianPlowing {
 [DefaultExecutionOrder(300)]public sealed class PlowViews:MonoBehaviour {
  public PlowMatch match;public SportsRallyViews rally;public Transform[] cabRoots;
  int mode=-1;SportsCameraFit fit;readonly SportsCabLook[] looks=new SportsCabLook[4];readonly Quaternion[] headings=new Quaternion[4];
  void Awake(){fit=rally.broadcast.gameObject.AddComponent<SportsCameraFit>();fit.cameraTarget=rally.broadcast.view;fit.arenaFit=true;fit.topInset=.12f;fit.bottomInset=.15f;for(int i=0;i<4;i++){looks[i]=rally.views[i].GetComponent<SportsCabLook>();cabRoots[i].SetParent(null,true);headings[i]=Quaternion.identity;}}
  public int Owner(int view)=>match.option==0?-1:rally.Humans==0?0:rally.owners[view];
  public void Layout(){mode=match.option;fit.enabled=mode==0;rally.Layout();foreach(var camera in rally.views)camera.view.orthographic=false;rally.broadcast.view.orthographic=mode==0;
   if(mode==0){foreach(var c in rally.views)c.view.enabled=false;rally.broadcast.view.enabled=true;rally.broadcast.view.rect=new Rect(0,0,1,1);match.cameras=new[]{rally.broadcast.view};}
   else if(rally.Humans==0){rally.broadcast.view.enabled=false;rally.views[0].view.enabled=true;rally.views[0].view.rect=new Rect(0,0,1,1);match.cameras=new[]{rally.views[0].view};}
   for(int i=0;i<4;i++){cabRoots[i].gameObject.SetActive(mode==2&&rally.views[i].view.enabled);looks[i].ResetLook();}
  }
  public void ReplayView(Vector3 focus,int angle){fit.enabled=false;rally.broadcast.view.orthographic=false;foreach(var cab in cabRoots)cab.gameObject.SetActive(false);focus.y=Mathf.Max(.8f,focus.y);rally.ReplayView(focus,angle);if(match.cameras.Length!=1||match.cameras[0]!=rally.broadcast.view)match.cameras=new[]{rally.broadcast.view};}
  void LateUpdate(){Tick(Time.deltaTime);}
  public void Tick(float dt){if(match.ReplayControls)return;if(mode!=match.option)Layout();if(mode==0){var c=rally.broadcast.view;Bounds bounds=default;bool first=true;for(int i=0;i<match.setup.count;i++){if(rally.Humans>0&&match.setup.players[i].source==BowlingSource.AI)continue;var p=match.trucks[i].body.position;if(first){bounds=new Bounds(p,Vector3.one*36);first=false;}else bounds.Encapsulate(new Bounds(p,Vector3.one*36));}bounds.size=new Vector3(Mathf.Min(144,bounds.size.x),12,Mathf.Min(144,bounds.size.z));Vector3 focus=bounds.center;focus.y=0;c.transform.SetPositionAndRotation(focus+new Vector3(0,118,-66),Quaternion.Euler(61,0,0));c.orthographic=true;fit.arenaBounds=bounds;fit.Apply();return;}
   rally.Follow(dt);int count=rally.Humans==0?1:rally.Humans;
   for(int v=0;v<count;v++){int slot=Owner(v);var t=match.trucks[slot];var camera=rally.views[v].view;if(mode==1&&rally.Humans==0)rally.views[v].Pose(t.body.position+t.transform.forward*5,new Vector3(0,9,-17),70);if(mode!=2)continue;
    var f=Vector3.ProjectOnPlane(t.transform.forward,Vector3.up);if(f.sqrMagnitude>.01f&&Vector3.Dot(t.transform.up,Vector3.up)>.2f)headings[v]=Quaternion.Slerp(headings[v],Quaternion.LookRotation(f),1-Mathf.Exp(-dt*9));
    camera.transform.position=t.body.position+headings[v]*new Vector3(-.17f,1.3f,.65f);cabRoots[v].SetPositionAndRotation(camera.transform.position,headings[v]);var player=match.setup.players[slot];camera.transform.rotation=looks[v].Apply(headings[v],player,slot,v==0,match.Live,SportsLookOptions.Sensitivity,SportsLookOptions.InvertY);camera.fieldOfView=78;camera.GetComponent<SportsEventCamera>().cabSteering.Bind(t);
   }
  }
 }
}

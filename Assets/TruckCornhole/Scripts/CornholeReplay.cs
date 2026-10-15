using UnityEngine;
namespace LWS.TruckCornhole {
// Bounded pose recording, visual meshes only: never rewinds PhysX or scores.
public sealed class CornholeReplay:MonoBehaviour {
 const int Capacity=1400;const float Step=.05f;
 struct Pose {public Vector3 position;public Quaternion rotation;public bool visible;}
 readonly Pose[,] samples=new Pose[Capacity,4];readonly float[] times=new float[Capacity];
 readonly Transform[] ghosts=new Transform[4];readonly Renderer[][] renderers=new Renderer[4][];
 public CornholeCourse course;public bool Playing{get;private set;}public int Angle{get;private set;}=1;
 public float Speed{get;private set;}=1;public float Clock{get;private set;}public float Duration=>count>0?times[count-1]:0;
 public bool Available=>count>1&&!recording;public string AngleName=>Angles[Angle];
 public int FrameCount=>count;int count,delivery;bool recording;float started,next,viewAge;int rate=2;
 static readonly float[] Rates={.25f,.5f,1,2,4};static readonly string[] Angles={"CAB","CHASE","BOARD SIDE","OVERHEAD"};
 void Ensure(){if(ghosts[0]!=null)return;for(int i=0;i<4;i++){var source=course.trucks[i];var g=new GameObject("Replay visual truck "+i);g.transform.SetParent(transform,false);ghosts[i]=g.transform;foreach(var filter in source.GetComponentsInChildren<MeshFilter>(true)){var renderer=filter.GetComponent<MeshRenderer>();if(renderer==null||filter.sharedMesh==null)continue;var mesh=new GameObject(filter.name,typeof(MeshFilter),typeof(MeshRenderer));mesh.layer=24+course.index;mesh.transform.SetParent(g.transform,false);mesh.transform.localPosition=source.transform.InverseTransformPoint(filter.transform.position);mesh.transform.localRotation=Quaternion.Inverse(source.transform.rotation)*filter.transform.rotation;mesh.transform.localScale=filter.transform.lossyScale;mesh.GetComponent<MeshFilter>().sharedMesh=filter.sharedMesh;mesh.GetComponent<MeshRenderer>().sharedMaterials=renderer.sharedMaterials;}renderers[i]=g.GetComponentsInChildren<Renderer>();g.SetActive(false);}}
 public void Record(){Stop();Ensure();count=0;delivery=course.Current.delivery;started=Time.time;next=0;recording=true;Sample();}
 void Sample(){if(count>=Capacity){recording=false;return;}times[count]=Time.time-started;for(int i=0;i<4;i++){var t=course.trucks[i];samples[count,i]=new Pose{position=t.body.position,rotation=t.body.rotation,visible=t.gameObject.activeSelf};}count++;next=Time.time+Step;}
 public void Complete(){if(recording)Sample();recording=false;}
 public bool Play(){if(!Available||!course.match.InMatch||course.match.Paused||course.match.RoundBreak||course.Phase!=CornholePhase.AttemptResult&&course.Phase!=CornholePhase.Waiting)return false;Ensure();Playing=true;Clock=viewAge=0;for(int i=0;i<4;i++){var block=new MaterialPropertyBlock();course.trucks[i].paint.GetPropertyBlock(block);foreach(var r in renderers[i])if(r.name=="Cab")r.SetPropertyBlock(block);}Apply();return true;}
 public void NextAngle(){Angle=(Angle+1)%4;course.chase.NewAttempt();}public void NextSpeed(){rate=(rate+1)%Rates.Length;Speed=Rates[rate];}public void Rewind(){Clock=0;Apply();course.chase.NewAttempt();}
 public void Stop(){Playing=false;if(course!=null&&course.chase!=null)course.chase.view.fieldOfView=72;foreach(var t in ghosts)if(t!=null)t.gameObject.SetActive(false);if(course!=null&&course.chase!=null)course.chase.NewAttempt();}
 public void Clear(){Stop();recording=false;count=0;}
 void LateUpdate(){if(recording&&Time.time>=next&&!course.match.Paused)Sample();if(!Playing||course.match.Paused)return;viewAge+=Time.unscaledDeltaTime;if(viewAge>45){Stop();return;}Clock=Mathf.Min(Duration,Clock+Time.unscaledDeltaTime*Speed);Apply();}
 void Apply(){int a=0;while(a<count-2&&times[a+1]<Clock)a++;int b=Mathf.Min(a+1,count-1);float f=Mathf.InverseLerp(times[a],times[b],Clock);for(int i=0;i<4;i++){var p=samples[a,i];var q=samples[b,i];ghosts[i].gameObject.SetActive(p.visible);ghosts[i].SetPositionAndRotation(Vector3.Lerp(p.position,q.position,f),Quaternion.Slerp(p.rotation,q.rotation,f));}}
 public void ShowGhosts(bool cab){for(int i=0;i<4;i++)foreach(var r in renderers[i])r.forceRenderingOff=cab&&i==delivery;}
 public void PositionCamera(CornholeCamera camera){var target=ghosts[delivery].position;var view=camera.transform;if(Angle==0){camera.view.fieldOfView=72;camera.Cab(target,ghosts[delivery].rotation,true,Time.unscaledDeltaTime);return;}Vector3 position;if(Angle==1){position=target+new Vector3(0,1.6f,-4);camera.view.fieldOfView=58;}else if(Angle==2){position=new Vector3(course.center+12,9,course.board.position.z-2);camera.view.fieldOfView=Mathf.Clamp(2*Mathf.Atan2(5,Vector3.Distance(position,target))*Mathf.Rad2Deg,12,65);}else{position=target+new Vector3(0,10,-.1f);camera.view.fieldOfView=55;}view.SetPositionAndRotation(position,Quaternion.LookRotation(target-position,Vector3.up));}
 void OnDisable(){Stop();}
}
}

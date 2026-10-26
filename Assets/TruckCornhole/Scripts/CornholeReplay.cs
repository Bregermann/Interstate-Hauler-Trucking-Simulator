using UnityEngine;
namespace LWS.TruckCornhole {
// The original independent pose timeline, now including the shared board's
// opponents. No replay body/collider exists and no live pose is ever rewound.
public sealed class CornholeReplay:MonoBehaviour {
 public const int Capacity=1400,MaximumTrucks=16;
 public const int MaximumSamplePayloadBytes=Capacity*MaximumTrucks*32+Capacity*4;
 const float Step=.05f;
 struct Pose {public Vector3 position;public Quaternion rotation;public bool visible;}
 readonly Pose[,] samples=new Pose[Capacity,MaximumTrucks];readonly float[] times=new float[Capacity];
 readonly Transform[] ghosts=new Transform[MaximumTrucks];readonly Renderer[][] renderers=new Renderer[MaximumTrucks][];readonly CornholeTruck[] sources=new CornholeTruck[MaximumTrucks];
 public CornholeCourse course;public bool Playing{get;private set;}public int Angle{get;private set;}=1;
 public float Speed{get;private set;}=1;public float Clock{get;private set;}public float Duration=>count>0?times[count-1]:0;
 public bool Available=>count>1&&!recording;public string AngleName=>Angles[Angle];
 public int FrameCount=>count;public int RecordedTruckCount{get;private set;}
 int count,delivery;bool recording;float started,next,viewAge;double presentedAt;int rate=2;
 static readonly float[] Rates={.25f,.5f,1,2,4};static readonly string[] Angles={"CAB","CHASE","BOARD SIDE","OVERHEAD"};
 readonly int[] appearanceRevisions=new int[MaximumTrucks];readonly Renderer[][] snapshotSources=new Renderer[MaximumTrucks][];
 // Keep the original sixteen root-pose timelines. Refresh only their bounded
 // render snapshots at stopped record boundaries, never live physics or clocks.
 void Ensure(){if(Playing)return;int index=0;foreach(var owner in course.match.courses)foreach(var source in owner.trucks){if(index>=MaximumTrucks)break;sources[index]=source;int revision=source.appearanceHost!=null?source.appearanceHost.AppearanceRevision:-1;
  if(ghosts[index]==null){var root=new GameObject("Replay visual truck "+owner.index+" / "+index);root.transform.SetParent(transform,false);ghosts[index]=root.transform;root.SetActive(false);}
  if(snapshotSources[index]==null||appearanceRevisions[index]!=revision)RebuildSnapshot(index,source);
  else for(int part=0;part<renderers[index].Length;part++)CopyMetadata(snapshotSources[index][part],renderers[index][part]);
  appearanceRevisions[index]=revision;index++;}}
 void RebuildSnapshot(int index,CornholeTruck source){var root=ghosts[index];while(root.childCount>0){var stale=root.GetChild(0);stale.gameObject.SetActive(false);stale.SetParent(null,true);Destroy(stale.gameObject);}
  var originals=new System.Collections.Generic.List<Renderer>();var copies=new System.Collections.Generic.List<Renderer>();
  foreach(var filter in source.GetComponentsInChildren<MeshFilter>(true)){var renderer=filter.GetComponent<MeshRenderer>();if(renderer==null||filter.sharedMesh==null||!renderer.enabled||!VisibleBelow(filter.transform,source.transform))continue;
   var mesh=new GameObject(filter.name,typeof(MeshFilter),typeof(MeshRenderer));mesh.layer=CornholeSharedTarget.PrivateLayerStart+course.index;mesh.transform.SetParent(root,false);mesh.transform.localPosition=source.transform.InverseTransformPoint(filter.transform.position);mesh.transform.localRotation=Quaternion.Inverse(source.transform.rotation)*filter.transform.rotation;var scale=source.transform.lossyScale;var actual=filter.transform.lossyScale;mesh.transform.localScale=new Vector3(actual.x/scale.x,actual.y/scale.y,actual.z/scale.z);mesh.GetComponent<MeshFilter>().sharedMesh=filter.sharedMesh;var copy=mesh.GetComponent<MeshRenderer>();CopyMetadata(renderer,copy);originals.Add(renderer);copies.Add(copy);}
  snapshotSources[index]=originals.ToArray();renderers[index]=copies.ToArray();}
 static bool VisibleBelow(Transform part,Transform truck){for(var current=part;current!=null&&current!=truck;current=current.parent)if(!current.gameObject.activeSelf)return false;return true;}
 static void CopyMetadata(Renderer source,Renderer copy){copy.enabled=source.enabled;copy.sharedMaterials=source.sharedMaterials;copy.shadowCastingMode=source.shadowCastingMode;copy.receiveShadows=source.receiveShadows;var block=new MaterialPropertyBlock();source.GetPropertyBlock(block);copy.SetPropertyBlock(block);for(int material=0;material<source.sharedMaterials.Length;material++){block.Clear();source.GetPropertyBlock(block,material);copy.SetPropertyBlock(block.isEmpty?null:block,material);}}
 public void Record(){Stop();Ensure();count=0;RecordedTruckCount=Mathf.Min(MaximumTrucks,course.match.setup.count*CornholeRules.Deliveries);delivery=course.index*CornholeRules.Deliveries+course.Current.delivery;started=Time.time;next=0;recording=true;Sample();}
 void Sample(){if(count>=Capacity){recording=false;return;}times[count]=Time.time-started;for(int i=0;i<RecordedTruckCount;i++){var t=sources[i];samples[count,i]=new Pose{position=t.body.position,rotation=t.body.rotation,visible=t.gameObject.activeSelf};}count++;next=Time.time+Step;}
 public void Complete(){if(recording)Sample();recording=false;}
 public bool Play(){if(!Available||!course.match.InMatch||course.match.Paused||course.match.RoundBreak||course.Phase!=CornholePhase.AttemptResult&&course.Phase!=CornholePhase.Waiting)return false;Ensure();Playing=true;Clock=viewAge=0;for(int i=0;i<RecordedTruckCount;i++){if(sources[i].appearanceHost!=null&&sources[i].appearanceHost.Applied)continue;var block=new MaterialPropertyBlock();sources[i].paint.GetPropertyBlock(block);foreach(var r in renderers[i])if(r.name=="Cab")r.SetPropertyBlock(block);}Apply();SyncClock();return true;}
 public void NextAngle(){Angle=(Angle+1)%4;course.chase.NewAttempt();}public void NextSpeed(){AdvanceClock();rate=(rate+1)%Rates.Length;Speed=Rates[rate];}public void Rewind(){Clock=0;Apply();course.chase.NewAttempt();SyncClock();}
 public void Stop(){Playing=false;SyncClock();if(course!=null&&course.chase!=null)course.chase.view.fieldOfView=72;foreach(var t in ghosts)if(t!=null)t.gameObject.SetActive(false);if(course!=null&&course.chase!=null)course.chase.NewAttempt();}
 public void Clear(){Stop();recording=false;count=0;RecordedTruckCount=0;}
 public void SyncClock(){presentedAt=Time.realtimeSinceStartupAsDouble;}
 void AdvanceClock(){double now=Time.realtimeSinceStartupAsDouble;float elapsed=(float)System.Math.Max(0,now-presentedAt);presentedAt=now;if(!Playing||course.match.Paused)return;viewAge+=elapsed;Clock=Mathf.Min(Duration,Clock+elapsed*Speed);}
 void LateUpdate(){if(recording&&Time.time>=next&&!course.match.Paused)Sample();if(!Playing||course.match.Paused){SyncClock();return;}AdvanceClock();if(viewAge>45){Stop();return;}Apply();}
 void OnApplicationFocus(bool focused){SyncClock();}void OnApplicationPause(bool paused){SyncClock();}
 void Apply(){int a=0;while(a<count-2&&times[a+1]<Clock)a++;int b=Mathf.Min(a+1,count-1);float f=Mathf.InverseLerp(times[a],times[b],Clock);for(int i=0;i<RecordedTruckCount;i++){var p=samples[a,i];var q=samples[b,i];ghosts[i].gameObject.SetActive(p.visible);ghosts[i].SetPositionAndRotation(Vector3.Lerp(p.position,q.position,f),Quaternion.Slerp(p.rotation,q.rotation,f));}}
 public void ShowGhosts(bool cab){for(int i=0;i<RecordedTruckCount;i++)foreach(var r in renderers[i])r.forceRenderingOff=cab&&i==delivery;}
 public void PositionCamera(CornholeCamera camera){var target=ghosts[delivery].position;var view=camera.transform;if(Angle==0){camera.view.fieldOfView=72;camera.Cab(target,ghosts[delivery].rotation,true,Time.unscaledDeltaTime);return;}Vector3 position;if(Angle==1){position=target+course.ApproachDirection(new Vector3(0,1.6f,-4));camera.view.fieldOfView=58;}else if(Angle==2){position=course.board.position+new Vector3(12,9-course.board.position.y,-2);camera.view.fieldOfView=Mathf.Clamp(2*Mathf.Atan2(5,Vector3.Distance(position,target))*Mathf.Rad2Deg,12,65);}else{position=target+new Vector3(0,10,-.1f);camera.view.fieldOfView=55;}view.SetPositionAndRotation(position,Quaternion.LookRotation(target-position,Vector3.up));}
 void OnDisable(){Stop();}
}
}
using UnityEngine;
namespace LWS.TruckSports {
// Actual transforms only. Match owners stop live simulation before playing this bounded render-only record.
public sealed class SportsMotionReplay:MonoBehaviour,ISportsRecordedVehicleView {
 public Transform[] sources,ghosts;public int capacity=3600;public bool rolling;public float interval=.05f;public float maximumReplayAge=18;public bool Playing{get;private set;}public float Cursor{get;private set;}public float Duration{get;private set;}public float Speed=1;public int Angle;public float Age{get;private set;}
 struct Pose {public Vector3 p,s;public Quaternion q;}
 Transform[][] liveParts,ghostParts;int[] offsets,masks;float[] times;Pose[] roots,parts;int count,width,start;float next,endTime;
 public bool TryGetRecordedVehicle(Transform physicalVehicle,out Transform recordedSource,out Transform recordedVehicle){recordedSource=recordedVehicle=null;if(!Playing||physicalVehicle==null)return false;for(int i=0;i<sources.Length;i++)if(sources[i]!=null&&(sources[i]==physicalVehicle||sources[i].IsChildOf(physicalVehicle))&&ghosts[i]!=null&&ghosts[i].gameObject.activeInHierarchy){recordedSource=sources[i];recordedVehicle=ghosts[i];return true;}return false;}
 void Initialize(){liveParts=new Transform[sources.Length][];ghostParts=new Transform[sources.Length][];offsets=new int[sources.Length];width=0;for(int i=0;i<sources.Length;i++){liveParts[i]=sources[i].GetComponentsInChildren<Transform>(true);ghostParts[i]=ghosts[i].GetComponentsInChildren<Transform>(true);if(liveParts[i].Length!=ghostParts[i].Length)throw new System.InvalidOperationException("Replay render hierarchy differs at "+i);offsets[i]=width;width+=liveParts[i].Length;}roots=new Pose[capacity*sources.Length];parts=new Pose[capacity*width];masks=new int[capacity];times=new float[capacity];}
 int Frame(int logical)=>(start+logical)%capacity;
 // Match/delivery boundary only. Root and surviving original child instances
 // are retained so authored replay cameras and headlight references stay valid.
 public bool InvalidateFreshRecord(){if(Playing)return false;roots=parts=null;liveParts=ghostParts=null;offsets=masks=null;times=null;count=start=width=0;next=Duration=Cursor=Age=0;return true;}
 public bool RebuildGhostHierarchy(int sourceIndex){if(Playing||sources==null||ghosts==null||sourceIndex<0||sourceIndex>=sources.Length||sourceIndex>=ghosts.Length||sources[sourceIndex]==null||ghosts[sourceIndex]==null)return false;var source=sources[sourceIndex];var ghost=ghosts[sourceIndex];SyncHierarchy(source,ghost,ghost.gameObject.layer);return InvalidateFreshRecord();}
 static void SyncHierarchy(Transform source,Transform target,int layer){var available=new System.Collections.Generic.List<Transform>();foreach(Transform child in target)available.Add(child);int sibling=0;foreach(Transform child in source){var match=available.Find(candidate=>candidate.name==child.name);if(match!=null){available.Remove(match);SyncHierarchy(child,match,layer);}else{match=SportsReplayVisuals.Copy(child).transform;match.SetParent(target,false);SyncHierarchy(child,match,layer);}match.SetSiblingIndex(sibling++);match.localPosition=child.localPosition;match.localRotation=child.localRotation;match.localScale=child.localScale;match.gameObject.SetActive(child.gameObject.activeSelf);}foreach(var stale in available){stale.gameObject.SetActive(false);stale.SetParent(null,true);if(Application.isPlaying)Destroy(stale.gameObject);else DestroyImmediate(stale.gameObject);}target.gameObject.layer=layer;SyncRenderMetadata(source,target);}
 static void SyncRenderMetadata(Transform source,Transform target)
 {
  var mesh=source.GetComponent<MeshFilter>();var meshCopy=target.GetComponent<MeshFilter>();if(mesh!=null&&meshCopy!=null)meshCopy.sharedMesh=mesh.sharedMesh;
  var live=source.GetComponent<Renderer>();var copy=target.GetComponent<Renderer>();if(live==null||copy==null)return;
  copy.enabled=live.enabled;copy.sharedMaterials=live.sharedMaterials;var block=new MaterialPropertyBlock();live.GetPropertyBlock(block);copy.SetPropertyBlock(block);
  for(int index=0;index<live.sharedMaterials.Length;index++){block.Clear();live.GetPropertyBlock(block,index);copy.SetPropertyBlock(block.isEmpty?null:block,index);}
 }
 // Opt-in appearance boundary: clear counters now, initialize only after the
 // original mode has reset equipment and the first actual sample is ready.
 public bool ResetFreshAppearanceRecord(){if(Playing)return false;count=start=0;next=Duration=Cursor=Age=0;End();return true;}
 public bool RefreshAppearanceSources(int[] sourceIndices,bool topologyChanged)
 {
  if(Playing||sourceIndices==null||sources==null||ghosts==null)return false;
  var indices=new System.Collections.Generic.HashSet<int>(sourceIndices);
  var live=new System.Collections.Generic.Dictionary<int,Transform[]>();var copies=new System.Collections.Generic.Dictionary<int,Transform[]>();
  // Validate the entire batch before changing a ghost or invalidating a record.
  foreach(int index in indices)
  {
   if(index<0||index>=sources.Length||index>=ghosts.Length||sources[index]==null||ghosts[index]==null)return false;
   if(topologyChanged)continue;
   var a=sources[index].GetComponentsInChildren<Transform>(true);var b=ghosts[index].GetComponentsInChildren<Transform>(true);if(a.Length!=b.Length)return false;
   for(int part=1;part<a.Length;part++)if(a[part].name!=b[part].name)return false;
   live.Add(index,a);copies.Add(index,b);
  }
  foreach(int index in indices)
  {
   if(topologyChanged)SyncHierarchy(sources[index],ghosts[index],ghosts[index].gameObject.layer);
   else for(int part=0;part<live[index].Length;part++){SyncRenderMetadata(live[index][part],copies[index][part]);if(part>0)copies[index][part].gameObject.SetActive(live[index][part].gameObject.activeSelf);}
  }
  // One allocation invalidation for the complete changed batch. Same-body
  // color/metadata refreshes retain the existing pose/timestamp/mask buffers.
  return !topologyChanged||indices.Count==0||InvalidateFreshRecord();
 }
 public void ResetRecord(){if(roots==null)Initialize();count=start=0;next=Duration=0;End();}
 public void Record(float time,int visibleMask){if(Playing||time<next||count>=capacity&&!rolling)return;next=time+interval;int frame;if(count==capacity){frame=start;start=(start+1)%capacity;}else frame=Frame(count++);times[frame]=time;masks[frame]=visibleMask;for(int i=0;i<sources.Length;i++){roots[frame*sources.Length+i]=new Pose{p=sources[i].position,q=sources[i].rotation,s=sources[i].lossyScale};for(int k=0;k<liveParts[i].Length;k++){var t=liveParts[i][k];parts[frame*width+offsets[i]+k]=new Pose{p=t.localPosition,q=t.localRotation,s=t.localScale};}}Duration=time;}
 public void Begin(float from=0,float until=float.PositiveInfinity){Playing=count>1;endTime=Mathf.Max(count>0?times[Frame(0)]:0,Mathf.Min(Duration,until));Cursor=Mathf.Clamp(from,count>0?times[Frame(0)]:0,endTime);Age=0;Speed=1;Angle=0;Draw();}
 public bool Step(float dt){if(!Playing)return true;Age+=dt;Cursor=Mathf.Min(endTime,Cursor+dt*Speed);Draw();return Cursor>=endTime&&Age>1.5f||Age>maximumReplayAge;}
 public void Seek(float value){Cursor=Mathf.Clamp(value,count>0?times[Frame(0)]:0,endTime);Draw();}
 public Vector3 Position(int index)=>ghosts[Mathf.Clamp(index,0,ghosts.Length-1)].position;
 void Draw(){if(!Playing)return;int lo=0,hi=count-1;while(lo+1<hi){int mid=(lo+hi)/2;if(times[Frame(mid)]<=Cursor)lo=mid;else hi=mid;}lo=Frame(lo);hi=Frame(hi);float blend=Mathf.InverseLerp(times[lo],times[hi],Cursor);for(int i=0;i<sources.Length;i++){ghosts[i].gameObject.SetActive((masks[lo]&(1<<i))!=0);var a=roots[lo*sources.Length+i];var b=roots[hi*sources.Length+i];ghosts[i].SetPositionAndRotation(Vector3.Lerp(a.p,b.p,blend),Quaternion.Slerp(a.q,b.q,blend));ghosts[i].localScale=Vector3.Lerp(a.s,b.s,blend);for(int k=1;k<ghostParts[i].Length;k++){a=parts[lo*width+offsets[i]+k];b=parts[hi*width+offsets[i]+k];ghostParts[i][k].SetLocalPositionAndRotation(Vector3.Lerp(a.p,b.p,blend),Quaternion.Slerp(a.q,b.q,blend));ghostParts[i][k].localScale=Vector3.Lerp(a.s,b.s,blend);}}}
 public void Colorize(Color[] colors){for(int i=0;i<ghosts.Length&&i<colors.Length;i++){var block=new MaterialPropertyBlock();block.SetColor("_BaseColor",colors[i]);block.SetColor("_Color",colors[i]);foreach(var r in ghosts[i].GetComponentsInChildren<Renderer>(true))if(r.name=="Cab")r.SetPropertyBlock(block);}}
 public void End(){Playing=false;if(ghosts!=null)foreach(var g in ghosts)if(g!=null)g.gameObject.SetActive(false);}
}
}

using UnityEngine;
namespace LWS.TruckSports {
// Actual transforms only. Match owners stop live simulation before playing this bounded render-only record.
public sealed class SportsMotionReplay:MonoBehaviour {
 public Transform[] sources,ghosts;public int capacity=3600;public float interval=.05f;public bool Playing{get;private set;}public float Cursor{get;private set;}public float Duration{get;private set;}public float Speed=1;public int Angle;public float Age{get;private set;}
 struct Pose {public Vector3 p;public Quaternion q;}
 Transform[][] liveParts,ghostParts;int[] offsets,masks;float[] times;Pose[] roots,parts;int count,width;float next,endTime;
 void Initialize(){liveParts=new Transform[sources.Length][];ghostParts=new Transform[sources.Length][];offsets=new int[sources.Length];width=0;for(int i=0;i<sources.Length;i++){liveParts[i]=sources[i].GetComponentsInChildren<Transform>(true);ghostParts[i]=ghosts[i].GetComponentsInChildren<Transform>(true);if(liveParts[i].Length!=ghostParts[i].Length)throw new System.InvalidOperationException("Replay render hierarchy differs at "+i);offsets[i]=width;width+=liveParts[i].Length;}roots=new Pose[capacity*sources.Length];parts=new Pose[capacity*width];masks=new int[capacity];times=new float[capacity];}
 public void ResetRecord(){if(roots==null)Initialize();count=0;next=Duration=0;End();}
 public void Record(float time,int visibleMask){if(Playing||time<next||count>=capacity)return;next=time+interval;times[count]=time;masks[count]=visibleMask;for(int i=0;i<sources.Length;i++){roots[count*sources.Length+i]=new Pose{p=sources[i].position,q=sources[i].rotation};for(int k=0;k<liveParts[i].Length;k++){var t=liveParts[i][k];parts[count*width+offsets[i]+k]=new Pose{p=t.localPosition,q=t.localRotation};}}count++;Duration=time;}
 public void Begin(float from=0,float until=float.PositiveInfinity){Playing=count>1;endTime=Mathf.Min(Duration,until);Cursor=Mathf.Clamp(from,0,endTime);Age=0;Speed=1;Angle=0;Draw();}
 public bool Step(float dt){if(!Playing)return true;Age+=dt;Cursor=Mathf.Min(endTime,Cursor+dt*Speed);Draw();return Cursor>=endTime&&Age>1.5f||Age>18;}
 public void Seek(float value){Cursor=Mathf.Clamp(value,0,endTime);Draw();}
 public Vector3 Position(int index)=>ghosts[Mathf.Clamp(index,0,ghosts.Length-1)].position;
 void Draw(){if(!Playing)return;int lo=0,hi=count-1;while(lo+1<hi){int mid=(lo+hi)/2;if(times[mid]<=Cursor)lo=mid;else hi=mid;}float blend=Mathf.InverseLerp(times[lo],times[hi],Cursor);for(int i=0;i<sources.Length;i++){ghosts[i].gameObject.SetActive((masks[lo]&(1<<i))!=0);var a=roots[lo*sources.Length+i];var b=roots[hi*sources.Length+i];ghosts[i].SetPositionAndRotation(Vector3.Lerp(a.p,b.p,blend),Quaternion.Slerp(a.q,b.q,blend));for(int k=1;k<ghostParts[i].Length;k++){a=parts[lo*width+offsets[i]+k];b=parts[hi*width+offsets[i]+k];ghostParts[i][k].SetLocalPositionAndRotation(Vector3.Lerp(a.p,b.p,blend),Quaternion.Slerp(a.q,b.q,blend));}}}
 public void Colorize(Color[] colors){for(int i=0;i<ghosts.Length;i++){var block=new MaterialPropertyBlock();block.SetColor("_BaseColor",colors[i]);block.SetColor("_Color",colors[i]);foreach(var r in ghosts[i].GetComponentsInChildren<Renderer>(true))if(r.name=="Cab")r.SetPropertyBlock(block);}}
 public void End(){Playing=false;if(ghosts!=null)foreach(var g in ghosts)g.gameObject.SetActive(false);}
}
}

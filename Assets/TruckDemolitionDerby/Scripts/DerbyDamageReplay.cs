using System;using UnityEngine;
namespace LWS.TruckDerby {
// Sparse snapshots of NWH's real deformed vertices. No collision resimulation or predicted dents.
// One baseline plus 48 bounded damage revisions per truck; later revisions replace oldest history.
public sealed class DerbyDamageReplay:MonoBehaviour {
 [Serializable]public sealed class Slot{public MeshFilter[] live,ghost;[NonSerialized]public Mesh[] original,playback;[NonSerialized]public Frame[] frames;[NonSerialized]public int count;[NonSerialized]public float pending=-1;[NonSerialized]public int drawn=-1;}
 public sealed class Frame{public float time;public Vector3[][] vertices;}
 public Slot[] slots;public DerbyPoseBridge[] poses;
 void Initialize(Slot s){if(s.original!=null)return;s.original=new Mesh[s.live.Length];s.playback=new Mesh[s.live.Length];s.frames=new Frame[49];for(int k=0;k<s.live.Length;k++){s.original[k]=s.live[k].sharedMesh;s.playback[k]=Instantiate(s.ghost[k].sharedMesh);s.playback[k].MarkDynamic();s.ghost[k].sharedMesh=s.playback[k];}}
 public void ResetRecord(){foreach(var s in slots){Initialize(s);s.count=0;s.pending=-1;s.drawn=-1;Array.Clear(s.frames,0,s.frames.Length);}}
 public void CaptureBaseline(){for(int i=0;i<slots.Length;i++)Capture(i,0);foreach(var p in poses)p.Sync();}
 public void Request(int slot,float time){slots[slot].pending=Mathf.Max(time,slots[slot].pending);}
 void Capture(int index,float time){var s=slots[index];int at=s.count;if(at>=s.frames.Length){Array.Copy(s.frames,2,s.frames,1,s.frames.Length-2);at=s.frames.Length-1;}else s.count++;var frame=new Frame{time=time,vertices=new Vector3[s.live.Length][]};for(int k=0;k<s.live.Length;k++)frame.vertices[k]=s.live[k].sharedMesh.vertices;s.frames[at]=frame;s.pending=-1;}
 public void Tick(float time){for(int i=0;i<slots.Length;i++)if(slots[i].pending>=0&&time>=slots[i].pending)Capture(i,time);foreach(var p in poses)p.Sync();}
 public void Draw(float time){foreach(var s in slots){if(s.count==0)continue;int at=0;for(int i=1;i<s.count;i++)if(s.frames[i].time<=time)at=i;else break;if(s.drawn==at)continue;s.drawn=at;for(int k=0;k<s.playback.Length;k++){s.playback[k].vertices=s.frames[at].vertices[k];s.playback[k].RecalculateNormals();s.playback[k].RecalculateBounds();}}}
 void OnDestroy(){if(slots!=null)foreach(var s in slots)if(s.playback!=null)foreach(var mesh in s.playback)if(mesh!=null)Destroy(mesh);}
}}

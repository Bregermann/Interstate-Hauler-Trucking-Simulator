using UnityEngine;
namespace LWS.TruckDerby {
// Bounded history of actual emitter states. Replay never calls damage or explosion physics.
public sealed class DerbyFxReplay:MonoBehaviour {
 public DerbyMatch match;readonly float[] times=new float[1200];readonly int[] masks=new int[1200];int start,count;float next,last=-1;ParticleSystem[] smoke,fire;
 void Awake(){smoke=new ParticleSystem[8];fire=new ParticleSystem[8];for(int i=0;i<8;i++){smoke[i]=Copy(match.fighters[i].smoke);fire[i]=Copy(match.fighters[i].fire);}}
 ParticleSystem Copy(ParticleSystem source){var p=Instantiate(source);p.name="Recorded emitter · "+source.name;p.transform.SetParent(null,true);p.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);return p;}
 public void ResetRecord(){start=count=0;next=0;Hide();}
 public void Record(float time){if(time<next)return;next=time+.1f;int f;if(count==times.Length){f=start;start=(start+1)%times.Length;}else f=(start+count++)%times.Length;times[f]=time;int bits=0;for(int i=0;i<8;i++){if(match.fighters[i].smoke.isPlaying)bits|=1<<i;if(match.fighters[i].fire.isPlaying)bits|=1<<(i+8);}masks[f]=bits;}
 public void Draw(float time){if(count==0)return;int f=start;for(int k=1;k<count;k++){int at=(start+k)%times.Length;if(times[at]>time)break;f=at;}bool seek=last<0||time<last||time-last>.4f;float dt=seek?.3f:Mathf.Max(0,time-last);last=time;for(int i=0;i<8;i++){var root=match.replay.ghosts[i];Apply(smoke[i],root.position+root.rotation*new Vector3(0,1.4f,2),(masks[f]&(1<<i))!=0,dt,seek);Apply(fire[i],root.position+root.rotation*new Vector3(0,1.4f,2),(masks[f]&(1<<(i+8)))!=0,dt,seek);}}
 static void Apply(ParticleSystem p,Vector3 position,bool active,float dt,bool seek){p.transform.position=position;if(seek)p.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);if(active){p.Play();p.Simulate(dt,true,false,true);p.Pause();}else p.Stop(true,ParticleSystemStopBehavior.StopEmitting);}
 public void Hide(){last=-1;if(smoke!=null)foreach(var p in smoke)if(p!=null)p.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);if(fire!=null)foreach(var p in fire)if(p!=null)p.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);}
 void OnDestroy(){if(smoke!=null)foreach(var p in smoke)if(p!=null)Destroy(p.gameObject);if(fire!=null)foreach(var p in fire)if(p!=null)Destroy(p.gameObject);}
}}

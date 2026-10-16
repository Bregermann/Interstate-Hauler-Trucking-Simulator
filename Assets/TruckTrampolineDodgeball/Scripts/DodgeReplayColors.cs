using UnityEngine;using LWS.TruckSports;
namespace LWS.TruckDodgeball {
// Replay metadata accompanies, rather than recomputes, the recorded PhysX trajectory.
public sealed class DodgeReplayColors:MonoBehaviour {
 public DodgeBall[] balls;public SportsMotionReplay replay;readonly float[] times=new float[2400];readonly int[] states=new int[2400];int count;float next;Renderer[][] shells;MaterialPropertyBlock block;
 void Awake(){block=new MaterialPropertyBlock();}
 public void ResetRecord(){count=0;next=0;if(shells==null){shells=new Renderer[balls.Length][];for(int i=0;i<balls.Length;i++)shells[i]=replay.ghosts[8+i].GetComponentsInChildren<Renderer>(true);}}
 public void Record(float time){if(time<next||count>=times.Length)return;next=time+.05f;int value=0;for(int i=0;i<balls.Length;i++)value|=(balls[i].Team+1)<<(i*2);times[count]=time;states[count++]=value;}
 public void Draw(float cursor){if(count==0)return;int lo=0,hi=count-1;while(lo+1<hi){int mid=(lo+hi)/2;if(times[mid]<=cursor)lo=mid;else hi=mid;}int state=states[lo];for(int i=0;i<balls.Length;i++){int team=((state>>(i*2))&3)-1;var c=team<0?new Color(.92f,.91f,.76f):team==0?new Color(.1f,.6f,1):new Color(1,.3f,.05f);block.SetColor("_BaseColor",c);block.SetColor("_Color",c);foreach(var shell in shells[i])shell.SetPropertyBlock(block);}}
}
}

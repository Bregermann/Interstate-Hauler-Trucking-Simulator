using System;
namespace LWS.TruckHungryHaulers
{
 public enum HaulerBallState{Loose,Carried,Deposited,Spent}
 public enum HaulerTake{Ignored,Carried,Explosive}
 [Serializable]public sealed class HaulerRules
 {
  public int Players{get;private set;}public int Capacity{get;private set;}public float Clock{get;private set;}public float Duration{get;private set;}public bool Live{get;private set;}public bool Complete{get;private set;}public int PositiveRemaining{get;private set;}
  public HaulerBallState[] state;public int[] owners;int[] values;
  public readonly int[] points=new int[4],carried=new int[4],deposits=new int[4],gold=new int[4],bombs=new int[4],spilled=new int[4];
  public void Reset(int players,int[] ballValues,float duration=180,int capacity=12){if(players<1||players>4||ballValues==null||ballValues.Length<16||ballValues.Length>512||capacity<1||capacity>32||duration<30||float.IsNaN(duration)||float.IsInfinity(duration))throw new ArgumentOutOfRangeException();values=(int[])ballValues.Clone();PositiveRemaining=0;foreach(int value in values){if(value<0||value>100)throw new ArgumentOutOfRangeException(nameof(ballValues));if(value>0)PositiveRemaining++;}Players=players;Capacity=capacity;Duration=duration;Clock=0;Live=Complete=false;state=new HaulerBallState[values.Length];owners=new int[values.Length];Array.Fill(owners,-1);foreach(var a in new[]{points,carried,deposits,gold,bombs,spilled})Array.Clear(a,0,4);}
  public void Begin(){if(values!=null&&!Complete)Live=true;}
  public void Tick(float dt){if(!Live||dt<=0||float.IsNaN(dt)||float.IsInfinity(dt))return;Clock=Math.Min(Duration,Clock+dt);if(Clock>=Duration){Live=false;Complete=true;}}
  public HaulerTake Collect(int ball,int player){if(!Live||!Valid(ball,player)||state[ball]!=HaulerBallState.Loose)return HaulerTake.Ignored;if(values[ball]==0){state[ball]=HaulerBallState.Spent;bombs[player]++;return HaulerTake.Explosive;}if(carried[player]>=Capacity)return HaulerTake.Ignored;state[ball]=HaulerBallState.Carried;owners[ball]=player;carried[player]++;return HaulerTake.Carried;}
  public int Deposit(int player){if(!Live||player<0||player>=Players)return 0;int count=0;for(int i=0;i<state.Length;i++)if(state[i]==HaulerBallState.Carried&&owners[i]==player){state[i]=HaulerBallState.Deposited;points[player]+=values[i];if(values[i]>1)gold[player]++;count++;PositiveRemaining--;}carried[player]-=count;deposits[player]+=count;if(PositiveRemaining==0){Live=false;Complete=true;}return count;}
  public int Drop(int player,int maximum,int[] released){if(!Live||player<0||player>=Players||maximum<1||released==null)return 0;int count=0;for(int i=0;i<state.Length&&count<maximum&&count<released.Length;i++)if(state[i]==HaulerBallState.Carried&&owners[i]==player){state[i]=HaulerBallState.Loose;owners[i]=-1;released[count++]=i;}carried[player]-=count;spilled[player]+=count;return count;}
  public int Place(int player){int place=1;for(int i=0;i<Players;i++)if(points[i]>points[player])place++;return place;}
  bool Valid(int ball,int player)=>ball>=0&&ball<state.Length&&player>=0&&player<Players;
 }
}

using System;
namespace LWS.TruckAvalancheEscape
{
 public enum AvalanchePhase{Setup,Countdown,Racing,Complete}
 [Serializable]public sealed class AvalancheRules
 {
  public int Players{get;private set;}public float Clock{get;private set;}public float Countdown{get;private set;}public float Length{get;private set;}public float Grace{get;private set;}public float TimeLimit{get;private set;}public AvalanchePhase Phase{get;private set;}public bool Complete=>Phase==AvalanchePhase.Complete;
  public readonly bool[] alive=new bool[12],finished=new bool[12];public readonly float[] distance=new float[12],finishTime=new float[12],caughtFor=new float[12];public readonly string[] reason=new string[12];
  public void Reset(int players,float length,float grace=4.5f,float limit=300){if(players<1||players>12||!float.IsFinite(length)||length<100||!float.IsFinite(grace)||grace<=0||!float.IsFinite(limit)||limit<30)throw new ArgumentOutOfRangeException();Players=players;Length=length;Grace=grace;TimeLimit=limit;Clock=0;Countdown=3;Phase=AvalanchePhase.Setup;for(int i=0;i<12;i++){alive[i]=i<players;finished[i]=false;distance[i]=caughtFor[i]=0;finishTime[i]=float.PositiveInfinity;reason[i]="";}}
  public void Begin(){if(Phase==AvalanchePhase.Setup)Phase=AvalanchePhase.Countdown;}
  public void Tick(float dt){if(!float.IsFinite(dt)||dt<=0||Complete||Phase==AvalanchePhase.Setup)return;if(Phase==AvalanchePhase.Countdown){float used=Math.Min(Countdown,dt);Countdown-=used;dt-=used;if(Countdown>0)return;Phase=AvalanchePhase.Racing;}Clock+=dt;if(Clock>=TimeLimit){Clock=TimeLimit;for(int i=0;i<Players;i++)if(alive[i]&&!finished[i])Eliminate(i,"AVALANCHE TIME LIMIT");}Resolve();}
  bool Eligible(int slot)=>Phase==AvalanchePhase.Racing&&slot>=0&&slot<Players&&alive[slot]&&!finished[slot];
  public bool Progress(int slot,float metres){if(!Eligible(slot)||!float.IsFinite(metres))return false;distance[slot]=Math.Max(distance[slot],Math.Clamp(metres,0,Length));return true;}
  public bool SnowContact(int slot,float dt,bool engulfed){if(!Eligible(slot)||!float.IsFinite(dt)||dt<=0)return false;caughtFor[slot]=Math.Clamp(caughtFor[slot]+(engulfed?dt:-dt*2),0,Grace);return engulfed&&caughtFor[slot]>=Grace&&Eliminate(slot,"BURIED IN THE AVALANCHE");}
  public event Action<int,float> FinishConfirmed;
  public bool Finish(int slot,float seconds){if(!Eligible(slot)||!float.IsFinite(seconds)||seconds<0||seconds>Clock+.05f)return false;finished[slot]=true;finishTime[slot]=seconds;distance[slot]=Length;reason[slot]="ESCAPED";Resolve();FinishConfirmed?.Invoke(slot,seconds);return true;}
  public bool Eliminate(int slot,string why){if(!Eligible(slot))return false;alive[slot]=false;reason[slot]=why;Resolve();return true;}
  void Resolve(){if(Phase!=AvalanchePhase.Racing)return;for(int i=0;i<Players;i++)if(alive[i]&&!finished[i])return;Phase=AvalanchePhase.Complete;}
  public int Place(int slot){int result=1;for(int i=0;i<Players;i++)if(i!=slot&&(finished[i]&&!finished[slot]||finished[i]&&finished[slot]&&finishTime[i]<finishTime[slot]-.001f||!finished[i]&&!finished[slot]&&distance[i]>distance[slot]+.01f))result++;return result;}
 }
}

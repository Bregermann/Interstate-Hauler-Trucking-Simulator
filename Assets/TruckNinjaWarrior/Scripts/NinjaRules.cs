using System;
namespace LWS.TruckNinjaWarrior
{
 public enum NinjaPhase{Setup,Countdown,Running,TurnResult,Complete}
 [Serializable]public sealed class NinjaRules
 {
  public int Players{get;private set;}public int Active{get;private set;}public int Checkpoint{get;private set;}public int Obstacles{get;private set;}public float Clock{get;private set;}public float Penalty{get;private set;}public float Limit{get;private set;}public float StateClock{get;private set;}public NinjaPhase Phase{get;private set;}public bool Complete=>Phase==NinjaPhase.Complete;
  public readonly float[] adjustedTime=new float[8];public readonly int[] falls=new int[8],cleared=new int[8];public readonly bool[] finished=new bool[8];
  public void Reset(int players,int obstacles=6,float limit=180){if(players<1||players>8||obstacles<6||obstacles>10||!float.IsFinite(limit)||limit<120||limit>180)throw new ArgumentOutOfRangeException();Players=players;Obstacles=obstacles;Limit=limit;Active=Checkpoint=0;Clock=Penalty=StateClock=0;Phase=NinjaPhase.Setup;for(int i=0;i<8;i++){adjustedTime[i]=float.PositiveInfinity;falls[i]=cleared[i]=0;finished[i]=false;}}
  public void Begin(){if(Phase!=NinjaPhase.Setup)return;NextAttempt();}
  void NextAttempt(){Checkpoint=0;Clock=Penalty=StateClock=0;Phase=NinjaPhase.Countdown;}
  public void Tick(float dt){if(!float.IsFinite(dt)||dt<=0||Phase==NinjaPhase.Setup||Complete)return;if(Phase==NinjaPhase.Running){Clock=Math.Min(Limit,Clock+dt);if(Clock>=Limit){Phase=NinjaPhase.TurnResult;StateClock=0;}return;}StateClock+=dt;if(Phase==NinjaPhase.Countdown&&StateClock>=3){Clock=StateClock-3;StateClock=0;Phase=NinjaPhase.Running;}else if(Phase==NinjaPhase.TurnResult&&StateClock>=3){if(++Active>=Players){Active=Players-1;Phase=NinjaPhase.Complete;}else NextAttempt();}}
  public bool Clear(int actor,int checkpoint){if(Phase!=NinjaPhase.Running||actor!=Active||checkpoint!=Checkpoint+1||checkpoint>Obstacles)return false;Checkpoint=checkpoint;cleared[actor]=checkpoint;return true;}
  public bool Fall(int actor){if(Phase!=NinjaPhase.Running||actor!=Active)return false;falls[actor]++;cleared[actor]=0;NextAttempt();return true;}
  public event Action<int,float> FinishConfirmed;
  public bool Bell(int actor){if(Phase!=NinjaPhase.Running||actor!=Active||Checkpoint!=Obstacles)return false;finished[actor]=true;adjustedTime[actor]=Clock+Penalty;Phase=NinjaPhase.TurnResult;StateClock=0;FinishConfirmed?.Invoke(actor,adjustedTime[actor]);return true;}
  public int Place(int slot){int rank=1;for(int i=0;i<Players;i++)if(i!=slot&&(finished[i]&&!finished[slot]||finished[i]&&finished[slot]&&(adjustedTime[i]<adjustedTime[slot]-.001f||Math.Abs(adjustedTime[i]-adjustedTime[slot])<=.001f&&falls[i]<falls[slot])||!finished[i]&&!finished[slot]&&cleared[i]>cleared[slot]))rank++;return rank;}
 }
}

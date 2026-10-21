using System;
using System.Collections.Generic;
namespace LWS.TruckFrogger
{
 public enum FrogPhase{Setup,Countdown,Driving,TurnResult,Complete}
 [Serializable]public sealed class FrogRules
 {
  [Serializable]public sealed class Attempt{public int participant,challenge,seed;public float seconds;public string reason;public bool success,tiebreak;}
  public int Players{get;private set;}public int Course{get;private set;}public int Active{get;private set;}public int Seed{get;private set;}public int CourseSeed=>unchecked(Seed+Course*4099+tiePass*131);public float Clock{get;private set;}public float TimeLimit{get;private set;}public FrogPhase Phase{get;private set;}public bool Complete=>Phase==FrogPhase.Complete;public bool Tiebreak=>tiePass>0;public int Winner{get;private set;}=-1;public string Announcement{get;private set;}="";
  public readonly int[] points=new int[8];public readonly float[,] times=new float[5,8];public IReadOnlyList<Attempt> History=>history;
  readonly List<Attempt> history=new List<Attempt>();readonly List<int> order=new List<int>();readonly float[] thisPass=new float[8];int selection,courseIndex,cursor,tiePass;bool matchTiebreak;
  public void Reset(int players,int option,int seed,float timeLimit=180)
  {
   if(players<1||players>8||option<0||option>5||!float.IsFinite(timeLimit)||timeLimit<15)throw new ArgumentOutOfRangeException();Players=players;selection=option;Seed=seed;TimeLimit=timeLimit;Course=option==0?0:option-1;courseIndex=cursor=tiePass=0;matchTiebreak=false;Winner=-1;Clock=0;Phase=FrogPhase.Setup;Announcement="";history.Clear();Array.Clear(points,0,8);for(int c=0;c<5;c++)for(int i=0;i<8;i++)times[c,i]=float.PositiveInfinity;order.Clear();
  }
  public void Begin(){if(Phase!=FrogPhase.Setup)return;PrepareCourse();}
  void PrepareCourse(){order.Clear();for(int i=0;i<Players;i++)order.Add((i+courseIndex)%Players);Array.Fill(thisPass,float.PositiveInfinity);cursor=tiePass=0;NextTurn();}
  void NextTurn(){Active=order[cursor];Clock=0;Phase=FrogPhase.Countdown;Announcement=(matchTiebreak?"MATCH TIEBREAK":Tiebreak?"CHALLENGE TIEBREAK":"ONE LIFE")+" · NEXT DRIVER "+(Active+1);}
  public void Tick(float dt)
  {
   if(!float.IsFinite(dt)||dt<=0)return;while(dt>0&&Phase!=FrogPhase.Setup&&Phase!=FrogPhase.Complete)
   {
    float limit=Phase==FrogPhase.Countdown?3:Phase==FrogPhase.Driving?TimeLimit:2.5f;float step=Math.Min(dt,Math.Max(0,limit-Clock));Clock+=step;dt-=step;if(Clock<limit)break;if(Phase==FrogPhase.Countdown){Phase=FrogPhase.Driving;Clock=0;}else if(Phase==FrogPhase.Driving)Fail(Active,"TIME LIMIT");else{cursor++;if(cursor<order.Count)NextTurn();else ResolveCourse();}
   }
  }
  public bool Finish(int actor,float crossingSeconds){if(Phase!=FrogPhase.Driving||actor!=Active||!float.IsFinite(crossingSeconds)||crossingSeconds<=0||crossingSeconds>Clock+.05f||crossingSeconds>TimeLimit)return false;Record(crossingSeconds,true,"FINISHED");return true;}
  public bool Fail(int actor,string reason){if(Phase!=FrogPhase.Driving||actor!=Active)return false;Record(float.PositiveInfinity,false,string.IsNullOrWhiteSpace(reason)?"ATTEMPT ENDED":reason);return true;}
  void Record(float seconds,bool success,string reason)
  {
   thisPass[Active]=seconds;if(!Tiebreak&&!matchTiebreak)times[Course,Active]=seconds;history.Add(new Attempt{participant=Active,challenge=Course,seed=CourseSeed,seconds=seconds,success=success,reason=reason,tiebreak=Tiebreak||matchTiebreak});Announcement=success?"FINISHED · "+seconds.ToString("F3")+"s":reason;Phase=FrogPhase.TurnResult;Clock=0;
  }
  void ResolveCourse()
  {
   float best=float.PositiveInfinity;var tied=new List<int>();foreach(int slot in order){float t=thisPass[slot];if(t<best-.0001f){best=t;tied.Clear();tied.Add(slot);}else if(float.IsFinite(t)&&Math.Abs(t-best)<=.0001f)tied.Add(slot);}
   if(tied.Count==1){if(matchTiebreak){Winner=tied[0];Announcement="PHYSICAL TIEBREAK WINNER";Phase=FrogPhase.Complete;return;}points[tied[0]]++;AdvanceCourse();return;}
   if(tied.Count>1||matchTiebreak||Tiebreak)
   {
    if(++tiePass>3){Announcement="SHARED DRAW · TIEBREAK ATTEMPTS EXHAUSTED";if(matchTiebreak)Phase=FrogPhase.Complete;else AdvanceCourse();return;}if(tied.Count>1){order.Clear();order.AddRange(tied);}cursor=0;Array.Fill(thisPass,float.PositiveInfinity);NextTurn();return;
   }
   Announcement="NO SUCCESSFUL FINISH · NO POINT AWARDED";AdvanceCourse();
  }
  void AdvanceCourse()
  {
   if(selection==0&&++courseIndex<5){Course=courseIndex;PrepareCourse();return;}
   int highest=-1;float best=float.PositiveInfinity;var leaders=new List<int>();for(int i=0;i<Players;i++)
   {
    float total=TieTime(i);if(points[i]>highest||points[i]==highest&&total<best-.0001f){highest=points[i];best=total;leaders.Clear();leaders.Add(i);}else if(points[i]==highest&&Math.Abs(total-best)<=.0001f)leaders.Add(i);
   }
   if(leaders.Count==1){Winner=leaders[0];Phase=FrogPhase.Complete;Announcement="MATCH COMPLETE";return;}Course=selection==0?4:selection-1;matchTiebreak=true;tiePass=1;order.Clear();order.AddRange(leaders);cursor=0;Array.Fill(thisPass,float.PositiveInfinity);NextTurn();
  }
  // Failed attempts use the advertised course limit for timing ties; a fast
  // failure can never beat someone who completed more of the same challenges.
  public float TieTime(int slot){float total=0;for(int c=0;c<5;c++)if(selection==0||c==selection-1)total+=float.IsFinite(times[c,slot])?times[c,slot]:TimeLimit;return total;}
  public int Place(int slot){if(Complete&&Winner>=0&&slot==Winner)return 1;int rank=1;for(int i=0;i<Players;i++)if(i!=slot&&(Complete&&i==Winner||points[i]>points[slot]||points[i]==points[slot]&&TieTime(i)<TieTime(slot)-.0001f))rank++;return rank;}
 }
 public static class FrogEventOrder
 {
  // Inputs are actual swept full-body crossing and genuine qualifying solid
  // contact times within one simulation step. Callback delivery is irrelevant.
  public static bool FinishWins(float crossing,float solidImpact)=>float.IsFinite(crossing)&&crossing>=0&&(!float.IsFinite(solidImpact)||crossing<solidImpact);
 }
}

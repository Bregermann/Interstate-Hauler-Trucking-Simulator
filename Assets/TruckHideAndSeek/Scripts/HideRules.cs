using System;
namespace LWS.TruckHideAndSeek
{
 public enum HidePhase{Setup,Hiding,Seeking,RoundEnd,Complete}
 public enum HideAwardKind{Found,Survival,Sweep}
 [Serializable]public sealed class HideRules
 {
  public int Players{get;private set;}public int Rounds{get;private set;}public int Round{get;private set;}public int Seeker=>Players>0?Round%Players:0;public HidePhase Phase{get;private set;}public float Clock{get;private set;}public float HidingSeconds{get;private set;}public float SeekingSeconds{get;private set;}public bool Complete=>Phase==HidePhase.Complete;public int Remaining{get;private set;}
  public readonly int[] points=new int[8],tags=new int[8],survived=new int[8],sweeps=new int[8];public readonly bool[] found=new bool[8];public bool[] seekerWins;
  public event Action<HideAwardKind,string,int,int,int> Awarded;
  void Award(HideAwardKind kind,string id,int slot,int delta){points[slot]+=delta;Awarded?.Invoke(kind,id,slot,delta,points[slot]);}
  public void Reset(int players,int rounds,float hidingSeconds=30,float seekingSeconds=180)
  {
   if(players<2||players>8||rounds<1||rounds>8||hidingSeconds<=0||seekingSeconds<=0||!float.IsFinite(hidingSeconds)||!float.IsFinite(seekingSeconds))throw new ArgumentOutOfRangeException();Players=players;Rounds=rounds;Round=0;HidingSeconds=hidingSeconds;SeekingSeconds=seekingSeconds;Phase=HidePhase.Setup;Clock=0;Remaining=players-1;foreach(var a in new[]{points,tags,survived,sweeps})Array.Clear(a,0,8);Array.Clear(found,0,8);seekerWins=new bool[rounds];
  }
  public void Begin(){if(Phase==HidePhase.Setup)BeginRound();}
  void BeginRound(){Array.Clear(found,0,8);Remaining=Players-1;Clock=0;Phase=HidePhase.Hiding;}
  public void Tick(float dt)
  {
   if(!float.IsFinite(dt)||dt<=0)return;while(dt>0&&Phase!=HidePhase.Setup&&Phase!=HidePhase.Complete){float limit=Phase==HidePhase.Hiding?HidingSeconds:Phase==HidePhase.Seeking?SeekingSeconds:2.5f;float consume=Math.Min(dt,Math.Max(0,limit-Clock));Clock+=consume;dt-=consume;if(Clock<limit)break;if(Phase==HidePhase.Hiding){Phase=HidePhase.Seeking;Clock=0;}else if(Phase==HidePhase.Seeking)FinishRound(false);else{Round++;if(Round>=Rounds){Round=Rounds-1;Phase=HidePhase.Complete;}else BeginRound();}}
  }
  public bool Tag(int actor,int target){if(Phase!=HidePhase.Seeking||actor!=Seeker||target<0||target>=Players||target==Seeker||found[target])return false;found[target]=true;Remaining--;tags[actor]++;Award(HideAwardKind.Found,"round:"+Round+":tag:"+target,actor,1);if(Remaining==0)FinishRound(true);return true;}
  void FinishRound(bool allFound){if(Phase!=HidePhase.Seeking)return;seekerWins[Round]=allFound;if(allFound){sweeps[Seeker]++;Award(HideAwardKind.Sweep,"round:"+Round+":sweep:"+Seeker,Seeker,3);}else for(int i=0;i<Players;i++)if(i!=Seeker&&!found[i]){survived[i]++;Award(HideAwardKind.Survival,"round:"+Round+":survival:"+i,i,2+(Remaining==1?1:0));}Phase=HidePhase.RoundEnd;Clock=0;}
  public int Place(int player){int place=1;for(int i=0;i<Players;i++)if(points[i]>points[player])place++;return place;}
 }
}

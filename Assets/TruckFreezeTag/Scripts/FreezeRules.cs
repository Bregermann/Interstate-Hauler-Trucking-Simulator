using System;
namespace LWS.TruckFreezeTag
{
 [Serializable]public sealed class FreezeRules
 {
  public int TeamA{get;private set;}public int TeamB{get;private set;}public int Players=>TeamA+TeamB;public int Round{get;private set;}public int Rounds{get;private set;}public int Freezers=>Round%2;public int RoundWinner{get;private set;}=-1;public int Winner{get;private set;}=-1;public float Clock{get;private set;}public float MatchClock{get;private set;}public float Duration{get;private set;}public bool Live{get;private set;}public bool Complete{get;private set;}
  public readonly float[] buildup=new float[8],protectedUntil=new float[8];public readonly bool[] frozen=new bool[8];public readonly int[] tags=new int[8],rescues=new int[8],wins=new int[2];
  public void Reset(int a,int b,int rounds=4,float seconds=90){if(a<1||a>4||b<1||b>4||rounds<2||rounds>6||rounds%2!=0||seconds<20||float.IsNaN(seconds)||float.IsInfinity(seconds))throw new ArgumentOutOfRangeException();TeamA=a;TeamB=b;Rounds=rounds;Duration=seconds;Round=0;MatchClock=0;Winner=-1;Complete=false;Array.Clear(tags,0,8);Array.Clear(rescues,0,8);Array.Clear(wins,0,2);ResetRound();}
  public int Team(int player)=>player<TeamA?0:1;
  void ResetRound(){Clock=0;RoundWinner=-1;Live=false;Array.Clear(buildup,0,8);Array.Clear(frozen,0,8);Array.Clear(protectedUntil,0,8);}
  public void Begin(){if(!Complete&&RoundWinner<0&&Rounds>0)Live=true;}
  public void Tick(float dt){if(!Live||dt<=0||float.IsNaN(dt)||float.IsInfinity(dt))return;Clock+=dt;MatchClock+=dt;if(Clock>=Duration)FinishRound(1-Freezers);}
  public bool Hit(int attacker,int victim,float amount=35){if(!Live||!Valid(attacker)||!Valid(victim)||Team(attacker)!=Freezers||Team(victim)==Freezers||frozen[victim]||Clock<protectedUntil[victim]||amount<=0||float.IsNaN(amount)||float.IsInfinity(amount))return false;buildup[victim]=Math.Min(100,buildup[victim]+Math.Min(50,amount));if(buildup[victim]>=100){frozen[victim]=true;tags[attacker]++;bool all=true;for(int i=0;i<Players;i++)if(Team(i)!=Freezers&&!frozen[i])all=false;if(all)FinishRound(Freezers);}return true;}
  public bool Rescue(int helper,int victim,float normalSpeed){if(!Live||!Valid(helper)||!Valid(victim)||helper==victim||Team(helper)!=Team(victim)||Team(helper)==Freezers||frozen[helper]||!frozen[victim]||float.IsNaN(normalSpeed)||float.IsInfinity(normalSpeed)||normalSpeed<2)return false;frozen[victim]=false;buildup[victim]=0;protectedUntil[victim]=Clock+2;rescues[helper]++;return true;}
  void FinishRound(int team){if(!Live||RoundWinner>=0)return;Live=false;RoundWinner=team;wins[team]++;if(Round+1>=Rounds){Complete=true;Winner=wins[0]==wins[1]?-1:wins[0]>wins[1]?0:1;}}
  public bool NextRound(){if(RoundWinner<0||Complete)return false;Round++;ResetRound();return true;}
  bool Valid(int p)=>p>=0&&p<Players;
 }
}

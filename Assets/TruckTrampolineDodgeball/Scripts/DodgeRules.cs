using System;
namespace LWS.TruckDodgeball {
public sealed class DodgeRules {
 public readonly bool[] alive=new bool[8];public readonly int[] wins=new int[2];public int TeamA{get;private set;}public int TeamB{get;private set;}public int Count=>TeamA+TeamB;public int Round{get;private set;}public int Winner{get;private set;}=-1;public int RoundWinner{get;private set;}=-1;public bool Locked{get;private set;}
 public int Team(int slot)=>slot<TeamA?0:1;public int Remaining(int team){int count=0;for(int i=0;i<Count;i++)if(alive[i]&&Team(i)==team)count++;return count;}
 public void Reset(int a,int b){TeamA=Math.Max(1,Math.Min(4,a));TeamB=Math.Max(1,Math.Min(4,b));wins[0]=wins[1]=0;Winner=-1;Round=0;NextRound();}
 public void NextRound(){Round++;Locked=false;RoundWinner=-1;for(int i=0;i<8;i++)alive[i]=i<Count;}
 public bool Eliminate(int slot){if(Locked||slot<0||slot>=Count||!alive[slot])return false;alive[slot]=false;return true;}
 // The match batches every ordered contact in a PhysX step before calling this once.
 public bool FinishStep(){if(Locked)return false;int a=Remaining(0),b=Remaining(1);if(a>0&&b>0)return false;Locked=true;RoundWinner=a==0&&b==0?-1:a>0?0:1;if(RoundWinner>=0&&++wins[RoundWinner]>=3)Winner=RoundWinner;return true;}
 public void Draw(){if(!Locked){Locked=true;RoundWinner=-1;}}
 // Earliest valid input window wins; equally timed contenders use physical distance, then a round-rotating tie rank.
 public static bool PreferStrike(float press,float distance,int rank,float priorPress,float priorDistance,int priorRank)=>press<priorPress-.00001f||Math.Abs(press-priorPress)<.00001f&&(distance<priorDistance-.0001f||Math.Abs(distance-priorDistance)<.0001f&&rank<priorRank);
 public static bool HitEliminates(int ballTeam,int targetTeam,bool sameStrikerGrace,bool returned)=>ballTeam>=0&&ballTeam!=targetTeam&&!sameStrikerGrace&&!returned;
}
}

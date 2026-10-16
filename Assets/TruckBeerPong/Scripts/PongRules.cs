using System;
namespace LWS.TruckBeerPong {
public enum PongPeriod{Normal,Rebuttal,Complete}
// Truck launch physics stays outside the house-rule authority.
public sealed class PongRules {
 public readonly bool[,] cups=new bool[2,10];public readonly int[] remaining=new int[2],shots=new int[2],hits=new int[2];readonly int[] next=new int[2];public int TeamA{get;private set;}public int TeamB{get;private set;}public bool Practice{get;private set;}public int ActiveTeam{get;private set;}public int Overtime{get;private set;}public int PracticeRacks{get;private set;}public PongPeriod Period{get;private set;}public int Winner{get;private set;}=-1;public bool ShotLocked{get;private set;}public int LastCup{get;private set;}=-1;public bool LastHit=>LastCup>=0;public bool Complete=>Period==PongPeriod.Complete;public int Shooter=>ActiveTeam==0?next[0]%TeamA:TeamA+next[1]%TeamB;public int NextTeammate=>ActiveTeam==0?(next[0]+1)%TeamA:TeamA+(next[1]+1)%TeamB;
 public void Reset(int teamA,int teamB,bool practice=false){Practice=practice;TeamA=practice?1:Math.Max(1,Math.Min(2,teamA));TeamB=Math.Max(1,Math.Min(2,teamB));ActiveTeam=Overtime=PracticeRacks=0;Winner=-1;Period=PongPeriod.Normal;ShotLocked=false;LastCup=-1;Array.Clear(shots,0,2);Array.Clear(hits,0,2);Array.Clear(next,0,2);Rack(10);}
 void Rack(int count){for(int team=0;team<2;team++){remaining[team]=count;for(int i=0;i<10;i++)cups[team,i]=i<count;}}
 public bool TryHit(int targetTeam,int cup){if(Complete||ShotLocked||targetTeam!=1-ActiveTeam||cup<0||cup>=10||!cups[targetTeam,cup])return false;cups[targetTeam,cup]=false;remaining[targetTeam]--;hits[ActiveTeam]++;shots[ActiveTeam]++;LastCup=cup;ShotLocked=true;return true;}
 public bool Miss(){if(Complete||ShotLocked)return false;LastCup=-1;shots[ActiveTeam]++;ShotLocked=true;return true;}
 public bool Advance(){if(Complete||!ShotLocked)return false;int shooter=ActiveTeam;next[shooter]=(next[shooter]+1)%(shooter==0?TeamA:TeamB);if(Practice){if(remaining[1]==0){PracticeRacks++;Rack(10);}ShotLocked=false;LastCup=-1;return true;}if(Period==PongPeriod.Rebuttal){if(!LastHit){Winner=1-shooter;Period=PongPeriod.Complete;return true;}if(remaining[1-shooter]==0){Overtime++;Period=PongPeriod.Normal;Rack(3);ActiveTeam=Overtime%2==1?1:0;}}else{if(remaining[1-shooter]==0)Period=PongPeriod.Rebuttal;ActiveTeam=1-shooter;}ShotLocked=false;LastCup=-1;return true;}
}
}

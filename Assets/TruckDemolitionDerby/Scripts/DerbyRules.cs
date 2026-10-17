using System;
namespace LWS.TruckDerby {
// Heat/series results have one owner; every elimination from one PhysX step is batched before locking.
public sealed class DerbyRules {
 public readonly bool[] alive=new bool[8];public readonly int[] wins=new int[8];public readonly string[] reasons=new string[8];public int Count{get;private set;}public int Heat{get;private set;}public int HeatWinner{get;private set;}=-1;public int Winner{get;private set;}=-1;public bool Locked{get;private set;}public int Remaining{get{int n=0;for(int i=0;i<Count;i++)if(alive[i])n++;return n;}}
 public void Reset(int count){Count=Math.Max(2,Math.Min(8,count));Array.Clear(wins,0,wins.Length);Heat=0;Winner=-1;NextHeat();}
 public void NextHeat(){Heat++;Locked=false;HeatWinner=-1;for(int i=0;i<8;i++){alive[i]=i<Count;reasons[i]="";}}
 public bool Eliminate(int slot,string reason){if(Locked||slot<0||slot>=Count||!alive[slot])return false;alive[slot]=false;reasons[slot]=reason;return true;}
 public bool FinishStep(){if(Locked)return false;int n=Remaining;if(n>1)return false;Locked=true;HeatWinner=-1;if(n==1){for(int i=0;i<Count;i++)if(alive[i]){HeatWinner=i;break;}if(++wins[HeatWinner]>=3)Winner=HeatWinner;}return true;}
}
}

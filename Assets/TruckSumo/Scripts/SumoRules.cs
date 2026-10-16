using System;
namespace LWS.TruckSumo {
public sealed class SumoRules {
 public readonly int[] wins=new int[4];public readonly bool[] alive=new bool[4];public int Count{get;private set;}public int Heat{get;private set;}public int Winner{get;private set;}=-1;public int HeatWinner{get;private set;}=-1;public bool Resolved{get;private set;}public bool Complete=>Winner>=0;
 public void Reset(int count){Count=Math.Max(2,Math.Min(4,count));Heat=0;Winner=-1;Array.Clear(wins,0,4);NextHeat();}
 public void NextHeat(){Heat++;Resolved=false;HeatWinner=-1;for(int i=0;i<4;i++)alive[i]=i<Count;}
 public bool Crossed(int slot){if(Resolved||slot<0||slot>=Count||!alive[slot])return false;alive[slot]=false;return true;}
 // Batch every crossing from one PhysX step before deciding winner/draw.
 public bool FinishStep(){if(Resolved)return false;int remaining=0,last=-1;for(int i=0;i<Count;i++)if(alive[i]){remaining++;last=i;}if(remaining>1)return false;Resolved=true;HeatWinner=last;if(last>=0&&++wins[last]>=5)Winner=last;return true;}
 public void TimeoutDraw(){if(!Resolved){Resolved=true;HeatWinner=-1;}}
 public static bool Outside(float x,float z,float radius)=>x*x+z*z>radius*radius;
}
}

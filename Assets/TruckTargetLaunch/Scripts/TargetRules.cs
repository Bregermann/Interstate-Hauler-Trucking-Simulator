using System;
namespace LWS.TruckTargetLaunch {
public sealed class TargetRules {
 public readonly int[] totals=new int[4],last=new int[4],attempts=new int[4];public readonly bool[] resolved=new bool[4];public int Count{get;private set;}public int Round{get;private set;}public int Eligible{get;private set;}public int Winner{get;private set;}=-1;public bool Complete=>Winner>=0;public int ExtraRound=>Math.Max(0,Round-5);
 public void Reset(int count){Count=Math.Max(1,Math.Min(4,count));Round=1;Eligible=(1<<Count)-1;Winner=-1;Array.Clear(totals,0,4);Array.Clear(last,0,4);Array.Clear(attempts,0,4);BeginRound();}
 public void BeginRound(){for(int i=0;i<4;i++){resolved[i]=i>=Count||(Eligible&(1<<i))==0;last[i]=0;}}
 public bool Lock(int slot,int points){if(Complete||slot<0||slot>=Count||resolved[slot])return false;if(points!=0&&points!=10&&points!=25&&points!=50&&points!=100)throw new ArgumentOutOfRangeException(nameof(points));resolved[slot]=true;last[slot]=points;totals[slot]+=points;attempts[slot]++;return true;}
 public bool RoundComplete{get{for(int i=0;i<Count;i++)if(!resolved[i])return false;return true;}}
 public bool Advance(){if(Complete||!RoundComplete)return false;if(Round<5){Round++;BeginRound();return true;}int best=int.MinValue,leaders=0,number=0,lastLeader=-1;for(int i=0;i<Count;i++)if((Eligible&(1<<i))!=0){if(totals[i]>best){best=totals[i];leaders=0;number=0;}if(totals[i]==best){leaders|=1<<i;number++;lastLeader=i;}}if(number==1){Winner=lastLeader;return true;}Eligible=leaders;Round++;BeginRound();return true;}
 // Target-local projected chassis center. Inclusive smaller zones take precedence.
 public static int Zone(float x,float z,float innerX=0,float innerZ=0){float precision=(x-innerX)*(x-innerX)+(z-innerZ)*(z-innerZ);float squared=x*x+z*z;if(precision<=2.3f*2.3f)return 100;if(precision<=8*8)return 50;if(squared<=18*18)return 25;if(squared<=30*30)return 10;return 0;}
 public static bool TopContact(float normalY,float incomingY,float pointHeight,float topHeight)=>normalY>.6f&&incomingY<-.1f&&pointHeight>=topHeight-.15f;
}
}

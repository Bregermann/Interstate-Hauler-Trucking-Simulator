using System;
namespace LWS.TruckSkeeBall {
// One delivery ID and one banked award per truck. Independent courses share only final tie resolution.
public sealed class SkeeRules {
 public readonly int[] totals=new int[8],attempts=new int[8],last=new int[8],required=new int[8];public int Count{get;private set;}public int Eligible{get;private set;}public int Winner{get;private set;}=-1;public bool Complete=>Winner>=0;public int ExtraRound{get;private set;}
 public void Reset(int count){Count=Math.Max(1,Math.Min(8,count));Eligible=(1<<Count)-1;Winner=-1;ExtraRound=0;Array.Clear(totals,0,8);Array.Clear(attempts,0,8);Array.Clear(last,0,8);for(int i=0;i<8;i++)required[i]=i<Count?9:0;}
 public bool CanLaunch(int slot)=>!Complete&&slot>=0&&slot<Count&&attempts[slot]<required[slot];
 public bool Lock(int slot,int delivery,int points){if(!CanLaunch(slot)||delivery!=attempts[slot]+1)return false;if(points!=0&&points!=10&&points!=20&&points!=30&&points!=40&&points!=50&&points!=100)throw new ArgumentOutOfRangeException(nameof(points));attempts[slot]++;last[slot]=points;totals[slot]+=points;for(int i=0;i<Count;i++)if(attempts[i]<required[i])return true;int best=-1,mask=0,n=0,winner=-1;for(int i=0;i<Count;i++)if((Eligible&(1<<i))!=0){if(totals[i]>best){best=totals[i];mask=n=0;}if(totals[i]==best){mask|=1<<i;n++;winner=i;}}if(n==1){Winner=winner;return true;}Eligible=mask;ExtraRound++;for(int i=0;i<Count;i++)if((mask&(1<<i))!=0)required[i]++;return true;}
}
}

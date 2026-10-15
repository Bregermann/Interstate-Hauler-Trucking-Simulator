using System;using UnityEngine;
namespace LWS.TruckSports {
// Shared placement-series bookkeeping; each sport supplies actual authoritative finish timestamps.
[Serializable]public sealed class SportsRaceSeries {
 public int count,round,completed,eligible,winner=-1;public bool placementTies,locked;public int[] total=new int[8],ranks=new int[8],finishes=new int[64];
 public void Reset(int competitors,bool comparePlacements){count=competitors;placementTies=comparePlacements;round=completed=0;eligible=(1<<count)-1;winner=-1;locked=false;Array.Clear(total,0,total.Length);Array.Clear(finishes,0,finishes.Length);}
 public void BeginRound(){round++;locked=false;Array.Clear(ranks,0,ranks.Length);}
 public bool Resolve(float[] times){if(locked)return false;locked=true;completed++;for(int i=0;i<count;i++){if((eligible&(1<<i))==0||float.IsInfinity(times[i])||float.IsNaN(times[i]))continue;int rank=1;for(int j=0;j<count;j++)if((eligible&(1<<j))!=0&&times[j]<times[i]-.0001f)rank++;ranks[i]=rank;total[i]+=count-rank+1;finishes[i*8+rank-1]++;}if(completed<3)return true;int leaders=0,best=int.MinValue;for(int i=0;i<count;i++)if((eligible&(1<<i))!=0){if(total[i]>best){best=total[i];leaders=1<<i;}else if(total[i]==best)leaders|=1<<i;}if(placementTies&&Count(leaders)>1)for(int place=0;place<count&&Count(leaders)>1;place++){int highest=-1,next=0;for(int i=0;i<count;i++)if((leaders&(1<<i))!=0){int n=finishes[i*8+place];if(n>highest){highest=n;next=1<<i;}else if(n==highest)next|=1<<i;}leaders=next;}if(Count(leaders)==1)winner=First(leaders);else eligible=leaders;return true;}
 public static int Count(int mask){int n=0;for(;mask!=0;mask&=mask-1)n++;return n;}public static int First(int mask){for(int i=0;i<31;i++)if((mask&(1<<i))!=0)return i;return -1;}
}
}

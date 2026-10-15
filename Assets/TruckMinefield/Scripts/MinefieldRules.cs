using System;using UnityEngine;
namespace LWS.TruckMinefield {
public enum MinefieldPhase {Countdown,Driving,Replay,HeatResult}
public enum MineAttempt {Driving,Launched,Landed,Failed,DrivenFinish}
public static class MinefieldRules {
 public const float Mile=1609.344f,HeatLimit=180;
 public static int Precision(float z)=>Mathf.Max(0,Mathf.RoundToInt(z*10));
 public static bool DrivenFinish(MineAttempt phase,float previous,float current,out float fraction){fraction=0;if(phase!=MineAttempt.Driving||previous>=Mile||current<Mile)return false;fraction=Mathf.InverseLerp(previous,current,Mile);return true;}
 public static int Leaders(int[] distance,int eligible){int best=-1,result=0;for(int i=0;i<distance.Length;i++)if((eligible&(1<<i))!=0&&distance[i]>=0){if(distance[i]>best){best=distance[i];result=1<<i;}else if(distance[i]==best)result|=1<<i;}return result;}
 public static int MatchLeader(int[] points,int eligible,int leaders,bool tieBreak,out int nextEligible,out bool nextTieBreak){nextEligible=eligible;nextTieBreak=tieBreak;if(tieBreak&&leaders!=0){if(Count(leaders)==1)return First(leaders);nextEligible=leaders;return -1;}int winners=0;for(int i=0;i<points.Length;i++)if(points[i]>=5&&(eligible&(1<<i))!=0)winners|=1<<i;if(Count(winners)==1)return First(winners);if(winners!=0){nextEligible=winners;nextTieBreak=true;}return -1;}
 public static int First(int mask){for(int i=0;i<31;i++)if((mask&(1<<i))!=0)return i;return -1;}
 public static int Count(int mask){int n=0;for(;mask!=0;mask&=mask-1)n++;return n;}
}
}

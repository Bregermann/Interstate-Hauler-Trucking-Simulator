using System;
namespace LWS.TruckSkyTow {
// Once-only ring outcomes; physics supplies the crossing and clearance.
public sealed class SkyTowRules {
 public readonly int[,] outcomes=new int[4,48];public readonly int[] score=new int[4],hits=new int[4],streak=new int[4];public readonly bool[] finished=new bool[4],winners=new bool[4];public int Count{get;private set;}public int Rings{get;private set;}public bool Complete{get;private set;}
 public void Start(int count,int rings){if(count<1||count>4||rings<1||rings>48)throw new ArgumentOutOfRangeException();Count=count;Rings=rings;Complete=false;Array.Clear(outcomes,0,outcomes.Length);Array.Clear(score,0,4);Array.Clear(hits,0,4);Array.Clear(streak,0,4);Array.Clear(finished,0,4);Array.Clear(winners,0,4);}
 public bool Ring(int slot,int ring,bool through,bool bonus,bool precise){if(Complete||slot<0||slot>=Count||ring<0||ring>=Rings||finished[slot]||outcomes[slot,ring]!=0)return false;outcomes[slot,ring]=through?1:-1;if(through){hits[slot]++;streak[slot]++;score[slot]+=(bonus?200:100)+(precise?25:0)+Math.Min(4,streak[slot]/3)*10;}else streak[slot]=0;return true;}
 public bool Finish(int slot){if(Complete||slot<0||slot>=Count||finished[slot])return false;for(int i=0;i<Rings;i++)if(outcomes[slot,i]==0)Ring(slot,i,false,false,false);finished[slot]=true;bool all=true;for(int i=0;i<Count;i++)all&=finished[i];if(all){Complete=true;int high=int.MinValue;for(int i=0;i<Count;i++)high=Math.Max(high,score[i]);for(int i=0;i<Count;i++)winners[i]=score[i]==high;}return true;}
 public static bool SweptOpening(float before,float after,float radialDistance,float radius,float bodyRadius,bool touched)=>before<0&&after>=0&&!touched&&radialDistance+bodyRadius<=radius;
}
}

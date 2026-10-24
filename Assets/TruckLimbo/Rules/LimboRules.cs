using System;
namespace LWS.TruckLimbo {
public enum LimboFormat {Elimination,BestHeight}
public sealed class LimboRules {
 public const int Capacity=8;public int Count{get;private set;}public int Round{get;private set;}public float Height{get;private set;}public bool Complete{get;private set;}public bool RoundComplete{get;private set;}public LimboFormat Format{get;private set;}
 public readonly bool[] active=new bool[Capacity],done=new bool[Capacity],cleared=new bool[Capacity],winners=new bool[Capacity];public readonly int[] points=new int[Capacity],clears=new int[Capacity];public readonly float[] best=new float[Capacity];float initial,step;int maximumRounds;
 public void Start(int count,LimboFormat format,float startingHeight,float heightStep,int rounds=9){if(count<1||count>Capacity||float.IsNaN(startingHeight)||float.IsInfinity(startingHeight)||startingHeight<=0||float.IsNaN(heightStep)||float.IsInfinity(heightStep)||heightStep<=0||rounds<1||startingHeight-heightStep*(rounds-1)<=0)throw new ArgumentException("Limbo needs 1–8 competitors and finite positive bar heights.");Count=count;Format=format;initial=Height=startingHeight;step=heightStep;maximumRounds=rounds;Round=1;Complete=RoundComplete=false;Array.Clear(points,0,Capacity);Array.Clear(clears,0,Capacity);Array.Clear(done,0,Capacity);Array.Clear(cleared,0,Capacity);Array.Clear(winners,0,Capacity);for(int i=0;i<Capacity;i++){active[i]=i<count;best[i]=float.PositiveInfinity;}}
 public bool ResolveAttempt(int slot,bool success,bool nearBar,bool controlledStyle){if(Complete||RoundComplete||slot<0||slot>=Count||!active[slot]||done[slot])return false;done[slot]=true;cleared[slot]=success;if(success){best[slot]=Math.Min(best[slot],Height);clears[slot]++;points[slot]+=100+(Round-1)*25+(nearBar?25:0)+(controlledStyle?25:0);}bool all=true;for(int i=0;i<Count;i++)if(active[i]&&!done[i])all=false;RoundComplete=all;return true;}
 public bool NextRound(){if(Complete||!RoundComplete)return false;int survivors=0,last=-1;for(int i=0;i<Count;i++)if(active[i]&&cleared[i]){survivors++;last=i;}
  if(Format==LimboFormat.Elimination&&(survivors==0||Count>1&&survivors==1)){Complete=true;if(survivors==1)winners[last]=true;else Rank();return false;}
  if(Round>=maximumRounds){Complete=true;Rank();return false;}
  for(int i=0;i<Count;i++){if(Format==LimboFormat.Elimination)active[i]&=cleared[i];done[i]=cleared[i]=false;}Round++;Height=initial-(Round-1)*step;RoundComplete=false;return true;
 }
 // Read-only display order: preserve all declared winners, then the existing
 // lowest-bar/point-tiebreak ordering. Never change winners or awarded points.
 public int ResultPlace(int slot){
  if(slot<0||slot>=Count)throw new ArgumentOutOfRangeException(nameof(slot));
  if(winners[slot])return 1;
  int place=1;for(int i=0;i<Count;i++){
   if(winners[i]){place++;continue;}
   if(best[i]<best[slot]-.0001f||Equal(best[i],best[slot])&&points[i]>points[slot])place++;
  }return place;
 }
 void Rank(){float lowest=float.PositiveInfinity;int score=-1;for(int i=0;i<Count;i++){if(best[i]<lowest-.0001f){lowest=best[i];score=points[i];}else if(Equal(best[i],lowest))score=Math.Max(score,points[i]);}for(int i=0;i<Count;i++)winners[i]=Equal(best[i],lowest)&&points[i]==score;}
 static bool Equal(float a,float b)=>a==b||Math.Abs(a-b)<.0001f;
 // Actual swept world bounds are supplied by the body's collider adapter.
 // These helpers never replace solid-bar collision detection.
 public static bool ValidEntry(float previousFront,float front,float plane,float minX,float maxX,float left,float right,bool grounded)=>grounded&&previousFront<plane&&front>=plane&&minX>=left&&maxX<=right;
 public static bool FullClearance(float rear,float plane,bool entered,bool actualBarHit,float barDisplacement)=>entered&&!actualBarHit&&barDisplacement<=.06f&&rear>plane+.08f;
}
}

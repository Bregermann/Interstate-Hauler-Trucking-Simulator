using System;
namespace LWS.TruckSnake {
 public enum SnakeMode {Endless,Timed,Competitive}
 public sealed class SnakeRules {
  public const int StartingTrailers=1;
  public readonly int[] attached=new int[4];
  public readonly int[] trailers=new int[4],collected=new int[4],combo=new int[4],score=new int[4],jumps=new int[4];
  public readonly float[] distance=new float[4],survived=new float[4];public readonly bool[] crashed=new bool[4],winners=new bool[4];
  readonly float[] lastPickup=new float[4];public int Count{get;private set;}public SnakeMode Mode{get;private set;}public float Clock{get;private set;}public float Seconds{get;private set;}public bool Complete{get;private set;}
  public void Start(int count,SnakeMode mode,float seconds=180){if(count<1||count>4||!Finite(seconds)||seconds<=0)throw new ArgumentOutOfRangeException();Count=count;Mode=mode;Seconds=seconds;Clock=0;Complete=false;Array.Clear(trailers,0,4);Array.Clear(attached,0,4);Array.Clear(collected,0,4);Array.Clear(combo,0,4);Array.Clear(score,0,4);Array.Clear(jumps,0,4);Array.Clear(distance,0,4);Array.Clear(survived,0,4);Array.Clear(crashed,0,4);Array.Clear(winners,0,4);for(int i=0;i<4;i++){lastPickup[i]=-100;if(i<Count)trailers[i]=StartingTrailers;}}
  public bool Playing(int slot)=>slot>=0&&slot<Count&&!Complete&&!crashed[slot];
  public int ScoreMultiplier(int slot)=>slot>=0&&slot<Count?Math.Max(1,attached[slot]):1;
  public void SetAttached(int slot,int count){if(slot<0||slot>=Count||count<0||count>trailers[slot])throw new ArgumentOutOfRangeException();attached[slot]=count;}
  public bool Pickup(int slot,bool golden){if(!Playing(slot))return false;int gain=golden?3:1;combo[slot]=Clock-lastPickup[slot]<12?Math.Min(combo[slot]+1,8):1;lastPickup[slot]=Clock;trailers[slot]+=gain;collected[slot]+=gain;score[slot]+=(gain*100+combo[slot]*10)*ScoreMultiplier(slot);return true;}
  public bool LoseTail(int slot,int count){if(!Playing(slot)||count<=0||count>trailers[slot])return false;trailers[slot]-=count;attached[slot]=Math.Min(attached[slot],trailers[slot]);score[slot]=Math.Max(0,score[slot]-count*60);combo[slot]=0;return true;}
  public bool LandJump(int slot,float airborneSeconds,float travelled){if(!Playing(slot)||airborneSeconds<.6f||travelled<8||!Finite(travelled))return false;jumps[slot]++;score[slot]+=Math.Min(300,(int)(travelled*4))*ScoreMultiplier(slot);return true;}
  public void Step(float dt,float[] metres){if(Complete)return;if(!Finite(dt)||dt<0)throw new ArgumentOutOfRangeException();Clock+=dt;for(int i=0;i<Count;i++)if(!crashed[i]){survived[i]+=dt;if(metres!=null&&i<metres.Length&&Finite(metres[i])&&metres[i]>0){float before=distance[i];distance[i]+=metres[i];score[i]+=((int)(distance[i]/10)-(int)(before/10))*ScoreMultiplier(i);}score[i]+=((int)survived[i]-(int)(survived[i]-dt))*ScoreMultiplier(i);if(Clock-lastPickup[i]>=12)combo[i]=0;}bool alive=false;for(int i=0;i<Count;i++)alive|=!crashed[i];if(!alive||Mode==SnakeMode.Timed&&Clock>=Seconds)Finish();}
  public bool Crash(int slot){if(!Playing(slot))return false;crashed[slot]=true;combo[slot]=0;bool alive=false;for(int i=0;i<Count;i++)alive|=!crashed[i];if(!alive)Finish();return true;}
  void Finish(){Complete=true;int best=int.MinValue;for(int i=0;i<Count;i++)best=Math.Max(best,score[i]);for(int i=0;i<Count;i++)winners[i]=score[i]==best;}
  static bool Finite(float v)=>!float.IsNaN(v)&&!float.IsInfinity(v);
 }
}

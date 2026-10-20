using System;
namespace LWS.TruckSheepHerding
{
 // Only the physical pen adapter calls Secure. Sheep identity, not contact count,
 // owns the one-time award. Ordinary collisions never call this authority.
 [Serializable] public sealed class HerdRules
 {
  public int Players{get;private set;}public float Clock{get;private set;}public float Duration{get;private set;}public bool Live{get;private set;}public bool Complete{get;private set;}public int Remaining{get;private set;}
  public int[] owners;public bool[] detonated;bool[] explosive;
  public readonly int[] points=new int[4];public readonly float[] lastCompletion=new float[4];
  public void Reset(int players,bool[] hazards,float duration=180)
  {
   if(players<1||players>4||hazards==null||hazards.Length<4||hazards.Length>128||duration<30||float.IsNaN(duration)||float.IsInfinity(duration))throw new ArgumentOutOfRangeException();
   Players=players;Duration=duration;Clock=0;Live=Complete=false;Remaining=0;explosive=(bool[])hazards.Clone();owners=new int[hazards.Length];Array.Fill(owners,-1);detonated=new bool[hazards.Length];Array.Clear(points,0,4);Array.Fill(lastCompletion,float.PositiveInfinity);foreach(bool hazard in hazards)if(!hazard)Remaining++;
  }
  public void Begin(){if(owners!=null&&!Complete)Live=true;}
  public void Tick(float dt){if(!Live||dt<=0||float.IsNaN(dt)||float.IsInfinity(dt))return;Clock=Math.Min(Duration,Clock+dt);if(Clock>=Duration){Live=false;Complete=true;}}
  public bool Secure(int sheep,int pen)
  {
   if(!Live||sheep<0||sheep>=owners.Length||pen<0||pen>=Players||explosive[sheep]||detonated[sheep]||owners[sheep]>=0)return false;
   owners[sheep]=pen;points[pen]++;lastCompletion[pen]=Clock;Remaining--;if(Remaining==0){Complete=true;Live=false;}return true;
  }
  public bool Detonate(int sheep){if(!Live||sheep<0||sheep>=owners.Length||!explosive[sheep]||detonated[sheep]||owners[sheep]>=0)return false;detonated[sheep]=true;return true;}
  public int Place(int slot){int rank=1;for(int i=0;i<Players;i++)if(points[i]>points[slot]||points[i]==points[slot]&&points[i]>0&&lastCompletion[i]<lastCompletion[slot]-.0001f)rank++;return rank;}
 }
}

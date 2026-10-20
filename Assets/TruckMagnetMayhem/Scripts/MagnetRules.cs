using System;
namespace LWS.TruckMagnetMayhem
{
 [Serializable]public sealed class MagnetRules
 {
  public int Players{get;private set;}public float Clock{get;private set;}public float Duration{get;private set;}public bool Live{get;private set;}public bool Complete{get;private set;}public int Remaining{get;private set;}
  public readonly int[] points=new int[8],deliveries=new int[8];public int[] owners;int[] values,pending;float[] settled;bool endOnClear;
  public float RequiredSettle=1.5f,MaximumSettleSpeed=.75f,MaximumSettleSpin=.6f;
  public void Reset(int players,int[] objectValues,float seconds,bool finishWhenClear=true)
  {
   if(players<1||players>8||objectValues==null||objectValues.Length<1||objectValues.Length>128||seconds<30||!Finite(seconds))throw new ArgumentOutOfRangeException();
   foreach(int value in objectValues)if(value<=0)throw new ArgumentOutOfRangeException(nameof(objectValues));
   Players=players;values=(int[])objectValues.Clone();owners=new int[values.Length];pending=new int[values.Length];settled=new float[values.Length];Array.Fill(owners,-1);Array.Fill(pending,-1);Array.Clear(points,0,8);Array.Clear(deliveries,0,8);Clock=0;Duration=seconds;Live=Complete=false;Remaining=values.Length;endOnClear=finishWhenClear;
  }
  public void Begin(){if(values==null)throw new InvalidOperationException("Configure the scrapyard before starting.");if(!Complete)Live=true;}
  public void Tick(float dt){if(!Live||Complete||dt<=0||!Finite(dt))return;Clock=Math.Min(Duration,Clock+dt);if(Clock>=Duration)Finish();}
  public bool Observe(int item,int bay,bool whollyContained,float speed,float spin,float dt)
  {
   if(!Live||Complete||item<0||item>=values.Length||owners[item]>=0||dt<=0||!Finite(dt))return false;
   if(bay<0||bay>=Players||!whollyContained||!Finite(speed)||!Finite(spin)||speed<0||spin<0||speed>MaximumSettleSpeed||spin>MaximumSettleSpin){pending[item]=-1;settled[item]=0;return false;}
   if(pending[item]!=bay){pending[item]=bay;settled[item]=0;}settled[item]+=dt;if(settled[item]+.00001f<RequiredSettle)return false;
   owners[item]=bay;points[bay]+=values[item];deliveries[bay]++;Remaining--;if(endOnClear&&Remaining==0)Finish();return true;
  }
  public void Finish(){if(!Live||Complete)return;Live=false;Complete=true;}
  public int Place(int slot){int rank=1;for(int i=0;i<Players;i++)if(points[i]>points[slot])rank++;return rank;}
  static bool Finite(float v)=>!float.IsNaN(v)&&!float.IsInfinity(v);
 }
}

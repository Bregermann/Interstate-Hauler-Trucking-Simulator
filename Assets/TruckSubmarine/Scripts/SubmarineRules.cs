using System;
namespace LWS.TruckSubmarine {
 public enum SubmarinePhase { Ready, Diving, Complete }
 public enum SubmarineCargoKind { Chest, Gold, Artifact, Cargo, Salvage }
 // Delivery/life/pressure semantics only; Archimeds and NWH own physical motion.
 [Serializable] public sealed class SubmarineRules {
  public const int Capacity=4, CargoCapacity=25;
  public readonly int[] points=new int[4],deliveries=new int[4],carried={-1,-1,-1,-1};
  public readonly float[] integrity=new float[4],deepest=new float[4];
  public readonly bool[] retired=new bool[4];
  readonly bool[] banked=new bool[CargoCapacity]; readonly int[] owners=new int[CargoCapacity];
  public SubmarinePhase Phase{get;private set;} public int Count{get;private set;} public float Clock{get;private set;} public float Remaining{get;private set;} public float RatedDepth{get;private set;} public bool Complete=>Phase==SubmarinePhase.Complete;
  public void Reset(int count,int option){Count=Math.Clamp(count,1,4);Clock=0;Remaining=option==0?180:option==1?240:300;RatedDepth=option==0?90:option==1?120:150;Phase=SubmarinePhase.Ready;Array.Clear(points,0,4);Array.Clear(deliveries,0,4);Array.Clear(deepest,0,4);Array.Clear(retired,0,4);Array.Clear(banked,0,banked.Length);for(int i=0;i<4;i++){integrity[i]=100;carried[i]=-1;}Array.Fill(owners,-1);}
  public void Tick(float dt){if(Complete||!float.IsFinite(dt)||dt<=0)return;dt=Math.Min(dt,.1f);Clock+=dt;if(Phase==SubmarinePhase.Ready){if(Clock>=3)Phase=SubmarinePhase.Diving;return;}Remaining=Math.Max(0,Remaining-dt);bool any=false;for(int i=0;i<Count;i++)any|=!retired[i];if(Remaining<=0||!any)Phase=SubmarinePhase.Complete;}
  public bool Active(int slot)=>Phase==SubmarinePhase.Diving&&slot>=0&&slot<Count&&!retired[slot];
  public bool Grab(int slot,int id){if(!Active(slot)||id<0||id>=CargoCapacity||banked[id]||owners[id]>=0||carried[slot]>=0)return false;owners[id]=slot;carried[slot]=id;return true;}
  public bool Release(int slot,int id){if(slot<0||slot>=Count||id<0||id>=CargoCapacity||owners[id]!=slot||carried[slot]!=id)return false;owners[id]=-1;carried[slot]=-1;return true;}
  public int Bank(int slot,int id,SubmarineCargoKind kind,float originalDepth){if(!Active(slot)||id<0||id>=CargoCapacity||banked[id]||owners[id]!=slot||carried[slot]!=id||!float.IsFinite(originalDepth))return 0;int value=Value(kind,originalDepth);banked[id]=true;owners[id]=-1;carried[slot]=-1;deliveries[slot]++;points[slot]+=value;return value;}
  public static int Value(SubmarineCargoKind kind,float depth){if(!float.IsFinite(depth))return 0;int baseValue=kind==SubmarineCargoKind.Gold?50:kind==SubmarineCargoKind.Artifact?40:kind==SubmarineCargoKind.Chest?30:kind==SubmarineCargoKind.Cargo?25:20;return baseValue*(1+Math.Clamp((int)(Math.Max(0,depth)/45),0,3));}
  public bool Pressure(int slot,float depth,float dt){if(!Active(slot)||!float.IsFinite(depth)||!float.IsFinite(dt)||dt<=0)return false;dt=Math.Min(dt,.1f);deepest[slot]=Math.Max(deepest[slot],Math.Max(0,depth));integrity[slot]=Math.Clamp(integrity[slot]+(depth>RatedDepth?-Math.Min(9,1.5f+(depth-RatedDepth)*.15f):.8f)*dt,0,100);if(integrity[slot]>0)return false;retired[slot]=true;return true;}
  public int Place(int slot){int place=1;for(int i=0;i<Count;i++)if(points[i]>points[slot])place++;return place;}
  public bool Available(int id)=>id>=0&&id<CargoCapacity&&!banked[id]&&owners[id]<0;
 }
}

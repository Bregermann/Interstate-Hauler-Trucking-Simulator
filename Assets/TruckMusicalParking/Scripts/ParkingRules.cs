using System;
using UnityEngine;
namespace LWS.TruckMusicalParking {
public readonly struct ParkingResolution {
 public readonly int SecuredMask,EliminatedMask,Remaining,Winner;public readonly bool Replay;
 public ParkingResolution(int secured,int eliminated,int remaining,int winner,bool replay){SecuredMask=secured;EliminatedMask=eliminated;Remaining=remaining;Winner=winner;Replay=replay;}
}
public static class ParkingRules {
 public const float CaptureSeconds=1,PaintTolerance=.08f,InnerBand=.76f,OuterBand=1.22f;
 public static float Radius(Vector3 position)=>new Vector2(position.x/30,position.z/23).magnitude;
 public static bool InBand(Vector3 p,float tolerance=.045f){float r=Radius(p);return r>=InnerBand-tolerance&&r<=OuterBand+tolerance;}
 public static bool Contains(BoxCollider shape,Rigidbody body,Vector3 center,Quaternion rotation,Vector2 size){var inverse=Quaternion.Inverse(rotation);for(int i=0;i<8;i++){var local=shape.center+Vector3.Scale(shape.size*.5f,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));var p=body.position+body.rotation*Vector3.Scale(local,shape.transform.lossyScale);p=inverse*(p-center);if(Mathf.Abs(p.x)>size.x*.5f+PaintTolerance||Mathf.Abs(p.z)>size.y*.5f+PaintTolerance)return false;}return true;}
 public static bool BetterClaim(float a,int aSlot,float b,int bSlot,int tieOffset)=>a<b-.0001f||Mathf.Abs(a-b)<.0001f&&(aSlot-tieOffset+8)%8<(bSlot-tieOffset+8)%8;
 public static ParkingResolution Resolve(bool[] active,int[] secured,int[] owners){if(active.Length!=8||secured.Length!=8||owners.Length!=8)throw new ArgumentException("Eight-slot snapshot required");int alive=0,parked=0;for(int i=0;i<8;i++)if(active[i]){alive|=1<<i;int bay=secured[i];if(bay>=0&&bay<8&&owners[bay]==i)parked|=1<<i;}if(parked==0)return new ParkingResolution(0,0,Count(alive),-1,true);int eliminated=alive&~parked,remaining=Count(parked);return new ParkingResolution(parked,eliminated,remaining,remaining==1?First(parked):-1,false);}
 public static int Count(int mask){int n=0;for(;mask!=0;mask&=mask-1)n++;return n;}
 public static int First(int mask){for(int i=0;i<8;i++)if((mask&(1<<i))!=0)return i;return -1;}
}
}

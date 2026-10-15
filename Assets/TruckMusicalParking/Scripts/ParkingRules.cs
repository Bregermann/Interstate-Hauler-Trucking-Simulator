using UnityEngine;
namespace LWS.TruckMusicalParking {
public static class ParkingRules {
 public const float CaptureSeconds=1.0f;
 public static bool Contains(BoxCollider shape,Rigidbody body,Vector3 center,Quaternion rotation,Vector2 size){var inverse=Quaternion.Inverse(rotation);for(int i=0;i<4;i++){var local=shape.center+new Vector3((i%2==0?-1:1)*shape.size.x*.5f,0,(i<2?-1:1)*shape.size.z*.5f);var p=body.position+body.rotation*Vector3.Scale(local,shape.transform.lossyScale);p=inverse*(p-center);if(Mathf.Abs(p.x)>size.x*.5f-.06f||Mathf.Abs(p.z)>size.y*.5f-.06f)return false;}return true;}
 public static bool BetterClaim(float a,int aSlot,float b,int bSlot,int tieOffset)=>a<b-.0001f||Mathf.Abs(a-b)<.0001f&&(aSlot-tieOffset+8)%8<(bSlot-tieOffset+8)%8;
 public static int Lowest(bool[] active,int[] secured,float[] progress,float[] distance,int rotation){int loser=-1;for(int i=0;i<8;i++){if(!active[i]||secured[i]>=0)continue;if(loser<0||progress[i]<progress[loser]-.0001f||Mathf.Abs(progress[i]-progress[loser])<.0001f&&(distance[i]>distance[loser]+.001f||Mathf.Abs(distance[i]-distance[loser])<.001f&&(i-rotation+8)%8<(loser-rotation+8)%8))loser=i;}return loser;}
}
}

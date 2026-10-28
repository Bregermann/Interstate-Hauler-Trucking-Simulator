using LWS.TruckTeaParty;using UnityEngine;
namespace LWS.TruckBakeOff {
 public sealed class BakeEgg:MonoBehaviour,IServingNativeContact {
  public BakeMatch match;public ServingVessel vessel;public bool Cracked{get;private set;}
  public void ResetEgg(){Cracked=false;}
  public bool NativeImpact(ServingVessel source,Collision collision){if(Cracked||source!=vessel||source.contents!=ServingContents.Egg||!match.rules.Active||collision.contactCount==0)return false;var bowl=collision.collider.GetComponentInParent<BakeBowl>();if(bowl==null||bowl.match!=match)return false;float speed=Mathf.Abs(Vector3.Dot(collision.relativeVelocity,collision.GetContact(0).normal));if(speed<1.8f)return false;float amount=source.Volume;float accepted=bowl.Receive(source,ServingContents.Egg,amount,collision.GetContact(0).point);if(accepted<=0)return false;source.Take(accepted);Cracked=true;match.NativeEggCracks++;source.ReceiveAllDamage();return true;}
 }
}

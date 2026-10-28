using LWS.TruckTeaParty;using UnityEngine;
namespace LWS.TruckBakeOff {
 public sealed class BakeBowl:ServingReceiver {
  public BakeMatch match;public ServingVessel vessel;public int slot;public ConfigurableJoint stand;float whiskAt=-100;
  public override float Receive(ServingVessel source,ServingContents kind,float amount,Vector3 point){if(!match.Live||!match.rules.Active||source.session!=match||vessel.Broken||match.rules.stages[slot]>BakeStage.Mixing)return 0;var local=vessel.transform.InverseTransformPoint(point);if(new Vector2(local.x,local.z).sqrMagnitude>.82f*.82f)return 0;BakeIngredient ingredient;if(kind==ServingContents.Flour)ingredient=BakeIngredient.Flour;else if(kind==ServingContents.Milk)ingredient=BakeIngredient.Milk;else if(kind==ServingContents.Sugar)ingredient=BakeIngredient.Sugar;else if(kind==ServingContents.Egg)ingredient=BakeIngredient.Egg;else return 0;float accepted=vessel.Accept(amount);if(accepted>0)match.rules.Measure(slot,ingredient,accepted);return accepted;}
  void OnTriggerStay(Collider other){var tool=other.GetComponentInParent<BakeWhisk>();if(tool!=null&&tool.match==match&&tool.arm.truck.slot==slot&&tool.Running&&tool.AngularSpeed>3)whiskAt=match.rules.Clock;}
  public void Prepare(){whiskAt=-100;stand.xMotion=stand.yMotion=stand.zMotion=ConfigurableJointMotion.Locked;stand.angularXMotion=stand.angularYMotion=stand.angularZMotion=ConfigurableJointMotion.Locked;vessel.PickupAllowed=false;vessel.pourable=false;}
  public void Tick(float dt){if(vessel.Broken){match.rules.Fail(slot);return;}if(match.rules.Mix(slot,dt,match.rules.Clock-whiskAt<.12f))match.NativeMixSteps++;if(match.rules.stages[slot]==BakeStage.Mixed){vessel.SetPreparedContents(ServingContents.Batter,vessel.Volume);vessel.pourable=true;vessel.PickupAllowed=true;stand.xMotion=stand.yMotion=stand.zMotion=ConfigurableJointMotion.Free;stand.angularXMotion=stand.angularYMotion=stand.angularZMotion=ConfigurableJointMotion.Free;}}
 }
}

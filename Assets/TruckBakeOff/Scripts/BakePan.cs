using LWS.TruckTeaParty;using UnityEngine;
namespace LWS.TruckBakeOff {
 public sealed class BakePan:ServingReceiver {
  public BakeMatch match;public ServingVessel vessel;public BakeBowl bowl;public int slot;public Renderer cake;public Transform[] icingPetals;public Collider batterOpening,creamOpening;
  public override float Receive(ServingVessel source,ServingContents contents,float amount,Vector3 point){if(!match.Live||!match.rules.Active||vessel.Broken||vessel.Banked||source.session!=match)return 0;var local=vessel.transform.InverseTransformPoint(point);if(new Vector2(local.x,local.z).sqrMagnitude>.82f*.82f)return 0;
   if(contents==ServingContents.Batter&&source==bowl.vessel&&match.rules.stages[slot]>=BakeStage.Mixed&&match.rules.stages[slot]<=BakeStage.Loaded){float accepted=vessel.Accept(amount);if(accepted>0&&match.rules.LoadPan(slot,accepted))return accepted;return 0;}
   if(contents==ServingContents.Cream&&source==match.nozzles[slot]&&(match.rules.stages[slot]==BakeStage.Cooked||match.rules.stages[slot]==BakeStage.Decorated)){int q=(local.x>=0?1:0)+(local.z>=0?2:0);if(match.rules.Decorate(slot,q,amount)){match.NativeDecorationHits++;return amount;}}
   return 0;}
  public void Tick(){var stage=match.rules.stages[slot];if(vessel.Broken&&stage!=BakeStage.Presented){match.rules.Fail(slot);return;}cake.gameObject.SetActive(stage>=BakeStage.Cooked&&stage!=BakeStage.Failed);batterOpening.gameObject.SetActive(stage<=BakeStage.Loaded);creamOpening.gameObject.SetActive(stage>=BakeStage.Cooked&&stage!=BakeStage.Failed);for(int i=0;i<icingPetals.Length;i++)icingPetals[i].gameObject.SetActive(stage!=BakeStage.Failed&&(match.rules.decorated[slot]&(1<<i))!=0);}
 }
}

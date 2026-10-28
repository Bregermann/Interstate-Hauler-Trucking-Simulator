using UnityEngine;
namespace LWS.TruckTeaParty {
 public sealed class TeaCup:ServingReceiver {
  public TeaMatch match;public ServingVessel vessel;public int cupId,guestId;
  public override float Receive(ServingVessel source,ServingContents contents,float amount,Vector3 point){if(!match.Live||!match.rules.Active||contents!=ServingContents.Tea||vessel.Broken||vessel.Banked)return 0;var local=vessel.transform.InverseTransformPoint(point);if(new Vector2(local.x,local.z).sqrMagnitude>.82f*.82f)return 0;return vessel.Accept(amount);}
 }
}

using UnityEngine;using LWS.TruckSports;using LWS.TruckTeaParty;
namespace LWS.TruckBakeOff {
 public sealed class BakeBowlOpening:ServingReceiver {public BakeBowl bowl;public override float Receive(ServingVessel source,ServingContents kind,float amount,Vector3 point)=>bowl.Receive(source,kind,amount,point);}
}

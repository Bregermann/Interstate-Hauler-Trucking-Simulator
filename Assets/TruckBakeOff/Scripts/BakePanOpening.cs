using UnityEngine;using LWS.TruckSports;using LWS.TruckTeaParty;
namespace LWS.TruckBakeOff {
 public sealed class BakePanOpening:ServingReceiver {public BakePan pan;public bool cream;public override float Receive(ServingVessel source,ServingContents kind,float amount,Vector3 point)=>cream!=(kind==ServingContents.Cream)?0:pan.Receive(source,kind,amount,point);}
}

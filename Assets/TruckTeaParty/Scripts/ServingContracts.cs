using LWS.TruckSports;using UnityEngine;
namespace LWS.TruckTeaParty {
 public enum ServingContents { Tea,Milk,Flour,Sugar,Egg,Batter,Cream }
 public interface IServingNativeContact {bool NativeImpact(ServingVessel source,Collision collision);}
 public interface IServingEvents {void Spilled(ServingVessel source,float amount,Vector3 point);void DishBroken(ServingVessel source,Vector3 point);}
 // A domain receiver, not a replacement for fluid/weather/vehicle frameworks.
 // Only the actual ballistic stream or native object contact invokes it.
 public abstract class ServingReceiver:MonoBehaviour {public abstract float Receive(ServingVessel source,ServingContents contents,float amount,Vector3 point);}
}

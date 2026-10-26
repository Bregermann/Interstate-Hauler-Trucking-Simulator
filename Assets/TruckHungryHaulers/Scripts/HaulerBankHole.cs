using UnityEngine;
namespace LWS.TruckHungryHaulers
{
 public sealed class HaulerBankHole:MonoBehaviour
 {
  public HaulerMatch match;public int slot;public Transform pit;public BoxCollider collection;
  public void Align()=>pit.rotation=Quaternion.identity;
  void OnTriggerEnter(Collider other)=>Collect(other);
  void OnTriggerStay(Collider other)=>Collect(other);
  void Collect(Collider other)
  {
   var ball=other.attachedRigidbody!=null?other.attachedRigidbody.GetComponent<HaulerBall>():null;
   if(ball!=null&&other==ball.shape&&!ball.body.isKinematic&&ball.body.worldCenterOfMass.y<pit.position.y-.6f&&collection.bounds.Contains(ball.body.worldCenterOfMass))match.BankBall(this,ball);
  }
 }
}

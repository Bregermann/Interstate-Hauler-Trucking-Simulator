using UnityEngine;
namespace LWS.TruckSheepHerding
{
 public sealed class HerdPenVolume:MonoBehaviour
 {
  public HerdPen pen;
  void OnTriggerStay(Collider other){var animal=other.GetComponentInParent<HerdSheep>();if(animal!=null&&pen.ContainsWhole(animal))pen.match.Secure(animal,pen.owner);}
 }
}

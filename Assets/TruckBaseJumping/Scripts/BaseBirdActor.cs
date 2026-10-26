using UnityEngine;

namespace LWS.TruckBaseJumping
{
 // The flock is the only flight/contact authority. This callback just forwards
 // a real PhysX collision; artist clips retain ownership of wing animation.
 public sealed class BaseBirdActor:MonoBehaviour
 {
  public BaseBirdFlock flock;public Rigidbody body;public SphereCollider shape;public Animator animator;
  public int index;public float animationSpeed=1;
  void OnCollisionEnter(Collision collision){if(flock!=null)flock.Contact(this,collision);}
 }
}

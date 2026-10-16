using UnityEngine;
namespace LWS.TruckBeerPong {
// Static hollow mesh is the sole physical cup. Scoring belongs to the active shot.
public sealed class PongCup:MonoBehaviour {
 public PongMatch match;public int team,index;public MeshCollider shell;public Transform visual,liquid;public float Top=>transform.position.y+3.55f;public Vector3 Center=>transform.position;
 void OnCollisionEnter(Collision c){if(c.rigidbody==match.truck.body)match.CupImpact(c.relativeVelocity.magnitude);}
}
}

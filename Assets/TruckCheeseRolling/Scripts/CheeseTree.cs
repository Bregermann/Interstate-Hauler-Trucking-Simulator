using UnityEngine;
namespace LWS.TruckCheeseRolling {
// A breakable world anchor holds the tree until PhysX contact tears the roots free.
// Truck contact response remains entirely PhysX; no second truck impact impulse.
public sealed class CheeseTree:MonoBehaviour {
 public Rigidbody body;public float rootForce=1400,rootTorque=300;FixedJoint roots;Vector3 pendingLinear,pendingAngular;int releaseFrame=-1;
 public bool KnockedDown{get;private set;}public int Blasts{get;private set;}
 public void ResetTree(Vector3 p,Quaternion q){body.isKinematic=false;transform.SetPositionAndRotation(p,q);body.position=p;body.rotation=q;body.linearVelocity=body.angularVelocity=Vector3.zero;body.constraints=RigidbodyConstraints.None;KnockedDown=false;Blasts=0;pendingLinear=pendingAngular=Vector3.zero;releaseFrame=-1;if(roots==null)roots=gameObject.AddComponent<FixedJoint>();roots.autoConfigureConnectedAnchor=false;roots.anchor=Vector3.zero;roots.connectedAnchor=p;roots.breakForce=rootForce;roots.breakTorque=rootTorque;roots.enableCollision=false;body.WakeUp();}
 public void Blast(Vector3 origin){if(body.isKinematic)return;Blasts++;if(roots!=null){Destroy(roots);roots=null;releaseFrame=Time.frameCount;}KnockedDown=true;Vector3 away=body.worldCenterOfMass-origin;float distance=away.magnitude;float delta=Mathf.Lerp(14,2,Mathf.Clamp01(distance/25));away.y=Mathf.Max(2,away.y+3);var linear=away.normalized*delta;var angular=new Vector3(delta*.22f,delta*.06f,delta*.18f);
  // Destroy is deferred until frame end. Applying now lets the still-live joint
  // absorb the explosion, so hold each event once until its root has gone.
  if(Time.frameCount<=releaseFrame){pendingLinear+=linear;pendingAngular+=angular;}else Apply(linear,angular);
 }
 void Apply(Vector3 linear,Vector3 angular){body.AddForce(linear,ForceMode.VelocityChange);body.AddTorque(angular,ForceMode.VelocityChange);body.WakeUp();}
 void FixedUpdate(){if(Time.frameCount<=releaseFrame||pendingLinear==Vector3.zero&&pendingAngular==Vector3.zero)return;Apply(pendingLinear,pendingAngular);pendingLinear=pendingAngular=Vector3.zero;}

 void OnJointBreak(float force){roots=null;KnockedDown=true;}
}
}

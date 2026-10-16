using UnityEngine;
namespace LWS.TruckFieldGoal {
// Fixed-step kinematic contact geometry follows the visible boot. Launch may
// occur only during its one committed stroke and a real authoritative overlap.
public sealed class FieldGoalBoot:MonoBehaviour {
 public FieldGoalMatch match;public Rigidbody body;public BoxCollider strike;public Transform visual;public float Angle{get;private set;}=55;public bool Stroking{get;private set;}public bool Contacted{get;private set;}float age;
 public void ResetBoot(){Stroking=Contacted=false;age=0;Angle=55;body.position=new Vector3(0,8.5f,-1.8f);body.rotation=Quaternion.Euler(Angle,0,0);transform.SetPositionAndRotation(body.position,body.rotation);}
 public void Begin(){if(Stroking)return;Stroking=true;age=0;}
 public void Tick(float dt){if(!Stroking)return;age+=dt;float t=Mathf.Clamp01(age/.5f);Angle=Mathf.Lerp(55,-24,t*t*(3-2*t));body.MoveRotation(Quaternion.Euler(Angle,0,0));}
 void TryContact(Collider other){if(!Stroking||Contacted||other!=match.truck.shape||!match.Live||match.Phase!=FieldGoalPhase.BootSwing)return;Vector3 direction;float depth;if(!Physics.ComputePenetration(strike,body.position,body.rotation,other,match.truck.body.position,match.truck.body.rotation,out direction,out depth))return;Contacted=true;match.BootContact(strike.ClosestPoint(match.truck.body.worldCenterOfMass));}
 void OnTriggerEnter(Collider other){TryContact(other);}void OnTriggerStay(Collider other){TryContact(other);}
}
}

using LWS.TruckTeaParty;using UnityEngine;
namespace LWS.TruckBakeOff {
 public sealed class BakeWhisk:MonoBehaviour {
  public BakeMatch match;public ServingArm arm;public Transform rotor;public bool Running{get;set;}public float AngularSpeed{get;private set;}
  public void Tick(float dt){AngularSpeed=match.Live&&Running?14:0;if(AngularSpeed>0)rotor.Rotate(Vector3.up,AngularSpeed*Mathf.Rad2Deg*dt,Space.Self);}
 }
}

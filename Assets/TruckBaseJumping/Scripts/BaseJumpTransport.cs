using UnityEngine;
using DG.Tweening;
namespace LWS.TruckBaseJumping {
// Presentation owns the cargo choreography only. Physical flight begins at the shared exit plane.
public sealed class BaseJumpTransport:MonoBehaviour {
 public GameObject model;public AnimationClip rotorClip;public Transform[] staging;public Transform doorway,exit;public float stagger=1.45f,shuffleDuration=2.1f;
 float rotorAge;
 public Vector3 Stage(int slot)=>staging[slot].position;
 public float ExitAge(int order)=>order*stagger+shuffleDuration;
 public void AnimateRotors(float dt){if(model==null||rotorClip==null)return;rotorAge+=dt;rotorClip.SampleAnimation(model,Mathf.Repeat(rotorAge,rotorClip.length));}
 public void StageTruck(BaseJumper jumper,int order,float age){
  float t=Mathf.Clamp01((age-order*stagger)/shuffleDuration);var from=Stage(jumper.slot);Vector3 p;Quaternion q=Quaternion.Euler(0,180,0);
  if(t<.24f){float u=t/.24f;float wiggle=Mathf.Sin(u*Mathf.PI*4)*Mathf.Sin(u*Mathf.PI);p=from+Vector3.up*(Mathf.Sin(u*Mathf.PI*3)*.11f);q*=Quaternion.Euler(0,wiggle*7,wiggle*3);}
  else if(t<.64f){float u=DOVirtual.EasedValue(0,1,(t-.24f)/.4f,Ease.InOutSine);p=Vector3.Lerp(from,doorway.position,u);p.y+=Mathf.Sin(u*Mathf.PI*6)*.055f;}
  else{float u=(t-.64f)/.36f;p=Vector3.Lerp(doorway.position,exit.position,u);p.y+=Mathf.Sin(u*Mathf.PI)*.35f;q*=Quaternion.Euler(-Mathf.Sin(u*Mathf.PI)*7,0,0);}
  jumper.truck.body.position=p;jumper.truck.body.rotation=q;
 }
}
}

using UnityEngine;
namespace LWS.TruckBakeOff {
 public sealed class BakeJudge:MonoBehaviour {
  public BakeMatch match;public int slot;public Collider table;public Transform placement;public Animator judge;float stable;
  public void Prepare(){stable=0;if(judge!=null)judge.Play("Idle",0,0);}
  void OnTriggerStay(Collider other){if(!match.Live||!match.rules.Active)return;var pan=other.attachedRigidbody!=null?other.attachedRigidbody.GetComponentInChildren<BakePan>():null;if(pan==null||pan.match!=match||pan.slot!=slot)return;var v=pan.vessel;bool ready=!v.Broken&&!v.Banked&&v.Holder==null&&v.LastCarrier==slot&&v.LastSupport==table&&v.SupportAge<.1f&&v.body.linearVelocity.magnitude<.5f&&Vector3.Dot(v.transform.up,Vector3.up)>.9f;stable=ready?stable+Time.fixedDeltaTime:0;if(stable>=.7f)match.Present(pan,this);}
 }
}

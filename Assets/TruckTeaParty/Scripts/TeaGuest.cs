using LWS.TruckSports;using UnityEngine;
namespace LWS.TruckTeaParty {
 public sealed class TeaGuest:MonoBehaviour {
  public TeaMatch match;public int id;public Collider table;public Transform receivingPoint;public Animator attendant;readonly float[] visiting=new float[4],stable=new float[12];
  void OnTriggerStay(Collider other){if(!match.Live||!match.rules.Active)return;var rb=other.attachedRigidbody;if(rb==null)return;var truck=rb.GetComponentInParent<SportsEventTruck>();if(truck!=null&&truck.session==match&&truck.slot<match.setup.count)visiting[truck.slot]=match.rules.Clock;var cup=rb.GetComponentInChildren<TeaCup>();if(cup==null)cup=rb.GetComponent<TeaCup>();if(cup==null||cup.match!=match||cup.cupId<0||cup.cupId>=12)return;var v=cup.vessel;bool ready=!v.Broken&&!v.Banked&&v.Holder==null&&v.LastCarrier>=0&&v.LastSupport==table&&v.SupportAge<.1f&&v.body.linearVelocity.magnitude<.5f&&Vector3.Dot(v.transform.up,Vector3.up)>.9f;stable[cup.cupId]=ready?stable[cup.cupId]+Time.fixedDeltaTime:0;if(stable[cup.cupId]>=.6f)match.Serve(cup,this);}
  public bool Visiting(int slot)=>slot>=0&&slot<match.setup.count&&match.rules.Clock-visiting[slot]<.15f;
  public void ResetGuest(){System.Array.Clear(visiting,0,4);System.Array.Clear(stable,0,12);if(attendant!=null)attendant.Play("Idle",0,0);}
 }
}

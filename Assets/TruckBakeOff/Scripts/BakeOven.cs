using LWS.TruckSports;using UnityEngine;
namespace LWS.TruckBakeOff {
 public sealed class BakeOven:MonoBehaviour {
  public BakeMatch match;public int slot;public Rigidbody door;public BoxCollider chamber;public Collider shelf;public Transform trayPoint;public Light heatLight;public float Temperature{get;private set;}public bool WantsClosed{get;private set;}public bool Closed=>Quaternion.Angle(door.rotation,Quaternion.identity)<4;public float LastVisit{get;private set;}=-100;bool crewMotionPending;public bool CrewMotionPending=>crewMotionPending;public bool Open=>Quaternion.Angle(door.rotation,Quaternion.Euler(0,105,0))<4;
  public void Prepare(){Temperature=25;WantsClosed=false;crewMotionPending=false;LastVisit=-100;door.rotation=Quaternion.Euler(0,105,0);door.transform.rotation=door.rotation;}
  public bool Toggle(){if(!match.Live||match.rules.Clock-LastVisit>.15f)return false;WantsClosed=!WantsClosed;crewMotionPending=false;return true;}
  // Same real control visit as Toggle. AI presses the control before backing
  // clear of this physical OUTWARD leaf; it cannot request a remote action.
  // A fresh manual Toggle cancels any pending crew request normally.
  public bool RequestToggleAfterCrewClear(){if(!Toggle())return false;crewMotionPending=true;return true;}
  public void CrewClearedMotion(){crewMotionPending=false;}
  public void CancelCrewRequest(){if(crewMotionPending)WantsClosed=Closed;crewMotionPending=false;}
  public void ObserveVisit(SportsEventTruck truck){if(truck!=null&&truck.session==match&&truck.slot==slot)LastVisit=match.rules.Clock;}
  public void Tick(float dt){var goal=Quaternion.Euler(0,WantsClosed?0:105,0);if(!crewMotionPending)door.MoveRotation(Quaternion.RotateTowards(door.rotation,goal,dt*55));Temperature=Mathf.MoveTowards(Temperature,Closed&&WantsClosed?200:25,dt*(Closed?38:65));if(heatLight!=null)heatLight.intensity=Mathf.InverseLerp(25,200,Temperature)*3;var pan=match.pans[slot].vessel;bool inside=chamber.bounds.Contains(pan.body.worldCenterOfMass)&&pan.Holder==null&&pan.LastSupport==shelf&&pan.SupportAge<.15f;if(match.rules.Heat(slot,dt,Temperature,inside&&Closed&&WantsClosed))match.NativeHeatSteps++;}
 }
}

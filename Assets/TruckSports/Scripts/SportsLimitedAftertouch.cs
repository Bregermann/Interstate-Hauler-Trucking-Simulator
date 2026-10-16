using UnityEngine;
namespace LWS.TruckSports {
// The bounded horizontal correction used by the collection's truck-launch sports.
// It never changes vertical velocity or propulsion after a landing.
public sealed class SportsLimitedAftertouch {
 public float Spent{get;private set;}public bool RangeArmed{get;private set;}public float Age{get;private set;}float budget,gate;
 public void Reset(float maximum,Vector2 held){budget=Mathf.Max(0,maximum);Spent=Age=gate=0;RangeArmed=Mathf.Abs(held.y)<.15f;}
 public void Tick(Rigidbody body,Vector2 input,float dt,float acceleration=.65f){Age+=dt;if(Mathf.Abs(input.y)<.15f)RangeArmed=true;gate=Mathf.MoveTowards(gate,Age>.25f?1:0,dt*2);var touch=Vector2.ClampMagnitude(new Vector2(input.x,RangeArmed?input.y:0),1);float cost=touch.magnitude*acceleration*dt;float allowed=Mathf.Min(cost,Mathf.Max(0,budget-Spent));if(cost<=0||allowed<=0)return;float factor=allowed/cost*gate;body.AddForce(new Vector3(touch.x,0,touch.y*.65f)*acceleration*factor,ForceMode.Acceleration);Spent+=allowed*gate;}
}
}

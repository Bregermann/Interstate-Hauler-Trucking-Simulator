using System;
using UnityEngine;
namespace LWS.TruckSnowmanBuilding
{
    public sealed partial class SnowmanMatch
    {
        // 0 actual rolling, 1 parked collection, 2 original hoist, 3 physical recovery.
        public readonly int[] AiDeliveryPhase=new int[4];
        public int PhysicalInterceptions {get;private set;} // Retained V2/V3 diagnostic; no longer a delivery criterion.
        static readonly float[] AiMinimumRadius={1.8f,1.2f,.85f};readonly float[] nextAiTrace=new float[4];
        void ResetAiDelivery(){Array.Clear(AiDeliveryPhase,0,4);Array.Clear(nextAiTrace,0,4);PhysicalInterceptions=0;}
        void AiPark(int owner){var truck=trucks[owner];truck.TickDrive(Vector2.zero);var state=truck.tractorMotor.vehicle.input.states;state.handbrake=state.handbrakeRaw=1;truck.tractorMotor.vehicle.input.states=state;}
        Vector2 PushToward(int owner,SnowmanBall ball,Vector3 goal,float speed)
        {
            Vector3 p=ball.body.position,direction=Vector3.ProjectOnPlane(goal-p,Vector3.up).normalized,relative=trucks[owner].body.position-p;
            if(direction.sqrMagnitude<.01f)return new Vector2(0,-1);
            Vector3 behind=p-direction*(ball.radius+4),target=Vector3.Dot(relative,direction)>-ball.radius-1||Vector3.ProjectOnPlane(relative,Vector3.up).magnitude>ball.radius+9?behind:p+direction*9;
            return DriveTo(trucks[owner],target,speed);
        }
        Vector2 DeliverRollingBall(int owner,int role)
        {
            var ball=balls[role];Vector3 p=ball.body.position;float lane=(role-1)*10;
            if(ball.Packed||role==LiftRole){AiDeliveryPhase[owner]=2;return new Vector2(0,-1);}
            // Ordinary physical bumper pushing continues until the actual collection mouth.
            // No concurrent flank/pass routes through another worker's live snow lane.
            if(p.z<=5&&collectionRack.ContainsLane(ball))
            {
                if(p.z< -8||ball.radius<AiMinimumRadius[role]||ball.Rolled<8||collectionRack.RealContacts[role]==0){AiDeliveryPhase[owner]=0;TraceAiDelivery(owner,role);return PushToward(owner,ball,new Vector3(lane,p.y,4),2.1f);}
                // Sleeping spheres do not emit OnCollisionStay. Keep the observed contact
                // provenance while the original lift checks independently require a slow,
                // grown sphere inside its unchanged collection zone.
                AiDeliveryPhase[owner]=1;if(role==Rules.Placed)RequestLift();TraceAiDelivery(owner,role);return new Vector2(0,-1);
            }
            // An externally displaced/saved sphere is pushed around the visible rack,
            // through its open mouth. The same NWH/physical contact owns every movement.
            AiDeliveryPhase[owner]=3;float outside=lane<0?-22:22;Vector3 goal;
            if(p.z>5&&Mathf.Abs(p.x)<20)goal=new Vector3(outside,p.y,p.z);
            else if(p.z> -16)goal=new Vector3(Mathf.Abs(p.x)>20?p.x:outside,p.y,-18);
            else goal=new Vector3(lane,p.y,-18);
            TraceAiDelivery(owner,role);return PushToward(owner,ball,goal,1.2f);
        }
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        void TraceAiDelivery(int owner,int role)
        {
            if(Clock<nextAiTrace[owner])return;nextAiTrace[owner]=Clock+2;var ball=balls[role];var truck=trucks[owner];
            Debug.Log($"SNOWMAN_COLLECTION clock={Clock:R} owner={owner} role={role} phase={AiDeliveryPhase[owner]} truck={truck.body.position.ToString("F5")} v={truck.body.linearVelocity.ToString("F5")} forward={truck.transform.forward.ToString("F4")} command={truck.command} gear={truck.tractorMotor.vehicle.powertrain.transmission.Gear} park={truck.tractorMotor.vehicle.input.states.handbrake:R} ball={ball.body.position.ToString("F5")} ballV={ball.body.linearVelocity.ToString("F5")} radius={ball.radius:R} rolled={ball.Rolled:R} lastTruck={ball.LastTruckContactSlot} rackContacts={collectionRack.RealContacts[role]} rackAge={Clock-collectionRack.LastContactClock[role]:R} lift={LiftRole} crane={hoist.Stage}");
        }
    }
}

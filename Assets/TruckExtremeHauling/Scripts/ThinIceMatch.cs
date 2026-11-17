using UnityEngine;
using LWS.TruckSports;
using LWS.TruckHitchSports;
using LWS.TruckBowling;

namespace LWS.TruckExtremeHauling
{
    public sealed class ThinIceMatch : ExtremeHaulingMatch
    {
        public LoadSensitiveIce[] ice;
        NWH.WheelController3D.WheelController[][] axleContacts;
        readonly Vector3[] waypoints=new Vector3[4], previous=new Vector3[4];
        readonly float[] replan=new float[4], distanceTravelled=new float[4];
        readonly int[] survivalLeg=new int[4];
        public override bool Cooperative => option==3;
        public override bool TryReadScoreCard(int card,out string value,out string detail)
        {
            value=detail=null;if(card<0||card>=setup.count)return false;
            value=scores[card].ToString("0");float danger=0;
            foreach(var tile in ice)if(!tile.Broken&&Vector3.SqrMagnitude(tile.transform.position-trucks[card].body.position)<1600)danger=Mathf.Max(danger,tile.Stress);
            detail=retired[card]?"ATTEMPT COMPLETE":danger>.6f?"ICE CRACKING · MOVE TO THICK ICE":option==1?"SURVIVAL · "+distanceTravelled[card].ToString("0")+" M HAULED":"CARGO · "+Mathf.RoundToInt(trailers[card].CargoCondition*100)+"%";return true;
        }
        protected override void ReportFinalMeasurements(int i) { RecordMetric(i,"thin_ice_supported_haul_metres",distanceTravelled[i]);RecordMetric(i,"thin_ice_cargo_condition",trailers[i].CargoCondition);RecordMetric(i,"thin_ice_vehicle_below_surface",trailers[i].Body.position.y<-3?1:0); }
        public override string Title => "ICE ROAD RECKONING: THIN ICE";
        protected override string[] Modes => new[] { "ICE ROAD DELIVERY", "THIN ICE SURVIVAL", "HEAVY HAUL CHAMPIONSHIP", "CONVOY RESCUE" };
        public override string Help => "Haul actual cargo across load-sensitive ice. Darkening ice and cracks warn of cumulative stress. Heavy cargo and hard impacts increase risk. Choose the wider marked thick-ice route or risk a short crossing. Deliver the connected trailer upright and stopped in the northern depot. A breakthrough is a real loss of support, not a random failure roll.";
        protected override void BeginMechanic()
        {
            axleContacts = new NWH.WheelController3D.WheelController[trailers.Length][];
            for (int i = 0; i < trailers.Length; i++) axleContacts[i] = trailers[i].GetComponentsInChildren<NWH.WheelController3D.WheelController>(true);
            foreach (var tile in ice) tile.ResetIce();
            System.Array.Clear(replan,0,replan.Length);System.Array.Clear(distanceTravelled,0,distanceTravelled.Length);System.Array.Clear(survivalLeg,0,survivalLeg.Length);
            for(int i=0;i<setup.count;i++)previous[i]=trucks[i].body.position;
            for (int i = 0; i < setup.count; i++) { trucks[i].tractorMotor.gripScale = .35f; trailers[i].AddCargo(option == 2 ? 16000 : 6000); }
        }
        protected override Vector3 Destination(int i)
        {
            Vector3 final=option==1?starts[i]+new Vector3(survivalLeg[i]%2==0?18:-18,0,survivalLeg[i]%2==0?260:20):base.Destination(i);
            if(option==1&&Vector3.ProjectOnPlane(trucks[i].body.position-final,Vector3.up).magnitude<18){survivalLeg[i]++;replan[i]=0;}
            if(age>=replan[i])
            {
                replan[i]=age+.6f;Vector3 at=trucks[i].body.position;float best=float.MaxValue;waypoints[i]=final;
                float mass=trucks[i].body.mass+trailers[i].Body.mass;
                foreach(var tile in ice)
                {
                    if(tile.Broken)continue;Vector3 center=tile.transform.position;float near=Vector3.ProjectOnPlane(center-at,Vector3.up).magnitude;if(near<8||near>65)continue;
                    float capacity=Mathf.Max(1,tile.bearingMass*tile.thickness*tile.thickness/(.45f*.45f));
                    float risk=Mathf.Max(0,mass/capacity-.65f)*55+tile.Stress*180;
                    float score=Vector3.ProjectOnPlane(center-final,Vector3.up).magnitude+near*.12f+risk;
                    if(score<best){best=score;waypoints[i]=new Vector3(center.x,at.y,center.z);}
                }
                if(Vector3.ProjectOnPlane(final-at,Vector3.up).magnitude<35)waypoints[i]=final;
            }
            return waypoints[i];
        }
        protected override Vector2 AICommand(int i,float dt)
        {
            if(option==1)return base.AICommand(i,dt);
            Vector3 target=Destination(i);Vector3 local=trucks[i].transform.InverseTransformPoint(target);
            float distance=Vector3.ProjectOnPlane(trailers[i].Body.worldCenterOfMass-objectives[i].position,Vector3.up).magnitude;
            // Heavy freight on ice needs braking distance, rather than a late
            // turn around a depot already passed by the entire trailer.
            float desired=Mathf.Min(8,Mathf.Sqrt(2*1.1f*Mathf.Max(0,distance-5)));
            float speed=Vector3.Dot(trucks[i].body.linearVelocity,trucks[i].transform.forward);
            return new Vector2(Mathf.Clamp(Mathf.Atan2(local.x,local.z)*1.3f,-1,1),Mathf.Clamp((desired-speed)*.45f,-1,.8f));
        }
        protected override void Simulate(float dt) { base.Simulate(dt); foreach (var tile in ice) tile.Simulate(dt); }
        void SupportedWeight(Rigidbody body, NWH.WheelController3D.WheelController[] wheels)
        {
            if (wheels == null || wheels.Length == 0) return;
            foreach (var wheel in wheels)
                if (wheel != null && wheel.IsGrounded && Physics.Raycast(wheel.transform.position + Vector3.up, Vector3.down, out var hit, 5, 1, QueryTriggerInteraction.Ignore))
                    hit.collider.GetComponent<LoadSensitiveIce>()?.RegisterSupportedLoad(body, 1f / wheels.Length);
        }
        protected override void StepMechanic(int i, Vector2 drive, bool ai, float dt)
        {
            if (hitches[i].Attached == null) hitches[i].GuideCoupling(trailers[i], dt);
            SupportedWeight(trucks[i].body, trucks[i].wheels); SupportedWeight(trailers[i].Body, axleContacts[i]);
            float moved=Vector3.Distance(previous[i],trucks[i].body.position);previous[i]=trucks[i].body.position;
            if(hitches[i].Attached!=null&&trailers[i].WheelContact&&moved<3&&moved>0)distanceTravelled[i]+=moved;
            if(option==1)
            {
                if(hitches[i].Attached!=null&&trailers[i].WheelContact&&trailers[i].CargoCondition>.1f)scores[i]+=dt;
                if(age>=matchSeconds-dt&&distanceTravelled[i]>20&&trailers[i].Body.position.y>0&&hitches[i].Attached!=null)Award(i,distanceTravelled[i]*2,"thin_ice_survived_under_load");
            }
            else if (hitches[i].Attached != null && AtRestIn(i, trailers[i].Body, objectives[i], 15))
            { Award(i, 2000 * trailers[i].CargoCondition + Mathf.Max(0, matchSeconds - age) * 5, "ice_road_delivery"); retired[i] = true; }
            if(option!=1 && ai && Vector3.ProjectOnPlane(trailers[i].Body.worldCenterOfMass-objectives[i].position,Vector3.up).magnitude<10)
            {
                trucks[i].TickDrive(Vector2.zero);var input=trucks[i].tractorMotor.vehicle.input;
                input.states.handbrake=input.states.handbrakeRaw=1;input.states.brakes=input.states.brakesRaw=1;trailers[i].SetBrakes(1);
            }
            if (trailers[i].Body.position.y < -3) { retired[i] = true; SportsSessionTelemetry.GameplayEvent(gameObject.scene.name, "ice_breakthrough", setup.players[i].participantId, "Physical trailer below lake surface", age); }
        }
    }
}

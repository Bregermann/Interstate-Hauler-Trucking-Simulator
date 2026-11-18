using UnityEngine;
using LWS.TruckSports;
using LWS.TruckHitchSports;
using LWS.TruckBowling;

namespace LWS.TruckExtremeHauling
{
    public sealed class ZeroGHitchMatch : ExtremeHaulingMatch
    {
        public OrbitalFreightThrusters[] thrusters;
        bool flightInitialized;
        readonly int[] relayLeg=new int[2];
        readonly float[] handoffAt=new float[2],handoffStartedAt=new float[2],dockError=new float[4],dockAngle=new float[4],precisionBonus=new float[4];
        readonly bool[] receiverCaptured=new bool[2];
        enum ReceiverApproach { ClimbClear, AboveAndAhead, MatchAhead, ClosePin }
        readonly ReceiverApproach[] receiverApproach=new ReceiverApproach[2];
        readonly Vector3[] receiverLiftOrigins=new Vector3[2],leadReleaseCenters=new Vector3[2],leadReleaseForward=new Vector3[2];
        readonly bool[] leadForwardClear=new bool[2];
        public string RelayApproachStage(int slot)=>!Relay||slot<2?"NOT RECEIVER":receiverApproach[slot%2].ToString();
        public bool RelayLeadForwardClear(int team)=>leadForwardClear[Mathf.Clamp(team,0,1)];
        string ReceiverPrompt(int team)=>receiverApproach[team]==ReceiverApproach.ClimbClear?"CLIMB CLEAR · LOAD AHEAD":receiverApproach[team]==ReceiverApproach.AboveAndAhead?"MOVE ABOVE THE TRANSFER LOAD":receiverApproach[team]==ReceiverApproach.MatchAhead?"ALIGN AHEAD · MATCH DRIFT":"CLOSE KINGPIN · FINAL DOCK";
        public OrbitalRecoveryField recoveryField;
        readonly ConfigurableJoint[] orbitalFrames=new ConfigurableJoint[4];
        readonly int[] frameLocks=new int[4];
        public bool OrbitalFrameLocked(int slot)=>orbitalFrames[slot]!=null&&orbitalFrames[slot].connectedBody==LoadFor(slot).Body;
        bool Relay=>option==1;
        protected override bool HasAtmosphere=>false;
        TrailerBody LoadFor(int slot)=>trailers[Relay?slot%2:slot];
        public override int MinimumPlayers=>Relay?4:1;
        public int RelayLeg(int team)=>relayLeg[Mathf.Clamp(team,0,1)];
        public float HandoffSeconds(int team)=>handoffAt[Mathf.Clamp(team,0,1)];
        public float PrecisionBonus(int slot)=>precisionBonus[slot];
        public float DockErrorMetres(int slot)=>dockError[slot];
        public override string Status=>Relay?"TEAMS 1+3 / 2+4 · SAME TRAILER · STOP / ACTION TO HAND OFF":option==2?"CENTER / ALIGN THE REAL LOAD · PRESERVE FUEL":option==3?"RECOVER DRIFTING FREIGHT · AVOID PHYSICAL DEBRIS":base.Status;
        NWH.WheelController3D.WheelController[][] tractorWheels, trailerWheels;
        readonly float[] tractorMass = new float[4];
        readonly bool[] keyboardAim = new bool[4];
        protected override void ReportFinalMeasurements(int i)
        {
            RecordMetric(i,"orbital_fuel_used",thrusters[i].FuelUsed);RecordMetric(i,"orbital_captured_payload_frame_locks",frameLocks[i]);RecordMetric(i,"orbital_successful_hitches",hitches[i].SuccessfulHitches);
            if(deliveries[i]>0){if(option==2)RecordMetric(i,"orbital_precision_bonus",precisionBonus[i]);RecordMetric(i,"orbital_delivery_error_metres",dockError[i]);RecordMetric(i,"orbital_delivery_angle_degrees",dockAngle[i]);}
            if(Relay)RecordMetric(i,"orbital_physical_handoff_seconds",handoffAt[i%2]);
        }
        public override bool TryReadScoreCard(int slot,out string value,out string detail)
        {
            if(!Relay)return base.TryReadScoreCard(slot,out value,out detail);
            value=scores[slot].ToString("0");int team=slot%2;
            detail=retired[slot]?"LEG COMPLETE":Departing(slot)?"FLY FORWARD CLEAR · THEN UP":slot<2?"TRANSFER DOCK · STOP / ACTION":relayLeg[team]==0?"WAIT FOR "+setup.players[team].name:receiverCaptured[team]?"ORIGINAL TRAILER LOCKED · FINAL DOCK":setup.players[slot].source==BowlingSource.AI?ReceiverPrompt(team):"RECEIVE FROM AHEAD · MATCH DRIFT";
            return true;
        }
        public override string Title => "ZERO-G HITCH-UP";
        protected override string[] Modes => new[] { "ZERO-G DOCKING", "SPACE FREIGHT RELAY", "ORBITAL PRECISION CHALLENGE", "ASTEROID CARGO RECOVERY" };
        public override string ControlCaption(int slot)
        {
            var source=setup.players[slot].source;
            if(source==BowlingSource.AI)return "AI · PHYSICAL THRUSTERS AND DOCKING";
            if(source==BowlingSource.WholeController)return "LS TRANSLATE · RS AIM · LB/RB ROLL · LT/RT LIFT · SOUTH BRAKE/HITCH";
            if(source==BowlingSource.LeftStick)return "LS TRANSLATE · LB AIM · LT LIFT/BRAKE/HITCH · LB+LT ROLL";
            if(source==BowlingSource.RightStick)return "RS TRANSLATE · RB AIM · RT LIFT/BRAKE/HITCH · RB+RT ROLL";
            return Controls(slot)+" · TAP ACTION: "+(keyboardAim[slot]?"TRANSLATE":"AIM")+" · HOLD: LIFT/ROLL · STILL: BRAKE · TAP AT PIN: HITCH";
        }
        public override string Help => "No wheel propulsion in orbit. Left stick translates sideways/forward; right stick pitches and yaws. Bumpers roll; triggers translate up/down. Hold ACTION to brake relative motion; tap it beside a precisely aligned kingpin to couple. Keyboard: tap ACTION away from the pin to switch translation/aim, hold it with movement for vertical thrust and roll, hold it still to brake; tap beside the pin to hitch. Deliver the trailer slowly inside your orbital dock. Fuel and contact damage affect your result. Relay teams are 1+3 and 2+4: stop and release the actual trailer at the first transfer dock; the second pilot must physically recapture that same load and deliver it. Relay and recovery engage a physical payload rotation lock after native capture, so your thrusters turn the coupled assembly. Precision rewards final position and orientation. Asteroid Recovery uses drifting, rotating cargo and moving physical rock hazards.";
        protected override void BeginMechanic()
        {
            flightInitialized = false; System.Array.Clear(keyboardAim,0,keyboardAim.Length);
            System.Array.Clear(receiverApproach,0,2);System.Array.Clear(receiverLiftOrigins,0,2);System.Array.Clear(leadReleaseCenters,0,2);System.Array.Clear(leadReleaseForward,0,2);System.Array.Clear(leadForwardClear,0,2);
            System.Array.Clear(relayLeg,0,relayLeg.Length);System.Array.Clear(handoffAt,0,handoffAt.Length);System.Array.Clear(dockError,0,dockError.Length);System.Array.Clear(dockAngle,0,dockAngle.Length);System.Array.Clear(precisionBonus,0,precisionBonus.Length);System.Array.Clear(handoffStartedAt,0,handoffStartedAt.Length);System.Array.Clear(receiverCaptured,0,receiverCaptured.Length);System.Array.Clear(orbitalFrames,0,orbitalFrames.Length);System.Array.Clear(frameLocks,0,frameLocks.Length);
            if(option==3&&recoveryField==null)throw new System.InvalidOperationException("Asteroid recovery requires its authored physical debris field.");
            if(recoveryField!=null)recoveryField.Prepare(option==3,MatchId);
            if(tractorWheels==null)
            {
                tractorWheels=new NWH.WheelController3D.WheelController[trucks.Length][];
                trailerWheels=new NWH.WheelController3D.WheelController[trailers.Length][];
                for(int i=0;i<trucks.Length;i++)tractorWheels[i]=trucks[i].GetComponentsInChildren<NWH.WheelController3D.WheelController>(true);
                for(int i=0;i<trailers.Length;i++)trailerWheels[i]=trailers[i].GetComponentsInChildren<NWH.WheelController3D.WheelController>(true);
            }
            for (int i = 0; i < setup.count; i++)
            {
                trucks[i].Power(true); thrusters[i].ResetFuel();
                foreach (var body in trucks[i].GetComponentsInChildren<Rigidbody>(true)) { body.useGravity = false; body.isKinematic = true; }
            }
            foreach (var trailer in trailers) { trailer.Vehicle.enabled = true; foreach (var body in trailer.GetComponentsInChildren<Rigidbody>(true)) { body.useGravity = false; body.isKinematic = true; } }
        }
        void DisableGroundAuthority(int slot)
        {
            // NWH disables a wheel's update without removing its generated convex
            // wheel collider. Ground suspension and those mount-position contact
            // cylinders are inappropriate here and obstruct the real kingpin.
            // Chassis, cab and trailer hull contacts and the native hitch remain active.
            DisableWheels(tractorWheels[slot]);
            DisableWheels(trailerWheels[Relay?slot%2:slot]);
        }
        static void DisableWheels(NWH.WheelController3D.WheelController[] wheels)
        {
            foreach(var wheel in wheels)
            {
                wheel.enabled=false;
                if(wheel.wheel.meshCollider!=null)wheel.wheel.meshCollider.enabled=false;
            }
        }
        bool Departing(int i)=>Relay&&i<2&&relayLeg[i]>=1;
        protected override Vector3 Destination(int i) => Departing(i)?leadForwardClear[i]?objectives[i].position+new Vector3(0,8,25):leadReleaseCenters[i]+leadReleaseForward[i]*14:hitches[i].Attached != null ? objectives[i].position : LoadFor(i).Kingpin.position;

        Vector3 ReceiverWaypoint(int slot,TrailerBody cargo,out Vector3 targetVelocity)
        {
            int team=slot%2;var phase=receiverApproach[team];
            Vector3 pin=cargo.Kingpin.position,forward=cargo.Body.rotation*Vector3.forward,up=cargo.Body.rotation*Vector3.up;
            Vector3 target;
            if(phase==ReceiverApproach.ClimbClear)
            {
                target=receiverLiftOrigins[team];target.y=Mathf.Max(target.y,pin.y+22);
                targetVelocity=target.y>receiverLiftOrigins[team].y?Vector3.up*cargo.Body.linearVelocity.y:Vector3.zero;
            }
            else
            {
                // The actual trailer extends behind its kingpin. Traverse above
                // its entire hull, descend well ahead, then close along the pin's
                // forward axis. Never sweep a tractor diagonally through its side.
                target=phase==ReceiverApproach.AboveAndAhead?pin+forward*32+up*20:phase==ReceiverApproach.MatchAhead?pin+forward*32:pin;
                targetVelocity=cargo.Body.GetPointVelocity(target);
            }
            Vector3 relative=trucks[slot].body.GetPointVelocity(hitches[slot].Anchor.position)-targetVelocity;
            float distance=Vector3.Distance(hitches[slot].Anchor.position,target);
            bool arrived=distance<(phase==ReceiverApproach.MatchAhead?1.5f:2.5f)&&relative.magnitude<(phase==ReceiverApproach.MatchAhead?.5f:1.2f);
            if(phase==ReceiverApproach.MatchAhead)arrived&=Quaternion.Angle(trucks[slot].body.rotation,cargo.Body.rotation)<4&&(trucks[slot].body.angularVelocity-cargo.Body.angularVelocity).magnitude<.2f;
            // A receiver may use its real RCS to pre-stage safely overhead during
            // the first leg. It must wait there until native release and until
            // the departing tractor has physically cleared the trailer floor.
            bool allowed=phase!=ReceiverApproach.AboveAndAhead||relayLeg[team]>=1&&leadForwardClear[team];
            if(phase!=ReceiverApproach.ClosePin&&arrived&&allowed)
            {
                receiverApproach[team]=(ReceiverApproach)((int)phase+1);
                SportsSessionTelemetry.GameplayEvent(gameObject.scene.name,"orbital_receiver_approach_stage",setup.players[slot].participantId,receiverApproach[team].ToString(),(int)receiverApproach[team]);
            }
            return target;
        }
        protected override void StepMechanic(int i, Vector2 drive, bool ai, float dt)
        {
            if (!flightInitialized)
            {
                for (int slot = 0; slot < setup.count; slot++) if (!trucks[slot].tractorMotor.vehicle.IsInitialized || !trailers[slot].Vehicle.IsInitialized) return;
                for (int slot = 0; slot < setup.count; slot++)
                {
                    trucks[slot].Power(false);tractorMass[slot]=0;foreach(var part in trucks[slot].GetComponentsInChildren<Rigidbody>(true))tractorMass[slot]+=part.mass;
                    foreach (var body in trucks[slot].GetComponentsInChildren<Rigidbody>(true)) body.isKinematic = false;
                    trailers[slot].Vehicle.enabled = false; foreach (var body in trailers[slot].GetComponentsInChildren<Rigidbody>(true)) body.isKinematic = false;
                    DisableGroundAuthority(slot);
                }
                if(Relay)for(int extra=2;extra<trailers.Length;extra++)trailers[extra].gameObject.SetActive(false);
                if(Relay)for(int team=0;team<2;team++)receiverLiftOrigins[team]=hitches[team+2].Anchor.position;
                if(option==3)for(int slot=0;slot<setup.count;slot++)
                {var load=LoadFor(slot);load.Body.AddForce(new Vector3(slot%2==0?1.4f:-1.4f,.65f,-.35f),ForceMode.VelocityChange);load.Body.AddTorque(new Vector3(.045f,.1f,slot%2==0?.035f:-.035f),ForceMode.VelocityChange);}
                flightInitialized = true;
            }
            var cargo=LoadFor(i);
            thrusters[i].SetCapturedPayload(OrbitalFrameLocked(i)&&hitches[i].PhysicalConstraintPresent?cargo.Body:null);
            if(Relay&&i>=2&&relayLeg[i%2]==0&&!ai){bool drifting=trucks[i].body.linearVelocity.sqrMagnitude>.0025f||trucks[i].body.angularVelocity.sqrMagnitude>.0004f;thrusters[i].Apply(Vector3.zero,Vector3.zero,drifting,dt);return;}
            Vector3 translation = new Vector3(drive.x, 0, drive.y), rotation = Vector3.zero;
            var pad = setup.players[i].pad;
            if (!ai && pad != null && setup.players[i].source == BowlingSource.WholeController)
            {
                translation.x = pad.leftStick.ReadValue().x; translation.z = pad.leftStick.ReadValue().y;
                translation.y = pad.rightTrigger.ReadValue() - pad.leftTrigger.ReadValue();
                var look = pad.rightStick.ReadValue(); rotation = new Vector3(-look.y, look.x, (pad.leftShoulder.isPressed ? 1 : 0) - (pad.rightShoulder.isPressed ? 1 : 0));
            }
            else if (!ai && pad != null)
            {
                bool left = setup.players[i].source == BowlingSource.LeftStick;
                var stick = left ? pad.leftStick.ReadValue() : pad.rightStick.ReadValue();
                bool aim = left ? pad.leftShoulder.isPressed : pad.rightShoulder.isPressed;
                if (aim) { translation = Vector3.zero; rotation = ActionHeld(i) ? new Vector3(-stick.y,0,stick.x) : new Vector3(-stick.y,stick.x,0); }
                else translation = ActionHeld(i) ? new Vector3(stick.x,stick.y,0) : new Vector3(stick.x,0,stick.y);
            }
            else if (ai)
            {
                float skill=Mathf.Lerp(.85f,1.15f,Mathf.Clamp((int)setup.players[i].difficulty,0,2)/2f);
                bool coupled=hitches[i].PhysicalConstraintPresent,departing=Departing(i);
                Vector3 reference=departing?trucks[i].body.worldCenterOfMass:coupled?cargo.Body.worldCenterOfMass:hitches[i].Anchor.position;
                bool stagedReceiver=Relay&&i>=2&&!coupled;
                bool receiverClimbing=stagedReceiver&&receiverApproach[i%2]==ReceiverApproach.ClimbClear;
                Vector3 receiverVelocity=Vector3.zero;
                Vector3 navigationTarget=stagedReceiver?ReceiverWaypoint(i,cargo,out receiverVelocity):Destination(i);
                Vector3 delta=navigationTarget-reference;
                Vector3 targetVelocity=coupled||departing?Vector3.zero:cargo.Body.GetPointVelocity(cargo.Kingpin.position);
                Vector3 velocityAtReference=coupled?cargo.Body.linearVelocity:trucks[i].body.GetPointVelocity(reference);
                Vector3 targetArm=coupled||departing?Vector3.zero:cargo.Kingpin.position-cargo.Body.worldCenterOfMass;
                Vector3 targetSpinFeed=coupled||departing?Vector3.zero:cargo.Body.angularVelocity;
                if(stagedReceiver){targetVelocity=receiverVelocity;targetArm=navigationTarget-cargo.Body.worldCenterOfMass;if(receiverClimbing)targetSpinFeed=Vector3.zero;}
                Vector3 anchorArm=reference-trucks[i].body.worldCenterOfMass;
                Vector3 pointAcceleration=Vector3.Cross(targetSpinFeed,Vector3.Cross(targetSpinFeed,targetArm))-Vector3.Cross(trucks[i].body.angularVelocity,Vector3.Cross(trucks[i].body.angularVelocity,anchorArm));
                Vector3 acceleration;
                float available=thrusters[i].acceleration;
                if(coupled)
                {
                    float mass=cargo.Body.mass+tractorMass[i];
                    available*=trucks[i].body.mass/Mathf.Max(1,mass);
                    float distance=delta.magnitude;
                    float desiredSpeed=Mathf.Min(6*skill,Mathf.Sqrt(2*available*distance)*.7f,distance*.6f);
                    Vector3 desiredVelocity=distance>.001f?delta/distance*desiredSpeed:Vector3.zero;
                    acceleration=(desiredVelocity-velocityAtReference)*1.8f;
                }
                else if(Relay||option==3)
                {
                    float distance=delta.magnitude;
                    float desiredSpeed=Mathf.Min(6*skill,Mathf.Sqrt(2*available*distance)*.65f,distance*1.2f);
                    Vector3 relative=distance>.001f?delta/distance*desiredSpeed:Vector3.zero;
                    acceleration=(targetVelocity+relative-velocityAtReference)*3+pointAcceleration;
                }
                else acceleration=delta*(4*skill)+(targetVelocity-velocityAtReference)*3+pointAcceleration;
                translation=trucks[i].transform.InverseTransformDirection(Vector3.ClampMagnitude(acceleration/Mathf.Max(.1f,available),1));
                Quaternion desired=coupled||departing?Quaternion.identity:cargo.Body.rotation;
                Quaternion difference=desired*Quaternion.Inverse(trucks[i].body.rotation);
                difference.ToAngleAxis(out float angle,out Vector3 axis);if(angle>180)angle-=360;
                Vector3 targetSpin=coupled||departing?Vector3.zero:cargo.Body.angularVelocity;
                Vector3 correction=axis*(angle*Mathf.Deg2Rad*(3*skill))+(targetSpin-trucks[i].body.angularVelocity)*2;
                rotation=trucks[i].transform.InverseTransformDirection(Vector3.ClampMagnitude(correction,1));
            }
            else
            {
                bool atPin=Vector3.Distance(hitches[i].Anchor.position,cargo.Kingpin.position)<.65f;
                if(actions[i]&&!atPin)keyboardAim[i]=!keyboardAim[i];
                if(ActionHeld(i)){translation=new Vector3(0,drive.y,0);rotation=new Vector3(0,0,-drive.x);}
                else if(keyboardAim[i]){translation=Vector3.zero;rotation=new Vector3(-drive.y,drive.x,0);}
            }
            thrusters[i].Apply(translation, rotation, !ai && ActionHeld(i) && drive.sqrMagnitude < .01f, dt);
            if(Departing(i))
            {
                bool arrived=Vector3.Distance(trucks[i].body.worldCenterOfMass,Destination(i))<3&&trucks[i].body.linearVelocity.magnitude<.4f&&trucks[i].body.angularVelocity.magnitude<.2f;
                if(arrived){if(!leadForwardClear[i])leadForwardClear[i]=true;else retired[i]=true;}
                return;
            }
            if (actions[i] || ai&&(!Relay||i<2||relayLeg[i%2]>=1&&receiverApproach[i%2]==ReceiverApproach.ClosePin)) hitches[i].GuideCoupling(cargo, dt, true);
            ConfigurePayloadFrame(i,cargo);
            if(Relay&&i>=2&&!receiverCaptured[i%2]&&hitches[i].PhysicalConstraintPresent&&hitches[i].Attached==cargo)
            {receiverCaptured[i%2]=true;handoffAt[i%2]=age-handoffStartedAt[i%2];}
            if(cargo.Vehicle.enabled){cargo.Vehicle.enabled=false;DisableGroundAuthority(i);}
            if (hitches[i].PhysicalConstraintPresent && AtRestIn(i, cargo.Body, objectives[i], 6))
            {
                if(Relay&&i<2)
                {
                    if(!ai&&!actions[i])return;if(hitches[i].Release()!=cargo)return;
                    // Clear the chassis rear before any rise. The prior diagonal
                    // departure lifted it through the released trailer's floor.
                    leadReleaseCenters[i]=trucks[i].body.worldCenterOfMass;leadReleaseForward[i]=cargo.Body.rotation*Vector3.forward;leadForwardClear[i]=false;
                    if(cargo.Module.module.trailerStand!=null)cargo.Module.module.trailerStand.SetActive(false);
                    cargo.Vehicle.enabled=false;DisableGroundAuthority(i);relayLeg[i%2]=1;handoffStartedAt[i%2]=age;
                    SportsSessionTelemetry.GameplayEvent(gameObject.scene.name,"orbital_trailer_handoff",setup.players[i].participantId,"original cargo released for "+setup.players[i+2].participantId,age);return;
                }
                float error=Vector3.Distance(cargo.Body.worldCenterOfMass,objectives[i].position),angle=Quaternion.Angle(cargo.Body.rotation,objectives[i].rotation);
                double award=1500*cargo.CargoCondition+thrusters[i].fuel*5+matchSeconds-age;
                if(option==2){precisionBonus[i]=Mathf.Max(0,1200-error*150-angle*3-cargo.Body.linearVelocity.magnitude*100);award+=precisionBonus[i];}
                dockError[i]=error;dockAngle[i]=angle;Award(i,award,option==2?"precision_orbital_delivery":option==3?"drifting_cargo_recovered":"orbital_cargo_docked");retired[i]=true;
                if(Relay){int lead=i%2;dockError[lead]=error;dockAngle[lead]=angle;Award(lead,award,"original_relay_cargo_delivered");relayLeg[lead]=2;}
            }
        }
        void ConfigurePayloadFrame(int slot,TrailerBody cargo)
        {
            if(!(Relay||option==3)||!hitches[slot].PhysicalConstraintPresent||OrbitalFrameLocked(slot))return;
            foreach(var joint in trucks[slot].GetComponents<ConfigurableJoint>())if(joint.connectedBody==cargo.Body)
            {
                // A road fifth wheel's free articulation is not a rigid freight-dock
                // latch in orbit. Lock only this newly captured native joint's axes.
                // Native momentum/constraints still move both real bodies; no pose or
                // velocity is corrected, and ordinary road/default docking is untouched.
                joint.angularXMotion=joint.angularYMotion=joint.angularZMotion=ConfigurableJointMotion.Locked;
                orbitalFrames[slot]=joint;frameLocks[slot]++;return;
            }
        }
        protected override void StopGame()
        {
            base.StopGame();recoveryField?.Freeze();
        }
    }
}

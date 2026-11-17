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
        NWH.WheelController3D.WheelController[][] groundWheels;
        readonly float[] tractorMass = new float[4];
        readonly bool[] keyboardAim = new bool[4];
        protected override void ReportFinalMeasurements(int i) { RecordMetric(i,"orbital_fuel_used",thrusters[i].FuelUsed);RecordMetric(i,"orbital_constraint_intact",hitches[i].PhysicalConstraintPresent?1:0);if(deliveries[i]>0)RecordMetric(i,"orbital_delivery_error_metres",Vector3.Distance(trailers[i].Body.worldCenterOfMass,objectives[i].position)); }
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
        public override string Help => "No wheel propulsion in orbit. Left stick translates sideways/forward; right stick pitches and yaws. Bumpers roll; triggers translate up/down. Hold ACTION to brake relative motion; tap it beside a precisely aligned kingpin to couple. Keyboard: tap ACTION away from the pin to switch translation/aim, hold it with movement for vertical thrust and roll, hold it still to brake; tap beside the pin to hitch. Deliver the trailer slowly inside your orbital dock. Fuel and contact damage affect your result.";
        protected override void BeginMechanic()
        {
            flightInitialized = false; System.Array.Clear(keyboardAim,0,keyboardAim.Length);
            if(groundWheels==null)
            {
                groundWheels=new NWH.WheelController3D.WheelController[trucks.Length][];
                for(int i=0;i<trucks.Length;i++)
                {
                    var list=new System.Collections.Generic.List<NWH.WheelController3D.WheelController>(trucks[i].GetComponentsInChildren<NWH.WheelController3D.WheelController>(true));
                    list.AddRange(trailers[i].GetComponentsInChildren<NWH.WheelController3D.WheelController>(true));groundWheels[i]=list.ToArray();
                }
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
            foreach(var wheel in groundWheels[slot])
            {
                wheel.enabled=false;
                if(wheel.wheel.meshCollider!=null)wheel.wheel.meshCollider.enabled=false;
            }
        }
        protected override Vector3 Destination(int i) => hitches[i].Attached != null ? objectives[i].position : trailers[i].Kingpin.position;
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
                flightInitialized = true;
            }
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
                bool coupled=hitches[i].PhysicalConstraintPresent;
                Vector3 reference=coupled?trailers[i].Body.worldCenterOfMass:hitches[i].Anchor.position;
                Vector3 delta=Destination(i)-reference;
                Vector3 targetVelocity=coupled?Vector3.zero:trailers[i].Body.GetPointVelocity(trailers[i].Kingpin.position);
                Vector3 velocityAtReference=coupled?trailers[i].Body.linearVelocity:trucks[i].body.GetPointVelocity(reference);
                Vector3 targetArm=coupled?Vector3.zero:trailers[i].Kingpin.position-trailers[i].Body.worldCenterOfMass;
                Vector3 targetSpinFeed=coupled?Vector3.zero:trailers[i].Body.angularVelocity;
                Vector3 anchorArm=reference-trucks[i].body.worldCenterOfMass;
                Vector3 pointAcceleration=Vector3.Cross(targetSpinFeed,Vector3.Cross(targetSpinFeed,targetArm))-Vector3.Cross(trucks[i].body.angularVelocity,Vector3.Cross(trucks[i].body.angularVelocity,anchorArm));
                Vector3 acceleration;
                float available=thrusters[i].acceleration;
                if(coupled)
                {
                    float mass=trailers[i].Body.mass+tractorMass[i];
                    available*=trucks[i].body.mass/Mathf.Max(1,mass);
                    float distance=delta.magnitude;
                    float desiredSpeed=Mathf.Min(6,Mathf.Sqrt(2*available*distance)*.7f,distance*.6f);
                    Vector3 desiredVelocity=distance>.001f?delta/distance*desiredSpeed:Vector3.zero;
                    acceleration=(desiredVelocity-velocityAtReference)*1.8f;
                }
                else acceleration=delta*4+(targetVelocity-velocityAtReference)*3+pointAcceleration;
                translation=trucks[i].transform.InverseTransformDirection(Vector3.ClampMagnitude(acceleration/Mathf.Max(.1f,available),1));
                Quaternion desired=coupled?Quaternion.identity:trailers[i].Body.rotation;
                Quaternion difference=desired*Quaternion.Inverse(trucks[i].body.rotation);
                difference.ToAngleAxis(out float angle,out Vector3 axis);if(angle>180)angle-=360;
                Vector3 targetSpin=coupled?Vector3.zero:trailers[i].Body.angularVelocity;
                Vector3 correction=axis*(angle*Mathf.Deg2Rad*3)+(targetSpin-trucks[i].body.angularVelocity)*2;
                rotation=trucks[i].transform.InverseTransformDirection(Vector3.ClampMagnitude(correction,1));
            }
            else
            {
                bool atPin=Vector3.Distance(hitches[i].Anchor.position,trailers[i].Kingpin.position)<.65f;
                if(actions[i]&&!atPin)keyboardAim[i]=!keyboardAim[i];
                if(ActionHeld(i)){translation=new Vector3(0,drive.y,0);rotation=new Vector3(0,0,-drive.x);}
                else if(keyboardAim[i]){translation=Vector3.zero;rotation=new Vector3(-drive.y,drive.x,0);}
            }
            thrusters[i].Apply(translation, rotation, !ai && ActionHeld(i) && drive.sqrMagnitude < .01f, dt);
            if (actions[i] || ai) hitches[i].GuideCoupling(trailers[i], dt, true);
            if(trailers[i].Vehicle.enabled){trailers[i].Vehicle.enabled=false;DisableGroundAuthority(i);}
            if (hitches[i].PhysicalConstraintPresent && AtRestIn(i, trailers[i].Body, objectives[i], 6))
            { Award(i, 1500 * trailers[i].CargoCondition + thrusters[i].fuel * 5 + matchSeconds - age, "orbital_cargo_docked"); retired[i] = true; }
        }
    }
}

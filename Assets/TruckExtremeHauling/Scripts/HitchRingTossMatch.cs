using UnityEngine;
using LWS.TruckSports;
using LWS.TruckHitchSports;
using LWS.TruckBowling;

namespace LWS.TruckExtremeHauling
{
    public sealed class HitchRingTossMatch : ExtremeHaulingMatch
    {
        public HaulingTargetRing[] rings;
        [Range(1,5)] public int throwsPerPlayer=3;
        [Min(0),Tooltip("Trick-only equipment offset, applied at ordinary match setup after physical lane clearance checks.")]
        public float trickRingDownrangeOffset=10;
        [Min(0),Tooltip("Actual airborne roll actuator torque in Newton metres; no angular velocity or pose is assigned.")]
        public float trickRollTorque=320000;
        [Min(.1f),Tooltip("Player commanded maximum roll rate in radians per second.")]
        public float trickMaximumRollRate=8;
        float[] originalAngularLimits;
        readonly bool[] trickLaneReady=new bool[4];
        readonly string[] trickLayoutState=new string[4];
        readonly float[] peakTrickTorque=new float[4];
        readonly string[] lastTrickAttempt=new string[4];
        public string LastTrickAttemptDiagnostic(int slot)=>lastTrickAttempt[slot]??"no completed throw";
        readonly int[] attempts=new int[4],misses=new int[4];
        readonly float[] reloadUntil=new float[4],throwAge=new float[4],quiet=new float[4],centering=new float[4];
        public int AttemptsResolved(int slot)=>attempts[slot];
        public int Misses(int slot)=>misses[slot];
        public override bool TryReadScoreCard(int slot,out string value,out string detail)
        {value=scores[slot].ToString("0");detail="THROW "+Mathf.Min(attempts[slot]+1,Mathf.Clamp(throwsPerPlayer,1,5))+"/"+Mathf.Clamp(throwsPerPlayer,1,5)+" · "+deliveries[slot]+" RINGS · "+misses[slot]+" MISSES"+(option==3?" · "+AerialTrickTurns(slot)+" AIR TURNS":"");return true;}
        public override string Results
        {
            get
            {
                if(option!=3)return base.Results;
                var text=new System.Text.StringBuilder();
                for(int i=0;i<setup.count;i++)text.Append(setup.players[i].name).Append(" · ").Append(scores[i].ToString("0")).Append(" PTS · ").Append(deliveries[i]).Append(" RINGS · ").Append(passedTrickTurns[i]).Append(" QUALIFIED AIR TURNS · ").Append(misses[i]).Append(" MISSES").AppendLine();
                return text.ToString();
            }
        }
        protected override Vector3 CameraFocus(int slot) {slot=CameraParticipant(slot);return released[slot] ? trailers[slot].Body.worldCenterOfMass : trucks[slot].body.position;}
        Vector3[] previous = new Vector3[4]; bool[] released = new bool[4];
        Collider[][] shapes;
        Vector3[] ringOrigins;
        float[] baseRadii;
        Rigidbody[] movingRings;
        float[] airtime=new float[4], rotations=new float[4],totalAirtime=new float[4],totalRevolutions=new float[4];
        HaulingTargetRing[] crossing = new HaulingTargetRing[4];
        float[] crossingDirection = new float[4];
        readonly RingAerialTrickMeter[] trickMeters = new RingAerialTrickMeter[4];
        readonly float[] rejectedGroundRotation = new float[4];
        readonly int[] passedTrickTurns = new int[4];
        public int AerialTrickTurns(int slot) => (int)totalRevolutions[slot] + (released[slot] && !retired[slot] && trickMeters[slot] != null ? trickMeters[slot].CompletedTurns : 0);
        public int PassedAerialTrickTurns(int slot) => passedTrickTurns[slot];
        public bool TrailerReleased(int slot) => released[slot];
        public bool TrickLaneReady(int slot) => trickLaneReady[slot];
        public Vector3 AuthoredRingOrigin(int ring) => ringOrigins[ring];
        public string TrickDiagnostic(int slot) => trickMeters[slot] != null ? trickMeters[slot].Diagnostic() + " passedTurns=" + passedTrickTurns[slot] + " released=" + released[slot] + " nativeWheelContact=" + trailers[slot].WheelContact + " rollTorquePeakNm=" + peakTrickTorque[slot].ToString("F0") + " layout=" + trickLayoutState[slot] : "Trick equipment not initialized";
        protected override void ReportFinalMeasurements(int i) { RecordMetric(i,"ring_attempts_resolved",attempts[i]);RecordMetric(i,"ring_misses",misses[i]);RecordMetric(i,"ring_trailer_released",released[i]?1:0);RecordMetric(i,"ring_airborne_seconds",totalAirtime[i]);RecordMetric(i,"ring_physical_revolutions",totalRevolutions[i]);RecordMetric(i,"ring_complete_passages",deliveries[i]); if (option == 3) { RecordMetric(i,"ring_rejected_supported_rotation_degrees",rejectedGroundRotation[i]); RecordMetric(i,"ring_passage_qualified_aerial_turns",passedTrickTurns[i]); RecordMetric(i,"ring_actual_roll_actuator_peak_nm",peakTrickTorque[i]); } }
        public override string Title => "HITCH RING TOSS";
        public override string Status => option==3?"ACTION RELEASE · AIRBORNE STEER ROLLS · NEUTRAL BRAKES SPIN":base.Status;
        protected override string[] Modes => new[] { "PRECISION RING TOSS", "LONG-DISTANCE TOSS", "MOVING RING CHALLENGE", "TRICK SHOT CHAMPIONSHIP" };
        public override string Help => "Swing the attached trailer with the tractor. ACTION releases the real hitch, preserving momentum. The trailer must physically cross a ring aperture; projected aim is not a score. More distant rings award larger multipliers. Precision rewards the actual centered passage of the whole load. Trick Championship gives its ring a longer approach. After release, STEER LEFT/RIGHT rolls your airborne trailer; let steering return to neutral to brake the spin. The same steering inputs work on keyboard, assigned split-controller and whole-controller layouts. Roll torque only acts during genuine free flight. Complete a full360-degree turn and clear the physical ring to bank a trick bonus. Ground spins, roof sliding and back-and-forth rocking do not earn trick bonuses. The live AIR TURNS counter shows measured revolutions; a missed ring cannot bank trick points. Each match has the configured number of physical throws; a missed throw costs100points. Between throws the normal equipment reset returns your original rig to the loading start.";
        protected override void BeginMechanic()
        {
            System.Array.Clear(attempts,0,attempts.Length);System.Array.Clear(misses,0,misses.Length);System.Array.Clear(reloadUntil,0,reloadUntil.Length);System.Array.Clear(throwAge,0,throwAge.Length);System.Array.Clear(quiet,0,quiet.Length);System.Array.Clear(centering,0,centering.Length);
            if(ringOrigins==null)
            {
                ringOrigins=new Vector3[rings.Length];baseRadii=new float[rings.Length];movingRings=new Rigidbody[rings.Length];
                for(int i=0;i<rings.Length;i++){ringOrigins[i]=rings[i].transform.position;baseRadii[i]=rings[i].radius;movingRings[i]=rings[i].GetComponent<Rigidbody>();if(movingRings[i]==null)movingRings[i]=rings[i].gameObject.AddComponent<Rigidbody>();movingRings[i].isKinematic=true;movingRings[i].useGravity=false;}
            }
            for(int i=0;i<rings.Length;i++){movingRings[i].position=ringOrigins[i];rings[i].transform.position=ringOrigins[i];rings[i].radius=baseRadii[i];}
            System.Array.Clear(airtime,0,airtime.Length);System.Array.Clear(rotations,0,rotations.Length);System.Array.Clear(totalAirtime,0,totalAirtime.Length);System.Array.Clear(totalRevolutions,0,totalRevolutions.Length);
            if(originalAngularLimits==null){originalAngularLimits=new float[trailers.Length];for(int i=0;i<trailers.Length;i++)originalAngularLimits[i]=trailers[i].Body.maxAngularVelocity;}
            for(int i=0;i<trailers.Length;i++)trailers[i].Body.maxAngularVelocity=option==3?Mathf.Max(originalAngularLimits[i],trickMaximumRollRate+1):originalAngularLimits[i];
            System.Array.Clear(trickLaneReady,0,trickLaneReady.Length);System.Array.Clear(peakTrickTorque,0,peakTrickTorque.Length);System.Array.Clear(lastTrickAttempt,0,lastTrickAttempt.Length);
            System.Array.Clear(rejectedGroundRotation,0,rejectedGroundRotation.Length);System.Array.Clear(passedTrickTurns,0,passedTrickTurns.Length);
            System.Array.Clear(released,0,released.Length);System.Array.Clear(crossing,0,crossing.Length);shapes=new Collider[trailers.Length][];
            for(int i=0;i<trailers.Length;i++)
            {
                shapes[i]=trailers[i].GetComponentsInChildren<Collider>(true);previous[i]=PhysicalBounds(i).center;
                if(option==3)
                {
                    var contact=trailers[i].Body.GetComponent<UnhitchedSurfaceContact>();
                    if(contact==null)contact=trailers[i].Body.gameObject.AddComponent<UnhitchedSurfaceContact>();
                    contact.Configure(trailers[i].transform);
                    trickMeters[i]=new RingAerialTrickMeter(trailers[i].Body,contact);
                }
            }
            if(option==3)for(int i=0;i<Mathf.Min(setup.count,rings.Length);i++)ConfigureTrickRing(i);
        }
        Bounds PhysicalBounds(int slot)
        {
            Bounds result=new Bounds(trailers[slot].Body.worldCenterOfMass,Vector3.zero);bool first=true;
            foreach(var shape in shapes[slot])if(shape!=null&&shape.enabled&&!shape.isTrigger){if(first){result=shape.bounds;first=false;}else result.Encapsulate(shape.bounds);}
            return result;
        }
        static float Extent(Vector3 axis,Vector3 extents)=>Mathf.Abs(axis.x)*extents.x+Mathf.Abs(axis.y)*extents.y+Mathf.Abs(axis.z)*extents.z;
        static float ProjectedRadius(HaulingTargetRing ring,Bounds body)=>new Vector2(Extent(ring.transform.right,body.extents),Extent(ring.transform.up,body.extents)).magnitude;
        protected override void Simulate(float dt)
        {
            if(option==2&&movingRings!=null)for(int i=0;i<rings.Length;i++)movingRings[i].MovePosition(ringOrigins[i]+Vector3.right*(Mathf.Sin(age*.7f+i)*5));
            base.Simulate(dt);
        }
        protected override Vector2 AICommand(int i,float dt)
        {
            if(released[i])return new Vector2(i%2==0?-1:1,.8f);
            Vector3 delta=trucks[i].transform.InverseTransformPoint(rings[i%rings.Length].transform.position);float angle=Mathf.Atan2(delta.x,delta.z)*Mathf.Rad2Deg;
            return new Vector2(Mathf.Clamp(angle/40,-1,1),1);
        }
        protected override void StepMechanic(int i, Vector2 drive, bool ai, float dt)
        {
            var trailer = trailers[i];
            if(reloadUntil[i]>0)
            {
                trucks[i].TickDrive(Vector2.zero);
                if(age<reloadUntil[i]||!hitches[i].TryCouple(trailer))return;
                foreach(var part in trucks[i].GetComponentsInChildren<Rigidbody>(true))part.isKinematic=false;
                foreach(var part in trailer.GetComponentsInChildren<Rigidbody>(true))part.isKinematic=false;
                reloadUntil[i]=0;previous[i]=PhysicalBounds(i).center;return;
            }
            throwAge[i]+=dt;
            if (!released[i] && hitches[i].Attached == null) hitches[i].GuideCoupling(trailer, dt);
            var ownRing=rings[i%rings.Length];Vector3 relative=ownRing.transform.InverseTransformPoint(trailer.Body.worldCenterOfMass);
            bool aiRelease=ai&&relative.z<0&&relative.z>-(10+(int)setup.players[i].difficulty*3)&&trailer.Body.linearVelocity.magnitude>6;
            if (!released[i] && (actions[i] || aiRelease) && hitches[i].Attached != null)
            {
                hitches[i].Release();trailer.SetBrakes(0);
                // NWH lowers parking legs on normal uncoupling. A thrown load
                // keeps its legs stowed so they cannot spear the landing lane;
                // the original chassis and axle contacts remain physical.
                if(trailer.Module.module.trailerStand!=null)trailer.Module.module.trailerStand.SetActive(false);
                released[i]=true;
            }
            if(released[i])
            {
                if(option==3)
                {
                    trickMeters[i].Sample(trailer.WheelContact,dt);airtime[i]=trickMeters[i].FreeFlightSeconds;rotations[i]=trickMeters[i].CompletedTurns;
                    ApplyTrickRoll(i,ai,drive.x);
                }
                else {if(!trailer.WheelContact)airtime[i]+=dt;rotations[i]+=trailer.Body.angularVelocity.magnitude*dt/(2*Mathf.PI);}
            }
            Bounds bounds=PhysicalBounds(i);Vector3 point=bounds.center;
            if(released[i]&&crossing[i]==null)foreach(var ring in rings)if(ring.TryPass(previous[i],point,ProjectedRadius(ring,bounds)))
            {crossing[i]=ring;centering[i]=new Vector2(ring.transform.InverseTransformPoint(point).x,ring.transform.InverseTransformPoint(point).y).magnitude;crossingDirection[i]=Mathf.Sign(Vector3.Dot(point-ring.transform.position,ring.transform.forward));break;}
            if(crossing[i]!=null)
            {
                var ring=crossing[i];Vector3 local=ring.transform.InverseTransformPoint(point);
                centering[i]=Mathf.Max(centering[i],new Vector2(local.x,local.y).magnitude);
                if(new Vector2(local.x,local.y).magnitude+ProjectedRadius(ring,bounds)>ring.radius)crossing[i]=null;
                else if(local.z*crossingDirection[i]>Extent(ring.transform.forward,bounds.extents)+.1f)
                {float distance=Vector3.Distance(starts[i],ring.transform.position);
                    double points=option==1?distance*35:option==3?1000+trickMeters[i].CompletedTurns*350+trickMeters[i].TrickFlightSeconds*100:option==2?1600+distance*10:1000+distance*10+Mathf.Max(0,1-centering[i]/Mathf.Max(.1f,ring.radius-ProjectedRadius(ring,bounds)))*1000;
                    RecordMetric(i,"ring_passage_center_error_metres",centering[i]);Award(i,points,"complete_physical_ring_passage");ResolveThrow(i,true);return;}
            }
            previous[i] = point;
            quiet[i]=released[i]&&trailer.WheelContact&&trailer.Body.linearVelocity.magnitude<.6f&&trailer.Body.angularVelocity.magnitude<.3f?quiet[i]+dt:0;
            if(trailer.Body.position.y< -55||quiet[i]>2||throwAge[i]>(released[i]?50:35))ResolveThrow(i,false);
        }
        void ApplyTrickRoll(int slot,bool ai,float steer)
        {
            var meter=trickMeters[slot];var body=trailers[slot].Body;
            if(!meter.FreeFlight||body.isKinematic)return;
            Vector3 axis=meter.FlightRollAxis;
            Vector3 principal=Quaternion.Inverse(body.rotation*body.inertiaTensorRotation)*axis;
            Vector3 inertia=body.inertiaTensor;
            float reciprocal=principal.x*principal.x/Mathf.Max(.01f,inertia.x)+principal.y*principal.y/Mathf.Max(.01f,inertia.y)+principal.z*principal.z/Mathf.Max(.01f,inertia.z);
            float moment=1/Mathf.Max(.000001f,reciprocal);
            float requested=Mathf.Clamp(steer,-1,1)*trickMaximumRollRate;
            if(ai)
            {
                // The AI uses the same finite actuator a human operates. It
                // observes actual signed travel and brakes for one real turn.
                float remaining=((slot%2==0?370:-370)-meter.CurrentNetDegrees.z)*Mathf.Deg2Rad;
                float stoppingRate=Mathf.Sqrt(2*Mathf.Max(0,trickRollTorque/moment)*Mathf.Abs(remaining));
                requested=Mathf.Sign(remaining)*Mathf.Min(trickMaximumRollRate,stoppingRate);
            }
            float rate=Vector3.Dot(body.angularVelocity,axis);
            float torque=Mathf.Clamp(moment*(requested-rate)/.12f,-trickRollTorque,trickRollTorque);
            body.AddTorque(axis*torque,ForceMode.Force);
            peakTrickTorque[slot]=Mathf.Max(peakTrickTorque[slot],Mathf.Abs(torque));
        }
        void ConfigureTrickRing(int slot)
        {
            var ring=rings[slot];Vector3 origin=ringOrigins[slot],forward=ring.transform.forward;
            Vector3 destination=origin+forward*trickRingDownrangeOffset;
            trickLayoutState[slot]="original approach retained: clearance unverified";
            // Locate the existing physical ramp, not a generated trajectory.
            if(!Physics.Raycast(origin-forward*5+Vector3.up*25,Vector3.down,out var rampHit,60,~0,QueryTriggerInteraction.Ignore)
                ||rampHit.collider.name!="Momentum launch ramp"||!(rampHit.collider is BoxCollider ramp))
            {trickLayoutState[slot]="original approach retained: physical ramp unavailable";return;}
            Vector3 crest=Vector3.zero;float far=float.NegativeInfinity;
            foreach(int x in new[]{-1,1})foreach(int z in new[]{-1,1})
            {
                Vector3 point=ramp.transform.TransformPoint(ramp.center+Vector3.Scale(ramp.size,new Vector3(x*.5f,.5f,z*.5f)));
                float along=Vector3.Dot(point,forward);if(along>far){far=along;crest=point;}
            }
            crest-=ring.transform.right*Vector3.Dot(crest-origin,ring.transform.right);
            if(!Physics.Raycast(destination+Vector3.up*25,Vector3.down,out var support,80,~0,QueryTriggerInteraction.Ignore)
                ||support.rigidbody!=null||Vector3.Dot(support.normal,Vector3.up)<.8f)
            {trickLayoutState[slot]="original approach retained: no clear supported tracking surface";return;}
            float inner=Mathf.Min(ring.radius*.55f,destination.y-support.point.y-.5f);
            if(inner<3){trickLayoutState[slot]="original approach retained: aperture too close to support";return;}
            Bounds load=PhysicalBounds(slot);float depth=Extent(forward,load.extents);
            Vector3 beginning=origin+forward*Mathf.Max(0,Vector3.Dot(crest-origin,forward));
            Vector3 ending=destination+forward*(depth+.5f);
            Vector3 middle=(beginning+ending)*.5f;middle.y=destination.y;
            Vector3 half=new Vector3(inner,inner,Mathf.Max(.5f,Vector3.Distance(beginning,ending)*.5f));
            foreach(var shape in Physics.OverlapBox(middle,half,ring.transform.rotation,~0,QueryTriggerInteraction.Ignore))
            {
                if(shape==ramp||shape.transform.IsChildOf(ring.transform))continue;
                bool actor=false;foreach(var truck in trucks)actor|=shape.transform.IsChildOf(truck.transform);foreach(var trailer in trailers)actor|=shape.transform.IsChildOf(trailer.transform);
                if(actor)continue;
                trickLayoutState[slot]="original approach retained: flight corridor obstructed by "+shape.name;return;
            }
            // This conservative geometry forecast only guards equipment setup.
            // Actual forces, full swept aperture passage and the signed meter
            // still decide every throw, failure and bonus.
            Vector3 tangent=Vector3.ProjectOnPlane(forward,rampHit.normal).normalized;
            float rear=0;foreach(var wheel in trailers[slot].GetComponentsInChildren<NWH.WheelController3D.WheelController>(true))rear=Mathf.Max(rear,Vector3.Dot(load.center-wheel.transform.position,forward));
            Vector3 takeoff=crest+forward*rear+Vector3.up*Mathf.Max(.5f,trailers[slot].Body.centerOfMass.y);
            Vector3 velocity=tangent*12;float speed=Vector3.Dot(velocity,forward);
            float atCenter=Vector3.Dot(destination-takeoff,forward)/Mathf.Max(.1f,speed),atClear=(Vector3.Dot(destination-takeoff,forward)+depth+.1f)/Mathf.Max(.1f,speed);
            float radius=ProjectedRadius(ring,load);
            foreach(float t in new[]{atCenter,atClear})
            {
                Vector3 point=takeoff+velocity*t+Physics.gravity*(.5f*t*t)-destination;
                float apertureOffset=new Vector2(Vector3.Dot(point,ring.transform.right),Vector3.Dot(point,ring.transform.up)).magnitude;
                if(t<=0||apertureOffset+radius>=ring.radius)
                {trickLayoutState[slot]="original approach retained: existing ramp arc cannot clear relocated aperture";return;}
            }
            movingRings[slot].position=destination;ring.transform.position=destination;trickLaneReady[slot]=true;
            trickLayoutState[slot]="clear physical approach; existing ring offset="+trickRingDownrangeOffset.ToString("F1")+"m support="+support.collider.name;
        }
        void ResolveThrow(int slot,bool passed)
        {
            if(option==3)lastTrickAttempt[slot]="throw="+(attempts[slot]+1)+" passed="+passed+" body="+trailers[slot].Body.worldCenterOfMass.ToString("F2")+" velocity="+trailers[slot].Body.linearVelocity.ToString("F2")+" "+TrickDiagnostic(slot);
            totalAirtime[slot]+=airtime[slot];totalRevolutions[slot]+=this.rotations[slot];attempts[slot]++;
            if(option==3) { rejectedGroundRotation[slot]+=trickMeters[slot].RejectedSupportedDegrees;if(passed)passedTrickTurns[slot]+=trickMeters[slot].CompletedTurns; }
            if(!passed)
            {
                misses[slot]++;scores[slot]-=100;
                ConfirmScore(MatchId+":ring_miss:"+slot+":"+attempts[slot],slot,-100,scores[slot],"physical_ring_miss",SportsScoreTier.Major);
                RecordMetric(slot,"ring_missed_throw",attempts[slot]);
            }
            if(attempts[slot]>=Mathf.Clamp(throwsPerPlayer,1,5)){retired[slot]=true;return;}
            hitches[slot].Release();trailers[slot].ResetForRound();trucks[slot].Prepare(starts[slot],base.rotations[slot]);
            foreach(var part in trucks[slot].GetComponentsInChildren<Rigidbody>(true))part.isKinematic=true;
            foreach(var part in trailers[slot].GetComponentsInChildren<Rigidbody>(true))part.isKinematic=true;
            released[slot]=false;crossing[slot]=null;crossingDirection[slot]=airtime[slot]=this.rotations[slot]=quiet[slot]=throwAge[slot]=centering[slot]=0;
            if(option==3)trickMeters[slot].Reset();
            reloadUntil[slot]=age+3;
        }
    }
}

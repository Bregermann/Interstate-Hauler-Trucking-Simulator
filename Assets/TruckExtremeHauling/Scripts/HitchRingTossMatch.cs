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
        Matrix4x4[] previousRingFrames;
        readonly Bounds[] previousBounds = new Bounds[4];
        readonly Vector3[] movingAim = new Vector3[4];
        readonly float[] movingLeadSeconds = new float[4], movingPassageSpeed = new float[4];
        float[] airtime=new float[4], rotations=new float[4],totalAirtime=new float[4],totalRevolutions=new float[4];
        HaulingTargetRing[] crossing = new HaulingTargetRing[4];
        float[] crossingDirection = new float[4];
        readonly RingAerialTrickMeter[] trickMeters = new RingAerialTrickMeter[4];
        readonly float[] rejectedGroundRotation = new float[4];
        readonly int[] passedTrickTurns = new int[4];
        public int AerialTrickTurns(int slot) => (int)totalRevolutions[slot] + (released[slot] && !retired[slot] && trickMeters[slot] != null ? trickMeters[slot].CompletedTurns : 0);
        public int PassedAerialTrickTurns(int slot) => passedTrickTurns[slot];
        public bool TrailerReleased(int slot) => released[slot];
        public float MaximumMovingPassageSpeed(int slot) => movingPassageSpeed[slot];
        public string MovingRingDiagnostic(int slot) => "attempts=" + attempts[slot] + " passages=" + deliveries[slot] + " misses=" + misses[slot] + " released=" + released[slot] + " aim=" + movingAim[slot].ToString("F2") + " leadSeconds=" + movingLeadSeconds[slot].ToString("F2") + " ring=" + rings[slot % rings.Length].transform.position.ToString("F2") + " load=" + trailers[slot].Body.worldCenterOfMass.ToString("F2") + " loadVelocity=" + trailers[slot].Body.linearVelocity.ToString("F2") + " crossing=" + (crossing[slot] != null) + " maxPassageRingSpeed=" + movingPassageSpeed[slot].ToString("F2");
        public string TrickDiagnostic(int slot) => trickMeters[slot] != null ? trickMeters[slot].Diagnostic() + " passedTurns=" + passedTrickTurns[slot] + " released=" + released[slot] + " nativeWheelContact=" + trailers[slot].WheelContact : "Trick equipment not initialized";
        protected override void ReportFinalMeasurements(int i) { RecordMetric(i,"ring_attempts_resolved",attempts[i]);RecordMetric(i,"ring_misses",misses[i]);RecordMetric(i,"ring_trailer_released",released[i]?1:0);RecordMetric(i,"ring_airborne_seconds",totalAirtime[i]);RecordMetric(i,"ring_physical_revolutions",totalRevolutions[i]);RecordMetric(i,"ring_complete_passages",deliveries[i]); if (option == 3) { RecordMetric(i,"ring_rejected_supported_rotation_degrees",rejectedGroundRotation[i]); RecordMetric(i,"ring_passage_qualified_aerial_turns",passedTrickTurns[i]); } }
        public override string Title => "HITCH RING TOSS";
        protected override string[] Modes => new[] { "PRECISION RING TOSS", "LONG-DISTANCE TOSS", "MOVING RING CHALLENGE", "TRICK SHOT CHAMPIONSHIP" };
        public override string Help => "Swing the attached trailer with the tractor. ACTION releases the real hitch, preserving momentum. The trailer must physically cross a ring aperture; projected aim is not a score. More distant rings award larger multipliers. Precision rewards the actual centered passage of the whole load. Moving Ring Challenge: lead the laterally moving aperture and allow room for your entire trailer to clear while the physical frame keeps moving. Trick Championship rewards complete signed pitch, yaw or roll turns only during genuine free flight, then a complete ring passage. Ground spins, roof sliding and back-and-forth rocking do not earn trick bonuses. The live AIR TURNS counter shows measured revolutions; a missed ring cannot bank trick points. Each match has the configured number of physical throws; a missed throw costs100points. Between throws the normal equipment reset returns your original rig to the loading start.";
        protected override void BeginMechanic()
        {
            System.Array.Clear(attempts,0,attempts.Length);System.Array.Clear(misses,0,misses.Length);System.Array.Clear(reloadUntil,0,reloadUntil.Length);System.Array.Clear(throwAge,0,throwAge.Length);System.Array.Clear(quiet,0,quiet.Length);System.Array.Clear(centering,0,centering.Length);
            if(ringOrigins==null)
            {
                ringOrigins=new Vector3[rings.Length];baseRadii=new float[rings.Length];movingRings=new Rigidbody[rings.Length];
                for(int i=0;i<rings.Length;i++){ringOrigins[i]=rings[i].transform.position;baseRadii[i]=rings[i].radius;movingRings[i]=rings[i].GetComponent<Rigidbody>();if(movingRings[i]==null)movingRings[i]=rings[i].gameObject.AddComponent<Rigidbody>();movingRings[i].isKinematic=true;movingRings[i].useGravity=false;}
            }
            for(int i=0;i<rings.Length;i++){movingRings[i].position=ringOrigins[i];rings[i].transform.position=ringOrigins[i];rings[i].radius=baseRadii[i];}
            previousRingFrames=new Matrix4x4[rings.Length];
            for(int i=0;i<rings.Length;i++)previousRingFrames[i]=rings[i].transform.worldToLocalMatrix;
            System.Array.Clear(movingAim,0,movingAim.Length);System.Array.Clear(movingLeadSeconds,0,movingLeadSeconds.Length);System.Array.Clear(movingPassageSpeed,0,movingPassageSpeed.Length);
            System.Array.Clear(airtime,0,airtime.Length);System.Array.Clear(rotations,0,rotations.Length);System.Array.Clear(totalAirtime,0,totalAirtime.Length);System.Array.Clear(totalRevolutions,0,totalRevolutions.Length);
            System.Array.Clear(rejectedGroundRotation,0,rejectedGroundRotation.Length);System.Array.Clear(passedTrickTurns,0,passedTrickTurns.Length);
            System.Array.Clear(released,0,released.Length);System.Array.Clear(crossing,0,crossing.Length);shapes=new Collider[trailers.Length][];
            for(int i=0;i<trailers.Length;i++)
            {
                shapes[i]=trailers[i].GetComponentsInChildren<Collider>(true);previousBounds[i]=PhysicalBounds(i);previous[i]=previousBounds[i].center;
                if(option==3)
                {
                    var contact=trailers[i].Body.GetComponent<UnhitchedSurfaceContact>();
                    if(contact==null)contact=trailers[i].Body.gameObject.AddComponent<UnhitchedSurfaceContact>();
                    contact.Configure(trailers[i].transform);
                    trickMeters[i]=new RingAerialTrickMeter(trailers[i].Body,contact);
                }
            }
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
            // Keep the actual last simulated frame, not the scheduled next
            // MovePosition pose. PhysX owns both load and moving frame motion.
            if(option==2&&previousRingFrames!=null)for(int i=0;i<rings.Length;i++)previousRingFrames[i]=rings[i].transform.worldToLocalMatrix;
        }
        protected override Vector2 AICommand(int i,float dt)
        {
            if(released[i])return new Vector2(i%2==0?-1:1,.8f);
            int ringIndex=i%rings.Length;
            Vector3 target=rings[ringIndex].transform.position;
            if(option==2)
            {
                var load=trailers[i].Body;var ring=rings[ringIndex];
                float remaining=Vector3.Dot(target-load.worldCenterOfMass,ring.transform.forward);
                float speed=Mathf.Max(4,Vector3.Dot(load.linearVelocity,ring.transform.forward));
                movingLeadSeconds[i]=Mathf.Clamp(remaining/speed,0,4)*(setup.players[i].difficulty==BowlingDifficulty.Easy ? .6f : 1);
                target=ringOrigins[ringIndex]+Vector3.right*(Mathf.Sin((age+movingLeadSeconds[i])*.7f+ringIndex)*5);
                movingAim[i]=target;
            }
            Vector3 delta=trucks[i].transform.InverseTransformPoint(target);float angle=Mathf.Atan2(delta.x,delta.z)*Mathf.Rad2Deg;
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
                reloadUntil[i]=0;previousBounds[i]=PhysicalBounds(i);previous[i]=previousBounds[i].center;return;
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
                }
                else {if(!trailer.WheelContact)airtime[i]+=dt;rotations[i]+=trailer.Body.angularVelocity.magnitude*dt/(2*Mathf.PI);}
            }
            Bounds bounds=PhysicalBounds(i);Vector3 point=bounds.center;
            if(released[i]&&crossing[i]==null)for(int ringIndex=0;ringIndex<rings.Length;ringIndex++)
            {
                var ring=rings[ringIndex];float direction=0,error=0;
                bool enters=option==2
                    ? TryMovingRingCenterCrossing(previousRingFrames[ringIndex],ring.transform.worldToLocalMatrix,previous[i],point,ring.radius,Mathf.Max(ProjectedRadius(ring,previousBounds[i]),ProjectedRadius(ring,bounds)),out direction,out error)
                    : ring.TryPass(previous[i],point,ProjectedRadius(ring,bounds));
                if(!enters)continue;
                crossing[i]=ring;
                centering[i]=option==2?error:new Vector2(ring.transform.InverseTransformPoint(point).x,ring.transform.InverseTransformPoint(point).y).magnitude;
                crossingDirection[i]=option==2?direction:Mathf.Sign(Vector3.Dot(point-ring.transform.position,ring.transform.forward));break;
            }
            if(crossing[i]!=null)
            {
                var ring=crossing[i];Vector3 local=ring.transform.InverseTransformPoint(point);
                centering[i]=Mathf.Max(centering[i],new Vector2(local.x,local.y).magnitude);
                if(new Vector2(local.x,local.y).magnitude+ProjectedRadius(ring,bounds)>ring.radius)crossing[i]=null;
                else if(local.z*crossingDirection[i]>Extent(ring.transform.forward,bounds.extents)+.1f)
                {float distance=Vector3.Distance(starts[i],ring.transform.position);
                    double points=option==1?distance*35:option==3?1000+trickMeters[i].CompletedTurns*350+trickMeters[i].TrickFlightSeconds*100:option==2?1600+distance*10:1000+distance*10+Mathf.Max(0,1-centering[i]/Mathf.Max(.1f,ring.radius-ProjectedRadius(ring,bounds)))*1000;
                    if(option==2)
                    {
                        int ringIndex=System.Array.IndexOf(rings,ring);
                        Vector3 lastRingPosition=previousRingFrames[ringIndex].inverse.MultiplyPoint3x4(Vector3.zero);
                        float actualSpeed=Vector3.Distance(ring.transform.position,lastRingPosition)/Mathf.Max(.0001f,dt);
                        movingPassageSpeed[i]=Mathf.Max(movingPassageSpeed[i],actualSpeed);
                        RecordMetric(i,"ring_moving_aperture_speed_mps",actualSpeed);
                    }
                    RecordMetric(i,"ring_passage_center_error_metres",centering[i]);Award(i,points,"complete_physical_ring_passage");ResolveThrow(i,true);return;}
            }
            previous[i] = point;previousBounds[i]=bounds;
            quiet[i]=released[i]&&trailer.WheelContact&&trailer.Body.linearVelocity.magnitude<.6f&&trailer.Body.angularVelocity.magnitude<.3f?quiet[i]+dt:0;
            if(trailer.Body.position.y< -55||quiet[i]>2||throwAge[i]>(released[i]?50:35))ResolveThrow(i,false);
        }
        public static bool TryMovingRingCenterCrossing(Matrix4x4 previousWorldToLocal,Matrix4x4 currentWorldToLocal,Vector3 previousCenter,Vector3 currentCenter,float apertureRadius,float bodyRadius,out float direction,out float centerError)
        {
            direction=centerError=0;
            if(bodyRadius<0||bodyRadius>=apertureRadius)return false;
            Vector3 a=previousWorldToLocal.MultiplyPoint3x4(previousCenter),b=currentWorldToLocal.MultiplyPoint3x4(currentCenter);
            if(a.z*b.z>0)return false;
            float denominator=a.z-b.z;
            if(Mathf.Abs(denominator)<.00001f)return false;
            Vector3 entry=Vector3.Lerp(a,b,a.z/denominator);
            centerError=new Vector2(entry.x,entry.y).magnitude;
            direction=Mathf.Sign(b.z-a.z);
            return centerError+bodyRadius<=apertureRadius;
        }
        void ResolveThrow(int slot,bool passed)
        {
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

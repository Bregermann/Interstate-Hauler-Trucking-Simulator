using UnityEngine;
using LWS.TruckSports;
using LWS.TruckHitchSports;
using LWS.TruckBowling;

namespace LWS.TruckExtremeHauling
{
    public sealed class HitchRingTossMatch : ExtremeHaulingMatch
    {
        public HaulingTargetRing[] rings;
        protected override Vector3 CameraFocus(int slot) {slot=CameraParticipant(slot);return released[slot] ? trailers[slot].Body.worldCenterOfMass : trucks[slot].body.position;}
        Vector3[] previous = new Vector3[4]; bool[] released = new bool[4];
        Collider[][] shapes;
        Vector3[] ringOrigins;
        float[] baseRadii;
        Rigidbody[] movingRings;
        float[] airtime=new float[4], rotations=new float[4];
        HaulingTargetRing[] crossing = new HaulingTargetRing[4];
        float[] crossingDirection = new float[4];
        protected override void ReportFinalMeasurements(int i) { RecordMetric(i,"ring_trailer_released",released[i]?1:0);RecordMetric(i,"ring_airborne_seconds",airtime[i]);RecordMetric(i,"ring_physical_revolutions",rotations[i]);RecordMetric(i,"ring_complete_passages",deliveries[i]); }
        public override string Title => "HITCH RING TOSS";
        protected override string[] Modes => new[] { "PRECISION RING TOSS", "LONG-DISTANCE TOSS", "MOVING RING CHALLENGE", "TRICK SHOT CHAMPIONSHIP" };
        public override string Help => "Swing the attached trailer with the tractor. ACTION releases the real hitch, preserving momentum. The trailer must physically cross a ring aperture; projected aim is not a score. More distant rings award larger multipliers.";
        protected override void BeginMechanic()
        {
            if(ringOrigins==null)
            {
                ringOrigins=new Vector3[rings.Length];baseRadii=new float[rings.Length];movingRings=new Rigidbody[rings.Length];
                for(int i=0;i<rings.Length;i++){ringOrigins[i]=rings[i].transform.position;baseRadii[i]=rings[i].radius;movingRings[i]=rings[i].GetComponent<Rigidbody>();if(movingRings[i]==null)movingRings[i]=rings[i].gameObject.AddComponent<Rigidbody>();movingRings[i].isKinematic=true;movingRings[i].useGravity=false;}
            }
            for(int i=0;i<rings.Length;i++){movingRings[i].position=ringOrigins[i];rings[i].transform.position=ringOrigins[i];rings[i].radius=baseRadii[i];}
            System.Array.Clear(airtime,0,airtime.Length);System.Array.Clear(rotations,0,rotations.Length);
            System.Array.Clear(released,0,released.Length);System.Array.Clear(crossing,0,crossing.Length);shapes=new Collider[trailers.Length][];
            for(int i=0;i<trailers.Length;i++){shapes[i]=trailers[i].GetComponentsInChildren<Collider>(true);previous[i]=PhysicalBounds(i).center;}
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
            if(released[i]){if(!trailer.WheelContact)airtime[i]+=dt;rotations[i]+=trailer.Body.angularVelocity.magnitude*dt/(2*Mathf.PI);}
            Bounds bounds=PhysicalBounds(i);Vector3 point=bounds.center;
            if(released[i]&&crossing[i]==null)foreach(var ring in rings)if(ring.TryPass(previous[i],point,ProjectedRadius(ring,bounds)))
            {crossing[i]=ring;crossingDirection[i]=Mathf.Sign(Vector3.Dot(point-ring.transform.position,ring.transform.forward));break;}
            if(crossing[i]!=null)
            {
                var ring=crossing[i];Vector3 local=ring.transform.InverseTransformPoint(point);
                if(new Vector2(local.x,local.y).magnitude+ProjectedRadius(ring,bounds)>ring.radius)crossing[i]=null;
                else if(local.z*crossingDirection[i]>Extent(ring.transform.forward,bounds.extents)+.1f)
                {float distance=Vector3.Distance(starts[i],ring.transform.position);
                    double points=option==1?distance*35:option==3?1000+Mathf.Floor(rotations[i])*350+airtime[i]*100:option==2?1600+distance*10:1000+distance*10;
                    Award(i,points,"complete_physical_ring_passage");retired[i]=true;crossing[i]=null;}
            }
            previous[i] = point;
        }
    }
}

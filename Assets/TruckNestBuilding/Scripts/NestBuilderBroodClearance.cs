using UnityEngine;
using LWS.TruckBowling;
namespace LWS.TruckNestBuilding
{
    public sealed partial class NestBuilder
    {
        NestMaterial broodSurplus;
        float broodCarryHeight;
        public bool ClearingBrood => broodSurplus != null;
        public int BroodClearancesCompleted { get; private set; }
        void ClearBroodClearance() { broodSurplus=null;broodCarryHeight=0; }

        // Planning only: the original rooted lining/contact/egg predicates stay
        // authoritative. A loose duplicate can ride a replacement up to the
        // brood, where the egg rests on the wrong physical body (R35/R41).
        public static bool ObstructsBrood(NestBuildSite site,NestMaterial piece)
        {
            if(site==null||piece==null||piece.kind!=NestMaterialKind.Moss||piece.Site!=null||piece.Interlock!=null||piece.Grip!=null
                ||piece.shape==null||!piece.shape.enabled||!piece.gameObject.activeInHierarchy)return false;
            var lining=site.slots[9];
            if(lining==null||!site.Rooted(lining)||piece==lining)return false;
            var bounds=piece.shape.bounds;var support=lining.shape.bounds;
            // Only the actual central egg landing footprint above the rooted
            // lining, not every spare in the venue or another player's cargo.
            var centre=lining.body.position;
            return bounds.min.x<centre.x+.65f&&bounds.max.x>centre.x-.65f
                &&bounds.min.z<centre.z+.65f&&bounds.max.z>centre.z-.65f
                &&bounds.max.y>support.max.y&&bounds.min.y>=support.min.y-.05f&&bounds.min.y<support.max.y+2;
        }
        public static bool OutsideBroodPlatform(Bounds cargo,Bounds deck)
        {
            // Keep the entire actual loose body beyond the platform before its
            // ordinary release. Gravity then stages it on the existing ground.
            return cargo.max.x<deck.min.x-.5f||cargo.min.x>deck.max.x+.5f
                ||cargo.max.z<deck.min.z-.5f||cargo.min.z>deck.max.z+.5f;
        }
        public static Vector3 BroodStagingTarget(Bounds cargo,Bounds deck,Vector3 home,float height)
        {
            var toward=home-deck.center;var target=home;target.y=height;
            // The existing hand home gives the direction of its real work bay.
            // Select the corresponding platform edge using ACTUAL collider
            // extents, so a closer parked truck cannot leave the target inside.
            if(Mathf.Abs(toward.z)>=Mathf.Abs(toward.x))
                target.z=toward.z<0?deck.min.z-cargo.extents.z-.75f:deck.max.z+cargo.extents.z+.75f;
            else target.x=toward.x<0?deck.min.x-cargo.extents.x-.75f:deck.max.x+cargo.extents.x+.75f;
            return target;
        }
        bool ClosestFreeBroodBuilder(NestMaterial piece,NestBuildSite site)
        {
            NestBuilder closest=null;float distance=float.PositiveInfinity;
            for(int i=0;i<match.setup.count;i++)
            {
                var other=match.builders[i];
                if(match.setup.players[i].source!=BowlingSource.AI||other.Carried!=null||other.Selected!=null
                    ||other.Mode!=NestBuilderMode.Drive||other.ClearingBrood)continue;
                if(!match.rules.Cooperative&&i!=site.index)continue;
                float d=(other.truck.body.position-piece.body.position).sqrMagnitude;
                if(d<distance){distance=d;closest=other;}
            }
            return closest==this;
        }
        bool ThinkBroodClearance(NestBuildSite site,float dt,out Vector2 drive)
        {
            drive=Vector2.zero;
            if(ClearingBrood)
            {
                if(Carried==broodSurplus)
                {
                    // Slide level away from the brood rather than descending
                    // through the real deck lip or lifting the live egg. Cargo
                    // remains dynamic on the original2500/1800 finite joint.
                    var target=BroodStagingTarget(Carried.shape.bounds,site.deck.bounds,truck.transform.TransformPoint(carryHome),broodCarryHeight);
                    var error=Quaternion.Inverse(truck.body.rotation)*(target-Carried.body.position);
                    Nudge(Vector3.ClampMagnitude(error*2,2),0,dt);
                    if(OutsideBroodPlatform(Carried.shape.bounds,site.deck.bounds))
                    {
                        Carried.Drop();Carried=null;ClearGraspOrientation();stowing=false;
                        ClearBroodClearance();PhysicalDrops++;BroodClearancesCompleted++;
                        Mode=NestBuilderMode.Drive;
                    }
                    return true;
                }
                if(Selected==broodSurplus&&Mode==NestBuilderMode.Grasping)return false;
                ClearBroodClearance(); // real failed/cancelled/broken grasp
            }
            if(Carried!=null||Selected!=null||Mode!=NestBuilderMode.Drive)return false;
            NestMaterial obstruction=null;
            foreach(var piece in match.materials)
                if(piece.Available&&piece.body.linearVelocity.sqrMagnitude<1&&ObstructsBrood(site,piece))
                    {obstruction=piece;break;}
            if(obstruction==null||!ClosestFreeBroodBuilder(obstruction,site))return false;
            var local=truck.transform.InverseTransformPoint(obstruction.body.position);
            if(WithinHandWorkspace(local)&&Mathf.Abs(local.x)<23&&local.z>2&&local.z<30
                &&Vector3.Distance(obstruction.body.position,truck.body.position)<32&&truck.body.linearVelocity.magnitude<.7f)
            {
                if(TrySelect(obstruction))
                {
                    site.ReleaseClaim(slot);GoalSite=site;GoalSocket=-1;waiting=null;
                    broodSurplus=obstruction;
                }
                return true;
            }
            var park=AcquisitionParkingTarget(obstruction.body.position,truck.transform.forward,truck.body.position.y);
            drive=Navigate(park,Vector3.Dot(park-truck.body.position,truck.transform.forward)<-4);
            return true;
        }
    }
}

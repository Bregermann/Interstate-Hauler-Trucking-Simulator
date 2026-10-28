using UnityEngine;
using UnityEngine.AI;
namespace LWS.TruckCamping
{
    public sealed partial class CampExplorer
    {
        public int NavigationShortcutRejections { get; private set; }
        public int NavigationRecoveries { get; private set; }
        Vector3 navigationProgress;
        float navigationSampleTime, navigationStuckAge, navigationReverseAge, navigationRecoveryCooldown;
        bool navigationProgressReady;
        CampHammock navigationHammock;
        Vector3 hammockRunup;
        bool hammockRunupReached;
        void ResetNavigationRecovery()
        {
            navigationProgressReady=false;navigationStuckAge=navigationReverseAge=navigationRecoveryCooldown=0;
            NavigationShortcutRejections=NavigationRecoveries=0;
            navigationHammock=null;hammockRunupReached=false;
        }
        Vector3 HammockNavigationLeg(Vector3 requested,ref float stoppingDistance,out bool directDeckApproach)
        {
            directDeckApproach=false;CampHammock found=null;
            if(match.world.hammocks!=null)foreach(var hammock in match.world.hammocks)
                if(hammock!=null&&hammock.approach!=null&&hammock.deck!=null&&
                    (hammock.approach.position-requested).sqrMagnitude<.01f){found=hammock;break;}
            if(found==null){navigationHammock=null;hammockRunupReached=false;return requested;}
            if(navigationHammock!=found)
            {
                navigationHammock=found;hammockRunupReached=false;
                var forward=Vector3.ProjectOnPlane(found.transform.forward,Vector3.up).normalized;
                if(forward.sqrMagnitude<.9f){navigationHammock=null;return requested;}
                float side=Vector3.Dot(truck.body.position-found.transform.position,forward)>=0?1:-1;
                float rampExtent=0;
                foreach(var shape in found.GetComponentsInChildren<Collider>(true))
                {
                    if(!shape.enabled||shape.isTrigger||shape.name!="Actual gently sloped hammock approach")continue;
                    var bounds=shape.bounds;float extent=Mathf.Abs(Vector3.Dot(bounds.center-found.transform.position,forward))+
                        Mathf.Abs(forward.x)*bounds.extents.x+Mathf.Abs(forward.z)*bounds.extents.z;
                    rampExtent=Mathf.Max(rampExtent,extent);
                }
                if(rampExtent<=0){navigationHammock=null;return requested;}
                // A walking-agent corner can clip the edge of this truck-wide
                // moving deck. Approach its real ramp from the centerline with
                // room to turn before the lip, then use the original NWH drive.
                hammockRunup=found.transform.position+forward*(side*(rampExtent+24));
                if(match.world.WalkingPoint(hammockRunup,out var ground))hammockRunup=ground;
                var toDeck=Vector3.ProjectOnPlane(requested-truck.body.position,Vector3.up);
                var lateral=toDeck-forward*Vector3.Dot(toDeck,forward);
                if(lateral.magnitude<1&&toDeck.sqrMagnitude>.01f&&toDeck.magnitude<rampExtent+29&&
                    Vector3.Dot(truck.transform.forward,toDeck.normalized)>.94f)hammockRunupReached=true;
                nextPath=0;
            }
            if(!hammockRunupReached)
            {
                float remaining=Vector3.ProjectOnPlane(hammockRunup-truck.body.position,Vector3.up).magnitude;
                if(remaining<2.8f&&truck.body.linearVelocity.magnitude<1.3f){hammockRunupReached=true;nextPath=0;}
                else{stoppingDistance=2.5f;return hammockRunup;}
            }
            // The final target is on a genuine dynamic spring platform, which
            // is not a walking NavMesh surface. Its authored ramp is the route;
            // hull/wheel collisions and steering stay fully physical.
            directDeckApproach=true;return requested;
        }
        bool ActualHullObstruction(Vector3 direction,float distance)
        {
            if(direction.sqrMagnitude<.001f||distance<=0)return false;
            foreach(var hit in truck.body.SweepTestAll(direction.normalized,distance,QueryTriggerInteraction.Ignore))
            {
                if(hit.collider==null||hit.collider.transform.IsChildOf(truck.transform)||hit.rigidbody==truck.body)continue;
                // Terrain/trail top support is not a lateral obstacle. All
                // actual furniture/other-truck/pickup hull collisions remain.
                if(Vector3.Dot(hit.normal,Vector3.up)<.6f)return true;
            }
            return false;
        }
        public static int FirstUnreachedNavigationCorner(Vector3 position,Vector3[] corners)
        {
            if(corners==null||corners.Length<2)return -1;
            for(int i=1;i<corners.Length;i++)if(Vector3.ProjectOnPlane(corners[i]-position,Vector3.up).magnitude>2.5f)return i;
            return corners.Length-1;
        }
        Vector3 HullClearNavigationAim(Vector3[] corners,Vector3 destination)
        {
            int first=FirstUnreachedNavigationCorner(truck.body.position,corners);if(first<0)return destination;
            Vector3 aim=corners[first];
            for(int i=first+1;i<corners.Length;i++)
            {
                var delta=Vector3.ProjectOnPlane(corners[i]-truck.body.position,Vector3.up);bool clear=false;
                if(match.world.WalkingPoint(truck.body.position,out var from)&&match.world.WalkingPoint(corners[i],out var to))
                    clear=!NavMesh.Raycast(from,to,out _,NavMesh.AllAreas)&&!ActualHullObstruction(delta,Mathf.Min(18,delta.magnitude));
                if(!clear){NavigationShortcutRejections++;break;}
                aim=corners[i];if(delta.magnitude>18)break;
            }
            return aim;
        }
        Vector2 RecoverActualBlockedNavigation(Vector2 requested,float headingError,float remaining,float stoppingDistance)
        {
            float now=Time.fixedTime;
            if(!navigationProgressReady){navigationProgressReady=true;navigationProgress=truck.body.position;navigationSampleTime=now;return requested;}
            float dt=Mathf.Clamp(now-navigationSampleTime,0,.25f);navigationSampleTime=now;
            navigationRecoveryCooldown=Mathf.Max(0,navigationRecoveryCooldown-dt);
            if(navigationReverseAge>0)
            {
                navigationReverseAge=Mathf.Max(0,navigationReverseAge-dt);navigationProgress=truck.body.position;navigationStuckAge=0;
                return new Vector2(-Mathf.Sign(headingError),-.3f);
            }
            float moved=Vector3.ProjectOnPlane(truck.body.position-navigationProgress,Vector3.up).magnitude;
            if(moved>.25f||remaining<=stoppingDistance+2||requested.y<=.1f||truck.body.linearVelocity.magnitude>.35f)
            {navigationProgress=truck.body.position;navigationStuckAge=0;return requested;}
            navigationStuckAge+=dt;
            if(navigationStuckAge>=2&&navigationRecoveryCooldown<=0&&ActualHullObstruction(truck.transform.forward,1.5f))
            {
                // Ordinary bounded NWH reverse/steer only. No pose writes,
                // collider changes, immunity, force changes or remote gathers.
                NavigationRecoveries++;navigationReverseAge=2.5f;navigationRecoveryCooldown=6;navigationStuckAge=0;
                return new Vector2(-Mathf.Sign(headingError),-.3f);
            }
            return requested;
        }
    }
}

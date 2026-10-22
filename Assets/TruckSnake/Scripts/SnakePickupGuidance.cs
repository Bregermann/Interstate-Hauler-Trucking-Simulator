using CompassNavigatorPro;
using UnityEngine;
using LWS.TruckSports;

namespace LWS.TruckSnake
{
    // Only objective selection is sport-owned. CNP retains all projection,
    // rotation, clipping and per-camera indicator presentation.
    [DefaultExecutionOrder(900)]
    public sealed class SnakePickupGuidance : MonoBehaviour
    {
        public SnakeMatch match;
        public SportsBroadcastCompass[] compasses;
        public CompassProPOI[] targets;
        readonly SnakeRouteAI[] routes={new SnakeRouteAI(),new SnakeRouteAI(),new SnakeRouteAI(),new SnakeRouteAI()};
        readonly SnakePickup[] selected=new SnakePickup[4];
        readonly float[] next=new float[4];
        readonly bool[] presenting=new bool[4];
        public int RouteQueries{get;private set;}
        public float MaxQueryMilliseconds{get;private set;}
        public SnakePickup Objective(int view)=>view>=0&&view<selected.Length?selected[view]:null;
        int SharedOwner(){var framing=match.broadcast.broadcastFraming;if(framing!=null&&framing.Primary>=0&&framing.Primary<match.setup.count&&match.rules.Playing(framing.Primary))return framing.Primary;for(int slot=0;slot<match.setup.count;slot++)if(match.rules.Playing(slot)&&match.setup.players[slot].source!=LWS.TruckBowling.BowlingSource.AI)return slot;for(int slot=0;slot<match.setup.count;slot++)if(match.rules.Playing(slot))return slot;return 0;}

        void LateUpdate()
        {
            for(int view=0;view<compasses.Length;view++)
            {
                var compass=compasses[view];var marker=targets[view];
                compass.onScreenIndicatorShowDistance=compass.offScreenIndicatorShowDistance=false;
                bool arena=match.drivingViews.NeedsArena;
                bool active=match.Running&&!match.Finished&&match.Intro<=0&&view<(arena?1:match.drivingViews.rally.Humans)&&view<match.cameras.Length&&match.cameras[view].enabled;
                int owner=active?(arena?SharedOwner():match.drivingViews.rally.owners[view]):0;
                if(active)active=match.rules.Playing(owner);
                if(active&&!presenting[view])next[view]=Time.unscaledTime+view*.07f;
                presenting[view]=active;
                if(active&&(Time.unscaledTime>=next[view]||selected[view]!=null&&!selected[view].Live))
                {
                    next[view]=Time.unscaledTime+1.25f+view*.07f;
                    var timer=System.Diagnostics.Stopwatch.StartNew();selected[view]=routes[view].NearestReachablePickup(match,owner);timer.Stop();RouteQueries++;
                    MaxQueryMilliseconds=Mathf.Max(MaxQueryMilliseconds,(float)timer.Elapsed.TotalMilliseconds);
                }
                if(!active){selected[view]=null;next[view]=0;}
                bool visible=active&&selected[view]!=null&&selected[view].Live;
                if(compass.gameObject.activeSelf!=visible)compass.gameObject.SetActive(visible);
                if(marker.gameObject.activeSelf!=visible)marker.gameObject.SetActive(visible);
                if(!visible)continue;
                marker.transform.position=selected[view].transform.position+Vector3.up*2;
                marker.tintColor=selected[view].Golden?new Color(1,.75f,.1f):new Color(.2f,1,.4f);
                compass.cameraMain=match.cameras[view];compass.viewportRect=match.cameras[view].rect;compass.follow=match.trucks[owner].transform;
                if(!compass.POIisRegistered(marker))compass.POIRegister(marker);
            }
        }
        void OnDisable(){if(compasses!=null)foreach(var compass in compasses)if(compass!=null)compass.gameObject.SetActive(false);if(targets!=null)foreach(var marker in targets)if(marker!=null)marker.gameObject.SetActive(false);}
    }
}

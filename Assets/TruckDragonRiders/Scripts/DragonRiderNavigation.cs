using System;
using CompassNavigatorPro;
using UnityEngine;

namespace LWS.TruckDragonRiders
{
    // Aerial objective semantics over the installed Compass renderer. Ground
    // road routing is intentionally not used to invent a three-dimensional path.
    public sealed class DragonRiderNavigation : MonoBehaviour
    {
        public DragonRidersMatch match;
        public CompassPro[] compasses=Array.Empty<CompassPro>();
        public CompassProPOI[] objectives=Array.Empty<CompassProPOI>();
        void LateUpdate()
        {
            if(match==null||match.drivingViews==null)return;
            for(int view=0;view<compasses.Length;view++)
            {
                bool visible=match.Running&&!match.Finished&&!match.drivingViews.NeedsArena&&view<match.drivingViews.rally.Humans;
                int slot=visible?match.drivingViews.rally.owners[view]:-1;
                visible&=slot>=0&&slot<match.rules.Count&&!match.rules.Finished(slot)&&!match.rules.Retired[slot];
                compasses[view].gameObject.SetActive(visible);objectives[view].gameObject.SetActive(visible);
                if(!visible)continue;
                var camera=match.cameras[view];compasses[view].cameraMain=camera;compasses[view].viewportRect=camera.rect;compasses[view].follow=match.trucks[slot].transform;
                int next=match.rules.NextGate(slot);objectives[view].transform.position=match.rules.GateCenter(next);
                objectives[view].title="GATE "+(next+1)+" · LAP "+match.rules.CurrentLap(slot);objectives[view].tintColor=match.setup.players[slot].color;
            }
        }
        void OnDisable()
        {foreach(var compass in compasses)if(compass!=null)compass.gameObject.SetActive(false);foreach(var marker in objectives)if(marker!=null)marker.gameObject.SetActive(false);}
    }
}

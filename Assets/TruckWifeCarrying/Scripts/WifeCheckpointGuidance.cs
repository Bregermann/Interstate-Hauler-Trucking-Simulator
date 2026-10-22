using CompassNavigatorPro;
using UnityEngine;
using LWS.TruckSports;

namespace LWS.TruckWifeCarrying
{
    // Presentation only: ordered checkpoints and pair/device ownership stay in
    // WifePair and SportsRallyViews. CNP owns projection and viewport placement.
    [DefaultExecutionOrder(900)]
    public sealed class WifeCheckpointGuidance : MonoBehaviour
    {
        public WifeMatch match;
        public SportsBroadcastCompass[] compasses;
        public CompassProPOI[] targets;
        void LateUpdate()
        {
            for(int view=0;view<compasses.Length;view++)
            {
                var compass=compasses[view];var target=targets[view];
                bool active=match.Running&&!match.Finished&&match.Phase==WifePhase.Race&&view<match.views.Humans&&view<match.cameras.Length&&match.cameras[view].enabled;
                int pair=active?match.views.owners[view]:0;
                if(active){var state=match.pairs[pair].State;active=state!=WifeState.Finished&&state!=WifeState.DNF&&state!=WifeState.Inactive;}
                if(compass.gameObject.activeSelf!=active)compass.gameObject.SetActive(active);
                if(target.gameObject.activeSelf!=active)target.gameObject.SetActive(active);
                if(!active)continue;
                var racer=match.pairs[pair];float distance=match.course.checkpoints[Mathf.Min(racer.Checkpoint,match.course.checkpoints.Length-1)];
                target.transform.position=(match.course.loopCircuit?match.course.Point(distance):match.course.Surface(new Vector3(match.course.Center(distance),0,distance)))+Vector3.up*2;
                target.tintColor=match.setup.players[pair].color;
                compass.cameraMain=match.cameras[view];compass.viewportRect=match.cameras[view].rect;compass.follow=racer.driver.transform;
                if(!compass.POIisRegistered(target))compass.POIRegister(target);
            }
        }
        void OnDisable(){if(compasses!=null)foreach(var compass in compasses)if(compass!=null)compass.gameObject.SetActive(false);if(targets!=null)foreach(var target in targets)if(target!=null)target.gameObject.SetActive(false);}
    }
}

using System;
using CompassNavigatorPro;
using UnityEngine;

namespace LWS.TruckDragonKingdom
{
    // Compass observes the already-computed per-owner LWS road routes. No graph
    // search or competitor control is performed by a camera or HUD update.
    public sealed class DragonKingdomNavigation : MonoBehaviour
    {
        public DragonKingdomGroundMatch match;
        public CompassPro[] gps = Array.Empty<CompassPro>();
        public CompassProPOI[] markers = Array.Empty<CompassProPOI>();
        readonly int[] owners = { -1, -1, -1, -1 };
        readonly int[] searches = { -1, -1, -1, -1 };
        void LateUpdate()
        {
            if (match == null || match.drivingViews == null) return;
            for (int view = 0; view < gps.Length; view++)
            {
                bool visible = match.Running && !match.Finished && !match.drivingViews.NeedsArena
                    && view < match.drivingViews.rally.Humans;
                int slot = visible ? match.drivingViews.rally.owners[view] : -1;
                visible &= slot >= 0 && slot < match.setup.count && !match.Retired[slot];
                gps[view].gameObject.SetActive(visible);
                if (view < markers.Length && markers[view] != null) markers[view].gameObject.SetActive(visible);
                if (!visible) continue;
                gps[view].follow = match.trucks[slot].transform;
                gps[view].cameraMain = match.cameras[view]; gps[view].viewportRect = match.cameras[view].rect;
                var route = match.NavigationRoute(slot);
                if (owners[view] != slot || searches[view] != route.Searches)
                {
                    owners[view] = slot; searches[view] = route.Searches;
                    if (route.points.Count > 1) gps[view].SetRoute(route.points); else gps[view].ClearRoute();
                }
                if (view < markers.Length && markers[view] != null)
                {
                    markers[view].transform.position = match.NavigationGoal(slot);
                    markers[view].title = match.NavigationCaption(slot);
                    markers[view].tintColor = match.setup.players[slot].color;
                }
            }
        }
        public void ResetRoutes()
        {
            for (int i = 0; i < gps.Length; i++) { owners[i] = searches[i] = -1; gps[i].ClearRoute(); }
        }
        void OnDisable()
        {
            foreach (var item in gps) if (item != null) { item.ClearRoute(); item.gameObject.SetActive(false); }
            foreach (var marker in markers) if (marker != null) marker.gameObject.SetActive(false);
        }
    }
}

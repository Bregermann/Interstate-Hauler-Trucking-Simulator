using UnityEngine;
namespace LWS.TruckSports
{
    // Read-only transition for retired competitors, over the existing owned
    // Cinemachine cameras and free-look adapter. No truck or result is changed.
    [DefaultExecutionOrder(450)]
    public sealed class SportsExpansionSpectators : MonoBehaviour
    {
        public SportsEventSession match;
        public SportsDrivingViews views;
        void LateUpdate()
        {
            if (!match.Running || match.ReplayControls || match.menus.Screen != SportsEventScreen.Play || views.NeedsArena) return;
            for (int view = 0; view < views.rally.Humans; view++)
            {
                int owner = views.rally.owners[view];
                if (!match.ParticipantLocked(owner)) continue;
                int target = owner; float closest = float.MaxValue;
                for (int i = 0; i < match.setup.count; i++) if (!match.ParticipantLocked(i))
                {
                    float distance = (views.rally.trucks[i].body.position - views.rally.trucks[owner].body.position).sqrMagnitude;
                    if (distance < closest) { closest = distance; target = i; }
                }
                views.cabs[view].gameObject.SetActive(false);
                var camera = views.rally.views[view];
                var focus = views.rally.trucks[target].body.position + Vector3.up * 3.5f;
                camera.Pose(focus, new Vector3(13, 12, -20), 64);
                camera.view.transform.rotation = views.looks[view].Apply(camera.view.transform.rotation, match.setup.players[owner], owner, view == 0, match.Live, SportsLookOptions.Sensitivity, SportsLookOptions.InvertY);
            }
        }
    }
}

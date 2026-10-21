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
        readonly int[] targets={-1,-1,-1,-1};
        public int WatchedSlot(int view)=>view>=0&&view<targets.Length?targets[view]:-1;
        bool LiveTarget(int slot)=>slot>=0&&slot<match.setup.count&&slot<views.rally.trucks.Length&&views.rally.trucks[slot]!=null&&views.rally.trucks[slot].gameObject.activeInHierarchy&&match.BroadcastParticipantActive(slot);
        public bool OwnsView(int view)
        {
            if(match==null||views==null||!match.Running||match.Finished||match.ReplayControls||match.menus.Screen!=SportsEventScreen.Play||views.NeedsArena||view<0||view>=views.rally.Humans)return false;
            return !LiveTarget(views.rally.owners[view]);
        }
        void LateUpdate()
        {
            if (!match.Running || match.ReplayControls || match.menus.Screen != SportsEventScreen.Play || views.NeedsArena) return;
            for (int view = 0; view < views.rally.Humans; view++)
            {
                int owner = views.rally.owners[view];
                if (!OwnsView(view)){targets[view]=-1;continue;}
                int target=targets[view];float closest=float.MaxValue;
                if(!LiveTarget(target))for (int i = 0; i < match.setup.count; i++) if (LiveTarget(i))
                {
                    // Use the last camera position, not a destroyed body falling
                    // through space. Retain a valid selected subject thereafter.
                    float distance = (views.rally.trucks[i].body.position - views.rally.views[view].transform.position).sqrMagnitude;
                    if (distance < closest) { closest = distance; target = i; }
                }
                views.cabs[view].gameObject.SetActive(false);
                if(!LiveTarget(target))continue;
                targets[view]=target;
                var camera = views.rally.views[view];
                var focus = views.rally.trucks[target].body.position + Vector3.up * 3.5f;
                camera.Pose(focus, new Vector3(13, 12, -20), 64);
                camera.view.transform.rotation = views.looks[view].Apply(camera.view.transform.rotation, match.setup.players[owner], owner, view == 0, match.Live, SportsLookOptions.Sensitivity, SportsLookOptions.InvertY);
            }
        }
    }
}

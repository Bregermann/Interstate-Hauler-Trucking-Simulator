using LWS.TruckBowling;
using UnityEngine;

namespace LWS.TruckSports
{
    // Lives on the existing cab. Its camera, input, jug and steering stay owned
    // by their established adapters; only a dashboard figure is added here.
    [DefaultExecutionOrder(350)]
    public sealed class SportsTruckAppearanceCabBinding : MonoBehaviour
    {
        public SportsDrivingViews drivingViews;
        public BowlingLane bowlingLane;
        public int viewIndex;
        public Transform accessoryMount;
        // Explicitly assigned by a sport-owned cab layout when it does not use
        // SportsDrivingViews/BowlingLane. No input or camera ownership changes.
        public BowlingCabCamera visibility;
        SportsTruckAppearanceHost assignedOwner;
        SportsTruckAppearanceHost owner;
        int revision = -1;
        GameObject graphic;
        public void SetOwner(SportsTruckAppearanceHost next)
        {
            assignedOwner = next;
            Refresh();
        }
        void LateUpdate()
        {
            Refresh();
        }
        void Refresh()
        {
            SportsTruckAppearanceHost next = assignedOwner;
            if (bowlingLane != null) next = bowlingLane.truck.appearanceHost;
            else if (drivingViews != null && viewIndex >= 0 && viewIndex < drivingViews.rally.Humans)
            {
                int slot = drivingViews.rally.owners[viewIndex];
                if (slot >= 0 && slot < drivingViews.rally.trucks.Length) next = drivingViews.rally.trucks[slot].appearanceHost;
            }
            if (next == owner && (next == null || revision == next.AppearanceRevision)) return;
            Clear(); owner = next; revision = next != null ? next.AppearanceRevision : -1;
            if (owner != null) graphic = owner.CreateDashboard(accessoryMount);
            if (graphic != null && bowlingLane != null)
            {
                // Bowling's existing per-camera interior visibility authority.
                bowlingLane.cab.interior = bowlingLane.cab.GetComponent<SportsLegacyCabLook>().cockpit.GetComponentsInChildren<Renderer>(true);
            }
            if (visibility != null) visibility.interior = GetComponentsInChildren<Renderer>(true);
        }
        void Clear() { if (graphic != null) { graphic.SetActive(false); Destroy(graphic); } graphic = null; owner = null; revision = -1; }
        void OnDisable() => Clear();
        void OnDestroy() => Clear();
    }
}

using System.Collections;
using UnityEngine;

namespace LWS.TruckTaxi
{
    // Opt-in Play Mode observation; this does not force weather or synthesize plow motion.
    public sealed class TruckTaxiSnowRuntimeProbe : MonoBehaviour
    {
        public string LastResult { get; private set; } = "Not run";

        [ContextMenu("Observe Taxi Snow And Plow")]
        public void Observe()
        {
            if (!Application.isPlaying) { LastResult = "Enter Play Mode first."; Debug.LogWarning(LastResult, this); return; }
            StartCoroutine(ObserveMovement());
        }

        private IEnumerator ObserveMovement()
        {
            var environment = GetComponent<TruckTaxiEnvironmentCoordinator>();
            var snow = environment?.Snow;
            if (snow?.Region == null || snow.Plow == null)
            {
                LastResult = "Taxi snow was not initialized.";
                Debug.LogError(LastResult, this);
                yield break;
            }
            string id = snow.Plow.VehicleId;
            GameObject vehicle = null;
            bool spawned = !string.IsNullOrEmpty(id) && TruckTaxiBootstrap.Instance?.traffic != null &&
                TruckTaxiBootstrap.Instance.traffic.TryResolveVehicle(id, out vehicle);
            Vector3 before = spawned ? vehicle.transform.position : Vector3.zero;
            int cleared = snow.Plow.ClearedCellCount;
            yield return new WaitForSeconds(3);
            GameObject afterVehicle = null;
            bool stillPresent = spawned && TruckTaxiBootstrap.Instance.traffic.TryResolveVehicle(id, out afterVehicle);
            float moved = stillPresent ? Vector3.Distance(before, afterVehicle.transform.position) : 0;
            var surface = snow.GetComponent<TruckTaxiSnowSurface>();
            bool geometry = surface != null && surface.RenderedCells > 0;
            LastResult = $"Weather={environment.CurrentTaxiWeatherId}, cells={snow.VisibleCellCount}, visibleGeometry={geometry}, playerDepth={snow.PlayerSnowDepth:0.000}m, plowId={id ?? "none"}, UTSmoved={moved:0.0}m, newlyCleared={snow.Plow.ClearedCellCount - cleared}.";
            Debug.Log("TAXI SNOW PROBE: " + LastResult, this);
        }
    }
}

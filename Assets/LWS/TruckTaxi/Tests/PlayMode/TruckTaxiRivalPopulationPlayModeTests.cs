using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace LWS.TruckTaxi.Tests
{
    [Category("TaxiMegaAutomatedIntegration")]
    public sealed class TruckTaxiRivalPopulationPlayModeTests
    {
        [UnityTest]
        public IEnumerator DedicatedUtsVehicleMaterializesWithinRivalBudget()
        {
            yield return SceneManager.LoadSceneAsync("TruckTaxi_DemoCity");
            float deadline = Time.realtimeSinceStartup + 60;
            while ((TruckTaxiBootstrap.Instance == null || !TruckTaxiBootstrap.Instance.Ready) &&
                   Time.realtimeSinceStartup < deadline) yield return null;
            var host = TruckTaxiBootstrap.Instance;
            Assert.IsNotNull(host);
            Assert.IsTrue(host.Ready);
            var rivals = host.Rivals;
            Assert.IsNotNull(rivals);
            Assert.AreEqual(12, rivals.LogicalCount);
            string id = null;
            foreach (var active in host.traffic.ActiveVehicles)
                if (active.Key.StartsWith("taxi.rival.", System.StringComparison.Ordinal))
                { id = active.Key; break; }
            foreach (var lane in host.traffic.cityLanes)
            {
                if (id != null) break;
                if (lane == null || lane.centerline == null || !lane.spawnEnabled) continue;
                for (int point = 2; point < lane.centerline.Length - 2 && id == null; point++)
                    id = rivals.DebugSpawn(lane.centerline[point]);
                if (id != null) break;
            }
            Assert.IsNotNull(id, "At least one authored road point must have UTS spawn clearance.");
            Assert.IsTrue(host.traffic.TryResolveVehicle(id, out var vehicle));
            Assert.IsNotNull(vehicle.GetComponent<TruckTaxiTrafficPooledActor>());
            Assert.IsNotNull(vehicle.GetComponent<TruckTaxiTrafficBehaviour>());
            Assert.IsNotNull(vehicle.GetComponent<TruckTaxiRivalVehicleVisual>());
            Assert.LessOrEqual(rivals.LiveCount, rivals.maximumLive);
            var playerState = host.Session.State;
            int history = host.Session.RideHistory.Count;
            Assert.IsTrue(rivals.ForcePickup(id));
            yield return new WaitForSecondsRealtime(.5f);
            Assert.AreEqual(playerState, host.Session.State);
            Assert.AreEqual(history, host.Session.RideHistory.Count);
        }
    }
}

using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace LWS.TruckTaxi.Tests
{
    public sealed class TruckTaxiPopulationSteeringPlayModeTests
    {
        [UnityTest]
        public IEnumerator DenseCityReusesNativePopulationAndVisualSteeringAuthorities()
        {
            yield return SceneManager.LoadSceneAsync("TruckTaxi_DemoCity");
            float deadline = Time.realtimeSinceStartup + 60;
            while ((TruckTaxiBootstrap.Instance == null || !TruckTaxiBootstrap.Instance.Ready) && Time.realtimeSinceStartup < deadline) yield return null;
            var host = TruckTaxiBootstrap.Instance;
            Assert.IsNotNull(host); Assert.IsTrue(host.Ready);
            Assert.AreEqual(12, host.pedestrians.BaselineActiveCount, "Measured native UTS baseline, not the old maximumPeople value.");
            Assert.AreEqual(12, host.traffic.BaselineActiveCount);
            Assert.AreEqual(144, host.pedestrians.TargetCount);
            Assert.AreEqual(60, host.traffic.TargetCount);
            int paths = host.pedestrians.peoplePaths.Length;
            host.pedestrians.Initialize(); host.traffic.Initialize();
            Assert.AreEqual(paths, host.pedestrians.peoplePaths.Length);
            var visual = host.Player.GetComponent<TruckTaxiSteeringWheelVisual>();
            Assert.IsNotNull(visual, "Main integration must attach the reusable presenter to the runtime player.");
            Assert.IsTrue(visual.Applied);
            Assert.IsFalse(host.Player.DashboardController.SteeringWheelAnimationEnabled);
            Assert.IsNull(host.Player.NwhAdapter.VehicleController.steering.steeringWheel);
            host.SetPaused(false);
            deadline = Time.realtimeSinceStartup + 30;
            while ((host.pedestrians.ActiveCount < 144 || host.traffic.ActiveCount < 60) && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.AreEqual(144, host.pedestrians.ActiveCount);
            Assert.AreEqual(60, host.traffic.ActiveCount);
            Assert.LessOrEqual(host.pedestrians.RagdollCount, host.pedestrians.densityProfile.maximumActiveRagdolls);
            Time.timeScale = 1;
        }
    }
}

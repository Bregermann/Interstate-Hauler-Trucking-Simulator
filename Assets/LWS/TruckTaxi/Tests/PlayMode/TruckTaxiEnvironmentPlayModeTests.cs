using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace LWS.TruckTaxi.Tests
{
    public sealed class TruckTaxiEnvironmentPlayModeTests
    {
        [UnityTest, Timeout(180000)]
        public IEnumerator SceneReloadAndAdapterReenableReuseOneWeatherRuntime()
        {
            Transform previousRoot = null;
            try
            {
                for (int pass = 0; pass < 2; pass++)
                {
                    yield return SceneManager.LoadSceneAsync("TruckTaxi_DemoCity");
                    float deadline = Time.realtimeSinceStartup + 45;
                    while ((TruckTaxiBootstrap.Instance == null || !TruckTaxiBootstrap.Instance.Ready) &&
                           Time.realtimeSinceStartup < deadline) yield return null;
                    var host = TruckTaxiBootstrap.Instance;
                    Assert.IsNotNull(host); Assert.IsTrue(host.Ready);
                    Assert.IsTrue(host.Environment.IsInitialized);
                    Assert.IsTrue(RenderSettings.skybox == null || RenderSettings.skybox.shader == null ||
                        RenderSettings.skybox.shader.name != "Skybox/Procedural",
                        "Unity's default procedural sky must not mask Weather Maker's day/night sky after Taxi is ready.");
                    if (pass > 0) Assert.IsTrue(previousRoot == null, "Taxi weather from the previous scene must not survive reload.");
                    var adapter = host.Environment.weatherAdapter;
                    var root = adapter.WeatherMakerRuntimeRoot;
                    Assert.IsNotNull(root);
                    Assert.AreEqual(host.gameObject.scene, root.gameObject.scene);
                    Assert.AreEqual(1, CountWeatherRoots(), "Scene activation must not create a duplicate during quality initialization.");
                    adapter.enabled = false; adapter.enabled = true; yield return null;
                    Assert.AreSame(root, adapter.WeatherMakerRuntimeRoot);
                    Assert.AreEqual(1, CountWeatherRoots(), "Re-enabling the adapter must reuse its runtime.");
                    previousRoot = root;
                }
            }
            finally
            {
                var host = TruckTaxiBootstrap.Instance;
                if (host != null) host.SetPaused(false);
                Time.timeScale = 1;
            }
        }

        private static int CountWeatherRoots() => Object.FindObjectsByType<MonoBehaviour>(
            FindObjectsInactive.Include, FindObjectsSortMode.None).Count(component => component != null &&
            component.GetType().FullName == "DigitalRuby.WeatherMaker.WeatherMakerScript");
    }
}

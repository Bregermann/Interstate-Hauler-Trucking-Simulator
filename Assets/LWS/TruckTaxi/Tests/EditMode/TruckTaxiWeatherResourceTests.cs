using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using LWS.InterstateHauler;
using LWS.TruckTaxi.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LWS.TruckTaxi.Tests
{
    public sealed class TruckTaxiWeatherResourceTests
    {
        [Test]
        public void TaxiWeatherPrefabExcludesUnusedWaterShaderDependencies()
        {
            Assert.IsNotNull(AssetDatabase.LoadMainAssetAtPath(TruckTaxiEnvironmentSetup.WeatherResourcesPath),
                "Run TruckTaxiEnvironmentSetup.ConfigureWeatherResources before validating authored weather assets.");
            var dependencies = AssetDatabase.GetDependencies(TruckTaxiEnvironmentSetup.WeatherPrefabPath, true);
            Assert.That(dependencies, Does.Contain(TruckTaxiEnvironmentSetup.WeatherResourcesPath));
            Assert.IsFalse(dependencies.Any(path => path.EndsWith(".shader", StringComparison.OrdinalIgnoreCase) &&
                path.IndexOf("Water", StringComparison.OrdinalIgnoreCase) >= 0), "Taxi has no water surface and must not bundle water shaders.");
            Assert.IsTrue(dependencies.Any(path => path.EndsWith("WeatherMakerProfile_LightRain.asset", StringComparison.Ordinal)),
                "Excluding water must preserve weather precipitation profiles.");
        }
    }

    public sealed class TruckTaxiWeatherAdapterCacheTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private GameObject host;
        private GameObject weatherRoot;
        private LwsWeatherMakerAdapter adapter;

        [SetUp] public void SetUp()
        {
            // Inactive host avoids invoking vendor initialization from an EditMode fixture.
            host = new GameObject("Weather adapter cache test"); host.SetActive(false);
            adapter = host.AddComponent<LwsWeatherMakerAdapter>();
            weatherRoot = Child("Known weather runtime");
            Set("_weatherMakerInstance", weatherRoot.transform);
        }

        [TearDown] public void TearDown() => Object.DestroyImmediate(host);

        [Test] public void KnownRootWinsAndRepeatedManagerResolutionReusesCache()
        {
            Child("Unrelated scene manager").AddComponent<BoxCollider>();
            var manager = weatherRoot.AddComponent<BoxCollider>();
            for (int i = 0; i < 20; i++) Assert.AreSame(manager, Resolve());
            Assert.AreEqual(1, Cache.Count);
            Assert.AreSame(weatherRoot.transform, Get("_managerCacheRoot"));
        }

        [Test] public void RootReplacementAndDestroyedManagerInvalidateReferences()
        {
            var original = weatherRoot.AddComponent<BoxCollider>();
            Assert.AreSame(original, Resolve());
            var replacementRoot = Child("Replacement weather runtime");
            var replacement = replacementRoot.AddComponent<BoxCollider>();
            Set("_weatherMakerInstance", replacementRoot.transform);
            Assert.AreSame(replacement, Resolve());
            Object.DestroyImmediate(replacement);
            replacement = replacementRoot.AddComponent<BoxCollider>();
            Assert.AreSame(replacement, Resolve(), "Destroyed Unity references must be resolved again immediately.");
            Assert.AreEqual(1, Cache.Count);
        }

        [Test] public void DisableClearsManagerCacheAndPendingDiagnosticInterval()
        {
            weatherRoot.AddComponent<BoxCollider>(); Resolve();
            Set("_nextVisualDiagnosticsTime", Time.unscaledTime + 100);
            Invoke("OnDisable");
            Assert.AreEqual(0, Cache.Count);
            Assert.IsNull(Get("_managerCacheRoot"));
            Assert.AreEqual(0f, Get("_nextVisualDiagnosticsTime"));
        }

        [Test] public void VisualDiagnosticsAreBoundedButExplicitWeatherChangesCanRefreshImmediately()
        {
            Assert.AreEqual(1f, Get("diagnosticsRefreshIntervalSeconds"));
            Invoke("RefreshWeatherMakerVisualDiagnostics", true);
            float next = (float)Get("_nextVisualDiagnosticsTime");
            Assert.Greater(next, Time.unscaledTime);
            Set("<FogDiagnostic>k__BackingField", "unchanged between samples");
            for (int i = 0; i < 20; i++) Invoke("RefreshWeatherMakerVisualDiagnostics", false);
            Assert.AreEqual("unchanged between samples", adapter.FogDiagnostic);
            Assert.AreEqual(next, Get("_nextVisualDiagnosticsTime"));
            Invoke("RefreshWeatherMakerVisualDiagnostics", true);
            Assert.AreEqual("Weather Maker unavailable.", adapter.FogDiagnostic);
        }

        private GameObject Child(string name)
        {
            var child = new GameObject(name); child.transform.SetParent(host.transform); return child;
        }
        private IDictionary Cache => (IDictionary)Get("_managerCache");
        private object Resolve() => Invoke("FindRuntimeManager", typeof(BoxCollider));
        private object Get(string field) => typeof(LwsWeatherMakerAdapter).GetField(field, Private).GetValue(adapter);
        private void Set(string field, object value) => typeof(LwsWeatherMakerAdapter).GetField(field, Private).SetValue(adapter, value);
        private object Invoke(string method, params object[] arguments) =>
            typeof(LwsWeatherMakerAdapter).GetMethod(method, Private).Invoke(adapter, arguments);
    }
}

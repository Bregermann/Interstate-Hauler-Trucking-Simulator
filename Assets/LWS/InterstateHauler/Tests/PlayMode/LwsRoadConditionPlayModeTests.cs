using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace LWS.InterstateHauler.Tests.PlayMode
{
    public sealed class LwsRoadConditionPlayModeTests
    {
        [UnityTest]
        public IEnumerator BootstrapRegistersRoadConditionService()
        {
            LwsApplicationBootstrap.ResetForTests();
            var bootstrapObject = new GameObject("road-condition-bootstrap-test");
            bootstrapObject.AddComponent<LwsApplicationBootstrap>();
            yield return null;

            LwsApplicationBootstrap bootstrap = LwsApplicationBootstrap.Instance;
            Assert.IsNotNull(bootstrap);
            Assert.IsTrue(bootstrap.Registry.TryGet(out ILwsRoadConditionService roadConditions));
            Assert.AreEqual(LwsRoadConditionType.Dry, roadConditions.CurrentSnapshot.condition);

            Object.Destroy(bootstrapObject);
            LwsApplicationBootstrap.ResetForTests();
        }

        [UnityTest]
        public IEnumerator ForcedRoadConditionReachesAdaptersAndResetRestoresDry()
        {
            LwsServiceRegistry registry = LwsApplicationBootstrap.CreateDefaultRegistry();
            Assert.IsTrue(registry.InitializeAll().Succeeded);
            Assert.IsTrue(registry.TryGet(out ILwsRoadConditionService roadConditions));

            var physics = new FakePhysicsAdapter();
            var visual = new FakeVisualAdapter();
            Assert.IsTrue(roadConditions.AttachPhysicsAdapter(physics));
            Assert.IsTrue(roadConditions.AttachVisualAdapter(visual));

            roadConditions.ForceCondition(LwsRoadConditionOverrideMode.ForceIce);
            yield return null;

            Assert.AreEqual(LwsRoadConditionType.Ice, physics.LastSnapshot.condition);
            Assert.AreEqual(LwsRoadConditionType.Ice, visual.LastSnapshot.condition);

            roadConditions.ResetCurrentRoad();
            yield return null;

            Assert.GreaterOrEqual(physics.RestoreCount, 1);
            Assert.AreEqual(LwsRoadConditionType.Dry, physics.LastSnapshot.condition);
            registry.ShutdownAll();
        }

        [UnityTest]
        public IEnumerator WeatherServiceRainDrivesWetnessUp()
        {
            LwsServiceRegistry registry = LwsApplicationBootstrap.CreateDefaultRegistry();
            Assert.IsTrue(registry.InitializeAll().Succeeded);
            Assert.IsTrue(registry.TryGet(out ILwsWeatherService weather));
            Assert.IsTrue(registry.TryGet(out ILwsRoadConditionService roadConditions));

            roadConditions.UpdatePlayerRoad(Vector3.zero, Vector3.forward);
            Assert.IsTrue(weather.RequestWeather(LwsWeatherPresetCatalog.HeavyRainId, 0f, true).Succeeded);
            roadConditions.Tick(120f);
            yield return null;

            Assert.Greater(roadConditions.CurrentSnapshot.wetness01, 0.05f);
            Assert.AreNotEqual(LwsRoadConditionType.Ice, roadConditions.CurrentSnapshot.condition);
            registry.ShutdownAll();
        }

        [UnityTest]
        public IEnumerator DefaultRegistryHasSingleRoadConditionService()
        {
            LwsServiceRegistry registry = LwsApplicationBootstrap.CreateDefaultRegistry();
            int count = registry.Registrations.Count(r => r.ServiceType == typeof(ILwsRoadConditionService));

            Assert.AreEqual(1, count);
            yield return null;
        }


        [UnityTest]
        public IEnumerator WeatheradeAdapterBindsRoadRendererToRainAndSnowMaterials()
        {
            var roadObject = GameObject.CreatePrimitive(PrimitiveType.Plane);
            roadObject.name = "weatherade-road-surface-test";
            Renderer renderer = roadObject.GetComponent<Renderer>();
            renderer.sharedMaterial = LwsWeatheradeMaterialFactory.CreateFallbackLitMaterial("IH Weatherade Test Dry Material", new Color(0.07f, 0.07f, 0.065f, 1f));
            roadObject.AddComponent<LwsRoadSurface>().Configure("road.weatherade.test", "segment.weatherade.test", LwsRoadSurfaceType.AsphaltInterstate, "Weatherade adapter test asphalt");

            var adapterObject = new GameObject("weatherade-adapter-test");
            LwsWeatheradeAdapter adapter = adapterObject.AddComponent<LwsWeatheradeAdapter>();
            yield return null;

            Assert.IsTrue(adapter.IsAvailable, adapter.Status);

            LwsRoadConditionSnapshot wet = LwsRoadConditionSnapshot.CreateDry("road.weatherade.test", "segment.weatherade.test", "edge.weatherade.test", LwsRoadSurfaceType.AsphaltInterstate, 10f, LwsRoadConditionProfile.Default());
            wet.condition = LwsRoadConditionType.Wet;
            wet.wetness01 = 0.9f;
            wet.standingWater01 = 0.5f;
            adapter.ApplyRoadCondition(wet);
            yield return null;

            Assert.GreaterOrEqual(adapter.BoundWeatheradeSurfaceCount, 1, adapter.RoadMaterialDiagnostic);
            Assert.IsTrue(LwsWeatheradeMaterialFactory.IsWeatheradeMaterialForMode(renderer.sharedMaterial, LwsWeatheradeSurfaceMaterialMode.Rain), renderer.sharedMaterial != null && renderer.sharedMaterial.shader != null ? renderer.sharedMaterial.shader.name : "missing material");

            LwsRoadConditionSnapshot snow = wet;
            snow.condition = LwsRoadConditionType.LightSnow;
            snow.wetness01 = 0f;
            snow.standingWater01 = 0f;
            snow.snowDepth01 = 0.8f;
            adapter.ApplyRoadCondition(snow);
            yield return null;

            Assert.IsTrue(LwsWeatheradeMaterialFactory.IsWeatheradeMaterialForMode(renderer.sharedMaterial, LwsWeatheradeSurfaceMaterialMode.Snow), renderer.sharedMaterial != null && renderer.sharedMaterial.shader != null ? renderer.sharedMaterial.shader.name : "missing material");
            Assert.IsTrue(adapter.RoadMaterialCompatible, adapter.RoadMaterialDiagnostic);

            Object.Destroy(adapterObject);
            Object.Destroy(roadObject);
            yield return null;
        }
        private sealed class FakePhysicsAdapter : ILwsRoadConditionPhysicsAdapter
        {
            public string AdapterId => "fake.physics";
            public bool IsAvailable => true;
            public string Status => "Ready";
            public int BoundWheelCount => 4;
            public int RestoreCount { get; private set; }
            public LwsRoadConditionSnapshot LastSnapshot { get; private set; }

            public void ApplyRoadCondition(LwsRoadConditionSnapshot snapshot)
            {
                LastSnapshot = snapshot;
            }

            public void RestoreDryBaseline()
            {
                RestoreCount++;
            }
        }

        private sealed class FakeVisualAdapter : ILwsRoadConditionVisualAdapter
        {
            public string AdapterId => "fake.visual";
            public bool IsAvailable => true;
            public string Status => "Ready";
            public LwsRoadConditionSnapshot LastSnapshot { get; private set; }

            public void ApplyRoadCondition(LwsRoadConditionSnapshot snapshot)
            {
                LastSnapshot = snapshot;
            }

            public void ApplyQualityTier(LwsRenderQualityTier tier)
            {
            }
        }
    }
}

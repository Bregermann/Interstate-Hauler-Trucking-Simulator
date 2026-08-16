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

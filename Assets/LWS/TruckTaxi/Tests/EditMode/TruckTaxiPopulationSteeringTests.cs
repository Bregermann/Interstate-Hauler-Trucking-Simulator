using System.Collections.Generic;
using LWS.InterstateHauler;
using NWH.VehiclePhysics2;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace LWS.TruckTaxi.Tests
{
    public sealed class TruckTaxiPopulationSteeringTests
    {
        [Test]
        public void MeasuredBaselineRatherThanOldCapDeterminesRequestedDensity()
        {
            var profile = ScriptableObject.CreateInstance<TruckTaxiPopulationProfile>();
            try
            {
                Assert.AreEqual(144, profile.PedestrianTarget(12));
                Assert.AreEqual(60, profile.TrafficTarget(12));
                profile.measuredPedestrianBaseline = 12;
                Assert.AreEqual(144, profile.PedestrianTarget(24));
                profile.maximumActivePedestrians = 100;
                Assert.AreEqual(100, profile.PedestrianTarget(12));
                Assert.AreEqual(12, TruckTaxiPopulationProfile.ScaleTarget(12, 1, 288));
            }
            finally { Object.DestroyImmediate(profile); }
        }

        [TestCase(0, 12, 288, 0)]
        [TestCase(12, 0, 288, 0)]
        [TestCase(12, 12, 0, 0)]
        [TestCase(12, -1, 288, 0)]
        [TestCase(12, 12, 288, 144)]
        public void DensityCapsAreExplicit(int baseline, float scale, int cap, int expected)
            => Assert.AreEqual(expected, TruckTaxiPopulationProfile.ScaleTarget(baseline, scale, cap));

        [Test]
        public void CityRadiusUsesHorizontalDistanceAndDespawnHysteresis()
        {
            var profile = ScriptableObject.CreateInstance<TruckTaxiPopulationProfile>();
            try
            {
                Assert.IsTrue(profile.AllowsSpawn(new Vector3(599, 1000, 0), Vector3.zero));
                Assert.IsFalse(profile.AllowsSpawn(new Vector3(601, 0, 0), Vector3.zero));
                Assert.IsTrue(profile.AllowsPresence(new Vector3(650, 0, 0), Vector3.zero));
                Assert.IsFalse(profile.AllowsPresence(new Vector3(701, 0, 0), Vector3.zero));
                profile.spawnRadius = profile.despawnRadius = 0;
                Assert.IsTrue(profile.AllowsSpawn(Vector3.one * 10000, Vector3.zero));
            }
            finally { Object.DestroyImmediate(profile); }
        }

        [Test]
        public void BaselineConfigurationPreservesAuthoredCapsAndDoesNotMutateSharedProfile()
        {
            var profile = ScriptableObject.CreateInstance<TruckTaxiPopulationProfile>();
            var root = new GameObject("Population configuration fixture"); root.SetActive(false);
            try
            {
                profile.measuredPedestrianBaseline = profile.measuredTrafficBaseline = 12;
                var pedestrians = root.AddComponent<TruckTaxiPedestrianPopulation>();
                var traffic = root.AddComponent<TruckTaxiTrafficAdapter>();
                Assert.IsTrue(pedestrians.ConfigureBaselineForValidation(profile));
                Assert.IsTrue(traffic.ConfigureBaselineForValidation(profile));
                Assert.AreEqual(12, pedestrians.TargetCount); Assert.AreEqual(12, traffic.TargetCount);
                Assert.AreEqual(24, pedestrians.maximumPeople); Assert.AreEqual(12, traffic.maximumVehicles);
                Assert.AreEqual(12, profile.pedestrianDensityMultiplier); Assert.AreEqual(5, profile.trafficDensityMultiplier);
                Assert.IsTrue(pedestrians.ConfigureDensity(profile)); Assert.IsTrue(traffic.ConfigureDensity(profile));
                Assert.AreEqual(144, pedestrians.TargetCount); Assert.AreEqual(60, traffic.TargetCount);
                Assert.IsFalse(pedestrians.IsBaselineValidation); Assert.IsFalse(traffic.IsBaselineValidation);
                Assert.IsTrue(pedestrians.ConfigureDensity(profile, false));
                Assert.AreEqual(24, pedestrians.TargetCount, "Original manual spawn cap remains available with the override disabled.");
            }
            finally { Object.DestroyImmediate(root); Object.DestroyImmediate(profile); }
        }

        [TestCase(27)]
        [TestCase(50)]
        [TestCase(73)]
        [TestCase(100)]
        public void DenseTrafficVisitsEveryEligiblePointEvenWhenLengthSharesFactor23(int pointCount)
        {
            var visited = new HashSet<int>();
            const int lanes = 4;
            for (int cycle = 0; cycle < pointCount - 4; cycle++)
            {
                int point = TruckTaxiTrafficAdapter.DensePointIndex(cycle * lanes + 2, lanes, pointCount);
                Assert.That(point, Is.InRange(2, pointCount - 3));
                Assert.IsTrue(visited.Add(point));
            }
            Assert.AreEqual(pointCount - 4, visited.Count);
        }

        [Test]
        public void TargetUsesResolvedRoadAngleNotRawDigitalInput()
        {
            Assert.AreEqual(0, TruckTaxiSteeringWheelVisual.ResolveTargetAngle(0, 65, 900));
            Assert.AreEqual(225, TruckTaxiSteeringWheelVisual.ResolveTargetAngle(32.5f, 65, 900));
            Assert.AreEqual(-450, TruckTaxiSteeringWheelVisual.ResolveTargetAngle(-65, 65, 900));
            Assert.AreEqual(450, TruckTaxiSteeringWheelVisual.ResolveTargetAngle(100, 65, 900));
            Assert.AreEqual(0, TruckTaxiSteeringWheelVisual.ResolveTargetAngle(20, 0, 900));
            Assert.AreEqual(0, TruckTaxiSteeringWheelVisual.ResolveTargetAngle(float.NaN, 65, 900));
        }

        [TestCase(30)]
        [TestCase(60)]
        [TestCase(120)]
        public void ContinuousAngleCrosses360WithoutWrappingAndReturnsGradually(int fps)
        {
            var motion = new TruckTaxiSteeringVisualMotion(-450);
            float previous = motion.Angle;
            for (int i = 0; i < fps * 3; i++)
            {
                motion.Step(450, 1f / fps, .16f, 1, 900);
                Assert.That(motion.Angle, Is.GreaterThanOrEqualTo(previous - .001f));
                Assert.That(Mathf.Abs(motion.Angle - previous), Is.LessThanOrEqualTo(900f / fps + .002f));
                Assert.That(Mathf.Abs(motion.Velocity), Is.LessThanOrEqualTo(900.001f));
                previous = motion.Angle;
            }
            Assert.AreEqual(450, motion.Angle, .01f);
            motion.Step(0, 1f / fps, .23f, 1, 900);
            Assert.That(motion.Angle, Is.GreaterThan(400));
            for (int i = 0; i < fps * 3; i++) motion.Step(0, 1f / fps, .23f, 1, 900);
            Assert.AreEqual(0, motion.Angle, .01f);
        }

        [TestCase(1f)]
        [TestCase(1.5f)]
        [TestCase(2f)]
        public void ThirtySixtyAndOneTwentyFpsAgreeAcrossReversals(float damping)
        {
            float[] reference = Simulate(120, damping);
            foreach (int fps in new[] { 30, 60 })
            {
                float[] actual = Simulate(fps, damping);
                for (int i = 0; i < actual.Length; i++) Assert.AreEqual(reference[i], actual[i], .2f, "FPS=" + fps + " sample=" + i);
            }
        }
        private static float[] Simulate(int fps, float damping)
        {
            var motion = new TruckTaxiSteeringVisualMotion(0);
            var samples = new List<float>();
            foreach (float target in new[] { -450f, 450f, -225f, 225f, 0f })
                for (int segment = 0; segment < 4; segment++)
                {
                    for (int i = 0; i < fps / 10; i++) motion.Step(target, 1f / fps, target == 0 ? .23f : .16f, damping, 900);
                    samples.Add(motion.Angle);
                }
            return samples.ToArray();
        }

        [Test]
        public void PausingDoesNotResetMotionAndNonFiniteInputsDoNotPoisonRotation()
        {
            var motion = new TruckTaxiSteeringVisualMotion(180);
            motion.Step(-450, 0, .16f, 1, 900);
            Assert.AreEqual(180, motion.Angle);
            motion.Step(float.NaN, .1f, .16f, 1, 900);
            motion.Step(0, float.NaN, .16f, 1, 900);
            Assert.AreEqual(180, motion.Angle);
        }

        [Test]
        public void PresenterLeasesAndRestoresBothVisualWritersWithoutChangingPhysics()
        {
            const string prefabPath = "Assets/LWS/InterstateHauler/Vehicles/Prefabs/IH_PlayerTruck_NWH.prefab";
            // NWH requires authored StateSettings even in OnValidate. Use the real configured prefab, not a bare controller.
            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            root.SetActive(false);
            var wheel = new GameObject("steering wheel").transform;
            wheel.SetParent(root.transform, false);
            wheel.localRotation = Quaternion.Euler(25, 0, 0);
            try
            {
                var vehicle = root.GetComponent<VehicleController>();
                var dashboard = root.GetComponent<LwsTruckDashboardController>();
                Assert.IsNotNull(vehicle, "The configured truck prefab must contain its NWH controller.");
                // LwsPlayerTruckSpawner adds this runtime presenter; it is not authored on the base prefab.
                if (dashboard == null) dashboard = root.AddComponent<LwsTruckDashboardController>();
                var binding = new SerializedObject(dashboard);
                binding.FindProperty("steeringWheelVisual").objectReferenceValue = wheel;
                binding.ApplyModifiedPropertiesWithoutUndo();
                vehicle.steering.steeringWheel = wheel;
                float originalLock = vehicle.steering.maximumSteerAngle;
                float originalRate = vehicle.steering.degreesPerSecondLimit;
                var originalCurve = vehicle.steering.speedSensitiveSteeringCurve;
                var originalSmoothing = vehicle.steering.speedSensitiveSmoothingCurve;
                var presenter = root.AddComponent<TruckTaxiSteeringWheelVisual>();
                Assert.IsTrue(presenter.Initialize(vehicle));
                Assert.IsTrue(presenter.Initialize(vehicle), "Repeated bootstrap initialization must be idempotent.");
                Assert.AreEqual(1, root.GetComponents<TruckTaxiSteeringWheelVisual>().Length);
                Assert.IsFalse(dashboard.SteeringWheelAnimationEnabled);
                Assert.IsNull(vehicle.steering.steeringWheel);
                Assert.AreEqual(originalLock, vehicle.steering.maximumSteerAngle);
                Assert.AreEqual(originalRate, vehicle.steering.degreesPerSecondLimit);
                Assert.AreSame(originalCurve, vehicle.steering.speedSensitiveSteeringCurve);
                Assert.AreSame(originalSmoothing, vehicle.steering.speedSensitiveSmoothingCurve);
                presenter.Restore();
                Assert.IsTrue(dashboard.SteeringWheelAnimationEnabled);
                Assert.AreSame(wheel, vehicle.steering.steeringWheel);
                Assert.IsFalse(presenter.Applied);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
    }
}

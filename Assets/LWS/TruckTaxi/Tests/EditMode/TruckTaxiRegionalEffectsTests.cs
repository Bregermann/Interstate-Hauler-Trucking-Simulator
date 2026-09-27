using NUnit.Framework;
using UnityEngine;

namespace LWS.TruckTaxi.Tests
{
    [Category("TaxiRegional")]
    public sealed class TruckTaxiRegionalEffectsTests
    {
        [Test]
        public void MushroomPeakStartsAtFullStrengthAndStillFadesOut()
        {
            var effects = new TruckTaxiTemporaryEffects();
            int started = 0;
            effects.Started += _ => started++;

            Assert.IsTrue(effects.DebugMushroomPeak());
            var peak = effects.GetSnapshot(TruckTaxiTemporaryEffectKind.MysteryMushroom);
            Assert.AreEqual(1f, peak.Intensity01);
            Assert.GreaterOrEqual(peak.Profile.Saturation, 100);
            Assert.Greater(peak.Profile.BloomIntensity, 1f);
            Assert.AreEqual(1, started);

            effects.Tick(12);
            Assert.AreEqual(1f, effects.GetSnapshot(TruckTaxiTemporaryEffectKind.MysteryMushroom).Intensity01);
            effects.Tick(2);
            Assert.AreEqual(.5f, effects.GetSnapshot(TruckTaxiTemporaryEffectKind.MysteryMushroom).Intensity01, .001f);
            effects.Tick(2);
            Assert.AreEqual(0f, effects.GetSnapshot(TruckTaxiTemporaryEffectKind.MysteryMushroom).Intensity01);
        }

        [Test]
        public void DebugThirstSetterClampsAndRejectsNonFiniteInput()
        {
            var settings = ScriptableObject.CreateInstance<TruckTaxiEnvironmentSettings>();
            try
            {
                var state = new TruckTaxiDriverNeedsState(settings, 11);
                state.DebugSetThirst(2f);
                Assert.AreEqual(1f, state.Thirst);
                state.DebugSetThirst(float.NaN);
                Assert.AreEqual(1f, state.Thirst);
                state.SatisfyThirst();
                Assert.AreEqual(0f, state.Thirst);
            }
            finally { Object.DestroyImmediate(settings); }
        }

        [Test]
        public void HydraulicRockingIsVisibleButLeavesTheRigidbodyAlone()
        {
            var truck = new GameObject("Truck");
            var controller = new GameObject("Motion");
            var bodyVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                var body = truck.AddComponent<Rigidbody>();
                bodyVisual.transform.SetParent(truck.transform, false);
                var motion = controller.AddComponent<TruckTaxiPrivateEventVehicleMotion>();
                Assert.IsTrue(motion.Initialize(truck.transform));
                Assert.IsTrue(motion.Begin());
                Vector3 physicalPosition = body.position;
                Quaternion physicalRotation = body.rotation;

                motion.Step(.2f);
                var proxy = truck.transform.GetChild(1);
                Assert.Greater(Mathf.Abs(proxy.localPosition.y), .3f);
                Assert.Greater(Mathf.Abs(proxy.localRotation.eulerAngles.z), 10f);
                Assert.AreEqual(physicalPosition, body.position);
                Assert.AreEqual(physicalRotation, body.rotation);

                motion.StopMotion();
                Assert.IsTrue(bodyVisual.GetComponent<Renderer>().enabled);
                Assert.IsTrue(bodyVisual.GetComponent<Collider>().enabled);
            }
            finally
            {
                Object.DestroyImmediate(controller);
                Object.DestroyImmediate(truck);
            }
        }
    }
}

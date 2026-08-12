using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace LWS.InterstateHauler.Tests.PlayMode
{
    public sealed class LwsTruckControlsPlayModeTests
    {
        [UnityTest]
        public IEnumerator ControllerInitializesWithoutNwhAndPublishesServiceState()
        {
            var registry = new LwsServiceRegistry();
            var service = new LwsTruckControlService();
            service.Initialize(new LwsServiceContext(registry));
            var go = new GameObject("truck-control-playmode");
            LwsTruckControlController controller = go.AddComponent<LwsTruckControlController>();

            LwsServiceResult result = service.RegisterActiveController(controller);
            service.PublishState(controller, controller.CurrentState);

            Assert.IsTrue(result.Succeeded, result.Message);
            Assert.AreEqual(controller, service.ActiveController);
            yield return null;
            Object.Destroy(go);
        }

        [UnityTest]
        public IEnumerator NwhProviderConsumesTruckControlPulsesAndState()
        {
            GameObject go = new GameObject("nwh-control-provider-test");
            LwsTruckControlController controller = go.AddComponent<LwsTruckControlController>();
            LwsNwhVehicleInputProvider provider = go.AddComponent<LwsNwhVehicleInputProvider>();
            provider.SetTruckControlController(controller);

            controller.ApplyCommandFrame(new LwsVehicleCommandFrame
            {
                lowBeamLights = LwsMomentaryIntent.Pressed,
                horn = LwsMomentaryIntent.Pressed,
                parkingBrakeToggle = LwsMomentaryIntent.Pressed
            }, default);

            Assert.IsTrue(provider.LowBeamLights());
            Assert.IsFalse(provider.LowBeamLights());
            Assert.IsTrue(provider.Horn());
            Assert.AreEqual(1f, provider.Handbrake());

            yield return null;
            Object.Destroy(go);
        }

        [UnityTest]
        public IEnumerator CruiseFallbackReachesNwhProviderAndCancelsOnBrake()
        {
            GameObject go = new GameObject("truck-control-cruise-provider-test");
            LwsTruckControlController controller = go.AddComponent<LwsTruckControlController>();
            LwsNwhVehicleInputProvider provider = go.AddComponent<LwsNwhVehicleInputProvider>();
            provider.SetTruckControlController(controller);

            controller.ApplyCommandFrame(new LwsVehicleCommandFrame { cruiseSet = LwsMomentaryIntent.Pressed }, default);
            Assert.Greater(provider.Throttle(), 0f);

            controller.ApplyCommandFrame(default, new LwsVehicleContinuousInput { brake = 1f });
            Assert.IsFalse(controller.CurrentState.cruiseEnabled);
            Assert.AreEqual(0f, provider.Throttle());

            yield return null;
            Object.Destroy(go);
        }

        [UnityTest]
        public IEnumerator FlipOffRaisesGestureEventWithoutTarget()
        {
            GameObject go = new GameObject("truck-control-gesture-service-test");
            LwsPlayerGestureController gesture = go.AddComponent<LwsPlayerGestureController>();
            LwsTruckControlController controller = go.AddComponent<LwsTruckControlController>();

            bool published = false;
            gesture.GestureStarted += gestureEvent =>
            {
                published = gestureEvent.gestureType == LwsTruckGestureType.FlipOffDriver;
            };

            controller.ApplyCommandFrame(new LwsVehicleCommandFrame { flipOffDriver = LwsMomentaryIntent.Pressed }, default);

            Assert.IsTrue(published);
            Assert.AreEqual(LwsTruckGestureType.FlipOffDriver, gesture.ActiveGesture);
            yield return null;
            Object.Destroy(go);
        }
    }
}

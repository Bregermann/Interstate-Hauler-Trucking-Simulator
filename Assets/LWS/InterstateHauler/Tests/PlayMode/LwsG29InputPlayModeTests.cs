using System.Collections;
using NUnit.Framework;
using NWH.VehiclePhysics2.Input;
using UnityEngine;
using UnityEngine.TestTools;

namespace LWS.InterstateHauler.Tests.PlayMode
{
    public sealed class LwsG29InputPlayModeTests
    {
        [UnityTest]
        public IEnumerator WheelProviderCanBecomeActiveInputOwner()
        {
            var service = new LwsVehicleInputService();
            service.Initialize(new LwsServiceContext(new LwsServiceRegistry()));
            var source = new SimulatedSource();

            LwsServiceResult result = service.SetInputSource(source, LwsVehicleInputOwner.Wheel);

            Assert.IsTrue(result.Succeeded, result.Message);
            Assert.AreEqual(LwsVehicleInputOwner.Wheel, service.ActiveOwner);
            yield return null;
        }

        [UnityTest]
        public IEnumerator SimulatedWheelDataReachesNwhProvider()
        {
            var go = new GameObject("lws-wheel-provider");
            LwsWheelInputSource source = go.AddComponent<LwsWheelInputSource>();
            source.SetSimulatedFrameForTests(new LwsWheelInputFrame
            {
                connectionState = LwsWheelConnectionState.Connected,
                analog = new LwsWheelAnalogState { steering = 0.25f, throttle = 0.75f, brake = 0.1f, clutch = 0.5f },
                shifter = new LwsHPatternShifterState { activeGate = LwsTruckShifterGate.Gate2 }
            });
            LwsNwhVehicleInputProvider provider = go.AddComponent<LwsNwhVehicleInputProvider>();
            provider.SetInputSource(source);

            yield return null;

            Assert.AreEqual(0.25f, provider.Steering(), 0.0001f);
            Assert.AreEqual(0.75f, provider.Throttle(), 0.0001f);
            Assert.AreEqual(0.1f, provider.Brakes(), 0.0001f);
            Assert.AreEqual(0.5f, provider.Clutch(), 0.0001f);
            Assert.AreEqual(2, provider.ShiftInto());
            Object.Destroy(go);
        }

        [UnityTest]
        public IEnumerator WheelOwnershipDisablesStockNwhProvider()
        {
            var stockObject = new GameObject("stock-nwh-input");
            InputSystemVehicleInputProvider stockProvider = stockObject.AddComponent<InputSystemVehicleInputProvider>();

            var wheelObject = new GameObject("lws-wheel-input");
            LwsWheelInputSource source = wheelObject.AddComponent<LwsWheelInputSource>();
            source.SetSimulatedFrameForTests(new LwsWheelInputFrame
            {
                connectionState = LwsWheelConnectionState.Connected,
                analog = new LwsWheelAnalogState { steering = 0.1f },
                shifter = LwsHPatternShifterState.Neutral()
            });
            LwsWheelInputBootstrap bootstrap = wheelObject.AddComponent<LwsWheelInputBootstrap>();

            yield return null;
            yield return null;

            Assert.IsTrue(bootstrap.WheelOwnsInput);
            Assert.IsFalse(stockProvider.enabled);

            bootstrap.ReleaseWheelOwner();
            Assert.IsTrue(stockProvider.enabled);

            Object.Destroy(stockObject);
            Object.Destroy(wheelObject);
        }

        [UnityTest]
        public IEnumerator ReleasingWheelOwnerNeutralizesImmediately()
        {
            var wheelObject = new GameObject("lws-wheel-release");
            LwsWheelInputSource source = wheelObject.AddComponent<LwsWheelInputSource>();
            source.SetSimulatedFrameForTests(new LwsWheelInputFrame
            {
                connectionState = LwsWheelConnectionState.Connected,
                analog = new LwsWheelAnalogState { steering = 1f, throttle = 1f },
                shifter = new LwsHPatternShifterState { activeGate = LwsTruckShifterGate.Gate5 }
            });
            LwsWheelInputBootstrap bootstrap = wheelObject.AddComponent<LwsWheelInputBootstrap>();

            yield return null;
            bootstrap.ReleaseWheelOwner();

            LwsVehicleContinuousInput continuous = source.ReadContinuousInput();
            LwsTruckGearIntent gear = source.ReadGearIntent();
            Assert.AreEqual(0f, continuous.steering);
            Assert.AreEqual(0f, continuous.throttle);
            Assert.IsTrue(gear.neutralRequested);

            Object.Destroy(wheelObject);
        }

        private sealed class SimulatedSource : ILwsVehicleInputSource
        {
            public string SourceId => "simulated";
            public LwsVehicleContinuousInput ReadContinuousInput() => new LwsVehicleContinuousInput { steering = 0.5f };
            public LwsVehicleCommandFrame ReadCommandFrame() => default;
            public LwsTruckGearIntent ReadGearIntent() => default;
        }
    }
}

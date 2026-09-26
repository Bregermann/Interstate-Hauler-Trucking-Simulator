using System;
using System.Reflection;
using LWS.InterstateHauler;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object = UnityEngine.Object;

namespace LWS.TruckTaxi.Tests
{
    public sealed class TruckTaxiJugInputSuppressionTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private GameObject root;
        private LwsKeyboardGamepadTruckInputSource source;
        private Gamepad pad;
        private TruckTaxiEnvironmentSettings settings;
        [SetUp] public void SetUp()
        {
            root = new GameObject("Jug input test"); source = root.AddComponent<LwsKeyboardGamepadTruckInputSource>();
            pad = InputSystem.AddDevice<Gamepad>(); pad.MakeCurrent();
            settings = ScriptableObject.CreateInstance<TruckTaxiEnvironmentSettings>();
        }
        [TearDown] public void TearDown()
        {
            Object.DestroyImmediate(root); Object.DestroyImmediate(settings);
            if (pad != null && pad.added) InputSystem.RemoveDevice(pad);
        }
        [Test] public void LeasePreservesPriorAndConcurrentSuppression()
        {
            Assert.IsFalse(source.GamepadIndicatorsSuppressed);
            var jug = new object(); var other = new object();
            source.SetGamepadIndicatorsSuppressed(true);
            source.AcquireGamepadIndicatorSuppression(jug); source.AcquireGamepadIndicatorSuppression(jug);
            source.ReleaseGamepadIndicatorSuppression(jug); Assert.IsTrue(source.GamepadIndicatorsSuppressed);
            source.SetGamepadIndicatorsSuppressed(false);
            source.AcquireGamepadIndicatorSuppression(jug); source.AcquireGamepadIndicatorSuppression(other);
            source.ReleaseGamepadIndicatorSuppression(jug); Assert.IsTrue(source.GamepadIndicatorsSuppressed);
            source.ReleaseGamepadIndicatorSuppression(other); Assert.IsFalse(source.GamepadIndicatorsSuppressed);
        }
        [Test] public void SuppressionConsumesEdgesAndLeavesDrivingCommandsAlone()
        {
            source.AcquireGamepadIndicatorSuppression(this);
            Push(new GamepadState { leftStick = new Vector2(.6f, 0), rightTrigger = .8f, leftTrigger = .3f }
                .WithButton(GamepadButton.LeftShoulder).WithButton(GamepadButton.RightShoulder).WithButton(GamepadButton.South));
            var frame = Read<LwsVehicleCommandFrame>("ReadGamepadCommands");
            Assert.AreEqual(LwsMomentaryIntent.None, frame.leftIndicator); Assert.AreEqual(LwsMomentaryIntent.None, frame.rightIndicator);
            Assert.AreEqual(LwsMomentaryIntent.Pressed, frame.horn);
            var continuous = Read<LwsVehicleContinuousInput>("ReadContinuousNow");
            Assert.Greater(continuous.steering, .4f); Assert.Greater(continuous.throttle, .7f); Assert.Greater(continuous.brake, .2f);
            source.ReleaseGamepadIndicatorSuppression(this); frame = Read<LwsVehicleCommandFrame>("ReadGamepadCommands");
            Assert.AreEqual(LwsMomentaryIntent.Held, frame.leftIndicator); Assert.AreEqual(LwsMomentaryIntent.Held, frame.rightIndicator);
            source.AcquireGamepadIndicatorSuppression(this);
            Push(new GamepadState()); Read<LwsVehicleCommandFrame>("ReadGamepadCommands");
            source.ReleaseGamepadIndicatorSuppression(this); frame = Read<LwsVehicleCommandFrame>("ReadGamepadCommands");
            Assert.AreEqual(LwsMomentaryIntent.None, frame.leftIndicator); Assert.AreEqual(LwsMomentaryIntent.None, frame.rightIndicator);
            Push(new GamepadState().WithButton(GamepadButton.LeftShoulder).WithButton(GamepadButton.RightShoulder));
            frame = Read<LwsVehicleCommandFrame>("ReadGamepadCommands");
            Assert.AreEqual(LwsMomentaryIntent.Pressed, frame.leftIndicator); Assert.AreEqual(LwsMomentaryIntent.Pressed, frame.rightIndicator);
        }
        [TestCase("success"), TestCase("spill"), TestCase("cancel"), TestCase("crisis")]
        public void NeedsReleasesOnlyItsOwnLease(string ending)
        {
            var needs = root.AddComponent<TruckTaxiDriverNeedsCoordinator>();
            typeof(TruckTaxiDriverNeedsCoordinator).GetField("indicatorSources", Private).SetValue(needs, new[] { source });
            var callback = (Action<TruckTaxiDriverNeedEvent>)Delegate.CreateDelegate(typeof(Action<TruckTaxiDriverNeedEvent>), needs,
                typeof(TruckTaxiDriverNeedsCoordinator).GetMethod("OnJugInputEvent", Private));
            var state = new TruckTaxiDriverNeedsState(settings, 1); state.Event += callback;
            state.SetPressure(.9f); Assert.IsTrue(state.StartJug()); Assert.IsTrue(source.GamepadIndicatorsSuppressed);
            if (ending == "success") state.FinishJug(true);
            else if (ending == "spill") state.FinishJug(false);
            else if (ending == "cancel") state.CancelJug();
            else { state.SetPressure(1); state.AdvanceGameSeconds(settings.crisisGraceGameMinutes * 60 + 1); }
            Assert.IsFalse(source.GamepadIndicatorsSuppressed);
            source.SetGamepadIndicatorsSuppressed(true);
            state.DisposeJug(true); state.SetPressure(.9f); Assert.IsTrue(state.StartJug()); state.CancelJug();
            Assert.IsTrue(source.GamepadIndicatorsSuppressed); state.Event -= callback;
        }
        [Test] public void DestroyedNeedsReleasesItsLeaseButNotOtherOwners()
        {
            var needs = root.AddComponent<TruckTaxiDriverNeedsCoordinator>();
            typeof(TruckTaxiDriverNeedsCoordinator).GetField("indicatorSources", Private).SetValue(needs, new[] { source });
            source.AcquireGamepadIndicatorSuppression(needs); source.AcquireGamepadIndicatorSuppression(this);
            Object.DestroyImmediate(needs); Assert.IsTrue(source.GamepadIndicatorsSuppressed);
            source.ReleaseGamepadIndicatorSuppression(this); Assert.IsFalse(source.GamepadIndicatorsSuppressed);
        }
        private void Push(GamepadState state) { InputSystem.QueueStateEvent(pad, state); InputSystem.Update(); }
        private T Read<T>(string method) => (T)typeof(LwsKeyboardGamepadTruckInputSource).GetMethod(method, Private).Invoke(source, null);
    }
}

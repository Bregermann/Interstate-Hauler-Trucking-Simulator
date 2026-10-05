using System.Reflection;
using LWS.InterstateHauler;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object = UnityEngine.Object;

namespace LWS.TruckTaxi.Tests
{
    public sealed class TruckTaxiHornBindingTests
    {
        private GameObject root;
        private LwsKeyboardGamepadTruckInputSource source;
        private Keyboard keyboard;
        private Gamepad pad;

        [SetUp] public void SetUp()
        {
            root = new GameObject("Horn binding test");
            source = root.AddComponent<LwsKeyboardGamepadTruckInputSource>();
            keyboard = InputSystem.AddDevice<Keyboard>(); keyboard.MakeCurrent();
            pad = InputSystem.AddDevice<Gamepad>(); pad.MakeCurrent();
        }

        [TearDown] public void TearDown()
        {
            Object.DestroyImmediate(root);
            if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
            if (pad != null && pad.added) InputSystem.RemoveDevice(pad);
        }

        [Test] public void HIsTheNormalKeyboardHornForItsEntirePressCycle()
        {
            PushKeyboard(Key.H);
            AssertHorns(Read("ReadKeyboardCommands"), LwsMomentaryIntent.Pressed, LwsMomentaryIntent.None);
            AssertHorns(Read("ReadKeyboardCommands"), LwsMomentaryIntent.Held, LwsMomentaryIntent.None);
            PushKeyboard();
            AssertHorns(Read("ReadKeyboardCommands"), LwsMomentaryIntent.Released, LwsMomentaryIntent.None);
            AssertHorns(Read("ReadKeyboardCommands"), LwsMomentaryIntent.None, LwsMomentaryIntent.None);
        }

        [Test] public void BProducesNoHornAndDoesNotInterfereWithH()
        {
            PushKeyboard(Key.B);
            AssertHorns(Read("ReadKeyboardCommands"), LwsMomentaryIntent.None, LwsMomentaryIntent.None);
            AssertHorns(Read("ReadKeyboardCommands"), LwsMomentaryIntent.None, LwsMomentaryIntent.None);
            PushKeyboard(Key.B, Key.H);
            AssertHorns(Read("ReadKeyboardCommands"), LwsMomentaryIntent.Pressed, LwsMomentaryIntent.None);
            PushKeyboard(Key.B);
            AssertHorns(Read("ReadKeyboardCommands"), LwsMomentaryIntent.Released, LwsMomentaryIntent.None);
            PushKeyboard();
            AssertHorns(Read("ReadKeyboardCommands"), LwsMomentaryIntent.None, LwsMomentaryIntent.None);
        }

        [Test] public void GamepadHornAndAirHornKeepTheirIndependentBindings()
        {
            PushPad(new GamepadState().WithButton(GamepadButton.South));
            AssertHorns(Read("ReadGamepadCommands"), LwsMomentaryIntent.Pressed, LwsMomentaryIntent.None);
            PushPad(new GamepadState().WithButton(GamepadButton.RightStick));
            AssertHorns(Read("ReadGamepadCommands"), LwsMomentaryIntent.Released, LwsMomentaryIntent.Pressed);
            AssertHorns(Read("ReadGamepadCommands"), LwsMomentaryIntent.None, LwsMomentaryIntent.Held);
            PushPad(new GamepadState());
            AssertHorns(Read("ReadGamepadCommands"), LwsMomentaryIntent.None, LwsMomentaryIntent.Released);
        }

        private void PushKeyboard(params Key[] keys)
        { InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys)); InputSystem.Update(); }
        private void PushPad(GamepadState state)
        { InputSystem.QueueStateEvent(pad, state); InputSystem.Update(); }
        private LwsVehicleCommandFrame Read(string method) => (LwsVehicleCommandFrame)
            typeof(LwsKeyboardGamepadTruckInputSource).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(source, null);
        private static void AssertHorns(LwsVehicleCommandFrame frame, LwsMomentaryIntent horn, LwsMomentaryIntent airHorn)
        { Assert.AreEqual(horn, frame.horn); Assert.AreEqual(airHorn, frame.airHorn); }
    }
}

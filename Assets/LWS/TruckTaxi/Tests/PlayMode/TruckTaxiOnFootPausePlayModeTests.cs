using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

namespace LWS.TruckTaxi.Tests
{
    [Category("TaxiOnFootPause")]
    public sealed class TruckTaxiOnFootPausePlayModeTests
    {
        [TestCase(TruckTaxiPlayerControlMode.RestroomQte)]
        [TestCase(TruckTaxiPlayerControlMode.SpecialInteraction)]
        public void ActivityStartYieldsToPauseWithoutCancellingOrInteracting(TruckTaxiPlayerControlMode mode)
        {
            using (var fixture = new WorldLifeInputFixture())
            {
                var challenge = new TruckTaxiRoadPepChallenge();
                fixture.Set("<Mode>k__BackingField", mode);
                fixture.Set("ambientChallenge", challenge);
                fixture.Set("restroomProgress", .4f);
                fixture.Set("restroomCues", 1);
                fixture.Push(new GamepadState().WithButton(GamepadButton.Start).WithButton(GamepadButton.South));
                Assert.IsTrue(fixture.Input.Pause.WasPressedThisFrame());
                Assert.IsFalse(fixture.Input.Cancel.WasPressedThisFrame());
                Assert.IsFalse(fixture.World.HandleHudInput(fixture.Input), "The existing HUD must receive Start.");
                Assert.AreEqual(mode, fixture.World.Mode);
                Assert.AreSame(challenge, fixture.Read<TruckTaxiRoadPepChallenge>("ambientChallenge"));
                Assert.AreEqual(.4f, fixture.Read<float>("restroomProgress"));
                Assert.AreEqual(1, fixture.Read<int>("restroomCues"), "Pause must win over a simultaneous interaction.");

                fixture.World.SetExternalModalFocus(true);
                fixture.Push(new GamepadState().WithButton(GamepadButton.East));
                Assert.IsFalse(fixture.World.HandleHudInput(fixture.Input), "While paused, Cancel belongs to the modal owner.");
                Assert.AreEqual(mode, fixture.World.Mode);
                fixture.World.SetExternalModalFocus(false);
                fixture.Push(new GamepadState());
                Assert.IsTrue(fixture.World.HandleHudInput(fixture.Input), "The activity keeps ownership after pause closes.");
                fixture.Push(new GamepadState().WithButton(GamepadButton.East));
                Assert.IsTrue(fixture.World.HandleHudInput(fixture.Input));
                Assert.AreEqual(TruckTaxiPlayerControlMode.OnFoot, fixture.World.Mode, "B retains activity cancellation.");

                fixture.Set("<Mode>k__BackingField", mode);
                fixture.Set("ambientChallenge", challenge);
                fixture.Push(new GamepadState());
                InputSystem.QueueStateEvent(fixture.Keyboard, new KeyboardState(Key.Escape));
                InputSystem.Update();
                Assert.IsTrue(fixture.Input.Pause.WasPressedThisFrame());
                Assert.IsTrue(fixture.Input.Cancel.WasPressedThisFrame());
                Assert.IsTrue(fixture.World.HandleHudInput(fixture.Input));
                Assert.AreEqual(TruckTaxiPlayerControlMode.OnFoot, fixture.World.Mode, "Escape retains its existing Cancel semantics.");
            }
        }

        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        public void PanelStartClosesWithoutSubmittingOrTogglingHudPause(bool temporary, bool pauseUtility)
        {
            using (var fixture = new WorldLifeInputFixture())
            {
                var panel = new GameObject("Owned panel", typeof(RectTransform));
                panel.transform.SetParent(fixture.Root.transform);
                var button = new GameObject("Must not submit", typeof(RectTransform), typeof(Button));
                button.transform.SetParent(panel.transform);
                int submits = 0;
                button.GetComponent<Button>().onClick.AddListener(() => submits++);
                fixture.Set(temporary ? "temporaryPanel" : "inventoryPanel", panel.GetComponent<RectTransform>());
                fixture.Set("<Mode>k__BackingField", temporary ? TruckTaxiPlayerControlMode.SeatedInteraction : TruckTaxiPlayerControlMode.OnFoot);
                fixture.Set("pauseUtilityOpen", pauseUtility);
                fixture.Set("seatedAtDiner", temporary);
                fixture.Set("preSeatPosition", Vector3.right * 3);
                if (pauseUtility) fixture.World.SetExternalModalFocus(true);
                fixture.Input.Focus(panel.transform);
                fixture.Push(new GamepadState().WithButton(GamepadButton.Start).WithButton(GamepadButton.South));

                Assert.IsTrue(fixture.Input.Pause.WasPressedThisFrame(), "The fixture must deliver the real Start edge.");
                Assert.IsTrue(fixture.Input.Submit.WasPressedThisFrame(), "The fixture must deliver simultaneous Submit.");
                Assert.IsTrue(fixture.World.HandleHudInput(fixture.Input), "Closing a panel must consume Start, not also toggle pause.");
                Assert.IsFalse(panel.activeSelf);
                Assert.AreEqual(0, submits, "Start must win over a simultaneous Submit.");
                Assert.AreEqual(TruckTaxiPlayerControlMode.OnFoot, fixture.World.Mode);
                Assert.IsFalse(fixture.Read<bool>("pauseUtilityOpen"));
                Assert.IsTrue(fixture.Root.GetComponent<CharacterController>().enabled);
                Assert.IsTrue(fixture.World.PointerInputBlocked, "Closing preserves the input-release-frame guard.");
                if (temporary) Assert.AreEqual(Vector3.right * 3, fixture.Root.transform.position);
                if (pauseUtility) Assert.IsTrue(fixture.Read<bool>("externalModalFocus"), "An existing pause/modal owner must remain intact.");
            }
        }

        private sealed class WorldLifeInputFixture : System.IDisposable
        {
            private const System.Reflection.BindingFlags Private = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            private readonly CursorLockMode previousLock = Cursor.lockState;
            private readonly bool previousCursor = Cursor.visible;
            private readonly InputSettings previousSettings = InputSystem.settings;
            private readonly InputSettings testSettings;
            public readonly GameObject Root = new GameObject("WorldLife pause input test");
            public readonly TruckTaxiWorldLifeController World;
            public readonly TruckTaxiUIInput Input;
            public readonly Gamepad Pad;
            public readonly Keyboard Keyboard;
            public WorldLifeInputFixture()
            {
                Assert.IsTrue(Application.isPlaying, "Action-edge regression cases require a player input update, not an Editor update.");
                testSettings = Object.Instantiate(previousSettings);
                InputSystem.settings = testSettings;
                testSettings.updateMode = InputSettings.UpdateMode.ProcessEventsManually;
                testSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
                testSettings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
                World = Root.AddComponent<TruckTaxiWorldLifeController>();
                Set("footBody", Root);
                Set("controller", Root.AddComponent<CharacterController>());
                Input = new TruckTaxiUIInput(Root.AddComponent<EventSystem>());
                Pad = InputSystem.AddDevice<Gamepad>();
                Keyboard = InputSystem.AddDevice<Keyboard>();
                Input.Actions.devices = new InputDevice[] { Pad, Keyboard };
            }
            public void Set(string name, object value) => typeof(TruckTaxiWorldLifeController).GetField(name, Private).SetValue(World, value);
            public T Read<T>(string name) => (T)typeof(TruckTaxiWorldLifeController).GetField(name, Private).GetValue(World);
            public void Push(GamepadState state) { InputSystem.QueueStateEvent(Pad, state); InputSystem.Update(); }
            public void Dispose()
            {
                Input.Dispose();
                if (Pad.added) InputSystem.RemoveDevice(Pad);
                if (Keyboard.added) InputSystem.RemoveDevice(Keyboard);
                Set("<Mode>k__BackingField", TruckTaxiPlayerControlMode.Driving);
                Object.DestroyImmediate(Root);
                InputSystem.settings = previousSettings;
                Object.DestroyImmediate(testSettings);
                Cursor.lockState = previousLock; Cursor.visible = previousCursor;
            }
        }
    }
}

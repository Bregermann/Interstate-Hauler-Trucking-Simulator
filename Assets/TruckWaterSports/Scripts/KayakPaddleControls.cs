using UnityEngine;
using LWS.TruckBowling;
using LWS.TruckSports;

namespace LWS.TruckWaterSports
{
    // Sport semantics over the existing assigned source. Never changes device
    // ownership, global actions, keyboards, horn buttons or camera bindings.
    public static class KayakPaddleControls
    {
        public struct Strokes
        {
            public float left, right, carve, lean;
            public bool Neutral => Mathf.Abs(left) < .05f && Mathf.Abs(right) < .05f;
        }

        public static Strokes Read(BowlingPlayer player)
        {
            if (player == null || !player.released || SportsControllerKeyboard.InputBlocked) return default;
            if (player.source == BowlingSource.Keyboard)
            {
                bool left = SportsDeviceInput.KeyHeld(player.left), right = SportsDeviceInput.KeyHeld(player.right);
                bool forward = SportsDeviceInput.KeyHeld(player.forward), backward = SportsDeviceInput.KeyHeld(player.back);
                float strength = backward && !forward ? -1 : 1;
                bool both = !left && !right && (forward || backward);
                return new Strokes { left = left || both ? strength : 0, right = right || both ? strength : 0, lean = (right ? 1 : 0) - (left ? 1 : 0) };
            }
            var pad = player.pad;
            if (pad == null || !pad.added || !pad.enabled) return default;
            if (player.source == BowlingSource.WholeController)
            {
                float forward = pad.rightTrigger.ReadValue(), backward = pad.leftTrigger.ReadValue();
                float strength = forward > .12f || backward > .12f ? forward - backward : 1;
                float steer = pad.leftStick.ReadValue().x;
                // Whole-pad horn remains L3; camera/action buttons are untouched.
                return new Strokes { left = pad.leftShoulder.isPressed ? strength : 0, right = pad.rightShoulder.isPressed ? strength : 0, carve = steer, lean = steer };
            }
            if (player.source != BowlingSource.LeftStick && player.source != BowlingSource.RightStick) return default;
            Vector2 stick = player.source == BowlingSource.LeftStick ? pad.leftStick.ReadValue() : pad.rightStick.ReadValue();
            float power = Mathf.Abs(stick.y) > .15f ? stick.y : 1;
            bool leftSide = stick.x < -.35f, rightSide = stick.x > .35f;
            bool pair = !leftSide && !rightSide && Mathf.Abs(stick.y) > .15f;
            // Split-pad shoulder stays the horn; its trigger stays roll/recover.
            return new Strokes { left = leftSide || pair ? power : 0, right = rightSide || pair ? power : 0, lean = stick.x };
        }
    }
}

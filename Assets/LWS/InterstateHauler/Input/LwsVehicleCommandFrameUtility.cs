namespace LWS.InterstateHauler
{
    public static class LwsVehicleCommandFrameUtility
    {
        public static bool IsPressed(LwsMomentaryIntent intent)
        {
            return intent == LwsMomentaryIntent.Pressed;
        }

        public static bool IsActive(LwsMomentaryIntent intent)
        {
            return intent == LwsMomentaryIntent.Pressed || intent == LwsMomentaryIntent.Held;
        }

        public static LwsMomentaryIntent Combine(LwsMomentaryIntent a, LwsMomentaryIntent b)
        {
            if (a == LwsMomentaryIntent.Pressed || b == LwsMomentaryIntent.Pressed)
            {
                return LwsMomentaryIntent.Pressed;
            }

            if (a == LwsMomentaryIntent.Held || b == LwsMomentaryIntent.Held)
            {
                return LwsMomentaryIntent.Held;
            }

            if (a == LwsMomentaryIntent.Released || b == LwsMomentaryIntent.Released)
            {
                return LwsMomentaryIntent.Released;
            }

            return LwsMomentaryIntent.None;
        }

        public static LwsVehicleCommandFrame Combine(LwsVehicleCommandFrame a, LwsVehicleCommandFrame b)
        {
            return new LwsVehicleCommandFrame
            {
                ignitionToggle = Combine(a.ignitionToggle, b.ignitionToggle),
                ignition = Combine(a.ignition, b.ignition),
                engineStart = Combine(a.engineStart, b.engineStart),
                engineStop = Combine(a.engineStop, b.engineStop),
                parkingBrakeToggle = Combine(a.parkingBrakeToggle, b.parkingBrakeToggle),
                horn = Combine(a.horn, b.horn),
                airHorn = Combine(a.airHorn, b.airHorn),
                lowBeamLights = Combine(a.lowBeamLights, b.lowBeamLights),
                highBeamLights = Combine(a.highBeamLights, b.highBeamLights),
                hazardLights = Combine(a.hazardLights, b.hazardLights),
                leftIndicator = Combine(a.leftIndicator, b.leftIndicator),
                rightIndicator = Combine(a.rightIndicator, b.rightIndicator),
                wipers = Combine(a.wipers, b.wipers),
                wiperIncrease = Combine(a.wiperIncrease, b.wiperIncrease),
                wiperDecrease = Combine(a.wiperDecrease, b.wiperDecrease),
                cruiseControl = Combine(a.cruiseControl, b.cruiseControl),
                cruiseSet = Combine(a.cruiseSet, b.cruiseSet),
                cruiseResume = Combine(a.cruiseResume, b.cruiseResume),
                cruiseCancel = Combine(a.cruiseCancel, b.cruiseCancel),
                cruiseIncrease = Combine(a.cruiseIncrease, b.cruiseIncrease),
                cruiseDecrease = Combine(a.cruiseDecrease, b.cruiseDecrease),
                engineBrake = Combine(a.engineBrake, b.engineBrake),
                engineBrakeIncrease = Combine(a.engineBrakeIncrease, b.engineBrakeIncrease),
                engineBrakeDecrease = Combine(a.engineBrakeDecrease, b.engineBrakeDecrease),
                retarder = Combine(a.retarder, b.retarder),
                retarderIncrease = Combine(a.retarderIncrease, b.retarderIncrease),
                retarderDecrease = Combine(a.retarderDecrease, b.retarderDecrease),
                differentialLock = Combine(a.differentialLock, b.differentialLock),
                transmissionShiftUp = Combine(a.transmissionShiftUp, b.transmissionShiftUp),
                transmissionShiftDown = Combine(a.transmissionShiftDown, b.transmissionShiftDown),
                trailerAttachDetach = Combine(a.trailerAttachDetach, b.trailerAttachDetach),
                trailerBrake = Combine(a.trailerBrake, b.trailerBrake),
                cameraCycle = Combine(a.cameraCycle, b.cameraCycle),
                lookReset = Combine(a.lookReset, b.lookReset),
                flipOffDriver = Combine(a.flipOffDriver, b.flipOffDriver),
                interact = Combine(a.interact, b.interact),
                menuSubmit = Combine(a.menuSubmit, b.menuSubmit),
                menuCancel = Combine(a.menuCancel, b.menuCancel),
                pause = Combine(a.pause, b.pause),
                navigateUp = Combine(a.navigateUp, b.navigateUp),
                navigateDown = Combine(a.navigateDown, b.navigateDown),
                navigateLeft = Combine(a.navigateLeft, b.navigateLeft),
                navigateRight = Combine(a.navigateRight, b.navigateRight)
            };
        }
    }
}

using UnityEngine;

namespace DeadAir
{
    public sealed class DeadAirGpsEventTrigger : DeadAirTriggerZone
    {
        [SerializeField] private string instruction = "Recalculating.";
        [SerializeField] private string destination = "Exit 17";
        [SerializeField] private DeadAirGpsArrow arrow = DeadAirGpsArrow.Straight;
        [SerializeField] private float distanceMeters = 805f;
        [SerializeField] private bool enabledState = true;
        [SerializeField] private bool routeVisible = true;
        [SerializeField] private bool intentionallyWrong;
        [SerializeField] private bool recalculate;
        [SerializeField] private bool signalLost;
        [SerializeField] private bool corrupted;
        [SerializeField] private bool restoreNormal;

        protected override void Reset()
        {
            base.Reset();
            SetCategory(DeadAirTriggerCategory.GPS);
        }

        protected override void OnActivated(DeadAirTriggerEvent triggerEvent, DeadAirStoryDirector director)
        {
            DeadAirGPSDirector gps = DeadAirGameManager.Instance != null
                ? DeadAirGameManager.Instance.GpsDirector
                : FindAnyObjectByType<DeadAirGPSDirector>();
            DeadAirGPSController narrativeGps = DeadAirGPSController.ResolveShared();

            if (restoreNormal)
            {
                gps?.ResetGps();
                narrativeGps?.SetNormal();
                return;
            }

            if (gps != null)
            {
                gps.SetEnabled(enabledState);
                gps.SetDestination(destination);
                gps.SetRouteVisible(routeVisible);
                gps.SetGpsInstruction(instruction, arrow, distanceMeters, intentionallyWrong);
                if (recalculate)
                {
                    gps.SetRecalculating(true);
                }

                if (signalLost)
                {
                    gps.SetSignalLost(true);
                }

                if (corrupted)
                {
                    gps.SetCorrupted(true);
                }
            }

            if (narrativeGps != null)
            {
                narrativeGps.ExecuteEvent(BuildNarrativeEvent());
            }
        }

        private DeadAirGpsNarrativeEvent BuildNarrativeEvent()
        {
            string secondary = FormatDistance(distanceMeters);
            var gpsEvent = new DeadAirGpsNarrativeEvent
            {
                enableGpsEvent = true,
                eventType = ResolveEventType(),
                primaryText = signalLost ? "GPS SIGNAL LOST" : instruction,
                secondaryText = signalLost ? destination : secondary,
                arrow = signalLost ? DeadAirGpsArrow.None : arrow,
                distanceMeters = distanceMeters,
                destination = destination,
                routeVisible = routeVisible,
                intentionallyWrong = intentionallyWrong,
                flash = !signalLost,
                glitch = corrupted,
                glitchDuration = corrupted ? 0.8f : 0.45f,
                glitchIntensity = corrupted ? 0.75f : 0.45f,
                recalculating = recalculate,
                recalculatingMessage = "RECALCULATING...",
                recalculatingDuration = 1.4f,
                finalPrimaryText = instruction,
                finalSecondaryText = secondary,
                finalArrow = arrow
            };

            return gpsEvent;
        }

        private DeadAirGpsNarrativeEventType ResolveEventType()
        {
            if (corrupted && !recalculate)
            {
                return DeadAirGpsNarrativeEventType.GlitchThenDirection;
            }

            if (recalculate)
            {
                return DeadAirGpsNarrativeEventType.RecalculatingThenDirection;
            }

            if (signalLost)
            {
                return DeadAirGpsNarrativeEventType.ChangeDirection;
            }

            return DeadAirGpsNarrativeEventType.FlashAndChangeDirection;
        }

        private static string FormatDistance(float meters)
        {
            if (meters >= 1609.344f)
            {
                return $"{meters / 1609.344f:0.0} MI";
            }

            return $"{Mathf.Max(0f, meters) * 3.28084f:0} FT";
        }
    }
}

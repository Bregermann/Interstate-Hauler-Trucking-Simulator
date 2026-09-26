#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace LWS.TruckTaxi
{
    // Opt-in actual-player fixture. It never starts during an ordinary game launch.
    public static class TruckTaxiOfferRuntimeProbe
    {
        public static IEnumerator Run(TruckTaxiBootstrap host,Action<bool,string> check,Action<string> capture)
        {
            var keyboard=InputSystem.AddDevice<Keyboard>("Taxi offer test keyboard");
            var gamepad=InputSystem.AddDevice<Gamepad>("Taxi offer test gamepad");
            float frequency=host.Configuration.rideFrequency;
            float duration=host.Configuration.offerDuration;
            try
            {
                host.Configuration.rideFrequency=10000;
                host.Session.EndShift(); host.StartShift();
                host.Configuration.offerDuration=10;
                check(host.Session.OfferRide(),"Offer created through session");
                var offer=host.Session.Offer;
                check(offer!=null && offer.NotificationClaimed,"Bootstrap claimed notification once");
                check(host.Configuration.offerSound!=null,"Real Heat notification clip assigned");
                yield return new WaitForSecondsRealtime(.15f);
                check(host.hud.OfferCountdown.gameObject.activeInHierarchy && host.hud.OfferCountdown.FillAmount>.9f,"Heat countdown starts full");
                capture?.Invoke("Offer_Full");
                host.Session.OfferTimerPaused=true;
                float remaining=host.Session.OfferRemaining;
                yield return new WaitForSecondsRealtime(.4f);
                check(Mathf.Abs(host.Session.OfferRemaining-remaining)<.02f,"Debug timer pause freezes the one session deadline");
                check(!offer.TryClaimNotification(),"Repeated presentation cannot replay offer notification");
                host.Session.OfferTimerPaused=false;
                yield return PressKey(keyboard,Key.Escape);
                check(host.Session.State==TruckTaxiState.Available && !host.hud.OfferCountdown.gameObject.activeInHierarchy,"Keyboard decline closes countdown");
                host.Session.OfferRide(); yield return new WaitForSecondsRealtime(.2f);
                check(host.Session.Offer.Id!=offer.Id && host.hud.OfferCountdown.FillAmount>.9f,"Next offer has fresh identity and full timer");
                yield return Pad(gamepad,GamepadButton.South);
                check(host.Session.State==TruckTaxiState.DrivingToPickup && !host.hud.OfferCountdown.gameObject.activeInHierarchy,"Controller accepts once and closes countdown");
                host.Session.EndShift(); host.StartShift(); host.Session.OfferRide();
                yield return new WaitForSecondsRealtime(.15f);
                yield return Pad(gamepad,GamepadButton.East);
                check(host.Session.State==TruckTaxiState.Available,"Controller declines without mouse");
                host.Session.OfferRide(); yield return new WaitForSecondsRealtime(.15f);
                yield return PressKey(keyboard,Key.Enter);
                check(host.Session.State==TruckTaxiState.DrivingToPickup,"Keyboard accepts without mouse");
                host.Session.EndShift(); host.StartShift();
                host.Configuration.offerDuration=3; host.Session.OfferRide();
                yield return new WaitForSecondsRealtime(1.1f);
                check(Mathf.Abs(host.hud.OfferCountdown.FillAmount-host.Session.OfferRemainingNormalized)<.08f,"Visible ring follows gameplay timer");
                capture?.Invoke("Offer_Urgent");
                yield return new WaitForSecondsRealtime(2.2f);
                check(host.Session.State==TruckTaxiState.Available && !host.Session.AcceptRide(),"Expired offer cannot be accepted");
                check(!host.hud.OfferCountdown.gameObject.activeInHierarchy,"Expiry hides countdown");
            }
            finally
            {
                host.Session.OfferTimerPaused=false;
                host.Configuration.rideFrequency=frequency; host.Configuration.offerDuration=duration;
                host.Session.EndShift(); host.StartShift();
                InputSystem.RemoveDevice(keyboard); InputSystem.RemoveDevice(gamepad);
            }
        }
        private static IEnumerator PressKey(Keyboard device,Key key)
        {
            InputSystem.QueueStateEvent(device,new KeyboardState(key)); yield return null;
            InputSystem.QueueStateEvent(device,new KeyboardState()); yield return new WaitForSecondsRealtime(.18f);
        }
        private static IEnumerator Pad(Gamepad device,GamepadButton button)
        {
            InputSystem.QueueStateEvent(device,new GamepadState().WithButton(button)); yield return null;
            InputSystem.QueueStateEvent(device,new GamepadState()); yield return new WaitForSecondsRealtime(.18f);
        }
    }
}
#endif

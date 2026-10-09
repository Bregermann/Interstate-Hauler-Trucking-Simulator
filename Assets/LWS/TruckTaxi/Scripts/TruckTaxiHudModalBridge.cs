using System;
using UnityEngine;

namespace LWS.TruckTaxi
{
    [DisallowMultipleComponent]
    public sealed class TruckTaxiHudModalBridge : MonoBehaviour
    {
        private sealed class Lease : IDisposable
        {
            private TruckTaxiHudModalBridge bridge;
            private readonly object owner;
            private readonly long token;
            public Lease(TruckTaxiHudModalBridge bridge, object owner, long token)
            { this.bridge = bridge; this.owner = owner; this.token = token; }
            public void Dispose()
            {
                var current = bridge; bridge = null;
                if (current != null) current.Release(owner, token);
            }
        }

        private readonly TruckTaxiModalPauseGuard guard = new TruckTaxiModalPauseGuard();
        private TruckTaxiBootstrap host;
        private TruckTaxiHud hud;
        private object owner;
        private long token;
        private Transform focus;
        private Action cancel;
        private Func<bool> interrupted;
        private string acquiredContext;
        private bool interruptNotified;
        private int routedFrame = int.MinValue;
        public bool RoutingAvailable => routedFrame != int.MinValue && Time.frameCount - routedFrame <= 1;
        public bool HasLease => guard.IsHeld;
        public bool IsOpen => isActiveAndEnabled && guard.IsHeld && host != null && focus != null &&
            Context == acquiredContext && !ForeignModalOpen;
        public Transform FocusRoot => IsOpen ? focus : null;
        public bool CanAcquire => isActiveAndEnabled && RoutingAvailable && !guard.IsHeld && host != null && host.Ready &&
            !host.Paused && hud != null && hud.Root != null && hud.CanOpenFullMap && !ForeignModalOpen;
        public bool IsOwnedBy(object value) => guard.IsOwnedBy(value);
        private string Context => host != null && host.Session != null ? host.Session.CurrentRideId + ":" + host.Session.State : "unavailable";

        // Only known public ownership flags are observed. No reflection into the HUD's private panels.
        private bool ForeignModalOpen => host == null || hud == null || host.Session == null ||
            hud.DebugOverlayOpen || hud.FullMap?.IsOpen == true || hud.Planning?.IsOpen == true || hud.RadioPanel?.IsOpen == true ||
            hud.GPSSettings?.IsOpen == true || hud.AudioSettings?.IsOpen == true || hud.EnvironmentNeeds?.IsOpen == true ||
            hud.Controls?.IsOpen == true || hud.Career?.IsOpen == true || hud.DriverAppearance?.IsOpen == true || hud.CabLookSettings?.IsOpen == true || host.Races?.MenuOpen == true ||
            host.Fuel?.IsRescuing == true || host.Roadside?.IsRecovering == true || host.FastTravel?.IsTravelling == true ||
            host.Kaiju?.IsCapturingPlayer == true || host.DriverNeeds?.IsSleeping == true ||
            host.CustomContent?.IsPlaying == true || host.CustomContent?.OwnsInputHold == true ||
            host.Companions?.SleeperSequenceActive == true || host.Session.PendingDiversion != null ||
            host.Session.State == TruckTaxiState.RideOffered || host.Session.State == TruckTaxiState.AppreciationOffer ||
            host.Session.State == TruckTaxiState.AppreciationSequence || host.Session.State == TruckTaxiState.RideComplete ||
            host.Session.State == TruckTaxiState.RideFailed || host.Session.State == TruckTaxiState.Inactive;

        public void Initialize(TruckTaxiBootstrap value, TruckTaxiHud view)
        {
            if (host == value && hud == view) return;
            if (guard.IsHeld) throw new InvalidOperationException("Cannot rebind an active HUD modal lease.");
            host = value; hud = view; routedFrame = int.MinValue;
        }

        public IDisposable TryAcquire(object value, Transform scope, Action onCancel, Func<bool> onInterrupted)
        {
            if (!CanAcquire || scope == null || !scope.IsChildOf(hud.Root)) return null;
            var context = Context;
            long claim = guard.TryAcquire(value, context, host.Paused, ForeignModalOpen, Time.frameCount);
            if (claim == 0) return null;
            owner = value; token = claim; focus = scope; cancel = onCancel; interrupted = onInterrupted;
            acquiredContext = context; interruptNotified = false;
            host.SetPaused(true);
            return new Lease(this, value, claim);
        }

        // Called once by the existing HUD dispatcher, before its normal map/pause/submit branches.
        // No Update polling, InputAction creation, performed subscription, EventSystem or bindings here.
        public bool HandleUiActions(TruckTaxiUIInput input)
        {
            if (!isActiveAndEnabled || hud == null || input == null || input != hud.UIInput) return false;
            routedFrame = Time.frameCount;
            CheckInterruption();
            if (guard.IsHeld && !IsOpen) return false;
            int dispatch = guard.Dispatch(Time.frameCount);
            if (dispatch == 0) return false;
            if (dispatch < 0) return true;
            input.Focus(focus);
            if (input.Cancel.WasPressedThisFrame() || input.Pause.WasPressedThisFrame()) cancel?.Invoke();
            else if (input.Submit.WasPressedThisFrame()) input.SubmitSelected();
            return true;
        }

        private void CheckInterruption()
        {
            if (!guard.IsHeld) return;
            bool foreign = ForeignModalOpen;
            bool paused = host != null && host.Paused;
            bool changed = Context != acquiredContext;
            guard.Observe(Context, paused, foreign);
            if ((foreign || changed || !paused || !RoutingAvailable || focus == null) && !interruptNotified)
            {
                guard.InvalidateResume(); interruptNotified = true;
                interrupted?.Invoke();
            }
            // A failed reboard may retain the lease. Never let an accidental resume drive the actor away.
            if (guard.IsHeld && !paused && !foreign && !changed && host != null) host.SetPaused(true);
        }
        private void LateUpdate() => CheckInterruption();
        private void Release(object value, long claim)
        {
            if (!guard.IsOwnedBy(value) || token != claim) return;
            bool resume = guard.Release(value, claim, Context, host != null && host.Paused, ForeignModalOpen, Time.frameCount);
            if (hud != null && hud.UIInput?.FocusRoot == focus) hud.UIInput.Focus(null);
            owner = null; token = 0; focus = null; cancel = null; interrupted = null;
            if (resume && host != null) host.SetPaused(false);
        }
        private void OnDisable()
        {
            routedFrame = int.MinValue;
            if (!guard.IsHeld) return;
            guard.InvalidateResume();
            interrupted?.Invoke();
            if (guard.IsHeld) Release(owner, token);
        }
    }
}

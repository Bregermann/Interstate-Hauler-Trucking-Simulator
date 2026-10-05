using System;
using TMPro;
using UnityEngine;

namespace LWS.TruckTaxi
{
    [DisallowMultipleComponent]
    public sealed class TruckTaxiPassengerBathroomPanel : MonoBehaviour
    {
        private TruckTaxiPassengerBathroomNeed need;
        private TruckTaxiBootstrap host;
        private TruckTaxiHud hud;
        private TruckTaxiHudModalBridge bridge;
        private RectTransform overlay, panel;
        private TextMeshProUGUI heading, message, detail;
        private GameObject accept, decline, retry;
        private IDisposable offerLease;
        private bool presentationClaim;
        private string feedback = "";
        public bool IsOpen => overlay != null && overlay.gameObject.activeSelf;
        public Transform FocusRoot => panel;
        public bool CanOffer => isActiveAndEnabled && need != null && bridge != null && bridge.CanAcquire;

        public bool Initialize(TruckTaxiPassengerBathroomNeed value, TruckTaxiBootstrap owner, TruckTaxiHud view)
        {
            if (value == null || owner == null || view == null || view.Root == null || view.UIInput == null ||
                view.Root.GetComponentInParent<Canvas>() == null) return false;
            if (need != null) Detach(need);
            need = value; host = owner; hud = view;
            bridge = view.GetComponent<TruckTaxiHudModalBridge>() ?? view.gameObject.AddComponent<TruckTaxiHudModalBridge>();
            bridge.Initialize(owner, view);
            if (overlay == null) Build();
            need.RequestOffered += OnRequestOffered;
            need.Changed += Refresh;
            need.AcquirePresentationHold = AcquirePresentationHold;
            Refresh();
            return true;
        }

        private void Build()
        {
            overlay = TruckTaxiHud.Rect(hud.Root, "Passenger restroom modal", Vector2.zero, Vector2.one);
            var blocker = overlay.gameObject.AddComponent<UnityEngine.UI.Image>();
            blocker.color = new Color(0, 0, 0, .62f); blocker.raycastTarget = true;
            panel = hud.Panel(overlay, "Passenger restroom request", new Vector2(.22f, .29f), new Vector2(.78f, .71f));
            heading = hud.Text(panel, "Restroom heading", new Vector2(.055f, .78f), new Vector2(.945f, .94f), 30);
            message = hud.Text(panel, "Restroom message", new Vector2(.055f, .4f), new Vector2(.945f, .73f), 25);
            detail = hud.Text(panel, "Restroom status", new Vector2(.055f, .25f), new Vector2(.945f, .39f), 20);
            foreach (var label in new[] { heading, message, detail })
            { label.richText = false; label.fontSizeMin = 16; label.characterSpacing = 0; }
            accept = hud.Button(panel, "ACCEPT", new Vector2(.055f, .055f), new Vector2(.475f, .215f), Accept);
            decline = hud.Button(panel, "DECLINE", new Vector2(.525f, .055f), new Vector2(.945f, .215f), Decline);
            retry = hud.Button(panel, "RETRY BOARDING", new Vector2(.25f, .055f), new Vector2(.75f, .215f), Retry);
            overlay.gameObject.SetActive(false);
        }

        private void OnRequestOffered()
        {
            if (need == null || !need.HasPendingRequest) return;
            offerLease = bridge.TryAcquire(this, panel, Decline, Interrupt);
            if (offerLease == null) { need.Cancel("Another HUD interaction owns the request slot."); return; }
            presentationClaim = false;
            feedback = "";
            Refresh();
            hud.UIInput.Focus(panel);
        }
        private IDisposable AcquirePresentationHold()
        {
            presentationClaim = true;
            var hold = bridge != null ? bridge.TryAcquire(this, panel, CancelPresentationInput, Interrupt) : null;
            if (hold == null) { presentationClaim = false; return null; }
            Refresh();
            return hold;
        }
        private void CancelPresentationInput()
        {
            // A short break cannot be dismissed while the passenger is outside; keep the owned hold.
            if (need != null && need.ReboardRequired) Retry();
        }
        private bool Interrupt() => need == null || need.Cancel("Restroom request interrupted by another HUD or ride operation.");
        private void Accept()
        {
            if (need == null || !bridge.IsOwnedBy(this) || !need.HasPendingRequest) return;
            if (!need.Accept()) feedback = "Restroom route unavailable.";
            Refresh();
        }
        private void Decline()
        {
            if (need == null || !bridge.IsOwnedBy(this) || !need.HasPendingRequest) return;
            need.Decline(); Refresh();
        }
        private void Retry() { need?.RetryReboard(); Refresh(); }

        public void Refresh()
        {
            if (overlay == null) return;
            if (need == null || !need.HasPendingRequest)
            {
                var previous = offerLease; offerLease = null;
                previous?.Dispose();
            }
            bool visible = need != null && bridge != null && bridge.IsOwnedBy(this) && bridge.IsOpen;
            overlay.gameObject.SetActive(visible);
            if (!visible) return;
            overlay.SetAsLastSibling();
            bool offered = need.HasPendingRequest && !presentationClaim;
            heading.text = offered ? (need.IsUrgent ? "RESTROOM REQUEST - URGENT" : "RESTROOM REQUEST") : "RESTROOM BREAK";
            message.text = offered ? (host.Session.Passenger?.passengerName ?? "Passenger") + "\n" + need.RequestText :
                need.ReboardRequired ? "The passenger cannot reboard yet." : "A short restroom break. Returning to the truck...";
            detail.text = offered ? feedback :
                need.ReboardRequired ? "Cab seat unavailable. Driving remains paused." : "";
            accept.SetActive(offered); decline.SetActive(offered); retry.SetActive(!offered && need.ReboardRequired);
        }
        private void LateUpdate() => Refresh();
        public void Detach(TruckTaxiPassengerBathroomNeed value)
        {
            if (need == null || need != value) return;
            need.RequestOffered -= OnRequestOffered;
            need.Changed -= Refresh;
            if (need.AcquirePresentationHold == AcquirePresentationHold) need.AcquirePresentationHold = null;
            need = null;
            var previous = offerLease; offerLease = null; previous?.Dispose();
            if (overlay != null) overlay.gameObject.SetActive(false);
        }
        private void OnDisable()
        {
            if (need == null) return;
            // A blocked return keeps its callbacks and pause lease alive until the actor can safely reboard.
            if (!need.Cancel("Passenger restroom panel disabled.")) { Refresh(); return; }
            Detach(need);
        }
        private void OnDestroy()
        {
            if (overlay != null) Destroy(overlay.gameObject);
        }
    }
}

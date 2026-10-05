using System;
using TMPro;
using UnityEngine;

namespace LWS.TruckTaxi
{
    [DisallowMultipleComponent]
    public sealed class TruckTaxiCompanionPickupRequestPanel : MonoBehaviour
    {
        private TruckTaxiCompanionPickupRequest request;
        private TruckTaxiBootstrap host;
        private TruckTaxiHud hud;
        private TruckTaxiHudModalBridge bridge;
        private RectTransform overlay, panel;
        private TextMeshProUGUI heading, message, detail;
        private IDisposable offerLease;
        private bool subscribed;
        private string feedback = "";
        public bool IsOpen => overlay != null && overlay.gameObject.activeSelf;
        public Transform FocusRoot => panel;
        public bool OwnsOfferHold => isActiveAndEnabled && request != null && request.HasOffer &&
            bridge != null && bridge.IsOwnedBy(this) && bridge.IsOpen;
        public bool CanOffer => isActiveAndEnabled && request != null && request.isActiveAndEnabled &&
            bridge != null && bridge.CanAcquire && host != null && host.Companions?.IsBusy != true &&
            host.Session?.State == TruckTaxiState.DrivingToDestination && host.Session.ActiveStop == null &&
            host.Session.PendingDiversion == null && host.GPS?.ActiveLayer == TruckTaxiNavigationLayer.RideFinal;

        public bool Initialize(TruckTaxiCompanionPickupRequest value, TruckTaxiBootstrap owner, TruckTaxiHud view)
        {
            if (value == null || owner == null || view == null || view.Root == null || view.UIInput == null ||
                view.Root.GetComponentInParent<Canvas>() == null) return false;
            if (request == value && host == owner && hud == view) { Subscribe(); Refresh(); return true; }
            if (request != null) Detach(request);
            request = value; host = owner; hud = view;
            bridge = view.GetComponent<TruckTaxiHudModalBridge>() ?? view.gameObject.AddComponent<TruckTaxiHudModalBridge>();
            bridge.Initialize(owner, view);
            if (overlay == null) Build();
            request.CanOfferPresentation = CanPresentOffer;
            Subscribe(); Refresh(); return true;
        }

        private bool CanPresentOffer() => CanOffer;
        private void Build()
        {
            overlay = TruckTaxiHud.Rect(hud.Root, "Companion pickup modal", Vector2.zero, Vector2.one);
            var blocker = overlay.gameObject.AddComponent<UnityEngine.UI.Image>();
            blocker.color = new Color(0, 0, 0, .62f); blocker.raycastTarget = true;
            panel = hud.Panel(overlay, "Companion pickup request", new Vector2(.22f, .29f), new Vector2(.78f, .71f));
            heading = hud.Text(panel, "Companion pickup heading", new Vector2(.055f, .78f), new Vector2(.945f, .94f), 30);
            message = hud.Text(panel, "Companion pickup message", new Vector2(.055f, .4f), new Vector2(.945f, .73f), 25);
            detail = hud.Text(panel, "Companion pickup detail", new Vector2(.055f, .25f), new Vector2(.945f, .39f), 20);
            foreach (var label in new[] { heading, message, detail })
            { label.richText = false; label.fontSizeMin = 16; label.characterSpacing = 0; }
            hud.Button(panel, "ACCEPT", new Vector2(.055f, .055f), new Vector2(.475f, .215f), Accept);
            hud.Button(panel, "DECLINE", new Vector2(.525f, .055f), new Vector2(.945f, .215f), Decline);
            overlay.gameObject.SetActive(false);
        }

        private void OnRequestOffered()
        {
            if (request == null || !request.HasOffer) return;
            if (!CanOffer) { request.Cancel(); return; }
            offerLease = bridge.TryAcquire(this, panel, Decline, Interrupt);
            if (offerLease == null) { request.Cancel(); return; }
            feedback = "";
            Refresh(); hud.UIInput.Focus(panel);
        }
        private bool Interrupt() { request?.Cancel(); return true; }
        private void Accept()
        {
            if (!OwnsOfferHold) return;
            // Coordinator authorizes only this open owned modal while the shared bridge keeps its pause lease.
            if (!request.Accept() && request.HasOffer) feedback = request.Feedback;
            Refresh();
        }
        private void Decline()
        {
            if (!OwnsOfferHold) return;
            request.Decline(); Refresh();
        }
        public void Refresh()
        {
            if (overlay == null) return;
            if (request == null || !request.HasOffer) ReleaseOffer();
            bool visible = OwnsOfferHold;
            overlay.gameObject.SetActive(visible);
            if (!visible) return;
            overlay.SetAsLastSibling();
            heading.text = "COMPANION PICKUP";
            string name = host.Session.Passenger?.passengerName;
            message.text = (string.IsNullOrWhiteSpace(name) ? "Passenger" : name) + "\nCould we pick up that companion nearby?";
            detail.text = string.IsNullOrEmpty(feedback) ?
                "Pickup bonus: " + TruckTaxiHud.Money(Mathf.Clamp(request.rewardFareCents, 0, 10000)) : feedback;
        }
        private void ReleaseOffer()
        { var previous = offerLease; offerLease = null; previous?.Dispose(); }
        private void Subscribe()
        {
            if (subscribed || !isActiveAndEnabled || request == null) return;
            request.RequestOffered += OnRequestOffered; request.Changed += Refresh; subscribed = true;
        }
        private void Unsubscribe()
        {
            if (!subscribed || request == null) return;
            request.RequestOffered -= OnRequestOffered; request.Changed -= Refresh; subscribed = false;
        }
        public void Detach(TruckTaxiCompanionPickupRequest value)
        {
            if (request == null || request != value) return;
            request.Cancel(); Unsubscribe();
            if (request.CanOfferPresentation == CanPresentOffer) request.CanOfferPresentation = null;
            request = null; ReleaseOffer();
            if (overlay != null) overlay.gameObject.SetActive(false);
        }
        private void OnEnable() { Subscribe(); Refresh(); }
        private void LateUpdate() => Refresh();
        private void OnDisable() { request?.Cancel(); Unsubscribe(); ReleaseOffer(); if (overlay != null) overlay.gameObject.SetActive(false); }
        private void OnDestroy() { if (request != null) Detach(request); if (overlay != null) Destroy(overlay.gameObject); }
    }
}

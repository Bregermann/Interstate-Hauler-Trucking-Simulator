using System;
using System.Collections;
using System.Collections.Generic;
using NWH.VehiclePhysics2;
using NWH.VehiclePhysics2.Modules.Trailer;
using UnityEngine;

namespace LWS.TruckTaxi
{
    // The real rig remains intact. Existing roadside/NWH code owns relocation and physics.
    [DisallowMultipleComponent]
    public sealed class TruckTaxiKaijuPlayerRecovery : MonoBehaviour
    {
        private TruckTaxiBootstrap host;
        private TruckTaxiRegionalWorld regional;
        private VehicleController vehicle;
        private TruckTaxiKaijuRendererLease presentation;
        private Camera eventCamera;
        private Coroutine towMonitor;
        private bool wasPaused;
        private Vector3 threatOrigin;
        public bool Captured => presentation != null;
        public string LastFailure { get; private set; } = "";

        public bool Initialize(TruckTaxiBootstrap boot)
        {
            if (host != null) return host == boot;
            if (boot == null || boot.Player == null || boot.Session == null || boot.Roadside == null) return false;
            vehicle = boot.Player.GetComponent<VehicleController>();
            if (vehicle == null || boot.Player.UprightRecoveryController == null) return false;
            host = boot;
            regional = boot.GetComponent<TruckTaxiRegionalWorld>();
            return true;
        }

        public bool TryBegin(TruckTaxiKaijuCaptureContext context)
        {
            if (!isActiveAndEnabled || host == null || !host.Ready || host.Paused || Captured || towMonitor != null ||
                context.Hand == null || context.Mouth == null || host.Roadside == null || !host.Roadside.CanRequest)
                return false;
            threatOrigin = context.Origin;
            if (!PrepareSafeTow()) return false;
            LastFailure = "";
            wasPaused = host.Paused;
            host.Session.AcquireOfferSuppression(this);
            try
            {
                host.Session.CancelTemporaryActionsForRecovery();
                presentation = new TruckTaxiKaijuRendererLease(GetRigRenderers());
                host.SetPaused(true);
                CreateEventCamera(context);
                return true;
            }
            catch
            {
                Complete(false);
                host.Session.ReleaseOfferSuppression(this);
                throw;
            }
        }

        public Renderer[] GetRigRenderers()
        {
            if (host == null || host.Player == null) return Array.Empty<Renderer>();
            var renderers = new List<Renderer>(host.Player.GetComponentsInChildren<Renderer>(true));
            var hitch = vehicle != null ? vehicle.GetComponent<TrailerHitchModuleWrapper>() : null;
            var trailer = hitch != null && hitch.module.attached ? hitch.module.attachedTrailerModule?.vehicleController : null;
            if (trailer != null) renderers.AddRange(trailer.GetComponentsInChildren<Renderer>(true));
            return renderers.ToArray();
        }

        private bool PrepareSafeTow()
        {
            if (!host.Roadside.PrepareQuote()) { LastFailure = "No roadside recovery is available."; return false; }
            var destination = host.Roadside.QuotedDestination;
            if (destination == null || !destination.isActiveAndEnabled ||
                (regional != null && !regional.IsPositionAvailable(destination.RecoveryPosition)) ||
                Vector3.ProjectOnPlane(destination.RecoveryPosition - threatOrigin, Vector3.up).sqrMagnitude < 180 * 180)
            { LastFailure = "No loaded recovery bay outside the kaiju danger area."; return false; }
            return true;
        }

        public void Complete(bool swallowed)
        {
            if (!Captured) return;
            presentation.Dispose();
            presentation = null;
            if (eventCamera != null) { eventCamera.enabled = false; Destroy(eventCamera.gameObject); }
            eventCamera = null;
            if (host == null) return;
            if (swallowed && host.Roadside != null && host.Roadside.CanRequest && PrepareSafeTow())
            {
                var destination = host.Roadside.QuotedDestination;
                if (host.Roadside.ConfirmTow())
                {
                    // Roadside assistance owns pause from this point, including its failure cleanup.
                    towMonitor = StartCoroutine(ObserveTow(destination));
                    return;
                }
            }
            host.Session?.ReleaseOfferSuppression(this);
            if (host.Roadside?.IsRecovering != true && host.Fuel?.IsRescuing != true) host.SetPaused(wasPaused);
            if (swallowed) ReportFailure("Kaiju recovery could not start; truck control restored without a capture penalty.");
        }

        private IEnumerator ObserveTow(TruckTaxiServicePoint destination)
        {
            while (host != null && host.Roadside != null && host.Roadside.IsRecovering) yield return null;
            towMonitor = null;
            if (host == null) yield break;
            host.Session.ReleaseOfferSuppression(this);
            bool recovered = host.Player != null && destination != null &&
                Vector3.Distance(host.Player.transform.position, destination.RecoveryPosition) < 20 &&
                Vector3.Dot(host.Player.transform.up, Vector3.up) > .8f;
            if (recovered)
            {
                host.Session.DiscardTeleportDistance();
                // Onboard passengers follow the session's existing safe-cancellation flow.
                host.Session.FailRide("Kaiju captured the truck.");
            }
            else ReportFailure("Roadside recovery did not complete. Truck visuals and control have been restored.");
        }

        public void ApplyFireDamage(Vector3 position, float requestedFraction)
        {
            if (host == null || host.Paused || Captured || vehicle == null || host.Roadside?.IsRecovering == true ||
                !float.IsFinite(requestedFraction) || requestedFraction <= 0) return;
            var engine = vehicle.powertrain.engine;
            float previous = engine.Damage;
            engine.Damage = Mathf.Max(previous, Mathf.Min(.9f, previous + Mathf.Min(.08f, requestedFraction)));
            host.Roadside?.RefreshCondition();
        }

        private void CreateEventCamera(TruckTaxiKaijuCaptureContext context)
        {
            var source = Camera.main;
            if (source == null) return;
            var go = new GameObject("Kaiju capture camera");
            go.transform.SetParent(transform, false);
            eventCamera = go.AddComponent<Camera>();
            eventCamera.CopyFrom(source);
            eventCamera.depth = source.depth + 10;
            eventCamera.farClipPlane = Mathf.Max(2000, source.farClipPlane);
            Vector3 away = Vector3.ProjectOnPlane(host.Player.transform.position - context.Origin, Vector3.up).normalized;
            if (away.sqrMagnitude < .01f) away = Vector3.back;
            go.transform.position = context.Origin + away * 120 + Vector3.up * 48;
            go.transform.LookAt(Vector3.Lerp(context.Hand.position, context.Mouth.position, .5f));
        }

        private void ReportFailure(string message)
        {
            LastFailure = message;
            host?.hud?.Alerts?.Publish("kaiju:recovery", message, TruckTaxiAlertPriority.Critical, 10);
        }

        private void LateUpdate() { presentation?.Suppress(); }
        private void OnDisable()
        {
            Complete(false);
            if (towMonitor != null) { StopCoroutine(towMonitor); towMonitor = null; }
            if (host != null) host.Session?.ReleaseOfferSuppression(this);
        }
        private void OnDestroy() { OnDisable(); }
    }

    // Exact renderer-state restoration, including interruption and previously hidden cab meshes.
    public sealed class TruckTaxiKaijuRendererLease : IDisposable
    {
        private Renderer[] renderers;
        private readonly bool[] enabledStates;
        public TruckTaxiKaijuRendererLease(Renderer[] targets)
        {
            renderers = (Renderer[])(targets ?? Array.Empty<Renderer>()).Clone();
            enabledStates = new bool[renderers.Length];
            for (int i = 0; i < renderers.Length; i++) enabledStates[i] = renderers[i] != null && renderers[i].enabled;
            Suppress();
        }
        public void Suppress()
        {
            if (renderers == null) return;
            foreach (var renderer in renderers) if (renderer != null) renderer.enabled = false;
        }
        public void Dispose()
        {
            if (renderers == null) return;
            for (int i = 0; i < renderers.Length; i++) if (renderers[i] != null) renderers[i].enabled = enabledStates[i];
            renderers = null;
        }
    }
}

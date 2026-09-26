using System;
using System.Collections;
using NWH.VehiclePhysics2;
using NWH.VehiclePhysics2.Modules.Fuel;
using NWH.VehiclePhysics2.Modules.Trailer;
using UnityEngine;
using UnityEngine.Events;

namespace LWS.TruckTaxi
{
    // NWH owns consumption/engine gating. Taxi owns purchases and the arcade rescue.
    [DisallowMultipleComponent]
    public sealed class TruckTaxiFuelController : MonoBehaviour
    {
        [Header("Taxi fuel / NWH module")]
        [Min(1)] public float tankLiters = 80;
        [Min(.01f), Tooltip("Multiplies NWH power-based consumption; 12 gives a sandbox-sized tank lifetime.")]
        public float consumptionMultiplier = 12;
        [Header("Cartoon rescue")]
        [Range(2, 10)] public float launchSpeed = 6;
        [Range(.2f, 2)] public float launchSeconds = 1;
        public AudioClip explosionClip, evilLaughClip;
        public FuelModule Fuel { get; private set; }
        public float Fraction => Fuel == null ? 1 : Fuel.FuelPercentage;
        public bool IsRescuing { get; private set; }
        public float FadeAlpha { get; private set; }
        public int Rescues { get; private set; }
        public long TotalFuelCharges { get; private set; }
        public string Feedback { get; private set; } = "";
        public event Action Rescued;
        private TruckTaxiBootstrap host;
        private VehicleController vehicle;
        private Rigidbody body;
        private FuelModuleWrapper wrapper;
        private bool ownsModule;
        private AudioSource sound;
        private AudioClip synthesizedLaugh;
        private ParticleSystem burst;
        private Material burstMaterial;
        private double unbilledCents;
        private float refuelElapsed;

        public void Initialize(TruckTaxiBootstrap owner)
        {
            if (host != null) return;
            host = owner; vehicle = owner.Player.GetComponent<VehicleController>(); body = vehicle.vehicleRigidbody;
            wrapper = vehicle.GetComponent<FuelModuleWrapper>(); ownsModule = wrapper == null;
            if (ownsModule) wrapper = vehicle.gameObject.AddComponent<FuelModuleWrapper>();
            Fuel = wrapper.module;
            if (ownsModule)
            {
                Fuel.capacity = tankLiters; Fuel.amount = tankLiters; Fuel.consumptionMultiplier = consumptionMultiplier;
                Fuel.onOutOfFuel ??= new UnityEvent();
                if (!vehicle.moduleManager.Components.Contains(Fuel)) vehicle.moduleManager.AddAndOnboardNewComponent(Fuel);
                Fuel.VC_Enable(false);
            }
            Fuel.onOutOfFuel ??= new UnityEvent(); Fuel.onOutOfFuel.AddListener(BeginRescue);
            sound = gameObject.AddComponent<AudioSource>(); sound.playOnAwake = false; sound.spatialBlend = 0;
            owner.Audio.Route(sound, TruckTaxiAudioCategory.World);
            if (evilLaughClip == null) synthesizedLaugh = CreateCartoonCackle();
        }
        private void Update()
        {
            if (Fuel == null || host.Paused || IsRescuing) return;
            if (Fuel.amount <= 0) { BeginRescue(); return; }
            var station = TruckTaxiGasStationPoint.Nearest(body.position);
            if (station == null || !station.CanRefuel(body.position, body.linearVelocity.magnitude) || Fuel.amount >= Fuel.capacity)
            { Feedback = ""; refuelElapsed=0; return; }
            refuelElapsed+=Time.deltaTime;
            if(refuelElapsed<.25f) return;
            float liters = Mathf.Min(Fuel.capacity - Fuel.amount, station.litersPerSecond * refuelElapsed);
            refuelElapsed=0;
            double price = unbilledCents + liters * station.centsPerLiter;
            long charge = (long)Math.Floor(price);
            if (!host.Session.TrySpend(charge)) { Feedback = "NOT ENOUGH CASH TO REFUEL"; return; }
            unbilledCents = price - charge; TotalFuelCharges += charge; Fuel.amount += liters;
            Feedback = "REFUELING  " + Mathf.RoundToInt(Fraction * 100) + "%";
        }
        public static long RescueCost(float capacity, int centsPerLiter) =>
            (long)Math.Ceiling(Math.Max(0, capacity) * Math.Max(0, centsPerLiter) * 2d);
        public bool RouteToGas()
        {
            var station = body != null ? TruckTaxiGasStationPoint.Nearest(body.position) : null;
            if (station == null) return false;
            host.GPS.SetServiceDestination(station.stableId, station.displayName, station.Position); return true;
        }
        private void BeginRescue()
        {
            if (host == null || IsRescuing || host.Paused) return;
            StartCoroutine(Rescue());
        }
        private IEnumerator Rescue()
        {
            IsRescuing = true;
            try
            {
                Feedback = "OUT OF FUEL";
                vehicle.powertrain.engine.StopEngine();
                CreateBurst(); burst.transform.position = body.worldCenterOfMass; burst.Play();
                if (explosionClip != null) sound.PlayOneShot(explosionClip, .8f);
                sound.PlayOneShot(evilLaughClip != null ? evilLaughClip : synthesizedLaugh, .65f);
                body.linearVelocity = Vector3.up * launchSpeed; body.angularVelocity = Vector3.zero;
                var trailer = AttachedTrailerBody();
                if (trailer != null) { trailer.linearVelocity = body.linearVelocity; trailer.angularVelocity = Vector3.zero; }
                yield return new WaitForSeconds(launchSeconds);
                for (float t = 0; t < .35f; t += Time.unscaledDeltaTime) { FadeAlpha = Mathf.Clamp01(t / .35f); yield return null; }
                FadeAlpha = 1;
                var station = TruckTaxiGasStationPoint.Nearest(body.position);
                if (station != null)
                {
                    Vector3 offset = station.RecoveryPosition + Vector3.up * 1.6f - body.position;
                    MoveBody(body, body.position + offset);
                    if (trailer != null) MoveBody(trailer, trailer.position + offset);
                    Physics.SyncTransforms();
                    host.Player.UprightRecoveryController.RequestResetUpright("Taxi out-of-fuel combination rescue");
                    long cost = RescueCost(Fuel.capacity, station.centsPerLiter);
                    host.Session.ChargeService(cost); TotalFuelCharges += cost;
                    Feedback = "RESCUED TO " + station.displayName + "  " + TruckTaxiHud.Money(cost);
                }
                else
                {
                    host.Player.UprightRecoveryController.RequestResetUpright("Taxi missing gas-station fallback");
                    Debug.LogError("Truck Taxi has no gas station. Fuel restored in place to avoid stranding the player; author TT_GAS_ anchors.", this);
                }
                Fuel.amount = Fuel.capacity; vehicle.powertrain.engine.StartEngine();
                host.Session.DiscardTeleportDistance(); Rescues++; Rescued?.Invoke();
                yield return new WaitForSecondsRealtime(.3f);
                for (float t = .35f; t > 0; t -= Time.unscaledDeltaTime) { FadeAlpha = t / .35f; yield return null; }
            }
            finally { FadeAlpha = 0; IsRescuing = false; }
        }
        private Rigidbody AttachedTrailerBody()
        {
            var hitch = vehicle.GetComponent<TrailerHitchModuleWrapper>();
            return hitch != null && hitch.module.attached ? hitch.module.attachedTrailerModule?.vehicleController?.vehicleRigidbody : null;
        }
        private static void MoveBody(Rigidbody target, Vector3 position)
        { target.linearVelocity = target.angularVelocity = Vector3.zero; target.position = position; target.transform.position = position; }
        private void CreateBurst()
        {
            if (burst != null) return;
            var go = new GameObject("Taxi cartoon fuel burst"); go.transform.SetParent(transform, false);
            burst = go.AddComponent<ParticleSystem>(); burst.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = burst.main; main.loop = false; main.playOnAwake = false; main.duration = .3f;
            main.startLifetime = .7f; main.startSpeed = 12; main.startSize = 1.4f; main.maxParticles = 36;
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1,.8f,.05f), new Color(1,.17f,.02f));
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = burst.emission; emission.rateOverTime = 0; emission.SetBursts(new[] { new ParticleSystem.Burst(0, 32) });
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader != null) { burstMaterial = new Material(shader); go.GetComponent<ParticleSystemRenderer>().sharedMaterial = burstMaterial; }
        }
        private static AudioClip CreateCartoonCackle()
        {
            // Non-speech placeholder SFX; no passenger voice or external generation pipeline.
            const int rate = 22050; var samples = new float[rate * 2];
            for (int i = 0; i < samples.Length; i++)
            {
                float t = i / (float)rate, pulse = Mathf.Repeat(t, .32f) / .32f;
                float envelope = Mathf.Sin(pulse * Mathf.PI) * (pulse < .75f ? 1 : 0) * Mathf.Clamp01((2-t)*2);
                float phase = 2 * Mathf.PI * (105*t + 9*Mathf.Sin(t*5));
                samples[i] = envelope * .22f * (Mathf.Sin(phase) + .4f*Mathf.Sin(phase*2.03f));
            }
            var clip = AudioClip.Create("Taxi cartoon cackle (placeholder)", samples.Length, 1, rate, false); clip.SetData(samples, 0); return clip;
        }
        private void OnDestroy()
        {
            if (Fuel != null) Fuel.onOutOfFuel?.RemoveListener(BeginRescue);
            if (ownsModule && vehicle != null) { Fuel.VC_Disable(false); vehicle.moduleManager.Components.Remove(Fuel); if (wrapper != null) Destroy(wrapper); }
            if (synthesizedLaugh != null) Destroy(synthesizedLaugh);
            if (burstMaterial != null) Destroy(burstMaterial);
        }
    }
}

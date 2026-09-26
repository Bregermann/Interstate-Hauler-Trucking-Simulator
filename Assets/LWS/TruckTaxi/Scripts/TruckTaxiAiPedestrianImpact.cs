using System;
using UnityEngine;

namespace LWS.TruckTaxi
{
    // World-only collision report. The player observer alone owns session scoring.
    public sealed class TruckTaxiAiPedestrianImpact : MonoBehaviour
    {
        public readonly struct Impact
        {
            public readonly string PedestrianId;
            public readonly string VehicleId;
            public readonly Vector3 Position;
            public readonly float Speed;
            public Impact(string pedestrianId, string vehicleId, Vector3 position, float speed)
            { PedestrianId = pedestrianId; VehicleId = vehicleId; Position = position; Speed = speed; }
        }

        public static event Action<Impact> WorldImpact;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetEvent() { WorldImpact = null; }
        [SerializeField] private AudioSource impactAudio;
        [SerializeField] private AudioClip impactClip;
        private TruckTaxiPedestrian pedestrian;
        private static AudioClip fallbackImpact;

        private void Awake()
        {
            pedestrian = GetComponent<TruckTaxiPedestrian>();
            if (impactAudio == null) impactAudio = gameObject.AddComponent<AudioSource>();
            impactAudio.playOnAwake = false;
            impactAudio.spatialBlend = 1;
            impactAudio.maxDistance = 28;
            impactAudio.volume = .45f;
            TruckTaxiAudioController.Instance?.Route(impactAudio, TruckTaxiAudioCategory.World);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (pedestrian == null || pedestrian.IsRagdoll) return;
            var target = collision.collider.GetComponentInParent<TruckTaxiImpactTarget>();
            if (target == null || target.kind != TaxiImpactKind.Traffic) return;
            Vector3 relative = collision.relativeVelocity;
            Vector3 contact = collision.contactCount > 0 ? collision.GetContact(0).point : transform.position;
            if (!pedestrian.TryStrike(relative, contact)) return;
            pedestrian.MarkHitEventSent();
            impactAudio.PlayOneShot(impactClip != null ? impactClip : FallbackImpact());
            WorldImpact?.Invoke(new Impact(pedestrian.PedestrianId, target.targetId, contact, relative.magnitude));
        }

        private static AudioClip FallbackImpact()
        {
            if (fallbackImpact != null) return fallbackImpact;
            const int sampleRate = 22050, sampleCount = 3308;
            var samples = new float[sampleCount];
            for (int i=0;i<sampleCount;i++)
            {
                float t = (float)i/sampleRate;
                float envelope = Mathf.Exp(-32*t);
                samples[i] = envelope*(.72f*Mathf.Sin(2*Mathf.PI*(95-180*t)*t) +
                    .12f*Mathf.Sin(2*Mathf.PI*47*t));
            }
            fallbackImpact = AudioClip.Create("Taxi pedestrian impact thud", sampleCount, 1, sampleRate, false);
            fallbackImpact.SetData(samples, 0);
            return fallbackImpact;
        }
    }
}

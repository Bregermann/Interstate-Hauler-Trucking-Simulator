using System;
using UnityEngine;

namespace LWS.TruckExtremeHauling
{
    public sealed class LoadSensitiveIce : MonoBehaviour
    {
        public float thickness = .45f, bearingMass = 22000, damageRate = .18f;
        public Collider surface;
        public Renderer iceRenderer;
        public float Stress { get; private set; }
        public bool Broken { get; private set; }
        public event Action<LoadSensitiveIce> Breakthrough;
        readonly System.Collections.Generic.Dictionary<Rigidbody, float> loads = new System.Collections.Generic.Dictionary<Rigidbody, float>();
        MaterialPropertyBlock properties;
        bool visibleBeforeBreak=true;
        float load;
        public void RegisterSupportedLoad(Rigidbody body, float fraction)
        {
            if (Broken || body == null || fraction <= 0) return;
            float supported = body.mass * Mathf.Clamp01(fraction) * (1 + body.linearVelocity.sqrMagnitude * .004f);
            loads.TryGetValue(body, out float prior); loads[body] = prior + supported;
        }
        void Awake() { if (surface == null) surface = GetComponent<Collider>(); properties = new MaterialPropertyBlock();if(iceRenderer!=null)visibleBeforeBreak=iceRenderer.enabled; }
        void OnCollisionStay(Collision collision)
        {
            if (Broken || collision.rigidbody == null) return;
            var body = collision.rigidbody;
            float impulseLoad = collision.impulse.magnitude / Mathf.Max(Time.fixedDeltaTime * Physics.gravity.magnitude, .001f);
            loads[body] = Mathf.Max(body.mass * .1f, impulseLoad);
        }
        public void Simulate(float dt)
        {
            if (Broken) return;
            load = 0; foreach (var pair in loads) if (pair.Key != null) load += pair.Value;
            loads.Clear();
            float capacity = Mathf.Max(1, bearingMass * thickness * thickness / (.45f * .45f));
            float ratio = load / capacity;
            Stress = Mathf.Clamp01(Stress + (ratio > .65f ? (ratio - .65f) * damageRate : -.008f) * dt);
            if (iceRenderer != null)
            {
                if (properties == null) properties = new MaterialPropertyBlock();
                properties.SetColor("_BaseColor", Color.Lerp(new Color(.48f, .76f, .86f), new Color(.03f, .13f, .2f), Stress));
                iceRenderer.SetPropertyBlock(properties);
            }
            if (Stress >= 1) { Broken = true; if (surface != null) surface.enabled = false;if(iceRenderer!=null)iceRenderer.enabled=false; Breakthrough?.Invoke(this); }
        }
        public void ResetIce() { loads.Clear(); Stress = 0; Broken = false; if (surface != null) surface.enabled = true;if(iceRenderer!=null)iceRenderer.enabled=visibleBeforeBreak; }
    }
}

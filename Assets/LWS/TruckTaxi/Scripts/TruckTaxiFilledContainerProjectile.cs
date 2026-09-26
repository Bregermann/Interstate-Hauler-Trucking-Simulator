using UnityEngine;

namespace LWS.TruckTaxi
{
    [RequireComponent(typeof(Rigidbody), typeof(Collider))]
    public sealed class TruckTaxiFilledContainerProjectile : MonoBehaviour
    {
        private AudioClip impactClip;
        private PhysicsMaterial bounceMaterial;
        private bool impacted;
        public void Configure(AudioClip clip)
        {
            impactClip = clip;
            bounceMaterial = new PhysicsMaterial("Taxi container bounce") { bounciness = .32f, bounceCombine = PhysicsMaterialCombine.Maximum };
            GetComponent<Collider>().material = bounceMaterial;
            Destroy(gameObject, 20);
        }
        private void OnCollisionEnter(Collision collision)
        {
            if (impacted || impactClip == null || collision.relativeVelocity.sqrMagnitude < 2) return;
            impacted = true;
            var source = gameObject.AddComponent<AudioSource>();
            source.spatialBlend = 1;
            source.maxDistance = 18;
            source.playOnAwake = false;
            TruckTaxiAudioController.Instance?.Route(source, TruckTaxiAudioCategory.World);
            source.PlayOneShot(impactClip);
        }
        private void OnDestroy() { if (bounceMaterial != null) Destroy(bounceMaterial); }
    }
}

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace LWS.TruckTaxi
{
    // An owned, zero-weight-when-idle Volume. No edits to weather or shared VolumeProfiles.
    [DisallowMultipleComponent]
    public sealed class TruckTaxiEffectPresentation : MonoBehaviour
    {
        private TruckTaxiTemporaryEffects effects;
        private Volume volume;
        private VolumeProfile profile;
        private ColorAdjustments color;
        private UniversalAdditionalCameraData cameraData;
        private bool originalPostProcessing;
        private LayerMask originalMask;
        private Camera activeCamera;
        public float Intensity => volume != null ? volume.weight : 0;
        public void Initialize(TruckTaxiTemporaryEffects state)
        {
            effects = state;
            if (volume != null) return;
            var root = new GameObject("Taxi Temporary Item Volume"); root.transform.SetParent(transform, false);
            volume = root.AddComponent<Volume>(); volume.isGlobal = true; volume.priority = 200; volume.weight = 0;
            profile = ScriptableObject.CreateInstance<VolumeProfile>(); volume.sharedProfile = profile;
            var settings = TruckTaxiTemporaryEffects.Profile(TruckTaxiTemporaryEffectKind.MysteryMushroom);
            color = profile.Add<ColorAdjustments>();
            color.saturation.Override(settings.Saturation); color.contrast.Override(settings.Contrast); color.hueShift.Override(0);
            var bloom = profile.Add<Bloom>(); bloom.intensity.Override(settings.BloomIntensity); bloom.threshold.Override(.9f);
            profile.Add<ChromaticAberration>().intensity.Override(settings.ChromaticAberration);
        }
        private void LateUpdate()
        {
            if (effects == null || volume == null) return;
            var snapshot = effects.GetSnapshot(TruckTaxiTemporaryEffectKind.MysteryMushroom);
            volume.weight = snapshot.Intensity01;
            if (volume.weight <= 0) { RestoreCamera(); return; }
            var current = Camera.main;
            if (current != activeCamera)
            {
                RestoreCamera(); activeCamera = current;
                if (current != null && current.TryGetComponent(out cameraData))
                {
                    originalPostProcessing = cameraData.renderPostProcessing; originalMask = cameraData.volumeLayerMask;
                    cameraData.renderPostProcessing = true; cameraData.volumeLayerMask |= 1 << volume.gameObject.layer;
                }
            }
            color.hueShift.value = Mathf.Sin(Time.time * .6f) * 12;
        }
        private void RestoreCamera()
        {
            if (cameraData != null) { cameraData.renderPostProcessing = originalPostProcessing; cameraData.volumeLayerMask = originalMask; }
            activeCamera = null; cameraData = null;
        }
        private void OnDisable() { if (volume != null) volume.weight = 0; RestoreCamera(); }
        private void OnDestroy()
        {
            RestoreCamera();
            if (volume != null) Destroy(volume.gameObject);
            if (profile != null) { foreach (var component in profile.components) if (component != null) Destroy(component); Destroy(profile); }
        }
    }
}

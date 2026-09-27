using System.Collections.Generic;
using NWH.Common.Cameras;
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
        private struct CameraBaseline
        {
            public bool postProcessing;
            public LayerMask volumeMask;
            public bool created;
        }
        private readonly Dictionary<UniversalAdditionalCameraData, CameraBaseline> cameraBaselines =
            new Dictionary<UniversalAdditionalCameraData, CameraBaseline>();
        private Vignette vignette;
        private LensDistortion lens;
        private CameraChanger cameraChanger;
        private const int EffectLayer = 31; // Unassigned project layer; exclude map/preview cameras.
        public float Intensity => volume != null ? volume.weight : 0;
        public void Initialize(TruckTaxiTemporaryEffects state)
        {
            effects = state;
            if (volume != null) return;
            var root = new GameObject("Taxi Temporary Item Volume"); root.layer = EffectLayer; root.transform.SetParent(transform, false);
            volume = root.AddComponent<Volume>(); volume.isGlobal = true; volume.priority = 200; volume.weight = 0;
            profile = ScriptableObject.CreateInstance<VolumeProfile>(); volume.sharedProfile = profile;
            var settings = TruckTaxiTemporaryEffects.Profile(TruckTaxiTemporaryEffectKind.MysteryMushroom);
            color = profile.Add<ColorAdjustments>();
            color.saturation.Override(settings.Saturation); color.contrast.Override(settings.Contrast); color.hueShift.Override(0);
            var bloom = profile.Add<Bloom>(); bloom.intensity.Override(settings.BloomIntensity); bloom.threshold.Override(.65f);
            profile.Add<ChromaticAberration>().intensity.Override(settings.ChromaticAberration);
            lens = profile.Add<LensDistortion>(); lens.intensity.Override(-.12f); lens.scale.Override(1.04f);
            vignette = profile.Add<Vignette>(); vignette.intensity.Override(.22f); vignette.smoothness.Override(.5f);
        }
        private void LateUpdate()
        {
            if (effects == null || volume == null) return;
            var snapshot = effects.GetSnapshot(TruckTaxiTemporaryEffectKind.MysteryMushroom);
            volume.weight = snapshot.Intensity01;
            if (volume.weight <= 0) { RestoreCameras(); return; }
            if (cameraChanger == null && TruckTaxiBootstrap.Instance?.Player != null)
                cameraChanger = TruckTaxiBootstrap.Instance.Player.GetComponentInChildren<CameraChanger>(true);
            if (cameraChanger != null && cameraChanger.cameras != null && cameraChanger.cameras.Count > 0)
            {
                foreach (var cameraObject in cameraChanger.cameras)
                    if (cameraObject != null) BindCamera(cameraObject.GetComponent<Camera>());
            }
            else BindCamera(Camera.main);
            color.hueShift.value = Mathf.Sin(Time.time * .48f) * 24f;
            lens.intensity.value = -.12f + Mathf.Sin(Time.time * 1.1f) * .025f;
            vignette.intensity.value = .22f + Mathf.Sin(Time.time * 1.4f) * .035f;
        }
        private void BindCamera(Camera camera)
        {
            if (camera == null) return;
            var data = camera.GetComponent<UniversalAdditionalCameraData>();
            bool created = data == null;
            if (created) data = camera.gameObject.AddComponent<UniversalAdditionalCameraData>();
            if (!cameraBaselines.ContainsKey(data))
                cameraBaselines.Add(data, new CameraBaseline {
                    postProcessing = data.renderPostProcessing, volumeMask = data.volumeLayerMask, created = created });
            data.renderPostProcessing = true;
            data.volumeLayerMask |= 1 << EffectLayer;
        }
        private void RestoreCameras()
        {
            foreach (var pair in cameraBaselines)
            {
                if (pair.Key == null) continue;
                if (pair.Value.created) Destroy(pair.Key);
                else
                {
                    pair.Key.renderPostProcessing = pair.Value.postProcessing;
                    pair.Key.volumeLayerMask = pair.Value.volumeMask;
                }
            }
            cameraBaselines.Clear();
        }
        private void OnDisable() { if (volume != null) volume.weight = 0; RestoreCameras(); }
        private void OnDestroy()
        {
            RestoreCameras();
            if (volume != null) Destroy(volume.gameObject);
            if (profile != null) { foreach (var component in profile.components) if (component != null) Destroy(component); Destroy(profile); }
        }
    }
}

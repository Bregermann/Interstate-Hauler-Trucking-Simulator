using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace LWS.TruckTaxi
{
    // World-camera Volume only. The existing Heat screen-space HUD is never darkened.
    [DisallowMultipleComponent]
    public sealed class TruckTaxiFatiguePresentation : MonoBehaviour
    {
        [SerializeField] private TruckTaxiFatigueVisualMode visualMode;
        private TruckTaxiBootstrap host;
        private TruckTaxiDriverNeedsState state;
        private TruckTaxiEffectPresentation cameras;
        private Volume volume;
        private VolumeProfile profile;
        private ColorAdjustments color;
        private Vignette vignette;
        private DepthOfField focus;
        private Sequence pulse;
        private float nextPulse = 50;
        private readonly System.Random random = new System.Random();
        public float Intensity => volume != null ? volume.weight : 0;
        public TruckTaxiFatigueVisualMode VisualMode
        {
            get => visualMode;
            set { if (visualMode == value) return; visualMode = value; ClearPresentation(); }
        }
        public void Initialize(TruckTaxiBootstrap owner, TruckTaxiDriverNeedsState needs, TruckTaxiEffectPresentation cameraAdapter)
        {
            host = owner; state = needs; cameras = cameraAdapter;
            if (volume != null) return;
            var root = new GameObject("Taxi Fatigue World Volume");
            root.layer = 31; root.transform.SetParent(transform, false);
            volume = root.AddComponent<Volume>(); volume.isGlobal = true; volume.priority = 210; volume.weight = 0;
            profile = ScriptableObject.CreateInstance<VolumeProfile>(); volume.sharedProfile = profile;
            color = profile.Add<ColorAdjustments>(); color.postExposure.Override(0);
            vignette = profile.Add<Vignette>(); vignette.intensity.Override(0); vignette.smoothness.Override(.9f);
            focus = profile.Add<DepthOfField>(); focus.mode.Override(DepthOfFieldMode.Gaussian);
            focus.gaussianStart.Override(0); focus.gaussianEnd.Override(8); focus.gaussianMaxRadius.Override(.5f);
        }
        private void Update()
        {
            if (volume == null || state == null) return;
            if (host == null || !host.Ready || host.Paused || host.hud?.NormalHudSuppressed == true ||
                host.Session.State == TruckTaxiState.Inactive || state.Fatigue < TruckTaxiFatigueRules.MicrosleepThreshold)
            { ClearPresentation(); return; }
            if (pulse != null && pulse.IsActive()) return;
            nextPulse -= Time.deltaTime;
            if (nextPulse > 0) return;
            float severity = TruckTaxiFatigueRules.VisualSeverity(state.Fatigue);
            bool reduced = visualMode == TruckTaxiFatigueVisualMode.Reduced;
            color.postExposure.value = reduced ? -.45f : -Mathf.Lerp(.65f, 7, severity * severity);
            vignette.intensity.value = reduced ? .22f : Mathf.Lerp(.25f, .65f, severity);
            focus.gaussianMaxRadius.value = reduced ? .5f : Mathf.Lerp(.6f, 1.4f, severity);
            focus.active = !reduced;
            cameras?.SetFatiguePresentationActive(true);
            // Slow close/open, one short hold, then at least eight seconds of clear view.
            pulse = DOTween.Sequence();
            pulse.Append(DOTween.To(() => volume.weight, value => volume.weight = value, 1, .75f).SetEase(Ease.InOutSine));
            pulse.AppendInterval(reduced ? .1f : Mathf.Lerp(.08f, .2f, severity));
            pulse.Append(DOTween.To(() => volume.weight, value => volume.weight = value, 0, 1).SetEase(Ease.InOutSine));
            pulse.OnComplete(() => { pulse = null; cameras?.SetFatiguePresentationActive(false); });
            nextPulse = TruckTaxiFatigueRules.MinimumEffectInterval(state.Fatigue) * (1 + (float)random.NextDouble() * .6f);
        }
        public void ClearPresentation()
        {
            pulse?.Kill(); pulse = null;
            if (volume != null) volume.weight = 0;
            cameras?.SetFatiguePresentationActive(false);
            nextPulse = state == null ? 50 : TruckTaxiFatigueRules.MinimumEffectInterval(state.Fatigue);
        }
        private void OnDisable() => ClearPresentation();
        private void OnDestroy()
        {
            ClearPresentation();
            if (volume != null) Destroy(volume.gameObject);
            if (profile == null) return;
            foreach (var component in profile.components) if (component != null) Destroy(component);
            Destroy(profile);
        }
    }
}

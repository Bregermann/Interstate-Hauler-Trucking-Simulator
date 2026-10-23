using DigitalRuby.WeatherMaker;
using UnityEngine;

namespace LWS.TruckSports
{
    // Explicit scene binding; no runtime scene search, weather replacement, or venue regeneration.
    [DefaultExecutionOrder(100)]
    public sealed class SportsMatchTime : MonoBehaviour
    {
        public WeatherMakerDayNightCycleManagerScript cycle;
        public WeatherMakerFullScreenCloudsScript clouds;
        public WeatherMakerSkySphereScript skySphere;
        [Tooltip("Elevated legacy venues: freeze the vendor ground gradient at the selected match time, including when its sun lookup is inactive.")]
        public bool coordinateGroundColor;
        [Range(0, 1)] public float nightCloudAmbientMultiplier = .18f;
        public WeatherMakerCelestialObjectScript perspectiveSun, orthographicSun;
        [Tooltip("Legacy fixed directional fill, replaced by the vendor sun/moon outdoors.")]
        public Light daylightFill;
        public bool preserveInteriorAmbient;
        [HideInInspector] public int setupLayoutVersion;
        [Tooltip("Only authored venue lamps. Concert/show lighting must not be assigned here.")]
        public Light[] nightLights = System.Array.Empty<Light>();
        public SportsTimeChoice Applied { get; private set; }
        public int Revision { get; private set; }
        WeatherMakerDayNightCycleProfileScript ownedProfile;
        WeatherMakerDayNightCycleProfileScript previousProfile;
        string matchId;
        float[] daylightIntensity;
        bool fillWasEnabled;
        Gradient previousCloudAmbient, ownedCloudAmbient;
        WeatherMakerSkyProfileScript previousSkyProfile, ownedSkyProfile;

        public void Configure(int seed, string currentMatchId)
        {
            // A heat/attempt may reconfigure weather. Random time stays latched for that match.
            if (Revision != 0 && currentMatchId != null && currentMatchId == matchId) return;
            if (cycle == null || cycle.DayNightProfile == null) throw new System.InvalidOperationException("Bind the scene's Weather Maker day/night manager.");
            if (ownedProfile == null)
            {
                previousProfile = cycle.DayNightProfile;
                previousCloudAmbient = clouds != null ? clouds.GlobalAmbientMultiplierGradient : null;
                fillWasEnabled = daylightFill != null && daylightFill.enabled;
                ownedProfile = Instantiate(previousProfile);
                ownedProfile.name = previousProfile.name + " (Sports match time)";
                ownedProfile.hideFlags = HideFlags.DontSave;
                daylightIntensity = new float[nightLights.Length];
                for (int i = 0; i < nightLights.Length; i++) if (nightLights[i] != null) daylightIntensity[i] = nightLights[i].intensity;
            }
            cycle.DayNightProfile = ownedProfile;
            if (!preserveInteriorAmbient && daylightFill != null && daylightFill.GetComponent<WeatherMakerCelestialObjectScript>() == null) daylightFill.enabled = false;
            Applied = SportsTimeOfDay.Resolve(seed); matchId = currentMatchId; Revision++;
            if (coordinateGroundColor)
            {
                if (skySphere == null || skySphere.SkySphereProfile == null) throw new System.InvalidOperationException("Bind the elevated venue's Weather Maker sky profile.");
                if (ownedSkyProfile == null)
                {
                    previousSkyProfile = skySphere.SkySphereProfile;
                    ownedSkyProfile = Instantiate(previousSkyProfile); ownedSkyProfile.name = previousSkyProfile.name + " (Sports match horizon)"; ownedSkyProfile.hideFlags = HideFlags.DontSave;
                }
                // The inactive night sun reports a gradient lookup of1. Freeze
                // only the public ground tint using the already selected clock;
                // all atmospheric/star rendering remains Weather Maker-owned.
                float lookup = Applied == SportsTimeChoice.Night ? 0 : Applied == SportsTimeChoice.Evening ? .5f : Applied == SportsTimeChoice.Morning ? .55f : 1;
                Color ground = previousSkyProfile.SkyGroundGradient.Evaluate(lookup);
                var gradient = new Gradient(); gradient.SetKeys(new[] { new GradientColorKey(ground, 0), new GradientColorKey(ground, 1) }, new[] { new GradientAlphaKey(ground.a, 0), new GradientAlphaKey(ground.a, 1) });
                ownedSkyProfile.SkyGroundGradient = gradient; skySphere.SkySphereProfile = ownedSkyProfile;
            }
            ownedProfile.Speed = ownedProfile.NightSpeed = 0;
            ownedProfile.TimeOfDay = SportsTimeOfDay.Seconds(Applied);
            if (clouds != null)
            {
                // The installed cloud profiles use a constant ambient gain of1.
                // At night an inactive sun's gradient lookup also returns1.
                // Coordinate the frozen match time through the vendor's public
                // ambient control, preserving the authored gradient and profiles.
                var original = previousCloudAmbient ?? new Gradient();
                var keys = original.alphaKeys;
                float gain = Applied == SportsTimeChoice.Night ? nightCloudAmbientMultiplier : Applied == SportsTimeChoice.Evening ? .55f : Applied == SportsTimeChoice.Morning ? .7f : 1;
                for (int i = 0; i < keys.Length; i++) keys[i].alpha *= gain;
                ownedCloudAmbient = new Gradient(); ownedCloudAmbient.mode = original.mode;
                ownedCloudAmbient.SetKeys(original.colorKeys, keys);
                clouds.GlobalAmbientMultiplierGradient = ownedCloudAmbient;
            }
            if (preserveInteriorAmbient) ownedProfile.AmbientColorMode = WeatherMakerDayNightAmbientColorMode.UnityAmbientSettings;
            // No live shadow proliferation or reflection-probe refresh per frame.
            bool lampsOn = Applied == SportsTimeChoice.Night || Applied == SportsTimeChoice.Evening;
            for (int i = 0; i < nightLights.Length; i++) if (nightLights[i] != null) nightLights[i].intensity = lampsOn ? daylightIntensity[i] : 0;
        }
        void LateUpdate()
        {
            // A 3D venue can use an orthographic broadcast lens. The vendor's
            // SunForCamera lookup still requires an orthographic celestial entry.
            // Mirror its own astronomical sun into a non-illuminating lookup light;
            // never calculate a second sun, add world illumination, or move a camera.
            if (perspectiveSun == null || orthographicSun == null || perspectiveSun.Light == null || orthographicSun.Light == null) return;
            orthographicSun.transform.rotation = perspectiveSun.transform.rotation;
            orthographicSun.Light.color = perspectiveSun.Light.color;
            orthographicSun.Light.intensity = perspectiveSun.Light.intensity;
        }
        void OnDestroy()
        {
            if (cycle != null && cycle.DayNightProfile == ownedProfile) cycle.DayNightProfile = previousProfile;
            if (clouds != null && clouds.GlobalAmbientMultiplierGradient == ownedCloudAmbient) clouds.GlobalAmbientMultiplierGradient = previousCloudAmbient;
            if (ownedProfile != null && daylightFill != null) daylightFill.enabled = fillWasEnabled;
            if (ownedProfile != null) Destroy(ownedProfile);
            if (skySphere != null && skySphere.SkySphereProfile == ownedSkyProfile) skySphere.SkySphereProfile = previousSkyProfile;
            if (ownedSkyProfile != null) Destroy(ownedSkyProfile);
        }
    }
}

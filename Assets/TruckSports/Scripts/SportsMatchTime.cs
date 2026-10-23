using DigitalRuby.WeatherMaker;
using UnityEngine;

namespace LWS.TruckSports
{
    // Explicit scene binding; no runtime scene search, weather replacement, or venue regeneration.
    [DefaultExecutionOrder(100)]
    public sealed class SportsMatchTime : MonoBehaviour
    {
        public WeatherMakerDayNightCycleManagerScript cycle;
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

        public void Configure(int seed, string currentMatchId)
        {
            // A heat/attempt may reconfigure weather. Random time stays latched for that match.
            if (Revision != 0 && currentMatchId != null && currentMatchId == matchId) return;
            if (cycle == null || cycle.DayNightProfile == null) throw new System.InvalidOperationException("Bind the scene's Weather Maker day/night manager.");
            if (ownedProfile == null)
            {
                previousProfile = cycle.DayNightProfile;
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
            ownedProfile.Speed = ownedProfile.NightSpeed = 0;
            ownedProfile.TimeOfDay = SportsTimeOfDay.Seconds(Applied);
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
            if (ownedProfile != null && daylightFill != null) daylightFill.enabled = fillWasEnabled;
            if (ownedProfile != null) Destroy(ownedProfile);
        }
    }
}

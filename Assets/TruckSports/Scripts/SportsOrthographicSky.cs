using DigitalRuby.WeatherMaker;
using UnityEngine;

namespace LWS.TruckSports
{
    // A 3D arena with an orthographic broadcast lens still needs the 3D sky
    // profile upload. The vendor sphere deliberately skips orthographic lenses.
    public sealed class SportsOrthographicSky : MonoBehaviour
    {
        public WeatherMakerScript weather;
        public WeatherMakerCommandBufferManagerScript commands;
        public WeatherMakerSkySphereScript sphere;
        public WeatherMakerCelestialObjectScript sun;

        void OnEnable()
        {
            if (commands != null) commands.RegisterPreCull(PrepareSky, this);
        }

        void OnDisable()
        {
            if (commands != null) commands.UnregisterPreCull(this);
        }

        void PrepareSky(Camera camera)
        {
            if (camera == null || !camera.orthographic || weather == null ||
                !weather.AllowCameras.Contains(camera) || sphere == null ||
                !sphere.isActiveAndEnabled || sphere.SkySphereProfile == null || sun == null) return;

            // Use the public vendor API and the actual scene profile. Perspective
            // views keep the vendor's ordinary callback; no second sky renderer.
            var profile = sphere.SkySphereProfile;
            profile.UpdateSkySphere(camera, sphere.MeshRenderer.sharedMaterial, sphere.gameObject, sun);
            if (profile.AtmosphereProfile != null)
                profile.AtmosphereProfile.UpdateShaderVariables(camera, sphere.AtmosphereLookupMaterial, sphere.AtmosphereComputeShader);
        }
    }
}

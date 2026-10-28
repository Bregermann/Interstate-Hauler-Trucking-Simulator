using UnityEngine;
namespace LWS.TruckCamping
{
    public sealed class CampSite : MonoBehaviour
    {
        public string id;
        public CampLandmark landmark;
        public ParticleSystem flame, smoke;
        public Light fireLight;
        public AudioSource crackle;
        public GameObject[] arrangedLogs;
        public Transform pot, food;
        public CampfireState state;
        float lightPhase;
        public void ResetSite() { state = new CampfireState { id = id }; Present(); }
        public void Tick(float dt)
        {
            state.Tick(dt); lightPhase += dt;
            if (fireLight != null) fireLight.intensity = state.Burning ? 2.2f + Mathf.Sin(lightPhase * 7) * .2f : 0;
            if (pot != null && state.Burning) pot.localRotation = Quaternion.Euler(Mathf.Sin(lightPhase * 5) * .5f, 0, Mathf.Cos(lightPhase * 4) * .6f);
            Present();
        }
        void Present()
        {
            if (state == null) return;
            foreach (var log in arrangedLogs) log.SetActive(state.built);
            Set(flame, state.Burning); Set(smoke, state.Burning);
            if (crackle != null) { bool audible = state.Burning && Application.isFocused; if (audible && !crackle.isPlaying) crackle.Play(); else if (!audible && crackle.isPlaying) crackle.Stop(); }
        }
        static void Set(ParticleSystem fx, bool active) { if (fx == null) return; if (active && !fx.isPlaying) fx.Play(); else if (!active && fx.isPlaying) fx.Stop(); }
        public bool Nearby(Vector3 actual) => Vector3.Distance(actual, landmark.parking.position) < 16;
    }
}

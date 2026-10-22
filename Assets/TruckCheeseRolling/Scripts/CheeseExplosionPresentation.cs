using UnityEngine;

namespace LWS.TruckCheeseRolling
{
    // Adapter over the original eight ParticleSystem pool entries. Match.Explode
    // still owns timing, root lifecycle, sounds, damage, forces and debris.
    public sealed class CheeseExplosionPresentation : MonoBehaviour
    {
        public const string Resource = "CheeseOwnedExplosion";
        public GameObject visual { get; private set; }
        void Awake()
        {
            var prefab = Resources.Load<GameObject>(Resource);
            if (prefab == null) { Debug.LogError("Owned Cheese explosion VFX missing. Run the licensed VFX intake and Cheese explosion presentation authoring before building."); return; }
            visual = Instantiate(prefab, transform, false); visual.name = "Pooled owned GAPH explosion visual";
            // The legacy demo's light lost its vendor fade script during the
            // visual-only conversion. Never leave eight permanent 40m lights
            // in the race; the existing weather retains lighting authority.
            foreach (var lamp in visual.GetComponentsInChildren<Light>(true)) { lamp.enabled = false; Destroy(lamp); }
            foreach (var system in visual.GetComponentsInChildren<ParticleSystem>(true)) { var main = system.main; main.playOnAwake = false; system.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear); }
            visual.SetActive(true);
            // Keep the existing root simulation unchanged: its isPlaying state
            // participates in recovery safety. Only its flat renderer is hidden.
            GetComponent<ParticleSystemRenderer>().enabled = false;
        }
    }
}

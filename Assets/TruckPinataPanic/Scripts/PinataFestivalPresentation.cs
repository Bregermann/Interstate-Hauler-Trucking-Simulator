using DG.Tweening;
using LWS.TruckFieldGoal;
using TMPro;
using UnityEngine;

namespace LWS.TruckPinataPanic
{
    // Presentation reads confirmed mode state. It never damages, collects or
    // chooses a winner. Existing crowd/audio/VFX and DOTween own those visuals.
    public sealed class PinataFestivalPresentation : MonoBehaviour
    {
        public PinataMatch match;
        public FieldGoalCrowd crowd;
        public ParticleSystem confetti, paperDust, sweetSparkle, blast;
        public AudioSource music;
        public TMP_Text arenaBoard;
        public Light[] festoonLights;
        public Renderer[] damageLayers;
        float lastHit, lastPickup;
        int lastDamageStage = -1;
        bool musicPaused;
        public void ResetFestival()
        {
            StopPresentation(); lastHit = lastPickup = -10; lastDamageStage = -1;
            crowd?.ResetAttempt();
            foreach (var layer in damageLayers) if (layer != null) layer.enabled = false;
            if (music != null) { music.time = 0; music.volume = match.settings.sfx * .12f; if (music.clip != null) music.Play(); }
            foreach (var light in festoonLights) if (light != null) { light.DOKill(); light.intensity = 1.3f; }
        }
        void Update()
        {
            if (match == null) return;
            if (arenaBoard != null) arenaBoard.text = match.Header + "\n" + match.PhaseReadout;
            if (music != null) { music.volume = match.Live && Application.isFocused ? match.settings.sfx * .12f : 0; if (match.Live && Application.isFocused && !musicPaused && !music.isPlaying && music.clip != null) music.Play(); }
            if (!match.Running || match.Paused) return;
            int stage = Mathf.Clamp(Mathf.FloorToInt((1 - match.pinata.Health / Mathf.Max(1, match.pinata.MaximumHealth)) * 4), 0, 3);
            if (stage != lastDamageStage)
            {
                lastDamageStage = stage;
                for (int i = 0; i < damageLayers.Length; i++) if (damageLayers[i] != null) damageLayers[i].enabled = i < stage;
            }
        }
        public void Hit(Vector3 point, float lost)
        {
            if (Time.time - lastHit < .2f) return; lastHit = Time.time;
            Play(paperDust, point); if (lost > 180) crowd?.React(true, "A real shell impact", point);
        }
        public void Rupture(Vector3 point)
        {
            Play(confetti, point); crowd?.React(true, "The shell ruptured", point);
            foreach (var light in festoonLights) if (light != null) { light.DOKill(); light.DOIntensity(3, .2f).SetLoops(6, LoopType.Yoyo).SetUpdate(false); }
        }
        public void Pickup(Vector3 point, bool golden)
        {
            if (Time.time - lastPickup < .12f) return; lastPickup = Time.time; Play(sweetSparkle, point);
            if (golden) crowd?.React(true, "Golden sweet collected", point);
        }
        public void Explosion(Vector3 point) => Play(blast, point);
        static void Play(ParticleSystem system, Vector3 position) { if (system == null) return; system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); system.transform.position = position; system.Play(true); }
        public void Pause(bool paused)
        {
            foreach (var system in new[] { confetti, paperDust, sweetSparkle, blast }) if (system != null) { if (paused) system.Pause(true); else if (system.isPaused) system.Play(true); }
            foreach (var light in festoonLights) if (light != null) { if (paused) light.DOPause(); else light.DOPlay(); }
            if (music != null) { if (paused) { musicPaused = music.isPlaying; music.Pause(); } else if (musicPaused) { music.UnPause(); musicPaused = false; } }
        }
        public void StopSound() { if (music != null) music.Stop(); musicPaused = false; }
        public void StopPresentation()
        {
            foreach (var system in new[] { confetti, paperDust, sweetSparkle, blast }) if (system != null) system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (festoonLights != null) foreach (var light in festoonLights) if (light != null) light.DOKill();
            StopSound();
        }
        void OnDisable() => StopPresentation();
    }
}

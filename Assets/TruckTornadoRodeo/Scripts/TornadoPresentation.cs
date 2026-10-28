using DG.Tweening;
using LWS.TruckSports;
using TMPro;
using UnityEngine;

namespace LWS.TruckTornadoRodeo
{
    public sealed class TornadoPresentation : MonoBehaviour
    {
        public TornadoMatch match;
        public ParticleSystem funnel, dust, contact;
        public AudioSource roar;
        public TMP_Text arenaBoard;
        public Transform windVane;
        public Renderer[] ringLights;
        float nextEmission, lastContact;
        int emission;
        MaterialPropertyBlock glow;
        void Awake() => glow = new MaterialPropertyBlock();
        public void Begin()
        {
            Stop(); emission = 0; nextEmission = lastContact = -10; funnel.Play(true); dust.Play(true);
            if (roar != null && roar.clip != null) roar.Play();
            foreach (var ring in ringLights) { ring.transform.DOKill(); ring.SetPropertyBlock(null); }
        }
        void Update()
        {
            if (match == null) return;
            if (arenaBoard != null) arenaBoard.text = match.Title + "\n" + match.Status;
            if (roar != null) { roar.volume = match.Live && Application.isFocused ? match.settings.sfx * .16f : 0; if (match.Live && !roar.isPlaying && roar.clip != null && Application.isFocused) roar.Play(); }
            if (!match.Live) return;
            if (windVane != null && match.presentationWeather != null && match.presentationWeather.Flow.sqrMagnitude > .01f) windVane.rotation = Quaternion.RotateTowards(windVane.rotation, Quaternion.LookRotation(match.presentationWeather.Flow, Vector3.up) * Quaternion.Euler(0, -90, 0), Time.deltaTime * 90);
            if (Time.time < nextEmission) return; nextEmission = Time.time + .04f;
            // A bounded local particle spiral visualizes this hazard's actual
            // force space. Three coherent rising strands replace sparse random
            // azimuths; the same12/emission,2.5second lifetime and900particle
            // cap remain. Weather Maker owns sky/rain/clouds/lightning.
            for (int i = 0; i < 12; i++)
            {
                int n = emission++; int layer = n % 72, strand = n / 72 % 3;
                float height = 2 + layer * (116f / 71), angle = height * .12f + match.rules.Clock * 1.3f + strand * (Mathf.PI * 2 / 3);
                float radius = 4 + height * .25f;
                var p = match.field.Center + new Vector3(Mathf.Cos(angle) * radius, height, Mathf.Sin(angle) * radius);
                var velocity = match.field.Acceleration(p, Vector3.zero); var emit = new ParticleSystem.EmitParams { position = p, velocity = velocity * .2f, startSize = 6.5f + height * .08f, startLifetime = 2.5f, startColor = new Color(.34f, .38f, .37f, .32f + height / 1000) };
                funnel.Emit(emit, 1);
            }
        }
        public void Ring(Vector3 point)
        {
            for (int i = 0; i < match.rings.Length; i++) if ((match.rings[i].transform.position - point).sqrMagnitude < .1f)
            {
                var render = ringLights[i]; float value = 0; render.transform.DOKill();
                DOTween.To(() => value, v => { value = v; glow.SetColor("_EmissionColor", new Color(1, .6f, .05f) * (.8f + v)); render.SetPropertyBlock(glow); }, 4, .2f).SetTarget(render.transform).SetLoops(4, LoopType.Yoyo);
            }
        }
        public void Landing(Vector3 point) { Contact(point); match.feedback.PlayAt(5, point, 0, true); }
        public void Contact(Vector3 point) { if (contact == null || Time.time < lastContact + .2f) return; lastContact = Time.time; contact.transform.position = point; contact.Play(true); }
        public void Pause(bool paused) { foreach (var p in new[] { funnel, dust, contact }) if (p != null) { if (paused) p.Pause(true); else if (p.isPaused) p.Play(true); } foreach (var r in ringLights) { if (paused) r.transform.DOPause(); else r.transform.DOPlay(); } if (roar != null) { if (paused) roar.Pause(); else roar.UnPause(); } }
        public void StopSound() { if (roar != null) roar.Stop(); }
        public void Stop() { foreach (var p in new[] { funnel, dust, contact }) if (p != null) p.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); if (ringLights != null) foreach (var r in ringLights) if (r != null) r.transform.DOKill(); StopSound(); }
        void OnDisable() => Stop();
    }
}

using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace LWS.TruckTaxi
{
    // Presentation only: the clock, inventory and saved urgency remain in DriverNeedsState.
    public sealed class TruckTaxiNeedsPresentation : MonoBehaviour
    {
        private sealed class Slot
        {
            public UnityEngine.UI.RawImage Image;
            public Sequence Motion;
            public bool Active;
        }
        private TruckTaxiBootstrap host;
        private TruckTaxiDriverNeedsCoordinator owner;
        private TruckTaxiDriverNeedsState state;
        private TruckTaxiEnvironmentSettings settings;
        private RectTransform root;
        private readonly Slot[][] slots = new Slot[2][];
        private readonly Texture2D[][] pools = new Texture2D[2][];
        private readonly float[] nextImage = new float[2];
        private readonly UnityEngine.UI.RawImage[] edges = new UnityEngine.UI.RawImage[4];
        private Texture2D edgeTexture;
        private AudioSource audioSource;
        private AudioClip generatedGut;
        private float nextGut, nextWarning, nextCravingSound;
        private bool visible, lastSoundWasHunger, pendingFailure;
        private float previousBowelPressure;

        public void Initialize(TruckTaxiBootstrap host, TruckTaxiDriverNeedsCoordinator owner,
            TruckTaxiEnvironmentSettings settings, TruckTaxiDriverNeedsState state)
        {
            if (root != null) return;
            this.host = host; this.owner = owner; this.settings = settings; this.state = state;
            root = TruckTaxiHud.Rect(host.hud.Root, "Needs screen presentation", Vector2.zero, Vector2.one);
            root.SetAsFirstSibling();
            pools[0] = ResolvePool(settings.hungerCravingPool, settings.hungerCravingImage, "Hunger", "Pizza");
            pools[1] = ResolvePool(settings.thirstCravingPool, settings.thirstCravingImage, "Thirst", "Soda");
            for (int need = 0; need < 2; need++)
            {
                slots[need] = new Slot[Mathf.Clamp(settings.cravingCapacityPerNeed, 1, 4)];
                for (int i = 0; i < slots[need].Length; i++)
                {
                    var rect = TruckTaxiHud.Rect(root, (need == 0 ? "Food " : "Drink ") + i, Vector2.one * .5f, Vector2.one * .5f);
                    var image = rect.gameObject.AddComponent<UnityEngine.UI.RawImage>();
                    image.raycastTarget = false; image.color = Color.clear;
                    image.gameObject.SetActive(false);
                    slots[need][i] = new Slot { Image = image };
                }
            }
            edgeTexture = new Texture2D(32, 1, TextureFormat.RGBA32, false) { name = "Needs edge fade", wrapMode = TextureWrapMode.Clamp };
            for (int x = 0; x < 32; x++) edgeTexture.SetPixel(x, 0, new Color(1, 1, 1, Mathf.Pow(1 - x / 31f, 2)));
            edgeTexture.Apply(false, true);
            for (int i = 0; i < edges.Length; i++)
            {
                var rect = TruckTaxiHud.Rect(root, "Discomfort edge " + i, Vector2.zero, Vector2.one);
                edges[i] = rect.gameObject.AddComponent<UnityEngine.UI.RawImage>();
                edges[i].texture = edgeTexture; edges[i].raycastTarget = false; edges[i].color = Color.clear;
            }
            audioSource = root.gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false; audioSource.spatialBlend = 0;
            TruckTaxiAudioController.Instance?.Route(audioSource, TruckTaxiAudioCategory.UI);
            state.Event += OnNeedEvent;
            ClearPresentation(true);
        }

        public static Texture2D[] ResolvePool(Texture2D[] configured, Texture2D legacy, string first, string second)
        {
            var found = new List<Texture2D>();
            if (configured != null) foreach (var image in configured)
                if (image != null && !found.Contains(image)) found.Add(image);
            if (found.Count == 0)
            {
                if (legacy != null) found.Add(legacy);
                var a = Resources.Load<Texture2D>("TruckTaxi/Needs/" + first);
                var b = Resources.Load<Texture2D>("TruckTaxi/Needs/" + second);
                if (a != null && !found.Contains(a)) found.Add(a);
                if (b != null && !found.Contains(b)) found.Add(b);
            }
            return found.ToArray();
        }

        public static float Severity(float need, float threshold) => !float.IsFinite(need) || !float.IsFinite(threshold) ? 0 :
            Mathf.InverseLerp(Mathf.Clamp(threshold, .8f, .99f), 1, need);
        public static int ConcurrentImages(float severity, int capacity) => severity <= 0 || !float.IsFinite(severity) ? 0 :
            Mathf.Clamp(Mathf.CeilToInt(Mathf.Clamp01(severity) * Mathf.Clamp(capacity, 1, 4)), 1, 4);

        private bool CanPresent => owner != null && owner.isActiveAndEnabled && host != null && host.Ready && !host.Paused &&
            host.hud != null && !host.hud.NormalHudSuppressed && host.Session != null && host.Session.State != TruckTaxiState.Inactive;
        private void Update()
        {
            if (root == null) return;
            if (!CanPresent) { if (visible) ClearPresentation(); return; }
            visible = true; root.gameObject.SetActive(true);
            float now = Time.time;
            if (pendingFailure)
            {
                pendingFailure = false;
                host.hud.Alerts?.Publish("bowel-failure", "SHIT YOUR PANTS", TruckTaxiAlertPriority.Normal, 7);
                PlayGut(settings.bowelFailureSound, 1); nextGut = now + 10;
            }
            for (int need = 0; need < 2; need++)
            {
                float severity = Severity(need == 0 ? state.Hunger : state.Thirst, settings.cravingThreshold);
                int limit = ConcurrentImages(severity, slots[need].Length), active = 0;
                foreach (var slot in slots[need])
                {
                    if (slot.Active && ++active > limit) Stop(slot);
                }
                if (limit == 0 || active >= limit || now < nextImage[need] || pools[need].Length == 0) continue;
                foreach (var slot in slots[need]) if (!slot.Active) { Show(slot, need, severity); break; }
                nextImage[need] = now + Random.Range(1.2f, 2.4f) * Mathf.Lerp(4, 1, severity);
            }
            float pressure = state.BowelPressure;
            if (previousBowelPressure >= .25f && pressure < .25f && state.BowelDiscomfortGameSeconds <= 0)
            { audioSource.Stop(); nextGut = now + 1; nextWarning = now + 1; }
            previousBowelPressure = pressure;
            float intensity = pressure >= .5f ? Mathf.Lerp(.08f, 1, Mathf.InverseLerp(.5f, 1, pressure)) : 0;
            if (state.BowelDiscomfortGameSeconds > 0) intensity = Mathf.Max(intensity, .15f);
            RefreshEdges(intensity);
            if (pressure >= .25f && now >= nextGut && !audioSource.isPlaying)
            {
                PlayGut(pressure >= .75f ? settings.bowelUrgentSound : settings.bowelModerateSound, pressure);
                nextGut = now + Mathf.Lerp(65, 7, pressure) * Random.Range(.85f, 1.15f);
            }
            if (pressure >= .75f && now >= nextWarning)
            {
                host.hud.Alerts?.Publish("bowel-warning", pressure >= .99f ? "Toilet. Now." : "Your stomach is making demands.", TruckTaxiAlertPriority.Normal, 4);
                nextWarning = now + (pressure >= .99f ? 12 : 35);
            }
            if (now >= nextCravingSound && !audioSource.isPlaying)
            {
                bool hunger = Severity(state.Hunger, settings.cravingThreshold) > 0;
                bool thirst = Severity(state.Thirst, settings.cravingThreshold) > 0;
                bool food = hunger && (!thirst || !lastSoundWasHunger);
                var clip = food ? settings.hungerCravingSound : thirst ? settings.thirstCravingSound : null;
                if (clip != null) audioSource.PlayOneShot(clip, .4f);
                lastSoundWasHunger = food;
                nextCravingSound = now + Mathf.Clamp(settings.cravingSoundInterval, 10, 120);
            }
        }

        private void Show(Slot slot, int need, float severity)
        {
            slot.Active = true;
            var image = slot.Image;
            image.texture = pools[need][Random.Range(0, pools[need].Length)];
            float size = Mathf.Clamp(settings.cravingImageSize, 80, 220) * Random.Range(.75f, 1);
            size = Mathf.Min(size, Mathf.Min(root.rect.width, root.rect.height) * .22f);
            float aspect = image.texture.width / (float)Mathf.Max(1, image.texture.height);
            image.rectTransform.sizeDelta = aspect > 1 ? new Vector2(size, size / aspect) : new Vector2(size * aspect, size);
            Vector2 anchor = new Vector2(Random.Range(.18f, .82f), Random.Range(.25f, .8f));
            image.rectTransform.anchorMin = image.rectTransform.anchorMax = anchor;
            image.rectTransform.anchoredPosition = Vector2.zero; image.rectTransform.localScale = Vector3.one * .85f;
            image.color = new Color(1, 1, 1, 0); image.gameObject.SetActive(true);
            float duration = Random.Range(4, 7), alpha = Mathf.Clamp(settings.cravingMaximumOpacity, 0, .5f) * Mathf.Lerp(.35f, 1, severity);
            slot.Motion = DOTween.Sequence().Append(Fade(image, alpha)).AppendInterval(duration - 1.6f)
                .Append(Fade(image, 0)).Insert(0, DOTween.To(() => image.rectTransform.anchoredPosition,
                    value => image.rectTransform.anchoredPosition = value, Random.insideUnitCircle * 22, duration))
                .Insert(0, DOTween.To(() => image.rectTransform.localScale.x,
                    value => image.rectTransform.localScale = Vector3.one * value, Random.Range(.95f, 1.08f), duration))
                .OnComplete(() => { slot.Active = false; image.gameObject.SetActive(false); slot.Motion = null; });
        }
        private static Tween Fade(UnityEngine.UI.RawImage image, float alpha) => DOTween.To(() => image.color.a,
            value => image.color = new Color(1, 1, 1, value), alpha, .8f);

        private void RefreshEdges(float intensity)
        {
            float depth = Mathf.Lerp(.045f, .19f, intensity);
            float pulse = .92f + Mathf.Sin(Time.time * 2) * .08f;
            for (int i = 0; i < edges.Length; i++)
            {
                var rect = edges[i].rectTransform;
                bool vertical = i < 2;
                rect.anchorMin = rect.anchorMax = i == 0 ? new Vector2(0, .5f) : i == 1 ? new Vector2(1, .5f) : i == 2 ? new Vector2(.5f, 0) : new Vector2(.5f, 1);
                rect.pivot = new Vector2(0, .5f);
                rect.localRotation = Quaternion.Euler(0, 0, i == 0 ? 0 : i == 1 ? 180 : i == 2 ? 90 : -90);
                rect.sizeDelta = new Vector2((vertical ? root.rect.width : root.rect.height) * depth, vertical ? root.rect.height : root.rect.width);
                rect.anchoredPosition = Vector2.zero;
                edges[i].color = new Color(.28f, .12f, .035f, intensity * Mathf.Clamp(settings.bowelMaximumEdgeOpacity, 0, .6f) * pulse);
            }
        }

        private void OnNeedEvent(TruckTaxiDriverNeedEvent kind)
        {
            // Debug and sleep can fail while a modal pauses gameplay. Present once after it closes.
            if (kind == TruckTaxiDriverNeedEvent.BowelAccident) pendingFailure = true;
        }
        private void PlayGut(AudioClip clip, float intensity)
        {
            if (clip == null)
            {
                // Replaceable non-voice synthesized rumble, not an asset or recorded performance.
                if (generatedGut == null)
                {
                    const int rate = 22050;
                    var samples = new float[rate * 2];
                    for (int i = 0; i < samples.Length; i++)
                    {
                        float t = i / (float)rate, envelope = Mathf.Sin(Mathf.PI * i / samples.Length);
                        samples[i] = envelope * .3f * Mathf.Sin(2 * Mathf.PI * 63 * t + 5 * Mathf.Sin(2 * Mathf.PI * 3 * t)) *
                            (.6f + .4f * Mathf.Sin(2 * Mathf.PI * 11 * t));
                    }
                    generatedGut = AudioClip.Create("Needs non-voice gut rumble", samples.Length, 1, rate, false);
                    generatedGut.SetData(samples, 0);
                }
                clip = generatedGut;
            }
            audioSource.PlayOneShot(clip, Mathf.Clamp01(settings.bowelSoundVolume) * Mathf.Lerp(.4f, 1, intensity));
        }
        private static void Stop(Slot slot)
        {
            slot.Motion?.Kill(); slot.Motion = null; slot.Active = false;
            if (slot.Image != null) { slot.Image.color = Color.clear; slot.Image.gameObject.SetActive(false); }
        }
        public void ClearPresentation(bool discardFailure = false)
        {
            if (discardFailure) pendingFailure = false;
            foreach (var pool in slots) if (pool != null) foreach (var slot in pool) Stop(slot);
            if (audioSource != null) audioSource.Stop();
            if (root != null) root.gameObject.SetActive(false);
            visible = false; nextGut = nextWarning = Time.time + 1;
            nextImage[0] = nextImage[1] = Time.time + .5f;
        }
        private void OnDisable() => ClearPresentation();
        private void OnDestroy()
        {
            if (state != null) state.Event -= OnNeedEvent;
            ClearPresentation();
            if (root != null) Destroy(root.gameObject);
            if (edgeTexture != null) Destroy(edgeTexture);
            if (generatedGut != null) Destroy(generatedGut);
        }
    }
}

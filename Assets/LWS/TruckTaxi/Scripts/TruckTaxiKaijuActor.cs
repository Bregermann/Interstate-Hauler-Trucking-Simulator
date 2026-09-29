using UnityEngine;

namespace LWS.TruckTaxi
{
    // Original six-legged "Cinder Spindle" prototype: arch-backed, lantern-faced, no dorsal plates.
    [DisallowMultipleComponent]
    public sealed class TruckTaxiKaijuActor : MonoBehaviour
    {
        private Transform nearRoot, farRoot, mouth, hand;
        private Transform[] legs;
        private GameObject fireBeam;
        private Material shell, glow, flame;
        private AudioSource voice;
        private AudioClip generatedRoar, generatedStomp, generatedFire;
        private float phase;
        private float lodDistance;
        private float speed;
        public Transform Mouth => mouth;
        public Transform Hand => hand;
        public Vector3 FireOrigin => mouth != null ? mouth.position : transform.position + Vector3.up * 40;

        public void Initialize(TruckTaxiKaijuConfig config)
        {
            if (nearRoot != null) return;
            lodDistance = config.distantLodMeters;
            speed = config.walkMetersPerSecond;
            float scale = config.creatureHeightMeters / 68f;
            transform.localScale = Vector3.one * scale;
            shell = MaterialFor(new Color(.13f, .19f, .19f), false);
            glow = MaterialFor(new Color(.15f, .8f, .65f), true);
            flame = MaterialFor(new Color(1f, .34f, .08f), true);
            nearRoot = new GameObject("Near silhouette").transform;
            nearRoot.SetParent(transform, false);
            farRoot = new GameObject("Distant silhouette").transform;
            farRoot.SetParent(transform, false);
            Part(nearRoot, PrimitiveType.Capsule, "Spindle torso", new Vector3(0, 34, 0), new Vector3(14, 26, 15), shell);
            Part(nearRoot, PrimitiveType.Sphere, "Hollow shoulder ring", new Vector3(0, 47, -2), new Vector3(20, 5, 18), glow);
            Part(nearRoot, PrimitiveType.Capsule, "High neck", new Vector3(0, 55, 7), new Vector3(5, 12, 5), shell);
            Part(nearRoot, PrimitiveType.Sphere, "Lantern face", new Vector3(0, 66, 11), new Vector3(10, 5, 7), glow);
            mouth = new GameObject("Mouth").transform;
            mouth.SetParent(transform, false);
            mouth.localPosition = new Vector3(0, 61, 17);
            legs = new Transform[6];
            for (int i = 0; i < legs.Length; i++)
            {
                float side = i < 3 ? -1 : 1;
                float z = (i % 3 - 1) * 12;
                var pivot = new GameObject("Walking stilt " + i).transform;
                pivot.SetParent(nearRoot, false);
                pivot.localPosition = new Vector3(side * 7, 29, z);
                Part(pivot, PrimitiveType.Capsule, "Jointed leg", new Vector3(side * 7, -15, 0),
                    new Vector3(4, 23, 4), shell).localRotation = Quaternion.Euler(0, 0, side * -22);
                Part(pivot, PrimitiveType.Sphere, "Foot", new Vector3(side * 12, -28, 3), new Vector3(10, 3, 9), shell);
                legs[i] = pivot;
            }
            hand = new GameObject("Grasp").transform;
            hand.SetParent(transform, false);
            hand.localPosition = new Vector3(20, 32, 15);
            Part(nearRoot, PrimitiveType.Capsule, "Grasping arm", new Vector3(16, 38, 5), new Vector3(5, 17, 5), shell)
                .localRotation = Quaternion.Euler(25, 0, -48);
            Part(farRoot, PrimitiveType.Capsule, "Distant body", new Vector3(0, 37, 0), new Vector3(19, 32, 17), shell);
            Part(farRoot, PrimitiveType.Sphere, "Distant lantern", new Vector3(0, 66, 10), new Vector3(11, 7, 9), glow);
            for (int i = 0; i < 6; i++)
                Part(farRoot, PrimitiveType.Capsule, "Distant stilt", new Vector3((i < 3 ? -1 : 1) * 15, 13, (i % 3 - 1) * 12),
                    new Vector3(4, 23, 4), shell);
            fireBeam = Part(transform, PrimitiveType.Cylinder, "Fire breath", Vector3.zero, Vector3.one, flame).gameObject;
            fireBeam.SetActive(false);
            voice = gameObject.AddComponent<AudioSource>();
            voice.playOnAwake = false; voice.spatialBlend = 1;
            voice.minDistance = 60; voice.maxDistance = 1800; voice.rolloffMode = AudioRolloffMode.Logarithmic;
            generatedRoar = GenerateSound("Cinder Spindle roar", 2.4f, 0);
            generatedStomp = GenerateSound("Cinder Spindle stomp", .65f, 1);
            generatedFire = GenerateSound("Cinder Spindle fire", 2.8f, 2);
        }

        public void Tick(float delta, Vector3 heading, Transform camera)
        {
            if (heading.sqrMagnitude > .001f)
            {
                transform.rotation = Quaternion.RotateTowards(transform.rotation,
                    Quaternion.LookRotation(heading.normalized, Vector3.up), delta * 30);
                transform.position += transform.forward * (speed * delta);
            }
            phase += delta * speed * .45f;
            for (int i = 0; i < legs.Length; i++)
                legs[i].localRotation = Quaternion.Euler(Mathf.Sin(phase + i * 2.1f) * 15, 0, 0);
            hand.localPosition = new Vector3(20 + Mathf.Sin(phase * .7f) * 4, 32 + Mathf.Sin(phase * 1.3f) * 6, 15);
            bool distant = camera != null && (camera.position - transform.position).sqrMagnitude > lodDistance * lodDistance;
            if (nearRoot.gameObject.activeSelf == distant) nearRoot.gameObject.SetActive(!distant);
            if (farRoot.gameObject.activeSelf != distant) farRoot.gameObject.SetActive(distant);
        }

        public void Roar(AudioClip clip)
        {
            if (voice != null && !voice.isPlaying) voice.PlayOneShot(clip != null ? clip : generatedRoar);
        }

        public void Stomp(AudioClip clip) => voice?.PlayOneShot(clip != null ? clip : generatedStomp, .55f);
        public void Fire(AudioClip clip) => voice?.PlayOneShot(clip != null ? clip : generatedFire, .75f);

        public void SetFire(bool active, bool telegraph, float length, Vector3 direction)
        {
            if (fireBeam == null) return;
            fireBeam.SetActive(active || telegraph);
            if (!fireBeam.activeSelf) return;
            fireBeam.transform.position = FireOrigin + direction * length * .5f;
            fireBeam.transform.rotation = Quaternion.FromToRotation(Vector3.up, direction);
            float parentScale = Mathf.Max(.01f, transform.lossyScale.x);
            fireBeam.transform.localScale = new Vector3(telegraph ? 1 : 4, length * .5f, telegraph ? 1 : 4) / parentScale;
        }

        public void PulseExplosion(Vector3 position)
        {
            var pulse = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            pulse.name = "Kaiju fire blast";
            pulse.transform.SetParent(transform, true);
            pulse.transform.position = position;
            pulse.transform.localScale = Vector3.one * 16;
            pulse.GetComponent<Renderer>().sharedMaterial = flame;
            var collider = pulse.GetComponent<Collider>();
            collider.enabled = false;
            Destroy(collider);
            Destroy(pulse, .8f);
        }

        private static Transform Part(Transform parent, PrimitiveType primitive, string name, Vector3 position,
            Vector3 size, Material material)
        {
            var go = GameObject.CreatePrimitive(primitive);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = size;
            var collider = go.GetComponent<Collider>();
            if (collider != null) { collider.enabled = false; Destroy(collider); }
            go.GetComponent<Renderer>().sharedMaterial = material;
            return go.transform;
        }

        private static Material MaterialFor(Color color, bool emissive)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var material = new Material(shader) { color = color };
            if (emissive) { material.EnableKeyword("_EMISSION"); material.SetColor("_EmissionColor", color * 2); }
            return material;
        }

        private static AudioClip GenerateSound(string name, float seconds, int mode)
        {
            const int rate = 22050;
            int count = Mathf.CeilToInt(seconds * rate);
            var samples = new float[count];
            uint noise = 84391;
            for (int i = 0; i < count; i++)
            {
                float t = (float)i / rate;
                float envelope = Mathf.Sin(Mathf.PI * i / count);
                noise = noise * 1664525u + 1013904223u;
                float hiss = ((noise >> 16) / 32768f - 1);
                float tone = Mathf.Sin(2 * Mathf.PI * (mode == 1 ? 65 : 95) * t + 13 * Mathf.Sin(t * 7));
                samples[i] = Mathf.Clamp((mode == 2 ? hiss * .5f + tone * .15f :
                    mode == 1 ? tone * .65f + hiss * .2f : tone * .45f + Mathf.Sin(2 * Mathf.PI * 143 * t) * .25f + hiss * .12f)
                    * envelope, -.9f, .9f);
            }
            var clip = AudioClip.Create(name, count, 1, rate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private void OnDestroy()
        {
            if (shell != null) Destroy(shell);
            if (glow != null) Destroy(glow);
            if (flame != null) Destroy(flame);
            if (generatedRoar != null) Destroy(generatedRoar);
            if (generatedStomp != null) Destroy(generatedStomp);
            if (generatedFire != null) Destroy(generatedFire);
        }
    }
}

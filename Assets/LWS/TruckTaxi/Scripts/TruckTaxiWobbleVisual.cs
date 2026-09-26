using System;
using UnityEngine;

namespace LWS.TruckTaxi
{
    // Visual-only overlay. UTS keeps its skeleton, colliders, movement, and ragdoll components.
    public static class TruckTaxiWobbleVisual
    {
        public const string StyleName = "TruckTaxiWobblePeople";
        public const string VisualName = "TruckTaxiWobbleVisual";
        private static Material[] pedestrianClothes;
        private static Material pedestrianDetail;
        private static Material pedestrianSkin;

        public sealed class Binding
        {
            private readonly Renderer[] original;
            private readonly bool[] enabled;
            private readonly GameObject visual;

            internal Binding(Renderer[] original, bool[] enabled, GameObject visual)
            {
                this.original = original;
                this.enabled = enabled;
                this.visual = visual;
            }

            public void SetWobbleVisible(bool wobble)
            {
                if (visual != null) visual.SetActive(wobble);
                for (int i = 0; i < original.Length; i++)
                    if (original[i] != null) original[i].enabled = !wobble && enabled[i];
            }
        }

        public static Binding BindPedestrian(Transform utsRoot, Material body, Material detail, float height = 1.7f, int paletteIndex = 0)
        {
            if (utsRoot == null) throw new ArgumentNullException(nameof(utsRoot));
            var renderers = utsRoot.GetComponentsInChildren<Renderer>(true);
            var enabled = new bool[renderers.Length];
            for (int i = 0; i < renderers.Length; i++) enabled[i] = renderers[i].enabled;
            EnsurePedestrianMaterials();
            var visual = Create(utsRoot, height, 1f,
                body != null ? body : pedestrianClothes[Mathf.Abs(paletteIndex % pedestrianClothes.Length)],
                detail != null ? detail : pedestrianDetail, pedestrianSkin);
            visual.AddComponent<TruckTaxiWobbleWalkSway>();
            var binding = new Binding(renderers, enabled, visual);
            binding.SetWobbleVisible(true);
            return binding;
        }

        public static GameObject Create(Transform parent, float height, float upperBodyScale, Material body, Material detail, Material skin = null)
        {
            if (parent == null) throw new ArgumentNullException(nameof(parent));
            height = Mathf.Max(.5f, height);
            upperBodyScale = Mathf.Clamp(upperBodyScale, .5f, 2f);
            var root = new GameObject(VisualName);
            root.transform.SetParent(parent, false);
            skin = skin != null ? skin : body;
            Part(root.transform, "Body", PrimitiveType.Capsule, new Vector3(0, .47f, 0) * height,
                new Vector3(.38f * upperBodyScale, .52f, .31f) * height, body);
            Part(root.transform, "Pants", PrimitiveType.Capsule, new Vector3(0, .28f, 0) * height,
                new Vector3(.31f, .30f, .30f) * height, detail);
            Part(root.transform, "Left sleeve", PrimitiveType.Capsule, new Vector3(-.36f * upperBodyScale, .58f, 0) * height,
                new Vector3(.14f, .29f, .16f) * height, body);
            Part(root.transform, "Right sleeve", PrimitiveType.Capsule, new Vector3(.36f * upperBodyScale, .58f, 0) * height,
                new Vector3(.14f, .29f, .16f) * height, body);
            Part(root.transform, "Left hand", PrimitiveType.Sphere, new Vector3(-.37f * upperBodyScale, .39f, 0) * height,
                Vector3.one * (.12f * height), skin);
            Part(root.transform, "Right hand", PrimitiveType.Sphere, new Vector3(.37f * upperBodyScale, .39f, 0) * height,
                Vector3.one * (.12f * height), skin);
            Part(root.transform, "Head", PrimitiveType.Sphere, new Vector3(0, .81f, 0) * height,
                new Vector3(.36f, .34f, .33f) * height, skin);
            Part(root.transform, "Left foot", PrimitiveType.Capsule, new Vector3(-.13f, .1f, .04f) * height,
                new Vector3(.15f, .19f, .24f) * height, detail);
            Part(root.transform, "Right foot", PrimitiveType.Capsule, new Vector3(.13f, .1f, .04f) * height,
                new Vector3(.15f, .19f, .24f) * height, detail);
            Part(root.transform, "Left eye", PrimitiveType.Sphere, new Vector3(-.09f, .84f, .158f) * height,
                Vector3.one * (.045f * height), detail);
            Part(root.transform, "Right eye", PrimitiveType.Sphere, new Vector3(.09f, .84f, .158f) * height,
                Vector3.one * (.045f * height), detail);
            return root;
        }

        private static void EnsurePedestrianMaterials()
        {
            if (pedestrianClothes != null) return;
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            pedestrianClothes = new[] {
                SharedMaterial(shader, new Color(.16f, .57f, .61f)),
                SharedMaterial(shader, new Color(.83f, .31f, .24f)),
                SharedMaterial(shader, new Color(.86f, .66f, .22f)),
                SharedMaterial(shader, new Color(.34f, .53f, .29f))
            };
            pedestrianDetail = SharedMaterial(shader, new Color(.13f, .17f, .24f));
            pedestrianSkin = SharedMaterial(shader, new Color(.78f, .54f, .38f));
        }

        private static Material SharedMaterial(Shader shader, Color color)
        {
            return new Material(shader) { color = color, hideFlags = HideFlags.DontSave };
        }

        private static void Part(Transform parent, string name, PrimitiveType shape, Vector3 position, Vector3 scale, Material material)
        {
            var part = GameObject.CreatePrimitive(shape);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            part.GetComponent<Renderer>().sharedMaterial = material;
            var collider = part.GetComponent<Collider>();
            if (Application.isPlaying) UnityEngine.Object.Destroy(collider);
            else UnityEngine.Object.DestroyImmediate(collider);
        }
    }
}

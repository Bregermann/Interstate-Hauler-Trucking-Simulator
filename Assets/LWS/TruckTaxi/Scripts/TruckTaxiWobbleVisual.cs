using System;
using UnityEngine;

namespace LWS.TruckTaxi
{
    // Visual-only overlay. UTS keeps its skeleton, colliders, movement, and ragdoll components.
    public static class TruckTaxiWobbleVisual
    {
        public const string StyleName = "TruckTaxiWobblePeople";
        public const string VisualName = "TruckTaxiWobbleVisual";
        public enum Silhouette { Human, Wide, Small, Dragon, Bird, Cat, Bear, Faun, Kangaroo, Yeti, Primate, Alien, Robot, Mask, Chicken }
        public enum Headwear { None, Hair, Curls, Braid, Cap, Helmet, WideHat, Hood, Crown, Horns, Ears, Beard, Visor, Crest }
        public enum Accessory { None, Tie, Backpack, Satchel, Headset, Badge, Goggles, Cape, Armor, Scarf, Tool, Book, Staff, Bow, Shield, Apron, Medical, Camera, Trophy, Clock, Card, Food, Coin, Circuit, Flower, Crate, Microphone }
        public struct Design
        {
            public Silhouette silhouette;
            public Headwear headwear;
            public Accessory accessory;
            public float height, width, headScale, upperBodyScale;
            public bool clothedAdult;
            public Material body, clothes, accent, skin;
        }
        private static Material[] pedestrianClothes;
        private static Material pedestrianDetail;
        private static Material pedestrianSkin;
        private static Mesh pedestrianMesh;

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
            var visual = CreateAmbientVisual(utsRoot, height,
                body != null ? body : pedestrianClothes[Mathf.Abs(paletteIndex % pedestrianClothes.Length)],
                detail != null ? detail : pedestrianDetail, pedestrianSkin);
            visual.AddComponent<TruckTaxiWobbleWalkSway>();
            var binding = new Binding(renderers, enabled, visual);
            binding.SetWobbleVisible(true);
            return binding;
        }

        private static GameObject CreateAmbientVisual(Transform parent,float height,Material clothes,Material detail,Material skin)
        {
            if(pedestrianMesh==null)
            {
                // Three shared material submeshes replace eleven renderers per ambient actor.
                // The authored passenger variants still retain their individually editable parts.
                var prototype=Create(parent,1,1,pedestrianClothes[0],pedestrianDetail,pedestrianSkin);
                prototype.SetActive(false);
                var filters=prototype.GetComponentsInChildren<MeshFilter>(true);
                var palette=new[]{pedestrianClothes[0],pedestrianDetail,pedestrianSkin};
                var groups=new CombineInstance[palette.Length];
                for(int m=0;m<palette.Length;m++)
                {
                    var parts=new System.Collections.Generic.List<CombineInstance>();
                    foreach(var filter in filters)
                        if(filter.GetComponent<Renderer>().sharedMaterial==palette[m])
                            parts.Add(new CombineInstance{mesh=filter.sharedMesh,
                                transform=prototype.transform.worldToLocalMatrix*filter.transform.localToWorldMatrix});
                    var mesh=new Mesh();mesh.CombineMeshes(parts.ToArray(),true,true);
                    groups[m]=new CombineInstance{mesh=mesh,transform=Matrix4x4.identity};
                }
                pedestrianMesh=new Mesh{name="Shared ambient Wobble geometry",hideFlags=HideFlags.DontSave};
                pedestrianMesh.CombineMeshes(groups,false,true);
                pedestrianMesh.RecalculateBounds();
                foreach(var group in groups) DestroyTemporary(group.mesh);
                DestroyTemporary(prototype);
            }
            var visual=new GameObject(VisualName,typeof(MeshFilter),typeof(MeshRenderer));
            visual.transform.SetParent(parent,false);
            visual.transform.localScale=Vector3.one*Mathf.Max(.5f,height);
            visual.GetComponent<MeshFilter>().sharedMesh=pedestrianMesh;
            visual.GetComponent<MeshRenderer>().sharedMaterials=new[]{clothes,detail,skin};
            return visual;
        }

        private static void DestroyTemporary(UnityEngine.Object value)
        { if(Application.isPlaying) UnityEngine.Object.Destroy(value); else UnityEngine.Object.DestroyImmediate(value); }

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

        // Visual only: no collider or gameplay component is added. Materials must outlive the visual.
        public static GameObject CreateThemed(Transform parent, Design d)
        {
            if (d.body == null || d.clothes == null || d.accent == null)
                throw new ArgumentException("Themed Wobble requires body, clothes and accent materials.", nameof(d));
            float h = Mathf.Max(.5f, d.height);
            var root = Create(parent, h, Mathf.Clamp(d.upperBodyScale, .5f, 2f), d.body, d.clothes,
                d.skin != null ? d.skin : d.body);
            float width = Mathf.Clamp(d.width, .6f, 1.8f);
            foreach (string name in new[] { "Body", "Pants", "Head" })
            {
                var t = root.transform.Find(name);
                t.localScale = Vector3.Scale(t.localScale, new Vector3(width, 1f, Mathf.Lerp(1f, width, .6f)));
            }
            root.transform.Find("Head").localScale *= Mathf.Clamp(d.headScale, .7f, 1.5f);
            if (d.clothedAdult)
            {
                Pair(root, "Clothed chest", PrimitiveType.Sphere, .13f, .61f, .16f,
                    .2f * Mathf.Clamp(d.upperBodyScale, 1f, 1.4f), .18f, .18f, h, d.body);
            }
            switch (d.silhouette)
            {
                case Silhouette.Wide: Add(root, "Broad shoulders", PrimitiveType.Capsule, 0, .65f, 0, .72f, .18f, .32f, h, d.clothes); break;
                case Silhouette.Small: root.transform.localScale = Vector3.one * .78f; break;
                case Silhouette.Dragon: Pair(root, "Wing", PrimitiveType.Cube, .38f, .61f, -.18f, .42f, .1f, .3f, h, d.accent); Add(root, "Dragon tail", PrimitiveType.Capsule, 0, .35f, -.37f, .14f, .49f, .14f, h, d.body); break;
                case Silhouette.Bird: case Silhouette.Chicken:
                    Pair(root, "Wing", PrimitiveType.Capsule, .4f, .59f, 0, .27f, .35f, .16f, h, d.clothes);
                    Add(root, "Beak", PrimitiveType.Cube, 0, .82f, .22f, .18f, .09f, .21f, h, d.accent);
                    if (d.silhouette == Silhouette.Chicken) Add(root, "Comb", PrimitiveType.Sphere, 0, 1.03f, 0, .19f, .24f, .1f, h, d.accent);
                    break;
                case Silhouette.Cat: Pair(root, "Cat ear", PrimitiveType.Cube, .2f, 1.01f, 0, .15f, .21f, .13f, h, d.body); Add(root, "Cat tail", PrimitiveType.Capsule, .2f, .36f, -.31f, .12f, .48f, .12f, h, d.body); break;
                case Silhouette.Bear: Pair(root, "Bear ear", PrimitiveType.Sphere, .23f, .98f, 0, .16f, .16f, .12f, h, d.body); Add(root, "Muzzle", PrimitiveType.Sphere, 0, .76f, .19f, .29f, .16f, .18f, h, d.accent); break;
                case Silhouette.Faun: Pair(root, "Antler", PrimitiveType.Capsule, .21f, 1.06f, 0, .08f, .35f, .08f, h, d.accent); Pair(root, "Hoof", PrimitiveType.Cube, .14f, .07f, .06f, .2f, .15f, .25f, h, d.clothes); break;
                case Silhouette.Kangaroo: Pair(root, "Long ear", PrimitiveType.Capsule, .2f, 1.07f, 0, .1f, .37f, .1f, h, d.body); Add(root, "Pouch", PrimitiveType.Sphere, 0, .4f, .19f, .34f, .23f, .14f, h, d.accent); break;
                case Silhouette.Yeti: Pair(root, "Heavy arm", PrimitiveType.Capsule, .45f, .52f, 0, .22f, .45f, .22f, h, d.body); break;
                case Silhouette.Primate: Pair(root, "Round ear", PrimitiveType.Sphere, .26f, .82f, 0, .19f, .19f, .1f, h, d.accent); Add(root, "Curled tail", PrimitiveType.Capsule, .19f, .4f, -.32f, .12f, .4f, .12f, h, d.body); break;
                case Silhouette.Alien: Pair(root, "Antenna", PrimitiveType.Capsule, .18f, 1.07f, 0, .07f, .28f, .07f, h, d.accent); break;
                case Silhouette.Robot: Add(root, "Mechanical torso", PrimitiveType.Cube, 0, .49f, 0, .48f*width, .46f, .34f, h, d.clothes); Add(root, "Face plate", PrimitiveType.Cube, 0, .83f, .18f, .37f, .19f, .08f, h, d.accent); Pair(root, "Joint", PrimitiveType.Sphere, .36f, .58f, 0, .15f, .15f, .15f, h, d.accent); break;
                case Silhouette.Mask: Add(root, "Carved mask", PrimitiveType.Cube, 0, .83f, .17f, .46f, .4f, .09f, h, d.accent); Pair(root, "Mask plume", PrimitiveType.Capsule, .3f, .97f, 0, .1f, .39f, .08f, h, d.clothes); break;
            }
            switch (d.headwear)
            {
                case Headwear.Hair: case Headwear.Curls: Add(root, "Hair", PrimitiveType.Sphere, 0, .96f, -.04f, .43f, d.headwear == Headwear.Curls ? .28f : .18f, .37f, h, d.clothes); break;
                case Headwear.Braid: Add(root, "Hair", PrimitiveType.Sphere, 0, .96f, -.04f, .4f, .17f, .36f, h, d.clothes); Add(root, "Braid", PrimitiveType.Capsule, .22f, .68f, -.17f, .11f, .5f, .11f, h, d.clothes); break;
                case Headwear.Cap: case Headwear.Helmet: Add(root, "Cap", PrimitiveType.Cylinder, 0, 1.005f, 0, .43f, d.headwear == Headwear.Helmet ? .22f : .1f, .38f, h, d.clothes); Add(root, "Brim", PrimitiveType.Cube, 0, .98f, .18f, .45f, .04f, .21f, h, d.accent); break;
                case Headwear.WideHat: Add(root, "Wide brim", PrimitiveType.Cylinder, 0, 1.02f, 0, .66f, .04f, .66f, h, d.clothes); Add(root, "Hat crown", PrimitiveType.Cylinder, 0, 1.11f, 0, .31f, .19f, .31f, h, d.accent); break;
                case Headwear.Hood: Add(root, "Hood", PrimitiveType.Sphere, 0, .84f, -.07f, .49f, .48f, .42f, h, d.clothes); break;
                case Headwear.Crown: Add(root, "Crown", PrimitiveType.Cylinder, 0, 1.05f, 0, .38f, .15f, .38f, h, d.accent); break;
                case Headwear.Horns: Pair(root, "Horn", PrimitiveType.Capsule, .22f, 1.05f, 0, .12f, .31f, .12f, h, d.accent); break;
                case Headwear.Ears: Pair(root, "Pointed ear", PrimitiveType.Cube, .29f, .85f, 0, .27f, .12f, .09f, h, d.accent); break;
                case Headwear.Beard: Add(root, "Beard", PrimitiveType.Capsule, 0, .68f, .19f, .29f, .27f, .14f, h, d.clothes); break;
                case Headwear.Visor: Add(root, "Visor", PrimitiveType.Cube, 0, .85f, .2f, .4f, .08f, .08f, h, d.accent); break;
                case Headwear.Crest: Add(root, "Crest", PrimitiveType.Capsule, 0, 1.04f, 0, .14f, .33f, .33f, h, d.accent); break;
            }
            switch (d.accessory)
            {
                case Accessory.None: break;
                case Accessory.Cape: Add(root, "Cape", PrimitiveType.Cube, 0, .47f, -.22f, .62f, .62f, .07f, h, d.accent); break;
                case Accessory.Armor: Add(root, "Breastplate", PrimitiveType.Cube, 0, .55f, .17f, .48f, .4f, .07f, h, d.accent); break;
                case Accessory.Apron: Add(root, "Apron", PrimitiveType.Cube, 0, .38f, .19f, .42f, .42f, .06f, h, d.accent); break;
                case Accessory.Tie: Add(root, "Tie", PrimitiveType.Cube, 0, .56f, .18f, .09f, .36f, .04f, h, d.accent); break;
                case Accessory.Badge: case Accessory.Medical: case Accessory.Circuit: Add(root, d.accessory.ToString(), PrimitiveType.Cube, -.14f, .64f, .2f, .15f, .15f, .04f, h, d.accent); break;
                case Accessory.Headset: case Accessory.Microphone: Add(root, "Headset band", PrimitiveType.Cylinder, 0, .96f, 0, .42f, .04f, .42f, h, d.accent); Add(root, "Microphone", PrimitiveType.Capsule, .28f, .75f, .18f, .04f, .23f, .04f, h, d.accent); break;
                case Accessory.Goggles: Add(root, "Goggles", PrimitiveType.Cube, 0, .86f, .2f, .4f, .1f, .08f, h, d.accent); break;
                case Accessory.Scarf: Add(root, "Scarf", PrimitiveType.Cylinder, 0, .69f, 0, .47f, .1f, .37f, h, d.accent); break;
                case Accessory.Shield: Add(root, "Shield", PrimitiveType.Cylinder, .48f, .45f, .13f, .33f, .07f, .33f, h, d.accent); break;
                default:
                    Add(root, d.accessory.ToString(), d.accessory == Accessory.Flower || d.accessory == Accessory.Food || d.accessory == Accessory.Coin || d.accessory == Accessory.Trophy ? PrimitiveType.Sphere : PrimitiveType.Cube, .47f, .42f, .22f, .22f, .25f, .13f, h, d.accent);
                    if (d.accessory == Accessory.Bow || d.accessory == Accessory.Staff || d.accessory == Accessory.Tool)
                        Add(root, "Handle", PrimitiveType.Capsule, .48f, .39f, .22f, .06f, .57f, .06f, h, d.clothes);
                    break;
            }
            return root;
        }

        private static void Pair(GameObject root, string name, PrimitiveType shape, float x, float y, float z, float sx, float sy, float sz, float h, Material material)
        {
            Add(root, "Left " + name, shape, -x, y, z, sx, sy, sz, h, material);
            Add(root, "Right " + name, shape, x, y, z, sx, sy, sz, h, material);
        }

        private static void Add(GameObject root, string name, PrimitiveType shape, float x, float y, float z, float sx, float sy, float sz, float h, Material material)
        {
            Part(root.transform, name, shape, new Vector3(x, y, z) * h, new Vector3(sx, sy, sz) * h, material);
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
            return new Material(shader) { color = color, hideFlags = HideFlags.DontSave, enableInstancing=true };
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

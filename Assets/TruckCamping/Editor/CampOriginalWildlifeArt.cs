using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using LWS.TruckMechanicalBull.Editor;
namespace LWS.TruckCamping.Editor
{
    // Finished original missing wildlife, not substitute vendor models or static
    // primitive stand-ins. One skinned mesh, anatomical joint chains, muzzle,
    // eyes, ears, feet and species silhouette; the original rig is animated by
    // CampLivingArt. Purchased/source assets are never edited.
    public static class CampOriginalWildlifeArt
    {
        public enum Species { Bear, Deer, Rabbit }
        sealed class Skin
        {
            public readonly List<Vector3> vertices = new List<Vector3>();
            public readonly List<Vector2> uv = new List<Vector2>();
            public readonly List<BoneWeight> weights = new List<BoneWeight>();
            public readonly List<int>[] triangles = { new List<int>(), new List<int>(), new List<int>(), new List<int>() };
            public void Ovoid(Vector3 center, Vector3 size, int bone, int material, Quaternion rotation = default)
            {
                if (rotation == default) rotation = Quaternion.identity;
                const int rings = 12, sides = 16; int first = vertices.Count;
                for (int y = 0; y <= rings; y++) for (int x = 0; x <= sides; x++)
                {
                    float a = y * Mathf.PI / rings, b = x * Mathf.PI * 2 / sides;
                    var p = new Vector3(Mathf.Sin(a) * Mathf.Cos(b), Mathf.Cos(a), Mathf.Sin(a) * Mathf.Sin(b));
                    vertices.Add(center + rotation * Vector3.Scale(p, size) * .5f); uv.Add(new Vector2(x / (float)sides, y / (float)rings)); weights.Add(new BoneWeight { boneIndex0 = bone, weight0 = 1 });
                }
                for (int y = 0; y < rings; y++) for (int x = 0; x < sides; x++)
                {
                    int a = first + y * (sides + 1) + x, b = a + sides + 1;
                    if (y > 0) triangles[material].AddRange(new[] { a, a + 1, b });
                    if (y < rings - 1) triangles[material].AddRange(new[] { a + 1, b + 1, b });
                }
            }
            public void Taper(Vector3 from, Vector3 to, float radius, int bone, int material)
            {
                var axis = to - from; int start = vertices.Count; var rotation = Quaternion.FromToRotation(Vector3.up, axis.normalized);
                for (int row = 0; row < 6; row++) for (int s = 0; s <= 10; s++)
                {
                    float t = row / 5f, angle = s * Mathf.PI * 2 / 10;
                    vertices.Add(Vector3.Lerp(from, to, t) + rotation * new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * Mathf.Lerp(radius, .008f, t));
                    uv.Add(new Vector2(s / 10f, t)); weights.Add(new BoneWeight { boneIndex0 = bone, weight0 = 1 });
                }
                for (int row = 0; row < 5; row++) for (int s = 0; s < 10; s++) { int a = start + row * 11 + s, b = a + 11; triangles[material].AddRange(new[] { a, b, a + 1, a + 1, b, b + 1 }); }
            }
        }
        public static GameObject Prefab(Species species)
        {
            string folder = "Assets/TruckCamping/Prefabs"; System.IO.Directory.CreateDirectory(folder); AssetDatabase.Refresh();
            string path = folder + "/Original animated " + species + ".prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path); if (existing != null) return existing;
            var root = new GameObject("Original woodland " + species); var model = new GameObject("Jointed original " + species + " skin").transform; model.SetParent(root.transform, false);
            bool deer = species == Species.Deer, rabbit = species == Species.Rabbit;
            float height = deer ? 1.8f : rabbit ? .62f : 1.5f, length = deer ? 2.2f : rabbit ? .85f : 2.7f, width = deer ? .62f : rabbit ? .48f : 1.25f;
            float hip = height * .59f, shoulder = height * .65f, rearZ = -length * .28f, frontZ = length * .24f;
            var bones = new List<Transform>(); var roles = new List<CampBone>();
            Func<string, CampBone, Transform, Vector3, int> joint = (name, role, parent, position) => { var b = new GameObject(name).transform; b.SetParent(parent, false); b.position = position; bones.Add(b); roles.Add(role); return bones.Count - 1; };
            int pelvis = joint("Pelvis", CampBone.Pelvis, model, new Vector3(0, hip, rearZ));
            int spine = joint("Flexible chest", CampBone.Spine, bones[pelvis], new Vector3(0, shoulder, frontZ));
            int head = joint("Neck and expressive head", CampBone.Head, bones[spine], new Vector3(0, height * .92f, length * .4f));
            int tail = joint("Tail", CampBone.Tail, bones[pelvis], new Vector3(0, hip, -length * .49f));
            var skin = new Skin();
            skin.Ovoid(new Vector3(0, hip, -length * .12f), new Vector3(width, height * .68f, length * .72f), pelvis, 0);
            skin.Ovoid(new Vector3(0, shoulder, length * .16f), new Vector3(width * .92f, height * .68f, length * .55f), spine, 0);
            var headPosition = bones[head].position;
            skin.Ovoid(headPosition, new Vector3(width * .78f, height * .44f, length * .35f), head, 0, Quaternion.Euler(-12, 0, 0));
            skin.Ovoid(headPosition + new Vector3(0, -height * .08f, length * .17f), new Vector3(width * .58f, height * .19f, length * .29f), head, 1);
            skin.Ovoid(headPosition + new Vector3(0, -height * .04f, length * .31f), new Vector3(width * .3f, height * .09f, length * .06f), head, 2);
            for (int side = -1; side <= 1; side += 2)
            {
                var eye = headPosition + new Vector3(side * width * .31f, height * .035f, length * .115f);
                skin.Ovoid(eye, Vector3.one * height * .072f, head, 2); skin.Ovoid(eye + new Vector3(side * .005f, height * .015f, .025f), Vector3.one * height * .021f, head, 3);
                int ear = joint(side < 0 ? "Left ear" : "Right ear", side < 0 ? CampBone.LeftEar : CampBone.RightEar, bones[head], headPosition + new Vector3(side * width * .3f, height * .19f, -length * .05f));
                var earSize = new Vector3(width * .25f, rabbit ? height * .68f : height * .24f, length * .09f);
                skin.Ovoid(bones[ear].position + Vector3.up * earSize.y * .3f, earSize, ear, 0, Quaternion.Euler(0, 0, side * -17));
                skin.Ovoid(bones[ear].position + Vector3.up * earSize.y * .3f + Vector3.forward * length * .037f, Vector3.Scale(earSize, new Vector3(.6f, .68f, .25f)), ear, 1, Quaternion.Euler(0, 0, side * -17));
                for (int front = 0; front < 2; front++)
                {
                    bool f = front == 1, left = side < 0;
                    var upperPos = new Vector3(side * width * .34f, f ? shoulder : hip, f ? frontZ : rearZ);
                    int upper = joint((left ? "Left " : "Right ") + (f ? "front shoulder" : "rear haunch"), f ? left ? CampBone.FrontLeft : CampBone.FrontRight : left ? CampBone.RearLeft : CampBone.RearRight, bones[f ? spine : pelvis], upperPos);
                    float knee = height * (deer ? .28f : .2f);
                    int lower = joint((left ? "Left " : "Right ") + (f ? "front knee" : "rear hock"), f ? left ? CampBone.FrontLeftLower : CampBone.FrontRightLower : left ? CampBone.RearLeftLower : CampBone.RearRightLower, bones[upper], new Vector3(upperPos.x, knee, upperPos.z - height * .04f));
                    skin.Ovoid(Vector3.Lerp(upperPos, bones[lower].position, .45f), new Vector3(width * (deer ? .22f : .42f), upperPos.y - knee + .2f, length * (deer ? .16f : .23f)), upper, 0);
                    skin.Ovoid(new Vector3(upperPos.x, knee * .5f + .04f, upperPos.z), new Vector3(width * (deer ? .15f : .31f), knee + .08f, length * .12f), lower, 0);
                    skin.Ovoid(new Vector3(upperPos.x, .07f, upperPos.z + length * .045f), new Vector3(width * .29f, .14f, length * (rabbit && !f ? .25f : .16f)), lower, deer ? 2 : 1);
                    if (!deer) for (int toe = -1; toe <= 1; toe++) skin.Ovoid(new Vector3(upperPos.x + toe * width * .065f, .06f, upperPos.z + length * .12f), new Vector3(.025f, .035f, .06f), lower, 2);
                }
                if (deer)
                {
                    var start = headPosition + new Vector3(side * .2f, .25f, -.12f); var top = start + new Vector3(side * .3f, .8f, -.35f);
                    skin.Taper(start, top, .065f, head, 1);
                    for (int fork = 0; fork < 3; fork++) { var from = Vector3.Lerp(start, top, .3f + fork * .22f); skin.Taper(from, from + new Vector3(side * .2f, .28f, .22f), .03f, head, 1); }
                }
            }
            skin.Ovoid(bones[tail].position, new Vector3(width * .28f, height * .18f, length * .13f), tail, rabbit ? 3 : 0);
            var mesh = new Mesh { name = "Original " + species + " articulated skin", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.SetVertices(skin.vertices); mesh.SetUVs(0, skin.uv); mesh.boneWeights = skin.weights.ToArray(); mesh.bindposes = bones.Select(b => b.worldToLocalMatrix * model.localToWorldMatrix).ToArray(); mesh.subMeshCount = 4;
            for (int i = 0; i < 4; i++) mesh.SetTriangles(skin.triangles[i], i); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            string meshPath = "Assets/TruckCamping/Models/Original " + species + " articulated skin.asset"; System.IO.Directory.CreateDirectory("Assets/TruckCamping/Models"); AssetDatabase.Refresh(); AssetDatabase.CreateAsset(mesh, meshPath);
            var art = model.gameObject.AddComponent<SkinnedMeshRenderer>(); art.sharedMesh = mesh; art.bones = bones.ToArray(); art.rootBone = bones[pelvis]; art.updateWhenOffscreen = true;
            Color fur = deer ? new Color(.47f, .26f, .11f) : rabbit ? new Color(.57f, .53f, .46f) : new Color(.23f, .13f, .075f);
            art.sharedMaterials = new[] { ImpossibleBalanceArt.Material("TruckCamping", species + " original fur", fur), ImpossibleBalanceArt.Material("TruckCamping", species + " muzzle inner ear and claws", deer ? new Color(.78f,.65f,.43f) : new Color(.66f,.48f,.37f)), ImpossibleBalanceArt.Material("TruckCamping", "Original wildlife dark eyes and noses", new Color(.015f,.01f,.008f), .75f), ImpossibleBalanceArt.Material("TruckCamping", "Original wildlife eye glint", new Color(.92f,.9f,.82f), .8f) };
            var living = root.AddComponent<CampLivingArt>(); living.model = model; living.bones = bones.ToArray(); living.roles = roles.ToArray(); living.groundSize = height;
            try { return PrefabUtility.SaveAsPrefabAsset(root, path); } finally { UnityEngine.Object.DestroyImmediate(root); }
        }
    }
}

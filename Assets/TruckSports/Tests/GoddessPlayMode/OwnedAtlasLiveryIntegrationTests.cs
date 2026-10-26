#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

// Controlled closeups render the actual saved-venue hosts. This proves livery
// geometry/color visibility, not gameplay-camera composition or art acceptance.
public sealed class OwnedAtlasLiveryIntegrationTests
{
    const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    const string Part = "Owned surface-following participant livery";
    static Type Sports(string name) => Type.GetType("LWS.TruckSports." + name + ", Assembly-CSharp", true);
    static object Get(object owner, string name) => owner.GetType().GetField(name, Any) is FieldInfo field ? field.GetValue(owner) : owner.GetType().GetProperty(name, Any).GetValue(owner);
    static void Set(object owner, string name, object value)
    {
        var field = owner.GetType().GetField(name, Any); if (field != null) field.SetValue(owner, value); else owner.GetType().GetProperty(name, Any).SetValue(owner, value);
    }
    static object Call(object owner, string name, params object[] args) => owner.GetType().GetMethods(Any).Single(method => method.Name == name && method.GetParameters().Length == args.Length).Invoke(owner, args);
    static object Registry(string name, params object[] args) => Sports("SportsParticipantRegistry").GetMethod(name).Invoke(null, args);
    Component match; GameObject isolation; IDisposable lease;
    [TearDown] public void Cleanup()
    {
        if (match != null) Call(match, "SetupScreen"); Time.timeScale = 1;
        if (isolation != null) UnityEngine.Object.DestroyImmediate(isolation);
        Registry("ForgetCache"); lease?.Dispose(); lease = null;
    }
    static Texture2D Read(Camera camera, RenderTexture target)
    {
        camera.Render(); var active = RenderTexture.active;
        try
        {
            RenderTexture.active = target; var image = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false);
            image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); image.Apply(); return image;
        }
        finally { RenderTexture.active = active; }
    }
    static Color VisibleLivery(GameObject model, Renderer livery, string label)
    {
        Assert.That(model.GetComponentsInChildren<Collider>(true), Is.Empty);
        var transforms = model.GetComponentsInChildren<Transform>(true); var layers = transforms.Select(item => item.gameObject.layer).ToArray();
        var cameraObject = new GameObject("Controlled actual livery closeup"); var lightObject = new GameObject("Controlled livery-only light");
        var target = new RenderTexture(640, 360, 24, RenderTextureFormat.ARGB32); Texture2D before = null, after = null;
        bool enabled = livery.enabled;
        try
        {
            foreach (var item in transforms) item.gameObject.layer = 31;
            var camera = cameraObject.AddComponent<Camera>(); camera.enabled = false; camera.cameraType = CameraType.Preview;
            camera.cullingMask = 1 << 31; camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.035f, .035f, .035f);
            camera.targetTexture = target; camera.orthographic = true; camera.aspect = 640f / 360; camera.nearClipPlane = .03f; camera.farClipPlane = 200;
            var dataType = Type.GetType("UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime", true);
            var data = cameraObject.AddComponent(dataType); Set(data, "renderPostProcessing", false); Set(data, "renderShadows", false); Set(data, "volumeLayerMask", (LayerMask)0);
            var renderers = model.GetComponentsInChildren<Renderer>(true).Where(renderer => renderer.enabled && renderer.gameObject.activeInHierarchy).ToArray();
            Assert.That(renderers, Is.Not.Empty); var bounds = renderers[0].bounds; foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
            var direction = livery.transform.TransformDirection(new Vector3(1, .25f, .85f)).normalized;
            camera.transform.SetPositionAndRotation(bounds.center + direction * bounds.size.magnitude * 3, Quaternion.LookRotation(-direction, Vector3.up));
            camera.orthographicSize = bounds.size.magnitude * .6f;
            var light = lightObject.AddComponent<Light>(); light.type = LightType.Directional; light.cullingMask = 1 << 31; light.intensity = 1.8f; light.shadows = LightShadows.None;
            light.transform.rotation = camera.transform.rotation;
            livery.enabled = false; before = Read(camera, target); livery.enabled = true; after = Read(camera, target);
            var a = before.GetPixels32(); var b = after.GetPixels32(); int changed = 0; Color sum = Color.clear;
            for (int i = 0; i < a.Length; i++)
            {
                int difference = Math.Abs(a[i].r - b[i].r) + Math.Abs(a[i].g - b[i].g) + Math.Abs(a[i].b - b[i].b);
                if (difference < 18) continue; changed++; sum += (Color)b[i];
            }
            string path = Path.GetFullPath("Logs/TruckSports/Carnival/OwnedLivery/CONTROLLED-" + label + ".png"); Directory.CreateDirectory(Path.GetDirectoryName(path)); File.WriteAllBytes(path, after.EncodeToPNG());
            if (changed == 0)
            {
                var mesh = livery.GetComponent<MeshFilter>().sharedMesh;
                TestContext.WriteLine("LIVERY_RENDER_DIAGNOSTIC bounds=" + livery.bounds + " meshBounds=" + mesh.bounds + " verts=" + mesh.vertexCount + " triangles=" + mesh.triangles.Length + " enabled=" + livery.enabled + " forceRenderingOff=" + livery.forceRenderingOff + " layer=" + livery.gameObject.layer + " determinant=" + livery.localToWorldMatrix.determinant + " shader=" + livery.sharedMaterial.shader.name + " cull=" + livery.sharedMaterial.GetFloat("_Cull"));
                var originalEnabled = renderers.Select(renderer => renderer.enabled).ToArray();
                try
                {
                    foreach (var renderer in renderers) renderer.enabled = renderer == livery;
                    var only = Read(camera, target); File.WriteAllBytes(path.Replace(".png", "-livery-only.png"), only.EncodeToPNG()); UnityEngine.Object.Destroy(only);
                }
                finally { for (int i = 0; i < renderers.Length; i++) renderers[i].enabled = originalEnabled[i]; }
            }
            Assert.That(changed, Is.GreaterThan(200), "Actual livery geometry must visibly change the controlled native image; capture=" + path);
            var mean = sum / changed; TestContext.WriteLine("CONTROLLED_OWNED_LIVERY " + label + " visibleChangedPixels=" + changed + " meanRGB=" + mean + " capture=" + path); return mean;
        }
        finally
        {
            livery.enabled = enabled; for (int i = 0; i < transforms.Length; i++) if (transforms[i] != null) transforms[i].gameObject.layer = layers[i];
            cameraObject.SetActive(false); lightObject.SetActive(false); UnityEngine.Object.Destroy(cameraObject); UnityEngine.Object.Destroy(lightObject);
            target.Release(); UnityEngine.Object.Destroy(target); if (before != null) UnityEngine.Object.Destroy(before); if (after != null) UnityEngine.Object.Destroy(after);
        }
    }
    [UnityTest, Timeout(180000)]
    public IEnumerator EightEffectiveColorsAreVisibleOnRealFireAndIcecreamBodiesWithoutAtlasOrGlassTinting()
    {
        lease = (IDisposable)Sports("SportsPrefs").GetMethod("IsolateValidation").Invoke(null, new object[] { "OwnedLivery" + Guid.NewGuid().ToString("N") });
        Registry("ForgetCache"); isolation = new GameObject("Owned livery native isolation"); UnityEngine.Object.DontDestroyOnLoad(isolation); isolation.AddComponent(Sports("SportsProbeInputIsolation"));
        EditorSceneManager.LoadSceneInPlayMode("Assets/TruckMusicalParking/Scenes/TruckMusicalParking.unity", new LoadSceneParameters(LoadSceneMode.Single));
        yield return null; yield return null;
        match = (Component)UnityEngine.Object.FindAnyObjectByType(Sports("SportsEventSession")); Assert.That(match, Is.Not.Null);
        Call(match, "AllBots"); Set(Get(match, "setup"), "count", 8); Call(match, "NormalizeSetup");
        var trucks = ((Array)Get(match, "trucks")).Cast<Component>().ToArray(); Assert.That(trucks.Length, Is.EqualTo(8));
        var ids = new string[8]; var preferred = new Color[8];
        for (int i = 0; i < 8; i++)
        {
            var profile = Registry("Create", "Livery driver " + i); ids[i] = (string)Get(profile, "id"); preferred[i] = (Color)Get(profile, "color"); Registry("UseInSeat", i, ids[i]);
        }
        foreach (string bodyId in new[] { "truck.fire", "novelty.icecream" })
        {
            for (int i = 0; i < 8; i++)
            { var draft = Registry("GetAppearance", ids[i]); Set(draft, "bodyId", bodyId); Assert.That(Registry("TrySetAppearance", ids[i], draft), Is.True); }
            Call(match, "RefreshParticipants", false); Assert.That(Call(match, "StartMatch"), Is.True); ((Behaviour)match).enabled = false; Time.timeScale = 0;
            var players = (Array)Get(Get(match, "setup"), "players"); var colors = Enumerable.Range(0, 8).Select(i => (Color)Get(players.GetValue(i), "color")).ToArray(); var means = new Color[8];
            for (int i = 0; i < 8; i++) for (int j = 0; j < i; j++)
                Assert.That((float)Sports("SportsMatchColors").GetMethod("Distance").Invoke(null, new object[] { colors[i], colors[j] }), Is.GreaterThanOrEqualTo(21.99f));
            for (int i = 0; i < 8; i++)
            {
                var host = Get(trucks[i], "appearanceHost"); Assert.That(host, Is.Not.Null); Assert.That(Get(host, "EffectiveBodyId"), Is.EqualTo(bodyId));
                var body = Call(Get(host, "catalog"), "FindBody", bodyId); var source = ((GameObject)Get(body, "visualPrefab")).GetComponentsInChildren<Renderer>(true);
                var parts = (Renderer[])Get(host, "paintRenderers"); Assert.That(parts.Length, Is.EqualTo(source.Length));
                var livery = parts.Single(renderer => renderer.name == Part); var block = new MaterialPropertyBlock();
                for (int k = 0; k < parts.Length; k++)
                {
                    CollectionAssert.AreEqual(source[k].sharedMaterials, parts[k].sharedMaterials, "Vendor atlas/glass/chrome materials must retain actual asset references.");
                    if (source[k].TryGetComponent<MeshFilter>(out var sourceMesh)) Assert.That(parts[k].GetComponent<MeshFilter>().sharedMesh, Is.SameAs(sourceMesh.sharedMesh));
                    for (int slot = 0; slot < parts[k].sharedMaterials.Length; slot++)
                    {
                        block.Clear(); parts[k].GetPropertyBlock(block, slot);
                        if (parts[k] == livery)
                        {
                            Assert.That(parts[k].sharedMaterials[slot].name, Is.EqualTo("Sports participant livery"));
                            var actual = block.GetColor("_BaseColor");
                            float delta = Mathf.Max(Mathf.Abs(actual.r-colors[i].r),Mathf.Abs(actual.g-colors[i].g),Mathf.Abs(actual.b-colors[i].b),Mathf.Abs(actual.a-colors[i].a));
                            TestContext.WriteLine("LIVERY_MPB_NATIVE_STORAGE slot="+i+" maxChannelDelta="+delta.ToString("R"));
                            Assert.That(delta,Is.LessThanOrEqualTo(.000002f),"Native MPB channel storage must retain the actual effective color within2e-6; profile bytes remain exact.");
                        }
                        else Assert.That(block.isEmpty, Is.True, "A combined vendor atlas or glass renderer must not receive participant tint: " + parts[k].name);
                    }
                }
                means[i] = VisibleLivery((GameObject)Get(host, "model"), livery, bodyId + "-slot" + i + "-" + ColorUtility.ToHtmlStringRGB(colors[i]));
                Assert.That((Color)Get(Registry("Find", ids[i]), "color"), Is.EqualTo(preferred[i]), "The effective match palette may not overwrite preferred profile colors.");
            }
            for (int i = 0; i < 8; i++) for (int j = 0; j < i; j++)
                Assert.That(Vector3.Distance(new Vector3(means[i].r, means[i].g, means[i].b), new Vector3(means[j].r, means[j].g, means[j].b)), Is.GreaterThan(.015f), "Eight effective colors must also produce distinguishable native livery pixels: " + bodyId + " slots " + i + "/" + j);
        }
        TestContext.WriteLine("NATIVE_CONTROLLED_LIVERY16 actual saved hosts,8 effective colors on each atlas body, visible surface geometry, untouched vendor materials/meshes and retained preferred colors. No gameplay framing/human art acceptance claimed.");
    }
}
#endif

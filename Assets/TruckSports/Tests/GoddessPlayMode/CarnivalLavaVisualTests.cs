#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class CarnivalLavaVisualTests
{
    const BindingFlags Any = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    static Type Runtime(string name) => Type.GetType("LWS.TruckSports." + name + ", Assembly-CSharp", true);
    static object Get(object owner, string member) => owner.GetType().GetField(member, Any) is FieldInfo field ? field.GetValue(owner) : owner.GetType().GetProperty(member, Any).GetValue(owner);
    static void Set(object owner, string member, object value) => owner.GetType().GetField(member, Any).SetValue(owner, value);
    static object Call(object owner, string method, params object[] args) => owner.GetType().GetMethod(method, Any).Invoke(owner, args);
    IDisposable lease;
    GameObject isolation, synthetic;
    [TearDown] public void Cleanup()
    {
        Time.timeScale = 1;
        if (synthetic != null) UnityEngine.Object.DestroyImmediate(synthetic);
        if (isolation != null) UnityEngine.Object.DestroyImmediate(isolation);
        lease?.Dispose(); lease = null;
    }
    IEnumerator Load(string name)
    {
        if (lease == null)
        {
            lease = (IDisposable)Runtime("SportsPrefs").GetMethod("IsolateValidation").Invoke(null, new object[] { "CarnivalLava" + Guid.NewGuid().ToString("N") });
            isolation = new GameObject("Explicit molten rendering validation"); UnityEngine.Object.DontDestroyOnLoad(isolation); isolation.AddComponent(Runtime("SportsProbeInputIsolation"));
        }
        EditorSceneManager.LoadSceneInPlayMode("Assets/" + name + "/Scenes/" + name + ".unity", new LoadSceneParameters(LoadSceneMode.Single));
        yield return null; yield return null;
    }
    static string Authority(Scene scene)
    {
        var components = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Component>(true)).Where(component => component != null).ToArray();
        return string.Join("|", components.OfType<Collider>().OrderBy(collider => collider.GetInstanceID()).Select(collider => collider.GetInstanceID() + ":" + collider.enabled + ":" + collider.isTrigger + ":" + (collider is BoxCollider box ? box.center + ":" + box.size : collider.bounds.size.ToString()))) +
            "/bodies=" + string.Join("|", components.OfType<Rigidbody>().OrderBy(body => body.GetInstanceID()).Select(body => body.GetInstanceID() + ":" + body.mass + ":" + body.centerOfMass)) +
            "/lights=" + string.Join("|", components.OfType<Light>().OrderBy(light => light.GetInstanceID()).Select(light => light.GetInstanceID() + ":" + light.enabled + ":" + light.type + ":" + light.intensity + ":" + light.range + ":" + light.color));
    }
    [UnityTest, Timeout(180000)]
    public IEnumerator FourSavedLavaVenuesUseOwnedFlowAndBoundedEffectsWithoutChangingPhysicalOrLightAuthority()
    {
        foreach (string name in new[] { "TruckTargetLaunch", "TruckTerrierRacing", "TruckFloorIsLava", "TruckTugOfWar" })
        {
            yield return Load(name); var presentation = (Component)UnityEngine.Object.FindAnyObjectByType(Runtime("SportsCarnivalLavaSurface"));
            Assert.That(presentation, Is.Not.Null, "Explicit InstallMissingPresentations is required for " + name);
            var renderer = (Renderer)Get(presentation, "surface"); Assert.That(renderer.sharedMaterial.shader.name, Is.EqualTo("LWS/TruckSports/Carnival Molten Surface"));
            Assert.That(renderer.sharedMaterial.shader.isSupported, Is.True);
            var ambient = ((ParticleSystem[])Get(presentation, "ambientEffects")); var reactions = ((ParticleSystem[])Get(presentation, "reactionPool"));
            Assert.That(reactions.Length, Is.EqualTo(4)); Assert.That(ambient.Length, Is.InRange(6, 15));
            Assert.That(ambient.Concat(reactions).Sum(system => system.main.maxParticles), Is.LessThanOrEqualTo(600));
            Assert.That(presentation.GetComponentsInChildren<Collider>(true), Is.Empty); Assert.That(presentation.GetComponentsInChildren<Rigidbody>(true), Is.Empty); Assert.That(presentation.GetComponentsInChildren<Light>(true), Is.Empty);
            var observers = presentation.gameObject.scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Component>(true)).Where(component => component != null && component.GetType() == Runtime("SportsCarnivalLavaContact")).ToArray();
            Assert.That(observers, Is.Not.Empty); Assert.That(observers.All(observer => observer.GetComponent<Collider>() != null), Is.True);
            Time.timeScale = 0; float clock = (float)Get(presentation, "PresentationTime"); yield return null; yield return null;
            Assert.That((float)Get(presentation, "PresentationTime"), Is.EqualTo(clock));
            string before = Authority(presentation.gameObject.scene);
            Call(presentation, "Advance", 8f); Assert.That(Authority(presentation.gameObject.scene), Is.EqualTo(before), name);
            var block = new MaterialPropertyBlock(); renderer.GetPropertyBlock(block); Assert.That(block.GetFloat("_LavaTime"), Is.EqualTo(clock + 8f));
            var original = (Material[])Get(presentation, "originalMaterials"); ((Behaviour)presentation).enabled = false;
            CollectionAssert.AreEqual(original, renderer.sharedMaterials); Assert.That(reactions.All(system => !system.IsAlive(true)), Is.True);
            Assert.That(Authority(presentation.gameObject.scene), Is.EqualTo(before), name + " disable restoration");
            TestContext.WriteLine("SAVED_MOLTEN_CONFIG " + name + " existing collider/rigidbody/light state retained; scaled-time pause, owned material restoration and bounded VFX checked. No complete gameplay or human art acceptance.");
            Time.timeScale = 1;
        }
    }
    [UnityTest, Timeout(90000)]
    public IEnumerator ActualAnonymousDebrisCrossesExistingLavaVolumeAndProducesOnlyBoundedPresentationReaction()
    {
        yield return Load("TruckFloorIsLava"); var presentation = (Component)UnityEngine.Object.FindAnyObjectByType(Runtime("SportsCarnivalLavaSurface"));
        Assert.That(presentation, Is.Not.Null); var match = UnityEngine.Object.FindAnyObjectByType(Type.GetType("LWS.TruckFloorIsLava.LavaMatch, Assembly-CSharp", true));
        int originalImpacts = (int)Get(match, "LavaImpacts"); int reactions = (int)Get(presentation, "Reactions"); var bounds = (Bounds)Get(presentation, "worldEnvelope");
        synthetic = new GameObject("Explicit anonymous debris contact fixture"); synthetic.transform.position = new Vector3(bounds.center.x, (float)Get(presentation, "surfaceHeight") + 3, bounds.center.z);
        synthetic.AddComponent<BoxCollider>().size = Vector3.one * .4f; var body = synthetic.AddComponent<Rigidbody>(); body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; body.linearVelocity = Vector3.down * 3;
        Time.timeScale = 1; double deadline = Time.realtimeSinceStartupAsDouble + 5;
        while ((int)Get(presentation, "Reactions") == reactions && Time.realtimeSinceStartupAsDouble < deadline) yield return null;
        Assert.That((int)Get(presentation, "Reactions"), Is.GreaterThan(reactions), "Real anonymous debris must enter the existing lava volume.");
        Assert.That((int)Get(match, "LavaImpacts"), Is.EqualTo(originalImpacts), "A visual observer must never award or eliminate a truck for anonymous debris.");
        Assert.That(((ParticleSystem[])Get(presentation, "reactionPool")).Any(system => system.isPlaying), Is.True);
        Assert.That(Call(presentation, "ObserveImpact", body.worldCenterOfMass, body.GetInstanceID(), true), Is.False, "Duplicate same-body contact is bounded.");
        Assert.That(Call(presentation, "ObserveImpact", bounds.max + Vector3.one * 5000, 0, true), Is.False);
        TestContext.WriteLine("ACTUAL_MOLTEN_CONTACT anonymous physical debris crossed the existing Floor Is Lava trigger; bounded droplets/ripple reacted while original truck elimination count stayed unchanged.");
    }
    [UnityTest, Timeout(90000)]
    public IEnumerator SyntheticRenderedMoltenCurrentsChangePixelsAndFreezeAtTheSamePresentationTime()
    {
        var shader = Shader.Find("LWS/TruckSports/Carnival Molten Surface"); Assert.That(shader, Is.Not.Null); Assert.That(shader.isSupported, Is.True);
        var root = new GameObject("SYNTHETIC molten shader pixels"); root.SetActive(false); root.layer = 31; root.transform.position = new Vector3(30000, -30000, 30000); synthetic = root;
        var mesh = new Mesh { name = "Synthetic flat molten render patch" }; mesh.vertices = new[] { new Vector3(-32, 0, -32), new Vector3(-32, 0, 32), new Vector3(32, 0, 32), new Vector3(32, 0, -32) }; mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 }; mesh.RecalculateNormals(); mesh.RecalculateBounds();
        root.AddComponent<MeshFilter>().sharedMesh = mesh; var renderer = root.AddComponent<MeshRenderer>();
        var baseline = new Material(Shader.Find("Universal Render Pipeline/Unlit")); var molten = new Material(shader); renderer.sharedMaterial = baseline;
        var presentation = root.AddComponent(Runtime("SportsCarnivalLavaSurface")); Set(presentation, "surface", renderer); Set(presentation, "moltenMaterial", molten); Set(presentation, "originalMaterials", new[] { baseline }); Set(presentation, "worldEnvelope", new Bounds(root.transform.position, new Vector3(64, 1, 64))); Set(presentation, "surfaceHeight", root.transform.position.y);
        var cameraObject = new GameObject("Synthetic molten pixel camera"); var camera = cameraObject.AddComponent<Camera>(); camera.enabled = false; camera.cameraType = CameraType.Preview; camera.orthographic = true; camera.orthographicSize = 16; camera.aspect = 2; camera.cullingMask = 1 << 31; camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black;
        camera.transform.SetPositionAndRotation(root.transform.position + Vector3.up * 20, Quaternion.Euler(90, 0, 0)); camera.nearClipPlane = .1f; camera.farClipPlane = 50;
        var texture = new RenderTexture(256, 128, 24); texture.Create(); var image = new Texture2D(256, 128, TextureFormat.RGBA32, false); var prior = RenderTexture.active;
        try
        {
            root.SetActive(true); Time.timeScale = 0; yield return null;
            Color32[] Capture(string suffix)
            {
                camera.targetTexture = texture; camera.Render(); RenderTexture.active = texture; image.ReadPixels(new Rect(0, 0, 256, 128), 0, 0); image.Apply();
                Directory.CreateDirectory("Logs/TruckSports/Carnival/Lava"); File.WriteAllBytes("Logs/TruckSports/Carnival/Lava/SYNTHETIC-molten-" + suffix + ".png", image.EncodeToPNG()); return image.GetPixels32();
            }
            var first = Capture("time0"); Call(presentation, "Advance", 8f); var moved = Capture("time8"); var frozen = Capture("time8-frozen");
            int changed = first.Zip(moved, (a, b) => Math.Abs(a.r - b.r) + Math.Abs(a.g - b.g) + Math.Abs(a.b - b.b)).Count(delta => delta > 24);
            Assert.That(changed, Is.GreaterThan(first.Length / 10), "Advected currents/churning/bubbles must change rendered pixels, not merely a clock property.");
            CollectionAssert.AreEqual(moved, frozen, "Identical presentation time must freeze the actual surface image.");
            TestContext.WriteLine("SYNTHETIC_MOLTEN_SHADER changedPixels=" + changed + "/" + first.Length + " at t0/t8; repeated t8 image frozen. Isolated render patch, not actual venue art acceptance.");
        }
        finally
        {
            camera.targetTexture = null; RenderTexture.active = prior; texture.Release(); UnityEngine.Object.DestroyImmediate(cameraObject); UnityEngine.Object.DestroyImmediate(texture); UnityEngine.Object.DestroyImmediate(image); UnityEngine.Object.DestroyImmediate(root); synthetic = null; UnityEngine.Object.DestroyImmediate(mesh); UnityEngine.Object.DestroyImmediate(baseline); UnityEngine.Object.DestroyImmediate(molten);
        }
    }
}
#endif

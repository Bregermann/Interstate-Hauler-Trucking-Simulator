#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class FashionGalleryNativeTests
{
    const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    static Type Runtime(string name) => Type.GetType(name + ", Assembly-CSharp", true);
    static object Get(object o, string n) => o.GetType().GetField(n, Any) is FieldInfo f ? f.GetValue(o) : o.GetType().GetProperty(n, Any).GetValue(o);
    static object Call(object o, string name, params object[] args) => o.GetType().GetMethods(Any).Single(m => m.Name == name && m.GetParameters().Length == args.Length).Invoke(o, args);
    static Component[] Components(object array) => ((IEnumerable)array).Cast<Component>().ToArray();
    sealed class Scope : IDisposable
    {
        public Component match, gallery; public Keyboard keyboard;
        readonly IDisposable prefs; readonly InputSettings original, settings; readonly GameObject isolation;
        public Scope()
        {
            original = InputSystem.settings; settings = UnityEngine.Object.Instantiate(original); settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView; InputSystem.settings = settings;
            prefs = (IDisposable)Runtime("LWS.TruckSports.SportsPrefs").GetMethod("IsolateValidation").Invoke(null, new object[] { "FashionGallery" + Guid.NewGuid().ToString("N") });
            isolation = new GameObject("Fashion gallery owned software-input isolation"); UnityEngine.Object.DontDestroyOnLoad(isolation); isolation.AddComponent(Runtime("LWS.TruckSports.SportsProbeInputIsolation"));
            keyboard = InputSystem.AddDevice<Keyboard>(); InputSystem.EnableDevice(keyboard); keyboard.MakeCurrent();
        }
        public IEnumerator Load(bool humans)
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/TruckFashionShow/Scenes/TruckFashionShow.unity", new LoadSceneParameters(LoadSceneMode.Single)); yield return null; yield return null;
            InputSystem.EnableDevice(keyboard); keyboard.MakeCurrent(); match = (Component)UnityEngine.Object.FindAnyObjectByType(Runtime("LWS.TruckFashionShow.FashionMatch"));
            Assert.That(match, Is.Not.Null); gallery = (Component)Get(match, "gallery"); Assert.That(gallery, Is.Not.Null, "Run guarded V2 presentation repair first."); Call(match, "AllBots");
            if (humans)
            {
                var players = (Array)Get(Get(match, "setup"), "players"); Key[] forward = { Key.W, Key.UpArrow, Key.T, Key.I }, left = { Key.A, Key.LeftArrow, Key.F, Key.J }, back = { Key.S, Key.DownArrow, Key.G, Key.K }, right = { Key.D, Key.RightArrow, Key.H, Key.L };
                for (int i = 0; i < 4; i++)
                {
                    var p = players.GetValue(i); var source = p.GetType().GetField("source"); source.SetValue(p, Enum.Parse(source.FieldType, "Keyboard"));
                    p.GetType().GetField("forward").SetValue(p, forward[i]); p.GetType().GetField("back").SetValue(p, back[i]); p.GetType().GetField("left").SetValue(p, left[i]); p.GetType().GetField("right").SetValue(p, right[i]);
                }
            }
            Assert.That((bool)Call(match, "StartMatch"), Is.True); yield return null; yield return null;
        }
        public Camera View => (Camera)Get(Get(match, "broadcast"), "view");
        public void Dispose()
        {
            Time.timeScale = 1; if (match != null) Call(match, "SetupScreen");
            if (keyboard.added) InputSystem.RemoveDevice(keyboard); UnityEngine.Object.DestroyImmediate(isolation); InputSystem.settings = original; UnityEngine.Object.DestroyImmediate(settings); prefs.Dispose();
        }
        public void Capture(string name, bool hub, bool humanButtons)
        {
            string directory = Path.GetFullPath("Logs/TruckSports/PlaytestRepair/Impossible07FashionGalleryV2"); Directory.CreateDirectory(directory);
            foreach (int width in new[] { 1280, 1920 })
            {
                int height = width == 1280 ? 720 : 1080;
                string path = Path.Combine(directory, name + "-" + width + "-controlled.png");
                Action<Camera> inspect = ui => InspectUI(this, ui, hub, humanButtons);
                Runtime("LWS.TruckSports.SportsControlledCapture").GetMethods().Single(m => m.Name == "Save" && m.GetParameters().Length == 5).Invoke(null, new object[] { match, width, height, path, inspect });
                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                try
                {
                    Assert.That(texture.LoadImage(File.ReadAllBytes(path)), Is.True); Assert.That(texture.width, Is.EqualTo(width)); Assert.That(texture.height, Is.EqualTo(height));
                    var rect = ActorRect(this, hub ? 0 : (int)Get(match, "ActiveOwner")); int x0 = Mathf.Clamp((int)(rect.xMin * width), 0, width - 2), y0 = Mathf.Clamp((int)(rect.yMin * height), 0, height - 2);
                    int w = Mathf.Clamp((int)(rect.width * width), 2, width - x0), h = Mathf.Clamp((int)(rect.height * height), 2, height - y0);
                    var pixels = texture.GetPixels(x0, y0, w, h); float low = pixels.Min(c => c.grayscale), high = pixels.Max(c => c.grayscale), mean = pixels.Average(c => c.grayscale);
                    Assert.That(high - low, Is.GreaterThan(.09f), "Real controlled actor region must retain lit surface detail."); Assert.That(mean, Is.InRange(.035f, .97f), "Reject black-crushed or entirely washed-out actor regions.");
                    TestContext.WriteLine($"FASHION_GALLERY_CAPTURE {name} {width}x{height} actualActorRegion={rect} min={low:R} max={high:R} mean={mean:R} CONTROLLED_NATIVE_ONLY");
                }
                finally { UnityEngine.Object.DestroyImmediate(texture); }
            }
        }
    }
    static Rect ScreenRect(RectTransform rect, Camera camera)
    {
        var corners = new Vector3[4]; rect.GetWorldCorners(corners); var points = corners.Select(p => RectTransformUtility.WorldToScreenPoint(camera, p)).ToArray();
        return Rect.MinMaxRect(points.Min(p => p.x), points.Min(p => p.y), points.Max(p => p.x), points.Max(p => p.y));
    }
    static void InspectUI(Scope s, Camera ui, bool hub, bool humans)
    {
        var menus = Get(s.match, "menus"); var status = (Component)Get(menus, "status"); Call(status, "ForceMeshUpdate", false, false);
        Assert.That((bool)Get(status, "isTextOverflowing"), Is.False, "Actual current QTE/context text fits at the controlled render dimensions.");
        Rect context = ScreenRect((RectTransform)status.transform, ui); var buttons = Components(Get(s.gallery, "readyButtons"));
        Assert.That(buttons.Length, Is.EqualTo(4));
        for (int i = 0; i < 4; i++)
        {
            Assert.That(buttons[i].gameObject.activeInHierarchy, Is.EqualTo(hub)); if (!hub) continue;
            var text = (Component)Get(buttons[i], "normalTextObj"); Call(text, "ForceMeshUpdate", false, false);
            Assert.That((bool)Get(text, "isTextOverflowing"), Is.False, "Owner" + i + " nearest-shop/ready context must fit.");
            Assert.That((bool)Get(buttons[i], "isInteractable"), Is.EqualTo(humans));
            var rect = ScreenRect((RectTransform)buttons[i].transform, ui); Assert.That(rect.yMax, Is.LessThan(context.yMin - 2), "Ready buttons stay below the live context plate.");
            Assert.That(rect.xMin, Is.GreaterThan(0)); Assert.That(rect.xMax, Is.LessThan(ui.pixelWidth));
            if (i > 0) Assert.That(rect.xMin, Is.GreaterThan(ScreenRect((RectTransform)buttons[i - 1].transform, ui).xMax + 2));
        }
    }
    static Rect ActorRect(Scope s, int owner)
    {
        var truck = Components(Get(s.match, "trucks"))[owner]; var host = Get(truck, "appearanceHost"); Assert.That(host, Is.Not.Null); Assert.That((bool)Get(host, "Applied"), Is.True);
        var renderers = ((Renderer[])Get(host, "ExteriorRenderers")).Where(r => (bool)Runtime("LWS.TruckFashionShow.FashionGalleryPresentation").GetMethod("ActualVisibleMesh").Invoke(null, new object[] { r })).ToArray();
        Assert.That(renderers.Length, Is.GreaterThan(0)); var bounds = renderers[0].bounds; foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
        var points = Enumerable.Range(0, 8).Select(i => s.View.WorldToViewportPoint(bounds.center + Vector3.Scale(bounds.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1)))).ToArray();
        Assert.That(points.All(p => p.z > 0), Is.True); return Rect.MinMaxRect(points.Min(p => p.x), points.Min(p => p.y), points.Max(p => p.x), points.Max(p => p.y));
    }
    static void InspectFourActors(Scope scope)
    {
        for (int i = 0; i < 4; i++)
        {
            var rect = ActorRect(scope, i); Assert.That(rect.xMin, Is.GreaterThan(.02f)); Assert.That(rect.xMax, Is.LessThan(.98f));
            Assert.That(rect.yMin, Is.GreaterThan(.23f)); Assert.That(rect.yMax, Is.LessThan(.94f)); Assert.That(rect.height, Is.GreaterThan(.045f), "Actual four-owner hub trucks must remain readable.");
        }
    }
    static IEnumerator Until(Scope scope, Func<bool> predicate, float seconds, string label)
    {
        float end = Time.realtimeSinceStartup + seconds; while (!predicate() && Time.realtimeSinceStartup < end) yield return null;
        Assert.That(predicate(), Is.True, label + " phase=" + Get(scope.match, "Phase") + " clock=" + Get(scope.match, "Clock"));
    }
    [UnityTest, Timeout(45000)] public IEnumerator FourAssignedHumanReadyButtonsAndFixedSignsStayReadableAtBothRealControlledResolutions()
    {
        using (var scope = new Scope())
        {
            yield return scope.Load(true); yield return Until(scope, () => (float)Get(scope.match, "Clock") > .7f, 5, "Original live hub clock");
            InspectFourActors(scope);
            Assert.That(((GameObject[])Get(scope.gallery, "supersededFloatingSigns")).All(g => g != null && !g.activeInHierarchy), Is.True);
            foreach (var sign in (TextMesh[])Get(scope.gallery, "fixedSigns"))
            {
                Assert.That(sign.gameObject.activeInHierarchy, Is.True); Assert.That(sign.GetComponents<Collider>(), Is.Empty);
                float width = sign.text == "TOTALLY RIDICULOUS RUNWAY" ? 42 : sign.text == "DRESSING ROOM" ? 23 : 17.6f;
                Assert.That(sign.GetComponent<Renderer>().bounds.size.x, Is.LessThanOrEqualTo(width + .1f));
            }
            Assert.That(((Light[])Get(scope.gallery, "practicalLights")).Count(l => l.type == LightType.Spot && l.intensity > 600), Is.GreaterThanOrEqualTo(8));
            scope.Capture("four-assigned-human-hub-context", true, true);
        }
    }
    [UnityTest, Timeout(120000)] public IEnumerator NaturalAiBrowseThenRunwayUsesSingleRelativeShotAndNeverCoversActualQteWithReadyControls()
    {
        using (var scope = new Scope())
        {
            yield return scope.Load(false); Time.timeScale = 3;
            var observer = scope.match.gameObject.AddComponent<FashionGalleryLateProbe>(); bool singleWriter = true; int observed = 0;
            observer.sample = () => { if ((bool)Get(scope.match, "Live")) { singleWriter &= (int)Get(Get(scope.match, "broadcast"), "FrameEvaluations") == 1; observed++; } };
            yield return Until(scope, () => Enumerable.Range(0, 4).Any(i => ((string)Call(scope.gallery, "NearestShopContext", i)).Contains("ACTION TO DRESS")), 25, "Ordinary original AI must actually reach a free shop");
            InspectFourActors(scope); scope.Capture("natural-four-ai-boutique-browse", true, false);
            yield return Until(scope, () => Get(scope.match, "Phase").ToString() == "Performance" && (float)Get(scope.match, "PerformanceTime") > 4, 65, "Actual original lineup/runway entry");
            yield return null; int owner = (int)Get(scope.match, "ActiveOwner"); var actor = ActorRect(scope, owner);
            Assert.That(actor.height, Is.GreaterThan(.12f)); Assert.That(actor.xMin, Is.GreaterThan(.02f)); Assert.That(actor.xMax, Is.LessThan(.98f));
            Assert.That(actor.yMin, Is.GreaterThan(.2f)); Assert.That(actor.yMax, Is.LessThan(.96f));
            Assert.That(singleWriter, Is.True, "Only original FashionMatch.Present evaluates the existing camera."); Assert.That(observed, Is.GreaterThan(20));
            scope.Capture("natural-live-outfit-qte-runway", false, false); observer.sample = null;
            TestContext.WriteLine("FASHION_GALLERY_NATIVE real AI browse/equip/lineup/performance, original rules/strut/router, actual composed bodies and existing single camera. Controlled native captures; no Windows/human/device/performance claim.");
        }
    }
}
#endif

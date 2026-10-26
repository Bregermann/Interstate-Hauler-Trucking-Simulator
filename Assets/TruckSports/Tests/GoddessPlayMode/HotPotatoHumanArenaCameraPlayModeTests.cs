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

public sealed class HotPotatoHumanArenaCameraPlayModeTests
{
    const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    static Type Runtime(string name) => Type.GetType(name + ", Assembly-CSharp", true);
    static object Get(object owner, string name) => owner.GetType().GetField(name, Any) is FieldInfo field ? field.GetValue(owner) : owner.GetType().GetProperty(name, Any).GetValue(owner);
    static void Set(object owner, string name, object value) => owner.GetType().GetField(name, Any).SetValue(owner, value);
    static object Call(object owner, string name, params object[] args) => owner.GetType().GetMethods(Any).Single(m => m.Name == name && m.GetParameters().Length == args.Length).Invoke(owner, args);
    sealed class Fixture : IDisposable
    {
        readonly IDisposable prefs;
        readonly GameObject isolation;
        readonly InputSettings original, temporary;
        readonly Gamepad[] pads = new Gamepad[4];
        public Behaviour match;
        public object Rules => Get(match, "rules");
        public object Policy => Get(match, "humanArenaCamera");
        public object Driving => Get(match, "drivingViews");
        public object Broadcast => Get(match, "broadcast");
        public object Global => Get(Broadcast, "broadcastFraming");
        public Camera Camera => (Camera)Get(Broadcast, "view");
        public object Truck(int slot) => ((Array)Get(match, "trucks")).GetValue(slot);
        public Rigidbody Body(int slot) => (Rigidbody)Get(Truck(slot), "body");
        public Fixture(string suffix)
        {
            prefs = (IDisposable)Runtime("LWS.TruckSports.SportsPrefs").GetMethod("IsolateValidation").Invoke(null, new object[] { "HotHumanArena" + suffix });
            original = InputSystem.settings; temporary = UnityEngine.Object.Instantiate(original);
            temporary.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            temporary.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView; InputSystem.settings = temporary;
            isolation = new GameObject("Controlled Hot camera fixture neutral input isolation"); UnityEngine.Object.DontDestroyOnLoad(isolation);
            isolation.AddComponent(Runtime("LWS.TruckSports.SportsProbeInputIsolation"));
            for (int i = 0; i < pads.Length; i++) { pads[i] = InputSystem.AddDevice<Gamepad>(); InputSystem.EnableDevice(pads[i]); }
        }
        public IEnumerator Load()
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/TruckHotPotato/Scenes/TruckHotPotato.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null; yield return null;
            match = UnityEngine.Object.FindObjectsByType(Runtime("LWS.TruckHotPotato.HotPotatoMatch"), FindObjectsSortMode.None).Cast<Behaviour>().Single();
            match.enabled = false;
        }
        public void Start(int humans)
        {
            Call(match, "SetupScreen"); Set(Get(match, "setup"), "count", 8); Call(match, "NormalizeSetup"); Call(match, "AllBots");
            var players = (Array)Get(Get(match, "setup"), "players");
            for (int i = 0; i < humans; i++) { var player = players.GetValue(i); Set(player, "source", Enum.Parse(Get(player, "source").GetType(), "WholeController")); Set(player, "pad", pads[i]); }
            Assert.That((bool)Call(match, "StartMatch"), Is.True, "Original source validation and real independently assigned test devices.");
            Call(Driving, "Select", 0); Time.timeScale = 0;
        }
        public void Pose(int slot, Vector3 position)
        { Call(Truck(slot), "Prepare", position, Quaternion.identity, true); Call(Get(Truck(slot), "tractorMotor"), "ResetAttachedBodies"); }
        public void Spread()
        {
            // Original NWH actors, inside the actual 64m arena. Controlled presentation
            // geometry only: no natural gameplay, hardware acceptance or score claim.
            Vector3[] positions = { new Vector3(-46,.65f,-30), new Vector3(46,.65f,30), new Vector3(-46,.65f,30), new Vector3(46,.65f,-30),
                new Vector3(0,.65f,4), new Vector3(8,.65f,4), new Vector3(0,.65f,51), new Vector3(0,.65f,-51) };
            for (int i = 0; i < 8; i++) Pose(i, positions[i]); Physics.SyncTransforms();
        }
        public void Present(float dt = .016f)
        { Call(match, "Present", dt); if((int)Get(Broadcast,"evaluationFrame")==Time.frameCount)Assert.That((int)Get(Broadcast, "FrameEvaluations"), Is.LessThanOrEqualTo(1), "One Cinemachine evaluation in this rendered frame; lifecycle calls from an older frame are excluded."); }
        public Bounds Bounds(int slot) => (Bounds)Call(Global, "TruckBounds", slot);
        public void Visible(Bounds bounds, string subject)
        {
            for (int i = 0; i < 8; i++)
            {
                var corner = bounds.center + Vector3.Scale(bounds.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                Vector3 point = Camera.WorldToViewportPoint(corner);
                Assert.That(point.z, Is.InRange(Camera.nearClipPlane, Camera.farClipPlane), subject + " depth corner " + i);
                Assert.That(point.x, Is.InRange(0f, 1f), subject + " horizontal corner " + i);
                Assert.That(point.y, Is.InRange(0f, 1f), subject + " vertical corner " + i);
            }
        }
        public void RequiredVisible(int humans)
        {
            var alive = (bool[])Get(Rules, "alive"); for (int i = 0; i < humans; i++) if (alive[i]) Visible(Bounds(i), "active human " + i);
            Visible(Bounds((int)Get(Rules, "Carrier")), "carrier");
            foreach (var renderer in ((Transform)Get(match, "bomb")).GetComponentsInChildren<Renderer>())
                if (renderer.enabled && !(renderer is ParticleSystemRenderer)) Visible(renderer.bounds, "actual bomb " + renderer.name);
        }
        public void Dispose()
        {
            Time.timeScale = 1; if (match != null) { Call(match, "SetupScreen"); match.enabled = true; }
            foreach (var pad in pads) if (pad != null && pad.added) InputSystem.RemoveDevice(pad);
            InputSystem.settings = original; UnityEngine.Object.DestroyImmediate(temporary); UnityEngine.Object.DestroyImmediate(isolation); prefs.Dispose();
        }
    }

    [UnityTest, Timeout(120000)]
    public IEnumerator OneToFourAssignedHumansAndCarrierHaveAllRendererCornersAt720And1080()
    {
        using (var f = new Fixture("Corners"))
        {
            yield return f.Load();
            string output = Path.Combine("Logs/TruckSports/PlaytestRepair/HotHumanArenaNative", DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff")); Directory.CreateDirectory(output);
            for (int humans = 1; humans <= 4; humans++)
            {
                f.Start(humans); f.Spread(); int journal = (int)Get(Get(f.match, "ScoreJournal"), "Count"); float clock = (float)Get(f.Rules, "Clock");
                foreach (int width in new[] { 1280, 1920 })
                {
                    var target = new RenderTexture(width, width * 9 / 16, 24); target.Create(); var prior = f.Camera.targetTexture; float aspect = f.Camera.aspect;
                    try
                    {
                        f.Camera.targetTexture = target; f.Camera.aspect = (float)target.width / target.height;
                        yield return null; f.Present(); yield return null; f.Present(); yield return null;
                        f.RequiredVisible(humans); Assert.That((bool)Get(f.Global, "OwnsShot"), Is.False); Assert.That((bool)Get(f.Policy, "Acquired"), Is.True);
                        var active = RenderTexture.active; var image = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
                        try
                        {
                            RenderTexture.active = target; image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); image.Apply();
                            float darkest = 1, brightest = 0;
                            for (int y = 1; y < 12; y++) for (int x = 1; x < 16; x++) { float light = image.GetPixel(x * image.width / 16, y * image.height / 12).grayscale; darkest = Mathf.Min(darkest, light); brightest = Mathf.Max(brightest, light); }
                            Assert.That(brightest, Is.GreaterThan(.02f), "Actual native camera target must contain rendered pixels.");
                            Assert.That(brightest - darkest, Is.GreaterThan(.02f), "A uniform clear target is not a scene capture.");
                            File.WriteAllBytes(Path.Combine(output, humans + "-humans-" + width + ".png"), image.EncodeToPNG());
                        }
                        finally { RenderTexture.active = active; UnityEngine.Object.DestroyImmediate(image); }
                        TestContext.WriteLine("HOT_HUMAN_BOUNDS humans=" + humans + " resolution=" + width + " carrier=" + Get(f.Rules, "Carrier") + " required=" + Get(f.Policy, "RequiredBounds") + " framed=" + Get(f.Policy, "FramedBounds") + " optionalAI=" + Get(f.Policy, "NearbyAI"));
                    }
                    finally { f.Camera.targetTexture = prior; f.Camera.aspect = aspect; target.Release(); UnityEngine.Object.DestroyImmediate(target); }
                }
                Assert.That((int)Get(Get(f.match, "ScoreJournal"), "Count"), Is.EqualTo(journal)); Assert.That((float)Get(f.Rules, "Clock"), Is.EqualTo(clock));
                Assert.That(((int[])Get(f.Rules, "scores")).Sum(), Is.Zero, "Camera reads cannot create scoring.");
            }
            TestContext.WriteLine("Native RenderTexture captures of controlled original actor geometry: " + Path.GetFullPath(output) + ". Windows ScreenCapture/human acceptance remains separate.");
        }
    }

    [UnityTest, Timeout(90000)]
    public IEnumerator DistantAiCannotEnlargeMandatoryHumanShotAndMovingHumanExpandsImmediately()
    {
        using (var f = new Fixture("Priorities"))
        {
            yield return f.Load(); f.Start(2);
            for (int seed = 1; seed < 1000; seed++) { Call(f.Rules, "Reset", 8, seed); if ((int)Get(f.Rules, "Carrier") == 0) break; }
            Assert.That((int)Get(f.Rules, "Carrier"), Is.Zero);
            f.Pose(0, new Vector3(-35,.65f,0)); f.Pose(1, new Vector3(-32.5f,.65f,0)); f.Pose(2, new Vector3(-30,.65f,0));
            for (int i = 3; i < 8; i++) f.Pose(i, new Vector3(35,.65f,(i-4)*8)); Physics.SyncTransforms();
            yield return null; f.Present(); f.RequiredVisible(2);
            Assert.That((int)Get(f.Policy, "NearbyAI"), Is.EqualTo(1), "Only the adjacent challenger is optional priority three.");
            Bounds initial = (Bounds)Get(f.Policy, "FramedBounds"); Assert.That(initial.size.x, Is.LessThan(20));
            Assert.That(initial.Contains(f.Body(7).position), Is.False, "Distant AI is deliberately outside this human-first shot.");
            // Same acquired camera, no reset/snap: a newly spread required human
            // must still fit on its very first frame, even while center damping runs.
            f.Pose(1, new Vector3(46,.65f,28)); Physics.SyncTransforms();
            yield return null; f.Present(); f.RequiredVisible(2);
            Assert.That(((Bounds)Get(f.Policy, "FramedBounds")).size.x, Is.GreaterThan(75));
            Assert.That((int)Get(Get(f.match, "ScoreJournal"), "Count"), Is.Zero);
        }
    }

    [UnityTest, Timeout(90000)]
    public IEnumerator OriginalEliminationDropsHumanSmoothlyPauseHoldsAndRematchReacquires()
    {
        using (var f = new Fixture("Lifecycle"))
        {
            yield return f.Load(); f.Start(2);
            // Seed selection uses original Reset/Step authority, never writes alive/carrier/scores.
            for (int seed = 1; seed < 1000; seed++) { Call(f.Rules, "Reset", 8, seed); if ((int)Get(f.Rules, "Carrier") == 0) break; }
            Assert.That((int)Get(f.Rules, "Carrier"), Is.Zero);
            f.Pose(0, new Vector3(-51,.65f,0)); f.Pose(1,new Vector3(46,.65f,0));for (int i = 2; i < 8; i++) f.Pose(i, new Vector3(15 + i*2,.65f,i*2)); Physics.SyncTransforms();
            // Every possible new carrier lies within the initial human span.
            // Elimination may legitimately EXPAND immediately if its new carrier
            // is outside that span; this case specifically tests inward damping.
            yield return null; f.Present(); Bounds wide = (Bounds)Get(f.Policy, "FramedBounds"); float wideLens = f.Camera.orthographicSize;
            int eliminated = (int)Call(f.Rules, "Step", (float)Get(f.Rules, "Fuse") + .01f); Assert.That(eliminated, Is.Zero);
            int receipts = (int)Get(Get(f.match, "ScoreJournal"), "Count");
            yield return null; f.Present(); Bounds first = (Bounds)Get(f.Policy, "FramedBounds"); f.RequiredVisible(2);
            Assert.That(first.size.x, Is.LessThan(wide.size.x)); Assert.That(first.size.x, Is.GreaterThan(wide.size.x * .9f), "Elimination shrinks smoothly rather than cutting.");
            Assert.That(f.Camera.orthographicSize, Is.InRange(wideLens * .9f, wideLens), "Actual fitted camera lens follows gradual shrink.");
            for (int i = 0; i < 80; i++) { yield return null; f.Present(.025f); }
            Bounds settled = (Bounds)Get(f.Policy, "FramedBounds"); Assert.That(settled.size.x, Is.LessThan(wide.size.x * .6f));
            Assert.That(settled.Contains(f.Body(eliminated).position), Is.False, "The eliminated human no longer constrains the shot.");
            Assert.That((int)Get(Get(f.match, "ScoreJournal"), "Count"), Is.EqualTo(receipts));
            Call(f.match, "Pause", 0, false); Vector3 position = f.Camera.transform.position; float size = f.Camera.orthographicSize;
            for (int i = 0; i < 8; i++) { yield return null; f.Present(); }
            Assert.That(f.Camera.transform.position, Is.EqualTo(position)); Assert.That(f.Camera.orthographicSize, Is.EqualTo(size));
            Call(f.match, "Resume"); Time.timeScale = 0; f.Start(2); f.Spread(); yield return null; f.Present(); f.RequiredVisible(2);
            Assert.That(((bool[])Get(f.Rules, "alive"))[0], Is.True); Assert.That((int)Get(Get(f.match, "ScoreJournal"), "Count"), Is.Zero);
        }
    }

    [UnityTest, Timeout(90000)]
    public IEnumerator SharedOwnershipReleasesForOriginalSplitCabAiOnlyAndRecordedReview()
    {
        using (var f = new Fixture("Handoffs"))
        {
            yield return f.Load(); f.Start(2); f.Spread(); yield return null; f.Present();
            foreach (int mode in new[] { 1, 2 })
            {
                Call(f.Driving, "Select", mode); yield return null; f.Present(); Assert.That((bool)Get(f.match, "HumanArenaFraming"), Is.False); Assert.That((bool)Get(f.Policy, "Acquired"), Is.False);
                var cameras = (Array)Get(f.match, "cameras"); Assert.That(cameras.Length, Is.EqualTo(2)); Assert.That(f.Camera.enabled, Is.False);
                Call(f.Driving, "Select", 0); yield return null; f.Present(); f.RequiredVisible(2);
            }
            // Build a real original pose record from controlled actors, then finish
            // only via original fuse rules. This is a camera handoff fixture.
            object review = Get(f.match, "review");
            for (int sample = 0; sample <= 80; sample++) Call(review, "Capture", 3f + sample * .08f);
            while (!(bool)Get(f.Rules, "Complete")) Call(f.Rules, "Step", (float)Get(f.Rules, "Fuse") + .01f);
            Call(f.match, "EndMatch"); Call(review, "Begin"); Assert.That((bool)Get(review, "Playing"), Is.True);
            yield return null; f.Present(); Assert.That((bool)Get(f.Policy, "Acquired"), Is.False); Assert.That((bool)Get(f.match, "HumanArenaFraming"), Is.False);
            Assert.That(f.Camera.orthographic, Is.False); Call(review, "End", false);
            f.Start(2); f.Spread(); yield return null; f.Present(); f.RequiredVisible(2);
            f.Start(0); yield return null; f.Present(); yield return null;
            Assert.That((bool)Get(f.match, "HumanArenaFraming"), Is.False); Assert.That((bool)Get(f.Global, "OwnsShot"), Is.True); Assert.That((bool)Get(f.Policy, "Acquired"), Is.False);
            Assert.That((int)Get(f.Broadcast, "FrameEvaluations"), Is.LessThanOrEqualTo(1));
            // Exercise acquisition after the global group composer/occlusion
            // extension actually existed and owned a real AI-only frame.
            f.Start(2); f.Spread(); yield return null; f.Present(); f.RequiredVisible(2);
            Assert.That((bool)Get(f.Global, "OwnsShot"), Is.False);
            Assert.That(Get(Get(f.Broadcast, "rig"), "LookAt"), Is.SameAs(Get(f.Broadcast, "aim")));
            var occlusion = Get(f.Global, "occlusion") as Behaviour; Assert.That(occlusion, Is.Not.Null); Assert.That(occlusion.enabled, Is.False);
        }
    }
}
#endif

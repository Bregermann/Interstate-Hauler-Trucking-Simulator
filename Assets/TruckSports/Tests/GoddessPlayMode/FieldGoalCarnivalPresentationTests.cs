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

public sealed class FieldGoalCarnivalPresentationTests
{
    const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    static Type Sports(string name) => Type.GetType("LWS.TruckSports." + name + ", Assembly-CSharp", true);
    static object Get(object owner, string name) => owner.GetType().GetField(name, Any) is FieldInfo field ? field.GetValue(owner) : owner.GetType().GetProperty(name, Any).GetValue(owner);
    static void Set(object owner, string name, object value) => owner.GetType().GetField(name, Any).SetValue(owner, value);
    static object Call(object owner, string name, params object[] args) => owner.GetType().GetMethods(Any).Single(method => method.Name == name && method.GetParameters().Length == args.Length).Invoke(owner, args);
    Component match, presentation; IDisposable lease; GameObject isolation;
    IEnumerator Load()
    {
        lease = (IDisposable)Sports("SportsPrefs").GetMethod("IsolateValidation").Invoke(null, new object[] { "FieldCarnival" + Guid.NewGuid().ToString("N") });
        isolation = new GameObject("Explicit Field Goal carnival fixture"); UnityEngine.Object.DontDestroyOnLoad(isolation); isolation.AddComponent(Sports("SportsProbeInputIsolation"));
        EditorSceneManager.LoadSceneInPlayMode("Assets/TruckFieldGoal/Scenes/TruckFieldGoal.unity", new LoadSceneParameters(LoadSceneMode.Single)); yield return null; yield return null;
        match = (Component)UnityEngine.Object.FindAnyObjectByType(Type.GetType("LWS.TruckFieldGoal.FieldGoalMatch, Assembly-CSharp", true));
        Assert.That(match, Is.Not.Null); presentation = (Component)Get(match, "carnivalPresentation"); Assert.That(presentation, Is.Not.Null, "Invoke explicit FieldGoalCarnivalAuthoring.InstallMissingPresentation first.");
    }
    [TearDown] public void Cleanup()
    {
        if (match != null) Call(match, "SetupScreen"); Time.timeScale = 1;
        if (isolation != null) UnityEngine.Object.DestroyImmediate(isolation); lease?.Dispose(); lease = null;
    }
    static string Physics(Component truck, Collider[] originalColliders = null)
    {
        var body = (Rigidbody)Get(truck, "body"); string value = body.GetInstanceID() + ":" + body.mass + ":" + body.centerOfMass;
        foreach (var collider in originalColliders ?? truck.GetComponentsInChildren<Collider>(true))
        { Assert.That(collider != null, Is.True, "An original physical component must survive."); value += "/" + collider.GetInstanceID() + ":" + collider.isTrigger; if (collider is BoxCollider box) value += ":" + box.center + ":" + box.size; if (collider is MeshCollider mesh) value += ":" + mesh.convex + ":" + mesh.sharedMesh.GetInstanceID(); }
        foreach (var wheel in (IEnumerable)Get(truck, "wheels")) { var data = Get(wheel, "wheel"); var spring = Get(wheel, "spring"); value += "/" + ((Component)wheel).GetInstanceID() + ":" + Get(data, "radius") + ":" + Get(data, "width") + ":" + Get(spring, "maxLength") + ":" + Get(spring, "maxForce"); }
        return value;
    }
    static Color32[] SaveFeed(RenderTexture texture, string stage)
    {
        Assert.That(texture, Is.Not.Null); var active = RenderTexture.active; Texture2D image = null;
        try
        {
            RenderTexture.active = texture; image = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false); image.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0); image.Apply();
            var pixels = image.GetPixels32(); Assert.That(pixels.Count(color => color.r + color.g + color.b > 45), Is.GreaterThan(5000), "The native feed must contain rendered scene content.");
            string path = Path.GetFullPath("Logs/TruckSports/Carnival/FieldGoal/CONTROLLED-" + stage + "-recorded-feed.png"); Directory.CreateDirectory(Path.GetDirectoryName(path)); File.WriteAllBytes(path, image.EncodeToPNG()); TestContext.WriteLine("FIELD_GOAL_ACTUAL_RECORDED_FEED " + stage + " capture=" + path); return pixels;
        }
        finally { RenderTexture.active = active; if (image != null) UnityEngine.Object.Destroy(image); }
    }
    void CaptureBoard()
    {
        var save = Sports("SportsControlledCapture").GetMethods().Single(method => method.Name == "Save" && method.GetParameters().Length == 5);
        save.Invoke(null, new object[] { match, 1920, 1080, "Logs/TruckSports/Carnival/FieldGoal/CONTROLLED-default-replay-board-1920.png", null });
    }
    [UnityTest, Timeout(90000)]
    public IEnumerator SavedBowlHasTwoCurvedSteppedDecksOriginalCrowdAndReservedNonoverlappingReplayScreen()
    {
        yield return Load();
        Assert.That(presentation.GetComponentsInChildren<Rigidbody>(true), Is.Empty); Assert.That(presentation.GetComponentsInChildren<Collider>(true), Is.Empty);
        Assert.That(presentation.GetComponentsInChildren<AudioListener>(true), Is.Empty); Assert.That(presentation.GetComponentsInChildren<Light>(true), Is.Empty);
        var decks = (MeshFilter[])Get(presentation, "curvedDecks"); Assert.That(decks.Length, Is.EqualTo(2));
        Assert.That(decks.All(deck => deck.sharedMesh.vertexCount > 8000 && deck.sharedMesh.bounds.size.z > 140 && deck.sharedMesh.bounds.size.x > 90), Is.True, "Actual curved stepped geometry, not renamed straight primitives.");
        Assert.That(decks[1].GetComponent<Renderer>().bounds.min.y - decks[0].GetComponent<Renderer>().bounds.max.y, Is.GreaterThan(4));
        var straight = (MeshRenderer[])Get(presentation, "replacedStraightStands"); Assert.That(straight.Length, Is.EqualTo(8)); Assert.That(straight.All(renderer => !renderer.enabled && renderer.GetComponent<MeshFilter>().sharedMesh.name == "Cube"), Is.True);
        var crowd = match.GetComponent(Type.GetType("LWS.TruckFieldGoal.FieldGoalCrowd, Assembly-CSharp", true)); var people = (Transform[])Get(presentation, "remappedCrowd");
        Assert.That(people.Length, Is.EqualTo((int)Get(crowd, "Spectators"))); Assert.That(people.Length, Is.GreaterThan(2000));
        var superseded = (Transform)Get(presentation, "supersededAudience"); Assert.That(superseded, Is.Not.Null, "Run the explicit guarded retirement upgrade on already-authored bowls.");
        Assert.That(superseded.gameObject.activeSelf, Is.False); Assert.That(superseded.GetComponentsInChildren<Collider>(true), Is.Empty); Assert.That(superseded.GetComponentsInChildren<Rigidbody>(true), Is.Empty);
        Assert.That(people.All(person => person.gameObject.activeInHierarchy), Is.True, "The actual bound2304 crowd stays live after the unreferenced old rendering root is retired.");
        Assert.That((string)Get(presentation, "retirementPhysicsBefore"), Has.Length.EqualTo(64)); Assert.That(Get(presentation, "retirementPhysicsAfter"), Is.EqualTo(Get(presentation, "retirementPhysicsBefore")), "Native explicit authoring retained original Collider/Rigidbody serialized byte fingerprints.");
        var bowl = (Transform)Get(presentation, "bowl");
        foreach (var person in people)
        {
            Assert.That(person.IsChildOf(bowl), Is.True); var point = bowl.InverseTransformPoint(person.position);
            int deck = point.y >= 18 ? 1 : 0; int row = Mathf.RoundToInt((point.y - 3 - deck * 15 - .025f) / 1.15f);
            Assert.That(row, Is.InRange(0, 7)); Assert.That(point.y, Is.EqualTo(3 + deck * 15 + row * 1.15f + .025f).Within(.08f), "Keep each original crowd foot on its remapped tread.");
            float x = point.x / (45 + deck * 16 + (row + .5f) * 2.25f), z = point.z / (77 + deck * 16 + (row + .5f) * 2.25f);
            Assert.That(x * x + z * z, Is.EqualTo(1).Within(.005f), "Crowd must occupy the actual elliptical seating row.");
            float angle = Mathf.Repeat(Mathf.Atan2(x, z) * Mathf.Rad2Deg, 360); Assert.That(angle, Is.InRange(37.9f, 322.1f), "Keep the deliberate elliptical scoreboard opening empty.");
        }
        var video = (Canvas)Get(presentation, "videoCanvas"); var board = Get(match, "stadiumBoard"); var stats = (Canvas)Get(board, "screen");
        var a = new Vector3[4]; var b = new Vector3[4]; video.GetComponent<RectTransform>().GetWorldCorners(a); stats.GetComponent<RectTransform>().GetWorldCorners(b);
        Assert.That(a.Min(point => point.y), Is.GreaterThan(b.Max(point => point.y) + 1), "Reserve a separate video wing; never cover the existing stats.");
        Assert.That(stats.GetComponent<RectTransform>().sizeDelta, Is.EqualTo(new Vector2(1600, 660)));
        var feed = (Camera)Get(Get(presentation, "feedCamera"), "view"); Assert.That(feed.CompareTag("MainCamera"), Is.False); Assert.That(feed.enabled, Is.False); Assert.That(feed.cullingMask & (1 << video.gameObject.layer), Is.Zero, "Exclude the video screen from its own camera to prevent recursion.");
        Assert.That(Get(presentation, "FeedTexture"), Is.Null);
        TestContext.WriteLine("SAVED_FIELD_GOAL_CARNIVAL two real curved stepped decks, original crowd refs and reserved board opening; separate video wing retains1600×660 stats, no added physics/lights/listeners.");
    }
    [UnityTest, Timeout(180000)]
    public IEnumerator ActualPhysicalKickReplaysRunupContactFlightAndResultOnVisibleBoardWithoutChangingLockedScore()
    {
        yield return Load(); Call(match, "AllBots"); Set(match, "option", 0); Set(Get(match, "setup"), "count", 1);
        var player = ((Array)Get(Get(match, "setup"), "players")).GetValue(0); Set(player, "difficulty", Enum.Parse(Get(player, "difficulty").GetType(), "Hard"));
        var presets = Get(match, "weatherPresets"); var clear = ((Array)Get(presets, "entries")).Cast<object>().Single(entry => (string)Get(entry, "displayName") == "Clear"); Call(presets, "Select", (string)Get(clear, "id"));
        var truck = (Component)Get(match, "truck"); var originalColliders = truck.GetComponentsInChildren<Collider>(true); string originalPhysical = Physics(truck, originalColliders); Assert.That(Call(match, "StartMatch"), Is.True);
        // Initially disabled NWH wheels create their four owned cylinder colliders
        // in Start. Prove their authority before taking the complete live baseline.
        yield return null; yield return null;
        Assert.That(Physics(truck, originalColliders), Is.EqualTo(originalPhysical));
        var wheelColliders = ((IEnumerable)Get(truck, "wheels")).Cast<object>().Select(wheel => (Collider)Get(Get(wheel, "wheel"), "meshCollider")).ToArray();
        Assert.That(wheelColliders.Length, Is.EqualTo(4)); Assert.That(wheelColliders.All(collider => collider != null && collider is MeshCollider && collider.name == "Collider"), Is.True);
        CollectionAssert.AreEquivalent(originalColliders.Concat(wheelColliders).Distinct(), truck.GetComponentsInChildren<Collider>(true), "Only the existing NWH wheel initialization may add physical components.");
        string physical = Physics(truck); TestContext.WriteLine("FIELD_GOAL_INITIALIZED_PHYSICS original=" + originalPhysical + " wheelColliders=" + string.Join(",", wheelColliders.Select(collider => collider.GetInstanceID() + ":" + collider.transform.parent.name)) + " complete=" + physical);
        double deadline = Time.realtimeSinceStartupAsDouble + 70;
        while (Get(match, "Phase").ToString() != "Replay" && Time.realtimeSinceStartupAsDouble < deadline) yield return null;
        Assert.That(Get(match, "Phase").ToString(), Is.EqualTo("Replay"), "The original AI and real bumper collision must earn a locked physical attempt.");
        var ball = Get(match, "football"); Assert.That((int)Get(ball, "Launches"), Is.EqualTo(1)); Assert.That((bool)Get(ball, "LaunchObserved"), Is.True); Assert.That(((Vector3)Get(ball, "LaunchVelocity")).magnitude, Is.GreaterThan(2));
        var rules = Get(match, "rules"); Assert.That((bool)Get(rules, "AttemptLocked"), Is.True); int[] goals = ((int[])Get(rules, "goals")).ToArray(), attempts = ((int[])Get(rules, "attempts")).ToArray(); Assert.That(attempts[0], Is.EqualTo(1));
        int journal = (int)Get(Get(match, "ScoreJournal"), "Count"); var replay = Get(match, "replay");
        yield return null; yield return null;
        Assert.That((bool)Get(presentation, "FeedVisible"), Is.True); float kick = (float)Get(presentation, "KickTime"); Assert.That(kick, Is.GreaterThan(3));
        Assert.That((float)Get(presentation, "FirstRecordedCursor"), Is.LessThan(kick - .1f), "The default replay must include the real run-up rather than only the last seven seconds.");
        Assert.That((int)Get(presentation, "RunupFrames"), Is.GreaterThan(0));
        var camera = (Camera)Get(Get(match, "cameraRig"), "view"); var corners = new Vector3[4]; ((Canvas)Get(presentation, "videoCanvas")).GetComponent<RectTransform>().GetWorldCorners(corners);
        foreach (var corner in corners) { var projected = camera.WorldToViewportPoint(corner); Assert.That(projected.z, Is.GreaterThan(camera.nearClipPlane)); Assert.That(projected.x, Is.InRange(.025f, .975f)); Assert.That(projected.y, Is.InRange(.025f, .975f)); }
        CaptureBoard(); Set(replay, "Speed", 0f);
        var ghosts = (Transform[])Get(replay, "ghosts"); foreach (var ghost in ghosts) { Assert.That(ghost.GetComponentsInChildren<Rigidbody>(true), Is.Empty); Assert.That(ghost.GetComponentsInChildren<Collider>(true), Is.Empty); }
        Call(replay, "Seek", Mathf.Max((float)Get(presentation, "FirstRecordedCursor"), kick - .8f)); yield return null; yield return null;
        for(int frame=0;frame<12;frame++)yield return null;
        var replayRenderers=ghosts[0].GetComponentsInChildren<Renderer>(true).Where(renderer=>renderer.enabled&&renderer.gameObject.activeInHierarchy).ToArray();Assert.That(replayRenderers,Is.Not.Empty);
        var truckBounds=replayRenderers[0].bounds;foreach(var renderer in replayRenderers.Skip(1))truckBounds.Encapsulate(renderer.bounds);
        Vector3 recordedCenter=(Vector3)Get(presentation,"RecordedTruckCenter");var expanded=truckBounds;expanded.Expand(.4f);Assert.That(expanded.Contains(recordedCenter),Is.True,"Mapped physical COM must lie on the actual recorded truck, not its imported NWH visual pivot.");
        var expectedFocus=recordedCenter+Vector3.up*.9f+Vector3.forward*3;Assert.That(Vector3.Distance((Vector3)Get(presentation,"LastFeedFocus"),expectedFocus),Is.LessThan(.001f));
        var feedRig=Get(presentation,"feedCamera");var feedView=(Camera)Get(feedRig,"view");Assert.That(Vector3.Distance(((Transform)Get(feedRig,"aim")).position,expectedFocus),Is.LessThan(.001f),"The actual RT rig must receive the physical-centre framing target.");
        Vector2 min=Vector2.one*float.MaxValue,max=Vector2.one*float.MinValue;for(int i=0;i<8;i++){var point=feedView.WorldToViewportPoint(truckBounds.center+Vector3.Scale(truckBounds.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1)));Assert.That(point.z,Is.GreaterThan(feedView.nearClipPlane));min=Vector2.Min(min,new Vector2(point.x,point.y));max=Vector2.Max(max,new Vector2(point.x,point.y));}
        var projectedCenter=feedView.WorldToViewportPoint(truckBounds.center);Assert.That(projectedCenter.x,Is.InRange(.04f,.96f));Assert.That(projectedCenter.y,Is.InRange(.04f,.96f));Assert.That(max.y-min.y,Is.GreaterThan(.045f));Assert.That(max.x-min.x,Is.GreaterThan(.04f));
        TestContext.WriteLine("FIELD_GOAL_RUNUP_RT_AIM importedPivot="+ghosts[0].position+" actualRecordedCenter="+recordedCenter+" mappedLocal="+Get(presentation,"TruckReplayCenterLocal")+" focusError="+Vector3.Distance(((Transform)Get(feedRig,"aim")).position,expectedFocus)+" projectedBounds="+Rect.MinMaxRect(min.x,min.y,max.x,max.y));
        var runupPosition = ghosts[0].position; var first = SaveFeed((RenderTexture)Get(presentation, "FeedTexture"), "runup");
        Call(replay, "Seek", kick); yield return null; yield return null; SaveFeed((RenderTexture)Get(presentation, "FeedTexture"), "contact");
        Assert.That(Vector3.Distance(runupPosition, ghosts[0].position), Is.GreaterThan(.1f), "Original recorded truck poses must actually advance toward contact.");
        Call(replay, "Seek", Mathf.Min((float)Get(replay, "Duration") - .1f, kick + .65f)); yield return null; yield return null;
        var flight = SaveFeed((RenderTexture)Get(presentation, "FeedTexture"), "flight"); Assert.That((int)Get(presentation, "FlightFrames"), Is.GreaterThan(0));
        Assert.That(first.Zip(flight, (a, b) => Math.Abs(a.r - b.r) + Math.Abs(a.g - b.g) + Math.Abs(a.b - b.b)).Count(delta => delta > 30), Is.GreaterThan(300), "Native video must change with genuine recorded poses.");
        Call(replay, "Seek", Mathf.Max(kick, (float)Get(replay, "Duration") - .05f)); yield return null; SaveFeed((RenderTexture)Get(presentation, "FeedTexture"), "result");
        Assert.That((string)Get(match, "Outcome"), Is.Not.Empty); CollectionAssert.AreEqual(goals, (int[])Get(rules, "goals")); CollectionAssert.AreEqual(attempts, (int[])Get(rules, "attempts")); Assert.That((int)Get(Get(match, "ScoreJournal"), "Count"), Is.EqualTo(journal)); Assert.That(Physics(truck), Is.EqualTo(physical));
        var main = Get(match, "cameraRig"); Assert.That((int)Get(main, "FrameEvaluations"), Is.EqualTo(1));
        Call(match, "ExtraAction", "replayangle"); Assert.That((int)Get(replay, "Angle"), Is.EqualTo(1)); yield return null; Assert.That((bool)Get(presentation, "BoardShotFresh"), Is.False, "Manual angle1 retains the original recorded-play camera path.");
        Call(match, "ExtraAction", "replayskip"); deadline = Time.realtimeSinceStartupAsDouble + 3; while ((bool)Get(presentation, "FeedVisible") && Time.realtimeSinceStartupAsDouble < deadline) yield return null;
        Assert.That((bool)Get(presentation, "FeedVisible"), Is.False); Assert.That(Get(presentation, "FeedTexture"), Is.Null);
        Call(match, "SetupScreen"); Assert.That(Call(match, "StartMatch"), Is.True); yield return null; Assert.That(Get(presentation, "FeedTexture"), Is.Null); Assert.That((float)Get(presentation, "KickTime"), Is.LessThan(0)); Assert.That(Physics(truck), Is.EqualTo(physical));
        TestContext.WriteLine("ACTUAL_FIELD_GOAL_CARNIVAL physical AI bumper contact/ball flight locked once; existing recorded poses rendered as run-up/contact/flight/result on visible in-world board; diagnostic Seek/Speed0 changed only playback. Locked goals/attempts/journal/NWH/colliders unchanged; original manualangle, skip and rematch cleanup verified. No human/art/audio acceptance claimed.");
    }
}
#endif

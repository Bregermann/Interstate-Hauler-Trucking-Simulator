#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using LWS.TruckNestBuilding;

public sealed partial class NestNativeTests
{
    const BindingFlags Any = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    static Type Sports(string name) => Type.GetType("LWS.TruckSports." + name + ", Assembly-CSharp", true);
    static object Get(object owner, string name) => owner.GetType().GetField(name, Any) is FieldInfo field ? field.GetValue(owner) : owner.GetType().GetProperty(name, Any).GetValue(owner);
    static void Set(object owner, string name, object value) => owner.GetType().GetField(name, Any).SetValue(owner, value);
    static object Call(object owner, string name, params object[] args) => owner.GetType().GetMethods(Any).Single(m => m.Name == name && m.GetParameters().Length == args.Length && m.GetParameters().Select((p, i) => args[i] == null || p.ParameterType.IsInstanceOfType(args[i])).All(x => x)).Invoke(owner, args);
    static object Static(string type, string name, params object[] args) => Sports(type).GetMethod(name, BindingFlags.Public | BindingFlags.Static).Invoke(null, args);
    static Component[] Components(object list) => ((IEnumerable)list).Cast<Component>().ToArray();
    sealed class Scope : IDisposable
    {
        public Component match; public Keyboard keyboard;
        readonly InputSettings original; readonly InputSettings fixture;
        readonly IDisposable prefs; readonly GameObject isolation;
        public readonly string progress;
        public Scope(string name)
        {
            original = InputSystem.settings; fixture = UnityEngine.Object.Instantiate(original);
            fixture.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            fixture.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            fixture.updateMode = InputSettings.UpdateMode.ProcessEventsInDynamicUpdate; InputSystem.settings = fixture;
            prefs = (IDisposable)Static("SportsPrefs", "IsolateValidation", "ImpossibleNest" + Guid.NewGuid().ToString("N"));
            Static("SportsParticipantRegistry", "ForgetCache");
            isolation = new GameObject("Nest native owned input isolation"); UnityEngine.Object.DontDestroyOnLoad(isolation); isolation.AddComponent(Sports("SportsProbeInputIsolation"));
            keyboard = InputSystem.AddDevice<Keyboard>();
            progress = Path.GetFullPath("Logs/TruckSports/Impossible07/NestLifecycle/" + name + "-" + Guid.NewGuid().ToString("N") + ".jsonl"); Directory.CreateDirectory(Path.GetDirectoryName(progress));
        }
        public IEnumerator Load()
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/TruckNestBuilding/Scenes/TruckNestBuilding.unity", new LoadSceneParameters(LoadSceneMode.Single)); yield return null; yield return null;
            InputSystem.EnableDevice(keyboard); keyboard.MakeCurrent();
            match = (Component)UnityEngine.Object.FindAnyObjectByType(Type.GetType("LWS.TruckNestBuilding.NestMatch, Assembly-CSharp", true)); Assert.That(match, Is.Not.Null);foreach(var site in Components(Get(match,"sites"))){var family=Get(site,"family");var shape=(Collider)Get(family,"eggCollider");shape.gameObject.AddComponent<NestEggCollisionReadOnlyProbe>();} Trace("scene-loaded");
        }
        public void Trace(string name)
        {
            if (match == null) return;
            var rules = (NestRules)Get(match, "rules");
            string row = JsonUtility.ToJson(new Sample { checkpoint = name, frame = Time.frameCount, real = Time.realtimeSinceStartup, scaled = Time.time, scale = Time.timeScale, live = (bool)Get(match, "Live"), paused = (bool)Get(match, "Paused"), ready = (bool)Get(match, "DevicesReady"), menu = Get(Get(match, "menus"), "Screen").ToString(), clock = (float)Get(match, "Clock"), modes = string.Join("/", Components(Get(match, "builders")).Select(b => Get(b, "Mode") + ":grasp=" + Get(b, "Grasps") + ":drop=" + Get(b, "PhysicalDrops") + ":socket=" + Get(b, "GoalSocket"))), rooted = string.Join("/", Components(Get(match, "sites")).Select(s => Get(s, "RootedCount").ToString())), hatched = string.Join("/", rules.hatched.Select(h => h ? "1" : "0")), builders = Components(Get(match, "builders")).Select(Observe).ToArray() });
            var observation = JsonUtility.FromJson<Sample>(row); observation.droppedMaterials = ObserveDropped(match);observation.families=Components(Get(match,"sites")).Select(ObserveEgg).ToArray(); row = JsonUtility.ToJson(observation);
            File.AppendAllText(progress, row + "\n"); TestContext.WriteLine("NEST_NATIVE " + row);
        }
        public void Dispose()
        {
            Trace("finally"); Time.timeScale = 1; if (match != null) Call(match, "SetupScreen");
            if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
            UnityEngine.Object.DestroyImmediate(isolation); InputSystem.settings = original; UnityEngine.Object.DestroyImmediate(fixture);
            Static("SportsParticipantRegistry", "ForgetCache"); prefs.Dispose(); Static("SportsParticipantRegistry", "ForgetCache");
        }
    }
    [Serializable] sealed class Sample { public string checkpoint, menu, modes, rooted, hatched; public int frame; public float real, scaled, scale, clock; public bool live, paused, ready; public BuilderFacts[] builders; public SupportFacts[] droppedMaterials;public EggContactFacts[] families; }
    [Serializable] sealed class SupportFacts
    {
        public string id; public Vector3 position, euler, velocity, angular; public bool sleeping, gripped, fits0, fits1, support0, support1; public int lastOwner, socket; public float platformAge, deckGap, distance0, distance1, alignment0, alignment1;
    }
    static SupportFacts[] ObserveDropped(Component match)
    {
        var site=Components(Get(match,"sites"))[0];var deck=(BoxCollider)Get(site,"deck");
        return Components(Get(match,"materials")).Where(part=>(int)Get(part,"LastOwner")>=0&&Vector3.Distance(((Rigidbody)Get(part,"body")).position,site.transform.position)<18).Take(16).Select(part=>
        {
            var body=(Rigidbody)Get(part,"body");var shape=(Collider)Get(part,"shape");var target0=(Vector3)Call(site,"Target",0);var target1=(Vector3)Call(site,"Target",1);var rotation0=(Quaternion)Call(site,"TargetRotation",0);var rotation1=(Quaternion)Call(site,"TargetRotation",1);
            return new SupportFacts{id=(string)Get(part,"pieceId"),position=body.position,euler=body.rotation.eulerAngles,velocity=body.linearVelocity,angular=body.angularVelocity,sleeping=body.IsSleeping(),gripped=(FixedJoint)Get(part,"Grip")!=null,lastOwner=(int)Get(part,"LastOwner"),socket=(int)Get(part,"Socket"),platformAge=(float)Get(part,"platformAge"),deckGap=shape.bounds.min.y-deck.bounds.max.y,distance0=Vector3.Distance(body.position,target0),distance1=Vector3.Distance(body.position,target1),alignment0=Mathf.Abs(Vector3.Dot(body.rotation*Vector3.forward,rotation0*Vector3.forward)),alignment1=Mathf.Abs(Vector3.Dot(body.rotation*Vector3.forward,rotation1*Vector3.forward)),fits0=(bool)Call(site,"Fits",part,0,body.position,body.rotation),fits1=(bool)Call(site,"Fits",part,1,body.position,body.rotation),support0=(Rigidbody)Call(part,"ContactSupport",site,0)!=null,support1=(Rigidbody)Call(part,"ContactSupport",site,1)!=null};
        }).ToArray();
    }
    [Serializable] sealed class BuilderFacts
    {
        public int slot, socket, grasps, drops, placementStage; public string mode, selected, carried;
        public Vector3 body, velocity, bodyEuler, hand, handTarget, localTarget, piece, pieceVelocity, gripForce, gripTorque, placement;
        public Quaternion handRotation, pieceRotation, achievedGraspOffset, requestedHandRotation, capturedGraspOffset;
        public bool graspOffsetCaptured,clearingBrood;public int broodClearances;
        public Vector3 pieceForward, handForward, stageTarget;
        public float planarYawError, forwardPitchDegrees, commandedYaw, stageError;
        public float gripSeparation, placementError, placementAngle, alignedSeconds;
        public bool grip, transit;
    }
    static BuilderFacts Observe(Component builder)
    {
        var truck = (Component)Get(builder, "truck"); var body = (Rigidbody)Get(truck, "body"); var hand = (Rigidbody)Get(builder, "hand");
        var item = (Component)Get(builder, "Carried"); var selected = (Component)Get(builder, "Selected"); var site = (Component)Get(builder, "GoalSite"); int socket = (int)Get(builder, "GoalSocket");
        var sample = new BuilderFacts { slot = (int)Get(builder, "slot"), socket = socket, grasps = (int)Get(builder, "Grasps"), drops = (int)Get(builder, "PhysicalDrops"), mode = Get(builder, "Mode").ToString(), selected = selected == null ? "" : (string)Get(selected, "pieceId"), carried = item == null ? "" : (string)Get(item, "pieceId"), body = body.position, velocity = body.linearVelocity, bodyEuler = body.rotation.eulerAngles, hand = hand.position, handTarget = (Vector3)Get(builder, "HandTarget"), localTarget = (Vector3)Get(builder, "localTarget"), transit = (bool)Get(builder, "transit"), alignedSeconds = (float)Get(builder, "alignedAge") };
        sample.placementStage = (int)Get(builder, "PlacementStage");sample.clearingBrood=(bool)Get(builder,"ClearingBrood");sample.broodClearances=(int)Get(builder,"BroodClearancesCompleted");
        sample.capturedGraspOffset=(Quaternion)Get(builder,"CapturedGraspOffset");sample.graspOffsetCaptured=(bool)Get(builder,"GraspOffsetCaptured");sample.handRotation=hand.rotation;sample.handForward=hand.rotation*Vector3.forward;sample.commandedYaw=(float)Get(builder,"yaw");sample.requestedHandRotation=(Quaternion)Get(builder,"RequestedHandRotation");
        if (site != null && socket >= 0) sample.placement = (Vector3)Call(site, "Target", socket);
        if (item != null)
        {
            var piece = (Rigidbody)Get(item, "body"); var grip = (FixedJoint)Get(item, "Grip");
            sample.pieceRotation=piece.rotation;sample.achievedGraspOffset=Quaternion.Inverse(hand.rotation)*piece.rotation;sample.pieceForward=piece.rotation*Vector3.forward;sample.forwardPitchDegrees=Mathf.Asin(Mathf.Clamp(sample.pieceForward.y,-1,1))*Mathf.Rad2Deg;
            sample.piece = piece.position; sample.pieceVelocity = piece.linearVelocity; sample.grip = grip != null; sample.gripSeparation = Vector3.Distance(hand.position, piece.worldCenterOfMass);
            if (grip != null) { sample.gripForce = grip.currentForce; sample.gripTorque = grip.currentTorque; }
            if (site != null && socket >= 0)
            {
                var desired=(Quaternion)Call(site,"TargetRotation",socket)*Vector3.forward;
                sample.placementError=Vector3.Distance(sample.placement,piece.position);sample.placementAngle=Vector3.SignedAngle(sample.pieceForward,desired,Vector3.up);
                sample.planarYawError=Vector3.SignedAngle(Vector3.ProjectOnPlane(sample.pieceForward,Vector3.up),Vector3.ProjectOnPlane(desired,Vector3.up),Vector3.up);
                var deck=(BoxCollider)Get(site,"deck");sample.stageTarget=(Vector3)builder.GetType().GetMethod("PlacementTarget",BindingFlags.Public|BindingFlags.Static).Invoke(null,new object[]{sample.placementStage,(Vector3)Get(builder,"placementEntry"),sample.placement,deck.bounds.max.y});
                sample.stageError=Vector3.Distance(sample.stageTarget,piece.position);
            }
        }
        return sample;
    }
    static void EquivalentRestoredSnapshot(Scope scope, string before, string after)
    {
        File.WriteAllText(Path.ChangeExtension(scope.progress, ".before-restore.json"), before); File.WriteAllText(Path.ChangeExtension(scope.progress, ".after-restore.json"), after);
        Type type = scope.match.GetType().GetNestedType("Workshop", BindingFlags.Public); object a = JsonUtility.FromJson(before, type), b = JsonUtility.FromJson(after, type);
        float maximumPosition = 0, maximumQuaternion = 0;
        Action<object, object, string> pose = (left, right, label) =>
        {
            Vector3 lp = (Vector3)Get(left, "position"), rp = (Vector3)Get(right, "position"); var lq = (Quaternion)Get(left, "rotation"); var rq = (Quaternion)Get(right, "rotation");
            float position = Vector3.Distance(lp, rp), rotation = Mathf.Max(Mathf.Abs(lq.x - rq.x), Mathf.Abs(lq.y - rq.y), Mathf.Abs(lq.z - rq.z), Mathf.Abs(lq.w - rq.w));
            maximumPosition = Mathf.Max(maximumPosition, position); maximumQuaternion = Mathf.Max(maximumQuaternion, rotation);
            Assert.That(position, Is.LessThanOrEqualTo(.00001f), label + " physical position readback (10micrometres, V1 observed0.238micrometres).");
            Assert.That(rotation, Is.LessThanOrEqualTo(.000001f), label + " normalized physical quaternion readback.");
            Assert.That((Vector3)Get(right, "velocity"), Is.EqualTo((Vector3)Get(left, "velocity")), label + " exact saved velocity"); Assert.That((Vector3)Get(right, "angular"), Is.EqualTo((Vector3)Get(left, "angular")), label + " exact saved angular velocity");
            foreach (object node in new[] { left, right }) { Set(node, "position", Vector3.zero); Set(node, "rotation", Quaternion.identity); Set(node, "velocity", Vector3.zero); Set(node, "angular", Vector3.zero); }
        };
        var ap = (Array)Get(a, "pieces"); var bp = (Array)Get(b, "pieces"); Assert.That(bp.Length, Is.EqualTo(ap.Length)); for (int i = 0; i < ap.Length; i++) pose(Get(ap.GetValue(i), "pose"), Get(bp.GetValue(i), "pose"), "piece" + i);
        var af = (Array)Get(a, "families"); var bf = (Array)Get(b, "families"); Assert.That(bf.Length, Is.EqualTo(af.Length)); for (int i = 0; i < af.Length; i++) pose(Get(af.GetValue(i), "egg"), Get(bf.GetValue(i), "egg"), "egg" + i);
        var at = (Array)Get(a, "trucks"); var bt = (Array)Get(b, "trucks"); Assert.That(bt.Length, Is.EqualTo(at.Length));
        for (int i = 0; i < at.Length; i++) { pose(Get(at.GetValue(i), "body"), Get(bt.GetValue(i), "body"), "truck" + i); var ac = (Array)Get(at.GetValue(i), "attached"); var bc = (Array)Get(bt.GetValue(i), "attached"); Assert.That(bc.Length, Is.EqualTo(ac.Length)); for (int j = 0; j < ac.Length; j++) pose(ac.GetValue(j), bc.GetValue(j), "truck" + i + "/cab" + j); }
        Assert.That(JsonUtility.ToJson(b), Is.EqualTo(JsonUtility.ToJson(a)), "Every schema/version/profile/piece/support/family/clock value remains exact; only typed PhysX pose readback has a numerical bound.");
        TestContext.WriteLine($"NEST_RESTORE_READBACK maximumPositionMetres={maximumPosition:R} maximumQuaternionComponent={maximumQuaternion:R} allSemanticIdsClockAndVelocitiesExact=true");
    }
    static IEnumerator Advance(Scope s, float seconds, float deadlineSeconds = 6)
    {
        float start = Time.time, deadline = Time.realtimeSinceStartup + deadlineSeconds;
        while (Time.time - start < seconds && Time.realtimeSinceStartup < deadline) yield return null;
        s.Trace("advance-" + seconds); Assert.That(Time.time - start, Is.GreaterThanOrEqualTo(seconds), "Original simulation must advance, never hang on scaled waits.");
    }
    static IEnumerator Tap(Scope s)
    {
        var action = ((Key[])Get(s.match, "actionKeys"))[0]; InputSystem.QueueStateEvent(s.keyboard, new KeyboardState(action)); InputSystem.Update(); yield return Advance(s, .12f);
        InputSystem.QueueStateEvent(s.keyboard, new KeyboardState()); InputSystem.Update(); yield return Advance(s, .16f);
    }
    static string Geometry(Component truck)
    {
        var box = (BoxCollider)Get(truck, "shape");
        return truck.GetInstanceID() + ":" + ((Rigidbody)Get(truck, "body")).mass + ":" + box.GetInstanceID() + ":" + box.center.ToString("R") + ":" + box.size.ToString("R") + string.Join("/", Components(Get(truck, "wheels")).Select(w => w.GetInstanceID() + ":" + Get(Get(w, "wheel"), "radius") + ":" + Get(Get(w, "spring"), "maxLength")));
    }
    static void Capture(Scope s, string name, int width = 1280, int height = 720)
    {
        Sports("SportsControlledCapture").GetMethods().Single(m => m.Name == "Save" && m.GetParameters().Length == 5).Invoke(null, new object[] { s.match, width, height, Path.GetFullPath("Logs/TruckSports/Impossible07/NestControlled/" + name + ".png"), null });
    }
    [UnityTest] public IEnumerator OwnedKeyboardGraspsFiniteForceMaterialDropsWithoutPickupScoreAndSafeSaveRejectsCorruption()
    {
        using (var s = new Scope("human-save"))
        {
            yield return s.Load(); var m = s.match; var setup = Get(m, "setup"); Set(setup, "count", 1); Call(m, "AllBots");
            var player = ((Array)Get(setup, "players")).GetValue(0); Set(player, "source", Enum.ToObject(Get(player, "source").GetType(), 1)); Set(m, "option", 0);
            InputSystem.QueueStateEvent(s.keyboard, new KeyboardState()); InputSystem.Update(); Assert.That((bool)Call(m, "StartMatch"), Is.True); Time.timeScale = 3; yield return Advance(s, 2);
            var builder = Components(Get(m, "builders"))[0]; var truck = (Component)Get(builder, "truck"); string geometry = Geometry(truck); var rules = (NestRules)Get(m, "rules");
            Assert.That(Get(truck, "appearanceHost"), Is.Not.Null); Assert.That(((Rigidbody)Get(truck, "body")).isKinematic, Is.False);
            yield return Tap(s); float deadline = Time.realtimeSinceStartup + 18;
            while (Get(builder, "Carried") == null && Time.realtimeSinceStartup < deadline) yield return null;
            s.Trace("physical-grasp"); var material = (Component)Get(builder, "Carried"); Assert.That(material, Is.Not.Null, "The original physical hand must reach and grasp a real loose piece.");
            var joint = (FixedJoint)Get(material, "Grip"); Assert.That(joint.connectedBody, Is.SameAs(Get(builder, "hand"))); Assert.That(joint.breakForce, Is.EqualTo(2500)); Assert.That(joint.breakTorque, Is.EqualTo(1800));
            Assert.That(((Rigidbody)Get(material, "body")).isKinematic, Is.False); Assert.That(rules.quality[0], Is.Zero); Assert.That(Get(material, "Site"), Is.Null);
            yield return Tap(s); Assert.That(Get(builder, "Mode").ToString(), Is.EqualTo("Placing"));
            var hand = (Rigidbody)Get(builder, "hand"); var before = hand.position;
            InputSystem.QueueStateEvent(s.keyboard, new KeyboardState(Key.D)); InputSystem.Update(); yield return Advance(s, .8f);
            InputSystem.QueueStateEvent(s.keyboard, new KeyboardState()); InputSystem.Update(); Assert.That(Vector3.Distance(before, hand.position), Is.GreaterThan(.4f));
            yield return Tap(s); yield return Advance(s, .3f); Assert.That(Get(material, "Grip"), Is.Null); Assert.That((int)Get(builder, "PhysicalDrops"), Is.EqualTo(1)); Assert.That(rules.quality[0], Is.Zero);
            Call(m, "Pause", 0, false); yield return null; yield return null; s.Trace("paused-save");
            string save = (string)Call(m, "CaptureWorkshopJson"); var poses = Components(Get(m, "materials")).Select(p => ((Rigidbody)Get(p, "body")).position).ToArray();
            string impossibleIncubation = System.Text.RegularExpressions.Regex.Replace(save, "\"incubation\":0(?:\\.0)?(?=[,}])", "\"incubation\":2.0");
            foreach (string bad in new[] { save.Replace("\"version\":1", "\"version\":999"), save.Replace("\"version\":1,", ""), save.Replace("depot0.piece0", "unknown-piece"), impossibleIncubation, "{not-json" })
            {
                Assert.That(bad, Is.Not.EqualTo(save), "Every malformed-payload fixture must actually change the captured JSON.");
                var args = new object[] { bad, null }; Assert.That((bool)Call(m, "TryRestoreWorkshopJson", args), Is.False); Assert.That(args[1], Is.Not.Empty);
                CollectionAssert.AreEqual(poses, Components(Get(m, "materials")).Select(p => ((Rigidbody)Get(p, "body")).position).ToArray()); Assert.That((string)Call(m, "CaptureWorkshopJson"), Is.EqualTo(save));
            }
            var restore = new object[] { save, null }; Assert.That((bool)Call(m, "TryRestoreWorkshopJson", restore), Is.True, restore[1] as string);
            EquivalentRestoredSnapshot(s, save, (string)Call(m, "CaptureWorkshopJson")); Assert.That(Geometry(truck), Is.EqualTo(geometry));
            Capture(s, "physical-human-grasp-drop-save"); Call(m, "ExtraAction", "nest-finish"); Assert.That((bool)Get(m, "Finished"), Is.True); Assert.That(Get(m, "Results").ToString(), Does.Contain("COOPERATIVE"));
            TestContext.WriteLine("NEST_HUMAN synthetic keyboard only; actual original NWH truck, finite-force grasp/placement/drop, zero pickup score and atomic paused semantic save. No hardware/Windows/listening acceptance.");
        }
    }
    [UnityTest, Timeout(60000)] public IEnumerator DiagnosticOnlyOrdinaryAITransportPosesAndFiniteGripForces()
    {
        using (var s = new Scope("transport-diagnosis-v2"))
        {
            yield return s.Load(); var m = s.match; Set(Get(m, "setup"), "count", 4); Set(m, "option", 0); Call(m, "AllBots");
            var players = (Array)Get(Get(m, "setup"), "players"); var bodies = new[] { "novelty.hotdog", "truck.monster", "truck.fire", "novelty.icecream" };
            for (int i = 0; i < 4; i++) { string id = (string)Get(players.GetValue(i), "participantId"); var draft = Static("SportsParticipantRegistry", "GetAppearance", id); Set(draft, "bodyId", bodies[i]); Static("SportsParticipantRegistry", "TrySetAppearance", id, draft); }
            Assert.That((bool)Call(m, "StartMatch"), Is.True); Time.timeScale = 4; float end = Time.realtimeSinceStartup + 28, next = 0; int samples = 0;
            while (Time.realtimeSinceStartup < end)
            {
                if (Time.realtimeSinceStartup >= next) { s.Trace("transport-physical-observation"); samples++; next = Time.realtimeSinceStartup + .2f; }
                yield return null;
            }
            Assert.That(samples, Is.GreaterThan(100)); Assert.That((float)Get(m, "Clock"), Is.GreaterThan(75)); Assert.That((bool)Get(m, "Live"), Is.True);
            Capture(s, "diagnostic-only-transport-v2", 1920, 1080);
            TestContext.WriteLine("DIAGNOSTIC ONLY: real ordinary AI poses, hand/placement targets, exact finite-joint force/torque and grasp/drop observations. No construction acceptance, timeout increase or gameplay mutation.");
        }
    }
    [UnityTest,Timeout(270000)] public IEnumerator FourOrdinaryAIPhysicallyBuildRootedNestProtectEggAndHatchAnimatedFamilyThenRematch()
    {
        using (var s = new Scope("four-ai-family"))
        {
            yield return s.Load(); var m = s.match; var setup = Get(m, "setup"); Set(setup, "count", 4); Set(m, "option", 0); Call(m, "AllBots");
            var players = (Array)Get(setup, "players"); var bodyIds = new[] { "novelty.hotdog", "truck.monster", "truck.fire", "novelty.icecream" };
            for (int i = 0; i < 4; i++)
            {
                string id = (string)Get(players.GetValue(i), "participantId"); var draft = Static("SportsParticipantRegistry", "GetAppearance", id); Set(draft, "bodyId", bodyIds[i]);
                Static("SportsParticipantRegistry", "TrySetAppearance", id, draft);
            }
            Assert.That((bool)Call(m, "StartMatch"), Is.True); Time.timeScale = 4;
            var builders = Components(Get(m, "builders")); var trucks = builders.Select(b => (Component)Get(b, "truck")).ToArray(); var geometry = trucks.Select(Geometry).ToArray();
            var rules = (NestRules)Get(m, "rules"); var site = Components(Get(m, "sites"))[0]; var family = (Component)Get(site, "family");
            var birds = UnityEngine.Object.FindObjectsByType(Type.GetType("LWS.TruckNestBuilding.NestBirdFlight, Assembly-CSharp", true), FindObjectsSortMode.None).Cast<Component>().ToArray();
            Assert.That(birds.Length, Is.EqualTo(6)); var bones = birds.SelectMany(b => ((Animator)Get(b, "animator")).GetComponentsInChildren<SkinnedMeshRenderer>(true).SelectMany(r => r.bones)).Distinct().ToArray();
            Assert.That(bones.Length, Is.GreaterThanOrEqualTo(12), "Use actual imported skin bones, never the orbiting actor transform as animation evidence.");
            var boneStart = bones.Select(b => b.localRotation).ToArray(); bool boneMotion = false, eggObserved = false; int maximumRooted = 0;
            float deadline = Time.realtimeSinceStartup + 240, next = 0,nextOrientation=0;
            var offsets=new Dictionary<string,Quaternion>();var tilted=new HashSet<string>();var corrected=new HashSet<string>();
            while (!(bool)Get(m, "Finished") && Time.realtimeSinceStartup < deadline)
            {
                if(Time.realtimeSinceStartup>=nextOrientation){VerifyActualPlacementGraspTargets(m,offsets,tilted,corrected);nextOrientation=Time.realtimeSinceStartup+.15f;}
                if (Time.realtimeSinceStartup >= next) { s.Trace("ordinary-ai-build"); next = Time.realtimeSinceStartup + 3; }
                int rooted = (int)Get(site, "RootedCount"); maximumRooted = Mathf.Max(rooted, maximumRooted);
                if (rules.received[0] && !eggObserved) { eggObserved = true; Assert.That((bool)Get(site, "CoreReady"), Is.True); Assert.That(((Rigidbody)Get(family, "eggBody")).isKinematic, Is.False); Capture(s, "actual-ai-supported-egg"); }
                for (int i = 0; i < bones.Length; i++) boneMotion |= Quaternion.Angle(boneStart[i], bones[i].localRotation) > 5;
                yield return null;
            }
            s.Trace("ordinary-ai-end"); Assert.That((bool)Get(m, "Finished"), Is.True, "No AI placement, score, pose or hatch injection: ordinary physical construction must complete within the bounded native window.");
            Assert.That(maximumRooted, Is.GreaterThanOrEqualTo(10)); Assert.That(eggObserved, Is.True); Assert.That(rules.hatched[0], Is.True); Assert.That(rules.quality[0], Is.GreaterThanOrEqualTo(380));
            Assert.That(builders.All(b => (int)Get(b, "Grasps") > 0 && (int)Get(b, "PhysicalDrops") > 0), Is.True); Assert.That((int)Get(family, "WiggleFrames"), Is.GreaterThan(2));
            Assert.That(boneMotion, Is.True); Assert.That(birds.All(b => ((Animator)Get(b, "animator")).runtimeAnimatorController.animationClips.Length > 0), Is.True);
            foreach (var baby in (Transform[])Get(family, "babies")) { Assert.That(baby.gameObject.activeInHierarchy, Is.True); Assert.That(baby.GetComponentsInChildren<Renderer>(true).Length, Is.GreaterThan(0)); Assert.That(baby.GetComponentsInChildren<Rigidbody>(true), Is.Empty); }
            Assert.That(((AudioSource)Get(family, "tinyHorn")).clip, Is.Not.Null); CollectionAssert.AreEqual(geometry, trucks.Select(Geometry).ToArray()); Capture(s, "actual-four-ai-family-results", 1920, 1080);
            Assert.That((bool)Call(m, "StartMatch"), Is.True); Assert.That(rules.hatched.All(h => !h), Is.True); Assert.That((int)Get(site, "RootedCount"), Is.Zero); Assert.That(builders.All(b => (int)Get(b, "Grasps") == 0), Is.True);Assert.That(builders.All(b=>!(bool)Get(b,"GraspOffsetCaptured")),Is.True,"Rematch clears the previous actual grasp frames.");
            TestContext.WriteLine("NEST_ACTUAL_ORIENTATION generations="+offsets.Count+" tiltedPlacingGenerations="+tilted.Count+" physicallyAlignedTiltedGenerations="+corrected.Count+"; same4x/original240secondloop/allfamilygates; missingnaturaltiltcaseisnotclaimedobserved.");
            TestContext.WriteLine("NEST_AI ordinary four-owner NWH input, real grasp/carry/drop/contact/interlock support, dynamic egg lining contact, genuine animated CC0 birds and wiggling render-only baby haulers; no state/pose/outcome injection. Controlled native captures only.");
        }
    }
    [UnityTest,Timeout(60000)]public IEnumerator NaturalReleasedBaseBranchesKeepActualSleepingContactsAndRootOnTheUnchangedDeck()
    {
        using(var s=new Scope("natural-sleeping-base-support"))
        {
            yield return s.Load();var m=s.match;Set(Get(m,"setup"),"count",4);Set(m,"option",0);Call(m,"AllBots");Assert.That((bool)Call(m,"StartMatch"),Is.True);Time.timeScale=4;
            var site=Components(Get(m,"sites"))[0];var materials=Components(Get(m,"materials"));bool sleepingSupportObserved=false;float deadline=Time.realtimeSinceStartup+32,next=0;
            while((int)Get(site,"RootedCount")<2&&Time.realtimeSinceStartup<deadline)
            {
                foreach(var material in materials)
                {
                    var body=(Rigidbody)Get(material,"body");if((int)Get(material,"LastOwner")<0||Get(material,"Site")!=null||(FixedJoint)Get(material,"Grip")!=null||!body.IsSleeping())continue;
                    for(int slot=0;slot<2;slot++)sleepingSupportObserved|=(bool)Call(site,"Fits",material,slot,body.position,body.rotation)&&(Rigidbody)Call(material,"ContactSupport",site,slot)!=null;
                }
                if(Time.realtimeSinceStartup>=next){s.Trace("natural-sleeping-contact-rooting");next=Time.realtimeSinceStartup+1;}yield return null;
            }
            s.Trace("natural-sleeping-contact-result");Assert.That(sleepingSupportObserved,Is.True,"A genuine released fitting branch must keep its actual collision pair while PhysX sleeps; no WakeUp/force/pose injection.");Assert.That((int)Get(site,"RootedCount"),Is.GreaterThanOrEqualTo(2));
            var slots=(Array)Get(site,"slots");for(int slot=0;slot<2;slot++){var material=(Component)slots.GetValue(slot);Assert.That(material,Is.Not.Null);var joint=(FixedJoint)Get(material,"Interlock");Assert.That(joint.connectedBody,Is.SameAs(Get(site,"body")));Assert.That(joint.breakForce,Is.EqualTo(3500));Assert.That(joint.breakTorque,Is.EqualTo(1500));Assert.That((bool)Call(site,"Rooted",material),Is.True);}
            Assert.That(Components(Get(m,"builders")).Any(b=>(int)Get(b,"PhysicalDrops")>0),Is.True);Capture(s,"actual-natural-sleeping-contact-base-rooted",1920,1080);
            TestContext.WriteLine("NEST_SLEEP_SUPPORT ordinary AI/gravity/collisions/PhysX sleep and unchanged1.2srest/fit/finite interlocks. First-two-base support scope only; fullnest/egg/hatching acceptance remains the separate unchanged natural test.");
        }
    }
}
#endif

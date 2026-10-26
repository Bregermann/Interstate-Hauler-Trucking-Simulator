#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class SkyTowReplayRestartTests
{
    const BindingFlags Any = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    static Type Sports(string name) => Type.GetType("LWS.TruckSports." + name + ", Assembly-CSharp", true);
    static object Get(object target, string name) => target.GetType().GetField(name, Any) is FieldInfo f ? f.GetValue(target) : target.GetType().GetProperty(name, Any).GetValue(target);
    static void Set(object target, string name, object value) => target.GetType().GetField(name, Any).SetValue(target, value);
    static object Call(object target, string name, params object[] args) => target.GetType().GetMethods(Any).Single(m => m.Name == name && m.GetParameters().Length == args.Length).Invoke(target, args);
    static object Registry(string name, params object[] args) => Sports("SportsParticipantRegistry").GetMethod(name).Invoke(null, args);
    static int[] Ids(Component root) => root.GetComponentsInChildren<Component>(true).Where(c => c is Rigidbody || c is Collider || c is Joint).Select(c => c.GetInstanceID()).OrderBy(id => id).ToArray();
    static int[] OriginalPhysicalIds(Component truck) => new[] { ((Component)Get(truck, "body")).GetInstanceID(), ((Component)Get(truck, "shape")).GetInstanceID() }.Concat(((IEnumerable)Get(truck, "wheels")).Cast<Component>().Select(w => w.GetInstanceID())).ToArray();
    static string Appearance(string id) => JsonUtility.ToJson(Registry("GetAppearance", id));
    static void CurrentHierarchies(object motion)
    {
        var sources = (Transform[])Get(motion, "sources"); var ghosts = (Transform[])Get(motion, "ghosts");
        var cached = (Transform[][])Get(motion, "liveParts"); var copies = (Transform[][])Get(motion, "ghostParts");
        for (int i = 0; i < 8; i++)
        {
            var actual = sources[i].GetComponentsInChildren<Transform>(true); var ghost = ghosts[i].GetComponentsInChildren<Transform>(true);
            Assert.That(cached[i].All(part => part != null), Is.True, "No disposed appearance Transform may remain cached, source " + i);
            CollectionAssert.AreEqual(actual, cached[i], "Every cached source must be its actual current hierarchy, including inactive slots.");
            CollectionAssert.AreEqual(ghost, copies[i]); CollectionAssert.AreEqual(actual.Skip(1).Select(t => t.name), ghost.Skip(1).Select(t => t.name));
            Assert.That(ghosts[i].GetComponentsInChildren<Rigidbody>(true), Is.Empty); Assert.That(ghosts[i].GetComponentsInChildren<Collider>(true), Is.Empty);
        }
    }
    static void Weather(Behaviour match, bool snow)
    {
        var presets = Get(match, "presets"); var entry = ((Array)Get(presets, "entries")).Cast<object>().First(e => snow ? (bool)Get(e, "Snow") : (string)Get(e, "displayName") == "Clear");
        Call(presets, "Select", Get(entry, "id"));
    }

    [UnityTest, Timeout(360000)]
    public IEnumerator NativeMixedOwnerCountsRebindAllEightBeforeSamplesAndRetainWeatherReplayAuthority()
    {
        using (var prefs = (IDisposable)Sports("SportsPrefs").GetMethod("IsolateValidation").Invoke(null, new object[] { "SkyTowReplayRestart" + Guid.NewGuid().ToString("N") }))
        {
            Registry("ForgetCache"); var isolation = new GameObject("Sky replay restart original AI input isolation"); UnityEngine.Object.DontDestroyOnLoad(isolation); isolation.AddComponent(Sports("SportsProbeInputIsolation")); Behaviour match = null;
            try
            {
                EditorSceneManager.LoadSceneInPlayMode("Assets/TruckSkyTow/Scenes/TruckSkyTow.unity", new LoadSceneParameters(LoadSceneMode.Single)); yield return null; yield return null;
                match = UnityEngine.Object.FindObjectsByType(Sports("SportsEventSession"), FindObjectsSortMode.None).Cast<Behaviour>().Single();
                var setup = Get(match, "setup"); Set(setup, "count", 4); Call(match, "NormalizeSetup"); Call(match, "AllBots");
                var racers = ((Array)Get(match, "racers")).Cast<Component>().ToArray(); var trucks = racers.Select(r => (Component)Get(r, "truck")).ToArray();
                var hosts = trucks.Select(t => Get(t, "appearanceHost")).ToArray(); Assert.That(hosts.All(h => h is UnityEngine.Object o && o != null), Is.True, "Saved original Sky appearance hosts must be authored.");
                var ids = new string[4];
                for (int i = 0; i < 4; i++)
                {
                    ids[i] = (string)Get(Registry("Create", "Same Named Sky Driver"), "id"); Assert.That((bool)Registry("UseInSeat", i, ids[i]), Is.True);
                    var draft = Registry("GetAppearance", ids[i]); Set(draft, "bodyId", "novelty.icecream"); Registry("TrySetAppearance", ids[i], draft);
                }
                // Fifth genuine owner forces equal-name/count replacement on the
                // last full roster: topology equality cannot validate cached refs.
                string replacement = (string)Get(Registry("Create", "Same Named Sky Driver"), "id"); var alternate = Registry("GetAppearance", replacement); Set(alternate, "bodyId", "novelty.icecream"); Registry("TrySetAppearance", replacement, alternate);
                string[] saved = ids.Concat(new[] { replacement }).Select(Appearance).ToArray();
                Call(match, "RefreshParticipants", false); Call(match, "AllBots");
                var review = Get(match, "review"); var motion = Get(review, "motion"); var roots = ((Transform[])Get(motion, "sources")).ToArray(); var ghosts = ((Transform[])Get(motion, "ghosts")).ToArray();
                var actorIds = racers.Select(r => r.GetInstanceID()).ToArray(); var boatIds = racers.Select(r => ((Rigidbody)Get(r, "boat")).GetInstanceID()).ToArray(); var jointIds = racers.Select(r => ((Joint)Get(r, "tether")).GetInstanceID()).ToArray();
                var originalPhysics = trucks.Select(OriginalPhysicalIds).ToArray(); int round = 0;
                foreach (int count in new[] { 4, 1, 2, 3, 4, 4, 1, 4 })
                {
                    Call(match, "SetupScreen"); Set(setup, "count", count); Call(match, "NormalizeSetup");
                    if (round == 5) { Assert.That((bool)Registry("UseInSeat", 0, replacement), Is.True); Call(match, "RefreshParticipants", false); }
                    if (round == 7) { Assert.That((bool)Registry("UseInSeat", 0, ids[0]), Is.True); Call(match, "RefreshParticipants", false); }
                    Call(match, "AllBots"); bool snow = round >= 2 && round <= 4; Weather(match, snow);
                    Assert.That((bool)Call(match, "StartMatch"), Is.True); CurrentHierarchies(motion); Assert.That((int)Get(review, "WeatherMetadataRefreshes"), Is.Zero);
                    var poseBuffer = Get(motion, "roots"); var partBuffer = Get(motion, "parts"); var timeBuffer = Get(motion, "times");
                    float deadline = Time.realtimeSinceStartup + 30;
                    while (((float)Get(match, "Age") < 1 || (int)Get(review, "WeatherMetadataRefreshes") != 1) && Time.realtimeSinceStartup < deadline) yield return null;
                    Assert.That((float)Get(match, "Age"), Is.GreaterThanOrEqualTo(1)); Assert.That((int)Get(review, "WeatherMetadataRefreshes"), Is.EqualTo(1));
                    CurrentHierarchies(motion); Assert.That(Get(motion, "roots"), Is.SameAs(poseBuffer)); Assert.That(Get(motion, "parts"), Is.SameAs(partBuffer)); Assert.That(Get(motion, "times"), Is.SameAs(timeBuffer));
                    var snowSurface = UnityEngine.Object.FindObjectsByType(Sports("SportsSnowSurface"), FindObjectsInactive.Include, FindObjectsSortMode.None).Cast<Component>().Single(c => c.gameObject.scene == match.gameObject.scene);
                    Assert.That((bool)Get(Get(snowSurface, "surface"), "EnvironmentBindingsReady"), Is.True);
                    for (int i = 0; i < 4; i++)
                    {
                        Assert.That(racers[i].gameObject.activeSelf, Is.EqualTo(i < count));
                        if (i < count) Assert.That(Get(hosts[i], "ProfileId"), Is.EqualTo(i == 0 && round >= 5 && round < 7 ? replacement : ids[i])); else Assert.That(Get(hosts[i], "ProfileId"), Is.Null);
                        Assert.That(Get(hosts[i], "AppearanceRoot") is Transform t && t != null, Is.EqualTo(i < count));
                    }
                    CollectionAssert.AreEqual(roots, (Transform[])Get(motion, "sources")); CollectionAssert.AreEqual(ghosts, (Transform[])Get(motion, "ghosts"));
                    CollectionAssert.AreEqual(actorIds, racers.Select(r => r.GetInstanceID())); CollectionAssert.AreEqual(boatIds, racers.Select(r => ((Rigidbody)Get(r, "boat")).GetInstanceID())); CollectionAssert.AreEqual(jointIds, racers.Select(r => ((Joint)Get(r, "tether")).GetInstanceID()));
                    for (int i = 0; i < 4; i++) CollectionAssert.AreEqual(originalPhysics[i], OriginalPhysicalIds(trucks[i]), "Replay restart must retain the original chassis/body/wheel controllers.");
                    // Pure weather metadata refresh must preserve actual scored
                    // outcomes and every already populated pose/time buffer.
                    Call(match, "Pause", 0, false); string rules = JsonUtility.ToJson(Get(match, "rules")); int receipts = (int)Get(Get(match, "ScoreJournal"), "Count");
                    var physicalBeforeMetadata = trucks.Select(Ids).ToArray(); int frames = (int)Get(motion, "count"); var times = ((float[])Get(motion, "times")).ToArray(); var recorded = ((Array)poseBuffer).Cast<object>().Take(frames * 8).ToArray();
                    Assert.That(frames, Is.GreaterThan(2)); Assert.That((bool)Call(motion, "RefreshAppearanceSources", Enumerable.Range(0, 8).ToArray(), false), Is.True);
                    Assert.That(Get(motion, "roots"), Is.SameAs(poseBuffer)); Assert.That(Get(motion, "times"), Is.SameAs(timeBuffer)); CollectionAssert.AreEqual(times, (float[])Get(motion, "times")); CollectionAssert.AreEqual(recorded, ((Array)Get(motion, "roots")).Cast<object>().Take(frames * 8));
                    Assert.That(JsonUtility.ToJson(Get(match, "rules")), Is.EqualTo(rules)); Assert.That(Get(Get(match, "ScoreJournal"), "Count"), Is.EqualTo(receipts));
                    if (round == 6)
                    {
                        // An actual post-start topology change in an inactive
                        // source must still fail the strict weather guard. This
                        // temporary child has no renderer or physical component.
                        var unexpected = new GameObject("Controlled unexpected replay topology"); unexpected.transform.SetParent(roots[6], false); Set(review, "weatherMetadataPending", true);
                        try
                        {
                            var failure = Assert.Throws<TargetInvocationException>(() => Call(review, "Record", (float)Get(match, "Age") + .2f));
                            Assert.That(failure.InnerException, Is.TypeOf<InvalidOperationException>()); Assert.That(failure.InnerException.Message, Does.Contain("existing unchanged eight-source hierarchy"));
                            Assert.That(Get(motion, "roots"), Is.SameAs(poseBuffer)); Assert.That((int)Get(motion, "count"), Is.EqualTo(frames));
                        }
                        finally { UnityEngine.Object.DestroyImmediate(unexpected); Set(review, "weatherMetadataPending", false); }
                        CurrentHierarchies(motion);
                    }
                    for (int i = 0; i < 4; i++) CollectionAssert.AreEqual(physicalBeforeMetadata[i], Ids(trucks[i]), "Metadata refresh cannot add/remove any physical component.");
                    // Controlled original match-end/replay gate, never a fabricated
                    // ring score/finish. The recorded run above used native physics.
                    Call(match, "EndMatch"); Assert.That((bool)Call(review, "Play"), Is.True);
                    for (int i = 0; i < 8; i++) Assert.That(ghosts[i].gameObject.activeSelf, Is.EqualTo(i / 2 < count), "Recorded visibility must match genuine active owner count.");
                    foreach (int angle in new[] { 0, 1, 2 }) { if (angle > 0) Call(review, "Action", "replayangle"); Call(review, "Action", "replayrewind"); Assert.That((int)Get(motion, "Angle"), Is.EqualTo(angle)); }
                    Call(review, "Stop", false); CollectionAssert.AreEqual(saved, ids.Concat(new[] { replacement }).Select(Appearance));
                    TestContext.WriteLine("SKY_RESTART round=" + round + " count=" + count + " snow=" + snow + " frames=" + frames + " all8 current cached identities, retained roots/physics, one metadata update, actual owner mask and saved drafts unchanged"); round++;
                }
                Call(match, "SetupScreen");
            }
            finally { Time.timeScale = 1; if (match != null) Call(match, "SetupScreen"); UnityEngine.Object.DestroyImmediate(isolation); Registry("ForgetCache"); }
        }
    }
}
#endif

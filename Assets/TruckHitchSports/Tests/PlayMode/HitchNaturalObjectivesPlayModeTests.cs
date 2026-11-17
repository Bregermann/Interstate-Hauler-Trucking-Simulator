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

// Focused acceptance of the authored sports, using their normal AI, rules and
// match lengths. Reflection observes existing gameplay; it never resolves it.
public sealed class HitchNaturalObjectivesPlayModeTests
{
    const float PlaybackRate = 3;
    static Type RuntimeType(string name) => Type.GetType("LWS.TruckHitchSports." + name + ", Assembly-CSharp", true);
    static object Read(object target, string name)
    {
        for (Type type = target.GetType(); type != null; type = type.BaseType)
        {
            var field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            if (field != null) return field.GetValue(target);
        }
        throw new MissingFieldException(target.GetType().FullName, name);
    }
    static object Property(object target, string name) => target.GetType().GetProperty(name).GetValue(target);
    static object Call(object target, string name, params object[] args) => target.GetType().GetMethod(name).Invoke(target, args);
    static IDisposable Preferences() => (IDisposable)Type.GetType("LWS.TruckSports.SportsPrefs, Assembly-CSharp", true).GetMethod("IsolateValidation").Invoke(null, new object[] { "Alpha10NaturalHitchObjectives" });
    static int Count(object match) => (int)Read(Read(match, "setup"), "count");
    static Rigidbody TrailerBody(object trailer) => (Rigidbody)Read(trailer, "Body");
    static Vector3 Position(object truck) => ((Rigidbody)Read(truck, "body")).position;

    [UnityTest] public IEnumerator NaturalHighwireAICompletesActualSuspendedCrossingAndRematches()
        => NaturalRound("JackknifeHighwire", "HighwireMatch", true);
    [UnityTest] public IEnumerator NaturalHitchRunAIPhysicallyCouplesDeliversAndRematches()
        => NaturalRound("HitchAndRun", "HitchRunMatch", false);
    [UnityTest] public IEnumerator NaturalTrebuchetAILoadsReleasesImpactsAndRematches()
        => NaturalRound("TrailerTrebuchet", "TrebuchetMatch", true);
    [UnityTest] public IEnumerator NaturalHandoffAIPhysicallyTransfersFinishesAndRematches()
        => NaturalRound("HitchHandoff", "HandoffMatch", false);
    [UnityTest] public IEnumerator NaturalUnhitchedAICompletesSupportedTrailerRideAndRematches()
        => NaturalRound("Unhitched", "UnhitchedMatch", true);

    IEnumerator NaturalRound(string scene, string className, bool single)
    {
        using (Preferences())
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/TruckHitchSports/Scenes/" + scene + ".unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null; yield return null;
            object match = UnityEngine.Object.FindObjectsByType(RuntimeType(className), FindObjectsSortMode.None).Single();
            // These are ordinary setup choices, not changes to the course, timer,
            // physics parameters, actor poses or any gameplay result.
            match.GetType().GetField("option").SetValue(match, 0);
            if (single) Read(match, "setup").GetType().GetField("count").SetValue(Read(match, "setup"), 1);
            Call(match, "NormalizeSetup"); Call(match, "AllBots");
            object weather = Read(match, "presentationWeather");
            if (weather != null && Read(weather, "presets") != null) Call(Read(weather, "presets"), "Select", "9aad5ff5dfa09ce468205f676714f891");
            Assert.That((bool)Call(match, "StartMatch"), Is.True, scene + " must start through the ordinary menu/session entry point.");
            if (weather != null && Read(weather, "presets") == null) Call(weather, "Configure", 1234, 1, 0);
            var trailers = (Array)Read(match, "trailers"); var couplers = (Array)Read(match, "couplers");
            var origin = new Vector3[trailers.Length]; var travel = new float[trailers.Length];
            for (int t = 0; t < trailers.Length; t++) origin[t] = TrailerBody(trailers.GetValue(t)).position;
            var attached = new bool[Count(match)];
            var probes = new HitchNaturalImpactProbe[trailers.Length];
            bool loaded = false, released = false;
            float releasedHeight = 0, flightRise = 0, flightSpeed = 0;
            if (className == "TrebuchetMatch")
                for (int t = 0; t < trailers.Length; t++) probes[t] = TrailerBody(trailers.GetValue(t)).gameObject.AddComponent<HitchNaturalImpactProbe>();
            try
            {
                Time.timeScale = PlaybackRate;
                float normalSeconds = (float)Read(match, "matchSeconds");
                float wallDeadline = Time.realtimeSinceStartup + Mathf.Clamp((normalSeconds + 15) / PlaybackRate * 1.8f, 45, 150);
                while (!(bool)Property(match, "Finished") && Time.realtimeSinceStartup < wallDeadline)
                {
                    for (int i = 0; i < attached.Length; i++) attached[i] |= Property(couplers.GetValue(i), "Attached") != null;
                    for (int t = 0; t < trailers.Length; t++) travel[t] = Mathf.Max(travel[t], Vector3.ProjectOnPlane(TrailerBody(trailers.GetValue(t)).position - origin[t], Vector3.up).magnitude);
                    if (className == "TrebuchetMatch")
                    {
                        object engine = ((Array)Read(match, "engines")).GetValue(0); var body = TrailerBody(trailers.GetValue(0));
                        loaded |= Property(engine, "Load") != null;
                        if ((bool)Property(engine, "Released"))
                        {
                            if (!released) releasedHeight = body.position.y;
                            released = true; probes[0].armed = true;
                            flightRise = Mathf.Max(flightRise, body.position.y - releasedHeight);
                            flightSpeed = Mathf.Max(flightSpeed, body.linearVelocity.magnitude);
                        }
                    }
                    yield return null;
                }
                string diagnostics = Diagnostics(match, className, travel);
                Assert.That((bool)Property(match, "Finished"), Is.True, scene + " must resolve naturally within its authored length." + diagnostics);
                var scores = (double[])Read(match, "Scores");
                if (className == "HighwireMatch")
                {
                    Assert.That(attached[0], Is.True, diagnostics);
                    Assert.That(((int[])Read(match, "gate"))[0], Is.EqualTo(((Vector3[])Read(match, "route")).Length), "A fall or timer-only result is not a crossing." + diagnostics);
                    Assert.That(travel[0], Is.GreaterThan(150), "The physical trailer must traverse the suspended course." + diagnostics);
                    Assert.That((bool)Call(match, "ResultEligible", 0), Is.True, diagnostics);
                    Assert.That(scores[0], Is.GreaterThan(0), diagnostics);
                }
                else if (className == "HitchRunMatch")
                {
                    var deliveries = (int[])Read(match, "Deliveries");
                    int delivered = -1; for (int i = 0; i < Count(match); i++) if (deliveries[i] > 0) { delivered = i; break; }
                    Assert.That(delivered, Is.GreaterThanOrEqualTo(0), "At least one AI must genuinely hitch and complete the two-second stopped delivery." + diagnostics);
                    Assert.That(attached[delivered], Is.True, diagnostics);
                    Assert.That((int)Property(couplers.GetValue(delivered), "SuccessfulHitches"), Is.GreaterThan(0), diagnostics);
                    Assert.That(travel.Max(), Is.GreaterThan(60), "Delivery requires actual cargo transport." + diagnostics);
                    Assert.That(scores[delivered], Is.GreaterThan(0), diagnostics);
                    Assert.That((bool)Call(match, "ResultEligible", delivered), Is.True, diagnostics);
                }
                else if (className == "TrebuchetMatch")
                {
                    Assert.That(loaded, Is.True, "The native load must first enter the real loading cradle." + diagnostics);
                    Assert.That(released, Is.True, "The sling must physically release." + diagnostics);
                    Assert.That(flightSpeed, Is.GreaterThan(5), diagnostics);
                    Assert.That(flightRise, Is.GreaterThan(2), "A trailer merely dropping from the cradle is not a launch." + diagnostics);
                    Assert.That(probes[0].impacts, Is.GreaterThan(0), "The released trailer must physically collide during the throw." + diagnostics);
                    Assert.That(probes[0].maximumImpactSpeed, Is.GreaterThan(2), diagnostics);
                    Assert.That(((int[])Read(match, "attempts"))[0], Is.EqualTo(1), diagnostics);
                    Assert.That(((float[])Read(match, "longestThrow"))[0], Is.GreaterThan(10), diagnostics);
                    Assert.That(scores[0], Is.GreaterThan(0), "Default toss score requires a settled physical landing, not the watchdog." + diagnostics);
                }
                else if (className == "HandoffMatch")
                {
                    var finished = (bool[])Read(match, "finishedTeam"); int team = Array.FindIndex(finished, value => value);
                    Assert.That(team, Is.GreaterThanOrEqualTo(0), "At least one team must finish the entire relay." + diagnostics);
                    Assert.That(((int[])Read(match, "leg"))[team], Is.EqualTo(1), "The default relay requires one physical handoff between two actual legs." + diagnostics);
                    Assert.That(attached[team] && attached[team + 2], Is.True, "Both actual teammate tractors must possess the same baton trailer during the round." + diagnostics);
                    Assert.That((int)Property(couplers.GetValue(team + 2), "SuccessfulHitches"), Is.GreaterThan(0), diagnostics);
                    Assert.That(travel[team], Is.GreaterThan(200), "The same physical trailer must reach the far finish." + diagnostics);
                    Assert.That(scores[team], Is.GreaterThan(0), diagnostics);
                    Assert.That(scores[team + 2], Is.EqualTo(scores[team]).Within(.01), "Both participating teammates share the actual team result." + diagnostics);
                    Assert.That((bool)Call(match, "ResultEligible", team + 2), Is.True, diagnostics);
                }
                else
                {
                    Assert.That(attached[0], Is.False, "Unhitched has no invisible native coupling between rider and deck." + diagnostics);
                    Assert.That(((float[])Read(match, "supportedRideSeconds"))[0], Is.GreaterThanOrEqualTo(3), "Actual wheel/deck support must sustain the ride." + diagnostics);
                    Assert.That(travel[0], Is.GreaterThan(8), "The independently physical trailer must actually travel." + diagnostics);
                    Assert.That(((bool[])Read(match, "Completed"))[0] || (float)Property(match, "Elapsed") >= (float)Read(match, "matchSeconds"), Is.True, "A genuine supported ride ends through the normal dismount or authored round limit." + diagnostics);
                    Assert.That(scores[0], Is.GreaterThan(0), diagnostics);
                    Assert.That((bool)Call(match, "ResultEligible", 0), Is.True, diagnostics);
                }
                string matchId = (string)Property(match, "MatchId");
                Call(match, "SetupScreen"); yield return null;
                Assert.That((bool)Call(match, "StartMatch"), Is.True, "The same authored scene must support a normal rematch.");
                Time.timeScale = PlaybackRate;
                yield return new WaitForSeconds(.8f);
                Assert.That((string)Property(match, "MatchId"), Is.Not.EqualTo(matchId));
                Assert.That((float)Property(match, "Elapsed"), Is.LessThan(3), "Only the ordinary countdown has elapsed.");
                Assert.That(((double[])Read(match, "Scores")).Take(Count(match)).All(value => value == 0), Is.True, "No score may leak into the rematch.");
                Assert.That(((bool[])Read(match, "Completed")).Take(Count(match)).Any(value => value), Is.False, "Every entered participant receives a fresh attempt.");
                Assert.That(((int[])Read(match, "Deliveries")).Take(Count(match)).All(value => value == 0), Is.True);
                if (className == "HighwireMatch") Assert.That(((int[])Read(match, "gate"))[0], Is.EqualTo(0));
                if (className == "HitchRunMatch") Assert.That(((int[])Read(match, "successfulSteals")).All(value => value == 0), Is.True);
                if (className == "TrebuchetMatch")
                {
                    Assert.That(((int[])Read(match, "attempts"))[0], Is.EqualTo(0));
                    Assert.That((bool)Property(((Array)Read(match, "engines")).GetValue(0), "Released"), Is.False);
                }
                if (className == "HandoffMatch")
                { Assert.That(((int[])Read(match, "leg")).All(value => value == 0), Is.True); Assert.That(((bool[])Read(match, "finishedTeam")).Any(value => value), Is.False); }
                if (className == "UnhitchedMatch") Assert.That(((float[])Read(match, "supportedRideSeconds"))[0], Is.EqualTo(0));
                Debug.Log("ALPHA10_NATURAL_CORE_OBJECTIVE_PASS " + scene + diagnostics + " rematch=fresh");
            }
            finally
            {
                Time.timeScale = 1;
                if (match is UnityEngine.Object live && live != null) Call(match, "SetupScreen");
            }
        }
    }
    static string Diagnostics(object match, string className, float[] travel)
    {
        var couplers = (Array)Read(match, "couplers"); var trailers = (Array)Read(match, "trailers"); var trucks = (Array)Read(match, "trucks");
        string data = " age=" + ((float)Property(match, "Elapsed")).ToString("F2") + " scores=" + string.Join(",", ((double[])Read(match, "Scores")).Take(Count(match)).Select(value => value.ToString("F1"))) + " travel=" + string.Join(",", travel.Select(value => value.ToString("F1")));
        for (int i = 0; i < Count(match); i++)
        {
            int t = className == "HandoffMatch" ? i % 2 : Mathf.Min(i, trailers.Length - 1);
            data += " [seat=" + i + " truck=" + Position(trucks.GetValue(i)).ToString("F1") + " trailer=" + TrailerBody(trailers.GetValue(t)).position.ToString("F1") + " " + Call(couplers.GetValue(i), "DiagnosticState", trailers.GetValue(t)) + "]";
        }
        if (className == "HighwireMatch") data += " gates=" + string.Join(",", ((int[])Read(match, "gate"))) + " failed=" + string.Join(",", ((bool[])Read(match, "failed")));
        else if (className == "HitchRunMatch") data += " deliveries=" + string.Join(",", ((int[])Read(match, "Deliveries")).Take(Count(match))) + " approach=" + string.Join(",", ((int[])Read(match, "approachStage")));
        else if (className == "HandoffMatch") data += " legs=" + string.Join(",", ((int[])Read(match, "leg"))) + " released=" + string.Join(",", ((bool[])Read(match, "released"))) + " finished=" + string.Join(",", ((bool[])Read(match, "finishedTeam")));
        else if (className == "TrebuchetMatch")
        {
            object engine = ((Array)Read(match, "engines")).GetValue(0);
            var cradle = (BoxCollider)Read(engine, "LoadingCradle");
            data += " loaded=" + (Property(engine, "Load") != null) + " swinging=" + Property(engine, "Swinging") + " released=" + Property(engine, "Released") + " cradle=" + cradle.bounds.center.ToString("F1") + " attempts=" + ((int[])Read(match, "attempts"))[0] + " distance=" + ((float[])Read(match, "longestThrow"))[0].ToString("F1");
        }
        else data += " supported=" + ((float[])Read(match, "supportedRideSeconds"))[0].ToString("F2") + " landings=" + ((float[])Read(match, "combo"))[0];
        return data;
    }
}

public sealed class HitchNaturalImpactProbe : MonoBehaviour
{
    public bool armed;
    public int impacts;
    public float maximumImpactSpeed;
    void OnCollisionEnter(Collision collision)
    { if (armed) { impacts++; maximumImpactSpeed = Mathf.Max(maximumImpactSpeed, collision.relativeVelocity.magnitude); } }
}
#endif

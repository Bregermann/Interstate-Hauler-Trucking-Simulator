using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace LWS.TruckKickball.Tests
{
    public sealed class KickballSceneTests
    {
        Component match;
        Type matchType;
        IDisposable preferences;
        GameObject isolation;
        const BindingFlags AnyInstance = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        KickballRules Rules => (KickballRules)matchType.GetField("rules").GetValue(match);
        object Get(string name) => matchType.GetField(name)?.GetValue(match) ?? matchType.GetProperty(name).GetValue(match);
        object Call(string name, params object[] args) => matchType.GetMethod(name, AnyInstance).Invoke(match, args);
        object Agent(int slot) => ((Array)Get("agents")).GetValue(slot);
        object Truck(int slot) => Agent(slot).GetType().GetField("truck").GetValue(Agent(slot));
        Rigidbody Body(int slot) => (Rigidbody)Truck(slot).GetType().GetField("body").GetValue(Truck(slot));

        [UnitySetUp]
        public IEnumerator Load()
        {
            var prefs = Type.GetType("LWS.TruckSports.SportsPrefs, Assembly-CSharp", true);
            preferences = (IDisposable)prefs.GetMethod("IsolateValidation", BindingFlags.Public | BindingFlags.Static).Invoke(null, new object[] { "KickballPlayModeTests" });
            isolation = new GameObject("Owned Kickball PlayMode input isolation"); UnityEngine.Object.DontDestroyOnLoad(isolation);
            isolation.AddComponent(Type.GetType("LWS.TruckSports.SportsProbeInputIsolation, Assembly-CSharp", true));
#if UNITY_EDITOR
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode("Assets/TruckKickball/Scenes/TruckKickball.unity", new LoadSceneParameters(LoadSceneMode.Single));
#else
            yield return SceneManager.LoadSceneAsync("TruckKickball");
#endif
            yield return null; yield return null;
            matchType = Type.GetType("LWS.TruckKickball.KickballMatch, Assembly-CSharp", true);
            foreach (var component in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude))
                if (component.GetType() == matchType) { match = component; break; }
            Assert.That(match, Is.Not.Null, "The actual saved sport scene must contain its authority.");
            var catalog = Get("weatherPresets");
            foreach (var entry in (Array)catalog.GetType().GetField("entries").GetValue(catalog))
                if ((string)entry.GetType().GetField("displayName").GetValue(entry) == "Clear")
                { catalog.GetType().GetMethod("Select").Invoke(catalog, new[] { entry.GetType().GetField("id").GetValue(entry) }); break; }
            Call("AllBots"); Time.timeScale = 1;
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            Time.timeScale = 1;
            if (match != null) Call("SetupScreen");
#if UNITY_EDITOR
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode("Assets/TruckSports/Scenes/TruckSports.unity", new LoadSceneParameters(LoadSceneMode.Single));
#else
            yield return SceneManager.LoadSceneAsync("TruckSports");
#endif
            yield return null; yield return null;
            if (isolation != null) UnityEngine.Object.Destroy(isolation);
            yield return null; preferences?.Dispose(); preferences = null;
        }

        [UnityTest]
        public IEnumerator SavedSceneSupportsAllFourRealTeamSizes()
        {
            Assert.That(((Array)Get("agents")).Length, Is.EqualTo(12));
            for (int size = 3; size <= 6; size++)
            {
                matchType.GetField("option").SetValue(match, size - 3);
                Call("NormalizeSetup"); Call("AllBots"); Assert.That((bool)Call("StartMatch"));
                yield return null;
                Assert.That(Rules.Count, Is.EqualTo(size * 2));
                int active = 0;
                foreach (Component agent in (Array)Get("agents")) if (agent.gameObject.activeSelf) active++;
                Assert.That(active, Is.EqualTo(size * 2)); Call("SetupScreen");
            }
            Assert.That(UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude).Length, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator ActualNwhAndPhysxProducePitchKickAndBaseProgress()
        {
            matchType.GetField("option").SetValue(match, 3); Call("NormalizeSetup"); Call("AllBots");
            Assert.That((bool)Call("StartMatch")); Time.timeScale = 8;
            float deadline = Time.realtimeSinceStartup + 90;
            while (Time.realtimeSinceStartup < deadline && (Rules.CompletedPlays < 3 || (int)Get("PhysicalBaseTouches") == 0)) yield return null;
            Assert.That(Rules.Pitches, Is.GreaterThanOrEqualTo(3));
            Assert.That((int)Get("PhysicalKicks"), Is.GreaterThan(0));
            Assert.That((int)Get("PhysicalCatches"), Is.GreaterThan(0));
            Assert.That((int)Get("PhysicalBaseTouches"), Is.GreaterThan(0));
        }

        [UnityTest]
        public IEnumerator PauseDuringActualFlightPreservesBodiesAndClock()
        {
            matchType.GetField("option").SetValue(match, 3); Call("NormalizeSetup"); Call("AllBots"); Call("StartMatch");
            Time.timeScale = 8; float deadline = Time.realtimeSinceStartup + 50;
            while (Time.realtimeSinceStartup < deadline && (Rules.Phase != KickballPhase.Live || (int)Get("Carrier") >= 0)) yield return null;
            Assert.That(Rules.Phase, Is.EqualTo(KickballPhase.Live)); Assert.That((int)Get("Carrier"), Is.EqualTo(-1));
            Time.timeScale = 1; Call("Pause", 0, false);
            Vector3 position = Body(0).position; float clock = (float)Get("Clock");
            var ball = matchType.GetField("ball").GetValue(match); var body = (Rigidbody)ball.GetType().GetField("body").GetValue(ball); var ballPosition = body.position;
            yield return new WaitForSecondsRealtime(.2f);
            Assert.That(Body(0).position, Is.EqualTo(position)); Assert.That(body.position, Is.EqualTo(ballPosition)); Assert.That((float)Get("Clock"), Is.EqualTo(clock));
            Call("Resume"); yield return new WaitForSeconds(.1f); Assert.That((float)Get("Clock"), Is.GreaterThan(clock));
        }

        [UnityTest]
        public IEnumerator InitializedBaseRunnerDoesNotStealDuringRealPitch()
        {
            matchType.GetField("option").SetValue(match, 3); Call("NormalizeSetup"); Call("AllBots"); Call("StartMatch");
            // Explicit initialized fixture, separate from full physical matches:
            // place a legal runner on first, then roll an actual next pitch.
            Rules.Pitch(); Rules.Kick(); int runner = Rules.Batter;
            Rules.TouchBase(runner, 1); Rules.FinishPlay();
            var truck = Truck(runner);
            truck.GetType().GetMethod("Prepare").Invoke(truck, new object[] { new Vector3(27, .7f, 27), Quaternion.LookRotation(new Vector3(-1, 0, 1)), true });
            Call("PreparePlate", false); yield return new WaitForSeconds(1.4f);
            Call("RollPitch", Vector2.zero); yield return new WaitForSeconds(.9f);
            var position = Body(runner).position; position.y = 0;
            Assert.That(Vector3.Distance(position, new Vector3(27, 0, 27)), Is.LessThan(1));
            var velocity = Body(runner).linearVelocity; velocity.y = 0;
            Assert.That(velocity.magnitude, Is.LessThan(.5f), "An unclaimed pitch is not permission to advance a baserunner.");
        }
    }
}

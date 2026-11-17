#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Text;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
public sealed class HorrorPhysicalPlayModeTests
{
    static Type MatchType => Type.GetType("LWS.TruckHorrorSports.HorrorMatch, Assembly-CSharp", true);
    static object Field(object target, string name) => target.GetType().GetField(name).GetValue(target);
    static object Property(object target, string name) => target.GetType().GetProperty(name).GetValue(target);
    static object Call(object target, string method, params object[] args) => target.GetType().GetMethod(method).Invoke(target, args);
    [UnityTest]
    public IEnumerator NonFootVenuesAdvanceActualPhysicsWithoutInteriorRigs()
    {
        var prefs = Type.GetType("LWS.TruckSports.SportsPrefs, Assembly-CSharp", true);
        using ((IDisposable)prefs.GetMethod("IsolateValidation").Invoke(null, new object[] { "Alpha10HorrorOptionalFootRig" }))
        {
            foreach (string name in new[] { "ScarecrowSeason", "DeadEnd", "CursedCargo" })
            {
                EditorSceneManager.LoadSceneInPlayMode("Assets/TruckHorrorSports/" + name + "/Scenes/" + name + ".unity", new LoadSceneParameters(LoadSceneMode.Single));
                yield return null; yield return null;
                var match = UnityEngine.Object.FindObjectsByType(MatchType, FindObjectsSortMode.None).Single();
                var optionalRigs = Field(match, "footAgents") as Array;
                Assert.That(optionalRigs == null || optionalRigs.Length == 0, Is.True, name + " deliberately has no interior-control positions.");
                Call(match, "AllBots"); Assert.That((bool)Call(match, "StartMatch"), Is.True);
                for (int frame = 0; frame < 30; frame++) yield return new WaitForFixedUpdate();
                Assert.That((float)Property(match, "Age"), Is.GreaterThan(.2f), name + " must progress through its normal simulation.");
                // Scene OnEnable may briefly overlap the host development HUD before the existing
                // BilliardsHostIsolation.Start suppresses it. Check the settled input authority;
                // Unity Test Framework still fails unexpected errors/exceptions normally.
                var eventSystemType = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("UnityEngine.EventSystems.EventSystem", false)).First(t => t != null);
                var eventSystems = UnityEngine.Object.FindObjectsByType(eventSystemType, FindObjectsInactive.Include, FindObjectsSortMode.None).Cast<Behaviour>().ToArray();
                int activeSystems = eventSystems.Count(e => e.isActiveAndEnabled);
                string systems = string.Join(" | ", eventSystems.Select(e => e.name + " scene=" + e.gameObject.scene.path + " active=" + e.isActiveAndEnabled));
                Assert.That(activeSystems, Is.EqualTo(1), name + " must have one settled EventSystem after 30 physics frames. " + systems);
                Call(match, "SetupScreen"); yield return null;
            }
        }
    }
    [UnityTest]
    public IEnumerator AiDriversActuallyMoveAndRematchClearsObjectiveReceipts()
    {
        var prefs = Type.GetType("LWS.TruckSports.SportsPrefs, Assembly-CSharp", true);
        using ((IDisposable)prefs.GetMethod("IsolateValidation").Invoke(null, new object[] { "Alpha10HorrorPhysics" }))
        {
            foreach (string name in new[] { "GraveyardShift", "ScarecrowSeason", "DeadEnd", "CursedCargo" })
            {
                EditorSceneManager.LoadSceneInPlayMode("Assets/TruckHorrorSports/" + name + "/Scenes/" + name + ".unity", new LoadSceneParameters(LoadSceneMode.Single));
                yield return null; yield return null;
                var match = UnityEngine.Object.FindObjectsByType(MatchType, FindObjectsSortMode.None).Single();
                Call(match, "AllBots"); Assert.That((bool)Call(match, "StartMatch"), Is.True);
                var trucks = (Array)Field(match, "trucks"); var first = trucks.GetValue(0); var body = (Rigidbody)Field(first, "body"); var start = body.position;
                yield return new WaitForSeconds(8);
                Assert.That(Vector3.Distance(start, body.position), Is.GreaterThan(3), name + ": AI must physically drive rather than receive invented scores.");
                Assert.That(float.IsFinite(body.position.x + body.position.y + body.position.z), Is.True);
                if (name == "CursedCargo")
                {
                    var cargo = ((Array)Field(match, "cargo")).GetValue(0); var coupler = Field(cargo, "coupler");
                    Assert.That(Property(coupler, "Attached"), Is.Not.Null, "The haunted load must remain joined by native NWH fifth wheel.");
                }
                Call(match, "SetupScreen"); yield return null;
                Assert.That((bool)Call(match, "StartMatch"), Is.True); yield return null;
                var rules = Field(match, "rules"); Assert.That((int)Property(rules, "TotalTasks"), Is.Zero);
                Assert.That((float)Property(match, "Age"), Is.LessThan(1)); Call(match, "SetupScreen");
            }
        }
    }
    [UnityTest, Timeout(300000)]
    public IEnumerator GraveyardShiftNaturalAiRoundProducesRealObjectiveResults()
    {
        var prefs = Type.GetType("LWS.TruckSports.SportsPrefs, Assembly-CSharp", true);
        using ((IDisposable)prefs.GetMethod("IsolateValidation").Invoke(null, new object[] { "Alpha10HorrorNaturalRound" }))
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/TruckHorrorSports/GraveyardShift/Scenes/GraveyardShift.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null; yield return null;
            var match = UnityEngine.Object.FindObjectsByType(MatchType, FindObjectsSortMode.None).Single(); Call(match, "AllBots"); Assert.That((bool)Call(match, "StartMatch"), Is.True);
            float deadline = Time.realtimeSinceStartup + 270;
            while (!(bool)Property(match, "Finished") && Time.realtimeSinceStartup < deadline) yield return null;
            string diagnostics = NightDiagnostics(match);
            Assert.That((bool)Property(match, "Finished"), Is.True, "A normal night must terminate. " + diagnostics);
            Assert.That((int)Property(Field(match, "rules"), "TotalTasks"), Is.GreaterThan(0), "AI must perform real contextual tasks. " + diagnostics);
            Assert.That((string)Property(match, "Results"), Is.Not.Empty, diagnostics); Call(match, "SetupScreen");
        }
    }
    [UnityTest, Timeout(150000)]
    public IEnumerator ScarecrowSeasonNaturalDefaultEscapesAndRematches()
    { return NaturalDefaultNight("ScarecrowSeason", false); }
    [UnityTest, Timeout(150000)]
    public IEnumerator DeadEndNaturalDefaultEscapesAndRematches()
    { return NaturalDefaultNight("DeadEnd", false); }
    [UnityTest, Timeout(150000)]
    public IEnumerator CursedCargoNaturalDefaultDeliversAndRematches()
    { return NaturalDefaultNight("CursedCargo", true); }
    static IEnumerator NaturalDefaultNight(string name, bool requirePhysicalDelivery)
    {
        var prefs = Type.GetType("LWS.TruckSports.SportsPrefs, Assembly-CSharp", true);
        float previousScale = Time.timeScale;
        UnityEngine.Object match = null;
        using ((IDisposable)prefs.GetMethod("IsolateValidation").Invoke(null, new object[] { "Alpha10HorrorNaturalDefault" + name }))
        {
            try
            {
                EditorSceneManager.LoadSceneInPlayMode("Assets/TruckHorrorSports/" + name + "/Scenes/" + name + ".unity", new LoadSceneParameters(LoadSceneMode.Single));
                yield return null; yield return null;
                match = UnityEngine.Object.FindObjectsByType(MatchType, FindObjectsSortMode.None).Single();
                // Select the normal advertised mode. Duration, authored objectives and physics are unchanged.
                match.GetType().GetField("option").SetValue(match, 0);
                Call(match, "AllBots"); Assert.That((bool)Call(match, "StartMatch"), Is.True);
                float wallStart = Time.realtimeSinceStartup, deadline = wallStart + 120;
                var deliverySeen = new bool[12]; var delivered = new bool[12]; var deliveryEvidence = new string[12];
                Time.timeScale = 3; // Real fixed-step simulation, accelerated for bounded AI acceptance.
                while (!(bool)Property(match, "Finished") && Time.realtimeSinceStartup < deadline)
                { if (requirePhysicalDelivery) ObserveDeliveries(match, deliverySeen, delivered, deliveryEvidence); yield return null; }
                if (requirePhysicalDelivery) ObserveDeliveries(match, deliverySeen, delivered, deliveryEvidence);
                Time.timeScale = 1;
                string diagnostics = name + " wall=" + (Time.realtimeSinceStartup - wallStart).ToString("F1") + "s at 3x · " + NightDiagnostics(match);
                Assert.That((bool)Property(match, "Finished"), Is.True, "The default match must resolve through its normal rules. " + diagnostics);
                var rules = Field(match, "rules"); int totalTasks = (int)Property(rules, "TotalTasks");
                Assert.That(totalTasks, Is.GreaterThan(0), "A zero-objective timeout is not acceptance. " + diagnostics);
                int count = (int)Field(Field(match, "setup"), "count"); int expectedObjectives = ((Array)Field(match, "objectives")).Length;
                int escaped = 0;
                for (int i = 0; i < count; i++)
                {
                    if (!(bool)Call(match, "Escaped", i)) continue;
                    escaped++;
                    Assert.That((int)Call(match, "Objectives", i), Is.GreaterThanOrEqualTo(expectedObjectives), "An escape must follow the actual authored objective route. " + diagnostics);
                    if (!requirePhysicalDelivery) continue;
                    Assert.That(deliverySeen[i] && delivered[i], Is.True, "The original intact trailer must be physically received while still attached. " + deliveryEvidence[i] + " · " + diagnostics);
                }
                Assert.That(escaped, Is.GreaterThan(0), "The default escape/delivery objective must be reachable; timer expiry alone does not pass. " + diagnostics);
                Assert.That((string)Property(match, "Results"), Is.Not.Empty, diagnostics);
                Assert.That((bool)Property(match, "FinalMetricsRecorded"), Is.True, diagnostics);
                Call(match, "SetupScreen"); yield return null;
                Assert.That((bool)Call(match, "StartMatch"), Is.True, "Normal rematch must start. " + diagnostics);
                yield return new WaitForFixedUpdate();
                Assert.That((int)Property(Field(match, "rules"), "TotalTasks"), Is.Zero, "Rematch must clear objective receipts. " + NightDiagnostics(match));
                Assert.That((float)Property(match, "Age"), Is.LessThan(1), "Rematch must restart the actual match clock.");
                Assert.That((bool)Property(match, "FinalMetricsRecorded"), Is.False, "The next match must have a fresh final-metrics receipt.");
                for (int i = 0; i < count; i++) Assert.That((bool)Call(match, "Escaped", i), Is.False, "Prior outcomes must not leak into a rematch.");
                Debug.Log("ALPHA10_HORROR_NATURAL_PASS " + diagnostics + " escaped=" + escaped + " rematch=fresh");
                Call(match, "SetupScreen");
            }
            finally
            {
                try { if (match != null && (bool)Property(match, "Running")) Call(match, "SetupScreen"); }
                finally { Time.timeScale = previousScale; }
            }
        }
    }
    static void ObserveDeliveries(object match, bool[] seen, bool[] delivered, string[] evidence)
    {
        int count = (int)Field(Field(match, "setup"), "count"); var loads = (Array)Field(match, "cargo");
        for (int i = 0; i < count; i++) if (!seen[i] && (bool)Call(match, "Escaped", i))
        {
            seen[i] = true; var load = loads.GetValue(i); var body = (Rigidbody)Property(load, "Body");
            float condition = (float)Property(load, "Condition"), distance = Vector3.Distance(body.position, ((Transform)Field(match, "exit")).position);
            bool coupled = ReferenceEquals(Property(Field(load, "coupler"), "Attached"), Field(load, "trailer"));
            delivered[i] = condition > 0 && coupled && distance < 18;
            evidence[i] = "P" + (i + 1) + " at receipt clock=" + Property(match, "Age") + " load=" + body.position.ToString("F1") + " receiverDistance=" + distance.ToString("F1") + " condition=" + condition.ToString("F2") + " originalHitch=" + coupled;
        }
    }
    static string NightDiagnostics(object match)
    {
        var rules = Field(match, "rules"); var trucks = (Array)Field(match, "trucks");
        var tasks = (int[])Field(rules, "tasks"); var outcome = (Array)Field(rules, "outcome");
        int count = (int)Field(Field(match, "setup"), "count");
        var text = new StringBuilder("clock=").Append(((float)Property(match, "Age")).ToString("F1"))
            .Append(" totalTasks=").Append(Property(rules, "TotalTasks"))
            .Append(" paused=").Append(Property(match, "Paused")).Append(" timeScale=").Append(Time.timeScale.ToString("F2"));
        for (int i = 0; i < Math.Min(count, trucks.Length); i++)
        {
            var body = (Rigidbody)Field(trucks.GetValue(i), "body");
            text.Append(" | P").Append(i + 1).Append(" tasks=").Append(tasks[i]).Append(" outcome=").Append(outcome.GetValue(i))
                .Append(" position=").Append(body.position.ToString("F1"));
            var loads = Field(match, "cargo") as Array;
            if (loads != null && i < loads.Length && loads.GetValue(i) != null)
            { var load = loads.GetValue(i); text.Append(" load=").Append(((Rigidbody)Property(load, "Body")).position.ToString("F1")).Append(" condition=").Append(Property(load, "Condition")).Append(" coupled=").Append(Property(Field(load, "coupler"), "Attached") != null); }
        }
        return text.ToString();
    }
}
#endif

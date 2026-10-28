using System;
using System.Linq;
using NUnit.Framework;

namespace LWS.TruckTornadoRodeo.Tests
{
    public sealed class TornadoStormPolicyTests
    {
        Type type; object policy;
        object Call(string name, params object[] args) => type.GetMethod(name).Invoke(policy, args);
        object Static(string name, params object[] args) => type.GetMethod(name).Invoke(null, args);
        string Decision(string id = "matchA", bool running = true, bool finished = false, bool paused = false, bool bound = true, bool configured = true, string selection = "random") => Call("Decide", id, running, finished, paused, bound, configured, selection, "random").ToString();
        [SetUp] public void Reset() { type = Type.GetType("LWS.TruckTornadoRodeo.TornadoStormPolicy, Assembly-CSharp", true); policy = Activator.CreateInstance(type); }
        [Test] public void NoDecisionBeforeRealMatchOrAfterFinish() { Assert.That(Decision(running: false), Is.EqualTo("WaitForMatch")); Assert.That(Decision(id: null), Is.EqualTo("WaitForMatch")); Assert.That(Decision(finished: true), Is.EqualTo("WaitForMatch")); }
        [Test] public void MissingProviderBindingIsPendingAndDoesNotConsumeToken() { Assert.That(Decision(bound: false), Is.EqualTo("PendingBindings")); Assert.That(Decision(bound: true), Is.EqualTo("ChooseSevere")); }
        [Test] public void SessionForecastMustFinishBeforeModeRandomOverride() { Assert.That(Decision(configured: false), Is.EqualTo("PendingBaseForecast")); Assert.That(Decision(configured: true), Is.EqualTo("ChooseSevere")); }
        [Test] public void FirstPausedFrameWaitsUntilResumeWithoutReroll() { Assert.That(Decision(paused: true), Is.EqualTo("PendingResume")); Assert.That(Decision(), Is.EqualTo("ChooseSevere")); Call("Confirm", "matchA"); Assert.That(Decision(paused: true), Is.EqualTo("AlreadyHandled")); Assert.That(Decision(), Is.EqualTo("AlreadyHandled")); }
        [Test] public void EveryExplicitChoiceIncludingClearAndSnowRetainsBaseForecast() { foreach (string id in new[] { "actual-clear-guid", "actual-snow-guid", "actual-hurricane-guid" }) Assert.That(Decision(selection: id), Is.EqualTo("KeepExplicit")); Assert.That(Decision(selection: "actual-clear-guid", configured: false), Is.EqualTo("PendingBaseForecast")); }
        [Test] public void SameMatchIdCannotReapplyButNewMatchGetsOneDecision() { Call("Confirm", "matchA"); Assert.That(Decision(bound: false, configured: false), Is.EqualTo("AlreadyHandled")); Assert.That(Decision(id: "matchB"), Is.EqualTo("ChooseSevere")); Call("Confirm", "matchB"); Assert.That(Decision(id: "matchB"), Is.EqualTo("AlreadyHandled")); }
        [Test] public void SeedAndChoiceAreStableAcrossCatalogOrderingWithoutMutation()
        {
            string[] ids = { "e", "b", "d", "a", "c" }; var before = ids.ToArray(); var a = (string)Static("Choose", "stable-token", ids); var b = (string)Static("Choose", "stable-token", ids.Reverse().ToArray());
            Assert.That(a, Is.EqualTo(b)); Assert.That(ids, Is.EqualTo(before)); Assert.That((int)Static("Seed", "stable-token"), Is.EqualTo((int)Static("Seed", "stable-token")));
        }
        [Test] public void ActualProfileDescriptorsQualifyAndFakeClearLabelDoesNot()
        {
            Assert.That((bool)Static("Suitable", "WeatherMakerProfile_Storm", false, true, false, true, false, 0f), Is.True);
            Assert.That((bool)Static("Suitable", "Hurricane weather", false, true, false, true, true, 90f), Is.True);
            Assert.That((bool)Static("Suitable", "WeatherMakerProfile_Blizzard", false, false, true, false, false, 0f), Is.True);
            Assert.That((bool)Static("Suitable", "WeatherMakerProfile_HeavyRain", false, true, false, false, false, 0f), Is.True);
            Assert.That((bool)Static("Suitable", "WeatherMakerProfile_HeavySnow", false, false, true, false, false, 0f), Is.True);
            Assert.That((bool)Static("Suitable", "WeatherMakerProfile_Clear", false, false, false, false, false, 0f), Is.False, "A renamed catalog display label cannot qualify an actual Clear profile.");
            Assert.That((bool)Static("Suitable", "WeatherMakerProfile_HeavyRain", true, true, false, false, false, 0f), Is.False);
            Assert.That((bool)Static("Suitable", "Hurricane weather", false, true, false, true, true, 20f), Is.False);
        }
        [Test] public void EmptyOrMissingActualCandidatesNeverInventStormFallback() { Assert.That(Static("Choose", "token", Array.Empty<string>()), Is.Null); Assert.That(Static("Choose", "token", new string[] { null, "" }), Is.Null); }
        [Test] public void EqualDiscreteSelectionIncludesEveryDistinctActualId()
        {
            var ids = new[] { "a", "b", "c", "d", "e" }; var counts = new int[5];
            for (int i = 0; i < 2000; i++) { string chosen = (string)Static("Choose", "match-" + i, ids); counts[Array.IndexOf(ids, chosen)]++; }
            Assert.That(counts.All(n => n > 200 && n < 600), Is.True, "Each actual ID receives the same unweighted discrete choice opportunity; no label or catalog-order weight is introduced.");
            Assert.That(Static("Choose", "token", new[] { "only", "only" }), Is.EqualTo("only"));
        }
    }
}

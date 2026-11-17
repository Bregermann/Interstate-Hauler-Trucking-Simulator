#if UNITY_EDITOR
using System;
using System.Collections;
using System.Text;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace LWS.TruckWaterSports.Tests
{
    // Software/AI acceptance only. This does not certify physical gamepads,
    // human playtesting, audio listening or Windows presentation.
    public sealed class WaterDefaultOutcomeTests
    {
        const float PlaybackSpeed = 4, WallSeconds = 110, RematchSeconds = 4;
        static Type Runtime(string name)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(name);
                if (type != null) return type;
            }
            throw new InvalidOperationException("Missing runtime type: " + name);
        }
        static object Field(object target, string name) => target.GetType().GetField(name).GetValue(target);
        static object Property(object target, string name) => target.GetType().GetProperty(name).GetValue(target);
        static object Call(object target, string name, params object[] arguments) => target.GetType().GetMethod(name).Invoke(target, arguments);
        static float Clock(object match) => (float)Property(match, "Clock");
        static object Vessel(object match, int slot) => Call(match, "Vessel", slot);
        static Rigidbody Body(object match, int slot) => (Rigidbody)Field(Vessel(match, slot), "body");

        static void SelectClearWeather()
        {
            var catalog = AssetDatabase.LoadAssetAtPath("Assets/TruckSports/Settings/SportsWeatherPresets.asset", Runtime("LWS.TruckSports.SportsWeatherPresets"));
            Assert.That(catalog, Is.Not.Null);
            foreach (var entry in (Array)Field(catalog, "entries"))
                if ((string)Field(entry, "displayName") == "Clear") { Call(catalog, "Select", (string)Field(entry, "id")); return; }
            Assert.Fail("The installed Clear selection is missing.");
        }

        static string Observed(object match, bool blackwater)
        {
            var text = new StringBuilder("clock=").Append(Clock(match));
            var gates = (int[])Field(match, "Gates"); var misses = (int[])Field(match, "MissedGates"); var finishes = (float[])Field(match, "FinishTimes");
            for (int slot = 0; slot < 4; slot++)
            {
                text.Append(" | slot=").Append(slot).Append(" gate=").Append(gates[slot]).Append(" misses=").Append(misses[slot])
                    .Append(" finish=").Append(finishes[slot]).Append(" position=").Append(Body(match, slot).position.ToString("F1"));
                if (blackwater) text.Append(" hull=").Append(((float[])Field(match, "Integrity"))[slot]);
                else text.Append(" strokes=").Append(Property(Vessel(match, slot), "Strokes"));
            }
            return text.ToString();
        }

        [UnityTest]
        public IEnumerator AuthoredWhitewaterRaceAICompletesAndRematchSimulates()
            => Run("WhitewaterWipeout", "WhitewaterWipeoutMatch", false);

        [UnityTest]
        public IEnumerator AuthoredBlackwaterCrossingAICompletesAndRematchSimulates()
            => Run("Blackwater", "BlackwaterMatch", true);

        static IEnumerator Run(string sceneName, string typeName, bool blackwater)
        {
            var lease = (IDisposable)Runtime("LWS.TruckSports.SportsPrefs").GetMethod("IsolateValidation")
                .Invoke(null, new object[] { "WaterDefault" + sceneName });
            float originalScale = Time.timeScale, fixedStep = Time.fixedDeltaTime;
            Component match = null;
            try
            {
                Time.timeScale = 1; SelectClearWeather();
                EditorSceneManager.LoadSceneInPlayMode("Assets/TruckWaterSports/Scenes/" + sceneName + ".unity", new LoadSceneParameters(LoadSceneMode.Single));
                yield return null; yield return null;
                var matches = UnityEngine.Object.FindObjectsByType(Runtime("LWS.TruckWaterSports." + typeName), FindObjectsSortMode.None);
                Assert.That(matches.Length, Is.EqualTo(1)); match = (Component)matches[0];
                match.GetType().GetField("option").SetValue(match, 0);
                var setup = Field(match, "setup"); setup.GetType().GetField("count").SetValue(setup, 4); Call(match, "AllBots");
                var menus = Field(match, "menus"); var course = Field(match, "course");
                int checkpoints = ((Vector3[])Field(course, "stations")).Length - 1;
                float authoredDuration = (float)Field(match, "matchDuration");
                Assert.That(checkpoints, Is.GreaterThan(3), "Use the complete saved course.");
                var spawns = new Vector3[4]; var initialSteps = new int[4];
                for (int slot = 0; slot < 4; slot++) { spawns[slot] = Body(match, slot).position; initialSteps[slot] = (int)Property(Field(Vessel(match, slot), "flotation"), "Steps"); }
                var entity = blackwater ? (Rigidbody)Field(match, "entity") : null;
                Vector3 entitySpawn = entity != null ? entity.position : Vector3.zero;
                var cargo = blackwater ? (Rigidbody[])Field(match, "supplies") : null;
                var cargoSpawns = new Vector3[4];
                if (blackwater) for (int i = 0; i < cargo.Length; i++) { cargoSpawns[i] = cargo[i].position; Assert.That(cargo[i].isKinematic, Is.True, "Setup must park crates while flotation is match-gated."); }

                Call(menus, "Action", "start");
                Assert.That((bool)Property(match, "Live"), Is.True);
                string firstMatch = (string)Property(match, "MatchId");
                bool wetPhysics = false, entityWaveUnderWetHull = false;
                float furthestMotion = 0, entityMotion = 0;
                Time.timeScale = PlaybackSpeed;
                float wallDeadline = Time.realtimeSinceStartup + WallSeconds;
                while (!(bool)Property(match, "Finished") && Time.realtimeSinceStartup < wallDeadline && Clock(match) < authoredDuration + 5)
                {
                    for (int slot = 0; slot < 4; slot++)
                    {
                        var vessel = Vessel(match, slot); var flotation = Field(vessel, "flotation");
                        bool wet = Convert.ToUInt32(Property(flotation, "WetTriangles")) > 0;
                        wetPhysics |= wet && (int)Property(flotation, "Steps") > initialSteps[slot];
                        furthestMotion = Mathf.Max(furthestMotion, Vector3.Distance(Body(match, slot).position, spawns[slot]));
                        if (blackwater && wet)
                        {
                            var baseSurface = (Vector3)Call(course, "SurfacePoint", Body(match, slot).position);
                            entityWaveUnderWetHull |= Mathf.Abs((float)Field(flotation, "waterHeight") - baseSurface.y) > .001f;
                        }
                    }
                    if (entity != null) entityMotion = Mathf.Max(entityMotion, Vector3.Distance(entity.position, entitySpawn));
                    yield return null;
                }
                Time.timeScale = 1;
                string observed = Observed(match, blackwater);
                Assert.That((bool)Property(match, "Finished"), Is.True, "Default round did not resolve naturally. " + observed);
                Assert.That(Property(menus, "Screen").ToString(), Is.EqualTo("Result"), observed);
                Assert.That((float)Field(match, "matchDuration"), Is.EqualTo(authoredDuration), "Never shorten the gameplay deadline for acceptance.");
                Assert.That(wetPhysics, Is.True, "Actual wet Archimeds physics must run. " + observed);
                Assert.That(furthestMotion, Is.GreaterThan(20), "The AI must physically traverse the course. " + observed);
                int finishers = 0, winners = 0;
                var finishes = (float[])Field(match, "FinishTimes"); var misses = (int[])Field(match, "MissedGates");
                for (int slot = 0; slot < 4; slot++)
                {
                    if ((bool)Call(match, "ResultHasOutcome", slot))
                    {
                        finishers++; Assert.That(finishes[slot], Is.GreaterThan(0), observed);
                        Assert.That(checkpoints - misses[slot], Is.GreaterThanOrEqualTo(2), "A genuine outcome must include actual gate hits. " + observed);
                    }
                    if ((bool)Call(match, "ResultWinner", slot)) winners++;
                    if (!blackwater) Assert.That((int)Property(Vessel(match, slot), "Strokes"), Is.GreaterThan(0), "Each AI kayak must really paddle.");
                }
                Assert.That(finishers, Is.GreaterThan(0), "All-DNF or all-destroyed timeout is not a successful crossing. " + observed);
                Assert.That(winners, Is.GreaterThan(0), observed);
                if (blackwater)
                {
                    Assert.That(entityMotion, Is.GreaterThan(5), "The physical entity must actually move.");
                    Assert.That(entityWaveUnderWetHull, Is.True, "Entity waves must reach the existing wet-hull flotation adapter, not remain decorative.");
                }
                TestContext.WriteLine("WATER_DEFAULT_COMPLETED software_ai_only=true playback=4x fixed_dt=" + fixedStep + " entity_motion=" + entityMotion + "; " + observed);

                // Exercise the ordinary result -> setup -> start flow. This is
                // deliberately a short rematch smoke check, not a second claimed completion.
                Call(menus, "Action", "setup"); Assert.That(Property(menus, "Screen").ToString(), Is.EqualTo("Setup"));
                Call(menus, "Action", "start"); Assert.That((bool)Property(match, "Live"), Is.True);
                Assert.That((string)Property(match, "MatchId"), Is.Not.EqualTo(firstMatch));
                Assert.That((int[])Field(match, "Gates"), Is.All.EqualTo(1));
                Assert.That((int[])Field(match, "MissedGates"), Is.All.EqualTo(0));
                Assert.That((float[])Field(match, "FinishTimes"), Is.All.EqualTo(-1));
                for (int slot = 0; slot < 4; slot++)
                {
                    Assert.That(Vector3.Distance(Body(match, slot).position, spawns[slot]), Is.LessThan(.02f));
                    Assert.That((int)Property(Vessel(match, slot), "Capsizes"), Is.Zero);
                    Assert.That((int)Property(Vessel(match, slot), "Strokes"), Is.Zero);
                    initialSteps[slot] = (int)Property(Field(Vessel(match, slot), "flotation"), "Steps");
                }
                if (blackwater)
                {
                    Assert.That((float[])Field(match, "Integrity"), Is.All.EqualTo(100));
                    Assert.That((int[])Field(match, "Deliveries"), Is.All.EqualTo(0));
                    Assert.That((int)Property(match, "BankedSupplies"), Is.Zero);
                    for (int i = 0; i < cargo.Length; i++) Assert.That(Vector3.Distance(cargo[i].position, cargoSpawns[i]), Is.LessThan(.02f), "Reset real cargo, not just the HUD.");
                }
                Time.timeScale = PlaybackSpeed; wallDeadline = Time.realtimeSinceStartup + 12;
                while (Clock(match) < RematchSeconds && !(bool)Property(match, "Finished") && Time.realtimeSinceStartup < wallDeadline) yield return null;
                Time.timeScale = 1;
                Assert.That(Clock(match), Is.GreaterThanOrEqualTo(RematchSeconds), "Rematch physics must actually advance.");
                Assert.That((bool)Property(match, "Live"), Is.True);
                for (int slot = 0; slot < 4; slot++)
                {
                    Assert.That((int)Property(Field(Vessel(match, slot), "flotation"), "Steps"), Is.GreaterThan(initialSteps[slot]));
                    Assert.That(Vector3.Distance(Body(match, slot).position, spawns[slot]), Is.GreaterThan(1), "AI must move again after reset.");
                }
                Assert.That(Time.fixedDeltaTime, Is.EqualTo(fixedStep));
                TestContext.WriteLine("WATER_DEFAULT_REMATCH_SMOKE seconds=" + Clock(match) + " software_ai_only=true; " + Observed(match, blackwater));
            }
            finally
            {
                if (match != null) Call(match, "SetupScreen");
                Time.timeScale = originalScale; lease.Dispose();
            }
        }
    }
}
#endif

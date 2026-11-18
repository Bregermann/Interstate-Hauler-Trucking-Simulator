#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class HandoffVariantsPlayModeTests
{
    static object Field(object value, string name) => value.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetValue(value);
    static object Property(object value, string name) => value.GetType().GetProperty(name).GetValue(value);
    static object Call(object value, string name, params object[] args) => value.GetType().GetMethod(name).Invoke(value, args);
    static Type Runtime => Type.GetType("LWS.TruckHitchSports.HandoffMatch, Assembly-CSharp", true);
    static IDisposable Preferences() => (IDisposable)Type.GetType("LWS.TruckSports.SportsPrefs, Assembly-CSharp", true).GetMethod("IsolateValidation").Invoke(null, new object[] { "Alpha10HandoffVariants" });

    [UnityTest] public IEnumerator ExtendedRelayPhysicallyTransfersOriginalBatonThroughFourLegsAndRematches() => Natural(3);
    [UnityTest] public IEnumerator ReverseRelayActuallyBacksTheLoadIntoItsBayThenFinishesAndRematches() => Natural(2);
    [UnityTest] public IEnumerator ObstacleRelayTraversesSavedPhysicalChicaneHandsOffOriginalBatonAndRematches() => Natural(1);

    IEnumerator Natural(int mode)
    {
        using (Preferences())
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/TruckHitchSports/Scenes/HitchHandoff.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null; yield return null;
            var match = UnityEngine.Object.FindObjectsByType(Runtime, FindObjectsSortMode.None).Single();
            Assert.That(((Component)match).transform.Find("Authored Handoff extended and technical stations"), Is.Not.Null, "Apply the explicit saved-scene route/station upgrade first.");
            match.GetType().GetField("option").SetValue(match, mode);
            var setup = Field(match, "setup"); setup.GetType().GetField("count").SetValue(setup, 4);
            Call(match, "NormalizeSetup"); Call(match, "AllBots");
            var weather = Field(match, "presentationWeather");
            if (weather != null && Field(weather, "presets") != null) Call(Field(weather, "presets"), "Select", "9aad5ff5dfa09ce468205f676714f891");
            var trailers = (Array)Field(match, "trailers"); var couplers = (Array)Field(match, "couplers");
            var bodies = Enumerable.Range(0, 2).Select(i => (Rigidbody)Field(trailers.GetValue(i), "Body")).ToArray();
            var identity = bodies.Select(b => b.GetInstanceID()).ToArray(); var starts = bodies.Select(b => b.position).ToArray();
            var owners = new[] { new List<int>(), new List<int>() };
            var travel = new float[2]; var backed = new bool[2]; var reverseEntryObserved = new bool[2];
            Assert.That((bool)Call(match, "StartMatch"), Is.True);
            var obstaclePassages = new bool[2, 3]; var passageClearance = new float[2, 3];
            var previousCom = bodies.Select(b => b.worldCenterOfMass).ToArray();
            BoxCollider[,] obstacleGates = mode == 1 ? SavedObstacleGates((Component)match) : null;
            var players = (Array)Field(setup, "players"); var participantIds = Enumerable.Range(0, 4).Select(i => (string)Field(players.GetValue(i), "participantId")).ToArray();
            try
            {
                Time.timeScale = 3; float deadline = Time.realtimeSinceStartup + Mathf.Max(100,(float)Field(match,"matchSeconds")/3+30); float nextSample = 30; int samples = 0;
                while (!(bool)Property(match, "Finished") && Time.realtimeSinceStartup < deadline)
                {
                    for (int team = 0; team < 2; team++)
                    {
                        int owner = (int)Property(trailers.GetValue(team), "Owner");
                        if (owner >= 0 && (owners[team].Count == 0 || owners[team][owners[team].Count - 1] != owner)) owners[team].Add(owner);
                        travel[team] = Mathf.Max(travel[team], Vector3.ProjectOnPlane(bodies[team].position - starts[team], Vector3.up).magnitude);
                        if (mode == 1)
                        {
                            Vector3 current = bodies[team].worldCenterOfMass;
                            for (int gate = 0; gate < 3; gate++)
                            {
                                var shape = obstacleGates[team, gate]; var bounds = shape.bounds;
                                // Observe the real baton sweeping across each saved
                                // collider's centre plane. Correct-side clearance is
                                // derived from that physical barrier, never an AI
                                // waypoint or a manufactured gameplay receipt.
                                if (!obstaclePassages[team, gate] && previousCom[team].z < bounds.center.z && current.z >= bounds.center.z)
                                {
                                    float t = (bounds.center.z - previousCom[team].z) / (current.z - previousCom[team].z);
                                    float x = Mathf.Lerp(previousCom[team].x, current.x, t);
                                    float clearance = gate == 1 ? bounds.min.x - x : x - bounds.max.x;
                                    int requiredOwner = gate < 2 ? team : team + 2;
                                    bool physicallyAttached = owner == requiredOwner && ReferenceEquals(Property(couplers.GetValue(owner), "Attached"), trailers.GetValue(team)) && (bool)Property(couplers.GetValue(owner), "PhysicalConstraintPresent");
                                    if (clearance > 0 && physicallyAttached)
                                    { obstaclePassages[team, gate] = true; passageClearance[team, gate] = clearance; }
                                }
                            }
                            previousCom[team] = current;
                        }
                        if (mode == 2)
                        {
                            reverseEntryObserved[team] |= ((bool[])Field(match, "reverseEntered"))[team];
                            var receiving = ((Transform[])Field(match, "transferZones"))[team];
                            if (Vector3.Distance(bodies[team].position, receiving.position) < 24 && Vector3.Dot(bodies[team].linearVelocity, bodies[team].transform.forward) < -.2f) backed[team] = true;
                        }
                    }
                    float clock = (float)Property(match, "Elapsed");
                    if (clock >= nextSample && samples < 6)
                    {
                        nextSample += 30; samples++;
                        Debug.Log("HH10_HANDOFF_VARIANT_LIVE mode=" + mode + " elapsed=" + clock.ToString("F1") + " legs=" + string.Join(",", (int[])Field(match, "leg")) + " " + string.Join(" | ", Enumerable.Range(0, 4).Select(i => "P" + (i + 1) + " " + Call(match, "HandoffApproachDiagnostic", i))));
                    }
                    yield return null;
                }
                string state = "mode=" + mode + " elapsed=" + Property(match, "Elapsed") + " legs=" + string.Join(",", (int[])Field(match, "leg")) + " owners0=" + string.Join(",", owners[0]) + " owners1=" + string.Join(",", owners[1]);
                if (mode == 1) state += " obstaclePassages=" + string.Join("/", Enumerable.Range(0, 2).Select(team => string.Join(",", Enumerable.Range(0, 3).Select(gate => obstaclePassages[team, gate] + ":" + passageClearance[team, gate].ToString("F2")))));
                Assert.That((bool)Property(match, "Finished"), Is.True, state);
                int winnerTeam = Array.FindIndex((bool[])Field(match, "finishedTeam"), done => done);
                Assert.That(winnerTeam, Is.GreaterThanOrEqualTo(0), "At least one team must complete every real leg, not exhaust the timer. " + state);
                int[] expected = mode == 3 ? new[] { winnerTeam, winnerTeam + 2, winnerTeam, winnerTeam + 2 } : new[] { winnerTeam, winnerTeam + 2 };
                Assert.That(owners[winnerTeam], Is.EqualTo(expected), "Actual native ownership must traverse the team in the prescribed order. " + state);
                int legs = mode == 3 ? 4 : 2;
                Assert.That(((int[])Field(match, "leg"))[winnerTeam], Is.EqualTo(legs - 1));
                for (int leg = 0; leg < legs; leg++) Assert.That((float)Call(match, "LegSplitSeconds", winnerTeam, leg), Is.GreaterThan(0), "Every real leg records an elapsed split.");
                for (int leg = 0; leg < legs - 1; leg++) Assert.That((float)Call(match, "HandoffWaitSeconds", winnerTeam, leg), Is.GreaterThan(0), "Every exchange physically releases then couples the same load.");
                Assert.That(travel[winnerTeam], Is.GreaterThan(mode == 3 ? 350 : 200), state);
                if (mode == 1)
                {
                    for (int gate = 0; gate < 3; gate++) Assert.That(obstaclePassages[winnerTeam, gate], Is.True, "The winning original native baton must physically pass right of the first wall, left of the outer gate, and right of the return gate after the real teammate handoff. " + state);
                    Debug.Log("HH10_HANDOFF_OBSTACLE_ROUTE_PASS " + state);
                }
                if (mode == 2) Assert.That(backed[winnerTeam] && reverseEntryObserved[winnerTeam], Is.True, "Observe both real backward approach motion and the physics-qualified reverse-entry state in the receiving bay.");
                if (mode == 3)
                {
                    Assert.That((int)Property(couplers.GetValue(winnerTeam), "SuccessfulHitches"), Is.GreaterThanOrEqualTo(2));
                    Assert.That((int)Property(couplers.GetValue(winnerTeam + 2), "SuccessfulHitches"), Is.GreaterThanOrEqualTo(2));
                    Assert.That(((Transform[])Field(match, "finishes"))[winnerTeam].gameObject.activeInHierarchy, Is.False, "The old physical finish is masked in the extended layout.");
                }
                for (int team = 0; team < 2; team++) Assert.That(((Rigidbody)Field(trailers.GetValue(team), "Body")).GetInstanceID(), Is.EqualTo(identity[team]), "Every leg uses the original physical baton body.");
                var scores = (double[])Field(match, "Scores"); Assert.That(scores[winnerTeam], Is.GreaterThan(0)); Assert.That(scores[winnerTeam + 2], Is.EqualTo(scores[winnerTeam]).Within(.01));
                string previous = (string)Property(match, "MatchId"); Call(match, "SetupScreen"); Assert.That((bool)Call(match, "StartMatch"), Is.True);
                Assert.That((string)Property(match, "MatchId"), Is.Not.EqualTo(previous));
                for (int i = 0; i < 4; i++) Assert.That((string)Field(players.GetValue(i), "participantId"), Is.EqualTo(participantIds[i]), "Rematches preserve roster identities.");
                for (int team = 0; team < 2; team++) for (int leg = 0; leg < 4; leg++) Assert.That((float)Call(match, "LegSplitSeconds", team, leg), Is.Zero);
                Assert.That(((double[])Field(match, "Scores")).Take(4).All(value => value == 0), Is.True);
                if (mode == 1) for (int team = 0; team < 2; team++) for (int gate = 0; gate < 3; gate++) Assert.That(obstacleGates[team, gate].enabled && obstacleGates[team, gate].gameObject.activeInHierarchy, Is.True, "Rematch must retain every actual obstacle collider.");
                Time.timeScale = 3; yield return new WaitForSeconds(.8f);
                Assert.That((bool)Property(match, "Running"), Is.True);
                Call(match, "SetupScreen");
            }
            finally { Time.timeScale = 1; }
        }
    }
    static BoxCollider[,] SavedObstacleGates(Component match)
    {
        var result = new BoxCollider[2, 3];
        var stations = (Transform[])Field(match, "transferZones");
        string[] names = { "Obstacle relay offset chicane", "Outer obstacle relay gate", "Trailer-clearance return gate" };
        var shapes = match.GetComponentsInChildren<BoxCollider>(true);
        for (int gate = 0; gate < names.Length; gate++)
        {
            var saved = shapes.Where(shape => shape.name == names[gate]).OrderBy(shape => shape.bounds.center.x).ToArray();
            Assert.That(saved.Length, Is.EqualTo(2), "Use the TWO saved team barriers named " + names[gate] + ", not substitute test geometry.");
            for (int team = 0; team < 2; team++)
            {
                var shape = saved[stations[team].position.x < stations[1 - team].position.x ? 0 : 1];
                Assert.That(shape.enabled && shape.gameObject.activeInHierarchy && !shape.isTrigger, Is.True, "Obstacle Relay requires the saved solid collider " + names[gate]);
                Assert.That(shape.bounds.size.x, Is.GreaterThan(4), "The full-size physical gate must remain in the course.");
                result[team, gate] = shape;
            }
        }
        return result;
    }
}
#endif

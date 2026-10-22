#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class FishingSavedNetTests
{
    const BindingFlags Fields = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    static object Field(object owner, string name) => owner.GetType().GetField(name, Fields).GetValue(owner);
    static object Property(object owner, string name) => owner.GetType().GetProperty(name).GetValue(owner);
    static object Call(object owner, string name, params object[] args) => owner.GetType().GetMethod(name).Invoke(owner, args);

    [UnityTest, Timeout(60000)]
    public IEnumerator FourSavedStreamsKeepRealNetCrossingsClearBanksAndPooledRematches()
    {
        Component match = null; Keyboard keyboard = null; InputSettings temporary = null;
        var originalSettings = InputSystem.settings;
        var prefs = Type.GetType("LWS.TruckSports.SportsPrefs, Assembly-CSharp", true);
        var lease = (IDisposable)prefs.GetMethod("IsolateValidation").Invoke(null, new object[] { "FishingSavedNetTests" });
        var isolation = new GameObject("Owned Fishing scene regression isolation"); UnityEngine.Object.DontDestroyOnLoad(isolation);
        isolation.AddComponent(Type.GetType("LWS.TruckSports.SportsProbeInputIsolation, Assembly-CSharp", true));
        try
        {
            temporary = UnityEngine.Object.Instantiate(originalSettings);
            temporary.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            temporary.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings = temporary; keyboard = InputSystem.AddDevice<Keyboard>();
            EditorSceneManager.LoadSceneInPlayMode("Assets/TruckFishing/Scenes/TruckFishing.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null; yield return null;
            var type = Type.GetType("LWS.TruckFishing.FishingMatch, Assembly-CSharp", true);
            match = (Component)UnityEngine.Object.FindObjectsByType(type, FindObjectsSortMode.None).Single();
            object setup = Field(match, "setup"); setup.GetType().GetField("count").SetValue(setup, 4); Call(match, "AllBots");
            foreach (var player in (Array)Field(setup, "players"))
                player.GetType().GetField("source").SetValue(player, Enum.Parse(player.GetType().GetField("source").FieldType, "Keyboard"));
            // Only the first four seats participate. Default clusters are distinct.
            var players = (Array)Field(setup, "players");
            for (int i = 4; i < players.Length; i++)
            {
                var player = players.GetValue(i);
                player.GetType().GetField("source").SetValue(player, Enum.Parse(player.GetType().GetField("source").FieldType, "AI"));
            }
            Assert.That((bool)Call(match, "StartMatch"), Is.True);
            yield return null; Assert.That(((Camera[])Field(match, "cameras")).Length, Is.EqualTo(4));
            var streams = (Array)Field(match, "streams"); var pools = new Array[4]; var first = new Transform[4];
            for (int slot = 0; slot < 4; slot++)
            {
                var stream = (Component)streams.GetValue(slot);
                Assert.That((float)Field(stream, "netHalfTravel"), Is.EqualTo(2.6f));
                Assert.That((float)Field(stream, "radius"), Is.EqualTo(1.1f));
                foreach (GameObject prefab in (Array)Field(stream, "fishPrefabs"))
                    Assert.That(prefab.transform.localScale, Is.EqualTo(Vector3.one * 1.3f));
                pools[slot] = (Array)Field(stream, "models"); first[slot] = (Transform)pools[slot].GetValue(0);
                Assert.That(pools[slot].Length, Is.EqualTo(156));
                var net = (Transform)Field(stream, "net"); var terrain = stream.GetComponentInChildren<TerrainCollider>();
                foreach (float x in new[] { -2.6f, 0, 2.6f })
                {
                    var point = stream.transform.TransformPoint(new Vector3(x, .12f, 0));
                    Assert.That(terrain.Raycast(new Ray(point + Vector3.up * 4, Vector3.down), out var hit, 10), Is.True);
                    Assert.That(hit.point.y, Is.LessThan(point.y - .1f), "Solid river terrain must stay below the actual net sweep.");
                }
            }
            float until = Time.realtimeSinceStartup + 8;
            while (!(bool)Property(match, "Playing") && Time.realtimeSinceStartup < until) yield return null;
            Assert.That((bool)Property(match, "Playing"), Is.True);
            var planType = Type.GetType("LWS.TruckFishing.FishPlan, Assembly-CSharp", true);
            var kindType = Type.GetType("LWS.TruckFishing.FishKind, Assembly-CSharp", true);
            double arrival = (double)Property(match, "Clock") + .6;
            for (int slot = 0; slot < 4; slot++)
            {
                object plan = Activator.CreateInstance(planType);
                planType.GetField("id").SetValue(plan, 900000L);
                planType.GetField("kind").SetValue(plan, Enum.Parse(kindType, "Small"));
                planType.GetField("arrival").SetValue(plan, arrival);
                planType.GetField("travel").SetValue(plan, .25f);
                planType.GetField("entryLead").SetValue(plan, .35f);
                Call(streams.GetValue(slot), "Spawn", plan);
            }
            until = Time.realtimeSinceStartup + 2;
            while ((double)Property(match, "Clock") < arrival && Time.realtimeSinceStartup < until) yield return null;
            var rules = Field(match, "rules");
            CollectionAssert.AreEqual(new[] { 1, 1, 1, 1 }, (int[])Field(rules, "caught"), "Four real pooled net-plane crossings must each score exactly once.");
            yield return new WaitForSeconds(.15f);
            CollectionAssert.AreEqual(new[] { 1, 1, 1, 1 }, (int[])Field(rules, "caught"));
            Call(match, "Pause", 0, false); double frozen = (double)Property(match, "Clock");
            yield return new WaitForSecondsRealtime(.15f); Assert.That((double)Property(match, "Clock"), Is.EqualTo(frozen));
            Call(match, "SetupScreen"); Assert.That((bool)Call(match, "StartMatch"), Is.True);
            Assert.That((double)Property(match, "Clock"), Is.Zero);
            CollectionAssert.AreEqual(new[] { 0, 0, 0, 0 }, (int[])Field(rules, "caught"));
            for (int slot = 0; slot < 4; slot++)
            {
                var stream = streams.GetValue(slot);
                Assert.That(Field(stream, "models"), Is.SameAs(pools[slot]));
                Assert.That(((Array)Field(stream, "models")).GetValue(0), Is.SameAs(first[slot]));
                Assert.That((bool)Property(stream, "Overflowed"), Is.False);
            }
        }
        finally
        {
            if (match != null) Call(match, "SetupScreen");
            if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
            InputSystem.settings = originalSettings;
            if (temporary != null) UnityEngine.Object.DestroyImmediate(temporary);
            UnityEngine.Object.DestroyImmediate(isolation); lease.Dispose(); Time.timeScale = 1;
        }
    }
}
#endif

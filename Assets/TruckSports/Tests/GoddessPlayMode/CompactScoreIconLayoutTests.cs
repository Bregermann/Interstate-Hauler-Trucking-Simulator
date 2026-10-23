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

public sealed class CompactScoreIconLayoutTests
{
    const BindingFlags Any = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    static Type Runtime(string name) => Type.GetType("LWS.TruckSports." + name + ", Assembly-CSharp", true);
    static object Get(object owner, string name) => owner.GetType().GetField(name, Any) is FieldInfo field ? field.GetValue(owner) : owner.GetType().GetProperty(name, Any).GetValue(owner);
    static void Set(object owner, string name, object value) => owner.GetType().GetField(name, Any).SetValue(owner, value);
    static object Call(object owner, string name, params object[] args) => owner.GetType().GetMethods(Any).Single(method => method.Name == name && method.GetParameters().Length == args.Length).Invoke(owner, args);
    static Rect Bounds(RectTransform rect) { var c = new Vector3[4]; rect.GetWorldCorners(c); return Rect.MinMaxRect(c.Min(v => v.x), c.Min(v => v.y), c.Max(v => v.x), c.Max(v => v.y)); }
    [UnityTest, Timeout(60000)] public IEnumerator EightCardValuesStayClearOfTheLockedHumanIcon() { yield return Inspect(8); }
    [UnityTest, Timeout(60000)] public IEnumerator TwelveCardValuesStayClearOfTheLockedHumanIcon() { yield return Inspect(12); }
    static IEnumerator Inspect(int count)
    {
        var scope = (IDisposable)Runtime("SportsPrefs").GetMethod("IsolateValidation").Invoke(null, new object[] { "SyntheticCompactIconLayout" });
        var isolation = new GameObject("Explicit synthetic compact-card state fixture"); UnityEngine.Object.DontDestroyOnLoad(isolation); isolation.AddComponent(Runtime("SportsProbeInputIsolation")); Component match = null; Gamepad pad = null;
        try
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/TruckAvalancheEscape/Scenes/TruckAvalancheEscape.unity", new LoadSceneParameters(LoadSceneMode.Single)); yield return null; yield return null;
            match = (Component)UnityEngine.Object.FindAnyObjectByType(Runtime("SportsEventSession")); var setup = Get(match, "setup"); Set(setup, "count", count); Call(match, "AllBots");
            pad = InputSystem.AddDevice<Gamepad>("Explicit software compact-card owner"); var player = ((Array)Get(setup, "players")).GetValue(0); var source = player.GetType().GetField("source"); source.SetValue(player, Enum.Parse(source.FieldType, "WholeController")); Set(player, "pad", pad);
            Assert.That(Call(match, "StartMatch"), Is.True); ((Behaviour)match).enabled = false; Time.timeScale = 0;
            var rules = Get(match, "rules"); Call(rules, "Tick", 200f); Assert.That(Call(rules, "Finish", 0, 181f), Is.True);
            yield return new WaitForSecondsRealtime(.5f); var menus = (Component)Get(match, "menus"); var cardsOwner = menus.GetComponent(Runtime("SportsBroadcastScores")); var cards = ((Array)Get(cardsOwner, "cards")).Cast<object>().Take(count).ToArray();
            var first = cards[0]; var locked = (GameObject)Get(first, "padlock"); Assert.That(locked.activeSelf, Is.True);
            Action<Camera> inspect = camera =>
            {
                Canvas.ForceUpdateCanvases(); var value = (Component)Get(first, "value"); var row = ((GameObject)Get(first, "root")).GetComponent<RectTransform>();
                Assert.That(Bounds(locked.GetComponent<RectTransform>()).Overlaps(Bounds(value.GetComponent<RectTransform>())), Is.False, "The locked state must not cover the actual time/score suffix.");
                Assert.That(((Component)Get(first, "glyph")).gameObject.activeSelf, Is.False, "A finished compact card shows its lock in the icon lane instead of stacking both icons.");
                var body = Bounds(row); var label = Bounds(value.GetComponent<RectTransform>()); Assert.That(label.xMin, Is.GreaterThanOrEqualTo(body.xMin)); Assert.That(label.xMax, Is.LessThanOrEqualTo(body.xMax));
                Call(value, "ForceMeshUpdate", false, false); Assert.That(Get(value, "isTextOverflowing"), Is.False, "The actual 181.0s finish value must fit.");
                foreach (var card in cards) Assert.That(((GameObject)Get(card, "root")).activeSelf, Is.True);
            };
            var save = Runtime("SportsControlledCapture").GetMethods().Single(method => method.Name == "Save" && method.GetParameters().Length == 5);
            foreach (var size in new[] { new Vector2Int(1280,720), new Vector2Int(1920,1080) }) save.Invoke(null, new object[] { match, size.x, size.y, "Logs/TruckSports/PlaytestRepair/CompactScoreUI/" + count + "-" + size.y + ".png", inspect });
            TestContext.WriteLine("SYNTHETIC_COMPACT_UI " + count + " actual saved gameplay HUD with software controller owner and explicit rule-finish state; time/lock separation, single icon and controlled720/1080 geometry. Not physical-match acceptance.");
        }
        finally { Time.timeScale = 1; if (match != null) { ((Behaviour)match).enabled = true; Call(match, "SetupScreen"); } if (pad != null && pad.added) InputSystem.RemoveDevice(pad); UnityEngine.Object.DestroyImmediate(isolation); scope.Dispose(); }
    }
}
#endif

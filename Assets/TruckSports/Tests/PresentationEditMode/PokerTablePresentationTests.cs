#if UNITY_EDITOR
using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

public sealed class PokerTablePresentationTests
{
    static object Field(object target, string name) => target.GetType().GetField(name).GetValue(target);
    [Test] public void SavedPokerHudRetainsOriginalPrivateCardsActionsAndDealer()
    {
        EditorSceneManager.OpenScene("Assets/TruckPoker/Scenes/TruckPoker.unity");
        var m = UnityEngine.Object.FindObjectsByType(Type.GetType("LWS.TruckPoker.PokerMatch, Assembly-CSharp", true), FindObjectsSortMode.None).Single();
        var p = Field(m, "presentation"); var hud = Field(p, "tableHud"); Assert.That(hud, Is.Not.Null); Assert.That(Field(m, "dealerDrive"), Is.Not.Null);
        Assert.That(((RectTransform)Field(hud, "header")).anchorMin, Is.EqualTo(new Vector2(.5f, .88f))); Assert.That(Field(hud, "venueLogo"), Is.Not.Null);
        var panes = (Array)Field(p, "panes"); Assert.That(panes.Length, Is.EqualTo(4));
        foreach (var pane in panes)
        {
            var root = (RectTransform)Field(pane, "root"); Assert.That(root.GetComponent<UnityEngine.UI.RectMask2D>(), Is.Not.Null);
            foreach (var card in ((Array)Field(pane, "own")).Cast<Component>().Concat(((Array)Field(pane, "board")).Cast<Component>())) Assert.That(card.transform.IsChildOf(root), Is.True, "Original card remains isolated inside its owner pane.");
            var actions = (GameObject)Field(pane, "actions"); var routes = actions.GetComponentsInChildren(Type.GetType("LWS.TruckPoker.PokerMouseButton, Assembly-CSharp", true), true); Assert.That(routes.Length, Is.EqualTo(7)); Assert.That(routes.Select(r => (int)Field(r, "action")).Distinct().Count(), Is.EqualTo(7));
        }
        Assert.That(((Array)Field(hud, "broadcastSeats")).Length, Is.EqualTo(8)); Assert.That(((Array)Field(hud, "views")).Length, Is.EqualTo(4));
    }
    [Test] public void AmbientPivotsWrapOriginalReactionNodesWithoutPhysicsOrCamera()
    {
        EditorSceneManager.OpenScene("Assets/TruckPoker/Scenes/TruckPoker.unity");
        var m = UnityEngine.Object.FindObjectsByType(Type.GetType("LWS.TruckPoker.PokerMatch, Assembly-CSharp", true), FindObjectsSortMode.None).Single(); var p = Field(m, "presentation");
        var presence = Field(p, "presence"); var pivots = (Transform[])Field(presence, "pivots"); var expressions = (Transform[])Field(p, "expressions"); var avatars = (Transform[])Field(m, "avatars");
        Assert.That(pivots.Length, Is.EqualTo(8));
        for (int i = 0; i < 8; i++)
        { Assert.That(expressions[i].parent, Is.SameAs(pivots[i])); Assert.That(pivots[i].parent, Is.SameAs(avatars[i])); Assert.That(pivots[i].GetComponentsInChildren<Collider>(true).Length, Is.Zero); Assert.That(pivots[i].GetComponentsInChildren<Rigidbody>(true).Length, Is.Zero); Assert.That(pivots[i].GetComponentsInChildren<Camera>(true).Length, Is.Zero); Assert.That(pivots[i].localRotation, Is.EqualTo(Quaternion.identity)); }
    }
    [Test] public void AwardCaptionNeverQueriesOrRevealsUnrenderedPrivateHands()
    {
        EditorSceneManager.OpenScene("Assets/TruckPoker/Scenes/TruckPoker.unity");
        var m = UnityEngine.Object.FindObjectsByType(Type.GetType("LWS.TruckPoker.PokerMatch, Assembly-CSharp", true), FindObjectsSortMode.None).Single(); var p = Field(m, "presentation"); var hud = Field(p, "tableHud");
        var engine = Type.GetType("LWS.TruckPoker.PokerTable, Assembly-CSharp", true); var action = Type.GetType("LWS.TruckPoker.PokerAction, Assembly-CSharp", true); var backing = m.GetType().GetField("<Table>k__BackingField", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        object Table() { var t = Activator.CreateInstance(engine, new object[] { new[] { 200, 200, 200 }, 13, 5, 3 }); backing.SetValue(m, t); engine.GetMethod("BeginHand").Invoke(t, null); return t; }
        void Act(object t, int move) { int seat = (int)engine.GetProperty("Actor").GetValue(t); var args = new object[] { seat, Enum.ToObject(action, move), 0, null }; Assert.That(engine.GetMethod("Act").Invoke(t, args), Is.True); }
        string Caption() => (string)hud.GetType().GetMethod("PublicAwardCaption").Invoke(hud, null);
        var folded = Table(); Act(folded, 0); Act(folded, 0); Assert.That(Caption(), Is.EqualTo("LAST LIVE HAND"));
        var cards = (Array)Field(p, "seatCards"); Assert.That(cards.Cast<object>().All(c => (int)c.GetType().GetProperty("Card").GetValue(c) == -1), Is.True, "Fold win never populates a public card face.");
        var showdown = Table(); int steps = 0; while (!(bool)engine.GetProperty("HandDone").GetValue(showdown) && steps++ < 30) Act(showdown, 1);
        Assert.That((bool)engine.GetProperty("Showdown").GetValue(showdown), Is.True); Assert.That(Caption(), Is.EqualTo("SHOWDOWN"), "Rank stays hidden until the original public renderer legitimately exposes its cards.");
        foreach (var pot in (Array)engine.GetProperty("Awards").GetValue(showdown)) foreach (int seat in (int[])Field(pot, "Winners"))
        {
            Assert.That(engine.GetMethod("PubliclyRevealed").Invoke(showdown, new object[] { seat }), Is.True);
            var own = (int[])engine.GetMethod("OwnHand").Invoke(showdown, new object[] { seat });
            for (int c = 0; c < 2; c++) { var renderer = cards.GetValue(seat * 2 + c); renderer.GetType().GetMethod("Show").Invoke(renderer, new object[] { own[c] }); }
        }
        CollectionAssert.Contains(new[] { "HIGH CARD", "ONE PAIR", "TWO PAIR", "THREE OF A KIND", "STRAIGHT", "FLUSH", "FULL HOUSE", "FOUR OF A KIND", "STRAIGHT FLUSH" }, Caption());
    }
}
#endif

#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

public sealed class KickballWaitingReferenceTests
{
    [Test] public void SecuredBallFramesTheThreatenedRunnerAndTheirNextBase()
    {
        EditorSceneManager.OpenScene("Assets/TruckKickball/Scenes/TruckKickball.unity");
        var type = Type.GetType("LWS.TruckKickball.KickballMatch, Assembly-CSharp", true);
        var match = (Component)UnityEngine.Object.FindObjectsByType(type, FindObjectsSortMode.None).Single();
        object rules = type.GetField("rules").GetValue(match); var rulesType = rules.GetType();
        rulesType.GetMethod("Reset").Invoke(rules, new object[] { 6, 1 }); rulesType.GetMethod("Pitch").Invoke(rules, null);
        rulesType.GetMethod("Kick").Invoke(rules, null);
        type.GetProperty("Carrier").GetSetMethod(true).Invoke(match, new object[] { 6 });
        var views = (Component)type.GetField("views").GetValue(match);
        var rig = (Component)views.GetType().GetField("broadcast").GetValue(views);
        var framing = (Component)rig.GetType().GetField("broadcastFraming").GetValue(rig);
        var actionType = Type.GetType("LWS.TruckSports.SportsBroadcastAction, Assembly-CSharp", true);
        var action = Activator.CreateInstance(actionType); type.GetMethod("DescribeBroadcast").Invoke(match, new[] { action, framing });
        Assert.That((int)actionType.GetField("primary").GetValue(action), Is.EqualTo(0));
        StringAssert.Contains("RUNNER AND NEXT BASE", (string)actionType.GetField("reason").GetValue(action));
        Assert.That(((Bounds)actionType.GetField("objective").GetValue(action)).center, Is.EqualTo(new Vector3(27, 0, 27)));
    }
    [Test] public void ExistingSceneOwnsPacingMenusAndUpgradePreservesSavedCustomizations()
    {
        const string path = "Assets/TruckKickball/Scenes/TruckKickball.unity";
        EditorSceneManager.OpenScene(path);
        var type = Type.GetType("LWS.TruckKickball.KickballMatch, Assembly-CSharp", true);
        var match = (Component)UnityEngine.Object.FindObjectsByType(type, FindObjectsSortMode.None).Single();
        var pace = (Component)type.GetField("aiPlayback").GetValue(match);
        Assert.That((int)type.GetField("waitingPresentationVersion").GetValue(match), Is.EqualTo(2));
        Assert.That((bool)type.GetProperty("CompactViewportHud").GetValue(match), Is.True);
        Assert.That(pace, Is.Not.Null); Assert.That(pace.GetType().GetField("match").GetValue(pace), Is.SameAs(match));
        foreach (string field in new[] { "viewChoice", "pauseViewChoice", "paceChoice", "pausePaceChoice" })
            Assert.That(type.GetField(field).GetValue(match), Is.Not.Null, field);
        var agents = (Array)type.GetField("agents").GetValue(match); Assert.That(agents.Length, Is.EqualTo(12));
        var views = (Component)type.GetField("views").GetValue(match);
        var cameraRig = (Component)views.GetType().GetField("broadcast").GetValue(views);
        var framing = (Component)cameraRig.GetType().GetField("broadcastFraming").GetValue(cameraRig);
        Assert.That(framing, Is.Not.Null); Assert.That(cameraRig.GetType().GetField("rig").GetValue(cameraRig), Is.Not.Null);
        var settings = framing.GetType().GetProperty("Configuration").GetValue(framing);
        Assert.That((float)settings.GetType().GetField("azimuth").GetValue(settings), Is.EqualTo(-90));
        var saved = File.ReadAllBytes(path);
        Type.GetType("LWS.TruckKickball.Editor.KickballWaitingAuthoring, Assembly-CSharp-Editor", true).GetMethod("Upgrade").Invoke(null, null);
        CollectionAssert.AreEqual(saved, File.ReadAllBytes(path), "Running the tool again must preserve authored settings.");
    }
}
#endif

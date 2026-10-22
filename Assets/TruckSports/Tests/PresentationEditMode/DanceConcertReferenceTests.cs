#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

public sealed class DanceConcertReferenceTests
{
    [Test]public void DedicatedArtworkFrontFacingRigsAndExistingCinemachineAreSavedWithoutRebuilding()
    {
        const string path="Assets/TruckDanceOff/Scenes/TruckDanceOff.unity";EditorSceneManager.OpenScene(path);
        var type=Type.GetType("LWS.TruckDanceOff.DanceConcertCamera, Assembly-CSharp",true);var director=(Component)UnityEngine.Object.FindObjectsByType(type,FindObjectsSortMode.None).Single();
        var match=(Component)type.GetField("match").GetValue(director);var rig=(Component)type.GetField("cameraRig").GetValue(director);Assert.That(rig.GetType().GetField("rig").GetValue(rig),Is.Not.Null);Assert.That(rig.GetType().GetField("broadcastFraming").GetValue(rig),Is.Null,"One camera authority.");
        Assert.That((float)match.GetType().GetField("greatTimingWindow").GetValue(match),Is.EqualTo(.12f));Assert.That((float)match.GetType().GetField("goodTimingWindow").GetValue(match),Is.EqualTo(.21f));
        foreach(Component dancer in (Array)match.GetType().GetField("dancers").GetValue(match))Assert.That(Vector3.Dot(dancer.transform.forward,Vector3.back),Is.GreaterThan(.999f));
        var sign=GameObject.Find("Original Truck Sports Party stage wordmark").GetComponent<Renderer>();Assert.That(sign.sharedMaterial.mainTexture.name,Is.EqualTo("TruckDanceOff"));Assert.That(sign.GetComponent<Collider>(),Is.Null);
        var before=File.ReadAllBytes(path);Type.GetType("LWS.TruckDanceOff.Editor.DancePlaytestRepairAuthoring, Assembly-CSharp-Editor",true).GetMethod("Upgrade").Invoke(null,null);CollectionAssert.AreEqual(before,File.ReadAllBytes(path));
    }
}
#endif

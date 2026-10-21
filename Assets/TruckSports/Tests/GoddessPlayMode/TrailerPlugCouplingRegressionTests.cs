#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class TrailerPlugCouplingRegressionTests
{
    static Type MatchType => Type.GetType("LWS.TruckTrailerPlug.TrailerPlugMatch, Assembly-CSharp", true);
    static object Field(object target, string name) => target.GetType().GetField(name).GetValue(target);
    static object Call(object target, string method, params object[] args) => target.GetType().GetMethod(method).Invoke(target, args);
    static object Property(object target, string name) => target.GetType().GetProperty(name).GetValue(target);

    [UnityTest]
    public IEnumerator NativeHitchStaysAlignedThenDetachesWithoutChangingMomentumAcrossRepeatedStarts()
    {
        var prefs = Type.GetType("LWS.TruckSports.SportsPrefs, Assembly-CSharp", true);
        using ((IDisposable)prefs.GetMethod("IsolateValidation").Invoke(null, new object[] { "TrailerCouplingRegression" }))
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/TruckTrailerPlug/Scenes/TruckTrailerPlug.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null; yield return null;
            var match = UnityEngine.Object.FindObjectsByType(MatchType, FindObjectsSortMode.None).Single();
            Call(match, "AllBots");
            for (int round = 0; round < 3; round++)
            {
                Assert.That((bool)Call(match, "StartMatch"), Is.True);
                var rig = Field(match, "rig");
                var hitchField = rig.GetType().GetField("tractorHitch");
                Assert.That(hitchField, Is.Not.Null, "The real NWH coupling must replace the sport's hand-built trailer joint.");
                var hitchWrapper = hitchField.GetValue(rig);
                Assert.That(hitchWrapper, Is.Not.Null);
                var hitch = Field(hitchWrapper, "module");
                var trailerModule = Field(Field(rig, "trailerModule"), "module");
                yield return null; yield return null;
                Assert.That((bool)Field(hitch, "attached"), Is.True, "Countdown must attach through the vendor module after deferred joint destruction.");
                var a = (Transform)Field(hitch, "attachmentPoint");
                var b = (Transform)Field(trailerModule, "attachmentPoint");
                Assert.That(Vector3.Distance(a.position, b.position), Is.LessThan(.03f), "Countdown " + round + " hitch anchors: tractor=" + a.position.ToString("F4") + " trailer=" + b.position.ToString("F4"));
                var trailer = (Rigidbody)Field(rig, "trailer");
                var truck = Field(rig, "truck"); var tractor = (Rigidbody)Field(truck, "body");
                Assert.That(trailer.isKinematic && tractor.isKinematic, Is.True, "Countdown holds both actual bodies stationary.");
                var until = Time.realtimeSinceStartup + 5;
                while (Property(match, "Phase").ToString() == "CountIn" && Time.realtimeSinceStartup < until) yield return null;
                Assert.That(Property(match, "Phase").ToString(), Is.EqualTo("Swing"));
                for (int i = 0; i < 20; i++) yield return new WaitForFixedUpdate();
                Assert.That(Vector3.Distance(a.position, b.position), Is.LessThan(.35f), "Physical joint must maintain real hitch contact during the swing.");
                var velocity = trailer.linearVelocity; var angular = trailer.angularVelocity;
                Assert.That((bool)Call(match, "Release"), Is.True);
                Assert.That(trailer.linearVelocity, Is.EqualTo(velocity));
                Assert.That(trailer.angularVelocity, Is.EqualTo(angular));
                Assert.That((bool)Field(hitch, "attached"), Is.False);
                Assert.That((bool)Call(rig, "Release"), Is.False, "A second release cannot detach/score again.");
                yield return null;
                Assert.That(tractor.GetComponents<ConfigurableJoint>().Length, Is.Zero);
                Call(match, "SetupScreen"); yield return null;
            }
        }
    }

    [UnityTest]
    public IEnumerator SavedMatchedModelsKeepNativeProportionsAndPhysicalHoleMatchesCoverage()
    {
        EditorSceneManager.LoadSceneInPlayMode("Assets/TruckTrailerPlug/Scenes/TruckTrailerPlug.unity", new LoadSceneParameters(LoadSceneMode.Single));
        yield return null; yield return null;
        var match = UnityEngine.Object.FindObjectsByType(MatchType, FindObjectsSortMode.None).Single();
        var rig = Field(match, "rig"); var truck = (Component)Field(rig, "truck"); var trailer = (Rigidbody)Field(rig, "trailer");
        var authoring = Type.GetType("LWS.TruckTrailerPlug.Editor.TrailerPlugRepairAuthoring, Assembly-CSharp-Editor", true);
        var bounds = authoring.GetMethod("ModelBounds");
        float scale = -1;
        foreach (var pair in new[] { ("TractorSource", truck.gameObject), ("TrailerSource", trailer.gameObject) })
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>((string)authoring.GetField(pair.Item1).GetValue(null));
            var source = (Bounds)bounds.Invoke(null, new object[] { prefab });
            var target = (Bounds)bounds.Invoke(null, new object[] { pair.Item2 });
            float x = target.size.x / source.size.x, y = target.size.y / source.size.y, z = target.size.z / source.size.z;
            Assert.That(y, Is.EqualTo(x).Within(.03f)); Assert.That(z, Is.EqualTo(x).Within(.03f), "No stretched trailer dimensions.");
            if (scale < 0) scale = x; else Assert.That(x, Is.EqualTo(scale).Within(.03f), "Matched tractor and trailer use the same uniform authored scale.");
            Assert.That(pair.Item2.transform.localScale, Is.EqualTo(Vector3.one), "NWH root must retain its supported unit scale.");
        }
        var radii = (Vector2)Field(match, "openingRadii");
        Assert.That(radii.x, Is.GreaterThan(2.4f)); Assert.That(radii.y, Is.GreaterThan(6));
        var floor = GameObject.Find("Continuous physical floor around open hole");
        var mesh = floor.GetComponent<MeshFilter>().sharedMesh;
        Assert.That(floor.GetComponent<MeshCollider>().sharedMesh, Is.SameAs(mesh));
        for (int i = 0; i < mesh.vertexCount; i += 2)
        {
            var p = mesh.vertices[i];
            Assert.That(p.x * p.x / (radii.x * radii.x) + p.z * p.z / (radii.y * radii.y), Is.EqualTo(1).Within(.001f));
        }
    }

    [UnityTest]
    public IEnumerator LargerNativeAssemblyRemainsReadableInTheSwingCamera()
    {
        var prefs = Type.GetType("LWS.TruckSports.SportsPrefs, Assembly-CSharp", true);
        using ((IDisposable)prefs.GetMethod("IsolateValidation").Invoke(null, new object[] { "TrailerCameraRegression" }))
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/TruckTrailerPlug/Scenes/TruckTrailerPlug.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null; yield return null;
            var match = UnityEngine.Object.FindObjectsByType(MatchType, FindObjectsSortMode.None).Single();
            Call(match, "AllBots"); Assert.That((bool)Call(match, "StartMatch"), Is.True);
            yield return new WaitForSeconds(3.8f);
            var rig = Field(match, "rig"); var truck = Field(rig, "truck");
            var camera = ((Camera[])Field(match, "cameras"))[0];
            var hull = (BoxCollider)Field(truck, "shape");
            var point = camera.WorldToViewportPoint(hull.bounds.center);
            Assert.That(point.z, Is.GreaterThan(camera.nearClipPlane));
            Assert.That(point.x, Is.InRange(.06f, .94f)); Assert.That(point.y, Is.InRange(.12f, .9f), "New native tractor must not be above the frame after scaling.");
            var trailerCenter = (Vector3)Property(rig, "TrailerCenter");
            point = camera.WorldToViewportPoint(trailerCenter);
            Assert.That(point.z, Is.GreaterThan(camera.nearClipPlane));
            Assert.That(point.x, Is.InRange(.06f, .94f)); Assert.That(point.y, Is.InRange(.12f, .9f));
            var projection = Type.GetType("LWS.TruckSports.SportsCameraFit, Assembly-CSharp", true).GetMethod("Projected");
            var rect = (Rect)projection.Invoke(null, new object[] { camera, hull.bounds });
            Assert.That(rect.height, Is.InRange(.08f, .65f), "Scale repair must preserve readable truck size without zooming into it.");
            Call(match, "SetupScreen");
        }
    }
}
#endif

using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class DockVenueLifecycleTests
{
    static object Get(object t, string n) => t.GetType().GetField(n)?.GetValue(t) ?? t.GetType().GetProperty(n)?.GetValue(t);
    static object Call(object t, string n, params object[] args) => t.GetType().GetMethod(n).Invoke(t, args);
    [UnityTest] public IEnumerator RealCrowdPosesAndWaterAnimatePauseAndReuseAcrossRematch()
    {
        var prefs = Type.GetType("LWS.TruckSports.SportsPrefs, Assembly-CSharp", true); var scope = (IDisposable)prefs.GetMethod("IsolateValidation").Invoke(null, new object[] { "DockVenueNative" });
        try
        {
#if UNITY_EDITOR
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/TruckDockDiving/Scenes/TruckDockDiving.unity", new LoadSceneParameters(LoadSceneMode.Single));
#else
            yield return SceneManager.LoadSceneAsync("TruckDockDiving");
#endif
            yield return null; var m = UnityEngine.Object.FindObjectsByType(Type.GetType("LWS.TruckDockDiving.DockMatch, Assembly-CSharp", true), FindObjectsSortMode.None).Single(); var p = ((Component)m).GetComponent(Type.GetType("LWS.TruckDockDiving.DockAudiencePresentation, Assembly-CSharp", true));
            Assert.That((int)Get(p, "Spectators"), Is.EqualTo(464)); Assert.That((int)Get(p, "BakedMeshes"), Is.EqualTo(16));
            var crowd = Get(p, "crowd"); var batches = ((Array)Get(crowd, "batches")).Cast<object>().ToArray(); var ids = batches.Select(b => ((Mesh)Get(b, "idle")).GetInstanceID()).ToArray();
            foreach (var b in batches)
            {
                var idle = (Mesh)Get(b, "idle"); var cheer = (Mesh)Get(b, "cheer"); Assert.That(idle.vertexCount, Is.GreaterThan(200)); Assert.That(cheer.vertexCount, Is.EqualTo(idle.vertexCount));
                Assert.That(idle.vertices.Zip(cheer.vertices, (a, c) => (a - c).sqrMagnitude).Max(), Is.GreaterThan(.0001f), "Real vendor applause must visibly deform its compatible character."); Assert.That(((MeshFilter[])Get(b, "fans")).All(f => f.sharedMesh == idle), Is.True);
            }
            var variants = ((Array)Get(p, "variants")).Cast<object>().ToArray(); Assert.That(variants.All(v => !((Transform)Get(v, "root")).gameObject.activeSelf), Is.True, "Pose sources are retired after the single bounded bake.");
            var water = GameObject.Find("NWH water plane · URP pool surface").GetComponent<Renderer>(); var offset = water.sharedMaterial.mainTextureOffset; yield return new WaitForSeconds(.3f); Assert.That((water.sharedMaterial.mainTextureOffset - offset).sqrMagnitude, Is.GreaterThan(.00001f), "Existing DOTween water adapter moves the actual surface UVs.");
            Get(m, "setup").GetType().GetField("count").SetValue(Get(m, "setup"), 4); Call(m, "AllBots"); Assert.That(Call(m, "StartMatch"), Is.True); Call(m, "Pause", 0, false); offset = water.sharedMaterial.mainTextureOffset; yield return new WaitForSecondsRealtime(.15f); Assert.That(water.sharedMaterial.mainTextureOffset, Is.EqualTo(offset)); Call(m, "Resume");
            var fx = ((Component)m).GetComponent(Type.GetType("LWS.TruckSports.SportsWaterInteraction, Assembly-CSharp", true)); float limit = Time.realtimeSinceStartup + 25; while ((int)Get(fx, "Entries") == 0 && Time.realtimeSinceStartup < limit) yield return null; Assert.That((int)Get(fx, "Entries"), Is.GreaterThan(0), "Real autonomous water contact triggers existing entry effects.");
            Call(m, "SetupScreen"); Assert.That(Call(m, "StartMatch"), Is.True); yield return null; CollectionAssert.AreEqual(ids, batches.Select(b => ((Mesh)Get(b, "idle")).GetInstanceID()).ToArray()); Assert.That((int)Get(m, "CompletedAttempts"), Is.Zero); Call(m, "SetupScreen");
        }
        finally { Time.timeScale = 1; scope.Dispose(); }
    }
}

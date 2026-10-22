#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

public sealed class DockVenueReferenceTests
{
    static object Get(object t, string n) => t.GetType().GetField(n).GetValue(t);
    const string Scene = "Assets/TruckDockDiving/Scenes/TruckDockDiving.unity";
    [Test] public void SavedVenueUsesEightOwnedClothedVariantsAndRetainsOriginalCourse()
    {
        EditorSceneManager.OpenScene(Scene); var m = UnityEngine.Object.FindObjectsByType(Type.GetType("LWS.TruckDockDiving.DockMatch, Assembly-CSharp", true), FindObjectsSortMode.None).Single(); var comp = (Component)m;
        var p = comp.GetComponent(Type.GetType("LWS.TruckDockDiving.DockAudiencePresentation, Assembly-CSharp", true)); Assert.That((int)Get(p, "authoredVersion"), Is.EqualTo(4));
        var variants = ((Array)Get(p, "variants")).Cast<object>().ToArray(); Assert.That(variants.Length, Is.EqualTo(8)); Assert.That(variants.Select(v => (string)Get(v, "sourceName")).Distinct().Count(), Is.EqualTo(8));
        Assert.That(variants.Count(v => ((string)Get(v, "sourceName")).Contains("_f_")), Is.EqualTo(6)); Assert.That(variants.All(v => !((string)Get(v, "sourceName")).Contains("nude")), Is.True);
        Assert.That(variants.Sum(v => ((Array)Get(v, "fans")).Length), Is.EqualTo(440)); var crowd = Get(p, "crowd"); Assert.That(((Animator[])Get(crowd, "nearFans")).Length, Is.EqualTo(24));
        foreach (var v in variants) { Assert.That(Get(v, "idle"), Is.Not.Null); Assert.That(Get(v, "cheer"), Is.Not.Null); Assert.That(((Animator)Get(v, "animator")).avatar.isValid, Is.True); Assert.That(((Transform)Get(v, "root")).GetComponentsInChildren<Collider>(true).Length, Is.Zero); }
        Assert.That(GameObject.Find("UTS spectator crowd · render and animation only"), Is.Null, "Original crowd is retained inactive.");
        var dock = (Collider)Get(m, "dock"); Assert.That(dock.bounds.size, Is.EqualTo(new Vector3(7, 2.2f, 30))); Assert.That(dock.GetComponent<Renderer>().enabled, Is.False); var water = GameObject.Find("NWH water plane · URP pool surface"); Assert.That(water.GetComponent(Type.GetType("LWS.TruckFishing.FishingWaterFlow, Assembly-CSharp", true)), Is.Null); Assert.That(water.GetComponent<MeshFilter>().sharedMesh.vertexCount, Is.EqualTo(6369)); Assert.That(comp.GetComponent(Type.GetType("LWS.TruckDockDiving.DockPoolPresentation, Assembly-CSharp", true)), Is.Not.Null);
        var wall = comp.transform.Find("Finished Dock competition venue · owned artwork/Complete solid pool far wall"); Assert.That(wall.GetComponent<BoxCollider>(), Is.Not.Null); Assert.That(wall.GetComponent<Renderer>().bounds.min.z, Is.EqualTo(130).Within(.001f));
        var mesh = wall.GetComponent<MeshFilter>().sharedMesh; Assert.That(mesh.triangles.All(i => mesh.normals[i].y < .9f), Is.True, "Coping owns the top face; do not reintroduce coplanar faces.");
        Assert.That(UnityEngine.Object.FindObjectsByType<ReflectionProbe>(FindObjectsSortMode.None).Single().resolution, Is.EqualTo(128));
        Assert.That(comp.GetComponent(Type.GetType("LWS.TruckSports.SportsWaterInteraction, Assembly-CSharp", true)), Is.Not.Null);
        var pool=comp.GetComponent(Type.GetType("LWS.TruckDockDiving.DockPoolPresentation, Assembly-CSharp",true));Assert.That((int)Get(pool,"authoredVersion"),Is.EqualTo(4));Assert.That(Get(pool,"crownSheet"),Is.Not.Null);Assert.That(water.GetComponent<Renderer>().sharedMaterial.mainTextureScale,Is.EqualTo(new Vector2(3,16)));
        Assert.That(((MeshFilter)Get(pool,"crownSheet")).GetComponent<Renderer>().sharedMaterial.mainTexture,Is.Not.Null,"Broken-up water film reuses actual River Modeler alpha artwork.");
        var distances=UnityEngine.Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None).Where(t=>t.name.StartsWith("Distance ")).ToArray();Assert.That(distances.Length,Is.EqualTo(54));Assert.That(distances.All(t=>!t.GetComponent<Renderer>().enabled&&t.transform.Find("Depth-tested pool distance")!=null),Is.True,"Legacy always-on-top lettering must no longer draw through spectators.");
        Assert.That(((ParticleSystem)Get(m,"splash")).GetComponent<Renderer>().enabled,Is.False,"Original simulation remains, while its old billboard orb renderer is retired.");
        var splash = ((ParticleSystem)Get(m, "splash")).GetComponent<ParticleSystemRenderer>().sharedMaterial; Assert.That(splash.mainTexture, Is.Not.Null); Assert.That(splash.GetFloat("_Surface"), Is.EqualTo(1)); Assert.That(splash.GetFloat("_ZWrite"), Is.Zero);
        foreach (var truck in comp.GetComponentsInChildren<Transform>(true).Where(t => t.name == "Novelty truck spectator · render only")) { var renders = truck.GetComponentsInChildren<Renderer>(true); var bounds = renders[0].bounds; foreach (var r in renders.Skip(1)) bounds.Encapsulate(r.bounds); Assert.That(bounds.min.y, Is.EqualTo(.1f).Within(.03f)); Assert.That(bounds.size.y, Is.EqualTo(2.7f).Within(.03f)); }
        foreach (var anchor in comp.GetComponentsInChildren<Transform>(true).Where(t => t.name == "Owned timber approach decking" || t.name == "Owned approach safety railing"))
        {
            var renderers = anchor.GetComponentsInChildren<Renderer>(); var actual = renderers[0].bounds; foreach (var renderer in renderers.Skip(1)) actual.Encapsulate(renderer.bounds);
            var expected = anchor.name.Contains("decking") ? new Vector3(6.9f, .12f, 2.32f) : new Vector3(.16f, 1, 4.2f); Assert.That((actual.size - expected).magnitude, Is.LessThan(.025f), "Actual owned artwork must fill its intended dimensions: " + anchor.name);
        }
    }
    [Test] public void RepeatingVenueAuthoringPreservesSavedDesignerScene()
    {
        var before = File.ReadAllBytes(Scene); Type.GetType("LWS.TruckDockDiving.Editor.DockVenueRepairAuthoring, Assembly-CSharp-Editor", true).GetMethod("Upgrade").Invoke(null, null); Type.GetType("LWS.TruckDockDiving.Editor.DockPoolLifeAuthoring, Assembly-CSharp-Editor", true).GetMethod("Upgrade").Invoke(null, null); CollectionAssert.AreEqual(before, File.ReadAllBytes(Scene));
    }
}
#endif

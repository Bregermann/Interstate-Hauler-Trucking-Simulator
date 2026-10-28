#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using LWS.TruckYoga;

public sealed class YogaMeadowVisibilityTests
{
    const BindingFlags Any = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    static Type Sports(string name) => Type.GetType("LWS.TruckSports." + name + ", Assembly-CSharp", true);
    static object Get(object owner, string name) => owner.GetType().GetField(name, Any) is FieldInfo f ? f.GetValue(owner) : owner.GetType().GetProperty(name, Any).GetValue(owner);
    static object Call(object owner, string name, params object[] args) => owner.GetType().GetMethods(Any).Single(m => m.Name == name && m.GetParameters().Length == args.Length).Invoke(owner, args);
    static Bounds BoundsOf(Transform root) { var all = root.GetComponentsInChildren<Renderer>(true); Bounds b = all[0].bounds; foreach (var r in all.Skip(1)) b.Encapsulate(r.bounds); return b; }
    static Transform[] SceneTransforms(Component match) => match.gameObject.scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToArray();
    static void CheckGarden(Component match)
    {
        var all = SceneTransforms(match); var terrain = all.Select(t => t.GetComponent<Terrain>()).Single(t => t != null && t.name == "Vista grass mountain meadow");
        var rocks = all.Where(t => t.name.StartsWith("Owned Yoga V4 ", StringComparison.Ordinal)).ToArray();
        Assert.That(rocks.Length, Is.EqualTo(15));
        foreach (var rock in rocks)
        {
            var b = BoundsOf(rock); Assert.That(Mathf.Max(b.size.x, b.size.z), Is.EqualTo(12).Within(.004f), rock.name);
            float ground = terrain.SampleHeight(b.center) + terrain.transform.position.y;
            Assert.That(b.min.y, Is.EqualTo(ground).Within(.004f), rock.name + " is terrain-grounded.");
            Assert.That(rock.GetComponentsInChildren<Collider>(true), Is.Empty); Assert.That(rock.GetComponentsInChildren<Rigidbody>(true), Is.Empty);
            // The mats and teaching position have their original clear area.
            Assert.That(b.max.x < -24 || b.min.x > 24 || b.max.z < -10 || b.min.z > 20, Is.True, rock.name + " enters the actual yoga clearing.");
        }
        var cascade = all.Single(t => t.name == "Flowing River Modeler waterfall"); var water = BoundsOf(cascade);
        Assert.That(water.size.y, Is.EqualTo(8.1f).Within(.004f)); Assert.That(water.min.y, Is.EqualTo(terrain.SampleHeight(cascade.position) + terrain.transform.position.y + .05f).Within(.004f));
        var backdrop = rocks.Where(t => t.name.Contains("waterfall outcrop")).Select(BoundsOf).ToArray();
        Assert.That(backdrop.All(b => b.max.y >= water.max.y), Is.True, "The original flowing cascade cannot float above its natural-proportion rock backdrop.");
        var midpoint = new Vector3(water.center.x, water.max.y - .3f, water.center.z);
        Assert.That(backdrop.Any(b => b.Contains(midpoint)), Is.True, "The upper water actually overlaps the supporting backdrop envelope.");
    }
    static void CheckPrimarySightline(Component match)
    {
        var rocks = SceneTransforms(match).Where(t => t.name.StartsWith("Owned Yoga V4 ", StringComparison.Ordinal)).ToArray();
        foreach (Camera camera in (Camera[])Get(match, "cameras"))
        {
            if (!camera.enabled) continue;
            var framing = camera.GetComponent(Sports("SportsBroadcastFraming")); Assert.That(framing, Is.Not.Null);
            var primary = (Bounds)Get(framing, "PrimaryBounds"); Assert.That(primary.size.y, Is.GreaterThan(.5f));
            Vector3[] points = { primary.center, primary.center + Vector3.up * primary.extents.y * .45f, primary.center + Vector3.right * primary.extents.x * .35f, primary.center - Vector3.right * primary.extents.x * .35f };
            foreach (var p in points)
            {
                var delta = p - camera.transform.position; var ray = new Ray(camera.transform.position, delta.normalized);
                foreach (var rock in rocks)
                {
                    bool hit = BoundsOf(rock).IntersectRay(ray, out float distance) && distance < delta.magnitude;
                    Assert.That(hit, Is.False, rock.name + " occludes a central primary-body sightline (decorative meshes have no physics collider).");
                }
            }
            TestContext.WriteLine("YOGA_V4_SIGHTLINE actualCamera=" + camera.transform.position.ToString("R") + " actualPrimary=" + primary.ToString("R") + " projectedHeight=" + Get(framing, "ProjectedTruckHeight"));
        }
    }
    [UnityTest, Timeout(100000)]
    public IEnumerator SavedMeadowAndSevenActualAIPoseViewsRemainClearOfDecorativeRockOcclusion()
    {
        var lease = (IDisposable)Sports("SportsPrefs").GetMethod("IsolateValidation").Invoke(null, new object[] { "YogaGardenV4" + Guid.NewGuid().ToString("N") });
        Component match = null; var isolation = new GameObject("Yoga V4 native physical input isolation"); UnityEngine.Object.DontDestroyOnLoad(isolation); isolation.AddComponent(Sports("SportsProbeInputIsolation"));
        try
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/TruckYoga/Scenes/TruckYoga.unity", new LoadSceneParameters(LoadSceneMode.Single)); yield return null; yield return null;
            match = (Component)UnityEngine.Object.FindAnyObjectByType(Type.GetType("LWS.TruckYoga.YogaMatch, Assembly-CSharp", true)); Assert.That(match, Is.Not.Null);
            CheckGarden(match); Call(match, "AllBots"); Assert.That((bool)Call(match, "StartMatch"), Is.True); Time.timeScale = 4;
            var rules = (YogaRules)Get(match, "rules"); var seen = new HashSet<YogaPose>(); float deadline = Time.realtimeSinceStartup + 50;
            var capture = Sports("SportsControlledCapture").GetMethods().Single(m => m.Name == "Save" && m.GetParameters().Length == 5);
            while (!(bool)Get(match, "Finished") && Time.realtimeSinceStartup < deadline)
            {
                if (rules.accuracy[0] >= .86f && rules.hold[0] > .5f && seen.Add(rules.Pose))
                {
                    CheckPrimarySightline(match);
                    foreach (int width in new[] { 1280, 1920 }) capture.Invoke(null, new object[] { match, width, width == 1280 ? 720 : 1080, Path.GetFullPath("Logs/TruckSports/Impossible07/YogaMeadowV4/" + rules.Pose + "-" + width + "-controlled.png"), null });
                }
                yield return null;
            }
            Assert.That(seen.Count, Is.EqualTo(7)); Assert.That((bool)Get(match, "Finished"), Is.True); Assert.That(rules.completed.All(n => n == 7), Is.True);
            CheckGarden(match); TestContext.WriteLine("YOGA_V4_NATIVE bounded natural-proportion15decorative rocks, ground-connected cascade/backdrop, original clearing and7actual AI held poses/primary sightlines.14 controlled native captures; no Windows/human visual acceptance claimed.");
        }
        finally { Time.timeScale = 1; if (match != null) Call(match, "SetupScreen"); UnityEngine.Object.DestroyImmediate(isolation); lease.Dispose(); }
    }
}
#endif

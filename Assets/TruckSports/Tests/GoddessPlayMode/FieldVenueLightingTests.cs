#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class FieldVenueLightingTests
{
    const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    static Type Runtime(string name) => Type.GetType("LWS.TruckSports." + name + ", Assembly-CSharp", true);
    static object Get(object owner, string name) => owner.GetType().GetField(name, Any)?.GetValue(owner) ?? owner.GetType().GetProperty(name, Any).GetValue(owner);
    static object Call(object owner, string name, params object[] args) => owner.GetType().GetMethods(Any).Single(method => method.Name == name && method.GetParameters().Length == args.Length).Invoke(owner, args);
    [UnityTest, Timeout(120000)] public IEnumerator FreezeTagUsesSupportedSavedFixturesAndTheOriginalClock() => Verify("TruckFreezeTag", 6);
    [UnityTest, Timeout(120000)] public IEnumerator FroggerUsesSupportedSavedFixturesAndTheOriginalClock() => Verify("TruckFrogger", 0);
    [UnityTest, Timeout(120000)] public IEnumerator HungryHaulersUsesSupportedSavedFixturesAndTheOriginalClock() => Verify("TruckHungryHaulers", 6);
    [UnityTest, Timeout(120000)] public IEnumerator JoustingUsesSupportedSavedFixturesAndTheOriginalClock() => Verify("TruckJousting", 6);
    [UnityTest, Timeout(120000)] public IEnumerator LimboUsesSupportedSavedFixturesAndTheOriginalClock() => Verify("TruckLimbo", 4);
    [UnityTest, Timeout(120000)] public IEnumerator RedLightGreenLightUsesSupportedSavedFixturesAndTheOriginalClock() => Verify("TruckRedLightGreenLight", 8);
    [UnityTest, Timeout(120000)] public IEnumerator SheepHerdingUsesSupportedSavedFixturesAndTheOriginalClock() => Verify("TruckSheepHerding", 8);

    internal IEnumerator Verify(string name, int expected, bool diagnostic = false, bool clustered = false, bool photometry = false)
    {
        var prefs = (IDisposable)Runtime("SportsPrefs").GetMethod("IsolateValidation").Invoke(null, new object[] { "VenueLamps" + name });
        var isolation = new GameObject("Isolated saved venue-light acceptance"); UnityEngine.Object.DontDestroyOnLoad(isolation); isolation.AddComponent(Runtime("SportsProbeInputIsolation"));
        Component match = null;
        try
        {
            string folder = name;
            EditorSceneManager.LoadSceneInPlayMode("Assets/" + folder + "/Scenes/" + name + ".unity", new LoadSceneParameters(LoadSceneMode.Single)); yield return null; yield return null;
            match = UnityEngine.Object.FindObjectsByType(Runtime("SportsEventSession"), FindObjectsSortMode.None).Cast<Component>().Single(); Call(match, "AllBots");
            var clock = UnityEngine.Object.FindObjectsByType(Runtime("SportsMatchTime"), FindObjectsSortMode.None).Cast<Component>().Single();
            var lamps = (Light[])Get(clock, "nightLights");
            var authoredPowers = lamps.Select(light=>light.intensity).ToArray();
            if (name == "TruckFrogger") Assert.That(lamps.Length, Is.InRange(40,120)); else Assert.That(lamps.Length, Is.EqualTo(expected));
            var roots = SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(item => item.GetComponentsInChildren<Transform>(true)).Where(item => item.name == "Authored field venue night fixtures").ToArray();
            Assert.That(roots.Length, Is.EqualTo(name == "TruckFrogger" ? 5 : 1));
            foreach (var root in roots)
            {
                Assert.That(root.GetComponentsInChildren<Collider>(true), Is.Empty); Assert.That(root.GetComponentsInChildren<Rigidbody>(true), Is.Empty); Assert.That(root.GetComponentsInChildren<Joint>(true), Is.Empty); Assert.That(root.GetComponentsInChildren<MonoBehaviour>(true), Is.Empty);
                Assert.That(root.GetComponentsInChildren<MeshRenderer>(true).Length, Is.EqualTo(root.GetComponentsInChildren<Light>(true).Length), "Every fixture uses actual installed render geometry.");
            }
            Assert.That(lamps.All(light => roots.Any(root => light.transform.IsChildOf(root)) && light.type == LightType.Spot && light.shadows == LightShadows.None), Is.True);
            if (name == "TruckSheepHerding")
            {
                var ground = UnityEngine.Object.FindObjectsByType<MeshCollider>(FindObjectsSortMode.None).Single(item => item.name == "Continuous rolling collision-matched farm landscape"); Physics.SyncTransforms();
                foreach (var light in lamps)
                {
                    var mount = light.transform.parent.position;
                    Assert.That(ground.Raycast(new Ray(new Vector3(mount.x, ground.bounds.max.y+2, mount.z),Vector3.down),out var hit, ground.bounds.size.y+10), Is.True);
                    Assert.That(Mathf.Abs(hit.point.y-mount.y), Is.LessThan(.015f), "Farm fixture bases follow the actual saved uneven collider.");
                }
            }
            if (name == "TruckFrogger")
            {
                var world = (Component)Get(match,"world"); var courses = (Array)Get(world,"courses"); Assert.That(courses.Length, Is.EqualTo(5));
                foreach (var course in courses)
                {
                    var root = (Transform)Get(course,"root"); var owned = lamps.Where(light => light.transform.IsChildOf(root)).ToArray();Assert.That(owned.Length, Is.GreaterThanOrEqualTo(8));
                    var rows = owned.Select(light => light.transform.parent.position.z).Distinct().OrderBy(z=>z).ToArray();
                    Assert.That(rows.Zip(rows.Skip(1),(a,b)=>b-a).Max(), Is.LessThanOrEqualTo(110), "Avoid dark unlit gaps between supported roadside fixtures.");
                    foreach (var light in owned)
                    {
                        var point=light.transform.parent.position; Assert.That(Mathf.Abs(point.x-((Vector3)Get(course,"start")).x), Is.EqualTo(36).Within(.015f));
                        Assert.That(((Vector2[])Get(course,"water")).Any(water => point.z>water.x-8 && point.z<water.y+8), Is.False, "Roadside pole cannot float over a river.");
                        Assert.That(((Vector3[])Get(course,"gates")).Any(gate=>Mathf.Abs(point.z-gate.z)<12 || Mathf.Abs(point.z-gate.z-28)<12), Is.False, "Keep fixed render fixtures out of the actual transverse traffic/train corridors.");
                    }
                }
            }
            var catalog = Get(match.GetComponent(Runtime("SportsChaosWeather")), "presets"); var clear = ((Array)Get(catalog, "entries")).Cast<object>().Single(entry => (string)Get(entry, "displayName") == "Clear"); Call(catalog, "Select", Get(clear, "id"));
            var ids = lamps.Select(light => light.GetInstanceID()).ToArray();
            foreach (string choice in new[] { "Night", "Day", "Evening", "Morning", "Night" })
            {
                Call(match, "SetupScreen"); Runtime("SportsTimeOfDay").GetMethod("Select").Invoke(null, new object[] { Enum.Parse(Runtime("SportsTimeChoice"), choice) }); Assert.That(Call(match, "StartMatch"), Is.True);
                yield return new WaitForSecondsRealtime(.4f); bool on = choice == "Night" || choice == "Evening" || choice=="Morning" && (float)Get(clock,"morningLampMultiplier")>0;
                Assert.That(Get(clock, "Applied").ToString(), Is.EqualTo(choice)); Assert.That(lamps.All(light => on ? light.intensity > 0 : light.intensity == 0), Is.True);
                int revision = (int)Get(clock, "Revision"); Call(clock, "Configure", -812, Get(match, "MatchId")); Assert.That(Get(clock, "Revision"), Is.EqualTo(revision));
                Call(match, "Pause", 0, false); yield return null; Assert.That(lamps.All(light => on ? light.intensity > 0 : light.intensity == 0), Is.True); Call(match, "Resume");
                CollectionAssert.AreEqual(ids, lamps.Select(light => light.GetInstanceID()).ToArray());
                if(name=="TruckFrogger" && choice=="Morning")
                {
                    for(int i=0;i<lamps.Length;i++)Assert.That(lamps[i].intensity,Is.EqualTo(authoredPowers[i]*.65f).Within(.1f),"Explicit dawn gain lights the original dark road without changing the vendor sun.");
                    foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080)})Runtime("SportsControlledCapture").GetMethods().Single(method=>method.Name=="Save"&&method.GetParameters().Length==5).Invoke(null,new object[]{match,size.x,size.y,"Logs/TruckSports/PlaytestRepair/FieldVenueExposure/"+name+"-clear-morning-"+size.y+".png",null});
                }
            }
            // Let the ordinary countdown release before inspecting gameplay;
            // retain its original timing rather than overriding sport state.
            yield return new WaitForSecondsRealtime(4);
            Time.timeScale = 0; yield return new WaitForSecondsRealtime(1);
            var camera = ((Camera[])Get(match, "cameras")).First(item => item.enabled); Render(camera); var lit = Render(camera);
            foreach (var light in lamps) light.enabled = false;
            Color[] dark; try { dark = Render(camera); } finally { foreach (var light in lamps) light.enabled = true; }
            int brighter = lit.Zip(dark, (a, b) => a.maxColorComponent - b.maxColorComponent > .025f).Count(value => value);
            if (diagnostic)
            {
                var current = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
                var cap = current.GetType().GetProperty("maxAdditionalLightsCount");
                TestContext.WriteLine("DIAGNOSTIC " + name + " currentPipeline=" + current.name + " additionalCap=" + cap.GetValue(current) + " baselineBrighterPixels=" + brighter);
                foreach (var renderer in SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(item=>item.GetComponentsInChildren<MeshRenderer>(true)).Where(item=>item.name.StartsWith("Player crossing road") || item.name=="Continuous competition runway").Take(5))
                    TestContext.WriteLine("ROAD_DIAGNOSTIC " + renderer.name + " bounds=" + renderer.bounds + " layers=" + renderer.renderingLayerMask + " materials=" + string.Join(";",renderer.sharedMaterials.Select(material=>material.name+" shader="+material.shader.name)));
                var prior = QualitySettings.renderPipeline;
                var clone = UnityEngine.Object.Instantiate(current); clone.hideFlags = HideFlags.DontSave;
                ScriptableObject rendererClone = null;
                try
                {
                    cap.SetValue(clone, 8);
                    if(clustered)
                    {
                        var settings=new SerializedObject(clone);var first=settings.FindProperty("m_RendererDataList").GetArrayElementAtIndex(0);
                        rendererClone=UnityEngine.Object.Instantiate((ScriptableObject)first.objectReferenceValue);rendererClone.hideFlags=HideFlags.DontSave;
                        var mode=rendererClone.GetType().GetProperty("renderingMode");mode.SetValue(rendererClone,Enum.Parse(mode.PropertyType,"ForwardPlus"));
                        first.objectReferenceValue=rendererClone;settings.ApplyModifiedPropertiesWithoutUndo();
                        TestContext.WriteLine("DIAGNOSTIC temporaryRenderer="+mode.GetValue(rendererClone)+"; original renderer/features/assets retained.");
                    }
                    QualitySettings.renderPipeline = clone;
                    yield return null; yield return null;
                    TestContext.WriteLine("DIAGNOSTIC activeTemporaryPipeline=" + (UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline == clone) + " actualCap=" + cap.GetValue(UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline));
                    Render(camera); Render(camera); var expanded = Render(camera);
                    foreach(var light in lamps) light.enabled=false;
                    Color[] unlit; try { unlit=Render(camera); } finally { foreach(var light in lamps) light.enabled=true; }
                    int changed=expanded.Zip(unlit,(a,b)=>a.maxColorComponent-b.maxColorComponent>.025f).Count(value=>value);
                    TestContext.WriteLine("DIAGNOSTIC " + name + " temporaryRuntime"+(clustered?"Clustered":"Cap8")+"BrighterPixels=" + changed + "; cloned unsaved pipeline only, not production acceptance.");
                    Runtime("SportsControlledCapture").GetMethods().Single(method=>method.Name=="Save"&&method.GetParameters().Length==5).Invoke(null,new object[]{match,1280,720,"Logs/TruckSports/PlaytestRepair/FieldVenueLightingDiagnostics/"+name+(clustered?"-temporary-clustered":"-temporary-cap8")+"-720.png",null});
                }
                finally { QualitySettings.renderPipeline=prior; }
                yield return null; yield return null;
                UnityEngine.Object.DestroyImmediate(clone);
                if(rendererClone!=null) UnityEngine.Object.DestroyImmediate(rendererClone);
                yield break;
            }
            Assert.That(brighter, Is.GreaterThan(1000), name + " original gameplay camera must actually see the saved venue illumination.");
            if(name=="TruckFrogger" || name=="TruckRedLightGreenLight")
            {
                Assert.That(brighter, Is.GreaterThan(10000), "The long road surface must receive broad illumination, not just a few lit vehicle pixels.");
                var pipeline=UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
                Assert.That(pipeline.GetType().GetProperty("maxAdditionalLightsCount").GetValue(pipeline), Is.EqualTo(8));
                if(name=="TruckFrogger")
                {
                    var renderer=new SerializedObject(pipeline).FindProperty("m_RendererDataList").GetArrayElementAtIndex(0).objectReferenceValue;
                    Assert.That(renderer.GetType().GetProperty("renderingMode").GetValue(renderer).ToString(),Is.EqualTo("ForwardPlus"));
                    Assert.That(new SerializedObject(renderer).FindProperty("m_RendererFeatures").arraySize,Is.EqualTo(1),"Retain the installed Weather Maker feature in the local renderer.");
                }
            }
            if (name == "TruckFrogger")
            {
                var world = (Component)Get(match,"world"); var courses=(Array)Get(world,"courses");
                for (int i=0;i<courses.Length;i++)
                {
                    Call(world,"Prepare",i,1782);
                    for(int j=0;j<courses.Length;j++)
                    {
                        var root=(Transform)Get(courses.GetValue(j),"root");Assert.That(root.gameObject.activeSelf,Is.EqualTo(i==j));
                        Assert.That(lamps.Where(light=>light.transform.IsChildOf(root)).All(light=>light.gameObject.activeInHierarchy==(i==j)),Is.True,"Original course activation also gates all its lights; no new runtime toggle manager.");
                    }
                }
                Call(world,"Prepare",0,1782);
            }
            foreach (var size in new[] { new Vector2Int(1280, 720), new Vector2Int(1920, 1080) }) Runtime("SportsControlledCapture").GetMethods().Single(method => method.Name == "Save" && method.GetParameters().Length == 5).Invoke(null, new object[] { match, size.x, size.y, "Logs/TruckSports/PlaytestRepair/FieldVenueLighting/" + name + "-night-" + size.y + ".png", null });
            if(name=="TruckFrogger")
            {
                Call(match,"SetupScreen"); var entry=((Array)Get(catalog,"entries")).Cast<object>().Single(item=>(string)Get(item,"displayName")== (photometry ? "Blizzard" : "Snow"));Call(catalog,"Select",Get(entry,"id"));
                Assert.That(Call(match,"StartMatch"),Is.True);yield return new WaitForSecondsRealtime(5);Time.timeScale=0;
                var snow=match.GetComponent(Runtime("SportsSnowSurface"));Assert.That(snow,Is.Not.Null);
                Assert.That((float)Get(snow,"Coverage"),Is.GreaterThan(.5f));Assert.That((float)Get(snow,"MaximumRoadCoverage"),Is.GreaterThan(.5f));Assert.That((int)Get(snow,"depthRenderer"),Is.EqualTo(1),"Keep the original Weatherade depth/track renderer.");
                if(!photometry)
                {
                    Assert.That(Get(clock,"lampSnowSurface"),Is.SameAs(snow));
                    for(int i=0;i<lamps.Length;i++)Assert.That(lamps[i].intensity,Is.EqualTo(authoredPowers[i]*.15f).Within(.1f),"Reflective snow receives its explicit reduced venue power, including after rematch.");
                }
                if(photometry)
                {
                    var original=lamps.Select(light=>light.intensity).ToArray();
                    try
                    {
                        foreach(float gain in new[]{1f,.25f,.1f,.05f,.025f})
                        {
                            for(int i=0;i<lamps.Length;i++)lamps[i].intensity=original[i]*gain;
                            Render(camera);var pixels=Render(camera);
                            TestContext.WriteLine("SNOW_PHOTOMETRY gain="+gain+" clipped="+pixels.Count(p=>p.r>.98f&&p.g>.98f&&p.b>.98f)+" mean="+pixels.Average(p=>p.grayscale));
                            Runtime("SportsControlledCapture").GetMethods().Single(method=>method.Name=="Save"&&method.GetParameters().Length==5).Invoke(null,new object[]{match,1280,720,"Logs/TruckSports/PlaytestRepair/FieldSnowPhotometry/snow-gain-"+gain.ToString(System.Globalization.CultureInfo.InvariantCulture)+".png",null});
                        }
                    }
                    finally{for(int i=0;i<lamps.Length;i++)lamps[i].intensity=original[i];}
                    yield break;
                }
                Render(camera);var snowLit=Render(camera);foreach(var light in lamps)light.enabled=false;
                Color[] snowDark;try{snowDark=Render(camera);}finally{foreach(var light in lamps)light.enabled=true;}
                int snowBrighter=snowLit.Zip(snowDark,(a,b)=>a.maxColorComponent-b.maxColorComponent>.025f).Count(value=>value);
                Assert.That(snowBrighter,Is.GreaterThan(1000),"Actual snow-covered road shader must also receive the saved clustered venue lights.");
                foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080)})Runtime("SportsControlledCapture").GetMethods().Single(method=>method.Name=="Save"&&method.GetParameters().Length==5).Invoke(null,new object[]{match,size.x,size.y,"Logs/TruckSports/PlaytestRepair/FieldVenueLighting/"+name+"-snow-night-"+size.y+".png",null});
                TestContext.WriteLine("FROG_SNOW coverage="+Get(snow,"Coverage")+" roadCoverage="+Get(snow,"MaximumRoadCoverage")+" actualRoadContacts="+Get(snow,"RoadContacts")+" brighterPixels="+snowBrighter+"; original depth renderer1 retained. Snow lighting/coverage acceptance, not human snow driving or track-shape acceptance.");
            }
            TestContext.WriteLine(name + " brighterPixels=" + brighter + "/57600; saved purchased fixtures, real ground/course ownership, zero added physics, original clock, explicit time changes, pause and rematch identities passed. Native controlled rendering and AI-only match starts, not physical-device or full-match acceptance.");
        }
        finally { Time.timeScale = 1; if (match != null) Call(match, "SetupScreen"); UnityEngine.Object.DestroyImmediate(isolation); prefs.Dispose(); }
    }
    static Color[] Render(Camera camera)
    {
        var target = RenderTexture.GetTemporary(320, 180, 24, RenderTextureFormat.ARGB32); var prior = camera.targetTexture; var active = RenderTexture.active; Texture2D image = null;
        // A deliberate diagnostic pipeline reload can unload unused native
        // resources during Render. Allocate the readback only after it finishes.
        try { camera.targetTexture = target; camera.Render(); RenderTexture.active = target; image = new Texture2D(320, 180, TextureFormat.RGB24, false); image.ReadPixels(new Rect(0, 0, 320, 180), 0, 0); image.Apply(); return image.GetPixels(); }
        finally { camera.targetTexture = prior; RenderTexture.active = active; RenderTexture.ReleaseTemporary(target); if(image != null) UnityEngine.Object.DestroyImmediate(image); }
    }
}
public sealed class FieldVenueLightingDiagnosticsTests
{
    [UnityTest, Timeout(120000)] public IEnumerator InspectFroggerRuntimeLightCap() => new FieldVenueLightingTests().Verify("TruckFrogger",0,true);
    [UnityTest, Timeout(120000)] public IEnumerator InspectRedLightRuntimeLightCap() => new FieldVenueLightingTests().Verify("TruckRedLightGreenLight",8,true);
}
public sealed class FieldVenueClusteredDiagnosticsTests
{
    [UnityTest, Timeout(120000)] public IEnumerator InspectFroggerClusteredRoadLighting() => new FieldVenueLightingTests().Verify("TruckFrogger",0,true,true);
}
public sealed class FieldSnowPhotometryDiagnosticsTests
{
    [UnityTest, Timeout(120000)] public IEnumerator CompareOriginalSnowAtSeveralUnsavedLampPowers() => new FieldVenueLightingTests().Verify("TruckFrogger",0,false,false,true);
}
#endif

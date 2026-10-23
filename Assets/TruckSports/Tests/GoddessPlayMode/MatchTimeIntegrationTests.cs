#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class MatchTimeIntegrationTests
{
    const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    static Type Runtime(string name) => Type.GetType("LWS.TruckSports." + name + ", Assembly-CSharp", true);
    static object Get(object owner, string name) => owner.GetType().GetField(name, Any)?.GetValue(owner) ?? owner.GetType().GetProperty(name, Any).GetValue(owner);
    static object Call(object owner, string name, params object[] values) => owner.GetType().GetMethods(Any).Single(method => method.Name == name && method.GetParameters().Length == values.Length).Invoke(owner, values);
    static void Select(string name) => Runtime("SportsTimeOfDay").GetMethod("Select").Invoke(null, new object[] { Enum.Parse(Runtime("SportsTimeChoice"), name) });

    [UnityTest, Timeout(150000)]
    public IEnumerator RepresentativeScenesUseSavedTimeControlsAndStableMatchLighting()
    {
        var lease = (IDisposable)Runtime("SportsPrefs").GetMethod("IsolateValidation").Invoke(null, new object[] { "MatchTimeIntegration" });
        var isolation = new GameObject("Explicit saved time integration regression"); UnityEngine.Object.DontDestroyOnLoad(isolation); isolation.AddComponent(Runtime("SportsProbeInputIsolation"));
        var source = File.ReadAllBytes("Assets/TruckBaseJumping/Materials/Fixed daylight.asset"); Component match = null;
        try
        {
            foreach (string name in new[] { "TruckKeepaway", "TruckFieldGoal", "TruckSkyTow", "TruckTerrierRacing", "TruckSnake" })
            {
                EditorSceneManager.LoadSceneInPlayMode("Assets/" + name + "/Scenes/" + name + ".unity", new LoadSceneParameters(LoadSceneMode.Single)); yield return null; yield return null;
                match = UnityEngine.Object.FindObjectsByType(Runtime("SportsEventSession"), FindObjectsSortMode.None).Cast<Component>().Single(); Call(match, "AllBots");
                var weather = match.GetComponent(Runtime("SportsChaosWeather")); var time = Get(weather, "matchTime"); Assert.That(time, Is.Not.Null, name);
                var cycle = Get(time, "cycle"); var pickers = UnityEngine.Object.FindObjectsByType(Runtime("SportsWeatherPicker"), FindObjectsInactive.Include, FindObjectsSortMode.None).Cast<Component>().ToArray();
                Assert.That(RenderSettings.skybox.shader.name, Is.EqualTo("LWS/TruckSports/Weather Maker Viewport Skybox"), name + " uses actual vendor-computed sky for every viewport");
                Assert.That(pickers.Count(picker => (bool)Get(picker, "timeOfDay")), Is.EqualTo(1), name);
                var timePicker = pickers.Single(picker => (bool)Get(picker, "timeOfDay")); var weatherPicker = pickers.Single(picker => !(bool)Get(picker, "timeOfDay"));
                var timeRect = timePicker.GetComponent<RectTransform>(); var weatherRect = weatherPicker.GetComponent<RectTransform>();
                Assert.That(timeRect.anchoredPosition.x - weatherRect.anchoredPosition.x, Is.GreaterThan((timeRect.sizeDelta.x + weatherRect.sizeDelta.x) * .5f));
                {
                    var parent = timePicker.transform.parent;
                    var tmp = Type.GetType("TMPro.TMP_Text, Unity.TextMeshPro", true);
                    string heading = name == "TruckKeepaway" ? "TRUCK KEEPAWAY" : (string)Get(match, "Title");
                    var title = parent.GetComponentsInChildren(tmp, true).Cast<Component>().First(label => label.gameObject.activeInHierarchy && string.Equals((string)Get(label, "text"), heading, StringComparison.OrdinalIgnoreCase)).GetComponent<RectTransform>();
                    var headingBounds = RenderedTextRect(title.GetComponent(tmp));
                    Assert.That(headingBounds.Overlaps(WorldRect(timeRect)), Is.False, name + " time control must not overlap the heading.");
                    Assert.That(headingBounds.Overlaps(WorldRect(weatherRect)), Is.False, name + " weather control must not overlap the heading.");
                    foreach (string labelName in new[] { "Weather selection label", "Time of day selection label" })
                    {
                        var label = parent.Find(labelName).GetComponent<RectTransform>();
                        Assert.That(label.gameObject.activeInHierarchy, Is.True, name + " selector has a visible label.");
                        Assert.That(headingBounds.Overlaps(WorldRect(label)), Is.False, name + " selector label must not overlap the heading.");
                    }
                    if (name == "TruckKeepaway")
                    {
                        var legacy = parent.GetComponentsInChildren(Runtime("SportsMenuAction"), true).Cast<Component>().Single(action => (string)Get(action, "action") == "chaosweather");
                        Assert.That(legacy.gameObject.activeSelf, Is.False, "One authoritative weather selector; remove the obsolete misleading control from the layout.");
                    }
                }
                float dayAngle = 0;
                foreach (string choice in new[] { "Morning", "Day", "Evening", "Night" })
                {
                    Call(match, "SetupScreen"); Select(choice); Assert.That((bool)Call(match, "StartMatch"), Is.True);
                    var camera = ((Camera[])Get(match, "cameras")).First(view => view.enabled);
                    // Batch tests have no visible Game View. The vendor discovers the
                    // camera-specific celestial light during actual render callbacks.
                    Render(camera); yield return new WaitForSeconds(.3f); yield return null; yield return null; Render(camera);
                    Assert.That(Get(time, "Applied").ToString(), Is.EqualTo(choice), name); Assert.That((float)Get(cycle, "Speed"), Is.Zero); Assert.That((float)Get(cycle, "NightSpeed"), Is.Zero);
                    float seconds = (float)Get(cycle, "TimeOfDay"); Assert.That(seconds, Is.EqualTo((float)Runtime("SportsTimeOfDay").GetMethod("Seconds").Invoke(null, new[] { Enum.Parse(Runtime("SportsTimeChoice"), choice) })));
                    var fill = (Light)Get(time, "daylightFill"); Assert.That(fill.enabled, Is.False, "Fixed noon fill must not remain on at night.");
                    var lamps = (Light[])Get(time, "nightLights");
                    if (name == "TruckKeepaway" || name == "TruckFieldGoal")
                    {
                        Assert.That(lamps.Length, Is.EqualTo(4));
                        Assert.That(lamps.All(lamp => lamp.shadows == LightShadows.None && (choice == "Night" || choice == "Evening" ? lamp.intensity > 0 : lamp.intensity == 0)), Is.True, name + " authored night lamps activate without extra shadow maps");
                    }
                    var vendor = (Component)Get(weather, "vendor");
                    var manager = vendor.GetComponentsInChildren<MonoBehaviour>(true).Single(component => component.GetType().Name == "WeatherMakerLightManagerScript");
                    var celestial = manager.GetType().GetMethod("SunForCamera").Invoke(null, new object[] { camera });
                    Assert.That(celestial, Is.Not.Null, name + " actual rendered camera has a vendor sun");
                    var sun = (Light)Get(celestial, "Light");
                    float angle = sun.transform.eulerAngles.x; if (choice == "Day") dayAngle = angle;
                    if (choice == "Night") Assert.That(Mathf.Abs(Mathf.DeltaAngle(dayAngle, angle)), Is.GreaterThan(45), name + " vendor sun orientation must change");
                    TestContext.WriteLine(name + " " + choice + " vendorSun=" + angle + " intensity=" + sun.intensity + " ambient=" + RenderSettings.ambientLight + " profile=" + ((UnityEngine.Object)Get(cycle, "DayNightProfile")).name);
                    int revision = (int)Get(time, "Revision");
                    Call(time, "Configure", 123, Get(match, "MatchId")); yield return new WaitForSeconds(.1f);
                    Assert.That(Get(time, "Revision"), Is.EqualTo(revision), "Attempt reconfiguration must not reroll a match's time."); Assert.That((float)Get(cycle, "TimeOfDay"), Is.EqualTo(seconds));
                }
                Call(match, "SetupScreen"); Select("Random"); Assert.That((bool)Call(match, "StartMatch"), Is.True); yield return null;
                var applied = Get(time, "Applied"); Call(time, "Configure", -999, Get(match, "MatchId")); Assert.That(Get(time, "Applied"), Is.EqualTo(applied));
                match = null;
            }
            CollectionAssert.AreEqual(source, File.ReadAllBytes("Assets/TruckBaseJumping/Materials/Fixed daylight.asset"), "Vendor-backed source asset is never mutated by selected match time.");
        }
        finally { if (match != null) Call(match, "SetupScreen"); UnityEngine.Object.DestroyImmediate(isolation); lease.Dispose(); Time.timeScale = 1; }
    }
    static void Render(Camera camera)
    {
        var prior = camera.targetTexture; var target = RenderTexture.GetTemporary(320, 180, 24, RenderTextureFormat.ARGBHalf);
        try { camera.targetTexture = target; camera.Render(); }
        finally { camera.targetTexture = prior; RenderTexture.ReleaseTemporary(target); }
    }
    static Rect WorldRect(RectTransform rect)
    {
        var corners = new Vector3[4]; rect.GetWorldCorners(corners);
        return Rect.MinMaxRect(corners.Min(corner => corner.x), corners.Min(corner => corner.y), corners.Max(corner => corner.x), corners.Max(corner => corner.y));
    }
    static Rect RenderedTextRect(Component text)
    {
        // Heading containers include generous empty padding. Compare the real
        // rendered glyphs so a clear title is not mistaken for an overlap.
        text.GetType().GetMethod("ForceMeshUpdate", new[] { typeof(bool), typeof(bool) }).Invoke(text, new object[] { true, true });
        var bounds = (Bounds)Get(text, "textBounds");
        var points = new[] { bounds.min, bounds.max, new Vector3(bounds.min.x, bounds.max.y), new Vector3(bounds.max.x, bounds.min.y) }.Select(point => text.transform.TransformPoint(point)).ToArray();
        return Rect.MinMaxRect(points.Min(point => point.x), points.Min(point => point.y), points.Max(point => point.x), points.Max(point => point.y));
    }
}
#endif

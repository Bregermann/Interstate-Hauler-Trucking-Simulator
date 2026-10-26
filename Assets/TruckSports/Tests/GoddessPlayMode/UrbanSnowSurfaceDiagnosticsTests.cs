#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

// Read-only native integration audit. No profile, material or scene is saved.
public sealed class UrbanSnowSurfaceDiagnosticsTests
{
    const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    static Type Runtime(string name) => Type.GetType("LWS.TruckSports." + name + ", Assembly-CSharp", true);
    static object Get(object owner, string name) => owner.GetType().GetField(name, Any)?.GetValue(owner) ?? owner.GetType().GetProperty(name, Any).GetValue(owner);
    static object Call(object owner, string name, params object[] args) => owner.GetType().GetMethods(Any).Single(method => method.Name == name && method.GetParameters().Length == args.Length).Invoke(owner, args);
    [UnityTest, Timeout(180000)] public IEnumerator InspectSavedUrbanSnowBindingsAndMaterials()
    {
        var prefs = (IDisposable)Runtime("SportsPrefs").GetMethod("IsolateValidation").Invoke(null, new object[] { "UrbanSnowSurfaceAudit" });
        var isolation = new GameObject("Isolated urban snow audit"); UnityEngine.Object.DontDestroyOnLoad(isolation); isolation.AddComponent(Runtime("SportsProbeInputIsolation"));
        Component match = null; var report = new StringBuilder();
        try
        {
            foreach (string name in new[] { "TruckFareFight", "TruckSnake" })
            {
                EditorSceneManager.LoadSceneInPlayMode("Assets/" + name + "/Scenes/" + name + ".unity", new LoadSceneParameters(LoadSceneMode.Single)); yield return null; yield return null;
                match = UnityEngine.Object.FindObjectsByType(Runtime("SportsEventSession"), FindObjectsSortMode.None).Cast<Component>().Single(); Call(match, "AllBots");
                var weather = match.GetComponent(Runtime("SportsChaosWeather")); var catalog = Get(weather, "presets"); var snow = ((Array)Get(catalog, "entries")).Cast<object>().Single(item => (string)Get(item, "displayName") == "Snow"); Call(catalog, "Select", Get(snow, "id"));
                Runtime("SportsTimeOfDay").GetMethod("Select").Invoke(null, new object[] { Enum.Parse(Runtime("SportsTimeChoice"), "Morning") }); Assert.That(Call(match, "StartMatch"), Is.True); yield return new WaitForSeconds(6);
                var bridge = match.GetComponent(Runtime("SportsSnowSurface")); var surface = Get(bridge, "surface");
                report.AppendLine(name + " bridges=" + UnityEngine.Object.FindObjectsByType(Runtime("SportsSnowSurface"), FindObjectsSortMode.None).Length + " coverage=" + Get(bridge, "Coverage") + " falling=" + Get(bridge, "Falling") + " traceExposedRoads=" + Get(bridge, "traceExposedRoads") + " runtimeRoads=" + Get(bridge, "RuntimeRoadMaterialCount"));
                report.AppendLine(Get(surface, "Diagnostic").ToString()); report.AppendLine(Get(surface, "VisualDiagnostic").ToString());
                var renderers = UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(item => item.gameObject.scene == match.gameObject.scene && item.GetComponentInParent<Rigidbody>() == null).ToArray();
                foreach (var group in renderers.SelectMany(renderer => renderer.sharedMaterials).Where(material => material != null).GroupBy(material => material.shader.name)) report.AppendLine("SHADER " + group.Key + " renderSlots=" + group.Count());
                foreach (var renderer in renderers.Where(item => new[] { "road", "ground", "grass", "sidewalk", "plaza", "parking", "terrain" }.Any(token => item.name.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)).Take(65))
                {
                    var tags = renderer.GetComponentsInParent<MonoBehaviour>(true).Where(item => item != null && (item.GetType().Name == "LwsRoadSurface" || item.GetType().Name == "TruckTaxiSurface")).Select(item => item.GetType().Name);
                    report.AppendLine("SURFACE " + renderer.name + " active=" + renderer.gameObject.activeInHierarchy + " layer=" + renderer.gameObject.layer + " bounds=" + renderer.bounds + " tags=" + string.Join(",", tags) + " materials=" + string.Join(" | ", renderer.sharedMaterials.Select(material => material == null ? "MISSING" : material.name + ":" + material.shader.name)));
                }
                foreach (var camera in ((Camera[])Get(match, "cameras")).Where(item => item.enabled)) report.AppendLine("CAMERA " + camera.name + " at=" + camera.transform.position);
                Runtime("SportsControlledCapture").GetMethods().Single(method => method.Name == "Save" && method.GetParameters().Length == 5).Invoke(null, new object[] { match, 1280, 720, "Logs/TruckSports/PlaytestRepair/UrbanSnowSurfaceAudit/" + name + "-snow-original-720.png", null });
                Call(match, "SetupScreen"); match = null;
            }
            Directory.CreateDirectory("Logs/TruckSports/PlaytestRepair"); File.WriteAllText("Logs/TruckSports/PlaytestRepair/urban-snow-surface-native-audit.txt", report.ToString());
            TestContext.WriteLine("Saved-scene snow state and material audit completed; inspect the report and captures. Diagnostic execution is not snow acceptance.");
        }
        finally { Time.timeScale = 1; if (match != null) Call(match, "SetupScreen"); UnityEngine.Object.DestroyImmediate(isolation); prefs.Dispose(); }
    }
}
#endif

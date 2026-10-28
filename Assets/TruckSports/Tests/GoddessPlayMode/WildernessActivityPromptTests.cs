#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class WildernessActivityPromptTests
{
    const BindingFlags Any = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    static Type Runtime(string name) => Type.GetType("LWS.TruckSports." + name + ", Assembly-CSharp", true);
    static object Get(object o, string name) => o.GetType().GetField(name, Any)?.GetValue(o) ?? o.GetType().GetProperty(name, Any).GetValue(o);
    static object Call(object o, string method, params object[] args) => o.GetType().GetMethods(Any).Single(m => m.Name == method && m.GetParameters().Length == args.Length).Invoke(o, args);
    static float Light(Color c) => .2126f * Mathf.GammaToLinearSpace(c.r) + .7152f * Mathf.GammaToLinearSpace(c.g) + .0722f * Mathf.GammaToLinearSpace(c.b);
    static Rect Pixels(RectTransform rect, Camera camera) { var corners = new Vector3[4]; rect.GetWorldCorners(corners); var p = corners.Select(c => (Vector2)camera.WorldToScreenPoint(c)).ToArray(); return Rect.MinMaxRect(p.Min(v => v.x), p.Min(v => v.y), p.Max(v => v.x), p.Max(v => v.y)); }

    [UnityTest, Timeout(150000)] public IEnumerator ExistingWildernessPromptsRemainReadableOnSnowAndThroughRematch()
    {
        foreach (string mode in new[] { "TruckCamping", "TruckPetPals" })
        {
            IDisposable preferences = null; GameObject isolation = null; Component match = null;
            try
            {
                preferences = (IDisposable)Runtime("SportsPrefs").GetMethod("IsolateValidation").Invoke(null, new object[] { "WildernessPrompt" + Guid.NewGuid().ToString("N") });
                isolation = new GameObject("Wilderness prompt owned-input isolation"); UnityEngine.Object.DontDestroyOnLoad(isolation); isolation.AddComponent(Runtime("SportsProbeInputIsolation"));
                EditorSceneManager.LoadSceneInPlayMode("Assets/" + mode + "/Scenes/" + mode + ".unity", new LoadSceneParameters(LoadSceneMode.Single)); yield return null; yield return null;
                match = (Component)UnityEngine.Object.FindAnyObjectByType(Runtime("SportsEventSession")); Call(match, "AllBots");
                var weather = Get(match, "presentationWeather"); var catalog = Get(weather, "presets"); var preset = ((Array)Get(catalog, "entries")).Cast<object>().Single(p => (string)Get(p, "displayName") == "Heavy Snow");
                Call(catalog, "Select", Get(preset, "id")); Runtime("SportsTimeOfDay").GetMethod("Select").Invoke(null, new object[] { Enum.Parse(Runtime("SportsTimeChoice"), "Day") });
                for (int cycle = 0; cycle < 2; cycle++)
                {
                    Assert.That(Call(match, "StartMatch"), Is.True); yield return new WaitForSecondsRealtime(cycle == 0 ? 6 : 1);
                    var snow = Get(Get(weather, "precipitation"), "SnowScript");
                    Assert.That(Get(snow, "MistIntensityMultiplier"), Is.EqualTo(.15f));
                    Assert.That((float)Get(snow, "Intensity"), Is.GreaterThan(.5f), "The actual heavy-snow preset must remain active.");
                    Assert.That(((ParticleSystem)Get(snow, "ParticleSystem")).particleCount, Is.GreaterThan(0));
                    Assert.That(((ParticleSystem)Get(snow, "MistParticleSystem")).GetComponent<Renderer>().forceRenderingOff, Is.False, "Production retains real mist rather than the diagnostic hide switch.");
                    var menus = (Component)Get(match, "menus"); var hud = (GameObject)Get(menus, "hud"); var label = hud.transform.Find("Cooperative wilderness activity reminder").GetComponent(Type.GetType("TMPro.TMP_Text, Unity.TextMeshPro", true));
                    var plate = hud.transform.Find("Wilderness activity contrast plate"); Assert.That(plate, Is.Not.Null, "The original pale hint needs an actual authored contrast backing.");
                    var image = plate.GetComponent<UnityEngine.UI.Image>(); Assert.That(image, Is.Not.Null); Assert.That(image.raycastTarget, Is.False); Assert.That(plate.GetComponent(Runtime("SportsBroadcastPlate")), Is.Not.Null);
                    Assert.That(plate.GetSiblingIndex(), Is.LessThan(label.transform.GetSiblingIndex()), "Backing must render behind the actual existing hint.");
                    Assert.That(image.color.a, Is.GreaterThanOrEqualTo(.85f)); var composite = Color.Lerp(Color.white, image.color, image.color.a);
                    Assert.That((Light((Color)Get(label, "color")) + .05f) / (Light(composite) + .05f), Is.GreaterThanOrEqualTo(7), "Actual label/plate colors must retain high contrast even over white snow.");
                    foreach (var size in new[] { new Vector2Int(1280, 720), new Vector2Int(1920, 1080) })
                    {
                        Action<Camera> inspect = ui => { Canvas.ForceUpdateCanvases(); var text = Pixels(label.GetComponent<RectTransform>(), ui); var back = Pixels((RectTransform)plate, ui); Assert.That(back.Contains(text.min) && back.Contains(text.max), Is.True); Assert.That(back.xMin >= 0 && back.yMin >= 0 && back.xMax <= size.x && back.yMax <= size.y, Is.True); Assert.That(Get(label, "isTextOverflowing"), Is.False); };
                        Runtime("SportsControlledCapture").GetMethods().Single(m => m.Name == "Save" && m.GetParameters().Length == 6).Invoke(null, new object[] { menus.GetComponent<Canvas>(), Get(match, "cameras"), size.x, size.y, "Logs/TruckSports/PlaytestRepair/WildernessPromptNative/" + mode + "-cycle" + cycle + "-" + size.y + ".png", inspect });
                    }
                    Call(match, "SetupScreen"); yield return null;
                }
            }
            finally { Time.timeScale = 1; if (match != null) Call(match, "SetupScreen"); if (isolation != null) UnityEngine.Object.DestroyImmediate(isolation); preferences?.Dispose(); Runtime("SportsParticipantRegistry").GetMethod("ForgetCache").Invoke(null, null); }
        }
    }
}
#endif

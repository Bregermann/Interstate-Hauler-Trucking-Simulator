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

public sealed class OrthographicSkyProfileTests
{
    const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    static Type Runtime(string name) => Type.GetType("LWS.TruckSports." + name + ", Assembly-CSharp", true);
    static object Get(object owner, string name) => owner.GetType().GetField(name, Any)?.GetValue(owner) ?? owner.GetType().GetProperty(name, Any).GetValue(owner);
    static object Call(object owner, string name, params object[] values) => owner.GetType().GetMethods(Any).Single(method => method.Name == name && method.GetParameters().Length == values.Length).Invoke(owner, values);

    [UnityTest, Timeout(90000)]
    public IEnumerator OrthographicArenaRenderUsesItsOwnVendorSkyProfile()
    {
        var lease = (IDisposable)Runtime("SportsPrefs").GetMethod("IsolateValidation").Invoke(null, new object[] { "OrthographicSkyProfile" });
        var isolation = new GameObject("Explicit orthographic sky regression");
        UnityEngine.Object.DontDestroyOnLoad(isolation); isolation.AddComponent(Runtime("SportsProbeInputIsolation"));
        Component match = null;
        try
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/TruckKeepaway/Scenes/TruckKeepaway.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null; yield return null;
            match = UnityEngine.Object.FindObjectsByType(Runtime("SportsEventSession"), FindObjectsSortMode.None).Cast<Component>().Single();
            Call(match, "AllBots");
            Runtime("SportsTimeOfDay").GetMethod("Select").Invoke(null, new object[] { Enum.Parse(Runtime("SportsTimeChoice"), "Night") });
            Assert.That((bool)Call(match, "StartMatch"), Is.True);
            yield return new WaitForSeconds(.3f);
            var camera = (Camera)Get(Get(match, "broadcast"), "view");
            Assert.That(camera.orthographic, Is.True, "Exercise the saved arena lens, not a substitute perspective camera.");
            var weather = match.GetComponent(Runtime("SportsChaosWeather"));
            var vendor = (Component)Get(weather, "vendor");
            var sky = vendor.GetComponentsInChildren<MonoBehaviour>(true).Single(component => component.GetType().Name == "WeatherMakerSkySphereScript");
            var profile = Get(sky, "SkySphereProfile");
            Assert.That(profile, Is.Not.Null);
            Shader.SetGlobalFloat("_NightIntensity", -321);
            Shader.SetGlobalInt("_WeatherMakerSkyRenderType", -321);
            var target = RenderTexture.GetTemporary(320, 180, 24, RenderTextureFormat.ARGBHalf);
            var previous = camera.targetTexture;
            try { camera.targetTexture = target; camera.Render(); }
            finally { camera.targetTexture = previous; RenderTexture.ReleaseTemporary(target); }
            Assert.That(Shader.GetGlobalFloat("_NightIntensity"), Is.EqualTo((float)Get(profile, "NightIntensity")).Within(.0001f), "Actual arena rendering must upload its vendor sky state rather than inherit globals from the previous scene/lens.");
            Assert.That(Shader.GetGlobalInt("_WeatherMakerSkyRenderType"), Is.EqualTo(Convert.ToInt32(Get(profile, "SkyMode"))));
            var adapter = (Behaviour)match.GetComponent(Runtime("SportsOrthographicSky"));
            Assert.That(adapter, Is.Not.Null);
            foreach (bool enabled in new[] { false, true })
            {
                adapter.enabled = enabled; Shader.SetGlobalFloat("_NightIntensity", -321);
                var verification = RenderTexture.GetTemporary(320, 180, 24, RenderTextureFormat.ARGBHalf);
                try { camera.targetTexture = verification; camera.Render(); }
                finally { camera.targetTexture = previous; RenderTexture.ReleaseTemporary(verification); }
                Assert.That(Shader.GetGlobalFloat("_NightIntensity"), Is.EqualTo(enabled ? (float)Get(profile, "NightIntensity") : -321).Within(.0001f), "Disabled adapters unregister; re-enabled adapters restore exactly one vendor upload.");
            }
        }
        finally
        {
            if (match != null) Call(match, "SetupScreen");
            UnityEngine.Object.DestroyImmediate(isolation); lease.Dispose(); Time.timeScale = 1;
        }
    }
}
#endif

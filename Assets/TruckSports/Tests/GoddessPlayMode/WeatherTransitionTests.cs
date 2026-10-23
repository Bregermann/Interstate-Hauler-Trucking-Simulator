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

public sealed class WeatherTransitionTests
{
    const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    static Type Runtime(string name) => Type.GetType(name + ", Assembly-CSharp", true);
    static object Get(object owner, string name) => owner.GetType().GetField(name, Any)?.GetValue(owner) ?? owner.GetType().GetProperty(name, Any).GetValue(owner);
    static object Call(object owner, string name, params object[] values) => owner.GetType().GetMethods(Any).Single(method => method.Name == name && method.GetParameters().Length == values.Length).Invoke(owner, values);

    [UnityTest, Timeout(60000)]
    public IEnumerator SnowToHurricaneStopsSnowEmissionAndRetiresExistingFlakes()
    {
        var lease = (IDisposable)Runtime("LWS.TruckSports.SportsPrefs").GetMethod("IsolateValidation").Invoke(null, new object[] { "WeatherTransition" });
        var isolation = new GameObject("Explicit weather transition regression"); UnityEngine.Object.DontDestroyOnLoad(isolation);
        isolation.AddComponent(Runtime("LWS.TruckSports.SportsProbeInputIsolation")); Component match = null;
        try
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/TruckMinefield/Scenes/TruckMinefield.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null; yield return null;
            match = UnityEngine.Object.FindObjectsByType(Runtime("LWS.TruckMinefield.MinefieldMatch"), FindObjectsSortMode.None).Cast<Component>().Single();
            Call(match, "AllBots"); Assert.That((bool)Call(match, "StartMatch"), Is.True);
            var weather = Get(match, "weatherField"); var catalog = Get(weather, "presets"); var entries = ((Array)Get(catalog, "entries")).Cast<object>().ToArray();
            var snow = Get(Get(weather, "precipitation"), "SnowScript"); var rain = Get(Get(weather, "precipitation"), "RainScript");
            foreach (string name in new[] { "Blizzard", "Hurricane" })
            {
                Call(weather, "ConfigurePreset", entries.Single(item => (string)Get(item, "displayName") == name), 45671);
                foreach (float wait in new[] { .35f, .65f, 2f, 6f })
                {
                    yield return new WaitForSeconds(wait);
                    var particles = ((Component)snow).GetComponentsInChildren<ParticleSystem>(true);
                    TestContext.WriteLine(name + " after +" + wait + "s snowIntensity=" + Get(snow, "Intensity") + " rainIntensity=" + Get(rain, "Intensity") + " snowParticles=" + particles.Sum(system => system.particleCount) + " lifetimes=" + string.Join(",", particles.Select(system => system.main.startLifetime.constantMax)));
                }
                if (name == "Blizzard") Assert.That((float)Get(snow, "Intensity"), Is.GreaterThan(.1f));
            }
            Assert.That((float)Get(snow, "Intensity"), Is.LessThan(.001f), "The previous precipitation must actually stop emitting.");
            Assert.That((float)Get(rain, "Intensity"), Is.GreaterThan(.1f));
            Assert.That(((Component)snow).GetComponentsInChildren<ParticleSystem>(true).Sum(system => system.particleCount), Is.Zero, "Old flakes must finish within the observed bounded transition.");
        }
        finally { if (match != null) Call(match, "SetupScreen"); UnityEngine.Object.DestroyImmediate(isolation); lease.Dispose(); Time.timeScale = 1; }
    }
}
#endif

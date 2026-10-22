using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class CheeseLayeredExplosionPoolTests
{
    [UnityTest] public IEnumerator ExistingPoolPlaysAndClearsOwnedLayersWithoutStartupBurstsOrNewInstances()
    {
#if UNITY_EDITOR
        yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/TruckCheeseRolling/Scenes/TruckCheeseRolling.unity", new LoadSceneParameters(LoadSceneMode.Single));
#else
        yield return SceneManager.LoadSceneAsync("TruckCheeseRolling");
#endif
        yield return null;
        var type = Type.GetType("LWS.TruckCheeseRolling.CheeseMatch, Assembly-CSharp", true); var match = UnityEngine.Object.FindObjectsByType(type, FindObjectsSortMode.None).Single(); var roots = (ParticleSystem[])type.GetField("explosions").GetValue(match);
        var adapter = Type.GetType("LWS.TruckCheeseRolling.CheeseExplosionPresentation, Assembly-CSharp", true); var visuals = roots.Select(p => (GameObject)adapter.GetProperty("visual").GetValue(p.GetComponent(adapter))).ToArray(); Assert.That(visuals.All(v => v != null), Is.True); var ids = visuals.Select(v => v.GetInstanceID()).ToArray();
        Assert.That(visuals.All(v => v.GetComponentsInChildren<ParticleSystem>(true).All(p => p.particleCount == 0 && !p.isPlaying)), Is.True, "No cosmetic blast at scene load.");
        Assert.That(visuals.All(v => v.GetComponentsInChildren<Light>(true).Length == 0), Is.True, "Legacy example lights must not remain permanently active after their demo fade script is stripped.");
        for (int attempt = 0; attempt < 2; attempt++)
        {
            roots[0].Play(true); yield return new WaitForSeconds(.4f); int count = visuals[0].GetComponentsInChildren<ParticleSystem>(true).Sum(p => p.particleCount); Assert.That(count, Is.GreaterThan(0)); Assert.That(roots[0].main.maxParticles, Is.EqualTo(180));
            roots[0].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); Assert.That(visuals[0].GetComponentsInChildren<ParticleSystem>(true).All(p => p.particleCount == 0), Is.True);
            Assert.That(visuals.Select(v => v.GetInstanceID()).ToArray(), Is.EqualTo(ids)); Debug.Log("CHEESE_OWNED_VFX_POOL actual particles=" + count + " attempt=" + attempt + " original root settings and instance IDs retained.");
        }
        Assert.That((int)type.GetProperty("ExplosionCount").GetValue(match), Is.Zero, "Controlled visual fixture cannot award damage or trigger gameplay explosions.");
    }
}

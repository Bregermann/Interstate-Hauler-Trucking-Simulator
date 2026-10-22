#if UNITY_EDITOR
using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

public sealed class CheeseLayeredExplosionReferenceTests
{
    [Test] public void OwnedLayeredVfxHasBoundedParticlesAndNoGameplayComponents()
    {
        var prefab = Resources.Load<GameObject>("CheeseOwnedExplosion"); Assert.That(prefab, Is.Not.Null, "Run the licensed VFX intake/authoring before building.");
        var systems = prefab.GetComponentsInChildren<ParticleSystem>(true); Assert.That(systems.Length, Is.GreaterThanOrEqualTo(3)); Assert.That(systems.Sum(s => s.main.maxParticles), Is.LessThanOrEqualTo(800));
        Assert.That(prefab.GetComponentsInChildren<MonoBehaviour>(true).Length, Is.Zero); Assert.That(prefab.GetComponentsInChildren<Collider>(true).Length, Is.Zero); Assert.That(prefab.GetComponentsInChildren<Rigidbody>(true).Length, Is.Zero); Assert.That(prefab.GetComponentsInChildren<AudioSource>(true).Length, Is.Zero);
        foreach (var system in systems) { Assert.That(system.main.loop, Is.False); Assert.That(system.main.playOnAwake, Is.False); Assert.That(system.main.stopAction, Is.EqualTo(ParticleSystemStopAction.None)); foreach (var material in system.GetComponent<ParticleSystemRenderer>().sharedMaterials) Assert.That(material.shader.name, Is.EqualTo("Universal Render Pipeline/Particles/Unlit")); }
        EditorSceneManager.OpenScene("Assets/TruckCheeseRolling/Scenes/TruckCheeseRolling.unity"); var type = Type.GetType("LWS.TruckCheeseRolling.CheeseMatch, Assembly-CSharp", true); var match = UnityEngine.Object.FindObjectsByType(type, FindObjectsSortMode.None).Single(); var roots = (ParticleSystem[])type.GetField("explosions").GetValue(match); Assert.That(roots.Length, Is.EqualTo(8));
        foreach (var root in roots) { Assert.That(root.GetComponent(Type.GetType("LWS.TruckCheeseRolling.CheeseExplosionPresentation, Assembly-CSharp", true)), Is.Not.Null); Assert.That(root.main.maxParticles, Is.EqualTo(180), "Original root simulation and its recovery-safety lifetime stay intact."); }
    }
}
#endif

#if UNITY_EDITOR
using System;
using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class DragonPrefabContractTests
{
    static object Field(object instance, string name) => instance.GetType().GetField(name).GetValue(instance);
    [TestCase("dragon-inferno")]
    [TestCase("dragon-fire")]
    [TestCase("dragon-spark")]
    public void ConfiguredVendorStatesHaveSerializedLocalCacheBeforeAwake(string id)
    {
        string path = "Assets/TruckDragonKingdom/Prefabs/Dragons/" + id + "/" + id + " Dragon Animal.prefab";
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path); Assert.That(prefab, Is.Not.Null);
        var type = Type.GetType("LWS.TruckDragonKingdom.DragonAnimalMotor, Assembly-CSharp", true);
        var motor = prefab.GetComponentInChildren(type, true); Assert.That(motor, Is.Not.Null);
        Assert.That(Field(motor, "configuredSpeciesId"), Is.EqualTo(id));
        var animal = Field(motor, "animal"); var states = (IList)Field(animal, "states"); var cache = (IList)Field(animal, "states_C");
        Assert.That((bool)Field(animal, "CloneStates"), Is.True);
        Assert.That(states.Count, Is.GreaterThanOrEqualTo(3));
        Assert.That(cache.Count, Is.EqualTo(states.Count), "The real vendor Awake indexes this list before calling AwakeState.");
        for (int i = 0; i < states.Count; i++)
        {
            Assert.That(states[i], Is.Not.Null); Assert.That(Field(cache[i], "state"), Is.SameAs(states[i]));
            Assert.That((bool)Field(cache[i], "active"), Is.True);
        }
        var animator = (Animator)Field(motor, "animator");
        Assert.That(animator.avatar != null && animator.avatar.isValid && !animator.avatar.isHuman, Is.True);
        Assert.That(animator.runtimeAnimatorController, Is.Not.Null);
        Assert.That(prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length, Is.GreaterThan(0));
    }
}
#endif

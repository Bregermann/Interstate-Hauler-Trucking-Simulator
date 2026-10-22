#if UNITY_EDITOR
using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public sealed class CheeseAnimatedCatReferenceTests
{
    static object Field(object t, string n) => t.GetType().GetField(n).GetValue(t);
    [Test] public void FourOriginalCatPropsRetainTheirCollidersAndUseRealOwnedAnimations()
    {
        EditorSceneManager.OpenScene("Assets/TruckCheeseRolling/Scenes/TruckCheeseRolling.unity");
        var scene = UnityEngine.Object.FindObjectsByType(Type.GetType("LWS.TruckCheeseRolling.CheeseScenery, Assembly-CSharp", true), FindObjectsSortMode.None).Single();
        var cats = ((Transform[])Field(scene, "props")).Where(t => t.name.IndexOf("cat", StringComparison.OrdinalIgnoreCase) >= 0).ToArray(); Assert.That(cats.Length, Is.EqualTo(4));
        var component = Type.GetType("LWS.TruckCheeseRolling.CheeseCatPresentation, Assembly-CSharp", true);
        foreach (var cat in cats)
        {
            Assert.That(cat.GetComponentsInChildren<Collider>(true).Length, Is.EqualTo(1)); Assert.That(cat.GetComponent<CapsuleCollider>(), Is.Not.Null); Assert.That(cat.GetComponentsInChildren<Rigidbody>(true).Length, Is.Zero);
            Assert.That(cat.Find("Vendor model visual").gameObject.activeSelf, Is.False, "Preserve the original baked visual instead of deleting it.");
            var animation = cat.GetComponent(component); Assert.That(animation, Is.Not.Null); var animator = (Animator)Field(animation, "animator");
            Assert.That(animator.avatar.isValid && !animator.avatar.isHuman, Is.True); Assert.That(animator.applyRootMotion, Is.False); Assert.That(animator.runtimeAnimatorController, Is.Not.Null);
            Assert.That(animator.runtimeAnimatorController.animationClips.Distinct().Count(), Is.EqualTo(4));
            foreach (var clip in animator.runtimeAnimatorController.animationClips.Distinct()) { Assert.That(clip.length, Is.GreaterThan(.5f)); Assert.That(AnimationUtility.GetAnimationEvents(clip).Length, Is.Zero); Assert.That(AssetDatabase.GetAssetPath(clip).StartsWith("Assets/TruckCheeseRolling/PrivateLicensedAnimations/"), Is.True); }
            Assert.That(animator.GetComponentsInChildren<SkinnedMeshRenderer>(true).Count(r => r.enabled), Is.EqualTo(5));
        }
    }
}
#endif

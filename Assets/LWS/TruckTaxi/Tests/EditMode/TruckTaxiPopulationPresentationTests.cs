using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;

namespace LWS.TruckTaxi.Tests
{
    public sealed class TruckTaxiPopulationPresentationTests
    {
        [Test]
        public void PresentationDefaultsKeepMeasuredPopulationTargets()
        {
            var profile = ScriptableObject.CreateInstance<TruckTaxiPopulationProfile>();
            try
            {
                Assert.IsTrue(profile.optimizePresentation);
                Assert.AreEqual(144, profile.PedestrianTarget(12));
                Assert.AreEqual(60, profile.TrafficTarget(12));
                profile.optimizePresentation = false;
                Assert.AreEqual(144, profile.PedestrianTarget(12));
                Assert.AreEqual(60, profile.TrafficTarget(12));
            }
            finally { Object.DestroyImmediate(profile); }
        }

        [Test]
        public void NativeLodAdjustmentKeepsEveryMeshAndOriginalVisibilityRange()
        {
            var renderers = new Renderer[0];
            var original = new[] { new LOD(.5f, renderers), new LOD(.3f, renderers), new LOD(.135f, renderers), new LOD(.045f, renderers), new LOD(.009f, renderers) };
            var adjusted = TruckTaxiPopulationPresentation.AdjustedLods(original, 1.75f);
            Assert.AreEqual(original.Length, adjusted.Length);
            Assert.AreEqual(.875f, adjusted[0].screenRelativeTransitionHeight, .0001f);
            Assert.AreEqual(original[4].screenRelativeTransitionHeight, adjusted[4].screenRelativeTransitionHeight);
            Assert.AreEqual(.5f, original[0].screenRelativeTransitionHeight, "Do not mutate the restoration snapshot.");
            for (int i = 0; i < adjusted.Length; i++)
            {
                Assert.AreSame(original[i].renderers, adjusted[i].renderers);
                if (i > 0) Assert.Less(adjusted[i].screenRelativeTransitionHeight, adjusted[i - 1].screenRelativeTransitionHeight);
            }
            Assert.AreEqual(.5f, TruckTaxiPopulationPresentation.AdjustedLods(original, float.NaN)[0].screenRelativeTransitionHeight);
        }

        [Test]
        public void ShadowAndSkinningDistanceHasHysteresisAndAnOffSetting()
        {
            Assert.IsFalse(TruckTaxiPopulationPresentation.BeyondDistance(59 * 59, 60, 8, false));
            Assert.IsTrue(TruckTaxiPopulationPresentation.BeyondDistance(61 * 61, 60, 8, false));
            Assert.IsTrue(TruckTaxiPopulationPresentation.BeyondDistance(55 * 55, 60, 8, true));
            Assert.IsFalse(TruckTaxiPopulationPresentation.BeyondDistance(51 * 51, 60, 8, true));
            Assert.IsFalse(TruckTaxiPopulationPresentation.BeyondDistance(99999, 0, 8, true));
        }

        [Test]
        public void DistantPresentationKeepsSimulationAndRestoresAuthoredSettings()
        {
            var root = new GameObject("Population presentation fixture");
            var profile = ScriptableObject.CreateInstance<TruckTaxiPopulationProfile>();
            try
            {
                var animator = root.AddComponent<Animator>(); animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
                var skin = root.AddComponent<SkinnedMeshRenderer>();
                skin.quality = SkinQuality.Bone4; skin.skinnedMotionVectors = true;
                skin.shadowCastingMode = ShadowCastingMode.TwoSided;
                var body = root.AddComponent<Rigidbody>(); body.isKinematic = true;
                var collider = root.AddComponent<CapsuleCollider>();
                var presentation = new TruckTaxiPopulationPresentation(root, true);
                presentation.Refresh(profile, Vector3.forward * 200);
                Assert.IsTrue(presentation.ShadowsReduced); Assert.IsTrue(presentation.SkinningReduced);
                Assert.AreEqual(AnimatorCullingMode.CullCompletely, animator.cullingMode);
                Assert.AreEqual(SkinQuality.Bone2, skin.quality);
                Assert.IsFalse(skin.skinnedMotionVectors);
                Assert.AreEqual(ShadowCastingMode.Off, skin.shadowCastingMode);
                Assert.IsTrue(root.activeSelf); Assert.IsTrue(skin.enabled); Assert.IsTrue(collider.enabled); Assert.IsTrue(body.isKinematic);
                Assert.AreEqual(Vector3.zero, root.transform.position);
                // A struck pedestrian's disabled animator must never be restarted by a presentation refresh.
                animator.enabled = false;
                presentation.Refresh(profile, Vector3.zero);
                Assert.IsFalse(animator.enabled);
                Assert.AreEqual(SkinQuality.Bone4, skin.quality);
                Assert.AreEqual(ShadowCastingMode.TwoSided, skin.shadowCastingMode);
                profile.optimizePresentation = false;
                presentation.Refresh(profile, Vector3.forward * 200);
                Assert.IsFalse(presentation.Applied);
                Assert.AreEqual(AnimatorCullingMode.CullUpdateTransforms, animator.cullingMode);
                Assert.AreEqual(SkinQuality.Bone4, skin.quality); Assert.IsTrue(skin.skinnedMotionVectors);
                Assert.AreEqual(ShadowCastingMode.TwoSided, skin.shadowCastingMode);
                presentation.Restore(); // Idempotent disable/teardown.
            }
            finally { Object.DestroyImmediate(root); Object.DestroyImmediate(profile); }
        }

        [Test]
        public void RootMotionAndAuthoredSingleBoneQualityAreNotOverridden()
        {
            var root = new GameObject("Root-motion population fixture");
            var profile = ScriptableObject.CreateInstance<TruckTaxiPopulationProfile>();
            try
            {
                var animator = root.AddComponent<Animator>(); animator.applyRootMotion = true;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                var skin = root.AddComponent<SkinnedMeshRenderer>(); skin.quality = SkinQuality.Bone1;
                skin.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
                var presentation = new TruckTaxiPopulationPresentation(root, true);
                presentation.Refresh(profile, Vector3.forward * 200);
                Assert.AreEqual(AnimatorCullingMode.AlwaysAnimate, animator.cullingMode);
                Assert.AreEqual(SkinQuality.Bone1, skin.quality);
                Assert.AreEqual(ShadowCastingMode.ShadowsOnly, skin.shadowCastingMode);
                presentation.Restore();
            }
            finally { Object.DestroyImmediate(root); Object.DestroyImmediate(profile); }
        }
    }
}

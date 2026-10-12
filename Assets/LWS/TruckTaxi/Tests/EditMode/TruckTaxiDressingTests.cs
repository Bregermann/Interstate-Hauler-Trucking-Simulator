using System;
using NUnit.Framework;
using UnityEngine;

namespace LWS.TruckTaxi.Tests
{
    public sealed class TruckTaxiDressingTests
    {
        [Test] public void AllEightContextsExist()
        { Assert.AreEqual(8, Enum.GetValues(typeof(TruckTaxiDressingContext)).Length); }

        [Test]
        public void AuthoredPoolsHaveReviewedBoundsAndSoftClutter()
        {
            foreach (TruckTaxiDressingContext context in Enum.GetValues(typeof(TruckTaxiDressingContext)))
            {
                var profile = UnityEditor.AssetDatabase.LoadAssetAtPath<TruckTaxiDressingProfile>(
                    "Assets/LWS/TruckTaxi/Authoring/Dressing/" + context + ".asset");
                Assert.IsNotNull(profile, context.ToString());
                Assert.IsTrue(profile.Validate(out var reason), context + ": " + reason);
                using var serialized = new UnityEditor.SerializedObject(profile);
                var entries = serialized.FindProperty("prefabs");
                for (int index = 0; index < entries.arraySize; index++)
                {
                    var prefab = (GameObject)entries.GetArrayElementAtIndex(index).FindPropertyRelative("prefab").objectReferenceValue;
                    Assert.IsTrue(TruckTaxiDressingProfile.TryPrefabBounds(prefab, out var bounds));
                    Assert.Greater(bounds.size.sqrMagnitude, 0);
                    Assert.IsEmpty(prefab.GetComponentsInChildren<Light>(true));
                    if (context == TruckTaxiDressingContext.Forest || context == TruckTaxiDressingContext.Suburb ||
                        context == TruckTaxiDressingContext.Highway) continue;
                    var soft = prefab.GetComponent<TruckTaxiBreakawayProp>();
                    Assert.IsNotNull(soft, prefab.name);
                    Assert.IsFalse(soft.PreservesAuthoredPhysics, prefab.name);
                    Assert.IsTrue(soft.ValidateAuthoring(out reason), reason);
                }
            }
        }

        [Test] public void StableSeedDoesNotConsumeUnityRandomState()
        {
            var state = UnityEngine.Random.state;
            int first = TruckTaxiDressingProfile.StableSeed(12345, "dressing.highway.a");
            Assert.AreEqual(960788533, first, "Fixed cross-process hash contract; not only repeatability within this process.");
            for (int i = 0; i < 10; i++) Assert.AreEqual(first, TruckTaxiDressingProfile.StableSeed(12345, "dressing.highway.a"));
            Assert.AreEqual(state, UnityEngine.Random.state);
            Assert.AreNotEqual(first, TruckTaxiDressingProfile.StableSeed(12345, "dressing.highway.b"));
            Assert.AreNotEqual(first, TruckTaxiDressingProfile.StableSeed(12346, "dressing.highway.a"));
            Assert.Throws<ArgumentException>(() => TruckTaxiDressingProfile.StableSeed(1, ""));
        }

        [Test] public void EmptyContextProfileFailsClosed()
        {
            var profile = ScriptableObject.CreateInstance<TruckTaxiDressingProfile>();
            try { Assert.IsFalse(profile.Validate(out _)); }
            finally { UnityEngine.Object.DestroyImmediate(profile); }
        }

        [Test] public void FootprintNotOnlyCenterMustClearRoadAndPadding()
        {
            var road = TruckTaxiDressingBinding.RoadEnvelope(Vector3.zero, Vector3.forward * 100, 4);
            var footprint = new Bounds(new Vector3(6, 0, 50), new Vector3(5, 2, 2));
            Assert.IsTrue(TruckTaxiDressingBinding.IntersectsExclusion(footprint, new[] { road }, 0));
            footprint.center = new Vector3(8, 0, 50);
            Assert.IsFalse(TruckTaxiDressingBinding.IntersectsExclusion(footprint, new[] { road }, 0));
            Assert.IsTrue(TruckTaxiDressingBinding.IntersectsExclusion(footprint, new[] { road }, 2));
        }

        [Test] public void FinalModifierPoseIsUsedAndInvalidGeometryFailsClosed()
        {
            var entrance = new Bounds(new Vector3(10, 1, 0), new Vector3(2, 2, 3));
            var original = new Bounds(Vector3.zero, Vector3.one);
            Assert.IsFalse(TruckTaxiDressingBinding.IntersectsExclusion(original, new[] { entrance }, 0));
            original.center = entrance.center;
            Assert.IsTrue(TruckTaxiDressingBinding.IntersectsExclusion(original, new[] { entrance }, 0));
            original.center = new Vector3(float.NaN, 0, 0);
            Assert.IsTrue(TruckTaxiDressingBinding.IntersectsExclusion(original, new[] { entrance }, 0));
        }

        [Test] public void ExclusionOrderingDoesNotChangeDecision()
        {
            var bay = new Bounds(Vector3.zero, new Vector3(4, 5, 12));
            var route = new Bounds(Vector3.forward * 20, Vector3.one * 5);
            var prop = new Bounds(Vector3.zero, Vector3.one);
            Assert.AreEqual(TruckTaxiDressingBinding.IntersectsExclusion(prop, new[] { bay, route }, 1),
                TruckTaxiDressingBinding.IntersectsExclusion(prop, new[] { route, bay }, 1));
        }

        [Test] public void InvalidExclusionCannotSilentlyAuthorizePlacement()
        {
            var invalid = new Bounds(new Vector3(float.NaN, 0, 0), Vector3.one);
            Assert.IsTrue(TruckTaxiDressingBinding.IntersectsExclusion(new Bounds(Vector3.zero, Vector3.one), new[] { invalid }, 0));
            Assert.Throws<ArgumentException>(() => TruckTaxiDressingBinding.RoadEnvelope(Vector3.zero, Vector3.one, float.NaN));
        }
    }
}

using System;
using NUnit.Framework;
using UnityEngine;

namespace LWS.TruckTaxi.Tests
{
    [TestFixture, Category("TaxiCareer")]
    public sealed class TruckTaxiDriverAppearanceTests
    {
        private TruckTaxiConfiguration configuration;
        [SetUp] public void Setup() => configuration = ScriptableObject.CreateInstance<TruckTaxiConfiguration>();
        [TearDown] public void Cleanup() => UnityEngine.Object.DestroyImmediate(configuration);
        private TruckTaxiSession Session(bool initialize = true)
        {
            var session = new TruckTaxiSession(configuration, Array.Empty<TruckTaxiRideLocation>(), 33);
            session.StartShift();
            Assert.IsTrue(session.BindCareerRun("profile.appearance.test", out var reason), reason);
            if (initialize) Assert.IsTrue(session.TryInitializeCareer("Adult Driver", "truck.current", out reason), reason);
            return session;
        }
        private static TruckTaxiDriverAppearanceData Custom() => new TruckTaxiDriverAppearanceData {
            shirt = new Color(.7f,.2f,.3f), pantsAndCap = new Color(.2f,.3f,.4f),
            trim = new Color(.8f,.7f,.2f), skin = new Color(.4f,.25f,.16f), capVisible = false, chestShape = .75f };

        [Test]
        public void AppearanceRoundTripsInsideExistingSessionSnapshot()
        {
            var source = Session(); var expected = Custom(); int events = 0;
            source.CareerEquipmentChanged += () => events++;
            Assert.IsTrue(source.TrySetDriverAppearance(expected, out var reason), reason);
            Assert.AreEqual(1, events);
            var serialized = JsonUtility.ToJson(source.CapturePersistentState());
            var snapshot = JsonUtility.FromJson<TruckTaxiPersistentState>(serialized);
            Assert.IsTrue(TruckTaxiSession.ValidatePersistentState(snapshot, out reason), reason);
            var restored = Session(false);
            Assert.IsTrue(restored.TryRestorePersistentState(snapshot, out reason), reason);
            Assert.IsTrue(expected.SameAs(restored.CaptureDriverAppearance()));
            Assert.AreEqual("profile.appearance.test", restored.CareerRunProfileId);
            Assert.AreEqual(source.WalletBalanceCents, restored.WalletBalanceCents);
            Assert.AreEqual(0, restored.PurchaseChargesCents);
        }

        [Test]
        public void CommitCaptureProfileAndSnapshotCopiesAreIsolated()
        {
            var session = Session(); var input = Custom(); var expected = input.Copy();
            Assert.IsTrue(session.TrySetDriverAppearance(input, out _));
            input.shirt = Color.black;
            var capture = session.CaptureDriverAppearance(); capture.trim = Color.black;
            var profile = session.CaptureCareerProfile(); var copy = profile.Copy(); copy.driverAppearance.skin = Color.white;
            profile.driverAppearance.pantsAndCap = Color.white;
            var snapshot = session.CapturePersistentState(); var snapshotCopy = session.CapturePersistentState();
            snapshotCopy.ownedProfile.driverAppearance.capVisible = true;
            Assert.IsFalse(snapshot.ownedProfile.driverAppearance.capVisible);
            Assert.IsTrue(expected.SameAs(session.CaptureDriverAppearance()));
            Assert.AreNotSame(profile.driverAppearance, copy.driverAppearance);
        }

        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)]
        [TestCase(4)] [TestCase(5)] [TestCase(6)] [TestCase(7)]
        [TestCase(8)] [TestCase(9)] [TestCase(10)]
        public void InvalidAppearanceCannotMutateCareerOrEmitChange(int kind)
        {
            var session = Session(); var before = session.CaptureDriverAppearance();
            var invalid = Custom();
            switch (kind)
            {
                case 0: invalid.shirt = new Color(float.NaN, 0, 0); break;
                case 1: invalid.pantsAndCap = new Color(0, float.PositiveInfinity, 0); break;
                case 2: invalid.trim = new Color(0, 0, -.01f); break;
                case 3: invalid.skin = new Color(1.01f, 0, 0); break;
                case 4: invalid.skin = new Color(0, 0, 0, .5f); break;
                case 5: invalid.trim = new Color(0, 0, 0, float.NaN); break;
                case 6: invalid.version = 2; break;
                case 7: invalid = null; break;
                case 8: invalid.chestShape = float.NaN; break;
                case 9: invalid.chestShape = -1.01f; break;
                case 10: invalid.chestShape = 1.01f; break;
            }
            int events = 0; session.CareerEquipmentChanged += () => events++;
            Assert.IsFalse(session.TrySetDriverAppearance(invalid, out var reason));
            Assert.IsFalse(string.IsNullOrEmpty(reason)); Assert.AreEqual(0, events);
            Assert.IsTrue(before.SameAs(session.CaptureDriverAppearance()));
            Assert.AreEqual(0, session.PurchaseChargesCents);
        }

        [Test]
        public void InvalidPersistedAppearanceRejectsWholeRestore()
        {
            var session = Session(); var before = session.CapturePersistentState(); var corrupt = session.CapturePersistentState();
            corrupt.ownedProfile.driverAppearance.shirt = new Color(-1, 0, 0);
            Assert.IsFalse(TruckTaxiSession.ValidatePersistentState(corrupt, out _));
            Assert.IsFalse(session.TryRestorePersistentState(corrupt, out _));
            Assert.IsTrue(before.ownedProfile.driverAppearance.SameAs(session.CaptureDriverAppearance()));
        }

        [Test]
        public void NullableLegacyAppearanceUsesFreshDefaultsWithoutChangingIdentity()
        {
            var source = Session(); var legacy = source.CapturePersistentState(); legacy.ownedProfile.driverAppearance = null;
            Assert.IsTrue(TruckTaxiSession.ValidatePersistentState(legacy, out var reason), reason);
            var target = Session(false);
            Assert.IsTrue(target.TryRestorePersistentState(legacy, out reason), reason);
            var defaults = new TruckTaxiDriverAppearanceData();
            Assert.IsTrue(defaults.SameAs(target.CaptureDriverAppearance()));
            var capture = target.CaptureDriverAppearance(); capture.shirt = Color.white;
            Assert.IsTrue(defaults.SameAs(target.CaptureDriverAppearance()));
            Assert.AreEqual(source.CareerRunProfileId, target.CareerRunProfileId);
            Assert.AreEqual(source.DriverName, target.DriverName);
            Assert.IsNull(target.CaptureCareerProfile().driverAppearance, "Reading defaults must not silently rewrite a legacy save.");
            Assert.IsTrue(target.TrySetDriverAppearance(Custom(), out reason), reason);
            Assert.IsNotNull(target.CaptureCareerProfile().driverAppearance);
        }

        [Test]
        public void LegacyMissingChestFieldPreservesOriginalShapeAndColors()
        {
            var legacy = JsonUtility.FromJson<TruckTaxiDriverAppearanceData>(
                "{\"version\":1,\"shirt\":{\"r\":0.7,\"g\":0.2,\"b\":0.3,\"a\":1},\"capVisible\":false}");
            Assert.AreEqual(0, legacy.chestShape);
            Assert.AreEqual(new Color(.7f,.2f,.3f), legacy.shirt);
            Assert.IsFalse(legacy.capVisible);
            Assert.IsTrue(legacy.Validate(out var reason), reason);
        }

        [Test]
        public void ShapeOnlyEditUsesExistingCareerEventAndCopies()
        {
            var session = Session(); var original = session.CaptureDriverAppearance(); var changed = original.Copy();
            changed.chestShape = 1;
            Assert.IsFalse(original.SameAs(changed));
            int events = 0; session.CareerEquipmentChanged += () => events++;
            Assert.IsTrue(session.TrySetDriverAppearance(changed, out var reason), reason);
            changed.chestShape = -1;
            Assert.AreEqual(1, session.CaptureDriverAppearance().chestShape);
            Assert.AreEqual(original.shirt, session.CaptureDriverAppearance().shirt);
            Assert.AreEqual(original.capVisible, session.CaptureDriverAppearance().capVisible);
            Assert.AreEqual(1, events);
        }

        [Test]
        public void AdultClothedShapeIsVisibleBoundedAndDoesNotAccumulate()
        {
            var root = new GameObject("Adult shape test");
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            try
            {
                var visual = TruckTaxiWobbleVisual.CreateThemed(root.transform, new TruckTaxiWobbleVisual.Design {
                    silhouette = TruckTaxiWobbleVisual.Silhouette.Human, headwear = TruckTaxiWobbleVisual.Headwear.Cap,
                    height = 1.5f, width = 1, headScale = 1, upperBodyScale = 1, clothedAdult = true,
                    body = material, clothes = material, accent = material, skin = material
                });
                var part = visual.transform.Find("Left Clothed chest"); var original = part.localScale;
                var appearance = new TruckTaxiDriverAppearanceData { chestShape = 1, capVisible = false };
                Assert.IsTrue(TruckTaxiDriverPresentation.ApplyVisualAppearance(visual.transform, appearance));
                Assert.Greater(part.localScale.z, original.z);
                var large = part.localScale;
                Assert.IsTrue(TruckTaxiDriverPresentation.ApplyVisualAppearance(visual.transform, appearance));
                Assert.AreEqual(large, part.localScale);
                Assert.IsFalse(visual.transform.Find("Cap").gameObject.activeSelf);
                Assert.AreSame(material, part.GetComponent<Renderer>().sharedMaterial);
                appearance.chestShape = 0;
                Assert.IsTrue(TruckTaxiDriverPresentation.ApplyVisualAppearance(visual.transform, appearance));
                Assert.Less(Vector3.Distance(original, part.localScale), .0001f);
                Assert.IsFalse(TruckTaxiDriverPresentation.ApplyVisualAppearance(root.transform, appearance), "Unsupported rigs must not be fabricated.");
            }
            finally { UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(material); }
        }
    }
}

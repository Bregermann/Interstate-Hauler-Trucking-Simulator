using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace LWS.TruckTaxi.Tests.EditMode
{
    public sealed class TruckTaxiWorldLifeDamageTests
    {
        private static void Lifecycle(MonoBehaviour target,string method)
        {
            var callback=target.GetType().GetMethod(method,System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
            Assert.IsNotNull(callback);
            callback.Invoke(target,null);
        }
        private static void DestroyInteractionFixture(GameObject root)
        {
            foreach(var item in root.GetComponentsInChildren<TruckTaxiWorldInteractable>(true)) Lifecycle(item,"OnDisable");
            Object.DestroyImmediate(root);
        }
        private static TruckTaxiWorldLifeController CreateInteractionActor(GameObject root)
        {
            root.transform.position = new Vector3(10000, 10000, 10000);
            var actor = root.AddComponent<TruckTaxiWorldLifeController>();
            var foot = new GameObject("Test foot rig"); foot.transform.SetParent(root.transform, false);
            var eye = new GameObject("Test eye"); eye.transform.SetParent(foot.transform, false);
            var camera = eye.AddComponent<Camera>(); camera.enabled = false;
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            typeof(TruckTaxiWorldLifeController).GetField("footBody", flags).SetValue(actor, foot);
            typeof(TruckTaxiWorldLifeController).GetField("footCamera", flags).SetValue(actor, camera);
            typeof(TruckTaxiWorldLifeController).GetField("<Mode>k__BackingField", flags).SetValue(actor, TruckTaxiPlayerControlMode.OnFoot);
            return actor;
        }

        [Test]
        public void CandidateOrderAndSelectionSurviveMotionAndUnrelatedRemoval()
        {
            var root = new GameObject("Candidate test");
            try
            {
                var actor = CreateInteractionActor(root);
                var a = new GameObject("A"); a.transform.SetParent(root.transform); a.transform.localPosition = Vector3.forward * 2;
                var b = new GameObject("B"); b.transform.SetParent(root.transform); b.transform.localPosition = Vector3.forward;
                var first = a.AddComponent<TruckTaxiCheckoutRegister>(); first.SetStableId("register.a");
                var second = b.AddComponent<TruckTaxiCheckoutRegister>(); second.SetStableId("register.b");
                // Ordinary MonoBehaviours do not receive Play Mode lifecycle callbacks in EditMode.
                Lifecycle(first,"OnEnable"); Lifecycle(second,"OnEnable");
                actor.RefreshInteractionCandidates();
                CollectionAssert.AreEqual(new TruckTaxiWorldInteractable[] { first, second }, actor.InteractionCandidates);
                actor.CycleInteraction(1); Assert.AreSame(second, actor.SelectedInteraction);
                b.transform.localPosition = Vector3.right * 2;
                actor.RefreshInteractionCandidates(); Assert.AreSame(second, actor.SelectedInteraction);
                first.enabled = false;
                actor.RefreshInteractionCandidates(); Assert.AreSame(second, actor.SelectedInteraction);
                second.enabled = false;
                Lifecycle(first,"OnDisable"); Lifecycle(second,"OnDisable");
                actor.RefreshInteractionCandidates(); Assert.IsNull(actor.SelectedInteraction);
                Assert.IsEmpty(actor.InteractionCandidates);
            }
            finally { DestroyInteractionFixture(root); }
        }

        [Test]
        public void CategoryRadiiAreForgivingButBoundedAndAuthoredRangeCanOverride()
        {
            var root = new GameObject("Range test");
            try
            {
                var actor = root.AddComponent<TruckTaxiWorldLifeController>();
                var fixture = root.AddComponent<TruckTaxiRestroomFixture>(); fixture.bowel = true;
                Assert.AreEqual(3.5f, actor.InteractionRangeFor(fixture));
                actor.toiletRange = 4.5f; Assert.AreEqual(4.5f, actor.InteractionRangeFor(fixture));
                fixture.useCategoryRange = false; fixture.interactionRange = 2f;
                Assert.AreEqual(2f, actor.InteractionRangeFor(fixture));
                fixture.interactionRange = 500f; Assert.AreEqual(6f, actor.InteractionRangeFor(fixture));
                fixture.interactionRange = float.NaN; Assert.AreEqual(3f, actor.InteractionRangeFor(fixture));
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void CheckoutRequiresThisStoresVisibleRegisterNotItsOrigin()
        {
            var root = new GameObject("Checkout test");
            try
            {
                var actor = CreateInteractionActor(root);
                var storeObject = new GameObject("Store"); storeObject.transform.SetParent(root.transform, false);
                var store = storeObject.AddComponent<TruckTaxiStorePoint>();
                var registerObject = new GameObject("Register"); registerObject.transform.SetParent(storeObject.transform);
                registerObject.transform.localPosition = Vector3.forward * 2;
                var register = registerObject.AddComponent<TruckTaxiCheckoutRegister>();
                Lifecycle(register,"OnEnable");
                Physics.SyncTransforms();
                Assert.IsTrue(actor.CanCheckoutAt(store));
                var foreignStore = root.AddComponent<TruckTaxiStorePoint>();
                Assert.IsFalse(actor.CanCheckoutAt(foreignStore));
                var wall = GameObject.CreatePrimitive(PrimitiveType.Cube); wall.transform.SetParent(root.transform);
                wall.transform.localPosition = Vector3.forward;
                wall.transform.localScale = new Vector3(5, 5, .2f);
                Physics.SyncTransforms();
                Assert.IsFalse(actor.CanCheckoutAt(store), "Walls must occlude checkout even in range.");
                wall.SetActive(false); registerObject.transform.localPosition = Vector3.forward * 10;
                Physics.SyncTransforms();
                Assert.IsFalse(actor.CanCheckoutAt(store), "The store origin being close must not grant checkout.");
                registerObject.transform.localPosition = Vector3.forward * 2; register.enabled = false;
                Lifecycle(register,"OnDisable");
                Assert.IsFalse(actor.CanCheckoutAt(store));
            }
            finally { DestroyInteractionFixture(root); }
        }

        [Test]
        public void ModalFocusBlocksWorldCyclingAndDisablingReturnsControl()
        {
            var root = new GameObject("Modal test");
            try
            {
                var actor = CreateInteractionActor(root);
                actor.SetExternalModalFocus(true);
                Assert.IsTrue(actor.PointerInputBlocked);
                Assert.AreEqual(CursorLockMode.None, Cursor.lockState);
                actor.SetExternalModalFocus(false);
                Assert.IsTrue(actor.PointerInputBlocked, "Closing must swallow the same-frame gameplay input.");
                actor.SelectInventoryItem(TruckTaxiNeedsItem.WaterBottle);
                Assert.AreEqual(TruckTaxiNeedsItem.WaterBottle, actor.SelectedInventoryItem);
                Assert.IsFalse(actor.UseSelectedInventoryItem(), "Selection alone cannot use an item outside an open inventory.");
                actor.enabled = false;
                Lifecycle(actor,"OnDisable");
                Assert.AreEqual(TruckTaxiPlayerControlMode.Driving, actor.Mode);
                Assert.AreEqual(CursorLockMode.None, Cursor.lockState);
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void CareerHooksRecordOnlySuccessfulFirstDiscoveryAndAmbientChoice()
        {
            var root = new GameObject("World-life career fixture"); root.SetActive(false);
            var settings = ScriptableObject.CreateInstance<TruckTaxiEnvironmentSettings>();
            var configuration = ScriptableObject.CreateInstance<TruckTaxiConfiguration>();
            TruckTaxiDriverNeedsCoordinator needs = null;
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public;
            void Field(object target, string name, object value) => target.GetType().GetField(name, flags).SetValue(target, value);
            void Property(object target, string name, object value) => target.GetType().GetProperty(name, flags).SetValue(target, value);
            try
            {
                var host = root.AddComponent<TruckTaxiBootstrap>(); host.enabled = false;
                var session = new TruckTaxiSession(configuration, System.Array.Empty<TruckTaxiRideLocation>(), 31);
                Property(host, "Session", session);
                var environment = root.AddComponent<TruckTaxiEnvironmentCoordinator>();
                Property(environment, "Clock", new LWS.InterstateHauler.LwsGameClockService());
                needs = root.AddComponent<TruckTaxiDriverNeedsCoordinator>();
                Field(needs, "host", host); Field(needs, "environment", environment);
                var state = new TruckTaxiDriverNeedsState(settings, 31);
                Property(needs, "State", state); Property(host, "DriverNeeds", needs);
                var actor = CreateInteractionActor(root); Field(actor, "host", host);
                var finish = typeof(TruckTaxiWorldLifeController).GetMethod("FinishRestroom", flags);
                var choice = typeof(TruckTaxiWorldLifeController).GetMethod("RecordAmbientChoice", flags);

                Assert.IsTrue(actor.StartRestroom("test.career.toilet", false));
                finish.Invoke(actor, new object[] { false });
                Assert.IsEmpty(session.CaptureCareerStatistics().discoveredToiletIds);
                Assert.IsTrue(actor.StartRestroom("test.career.toilet", false));
                finish.Invoke(actor, new object[] { true });
                Assert.IsTrue(actor.StartRestroom("test.career.toilet", false));
                finish.Invoke(actor, new object[] { true });
                Assert.IsTrue(actor.StartRestroom("test.career.urinal", false));
                finish.Invoke(actor, new object[] { true });
                CollectionAssert.AreEqual(new[] { "test.career.toilet" }, session.CaptureCareerStatistics().discoveredToiletIds);
                Assert.AreEqual(1, state.ToiletsDiscovered);

                const string choiceId = "test.career.stranger.declined";
                choice.Invoke(actor, new object[] { string.Empty });
                Assert.IsEmpty(session.CaptureCareerStatistics().events);
                choice.Invoke(actor, new object[] { choiceId });
                choice.Invoke(actor, new object[] { choiceId });
                Assert.IsTrue(state.RestorePersistentState(state.CapturePersistentState(), out _));
                choice.Invoke(actor, new object[] { choiceId });
                var events = session.CaptureCareerStatistics().events;
                Assert.AreEqual(1, events.Count);
                Assert.AreEqual(choiceId, events[0].id);
                Assert.AreEqual(1, events[0].count);
            }
            finally
            {
                if (needs != null) Object.DestroyImmediate(needs);
                Object.DestroyImmediate(root); Object.DestroyImmediate(settings); Object.DestroyImmediate(configuration);
            }
        }

        [TestCase(0.04 * TruckTaxiUsUnits.MetersPerMile, "211 ft")]
        [TestCase(0.09 * TruckTaxiUsUnits.MetersPerMile, "475 ft")]
        [TestCase(0.10 * TruckTaxiUsUnits.MetersPerMile, "0.1 mi")]
        [TestCase(1.26 * TruckTaxiUsUnits.MetersPerMile, "1.3 mi")]
        public void DistancePresentationUsesFeetUntilOneTenthMile(double meters, string expected) =>
            Assert.AreEqual(expected, TruckTaxiUsUnits.Distance(meters));

        [Test]
        public void FuelAndSpeedRemainPresentationConversions()
        {
            Assert.AreEqual(60f, TruckTaxiUsUnits.Mph(26.8224f), .01f);
            Assert.AreEqual("10.0 gal", TruckTaxiUsUnits.Fuel((float)(10 * TruckTaxiUsUnits.LitersPerUsGallon)));
            Assert.AreEqual(568, TruckTaxiUsUnits.CentsPerGallon(150));
        }

        [Test]
        public void LegacyNeedsSnapshotRestoresWithHealthyBowelAndNoToilets()
        {
            var settings = ScriptableObject.CreateInstance<TruckTaxiEnvironmentSettings>();
            try
            {
                var state = new TruckTaxiDriverNeedsState(settings, 11);
                var old = state.CapturePersistentState();
                old.UsedToiletIds = null; old.BowelPressure = 0;
                Assert.IsTrue(new TruckTaxiDriverNeedsState(settings, 12).RestorePersistentState(old, out string error), error);
            }
            finally { Object.DestroyImmediate(settings); }
        }

        [Test]
        public void ToiletDiscoveryAndBowelNeedRoundTripThroughExistingNeedsSnapshot()
        {
            var settings = ScriptableObject.CreateInstance<TruckTaxiEnvironmentSettings>();
            try
            {
                var state = new TruckTaxiDriverNeedsState(settings, 15);
                int notifications = 0;
                state.ToiletCollectionChanged += _ => notifications++;
                state.AdvanceGameSeconds(3600);
                Assert.Greater(state.BowelPressure, 0);
                Assert.IsTrue(state.RecordToiletUse("truck-stop.west.porta"));
                Assert.IsFalse(state.RecordToiletUse("truck-stop.west.porta"));
                Assert.AreEqual(1, notifications);
                var restored = new TruckTaxiDriverNeedsState(settings, 16);
                Assert.IsTrue(restored.RestorePersistentState(state.CapturePersistentState(), out string error), error);
                Assert.AreEqual(state.BowelPressure, restored.BowelPressure, .0001f);
                Assert.IsTrue(restored.HasUsedToilet("truck-stop.west.porta"));
                Assert.AreEqual(1, restored.ToiletsDiscovered);
                restored.RelieveBowel();
                Assert.AreEqual(0, restored.BowelPressure);
            }
            finally { Object.DestroyImmediate(settings); }
        }

        [Test]
        public void DuplicateToiletIdsFailWithoutPartialRestore()
        {
            var settings = ScriptableObject.CreateInstance<TruckTaxiEnvironmentSettings>();
            try
            {
                var state = new TruckTaxiDriverNeedsState(settings, 17);
                var snapshot = state.CapturePersistentState();
                snapshot.UsedToiletIds = new[] { "same.toilet", "same.toilet" };
                Assert.IsFalse(state.RestorePersistentState(snapshot, out _));
                Assert.AreEqual(0, state.ToiletsDiscovered);
            }
            finally { Object.DestroyImmediate(settings); }
        }

        [Test]
        public void MotelAccessAndAmbientChoicesRoundTripWithoutChangingOldSaves()
        {
            var settings = ScriptableObject.CreateInstance<TruckTaxiEnvironmentSettings>();
            try
            {
                var state = new TruckTaxiDriverNeedsState(settings, 18);
                Assert.IsTrue(state.RentMotel("motel.validation", 1000, 12 * 3600));
                Assert.IsTrue(state.RecordAmbientChoice("motel.validation.ambient.Panhandler.gave"));
                Assert.IsFalse(state.RecordAmbientChoice("motel.validation.ambient.Panhandler.gave"));
                var restored = new TruckTaxiDriverNeedsState(settings, 19);
                Assert.IsTrue(restored.RestorePersistentState(state.CapturePersistentState(), out string error), error);
                Assert.IsTrue(restored.HasMotelAccess("motel.validation", 2000));
                Assert.IsFalse(restored.HasMotelAccess("motel.validation", 1000 + 12 * 3600));
                Assert.AreEqual(1, restored.AmbientChoiceIds.Count);
                var old = state.CapturePersistentState();
                old.RentedMotelId = null; old.MotelAccessExpiresAtGameSeconds = 0; old.AmbientChoiceIds = null;
                Assert.IsTrue(restored.RestorePersistentState(old, out error), error);
                Assert.IsNull(restored.RentedMotelId);
                Assert.AreEqual(0, restored.AmbientChoiceIds.Count);
            }
            finally { Object.DestroyImmediate(settings); }
        }

        [Test]
        public void MotelSleepRecoversMoreThanCabNapThroughExistingFatigueState()
        {
            var settings = ScriptableObject.CreateInstance<TruckTaxiEnvironmentSettings>();
            try
            {
                var cab = new TruckTaxiDriverNeedsState(settings, 20);
                var motel = new TruckTaxiDriverNeedsState(settings, 21);
                var starting = cab.CapturePersistentState(); starting.Fatigue = .95f;
                Assert.IsTrue(cab.RestorePersistentState(starting, out string error), error);
                Assert.IsTrue(motel.RestorePersistentState(starting, out error), error);
                Assert.IsTrue(cab.ApplySleep(4, .8f));
                Assert.IsTrue(motel.ApplySleep(4, 1.1f));
                Assert.Less(motel.Fatigue, cab.Fatigue);
            }
            finally { Object.DestroyImmediate(settings); }
        }

        [TestCase(-1, 2, "body.front.left")]
        [TestCase(1, 2, "body.front.right")]
        [TestCase(0, -2, "body.rear")]
        [TestCase(0, 0, "body.cab")]
        public void InspectionUsesTheHitRegionInsteadOfAlwaysSelectingTheFront(float x, float z, string id) =>
            Assert.AreEqual(id, TruckTaxiComponentDamage.BodyPartAtLocalPosition(new Vector3(x, 0, z)));

        [Test]
        public void DamageSaveKeysAreVehicleAndStableComponentIds()
        {
            var available = new Dictionary<string, TruckTaxiDamagePart>
            {
                ["engine"] = new TruckTaxiDamagePart("engine", "Engine", TruckTaxiDamageCategory.Engine, 350000),
                ["tire.axle1.left.1"] = new TruckTaxiDamagePart("tire.axle1.left.1", "Front Left Tire", TruckTaxiDamageCategory.Tire, 65000)
            };
            var saved = new TruckTaxiDamageSnapshot { VehicleId = "truck.stable", Parts = new[]
            { new TruckTaxiDamageEntry { Id = "tire.axle1.left.1", Condition = .2f } } };
            Assert.IsTrue(TruckTaxiComponentDamage.ValidSnapshot(saved, "truck.stable", available));
            Assert.IsFalse(TruckTaxiComponentDamage.ValidSnapshot(saved, "another.truck", available));
            saved.Parts[0].Id = "tire.axle1.right.1";
            Assert.IsFalse(TruckTaxiComponentDamage.ValidSnapshot(saved, "truck.stable", available));
            Assert.IsTrue(TruckTaxiComponentDamage.ValidSnapshot(null, "truck.stable", available));
        }

    }
}

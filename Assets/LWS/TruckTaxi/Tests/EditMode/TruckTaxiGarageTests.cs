using System;
using System.Collections.Generic;
using LWS.InterstateHauler;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LWS.TruckTaxi.Tests
{
    [Category("TaxiCareer")]
    public sealed class TruckTaxiGarageTests
    {
        private readonly List<Object> objects = new List<Object>();
        private TruckTaxiConfiguration config;
        private TruckTaxiCareerCatalog catalog;
        private T Asset<T>() where T : ScriptableObject
        { var value = ScriptableObject.CreateInstance<T>(); objects.Add(value); return value; }
        [SetUp] public void SetUp()
        {
            config = Asset<TruckTaxiConfiguration>(); catalog = Asset<TruckTaxiCareerCatalog>();
            var current = Asset<LwsTruckDefinition>(); var next = Asset<LwsTruckDefinition>();
            var model = new GameObject("Garage test prefab reference"); objects.Add(model);
            current.ConfigureForTests("truck.current", model); next.ConfigureForTests("truck.next", model);
            catalog.trucks = new[] { new TruckTaxiCareerTruckDefinition { truck = current },
                new TruckTaxiCareerTruckDefinition { truck = next, availableForPurchase = true } };
        }
        [TearDown] public void TearDown()
        { for (int i = objects.Count - 1; i >= 0; i--) Object.DestroyImmediate(objects[i]); objects.Clear(); }
        private TruckTaxiSession Career()
        {
            var session = new TruckTaxiSession(config, Array.Empty<TruckTaxiRideLocation>(), 33);
            session.StartShift(); Assert.IsTrue(session.BindCareerRun("profile.garage", out _));
            Assert.IsTrue(session.TryInitializeCareer("Driver", "truck.current", out _)); return session;
        }
        private static TruckTaxiGaragePaintData Paint() => new TruckTaxiGaragePaintData { color = new Color(.137f, .521f, .983f, 1) };

        [Test] public void CancelDiscardsDraftWithoutMutatingCareerOrPublishingEquipment()
        {
            var session = Career(); int events = 0; session.CareerEquipmentChanged += () => events++;
            var draft = new TruckTaxiGarageDraft(); Assert.IsTrue(draft.Begin(session.CaptureCareerProfile(), catalog));
            Assert.IsTrue(draft.SetPaint(Paint()));
            Assert.IsFalse(session.CaptureCareerProfile().trucks[0].hasCustomPaint);
            draft.Cancel(); Assert.IsFalse(draft.IsActive); Assert.AreEqual(0, events);
            Assert.IsFalse(session.CaptureCareerProfile().trucks[0].hasCustomPaint);
        }
        [Test] public void ApplyPublishesOneAtomicSelectionAndPaintWithoutCharging()
        {
            var session = Career(); int events = 0;
            session.CareerEquipmentChanged += () =>
            {
                events++; var profile = session.CaptureCareerProfile();
                Assert.AreEqual("truck.current", profile.equippedTruckId);
                Assert.AreEqual(Paint().color, profile.trucks[0].paint.color);
            };
            var draft = new TruckTaxiGarageDraft(); Assert.IsTrue(draft.Begin(session.CaptureCareerProfile(), catalog));
            var value = Paint(); draft.SetPaint(value); value.color = Color.black;
            Assert.IsTrue(draft.Apply(session, catalog, "truck.current", out var reason), reason);
            Assert.IsFalse(draft.IsActive); Assert.AreEqual(1, events); Assert.AreEqual(0, session.PurchaseChargesCents);
            var detached = session.CaptureCareerProfile(); detached.trucks[0].paint.color = Color.red;
            Assert.AreEqual(Paint().color, session.CaptureCareerProfile().trucks[0].paint.color);
        }
        [Test] public void OnlyOwnedEntriesWithRealMatchingDefinitionsAndPrefabsAreListed()
        {
            var session = Career(); Assert.AreEqual(1, TruckTaxiGarageDraft.OwnedTrucks(session.CaptureCareerProfile(), catalog).Count);
            Assert.IsFalse(session.TryApplyGarage(catalog, "truck.next", Paint(), out _));
            Assert.IsTrue(session.TryPurchaseTruck(catalog, "truck.next", out _));
            Assert.AreEqual(2, TruckTaxiGarageDraft.OwnedTrucks(session.CaptureCareerProfile(), catalog).Count);
            catalog.trucks[1].truck.ConfigureForTests("truck.next", null);
            Assert.AreEqual(1, TruckTaxiGarageDraft.OwnedTrucks(session.CaptureCareerProfile(), catalog).Count);
            Assert.IsFalse(session.TryApplyGarage(catalog, "truck.next", Paint(), out _));
            catalog.trucks[0].truck = null;
            Assert.AreEqual(0, TruckTaxiGarageDraft.OwnedTrucks(session.CaptureCareerProfile(), catalog).Count);
        }
        [Test] public void DraftNeverChangesCanonicalSelectionWithoutFactorySwap()
        {
            var session = Career(); session.TryPurchaseTruck(catalog, "truck.next", out _);
            var draft = new TruckTaxiGarageDraft(); draft.Begin(session.CaptureCareerProfile(), catalog);
            Assert.IsTrue(draft.Select(catalog, "truck.next")); draft.SetPaint(Paint());
            Assert.IsFalse(draft.Apply(session, catalog, "truck.current", out _));
            Assert.AreEqual("truck.current", session.EquippedTruckId);
            Assert.IsFalse(session.CaptureCareerProfile().trucks[1].hasCustomPaint);
        }
        [Test] public void ChangedCareerSelectionInvalidatesDraft()
        {
            var session = Career(); session.TryPurchaseTruck(catalog, "truck.next", out _);
            var draft = new TruckTaxiGarageDraft(); draft.Begin(session.CaptureCareerProfile(), catalog); draft.SetPaint(Paint());
            session.TryEquipTruck(catalog, "truck.next", out _);
            Assert.IsFalse(draft.Apply(session, catalog, "truck.current", out _));
        }
        [Test] public void InvalidPaintAndMissingDeclaredPaintDoNotPassValidation()
        {
            var session = Career(); var paint = Paint(); paint.color.r = float.NaN;
            Assert.IsFalse(session.TryApplyGarage(catalog, "truck.current", paint, out _));
            paint = Paint(); paint.color.a = .5f;
            Assert.IsFalse(session.TryApplyGarage(catalog, "truck.current", paint, out _));
            var profile = session.CaptureCareerProfile(); profile.trucks[0].hasCustomPaint = true;
            Assert.IsFalse(profile.Validate(out _)); Assert.IsFalse(session.CaptureCareerProfile().trucks[0].hasCustomPaint);
        }
        [Test] public void RestoreDefaultIsOnlyCommittedByApply()
        {
            var session = Career(); session.TryApplyGarage(catalog, "truck.current", Paint(), out _);
            var draft = new TruckTaxiGarageDraft(); draft.Begin(session.CaptureCareerProfile(), catalog); draft.SetPaint(null);
            Assert.IsTrue(session.CaptureCareerProfile().trucks[0].hasCustomPaint); draft.Cancel();
            Assert.IsTrue(session.CaptureCareerProfile().trucks[0].hasCustomPaint);
            draft.Begin(session.CaptureCareerProfile(), catalog); draft.SetPaint(null);
            Assert.IsTrue(draft.Apply(session, catalog, "truck.current", out _));
            Assert.IsFalse(session.CaptureCareerProfile().trucks[0].hasCustomPaint);
        }
        [TestCase(true)]
        [TestCase(false)]
        public void ExistingVendorPayloadRoundtripsPaintAndDefaultThroughNewSession(bool customPaint)
        {
            var source = Career();
            Assert.IsTrue(source.TryApplyGarage(catalog, "truck.current", customPaint ? Paint() : null, out _));
            var settings = Asset<TruckTaxiEnvironmentSettings>(); var needs = new TruckTaxiDriverNeedsState(settings, 17);
            Assert.IsTrue(needs.CreateNewVibinInventory());
            var payload = new TruckTaxiVibinSavePayload { regionId = "taxi.town01", session = source.CapturePersistentState(), needs = needs.CapturePersistentState() };
            var target = Career();
            var participant = new TruckTaxiVibinSaveParticipant(() => payload,
                restored => target.TryRestoreCareerRun("profile.garage", restored.session, out var reason)
                    ? LwsSaveOperationResult.Success() : LwsSaveOperationResult.Failure(reason),
                value => TruckTaxiVibinSaveParticipant.ValidatePayload(value, settings));
            var record = participant.CaptureState();
            var result = participant.RestoreState(record); Assert.IsTrue(result.Succeeded, result.Message);
            var profile = target.CaptureCareerProfile(); Assert.AreEqual("truck.current", profile.equippedTruckId);
            Assert.AreEqual(customPaint, profile.trucks[0].hasCustomPaint);
            if (customPaint)
            {
                Assert.That(profile.trucks[0].paint.color.r, Is.EqualTo(Paint().color.r).Within(.000001f));
                Assert.That(profile.trucks[0].paint.color.g, Is.EqualTo(Paint().color.g).Within(.000001f));
                Assert.That(profile.trucks[0].paint.color.b, Is.EqualTo(Paint().color.b).Within(.000001f));
            }
            else Assert.IsNull(profile.trucks[0].paint, "Inline JsonUtility null data must not invent custom paint.");
        }
    }
}

using System;
using System.Collections.Generic;
using LWS.InterstateHauler;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LWS.TruckTaxi.Tests
{
    [Category("TaxiCareer")]
    public sealed class TruckTaxiDealershipUiTests
    {
        private readonly List<Object> objects = new List<Object>();
        private TruckTaxiCareerCatalog catalog;
        private TruckTaxiSession session;

        private T Asset<T>() where T : ScriptableObject
        { var value = ScriptableObject.CreateInstance<T>(); objects.Add(value); return value; }

        [SetUp] public void SetUp()
        {
            catalog = Asset<TruckTaxiCareerCatalog>();
            var prefab = new GameObject("Dealership test content reference"); objects.Add(prefab);
            catalog.trucks = new TruckTaxiCareerTruckDefinition[6];
            for (int i = 0; i < catalog.trucks.Length; i++)
            {
                var definition = Asset<LwsTruckDefinition>();
                definition.ConfigureForTests("truck.dealer" + i, prefab);
                catalog.trucks[i] = new TruckTaxiCareerTruckDefinition { truck = definition, availableForPurchase = true };
            }
            session = new TruckTaxiSession(Asset<TruckTaxiConfiguration>(), Array.Empty<TruckTaxiRideLocation>(), 33);
            session.StartShift(); Assert.IsTrue(session.BindCareerRun("profile.dealer", out _));
            Assert.IsTrue(session.TryInitializeCareer("Driver", catalog.trucks[0].StableId, out _));
        }

        [TearDown] public void TearDown()
        { for (int i = objects.Count - 1; i >= 0; i--) Object.DestroyImmediate(objects[i]); objects.Clear(); }

        [Test] public void SixCatalogRowsProjectEquippedOwnedAndSaleWithoutChangingWallet()
        {
            Assert.IsTrue(session.TryPurchaseTruck(catalog, catalog.trucks[1].StableId, out _));
            int published = 0; session.CareerEquipmentChanged += () => published++;
            long cash = session.WalletBalanceCents;
            var profile = session.CaptureCareerProfile();
            Assert.AreEqual("EQUIPPED", TruckTaxiDealership.Status(catalog.trucks[0], profile, cash));
            Assert.AreEqual("OWNED", TruckTaxiDealership.Status(catalog.trucks[1], profile, cash));
            for (int i = 2; i < 6; i++) Assert.AreEqual("FOR SALE", TruckTaxiDealership.Status(catalog.trucks[i], profile, cash));
            Assert.AreEqual(cash, session.WalletBalanceCents);
            Assert.AreEqual(0, published);
            Assert.AreEqual(2, session.CaptureCareerProfile().trucks.Count);
        }

        [TestCase(999, "NEED CASH")]
        [TestCase(1000, "FOR SALE")]
        [TestCase(1001, "FOR SALE")]
        public void AffordabilityIncludesExactPrice(long cash, string expected)
        {
            catalog.trucks[1].priceCents = 1000;
            Assert.AreEqual(expected, TruckTaxiDealership.Status(catalog.trucks[1], session.CaptureCareerProfile(), cash));
        }

        [Test] public void MissingContentAndDisabledSalesDoNotLookPurchasable()
        {
            var entry = catalog.trucks[1]; entry.availableForPurchase = false;
            Assert.AreEqual("UNAVAILABLE", TruckTaxiDealership.Status(entry, session.CaptureCareerProfile(), long.MaxValue));
            entry.availableForPurchase = true; entry.truck = null;
            Assert.AreEqual("UNAVAILABLE", TruckTaxiDealership.Status(entry, session.CaptureCareerProfile(), long.MaxValue));
            Assert.AreEqual("UNAVAILABLE", TruckTaxiDealership.Status(null, session.CaptureCareerProfile(), 0));
        }

        [Test] public void MissingFactoryCannotCommitGarageSelectionOrPaint()
        {
            string next = catalog.trucks[1].StableId;
            Assert.IsTrue(session.TryPurchaseTruck(catalog, next, out _));
            int published = 0; session.CareerEquipmentChanged += () => published++;
            var draft = new TruckTaxiGarageDraft();
            Assert.IsTrue(draft.Begin(session.CaptureCareerProfile(), catalog));
            Assert.IsTrue(draft.Select(catalog, next));
            Assert.IsTrue(draft.SetPaint(new TruckTaxiGaragePaintData { color = Color.red }));
            Assert.IsFalse(draft.Apply(session, catalog, (TruckTaxiTruckFactory)null, out string reason));
            Assert.IsNotEmpty(reason);
            Assert.IsTrue(draft.IsActive, "Failed Apply remains retryable.");
            Assert.AreEqual(catalog.trucks[0].StableId, session.EquippedTruckId);
            Assert.IsFalse(session.CaptureCareerProfile().trucks[1].hasCustomPaint);
            Assert.AreEqual(0, published);
            draft.Cancel(); Assert.IsFalse(draft.IsActive);
        }

        [Test] public void UninitializedDealershipOpenCloseAndDisableAreSafe()
        {
            var go = new GameObject("Unconfigured dealership"); objects.Add(go);
            var dealership = go.AddComponent<TruckTaxiDealership>();
            Assert.IsFalse(dealership.TryOpen());
            Assert.DoesNotThrow(dealership.Close);
            Assert.DoesNotThrow(dealership.Close);
            go.SetActive(false);
            Assert.IsFalse(dealership.IsOpen);
            UnityEngine.TestTools.LogAssert.NoUnexpectedReceived();
        }
    }
}

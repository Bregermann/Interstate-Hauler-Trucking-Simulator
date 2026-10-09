using System;
using LWS.InterstateHauler;
using LWS.TruckTaxi.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

namespace LWS.TruckTaxi.Tests
{
    public sealed class TruckTaxiCareerMetadataTests
    {
        private TruckTaxiCareerCatalog catalog;
        private LwsTruckDefinition starter;
        private TruckTaxiConfiguration configuration;

        [SetUp] public void Setup()
        {
            catalog=ScriptableObject.CreateInstance<TruckTaxiCareerCatalog>();
            starter=ScriptableObject.CreateInstance<LwsTruckDefinition>();
            starter.ConfigureForTests("truck.starter",null);
            configuration=ScriptableObject.CreateInstance<TruckTaxiConfiguration>();
            catalog.trucks=new[]{new TruckTaxiCareerTruckDefinition {
                truck=starter,description="Keep description",priceCents=123,availableForPurchase=false
            }};
        }

        [TearDown] public void Cleanup()
        { Object.DestroyImmediate(catalog); Object.DestroyImmediate(starter); Object.DestroyImmediate(configuration); }

        [Test] public void MigrationPreservesOtherEntriesAndIsIdempotent()
        {
            catalog.trucks=new[]{catalog.trucks[0],new TruckTaxiCareerTruckDefinition {
                stableId="truck.other",displayName="Other",description="Untouched",priceCents=456
            }};
            catalog.upgrades=new[]{new TruckTaxiCareerUpgradeDefinition {
                stableId="upgrade.test",category=TruckTaxiUpgradeCategory.Engine,
                compatibleTruckIds=new[]{"truck.starter"},description="Retain upgrade",priceCents=321
            }};
            catalog.cosmetics=new[]{new TruckTaxiCareerCosmeticDefinition {
                stableId="cosmetic.test",category=TruckTaxiCosmeticCategory.Paint,paintColor=Color.red
            }};
            string other=JsonUtility.ToJson(catalog.trucks[1]);
            string upgrade=JsonUtility.ToJson(catalog.upgrades[0]);
            string cosmetic=JsonUtility.ToJson(catalog.cosmetics[0]);
            Assert.IsTrue(TruckTaxiCareerAuthoring.MigrateStarterEntry(catalog,starter));
            var entry=catalog.trucks[0];
            Assert.IsNull(entry.truck);
            Assert.AreEqual(starter.StableId,entry.StableId);
            Assert.AreEqual(starter.DisplayName,entry.DisplayName);
            Assert.AreEqual("Keep description",entry.description);
            Assert.AreEqual(123,entry.priceCents);
            Assert.IsFalse(entry.availableForPurchase);
            Assert.AreEqual(other,JsonUtility.ToJson(catalog.trucks[1]));
            Assert.AreEqual(upgrade,JsonUtility.ToJson(catalog.upgrades[0]));
            Assert.AreEqual(cosmetic,JsonUtility.ToJson(catalog.cosmetics[0]));
            string after=EditorJsonUtility.ToJson(catalog);
            Assert.IsFalse(TruckTaxiCareerAuthoring.MigrateStarterEntry(catalog,starter));
            Assert.AreEqual(after,EditorJsonUtility.ToJson(catalog));
        }

        [Test] public void MetadataCannotAuthorizePurchaseOrEquipEvenWhenPurchaseFlagIsEnabled()
        {
            TruckTaxiCareerAuthoring.MigrateStarterEntry(catalog,starter);
            catalog.trucks[0].availableForPurchase=true;
            Assert.IsTrue(catalog.Validate(out var reason),reason);
            Assert.IsFalse(catalog.trucks[0].HasContent);
            var session=new TruckTaxiSession(configuration,Array.Empty<TruckTaxiRideLocation>(),33);
            session.StartShift();
            Assert.IsTrue(session.BindCareerRun("profile.metadata",out reason),reason);
            Assert.IsTrue(session.TryInitializeCareer("Driver",starter.StableId,out reason),reason);
            Assert.IsFalse(session.TryPurchaseTruck(catalog,starter.StableId,out _));
            Assert.IsFalse(session.TryEquipTruck(catalog,starter.StableId,out _));
            Assert.AreEqual(0,session.PurchaseChargesCents);
        }

        [Test] public void ExistingRealDefinitionStillSuppliesNameIdentityAndContent()
        {
            var prefab=new GameObject("Fixture truck");
            try
            {
                starter.ConfigureForTests("truck.starter",prefab);
                Assert.AreEqual(starter.StableId,catalog.trucks[0].StableId);
                Assert.AreEqual(starter.DisplayName,catalog.trucks[0].DisplayName);
                Assert.IsTrue(catalog.trucks[0].HasContent);
                catalog.trucks[0].stableId="truck.wrong";
                Assert.IsFalse(catalog.trucks[0].HasContent);
                Assert.IsFalse(catalog.Validate(out _));
            }
            finally { Object.DestroyImmediate(prefab); }
        }

        [Test] public void MissingStarterRejectsWithoutMutatingCatalog()
        {
            catalog.trucks=new[]{new TruckTaxiCareerTruckDefinition {stableId="truck.other",displayName="Other"}};
            string before=EditorJsonUtility.ToJson(catalog);
            Assert.Throws<InvalidOperationException>(()=>TruckTaxiCareerAuthoring.MigrateStarterEntry(catalog,starter));
            Assert.AreEqual(before,EditorJsonUtility.ToJson(catalog));
        }

        [Test] public void DuplicateMetadataIdentityIsRejected()
        {
            TruckTaxiCareerAuthoring.MigrateStarterEntry(catalog,starter);
            catalog.trucks=new[]{catalog.trucks[0],new TruckTaxiCareerTruckDefinition {stableId=starter.StableId}};
            string before=EditorJsonUtility.ToJson(catalog);
            Assert.IsFalse(catalog.Validate(out _));
            Assert.Throws<InvalidOperationException>(()=>TruckTaxiCareerAuthoring.MigrateStarterEntry(catalog,starter));
            Assert.AreEqual(before,EditorJsonUtility.ToJson(catalog));
        }
    }
}

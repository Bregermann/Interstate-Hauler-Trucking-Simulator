using System;
using System.Collections.Generic;
using LWS.InterstateHauler;
using NUnit.Framework;
using UnityEngine;
using Object=UnityEngine.Object;

namespace LWS.TruckTaxi.Tests
{
    [TestFixture, Category("TaxiCareer")]
    public sealed class TruckTaxiCareerFoundationTests
    {
        private readonly List<Object> objects=new List<Object>();
        private TruckTaxiConfiguration configuration;
        private TruckTaxiCareerCatalog catalog;
        private T Asset<T>() where T:ScriptableObject
        { var item=ScriptableObject.CreateInstance<T>(); objects.Add(item); return item; }
        private GameObject Model(string name)
        { var item=new GameObject(name); objects.Add(item); return item; }
        private TruckTaxiSession Session()
        { var session=new TruckTaxiSession(configuration,Array.Empty<TruckTaxiRideLocation>(),33); session.StartShift(); return session; }
        private TruckTaxiSession Career()
        {
            var session=Session();
            Assert.IsTrue(session.BindCareerRun("profile.taxi.test",out var reason),reason);
            Assert.IsTrue(session.TryInitializeCareer("  Sam Driver  ","truck.current",out reason),reason);
            return session;
        }
        [SetUp] public void Setup()
        {
            configuration=Asset<TruckTaxiConfiguration>();
            catalog=Asset<TruckTaxiCareerCatalog>();
            var current=Asset<LwsTruckDefinition>(); current.ConfigureForTests("truck.current",Model("current truck fixture"));
            var future=Asset<LwsTruckDefinition>(); future.ConfigureForTests("truck.next",Model("second truck fixture"));
            catalog.trucks=new[] {
                new TruckTaxiCareerTruckDefinition { truck=current },
                new TruckTaxiCareerTruckDefinition { truck=future,priceCents=1200,availableForPurchase=true }
            };
            catalog.upgrades=new[] {
                new TruckTaxiCareerUpgradeDefinition { stableId="upgrade.gears",displayName="Gears",category=TruckTaxiUpgradeCategory.Transmission,
                    compatibleTruckIds=new[] { "truck.current" },priceCents=300,adapterId="lws.nwh.transmission",
                    adapterConfiguration=Asset<Lws18SpeedTransmissionDefinition>(),availableForPurchase=true },
                new TruckTaxiCareerUpgradeDefinition { stableId="upgrade.gears.second",displayName="Other gears",category=TruckTaxiUpgradeCategory.Transmission,
                    compatibleTruckIds=new[] { "truck.current" },priceCents=400,adapterId="lws.nwh.transmission",
                    adapterConfiguration=Asset<Lws18SpeedTransmissionDefinition>(),availableForPurchase=true }
            };
            catalog.cosmetics=new[] {
                new TruckTaxiCareerCosmeticDefinition { stableId="cosmetic.paint",category=TruckTaxiCosmeticCategory.Paint,
                    compatibleTruckIds=new[] { "truck.current" },priceCents=100,adapterId="lws.paint",availableForPurchase=true },
                new TruckTaxiCareerCosmeticDefinition { stableId="cosmetic.hat",category=TruckTaxiCosmeticCategory.PlayerHeadwear,
                    priceCents=50,adapterId="lws.player.headwear",mountId="player.head",prefab=Model("headwear fixture"),availableForPurchase=true }
            };
        }
        [TearDown] public void Cleanup()
        {
            for(int i=objects.Count-1;i>=0;i--) if(objects[i]!=null) Object.DestroyImmediate(objects[i]);
            objects.Clear();
        }

        [Test] public void DefaultOwnedTruckAndEditableNameUseExistingRunIdentity()
        {
            var session=Career();
            Assert.AreEqual("Sam Driver",session.DriverName);
            Assert.AreEqual("profile.taxi.test",session.CareerRunProfileId);
            Assert.AreEqual("truck.current",session.EquippedTruckId);
            Assert.AreEqual(1,session.CaptureCareerProfile().trucks.Count);
            Assert.AreEqual(0,session.PurchaseChargesCents);
            Assert.IsTrue(session.TrySetDriverName("New Name",out _));
            Assert.AreEqual("profile.taxi.test",session.CareerRunProfileId);
            Assert.IsFalse(session.BindCareerRun("profile.other",out _));
            Assert.IsFalse(session.TryInitializeCareer("Again","truck.next",out _));
        }
        [TestCase("")]
        [TestCase("  ")]
        [TestCase("<b>Driver</b>")]
        [TestCase("Driver\nName")]
        [TestCase("Driver\u202EName")]
        public void InvalidDriverNameCannotMutateIdentity(string name)
        {
            var session=Career();
            Assert.IsFalse(session.TrySetDriverName(name,out _));
            Assert.AreEqual("Sam Driver",session.DriverName);
            Assert.IsFalse(session.TrySetDriverName(new string('x',41),out _));
        }
        [Test] public void NewCareerClaimsGuideOnceWithExactSubtitleAndNoVoice()
        {
            var session=Career();
            Assert.IsTrue(session.TryClaimFirstDayGuide(out var line));
            Assert.AreEqual("Welcome hun",line.Subtitle);
            Assert.IsNull(line.Voice);
            StringAssert.Contains("21+",line.CharacterDescription);
            Assert.IsFalse(session.TryClaimFirstDayGuide(out _));
            var target=Session(); Assert.IsTrue(target.BindCareerRun("profile.taxi.test",out _));
            Assert.IsTrue(target.TryRestorePersistentState(session.CapturePersistentState(),out var reason),reason);
            Assert.IsFalse(target.TryClaimFirstDayGuide(out _));
            Assert.AreEqual("Welcome hun",TruckTaxiSession.FirstDayGuidePreview.Subtitle);
            Assert.IsTrue(target.CaptureCareerProfile().firstGuideClaimed,"Preview must not change the claimed flag.");
        }
        [TestCase(1)]
        [TestCase(2)]
        public void LegacySavesPreserveStatsAndDoNotAcquireFirstDayGuideEligibility(int version)
        {
            var source=Session(); source.RecordRaceResult(1,60,2500);
            var snapshot=source.CapturePersistentState(); snapshot.version=version; snapshot.ownedProfile=null;
            var target=Session(); Assert.IsTrue(target.BindCareerRun("profile.taxi.test",out _));
            Assert.IsTrue(target.TryRestorePersistentState(snapshot,out var reason),reason);
            Assert.IsTrue(target.TryInitializeCareer("Existing Driver","truck.current",out reason),reason);
            Assert.AreEqual(2500,target.ShiftEarnings); Assert.AreEqual(1,target.RaceWins);
            Assert.IsFalse(target.TryClaimFirstDayGuide(out _));
            Assert.IsTrue(TruckTaxiSession.ValidatePersistentState(target.CapturePersistentState(),out reason),reason);
        }
        [Test] public void PurchaseDebitsExistingWalletOnceAndDoesNotAutoEquip()
        {
            var session=Career(); session.RecordRaceResult(3,0,5000);
            Assert.IsTrue(session.TryPurchaseTruck(catalog,"truck.next",out var reason),reason);
            Assert.AreEqual(3800,session.WalletBalanceCents);
            Assert.AreEqual(1200,session.PurchaseChargesCents);
            Assert.AreEqual("truck.current",session.EquippedTruckId);
            Assert.IsFalse(session.TryPurchaseTruck(catalog,"truck.next",out _));
            Assert.AreEqual(3800,session.WalletBalanceCents);
            Assert.IsTrue(session.TryEquipTruck(catalog,"truck.next",out reason),reason);
            Assert.AreEqual("truck.next",session.EquippedTruckId);
        }
        [Test] public void UnavailableIncompatibleAndUnaffordableContentNeverCharges()
        {
            var session=Career();
            Assert.IsFalse(session.TryPurchaseTruck(catalog,"truck.next",out _));
            session.RecordRaceResult(3,0,5000);
            catalog.trucks[1].truck.ConfigureForTests("truck.next",null);
            Assert.IsFalse(session.TryPurchaseTruck(catalog,"truck.next",out _));
            catalog.upgrades[0].compatibleTruckIds=Array.Empty<string>();
            Assert.IsFalse(session.TryPurchaseUpgrade(catalog,"upgrade.gears",out _));
            catalog.upgrades[1].adapterConfiguration=null;
            Assert.IsFalse(session.TryPurchaseUpgrade(catalog,"upgrade.gears.second",out _));
            catalog.cosmetics[1].prefab=null;
            Assert.IsFalse(session.TryPurchaseCosmetic(catalog,"cosmetic.hat",out _));
            Assert.AreEqual(0,session.PurchaseChargesCents);
        }
        [Test] public void UpgradesReplacePerCategoryButRemainOwnedPerTruck()
        {
            var session=Career(); session.RecordRaceResult(3,0,5000);
            Assert.IsFalse(session.TryEquipUpgrade(catalog,"upgrade.gears",out _));
            Assert.IsTrue(session.TryPurchaseUpgrade(catalog,"upgrade.gears",out _));
            Assert.IsTrue(session.TryEquipUpgrade(catalog,"upgrade.gears",out _));
            Assert.IsTrue(session.TryPurchaseUpgrade(catalog,"upgrade.gears.second",out _));
            Assert.IsTrue(session.TryEquipUpgrade(catalog,"upgrade.gears.second",out _));
            var owned=session.CaptureCareerProfile().trucks[0];
            Assert.AreEqual(2,owned.ownedUpgradeIds.Count);
            Assert.AreEqual(1,owned.upgrades.Count);
            Assert.AreEqual("transmission",owned.upgrades[0].slotId);
            Assert.IsTrue(session.TryPurchaseTruck(catalog,"truck.next",out _));
            Assert.IsTrue(session.TryEquipTruck(catalog,"truck.next",out _));
            Assert.IsFalse(session.TryEquipUpgrade(catalog,"upgrade.gears",out _));
        }
        [Test] public void HeadwearIsPlayerOwnedAndSurvivesTruckSelection()
        {
            var session=Career(); session.RecordRaceResult(3,0,5000);
            Assert.IsTrue(session.TryPurchaseCosmetic(catalog,"cosmetic.hat",out _));
            Assert.IsTrue(session.TryEquipCosmetic(catalog,"cosmetic.hat",out _));
            Assert.IsTrue(session.TryPurchaseCosmetic(catalog,"cosmetic.paint",out _));
            Assert.IsTrue(session.TryEquipCosmetic(catalog,"cosmetic.paint",out _));
            Assert.IsTrue(session.TryPurchaseTruck(catalog,"truck.next",out _));
            Assert.IsTrue(session.TryEquipTruck(catalog,"truck.next",out _));
            var snapshot=session.CaptureCareerProfile();
            Assert.AreEqual("cosmetic.hat",snapshot.equippedHeadwearId);
            Assert.AreEqual(1,snapshot.trucks[0].cosmetics.Count);
            Assert.IsEmpty(snapshot.trucks[1].cosmetics);
            Assert.IsTrue(session.TryUnequipCosmetic(TruckTaxiCosmeticCategory.PlayerHeadwear,out _));
            Assert.IsNull(session.CaptureCareerProfile().equippedHeadwearId);
            Assert.AreEqual(1,session.CaptureCareerProfile().ownedHeadwearIds.Count);
        }
        [Test] public void PurchaseNotificationSeesAtomicOwnershipAndDebitAndRejectsReentry()
        {
            var session=Career(); session.RecordRaceResult(3,0,5000);
            var before=session.CapturePersistentState();
            int notifications=0;
            session.Changed+=()=> {
                notifications++;
                Assert.AreEqual(300,session.PurchaseChargesCents);
                Assert.Contains("upgrade.gears",session.CaptureCareerProfile().trucks[0].ownedUpgradeIds);
                Assert.IsFalse(session.TryPurchaseUpgrade(catalog,"upgrade.gears",out _));
                Assert.IsFalse(session.TryRestorePersistentState(before,out _));
                Assert.IsFalse(session.TryRestoreCareerRun("profile.taxi.test",before,out _));
            };
            Assert.IsTrue(session.TryPurchaseUpgrade(catalog,"upgrade.gears",out _));
            Assert.AreEqual(1,notifications);
        }
        [Test] public void JsonRoundTripIsDetachedIdempotentAndDoesNotDoubleCharge()
        {
            var source=Career(); source.RecordRaceResult(3,0,5000);
            Assert.IsTrue(source.TryPurchaseUpgrade(catalog,"upgrade.gears",out _));
            Assert.IsTrue(source.TryEquipUpgrade(catalog,"upgrade.gears",out _));
            var saved=JsonUtility.FromJson<TruckTaxiPersistentState>(JsonUtility.ToJson(source.CapturePersistentState()));
            var target=Session(); Assert.IsTrue(target.BindCareerRun("profile.taxi.test",out _));
            for(int i=0;i<2;i++)
                Assert.IsTrue(target.TryRestorePersistentState(saved,out var reason),reason);
            Assert.AreEqual(4700,target.WalletBalanceCents);
            Assert.AreEqual(300,target.PurchaseChargesCents);
            saved.ownedProfile.trucks[0].upgrades.Clear();
            var detached=target.CaptureCareerProfile(); detached.trucks.Clear();
            Assert.AreEqual(1,target.CaptureCareerProfile().trucks[0].upgrades.Count);
        }
        [Test] public void ForeignOrCorruptOwnedStateRejectsBeforeWalletOrIdentityMutation()
        {
            var session=Career(); var saved=session.CapturePersistentState();
            saved.ownedProfile.runProfileId="profile.other";
            Assert.IsFalse(session.TryRestorePersistentState(saved,out _));
            Assert.AreEqual("profile.taxi.test",session.CareerRunProfileId);
            saved=session.CapturePersistentState();
            saved.ownedProfile.trucks[0].upgrades.Add(new TruckTaxiCareerSelection { slotId="engine",itemId="unowned" });
            Assert.IsFalse(session.TryRestorePersistentState(saved,out _));
            Assert.AreEqual(0,session.PurchaseChargesCents);
        }
        [Test] public void ExplicitRestoreSwitchesRunAndNotifiesExistingSubscribersWithDetachedState()
        {
            var session=Career(); session.RecordRaceResult(3,0,5000);
            var before=session.CapturePersistentState();
            var other=Session();
            Assert.IsTrue(other.BindCareerRun("profile.other",out _));
            Assert.IsTrue(other.TryInitializeCareer("Other Driver","truck.next",out _));
            other.RecordRaceResult(3,0,1700);
            var incoming=other.CapturePersistentState();
            Assert.IsFalse(session.TryRestorePersistentState(incoming,out _),"Ordinary restore cannot switch profiles.");
            int notifications=0;
            session.Changed+=()=> {
                notifications++;
                Assert.AreEqual("profile.other",session.CareerRunProfileId);
                Assert.AreEqual("Other Driver",session.DriverName);
                Assert.AreEqual("truck.next",session.EquippedTruckId);
                Assert.AreEqual(1700,session.WalletBalanceCents);
                Assert.IsTrue(session.BindCareerRun("profile.other",out _),"Binding must be installed before Changed.");
            };
            Assert.IsTrue(session.TryRestoreCareerRun("profile.other",incoming,out var reason),reason);
            Assert.AreEqual(1,notifications);
            incoming.ownedProfile.driverName="Caller mutation";
            incoming.ownedProfile.trucks.Clear();
            Assert.AreEqual("Other Driver",session.DriverName);
            Assert.AreEqual(1,session.CaptureCareerProfile().trucks.Count);
            Assert.IsFalse(session.TryRestorePersistentState(before,out _),"Ordinary restore now guards the new binding.");
            Assert.IsFalse(session.BindCareerRun("profile.taxi.test",out _));
        }

        [Test] public void ExplicitRestoreRejectsForeignInnerIdentityWithoutChangingRunOrWallet()
        {
            var session=Career(); session.RecordRaceResult(3,0,5000);
            var before=session.CapturePersistentState();
            var incoming=session.CapturePersistentState(); incoming.ownedProfile.runProfileId="profile.foreign";
            int notifications=0; session.Changed+=()=>notifications++;
            Assert.IsFalse(session.TryRestoreCareerRun("profile.other",incoming,out _));
            Assert.AreEqual(JsonUtility.ToJson(before),JsonUtility.ToJson(session.CapturePersistentState()));
            Assert.IsTrue(session.BindCareerRun("profile.taxi.test",out _));
            Assert.IsFalse(session.BindCareerRun("profile.other",out _));
            Assert.AreEqual(0,notifications);
        }

        [TestCase(1)]
        [TestCase(2)]
        public void ExplicitRestoreAllowsLegacyIdentityAndBindsTargetBeforeInitialization(int version)
        {
            var session=Career();
            var legacy=Session(); legacy.RecordRaceResult(3,0,1700);
            var incoming=legacy.CapturePersistentState(); incoming.version=version; incoming.ownedProfile=null;
            Assert.IsTrue(session.TryRestoreCareerRun("profile.legacy",incoming,out var reason),reason);
            Assert.IsFalse(session.HasCareerIdentity);
            Assert.AreEqual(1700,session.WalletBalanceCents);
            Assert.IsFalse(session.BindCareerRun("profile.taxi.test",out _));
            Assert.IsTrue(session.TryInitializeCareer("Legacy Driver","truck.current",out reason),reason);
            Assert.AreEqual("profile.legacy",session.CareerRunProfileId);
            Assert.IsFalse(session.TryClaimFirstDayGuide(out _));
        }

        [Test] public void ExplicitRestoreRejectsInvalidPreparationWithoutRebinding()
        {
            var session=Career();
            var before=session.CapturePersistentState();
            var incoming=session.CapturePersistentState(); incoming.ownedProfile.runProfileId="profile.other";
            incoming.version=int.MaxValue;
            Assert.IsFalse(session.TryRestoreCareerRun("profile.other",incoming,out _));
            Assert.IsFalse(session.TryRestoreCareerRun("",before,out _));
            Assert.IsFalse(session.TryRestoreCareerRun("profile.other",null,out _));
            Assert.AreEqual(JsonUtility.ToJson(before),JsonUtility.ToJson(session.CapturePersistentState()));
            Assert.IsTrue(session.BindCareerRun("profile.taxi.test",out _));
        }

        [Test] public void EquipmentEventPrecedesGeneralChangeAfterCommitAndRestoreOnly()
        {
            var session=Career(); session.RecordRaceResult(3,0,5000);
            var order=new List<string>();
            session.CareerEquipmentChanged+=()=> {
                order.Add("equipment");
                Assert.AreEqual(2,session.CaptureCareerProfile().trucks.Count);
                Assert.AreEqual(1200,session.PurchaseChargesCents);
            };
            session.Changed+=()=>order.Add("general");
            Assert.IsTrue(session.TryPurchaseTruck(catalog,"truck.next",out _));
            CollectionAssert.AreEqual(new[] { "equipment","general" },order);
            order.Clear();
            Assert.IsFalse(session.TryPurchaseTruck(catalog,"truck.next",out _));
            Assert.IsEmpty(order);
            var saved=session.CapturePersistentState();
            Assert.IsTrue(session.TryRestoreCareerRun("profile.taxi.test",saved,out _));
            CollectionAssert.AreEqual(new[] { "equipment","general" },order);
            order.Clear();
            saved.ownedProfile.runProfileId="foreign";
            Assert.IsFalse(session.TryRestoreCareerRun("profile.taxi.test",saved,out _));
            Assert.IsEmpty(order);
        }

        [Test] public void DuplicateCatalogIdsAndActiveRaceRejectPurchases()
        {
            var session=Career(); session.RecordRaceResult(3,0,5000);
            session.RaceEventActive=true;
            Assert.IsFalse(session.TryPurchaseTruck(catalog,"truck.next",out _));
            session.RaceEventActive=false;
            catalog.upgrades[1].stableId=catalog.upgrades[0].stableId;
            Assert.IsFalse(session.TryPurchaseTruck(catalog,"truck.next",out _));
            Assert.AreEqual(0,session.PurchaseChargesCents);
        }
    }
}

using NUnit.Framework;

namespace LWS.TruckTaxi.Tests.EditMode
{
    public sealed class TruckTaxiStabilizationPresentationTests
    {
        [TestCase(TruckTaxiNeedsItem.Burger,TruckTaxiItemCategory.Food)]
        [TestCase(TruckTaxiNeedsItem.WaterBottle,TruckTaxiItemCategory.Drinks)]
        [TestCase(TruckTaxiNeedsItem.AirFreshener,TruckTaxiItemCategory.Cab)]
        [TestCase(TruckTaxiNeedsItem.EmptyBottle,TruckTaxiItemCategory.Supplies)]
        [TestCase(TruckTaxiNeedsItem.RoadPep,TruckTaxiItemCategory.Specials)]
        public void ItemCategoriesUseExistingRegistrySemantics(TruckTaxiNeedsItem item,TruckTaxiItemCategory category)
        {
            var definition=TruckTaxiNeedsItems.Find(item);
            Assert.AreEqual(category,definition.Category);
            Assert.IsNotEmpty(definition.UseSummary);
        }
        [TestCase(TruckTaxiUpgradeCategory.Engine,true)]
        [TestCase(TruckTaxiUpgradeCategory.Brakes,true)]
        [TestCase(TruckTaxiUpgradeCategory.Fuel,true)]
        [TestCase(TruckTaxiUpgradeCategory.Transmission,false)]
        [TestCase(TruckTaxiUpgradeCategory.Suspension,false)]
        [TestCase(TruckTaxiUpgradeCategory.Tires,false)]
        public void EquipmentOnlyExposesImplementedAdapters(TruckTaxiUpgradeCategory category,bool supported)
        {
            var tuning=UnityEngine.ScriptableObject.CreateInstance<TruckTaxiCareerUpgradeTuning>();
            try
            {
                var definition=new TruckTaxiCareerUpgradeDefinition { category=category,
                    adapterId=TruckTaxiCareerUpgradeTuning.AdapterId,adapterConfiguration=tuning };
                Assert.AreEqual(supported,TruckTaxiCareerEquipment.Supports(definition));
                tuning.multiplier=float.NaN;
                Assert.IsFalse(TruckTaxiCareerEquipment.Supports(definition));
                tuning.multiplier=1.1f; definition.adapterId="unknown";
                Assert.IsFalse(TruckTaxiCareerEquipment.Supports(definition));
            }
            finally { UnityEngine.Object.DestroyImmediate(tuning); }
        }
        [TestCase(0, 0)]
        [TestCase(.89f, 0)]
        [TestCase(.95f, .25f)]
        [TestCase(1, .5f)]
        [TestCase(100, .5f)]
        public void CravingsTrackNeedAndNeverBecomeOpaque(float need,float expected) =>
            Assert.AreEqual(expected, TruckTaxiStatusBars.CravingOpacity(need,.9f,.5f),.001f);
        [Test]
        public void InvalidPresentationValuesFailClosed()
        {
            Assert.AreEqual(0,TruckTaxiStatusBars.CravingOpacity(float.NaN,.9f,.5f));
            Assert.AreEqual(.5f,TruckTaxiStatusBars.CravingOpacity(1,.9f,50));
        }
        [Test]
        public void OfflinePlatformNeverClaimsExternalServiceSuccess()
        {
            ITruckTaxiPlatformCapabilities platform=new TruckTaxiOfflinePlatformCapabilities();
            Assert.AreEqual(TruckTaxiPlatformCapability.None,platform.Available);
            Assert.IsNull(platform.SuggestedDisplayName);
            Assert.IsFalse(platform.TryUnlockAchievement("taxi.first-fare"));
            Assert.IsFalse(platform.TryOpenOverlay());
            Assert.IsFalse(platform.TrySetRichPresence("taxi.driving"));
        }
    }
}

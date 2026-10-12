using NUnit.Framework;
using UnityEngine;

namespace LWS.TruckTaxi.Tests
{
    public sealed class TruckTaxiPlayerCorrectionMenuTests
    {
        [TestCase(0,false,1500)] [TestCase(1,false,1900)]
        [TestCase(1,true,2900)] [TestCase(100,true,6000)]
        public void TowIsBoundedAndDoesNotBundleFuelOrRepairs(float miles,bool overturned,long expected)
        { Assert.AreEqual(expected,TruckTaxiRoadsideAssistance.CalculateTowCost(miles,overturned)); }
        [Test] public void TowRejectsNonFiniteDistanceWithBoundedQuote()
        { Assert.AreEqual(6000,TruckTaxiRoadsideAssistance.CalculateTowCost(float.NaN,false)); }
        [Test] public void VideoValuesAreBoundedAndCopyIsIndependent()
        {
            var settings=new TruckTaxiVideoSettings{width=0,height=0,mode=9,quality=999,frameCap=999,
                msaa=3,renderScale=float.NaN,shadowDistance=float.PositiveInfinity,weatherQuality=999,textureLimit=-2};
            settings.Validate();
            Assert.AreEqual(640,settings.width);Assert.AreEqual(360,settings.height);Assert.AreEqual(2,settings.mode);
            Assert.AreEqual(240,settings.frameCap);Assert.AreEqual(2,settings.msaa);Assert.AreEqual(1,settings.renderScale);
            Assert.AreEqual(150,settings.shadowDistance);Assert.AreEqual(3,settings.weatherQuality);Assert.AreEqual(0,settings.textureLimit);
            var copy=settings.Copy();copy.width=1920;Assert.AreEqual(640,settings.width);
        }
        [Test] public void DebugCashUsesExistingWalletAndPersistentEarnings()
        {
            var config=ScriptableObject.CreateInstance<TruckTaxiConfiguration>();
            try
            {
                var session=new TruckTaxiSession(config,new TruckTaxiRideLocation[0],19);
                long before=session.WalletBalanceCents;int notifications=0;session.Changed+=()=>notifications++;
                Assert.IsTrue(session.DebugGiveCash());Assert.IsTrue(session.DebugGiveCash());Assert.IsTrue(session.DebugGiveCash());
                Assert.AreEqual(before+3000000,session.WalletBalanceCents);Assert.AreEqual(3,notifications);
                Assert.AreEqual(session.ShiftEarnings,session.CapturePersistentState().shiftEarningsCents);
            }
            finally { Object.DestroyImmediate(config); }
        }
    }
}

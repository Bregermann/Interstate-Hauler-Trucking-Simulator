using NUnit.Framework;
using UnityEngine;
using LWS.InterstateHauler;

namespace LWS.TruckTaxi.Tests
{
    [Category("TaxiIntegrated")]
    public sealed class TruckTaxiIntegratedTrafficTests
    {
        [Test]
        public void FollowingEnvelopeAccountsForClosingSpeedAndBraking()
        {
            float stopped=TruckTaxiTrafficBehaviour.SafeFollowingDistance(15,0,1.6f,6,5);
            float flowing=TruckTaxiTrafficBehaviour.SafeFollowingDistance(15,15,1.6f,6,5);
            Assert.Greater(stopped,flowing);
            Assert.Greater(TruckTaxiTrafficBehaviour.SafeFollowingDistance(25,0,1.6f,6,5),stopped);
            Assert.Greater(TruckTaxiTrafficBehaviour.SafeFollowingDistance(15,0,1.6f,3,5),stopped);
        }
        [Test]
        public void RequestedPopulationIsNotSilentlyCappedToOldDefaults()
        {
            var profile=ScriptableObject.CreateInstance<TruckTaxiPopulationProfile>();
            try { Assert.AreEqual(720,profile.PedestrianTarget(12)); Assert.AreEqual(300,profile.TrafficTarget(12)); }
            finally { Object.DestroyImmediate(profile); }
        }
        [Test]
        public void ParallelLanesPreserveAuthorityAndDoNotExpandOtherLevels()
        {
            var lane=new LwsTrafficLaneDefinition { laneId="taxi.loop.0",roadId="taxi.city.loop.0",laneWidthMeters=5,
                centerline=new[] { new Vector3(5,0,0),new Vector3(5,0,10),new Vector3(5,0,20),new Vector3(5,0,30),new Vector3(5,0,40) } };
            var expanded=TruckTaxiTrafficLaneNetwork.ExpandTaxiBoulevards(new[] { lane });
            Assert.AreEqual(2,expanded.Length); Assert.IsTrue(TruckTaxiTrafficLaneNetwork.AreAdjacent(expanded[0],expanded[1]));
            Assert.AreEqual(3.5f,Vector3.Distance(expanded[0].centerline[2],expanded[1].centerline[2]),.001f);
            Assert.AreEqual(5,lane.centerline[2].x);
            lane.roadId="interstate.authored";
            Assert.AreSame(lane,TruckTaxiTrafficLaneNetwork.ExpandTaxiBoulevards(new[] { lane })[0]);
        }
        [Test]
        public void LaneChangesRejectCorners()
        {
            Assert.IsTrue(TruckTaxiTrafficLaneNetwork.IsStraight(Vector3.zero,Vector3.forward,Vector3.forward*2));
            Assert.IsFalse(TruckTaxiTrafficLaneNetwork.IsStraight(Vector3.zero,Vector3.forward,Vector3.forward+Vector3.right));
        }
        [Test]
        public void OwnedStoppedBrakeIsReleasedExactlyOnce()
        {
            var go=new GameObject("Traffic brake fixture");
            try
            {
                go.AddComponent<Rigidbody>();
                var wheel=go.AddComponent<WheelCollider>(); wheel.brakeTorque=float.MaxValue*.8f;
                var behavior=go.AddComponent<TruckTaxiTrafficBehaviour>();
                var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
                typeof(TruckTaxiTrafficBehaviour).GetField("wheels",flags).SetValue(behavior,new[]{wheel});
                typeof(TruckTaxiTrafficBehaviour).GetField("ownsBrakeHold",flags).SetValue(behavior,true);
                var release=typeof(TruckTaxiTrafficBehaviour).GetMethod("ReleaseBrakeHold",flags);
                release.Invoke(behavior,null); Assert.AreEqual(0,wheel.brakeTorque);
                wheel.brakeTorque=123; release.Invoke(behavior,null);
                Assert.AreEqual(123,wheel.brakeTorque,"An unowned brake must not be repeatedly overwritten.");
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}

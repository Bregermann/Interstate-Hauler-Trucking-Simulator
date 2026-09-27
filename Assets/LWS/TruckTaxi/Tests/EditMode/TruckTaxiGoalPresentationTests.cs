using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace LWS.TruckTaxi.Tests
{
    [Category("TaxiIntegrated")]
    public class TruckTaxiGoalPresentationTests
    {
        private readonly List<Object> created=new List<Object>();
        private bool hadPreference;
        private int previousPreference;

        [SetUp] public void SetUp()
        {
            hadPreference=PlayerPrefs.HasKey(TruckTaxiSession.RideRequestsPreferenceKey);
            previousPreference=PlayerPrefs.GetInt(TruckTaxiSession.RideRequestsPreferenceKey);
            PlayerPrefs.SetInt(TruckTaxiSession.RideRequestsPreferenceKey,1);
        }

        [TearDown] public void TearDown()
        {
            foreach(var item in created) Object.DestroyImmediate(item);
            created.Clear();
            if(hadPreference) PlayerPrefs.SetInt(TruckTaxiSession.RideRequestsPreferenceKey,previousPreference);
            else PlayerPrefs.DeleteKey(TruckTaxiSession.RideRequestsPreferenceKey);
        }

        private TruckTaxiSession Session(PassengerRequestDefinition goal)
        {
            var config=ScriptableObject.CreateInstance<TruckTaxiConfiguration>(); created.Add(config);
            var passenger=ScriptableObject.CreateInstance<PassengerProfile>(); created.Add(passenger);
            passenger.passengerName="Passenger"; passenger.basePatience=1000;
            passenger.requestDifficultyRange=Vector2.one; passenger.possibleRequests=new[]{goal};
            config.passengers=new[]{passenger}; config.minimumTripDistance=10; config.maximumTripDistance=1000;
            var stops=new List<TruckTaxiRideLocation>();
            for(int i=0;i<2;i++)
            {
                var go=new GameObject("Stop"); created.Add(go);
                var stop=go.AddComponent<TruckTaxiRideLocation>(); stop.locationId="stop."+i;
                go.transform.position=Vector3.right*i*100; stops.Add(stop);
            }
            var session=new TruckTaxiSession(config,stops,21);
            session.Capabilities.Register(TruckTaxiObjectiveCapability.Shortcuts,10);
            session.StartShift(); Assert.IsTrue(session.OfferRide()); Assert.IsTrue(session.AcceptRide());
            Assert.IsFalse(TruckTaxiPickupZoneVisualizer.HasDropoffTarget(session));
            session.Tick(.1f,session.Pickup.StopPosition,0,0,true);
            session.Tick(2,session.Pickup.StopPosition,0,0,true);
            Assert.AreEqual(TruckTaxiState.DrivingToDestination,session.State);
            Assert.IsTrue(TruckTaxiPickupZoneVisualizer.HasDropoffTarget(session));
            return session;
        }

        [Test] public void CancelledRideCannotSucceedArrivalGoal()
        {
            var goal=ScriptableObject.CreateInstance<PassengerRequestDefinition>(); created.Add(goal);
            goal.requestType=TaxiRequestType.NoCollisions; goal.timer=90;
            var session=Session(goal);
            var progress=session.Requests[0];
            Assert.AreEqual(1,progress.NormalizedProgress);
            session.FailRide("Passenger cancelled before arrival.");
            Assert.AreEqual(TaxiRequestState.Failed,progress.State);
            Assert.AreEqual(0,progress.NormalizedProgress);
            Assert.AreEqual("Passenger cancelled before arrival.",progress.FailureReason);
            Assert.AreEqual(TaxiRequestState.Failed,session.RideHistory[0].Goals[0].State);
            Assert.IsFalse(TruckTaxiPickupZoneVisualizer.HasDropoffTarget(session));
        }

        [Test] public void ArrivalGoalCompletesAtDropoffAndRemainsInHistory()
        {
            var goal=ScriptableObject.CreateInstance<PassengerRequestDefinition>(); created.Add(goal);
            goal.requestType=TaxiRequestType.SmoothRide; goal.timer=90;
            var session=Session(goal);
            var progress=session.Requests[0];
            Assert.AreEqual(TaxiRequestState.Active,progress.State);
            session.Tick(.1f,session.Destination.StopPosition,0,0,true);
            session.Tick(2,session.Destination.StopPosition,0,0,true);
            Assert.AreEqual(TaxiRequestState.Succeeded,progress.State);
            Assert.AreEqual(progress.Target,progress.Progress);
            Assert.AreEqual(1,progress.NormalizedProgress);
            Assert.AreEqual(TaxiRequestState.Succeeded,session.RideHistory[0].Goals[0].State);
            Assert.IsFalse(TruckTaxiPickupZoneVisualizer.HasDropoffTarget(session));
        }

        [Test] public void CountGoalBarUsesMeasuredProgress()
        {
            var goal=ScriptableObject.CreateInstance<PassengerRequestDefinition>(); created.Add(goal);
            goal.requestType=TaxiRequestType.Shortcut; goal.target=4;
            var session=Session(goal);
            var progress=session.Requests[0];
            Assert.AreEqual(0,progress.NormalizedProgress);
            session.RecordEvent(TaxiEventType.Shortcut,"shortcut-a");
            session.RecordEvent(TaxiEventType.Shortcut,"shortcut-b");
            Assert.AreEqual(.5f,progress.NormalizedProgress);
        }
        [Test] public void TemporaryOfferSuppressionDoesNotChangeSavedPreference()
        {
            var goal=ScriptableObject.CreateInstance<PassengerRequestDefinition>();created.Add(goal);
            var session=Session(goal);
            var owner=new object(); session.AcquireOfferSuppression(owner);
            Assert.IsTrue(session.OffersSuppressed);
            Assert.IsTrue(session.RideRequestsEnabled);
            Assert.AreEqual(1,PlayerPrefs.GetInt(TruckTaxiSession.RideRequestsPreferenceKey));
            session.ReleaseOfferSuppression(owner);
            Assert.IsFalse(session.OffersSuppressed);
            Assert.IsTrue(session.RideRequestsEnabled);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Object=UnityEngine.Object;

namespace LWS.TruckTaxi.Tests
{
    public sealed class TruckTaxiObjectiveTests
    {
        private readonly List<Object> allocated=new List<Object>();
        private T Asset<T>() where T:ScriptableObject { var value=ScriptableObject.CreateInstance<T>(); allocated.Add(value); return value; }
        [TearDown] public void Cleanup() { foreach(var value in allocated) Object.DestroyImmediate(value); allocated.Clear(); }
        private TruckTaxiSession Session(out PassengerProfile passenger)
        {
            passenger=Asset<PassengerProfile>(); passenger.basePatience=10000; passenger.requestFrequency=10000; passenger.requestDifficultyRange=Vector2.one;
            var config=Asset<TruckTaxiConfiguration>(); config.minimumTripDistance=10; config.maximumTripDistance=1000; config.passengers=new[]{passenger};
            var stops=new List<TruckTaxiRideLocation>();
            for(int i=0;i<3;i++) { var go=new GameObject("Test ride location"); allocated.Add(go); var point=go.AddComponent<TruckTaxiRideLocation>();
                point.locationId="location."+i; go.transform.position=Vector3.right*i*100; stops.Add(point); }
            return new TruckTaxiSession(config,stops,31);
        }
        private void Board(TruckTaxiSession session)
        { session.StartShift(); Assert.IsTrue(session.OfferRide()); Assert.IsTrue(session.AcceptRide()); session.Tick(.1f,session.Pickup.StopPosition,0,0,true); session.Tick(2,session.Pickup.StopPosition,0,0,true); }
        [Test] public void TenThousandBundlesHaveCapabilitiesNoContradictionsAndStableIds()
        {
            var definitions=Enum.GetValues(typeof(TaxiRequestType)).Cast<TaxiRequestType>().Select(type=> { var d=Asset<PassengerRequestDefinition>(); d.requestType=type; return d; }).ToArray();
            var unsupported=Asset<PassengerRequestDefinition>(); unsupported.additionalCapabilities=TruckTaxiObjectiveCapability.VehicleDestruction;
            var caps=new TruckTaxiObjectiveCapabilities();
            foreach(var d in definitions) caps.Register(d.RequiredCapabilities,10);
            Assert.IsFalse(caps.Supports(unsupported));
            var random=new System.Random(20260925);
            for(int run=0;run<10000;run++)
            {
                var bundle=new List<PassengerRequestDefinition>();
                for(int attempt=0;attempt<12;attempt++)
                {
                    var proposed=bundle.Concat(new[]{definitions[random.Next(definitions.Length)]}).ToList();
                    if(caps.ValidateCombination(proposed,out _)) bundle=proposed;
                    if(bundle.Count==3) break;
                }
                Assert.IsTrue(caps.ValidateCombination(bundle,out string reason),reason);
                Assert.AreEqual(bundle.Count,bundle.Select(d=>d.StableId).Distinct().Count());
                foreach(var d in bundle)
                {
                    var progress=new TaxiRequestProgress(d,(float)(.8+random.NextDouble()*.4));
                    Assert.AreEqual(Mathf.Round(progress.Target),progress.Target);
                    StringAssert.DoesNotContain("fictional pedestrian",progress.Description.ToLowerInvariant());
                    if(d.requestType!=TaxiRequestType.SmoothRide && d.requestType!=TaxiRequestType.NoCollisions)
                        Assert.IsTrue(progress.Description.Contains(progress.TargetText) || progress.Target==1,progress.Description);
                }
            }
        }
        [Test] public void ChaosDescriptionDenominatorAndCompletionShareRoundedTarget()
        {
            var session=Session(out var passenger); var d=Asset<PassengerRequestDefinition>(); d.requestType=TaxiRequestType.MaximumChaos; d.target=400;
            passenger.requestDifficultyRange=new Vector2(.85225f,.85225f); passenger.possibleRequests=new[]{d}; Board(session);
            var request=session.Requests.Single(); Assert.AreEqual(300,request.Target);
            Assert.AreEqual(request.Description,session.Reaction,"Fallback instruction must not retain an authored unscaled number.");
            Assert.AreEqual("Make 300 Chaos points",request.Description); Assert.AreEqual("0/300",request.ProgressText);
            session.RecordEvent(TaxiEventType.Shortcut,"partial",scoreOverride:299); Assert.AreEqual(TaxiRequestState.Active,request.State);
            session.RecordEvent(TaxiEventType.Shortcut,"threshold",scoreOverride:1); Assert.AreEqual(TaxiRequestState.Succeeded,request.State);
            var exact=new TaxiRequestProgress(d,1); Assert.AreEqual("Make 400 Chaos points",exact.Description); Assert.AreEqual("0/400",exact.ProgressText);
        }
        [Test] public void CapabilitiesGateGenerationAndFailedCandidateKeepsRide()
        {
            var session=Session(out var p); Board(session); var offer=session.Offer;
            var d=Asset<PassengerRequestDefinition>(); d.requestType=TaxiRequestType.RamTraffic;
            Assert.IsFalse(session.GenerateRequest(d)); Assert.AreSame(offer,session.Offer); Assert.IsEmpty(session.Requests);
            session.Capabilities.Register(TruckTaxiObjectiveCapability.Traffic,1); Assert.IsTrue(session.GenerateRequest(d));
            var duplicate=Object.Instantiate(d); allocated.Add(duplicate); Assert.IsFalse(session.GenerateRequest(duplicate));
            var clean=Asset<PassengerRequestDefinition>(); clean.requestType=TaxiRequestType.NoCollisions;
            Assert.IsFalse(session.GenerateRequest(clean)); Assert.AreSame(p,session.Passenger); Assert.AreSame(offer.Destination,session.Destination);
        }
        [Test] public void StopProgressIsContinuousAndCannotCompleteByDriveThroughEvent()
        {
            var session=Session(out _); Board(session); var go=new GameObject("Scenic fixture"); allocated.Add(go);
            var stop=go.AddComponent<TruckTaxiStopObjectivePoint>(); stop.stableId="scenic.test"; stop.durationSeconds=12;
            session.Capabilities.Stops.Add(stop); session.Capabilities.Register(TruckTaxiObjectiveCapability.ScenicStops,1);
            var d=Asset<PassengerRequestDefinition>(); d.requestType=TaxiRequestType.ScenicRoute;
            Assert.IsTrue(session.GenerateRequest(d)); var request=session.ActiveStop;
            session.RecordEvent(TaxiEventType.ScenicPoint,stop.stableId); Assert.Zero(request.Progress);
            session.Tick(5,stop.Position,0,0,true); Assert.AreEqual(5,request.Progress);
            session.Tick(1,stop.Position,2,0,true); Assert.Zero(request.Progress);
            session.Tick(12,stop.Position,0,0,true); Assert.AreEqual(TaxiRequestState.Succeeded,request.State);
        }
        [Test] public void TenThousandActualGeneratedRidesRespectVaryingRuntimeCapabilities()
        {
            var session=Session(out var passenger);
            passenger.possibleRequests=Enum.GetValues(typeof(TaxiRequestType)).Cast<TaxiRequestType>().Select(type=>
            { var d=Asset<PassengerRequestDefinition>(); d.requestType=type; return d; }).ToArray();
            foreach(var category in new[]{TruckTaxiStopCategory.Scenic,TruckTaxiStopCategory.IllicitPickup})
            {
                var go=new GameObject("Stop "+category); allocated.Add(go); var point=go.AddComponent<TruckTaxiStopObjectivePoint>();
                point.stableId="test."+category; point.category=category; session.Capabilities.Stops.Add(point);
            }
            var random=new System.Random(707);
            for(int run=0;run<10000;run++)
            {
                foreach(TruckTaxiObjectiveCapability flag in Enum.GetValues(typeof(TruckTaxiObjectiveCapability)))
                    if(flag!=TruckTaxiObjectiveCapability.None) session.Capabilities.Register(flag,random.Next(0,3));
                session.EndShift(); Board(session); var offer=session.Offer;
                for(int attempt=0;attempt<8;attempt++) session.GenerateRequest();
                Assert.AreSame(offer,session.Offer);
                var definitions=session.Requests.Select(r=>r.Definition).ToArray();
                Assert.IsTrue(session.Capabilities.ValidateCombination(definitions,out var reason),reason);
                Assert.AreEqual(definitions.Length,definitions.Select(d=>d.StableId).Distinct().Count());
                foreach(var r in session.Requests)
                {
                    Assert.IsTrue(session.Capabilities.Supports(r.Definition));
                    Assert.LessOrEqual(r.Target,session.Capabilities.TargetLimit(r.Definition.requestType));
                    if(r.Definition.IsStop) Assert.IsNotNull(r.StopPoint);
                }
            }
        }
        [Test] public void StopDurationsUseOneTargetAndDifferentStopsMayFollowCompletion()
        {
            var session=Session(out _); Board(session);
            foreach(var category in new[]{TruckTaxiStopCategory.Scenic,TruckTaxiStopCategory.IllicitPickup})
            {
                var go=new GameObject("Stop"); allocated.Add(go); var point=go.AddComponent<TruckTaxiStopObjectivePoint>();
                point.stableId="test."+category; point.category=category; point.durationSeconds=8;
                session.Capabilities.Stops.Add(point);
            }
            session.Capabilities.Register(TruckTaxiObjectiveCapability.ScenicStops,1);
            session.Capabilities.Register(TruckTaxiObjectiveCapability.IllicitStops,1);
            var scenic=Asset<PassengerRequestDefinition>(); scenic.requestType=TaxiRequestType.ScenicRoute;
            var illicit=Asset<PassengerRequestDefinition>(); illicit.requestType=TaxiRequestType.IllicitStop;
            Assert.IsTrue(session.GenerateRequest(scenic)); Assert.IsFalse(session.GenerateRequest(illicit));
            var request=session.ActiveStop; Assert.AreEqual(8,request.Target); Assert.AreEqual("0/8s",request.ProgressText);
            session.Tick(8,request.StopPoint.Position,0,0,true);
            Assert.AreEqual(TaxiRequestState.Succeeded,request.State); Assert.IsTrue(session.GenerateRequest(illicit));
        }
        [Test] public void CountsCannotExceedAvailableWorldTargets()
        {
            var session=Session(out _); Board(session);
            var definition=Asset<PassengerRequestDefinition>(); definition.requestType=TaxiRequestType.PropertyDamage; definition.target=20;
            session.Capabilities.Register(TruckTaxiObjectiveCapability.DestructibleProps,2);
            Assert.IsTrue(session.GenerateRequest(definition)); Assert.AreEqual(2,session.Requests.Single().Target);
            StringAssert.Contains("2",session.Requests.Single().Description);
        }
        [Test] public void NearbyStopAccessIsNotMisreportedAsAnUnavailableRoute()
        {
            var graph=Editor.TruckTaxiDemoBuilder.CreateGraph(); var position=graph.nodes[25].position;
            var result=new TruckTaxiRouteDistanceService(graph).Measure(position+Vector3.right*2,position);
            Assert.IsTrue(result.Navigable); Assert.AreEqual(2,result.Meters); Assert.AreEqual(2,result.Points.Count);
        }
        [Test] public void RatingsAreDiscreteAndAverageUsesActualStarSum()
        {
            var session=Session(out var p);
            foreach(int stars in new[]{5,4,5,3}) { p.baseSatisfaction=stars; Board(session); session.DebugComplete(); Assert.AreEqual(stars,session.LastFare.Rating); session.ContinueShift(); }
            Assert.AreEqual(17,session.TotalStarsEarned); Assert.AreEqual(4,session.CompletedRides); Assert.AreEqual(4.25,session.DriverAverageRating);
            Assert.AreEqual("4.3",session.DriverAverageText);
            for(float s=1;s<=5;s+=.013f) Assert.That(TruckTaxiSession.StarsForSatisfaction(s),Is.InRange(1,5));
        }
        [Test] public void AppreciationRequiresExplicitAdultFemaleFlirtatiousOptInAndChoice()
        {
            var session=Session(out var p); p.casting=Asset<TruckTaxiCastingProfile>(); p.baseSatisfaction=5;
            p.appreciationChance=1; p.specialAppreciationEligible=true; p.explicitlyAdult=true; p.minimumAdultAge=21;
            p.adultFemalePresentation=true; p.flirtatiousPresentation=true;
            Assert.IsTrue(p.CanOfferAppreciation);
            p.minimumAdultAge=17; Assert.IsFalse(p.CanOfferAppreciation); p.minimumAdultAge=0; Assert.IsFalse(p.CanOfferAppreciation); p.minimumAdultAge=21;
            p.explicitlyAdult=false; Assert.IsFalse(p.CanOfferAppreciation); p.explicitlyAdult=true;
            p.adultFemalePresentation=false; Assert.IsFalse(p.CanOfferAppreciation); p.adultFemalePresentation=true;
            p.flirtatiousPresentation=false; Assert.IsFalse(p.CanOfferAppreciation); p.flirtatiousPresentation=true;
            p.specialAppreciationEligible=false; Assert.IsFalse(p.CanOfferAppreciation); p.specialAppreciationEligible=true;
            Board(session); session.DebugComplete(); Assert.AreEqual(TruckTaxiState.AppreciationOffer,session.State); Assert.AreEqual(0,session.CompletedRides);
            float satisfaction=session.Satisfaction; Assert.IsTrue(session.ChooseAppreciation(false)); Assert.AreEqual(satisfaction,session.Satisfaction);
            Assert.AreEqual(5,session.LastFare.Rating); session.ContinueShift(); Board(session); session.DebugComplete();
            session.ChooseAppreciation(true); Assert.AreEqual(TruckTaxiState.AppreciationSequence,session.State); Assert.AreEqual(1,session.CompletedRides);
            session.CompleteAppreciation(); session.CompleteAppreciation(); Assert.AreEqual(2,session.CompletedRides);
        }
    }
}

using System;
using System.Collections.Generic;
using LWS.InterstateHauler;
using NUnit.Framework;
using UnityEngine;
using Object=UnityEngine.Object;

namespace LWS.TruckTaxi.Tests
{
    [Category("TaxiMorality")]
    public sealed class TruckTaxiMoralityTests
    {
        private readonly List<Object> created=new List<Object>();
        private TruckTaxiConfiguration config;
        private TruckTaxiTownDefinition town;
        private TruckTaxiMoralChoiceDefinition definition;
        private LwsTrailerGameplayIdentity trailer;
        private TruckTaxiRideLocation[] stops;
        private bool hadPreference;
        private int preference;
        private sealed class ContextSource : ITruckTaxiMoralEventContextSource
        {
            public bool available=true;
            public TruckTaxiMoralEventContext context;
            public bool TryGetContext(string eventId,out TruckTaxiMoralEventContext value)
            { value=context; return available; }
        }
        private ContextSource source;
        private T Asset<T>() where T:ScriptableObject
        { var asset=ScriptableObject.CreateInstance<T>(); created.Add(asset); return asset; }
        private GameObject Model(string name)
        { var go=new GameObject(name); created.Add(go); return go; }
        [SetUp] public void Setup()
        {
            hadPreference=PlayerPrefs.HasKey(TruckTaxiSession.RideRequestsPreferenceKey);
            preference=PlayerPrefs.GetInt(TruckTaxiSession.RideRequestsPreferenceKey,1);
            PlayerPrefs.SetInt(TruckTaxiSession.RideRequestsPreferenceKey,1);
            config=Asset<TruckTaxiConfiguration>(); config.minimumTripDistance=10; config.maximumTripDistance=1000;
            config.intercityRideChance=0; config.baseFareCents=500;
            town=Asset<TruckTaxiTownDefinition>(); town.initialState.townId="town.fixture";
            trailer=Model("authored trailer fixture").AddComponent<LwsTrailerGameplayIdentity>();
            trailer.Configure("trailer.fixture",LwsTrailerTypeIds.DryVan,"Test trailer","depot.fixture",true);
            definition=Asset<TruckTaxiMoralChoiceDefinition>();
            definition.eventId="event.people.fixture"; definition.trailerId=trailer.StableTrailerId; definition.town=town;
            definition.options=new[] {
                new TruckTaxiMoralChoiceOption { choiceId=TruckTaxiMoralChoiceDefinition.TurnIn,label="Turn them in",
                    completionObjectiveId="objective.authority",effects=new TruckTaxiMoralChoiceEffects {
                        payoutCents=1200,safety=5,crime=-5,reputation=5,authorityStanding=5,fareMultiplier=.01f } },
                new TruckTaxiMoralChoiceOption { choiceId=TruckTaxiMoralChoiceDefinition.Deliver,label="Deliver them to destination",
                    completionObjectiveId="objective.contact",effects=new TruckTaxiMoralChoiceEffects {
                        payoutCents=1500,safety=-5,crime=5,trafficPressure=5,shadyStanding=5,fareMultiplier=-.01f } }
            };
            source=new ContextSource(); Context();
            stops=new TruckTaxiRideLocation[2];
            for(int i=0;i<2;i++)
            {
                stops[i]=Model("stop "+i).AddComponent<TruckTaxiRideLocation>();
                stops[i].locationId="stop."+i; stops[i].district="fixture";
                stops[i].transform.position=Vector3.right*i*100;
            }
        }
        [TearDown] public void Cleanup()
        {
            for(int i=created.Count-1;i>=0;i--) if(created[i]!=null) Object.DestroyImmediate(created[i]);
            created.Clear();
            if(hadPreference) PlayerPrefs.SetInt(TruckTaxiSession.RideRequestsPreferenceKey,preference);
            else PlayerPrefs.DeleteKey(TruckTaxiSession.RideRequestsPreferenceKey);
        }
        private void Context(string objective=null) => source.context=new TruckTaxiMoralEventContext(
            definition.eventId,town.initialState.townId,trailer,objective);
        private TruckTaxiSession Session()
        {
            var session=new TruckTaxiSession(config,stops,731); session.StartShift();
            Assert.IsTrue(session.BindCareerRun("profile.morality",out _));
            Assert.IsTrue(session.TryInitializeCareer("Driver","truck.fixture",out _));
            session.MoralEventContextSource=source;
            session.SetWorldConditions(0,12,TruckTaxiDemandWeather.Neutral);
            return session;
        }
        private void Accept(TruckTaxiSession session,string choice=TruckTaxiMoralChoiceDefinition.TurnIn)
        {
            Assert.IsTrue(session.TryPresentMoralChoice(definition,out _,out var reason),reason);
            Assert.IsTrue(session.TryAcceptMoralChoice(definition.eventId,choice,out reason),reason);
        }

        [Test] public void PresentationAndAcceptanceAreDetachedAndNeverPay()
        {
            var session=Session();
            Assert.IsTrue(session.TryPresentMoralChoice(definition,out var presentation,out _));
            Assert.IsNull(session.CaptureTownState(town.initialState.townId));
            presentation.options[0].effects.payoutCents=99999;
            definition.options[0].effects.payoutCents=88888;
            Assert.IsTrue(session.TryAcceptMoralChoice(definition.eventId,TruckTaxiMoralChoiceDefinition.TurnIn,out _));
            Assert.AreEqual(0,session.WalletBalanceCents);
            Assert.AreEqual(50,session.CaptureTownState(town.initialState.townId).safety);
            Assert.AreEqual(1200,session.CaptureActiveMoralChoice().effects.payoutCents);
            var detached=session.CaptureActiveMoralChoice(); detached.effects.payoutCents=1;
            Assert.AreEqual(1200,session.CaptureActiveMoralChoice().effects.payoutCents);
            Assert.IsFalse(session.TryAcceptMoralChoice(definition.eventId,TruckTaxiMoralChoiceDefinition.Deliver,out _));
        }
        [Test] public void MissingOrWrongLiveAuthoredContextCannotPresentOrPay()
        {
            var session=Session();
            Assert.IsFalse(session.TryCompleteMoralChoice(definition.eventId,out _));
            session.MoralEventContextSource=null;
            Assert.IsFalse(session.TryPresentMoralChoice(definition,out _,out _));
            session.MoralEventContextSource=source;
            source.context=new TruckTaxiMoralEventContext("wrong.event",town.initialState.townId,trailer);
            Assert.IsFalse(session.TryPresentMoralChoice(definition,out _,out _));
            Context(); Accept(session);
            Context("objective.contact"); Assert.IsFalse(session.TryCompleteMoralChoice(definition.eventId,out _));
            Context("objective.authority"); trailer.gameObject.SetActive(false);
            Assert.IsFalse(session.TryCompleteMoralChoice(definition.eventId,out _));
            trailer.gameObject.SetActive(true);
            trailer.Configure("trailer.other",LwsTrailerTypeIds.DryVan,"Other","depot.fixture",true);
            Assert.IsFalse(session.TryCompleteMoralChoice(definition.eventId,out _));
            Assert.AreEqual(0,session.WalletBalanceCents);
        }
        [TestCase(TruckTaxiMoralChoiceDefinition.TurnIn,"objective.authority",1200,55,45,1.01f)]
        [TestCase(TruckTaxiMoralChoiceDefinition.Deliver,"objective.contact",1500,45,55,.99f)]
        public void CompletionPaysAndAppliesAuthoredConsequencesOnce(string choice,string objective,long payout,float safety,float crime,float fare)
        {
            var session=Session(); Accept(session,choice); Context(objective);
            Assert.IsTrue(session.TryCompleteMoralChoice(definition.eventId,out var reason),reason);
            Assert.AreEqual(payout,session.WalletBalanceCents);
            var state=session.CaptureTownState(town.initialState.townId);
            Assert.AreEqual(safety,state.safety); Assert.AreEqual(crime,state.crime);
            Assert.That(state.fareMultiplier,Is.EqualTo(fare).Within(.00001f));
            if(choice==TruckTaxiMoralChoiceDefinition.TurnIn)
            { Assert.AreEqual(5,state.authorityStanding); Assert.AreEqual(5,state.reputation); }
            else { Assert.AreEqual(5,state.shadyStanding); Assert.AreEqual(55,state.trafficPressure); }
            Assert.IsFalse(session.TryCompleteMoralChoice(definition.eventId,out _));
            Assert.IsFalse(session.TryPresentMoralChoice(definition,out _,out _));
            Assert.AreEqual(payout,session.WalletBalanceCents);
        }
        [Test] public void AcceptedAndCompletedChoicesSurviveJsonRestoreWithoutReplayingPayout()
        {
            var session=Session(); Accept(session);
            var accepted=JsonUtility.FromJson<TruckTaxiPersistentState>(JsonUtility.ToJson(session.CapturePersistentState()));
            var target=Session(); Assert.IsTrue(target.TryRestoreCareerRun("profile.morality",accepted,out var reason),reason);
            Assert.AreEqual(0,target.WalletBalanceCents);
            source.available=false;
            Assert.IsFalse(target.TryCompleteMoralChoice(definition.eventId,out _));
            source.available=true; Context("objective.authority");
            Assert.IsTrue(target.TryCompleteMoralChoice(definition.eventId,out reason),reason);
            var completed=JsonUtility.FromJson<TruckTaxiPersistentState>(JsonUtility.ToJson(target.CapturePersistentState()));
            Assert.IsTrue(target.TryRestoreCareerRun("profile.morality",completed,out reason),reason);
            Assert.IsTrue(target.TryRestoreCareerRun("profile.morality",completed,out reason),reason);
            Assert.IsFalse(target.TryCompleteMoralChoice(definition.eventId,out _));
            Assert.AreEqual(1200,target.WalletBalanceCents);
            accepted.morality.choices.Clear(); completed.morality.towns.Clear();
            Assert.AreEqual(1,target.CaptureMoralityState().choices.Count);
            Assert.AreEqual(1,target.CaptureMoralityState().towns.Count);
        }
        [Test] public void CompletionNotificationSeesAtomicEffectsAndRejectsReentrantPaymentOrRestore()
        {
            var session=Session(); Accept(session); Context("objective.authority");
            var before=session.CapturePersistentState(); int notifications=0;
            session.Changed+=()=> {
                notifications++;
                Assert.AreEqual(1200,session.WalletBalanceCents);
                Assert.AreEqual(55,session.CaptureTownState(town.initialState.townId).safety);
                Assert.IsNull(session.CaptureActiveMoralChoice());
                Assert.IsFalse(session.TryCompleteMoralChoice(definition.eventId,out _));
                Assert.IsFalse(session.TryRestoreCareerRun("profile.morality",before,out _));
            };
            Assert.IsTrue(session.TryCompleteMoralChoice(definition.eventId,out _));
            Assert.AreEqual(1,notifications);
        }
        [Test] public void TownBoundsClampAndRegisterNeverReseedsSavedEffects()
        {
            town.initialState.safety=99; town.initialState.crime=1; town.initialState.fareMultiplier=1.099f;
            var session=Session(); Accept(session); Context("objective.authority");
            Assert.IsTrue(session.TryCompleteMoralChoice(definition.eventId,out _));
            town.initialState.safety=0;
            Assert.IsTrue(session.TryRegisterTown(town,out _));
            var state=session.CaptureTownState(town.initialState.townId);
            Assert.AreEqual(100,state.safety); Assert.AreEqual(0,state.crime); Assert.AreEqual(1.1f,state.fareMultiplier);
        }
        [TestCase(.9f)]
        [TestCase(1.1f)]
        public void SignedTownFareAdjustmentIsFrozenAndAdditive(float factor)
        {
            var session=Session(); town.initialState.fareMultiplier=factor;
            Assert.IsTrue(session.TryRegisterTown(town,out _));
            session.TownIdAtPosition=position=>town.initialState.townId;
            session.FareModifiersProvider=(a,b)=>new TruckTaxiFareModifiers(1.2f,1,1);
            var passenger=Asset<PassengerProfile>(); passenger.passengerId="passenger.fixture";
            Assert.IsTrue(session.OfferRide(passenger));
            Assert.That(session.DemandFareMultiplier,Is.EqualTo(1.2f+factor-1).Within(.0001f));
            Assert.IsTrue(session.AcceptRide());
            session.TownIdAtPosition=position=>"unknown";
            var fare=session.EstimateFare();
            Assert.AreEqual((long)Math.Round(500*(factor-1)),fare.TownBonus);
            Assert.AreEqual(600+fare.TownBonus,fare.Total);
            Assert.AreEqual(factor,session.AcceptedTownFareMultiplier);
            Assert.AreEqual(1,session.TownFareMultiplier("unknown"));
        }
        [TestCase(1)]
        [TestCase(2)]
        public void LegacyRestoreResetsTownDataAndClearsStalePresentation(int version)
        {
            var session=Session(); Assert.IsTrue(session.TryPresentMoralChoice(definition,out _,out _));
            Assert.IsTrue(session.TryRegisterTown(town,out _));
            var saved=session.CapturePersistentState(); saved.version=version; saved.morality=null;
            Assert.IsTrue(session.TryRestoreCareerRun("profile.morality",saved,out var reason),reason);
            Assert.IsEmpty(session.CaptureMoralityState().towns);
            Assert.IsFalse(session.TryAcceptMoralChoice(definition.eventId,TruckTaxiMoralChoiceDefinition.TurnIn,out _));
            Assert.AreEqual(1,session.AcceptedTownFareMultiplier);
        }
        [Test] public void InvalidStateOrOverflowCannotPartiallyPayOrChangeTown()
        {
            var session=Session(); Accept(session); Context("objective.authority");
            var saved=session.CapturePersistentState(); saved.shiftEarningsCents=long.MaxValue;
            Assert.IsTrue(session.TryRestoreCareerRun("profile.morality",saved,out var reason),reason);
            string before=JsonUtility.ToJson(session.CapturePersistentState());
            Assert.IsFalse(session.TryCompleteMoralChoice(definition.eventId,out _));
            Assert.AreEqual(before,JsonUtility.ToJson(session.CapturePersistentState()));
            saved.morality.towns[0].safety=float.NaN;
            Assert.IsFalse(session.TryRestoreCareerRun("profile.morality",saved,out _));
            Assert.AreEqual(before,JsonUtility.ToJson(session.CapturePersistentState()));
        }
        [Test] public void DuplicateOrUnboundedAuthoredChoicesAreRejected()
        {
            var session=Session(); definition.options[0].effects.fareMultiplier=.5f;
            Assert.IsFalse(session.TryPresentMoralChoice(definition,out _,out _));
            definition.options[0].effects.fareMultiplier=.01f;
            definition.options[1].choiceId=definition.options[0].choiceId;
            Assert.IsFalse(session.TryPresentMoralChoice(definition,out _,out _));
            Assert.AreEqual(0,session.WalletBalanceCents);
        }
        [TestCase("duplicate-event")]
        [TestCase("orphan-town")]
        [TestCase("invalid-choice")]
        [TestCase("unearned-payout")]
        public void MalformedMoralRecordsRejectBeforeRestoringAnyLiveState(string corruption)
        {
            var session=Session(); Accept(session);
            var snapshot=session.CapturePersistentState();
            string before=JsonUtility.ToJson(snapshot);
            switch(corruption)
            {
                case "duplicate-event": snapshot.morality.choices.Add(snapshot.morality.choices[0].Copy()); break;
                case "orphan-town": snapshot.morality.towns.Clear(); break;
                case "invalid-choice": snapshot.morality.choices[0].choiceId="unknown"; break;
                case "unearned-payout": snapshot.morality.choices[0].completed=true; break;
            }
            Assert.IsFalse(session.TryRestoreCareerRun("profile.morality",snapshot,out _));
            Assert.AreEqual(before,JsonUtility.ToJson(session.CapturePersistentState()));
        }

        [Test] public void DifferentProfileDoesNotInheritTownOrAcceptedEvent()
        {
            var session=Session(); Accept(session);
            var other=Session().CapturePersistentState(); other.ownedProfile.runProfileId="profile.other";
            Assert.IsTrue(session.TryRestoreCareerRun("profile.other",other,out var reason),reason);
            Assert.IsNull(session.CaptureActiveMoralChoice());
            Assert.IsEmpty(session.CaptureMoralityState().towns);
            Context("objective.authority"); Assert.IsFalse(session.TryCompleteMoralChoice(definition.eventId,out _));
        }
    }
}

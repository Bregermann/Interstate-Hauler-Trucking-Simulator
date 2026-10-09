using System;
using System.Collections.Generic;
using LWS.InterstateHauler;
using NUnit.Framework;
using UnityEngine;
using Object=UnityEngine.Object;

namespace LWS.TruckTaxi.Tests
{
    [Category("TaxiMoralityWorld")]
    public sealed class TruckTaxiMoralityWorldTests
    {
        private readonly List<Object> created=new List<Object>();
        private TruckTaxiMoralTrailerEvent authored;
        private Rigidbody truckBody, trailerBody;
        private LwsTrailerGameplayIdentity trailer;
        private TruckTaxiMoralChoiceDefinition definition;
        private TruckTaxiMoralDestination destination;
        private T Asset<T>() where T:ScriptableObject
        { var item=ScriptableObject.CreateInstance<T>(); created.Add(item); return item; }
        private GameObject SceneObject(string name)
        { var item=new GameObject(name); created.Add(item); return item; }
        [SetUp] public void Setup()
        {
            truckBody=SceneObject("existing truck body").AddComponent<Rigidbody>();
            trailerBody=SceneObject("existing trailer body").AddComponent<Rigidbody>();
            trailer=trailerBody.gameObject.AddComponent<LwsTrailerGameplayIdentity>();
            trailer.Configure("trailer.authored",LwsTrailerTypeIds.DryVan,"Fixture","depot.fixture",true);
            definition=Asset<TruckTaxiMoralChoiceDefinition>(); definition.eventId="event.authored"; definition.trailerId=trailer.StableTrailerId;
            definition.town=Asset<TruckTaxiTownDefinition>(); definition.town.initialState.townId="town.authored";
            definition.options=new[] {
                new TruckTaxiMoralChoiceOption { choiceId=TruckTaxiMoralChoiceDefinition.TurnIn,label="Turn them in",completionObjectiveId="objective.authority" },
                new TruckTaxiMoralChoiceOption { choiceId=TruckTaxiMoralChoiceDefinition.Deliver,label="Deliver",completionObjectiveId="objective.contact" }
            };
            destination=new TruckTaxiMoralDestination { objectiveId="objective.authority",center=SceneObject("authored authority destination").transform,radius=20 };
            authored=trailer.gameObject.AddComponent<TruckTaxiMoralTrailerEvent>(); authored.trailer=trailer; authored.definition=definition;
            authored.destinations=new[] { destination,new TruckTaxiMoralDestination {
                objectiveId="objective.contact",center=SceneObject("authored contact destination").transform,radius=20 } };
        }
        [TearDown] public void Cleanup()
        {
            for(int i=created.Count-1;i>=0;i--) if(created[i]!=null) Object.DestroyImmediate(created[i]);
            created.Clear();
        }
        private TruckTaxiMoralPhysicalEvidence Evidence(bool attached=true,string towingId="truck.actual",LwsTrailerGameplayIdentity actual=null) =>
            new TruckTaxiMoralPhysicalEvidence(attached,towingId,actual ?? trailer,truckBody,trailerBody);
        private TruckTaxiAcceptedMoralChoice Accepted() => new TruckTaxiAcceptedMoralChoice {
            eventId=definition.eventId,trailerId=trailer.StableTrailerId,townId=definition.town.initialState.townId,
            choiceId=TruckTaxiMoralChoiceDefinition.TurnIn,completionObjectiveId=destination.objectiveId,effects=new TruckTaxiMoralChoiceEffects()
        };

        [Test] public void PhysicalPolicyRequiresExactTrailerInstanceAndTowingOwner()
        {
            Assert.IsTrue(TruckTaxiMoralArrivalPolicy.Matches(trailer,"truck.actual",Evidence()));
            Assert.IsFalse(TruckTaxiMoralArrivalPolicy.Matches(trailer,"truck.actual",Evidence(false)));
            Assert.IsFalse(TruckTaxiMoralArrivalPolicy.Matches(trailer,"truck.actual",Evidence(true,"truck.other")));
            var duplicate=SceneObject("different trailer same ID").AddComponent<LwsTrailerGameplayIdentity>();
            duplicate.Configure(trailer.StableTrailerId,LwsTrailerTypeIds.DryVan,"Duplicate","depot.fixture",true);
            Assert.IsFalse(TruckTaxiMoralArrivalPolicy.Matches(trailer,"truck.actual",Evidence(actual:duplicate)));
        }
        [Test] public void DynamicBodiesAndActiveExistingIdentityAreMandatory()
        {
            trailerBody.isKinematic=true;
            Assert.IsFalse(TruckTaxiMoralArrivalPolicy.Matches(trailer,"truck.actual",Evidence()));
            trailerBody.isKinematic=false; truckBody.detectCollisions=false;
            Assert.IsFalse(TruckTaxiMoralArrivalPolicy.Matches(trailer,"truck.actual",Evidence()));
            truckBody.detectCollisions=true; trailer.enabled=false;
            Assert.IsFalse(TruckTaxiMoralArrivalPolicy.Matches(trailer,"truck.actual",Evidence()));
            trailer.enabled=true;
            Assert.IsFalse(TruckTaxiMoralArrivalPolicy.Matches(trailer,"truck.actual",
                new TruckTaxiMoralPhysicalEvidence(true,"truck.actual",trailer,truckBody,truckBody)));
        }
        [TestCase("truck-speed")]
        [TestCase("trailer-speed")]
        [TestCase("truck-spin")]
        [TestCase("trailer-spin")]
        public void BothBodiesMustRemainStationary(string condition)
        {
            switch(condition)
            {
                case "truck-speed": truckBody.linearVelocity=Vector3.right; break;
                case "trailer-speed": trailerBody.linearVelocity=Vector3.right; break;
                case "truck-spin": truckBody.angularVelocity=Vector3.up; break;
                case "trailer-spin": trailerBody.angularVelocity=Vector3.up; break;
            }
            Assert.IsFalse(TruckTaxiMoralArrivalPolicy.Arrived(destination,Evidence(),.2f));
        }
        [TestCase("truck-radius")]
        [TestCase("trailer-radius")]
        [TestCase("height")]
        public void ArrivalRequiresBothBodiesWithinAuthoredRadiusAndHeight(string condition)
        {
            Assert.IsTrue(TruckTaxiMoralArrivalPolicy.Arrived(destination,Evidence(),.2f));
            if(condition=="truck-radius") truckBody.position=Vector3.right*21;
            if(condition=="trailer-radius") trailerBody.position=Vector3.right*21;
            if(condition=="height") trailerBody.position=Vector3.up*4;
            Assert.IsFalse(TruckTaxiMoralArrivalPolicy.Arrived(destination,Evidence(),.2f));
        }
        [Test] public void ArrivalGateRequiresContinuousPhysicsTimeAndResetsOnContextChange()
        {
            var gate=new TruckTaxiMoralArrivalGate();
            for(int i=0;i<25;i++) gate.Observe("runA:event:choice",true,.1f,2);
            Assert.IsTrue(gate.IsReady("runA:event:choice",2));
            Assert.IsFalse(gate.IsReady("runB:event:choice",2));
            gate.Observe("runB:event:choice",true,.1f,2);
            Assert.IsFalse(gate.IsReady("runB:event:choice",2));
            gate.Observe("runB:event:choice",true,20,2);
            Assert.IsFalse(gate.IsReady("runB:event:choice",2),"A wall-clock jump cannot prove stationary arrival.");
            for(int i=0;i<25;i++) gate.Observe("runB:event:choice",true,.1f,2);
            gate.Observe("runB:event:choice",false,.02f,2);
            Assert.IsFalse(gate.IsReady("runB:event:choice",2));
        }
        [Test] public void AcceptedChoiceRebindsOnlySameExistingAuthoredTrailerTownAndObjective()
        {
            var accepted=Accepted();
            Assert.IsTrue(TruckTaxiMoralTrailerEvent.TryResolveAccepted(new[] { authored },accepted,out var found,out var target,out _));
            Assert.AreSame(authored,found); Assert.AreSame(destination,target);
            accepted.trailerId="trailer.changed";
            Assert.IsFalse(TruckTaxiMoralTrailerEvent.TryResolveAccepted(new[] { authored },accepted,out _,out _,out _));
            accepted=Accepted(); accepted.townId="town.changed";
            Assert.IsFalse(TruckTaxiMoralTrailerEvent.TryResolveAccepted(new[] { authored },accepted,out _,out _,out _));
            accepted=Accepted(); accepted.completionObjectiveId="objective.contact";
            Assert.IsFalse(TruckTaxiMoralTrailerEvent.TryResolveAccepted(new[] { authored },accepted,out _,out _,out _));
        }
        [Test] public void MissingDisabledAndDuplicateBindingsFailClosedWithoutCreatingContent()
        {
            var accepted=Accepted();
            Assert.IsFalse(TruckTaxiMoralTrailerEvent.TryResolveAccepted(Array.Empty<TruckTaxiMoralTrailerEvent>(),accepted,out _,out _,out _));
            authored.enabled=false;
            Assert.IsFalse(TruckTaxiMoralTrailerEvent.TryResolveAccepted(new[] { authored },accepted,out _,out _,out _));
            authored.enabled=true;
            Assert.IsFalse(TruckTaxiMoralTrailerEvent.TryResolveAccepted(new[] { authored,authored },accepted,out _,out _,out _));
            Assert.AreEqual("trailer.authored",trailer.StableTrailerId);
        }
        [Test] public void AuthoringRejectsMissingNwhAndInvalidDestinations()
        {
            Assert.IsTrue(authored.ValidateAuthored(out var reason),reason);
            Assert.IsFalse(authored.ValidatePhysical(out _),"Validation must not add an NWH vehicle to the fixture.");
            authored.destinations[1].objectiveId=destination.objectiveId;
            Assert.IsFalse(authored.ValidateAuthored(out _));
            authored.destinations[1].objectiveId="objective.contact";
            destination.radius=float.NaN;
            Assert.IsFalse(authored.ValidateAuthored(out _));
        }
        [Test] public void UninitializedControllerCannotInventAContext()
        {
            var controller=SceneObject("uninitialized morality controller").AddComponent<TruckTaxiMoralityWorldController>();
            controller.authoredEvents=new[] { authored };
            Assert.IsFalse(controller.TryGetContext(definition.eventId,out _));
            Assert.IsFalse(controller.Initialize(null,null));
            Assert.IsFalse(controller.Open());
        }
        [Test] public void NavigationReleaseTokenCannotClearAnotherOwnerOrReplacement()
        {
            var intents=new TruckTaxiNavigationIntents<string>();
            var moral=intents.Set(TruckTaxiNavigationLayer.RideFinal,"taxi-morality","objective.authority","moral route");
            var companion=intents.Set(TruckTaxiNavigationLayer.CompanionTask,"companion","stop.companion","companion route");
            Assert.IsTrue(intents.Release(moral.Layer,moral.OwnerId,moral.TargetId,moral.Token));
            Assert.AreSame(companion,intents.Active);
            var replacement=intents.Set(TruckTaxiNavigationLayer.RideFinal,"ride","ride.destination","ride route");
            Assert.IsFalse(intents.Release(moral.Layer,moral.OwnerId,moral.TargetId,moral.Token));
            Assert.AreSame(replacement,intents.Get(TruckTaxiNavigationLayer.RideFinal));
        }
        [Test] public void MoralityNotificationsPublishInstalledStateAndRestoreBeforeGeneralChange()
        {
            var config=Asset<TruckTaxiConfiguration>();
            var session=new TruckTaxiSession(config,Array.Empty<TruckTaxiRideLocation>(),33);
            Assert.IsTrue(session.BindCareerRun("profile.events",out _));
            Assert.IsTrue(session.TryInitializeCareer("Driver","truck.fixture",out _));
            var order=new List<string>();
            session.MoralityChanged+=()=> {
                Assert.IsNotNull(session.CaptureTownState("town.authored")); order.Add("morality");
            };
            session.CareerEquipmentChanged+=()=>order.Add("equipment");
            session.Changed+=()=>order.Add("general");
            Assert.IsTrue(session.TryRegisterTown(definition.town,out _));
            CollectionAssert.AreEqual(new[] { "morality","general" },order);
            order.Clear();
            var saved=session.CapturePersistentState();
            Assert.IsTrue(session.TryRestoreCareerRun("profile.events",saved,out var reason),reason);
            CollectionAssert.AreEqual(new[] { "morality","equipment","general" },order);
            order.Clear(); saved.morality.version=99;
            Assert.IsFalse(session.TryRestoreCareerRun("profile.events",saved,out _));
            Assert.IsEmpty(order);
        }
    }
}

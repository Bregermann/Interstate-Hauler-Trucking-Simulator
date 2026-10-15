using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace LWS.InterstateHauler.Tests.EditMode
{
    public sealed class LwsDeliveryEditModeTests
    {
        private const string DeliveryServicePath = "Assets/LWS/InterstateHauler/Gameplay/Delivery/LwsDeliveryService.cs";
        private const string DeliveryRuntimePath = "Assets/LWS/InterstateHauler/Gameplay/Delivery/LwsDeliveryRuntime.cs";
        private const string BootstrapPath = "Assets/LWS/InterstateHauler/Bootstrap/LwsApplicationBootstrap.cs";

        [Test]
        public void StableDestinationAndTrailerMatchingRejectWrongIdentity()
        {
            Assert.IsTrue(LwsDeliveryValidation.DestinationMatches(
                " destination.validation.warehouse ", "destination.validation.warehouse"));
            Assert.IsFalse(LwsDeliveryValidation.DestinationMatches(
                "destination.validation.warehouse", "destination.validation.service-annex"));
            Assert.IsTrue(LwsDeliveryValidation.AssignedTrailerMatches(
                "trailer.validation.dryvan", " trailer.validation.dryvan "));
            Assert.IsFalse(LwsDeliveryValidation.AssignedTrailerMatches(
                "trailer.validation.dryvan", "trailer.validation.other-dryvan"));
            Assert.IsFalse(LwsDeliveryValidation.AssignedTrailerMatches(
                "trailer.validation.dryvan", string.Empty));
        }

        [Test]
        public void HeadingAndContainmentValidationUsesTolerances()
        {
            Vector3 pitchedForward = Quaternion.Euler(25f, 8f, 0f) * Vector3.forward;
            float error = LwsDeliveryValidation.CalculateHorizontalHeadingError(
                pitchedForward, Vector3.forward, false);
            Assert.That(error, Is.EqualTo(8f).Within(0.1f));
            Assert.IsTrue(LwsDeliveryValidation.IsHeadingValid(error, 12f));
            Assert.IsFalse(LwsDeliveryValidation.IsHeadingValid(error, 5f));
            Assert.That(
                LwsDeliveryValidation.CalculateHorizontalHeadingError(
                    Vector3.back, Vector3.forward, true),
                Is.EqualTo(0f).Within(0.01f));

            Vector3[] points =
            {
                new Vector3(-2f, 0f, -8f),
                new Vector3(2f, 0f, -8f),
                new Vector3(-2f, 0f, 8f),
                new Vector3(2f, 0f, 8f),
                Vector3.zero,
                new Vector3(2.9f, 0f, 0f)
            };
            float strict = LwsDeliveryValidation.CalculateContainmentRatio(
                points, new Vector3(5.5f, 5.5f, 18f), 0f);
            float tolerant = LwsDeliveryValidation.CalculateContainmentRatio(
                points, new Vector3(5.5f, 5.5f, 18f), 0.35f);
            Assert.That(strict, Is.EqualTo(5f / 6f).Within(0.001f));
            Assert.That(tolerant, Is.EqualTo(1f).Within(0.001f));
        }

        [Test]
        public void StoppedAndValidHoldRequireContinuousSettling()
        {
            Assert.IsTrue(LwsDeliveryValidation.IsStopped(0.2f, 2f, 0.35f, 5f));
            Assert.IsFalse(LwsDeliveryValidation.IsStopped(0.4f, 2f, 0.35f, 5f));
            Assert.IsFalse(LwsDeliveryValidation.IsStopped(0.2f, 6f, 0.35f, 5f));

            float hold = LwsDeliveryValidation.AdvanceValidHold(0f, true, 0.8f);
            hold = LwsDeliveryValidation.AdvanceValidHold(hold, true, 0.7f);
            Assert.That(hold, Is.EqualTo(1.5f).Within(0.001f));
            Assert.AreEqual(0f, LwsDeliveryValidation.AdvanceValidHold(hold, false, 0.1f));
        }

        [Test]
        public void ActiveJobTransitionsThroughDeliveryAndRetainsAcceptedSemantics()
        {
            CreateServices(out ILwsGameplayStateService gameplay, out ILwsActiveJobService jobs);
            PrepareHaul(jobs);
            LwsActiveJob haul = jobs.CurrentActiveJob;
            string activeId = haul.stableActiveJobId;
            string sourceId = haul.sourceJobDefinitionId;
            int quotedGross = haul.quotedGrossPayCents;

            Assert.IsTrue(jobs.EnterDelivery(
                haul.destinationId, haul.trailerAssignment.stableTrailerId, "arrive").Succeeded);
            Assert.AreEqual(LwsJobStatus.Delivery, jobs.CurrentActiveJob.status);
            Assert.AreEqual(LwsGameplayState.Delivery, gameplay.CurrentState);
            Assert.IsTrue(gameplay.AllowsDrivingInput);

            Assert.IsTrue(jobs.CompleteDelivery(CreateCompletion(jobs.CurrentActiveJob), "parked").Succeeded);
            Assert.AreEqual(LwsJobStatus.Completed, jobs.CurrentActiveJob.status);
            Assert.AreEqual(LwsGameplayState.DeliveryResults, gameplay.CurrentState);
            Assert.IsFalse(gameplay.AllowsDrivingInput);
            Assert.AreEqual(activeId, jobs.CurrentActiveJob.stableActiveJobId);
            Assert.AreEqual(sourceId, jobs.CurrentActiveJob.sourceJobDefinitionId);
            Assert.AreEqual(quotedGross, jobs.CurrentActiveJob.quotedGrossPayCents);
            Assert.IsTrue(jobs.CurrentActiveJob.deliveryCompletion.IsValid);
        }

        [Test]
        public void WrongDestinationTrailerAndCompletionAreRejected()
        {
            CreateServices(out _, out ILwsActiveJobService jobs);
            PrepareHaul(jobs);
            LwsActiveJob haul = jobs.CurrentActiveJob;

            Assert.IsFalse(jobs.EnterDelivery(
                "destination.wrong", haul.trailerAssignment.stableTrailerId, "wrong destination").Succeeded);
            Assert.IsFalse(jobs.EnterDelivery(
                haul.destinationId, "trailer.wrong", "wrong trailer").Succeeded);
            Assert.AreEqual(LwsJobStatus.HaulActive, jobs.CurrentActiveJob.status);

            Assert.IsTrue(jobs.EnterDelivery(
                haul.destinationId, haul.trailerAssignment.stableTrailerId, "arrive").Succeeded);
            LwsDeliveryCompletionSnapshot wrong = CreateCompletion(jobs.CurrentActiveJob);
            wrong.assignedTrailerId = "trailer.wrong";
            Assert.IsFalse(jobs.CompleteDelivery(wrong, "wrong completion").Succeeded);
            Assert.AreEqual(LwsJobStatus.Delivery, jobs.CurrentActiveJob.status);
        }

        [Test]
        public void DeliveryAndCompletedJobsDeriveCorrectStateAfterRestore()
        {
            CreateServices(out _, out ILwsActiveJobService source);
            PrepareHaul(source);
            LwsActiveJob haul = source.CurrentActiveJob;
            Assert.IsTrue(source.EnterDelivery(
                haul.destinationId, haul.trailerAssignment.stableTrailerId, "arrive").Succeeded);
            LwsActiveJob deliverySave = source.CurrentActiveJob;

            CreateServices(out ILwsGameplayStateService deliveryState, out ILwsActiveJobService deliveryRestore);
            Assert.IsTrue(deliveryRestore.RestoreActiveJob(deliverySave, "restore delivery", true).Succeeded);
            Assert.AreEqual(LwsGameplayState.Delivery, deliveryState.CurrentState);

            Assert.IsTrue(source.CompleteDelivery(CreateCompletion(source.CurrentActiveJob), "complete").Succeeded);
            LwsActiveJob completedSave = source.CurrentActiveJob;
            CreateServices(out ILwsGameplayStateService resultsState, out ILwsActiveJobService resultsRestore);
            Assert.IsTrue(resultsRestore.RestoreActiveJob(completedSave, "restore completed", true).Succeeded);
            Assert.AreEqual(LwsGameplayState.DeliveryResults, resultsState.CurrentState);
            Assert.AreEqual(completedSave.stableActiveJobId, resultsRestore.CurrentActiveJob.stableActiveJobId);
        }

        [Test]
        public void CompletionSnapshotRoundTripsWithoutPersistingGameplayState()
        {
            CreateServices(out _, out ILwsActiveJobService jobs);
            PrepareHaul(jobs);
            LwsActiveJob haul = jobs.CurrentActiveJob;
            Assert.IsTrue(jobs.EnterDelivery(
                haul.destinationId, haul.trailerAssignment.stableTrailerId, "arrive").Succeeded);
            Assert.IsTrue(jobs.CompleteDelivery(CreateCompletion(jobs.CurrentActiveJob), "complete").Succeeded);

            string json = JsonUtility.ToJson(jobs.CurrentActiveJob);
            StringAssert.Contains("\"deliveryCompletion\"", json);
            StringAssert.Contains("\"completedSuccessfully\":true", json);
            Assert.IsFalse(json.Contains("LwsGameplayState"));

            LwsActiveJob restored = JsonUtility.FromJson<LwsActiveJob>(json);
            Assert.IsTrue(restored.IsValid);
            Assert.AreEqual(LwsJobStatus.Completed, restored.status);
            Assert.IsTrue(restored.deliveryCompletion.IsValid);
        }

        [Test]
        public void BootstrapRegistersOneDeliveryAuthorityAndSourceKeepsBoundaries()
        {
            LwsServiceRegistry registry = LwsApplicationBootstrap.CreateDefaultRegistry();
            Assert.AreEqual(1, registry.Registrations.Count(r => r.Service is ILwsDeliveryService));

            string service = File.ReadAllText(DeliveryServicePath);
            string runtime = File.ReadAllText(DeliveryRuntimePath);
            string bootstrap = File.ReadAllText(BootstrapPath);
            StringAssert.Contains("ILwsActiveJobService", service);
            StringAssert.Contains("ILwsTrailerRegistryService", service);
            StringAssert.Contains("ILwsNavigationService", service);
            StringAssert.Contains("RequestAutosave(\"delivery.entered\")", service);
            StringAssert.Contains("RequestAutosave(\"delivery.completed\")", service);
            StringAssert.Contains("LwsDeliveryParkingZone", runtime);
            StringAssert.Contains("ILwsDeliveryService", bootstrap);
            Assert.IsFalse(service.Contains("System.IO"));
            Assert.IsFalse(service.Contains("SaveToSlot"));
            Assert.IsFalse(service.Contains("CompassPro"));
            Assert.IsFalse(service.Contains("FindObjectsOfType"));
            Assert.IsFalse(runtime.Contains("FindObjectsOfType"));
        }

        private static void CreateServices(
            out ILwsGameplayStateService gameplay,
            out ILwsActiveJobService jobs)
        {
            var registry = new LwsServiceRegistry();
            gameplay = registry.Register<ILwsGameplayStateService>(new LwsGameplayStateService());
            jobs = registry.Register<ILwsActiveJobService>(
                new LwsActiveJobService(), typeof(ILwsGameplayStateService));
            Assert.IsTrue(registry.InitializeAll().Succeeded);
            Assert.IsTrue(gameplay.EnterFreeDrive("test ready").Succeeded);
        }

        private static void PrepareHaul(ILwsActiveJobService jobs)
        {
            var offer = new LwsJobOffer
            {
                stableOfferId = "offer.delivery.test",
                jobDefinitionId = "job.delivery.test",
                originDepotId = "depot.validation.interstate-corridor",
                destinationId = "destination.validation.warehouse",
                cargoId = "cargo.general-freight",
                requiredTrailerTypeId = LwsTrailerTypeIds.DryVan,
                cargoWeightLbs = 32000,
                estimatedDistanceMiles = 12f,
                quotedGrossPayCents = 125000
            };
            Assert.IsTrue(jobs.AcceptJob(offer, out _, "accept").Succeeded);
            LwsTrailerAssignment assignment = LwsTrailerAssignment.Create(
                "trailer.validation.delivery-dryvan",
                LwsTrailerTypeIds.DryVan,
                "Validation Dry Van",
                offer.originDepotId,
                true,
                new LwsWorldPositionD(0d, 0d, 20d));
            Assert.IsTrue(jobs.AssignTrailerToActiveJob(assignment, "assign").Succeeded);
            Assert.IsTrue(jobs.CompleteTrailerPickup(assignment.stableTrailerId, "pickup").Succeeded);
        }

        private static LwsDeliveryCompletionSnapshot CreateCompletion(LwsActiveJob job)
        {
            return new LwsDeliveryCompletionSnapshot
            {
                completedSuccessfully = true,
                activeJobId = job.stableActiveJobId,
                destinationId = job.destinationId,
                assignedTrailerId = job.trailerAssignment.stableTrailerId,
                completedUtcTicks = System.DateTime.UtcNow.Ticks,
                containmentRatio = 1f,
                headingErrorDegrees = 1.5f,
                lateralOffsetMeters = 0.1f,
                longitudinalOffsetMeters = -0.2f,
                validHoldSeconds = 2f,
                deliveryContextDurationSeconds = 45f,
                validationMode = "assigned-trailer-coupled-auto-hold"
            };
        }
    }
}

using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace LWS.InterstateHauler.Tests.PlayMode
{
    public sealed class LwsDeliveryPlayModeTests
    {
        [UnityTest]
        public IEnumerator ParkingZoneEvaluatesColliderHeadingAndMotion()
        {
            GameObject zoneObject = new GameObject("Delivery Parking Zone Test");
            GameObject trailerObject = new GameObject("Assigned Trailer Test");
            try
            {
                LwsDeliveryParkingZone zone = zoneObject.AddComponent<LwsDeliveryParkingZone>();
                zone.Configure(
                    new Vector3(5.5f, 5.5f, 18f),
                    0.35f,
                    0.8f,
                    12f,
                    false,
                    0.35f,
                    5f,
                    2f,
                    false);

                BoxCollider collider = trailerObject.AddComponent<BoxCollider>();
                collider.size = new Vector3(2.6f, 3.8f, 13.8f);
                Rigidbody body = trailerObject.AddComponent<Rigidbody>();
                body.useGravity = false;
                LwsTrailerGameplayIdentity identity = trailerObject.AddComponent<LwsTrailerGameplayIdentity>();
                identity.Configure(
                    "trailer.delivery.playmode",
                    LwsTrailerTypeIds.DryVan,
                    "PlayMode Dry Van",
                    "depot.validation.interstate-corridor",
                    true);

                trailerObject.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                yield return new WaitForFixedUpdate();

                LwsDeliveryParkingEvaluation centered = zone.EvaluateTrailer(identity);
                Assert.IsTrue(centered.trailerInsideZone);
                Assert.IsTrue(centered.headingValid);
                Assert.IsTrue(centered.stopped);
                Assert.That(centered.containmentRatio, Is.GreaterThanOrEqualTo(0.8f));

                trailerObject.transform.rotation = Quaternion.Euler(0f, 30f, 0f);
                Physics.SyncTransforms();
                Assert.IsFalse(zone.EvaluateTrailer(identity).headingValid);

                trailerObject.transform.rotation = Quaternion.identity;
                body.linearVelocity = Vector3.forward;
                Assert.IsFalse(zone.EvaluateTrailer(identity).stopped);

                body.linearVelocity = Vector3.zero;
                trailerObject.transform.position = Vector3.right * 10f;
                Physics.SyncTransforms();
                Assert.IsFalse(zone.EvaluateTrailer(identity).trailerInsideZone);
            }
            finally
            {
                Object.Destroy(zoneObject);
                Object.Destroy(trailerObject);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator DeliveryLifecycleReachesResultsWithoutClearingActiveJob()
        {
            var registry = new LwsServiceRegistry();
            ILwsGameplayStateService gameplay = registry.Register<ILwsGameplayStateService>(
                new LwsGameplayStateService());
            ILwsActiveJobService jobs = registry.Register<ILwsActiveJobService>(
                new LwsActiveJobService(), typeof(ILwsGameplayStateService));
            Assert.IsTrue(registry.InitializeAll().Succeeded);
            Assert.IsTrue(gameplay.EnterFreeDrive("playmode ready").Succeeded);

            var offer = new LwsJobOffer
            {
                stableOfferId = "offer.delivery.playmode",
                jobDefinitionId = "job.delivery.playmode",
                originDepotId = "depot.validation.interstate-corridor",
                destinationId = "destination.validation.warehouse",
                cargoId = "cargo.playmode",
                requiredTrailerTypeId = LwsTrailerTypeIds.DryVan,
                cargoWeightLbs = 1000,
                estimatedDistanceMiles = 1f,
                quotedGrossPayCents = 10000
            };
            Assert.IsTrue(jobs.AcceptJob(offer, out _, "accept").Succeeded);
            LwsTrailerAssignment assignment = LwsTrailerAssignment.Create(
                "trailer.delivery.playmode",
                LwsTrailerTypeIds.DryVan,
                "PlayMode Dry Van",
                offer.originDepotId,
                true,
                new LwsWorldPositionD(0d, 0d, 0d));
            Assert.IsTrue(jobs.AssignTrailerToActiveJob(assignment, "assign").Succeeded);
            Assert.IsTrue(jobs.CompleteTrailerPickup(assignment.stableTrailerId, "pickup").Succeeded);
            Assert.IsTrue(jobs.EnterDelivery(
                offer.destinationId, assignment.stableTrailerId, "arrive").Succeeded);

            string activeJobId = jobs.CurrentActiveJob.stableActiveJobId;
            var completion = new LwsDeliveryCompletionSnapshot
            {
                completedSuccessfully = true,
                activeJobId = activeJobId,
                destinationId = offer.destinationId,
                assignedTrailerId = assignment.stableTrailerId,
                completedUtcTicks = System.DateTime.UtcNow.Ticks,
                containmentRatio = 1f,
                validHoldSeconds = 2f,
                validationMode = "assigned-trailer-coupled-auto-hold"
            };
            Assert.IsTrue(jobs.CompleteDelivery(completion, "complete").Succeeded);
            yield return null;

            Assert.IsTrue(jobs.HasActiveJob);
            Assert.AreEqual(activeJobId, jobs.CurrentActiveJob.stableActiveJobId);
            Assert.AreEqual(LwsJobStatus.Completed, jobs.CurrentActiveJob.status);
            Assert.AreEqual(LwsGameplayState.DeliveryResults, gameplay.CurrentState);
            Assert.IsFalse(gameplay.AllowsDrivingInput);
        }
    }
}

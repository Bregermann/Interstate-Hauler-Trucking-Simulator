using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace LWS.InterstateHauler.Tests.EditMode
{
    public sealed class LwsTrailerPickupEditModeTests
    {
        private const string TrailerPickupServicePath = "Assets/LWS/InterstateHauler/Vehicles/Trailers/LwsTrailerPickupService.cs";
        private const string TrailerTypesPath = "Assets/LWS/InterstateHauler/Vehicles/Trailers/LwsTrailerAssignmentTypes.cs";
        private const string BootstrapPath = "Assets/LWS/InterstateHauler/Bootstrap/LwsApplicationBootstrap.cs";

        [Test]
        public void TrailerAssignmentMatchesRequiredTrailerType()
        {
            LwsTrailerAssignment assignment = LwsTrailerAssignment.Create(
                "trailer.validation.dryvan",
                LwsTrailerTypeIds.DryVan,
                "Dry Van",
                "depot.validation.interstate-corridor",
                false,
                default);

            Assert.IsTrue(assignment.IsValid);
            Assert.IsTrue(assignment.MatchesRequiredType("trailer.dry-van"));
            Assert.IsFalse(assignment.MatchesRequiredType("trailer.reefer"));
        }

        [Test]
        public void ActiveJobKeepsOneStableTrailerAssignment()
        {
            ILwsActiveJobService service = CreateActiveJobServiceWithAcceptedDryVanJob();
            LwsTrailerAssignment first = CreateAssignment("trailer.validation.dryvan", LwsTrailerTypeIds.DryVan);
            LwsTrailerAssignment duplicateReplacement = CreateAssignment("trailer.validation.other", LwsTrailerTypeIds.DryVan);

            LwsServiceResult firstResult = service.AssignTrailerToActiveJob(first, "assign first");
            LwsServiceResult secondResult = service.AssignTrailerToActiveJob(duplicateReplacement, "assign second");

            Assert.IsTrue(firstResult.Succeeded, firstResult.Message);
            Assert.IsFalse(secondResult.Succeeded, "Prompt 021 must not silently replace the assigned trailer for one active job.");
            Assert.AreEqual("trailer.validation.dryvan", service.CurrentActiveJob.trailerAssignment.stableTrailerId);
        }

        [Test]
        public void WrongTrailerDoesNotCompletePickupAndCorrectTrailerStartsHaul()
        {
            ILwsActiveJobService service = CreateActiveJobServiceWithAcceptedDryVanJob();
            Assert.IsTrue(service.AssignTrailerToActiveJob(CreateAssignment("trailer.correct", LwsTrailerTypeIds.DryVan), "assign").Succeeded);

            LwsServiceResult wrong = service.CompleteTrailerPickup("trailer.wrong", "wrong attached");
            Assert.IsFalse(wrong.Succeeded);
            Assert.AreEqual(LwsJobStatus.AwaitingTrailerPickup, service.CurrentActiveJob.status);

            string originalActiveJobId = service.CurrentActiveJob.stableActiveJobId;
            LwsServiceResult correct = service.CompleteTrailerPickup("trailer.correct", "correct attached");

            Assert.IsTrue(correct.Succeeded, correct.Message);
            Assert.AreEqual(originalActiveJobId, service.CurrentActiveJob.stableActiveJobId);
            Assert.AreEqual(LwsJobStatus.HaulActive, service.CurrentActiveJob.status);
            Assert.AreEqual("trailer.correct", service.CurrentActiveJob.trailerAssignment.stableTrailerId);
            Assert.Greater(service.CurrentActiveJob.trailerAssignment.pickupCompletedUtcTicks, 0);
        }

        [Test]
        public void TrailerRegistryFiltersByTypeDepotAndExistingAssignment()
        {
            GameObject dryVan = new GameObject("DryVan Trailer");
            GameObject reefer = new GameObject("Reefer Trailer");
            try
            {
                LwsTrailerGameplayIdentity dryIdentity = dryVan.AddComponent<LwsTrailerGameplayIdentity>();
                dryIdentity.Configure("trailer.dry", LwsTrailerTypeIds.DryVan, "Dry Van", "depot.origin", true);
                LwsTrailerGameplayIdentity reeferIdentity = reefer.AddComponent<LwsTrailerGameplayIdentity>();
                reeferIdentity.Configure("trailer.reefer", "trailer.reefer", "Reefer", "depot.origin", true);

                var registry = new LwsTrailerRegistryService();
                Assert.IsTrue(registry.Initialize(new LwsServiceContext(new LwsServiceRegistry())).Succeeded);

                Assert.IsTrue(registry.RegisterTrailer(dryIdentity).Succeeded);
                Assert.IsTrue(registry.RegisterTrailer(reeferIdentity).Succeeded);
                Assert.AreEqual(1, registry.FindAssignableTrailers(LwsTrailerTypeIds.DryVan, "depot.origin", "active.a").Count);

                dryIdentity.MarkAssignedToJob("active.other");
                Assert.AreEqual(0, registry.FindAssignableTrailers(LwsTrailerTypeIds.DryVan, "depot.origin", "active.a").Count);
                Assert.AreEqual(1, registry.FindAssignableTrailers(LwsTrailerTypeIds.DryVan, "depot.origin", "active.other").Count);
            }
            finally
            {
                Object.DestroyImmediate(dryVan);
                Object.DestroyImmediate(reefer);
            }
        }

        [Test]
        public void TrailerGameplayIdentitySyncsStableVehicleIdentity()
        {
            GameObject trailer = new GameObject("Trailer Identity Sync");
            try
            {
                LwsVehicleIdentity vehicleIdentity = trailer.AddComponent<LwsVehicleIdentity>();
                LwsTrailerGameplayIdentity gameplayIdentity = trailer.AddComponent<LwsTrailerGameplayIdentity>();

                gameplayIdentity.Configure("trailer.sync", LwsTrailerTypeIds.DryVan, "Synced Dry Van", "depot.sync", true);

                Assert.AreEqual("trailer.sync", vehicleIdentity.VehicleId);
                Assert.AreEqual(LwsVehicleRole.Trailer, vehicleIdentity.Role);
                Assert.AreEqual("Synced Dry Van", vehicleIdentity.DisplayName);
                Assert.IsTrue(gameplayIdentity.Validate(out string message), message);
            }
            finally
            {
                Object.DestroyImmediate(trailer);
            }
        }

        [Test]
        public void ActiveJobSerializationContainsTrailerAssignmentSemantics()
        {
            ILwsActiveJobService service = CreateActiveJobServiceWithAcceptedDryVanJob();
            Assert.IsTrue(service.AssignTrailerToActiveJob(CreateAssignment("trailer.serialize", LwsTrailerTypeIds.DryVan), "serialize").Succeeded);

            string json = JsonUtility.ToJson(service.CurrentActiveJob);
            StringAssert.Contains("trailerAssignment", json);
            StringAssert.Contains("trailer.serialize", json);

            LwsActiveJob restored = JsonUtility.FromJson<LwsActiveJob>(json);
            Assert.IsTrue(restored.IsValid);
            Assert.AreEqual("trailer.serialize", restored.trailerAssignment.stableTrailerId);
            Assert.AreEqual(LwsJobStatus.AwaitingTrailerPickup, restored.status);
        }

        [Test]
        public void BootstrapRegistersOneTrailerRegistryAndPickupService()
        {
            LwsServiceRegistry registry = LwsApplicationBootstrap.CreateDefaultRegistry();

            Assert.AreEqual(1, registry.Registrations.Count(r => r.Service is ILwsTrailerRegistryService));
            Assert.AreEqual(1, registry.Registrations.Count(r => r.Service is ILwsTrailerPickupService));
            Assert.IsTrue(registry.TryGet(out ILwsTrailerRegistryService _));
            Assert.IsTrue(registry.TryGet(out ILwsTrailerPickupService _));
        }

        [Test]
        public void Prompt021SourceUsesExistingAuthoritiesOnly()
        {
            string pickup = File.ReadAllText(TrailerPickupServicePath);
            string types = File.ReadAllText(TrailerTypesPath);
            string bootstrap = File.ReadAllText(BootstrapPath);

            StringAssert.Contains("ILwsNavigationService", pickup);
            StringAssert.Contains("RequestAutosave(\"trailer.pickup.complete\")", pickup);
            StringAssert.Contains("ILwsTrailerPickupService", bootstrap);
            StringAssert.Contains("LwsTrailerAssignment", types);
            Assert.IsFalse(pickup.Contains("System.IO"));
            Assert.IsFalse(pickup.Contains("SaveToSlot"));
            Assert.IsFalse(pickup.Contains("Random.Range"));
            Assert.IsFalse(pickup.Contains("CompassPro"), "Trailer pickup should use the LWS navigation seam, not Compass internals.");
        }

        private static ILwsActiveJobService CreateActiveJobServiceWithAcceptedDryVanJob()
        {
            var registry = new LwsServiceRegistry();
            ILwsGameplayStateService gameplay = registry.Register<ILwsGameplayStateService>(new LwsGameplayStateService());
            ILwsActiveJobService activeJobs = registry.Register<ILwsActiveJobService>(new LwsActiveJobService(), typeof(ILwsGameplayStateService));
            Assert.IsTrue(registry.InitializeAll().Succeeded);
            Assert.IsTrue(gameplay.EnterFreeDrive("test ready").Succeeded);
            LwsJobOffer offer = CreateDryVanOffer();
            Assert.IsTrue(activeJobs.AcceptJob(offer, out _, "accept dry van job").Succeeded);
            return activeJobs;
        }

        private static LwsJobOffer CreateDryVanOffer()
        {
            return new LwsJobOffer
            {
                stableOfferId = "offer.test.dryvan",
                jobDefinitionId = "job.test.dryvan",
                originDepotId = "depot.validation.interstate-corridor",
                originDepotDisplayName = "Interstate Validation Depot",
                destinationId = "destination.validation.turnaround",
                destinationDisplayName = "Validation Turnaround",
                cargoId = "cargo.general-freight",
                cargoDisplayName = "General Freight",
                flavorText = "Validation freight for trailer pickup tests.",
                requiredTrailerTypeId = LwsTrailerTypeIds.DryVan,
                cargoWeightLbs = 32000,
                estimatedDistanceMiles = 12f,
                quotedGrossPayCents = 125000
            };
        }

        private static LwsTrailerAssignment CreateAssignment(string stableTrailerId, string trailerTypeId)
        {
            return LwsTrailerAssignment.Create(
                stableTrailerId,
                trailerTypeId,
                "Dry Van",
                "depot.validation.interstate-corridor",
                true,
                new LwsWorldPositionD(10d, 0d, 20d));
        }
    }
}
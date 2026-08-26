using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace LWS.InterstateHauler.Tests.EditMode
{
    public sealed class LwsDepotJobEditModeTests
    {
        private const string JobServicesPath = "Assets/LWS/InterstateHauler/Gameplay/Jobs/LwsJobServices.cs";
        private const string JobBoardPath = "Assets/LWS/InterstateHauler/Gameplay/Jobs/LwsJobBoardRuntime.cs";
        private const string DepotServicePath = "Assets/LWS/InterstateHauler/Gameplay/Jobs/LwsDepotService.cs";
        private const string ScriptableSheetsPackagePath = "Packages/com.lunawolfstudios.scriptablesheets/package.json";

        [Test]
        public void DefinitionsValidateStableIdsAndRequiredFields()
        {
            LwsDepotDefinition depot = CreateDepot("depot.validation.test", "Validation Depot");
            LwsDestinationDefinition destination = CreateDestination("destination.validation.test", "Validation Destination");
            LwsJobDefinition job = CreateJob("job.validation.test", depot, destination);

            Assert.IsTrue(depot.Validate(out string depotMessage), depotMessage);
            Assert.IsTrue(destination.Validate(out string destinationMessage), destinationMessage);
            Assert.IsTrue(job.Validate(out string jobMessage), jobMessage);

            LwsDepotDefinition badDepot = CreateDepot("", "Bad Depot");
            Assert.IsFalse(badDepot.Validate(out _));
            LwsDestinationDefinition badDestination = CreateDestination("", "Bad Destination");
            Assert.IsFalse(badDestination.Validate(out _));
            LwsJobDefinition badJob = CreateJob("", depot, destination);
            Assert.IsFalse(badJob.Validate(out _));
        }

        [Test]
        public void DuplicateDefinitionIdsAreRejected()
        {
            LwsDepotDefinition depotA = CreateDepot("depot.duplicate", "Depot A");
            LwsDepotDefinition depotB = CreateDepot("depot.duplicate", "Depot B");
            LwsDestinationDefinition destinationA = CreateDestination("destination.duplicate", "Destination A");
            LwsDestinationDefinition destinationB = CreateDestination("destination.duplicate", "Destination B");
            LwsJobDefinition jobA = CreateJob("job.duplicate", depotA, destinationA);
            LwsJobDefinition jobB = CreateJob("job.duplicate", depotA, destinationA);

            Assert.IsFalse(LwsDepotDefinition.ValidateUniqueIds(new[] { depotA, depotB }, out _));
            Assert.IsFalse(LwsDestinationDefinition.ValidateUniqueIds(new[] { destinationA, destinationB }, out _));
            Assert.IsFalse(LwsJobDefinition.ValidateUniqueIds(new[] { jobA, jobB }, out _));
        }

        [Test]
        public void CatalogAndAuthoredProviderFilterDeterministicallyByOrigin()
        {
            LwsDepotDefinition origin = CreateDepot("depot.origin", "Origin Depot");
            LwsDepotDefinition other = CreateDepot("depot.other", "Other Depot");
            LwsDestinationDefinition destination = CreateDestination("destination.catalog", "Catalog Destination");
            LwsJobDefinition valid = CreateJob("job.catalog.valid", origin, destination, enabled: true);
            LwsJobDefinition disabled = CreateJob("job.catalog.disabled", origin, destination, enabled: false);
            LwsJobDefinition wrongOrigin = CreateJob("job.catalog.wrong-origin", other, destination, enabled: true);
            LwsJobCatalog catalog = CreateCatalog(origin, other, destination, valid, disabled, wrongOrigin);

            var registry = new LwsServiceRegistry();
            registry.Register<ILwsDepotService>(new LwsDepotService());
            ILwsJobCatalogService catalogService = registry.Register<ILwsJobCatalogService>(new LwsJobCatalogService(), typeof(ILwsDepotService));
            ILwsJobOfferProvider provider = registry.Register<ILwsJobOfferProvider>(new LwsAuthoredJobOfferProvider(), typeof(ILwsJobCatalogService));
            Assert.IsTrue(registry.InitializeAll().Succeeded);
            Assert.IsTrue(catalogService.RegisterCatalog(catalog).Succeeded);

            var offers = provider.GetOffersForDepot(origin.StableDepotId);

            Assert.AreEqual(1, offers.Count);
            Assert.AreEqual(valid.StableJobDefinitionId, offers[0].jobDefinitionId);
            Assert.AreEqual($"offer.{valid.StableJobDefinitionId}", offers[0].stableOfferId);
            StringAssert.Contains("Resources.Load<LwsJobCatalog>", File.ReadAllText(JobServicesPath));
            Assert.IsFalse(File.ReadAllText(JobServicesPath).Contains("AssetDatabase"), "Runtime job services must not use UnityEditor.AssetDatabase.");
        }

        [Test]
        public void GameplayStateFlowActivatesDepotJobSelectionAndTrailerPickup()
        {
            var gameplay = CreateInitializedGameplayState();
            Assert.IsTrue(gameplay.EnterFreeDrive("ready").Succeeded);

            Assert.IsTrue(gameplay.TryTransitionTo(LwsGameplayState.AtDepot, "enter depot").Succeeded);
            Assert.IsTrue(gameplay.AllowsDrivingInput);
            Assert.IsTrue(gameplay.TryTransitionTo(LwsGameplayState.JobSelection, "open job board").Succeeded);
            Assert.IsFalse(gameplay.AllowsDrivingInput);
            Assert.IsTrue(gameplay.TryTransitionTo(LwsGameplayState.AtDepot, "close job board").Succeeded);
            Assert.IsTrue(gameplay.TryTransitionTo(LwsGameplayState.JobSelection, "open again").Succeeded);
            Assert.IsTrue(gameplay.TryTransitionTo(LwsGameplayState.TrailerPickup, "accept job").Succeeded);
            Assert.IsTrue(gameplay.AllowsDrivingInput);
        }

        [Test]
        public void ActiveJobAcceptanceSnapshotsAuthoredValuesAndBlocksSecondJob()
        {
            LwsDepotDefinition depot = CreateDepot("depot.active", "Active Depot");
            LwsDestinationDefinition destination = CreateDestination("destination.active", "Active Destination");
            LwsJobDefinition job = CreateJob("job.active", depot, destination, cargoName: "Packaged Food");
            LwsJobOffer offer = job.ToOffer();
            var service = new LwsActiveJobService();
            service.Initialize(new LwsServiceContext(new LwsServiceRegistry()));

            LwsServiceResult first = service.AcceptJob(offer, out LwsActiveJob activeJob, "accept test");
            LwsServiceResult second = service.AcceptJob(offer, out _, "second test");
            job.ConfigureForTests("job.active", "Mutated Source", depot, destination, cargoLabel: "Changed Cargo");

            Assert.IsTrue(first.Succeeded, first.Message);
            Assert.IsNotNull(activeJob);
            Assert.IsTrue(activeJob.stableActiveJobId.StartsWith("active.", StringComparison.Ordinal));
            Assert.AreEqual(LwsJobStatus.AwaitingTrailerPickup, activeJob.status);
            Assert.AreEqual("Packaged Food", service.CurrentActiveJob.cargoDisplayName);
            Assert.AreEqual(job.StableJobDefinitionId, activeJob.sourceJobDefinitionId);
            Assert.IsFalse(second.Succeeded);
        }

        [Test]
        public void ActiveJobSaveParticipantRoundTripsSemanticStateOnly()
        {
            var registry = new LwsServiceRegistry();
            ILwsActiveJobService activeJobs = registry.Register<ILwsActiveJobService>(new LwsActiveJobService());
            Assert.IsTrue(registry.InitializeAll().Succeeded);
            var participant = new LwsActiveJobSaveParticipant(() => registry);

            LwsSaveParticipantState empty = participant.CaptureState();
            Assert.IsTrue(participant.RestoreState(empty).Succeeded);
            Assert.IsFalse(activeJobs.HasActiveJob);

            LwsDepotDefinition depot = CreateDepot("depot.save", "Save Depot");
            LwsDestinationDefinition destination = CreateDestination("destination.save", "Save Destination");
            LwsJobDefinition job = CreateJob("job.save", depot, destination);
            Assert.IsTrue(activeJobs.AcceptJob(job.ToOffer(), out _, "save test").Succeeded);

            LwsSaveParticipantState captured = participant.CaptureState();
            Assert.AreEqual(LwsSaveSchema.ActiveJobParticipantId, captured.participantId);
            Assert.IsTrue(activeJobs.ClearActiveJob("clear").Succeeded);
            Assert.IsTrue(participant.RestoreState(captured).Succeeded);
            Assert.IsTrue(activeJobs.HasActiveJob);
            Assert.AreEqual(LwsJobStatus.AwaitingTrailerPickup, activeJobs.CurrentActiveJob.status);
        }

        [Test]
        public void JobBoardUsesExistingServicesAndDoesNotGenerateJobsOrAutosaveInfrastructure()
        {
            string services = File.ReadAllText(JobServicesPath);
            string board = File.ReadAllText(JobBoardPath);

            StringAssert.Contains("ILwsJobOfferProvider", services);
            StringAssert.Contains("LwsAuthoredJobOfferProvider", services);
            StringAssert.Contains("RequestAutosave(\"job.accepted\")", services);
            Assert.IsFalse(services.Contains("Random.Range"));
            Assert.IsFalse(services.Contains("System.IO"));
            Assert.IsFalse(board.Contains("OnGUI"));
            Assert.IsFalse(board.Contains("SaveToSlot"));
        }

        [Test]
        public void DepotRuntimeUsesCanonicalTractorAndStateGuards()
        {
            string depot = File.ReadAllText(DepotServicePath);

            StringAssert.Contains("GetComponentInParent<LwsPlayerTruck>()", depot);
            StringAssert.Contains("_playerTractorColliders", depot);
            StringAssert.Contains("_playerVehicleService.ActiveTruck != truck", depot);
            StringAssert.Contains("_gameplayStateService.CurrentState != LwsGameplayState.FreeDrive", depot);
            StringAssert.Contains("_gameplayStateService.CurrentState != LwsGameplayState.AtDepot", depot);
        }

        [Test]
        public void ScriptableSheetsPackageIsInstalledAndDefinitionsAreScriptableSheetFriendly()
        {
            Assert.IsTrue(File.Exists(ScriptableSheetsPackagePath), "Scriptable Sheets must be present as an embedded package for Prompt 020 authoring.");
            string packageJson = File.ReadAllText(ScriptableSheetsPackagePath);
            StringAssert.Contains("LWS Scriptable Sheets", packageJson);
            StringAssert.Contains("\"version\": \"1.11.0\"", packageJson);
            StringAssert.Contains("CSV", packageJson);
            StringAssert.Contains("TSV", packageJson);
            Assert.IsTrue(typeof(LwsDepotDefinition).IsSubclassOf(typeof(ScriptableObject)));
            Assert.IsTrue(typeof(LwsDestinationDefinition).IsSubclassOf(typeof(ScriptableObject)));
            Assert.IsTrue(typeof(LwsJobDefinition).IsSubclassOf(typeof(ScriptableObject)));
            Assert.IsTrue(typeof(LwsJobCatalog).IsSubclassOf(typeof(ScriptableObject)));
        }

        private static LwsGameplayStateService CreateInitializedGameplayState()
        {
            var service = new LwsGameplayStateService();
            Assert.IsTrue(service.Initialize(new LwsServiceContext(new LwsServiceRegistry())).Succeeded);
            return service;
        }

        private static LwsDepotDefinition CreateDepot(string id, string label)
        {
            var depot = ScriptableObject.CreateInstance<LwsDepotDefinition>();
            depot.ConfigureForTests(id, label, LwsSaveSchema.DefaultStableWorldId, jobBoard: true, isEnabled: true);
            return depot;
        }

        private static LwsDestinationDefinition CreateDestination(string id, string label)
        {
            var destination = ScriptableObject.CreateInstance<LwsDestinationDefinition>();
            destination.ConfigureForTests(id, label, LwsSaveSchema.DefaultStableWorldId, LwsDestinationType.Warehouse, isEnabled: true);
            return destination;
        }

        private static LwsJobDefinition CreateJob(
            string id,
            LwsDepotDefinition origin,
            LwsDestinationDefinition destination,
            bool enabled = true,
            string cargoName = "General Freight")
        {
            var job = ScriptableObject.CreateInstance<LwsJobDefinition>();
            job.ConfigureForTests(id, id, origin, destination, "cargo.general-freight", cargoName, "trailer.dry-van", enabled);
            return job;
        }

        private static LwsJobCatalog CreateCatalog(
            LwsDepotDefinition depotA,
            LwsDepotDefinition depotB,
            LwsDestinationDefinition destination,
            params LwsJobDefinition[] jobs)
        {
            var catalog = ScriptableObject.CreateInstance<LwsJobCatalog>();
            catalog.ConfigureForTests(new[] { depotA, depotB }, new[] { destination }, jobs);
            return catalog;
        }
    }
}

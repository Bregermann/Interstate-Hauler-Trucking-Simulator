using System;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace LWS.InterstateHauler.Tests.EditMode
{
    public sealed class LwsPixelCrushersSaveEditModeTests
    {
        private const string SaveArchitecturePath = "Assets/LWS/InterstateHauler/Save/LwsSaveArchitecture.cs";
        private const string SaveProfileTypesPath = "Assets/LWS/InterstateHauler/Save/LwsSaveProfileTypes.cs";
        private const string SemanticSaverBridgePath = "Assets/LWS/InterstateHaulerPixelCrushers/Save/LwsPixelCrushersSemanticSaver.cs";
        private const string PixelCrushersSaveSystemPath = "Assets/Plugins/Pixel Crushers/Common/Scripts/Save System/SaveSystem.cs";
        private const string PixelCrushersSaverPath = "Assets/Plugins/Pixel Crushers/Common/Scripts/Save System/Savers/Saver.cs";
        private const string PixelCrushersDiskStorerPath = "Assets/Plugins/Pixel Crushers/Common/Scripts/Save System/Storers/DiskSavedGameDataStorer.cs";
        private const string PixelCrushersDialogueSaverPath = "Assets/Plugins/Pixel Crushers/Dialogue System/Scripts/Save System/DialogueSystemSaver.cs";

        [Test]
        public void PixelCrushersSaveSystemIsInstalled()
        {
            Assert.IsTrue(File.Exists(PixelCrushersSaveSystemPath), PixelCrushersSaveSystemPath);
            Assert.IsTrue(File.Exists(PixelCrushersSaverPath), PixelCrushersSaverPath);
            Assert.IsTrue(File.Exists(PixelCrushersDiskStorerPath), PixelCrushersDiskStorerPath);
            Assert.IsTrue(File.Exists(PixelCrushersDialogueSaverPath), PixelCrushersDialogueSaverPath);
        }

        [Test]
        public void SaveServiceUsesPixelCrushersAsSingleAuthority()
        {
            string source = ReadPrompt016SaveSources();

            StringAssert.Contains("interface ILwsSaveService", source);
            StringAssert.Contains("LwsPixelCrushersSaveAdapter", source);
            StringAssert.Contains("SaveToSlotImmediate", source);
            StringAssert.Contains("LoadFromSlot", source);
            StringAssert.Contains("DeleteSavedGameInSlot", source);
            StringAssert.Contains("SavedGameDataStorer", source);
            StringAssert.Contains("LwsPixelCrushersSemanticSaver", source);
            Assert.IsFalse(source.Contains("interface ILwsSaveStorage"), "LWS must not keep a second storage abstraction as production authority.");
            Assert.IsFalse(source.Contains("LwsPcSaveStorage"), "PC storage must be Pixel Crushers DiskSavedGameDataStorer, not an LWS storage class.");
            Assert.IsFalse(source.Contains("LwsInMemorySaveStorage"), "Prompt 016 must not keep the old in-memory save framework.");
            Assert.IsFalse(source.Contains("DevelopmentTestSlot"), "The old proof slot must not remain active.");
            Assert.IsFalse(source.Contains("SaveTestState"), "The old proof save method must not remain active.");
            Assert.IsFalse(source.Contains("LoadTestState"), "The old proof load method must not remain active.");
        }

        [Test]
        public void ProfileManualSlotMappingIsDeterministicAndIsolated()
        {
            var service = new LwsSaveService();
            LwsServiceResult result = service.Initialize(new LwsServiceContext(new LwsServiceRegistry()));
            Assert.IsTrue(result.Succeeded, result.Message);

            string firstProfileId = service.ActiveProfileId;
            int slot1A = service.MapToVendorSlot(firstProfileId, LwsSaveSlotType.Manual, 1);
            int slot1B = service.MapToVendorSlot(firstProfileId, LwsSaveSlotType.Manual, 1);
            int slot2 = service.MapToVendorSlot(firstProfileId, LwsSaveSlotType.Manual, 2);

            Assert.AreEqual(slot1A, slot1B);
            Assert.AreNotEqual(slot1A, slot2);
            Assert.AreEqual(LwsSaveSchema.ManualSlotCount, service.GetManualSlots(firstProfileId).Count);

            LwsSaveOperationResult create = service.CreateProfile("Second Driver", out LwsSaveProfileMetadata secondProfile);
            Assert.IsTrue(create.Succeeded, create.Message);
            Assert.IsNotNull(secondProfile);
            int secondProfileSlot1 = service.MapToVendorSlot(secondProfile.stableProfileId, LwsSaveSlotType.Manual, 1);
            Assert.AreNotEqual(slot1A, secondProfileSlot1);
            CollectionAssert.AllItemsAreUnique(new[] { slot1A, slot2, secondProfileSlot1 });
        }

        [Test]
        public void ProfileRenamePreservesStableProfileId()
        {
            var service = new LwsSaveService();
            Assert.IsTrue(service.Initialize(new LwsServiceContext(new LwsServiceRegistry())).Succeeded);
            string profileId = service.ActiveProfileId;

            LwsSaveOperationResult rename = service.RenameProfile(profileId, "Renamed Driver");

            Assert.IsTrue(rename.Succeeded, rename.Message);
            Assert.AreEqual(profileId, service.ActiveProfileId);
            Assert.AreEqual("Renamed Driver", service.ActiveProfile.DisplayNameOrFallback);
        }

        [Test]
        public void RegisteredSemanticParticipantsCoverCurrentSaveableSystems()
        {
            var service = new LwsSaveService();
            LwsServiceResult result = service.Initialize(new LwsServiceContext(new LwsServiceRegistry()));
            Assert.IsTrue(result.Succeeded, result.Message);

            string[] ids = service.Participants.Select(p => p.ParticipantId).ToArray();
            CollectionAssert.Contains(ids, "lws.world.global-position");
            CollectionAssert.Contains(ids, "lws.vehicle.player-truck");
            CollectionAssert.Contains(ids, "lws.game-clock");
            CollectionAssert.Contains(ids, "lws.weather.semantic");
            CollectionAssert.Contains(ids, "lws.road-condition.semantic");
            CollectionAssert.Contains(ids, "lws.navigation.destination-intent");
            Assert.IsFalse(ids.Contains("lws.validation.proof"));
        }

        [Test]
        public void GlobalPositionPayloadUsesDoublePrecision()
        {
            Type payloadType = typeof(LwsGlobalPositionSavePayload);

            Assert.AreEqual(typeof(double), payloadType.GetField(nameof(LwsGlobalPositionSavePayload.globalX)).FieldType);
            Assert.AreEqual(typeof(double), payloadType.GetField(nameof(LwsGlobalPositionSavePayload.globalY)).FieldType);
            Assert.AreEqual(typeof(double), payloadType.GetField(nameof(LwsGlobalPositionSavePayload.globalZ)).FieldType);
        }

        [Test]
        public void Prompt016SaveCodeDoesNotWritePlatformFilesDirectly()
        {
            string source = ReadPrompt016SaveSources();
            string[] forbidden =
            {
                "File.WriteAllText",
                "File.WriteAllBytes",
                "File.ReadAllText",
                "File.ReadAllBytes",
                "Directory.CreateDirectory",
                "FileStream",
                "StreamWriter",
                "StreamReader"
            };

            CollectionAssert.IsEmpty(forbidden.Where(source.Contains).ToArray());
        }

        [Test]
        public void Prompt016DefinesFutureVersionedPayloadSeamsOnly()
        {
            Assert.AreEqual(LwsSaveSchema.CurrentVersion, new LwsFutureCabAccessorySavePayload().schemaVersion);
            Assert.AreEqual(LwsSaveSchema.CurrentVersion, new LwsFutureCompanionSavePayload().schemaVersion);
            Assert.AreEqual(LwsSaveSchema.CurrentVersion, new LwsFutureLifeEventSavePayload().schemaVersion);
        }

        private static string ReadPrompt016SaveSources()
        {
            return File.ReadAllText(SaveArchitecturePath) + "\n" +
                   File.ReadAllText(SaveProfileTypesPath) + "\n" +
                   File.ReadAllText(SemanticSaverBridgePath);
        }
    }
}

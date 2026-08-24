using System;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace LWS.InterstateHauler.Tests.EditMode
{
    public sealed class LwsPixelCrushersSaveEditModeTests
    {
        private const string SaveArchitecturePath = "Assets/LWS/InterstateHauler/Save/LwsSaveArchitecture.cs";
        private const string PixelCrushersSaveSystemPath = "Assets/Plugins/Pixel Crushers/Common/Scripts/Save System/SaveSystem.cs";
        private const string PixelCrushersSaverPath = "Assets/Plugins/Pixel Crushers/Common/Scripts/Save System/Savers/Saver.cs";
        private const string PixelCrushersDiskStorerPath = "Assets/Plugins/Pixel Crushers/Common/Scripts/Save System/Storers/DiskSavedGameDataStorer.cs";

        [Test]
        public void PixelCrushersSaveSystemIsInstalled()
        {
            Assert.IsTrue(File.Exists(PixelCrushersSaveSystemPath), PixelCrushersSaveSystemPath);
            Assert.IsTrue(File.Exists(PixelCrushersSaverPath), PixelCrushersSaverPath);
            Assert.IsTrue(File.Exists(PixelCrushersDiskStorerPath), PixelCrushersDiskStorerPath);
        }

        [Test]
        public void SaveServiceUsesPixelCrushersAdapterAndStorageSeam()
        {
            string source = File.ReadAllText(SaveArchitecturePath);

            StringAssert.Contains("interface ILwsSaveService", source);
            StringAssert.Contains("LwsPixelCrushersSaveAdapter", source);
            StringAssert.Contains("interface ILwsSaveStorage", source);
            StringAssert.Contains("LwsPcSaveStorage", source);
            StringAssert.Contains("DiskSavedGameDataStorer", source);
            Assert.IsFalse(source.Contains("LwsInMemorySaveStorage"), "Prompt 016 should not keep the old in-memory save framework.");
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
            string source = File.ReadAllText(SaveArchitecturePath);
            string[] forbidden =
            {
                "File.WriteAllText",
                "File.WriteAllBytes",
                "FileStream",
                "StreamWriter",
                "StreamReader"
            };

            CollectionAssert.IsEmpty(forbidden.Where(source.Contains).ToArray());
        }

        [Test]
        public void SaveParticipantRegistrationIncludesPrompt016Providers()
        {
            var service = new LwsSaveService();
            LwsServiceResult result = service.Initialize(new LwsServiceContext(new LwsServiceRegistry()));

            Assert.IsTrue(result.Succeeded, result.Message);
            CollectionAssert.Contains(service.Participants.Select(p => p.ParticipantId).ToArray(), "lws.world.global-position");
            CollectionAssert.Contains(service.Participants.Select(p => p.ParticipantId).ToArray(), "lws.game-clock");
            CollectionAssert.Contains(service.Participants.Select(p => p.ParticipantId).ToArray(), "lws.weather.semantic");
            CollectionAssert.Contains(service.Participants.Select(p => p.ParticipantId).ToArray(), "lws.validation.proof");
        }
    }
}

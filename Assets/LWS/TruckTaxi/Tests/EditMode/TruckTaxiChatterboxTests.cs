using System;
using System.Collections;
using System.IO;
using System.Linq;
using LWS.TruckTaxi.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace LWS.TruckTaxi.Tests
{
    public sealed class TruckTaxiChatterboxTests
    {
        [TestCase("../escape", false)] [TestCase("C:\\voice", false)] [TestCase("CON", false)]
        [TestCase("NUL.wav", false)] [TestCase("a.", false)] [TestCase("", false)]
        [TestCase("p001.greeting", true)]
        public void StableIdsCannotEscapeGeneratedFolder(string id, bool valid) => Assert.AreEqual(valid, TruckTaxiChatterboxQueue.IsSafeId(id));

        [Test]
        public void EmotionReferenceUsesExactThenRelatedThenNeutral()
        {
            var voice = ScriptableObject.CreateInstance<TruckTaxiVoiceProfile>();
            try
            {
                voice.neutralReference = "neutral.wav";
                Assert.AreEqual("neutral.wav", voice.ResolveReference(TruckTaxiVoiceEmotion.Angry, out _));
                voice.annoyedReference = "annoyed.wav";
                Assert.AreEqual("annoyed.wav", voice.ResolveReference(TruckTaxiVoiceEmotion.Angry, out _));
                voice.angryReference = "angry.wav";
                Assert.AreEqual("angry.wav", voice.ResolveReference(TruckTaxiVoiceEmotion.Angry, out _));
            }
            finally { UnityEngine.Object.DestroyImmediate(voice); }
        }

        [Test]
        public void ImportRejectsForeignOutputPaths()
        {
            string hash = new string('a', 64);
            Assert.Throws<InvalidOperationException>(() => TruckTaxiChatterboxQueue.ValidateOutputPath("C:/outside.wav", "p001", "hello", hash));
            string expected = TruckTaxiChatterboxQueue.AudioRoot + "/p001/hello_" + hash.Substring(0, 24) + ".wav";
            Assert.AreEqual(expected, TruckTaxiChatterboxQueue.ValidateOutputPath(Path.Combine(TruckTaxiChatterboxQueue.ProjectRoot, expected), "p001", "hello", hash));
        }

        [Test]
        public void MissingAudioRetainsSubtitleAndLegacyDialogue()
        {
            var line = new TruckTaxiDialogueLine { text = "Hello" };
            Assert.IsNull(line.generatedAudio); Assert.AreEqual("Hello", line.Subtitle);
            line.subtitle = "Accessible caption"; Assert.AreEqual("Accessible caption", line.Subtitle);
            Assert.NotNull(typeof(PassengerProfile).GetField("dialogueSet"));
        }

        [Test]
        public void ExistingRosterDialogueBuildIsIdempotentAndPreservesLegacyContent()
        {
            var roster = TruckTaxiPassengerDialogueAuthoring.AllPassengers()
                .Where(p => AssetDatabase.GetAssetPath(p).Contains("/ScriptableObjects/Passengers/")).ToArray();
            Assert.GreaterOrEqual(roster.Length, 10);
            foreach (var passenger in roster)
            {
                var legacy = passenger.dialogueSet?.ToArray();
                TruckTaxiPassengerDialogueAuthoring.BuildMissingDialogueAssets(passenger);
                string before = JsonUtility.ToJson(passenger);
                string dialogue = JsonUtility.ToJson(passenger.authoredDialogue);
                var voice = passenger.voiceProfile;
                TruckTaxiPassengerDialogueAuthoring.BuildMissingDialogueAssets(passenger);
                Assert.AreEqual(before, JsonUtility.ToJson(passenger));
                Assert.AreEqual(dialogue, JsonUtility.ToJson(passenger.authoredDialogue));
                Assert.AreSame(voice, passenger.voiceProfile);
                CollectionAssert.AreEqual(legacy, passenger.dialogueSet);
            }
        }

        [Test]
        public void FactoryWindowConstructsUsingExistingPassengerAssets()
        {
            var window = ScriptableObject.CreateInstance<TruckTaxiPassengerFactoryWindow>();
            try { window.CreateGUI(); Assert.Greater(window.rootVisualElement.childCount, 4); }
            finally { UnityEngine.Object.DestroyImmediate(window); }
        }

        [UnityTest]
        public IEnumerator ExistingWorkHereGeneratesImportsAndSkipsUnchangedLine()
        {
            if (Environment.GetEnvironmentVariable("TRUCK_TAXI_RUN_VOICE_INTEGRATION") != "1")
                Assert.Ignore("Opt-in external WorkHere integration; set TRUCK_TAXI_RUN_VOICE_INTEGRATION=1.");
            const string fixture = "Assets/LWS/TruckTaxi/Tests/VoiceIntegrationFixture";
            Assert.IsFalse(AssetDatabase.IsValidFolder(fixture), "Do not overwrite an existing fixture.");
            TruckTaxiPassengerDialogueAuthoring.EnsureFolder(fixture);
            var passenger = ScriptableObject.CreateInstance<PassengerProfile>();
            var voice = ScriptableObject.CreateInstance<TruckTaxiVoiceProfile>();
            var dialogue = ScriptableObject.CreateInstance<TruckTaxiDialogueSet>();
            string oldJob = TruckTaxiChatterboxSettings.instance.lastJob;
            try
            {
                passenger.passengerId = "pipeline-validation"; passenger.passengerName = "Pipeline validation only";
                voice.voiceProfileId = "workhere-validation-neutral";
                voice.neutralReference = Path.Combine(TruckTaxiChatterboxSettings.instance.workHere, "voice_refs/neutralvoicesample.wav");
                passenger.voiceProfile = voice; passenger.authoredDialogue = dialogue;
                dialogue.lines = new[] { new TruckTaxiDialogueLine { lineId = "greeting", passengerId = passenger.passengerId,
                    text = "Thanks for the pickup. Let's take the scenic route.", category = TruckTaxiDialogueCategory.PickupGreeting } };
                AssetDatabase.CreateAsset(voice, fixture + "/voice.asset");
                AssetDatabase.CreateAsset(dialogue, fixture + "/dialogue.asset");
                AssetDatabase.CreateAsset(passenger, fixture + "/passenger.asset");
                AssetDatabase.SaveAssets();
                string referenceHash = TruckTaxiChatterboxQueue.HashFile(voice.neutralReference);
                TruckTaxiChatterboxQueue.Generate(new[] { passenger }, false);
                double deadline = EditorApplication.timeSinceStartup + 180;
                while (TruckTaxiChatterboxQueue.IsRunning && EditorApplication.timeSinceStartup < deadline) yield return null;
                Assert.IsFalse(TruckTaxiChatterboxQueue.IsRunning, "Generation did not finish in 180 seconds.");
                Assert.NotNull(dialogue.lines[0].generatedAudio, TruckTaxiChatterboxQueue.Status + "\n" + TruckTaxiChatterboxQueue.Log);
                Assert.Greater(dialogue.lines[0].generatedAudio.length, .1f);
                Assert.AreEqual(referenceHash, TruckTaxiChatterboxQueue.HashFile(voice.neutralReference));
                Assert.AreEqual(0, TruckTaxiChatterboxQueue.BuildJob(new[] { passenger }, true, false).items.Count);
                var original = TruckTaxiChatterboxQueue.CreateItem(passenger, dialogue.lines[0]);
                dialogue.lines[0].text += " Please.";
                Assert.AreNotEqual(original.requestHash, TruckTaxiChatterboxQueue.CreateItem(passenger, dialogue.lines[0]).requestHash);
                Assert.AreEqual(1, TruckTaxiChatterboxQueue.BuildJob(new[] { passenger }, true, false).items.Count);
            }
            finally
            {
                TruckTaxiChatterboxQueue.Cancel();
                AssetDatabase.DeleteAsset(fixture);
                TruckTaxiChatterboxSettings.instance.lastJob = oldJob; TruckTaxiChatterboxSettings.instance.Persist();
            }
        }
    }
}

using System;
using System.IO;
using System.Linq;
using LWS.TruckTaxi.Editor;
using NUnit.Framework;
using UnityEngine;

namespace LWS.TruckTaxi.Tests
{
    public sealed class TruckTaxiVoiceDiscoveryTests
    {
        private PassengerProfile passenger;
        private TruckTaxiVoiceProfile voice;
        [SetUp] public void Setup()
        {
            passenger=ScriptableObject.CreateInstance<PassengerProfile>(); passenger.passengerId="p001"; passenger.passengerName="Not an ID";
            voice=ScriptableObject.CreateInstance<TruckTaxiVoiceProfile>(); voice.voiceProfileId="voice-p001"; passenger.voiceProfile=voice;
        }
        [TearDown] public void Teardown() { UnityEngine.Object.DestroyImmediate(passenger); UnityEngine.Object.DestroyImmediate(voice); }
        private TruckTaxiVoiceReferenceDiscovery.Report Scan(params string[] names) => TruckTaxiVoiceReferenceDiscovery.MatchFiles(names.Select(n=>Path.Combine(Path.GetTempPath(),n)),new[]{passenger},new[]{voice});
        [TestCase("P001__NEUTRAL.WAV",TruckTaxiVoiceEmotion.Neutral)]
        [TestCase("voice-P001__excited.wav",TruckTaxiVoiceEmotion.Excited)]
        [TestCase("p001__angry.wav",TruckTaxiVoiceEmotion.Angry)]
        [TestCase("p001__afraid.wav",TruckTaxiVoiceEmotion.Afraid)]
        [TestCase("p001__sad.wav",TruckTaxiVoiceEmotion.Sad)]
        [TestCase("p001__annoyed.wav",TruckTaxiVoiceEmotion.Annoyed)]
        public void ExactIdsAndSixBaseEmotions(string file,TruckTaxiVoiceEmotion tone)
        { var report=Scan(file); Assert.IsFalse(report.HasErrors); Assert.AreEqual(1,report.MatchedCount); Assert.AreEqual(tone,report.Rows.Single(r=>r.Status==TruckTaxiVoiceReferenceDiscovery.MatchStatus.Matched).Emotion); }
        [TestCase("Not an ID__neutral.wav")][TestCase("p001-similar__neutral.wav")][TestCase("neutralvoicesample.wav")]
        public void NamesNeverGuessed(string file) => Assert.AreEqual(TruckTaxiVoiceReferenceDiscovery.MatchStatus.UnknownPassengerId,Scan(file).Rows[0].Status);
        [TestCase("flirty")][TestCase("calm")][TestCase("panic")][TestCase("sarcastic")][TestCase("energetic")]
        public void AdditionalTonesRequireAuthoredSupport(string tone)
        {
            Assert.IsTrue(Scan("p001__"+tone+".wav").HasErrors);
            var emotion=(TruckTaxiVoiceEmotion)Enum.Parse(typeof(TruckTaxiVoiceEmotion),tone,true);
            voice.supportedAdditionalTones=new[]{emotion}; Assert.IsFalse(Scan("p001__"+tone+".wav").HasErrors);
            voice.neutralReference="neutral.wav"; Assert.AreEqual("neutral.wav",voice.ResolveReference(emotion,out _));
            voice.SetReference(emotion,"tone.wav"); voice.SetReference(emotion,"tone.wav");
            Assert.AreEqual(1,voice.additionalReferences.Length); Assert.AreEqual("tone.wav",voice.ResolveReference(emotion,out _));
        }
        [TestCase("4")][TestCase("happy")][TestCase("")]
        public void UnknownOrNumericEmotionsRejected(string tone) => Assert.IsTrue(Scan("p001__"+tone+".wav").HasErrors);
        [Test] public void AliasAndRecursiveDuplicateNeverChosen()
        {
            var report=Scan("p001__neutral.wav","voice-p001__neutral.wav","sub/p001__neutral.wav");
            Assert.AreEqual(3,report.Rows.Count(r=>r.Status==TruckTaxiVoiceReferenceDiscovery.MatchStatus.DuplicateMatch));
            Assert.AreEqual(0,report.MatchedCount); Assert.Throws<InvalidOperationException>(()=>TruckTaxiVoiceReferenceDiscovery.ApplyValidated(report));
        }
        [Test] public void CollisionBetweenPassengerAndVoiceIdsIsRejected()
        {
            var other=ScriptableObject.CreateInstance<TruckTaxiVoiceProfile>(); other.voiceProfileId="p001";
            try { Assert.IsTrue(TruckTaxiVoiceReferenceDiscovery.MatchFiles(new[]{Path.Combine(Path.GetTempPath(),"p001__neutral.wav")},new[]{passenger},new[]{voice,other}).HasErrors); }
            finally { UnityEngine.Object.DestroyImmediate(other); }
        }
        [Test] public void ScanIsReadOnlyAndNeutralFallbackRemainsAvailable()
        {
            voice.neutralReference="already-authored.wav"; string before=JsonUtility.ToJson(voice);
            var first=Scan("p001__neutral.wav"); var second=Scan("p001__neutral.wav");
            Assert.AreEqual(first.ToMarkdown(),second.ToMarkdown()); Assert.AreEqual(before,JsonUtility.ToJson(voice));
            Assert.AreEqual(5,first.Rows.Count(r=>r.Status==TruckTaxiVoiceReferenceDiscovery.MatchStatus.NoSample));
            Assert.AreEqual("already-authored.wav",voice.ResolveReference(TruckTaxiVoiceEmotion.Angry,out _));
        }
    }
}

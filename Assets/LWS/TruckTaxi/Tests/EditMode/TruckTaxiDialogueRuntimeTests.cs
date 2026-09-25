using NUnit.Framework;
using UnityEngine;

namespace LWS.TruckTaxi.Tests
{
    public sealed class TruckTaxiDialogueRuntimeTests
    {
        private sealed class Output:ITruckTaxiBarkOutput
        {
            public bool IsPlaying { get; private set; }
            public bool AudioPlaying=>false;
            public string Subtitle { get; private set; }
            public void Play(string speaker,string subtitle,AudioClip clip) { IsPlaying=true; Subtitle=subtitle; }
            public void Stop() { IsPlaying=false; Subtitle=""; }
            public void SetPaused(bool paused) { }
        }
        private System.Func<GameObject,ITruckTaxiBarkOutput> prior;
        private GameObject go;
        private PassengerProfile passenger;
        private TruckTaxiDialogueSet lines;
        private TruckTaxiDialoguePlayer player;
        [SetUp] public void Setup()
        {
            prior=TruckTaxiBarkBridge.Create; TruckTaxiBarkBridge.Create=_=>new Output();
            go=new GameObject("Taxi bark fixture"); player=go.AddComponent<TruckTaxiDialoguePlayer>();
            typeof(TruckTaxiDialoguePlayer).GetMethod("Awake",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(player,null);
            passenger=ScriptableObject.CreateInstance<PassengerProfile>(); passenger.passengerId="test";
            lines=ScriptableObject.CreateInstance<TruckTaxiDialogueSet>(); passenger.authoredDialogue=lines;
        }
        [TearDown] public void Cleanup()
        { Object.DestroyImmediate(go); Object.DestroyImmediate(passenger); Object.DestroyImmediate(lines); TruckTaxiBarkBridge.Create=prior; }
        [Test] public void MissingAudioStillShowsSubtitle()
        {
            lines.lines=new[]{new TruckTaxiDialogueLine { lineId="hello",category=TruckTaxiDialogueCategory.PickupGreeting,text="Hello" }};
            Assert.IsTrue(player.Speak(passenger,TruckTaxiDialogueCategory.PickupGreeting,null));
            Assert.AreEqual("Hello",player.Subtitle); Assert.IsFalse(player.AudioPlaying);
        }
        [Test] public void OneShotAndCooldownCannotBeBypassedByFallback()
        {
            lines.lines=new[]{new TruckTaxiDialogueLine { lineId="once",category=TruckTaxiDialogueCategory.GeneralChatter,text="Once",oneShot=true,cooldown=10 }};
            Assert.IsTrue(player.Speak(passenger,TruckTaxiDialogueCategory.GeneralChatter,null)); player.Stop();
            Assert.IsFalse(player.Speak(passenger,TruckTaxiDialogueCategory.GeneralChatter,null,"fallback"));
            player.ResetRide(); Assert.IsTrue(player.Speak(passenger,TruckTaxiDialogueCategory.GeneralChatter,null));
        }
        [Test] public void EjectionInterruptsButChatterDoesNotInterruptEjection()
        {
            Assert.IsTrue(player.Speak(passenger,TruckTaxiDialogueCategory.GeneralChatter,null,"Calm",10));
            Assert.IsTrue(player.Speak(passenger,TruckTaxiDialogueCategory.EjectionReaction,null,"Ejected",100));
            Assert.IsFalse(player.Speak(passenger,TruckTaxiDialogueCategory.GeneralChatter,null,"Chatter",10));
            Assert.AreEqual("Ejected",player.Subtitle);
        }
        [Test] public void AuthoredPriorityCanOverrideCategoryDefault()
        {
            player.Speak(passenger,TruckTaxiDialogueCategory.GeneralChatter,null,"Existing",50);
            lines.lines=new[]{new TruckTaxiDialogueLine { lineId="urgent",category=TruckTaxiDialogueCategory.UniqueMechanicReaction,text="Urgent",interruptPriority=80 }};
            Assert.IsTrue(player.Speak(passenger,TruckTaxiDialogueCategory.UniqueMechanicReaction,null,null,10));
        }
    }
}

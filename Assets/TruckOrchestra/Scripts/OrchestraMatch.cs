using System;
using System.Text;
using LWS.TruckDanceOff;
using LWS.TruckSports;
using UnityEngine;

namespace LWS.TruckOrchestra
{
    // Dance-Off remains the sole DSP clock, input/calibration, AI-note, judgment,
    // combo and round authority. This adapter supplies independent music parts.
    public sealed class OrchestraMatch : DanceMatch
    {
        public OrchestraScore score;
        public OrchestraInstrument[] instruments = new OrchestraInstrument[4];
        public OrchestraConductor conductor;
        public SportsEventCamera broadcast;
        public TMPro.TMP_Text ensembleChoice;
        public bool Cooperative { get; private set; }
        public int AudibleNotes { get; private set; }
        public int AudibleMistakes { get; private set; }
        protected override string Prefix => "TruckOrchestra.Presentation.v1.";
        public override string Title => "TRUCK ORCHESTRA";
        public override bool BroadcastFramingApplicable => false; // Existing DanceConcertCamera owns musical shot composition.
        public override string Help => "FOUR TRUCKS. ONE RIDICULOUS SYMPHONY.\n\nEach participant owns a different instrumental part: grand piano, drums and cymbals, brass horns, or xylophone and bells. Use your assigned LEFT / DOWN / UP / RIGHT rhythm lanes. Complete simultaneous notes together and hold long notes to their tails. Successful note heads sound immediately; held notes score only when completed. Misses produce a quiet comic wrong note.\n\nThe conductor, moving keys, mallets, bells and trumpets follow the same music clock. PERFECT / GREAT / GOOD timing and calibrated keyboard/controller input use the established Dance-Off engine. Use CALIBRATE in setup if your display or audio is delayed. Your instrument contributes its own real notes to the arrangement.\n\nChoose ENSEMBLE for one combined performance or COMPETITIVE for individual timing scores. Choose one song or a three-song concert, seven original arrangements and four chart difficulties. AI uses the same timed note and hold rules with difficulty-dependent errors. Pause and reconnect preserve the music position and use a resume count-in. Horn remains available through the shared controls. All scores lock before the finale; rematch resets every instrument.";
        public override string Header => Title + " · " + (Cooperative ? "ENSEMBLE" : "COMPETITIVE") + (Song == null ? "" : " · " + Song.title);
        public override string Results
        {
            get
            {
                if (!Cooperative) return base.Results;
                int sum = 0; var text = new StringBuilder("ENSEMBLE CONCERT COMPLETE\n\n");
                for (int i = 0; i < setup.count; i++) { sum += total[i]; text.Append(setup.players[i].name).Append(" · ").Append(instruments[i].instrumentName).Append(" · ").Append(total[i].ToString("N0")).Append("\n"); }
                return text.Append("\nENSEMBLE SCORE ").Append(sum.ToString("N0")).ToString();
            }
        }
        protected override void Awake()
        {
            base.Awake(); Cooperative = SportsPrefs.GetInt(Prefix + "Cooperative", 1) != 0;
        }
        protected override DanceNote[] ChartForParticipant(int slot) => score.Chart((option % 7 + Round - 1) % 7, slot, option / 7);
        protected override void BeginGame()
        {
            score.Validate(library);
            if (instruments == null || instruments.Length != 4 || Array.Exists(instruments, item => item == null))
                throw new InvalidOperationException("Four authored animated instrumental stations are required.");
            AudibleNotes = AudibleMistakes = 0; base.BeginGame();
        }
        protected override void SongPrepared()
        {
            if (conductor != null) conductor.ResetPose(true);
            foreach (var dancer in dancers) if (dancer != null) dancer.GetComponent<OrchestraAssemblyStage>().Begin();
            for (int i = 0; i < 4; i++) instruments[i].ResetPerformance();
            for (int i = 0; i < setup.count; i++)
            {
                int owner = i;
                Rules[i].Started += (index, grade) =>
                {
                    var note = Rules[owner].Note(index);
                    int bar = (int)(note.time / (Song.Beat * 4));
                    instruments[owner].Play(note.mask, bar, (option % 7) % 5, grade, note.duration, SongTime);
                    AudibleNotes++;
                };
                Rules[i].Judged += (index, grade) =>
                {
                    if (grade != DanceGrade.Miss) return;
                    instruments[owner].Mistake(); AudibleMistakes++;
                };
            }
        }
        protected override void Present(float dt)
        {
            base.Present(dt);
            if (!Live || Song == null) return;
            foreach (var instrument in instruments) if (instrument != null) instrument.Animate(SongTime, Song.Beat);
            if (conductor != null) conductor.Conduct(SongTime, Song.Beat);
        }
        protected override void SetPaused(bool pause)
        {
            base.SetPaused(pause); foreach (var instrument in instruments) if (instrument != null) instrument.Pause(pause);
        }
        protected override void StopGame()
        {
            foreach (var instrument in instruments) if (instrument != null) instrument.ResetPerformance(false);
            if (conductor != null) conductor.ResetPose(); base.StopGame();
        }
        public override void RefreshExtraUI()
        {
            base.RefreshExtraUI(); if (ensembleChoice != null) ensembleChoice.text = Cooperative ? "ENSEMBLE · SHARED PERFORMANCE" : "COMPETITIVE · INDIVIDUAL TIMING";
        }
        public override void ExtraAction(string key)
        {
            if (key != "orchestra-ensemble") { base.ExtraAction(key); return; }
            Cooperative = !Cooperative; SportsPrefs.SetInt(Prefix + "Cooperative", Cooperative ? 1 : 0); SportsPrefs.Save(); RefreshExtraUI(); menus.Dirty();
        }
    }
}

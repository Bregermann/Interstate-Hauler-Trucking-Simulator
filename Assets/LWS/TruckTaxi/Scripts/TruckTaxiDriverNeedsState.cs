using System;
using UnityEngine;

namespace LWS.TruckTaxi
{
    public enum TruckTaxiBladderState { Fine, Building, NeedToGo, Urgent, Crisis }
    public enum TruckTaxiDriverNeedEvent { BladderUrgent, JugStarted, JugSucceeded, JugSpilled, BathroomStop, CrisisAccident, JugDisposed, CabCleaned, JugCancelled, JugThrownFromWindow, LitterThrown }

    // Session semantics only. No clock, files, physics, or temporary QTE persistence.
    public sealed class TruckTaxiDriverNeedsState
    {
        private readonly TruckTaxiEnvironmentSettings settings;
        private readonly System.Random random;
        private float cycleSeconds, crisisSeconds, releasedSeconds, instability;
        private bool urgentEmitted;
        private int nextCue;
        private readonly int[] items = new int[Enum.GetValues(typeof(TruckTaxiNeedsItem)).Length];
        private TruckTaxiNeedsItem reservedContainer;
        private bool freePlayInventoryGranted;
        public float Hunger { get; private set; }
        public float Thirst { get; private set; }
        public float StrangeUiSeconds { get; private set; }
        public float ArcadeRushSeconds { get; private set; }
        public int BottlesThrown { get; private set; }
        public int JugsThrown { get; private set; }
        public float Pressure { get; private set; }
        public bool FilledJug => Count(TruckTaxiNeedsItem.FilledJug) + Count(TruckTaxiNeedsItem.FilledBottle) > 0;
        public bool HasEmptyContainer => Count(TruckTaxiNeedsItem.EmptyPissJug) + Count(TruckTaxiNeedsItem.EmptyBottle) > 0;
        public bool CanStartJug => !JugActive && Pressure > .02f &&
            (Count(TruckTaxiNeedsItem.EmptyPissJug) > 0 && CanAdd(TruckTaxiNeedsItem.FilledJug) ||
             Count(TruckTaxiNeedsItem.EmptyBottle) > 0 && CanAdd(TruckTaxiNeedsItem.FilledBottle));
        public bool CabMess { get; private set; }
        public bool JugActive { get; private set; }
        public float JugElapsed { get; private set; }
        public int JugCuesCompleted { get; private set; }
        public int SuccessfulJugs { get; private set; }
        public int Spills { get; private set; }
        public int CrisisAccidents { get; private set; }
        public float CycleGameHours => cycleSeconds / 3600;
        public float JugProgress => Mathf.Clamp01(JugElapsed / Mathf.Max(3, settings.jugDurationSeconds));
        public float NextCueProgress => (nextCue + 1) * .25f;
        public TruckTaxiBladderState BladderState => Pressure >= 1 ? TruckTaxiBladderState.Crisis : Pressure >= .85f ? TruckTaxiBladderState.Urgent :
            Pressure >= .65f ? TruckTaxiBladderState.NeedToGo : Pressure >= .25f ? TruckTaxiBladderState.Building : TruckTaxiBladderState.Fine;
        public event Action<TruckTaxiDriverNeedEvent> Event;

        public TruckTaxiDriverNeedsState(TruckTaxiEnvironmentSettings settings, int seed)
        {
            this.settings = settings != null ? settings : throw new ArgumentNullException(nameof(settings));
            random = new System.Random(seed); SampleCycle(); SetPressure(settings.startingBladder);
            AddItem(TruckTaxiNeedsItem.EmptyPissJug);
        }
        public void GrantFreePlayTestInventory()
        {
            if (freePlayInventoryGranted) return;
            freePlayInventoryGranted = true;
            foreach (var entry in TruckTaxiNeedsItems.Store)
                if (Count(entry.Item) == 0) AddItem(entry.Item);
        }
        private void SampleCycle()
        {
            float minimum = Mathf.Max(.1f, settings.bladderCycleGameHours.x);
            cycleSeconds = Mathf.Lerp(minimum, Mathf.Max(minimum, settings.bladderCycleGameHours.y), (float)random.NextDouble()) * 3600;
        }
        public void AdvanceGameSeconds(double seconds)
        {
            if (seconds <= 0 || double.IsNaN(seconds) || double.IsInfinity(seconds)) return;
            double untilFull = (1 - Pressure) * cycleSeconds;
            Pressure = Mathf.Clamp01(Pressure + (float)(seconds / cycleSeconds));
            Hunger = Mathf.Clamp01(Hunger + (float)(seconds / (8 * 3600)));
            Thirst = Mathf.Clamp01(Thirst + (float)(seconds / (5 * 3600)));
            if (Pressure >= .85f && !urgentEmitted) { urgentEmitted = true; Event?.Invoke(TruckTaxiDriverNeedEvent.BladderUrgent); }
            if (Pressure < 1) return;
            crisisSeconds += (float)Math.Max(0, seconds - untilFull);
            if (crisisSeconds >= Mathf.Max(1, settings.crisisGraceGameMinutes) * 60)
            {
                if (JugActive) CancelJug();
                CabMess = true; CrisisAccidents++; Relieve(.15f);
                Event?.Invoke(TruckTaxiDriverNeedEvent.CrisisAccident);
            }
        }
        public void SetPressure(float value)
        {
            Pressure = float.IsFinite(value) ? Mathf.Clamp01(value) : 0;
            crisisSeconds = 0; urgentEmitted = Pressure < .85f ? false : urgentEmitted;
        }
        public bool StartJug()
        {
            if (!CanStartJug) return false;
            reservedContainer = Count(TruckTaxiNeedsItem.EmptyPissJug) > 0 && CanAdd(TruckTaxiNeedsItem.FilledJug) ?
                TruckTaxiNeedsItem.EmptyPissJug : TruckTaxiNeedsItem.EmptyBottle;
            RemoveItem(reservedContainer);
            JugActive = true; JugElapsed = releasedSeconds = instability = 0; JugCuesCompleted = nextCue = 0;
            Event?.Invoke(TruckTaxiDriverNeedEvent.JugStarted); return true;
        }
        public float CueHalfWindow(bool moving) => Mathf.Clamp(moving ? settings.drivingCueHalfWindow : settings.stoppedCueHalfWindow, .03f, .45f);
        public bool CueIsOpen(bool moving) => JugActive && nextCue < 3 && Mathf.Abs(JugProgress - NextCueProgress) <= CueHalfWindow(moving) * .25f;
        public void TickJug(float deltaTime, bool hold, bool tap, bool moving, float lateralAcceleration)
        {
            if (!JugActive || deltaTime <= 0 || !float.IsFinite(deltaTime)) return;
            JugElapsed += deltaTime;
            releasedSeconds = hold ? 0 : releasedSeconds + deltaTime;
            float acceleration = float.IsFinite(lateralAcceleration) ? Mathf.Abs(lateralAcceleration) : 0;
            instability = Mathf.Max(0, instability + deltaTime * (moving && acceleration > Mathf.Max(.5f, settings.lateralAccelerationSpillThreshold) ? 1 : -.75f));
            if (releasedSeconds > Mathf.Max(.1f, settings.jugReleaseGraceSeconds) || instability > .6f) { FinishJug(false); return; }
            if (tap)
            {
                if (!hold || !CueIsOpen(moving)) { FinishJug(false); return; }
                JugCuesCompleted++; nextCue++;
            }
            if (nextCue < 3 && JugProgress > NextCueProgress + CueHalfWindow(moving) * .25f) { FinishJug(false); return; }
            if (JugProgress >= 1) FinishJug(JugCuesCompleted == 3);
        }
        public void FinishJug(bool success)
        {
            if (!JugActive) return;
            JugActive = false;
            if (success) { AddItem(reservedContainer == TruckTaxiNeedsItem.EmptyPissJug ? TruckTaxiNeedsItem.FilledJug : TruckTaxiNeedsItem.FilledBottle); SuccessfulJugs++; Relieve(0); Event?.Invoke(TruckTaxiDriverNeedEvent.JugSucceeded); }
            else { AddItem(reservedContainer); CabMess = true; Spills++; Relieve(.2f); Event?.Invoke(TruckTaxiDriverNeedEvent.JugSpilled); }
        }
        public bool UseBathroom(bool properlyStopped)
        {
            if (!properlyStopped || JugActive) return false;
            Relieve(0); Event?.Invoke(TruckTaxiDriverNeedEvent.BathroomStop); return true;
        }
        public bool DisposeJug(bool atDisposal)
        {
            if (!atDisposal || JugActive || !FilledJug) return false;
            if (Count(TruckTaxiNeedsItem.FilledJug) > 0) { RemoveItem(TruckTaxiNeedsItem.FilledJug); AddItem(TruckTaxiNeedsItem.EmptyPissJug); }
            else { RemoveItem(TruckTaxiNeedsItem.FilledBottle); AddItem(TruckTaxiNeedsItem.EmptyBottle); }
            Event?.Invoke(TruckTaxiDriverNeedEvent.JugDisposed); return true;
        }
        public bool CleanCab(bool atService)
        {
            if (!atService || JugActive || !CabMess) return false;
            CabMess = false; Event?.Invoke(TruckTaxiDriverNeedEvent.CabCleaned); return true;
        }
        public void CancelJug()
        {
            bool wasActive = JugActive;
            JugActive = false; JugElapsed = 0;
            if (wasActive) AddItem(reservedContainer);
            if (wasActive) Event?.Invoke(TruckTaxiDriverNeedEvent.JugCancelled);
        }
        public int Count(TruckTaxiNeedsItem item) => (int)item >= 0 && (int)item < items.Length ? items[(int)item] : 0;
        public bool CanAdd(TruckTaxiNeedsItem item) => Enum.IsDefined(typeof(TruckTaxiNeedsItem), item) && Count(item) < 99;
        public bool AddItem(TruckTaxiNeedsItem item, int count = 1)
        {
            if (!Enum.IsDefined(typeof(TruckTaxiNeedsItem), item) || count <= 0 || items[(int)item] > 99 - count) return false;
            items[(int)item] += count; return true;
        }
        private bool RemoveItem(TruckTaxiNeedsItem item)
        { if (Count(item) <= 0) return false; items[(int)item]--; return true; }
        public bool Consume(TruckTaxiNeedsItem item)
        {
            var entry = TruckTaxiNeedsItems.Find(item);
            if (entry == null || (entry.HungerRelief <= 0 && entry.ThirstRelief <= 0 &&
                item != TruckTaxiNeedsItem.HighOctaneSuppository && item != TruckTaxiNeedsItem.MysteryMushrooms) ||
                (entry.CreatesBottle && !CanAdd(TruckTaxiNeedsItem.EmptyBottle)) || !RemoveItem(item)) return false;
            Hunger = Mathf.Clamp01(Hunger - entry.HungerRelief);
            Thirst = Mathf.Clamp01(Thirst - entry.ThirstRelief);
            if (entry.CreatesBottle) AddItem(TruckTaxiNeedsItem.EmptyBottle);
            if (item == TruckTaxiNeedsItem.MysteryMushrooms) StrangeUiSeconds = 12;
            if (item == TruckTaxiNeedsItem.HighOctaneSuppository) ArcadeRushSeconds = 8;
            return true;
        }
        public void TickArcadeEffects(float seconds)
        {
            if (seconds <= 0 || !float.IsFinite(seconds)) return;
            StrangeUiSeconds = Mathf.Max(0, StrangeUiSeconds - seconds);
            ArcadeRushSeconds = Mathf.Max(0, ArcadeRushSeconds - seconds);
        }
        public bool ThrowFilled(TruckTaxiNeedsItem item)
        {
            if (JugActive || (item != TruckTaxiNeedsItem.FilledBottle && item != TruckTaxiNeedsItem.FilledJug) || !RemoveItem(item)) return false;
            if (item == TruckTaxiNeedsItem.FilledJug) JugsThrown++; else BottlesThrown++;
            return true;
        }
        private void Relieve(float remainder) { Pressure = remainder; crisisSeconds = 0; urgentEmitted = false; SampleCycle(); }
    }
}

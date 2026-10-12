using System;
using System.Collections.Generic;
using UnityEngine;

namespace LWS.TruckTaxi
{
    public enum TruckTaxiBladderState { Fine, Building, NeedToGo, Urgent, Crisis }
    public enum TruckTaxiDriverNeedEvent { BladderUrgent, JugStarted, JugSucceeded, JugSpilled, BathroomStop, CrisisAccident, JugDisposed, CabCleaned, JugCancelled, JugThrownFromWindow, LitterThrown, BowelUrgent, BowelAccident }

    // Needs authority only. Save orchestration stays with LwsSaveService / Pixel Crushers.
    public sealed class TruckTaxiDriverNeedsState
    {
        private readonly TruckTaxiEnvironmentSettings settings;
        private readonly System.Random random;
        private float cycleSeconds, crisisSeconds, releasedSeconds, instability;
        private float bowelCrisisSeconds;
        private bool bowelUrgentEmitted;
        private bool urgentEmitted;
        private int nextCue;
        private readonly int[] items = new int[Enum.GetValues(typeof(TruckTaxiNeedsItem)).Length];
        private readonly HashSet<string> usedToiletIds = new HashSet<string>(StringComparer.Ordinal);
        private TruckTaxiNeedsItem reservedContainer;
        private bool freePlayInventoryGranted;
        private bool persistentStateRestored;
        public bool StarterInventoryGranted => freePlayInventoryGranted;
        public float Hunger { get; private set; }
        public float Thirst { get; private set; }
        public float Fatigue { get; private set; }
        public float FatigueGainMultiplier
        {
            get
            {
                float multiplier = Effects.GetSnapshot(TruckTaxiTemporaryEffectKind.RoadPep).RemainingSeconds > 0 ?
                    .25f + Effects.RoadPepOveruse * .2f : 1;
                if (Effects.GetSnapshot(TruckTaxiTemporaryEffectKind.RoadPepComedown).RemainingSeconds > 0)
                    multiplier *= 1.2f + Effects.RoadPepOveruse * .1f;
                if (Effects.GetSnapshot(TruckTaxiTemporaryEffectKind.EnergyDrink).RemainingSeconds > 0) multiplier *= .8f;
                return multiplier;
            }
        }
        public TruckTaxiTemporaryEffects Effects { get; } = new TruckTaxiTemporaryEffects();
        public float StrangeUiSeconds => Effects.GetSnapshot(TruckTaxiTemporaryEffectKind.MysteryMushroom).RemainingSeconds;
        public float ArcadeRushSeconds => Effects.GetSnapshot(TruckTaxiTemporaryEffectKind.HighOctaneBoost).RemainingSeconds;
        public int BottlesThrown { get; private set; }
        public int JugsThrown { get; private set; }
        public float Pressure { get; private set; }
        public float BowelPressure { get; private set; }
        public int BowelAccidents { get; private set; }
        public float BowelDiscomfortGameSeconds { get; private set; }
        public int ToiletsDiscovered => usedToiletIds.Count;
        private readonly HashSet<string> ambientChoiceIds = new HashSet<string>(StringComparer.Ordinal);
        public IReadOnlyCollection<string> AmbientChoiceIds => ambientChoiceIds;
        public bool RecordAmbientChoice(string stableChoiceId) => !string.IsNullOrWhiteSpace(stableChoiceId) &&
            stableChoiceId.Length <= 128 && ambientChoiceIds.Add(stableChoiceId);
        public string RentedMotelId { get; private set; }
        public double MotelAccessExpiresAtGameSeconds { get; private set; }
        public bool HasMotelAccess(string stableId, double now) => !string.IsNullOrWhiteSpace(stableId) &&
            string.Equals(stableId, RentedMotelId, StringComparison.Ordinal) &&
            double.IsFinite(now) && now < MotelAccessExpiresAtGameSeconds;
        public bool RentMotel(string stableId, double now, double durationGameSeconds)
        {
            if (string.IsNullOrWhiteSpace(stableId) || stableId.Length > 128 || !double.IsFinite(now) ||
                !double.IsFinite(durationGameSeconds) || durationGameSeconds <= 0) return false;
            RentedMotelId = stableId;
            MotelAccessExpiresAtGameSeconds = now + durationGameSeconds;
            return true;
        }
        public bool HasUsedToilet(string stableId) => !string.IsNullOrWhiteSpace(stableId) && usedToiletIds.Contains(stableId);
        public event Action<int> ToiletCollectionChanged;
        public bool RecordToiletUse(string stableId)
        {
            if (string.IsNullOrWhiteSpace(stableId) || stableId.Length > 128 || !usedToiletIds.Add(stableId)) return false;
            ToiletCollectionChanged?.Invoke(usedToiletIds.Count);
            return true;
        }
        public void RelieveBowel()
        { BowelPressure = 0; bowelCrisisSeconds = 0; bowelUrgentEmitted = false; BowelDiscomfortGameSeconds = 0; }
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
            CreateNewVibinInventory();
        }
        // Explicit new-save creation only. Initialize and restore never call this.
        public bool CreateNewVibinInventory()
        {
            if (freePlayInventoryGranted || persistentStateRestored || JugActive) return false;
            foreach (var entry in TruckTaxiNeedsItems.All)
                if (entry.PlayerOwnable) items[(int)entry.Item] = 10;
            freePlayInventoryGranted = true;
            return true;
        }
        public TruckTaxiDriverNeedsSnapshot CapturePersistentState()
        {
            var inventory = new List<TruckTaxiNeedsInventoryEntry>();
            foreach (var entry in TruckTaxiNeedsItems.All)
                if (entry.PlayerOwnable) inventory.Add(new TruckTaxiNeedsInventoryEntry {
                    ItemId = entry.StableId, Count = Count(entry.Item) + (JugActive && reservedContainer == entry.Item ? 1 : 0) });
            return new TruckTaxiDriverNeedsSnapshot {
                Pressure = Pressure, Hunger = Hunger, Thirst = Thirst, Fatigue = Fatigue,
                BowelPressure = BowelPressure, BowelCrisisGameSeconds = bowelCrisisSeconds,
                BowelAccidents = BowelAccidents, BowelDiscomfortGameSeconds = BowelDiscomfortGameSeconds,
                UsedToiletIds = new List<string>(usedToiletIds).ToArray(),
                AmbientChoiceIds = new List<string>(ambientChoiceIds).ToArray(),
                RentedMotelId = RentedMotelId, MotelAccessExpiresAtGameSeconds = MotelAccessExpiresAtGameSeconds,
                StarterInventoryGranted = freePlayInventoryGranted, CabMess = CabMess,
                CycleGameSeconds = cycleSeconds, CrisisGameSeconds = crisisSeconds, Inventory = inventory.ToArray() };
        }
        public bool RestorePersistentState(TruckTaxiDriverNeedsSnapshot snapshot, out string error)
        {
            error = null;
            if (snapshot == null || snapshot.Version != 1 || snapshot.Inventory == null)
            { error = "Missing or unsupported needs snapshot."; return false; }
            if (!Normalized(snapshot.Pressure) || !Normalized(snapshot.Hunger) || !Normalized(snapshot.Thirst) || !Normalized(snapshot.Fatigue) || !Normalized(snapshot.BowelPressure) ||
                !float.IsFinite(snapshot.CycleGameSeconds) || snapshot.CycleGameSeconds < 0 ||
                !float.IsFinite(snapshot.CrisisGameSeconds) || snapshot.CrisisGameSeconds < 0 ||
                !float.IsFinite(snapshot.BowelCrisisGameSeconds) || snapshot.BowelCrisisGameSeconds < 0 ||
                snapshot.BowelAccidents < 0 || !float.IsFinite(snapshot.BowelDiscomfortGameSeconds) || snapshot.BowelDiscomfortGameSeconds < 0)
            { error = "Invalid needs values."; return false; }
            if ((!string.IsNullOrEmpty(snapshot.RentedMotelId) &&
                    (string.IsNullOrWhiteSpace(snapshot.RentedMotelId) || snapshot.RentedMotelId.Length > 128)) ||
                !double.IsFinite(snapshot.MotelAccessExpiresAtGameSeconds) || snapshot.MotelAccessExpiresAtGameSeconds < 0)
            { error = "Invalid motel access."; return false; }
            var restored = new int[items.Length];
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var restoredToilets = new HashSet<string>(StringComparer.Ordinal);
            if (snapshot.UsedToiletIds != null)
                foreach (var id in snapshot.UsedToiletIds)
                    if (string.IsNullOrWhiteSpace(id) || id.Length > 128 || !restoredToilets.Add(id))
                    { error = "Invalid or duplicate toilet identity."; return false; }
            var restoredChoices = new HashSet<string>(StringComparer.Ordinal);
            if (snapshot.AmbientChoiceIds != null)
                foreach (var id in snapshot.AmbientChoiceIds)
                    if (string.IsNullOrWhiteSpace(id) || id.Length > 128 || !restoredChoices.Add(id))
                    { error = "Invalid or duplicate ambient choice identity."; return false; }
            foreach (var item in snapshot.Inventory)
            {
                var entry = item == null ? null : TruckTaxiNeedsItems.Find(item.ItemId);
                if (entry == null || !entry.PlayerOwnable || !seen.Add(item.ItemId) || item.Count < 0 || item.Count > 99)
                { error = "Invalid, duplicate, or unknown inventory item: " + item?.ItemId; return false; }
                restored[(int)entry.Item] = item.Count;
            }
            // Validate everything first. An invalid payload cannot partially overwrite live state.
            CancelJug(); Effects.Clear();
            Array.Copy(restored, items, items.Length);
            Pressure = snapshot.Pressure; Hunger = snapshot.Hunger; Thirst = snapshot.Thirst; Fatigue = snapshot.Fatigue;
            BowelPressure = snapshot.BowelPressure;
            bowelCrisisSeconds = snapshot.BowelCrisisGameSeconds;
            BowelAccidents = snapshot.BowelAccidents;
            BowelDiscomfortGameSeconds = snapshot.BowelDiscomfortGameSeconds;
            bowelUrgentEmitted = BowelPressure >= .85f;
            usedToiletIds.Clear(); usedToiletIds.UnionWith(restoredToilets);
            ambientChoiceIds.Clear(); ambientChoiceIds.UnionWith(restoredChoices);
            RentedMotelId = snapshot.RentedMotelId;
            MotelAccessExpiresAtGameSeconds = snapshot.MotelAccessExpiresAtGameSeconds;
            CabMess = snapshot.CabMess; freePlayInventoryGranted = snapshot.StarterInventoryGranted;
            if (snapshot.CycleGameSeconds > 0) cycleSeconds = snapshot.CycleGameSeconds;
            crisisSeconds = snapshot.CrisisGameSeconds; urgentEmitted = Pressure >= .85f;
            persistentStateRestored = true;
            return true;
        }
        private static bool Normalized(float value) => float.IsFinite(value) && value >= 0 && value <= 1;
        private void SampleCycle()
        {
            float minimum = Mathf.Max(.1f, settings.bladderCycleGameHours.x);
            cycleSeconds = Mathf.Lerp(minimum, Mathf.Max(minimum, settings.bladderCycleGameHours.y), (float)random.NextDouble()) * 3600;
        }
        public void AdvanceGameSeconds(double seconds)
        { AdvanceGameSeconds(seconds, TruckTaxiFatigueActivity.Parked, 12); }
        public void AdvanceGameSeconds(double seconds, TruckTaxiFatigueActivity activity, float startHour)
        {
            Fatigue = Mathf.Clamp01(Fatigue + TruckTaxiFatigueRules.Gain(seconds, activity, startHour) * FatigueGainMultiplier);
            AdvanceCoreNeeds(seconds);
        }
        public bool ApplySleep(int hours, float recoveryQuality = 1f)
        {
            if (!TruckTaxiFatigueRules.IsSleepDuration(hours) || JugActive) return false;
            // Sleep outlasts short stimulant effects; no indefinitely frozen timer across a time skip.
            Effects.Clear();
            for (int minute = 0; minute < hours * 60; minute++) AdvanceCoreNeeds(60);
            Fatigue = Mathf.Clamp01(Fatigue - TruckTaxiFatigueRules.SleepRecovery(hours) * Mathf.Clamp(recoveryQuality, .5f, 1.25f));
            return true;
        }
        private void AdvanceCoreNeeds(double seconds)
        {
            if (seconds <= 0 || double.IsNaN(seconds) || double.IsInfinity(seconds)) return;
            float bladderRate = Effects.BladderRateMultiplier;
            double untilFull = (1 - Pressure) * cycleSeconds / Mathf.Max(.01f, bladderRate);
            Pressure = Mathf.Clamp01(Pressure + (float)(seconds / cycleSeconds) * bladderRate);
            Hunger = Mathf.Clamp01(Hunger + (float)(seconds / (8 * 3600)) * Effects.HungerRateMultiplier);
            Thirst = Mathf.Clamp01(Thirst + (float)(seconds / 3600) * SafeRate(settings.thirstGainPerGameHour, .05f) * Effects.ThirstRateMultiplier);
            float bowelRate = SafeRate(settings.bowelGainPerGameHour, 1f / 12f);
            double bowelUntilFull = BowelPressure >= 1 ? 0 : bowelRate > 0 ? (1 - BowelPressure) * 3600 / bowelRate : double.PositiveInfinity;
            BowelDiscomfortGameSeconds = Mathf.Max(0, BowelDiscomfortGameSeconds - (float)seconds);
            BowelPressure = Mathf.Clamp01(BowelPressure + (float)(seconds / 3600) * bowelRate);
            if (BowelPressure >= .85f && !bowelUrgentEmitted)
            { bowelUrgentEmitted = true; Event?.Invoke(TruckTaxiDriverNeedEvent.BowelUrgent); }
            if (BowelPressure >= 1f)
            {
                bowelCrisisSeconds += (float)Math.Max(0, seconds - bowelUntilFull);
                if (bowelCrisisSeconds >= Mathf.Max(1, settings.bowelGraceGameMinutes) * 60) FailBowel();
            }
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
        private static float SafeRate(float value, float fallback) => float.IsFinite(value) ? Mathf.Clamp01(value) : fallback;
        private void AddBowelLoad(float value)
        {
            if (float.IsFinite(value) && value > 0) BowelPressure = Mathf.Clamp01(BowelPressure + value);
        }
        private void FailBowel()
        {
            CabMess = true;
            if (BowelAccidents < int.MaxValue) BowelAccidents++;
            BowelPressure = .15f; bowelCrisisSeconds = 0; bowelUrgentEmitted = false;
            BowelDiscomfortGameSeconds = 1800;
            Event?.Invoke(TruckTaxiDriverNeedEvent.BowelAccident);
        }
        public void DebugSetBowelPercent(float percent)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!float.IsFinite(percent)) return;
            BowelPressure = Mathf.Clamp01(percent / 100); bowelCrisisSeconds = 0; bowelUrgentEmitted = false;
#endif
        }
        public void DebugForceBowelFailure()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            FailBowel();
#endif
        }
        // Called by the completed adult interaction, never by ordinary drinks.
        public void SatisfyThirst() { Thirst = 0; }
        public void RelieveThirst(float fraction) { if(float.IsFinite(fraction)) Thirst=Mathf.Clamp01(Thirst-Mathf.Max(0,fraction)); }
        public void EatMeal(float hungerRelief, float bowelLoad = .24f)
        {
            if (!float.IsFinite(hungerRelief) || hungerRelief <= 0) return;
            Hunger = Mathf.Clamp01(Hunger - hungerRelief); AddBowelLoad(bowelLoad);
        }
        public void DebugSetThirst(float value)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (float.IsFinite(value)) Thirst = Mathf.Clamp01(value);
#endif
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
            Relieve(0); RelieveBowel(); Event?.Invoke(TruckTaxiDriverNeedEvent.BathroomStop); return true;
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
        public bool CanAdd(TruckTaxiNeedsItem item) => TruckTaxiNeedsItems.Find(item)?.PlayerOwnable == true &&
            Count(item) < 99 - (JugActive && reservedContainer == item ? 1 : 0);
        public bool AddItem(TruckTaxiNeedsItem item, int count = 1)
        {
            if (TruckTaxiNeedsItems.Find(item)?.PlayerOwnable != true || count <= 0 || count > 99 ||
                items[(int)item] > 99 - count - (JugActive && reservedContainer == item ? 1 : 0)) return false;
            items[(int)item] += count; return true;
        }
        private bool RemoveItem(TruckTaxiNeedsItem item)
        { if (Count(item) <= 0) return false; items[(int)item]--; return true; }
        // Content-hook inventory debit only; ordinary drink/food effects use Consume.
        public bool TryConsumeItem(string stableItemId, int quantity)
        {
            var entry = TruckTaxiNeedsItems.FindByStableId(stableItemId);
            if (JugActive || quantity < 0 || entry == null || !entry.PlayerOwnable || Count(entry.Item) < quantity) return false;
            items[(int)entry.Item] -= quantity;
            return true;
        }
        public bool BeginRoadPep() => !JugActive && RemoveItem(TruckTaxiNeedsItem.RoadPep);
        public void CompleteRoadPep(int score) => Effects.ActivateRoadPep(score);
        public bool Consume(TruckTaxiNeedsItem item)
        {
            var entry = TruckTaxiNeedsItems.Find(item);
            if (entry == null || (entry.HungerRelief <= 0 && entry.ThirstRelief <= 0 &&
                item != TruckTaxiNeedsItem.HighOctaneSuppository && item != TruckTaxiNeedsItem.MysteryMushrooms) ||
                (entry.CreatesBottle && !CanAdd(TruckTaxiNeedsItem.EmptyBottle)) || !RemoveItem(item)) return false;
            Hunger = Mathf.Clamp01(Hunger - entry.HungerRelief);
            AddBowelLoad(entry.BowelLoad);
            if (entry.IsDrink) RelieveThirst(TruckTaxiNeedsItems.DrinkThirstRelief);
            if (entry.CreatesBottle) AddItem(TruckTaxiNeedsItem.EmptyBottle);
            if (item == TruckTaxiNeedsItem.MysteryMushrooms) Effects.Activate(TruckTaxiTemporaryEffectKind.MysteryMushroom);
            if (item == TruckTaxiNeedsItem.HighOctaneSuppository) Effects.Activate(TruckTaxiTemporaryEffectKind.HighOctaneBoost);
            if (item == TruckTaxiNeedsItem.EnergyDrink) Effects.Activate(TruckTaxiTemporaryEffectKind.EnergyDrink);
            return true;
        }
        public void TickArcadeEffects(float seconds)
        {
            Effects.Tick(seconds);
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

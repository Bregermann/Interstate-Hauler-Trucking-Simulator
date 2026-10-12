using System;
using System.Collections.Generic;
using LWS.InterstateHauler;
using UnityEngine;

namespace LWS.TruckTaxi
{
    [Serializable]
    public sealed class TruckTaxiExtremeDayState
    {
        public long[] attempts = new long[5], occurrences = new long[5];
        public int[] dryDays = new int[5];
        public static bool Valid(TruckTaxiExtremeDayState value)
        {
            if (value == null) return true;
            if (value.attempts == null || value.occurrences == null || value.dryDays == null ||
                value.attempts.Length != 5 || value.occurrences.Length != 5 || value.dryDays.Length != 5) return false;
            for (int i = 0; i < 5; i++)
                if (value.attempts[i] < 0 || value.attempts[i] > DateTime.MaxValue.Ticks ||
                    value.occurrences[i] < 0 || value.occurrences[i] > DateTime.MaxValue.Ticks || value.dryDays[i] < 0 || value.dryDays[i] > 1000000) return false;
            return true;
        }
    }
    [Serializable]
    public sealed class TruckTaxiExtremeSaveState
    {
        public int version = 1;
        public int sequence;
        public int randomRolls;
        public long scheduledDay;
        public TruckTaxiExtremeDayState days;
        public TruckTaxiSavedExtreme[] active;
        public TruckTaxiSavedHazardSchedule[] scheduled;
        public static bool Valid(TruckTaxiExtremeSaveState value)
        {
            if (value == null) return true;
            if (value.version != 1 || value.sequence < 0 || value.sequence == int.MaxValue ||
                value.randomRolls < 0 || value.randomRolls > 1000000 || value.scheduledDay < 0 || value.scheduledDay > DateTime.MaxValue.Ticks ||
                !TruckTaxiExtremeDayState.Valid(value.days) ||
                value.active == null || value.active.Length > 128 || value.scheduled == null || value.scheduled.Length > 128) return false;
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var item in value.active)
                if (!TruckTaxiSavedExtreme.Valid(item) || !ids.Add(item.id)) return false;
            ids.Clear();
            foreach (var item in value.scheduled)
                if (item == null || !Enum.IsDefined(typeof(TruckTaxiExtremeKind), item.kind) ||
                    string.IsNullOrWhiteSpace(item.zoneId) || !ids.Add(item.zoneId) ||
                    item.starts < 0 || item.starts > DateTime.MaxValue.Ticks) return false;
            return true;
        }
    }
    [Serializable]
    public sealed class TruckTaxiSavedHazardSchedule
    {
        public TruckTaxiExtremeKind kind;
        public string zoneId;
        public long starts;
    }
    [Serializable]
    public sealed class TruckTaxiSavedExtreme
    {
        public string id, regionId, displayName;
        public TruckTaxiExtremeKind kind;
        public bool procedural, flatFallback;
        public float radius, realElapsed;
        public long started, movingEnds, ends;
        public TruckTaxiHazardPoint[] path;
        public static bool Valid(TruckTaxiSavedExtreme value)
        {
            if (value == null || !Enum.IsDefined(typeof(TruckTaxiExtremeKind), value.kind) ||
                string.IsNullOrWhiteSpace(value.id) || string.IsNullOrWhiteSpace(value.regionId) ||
                !float.IsFinite(value.radius) || value.radius < 1 || value.radius > 100000 ||
                !float.IsFinite(value.realElapsed) || value.realElapsed < 0 ||
                value.started < 0 || value.movingEnds <= value.started || value.ends < value.movingEnds ||
                value.ends > DateTime.MaxValue.Ticks || value.path == null || value.path.Length == 0 || value.path.Length > 4096) return false;
            foreach (var point in value.path)
                if (point == null || !double.IsFinite(point.x) || !double.IsFinite(point.y) || !double.IsFinite(point.z) ||
                    Math.Abs(point.x) > 1e9 || Math.Abs(point.y) > 1e9 || Math.Abs(point.z) > 1e9) return false;
            return true;
        }
    }
    [Serializable]
    public sealed class TruckTaxiHazardPoint { public double x, y, z; }

    public sealed partial class TruckTaxiExtremeEventDirector
    {
        public TruckTaxiExtremeSaveState Capture(Func<Vector3, LwsWorldPositionD> toGlobal)
        {
            var result = new TruckTaxiExtremeSaveState { sequence = proceduralSequence, randomRolls = naturalRolls,
                scheduledDay = lastScheduledDay.Ticks, days = daySchedule.Capture(),
                active = new TruckTaxiSavedExtreme[active.Count], scheduled = new TruckTaxiSavedHazardSchedule[scheduled.Count] };
            for (int i = 0; i < active.Count; i++)
            {
                var item = active[i]; var f = item.Footprint;
                var data = new TruckTaxiSavedExtreme { id = f.Id, regionId = f.RegionId, displayName = f.DisplayName,
                    kind = f.Kind, procedural = f.IsProcedural, flatFallback = f.IsFlatFallback, radius = f.RadiusMeters,
                    started = item.Started.Ticks, movingEnds = item.MovingEnds.Ticks, ends = item.Ends.Ticks,
                    realElapsed = Mathf.Max(0, Time.unscaledTime - item.RealStarted), path = new TruckTaxiHazardPoint[f.Path.Length] };
                for (int p = 0; p < data.path.Length; p++)
                {
                    var point = toGlobal(f.Path[p]);
                    data.path[p] = new TruckTaxiHazardPoint { x = point.x, y = point.y, z = point.z };
                }
                result.active[i] = data;
            }
            for (int i = 0; i < scheduled.Count; i++) result.scheduled[i] = new TruckTaxiSavedHazardSchedule
                { kind = scheduled[i].Kind, zoneId = scheduled[i].ZoneId, starts = scheduled[i].StartsAt.Ticks };
            return result;
        }

        public bool CanRestore(TruckTaxiExtremeSaveState value)
        {
            if (!TruckTaxiExtremeSaveState.Valid(value)) return false;
            if (value == null) return true;
            if (!initialized) return false;
            foreach (var item in value.active)
            {
                if (!item.procedural && (!TryZone(item.kind, item.id, out var zone) || zone.regionId != item.regionId)) return false;
                if (regional != null && regional.regions != null)
                {
                    bool known = false;
                    foreach (var region in regional.regions) if (region != null && region.id == item.regionId) { known = true; break; }
                    if (!known) return false;
                }
            }
            foreach (var item in value.scheduled)
                if (!TryZone(item.kind, item.zoneId, out _)) return false;
            return true;
        }
        public bool Restore(TruckTaxiExtremeSaveState value, Func<LwsWorldPositionD, Vector3> toLocal)
        {
            if (!CanRestore(value)) return false;
            // Clearing an old hurricane must not override the weather participant that restored before us.
            var weatherCallback = setWeather;
            setWeather = null;
            try { Clear(); }
            finally { setWeather = weatherCallback; }
            lastGameNow = gameNow();
            // Do not immediately replay the current day's already-running natural hazards.
            lastScheduledDay = lastGameNow.Date;
            lastNaturalAttempt.Clear(); daySchedule.Reset();
            LastNaturalEvent = default; LastNaturalKind = null;
            if (value == null) return true;
            proceduralSequence = value.sequence;
            lastScheduledDay = new DateTime(value.scheduledDay);
            daySchedule.Restore(value.days);
            naturalRandom = new System.Random(71629); naturalRolls = value.randomRolls;
            for (int i = 0; i < naturalRolls; i++) naturalRandom.NextDouble();
            if (value.days != null) for (int i = 0; i < 5; i++)
                if (value.days.occurrences[i] > LastNaturalEvent.Ticks)
                { LastNaturalEvent = new DateTime(value.days.occurrences[i]); LastNaturalKind = (TruckTaxiExtremeKind)i; }
            foreach (var data in value.active)
            {
                if (data.ends <= lastGameNow.Ticks && data.realElapsed >= 20) continue;
                TruckTaxiHazardZone zone = null;
                if (!data.procedural) TryZone(data.kind, data.id, out zone);
                var footprint = TruckTaxiExtremeFootprint.FromSaved(data, zone, toLocal);
                active.Add(new ActiveEvent { Footprint = footprint, Started = new DateTime(data.started),
                    MovingEnds = new DateTime(data.movingEnds), Ends = new DateTime(data.ends),
                    RealStarted = Time.unscaledTime - data.realElapsed,
                    PreviousPropOffsets = new Vector3[footprint.ShakeProps?.Length ?? 0] });
                if (data.kind == TruckTaxiExtremeKind.Hurricane)
                    ActiveSevereProfile = new TruckTaxiExtremeWeatherProfile("thunderstorm", 28, 160, 1);
                RaiseWarning(footprint, new DateTime(data.started), new DateTime(data.ends));
            }
            foreach (var data in value.scheduled)
                scheduled.Add(new ScheduledEvent(data.kind, data.zoneId, new DateTime(data.starts)));
            nextNaturalTick = Time.time + 1;
            Rebuild(lastGameNow); HazardChanged?.Invoke();
            return true;
        }
    }
}

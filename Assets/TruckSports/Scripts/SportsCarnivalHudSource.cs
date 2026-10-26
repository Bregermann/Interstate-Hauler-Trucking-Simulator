using System;
using UnityEngine;
using LWS.TruckBaseJumping;
using LWS.TruckMattressRacing;
using LWS.TruckWifeCarrying;
using LWS.TruckBogSnorkelling;
using LWS.TruckTerrierRacing;
using LWS.TruckAvalancheEscape;
using LWS.TruckRedLightGreenLight;

namespace LWS.TruckSports
{
    // Read-only sport semantics. A countdown capability is deliberately not a race classifier.
    [Serializable] public struct SportsCarnivalHudRow
    {
        public string id, name, value, detail;
        public int participant, place, group;
        public float progress, seconds, distance;
        public Color color;
    }

    public sealed class SportsCarnivalHudSource : MonoBehaviour
    {
        public SportsEventSession match;
        public static bool SupportsSport(Type type) => type == typeof(BaseJumpMatch) || type == typeof(MattressMatch) || type == typeof(WifeMatch) || type == typeof(BogMatch) || type == typeof(TerrierMatch) || type == typeof(AvalancheMatch) || type == typeof(RedLightMatch);
        public bool Supported => match != null && SupportsSport(match.GetType());
        public bool Vertical => match is BaseJumpMatch;
        public int Count => !Supported ? 0 : match is WifeMatch ? 4 : match.setup.count;
        public bool SingleGameplayView
        {
            get
            {
                if (!Supported || match.cameras == null) return false;
                int count = 0; Rect rect = default;
                foreach (var camera in match.cameras)
                    if (camera != null && camera.enabled && camera.gameObject.activeInHierarchy) { count++; rect = camera.rect; }
                return count == 1 && IsFullScreen(rect);
            }
        }
        public static bool IsFullScreen(Rect rect) => Mathf.Abs(rect.x) < .001f && Mathf.Abs(rect.y) < .001f && Mathf.Abs(rect.width - 1) < .001f && Mathf.Abs(rect.height - 1) < .001f;
        public bool Showing => Supported && match.menus != null && match.menus.Screen == SportsEventScreen.Play && !match.ReplayControls && SingleGameplayView;

        // Accepted gates bound in-segment progress; an unaccepted shortcut cannot earn a later segment.
        public static float GatedDistance(int checkpoint, float position, float[] gates, float finish)
        {
            checkpoint = Mathf.Clamp(checkpoint, 0, gates.Length);
            float lower = checkpoint == 0 ? 0 : gates[checkpoint - 1];
            float upper = checkpoint < gates.Length ? gates[checkpoint] : finish;
            return Mathf.Clamp(position, lower, Mathf.Max(lower, upper));
        }
        static string Seconds(float seconds) => Mathf.Max(0, seconds).ToString("F1") + "s";
        static string Identity(SportsEventSession session, int slot) => ((ISportsParticipantSlot)session.setup.players[slot]).ParticipantId;
        static string Bearing(Vector3 wind)
        {
            if (wind.sqrMagnitude < .01f) return "CALM";
            string[] directions = { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };
            int index = Mathf.RoundToInt(Mathf.Atan2(wind.x, wind.z) * Mathf.Rad2Deg / 45);
            return directions[(index + 8) % 8];
        }
        public string Context
        {
            get
            {
                if (match is BaseJumpMatch b)
                    return "ROUND " + b.Round + (b.Round <= 3 ? " / 3" : " · EXTRA") + " · " + (b.Phase == BaseJumpPhase.Intro ? b.Status : b.Phase == BaseJumpPhase.Flight ? "AIR " + Seconds(b.AirTime) + " · WIND " + (b.weatherField.Flow.magnitude * 2.23694f).ToString("F0") + " MPH TO " + Bearing(b.weatherField.Flow) : "ROUND POINTS BANKED");
                if (match is MattressMatch m) return "RACE " + m.series.round + " · " + (m.Phase == MattressPhase.Countdown ? "START IN " + Mathf.CeilToInt(m.RaceCountdownSeconds) : Seconds(m.RaceTime));
                if (match is WifeMatch w) return "RACE " + w.series.round + " · " + (w.Phase == WifePhase.Countdown ? "START IN " + Mathf.CeilToInt(w.RaceCountdownSeconds) : Seconds(w.Clock));
                if (match is BogMatch bog) return "RACE " + bog.series.round + " · " + (bog.Phase == BogPhase.Countdown ? "START IN " + Mathf.CeilToInt(bog.RaceCountdownSeconds) : Seconds(bog.Clock));
                if (match is TerrierMatch t) return "HEAT " + t.Heat + " · " + (t.Phase == TerrierPhase.Countdown ? "START IN " + Mathf.CeilToInt(t.RaceCountdownSeconds) : t.HeatWinner >= 0 ? t.setup.players[t.HeatWinner].name + " WINS · " + t.FinishWindowRemaining.ToString("F1") + "s" : Seconds(t.HeatTime));
                if (match is AvalancheMatch a) return a.rules.Phase == AvalanchePhase.Countdown ? "START IN " + Mathf.CeilToInt(a.rules.Countdown) : Seconds(a.rules.Clock) + " · SNOW FRONT " + a.front.CurrentZ.ToString("F0") + "m";
                if (match is RedLightMatch r) return r.rules.Phase == RedLightPhase.Countdown ? "START IN " + Mathf.Max(1, Mathf.CeilToInt(3 - r.rules.PhaseTime)) : r.rules.Phase == RedLightPhase.LaughWarning ? "THE LAUGH · RED IN ONE SECOND · BRAKE!" : r.rules.Phase == RedLightPhase.Red ? "RED LIGHT · STOP" : r.rules.Phase == RedLightPhase.Complete ? "RACE COMPLETE" : "GREEN LIGHT";
                return "";
            }
        }
        public Color ContextColor => match is RedLightMatch r && (r.rules.Phase == RedLightPhase.Red || r.rules.Phase == RedLightPhase.LaughWarning) ? new Color(1, .36f, .25f) : SportsBroadcastCanvas.Cream;

        public int Read(SportsCarnivalHudRow[] rows)
        {
            int count = Count;
            if (rows == null || rows.Length < count) throw new ArgumentException("A saved row is required for every competitor.", nameof(rows));
            for (int slot = 0; slot < count; slot++) rows[slot] = ReadRow(slot);
            // Existing placement APIs remain authoritative where provided. Others use accepted route data.
            if (!(match is BaseJumpMatch) && !(match is MattressMatch) && !(match is WifeMatch) && !(match is AvalancheMatch))
                for (int slot = 0; slot < count; slot++)
                {
                    var row = rows[slot]; row.place = row.group >= 2 ? 0 : 1;
                    if (row.place > 0) for (int other = 0; other < count; other++) if (other != slot && Ahead(rows[other], row)) row.place++;
                    rows[slot] = row;
                }
            Array.Sort(rows, 0, count, Comparer.Instance);
            return count;
        }
        static bool Ahead(SportsCarnivalHudRow a, SportsCarnivalHudRow b)
        {
            if (a.group != b.group) return a.group < b.group;
            return a.group == 0 ? a.seconds < b.seconds - .0001f : a.distance > b.distance + .01f;
        }
        public static int Compare(SportsCarnivalHudRow a, SportsCarnivalHudRow b)
        {
            int order = a.group.CompareTo(b.group);
            if (order == 0 && a.place > 0 && b.place > 0) order = a.place.CompareTo(b.place);
            if (order == 0) order = string.CompareOrdinal(a.id, b.id);
            return order;
        }
        sealed class Comparer : System.Collections.Generic.IComparer<SportsCarnivalHudRow>
        {
            public static readonly Comparer Instance = new Comparer();
            public int Compare(SportsCarnivalHudRow a, SportsCarnivalHudRow b) => SportsCarnivalHudSource.Compare(a, b);
        }
        SportsCarnivalHudRow ReadRow(int slot)
        {
            var player = match.setup.players[slot];
            var row = new SportsCarnivalHudRow { id = Identity(match, slot), name = player.name, participant = slot, color = player.color, group = 1, seconds = 0, progress = -1 };
            if (match is BaseJumpMatch b)
            {
                var j = b.jumpers[slot]; row.group = 0; row.place = b.ResultPlace(slot); row.value = b.totals[slot] + " PTS";
                var offset = j.truck.body.position - b.target.position;
                row.detail = j.Landed ? j.Outcome : !j.Released ? "STAGED" : Mathf.Max(0, j.truck.body.position.y * 3.28084f).ToString("F0") + "ft · TARGET " + new Vector2(offset.x, offset.z).magnitude.ToString("F0") + "m · " + (j.truck.body.position.y < 55 && !j.Deployed ? "LOW! DEPLOY" : j.Outcome);
                return row;
            }
            if (match is MattressMatch m)
            {
                var r = m.racers[slot]; bool finished = r.Phase == MattressAttempt.Finished, outOfRace = r.Phase == MattressAttempt.DNF || r.Phase == MattressAttempt.Inactive;
                row.group = finished ? 0 : outOfRace ? 2 : 1; row.place = outOfRace ? 0 : m.RunningPlace(slot); row.seconds = finished ? r.FinishTime : m.RaceTime;
                row.distance = GatedDistance(r.Checkpoint, r.truck.body.position.z, m.course.checkpoints, MattressCourse.Finish); row.progress = row.distance / MattressCourse.Finish;
                row.value = Seconds(row.seconds); row.detail = m.series.total[slot] + " PTS · " + (outOfRace ? "OUT" : finished ? "FIN" : r.Phase == MattressAttempt.Recovery ? "REC " + Seconds(r.RecoveryRemaining) : r.NeedsReset ? "RESET " + m.ResetControlCaption(slot) : r.Checkpoint + "/8 FLAGS");
            }
            else if (match is WifeMatch w)
            {
                var r = w.pairs[slot]; bool finished = r.State == WifeState.Finished, outOfRace = r.State == WifeState.DNF || r.State == WifeState.Inactive;
                row.id = w.PairScoreId(slot); row.name = w.teams[slot]; row.participant = w.DriverRole(slot); row.color = w.ScoreCardColor(slot);
                row.group = finished ? 0 : outOfRace ? 2 : 1; row.place = outOfRace ? 0 : w.RunningPlace(slot); row.seconds = finished ? r.FinishTime : w.Clock;
                row.distance = GatedDistance(r.Checkpoint, r.Progress, w.course.checkpoints, WifeCourse.Finish); row.progress = row.distance / WifeCourse.Finish;
                row.value = Seconds(row.seconds); row.detail = w.series.total[slot] + " PTS · " + (outOfRace ? "OUT" : finished ? "FIN" : r.State == WifeState.Dropped ? "PARTNER DOWN · STOP WITHIN 16ft" : r.State == WifeState.Pickup ? "LIFTING" : r.State == WifeState.Penalty ? "REC " + Seconds(r.PenaltyRemaining) : r.Checkpoint + "/5 FLAGS");
            }
            else if (match is BogMatch bog)
            {
                var r = bog.racers[slot]; bool finished = r.State == BogState.Finished, outOfRace = r.State == BogState.DNF || r.State == BogState.Inactive;
                row.group = finished ? 0 : outOfRace ? 2 : 1; row.seconds = finished ? r.FinishTime : bog.Clock; row.distance = GatedDistance(r.Checkpoint, r.truck.body.position.z, bog.course.checkpoints, bog.course.FinishDistance); row.progress = row.distance / bog.course.FinishDistance;
                row.value = Seconds(row.seconds); row.detail = bog.series.total[slot] + " PTS · " + (outOfRace ? "OUT" : finished ? "FIN" : r.State == BogState.Stalled ? "AIR EMPTY" : r.State == BogState.Penalty ? "REC " + Seconds(r.PenaltyRemaining) : (r.Air < 4 ? "LOW AIR " : "AIR ") + r.Air.ToString("F1") + "s");
            }
            else if (match is TerrierMatch t)
            {
                var r = t.racers[slot]; float finish = t.ConfirmedHeatFinishSeconds(slot); bool finished = float.IsFinite(finish);
                row.group = finished ? 0 : !r.Alive ? 2 : 1; row.seconds = finished ? finish : t.HeatTime; row.distance = Mathf.Clamp(t.course.TruckBounds(r.truck.shape).min.z, 0, t.course.Length); row.progress = row.distance / t.course.Length;
                row.value = Seconds(row.seconds); row.detail = t.wins[slot] + " / 5 WINS · " + (finished ? "FIN" : r.Alive ? "RACING" : "OUT");
            }
            else if (match is AvalancheMatch a)
            {
                bool finished = a.rules.finished[slot]; row.group = finished ? 0 : 1; row.place = a.rules.Place(slot); row.seconds = finished ? a.rules.finishTime[slot] : a.rules.Clock; row.distance = a.rules.distance[slot]; row.progress = row.distance / a.world.length;
                row.value = Seconds(row.seconds); row.detail = row.distance.ToString("F0") + "m · " + (finished ? "FIN" : !a.rules.alive[slot] ? "OUT" : a.rules.caughtFor[slot] > 0 ? "ESC " + Seconds(a.burialGrace - a.rules.caughtFor[slot]) : "RACING");
            }
            else if (match is RedLightMatch red)
            {
                bool finished = red.rules.outcomes[slot] == RedLightOutcome.Finished, eliminated = red.rules.outcomes[slot] == RedLightOutcome.Eliminated;
                row.group = finished ? 0 : eliminated ? 2 : 1; row.seconds = finished ? red.rules.finishTimes[slot] : Mathf.Max(0, red.rules.Elapsed - 3); row.distance = Mathf.Clamp(red.trucks[slot].shape.bounds.min.z, 0, red.finishZ); row.progress = row.distance / red.finishZ;
                row.value = Seconds(row.seconds); row.detail = finished ? red.ResultPoints(slot) + " PTS · FIN" : eliminated ? "OUT" : (red.finishZ - row.distance).ToString("F0") + "m TO FINISH";
            }
            return row;
        }
    }
}

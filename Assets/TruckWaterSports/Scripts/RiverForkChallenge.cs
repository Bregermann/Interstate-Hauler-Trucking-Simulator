using UnityEngine;
using LWS.TruckSports;

namespace LWS.TruckWaterSports
{
    // An optional, authored technical-course sector. Physical hull crossings,
    // native contacts and the existing current/buoyancy determine its outcome.
    public sealed class RiverForkChallenge : MonoBehaviour
    {
        public WaterMatch match;
        public Transform equipment;
        public Vector3 center, forward = Vector3.forward, right = Vector3.right;
        public float laneOffset = 8.5f, shortcutHalfWidth = 2.7f, safeHalfWidth = 4;
        public Rigidbody[] logs = System.Array.Empty<Rigidbody>();
        readonly bool[] crossed = new bool[4], missed = new bool[4];
        readonly Vector3[] previous = new Vector3[4];
        Vector3[] logPositions;
        Quaternion[] logRotations;
        string matchId;

        void Awake()
        {
            logPositions = new Vector3[logs.Length]; logRotations = new Quaternion[logs.Length];
            for (int i = 0; i < logs.Length; i++) { logPositions[i] = logs[i].position; logRotations[i] = logs[i].rotation; }
        }

        bool EnabledForMode => match != null && match.option == 1;
        int Index(WaterVessel vessel) => System.Array.IndexOf(match.vessels, vessel);
        void EnsureMatch()
        {
            if (matchId == match.MatchId) return;
            matchId = match.MatchId;
            System.Array.Clear(crossed, 0, crossed.Length); System.Array.Clear(missed, 0, missed.Length);
            for (int i = 0; i < match.vessels.Length; i++) previous[i] = match.vessels[i].body.position;
            for (int i = 0; i < logs.Length; i++)
            {
                logs[i].isKinematic = false;
                logs[i].position = logPositions[i]; logs[i].rotation = logRotations[i];
                logs[i].linearVelocity = logs[i].angularVelocity = Vector3.zero;
            }
        }

        public Vector3 Aim(WaterVessel vessel, Vector3 ordinaryTarget, int slot)
        {
            if (!EnabledForMode) return ordinaryTarget;
            EnsureMatch();
            int index = Index(vessel); if (index < 0) return ordinaryTarget;
            float along = Vector3.Dot(vessel.body.position - center, forward);
            if (along < -32 || along > 19) return ordinaryTarget;
            if (crossed[index]) return center + forward * 19;
            // Different real lanes, not predetermined scores. Both players in a
            // canoe receive the same waypoint because its vessel index is shared.
            int captain = Mathf.Clamp(vessel.firstSlot, 0, match.setup.count - 1);
            int difficulty = (int)match.setup.players[captain].difficulty;
            bool shortcut = (index + difficulty) % 2 == 0;
            return center + right * (shortcut ? -laneOffset : laneOffset);
        }

        public float PenaltySeconds(WaterVessel vessel)
        {
            int index = Index(vessel);
            return EnabledForMode && index >= 0 && missed[index] ? 3 : 0;
        }

        void FixedUpdate()
        {
            if (equipment == null || match == null) return;
            if (equipment.gameObject.activeSelf != EnabledForMode) equipment.gameObject.SetActive(EnabledForMode);
            if (!EnabledForMode || !match.Live)
            {
                foreach (var log in logs) if (!log.isKinematic) log.isKinematic = true;
                return;
            }
            EnsureMatch();
            foreach (var log in logs)
            {
                log.isKinematic = false;
                var floating = log.GetComponent<SportsFloatingBody>();
                floating.Clock = match.Clock; floating.waterHeight = match.course.SurfacePoint(log.position).y;
                var course = match.course;
                Vector3 waterPoint = course.SurfacePoint(log.position);
                bool supported = Vector3.ProjectOnPlane(log.position - waterPoint, Vector3.up).sqrMagnitude <= course.width * course.width * .25f;
                supported &= Vector3.Dot(log.position - course.Point(0), Vector3.ProjectOnPlane(course.Point(1) - course.Point(0), Vector3.up)) >= 0;
                int last = course.stations.Length - 1;
                supported &= Vector3.Dot(log.position - course.Point(last), Vector3.ProjectOnPlane(course.Point(last) - course.Point(last - 1), Vector3.up)) <= 0;
                // Beyond actual river water, retain gravity and real hull/land
                // contacts; do not supply an invisible infinite floating plane.
                floating.enabled = supported;
                if (supported && log.position.y < floating.waterHeight + .7f)
                    log.AddForce((match.course.Current(log.position, match.Clock) - log.linearVelocity) * .3f, ForceMode.Acceleration);
            }
            for (int i = 0; i < match.vessels.Length; i++)
            {
                var vessel = match.vessels[i]; if (!vessel.gameObject.activeSelf) continue;
                Vector3 position = vessel.body.position;
                float before = Vector3.Dot(previous[i] - center, forward), after = Vector3.Dot(position - center, forward);
                if (!crossed[i] && before < 0 && after >= 0)
                {
                    Vector3 passage = Vector3.Lerp(previous[i], position, -before / (after - before));
                    float lateral = Vector3.Dot(passage - center, right);
                    bool shortcut = lateral < 0;
                    float error = Mathf.Abs(lateral - (shortcut ? -laneOffset : laneOffset));
                    missed[i] = error > (shortcut ? shortcutHalfWidth : safeHalfWidth);
                    crossed[i] = true;
                    int slot = vessel.firstSlot;
                    SportsSessionTelemetry.GameplayEvent(gameObject.scene.name, missed[i] ? "river-fork-gate-missed" : "river-fork-gate-passed",
                        match.setup.players[slot].participantId, (shortcut ? "narrow log shortcut" : "wide shallow channel") + "; physical crossing; penalty_seconds=" + (missed[i] ? 3 : 0), error);
                }
                previous[i] = position;
            }
        }
    }
}

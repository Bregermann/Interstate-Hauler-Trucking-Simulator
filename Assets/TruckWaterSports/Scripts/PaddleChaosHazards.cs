using System;
using UnityEngine;
using LWS.TruckSports;

namespace LWS.TruckWaterSports
{
    // Optional Canoe Chaos equipment. PhysX owns contacts/tethers and the
    // existing Floating Entities adapter owns buoyancy; this supplies only
    // the same authored river-current response already used by fork driftwood.
    public sealed class PaddleChaosHazards : MonoBehaviour
    {
        public PaddlePalsMatch match;
        public Transform equipment;
        public Rigidbody[] logs = Array.Empty<Rigidbody>();
        public Transform[] moorings = Array.Empty<Transform>();
        public LineRenderer[] ropes = Array.Empty<LineRenderer>();
        public int ActualCanoeContacts { get; private set; }
        public bool ActiveForMode => match != null && match.option == 2;

        SportsFloatingBody[] flotation;
        Vector3[] starts;
        Quaternion[] rotations;
        readonly float[] nextImpact = new float[2];
        string matchId;

        void Awake()
        {
            starts = new Vector3[logs.Length]; rotations = new Quaternion[logs.Length];
            flotation = new SportsFloatingBody[logs.Length];
            for (int i = 0; i < logs.Length; i++)
            {
                starts[i] = logs[i].transform.position; rotations[i] = logs[i].transform.rotation;
                flotation[i] = logs[i].GetComponent<SportsFloatingBody>();
            }
            if (match != null) foreach (var vessel in match.vessels) if (vessel != null) vessel.HullImpact += ObserveImpact;
        }

        void OnDestroy()
        {
            if (match != null) foreach (var vessel in match.vessels) if (vessel != null) vessel.HullImpact -= ObserveImpact;
        }

        void BeginRound()
        {
            if (matchId == match.MatchId) return;
            matchId = match.MatchId; ActualCanoeContacts = 0; Array.Clear(nextImpact, 0, nextImpact.Length);
            for (int i = 0; i < logs.Length; i++)
            {
                logs[i].isKinematic = false;
                // Ordinary rematch initialization from saved equipment poses.
                // No log is repositioned while a live round is in progress.
                logs[i].position = starts[i]; logs[i].rotation = rotations[i];
                logs[i].linearVelocity = logs[i].angularVelocity = Vector3.zero;
                flotation[i].enabled = true;
            }
        }

        void FixedUpdate()
        {
            if (equipment == null || match == null) return;
            if (equipment.gameObject.activeSelf != ActiveForMode) equipment.gameObject.SetActive(ActiveForMode);
            if (!ActiveForMode || !match.Live)
            {
                foreach (var log in logs) if (!log.isKinematic) log.isKinematic = true;
                return;
            }
            BeginRound();
            for (int i = 0; i < logs.Length; i++)
            {
                var body = logs[i]; var floating = flotation[i]; var course = match.course;
                body.isKinematic = false;
                Vector3 surface = course.SurfacePoint(body.position);
                bool supported = Vector3.ProjectOnPlane(body.position - surface, Vector3.up).sqrMagnitude <= course.width * course.width * .25f;
                supported &= Vector3.Dot(body.position - course.Point(0), Vector3.ProjectOnPlane(course.Point(1) - course.Point(0), Vector3.up)) >= 0;
                int last = course.stations.Length - 1;
                supported &= Vector3.Dot(body.position - course.Point(last), Vector3.ProjectOnPlane(course.Point(last) - course.Point(last - 1), Vector3.up)) <= 0;
                floating.Clock = match.Clock; floating.waterHeight = surface.y; floating.enabled = supported;
                if (supported && body.position.y < surface.y + .7f)
                    body.AddForce((course.Current(body.position, match.Clock) - body.linearVelocity) * .3f, ForceMode.Acceleration);
            }
        }

        void LateUpdate()
        {
            for (int i = 0; i < Mathf.Min(logs.Length, ropes.Length); i++)
                if (ropes[i] != null && moorings[i] != null)
                {
                    ropes[i].SetPosition(0, moorings[i].position);
                    ropes[i].SetPosition(1, logs[i].transform.position);
                }
        }

        void ObserveImpact(WaterVessel vessel, Collision collision)
        {
            if (!ActiveForMode || !match.Live || collision.rigidbody == null || collision.relativeVelocity.magnitude < 1) return;
            int log = Array.IndexOf(logs, collision.rigidbody), canoe = Array.IndexOf(match.vessels, vessel);
            if (log < 0 || canoe < 0 || canoe >= nextImpact.Length || match.Clock < nextImpact[canoe]) return;
            nextImpact[canoe] = match.Clock + .8f; ActualCanoeContacts++;
            SportsSessionTelemetry.GameplayEvent(gameObject.scene.name, "canoe-chaos-timber-contact",
                match.setup.players[vessel.firstSlot].participantId, "native hull contact; moored log=" + log, collision.relativeVelocity.magnitude);
        }
    }
}

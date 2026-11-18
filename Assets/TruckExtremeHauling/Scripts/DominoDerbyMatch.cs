using UnityEngine;
using LWS.TruckHitchSports;

namespace LWS.TruckExtremeHauling
{
    public sealed class DominoDerbyMatch : ExtremeHaulingMatch
    {
        public int piecesPerPlayer = 12;
        public DominoCollapseReplay collapseReplay;
        bool replayStarted;
        public override bool ReplayControls => collapseReplay != null && collapseReplay.Playing;
        public override string Header => ReplayControls ? Title + " · RECORDED COLLAPSE HIGHLIGHTS" : base.Header;
        public override string Status => ReplayControls ? "ACTUAL CONTACTS · LANES ALIGNED TO FIRST PUSH · " + collapseReplay.Speed.ToString("0.##") + "×" : base.Status;
        public override void ExtraAction(string key) { if (!ReplayControls) return; collapseReplay.Action(key); ConsumeActions(); }
        [Min(5)] public float setupSeconds = 35;
        public GameObject creativeWorkshop;
        public Vector2 creativeLaneExtent = new Vector2(24, 18);
        readonly bool[] triggered = new bool[4];
        readonly int[] selected = new int[4], fallen = new int[4], longest = new int[4];
        readonly float[] triggerTime = new float[4], finishTime = new float[4];
        bool[] counted, activated;
        int[] depth, parent, children;
        Bounds[] envelopes;
        Vector3[] layoutStart;
        Quaternion[] layoutRotation;
        readonly Collider[] placementHits = new Collider[96];
        readonly RaycastHit[] floorHits = new RaycastHit[64];
        public override bool Cooperative => option == 3;
        protected override Vector3 CameraFocus(int slot) => trailers[slot * piecesPerPlayer + Mathf.Clamp(selected[slot], 0, piecesPerPlayer - 1)].Body.worldCenterOfMass;
        protected override void ReportFinalMeasurements(int i) { RecordMetric(i,"domino_contact_topples",fallen[i]);RecordMetric(i,"domino_longest_connected_chain",longest[i]);RecordMetric(i,"domino_triggered",triggered[i]?1:0);if(finishTime[i]>0)RecordMetric(i,"domino_complete_collapse_seconds",finishTime[i]); }
        public override string Title => "DOMINO DERBY";
        protected override string[] Modes => new[] { "LONGEST CHAIN", "PRECISION DOMINO COURSE", "FASTEST COMPLETE COLLAPSE", "CREATIVE CHAIN CHALLENGE" };
        protected override string ActionHint => age < setupSeconds ? "TAP NEUTRAL: NEXT · HOLD + STEER: ROTATE" : "TOPPLE FIRST TRAILER ONCE";
        public override string Help => "Arrange real upright trailers during setup. Steering slides the selected piece sideways, throttle changes its spacing; tap ACTION with centered controls to select the next piece, or hold ACTION and steer to rotate it. Invalid overlapping placements are refused. After the horn, ACTION gives only the first trailer one physical push; collisions must carry the chain. Longest Chain ranks genuine connected chain depth. Precision rewards propagated topples close to the authored course line. Fastest Complete Collapse scores only a complete chain and ranks actual collapse time. Creative Chain opens the whole construction lane. Lay out forks and place pieces on gently sloped rehearsal ramps. Real branches and direction changes add team craft points without individual winner points. A trailer that falls without contact from the triggered chain does not score.";
        public override bool TryReadScoreCard(int slot, out string value, out string detail)
        { if (slot < 0 || slot >= setup.count) { value = detail = null; return false; } value = option == 2 && finishTime[slot] > 0 ? finishTime[slot].ToString("F2") + "s" : scores[slot].ToString("F0"); detail = fallen[slot] + "/" + piecesPerPlayer + " TOPPLED · DEPTH " + longest[slot]; return true; }
        protected override void BeginMechanic()
        {
            if (creativeWorkshop != null) creativeWorkshop.SetActive(option == 3);
            collapseReplay?.ResetCapture(setup.count); replayStarted=false;
            counted = new bool[trailers.Length]; activated = new bool[trailers.Length]; depth = new int[trailers.Length]; parent = new int[trailers.Length]; children = new int[trailers.Length];
            envelopes = new Bounds[trailers.Length]; layoutStart = new Vector3[trailers.Length]; layoutRotation = new Quaternion[trailers.Length];
            System.Array.Clear(triggered, 0, 4); System.Array.Clear(selected, 0, 4); System.Array.Clear(fallen, 0, 4); System.Array.Clear(longest, 0, 4); System.Array.Clear(triggerTime, 0, 4); System.Array.Clear(finishTime, 0, 4);
            for (int index = 0; index < trailers.Length; index++)
            {
                var trailer = trailers[index]; trailer.Vehicle.enabled = false;
                foreach (var body in trailer.GetComponentsInChildren<Rigidbody>(true)) body.isKinematic = true;
                parent[index] = -1; envelopes[index] = LocalEnvelope(trailer);
                // Ignore the upright trailer itself: the default layer also contains
                // native NWH colliders. Only actual static venue support is floor.
                float floor = 0;
                int hits = Physics.RaycastNonAlloc(trailer.Body.position + Vector3.up * 60, Vector3.down, floorHits, 150, ~0, QueryTriggerInteraction.Ignore);
                for (int h = 0; h < hits; h++) if (floorHits[h].rigidbody == null) floor = Mathf.Max(floor, floorHits[h].point.y);
                float low = WorldBottom(envelopes[index], trailer.Body.position, trailer.Body.rotation);
                Vector3 position = trailer.Body.position + Vector3.up * Mathf.Max(0, floor + .05f - low);
                trailer.Body.position = position; trailer.transform.SetPositionAndRotation(position, trailer.Body.rotation);
                layoutStart[index] = position; layoutRotation[index] = trailer.Body.rotation;
                var contact = trailer.GetComponent<DominoChainContact>(); if (contact == null) contact = trailer.gameObject.AddComponent<DominoChainContact>(); contact.match = this; contact.index = index;
            }
            Physics.SyncTransforms();
            for (int i = 0; i < setup.count; i++) { trucks[i].Power(false); foreach (var body in trucks[i].GetComponentsInChildren<Rigidbody>(true)) body.isKinematic = true; }
        }
        protected override Vector2 AICommand(int i, float dt) => Vector2.zero;
        protected override void StepMechanic(int i, Vector2 drive, bool ai, float dt)
        {
            int first = i * piecesPerPlayer;
            if (age < setupSeconds)
            {
                if (ai) selected[i] = Mathf.Min(piecesPerPlayer - 1, Mathf.FloorToInt(age / Mathf.Max(.1f, setupSeconds / piecesPerPlayer)));
                else if (actions[i] && drive.sqrMagnitude < .04f) selected[i] = (selected[i] + 1) % piecesPerPlayer;
                int index = first + selected[i]; var body = trailers[index].Body;
                Vector3 proposed = body.position; Quaternion rotation = body.rotation;
                if (ai)
                {
                    int difficulty = (int)setup.players[i].difficulty;
                    float error = Mathf.Sin(index * 2.13f) * (2 - difficulty) * .12f;
                    Vector3 desired = layoutStart[index] + Vector3.right * error;
                    if (option == 3) desired.x += Mathf.Sin(selected[i] * .6f) * 2;
                    proposed = Vector3.MoveTowards(body.position, desired, dt * 3);
                    float angle = option == 3 ? Mathf.Sin(selected[i] * .6f) * 12 : 0;
                    rotation = Quaternion.RotateTowards(body.rotation, Quaternion.AngleAxis(angle, Vector3.up) * layoutRotation[index], dt * 35);
                }
                else if (ActionHeld(i)) rotation = Quaternion.AngleAxis(drive.x * dt * 45, Vector3.up) * body.rotation;
                else proposed += new Vector3(drive.x, 0, drive.y) * (3 * dt);
                if (option == 3)
                {
                    float laneX = layoutStart[first].x;
                    proposed.x = Mathf.Clamp(proposed.x, laneX - creativeLaneExtent.x, laneX + creativeLaneExtent.x);
                    proposed.z = Mathf.Clamp(proposed.z, layoutStart[first].z - 6,
                        layoutStart[first + piecesPerPlayer - 1].z + creativeLaneExtent.y);
                    if (!TrySeatOnWorkshopSurface(index, ref proposed, ref rotation)) return;
                }
                else
                {
                    proposed.x = Mathf.Clamp(proposed.x, layoutStart[index].x - 6, layoutStart[index].x + 6);
                    proposed.z = Mathf.Clamp(proposed.z, layoutStart[index].z - 2, layoutStart[index].z + 2);
                }
                if (PlacementClear(index, proposed, rotation)) { body.MovePosition(proposed); body.MoveRotation(rotation); }
                return;
            }
            for (int j = 0; j < piecesPerPlayer; j++) trailers[first + j].Body.isKinematic = false;
            if (!triggered[i] && (actions[i] || ai && age > setupSeconds + 1 + i * .2f))
            {
                var body = trailers[first].Body;
                activated[first] = true; depth[first] = 1; triggerTime[i] = age;
                body.AddForceAtPosition(-body.transform.up * body.mass * 5, body.worldCenterOfMass + Vector3.up * 5, ForceMode.Impulse);
                triggered[i] = true;
            }
            for (int j = 0; j < piecesPerPlayer; j++)
            {
                int index = first + j;
                if (!triggered[i] || !activated[index] || counted[index] || Vector3.Dot(trailers[index].transform.forward, Vector3.up) >= .45f) continue;
                counted[index] = true; fallen[i]++;
                int oldDepth = longest[i]; longest[i] = Mathf.Max(longest[i], depth[index]);
                if (option == 0 && longest[i] > Mathf.Max(1, oldDepth)) Award(i, (longest[i] - Mathf.Max(1, oldDepth)) * 100, "physical_connected_chain_depth");
                else if (option == 1 && depth[index] > 1)
                { float deviation = Mathf.Abs(trailers[index].Body.position.x - layoutStart[index].x); Award(i, Mathf.Max(1, 150 - deviation * 30), "precision_chain_topple"); }
                else if (option == 3 && depth[index] > 1) Award(i, 100, "creative_connected_topple");
            }
            if (fallen[i] >= piecesPerPlayer)
            {
                finishTime[i] = age - triggerTime[i];
                if (option == 2) Award(i, 1 + Mathf.Max(0, matchSeconds - finishTime[i]) * 1000, "complete_collapse_seconds");
                retired[i] = true;
            }
        }
        protected override void Simulate(float dt)
        {
            if(ReplayControls)
            {
                if(collapseReplay.Step(dt))
                {collapseReplay.End();views?.Restore();EndMatch();}
                return;
            }
            base.Simulate(dt);
            if(!ReplayControls && !Finished && collapseReplay!=null)
                for(int lane=0;lane<setup.count;lane++)collapseReplay.RecordLane(lane,age,triggered[lane]);
        }
        protected override bool DelayMatchCompletion()
        {
            if(replayStarted || collapseReplay==null)return false;
            replayStarted=true;
            for(int lane=0;lane<setup.count;lane++)collapseReplay.RecordLane(lane,age,triggered[lane]);
            // Recorded ghosts cannot interact with the stopped original simulation.
            foreach(var truck in trucks){truck.Power(false);foreach(var body in truck.GetComponentsInChildren<Rigidbody>(true))body.isKinematic=true;}
            foreach(var trailer in trailers)foreach(var body in trailer.GetComponentsInChildren<Rigidbody>(true))body.isKinematic=true;
            if(!collapseReplay.BeginPlayback())return false;
            StopSounds();Gate();return true;
        }
        protected override void Present(float dt)
        {if(ReplayControls){collapseReplay.Present(views,broadcast);return;}base.Present(dt);}
        protected override void StopGame()
        {collapseReplay?.End();if(views!=null && views.Replay)views.Restore();base.StopGame();}
        public void ChainContact(int a, int b, float speed)
        {
            if (ReplayControls || !Live || age < setupSeconds || speed < .15f || a < 0 || b < 0 || a >= activated.Length || b >= activated.Length || a / piecesPerPlayer != b / piecesPerPlayer) return;
            if (!activated[a] && activated[b]) { int swap = a; a = b; b = swap; }
            if (!activated[a] || activated[b]) return;
            activated[b] = true; parent[b] = a; depth[b] = depth[a] + 1; children[a]++;
            int slot = a / piecesPerPlayer;
            if (option != 3) return;
            if (children[a] == 2) Award(slot, 200, "physical_chain_branch");
            if (parent[a] >= 0)
            {
                Vector3 incoming = Vector3.ProjectOnPlane(trailers[a].Body.position - trailers[parent[a]].Body.position, Vector3.up);
                Vector3 outgoing = Vector3.ProjectOnPlane(trailers[b].Body.position - trailers[a].Body.position, Vector3.up);
                if (Vector3.Angle(incoming, outgoing) > 20) Award(slot, 75, "physical_chain_direction_change");
            }
        }
        bool TrySeatOnWorkshopSurface(int index, ref Vector3 position, ref Quaternion rotation)
        {
            // Use the footprint at the actual standing end of the trailer.
            // Four surface samples keep the piece stable at ramp edges. A gentle
            // incline is allowed. A ledge, wall or missing corner refuses it.
            var box = envelopes[index];
            Vector3 longAxis = layoutRotation[index] * Vector3.forward;
            bool onNose = Vector3.Dot(longAxis, Vector3.up) > 0;
            float baseZ = onNose ? box.min.z : box.max.z;
            Vector3 flatUp = Vector3.ProjectOnPlane(rotation * Vector3.up, Vector3.up).normalized;
            if (flatUp.sqrMagnitude < .9f) return false;
            Quaternion upright = Quaternion.LookRotation(onNose ? Vector3.up : Vector3.down, flatUp);
            Vector3 sole = new Vector3(box.center.x, box.center.y, baseZ);
            Vector3 center = position + rotation * sole;
            if (!ReadWorkshopFloor(center, out var floor)) return false;
            Quaternion seated = Quaternion.FromToRotation(Vector3.up, floor.normal) * upright;
            position = new Vector3(center.x, floor.point.y + .035f, center.z) - seated * sole;
            for (int corner = 0; corner < 4; corner++)
            {
                Vector3 local = new Vector3(
                    box.center.x + box.extents.x * ((corner & 1) == 0 ? -.7f : .7f),
                    box.center.y + box.extents.y * ((corner & 2) == 0 ? -.7f : .7f), baseZ);
                Vector3 point = position + seated * local;
                if (!ReadWorkshopFloor(point, out var sample) || Mathf.Abs(point.y - sample.point.y) > .13f)
                    return false;
            }
            rotation = seated;
            return true;
        }

        bool ReadWorkshopFloor(Vector3 point, out RaycastHit floor)
        {
            floor = default;
            float nearest = float.PositiveInfinity;
            int count = Physics.RaycastNonAlloc(point + Vector3.up * 2, Vector3.down, floorHits,
                4, ~0, QueryTriggerInteraction.Ignore);
            if (count == floorHits.Length) return false;
            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = floorHits[i];
                if (hit.rigidbody != null || hit.normal.y < .98f || hit.distance >= nearest) continue;
                nearest = hit.distance;
                floor = hit;
            }
            return floor.collider != null;
        }

        bool PlacementClear(int index, Vector3 position, Quaternion rotation)
        {
            Bounds box = envelopes[index]; int count = Physics.OverlapBoxNonAlloc(position + rotation * box.center, box.extents * .97f, placementHits, rotation, ~0, QueryTriggerInteraction.Ignore);
            if (count == placementHits.Length) return false;
            for (int i = 0; i < count; i++)
            {
                var hit = placementHits[i];
                var body = hit.attachedRigidbody;
                if (body != null && body != trailers[index].Body) return false;
                if (option == 3 && body == null && hit.bounds.max.y > position.y + .4f) return false;
            }
            return true;
        }
        static Bounds LocalEnvelope(TrailerBody trailer)
        {
            Bounds result = default; bool first = true;
            foreach (var renderer in trailer.GetComponentsInChildren<MeshRenderer>(true))
                for (int corner = 0; corner < 8; corner++)
                {
                    Vector3 p = renderer.bounds.center + Vector3.Scale(renderer.bounds.extents, new Vector3((corner & 1) == 0 ? -1 : 1, (corner & 2) == 0 ? -1 : 1, (corner & 4) == 0 ? -1 : 1));
                    p = trailer.transform.InverseTransformPoint(p); if (first) { result = new Bounds(p, Vector3.zero); first = false; } else result.Encapsulate(p);
                }
            return result;
        }
        static float WorldBottom(Bounds bounds, Vector3 position, Quaternion rotation)
        {
            float lowest = float.MaxValue;
            for (int corner = 0; corner < 8; corner++)
            { Vector3 p = position + rotation * (bounds.center + Vector3.Scale(bounds.extents, new Vector3((corner & 1) == 0 ? -1 : 1, (corner & 2) == 0 ? -1 : 1, (corner & 4) == 0 ? -1 : 1))); lowest = Mathf.Min(lowest, p.y); }
            return lowest;
        }
    }
    public sealed class DominoChainContact : MonoBehaviour
    {
        public DominoDerbyMatch match;
        public int index;
        void OnCollisionEnter(Collision collision) => Forward(collision);
        void OnCollisionStay(Collision collision) => Forward(collision);
        void Forward(Collision collision)
        { if (collision.rigidbody == null) return; var other = collision.rigidbody.GetComponent<DominoChainContact>(); if (other != null && other.match == match) match.ChainContact(index, other.index, collision.relativeVelocity.magnitude); }
    }
}

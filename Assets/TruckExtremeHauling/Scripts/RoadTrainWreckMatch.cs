using UnityEngine;
using LWS.TruckSports;
using LWS.TruckHitchSports;

namespace LWS.TruckExtremeHauling
{
    public sealed class RoadTrainWreckMatch : ExtremeHaulingMatch
    {
        public ConfigurableJoint[] additionalHitches;
        struct Link { public Rigidbody owner, next; public Vector3 anchor, connectedAnchor; public SoftJointLimit low, high, roll; public float force, torque; }
        Link[] links;
        readonly Vector3[] previous = new Vector3[4];
        readonly float[] safeDistance = new float[4], unbankedDistance = new float[4], survival = new float[4], unbankedSurvival = new float[4], lostHitch = new float[4], settled = new float[4];
        readonly int[] route = new int[4], connected = new int[4];
        readonly bool[] initialLock = new bool[4];
        protected override void Awake()
        {
            base.Awake();
            if (additionalHitches == null) additionalHitches = new ConfigurableJoint[0];
            links = new Link[additionalHitches.Length];
            for (int i = 0; i < links.Length; i++)
            {
                var joint = additionalHitches[i]; if (joint == null) continue;
                links[i] = new Link { owner = joint.GetComponent<Rigidbody>(), next = joint.connectedBody, anchor = joint.anchor, connectedAnchor = joint.connectedAnchor, low = joint.lowAngularXLimit, high = joint.highAngularXLimit, roll = joint.angularZLimit, force = joint.breakForce, torque = joint.breakTorque };
            }
        }
        protected override void ReportFinalMeasurements(int i) { RecordMetric(i,"road_train_safe_metres",safeDistance[i]);RecordMetric(i,"road_train_moving_survival_seconds",survival[i]);RecordMetric(i,"road_train_connected_trailers",CountConnected(i)); }
        public override string Title => "ROAD TRAIN WRECK";
        protected override string[] Modes => new[] { "LONGEST SAFE CONVOY", "ROAD TRAIN OBSTACLE RACE", "MAXIMUM TRAILER CHALLENGE", "ROAD TRAIN SURVIVAL" };
        protected override string ActionHint => "INDEPENDENT TRAILER BRAKES";
        public override string Help => "Drive a genuine three-trailer road train with native axle contact and physical articulated hitches. Hold ACTION to brake every trailer independently of the tractor. Longest Safe Convoy banks controlled travel with all three trailers attached. Obstacle Race rewards a stopped delivery at your marked depot, with speed and actual cargo condition deciding the result. Maximum Trailer Challenge prioritizes the number of trailers still physically connected when delivered, then cargo preservation and time; this venue supplies three per rig. Survival banks actual moving, supported survival time and ends when any link is lost. Detached trailers never count. A delivery requires the last connected trailer stopped at your depot and the whole connected train settled.";
        public override bool TryReadScoreCard(int slot, out string value, out string detail)
        {
            if (slot < 0 || slot >= setup.count) { value = detail = null; return false; }
            value = scores[slot].ToString("F0");
            detail = connected[slot] + "/3 LINKED · " + (option == 3 ? survival[slot].ToString("F1") + "s MOVING" : safeDistance[slot].ToString("F0") + "m SAFE") + (retired[slot] ? " · COMPLETE" : "");
            return true;
        }
        protected override Vector3 Destination(int i)
        {
            if (option == 0 || option == 3) return RoutePoint(i);
            int count = Mathf.Max(1, CountConnected(i));
            return objectives[i].position + trucks[i].body.position - trailers[i * 3 + count - 1].Body.worldCenterOfMass;
        }
        Vector3 RoutePoint(int i)
        {
            switch (route[i])
            {
                case 0: return new Vector3(starts[i].x, starts[i].y, 155);
                case 1: return new Vector3(300, starts[i].y, 230);
                case 2: return new Vector3(-90, starts[i].y, 230);
                case 3: return new Vector3(-90, starts[i].y, -100);
                default: return new Vector3(300, starts[i].y, -100);
            }
        }
        protected override void BeginMechanic()
        {
            for (int i = 0; i < links.Length; i++) if (additionalHitches[i] == null && links[i].owner != null && links[i].next != null)
            {
                var data = links[i]; var joint = data.owner.gameObject.AddComponent<ConfigurableJoint>();
                joint.connectedBody = data.next; joint.autoConfigureConnectedAnchor = false; joint.anchor = data.anchor; joint.connectedAnchor = data.connectedAnchor;
                joint.xMotion = joint.yMotion = joint.zMotion = ConfigurableJointMotion.Locked; joint.angularYMotion = ConfigurableJointMotion.Free; joint.angularXMotion = joint.angularZMotion = ConfigurableJointMotion.Limited;
                joint.lowAngularXLimit = data.low; joint.highAngularXLimit = data.high; joint.angularZLimit = data.roll; joint.breakForce = data.force; joint.breakTorque = data.torque;
                joint.enableCollision = false; joint.projectionMode = JointProjectionMode.PositionAndRotation; additionalHitches[i] = joint;
            }
            System.Array.Clear(safeDistance, 0, 4); System.Array.Clear(unbankedDistance, 0, 4); System.Array.Clear(survival, 0, 4); System.Array.Clear(unbankedSurvival, 0, 4); System.Array.Clear(lostHitch, 0, 4); System.Array.Clear(settled, 0, 4); System.Array.Clear(route, 0, 4); System.Array.Clear(connected, 0, 4); System.Array.Clear(initialLock, 0, 4);
            for (int i = 0; i < setup.count; i++) previous[i] = trucks[i].body.position;
        }
        int CountConnected(int slot)
        {
            int first = slot * 3;
            if (hitches[slot].Attached != trailers[first] || !Usable(trailers[first])) return 0;
            int count = 1;
            for (int j = 0; j < 2; j++)
            {
                int index = slot * 2 + j;
                if (index >= additionalHitches.Length) break;
                var joint = additionalHitches[index]; var before = trailers[first + j]; var after = trailers[first + j + 1];
                if (joint == null || joint.GetComponent<Rigidbody>() != before.Body || joint.connectedBody != after.Body || !Usable(after)) break;
                // A body that happens to be nearby is not part of the train. Both
                // actual joint identity and constraint separation must remain valid.
                if (Vector3.Distance(before.Body.transform.TransformPoint(joint.anchor), after.Body.transform.TransformPoint(joint.connectedAnchor)) > 1.5f) break;
                count++;
            }
            return count;
        }
        static bool Usable(TrailerBody trailer) => trailer.Body.position.y > -5 && trailer.CargoCondition > .1f;
        protected override Vector2 AICommand(int i, float dt)
        {
            Vector3 target = Destination(i);
            float distance = Vector3.ProjectOnPlane(target - trucks[i].body.position, Vector3.up).magnitude;
            if ((option == 0 || option == 3) && distance < 35)
            { route[i] = route[i] >= 4 ? 1 : route[i] + 1; target = Destination(i); distance = Vector3.ProjectOnPlane(target - trucks[i].body.position, Vector3.up).magnitude; }
            Vector3 local = trucks[i].transform.InverseTransformPoint(target);
            float angle = Mathf.Atan2(local.x, local.z);
            float speed = Vector3.Dot(trucks[i].body.linearVelocity, trucks[i].transform.forward);
            float articulation = 0;
            for (int j = 0; j < 3; j++) articulation = Mathf.Max(articulation, Vector3.Angle(j == 0 ? trucks[i].transform.forward : trailers[i * 3 + j - 1].transform.forward, trailers[i * 3 + j].transform.forward));
            float requested = 6 + (int)setup.players[i].difficulty * 1.5f;
            requested *= Mathf.Lerp(1, .35f, Mathf.Clamp01(Mathf.Abs(angle) / 1.2f));
            if (articulation > 55) requested = Mathf.Min(requested, 3);
            if (option == 1 || option == 2) requested = Mathf.Min(requested, Mathf.Max(0, distance - 7) * .35f);
            return new Vector2(Mathf.Clamp(angle * 1.1f, -1, 1), Mathf.Clamp((requested - speed) * .3f, -.8f, .85f));
        }
        protected override void StepMechanic(int i, Vector2 drive, bool ai, float dt)
        {
            connected[i] = CountConnected(i);
            if (connected[i] > 0) initialLock[i] = true;
            // Initial capture belongs to the base countdown. Losing a live hitch
            // cannot be repaired by automatically snapping/recreating it mid-run.
            if (connected[i] == 0)
            {
                lostHitch[i] += dt; previous[i] = trucks[i].body.position;
                if (initialLock[i] || lostHitch[i] > 2) FinishAttempt(i, "road_train_lost_native_hitch");
                return;
            }
            bool parking = ai && (option == 1 || option == 2) && Vector3.ProjectOnPlane(trailers[i * 3 + connected[i] - 1].Body.worldCenterOfMass - objectives[i].position, Vector3.up).magnitude < 12;
            if (parking)
            {
                trucks[i].TickDrive(Vector2.zero); var input = trucks[i].tractorMotor.vehicle.input;
                input.states.handbrake = input.states.handbrakeRaw = 1; input.states.brakes = input.states.brakesRaw = 1;
            }
            bool supported = true, stable = true, atRest = trucks[i].body.linearVelocity.magnitude < 1 && trucks[i].body.angularVelocity.magnitude < .4f;
            float condition = 0;
            for (int j = 0; j < 3; j++)
            {
                var trailer = trailers[i * 3 + j];
                trailer.SetBrakes(parking ? 1 : ActionHeld(i) ? .7f : 0);
                if (j >= connected[i]) continue;
                supported &= trailer.WheelContact;
                stable &= Vector3.Dot(trailer.transform.up, Vector3.up) > .65f && Vector3.Angle(j == 0 ? trucks[i].transform.forward : trailers[i * 3 + j - 1].transform.forward, trailer.transform.forward) < 75;
                atRest &= trailer.Body.linearVelocity.magnitude < 1 && trailer.Body.angularVelocity.magnitude < .4f;
                condition += trailer.CargoCondition;
            }
            condition /= connected[i];
            float distance = Vector3.ProjectOnPlane(trucks[i].body.position - previous[i], Vector3.up).magnitude;
            previous[i] = trucks[i].body.position;
            // Reject discontinuities rather than rewarding respawns or bad poses.
            bool moving = Vector3.ProjectOnPlane(trucks[i].body.linearVelocity, Vector3.up).magnitude > 2 && distance <= Mathf.Max(1, 35 * dt);
            if (supported && stable && moving)
            {
                safeDistance[i] += distance;
                if (option == 0 && connected[i] == 3)
                {
                    unbankedDistance[i] += distance;
                    if (unbankedDistance[i] >= 10) { Award(i, unbankedDistance[i] * 3 * condition, "connected_safe_metres"); unbankedDistance[i] = 0; }
                }
                if (option == 3 && connected[i] == 3)
                {
                    survival[i] += dt; unbankedSurvival[i] += dt;
                    if (unbankedSurvival[i] >= 2) { Award(i, unbankedSurvival[i] * 30 * condition, "moving_road_train_survival_seconds"); unbankedSurvival[i] = 0; }
                }
            }
            if ((option == 0 || option == 3) && (connected[i] < 3 || !stable)) { FinishAttempt(i, "road_train_chain_failed"); return; }
            if (option != 1 && option != 2) return;
            var tail = trailers[i * 3 + connected[i] - 1];
            bool atDepot = AtRestIn(i, tail.Body, objectives[i], 16) && atRest && supported && stable;
            settled[i] = atDepot ? settled[i] + dt : 0;
            if (settled[i] < 1.5f) return;
            // Trailer count dominates Maximum Challenge; fast empty tractors
            // and detached cargo can never beat a genuinely delivered train.
            float award = option == 2 ? connected[i] * 100000 + condition * 10000 + Mathf.Max(0, matchSeconds - age) : condition * 1000 + Mathf.Max(0, matchSeconds - age) * 10;
            Award(i, award, option == 2 ? "connected_trailers_delivered" : "road_train_timed_delivery");
            retired[i] = true;
        }
        void FinishAttempt(int i, string reason)
        {
            if (retired[i]) return;
            // Only completed ten-metre/two-second segments were banked. A tiny
            // initial roll or a failed startup is not a Game Night victory.
            retired[i] = true;
            SportsSessionTelemetry.GameplayEvent(gameObject.scene.name, reason, setup.players[i].participantId, "Actual connected trailer count", connected[i]);
        }
    }
}

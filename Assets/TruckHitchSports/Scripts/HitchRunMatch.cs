using UnityEngine;
using LWS.TruckBowling;
namespace LWS.TruckHitchSports
{
    public sealed class HitchRunMatch : HitchCompetitionSession
    {
        public Transform[] depots;
        public float deliveryRadius = 9;
        readonly float[] contest = new float[4], deliveryQuiet = new float[4];
        readonly int[] successfulSteals = new int[4];
        readonly float[] aiReadyAt = new float[4];
        readonly TrailerBody[] contested = new TrailerBody[4];
        readonly TrailerBody[] aiTarget = new TrailerBody[4];
        readonly int[] approachStage = new int[4], approachSide = new int[4];
        readonly float[] clearKingpinUntil = new float[4];
        readonly Vector3[] clearKingpinTarget = new Vector3[4];
        readonly int[] contestsWon = new int[4];
        readonly int[] ambushRival = new int[4], ambushStage = new int[4];
        readonly Vector3[] ambushGoal = new Vector3[4];
        float[] respawnAt;
        int[] contestedOwner, cargoStealer;
        public override int MinimumPlayers => option % 4 == 2 ? 4 : 1;
        public override string Title => "HITCH & RUN";
        public override string OptionLabel => new[] { "TRAILER THIEF", "DELIVERY RUSH", "TEAM SMUGGLING", "HIGH-VALUE CARGO HUNT" }[option % 4];
        protected override bool StartsCoupled => false;
        public override bool ResultEligible(int slot)
        {
            if (slot < 0 || slot >= setup.count || Scores[slot] <= 0) return false;
            if (option % 4 != 2) return Deliveries[slot] > 0;
            for (int i = 0; i < setup.count; i++) if (i % 2 == slot % 2 && Deliveries[i] > 0) return true;
            return false;
        }
        public override string Help => "Back the fifth wheel under a kingpin and hold ACTION to physically couple. Press ACTION to unhitch your own load. Deliver attached cargo to your marked depot and stop both real bodies for two uncontested seconds. To contest an occupied load, park within five meters of its kingpin and hold ACTION for three seconds while both rigs are slow; then physically recouple. Contact never transfers ownership. Trailer Thief pays a large bonus only when you actually steal and deliver another driver's cargo. Delivery Rush scores completed delivery count. Team Smuggling uses seats 1+3 against 2+4 with shared depots and shared cargo points; AI fills unused human seats. High-Value Cargo Hunt adds real mass and value to three load tiers, shown in your panel while hitched and reduced by damage. One Hard Trailer Thief AI can stake out a rival's first receiving run while other drivers haul; it still must win the physical stopped contest, recouple and deliver to its own depot. AI difficulty changes target aggression, reaction delay, approach pace, backing precision and delivery pace; every difficulty uses the same real coupling and delivery rules. Dispatched loads return at their original yard positions after a delay, only when the spawn is clear.";
        public override bool TryReadScoreCard(int slot, out string value, out string detail)
        {
            value = option % 4 == 1 ? Deliveries[slot] + " LOADS" : Scores[slot].ToString("F0");
            var trailer = couplers[slot].Attached;
            detail = trailer == null ? "ALIGN / HOLD ACTION TO HITCH" : "TIER " + (1 + System.Array.IndexOf(trailers, trailer) % 3) + " · " + (trailer.CargoCondition * 100).ToString("F0") + "% CARGO";
            return true;
        }
        protected override void BeginCompetition()
        {
            System.Array.Clear(contest, 0, 4); System.Array.Clear(deliveryQuiet, 0, 4); System.Array.Clear(successfulSteals, 0, 4); System.Array.Clear(contested, 0, 4);
            System.Array.Clear(aiTarget, 0, 4); System.Array.Clear(approachStage, 0, 4); System.Array.Clear(aiReadyAt, 0, 4);
            System.Array.Clear(clearKingpinUntil, 0, 4); System.Array.Clear(contestsWon, 0, 4);
            respawnAt = new float[trailers.Length]; contestedOwner = new int[trailers.Length]; cargoStealer = new int[trailers.Length];
            for (int slot = 0; slot < ambushRival.Length; slot++) { ambushRival[slot] = -1; ambushStage[slot] = 0; ambushGoal[slot] = Vector3.zero; }
            for (int t = 0; t < trailers.Length; t++) { contestedOwner[t] = cargoStealer[t] = -1; ConfigureCargo(t); }
        }
        protected override void ReportFinalMeasurements(int slot)
        {
            RecordMetric(slot, "hitch_run_steals", successfulSteals[slot]);
            RecordMetric(slot, "hitch_run_deliveries", Deliveries[slot]);
            RecordMetric(slot, "hitch_run_completed_stopped_contests", contestsWon[slot]);
        }
        protected override void TickCompetition(float dt)
        {
            UpdateDispatchedLoads();
            Notice = "ALIGN KINGPIN · HOLD ACTION TO COUPLE / CONTEST · STOP AT YOUR DEPOT";
            int bayAmbusher = -1;
            if (option % 4 == 0 && setup.count > 1)
                for (int slot = 0; slot < setup.count; slot++)
                    if (IsAI(slot) && setup.players[slot].difficulty == BowlingDifficulty.Hard)
                    {
                        bayAmbusher = slot;
                        break;
                    }
            for (int i = 0; i < setup.count; i++)
            {
                var owned = couplers[i].Attached; bool ai = IsAI(i);
                bool hardAmbush = i == bayAmbusher;
                bool recoveringContestedCargo = aiTarget[i] != null && aiTarget[i].Owner < 0 && contestedOwner[System.Array.IndexOf(trailers, aiTarget[i])] >= 0;
                hardAmbush &= !recoveringContestedCargo;
                if (hardAmbush) EnsureAmbushRival(i);
                // Close AI backing may choose R explicitly; every other path
                // retains the existing NWH automatic D/N/R behavior.
                trucks[i].tractorMotor.vehicle.powertrain.transmission.automaticTransmissionDNRShiftType = NWH.VehiclePhysics2.Powertrain.TransmissionComponent.AutomaticTransmissionDNRShiftType.Auto;
                if (owned == null)
                {
                    // A defeated AI must drive its cab out from over the real
                    // kingpin before either truck can physically collect it.
                    // Humans retain full control and can escape/recapture.
                    if (ai && Elapsed < clearKingpinUntil[i])
                    { trucks[i].TickDrive(DriveTo(i, clearKingpinTarget[i], 4)); continue; }
                    TrailerBody candidate = null; float nearest = float.MaxValue, priority = float.MaxValue;
                    foreach (var trailer in trailers)
                    {
                        if (!trailer.gameObject.activeSelf || trailer.Deliveries > 0 || trailer.Owner == i || option % 4 == 2 && trailer.Owner >= 0 && trailer.Owner % 2 == i % 2) continue;
                        if (ai && option % 4 == 2 && ReservedByCloserTeammate(i, trailer)) continue;
                        // Hard Trailer Thief deliberately trades routine loads
                        // for an early receiving-bay ambush. It waits
                        // for an actual courier, never claims imaginary cargo.
                        if (hardAmbush && (trailer.Owner < 0 || trailer.Owner != ambushRival[i])) continue;
                        float distance = (trailer.Kingpin.position - couplers[i].Anchor.position).sqrMagnitude;
                        int tier = 1 + System.Array.IndexOf(trailers, trailer) % 3;
                        float value = ai && option % 4 == 3 ? tier * tier : ai && option % 4 == 0 && trailer.Owner >= 0 ? 2.5f : 1;
                        // Difficulty changes real target decisions and driving, never awarded points.
                        if (ai && trailer.Owner >= 0) value *= DifficultyValue(i, .45f, 1, 1.35f);
                        if (ai && option % 4 == 0 && Deliveries[i] >= 2 && trailer.Owner >= 0) value *= 100;
                        if (hardAmbush && trailer.Owner == ambushRival[i]) value *= 4;
                        float weighted = distance / value;
                        if (weighted < priority) { priority = weighted; nearest = distance; candidate = trailer; }
                    }
                    bool seekTheft = option % 4 == 0 && Deliveries[i] >= 2 && candidate != null && candidate.Owner >= 0 && aiTarget[i] != null && aiTarget[i].Owner < 0 && approachStage[i] < 2;
                    bool validLockedTarget = aiTarget[i] != null && aiTarget[i].gameObject.activeSelf && aiTarget[i].Deliveries == 0 && (!hardAmbush || aiTarget[i].Owner == ambushRival[i]);
                    if (ai && !seekTheft && validLockedTarget && (option % 4 != 2 || (aiTarget[i].Owner < 0 || aiTarget[i].Owner % 2 != i % 2) && !ReservedByCloserTeammate(i, aiTarget[i])))
                    { candidate = aiTarget[i]; nearest = (candidate.Kingpin.position - couplers[i].Anchor.position).sqrMagnitude; }
                    if (candidate == null)
                    {
                        if (hardAmbush) { aiTarget[i] = null; AwaitRivalCourier(i); }
                        else trucks[i].TickDrive(ai ? Vector2.zero : commands[i]);
                        continue;
                    }
                    if (ai && aiTarget[i] != candidate) { aiTarget[i] = candidate; approachStage[i] = 0; approachSide[i] = Vector3.Dot(trucks[i].body.position - candidate.Body.position, candidate.transform.right) < 0 ? -1 : 1; aiReadyAt[i] = Elapsed + DifficultyValue(i, .8f, 0, .12f); }
                    if (ai && Elapsed < aiReadyAt[i]) { ParkAI(i); continue; }
                    if (contested[i] != candidate) { contested[i] = candidate; contest[i] = 0; }
                    if (candidate.Owner >= 0)
                    {
                        if (ai)
                        {
                            if (hardAmbush && Vector3.Distance(candidate.Body.worldCenterOfMass, depots[candidate.Owner].position) > 35)
                                AwaitRivalCourier(i);
                            else PursueOccupiedCargo(i, candidate, hardAmbush);
                        }
                        else trucks[i].TickDrive(commands[i]);
                        // Recompute after selecting a moving locked target.
                        nearest = (candidate.Kingpin.position - couplers[i].Anchor.position).sqrMagnitude;
                        bool contesting = nearest < 25 && trucks[i].body.linearVelocity.magnitude < 1 && candidate.Body.linearVelocity.magnitude < 1 && trucks[candidate.Owner].body.linearVelocity.magnitude < 1 && (ai || ActionHeld(i));
                        contest[i] = contesting ? contest[i] + dt : 0;
                        if (contest[i] > 3)
                        {
                            int oldOwner = candidate.Owner;
                            if (oldOwner < setup.count && couplers[oldOwner].Release() == candidate)
                            {
                                candidate.SetBrakes(1); contestedOwner[System.Array.IndexOf(trailers, candidate)] = oldOwner; contestsWon[i]++;
                                approachStage[i] = 0;
                                // A parallel Hard thief is already alongside
                                // the head, not behind the trailer. It can drive
                                // into the open ahead lane before real backing.
                                if (hardAmbush)
                                {
                                    Vector3 forward = Vector3.ProjectOnPlane(candidate.transform.forward, Vector3.up).normalized;
                                    Vector3 localAnchor = trucks[i].transform.InverseTransformPoint(couplers[i].Anchor.position);
                                    Vector3 root = candidate.Kingpin.position - Quaternion.LookRotation(forward, Vector3.up) * localAnchor;
                                    Vector3 offset = trucks[i].body.position - root;
                                    if (Vector3.Angle(trucks[i].transform.forward, forward) < 15 && Mathf.Abs(Vector3.Dot(offset, Vector3.Cross(Vector3.up, forward))) < 5 && Vector3.Dot(offset, forward) > -2)
                                        approachStage[i] = 2;
                                }
                                if (IsAI(oldOwner))
                                {
                                    clearKingpinTarget[oldOwner] = trucks[oldOwner].body.position + trucks[oldOwner].transform.forward * 16 + trucks[oldOwner].transform.right * 8;
                                    clearKingpinUntil[oldOwner] = Elapsed + 8;
                                    aiTarget[oldOwner] = null; approachStage[oldOwner] = 0;
                                }
                            }
                            contest[i] = 0;
                        }
                    }
                    else if (ai) { if (ApproachCargoAI(i, candidate, dt, false)) CoupledLoad(i, candidate); }
                    else { trucks[i].TickDrive(commands[i]); if (ActionHeld(i) && couplers[i].GuideCoupling(candidate, dt)) CoupledLoad(i, candidate); }
                    continue;
                }
                int depot = option % 4 == 2 ? i % 2 : i;
                float receivingDistance=Vector3.Distance(owned.Body.worldCenterOfMass,depots[depot].position);
                Vector3 receivingTarget=depots[depot].position+trucks[i].body.position-owned.Body.worldCenterOfMass;
                float receivingSpeed=Mathf.Min(9 * DifficultyValue(i, .75f, 1, 1.15f),Mathf.Sqrt(2*1.8f*Mathf.Max(0,receivingDistance-3)));
                trucks[i].TickDrive(ai?DriveTo(i,receivingTarget,receivingSpeed):commands[i]);
                if (!ai && actions[i]) { couplers[i].Release(); owned.SetBrakes(1); continue; }
                bool delivered = Vector3.Distance(owned.Body.worldCenterOfMass, depots[depot].position) < deliveryRadius && owned.Body.linearVelocity.magnitude < .8f && trucks[i].body.linearVelocity.magnitude < .8f;
                // A challenger already performing the legal stopped contest
                // prevents dispatch while ownership is being resolved. The
                // owner can still drive away; merely touching never contests.
                bool challenged = false;
                for (int rival = 0; rival < setup.count; rival++)
                    challenged |= rival != i && contested[rival] == owned && contest[rival] > 0 && Vector3.Distance(couplers[rival].Anchor.position, owned.Kingpin.position) < 5 && trucks[rival].body.linearVelocity.magnitude < 1;
                deliveryQuiet[i] = delivered && !challenged ? deliveryQuiet[i] + dt : 0;
                if (ai && receivingDistance < Mathf.Min(5,deliveryRadius*.6f))
                {
                    ParkAI(i);
                }
                if (deliveryQuiet[i] > 2 && owned.Deliveries == 0)
                {
                    int index = System.Array.IndexOf(trailers, owned); owned.Deliveries++; Deliveries[i]++;
                    double points = option % 4 == 1 ? 1000 : option % 4 == 0 ? (cargoStealer[index] == i ? 1500 : 150) * owned.CargoCondition : 500 * owned.CargoCondition * (option % 4 == 3 ? 1 + index % 3 : 1);
                    AddScore(i, points, "cargo-delivered");
                    if (option % 4 == 2) for (int teammate = 0; teammate < setup.count; teammate++) if (teammate != i && teammate % 2 == i % 2) AddScore(teammate, points, "team-cargo-delivered");
                    couplers[i].Release(); owned.SetBrakes(1); respawnAt[index] = Elapsed + 5; deliveryQuiet[i] = 0;
                }
            }
        }
        void EnsureAmbushRival(int slot)
        {
            if (ambushRival[slot] >= 0 && ambushRival[slot] < setup.count && ambushRival[slot] != slot) return;
            float nearest = float.MaxValue;
            for (int other = 0; other < setup.count; other++)
            {
                if (other == slot) continue;
                float distance = (trucks[slot].body.position - depots[other].position).sqrMagnitude;
                if (distance < nearest) { nearest = distance; ambushRival[slot] = other; }
            }
            ambushStage[slot] = 0;
        }
        void AwaitRivalCourier(int slot)
        {
            EnsureAmbushRival(slot);
            int rival = ambushRival[slot];
            if (rival < 0) { ParkAI(slot); return; }
            Vector3 forward = Vector3.ProjectOnPlane(depots[rival].forward, Vector3.up).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            float side = Vector3.Dot(depots[slot].position - depots[rival].position, right) < 0 ? -1 : 1;
            var referenceLoad = trailers[0];
            Vector3 localPinFromCom = referenceLoad.transform.InverseTransformVector(referenceLoad.Kingpin.position - referenceLoad.Body.worldCenterOfMass);
            Vector3 localFifthWheel = trucks[slot].transform.InverseTransformPoint(couplers[slot].Anchor.position);
            // The ordinary courier brakes toward a 3m receiving offset rather
            // than passing through the exact geometric center of its depot.
            Vector3 receivingRoot = depots[rival].position + forward * (localPinFromCom.z - localFifthWheel.z - 3);
            DriveParallelAmbush(slot, receivingRoot + right * side * 3.8f, forward, side);
        }
        void DriveParallelAmbush(int slot, Vector3 goal, Vector3 forward, float side)
        {
            ambushGoal[slot] = goal;
            var truck = trucks[slot]; var vehicle = truck.tractorMotor.vehicle;
            Vector3 right = Vector3.Cross(Vector3.up, forward), offset = truck.body.position - goal;
            float ahead = Vector3.Dot(offset, forward), lateral = Vector3.Dot(offset, right);
            float heading = Vector3.Angle(truck.transform.forward, forward);
            Vector3 target;
            if (ambushStage[slot] < 2)
            {
                target = goal + right * side * 12 + forward * (ambushStage[slot] == 0 ? -20 : 18);
                // This is an empty tractor moving along the open aisle.
                // Use the ordinary road pace here and retain the slow alignment
                // controller once it reaches the receiving bay.
                truck.TickDrive(DriveTo(slot, target, 8));
                if (Vector3.ProjectOnPlane(truck.body.position - target, Vector3.up).magnitude < 4) ambushStage[slot]++;
                return;
            }
            var transmission = vehicle.powertrain.transmission;
            transmission.automaticTransmissionDNRShiftType = NWH.VehiclePhysics2.Powertrain.TransmissionComponent.AutomaticTransmissionDNRShiftType.RequireShiftInput;
            float speed = Vector3.Dot(truck.body.linearVelocity, truck.transform.forward);
            if (ambushStage[slot] == 2)
            {
                if (heading < 8 && Mathf.Abs(lateral) < 1.25f && ahead > 8) { ambushStage[slot] = 3; ParkAI(slot); return; }
                if (transmission.Gear <= 0) { ParkAI(slot); if (Mathf.Abs(speed) < .3f) transmission.ShiftInto(1); return; }
                target = goal + forward * (ahead + 10);
                Vector3 local = truck.transform.InverseTransformPoint(target);
                truck.TickDrive(new Vector2(Mathf.Clamp(Mathf.Atan2(local.x, local.z) * 1.8f - truck.body.angularVelocity.y * .3f, -1, 1), Mathf.Clamp((2.5f - speed) * .5f, -.7f, .7f)));
                return;
            }
            float gap = Vector3.ProjectOnPlane(offset, Vector3.up).magnitude;
            if (gap < .6f && heading < 8) { ambushStage[slot] = 4; ParkAI(slot); return; }
            if (heading > 35 || Mathf.Abs(lateral) > 6) { ambushStage[slot] = 0; ParkAI(slot); return; }
            bool reverse = ahead > 0;
            if (reverse ? transmission.Gear >= 0 : transmission.Gear <= 0)
            { ParkAI(slot); if (Mathf.Abs(speed) < .3f) transmission.ShiftInto(reverse ? -1 : 1); return; }
            float steer;
            if (reverse)
            {
                Vector3 desired = Quaternion.AngleAxis(Mathf.Atan2(lateral, Mathf.Clamp(Mathf.Abs(ahead), 4, 12)) * Mathf.Rad2Deg, Vector3.up) * forward;
                steer = Mathf.Clamp(-Vector3.SignedAngle(truck.transform.forward, desired, Vector3.up) * .07f + truck.body.angularVelocity.y * .6f, -.65f, .65f);
            }
            else { Vector3 local = truck.transform.InverseTransformPoint(goal); steer = Mathf.Clamp(Mathf.Atan2(local.x, local.z) * 1.8f, -.65f, .65f); }
            float requested = Mathf.Clamp(gap * .3f, .12f, 2) * (reverse ? -1 : 1);
            float effort = Mathf.Clamp((requested - speed) * .7f, -.65f, .65f);
            if (!vehicle.powertrain.engine.IsRunning) effort = reverse ? -.35f : .35f;
            truck.TickDrive(new Vector2(steer, effort));
        }
        bool ReservedByCloserTeammate(int slot, TrailerBody trailer)
        {
            // Real dispatch reservation only coordinates AI choices. It never
            // changes physical ownership and humans can pursue any valid load.
            float ownDistance = (trucks[slot].body.position - trailer.Kingpin.position).sqrMagnitude;
            for (int other = 0; other < setup.count; other++)
            {
                if (other == slot || other % 2 != slot % 2 || !IsAI(other) || couplers[other].Attached != null || aiTarget[other] != trailer) continue;
                float distance = (trucks[other].body.position - trailer.Kingpin.position).sqrMagnitude;
                if (distance < ownDistance - 1 || Mathf.Abs(distance - ownDistance) <= 1 && other < slot) return true;
            }
            return false;
        }
        void PursueOccupiedCargo(int slot, TrailerBody trailer, bool holdReceivingBay = false)
        {
            Vector3 forward = Vector3.ProjectOnPlane(trailer.transform.forward, Vector3.up).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            int side = approachSide[slot] == 0 ? 1 : approachSide[slot];
            Vector3 anchorOffset = trucks[slot].transform.TransformVector(trucks[slot].transform.InverseTransformPoint(couplers[slot].Anchor.position));
            Vector3 alongside = trailer.Kingpin.position - anchorOffset + right * side * 3.8f;
            Vector3 velocity = Vector3.ProjectOnPlane(trailer.Body.linearVelocity, Vector3.up);
            int owner = trailer.Owner, depot = option % 4 == 2 ? owner % 2 : owner;
            // Anticipate the same measured receiving location the courier uses,
            // so a thief can physically wait alongside an arriving load rather
            // than chase stale pickup waypoints behind it indefinitely.
            float depotDistance = Vector3.Distance(trailer.Body.worldCenterOfMass, depots[depot].position);
            if (depotDistance < 35)
            {
                Vector3 receivingCom = depots[depot].position;
                if (holdReceivingBay)
                {
                    // Once stopped, use the real pin. Before then, anticipate
                    // the same final receiving offset as its physical driver.
                    receivingCom = velocity.magnitude < 1 ? trailer.Body.worldCenterOfMass : receivingCom - Vector3.ClampMagnitude(receivingCom - trailer.Body.worldCenterOfMass, 3);
                }
                alongside += receivingCom - trailer.Body.worldCenterOfMass;
            }
            else alongside += Vector3.ClampMagnitude(velocity * 1.2f, 12);
            if (holdReceivingBay) { DriveParallelAmbush(slot, alongside, forward, side); return; }
            float distance = Vector3.ProjectOnPlane(alongside - trucks[slot].body.position, Vector3.up).magnitude;
            if (distance < 1.2f && velocity.magnitude < .8f) ParkAI(slot);
            else trucks[slot].TickDrive(DriveTo(slot, alongside, Mathf.Min(12 * DifficultyValue(slot, .8f, 1, 1.12f), Mathf.Max(1.5f, velocity.magnitude + distance * .5f))));
        }
        public string ContestDiagnostic(int slot)
        {
            if (slot < 0 || slot >= setup.count) return "invalid seat";
            var target = contested[slot];
            return "contests=" + contestsWon[slot] + " steals=" + successfulSteals[slot] + " holding=" + contest[slot].ToString("F2") + " clearSeconds=" + Mathf.Max(0, clearKingpinUntil[slot] - Elapsed).ToString("F1") + " ambushRival=" + ambushRival[slot] + " bayStage=" + ambushStage[slot] + " tractor=" + trucks[slot].body.position.ToString("F2") + " bayGoal=" + ambushGoal[slot].ToString("F2") + " targetOwner=" + (target != null ? target.Owner : -1) + " gap=" + (target != null ? Vector3.Distance(couplers[slot].Anchor.position, target.Kingpin.position).ToString("F2") : "none");
        }
        void CoupledLoad(int slot, TrailerBody trailer)
        {
            int index = System.Array.IndexOf(trailers, trailer);
            if (contestedOwner[index] >= 0 && contestedOwner[index] != slot) { successfulSteals[slot]++; cargoStealer[index] = slot; }
            else if (contestedOwner[index] == slot) cargoStealer[index] = -1;
            contestedOwner[index] = -1;
            aiTarget[slot] = null; approachStage[slot] = 0;
        }
        bool ApproachCargoAI(int slot, TrailerBody trailer, float dt, bool contestOnly)
        {
            Vector3 forward = Vector3.ProjectOnPlane(trailer.transform.forward, Vector3.up).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            Vector3 hitchLocal = trucks[slot].transform.InverseTransformPoint(couplers[slot].Anchor.position);
            Vector3 root = trailer.Kingpin.position - Quaternion.LookRotation(forward, Vector3.up) * hitchLocal;
            int side = approachSide[slot] == 0 ? 1 : approachSide[slot];
            Vector3 target = root;
            switch (approachStage[slot])
            {
                case 0: target += right * side * 12 - forward * 14; break;
                case 1: target += right * side * 12 + forward * 18; break;
                case 2: target += contestOnly ? right * side * 3 : forward * 16; break;
                case 3: target += forward * 26; break;
            }
            float distance = Vector3.ProjectOnPlane(target - trucks[slot].body.position, Vector3.up).magnitude;
            if (contestOnly && approachStage[slot] >= 2)
            { if (distance < 1.5f) ParkAI(slot); else trucks[slot].TickDrive(DriveTo(slot, target, Mathf.Min(4 * DifficultyValue(slot, .75f, 1, 1.15f), distance * .6f))); return false; }
            if (approachStage[slot] < 3)
            {
                trucks[slot].TickDrive(DriveTo(slot, target, 5 * DifficultyValue(slot, .75f, 1, 1.15f)));
                if (distance < 5) approachStage[slot]++;
                return false;
            }
            if (approachStage[slot] == 3)
            {
                Vector3 offset = trucks[slot].body.position - root;
                float ahead = Vector3.Dot(offset, forward), sideError = Vector3.Dot(offset, right);
                // Stay in the real open gap before the second row of parked
                // trailers. Align the lane geometrically, not by driving into
                // the next load until an arbitrary sub-metre waypoint is hit.
                Vector3 lanePoint = root + forward * Mathf.Min(26, ahead + 10);
                Vector3 localPoint = trucks[slot].transform.InverseTransformPoint(lanePoint);
                float alignmentHeading = Vector3.Angle(trucks[slot].transform.forward, forward);
                float alignmentSpeed = Vector3.Dot(trucks[slot].body.linearVelocity, trucks[slot].transform.forward);
                float alignmentSteer = Mathf.Clamp(Mathf.Atan2(localPoint.x, localPoint.z) * 1.8f - trucks[slot].body.angularVelocity.y * .3f, -1, 1);
                trucks[slot].TickDrive(new Vector2(alignmentSteer, Mathf.Clamp((2.5f - alignmentSpeed) * .5f, -.7f, .6f)));
                if (alignmentHeading < 8 && Mathf.Abs(sideError) < 1.25f && ahead > 8) approachStage[slot]++;
                return false;
            }
            // Use the same measured close-approach controller that physically
            // completed Handoff's native recoupling. The old always-negative
            // speed target kept reversing after passing the pin, then steered
            // a turned-around tractor hundreds of metres away from its cargo.
            float heading = Vector3.Angle(trucks[slot].transform.forward, forward);
            Vector3 error = trucks[slot].body.position - root;
            if (heading > 35 || Mathf.Abs(Vector3.Dot(error, right)) > 6 || Vector3.Dot(error, forward) < -2)
            { approachStage[slot] = 0; trucks[slot].TickDrive(Vector2.zero); return false; }
            var vehicle = trucks[slot].tractorMotor.vehicle;
            var transmission = vehicle.powertrain.transmission;
            transmission.automaticTransmissionDNRShiftType = NWH.VehiclePhysics2.Powertrain.TransmissionComponent.AutomaticTransmissionDNRShiftType.RequireShiftInput;
            float speed = Vector3.Dot(trucks[slot].body.linearVelocity, trucks[slot].transform.forward);
            if (transmission.Gear >= 0)
            {
                ParkAI(slot);
                if (Mathf.Abs(speed) < .35f) transmission.ShiftInto(-1);
                return false;
            }
            float pinDistance = Vector3.Distance(couplers[slot].Anchor.position, trailer.Kingpin.position);
            float lateralError = Vector3.Dot(error, right);
            float lookahead = Mathf.Clamp(Mathf.Abs(Vector3.Dot(error, forward)), 4, 12);
            Vector3 desiredHeading = Quaternion.AngleAxis(Mathf.Atan2(lateralError, lookahead) * Mathf.Rad2Deg, Vector3.up) * forward;
            float headingError = Vector3.SignedAngle(trucks[slot].transform.forward, desiredHeading, Vector3.up);
            float steer = Mathf.Clamp(-headingError * DifficultyValue(slot, .055f, .07f, .075f) + trucks[slot].body.angularVelocity.y * .6f, -.65f, .65f);
            float requested = -Mathf.Clamp(pinDistance * .25f, .12f, DifficultyValue(slot, 1.6f, 2.5f, 2.8f));
            float effort = Mathf.Clamp((requested - speed) * .7f, -.75f, .7f);
            // Once within native capture range, let the real bounded kingpin
            // guide force brake and center the bodies rather than driving past.
            trucks[slot].TickDrive(new Vector2(steer, pinDistance < .6f ? 0 : effort));
            return couplers[slot].GuideCoupling(trailer, dt);
        }
        float DifficultyValue(int slot, float easy, float normal, float hard)
        {
            return setup.players[slot].difficulty == BowlingDifficulty.Easy ? easy : setup.players[slot].difficulty == BowlingDifficulty.Hard ? hard : normal;
        }
        // Presentation reads the authoritative trailer owner; duplicate display
        // names remain distinguishable by the permanent participant seat.
        public string CargoOwnershipLabel(TrailerBody trailer, out Color color)
        {
            int owner = trailer != null ? trailer.Owner : -1;
            if (owner >= 0 && owner < setup.count)
            {
                color = setup.players[owner].color;
                return "P" + (owner + 1) + " · " + setup.players[owner].name + "\nCARGO OWNER";
            }
            color = new Color(.9f, .88f, .7f);
            return "UNCLAIMED CARGO\nALIGN / HOLD ACTION";
        }
        public string HitchRunApproachDiagnostic(int slot)
        {
            if (slot < 0 || slot >= setup.count) return "invalid seat";
            var target = aiTarget[slot]; var vehicle = trucks[slot].tractorMotor.vehicle;
            return "stage=" + approachStage[slot] + " target=" + (target != null ? target.name : "none") + " gear=" + vehicle.powertrain.transmission.Gear + " speed=" + Vector3.Dot(trucks[slot].body.linearVelocity, trucks[slot].transform.forward).ToString("F2") + " throttle=" + vehicle.input.InputSwappedThrottle.ToString("F2") + " brakes=" + vehicle.input.InputSwappedBrakes.ToString("F2") + (target != null ? " " + couplers[slot].DiagnosticState(target) : "");
        }
        void ParkAI(int slot)
        {
            trucks[slot].TickDrive(Vector2.zero); var input = trucks[slot].tractorMotor.vehicle.input;
            input.states.handbrake = input.states.handbrakeRaw = 1; input.states.brakes = input.states.brakesRaw = 0;
        }
        void ConfigureCargo(int index) => trailers[index].AddCargo(option % 4 == 3 ? index % 3 * 3500 : 0);
        void UpdateDispatchedLoads()
        {
            for (int t = 0; t < trailers.Length; t++)
            {
                var trailer = trailers[t];
                if (respawnAt[t] <= 0 && trailer.gameObject.activeSelf && (trailer.Body.position.y < -40 || Mathf.Abs(trailer.Body.position.x) > 1000 || Mathf.Abs(trailer.Body.position.z) > 1000))
                { if (trailer.Owner >= 0) couplers[trailer.Owner].Release(); trailer.Deliveries = 1; respawnAt[t] = Elapsed + 5; }
                if (respawnAt[t] <= 0) continue;
                if (Elapsed < respawnAt[t]) { if (Elapsed >= respawnAt[t] - 4.8f) trailer.gameObject.SetActive(false); continue; }
                bool occupied = false;
                for (int i = 0; i < setup.count; i++) occupied |= Vector3.Distance(trucks[i].body.position, trailer.StartPosition) < 18;
                for (int j = 0; j < trailers.Length; j++) if (j != t && trailers[j].gameObject.activeSelf) occupied |= Vector3.Distance(trailers[j].Body.position, trailer.StartPosition) < 18;
                if (occupied) continue;
                trailer.gameObject.SetActive(true); trailer.ResetForRound(); foreach (var body in trailer.GetComponentsInChildren<Rigidbody>(true)) body.isKinematic = false;
                ConfigureCargo(t); respawnAt[t] = 0; contestedOwner[t] = cargoStealer[t] = -1;
            }
        }
    }
}

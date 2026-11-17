using UnityEngine;
namespace LWS.TruckHitchSports
{
    public sealed class HitchRunMatch : HitchCompetitionSession
    {
        public Transform[] depots;
        public float deliveryRadius = 9;
        readonly float[] contest = new float[4], deliveryQuiet = new float[4];
        readonly int[] successfulSteals = new int[4];
        readonly TrailerBody[] contested = new TrailerBody[4];
        readonly TrailerBody[] aiTarget = new TrailerBody[4];
        readonly int[] approachStage = new int[4], approachSide = new int[4];
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
        public override string Help => "Back the fifth wheel under a kingpin and hold ACTION to physically couple. Press ACTION to unhitch your own load. Deliver attached cargo to your marked depot and stop both real bodies for two seconds. To contest an occupied load, park within five meters of its kingpin and hold ACTION for three seconds while both rigs are slow; then physically recouple. Contact never transfers ownership. Trailer Thief pays a large bonus only when you actually steal and deliver another driver's cargo. Delivery Rush scores completed delivery count. Team Smuggling uses seats 1+3 against 2+4 with shared depots and shared cargo points; AI fills unused human seats. High-Value Cargo Hunt adds real mass and value to three load tiers, shown in your panel while hitched and reduced by damage. Dispatched loads return at their original yard positions after a delay, only when the spawn is clear.";
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
            System.Array.Clear(aiTarget, 0, 4); System.Array.Clear(approachStage, 0, 4);
            respawnAt = new float[trailers.Length]; contestedOwner = new int[trailers.Length]; cargoStealer = new int[trailers.Length];
            for (int t = 0; t < trailers.Length; t++) { contestedOwner[t] = cargoStealer[t] = -1; ConfigureCargo(t); }
        }
        protected override void ReportFinalMeasurements(int slot)
        {
            RecordMetric(slot, "hitch_run_steals", successfulSteals[slot]);
            RecordMetric(slot, "hitch_run_deliveries", Deliveries[slot]);
        }
        protected override void TickCompetition(float dt)
        {
            UpdateDispatchedLoads();
            Notice = "ALIGN KINGPIN · HOLD ACTION TO COUPLE / CONTEST · STOP AT YOUR DEPOT";
            for (int i = 0; i < setup.count; i++)
            {
                var owned = couplers[i].Attached; bool ai = IsAI(i);
                if (owned == null)
                {
                    TrailerBody candidate = null; float nearest = float.MaxValue, priority = float.MaxValue;
                    foreach (var trailer in trailers)
                    {
                        if (!trailer.gameObject.activeSelf || trailer.Deliveries > 0 || trailer.Owner == i || option % 4 == 2 && trailer.Owner >= 0 && trailer.Owner % 2 == i % 2) continue;
                        float distance = (trailer.Kingpin.position - couplers[i].Anchor.position).sqrMagnitude;
                        int tier = 1 + System.Array.IndexOf(trailers, trailer) % 3;
                        float value = ai && option % 4 == 3 ? tier * tier : ai && option % 4 == 0 && trailer.Owner >= 0 ? 2.5f : 1;
                        float weighted = distance / value;
                        if (weighted < priority) { priority = weighted; nearest = distance; candidate = trailer; }
                    }
                    if (ai && aiTarget[i] != null && aiTarget[i].gameObject.activeSelf && aiTarget[i].Deliveries == 0 && (option % 4 != 2 || aiTarget[i].Owner < 0 || aiTarget[i].Owner % 2 != i % 2))
                    { candidate = aiTarget[i]; nearest = (candidate.Kingpin.position - couplers[i].Anchor.position).sqrMagnitude; }
                    if (candidate == null) { trucks[i].TickDrive(ai ? Vector2.zero : commands[i]); continue; }
                    if (ai && aiTarget[i] != candidate) { aiTarget[i] = candidate; approachStage[i] = 0; approachSide[i] = Vector3.Dot(trucks[i].body.position - candidate.Body.position, candidate.transform.right) < 0 ? -1 : 1; }
                    if (contested[i] != candidate) { contested[i] = candidate; contest[i] = 0; }
                    if (candidate.Owner >= 0)
                    {
                        if (ai) ApproachCargoAI(i, candidate, dt, true); else trucks[i].TickDrive(commands[i]);
                        bool contesting = nearest < 25 && trucks[i].body.linearVelocity.magnitude < 1 && candidate.Body.linearVelocity.magnitude < 1 && (ai || ActionHeld(i));
                        contest[i] = contesting ? contest[i] + dt : 0;
                        if (contest[i] > 3)
                        { int oldOwner = candidate.Owner; if (couplers[oldOwner].Release() == candidate) { candidate.SetBrakes(1); contestedOwner[System.Array.IndexOf(trailers, candidate)] = oldOwner; } contest[i] = 0; }
                    }
                    else if (ai) { if (ApproachCargoAI(i, candidate, dt, false)) CoupledLoad(i, candidate); }
                    else { trucks[i].TickDrive(commands[i]); if (ActionHeld(i) && couplers[i].GuideCoupling(candidate, dt)) CoupledLoad(i, candidate); }
                    continue;
                }
                int depot = option % 4 == 2 ? i % 2 : i;
                trucks[i].TickDrive(ai ? DriveTo(i, depots[depot].position + Vector3.forward * 10, 9) : commands[i]);
                if (!ai && actions[i]) { couplers[i].Release(); owned.SetBrakes(1); continue; }
                bool delivered = Vector3.Distance(owned.Body.worldCenterOfMass, depots[depot].position) < deliveryRadius && owned.Body.linearVelocity.magnitude < .8f && trucks[i].body.linearVelocity.magnitude < .8f;
                deliveryQuiet[i] = delivered ? deliveryQuiet[i] + dt : 0;
                if (ai && Vector3.Distance(owned.Body.worldCenterOfMass, depots[depot].position) < deliveryRadius)
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
                case 1: target += right * side * 12 + forward * 20; break;
                case 2: target += contestOnly ? right * side * 3 : forward * 40; break;
                case 3: target += forward * 80; break;
            }
            float distance = Vector3.ProjectOnPlane(target - trucks[slot].body.position, Vector3.up).magnitude;
            if (contestOnly && approachStage[slot] >= 2)
            { if (distance < 1.5f) ParkAI(slot); else trucks[slot].TickDrive(DriveTo(slot, target, Mathf.Min(4, distance * .6f))); return false; }
            if (approachStage[slot] < 3)
            {
                trucks[slot].TickDrive(DriveTo(slot, target, 5));
                if (distance < 5) approachStage[slot]++;
                return false;
            }
            if (approachStage[slot] == 3)
            {
                trucks[slot].TickDrive(DriveTo(slot, target, 5));
                if (Vector3.Angle(trucks[slot].transform.forward, forward) < 6 && Mathf.Abs(Vector3.Dot(trucks[slot].body.position - root, right)) < .5f && Vector3.Dot(trucks[slot].body.position - root, forward) > 20) approachStage[slot]++;
                return false;
            }
            Vector3 local = trucks[slot].transform.InverseTransformPoint(root);
            float speed = Vector3.Dot(trucks[slot].body.linearVelocity, trucks[slot].transform.forward);
            float requested = -Mathf.Min(1.5f, Mathf.Max(.05f, local.magnitude * .35f));
            trucks[slot].TickDrive(new Vector2(Mathf.Clamp(-Mathf.Atan2(local.x, Mathf.Max(.2f, -local.z)) * 1.8f, -1, 1), Mathf.Clamp((requested - speed) * .5f, -.5f, .5f)));
            return couplers[slot].GuideCoupling(trailer, dt);
        }
        void ParkAI(int slot)
        {
            trucks[slot].TickDrive(Vector2.zero); var input = trucks[slot].tractorMotor.vehicle.input;
            input.states.handbrake = input.states.handbrakeRaw = 1; input.states.brakes = input.states.brakesRaw = 1;
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

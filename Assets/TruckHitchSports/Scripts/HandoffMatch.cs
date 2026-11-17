using UnityEngine;
namespace LWS.TruckHitchSports
{
    public sealed class HandoffMatch : HitchCompetitionSession
    {
        public Transform[] transferZones, finishes;
        readonly int[] leg = new int[2];
        readonly float[] transfer = new float[2];
        readonly bool[] released = new bool[2], reverseEntered = new bool[2], backing = new bool[2];
        readonly bool[] finishedTeam = new bool[2];
        readonly double[] handoffPenalty = new double[2];
        readonly float[] finishSeconds = new float[2];
        public override bool ResultEligible(int slot) => slot >= 0 && slot < setup.count && finishedTeam[slot % 2] && Scores[slot] > 0;
        public override string Title => "HITCH HANDOFF";
        // Four competitor seats; any vacant human seats are AI partners/opponents.
        public override int MinimumPlayers => 4;
        public override string OptionLabel => new[] { "STANDARD RELAY", "OBSTACLE RELAY", "REVERSE-HITCH RELAY", "EXTENDED TEAM HAUL" }[option % 4];
        public override string Help => "Teams are seats 1+3 against 2+4; unused human seats have actual AI partners/opponents. The trailer is the baton. Tow to the transfer zone, stop and press ACTION to unhitch. Your teammate backs under that same kingpin and holds ACTION to couple. Clear the old tractor before the next latch. Standard Relay has two physical legs. Obstacle Relay adds the offset barrier/chicane. Reverse-Hitch Relay requires an actual backward entry into the transfer zone before releasing. Extended Team Haul alternates both teammates through four real legs. Slow handoffs reduce the handoff award; final cargo condition and finish time determine the team result. Both teammates share it. There is no trailer teleport or automatic transfer.";
        protected override void BeginCompetition()
        { System.Array.Clear(leg, 0, 2); System.Array.Clear(transfer, 0, 2); System.Array.Clear(released, 0, 2); System.Array.Clear(reverseEntered, 0, 2); System.Array.Clear(backing, 0, 2); System.Array.Clear(finishedTeam, 0, 2); System.Array.Clear(handoffPenalty, 0, 2); System.Array.Clear(finishSeconds, 0, 2); }
        protected override void ReportFinalMeasurements(int slot)
        {
            int team = slot % 2; string context = "team=" + team;
            RecordMetric(slot, "relay_team_legs_completed", leg[team] + (finishedTeam[team] ? 1 : 0), context);
            RecordMetric(slot, "relay_team_handoffs", leg[team], context);
            RecordMetric(slot, "relay_handoff_penalty_points", handoffPenalty[team], context);
            RecordMetric(slot, "relay_team_finished", finishedTeam[team] ? 1 : 0, context);
            if (finishedTeam[team]) RecordMetric(slot, "relay_finish_seconds", finishSeconds[team], context);
        }
        protected override void TickCompetition(float dt)
        {
            Notice = "1 + 3 vs 2 + 4 · STOP, UNHITCH, CLEAR THE KINGPIN, RECOUPLE";
            int legs = option % 4 == 3 ? 4 : 2;
            for (int team = 0; team < 2; team++)
            {
                if (Completed[team]) continue;
                int active = team + (leg[team] % 2) * 2;
                int waiting = team + ((leg[team] + 1) % 2) * 2;
                var trailer = trailers[team];
                Vector3 station = transferZones[team].position + Vector3.forward * leg[team] * 80;
                bool final = leg[team] == legs - 1;
                Vector3 destination = final ? finishes[team].position + Vector3.forward * (legs - 2) * 80 : station;
                if (!released[team])
                {
                    bool inZone = Vector3.Distance(trailer.Body.position, destination) < 10;
                    Vector3 target = destination + Vector3.forward * 12;
                    if (option % 4 == 1 && leg[team] == 0 && trucks[active].body.position.z < 75)
                        target = new Vector3(destination.x + 17, destination.y, 85);
                    Vector2 aiDrive = inZone ? new Vector2(0, -1) : DriveTo(active, target, option % 4 == 1 ? 6 : 9);
                    if (!final && option % 4 == 2 && !reverseEntered[team])
                    {
                        if (trailer.Body.position.z < destination.z + 12) aiDrive = DriveTo(active, destination + Vector3.forward * 28, 3);
                        else backing[team] = true;
                        if (backing[team]) aiDrive = new Vector2(Mathf.Clamp((trucks[active].body.position.x - target.x) * .1f, -.5f, .5f), -.3f);
                        if (inZone && Vector3.Dot(trailer.Body.linearVelocity, trailer.transform.forward) < -.2f) reverseEntered[team] = true;
                    }
                    trucks[active].TickDrive(IsAI(active) ? aiDrive : commands[active]);
                    if (IsAI(active) && inZone && (final || option % 4 != 2 || reverseEntered[team])) ParkAI(active);
                    Vector3 staging = station + Vector3.forward * 30;
                    trucks[waiting].TickDrive(IsAI(waiting) ? (Vector3.Distance(trucks[waiting].body.position, staging) < 5 ? new Vector2(0, -1) : DriveTo(waiting, staging, 5)) : commands[waiting]);
                    if (IsAI(waiting) && Vector3.Distance(trucks[waiting].body.position, staging) < 5) ParkAI(waiting);
                    if (inZone && trailer.Body.linearVelocity.magnitude < .9f)
                    {
                        if (final)
                        {
                            double value = Mathf.Max(0, 2400 - Elapsed * 6) * trailer.CargoCondition;
                            AddScore(team, value, "relay-finish"); AddScore(team + 2, value, "relay-finish"); finishedTeam[team] = true; finishSeconds[team] = Elapsed; Completed[team] = Completed[team + 2] = true;
                        }
                        else if ((option % 4 != 2 || reverseEntered[team]) && (IsAI(active) || actions[active]))
                        { couplers[active].Release(); trailer.SetBrakes(1); released[team] = true; transfer[team] = Elapsed; }
                    }
                }
                else
                {
                    trucks[active].TickDrive(IsAI(active) ? DriveTo(active, station + new Vector3(18, 0, 28), 4) : commands[active]);
                    if (IsAI(waiting)) ApproachHitch(waiting, trailer, dt);
                    else { trucks[waiting].TickDrive(commands[waiting]); if (ActionHeld(waiting)) couplers[waiting].GuideCoupling(trailer, dt); }
                    if (couplers[waiting].Attached == trailer)
                    {
                        leg[team]++; released[team] = reverseEntered[team] = backing[team] = false;
                        double points = Mathf.Max(20, 200 - (Elapsed - transfer[team]) * 2);
                        handoffPenalty[team] += 200 - points;
                        AddScore(active, points, "relay-handoff"); AddScore(waiting, points, "relay-handoff");
                    }
                }
                if (trailer.Body.position.y < -20) Completed[team] = Completed[team + 2] = true;
            }
        }
        void ParkAI(int slot)
        {
            trucks[slot].TickDrive(Vector2.zero); var input = trucks[slot].tractorMotor.vehicle.input;
            input.states.handbrake = input.states.handbrakeRaw = 1; input.states.brakes = input.states.brakesRaw = 1;
        }
    }
}

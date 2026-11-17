using UnityEngine;
using System.Linq;
using LWS.TruckHitchSports;

namespace LWS.TruckExtremeHauling
{
    public sealed class TrailerPlinkoMatch : ExtremeHaulingMatch
    {
        public Transform[] boardOrigins;
        [Min(1)] public float boardScale=2.5f;
        public PlinkoLandingTray[] landingTrays;
        public float[] binPoints = { 100, 300, 1000, 300, 100 };
        Rigidbody[] movingPegs;
        Vector3[] pegPositions;
        int targetBin;
        public override string Status => option == 1 ? "PRECISION TARGET · BIN " + (targetBin + 1) + " · ACTION: DROP" : option == 2 ? "MOVING PEGS · PHYSICAL CHAOS · ACTION: DROP" : base.Status;
        bool[] dropped = new bool[4];
        float[] quiet = new float[4];
        float[] spin = new float[4];
        protected override void ReportFinalMeasurements(int i) { RecordMetric(i,"plinko_released",dropped[i]?1:0);RecordMetric(i,"plinko_release_spin_radians",spin[i]);if(deliveries[i]>0){Vector3 at=boardOrigins[i].InverseTransformPoint(trailers[i].Body.worldCenterOfMass);int bin=Mathf.Clamp(Mathf.FloorToInt((at.x+25*boardScale)/(10*boardScale)),0,binPoints.Length-1);RecordMetric(i,"plinko_supported_landing_bin",bin);if(option==1)RecordMetric(i,"plinko_target_bin",targetBin);} }
        public override string Title => "TRAILER PLINKO";
        protected override string[] Modes => new[] { "CLASSIC PLINKO", "PRECISION DROP", "CHAOS PLINKO", "HIGH-RISK JACKPOT" };
        protected override string ActionHint => "DROP TRAILER";
        public override string Help => "Steer to choose the release position. Throttle adds initial rotational momentum. ACTION releases the physical trailer into the peg board. Real peg collisions and inertia determine the final bin. The trailer must settle inside the catch tray before points are awarded.";
        protected override void BeginMechanic()
        {
            targetBin = (RoundSeed() % binPoints.Length + binPoints.Length) % binPoints.Length;
            if (movingPegs == null)
            {
                var pegs = gameObject.scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Collider>(true)).Where(c => c.name == "Physical rebound peg").Where((c,index) => index % 3 == 0).ToArray();
                movingPegs = new Rigidbody[pegs.Length]; pegPositions = new Vector3[pegs.Length];
                for(int p=0;p<pegs.Length;p++) { movingPegs[p] = pegs[p].GetComponent<Rigidbody>(); if(movingPegs[p]==null)movingPegs[p]=pegs[p].gameObject.AddComponent<Rigidbody>(); movingPegs[p].isKinematic=true; movingPegs[p].useGravity=false; pegPositions[p]=movingPegs[p].position; }
            }
            for(int p=0;p<movingPegs.Length;p++) { movingPegs[p].position=pegPositions[p]; movingPegs[p].transform.position=pegPositions[p]; }
            System.Array.Clear(dropped, 0, dropped.Length); System.Array.Clear(quiet, 0, quiet.Length); System.Array.Clear(spin, 0, spin.Length);
            foreach (var tray in landingTrays) tray.ResetContacts();
            for (int i = 0; i < setup.count; i++) { trailers[i].Vehicle.enabled = false; trailers[i].Body.isKinematic = true; trucks[i].Power(false); foreach(var body in trucks[i].GetComponentsInChildren<Rigidbody>(true))body.isKinematic=true; }
        }
        int RoundSeed() => MatchId == null ? 0 : MatchId.GetHashCode() & int.MaxValue;
        protected override void Simulate(float dt)
        {
            if(option==2 && movingPegs != null) for(int p=0;p<movingPegs.Length;p++) movingPegs[p].MovePosition(pegPositions[p] + Vector3.right * (Mathf.Sin(age*1.3f+p*.7f)*2.4f));
            base.Simulate(dt);
        }
        protected override Vector2 AICommand(int i, float dt)
        {
            // Difficulty affects aiming correction and release timing; never the bin.
            float error = Mathf.Sin(age * 1.5f + i) * (.4f - (int)setup.players[i].difficulty * .15f);
            return new Vector2(Mathf.Clamp((boardOrigins[i].position.x + (option==1 ? (targetBin-2)*8*boardScale : 0) - trailers[i].Body.position.x) * .3f + error, -1, 1), .2f);
        }
        protected override void StepMechanic(int i, Vector2 drive, bool ai, float dt)
        {
            var body = trailers[i].Body; var origin = boardOrigins[i];
            if (!dropped[i])
            {
                Vector3 local = origin.InverseTransformPoint(body.position);
                local.x = Mathf.Clamp(local.x + drive.x * dt * 6 * boardScale, -18 * boardScale, 18 * boardScale);
                body.MovePosition(origin.TransformPoint(local)); spin[i] = Mathf.Clamp(spin[i] + drive.y * dt, -2.5f, 2.5f);
                if (actions[i] || ai && age > 3 + i * .25f)
                { dropped[i] = true; body.isKinematic = false; body.useGravity = true; body.angularVelocity = origin.forward * spin[i]; }
                return;
            }
            Vector3 at = origin.InverseTransformPoint(body.worldCenterOfMass);
            bool inTray = at.y < -48*boardScale && at.y > -58*boardScale && Mathf.Abs(at.x) < 25*boardScale && Mathf.Abs(at.z) < 6*boardScale;
            quiet[i] = inTray && landingTrays[i].Supports(body) && body.linearVelocity.magnitude < .6f && body.angularVelocity.magnitude < .2f ? quiet[i] + dt : 0;
            if (quiet[i] > 1.5f)
            {
                int bin = Mathf.Clamp(Mathf.FloorToInt((at.x + 25*boardScale) / (10*boardScale)), 0, binPoints.Length - 1);
                Award(i, option == 1 ? Mathf.Max(25, 1500 - Mathf.Abs(bin-targetBin)*500) : binPoints[bin] * (option == 3 && bin == 2 ? 3 : 1), "settled_plinko_bin_" + bin); retired[i] = true;
            }
        }
        protected override Vector3 CameraFocus(int slot) => trailers[CameraParticipant(slot)].Body.worldCenterOfMass;
    }
}

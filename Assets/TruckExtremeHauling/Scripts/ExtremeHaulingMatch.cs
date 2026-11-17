using System;
using System.Text;
using UnityEngine;
using LWS.TruckBowling;
using LWS.TruckSports;
using LWS.TruckHitchSports;

namespace LWS.TruckExtremeHauling
{
    public abstract class ExtremeHaulingMatch : SportsEventSession
    {
        public SportsEventTruck[] trucks;
        public TrailerBody[] trailers;
        public SportsEventCamera broadcast;
        public SportsRallyViews views;
        public Transform[] objectives;
        public float matchSeconds = 150;
        protected float age;
        string reportedMatch;
        protected readonly double[] scores = new double[8];
        protected readonly int[] deliveries = new int[8];
        protected readonly bool[] retired = new bool[8];
        protected Vector3[] starts;
        protected Quaternion[] rotations;
        protected HitchCoupler[] hitches;
        Vector3[] viewFocus;
        bool[] initialCoupling = new bool[4];
        protected virtual bool StartsCoupled => this is JackknifeIceMatch || this is RoadTrainWreckMatch || this is HitchRingTossMatch || this is ThinIceMatch;
        public virtual bool Cooperative => false;
        public override int MinimumPlayers => 1;
        public override int MaximumPlayers => 4;
        public override int OptionCount => 4;
        public override bool SupportsSplitAIViews => true;
        protected abstract string[] Modes { get; }
        public override string OptionLabel => Modes[Mathf.Clamp(option, 0, Modes.Length - 1)];
        public override string Header => Title + " · " + OptionLabel + " · " + Mathf.CeilToInt(Mathf.Max(0, matchSeconds - age)) + "s";
        public override string Status => "STEER / THROTTLE · ACTION: " + ActionHint;
        protected virtual string ActionHint => "HITCH / RELEASE";
        public override bool CompactBroadcastStatus => true;
        public override string Standings => Results;
        public override string Results
        {
            get { var text = new StringBuilder(); for (int i = 0; i < setup.count; i++) text.AppendLine(setup.players[i].name + " · " + scores[i].ToString("0") + " · " + deliveries[i] + " OBJECTIVES"); return text.ToString(); }
        }
        public double ResultScore(int slot) => scores[slot];
        public int ObjectivesCompleted(int slot) => deliveries[slot];
        public bool ResultEligible(int slot) => slot>=0 && slot<setup.count && deliveries[slot]>0;
        public int ResultPlace(int slot) { if(!ResultEligible(slot))return 0;int place = 1; for (int i = 0; i < setup.count; i++) if (ResultEligible(i) && scores[i] > scores[slot]) place++; return place; }
        public bool ResultWinner(int slot) => !Cooperative && deliveries[slot] > 0 && ResultPlace(slot) == 1;
        public override bool TryReadScoreCard(int card, out string value, out string detail)
        { value = detail = null; if (card < 0 || card >= setup.count) return false; value = scores[card].ToString("0"); detail = retired[card] ? "ATTEMPT COMPLETE" : hitches!=null&&hitches[card].Attached!=null ? "HITCH LOCKED · " + deliveries[card] + " OBJECTIVES" : deliveries[card] + " OBJECTIVES"; return true; }
        protected override void Awake()
        {
            base.Awake();
            if (trucks == null) return;
            starts = new Vector3[trucks.Length]; rotations = new Quaternion[trucks.Length]; hitches = new HitchCoupler[trucks.Length];
            viewFocus = new Vector3[trucks.Length];
            for (int i = 0; i < trucks.Length; i++) { starts[i] = trucks[i].transform.position; rotations[i] = trucks[i].transform.rotation; hitches[i] = trucks[i].GetComponent<HitchCoupler>(); }
        }
        protected override void BeginGame()
        {
            age = 0; Array.Clear(scores, 0, scores.Length); Array.Clear(deliveries, 0, deliveries.Length); Array.Clear(retired, 0, retired.Length);
            Array.Clear(initialCoupling, 0, initialCoupling.Length);
            for (int i = 0; i < hitches.Length; i++) { hitches[i]?.Release(); hitches[i]?.ResetMatchRecords(); }
            foreach (var trailer in trailers) trailer.ResetForRound();
            for (int i = 0; i < trucks.Length; i++)
            {
                trucks[i].gameObject.SetActive(i < setup.count);
                int pieces=this is RoadTrainWreckMatch ? 3 : this is DominoDerbyMatch domino ? domino.piecesPerPlayer : 1;
                for(int piece=0;piece<pieces;piece++){int index=i*pieces+piece;if(index<trailers.Length)trailers[index].gameObject.SetActive(i<setup.count);}
                if (i < setup.count)
                {
                    foreach (var body in trucks[i].GetComponentsInChildren<Rigidbody>(true)) body.isKinematic = false;
                    trucks[i].tractorMotor?.Configure(); trucks[i].Prepare(starts[i], rotations[i]);
                    if (StartsCoupled)
                    {
                        foreach (var body in trucks[i].GetComponentsInChildren<Rigidbody>(true)) body.isKinematic = true;
                        int index = this is RoadTrainWreckMatch ? i * 3 : i;
                        foreach (var body in trailers[index].GetComponentsInChildren<Rigidbody>(true)) body.isKinematic = true;
                    }
                }
            }
            BeginMechanic();
            views?.Layout();
        }
        protected virtual void BeginMechanic() { }
        protected virtual Vector3 Destination(int slot)
        {
            var target = objectives[slot].position;
            if (hitches[slot].Attached != null) target += trucks[slot].body.position - hitches[slot].Attached.Body.worldCenterOfMass;
            return target;
        }
        protected virtual Vector2 AICommand(int slot, float dt)
        {
            var truck = trucks[slot]; Vector3 target = Destination(slot);
            Vector3 local = truck.transform.InverseTransformPoint(target);
            float distance = local.magnitude;
            bool reverse = local.z < -2 && distance < 12;
            float angle = Mathf.Atan2(local.x, reverse ? -local.z : local.z) * Mathf.Rad2Deg;
            int difficulty = (int)setup.players[slot].difficulty;
            float requestedSpeed = Mathf.Min(8 + difficulty * 3, distance * (.5f + difficulty * .2f)) * (reverse ? -1 : 1);
            float speed = Vector3.Dot(truck.body.linearVelocity, truck.transform.forward);
            return new Vector2(Mathf.Clamp(angle / 35, -1, 1) * (reverse ? -1 : 1), Mathf.Clamp((requestedSpeed - speed) * .25f, -.8f, .8f));
        }
        protected override void Simulate(float dt)
        {
            age += dt;
            if ((StartsCoupled || this is ZeroGHitchMatch) && age < 3)
            {
                for (int i = 0; i < setup.count; i++) if (StartsCoupled && !initialCoupling[i])
                {
                    int index = this is RoadTrainWreckMatch ? i * 3 : i;
                    initialCoupling[i] = hitches[i].TryCouple(trailers[index]);
                    if (initialCoupling[i])
                    {
                        foreach (var body in trucks[i].GetComponentsInChildren<Rigidbody>(true)) body.isKinematic = false;
                        foreach (var body in trailers[index].GetComponentsInChildren<Rigidbody>(true)) body.isKinematic = false;
                    }
                }
                return;
            }
            {
                float weatherGrip=presentationWeather!=null?presentationWeather.Grip:1;
                float traction=(this is JackknifeIceMatch ? .45f : this is ThinIceMatch ? .35f : 1f)*weatherGrip;
                for(int i=0;i<setup.count;i++){trucks[i].tractorMotor.gripScale=traction;trucks[i].tractorMotor.lateralGripMultiplier=this is JackknifeIceMatch?.25f:1;}
                foreach(var trailer in trailers)
                {
                    if(this is JackknifeIceMatch)trailer.SetGripMultipliers(traction,.025f*weatherGrip);else trailer.SetGripMultiplier(traction);
                    if(presentationWeather!=null&&!trailer.Body.isKinematic)trailer.Body.AddForce(Vector3.ClampMagnitude(presentationWeather.Flow,20)*trailer.Body.mass*.04f);
                }
            }
            for (int i = 0; i < setup.count; i++)
            {
                if (retired[i]) { trucks[i].TickDrive(Vector2.zero); continue; }
                bool ai = setup.players[i].source == BowlingSource.AI;
                Vector2 drive = ai ? AICommand(i, dt) : commands[i];
                trucks[i].TickDrive(drive);
                StepMechanic(i, drive, ai, dt);
                if (trucks[i].body.position.y < -60) retired[i] = true;
            }
            bool all = true; for (int i = 0; i < setup.count; i++) all &= retired[i];
            if(age>=matchSeconds||all)
            {
                if(!string.IsNullOrEmpty(MatchId)&&reportedMatch!=MatchId)
                {
                    reportedMatch=MatchId;
                    for(int slot=0;slot<setup.count;slot++)
                    {
                        RecordMetric(slot,"match_elapsed_seconds",age);
                        RecordMetric(slot,"qualifying_outcome",ResultEligible(slot)?1:0);
                        ReportFinalMeasurements(slot);
                    }
                }
                EndMatch();
            }
        }
        protected abstract void StepMechanic(int slot, Vector2 drive, bool ai, float dt);
        protected virtual void ReportFinalMeasurements(int slot) { }
        protected void RecordMetric(int slot,string measurement,double value)
        {
            if(slot<0||slot>=setup.count||double.IsNaN(value)||double.IsInfinity(value))return;
            SportsSessionTelemetry.GameplayEvent(gameObject.scene.name,measurement,setup.players[slot].participantId,"mode="+OptionLabel,value);
        }

        protected void Award(int slot, double value, string reason)
        {
            scores[slot] += value; deliveries[slot]++;
            ConfirmScore(MatchId + ":" + slot + ":" + deliveries[slot], slot, value, scores[slot], reason, SportsScoreTier.Major);
            SportsSessionTelemetry.GameplayEvent(gameObject.scene.name, reason, setup.players[slot].participantId, "Measured physical objective", value);
        }
        protected bool AtRestIn(int slot, Rigidbody body, Transform target, float radius = 8)
            => Vector3.Distance(body.worldCenterOfMass, target.position) < radius && body.linearVelocity.magnitude < 1 && body.angularVelocity.magnitude < .4f;
        protected int CameraParticipant(int slot)
        { if(slot<setup.count&&retired[slot])for(int i=0;i<setup.count;i++)if(!retired[i])return i;return slot; }
        protected virtual Vector3 CameraFocus(int slot) => trucks[CameraParticipant(slot)].body.position;
        protected override void Present(float dt)
        {
            if (broadcast == null || trucks == null || trucks.Length == 0) return;
            int focus = 0; for (int i = 0; i < setup.count; i++) if (!retired[i]) { focus = i; break; }
            if (views != null && views.Humans > 0)
            {
                if (this is TrailerPlinkoMatch || this is DominoDerbyMatch)
                {
                    for (int view = 0; view < views.Humans; view++)
                    {
                        int owner = views.owners[view];
                        SpecializedShot(owner, out var center, out var distance);
                        views.views[view].Pose(center, distance, 60, false);
                    }
                }
                else { for (int i = 0; i < trucks.Length; i++) viewFocus[i] = CameraFocus(i); views.Follow(dt, null, viewFocus); }
                return;
            }
            SpecializedShot(focus, out var target, out var offset);
            broadcast.Pose(target, offset, 60, false);
        }
        void SpecializedShot(int focus, out Vector3 target, out Vector3 offset)
        {
            target = CameraFocus(focus); offset = new Vector3(35, 28, -42);
            if (this is TrailerPlinkoMatch plinko) { target = plinko.boardOrigins[focus].position + Vector3.down * (27*plinko.boardScale); offset = new Vector3(10, 4, -95)*plinko.boardScale; }
            else if (this is DominoDerbyMatch domino) { target = (trailers[focus * domino.piecesPerPlayer].Body.position + trailers[(focus + 1) * domino.piecesPerPlayer - 1].Body.position) * .5f; offset = new Vector3(30, 55, -35); }
            else if (this is RoadTrainWreckMatch) { target = (trucks[focus].body.position + trailers[focus * 3 + 2].Body.position) * .5f; offset = new Vector3(55, 35, -25); }
            else if (this is ZeroGHitchMatch) offset = new Vector3(22, 16, -32);
            else if (this is SkyhookSprintMatch) offset = new Vector3(42, 16, -8);
        }

        protected override void StopGame()
        {
            if (Running && !Finished && trucks != null)
                for (int i = 0; i < setup.count; i++)
                {
                    SportsSessionTelemetry.GameplayEvent(gameObject.scene.name, "physical_objectives", setup.players[i].participantId, "Completed objective count", deliveries[i]);
                    SportsSessionTelemetry.GameplayEvent(gameObject.scene.name, "successful_hitches", setup.players[i].participantId, "Aligned native fifth-wheel locks", hitches[i].SuccessfulHitches);
                }
            if (trucks != null) foreach (var truck in trucks) { truck.Power(false); foreach (var body in truck.GetComponentsInChildren<Rigidbody>(true)) body.isKinematic = true; }
            if (trailers != null) foreach (var trailer in trailers) foreach (var body in trailer.GetComponentsInChildren<Rigidbody>(true)) body.isKinematic = true;
        }
        protected override void StopSounds() { if (trucks != null) foreach (var truck in trucks) truck.StopSound(); }
    }
}

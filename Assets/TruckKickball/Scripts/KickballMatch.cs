using System;
using System.Text;
using UnityEngine;
using LWS.TruckBowling;
using LWS.TruckSports;
using LWS.TruckFieldGoal;
using DG.Tweening;

namespace LWS.TruckKickball
{
    public sealed class KickballMatch : SportsEventSession
    {
        public override bool BroadcastParticipantActive(int slot)=>rules.TeamSize>0&&(rules.Team(slot)!=rules.BattingTeam||slot==rules.Batter||rules.Runner(slot));
        public override void DescribeBroadcast(SportsBroadcastAction action,SportsBroadcastFraming framing)
        {
            if(HomeRunCelebrating){action.primary=rules.Batter;action.Objective(agents[rules.Batter].truck.body.worldCenterOfMass,1.5f);action.preferHuman=false;action.reason="SEALED HOME RUN CELEBRATION";return;}
            bool pitch=rules.Phase==KickballPhase.PitchReady||rules.Phase==KickballPhase.Pitch;
            action.primary=pitch?rules.Batter:Carrier>=0?Carrier:framing.Nearest(ball.body.position);
            if(pitch)action.Objective(agents[Pitcher].truck.body.position,1.5f);else action.Objective(ball.body.position,ball.shape.radius);
            action.preferHuman=false;action.reason=pitch?"FIRST-BASE BOOTH / PITCHER AND KICKER":rules.Phase==KickballPhase.HomeRun?"HOME RUN BALL TRACK":"LIVE BALL / INVOLVED FIELDER AND RUNNER";
        }
        public KickballAgent[] agents;
        public KickballBall ball;
        public SportsRallyViews views;
        public KickballViews presentation;
        public SportsMotionReplay replay;
        public SportsChaosWeather atmosphere;
        public SportsWeatherPresets weatherPresets;
        public TMPro.TMP_Text viewChoice, scoreboard;
        public FieldGoalFlightModel contactModel = new FieldGoalFlightModel();
        public readonly KickballRules rules = new KickballRules();
        public static readonly Vector3[] Bases = { Vector3.zero, new Vector3(27, 0, 27), new Vector3(0, 0, 54), new Vector3(-27, 0, 27), Vector3.zero };
        public int Carrier { get; private set; } = -1;
        public int ViewMode { get; private set; }
        public int Pitcher => (1 - rules.BattingTeam) * rules.TeamSize;
        public float Clock { get; private set; }
        public float PlayTime { get; private set; }
        public int PhysicalKicks { get; private set; }
        public int PhysicalCatches { get; private set; }
        public int PhysicalThrows { get; private set; }
        public int PhysicalBaseTouches { get; private set; }
        public TMPro.TMP_Text homeRunBanner;public RectTransform homeRunCard;
        public bool HomeRunCelebrating=>homeRunCelebration>0;
        public int HomeRunCelebrations{get;private set;}
        float homeRunCelebration;Tween bannerTween,cabTween;Transform celebrationCab;Vector3 cabHome;Quaternion cabRotation;
        public override bool SupportsSplitAIViews=>true;public override bool AISplitViewsApplicable=>ViewMode!=0;
        public override string Title => "TRUCK KICKBALL";
        public override int MinimumPlayers => 6;
        public override int MaximumPlayers => 12;
        public override int OptionCount => 16;
        public override int WeatherCount => 1;
        public override string OptionLabel => (3 + option % 4) + " v " + (3 + option % 4) + " · " + Innings + " INNINGS";
        public override bool ReplayControls => replaying;
        public override string SourcePolicy => "BLUE: first half of roster · ORANGE: second half. Up to four humans; all other trucks use AI.\n" + base.SourcePolicy;
        public override string Help => "Six-a-side by default; choose 3v3 through 6v6 and 1/3/5/7 innings. Four local humans maximum. Blue kicks first. Pitcher: steer to aim, throttle/brake select speed, ACTION rolls the real ball. Kicker: drive your bumper into the pitch; approach speed and contact offset determine power, loft and spin. Three strikes retire a kicker; four balls award a walk. Fouls count only up to two strikes.\n\nAfter a fair kick, physically drive HOME → FIRST → SECOND → THIRD → HOME. Runners wait at their bases until the kick. Touch each large pad in order. Defenders secure the ball by actual front-cup contact and carry it visibly. Face a base or teammate and press ACTION to throw. Touch a forced base while holding the ball for a force out; collide with an unsafe runner for a tag. Fly catches retire the kicker; runners must return and tag their original base before advancing. A third force out cancels runs from that play. Three outs swap teams. Tied regulation games continue into extras.\n\nA fair airborne ball clearing the outfield fence is a home run; all entitled runs are credited immediately and defensive play ends for a short celebration. A grounded ball leaving the fenced field awards a ground-rule double, also driven physically. Walks use the same NWH controls to assist forced advancement. Three-a-side bases-loaded play remains live until a runner frees a kicking slot: no ghost runners.\n\nChoose Broadcast, Chase or Taxi Cab. Human-only split screens are default; Show AI Cameras is optional. Cab free look and animated steering respect device ownership. Highlights record actual motion, with angle, slow/fast, rewind and skip; scoring stays locked. Pause/reconnect preserves the match. Genuine 60-second unresolved loose-ball or out-of-world recovery never awards invented runs. Offline only.";
        public override string Header => Title + " · " + (rules.BattingTeam == 0 ? "TOP " : "BOTTOM ") + rules.Inning + " · BLUE " + rules.Scores[0] + " : " + rules.Scores[1] + " ORANGE";
        public override string Status => replaying ? "RECORDED HIGHLIGHT · SEALED RESULT" : transition > 0 ? notice : notice + " · " + rules.Outs + " OUT · " + rules.Balls + " BALL / " + rules.Strikes + " STRIKE";
        public override string Standings => "BLUE " + rules.Scores[0] + "   ORANGE " + rules.Scores[1] + "\n" + (rules.Complete ? "FINAL" : setup.players[rules.Batter].name + " KICKING");
        public override string Results => (rules.Winner == 0 ? "BLUE BANDITS" : "ORANGE OUTLAWS") + " WIN\n\nBLUE " + rules.Scores[0] + " : " + rules.Scores[1] + " ORANGE\n" + rules.Inning + " innings · " + PhysicalKicks + " physical kicks · " + PhysicalCatches + " catches\n" + PhysicalBaseTouches + " ordered base touches";
        public override int ScoreCardCount => 2;
        public override string ScoreCardLabel(int card) => card == 0 ? "BLUE BANDITS" : "ORANGE OUTLAWS";
        public override int ScoreCardSource(int card) => card * Math.Max(3, setup.count / 2);
        public override Color ScoreCardColor(int card) => card == 0 ? new Color(.15f, .65f, 1) : new Color(1, .45f, .12f);
        public override bool TryReadScoreCard(int card, out string value, out string detail)
        { value = rules.Scores[card].ToString(); detail = card == rules.BattingTeam ? rules.Outs + " OUT · KICKING" : "FIELDING"; return true; }
        public override string ControlCaption(int slot) => Controls(slot);
        public override string ViewText(int view)
        {
            int slot = views.owners[view];
            return setup.players[slot].name + " · " + Role(slot) + "\n" + Standings + "\n" + Status + "\n" + Controls(slot);
        }
        public override bool MultiViewHUD => ViewMode != 0 && views.Humans > 0 && !replaying;

        readonly bool[] tags = new bool[12];
        readonly StringBuilder scoreText = new StringBuilder(100);
        System.Random random;
        float transition, pitchReady, catchAvailable, deadTime, nextScoreboard;
        int contactSlot = -1, lastThrower = -1, lastTeam;
        LWS.TruckFieldGoal.FieldGoalCrowd audience;
        int audienceRuns, audienceOuts;
        float contactDistance;
        Vector3 contactPoint, firstGround;
        bool grounded, groundPending, replaying, pitchCrossed;
        string notice = "PREPARE TO PLAY";
        int Innings => new[] { 1, 3, 5, 7 }[Mathf.Clamp(option / 4, 0, 3)];

        protected override void Awake()
        {
            if (!SportsPrefs.HasKey(PreferenceKey)) option = 7;
            base.Awake(); ViewMode = Mathf.Clamp(SportsPrefs.GetInt("TruckKickball.View", 0), 0, 2);
        }
        public override void NormalizeSetup()
        {
            base.NormalizeSetup(); setup.count = (3 + option % 4) * 2;
        }
        public override bool ValidateSources(out string reason)
        {
            if (!base.ValidateSources(out reason)) return false;
            int humans = 0;
            for (int i = 0; i < setup.count; i++) if (setup.players[i].source != BowlingSource.AI) humans++;
            if (humans > 4) { reason = "Kickball supports up to four human drivers; assign remaining seats to AI."; return false; }
            return true;
        }
        public override void RefreshExtraUI()
        {
            if (viewChoice != null) viewChoice.text = "CAMERA · " + new[] { "BROADCAST", "CHASE", "TAXI CAB" }[ViewMode];
            if (menus == null || menus.rows == null) return;
            menus.countText.transform.parent.gameObject.SetActive(false);
            int size = 3 + option % 4;
            for (int i = 0; i < menus.rows.Length; i++)
                menus.rows[i].GetComponent<RectTransform>().anchoredPosition = new Vector2(i < size ? -360 : 360, 170 - (i % size) * 70);
        }
        public override void ExtraAction(string key)
        {
            if (key == "view") { ViewMode = (ViewMode + 1) % 3; SportsPrefs.SetInt("TruckKickball.View", ViewMode); SportsPrefs.Save(); RefreshExtraUI(); }
            if (!replaying) return;
            if (key == "replayangle") replay.Angle = (replay.Angle + 1) % 3;
            if (key == "replayspeed") replay.Speed = replay.Speed == 1 ? .5f : replay.Speed == .5f ? 2 : 1;
            if (key == "replayrewind") replay.Seek(Mathf.Max(0, replay.Cursor - 3));
            if (key == "replayskip") FinishReplay();
        }

        protected override void BeginGame()
        {
            random = new System.Random(Environment.TickCount); rules.Reset(setup.count / 2, Innings);
            audience=GetComponent<LWS.TruckFieldGoal.FieldGoalCrowd>();audience?.ResetAttempt();audienceRuns=audienceOuts=0;
            ClearHomeRunPresentation();HomeRunCelebrations=0;
            Clock = 0; PhysicalKicks = PhysicalCatches = PhysicalThrows = PhysicalBaseTouches = 0;
            if (weatherPresets != null) atmosphere.ConfigurePreset(weatherPresets.Resolve(random.Next()), random.Next());
            else atmosphere.Configure(random.Next(), 0, 1);
            foreach (var agent in agents) agent.truck.grip = 1.2f * atmosphere.Grip;
            replay.End(); replaying = false; lastTeam = rules.BattingTeam;
            replay.Colorize(Array.ConvertAll(setup.players, p => p.color));
            PreparePlate(true); presentation.Layout();
        }

        public string Role(int slot) => slot == Carrier ? "BALL CARRIER · ACTION THROW" : rules.Team(slot) != rules.BattingTeam ? slot == Pitcher && rules.Phase == KickballPhase.PitchReady ? "PITCHER · AIM / POWER / ACTION" : "FIELDER" : rules.Runner(slot) ? "RUN BASES IN ORDER" : slot == rules.Batter ? "KICK WITH YOUR BUMPER" : "DUGOUT";
        public Vector3 BasePosition(int number) => Bases[Mathf.Clamp(number, 0, 4)];
        public bool Safe(int slot) => rules.Base[slot] > 0 && rules.Base[slot] < 4 && FlatDistance(agents[slot].truck.body.position, Bases[rules.Base[slot]]) < 3.2f;
        static float FlatDistance(Vector3 a, Vector3 b) { a.y = b.y = 0; return Vector3.Distance(a, b); }
        Vector3 DefendPosition(int slot)
        {
            int index = slot % rules.TeamSize;
            return index == 0 ? new Vector3(0, .7f, 24) : index == 1 ? Bases[1] + new Vector3(5, .7f, 3) : index == 2 ? Bases[3] + new Vector3(-5, .7f, 3) : index == 3 ? Bases[2] + new Vector3(5, .7f, 3) : new Vector3(index == 4 ? -30 : 30, .7f, 66);
        }
        void PreparePlate(bool newHalf)
        {
            Carrier = -1; lastThrower = -1; contactSlot = -1; contactDistance = float.MaxValue;
            Array.Clear(tags, 0, tags.Length); PlayTime = deadTime = 0; grounded = groundPending = pitchCrossed = false;
            transition = newHalf ? 3 : 1.3f; pitchReady = 0; notice = newHalf ? "NEW HALF · GET READY" : "NEXT KICKER";
            for (int i = 0; i < agents.Length; i++)
            {
                var truck = agents[i].truck; truck.gameObject.SetActive(i < setup.count);
                if (i >= setup.count) continue;
                agents[i].ApplyTeam(rules.Team(i));
                truck.visuals.gameObject.SetActive(true);
                if (rules.Runner(i)) { truck.body.isKinematic = false; truck.Power(true); }
                else if (rules.Team(i) != rules.BattingTeam) truck.Prepare(DefendPosition(i), Quaternion.Euler(0, 180, 0));
                else if (i == rules.Batter) truck.Prepare(new Vector3(0, .7f, -8), Quaternion.identity);
                else truck.Park(new Vector3(rules.BattingTeam == 0 ? -49 : 49, .7f, 4 + i % rules.TeamSize * 5));
                agents[i].blocked = agents[i].reversing = 0; agents[i].Observe();
            }
            SetBallHeld(Pitcher); replay.ResetRecord();
            ball.gameObject.GetComponent<Renderer>().enabled = true;
            Gate();
        }

        void SetBallHeld(int carrier)
        {
            Carrier = carrier; ball.body.isKinematic = true; ball.body.detectCollisions = false;
            ball.body.position = agents[carrier].catchingCup.position; ball.Observe();
        }
        void RollPitch(Vector2 input)
        {
            if (!rules.Pitch()) return;
            float skill = (int)setup.players[Pitcher].difficulty;
            float x = input.x * 5;
            if (setup.players[Pitcher].source == BowlingSource.AI) x = ((float)random.NextDouble() * 2 - 1) * Mathf.Lerp(4.6f, 2.5f, skill / 2);
            float speed = Mathf.Lerp(8, 16, (input.y + 1) * .5f);
            Carrier = -1; lastThrower = Pitcher; catchAvailable = Clock + .7f;
            ball.body.position = new Vector3(0, .7f, 21); ball.body.rotation = Quaternion.identity;
            ball.body.isKinematic = false; ball.body.detectCollisions = true;
            ball.body.linearVelocity = (new Vector3(x, .7f, -2) - ball.body.position).normalized * speed;
            ball.body.angularVelocity = Vector3.right * -speed / ball.shape.radius;
            ball.Observe(); PlayTime = 0; notice = "PITCH IN · DRIVE TO KICK";
        }

        public void QueueBallContact(int slot, Vector3 point)
        {
            if (Carrier >= 0 || replaying || !Live) return;
            bool kick = rules.Phase == KickballPhase.Pitch && slot == rules.Batter;
            bool field = rules.Phase == KickballPhase.Live && rules.Team(slot) != rules.BattingTeam && Clock >= catchAvailable && (slot != lastThrower || Clock > catchAvailable + .25f);
            if (!kick && !field) return;
            float distance = (ball.body.position - agents[slot].catchingCup.position).sqrMagnitude;
            if (field && distance > 2.4f * 2.4f) return;
            if (distance < contactDistance || Mathf.Approximately(distance, contactDistance) && slot < contactSlot)
            { contactSlot = slot; contactDistance = distance; contactPoint = point; }
        }
        public void QueueTag(int slot) { if (rules.Runner(slot)) tags[slot] = true; }
        public void ObserveGroundContact(Vector3 point)
        { if (!grounded && !groundPending) { firstGround = point; groundPending = true; } }
        void ContactOutcome()
        {
            int slot = contactSlot; contactSlot = -1; contactDistance = float.MaxValue;
            if (slot < 0 || Carrier >= 0) return;
            if (rules.Phase == KickballPhase.Pitch && slot == rules.Batter)
            {
                var incoming = agents[slot].IncomingVelocity;
                if (Vector3.ProjectOnPlane(incoming - ball.body.linearVelocity, Vector3.up).magnitude < 1.5f) return;
                if (!rules.Kick()) return;
                contactModel.Kick(incoming, contactPoint - ball.body.worldCenterOfMass, ball.shape.radius * ball.transform.lossyScale.x, out var velocity, out var spin);
                // Replace the already resolved contact velocity once; never add a
                // second full-strength collision impulse on top of PhysX.
                ball.body.linearVelocity = velocity; ball.body.angularVelocity = spin;
                PhysicalKicks++; grounded = groundPending = false; PlayTime = deadTime = 0;
                notice = "FAIR OR FOUL? · RUN TO FIRST"; feedback.Play(0, 0, true); Pulse(slot, .45f, .15f);
            }
            else if (rules.Phase == KickballPhase.Live && rules.Team(slot) != rules.BattingTeam)
            {
                if (!grounded && !groundPending && PlayTime > .12f) { rules.FlyCaught(); notice = "FLY OUT · TAG UP BEFORE ADVANCING"; }
                else notice = setup.players[slot].name + " HAS THE BALL";
                SetBallHeld(slot); PhysicalCatches++; feedback.Play(1, 0, true);
            }
        }

        protected override void Simulate(float dt)
        {
            Clock += dt;
            if (replaying) { if (replay.Step(dt)) FinishReplay(); return; }
            if(homeRunCelebration>0){homeRunCelebration=Mathf.Max(0,homeRunCelebration-dt);if(homeRunCelebration<=0){ClearHomeRunPresentation();FinishPlay();}return;}
            if (transition > 0)
            {
                transition -= dt; foreach (var a in agents) if (a.truck.Powered) a.truck.TickDrive(Vector2.zero);
                HoldBall(); if (transition <= 0) notice = "PITCHER · AIM / POWER / ACTION"; return;
            }
            PlayTime += dt; pitchReady += dt; ContactOutcome(); ObserveBases();
            for (int i = 0; i < setup.count; i++)
            {
                if (tags[i] && Carrier >= 0 && rules.Tag(i, Safe(i))) { notice = setup.players[i].name + " TAGGED OUT"; feedback.Play(3, 0, true); }
                tags[i] = false;
            }
            if (groundPending && rules.Phase == KickballPhase.Live)
            {
                groundPending = false; grounded = true;
                if (firstGround.z < 0 || Mathf.Abs(firstGround.x) > firstGround.z + 1.2f)
                { rules.Foul(); notice = "FOUL · RETURN AND TOUCH YOUR BASE"; Carrier = -1; ParkBall(); }
                else notice = "FAIR BALL · RUN / FIELD / THROW";
            }
            if (rules.Phase == KickballPhase.Pitch && !pitchCrossed)
            {
                if (ball.PreviousPosition.z >= 0 && ball.body.position.z < 0 || PlayTime > 7)
                {
                    float u = Mathf.InverseLerp(ball.PreviousPosition.z, ball.body.position.z, 0);
                    var crossing = Vector3.Lerp(ball.PreviousPosition, ball.body.position, u);
                    bool strike = PlayTime < 7 && Mathf.Abs(crossing.x) <= 3.6f && crossing.y <= 2.2f;
                    pitchCrossed = true; rules.PitchPassed(strike); notice = strike ? "STRIKE" : "BALL";
                    if (rules.Phase == KickballPhase.PitchReady) { PreparePlate(false); return; }
                    if (rules.Phase == KickballPhase.Walk) { SetBallHeld(Pitcher); notice = "WALK · PHYSICAL BASE ADVANCE"; }
                }
            }
            if (rules.Phase == KickballPhase.Live && Carrier < 0)
            {
                var p = ball.body.position; var flat = new Vector2(p.x, p.z - 27);
                if (p.z > 27 && Mathf.Abs(p.x) < p.z && flat.magnitude > 78)
                {
                    if (!grounded && p.y > 3.7f) { if(rules.HomeRun()){BeginHomeRunCelebration();return;} }
                    else { rules.GroundRuleDouble(); notice = "GROUND-RULE DOUBLE"; }
                    ParkBall();
                }
            }
            if (Carrier >= 0 && rules.Phase == KickballPhase.Live)
            {
                for (int number = 1; number <= 4; number++)
                    if (FlatDistance(agents[Carrier].truck.body.position, Bases[number]) <= 3.2f && rules.ForceAt(number) >= 0)
                    { notice = "FORCE OUT AT " + (number == 4 ? "HOME" : number.ToString()); feedback.Play(3, 0, true); }
            }
            if (rules.Outs >= 3 || rules.Phase == KickballPhase.Between) { FinishPlay(); return; }
            if (rules.Phase == KickballPhase.Walk || rules.Phase == KickballPhase.HomeRun)
            { if (rules.ForcedAdvanceComplete()) { FinishPlay(); return; } }
            if (rules.Phase == KickballPhase.FoulReturn && rules.FinishFoulReturn()) { PreparePlate(false); return; }
            bool settledRunners = true;
            for (int i = 0; i < setup.count; i++) if (rules.Runner(i) && (!Safe(i) || rules.ForcedTo[i] > rules.Base[i] || rules.TagRequired[i])) settledRunners = false;
            deadTime = rules.Phase == KickballPhase.Live && Carrier >= 0 && settledRunners ? deadTime + dt : 0;
            if (deadTime > 2.5f) { FinishPlay(); return; }
            if (PlayTime > 60 && rules.Phase == KickballPhase.Live)
            {
                // Explicit simulation-failure outcome, never a fabricated run.
                for (int i = 0; i < setup.count; i++) if (rules.Runner(i) && rules.Base[i] == 0) rules.Tag(i, false);
                notice = "DEAD BALL RECOVERY · NO EXTRA RUNS"; FinishPlay(); return;
            }
            DriveAgents(dt); HoldBall();
            if (!ball.body.isKinematic) ball.body.AddForce(contactModel.AirAcceleration(ball.body.linearVelocity, ball.body.angularVelocity, atmosphere.Flow), ForceMode.Acceleration);
            for (int i = 0; i < setup.count; i++) agents[i].Observe(); ball.Observe();
            replay.Record(Clock, ((1 << setup.count) - 1) | (1 << 12));
        }

        void ObserveBases()
        {
            // Advance front runners first so an actual following arrival can use
            // the vacated base. A crossing must intersect the real six-metre pad.
            for (int progress = 3; progress >= 0; progress--) for (int i = 0; i < setup.count; i++)
            {
                if (!rules.Runner(i) || rules.Base[i] != progress) continue;
                int next = rules.TagRequired[i] ? rules.Base[i] : rules.Base[i] + 1;
                var from = agents[i].PreviousPosition; var to = agents[i].truck.body.position;
                var point = Bases[next]; var delta = to - from; float q = delta.sqrMagnitude;
                float u = q < .00001f ? 0 : Mathf.Clamp01(Vector3.Dot(point + Vector3.up * to.y - from, delta) / q);
                var close = Vector3.Lerp(from, to, u);
                if (Mathf.Abs(close.x - point.x) <= 3.2f && Mathf.Abs(close.z - point.z) <= 3.2f && close.y > -.2f && close.y < 3)
                    if (rules.TouchBase(i, next)) PhysicalBaseTouches++;
            }
        }
        void HoldBall()
        {
            if (Carrier >= 0) ball.body.position = agents[Carrier].catchingCup.position;
        }
        void ParkBall()
        { Carrier = -1; ball.body.isKinematic = true; ball.body.detectCollisions = false; ball.body.position = new Vector3(0, -20, 0); }
        void BeginHomeRunCelebration()
        {
            ParkBall();contactSlot=-1;Array.Clear(tags,0,tags.Length);homeRunCelebration=3.4f;HomeRunCelebrations++;
            notice="HOME RUN · +"+rules.HomeRunRuns+(rules.HomeRunRuns==1?" RUN!":" RUNS!");Gate();
            foreach(var agent in agents){if(!agent.gameObject.activeInHierarchy)continue;agent.truck.Power(false);if(!agent.truck.body.isKinematic){agent.truck.body.linearVelocity=Vector3.zero;agent.truck.body.angularVelocity=Vector3.zero;}agent.truck.body.isKinematic=true;}
            if(homeRunCard!=null){homeRunBanner.text="HOME RUN!\n<size=45%>"+setup.players[rules.Batter].name+" · +"+rules.HomeRunRuns+(rules.HomeRunRuns==1?" RUN":" RUNS")+"</size>";homeRunCard.gameObject.SetActive(true);homeRunCard.localScale=Vector3.one*.7f;bannerTween=homeRunCard.DOScale(1,.28f).SetEase(Ease.OutBack);}
            var cab=agents[rules.Batter].truck.paint;
            if(cab!=null&&cab.transform!=agents[rules.Batter].truck.transform){celebrationCab=cab.transform;cabHome=celebrationCab.localPosition;cabRotation=celebrationCab.localRotation;cabTween=DOTween.Sequence().Join(celebrationCab.DOPunchRotation(new Vector3(-8,0,5),3,8,.45f)).Join(celebrationCab.DOPunchPosition(Vector3.up*.18f,3,8,.45f));}
            audience?.React(true,"HOME RUN",agents[rules.Batter].truck.body.position);audienceRuns=rules.Scores[0]+rules.Scores[1];feedback.Play(5,0,true);Pulse(rules.Batter,.5f,.25f);
        }
        void ClearHomeRunPresentation()
        {
            homeRunCelebration=0;bannerTween?.Kill();cabTween?.Kill();bannerTween=cabTween=null;
            if(celebrationCab!=null){celebrationCab.localPosition=cabHome;celebrationCab.localRotation=cabRotation;celebrationCab=null;}
            if(homeRunCard!=null){homeRunCard.gameObject.SetActive(false);homeRunCard.localScale=Vector3.one;}
        }
        void FinishPlay()
        {
            if (!rules.FinishPlay()) return;
            if (rules.Complete) { EndMatch(); return; }
            replay.Record(Clock + .051f, ((1 << setup.count) - 1) | (1 << 12));
            replay.Begin(Mathf.Max(0, Clock - 7)); replaying = replay.Playing;
            if (!replaying) { PreparePlate(lastTeam != rules.BattingTeam); lastTeam = rules.BattingTeam; return; }
            for (int i = 0; i < setup.count; i++)
            { agents[i].truck.Power(false); agents[i].truck.body.isKinematic = true; agents[i].truck.visuals.gameObject.SetActive(false); }
            ball.gameObject.GetComponent<Renderer>().enabled = false;
        }
        void FinishReplay()
        {
            replay.End(); replaying = false; ball.gameObject.GetComponent<Renderer>().enabled = true;
            PreparePlate(lastTeam != rules.BattingTeam); lastTeam = rules.BattingTeam; presentation.Layout();
        }

        void DriveAgents(float dt)
        {
            int chaser = -1; float nearest = float.MaxValue;
            if (Carrier < 0) for (int i = 0; i < setup.count; i++) if (rules.Team(i) != rules.BattingTeam)
            { float d = (agents[i].truck.body.position - ball.body.position).sqrMagnitude; if (d < nearest) { nearest = d; chaser = i; } }
            for (int i = 0; i < setup.count; i++)
            {
                var agent = agents[i]; var truck = agent.truck;
                if (truck.body.isKinematic) continue;
                bool auto = setup.players[i].source == BowlingSource.AI;
                bool forced = rules.Phase == KickballPhase.HomeRun || rules.Phase == KickballPhase.Walk || rules.Phase == KickballPhase.FoulReturn;
                Vector2 drive = commands[i]; bool action = actions[i]; Vector3 target = truck.body.position;
                if (i == Pitcher && rules.Phase == KickballPhase.PitchReady)
                {
                    truck.TickDrive(Vector2.zero);
                    if (auto && pitchReady > 2 || !auto && action) RollPitch(auto ? new Vector2(0, Mathf.Lerp(-.2f, .6f, (int)setup.players[i].difficulty / 2f)) : drive);
                    continue;
                }
                if (rules.Runner(i))
                {
                    int next = rules.TagRequired[i] ? rules.Base[i] : rules.Base[i] + 1;
                    bool liveRunning = rules.Phase == KickballPhase.Live || forced;
                    bool advance = liveRunning && (rules.TagRequired[i] || rules.ForcedTo[i] > rules.Base[i] || rules.Base[i] == 0 || Carrier < 0 || setup.count == 6);
                    target = advance ? Bases[next] : Bases[rules.Base[i]];
                    if (forced || auto || !liveRunning) drive = Bot(agent, target, dt);
                }
                else if (rules.Team(i) != rules.BattingTeam && rules.Phase == KickballPhase.Live)
                {
                    target = i == Carrier ? ForceTarget() : i == chaser ? ball.body.position + ball.body.linearVelocity * .25f : DefendPosition(i);
                    if (auto) drive = Bot(agent, target, dt);
                    if (i == Carrier && (action || auto && FlatDistance(truck.body.position, target) > 18 && PlayTime > 2 && (Clock % 1.4f) < dt))
                        ThrowBall(auto ? target : truck.body.position + truck.transform.forward * 23);
                }
                else if (i == rules.Batter && rules.Phase == KickballPhase.Pitch)
                {
                    if (auto)
                    {
                        float speed = ball.body.linearVelocity.magnitude;
                        target = new Vector3(Mathf.Clamp(ball.body.position.x, -3, 3), .7f, 2);
                        drive = ball.body.position.z / Mathf.Max(1, speed) < 1.2f ? Bot(agent, target, dt) : Vector2.zero;
                    }
                }
                else drive = Vector2.zero;
                truck.TickDrive(drive);
                if (truck.body.position.y < -8 || new Vector2(truck.body.position.x, truck.body.position.z).magnitude > 170)
                {
                    Vector3 safe = rules.Runner(i) ? Bases[rules.Base[i]] + Vector3.up * .7f : DefendPosition(i);
                    truck.Prepare(safe, Quaternion.identity); agent.Observe(); notice = "OUT-OF-WORLD RECOVERY · NO ADVANCE";
                }
            }
        }
        Vector3 ForceTarget()
        {
            for (int i = 0; i < setup.count; i++) if (rules.Runner(i) && rules.ForcedTo[i] > 0) return Bases[rules.ForcedTo[i]];
            int target = rules.Batter;
            for (int i = 0; i < setup.count; i++) if (rules.Runner(i) && !Safe(i)) target = i;
            return agents[target].truck.body.position;
        }
        void ThrowBall(Vector3 target)
        {
            int owner = Carrier; if (owner < 0 || rules.Phase != KickballPhase.Live) return;
            Vector3 origin = agents[owner].catchingCup.position + agents[owner].truck.transform.forward * .8f;
            Carrier = -1; lastThrower = owner; catchAvailable = Clock + .45f;
            ball.body.position = origin; ball.body.isKinematic = false; ball.body.detectCollisions = true;
            Vector3 direction = Vector3.ProjectOnPlane(target - origin, Vector3.up);
            ball.body.linearVelocity = direction.normalized * Mathf.Clamp(direction.magnitude, 10, 23) + Vector3.up * 2.4f;
            ball.body.angularVelocity = Vector3.Cross(Vector3.up, direction.normalized) * 9;
            PhysicalThrows++; grounded = true; ball.Observe(); feedback.Play(2, 0, true);
        }
        Vector2 Bot(KickballAgent agent, Vector3 target, float dt)
        {
            var truck = agent.truck; var delta = target - truck.body.position; delta.y = 0;
            float speed = Vector3.Dot(truck.body.linearVelocity, truck.transform.forward);
            if (delta.magnitude < 2.0f) return new Vector2(0, Mathf.Abs(speed) > .35f ? -Mathf.Sign(speed) : 0);
            agent.blocked = truck.body.linearVelocity.magnitude < .5f ? agent.blocked + dt : 0;
            if (agent.blocked > 1.8f + truck.slot * .05f) { agent.reversing = .7f; agent.blocked = 0; }
            agent.reversing = Mathf.Max(0, agent.reversing - dt);
            float angle = Vector3.SignedAngle(truck.transform.forward, delta, Vector3.up);
            float gas = Mathf.Abs(angle) > 105 ? -.65f : Mathf.Abs(angle) > 55 ? .45f : 1;
            if (delta.magnitude < 8 && Mathf.Abs(speed) > Mathf.Max(2, delta.magnitude * 1.5f)) gas = -Mathf.Sign(speed);
            float steer = Mathf.Clamp(angle / 35, -1, 1) * (gas < 0 ? -1 : 1);
            if (agent.reversing > 0) { gas = -.7f; steer = truck.slot % 2 == 0 ? .65f : -.65f; }
            return new Vector2(steer, gas);
        }
        protected override void Present(float dt)
        {
            if (!Running) return;
            atmosphere.Tick(dt); presentation.Tick(dt);
            if (scoreboard != null && Time.unscaledTime >= nextScoreboard)
            { nextScoreboard = Time.unscaledTime + .2f; scoreText.Clear().Append("BLUE  ").Append(rules.Scores[0]).Append("   |   ").Append(rules.Scores[1]).Append("  ORANGE\n").Append(rules.BattingTeam == 0 ? "TOP " : "BOTTOM ").Append(rules.Inning).Append("   ").Append(rules.Outs).Append(" OUT"); scoreboard.text = scoreText.ToString();
              int runs=rules.Scores[0]+rules.Scores[1];if(runs>audienceRuns)audience?.React(true,"RUN SCORED",ball.body.position);else if(rules.Outs>audienceOuts)audience?.React(false,"OUT",ball.body.position);audienceRuns=runs;audienceOuts=rules.Outs;
            }
        }
        protected override void StopSounds() { if (agents != null) foreach (var a in agents) a.truck.StopSound(); }
        protected override void StopGame()
        {
            ClearHomeRunPresentation();
            audience?.ResetAttempt();
            if (replay != null) replay.End(); replaying = false;
            if (presentation != null) presentation.Restore();
            if (agents != null) foreach (var agent in agents)
            { agent.truck.Power(false); agent.truck.body.isKinematic = true; }
            if (ball != null) ball.body.isKinematic = true;
        }
    }
}

using System;
using System.Text;
using UnityEngine;
using UnityEngine.AI;
using LWS.TruckSports;
using LWS.TruckBowling;

namespace LWS.TruckHorrorSports
{
    public sealed class HorrorMatch : SportsEventSession
    {
        public HorrorSport sport;
        public SportsEventTruck[] trucks;
        public SportsRallyViews views;
        public HorrorObjective[] objectives;
        public HorrorPursuer antagonist;
        public HauntedCargo[] cargo;
        public Light[] headlights, venueLights;
        public HorrorFootAgent[] footAgents;
        public HorrorSupplyJob supplyJob;
        public CursedGroundingRitual groundingRitual;
        public GraveyardNightServices nightServices;
        public CursedConvoyRoad convoyRoad;
        public HorrorHarvestObjects harvestObjects;
        public DeadEndJunctionPuzzle junctionPuzzle;
        public Transform[] roadRoute;
        public Transform exit;
        public Transform ritualAltar;
        public HorrorAnomaly[] anomalies;
        public AudioSource ambience;
        public AudioSource encounterAudio;
        public AudioClip wind, warning;
        public SportsChaosWeather atmosphere;
        public SportsWeatherPresets presets;
        public float matchSeconds = 240, interactionRadius = 5, interactionSeconds = 2;
        [Min(8)] public float utilityCabApproachRadius = 14;
        public readonly HorrorObjectives rules = new HorrorObjectives();
        public float Age { get; private set; }
        public int Escalation { get; private set; }
        public override string PreferenceKey => base.PreferenceKey + "." + sport;
        public bool Cooperative => sport == HorrorSport.CursedCargo ? option == 1 : sport != HorrorSport.DeadEnd && option == 3;
        public bool SurvivalMode => sport == HorrorSport.DeadEnd ? option == 1 || option == 3 : sport == HorrorSport.CursedCargo ? option == 3 : option == 2;
        bool ConvoyMode => sport == HorrorSport.CursedCargo && option == 1;
        bool FreightSurvival => sport == HorrorSport.CursedCargo && option == 3;
        bool RoadPursuit => sport == HorrorSport.DeadEnd && option == 1;
        bool RoadPuzzle => sport == HorrorSport.DeadEnd && option == 2;
        bool ScarecrowSurvival => sport == HorrorSport.ScarecrowSeason && option == 2;
        bool TaskRush => sport == HorrorSport.GraveyardShift && option == 1;
        bool LastStanding => sport == HorrorSport.DeadEnd && option == 3 || sport == HorrorSport.ScarecrowSeason && option == 2 || sport == HorrorSport.GraveyardShift && option == 2;
        // Competitive point eligibility is separate from recorded participation/failed attempts.
        // Cooperative outcomes remain team results and never create an individual winner.
        public bool ResultEligible(int slot)
        {
            bool intactLoad = sport != HorrorSport.CursedCargo || cargo != null && slot >= 0 && slot < cargo.Length && cargo[slot] != null && cargo[slot].Ready && cargo[slot].Condition > 0;
            return rules.QualifiesCompetitiveResult(sport, option, slot, Finished, slot >= 0 && slot < physicallyExtracted.Length && physicallyExtracted[slot], objectives != null ? objectives.Length : 0, matchSeconds, intactLoad);
        }
        public bool ResultWinner(int slot) => ResultEligible(slot) && ResultPlace(slot) == 1;
        public int ResultPlace(int slot)
        { if (!ResultEligible(slot)) return 0; int place = 1; for (int i = 0; i < setup.count; i++) if (ResultEligible(i) && Score(i) > Score(slot) + .01) place++; return place; }
        public double Score(int slot) => RoadPursuit ? (rules.outcome[slot] != HorrorOutcome.Lost ? 100000 : 0) + rules.distance[slot] + rules.roadGates[slot] * 25 : SurvivalMode ? (rules.outcome[slot] != HorrorOutcome.Lost ? 10000 : 0) + rules.survival[slot] + Objectives(slot) * 5 : rules.Score(slot) - rules.wrongSignals[slot] * 25;
        public int Objectives(int slot) => ConvoyMode ? rules.convoyStops[slot] : FreightSurvival ? rules.containmentStops[slot] : RoadPursuit ? rules.roadGates[slot] : rules.tasks[slot];
        public bool Escaped(int slot) => rules.outcome[slot] == HorrorOutcome.Escaped || rules.outcome[slot] == HorrorOutcome.NightComplete || rules.outcome[slot] == HorrorOutcome.Contained;
        public float SurvivalSeconds(int slot) => rules.survival[slot];
        public bool Successful { get; private set; }
        public string Ending { get; private set; }
        public bool FinalMetricsRecorded { get; private set; }
        readonly NavMeshPath[] paths = new NavMeshPath[12];
        readonly Vector3[][] pathCorners = new Vector3[12][];
        readonly float[] nextPlan = new float[12], work = new float[12], lastDamage = new float[12], stuck = new float[12];
        readonly int[] routeIndex = new int[12], working = new int[12];
        readonly int[] carryingRelic = new int[12];
        readonly int[] securityChecks = new int[12];
        readonly bool[] physicallyExtracted = new bool[12];
        readonly bool[] aiParking = new bool[12];
        readonly bool[] backingFromWard = new bool[12];
        readonly int[] departedWard = new int[12];
        readonly Vector3[] wardDeparture = new Vector3[12], wardReverseDirection = new Vector3[12];
        readonly Vector3[] jobDeparture = new Vector3[12], jobReverseDirection = new Vector3[12];
        readonly float[] jobReverseUntil = new float[12];
        readonly Collider[] aiClearance = new Collider[24];
        readonly bool[] lights = new bool[12];
        readonly float[] rescueWork = new float[12], roadPenaltyUntil = new float[12], nextRoadWarning = new float[12], stationarySeconds = new float[12];
        readonly int[] rescuing = new int[12], lastWrongGate = new int[12];
        readonly Vector3[] lastTravelPosition = new Vector3[12];
        Vector3[] roadPoints = Array.Empty<Vector3>();
        readonly Vector3[] focusPositions = new Vector3[4];
        Vector3[] starts;
        Quaternion[] rotations;
        readonly StringBuilder text = new StringBuilder();
        TMPro.TMP_Text helpCopy;
        string announcement;
        float announcementUntil, nextScare, blackoutUntil;
        bool silent;
        public override int MinimumPlayers => 1;
        public override int MaximumPlayers => 4;
        public override int OptionCount => 4;
        public override string Title => sport == HorrorSport.GraveyardShift ? "GRAVEYARD SHIFT: THE LAST STOP" : sport == HorrorSport.ScarecrowSeason ? "TRUCK HARVEST: SCARECROW SEASON" : sport == HorrorSport.DeadEnd ? "DEAD END: THE LAST HEADLIGHT" : "CURSED CARGO";
        static readonly string[][] modes = {
            new[]{"COMPLETE THE NIGHT", "TASK RUSH", "LAST SHIFT STANDING", "COOPERATIVE NIGHT"},
            new[]{"HARVEST ESCAPE", "RITUAL HUNT", "LAST TRUCK STANDING", "COOPERATIVE NIGHTMARE"},
            new[]{"ESCAPE THE ROAD", "PURSUIT SURVIVAL", "ROUTE PUZZLE CHALLENGE", "LAST HEADLIGHT STANDING"},
            new[]{"CURSED DELIVERY", "HAUNTED CONVOY", "EXORCISM RUN", "SURVIVAL FREIGHT"}
        };
        public override string OptionLabel => modes[(int)sport][Mathf.Clamp(option, 0, 3)];
        public override void RefreshExtraUI()
        {
            if (menus == null || menus.pages == null || menus.pages.Length <= 4 || menus.pages[4] == null) return;
            if (helpCopy == null) foreach (var label in menus.pages[4].GetComponentsInChildren<TMPro.TMP_Text>(true)) if (label.name == "Rules and controls") { helpCopy = label; break; }
            if (helpCopy != null) helpCopy.text = Help;
        }
        public override string Help => "KEEP YOUR HEADLIGHTS ON. TRUST YOUR EYES, NOT EVERY SIGN.\n\n" +
            (sport == HorrorSport.GraveyardShift ? "An ordinary overnight shift: collect deliveries, service pumps, repair the breaker, answer the CB and restart the generator. Stop beside a marked station and HOLD ACTION to complete real work. Tasks unlock the final extraction. The stop changes as your shift progresses. Restored power repels the visitor. Complete every task to finish the night. For the delivery, exit the cab beside your colored parcel and hold ACTION to pick it up. Carry it to your matching shelf and hold ACTION to set it down; supplies count only after resting on that shelf." :
             sport == HorrorSport.ScarecrowSeason ? "Find the ritual objects in the maze, restore the four wards and escape through the farm gate. Corn passages change only while clear of vehicles. The scarecrow moves faster when nobody watches and hears engines. Face it with headlights to slow its approach; use alternate passages to escape. HOLD ACTION beside a ward to activate it. The barn key physically opens a locked shortcut; in Ritual Hunt your carried effigy must return to the barn altar before the next search." :
             sport == HorrorSport.DeadEnd ? "Follow the road, read the numbered exit clues and drive the correct route gates. The black semi is a physical pursuer: brake, turn and use junctions to avoid it. Repeated landmarks and contradictory signs are intentional. Complete the route before taking the last exit. Survival variants award actual time alive." :
             "The load is fighting you. Tow your physically attached trailer between the containment depots. Stop both tractor AND trailer at each ward and HOLD ACTION to contain its cargo. Haunted sway, brakes and shifting weight affect real momentum. Keep the load intact and deliver it to the final depot.") +
            ModeInstructions + "\n\nMovement uses your assigned driving controls. ACTION braces a tipped cursed load using physical restoring torque; otherwise ACTION away from a station toggles headlights. Each human keeps an independent truck and camera. Cooperative variants share objectives and never award individual winner points. Eliminated participants spectate surviving trucks. Rematch restores the whole venue, cargo and encounters. Default match: four minutes.";
        string ModeInstructions => TaskRush ? "\n\nTASK RUSH: bank as many real night-shift jobs as possible before the clock expires. Completing the full task route opens another circuit without moving your truck or clearing earned jobs. No extraction is required. The most completed jobs wins." :
            ConvoyMode ? "\n\nHAUNTED CONVOY: every real rig must stop at each ward before the shared route advances. Park in your own lane. Everyone and every surviving attached load must reach the final receiving area; one driver cannot deliver the whole team." :
            FreightSurvival ? "\n\nSURVIVAL FREIGHT: keep circulating between containment wards. Each physical stop briefly calms the load. Parking indefinitely increases its unrest. Survive the complete clock with the original cargo intact; containment stops break ties." :
            sport == HorrorSport.CursedCargo && option == 2 ? "\n\nEXORCISM RUN: stop beside the ward and hold ACTION to attach its ground lead to your original load. Hold both vehicles steady while the cable settles and you channel the ward. Release ACTION to disconnect. The final successful ward permanently banishes the haunting and completes your attempt there; no final depot delivery is needed." :
            RoadPuzzle ? "\n\nROUTE PUZZLE: take the west service junction. Real fork signs reveal different clues in headlights and darkness. Wrong forks lead through physical detours and cost points. Clear the generator-road closure, then choose Dawn with LIGHTS ON or the hidden Unlit exit with LIGHTS OFF. Gates alternate between LIGHTS ON and LIGHTS OFF. Read the current exit instruction. Passing with the wrong signal incurs a real three-second lockout and a 25-point penalty; correct it without stopping on the pursuer's route." :
            RoadPursuit ? "\n\nPURSUIT SURVIVAL: survive the full clock while covering real road distance and successive gates. Keep circulating; leaving the visible highway for more than a few seconds exposes you to damage." :
            sport == HorrorSport.DeadEnd && option == 3 ? "\n\nLAST HEADLIGHT STANDING: outlast the other trucks. The final surviving truck resolves the round; stay on the highway and dodge the physical pursuer." :
            sport == HorrorSport.ScarecrowSeason && option == 3 ? "\n\nCOOPERATIVE NIGHTMARE: hold ACTION beside a fallen teammate for three seconds to rescue them. Every teammate must be alive and reach the escape area after the shared ritual. AI partners can drive over and perform the same rescue." :
            sport == HorrorSport.ScarecrowSeason && option == 1 ? "\n\nRITUAL HUNT: finding an effigy is only half the job. Carry each found relic back to the barn altar to bank it before searching for the next one." : "";
        public override string Header => Title + " · " + OptionLabel + " · " + Mathf.Max(0, matchSeconds - Age).ToString("F0") + "s";
        public override string Status => Age < announcementUntil ? announcement : "STAGE " + (Escalation + 1) + " · " + (Cooperative ? "SHARED OBJECTIVES" : "COMPLETE OBJECTIVES AND ESCAPE");
        public override string Standings { get { text.Clear(); for (int i = 0; i < setup.count; i++) text.Append(setup.players[i].name).Append(" · ").Append(Objectives(i)).Append(" OBJECTIVES · ").Append(rules.health[i].ToString("F0")).Append("% · ").Append(rules.outcome[i]).Append('\n'); return text.ToString(); } }
        public override string Results
        {
            get
            {
                string summary=Ending+"\n\n"+Standings;
                if(junctionPuzzle!=null&&junctionPuzzle.Active)for(int i=0;i<setup.count;i++)summary+="\n"+setup.players[i].name+" · "+junctionPuzzle.EndingCaption(i);
                if(nightServices!=null)for(int i=0;i<setup.count;i++)summary+="\n"+setup.players[i].name+" · "+nightServices.EndingFor(i);
                return summary;
            }
        }
        public override bool CompactBroadcastStatus => true;
        public override bool CompactViewportHud => true;
        public override bool SupportsSplitAIViews => true;
        public override bool MultiViewHUD => views != null && views.Humans > 0;
        public override string ViewText(int view) => TryViewportOwner(view, out int slot) ? setup.players[slot].name + " · " + ParticipantState(slot) + "\n" + ObjectiveText(slot) : Status;
        public override bool TryReadScoreCard(int slot, out string value, out string detail)
        {
            value = detail = null;
            if (slot < 0 || slot >= setup.count || slot >= rules.Count) return false;
            value = TaskRush ? rules.tasks[slot].ToString() : SurvivalMode ? Clock(rules.survival[slot]) : CompletedTasks(slot) + "/" + objectives.Length;
            detail = ParticipantState(slot) + (rules.outcome[slot] == HorrorOutcome.Active ? " · " + (sport == HorrorSport.CursedCargo ? ConditionText(slot) : rules.health[slot].ToString("F0") + "%") : " · " + Clock(rules.survival[slot]));
            if (!SurvivalMode && Cooperative) detail = "TEAM · " + detail;
            if (TaskRush) detail = "JOBS · " + detail;
            if(nightServices!=null)
            {
                string cue=nightServices.CaptionFor(slot);
                if(!string.IsNullOrEmpty(cue))detail+=" · "+cue;
            }
            return true;
        }
        public override float ParticipantProgress(int slot) => SurvivalMode || slot < 0 || slot >= setup.count || objectives == null || objectives.Length == 0 ? -1 : Mathf.Clamp01((TaskRush ? rules.tasks[slot] % objectives.Length : CompletedTasks(slot)) / (float)objectives.Length);
        public override bool TryReadViewportCard(int view, out int participant, out string score, out string detail)
        {
            score = detail = null;
            if (!TryViewportOwner(view, out participant) || !TryReadScoreCard(participant, out score, out _)) return false;
            detail = ParticipantState(participant) + (rules.outcome[participant] == HorrorOutcome.Active ? "\n" + ConditionText(participant) : "");
            return true;
        }
        public override string ViewportTelemetry(int view)
        {
            if (!TryViewportOwner(view, out int slot)) return null;
            string time = Clock(Mathf.Max(0, matchSeconds - Age)) + " LEFT";
            if (rules.outcome[slot] == HorrorOutcome.Lost) return time + "\nSPECTATING · " + Clock(rules.survival[slot]) + " SURVIVED";
            if (rules.outcome[slot] != HorrorOutcome.Active) return time + " · " + Clock(rules.survival[slot]) + " ALIVE";
            if (sport == HorrorSport.DeadEnd && rules.exposure[slot] > 2) return "RETURN TO THE HIGHWAY!\n" + (rules.exposure[slot] < 6 ? Mathf.CeilToInt(6 - rules.exposure[slot]) + "s BEFORE THE DARKNESS HURTS" : "THE DARKNESS IS HURTING YOU");
            if (RoadPursuit) return time + " · " + (rules.distance[slot] * .000621371f).ToString("F2") + " MI\n" + rules.roadGates[slot] + " GATES · STAY ON THE HIGHWAY";
            if (ConvoyMode && NextObjective(slot) >= 0) return time + " · YOUR BAY " + Vector3.Distance(trucks[slot].body.position, TaskPosition(slot, NextObjective(slot))).ToString("F0") + "m\n" + ObjectiveText(slot);
            return time + " · " + (Cooperative ? "SHARED OBJECTIVES" : "YOUR OBJECTIVES") + "\n" + ObjectiveText(slot);
        }
        public override string ViewportContextPrompt(int view)
        {
            if (!TryViewportOwner(view, out int slot) || rules.outcome[slot] != HorrorOutcome.Active) return null;
            if(nightServices!=null&&nightServices.CanAnswerSecondCall(slot))return nightServices.InteractionFor(slot);
            string pursuitCue = antagonist != null ? antagonist.PursuitCueFor(slot) : null;
            if (!string.IsNullOrEmpty(pursuitCue)) return pursuitCue;
            int rescue = NearbyRescue(slot);
            if (rescue >= 0) return "HOLD ACTION · RESCUE\n" + setup.players[rescue].name;
            int task = NearestTask(slot);
            if (task >= 0 && groundingRitual != null && groundingRitual.Active) return groundingRitual.CaptionFor(slot);
            if (task >= 0) return sport == HorrorSport.CursedCargo && !CargoAtTask(slot, task) ? "STOP WITH YOUR ATTACHED LOAD\nINSIDE THE RECEIVING BAY" : "HOLD ACTION\n" + objectives[task].caption;
            var foot = footAgents != null && slot < footAgents.Length ? footAgents[slot] : null;
            if (foot != null && foot.Outside && Vector3.Distance(foot.transform.position, trucks[slot].body.position) < 4) return "ACTION · ENTER CAB";
            int next = NextObjective(slot);
            if(supplyJob!=null && next==supplyJob.objectiveIndex && foot!=null && foot.Outside)return supplyJob.Instruction(slot);
            if (foot != null && !foot.Outside && next >= 0 && objectives[next].requiresFoot && Vector3.Distance(trucks[slot].body.position, TaskTruckApproach(slot,next)) < FootParkingRadius(next))
                return trucks[slot].body.linearVelocity.magnitude < 1.4f ? "ACTION · EXIT CAB" : "STOP TO EXIT CAB";
            return FreightSurvival && stationarySeconds[slot] > 6 ? "KEEP MOVING · THE LOAD IS WAKING" : null;
        }
        bool TryViewportOwner(int view, out int participant)
        {
            participant = -1;
            if (views == null || view < 0 || view >= views.Humans || view >= views.owners.Length) return false;
            participant = views.owners[view]; return participant >= 0 && participant < setup.count && participant < rules.Count;
        }
        int CompletedTasks(int slot) => Cooperative ? rules.TotalTasks : rules.tasks[slot];
        string ConditionText(int slot) => sport == HorrorSport.CursedCargo && cargo != null && slot < cargo.Length && cargo[slot] != null ? "LOAD " + (cargo[slot].Condition * 100).ToString("F0") + "%" : rules.health[slot].ToString("F0") + "% HEALTH";
        string ParticipantState(int slot)
        {
            switch (rules.outcome[slot])
            {
                case HorrorOutcome.Lost: return "OUT";
                case HorrorOutcome.Escaped: return Cooperative ? "TEAM ESCAPED" : "ESCAPED";
                case HorrorOutcome.NightComplete: return "SHIFT COMPLETE";
                case HorrorOutcome.Contained: return "LOAD CONTAINED";
            }
            if (Finished) return TaskRush ? "SHIFT BANKED" : SurvivalMode ? "SURVIVED" : "UNFINISHED";
            if (sport == HorrorSport.CursedCargo && cargo != null && slot < cargo.Length && cargo[slot] != null && !cargo[slot].Ready) return "HITCH OPEN";
            return footAgents != null && slot < footAgents.Length && footAgents[slot] != null && footAgents[slot].Outside ? "ON FOOT" : "DRIVING";
        }
        static string Clock(float seconds)
        { int whole = Mathf.Max(0, Mathf.FloorToInt(seconds)); return (whole / 60).ToString() + ":" + (whole % 60).ToString("00"); }
        string ObjectiveText(int slot)
        {
            if (ScarecrowSurvival) return "BREAK LINE OF SIGHT · OUTLAST THE OTHER TRUCKS";
            if (sport == HorrorSport.DeadEnd && option == 3) return "OUTLAST THE OTHER HEADLIGHTS · STAY ON THE ROAD";
            if (sport == HorrorSport.ScarecrowSeason && option == 1 && carryingRelic[slot] >= 0) return "RETURN THE RELIC TO THE BARN ALTAR";
            int id = NextObjective(slot);
            if (TaskRush && id >= 0) return "JOB " + (rules.tasks[slot] + 1) + " · " + objectives[id].caption;
            if (RoadPuzzle && id >= 0) return "EXIT " + (id + 1) + " · LIGHTS " + (id % 2 == 0 ? "ON" : "OFF");
            if (ConvoyMode && id >= 0 && rules.ConvoyArrived(slot, id)) return "WARD CONFIRMED · WAIT FOR THE OTHER RIGS";
            if (FreightSurvival && id >= 0) return "CONTAINMENT STOP " + (rules.containmentStops[slot] + 1) + " · " + objectives[id].caption;
            int rescue = RescueTarget(slot); if (rescue >= 0) return "RESCUE " + setup.players[rescue].name + " · HOLD ACTION NEAR THEIR TRUCK";
            if (id < 0 && ConvoyMode) return "EVERY ORIGINAL LOAD TO THE FINAL RECEIVING AREA";
            if (id < 0 && sport == HorrorSport.ScarecrowSeason && option == 3) return "EVERY TEAMMATE TO THE FARM ESCAPE AREA";
            return id >= 0 ? objectives[id].caption + (objectives[id].requiresFoot ? " · EXIT CAB AT THE DOOR" : "") : "REACH THE EXTRACTION";
        }
        public Vector3 PlayerPosition(int slot) => footAgents != null && slot < footAgents.Length && footAgents[slot] != null && footAgents[slot].Outside ? footAgents[slot].transform.position : trucks[slot].body.position;
        public int NextObjective(int slot)
        {
            if (ScarecrowSurvival || sport == HorrorSport.DeadEnd && option == 3) return -1;
            if (RoadPursuit) return objectives.Length > 0 ? rules.roadGates[slot] % objectives.Length : -1;
            if (FreightSurvival) return objectives.Length > 0 ? rules.containmentStops[slot] % objectives.Length : -1;
            for (int i = 0; i < objectives.Length; i++) if (!rules.Done(slot, i) && (objectives[i].requires < 0 || rules.Done(slot, objectives[i].requires))) return i;
            return -1;
        }
        protected override void BeginGame()
        {
            bool requiresInteriorRigs = false;
            foreach (var objective in objectives) requiresInteriorRigs |= objective.requiresFoot;
            if (requiresInteriorRigs)
                for (int slot = 0; slot < setup.count; slot++)
                    if (footAgents == null || slot >= footAgents.Length || footAgents[slot] == null)
                        throw new InvalidOperationException(Title + " has an on-foot objective but no interior interaction rig for participant " + (slot + 1) + ". Assign the participant's HorrorFootAgent in the scene.");
            if (sport == HorrorSport.CursedCargo)
                for (int slot = 0; slot < setup.count; slot++)
                    if (cargo == null || slot >= cargo.Length || cargo[slot] == null || cargo[slot].Body == null || cargo[slot].coupler == null)
                        throw new InvalidOperationException("Cursed Cargo requires the original physical trailer and native hitch for participant " + (slot + 1) + ".");
            if (starts == null) { starts = new Vector3[trucks.Length]; rotations = new Quaternion[trucks.Length]; for (int i = 0; i < trucks.Length; i++) { starts[i] = trucks[i].transform.position; rotations[i] = trucks[i].transform.rotation; } }
            rules.Begin(setup.count, Cooperative); Age = 0; Escalation = 0; nextScare = 18; blackoutUntil = 0; silent = false;
            if (sport == HorrorSport.DeadEnd && (roadRoute == null || roadRoute.Length < 4)) throw new InvalidOperationException("Dead End requires its authored physical highway route.");
            roadPoints = sport == HorrorSport.DeadEnd && roadRoute != null ? new Vector3[roadRoute.Length] : Array.Empty<Vector3>();
            for (int p = 0; p < roadPoints.Length; p++) roadPoints[p] = roadRoute[p].position;
            Successful = false; Ending = null; FinalMetricsRecorded = false; Array.Clear(securityChecks, 0, securityChecks.Length); Array.Clear(physicallyExtracted, 0, physicallyExtracted.Length); announcement = "MIDNIGHT · BEGIN YOUR SHIFT"; announcementUntil = 5;
            for (int i = 0; i < trucks.Length; i++)
            {
                if (footAgents != null && i < footAgents.Length && footAgents[i] != null) footAgents[i].EnterCab();
                if (cargo != null && i < cargo.Length && cargo[i] != null)
                { if (i >= setup.count && cargo[i].coupler != null) cargo[i].coupler.Release(); cargo[i].gameObject.SetActive(i < setup.count); }
                trucks[i].gameObject.SetActive(i < setup.count);
                if (i >= setup.count) continue;
                aiParking[i] = backingFromWard[i] = false; departedWard[i] = -1; jobReverseUntil[i] = 0; paths[i] = new NavMeshPath(); nextPlan[i] = 0; routeIndex[i] = 0; working[i] = -1; carryingRelic[i] = -1; work[i] = 0; stuck[i] = 0; lights[i] = true; lastDamage[i] = -10;
                rescueWork[i] = roadPenaltyUntil[i] = nextRoadWarning[i] = stationarySeconds[i] = 0; rescuing[i] = lastWrongGate[i] = -1; lastTravelPosition[i] = starts[i];
                if (roadPoints.Length > 0) routeIndex[i] = NearestRoadWaypoint(starts[i]) + 1;
                else if (ScarecrowSurvival && objectives.Length > 0) routeIndex[i] = i % objectives.Length;
                foreach (var body in trucks[i].GetComponentsInChildren<Rigidbody>(true)) body.isKinematic = false;
                if (trucks[i].tractorMotor != null) trucks[i].tractorMotor.Configure();
                trucks[i].Prepare(starts[i], rotations[i]); pathCorners[i] = Array.Empty<Vector3>();
                if (cargo != null && i < cargo.Length && cargo[i] != null) cargo[i].ResetCargo();
            }
            groundingRitual?.ResetRitual();
            supplyJob?.ResetJobs(); nightServices?.ResetServices(); harvestObjects?.ResetObjects(); junctionPuzzle?.ResetPuzzle(sport==HorrorSport.DeadEnd&&option==2);
            foreach (var a in anomalies) if (a != null) a.ResetEvent();
            foreach (var task in objectives) { task.ResetObjective(); if (task.securityMonitor != null) task.securityMonitor.ResetFeed(); }
            if (antagonist != null) antagonist.ResetActor(this);
            convoyRoad?.ResetRoute(ConvoyMode);
            if (atmosphere != null && presets != null)
            {
                int seed = UnityEngine.Random.Range(1, int.MaxValue); SportsWeatherPresets.Entry forecast = null;
                if (presets.Selection == SportsWeatherPresets.RandomId)
                { foreach (var entry in presets.entries) if (entry.displayName == "Heavy Clouds" && entry.profile != null && !entry.profile.Disabled) { forecast = entry; break; } }
                atmosphere.ConfigurePreset(forecast ?? presets.Resolve(seed), seed); atmosphere.Pause(false);
            }
            foreach (var lamp in venueLights) if (lamp != null) lamp.enabled = true;
            views.Layout(); views.Replay = false;
            if (ambience != null) { ambience.clip = wind; ambience.loop = true; ambience.volume = settings.sfx * .25f; if (wind != null) ambience.Play(); }
            Gate();
        }
        protected override void Simulate(float dt)
        {
            Age += dt;
            if (atmosphere != null) atmosphere.Tick(Age);
            int completed = rules.TotalTasks;
            int stage = Mathf.Min(4, Mathf.FloorToInt(Age / 45) + completed / Mathf.Max(1, setup.count * 2));
            if (stage > Escalation) { Escalation = stage; TriggerEvent(stage); }
            junctionPuzzle?.Tick(dt);
            bool anyActive = false;
            for (int i = 0; i < setup.count; i++)
            {
                if (rules.outcome[i] != HorrorOutcome.Active) { trucks[i].TickDrive(Vector2.zero); continue; }
                anyActive = true; rules.survival[i] += dt;
                if(junctionPuzzle!=null&&junctionPuzzle.Active)
                {
                    junctionPuzzle.ObserveParticipant(i,lights[i]);int turns=junctionPuzzle.ConsumeWrongTurns(i);
                    if(turns>0){rules.wrongSignals[i]+=turns;roadPenaltyUntil[i]=Age+3;SportsSessionTelemetry.GameplayEvent(gameObject.scene.name,"wrong_physical_fork",setup.players[i].participantId,"Actual wrong-road detour",turns);}
                }
                bool ai = setup.players[i].source == BowlingSource.AI;
                bool cargoRecovery = sport == HorrorSport.CursedCargo && cargo != null && i < cargo.Length && cargo[i] != null
                    && (ai ? Vector3.Dot(cargo[i].Body.rotation * Vector3.up, Vector3.up) < .4f && trucks[i].body.linearVelocity.magnitude < 1.2f : actions[i]) && cargo[i].RequestRecovery();
                if(cargoRecovery)Say(setup.players[i].name+" · BRACING THE CURSED LOAD",3);

                supplyJob?.TickParticipant(i, ai || ActionHeld(i), dt);
                Vector2 drive = ai ? DriveAI(i, dt) : commands[i];
                if (sport == HorrorSport.CursedCargo && cargo != null && i < cargo.Length && !cargo[i].Ready) drive = Vector2.zero;
                var foot = footAgents != null && i < footAgents.Length ? footAgents[i] : null;
                int wanted = NextObjective(i);
                if (foot != null && !foot.Outside && wanted >= 0 && objectives[wanted].requiresFoot && Vector3.Distance(trucks[i].body.position, TaskTruckApproach(i,wanted)) < FootParkingRadius(wanted) && trucks[i].body.linearVelocity.magnitude < 1.4f && (ai || actions[i]))
                {
                    var truck = trucks[i];
                    Vector3 exitPoint = truck.body.position + truck.transform.right * 2.2f + Vector3.up * .5f;
                    if (supplyJob == null || supplyJob.TryCabExit(i, TaskPosition(i, wanted), out exitPoint))
                        foot.LeaveCab(exitPoint, truck.transform.rotation);
                }
                else if (foot != null && foot.Outside && Vector3.Distance(foot.transform.position, trucks[i].body.position) < 4 && (ai ? wanted < 0 || !objectives[wanted].requiresFoot : actions[i]))
                {
                    foot.EnterCab();
                    if (ai && sport == HorrorSport.GraveyardShift)
                    {
                        // Leave the shop or service bay along the space the
                        // player arrived through before turning onto the aisle.
                        jobDeparture[i] = trucks[i].body.position;
                        jobReverseDirection[i] = -Vector3.ProjectOnPlane(trucks[i].transform.forward, Vector3.up).normalized;
                        jobReverseUntil[i] = Age + 9;
                        drive = Vector2.zero;
                    }
                }
                if (foot != null && foot.Outside)
                {
                    trucks[i].HoldForInteraction();
                    if(ai)
                    {
                        bool supply=supplyJob!=null&&wanted==supplyJob.objectiveIndex;
                        bool returningToGraveyardCab = sport == HorrorSport.GraveyardShift && (wanted < 0 || !objectives[wanted].requiresFoot);
                        Vector3 target = returningToGraveyardCab ? GraveyardCabDoor(i)
                            : wanted >= 0 && objectives[wanted].requiresFoot ? TaskPosition(i, wanted) : trucks[i].body.position;
                        if ((returningToGraveyardCab || wanted >= 0 && objectives[wanted].requiresFoot) && paths[i] != null && paths[i].status == NavMeshPathStatus.PathComplete)
                            for(int c=1;c<pathCorners[i].Length;c++)
                            {
                                float cornerDistance=supply?Vector3.ProjectOnPlane(foot.transform.position-pathCorners[i][c],Vector3.up).magnitude:Vector3.Distance(foot.transform.position,pathCorners[i][c]);
                                if(cornerDistance>(supply ? .45f : 1.2f)){target=pathCorners[i][c];break;}
                            }
                        Vector3 local=foot.transform.InverseTransformPoint(target);
                        drive=supplyJob!=null?supplyJob.FootCommand(i,target,supply):new Vector2(Mathf.Clamp(Mathf.Atan2(local.x,local.z)*Mathf.Rad2Deg/45,-1,1),local.magnitude>1.4f?1:0);
                    }
                    foot.Move(drive,dt);
                }
                else trucks[i].TickDrive(drive);
                if (ai && aiParking[i]) HoldAITaskBrake(trucks[i]);
                if (atmosphere != null && !trucks[i].body.isKinematic) { trucks[i].grip = Mathf.Max(.65f, atmosphere.Grip); if (trucks[i].tractorMotor != null) trucks[i].tractorMotor.gripScale = Mathf.Max(.65f, atmosphere.Grip); trucks[i].body.AddForce(atmosphere.Flow * .04f, ForceMode.Acceleration); }
                int task = NearestTask(i);
                int rescue = NearbyRescue(i);
                bool held = ai ? task >= 0 || rescue >= 0 : ActionHeld(i);
                groundingRitual?.ObserveParticipant(i, held, dt);
                // Holding the real ward begins suppressing the haunting while
                // the physical assembly settles; progress still requires both
                // bodies stopped and the ordinary uninterrupted channel time.
                if(sport==HorrorSport.CursedCargo&&task>=0&&held&&cargo[i].Ready&&trucks[i].body.linearVelocity.magnitude<1.4f
                    &&Vector3.Distance(cargo[i].Body.position,TaskPosition(i,task))<interactionRadius+8) cargo[i].Contain(.25f);

                nightServices?.ObserveAction(i,!ai&&ActionHeld(i),dt);
                if (!ai && actions[i] && !cargoRecovery && task < 0 && rescue < 0 && (nightServices==null||!nightServices.CanAnswerSecondCall(i))) lights[i] = !lights[i];
                if (headlights != null && i < headlights.Length && headlights[i] != null) headlights[i].enabled = lights[i];
                if (foot != null && foot.flashlight != null) foot.flashlight.enabled = foot.Outside && lights[i];
                if (rescue >= 0 && held && trucks[i].body.linearVelocity.magnitude < 1.4f)
                {
                    if (rescuing[i] != rescue) { rescuing[i] = rescue; rescueWork[i] = 0; }
                    rescueWork[i] += dt; work[i] = 0; working[i] = -1;
                    if (rescueWork[i] >= 3) CompleteRescue(i, rescue);
                }
                else if (task >= 0 && held && (groundingRitual == null || groundingRitual.AllowsChannel(i, task)) && (foot != null && foot.Outside || trucks[i].body.linearVelocity.magnitude < 1.4f) && CargoAtTask(i, task) && (supplyJob==null || task!=supplyJob.objectiveIndex || supplyJob.Delivered(i)) && (objectives[task].securityMonitor == null || objectives[task].securityMonitor.HasFrame))
                {
                    rescueWork[i] = 0; rescuing[i] = -1;
                    if (working[i] != task) { working[i] = task; work[i] = 0; }
                    work[i] += dt;
                    float duration = sport == HorrorSport.CursedCargo && option == 2 ? objectives[task].seconds * 2 : objectives[task].seconds;
                    if (work[i] >= duration)
                    {
                        if (sport == HorrorSport.ScarecrowSeason && option == 1 && carryingRelic[i] < 0)
                        { carryingRelic[i] = task; work[i] = 0; nextPlan[i] = 0; Say(setup.players[i].name + " FOUND A RELIC · RETURN TO THE BARN ALTAR", 5); SportsSessionTelemetry.GameplayEvent(gameObject.scene.name, "ritual_relic_collected", setup.players[i].participantId, objectives[task].caption, task); }
                        else { CompleteTask(i, task); carryingRelic[i] = -1; nextPlan[i] = 0; }
                    }
                }
                else { work[i] = rescueWork[i] = 0; working[i] = rescuing[i] = -1; }
                harvestObjects?.SetCarried(i,carryingRelic[i]);
                if (sport == HorrorSport.DeadEnd) { UpdateRoadExposure(i, dt); CheckRoadGates(i); }
                if (cargo != null && i < cargo.Length && cargo[i] != null)
                {
                    stationarySeconds[i] = FreightSurvival && cargo[i].Body.linearVelocity.magnitude < 2 ? stationarySeconds[i] + dt : Mathf.Max(0, stationarySeconds[i] - dt * 2);
                    cargo[i].TickHaunting(dt, Escalation, Live, lights[i], (FreightSurvival ? 1 + Mathf.Max(0, stationarySeconds[i] - 6) / 12 : 1)*(convoyRoad!=null&&convoyRoad.Active?convoyRoad.UnrestFor(i):1));
                    if (cargo[i].Condition <= 0 || cargo[i].Body.position.y < -12)
                    { rules.Damage(i, 100); Say(setup.players[i].name + " · THE ORIGINAL LOAD IS LOST", 5); SportsSessionTelemetry.GameplayEvent(gameObject.scene.name, "cargo_lost", setup.players[i].participantId, "physical cargo condition or fall", cargo[i].Condition); }
                }
                if (trucks[i].body.position.y < -12) rules.Damage(i, 100);
                bool groupExtraction = ConvoyMode || sport == HorrorSport.ScarecrowSeason && option == 3;
                if (!TaskRush && !SurvivalMode && rules.outcome[i] == HorrorOutcome.Active && NextObjective(i) < 0 && Vector3.Distance(trucks[i].body.position, ExitPosition(i)) < (groupExtraction ? 8 : 7) && CargoAtExit(i) && (junctionPuzzle==null||!junctionPuzzle.Active||junctionPuzzle.CanExtract(i,lights[i])))
                {
                    if (groupExtraction)
                    {
                        if (TeamAtExit())
                        { for (int p = 0; p < setup.count; p++) { physicallyExtracted[p] = true; rules.outcome[p] = HorrorOutcome.Escaped; ConfirmOutcome("team_escape:" + p, p, Age, "TEAM EXTRACTION"); } FinishNight(); return; }
                        continue;
                    }
                    physicallyExtracted[i] = true;
                    rules.outcome[i] = sport == HorrorSport.GraveyardShift ? HorrorOutcome.NightComplete : sport == HorrorSport.CursedCargo && option == 2 ? HorrorOutcome.Contained : HorrorOutcome.Escaped;
                    ConfirmOutcome("escape:" + i, i, Age, rules.outcome[i].ToString());
                    SportsSessionTelemetry.GameplayEvent(gameObject.scene.name, "escape", setup.players[i].participantId, rules.outcome[i].ToString()+(junctionPuzzle!=null&&junctionPuzzle.Active?" · "+junctionPuzzle.EndingCaption(i):""), Age);
                    if (Cooperative) { Successful = true; for (int p = 0; p < setup.count; p++) if (rules.outcome[p] == HorrorOutcome.Active) rules.outcome[p] = rules.outcome[i]; FinishNight(); return; }
                }
            }
            nightServices?.TickServices(dt);
            convoyRoad?.TickRoad(dt);
            harvestObjects?.TickObjects(dt);
            if (antagonist != null) antagonist.TickActor(dt);
            for (int i = 0; i < anomalies.Length; i++) if (anomalies[i] != null) anomalies[i].TickEvent(this, dt);
            foreach (var lamp in venueLights) if (lamp != null) lamp.enabled = Age >= blackoutUntil;
            bool shouldSilence = sport == HorrorSport.GraveyardShift && Age < blackoutUntil;
            if (ambience != null && silent != shouldSilence) { silent = shouldSilence; if (silent) ambience.Pause(); else ambience.UnPause(); }
            if (Age >= nextScare) { nextScare = Age + 25 + Mathf.Max(0, 5 - Escalation); TriggerEvent(Escalation); }
            int survivors = 0; bool convoyLost = false; for (int i = 0; i < setup.count; i++) { if (rules.outcome[i] == HorrorOutcome.Active) survivors++; convoyLost |= rules.outcome[i] == HorrorOutcome.Lost; }
            if (!anyActive || Age >= matchSeconds || LastStanding && setup.count > 1 && survivors <= 1 && Age > 20 || ConvoyMode && convoyLost) FinishNight();
        }
        void CompleteTask(int slot, int task)
        {
            if (FreightSurvival)
            {
                rules.containmentStops[slot]++; stationarySeconds[slot] = 0; work[slot] = 0; working[slot] = -1;
                cargo[slot].Contain(12);
                ConfirmScore("survival_ward:" + slot + ":" + rules.containmentStops[slot], slot, 5, Score(slot), "LOAD CONTAINED · KEEP MOVING");
                SportsSessionTelemetry.GameplayEvent(gameObject.scene.name, "survival_containment", setup.players[slot].participantId, objectives[task].caption, rules.containmentStops[slot]);
                return;
            }
            if (ConvoyMode)
            {
                work[slot] = 0; working[slot] = -1;
                if (!rules.RegisterConvoyArrival(slot, task)) return;
                cargo[slot].Contain(25);
                SportsSessionTelemetry.GameplayEvent(gameObject.scene.name, "convoy_ward_arrival", setup.players[slot].participantId, objectives[task].caption, rules.convoyStops[slot]);
                Say(setup.players[slot].name + " · WARD CONFIRMED · WAIT FOR THE CONVOY", 4);
                if (rules.AllConvoyArrived(task) && rules.Commit(slot, task))
                { objectives[task].CompleteVisual(true); ConfirmTeamScore("convoy_ward:" + task, "haunted_convoy", "HAUNTED CONVOY", setup.players[0].color, 1, rules.TotalTasks, "EVERY RIG CONTAINED", unit: "WARDS"); Say("ALL RIGS CONTAINED · THE NEXT WARD IS OPEN", 5); }
                return;
            }
            if (!rules.Commit(slot, task)) return;
            groundingRitual?.ObserveCompletedWard(slot, task);
            nightServices?.ObserveCompletedObjective(slot,task);
            if (sport == HorrorSport.CursedCargo && (option == 0 || option == 2) && setup.players[slot].source == BowlingSource.AI)
            {
                // The receiving wards are beside warehouse walls. Pulling a full articulated
                // rig directly through a 150-degree turn jams the cab against the depot.
                // Back out using native controls before attempting the next road leg.
                backingFromWard[slot] = true; departedWard[slot] = task;
                wardDeparture[slot] = trucks[slot].body.position;
                wardReverseDirection[slot] = -Vector3.ProjectOnPlane(trucks[slot].transform.forward, Vector3.up).normalized;
            }
            if (objectives[task].securityMonitor != null && objectives[task].securityMonitor.HasFrame) securityChecks[slot]++;
            work[slot] = 0; working[slot] = -1;
            objectives[task].CompleteVisual(Cooperative);
            Say(setup.players[slot].name + " · " + objectives[task].caption + " COMPLETE", 4);
            ConfirmScore("task:" + slot + ":" + (TaskRush ? rules.taskCycles[slot] + ":" : "") + task, slot, 100, Score(slot), objectives[task].caption);
            SportsSessionTelemetry.GameplayEvent(gameObject.scene.name, "objective_complete", setup.players[slot].participantId, objectives[task].caption, task);
            Pulse(slot, .25f, .18f);
            if (objectives[task].restoresPower) { blackoutUntil = 0; if (antagonist != null) antagonist.Repel(8); }
            if (cargo != null && slot < cargo.Length && cargo[slot] != null) cargo[slot].Contain(20);
            if (TaskRush && NextObjective(slot) < 0 && rules.BeginNextTaskCycle(slot))
            { nextPlan[slot] = 0; Say(setup.players[slot].name + " · SHIFT CIRCUIT COMPLETE · KEEP THE JOBS COMING", 5); }
            if (sport == HorrorSport.CursedCargo && option == 2 && NextObjective(slot) < 0)
            {
                cargo[slot].Exorcise(); rules.outcome[slot] = HorrorOutcome.Contained;
                ConfirmOutcome("exorcised:" + slot, slot, Age, "HAUNTING PERMANENTLY BANISHED");
                SportsSessionTelemetry.GameplayEvent(gameObject.scene.name, "cargo_exorcised", setup.players[slot].participantId, "all physical wards channeled", Age);
            }
        }
        int NearestTask(int slot)
        {
            if (sport == HorrorSport.DeadEnd) return -1;
            if (FreightSurvival)
            { int ward = NextObjective(slot); return ward >= 0 && Vector3.Distance(PlayerPosition(slot), TaskPosition(slot, ward)) < interactionRadius ? ward : -1; }
            if (sport == HorrorSport.ScarecrowSeason && option == 1 && carryingRelic[slot] >= 0)
                return ritualAltar != null && Vector3.Distance(PlayerPosition(slot), ritualAltar.position) < interactionRadius ? carryingRelic[slot] : -1;
            for (int i = 0; i < objectives.Length; i++) if (!rules.Done(slot, i) && (!ConvoyMode || !rules.ConvoyArrived(slot, i)) && (objectives[i].requires < 0 || rules.Done(slot, objectives[i].requires)) && (!objectives[i].requiresFoot || footAgents != null && slot < footAgents.Length && footAgents[slot] != null && footAgents[slot].Outside) && Vector3.Distance(PlayerPosition(slot), TaskPosition(slot, i)) < (objectives[i].requiresFoot ? 2.2f : interactionRadius)) return i;
            return -1;
        }
        Vector3 TaskPosition(int slot, int task) => convoyRoad!=null&&convoyRoad.Active?convoyRoad.WardPosition(slot,task):junctionPuzzle!=null&&junctionPuzzle.Active?RoadGatePosition(task):supplyJob!=null && task==supplyJob.objectiveIndex ? supplyJob.Target(slot) : ConvoyMode ? ConvoyLane(objectives[task].transform.position, slot) : objectives[task].transform.position;
        Vector3 GraveyardCabDoor(int slot)
        {
            Vector3 position = footAgents[slot].transform.position;
            return supplyJob != null && supplyJob.TryCabExit(slot, position, out var door)
                ? door : trucks[slot].body.position;
        }
        Vector3 TaskTruckApproach(int slot,int task) => supplyJob!=null && task==supplyJob.objectiveIndex ? supplyJob.TruckApproach(slot) : objectives[task].truckApproach;
        float FootParkingRadius(int task) => sport == HorrorSport.GraveyardShift &&
            supplyJob != null && task != supplyJob.objectiveIndex ? utilityCabApproachRadius : 8;

        Vector3 ConvoyLane(Vector3 center, int slot) => center + Vector3.right * (center.x < 0 ? 8 * slot : -8 * slot);
        Vector3 ExitPosition(int slot) => convoyRoad!=null&&convoyRoad.Active?convoyRoad.ExitPosition(slot):junctionPuzzle!=null&&junctionPuzzle.Active?junctionPuzzle.ExitPosition(slot):ConvoyMode ? ConvoyLane(exit.position, slot) : sport == HorrorSport.ScarecrowSeason && option == 3 ? exit.position + new Vector3((slot - (setup.count - 1) * .5f) * 4, 0, 7) : exit.position;
        bool CargoAtTask(int slot, int task) => sport != HorrorSport.CursedCargo || cargo != null && slot < cargo.Length && cargo[slot] != null && cargo[slot].Ready && cargo[slot].Condition > 0 && Vector3.Distance(cargo[slot].Body.position, TaskPosition(slot, task)) < interactionRadius + 8 && cargo[slot].Body.linearVelocity.magnitude < 1.5f;
        bool CargoAtExit(int slot) => sport != HorrorSport.CursedCargo || cargo != null && slot < cargo.Length && cargo[slot] != null && cargo[slot].Ready && cargo[slot].Condition > 0 && Vector3.Distance(cargo[slot].Body.position, ExitPosition(slot)) < 16;
        bool TeamAtExit()
        {
            for (int slot = 0; slot < setup.count; slot++)
                if (rules.outcome[slot] == HorrorOutcome.Lost || Vector3.Distance(trucks[slot].body.position, ExitPosition(slot)) >= 8 || !CargoAtExit(slot)) return false;
            return true;
        }
        int RescueTarget(int helper)
        {
            if (sport != HorrorSport.ScarecrowSeason || option != 3 || rules.outcome[helper] != HorrorOutcome.Active) return -1;
            int nearest = -1; float distance = float.MaxValue;
            for (int i = 0; i < setup.count; i++) if (i != helper && rules.outcome[i] == HorrorOutcome.Lost)
            { float d = Vector3.Distance(trucks[helper].body.position, trucks[i].body.position); if (d < distance) { nearest = i; distance = d; } }
            return nearest;
        }
        int NearbyRescue(int helper)
        { int target = RescueTarget(helper); return target >= 0 && Vector3.Distance(trucks[helper].body.position, trucks[target].body.position) < 6.5f ? target : -1; }
        void CompleteRescue(int helper, int rescued)
        {
            rescueWork[helper] = 0; rescuing[helper] = -1;
            if (!rules.RegisterRescue(helper, rescued)) return;
            lastDamage[rescued] = Age;
            var body = trucks[rescued].body; body.WakeUp();
            if (Vector3.Dot(trucks[rescued].transform.up, Vector3.up) < .4f)
            { Vector3 axis = Vector3.Cross(trucks[rescued].transform.up, Vector3.up); if (axis.sqrMagnitude < .01f) axis = trucks[rescued].transform.forward; body.AddTorque(axis.normalized * 3, ForceMode.VelocityChange); }
            antagonist?.Repel(6); Pulse(helper, .35f, .2f); Pulse(rescued, .5f, .3f);
            Say(setup.players[helper].name + " RESCUED " + setup.players[rescued].name, 5);
            SportsSessionTelemetry.GameplayEvent(gameObject.scene.name, "teammate_rescued", setup.players[helper].participantId, "rescued=" + setup.players[rescued].participantId, rules.rescues[helper]);
        }
        int NearestRoadWaypoint(Vector3 position)
        {
            int nearest = 0; float best = float.MaxValue;
            for (int i = 0; i < roadPoints.Length; i++) { float d = Vector3.ProjectOnPlane(roadPoints[i] - position, Vector3.up).sqrMagnitude; if (d < best) { best = d; nearest = i; } }
            return nearest;
        }
        public float DistanceFromHighway(Vector3 position)
        {
            float best = float.MaxValue;
            for (int i = 0; i < roadPoints.Length; i++)
            {
                Vector3 a = roadPoints[i], b = roadPoints[(i + 1) % roadPoints.Length]; a.y = b.y = position.y;
                Vector3 segment = b - a; float t = segment.sqrMagnitude > .001f ? Mathf.Clamp01(Vector3.Dot(position - a, segment) / segment.sqrMagnitude) : 0;
                best = Mathf.Min(best, Vector3.Distance(position, a + segment * t));
            }
            if(junctionPuzzle!=null&&junctionPuzzle.Active)best=Mathf.Min(best,junctionPuzzle.DistanceFromRoad(position));
            return best;
        }
        void UpdateRoadExposure(int slot, float dt)
        {
            Vector3 p = trucks[slot].body.position;
            float distance = Vector3.ProjectOnPlane(p - lastTravelPosition[slot], Vector3.up).magnitude;
            lastTravelPosition[slot] = p;
            bool onRoad = DistanceFromHighway(p) < 22;
            if (onRoad && distance < 5 + 50 * dt) rules.distance[slot] += distance;
            rules.exposure[slot] = onRoad ? Mathf.Max(0, rules.exposure[slot] - dt * 2) : rules.exposure[slot] + dt;
            if (rules.exposure[slot] > 2 && Age >= nextRoadWarning[slot])
            { nextRoadWarning[slot] = Age + 7; Say(setup.players[slot].name + " · THE DARKNESS OFF THE HIGHWAY IS NOT SAFE · RETURN TO THE ROAD", 5); }
            if (rules.exposure[slot] > 6) Hit(slot, 20);
        }
        Vector3 RoadGatePosition(int id)=>junctionPuzzle!=null&&junctionPuzzle.Active?junctionPuzzle.GatePosition(id):objectives[id].transform.position;
        void CheckRoadGates(int slot)
        {
            int id = NextObjective(slot); if (id < 0) return;
            if (Vector3.Distance(trucks[slot].body.position, RoadGatePosition(id)) >= objectives[id].passageRadius || rules.outcome[slot] != HorrorOutcome.Active) return;
            if (RoadPuzzle)
            {
                if(junctionPuzzle!=null&&junctionPuzzle.Active&&!junctionPuzzle.CanAcceptGate(slot,id))return;
                if (lights[slot] != (id % 2 == 0))
                {
                    if (lastWrongGate[slot] != id)
                    { lastWrongGate[slot] = id; rules.wrongSignals[slot]++; roadPenaltyUntil[slot] = Age + 3; Say(setup.players[slot].name + " · WRONG SIGNAL · EXIT " + (id + 1) + " NEEDS LIGHTS " + (id % 2 == 0 ? "ON" : "OFF"), 4); SportsSessionTelemetry.GameplayEvent(gameObject.scene.name, "wrong_exit_signal", setup.players[slot].participantId, "exit=" + (id + 1), rules.wrongSignals[slot]); }
                    return;
                }
                if (Age < roadPenaltyUntil[slot]) return;
            }
            if (RoadPursuit)
            {
                if (rules.RegisterRoadGate(slot, id, objectives.Length))
                { ConfirmScore("pursuit_gate:" + slot + ":" + rules.roadGates[slot], slot, 25, Score(slot), "HIGHWAY GATE CLEARED"); SportsSessionTelemetry.GameplayEvent(gameObject.scene.name, "pursuit_gate", setup.players[slot].participantId, "physical road passage", rules.roadGates[slot]); }
            }
            else CompleteTask(slot, id);
            lastWrongGate[slot] = -1;
        }
        Vector2 DriveAI(int slot, float dt)
        {
            var truck = trucks[slot]; int id = NextObjective(slot);
            aiParking[slot] = false;
            if(convoyRoad!=null&&convoyRoad.TryDriveAI(slot,out var roadDrive,out var park)){aiParking[slot]=park;return roadDrive;}
            if (jobReverseUntil[slot] > 0)
            {
                float clearDistance = Vector3.Dot(truck.body.position - jobDeparture[slot], jobReverseDirection[slot]);
                if (Age < jobReverseUntil[slot] && clearDistance < 5)
                    return new Vector2(0, -.85f);
                jobReverseUntil[slot] = 0;
                stuck[slot] = 0;
                nextPlan[slot] = 0;
            }
            if (sport == HorrorSport.CursedCargo && (option == 0 || option == 2) && backingFromWard[slot])
            {
                var transmission = truck.tractorMotor.vehicle.powertrain.transmission;
                transmission.automaticTransmissionDNRShiftType = NWH.VehiclePhysics2.Powertrain.TransmissionComponent.AutomaticTransmissionDNRShiftType.RequireShiftInput;
                float speed = Vector3.Dot(truck.body.linearVelocity, truck.transform.forward);
                float retreat = Vector3.Dot(truck.body.position - wardDeparture[slot], wardReverseDirection[slot]);
                if (retreat < 18)
                {
                    // A brake request alone may leave NWH's heavy automatic rig in drive/neutral.
                    // Select the real reverse gear only after the original assembly has stopped.
                    if (transmission.Gear >= 0)
                    {
                        aiParking[slot] = true;
                        if (truck.body.linearVelocity.magnitude < .35f) transmission.ShiftInto(-1);
                        return Vector2.zero;
                    }
                    Vector3 reverseTarget = wardDeparture[slot] + wardReverseDirection[slot] * 18;
                    Vector3 bayLocal = truck.transform.InverseTransformPoint(reverseTarget);
                    // NWH's reversing rig turns toward a behind-right goal with positive steer.
                    // The old negative articulation feedback could steer away from the open court.
                    float steer = Mathf.Atan2(bayLocal.x, Mathf.Max(.2f, -bayLocal.z));
                    float requested = -Mathf.Clamp((18 - retreat) * .3f, .2f, 2);
                    return new Vector2(Mathf.Clamp(steer * 1.6f, -.6f, .6f), Mathf.Clamp((requested - speed) * .7f, -.75f, .7f));
                }
                // Stop the actual backing rig before selecting forward for the next road leg.
                if (transmission.Gear <= 0 || truck.body.linearVelocity.magnitude >= .35f)
                {
                    aiParking[slot] = true;
                    if (truck.body.linearVelocity.magnitude < .35f) transmission.ShiftInto(1);
                    return Vector2.zero;
                }
                transmission.automaticTransmissionDNRShiftType = NWH.VehiclePhysics2.Powertrain.TransmissionComponent.AutomaticTransmissionDNRShiftType.Auto;
                backingFromWard[slot] = false; nextPlan[slot] = 0;
            }
            bool carefulDriver = sport == HorrorSport.ScarecrowSeason || sport == HorrorSport.CursedCargo;
            bool outside = footAgents != null && slot < footAgents.Length && footAgents[slot] != null && footAgents[slot].Outside;
            bool graveyardServiceDrive = sport == HorrorSport.GraveyardShift && nightServices != null && !outside;
            Vector3 destination = id < 0 ? ExitPosition(slot) : objectives[id].requiresFoot && !outside ? TaskTruckApproach(slot,id) : TaskPosition(slot, id);
            if (sport == HorrorSport.GraveyardShift && outside && (id < 0 || !objectives[id].requiresFoot))
                destination = GraveyardCabDoor(slot);
            if (sport == HorrorSport.GraveyardShift && !outside && id >= 0 && objectives[id].requiresFoot &&
                supplyJob != null &&
                Vector3.Distance(truck.body.position, destination) < FootParkingRadius(id) &&
                supplyJob.TryCabExit(slot, TaskPosition(slot, id), out _))
            {
                // Stop in a usable service apron rather than insisting on the
                // center of a parking marker. The driver still walks to the task.
                aiParking[slot] = true;
                return Vector2.zero;
            }
            if (sport == HorrorSport.ScarecrowSeason && option == 1 && carryingRelic[slot] >= 0 && ritualAltar != null) destination = ritualAltar.position;
            if (ScarecrowSurvival && objectives.Length > 0)
            {
                int shelter = routeIndex[slot] % objectives.Length;
                bool reachedShelter = Vector3.Distance(truck.body.position, objectives[shelter].transform.position) < 8;
                if (reachedShelter || antagonist != null && Vector3.Distance(truck.body.position, antagonist.body.position) < 22)
                {
                    int bestShelter = shelter; float bestCover = float.MinValue;
                    for (int c = 0; c < objectives.Length; c++) if (!reachedShelter || c != shelter)
                    { Vector3 candidate = objectives[c].transform.position; float cover = antagonist != null ? Vector3.Distance(candidate, antagonist.body.position) - Vector3.Distance(candidate, truck.body.position) * .15f + (Physics.Linecast(antagonist.body.position + Vector3.up, candidate + Vector3.up, 1) ? 35 : 0) : Vector3.Distance(candidate, truck.body.position); if (cover > bestCover) { bestCover = cover; bestShelter = c; } }
                    routeIndex[slot] = bestShelter; if (bestShelter != shelter) nextPlan[slot] = 0;
                }
                destination = objectives[routeIndex[slot] % objectives.Length].transform.position;
            }
            int rescue = RescueTarget(slot); if (rescue >= 0) destination = trucks[rescue].body.position;
            bool nightmareExtraction = sport == HorrorSport.ScarecrowSeason && option == 3 && id < 0 && rescue < 0;
            if (nightmareExtraction && Vector3.ProjectOnPlane(destination - truck.body.position, Vector3.up).sqrMagnitude < 5.76f)
            {
                // Keep a truck in its own farm apron bay until the rest of
                // the crew arrives. Active rescue still takes priority.
                aiParking[slot] = true;
                stuck[slot] = 0;
                return Vector2.zero;
            }
            // Separate approach points remain inside the real interaction zone. The objective itself
            // and its eligibility checks stay authoritative; this only gives drivers room to stop.
            if (carefulDriver && rescue < 0 && !ScarecrowSurvival && !nightmareExtraction)
            {
                Vector3 offset = new Vector3(slot % 2 == 0 ? -2 : 2, 0, slot < 2 ? -1.5f : 1.5f);
                if (NavMesh.SamplePosition(destination + offset, out var approach, 2, NavMesh.AllAreas)
                    && Vector3.ProjectOnPlane(approach.position - destination, Vector3.up).magnitude < interactionRadius - .8f)
                    destination = approach.position;
            }
            if (sport == HorrorSport.CursedCargo && option == 0 && id >= 0 && !OwnsWardApproach(slot, id))
            {
                // A receiving bay is physically single-file, not a four-rig parking space.
                // Wait in the open court rather than blocking a departing trailer's turn.
                Vector3 ward = objectives[id].transform.position;
                destination = ward + new Vector3((ward.x < 0 ? 1 : -1) * (35 + slot * 9), 0, -28 - slot * 8);
                if (Vector3.ProjectOnPlane(destination - truck.body.position, Vector3.up).magnitude < 4)
                { stuck[slot] = 0; aiParking[slot] = true; return Vector2.zero; }
            }
            Vector3 target = destination;
            if(junctionPuzzle!=null&&junctionPuzzle.Active&&junctionPuzzle.TryDriveAI(slot,id,out var branchDrive,out var branchLights))
            {lights[slot]=branchLights;return branchDrive;}
            if (sport == HorrorSport.DeadEnd && roadRoute != null && roadRoute.Length > 0)
            {
                int waypoint = routeIndex[slot] % roadRoute.Length;
                target = roadRoute[waypoint].position;
                if (Vector3.Distance(truck.body.position, target) < 7) routeIndex[slot]++;
                if (!SurvivalMode && id < 0 && Vector3.Distance(truck.body.position, exit.position) < 45) target = ExitPosition(slot);
                if (RoadPuzzle && id >= 0 && Vector3.Distance(truck.body.position, RoadGatePosition(id)) < 35) lights[slot] = id % 2 == 0;
            }
            else if (Age >= nextPlan[slot])
            {
                nextPlan[slot] = Age + .65f;
                if (NavMesh.SamplePosition(PlayerPosition(slot), out var origin, 8, NavMesh.AllAreas) && NavMesh.SamplePosition(destination, out var dest, 8, NavMesh.AllAreas)) { NavMesh.CalculatePath(origin.position, dest.position, NavMesh.AllAreas, paths[slot]); pathCorners[slot] = paths[slot].corners;
                    if (carefulDriver || graveyardServiceDrive)
                        ClearAICorners(pathCorners[slot], truck.tractorMotor != null ? 4.5f : 2.8f);
                }
            }
            if (sport != HorrorSport.DeadEnd && paths[slot] != null && paths[slot].status == NavMeshPathStatus.PathComplete)
            { var corners = pathCorners[slot]; for (int c = 1; c < corners.Length; c++) if (Vector3.Distance(truck.body.position, corners[c]) > 3) { target = corners[c]; break; } }
            if (carefulDriver || graveyardServiceDrive) return DriveObjectiveAI(slot, destination, target, dt);
            float pursuerDistance = antagonist != null ? Vector3.Distance(truck.body.position, antagonist.body.position) : float.MaxValue;
            if (sport == HorrorSport.DeadEnd && pursuerDistance < 25)
            { float side = Vector3.Dot(antagonist.body.position - truck.body.position, truck.transform.right); target += truck.transform.right * (side >= 0 ? -4 : 4); }
            Vector3 local = truck.transform.InverseTransformPoint(target);
            float angle = Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg;
            float throttle = Mathf.Clamp01(1 - Mathf.Abs(angle) / 140);
            if (sport != HorrorSport.DeadEnd && Vector3.Distance(truck.body.position, destination) < interactionRadius * .7f) return new Vector2(0, truck.body.linearVelocity.magnitude > .4f ? -.3f : 0);
            if (truck.body.linearVelocity.magnitude < .5f && (throttle > .2f || (sport != HorrorSport.GraveyardShift || TaskRush) && Mathf.Abs(angle) > 130)) stuck[slot] += dt; else stuck[slot] = Mathf.Max(0, stuck[slot] - dt);
            if (stuck[slot] > 3) { if (stuck[slot] > 5) { stuck[slot] = 0; nextPlan[slot] = 0; } return new Vector2(-Mathf.Sign(angle == 0 ? 1 : angle), -.7f); }
            float skill = (int)setup.players[slot].difficulty;
            float desiredSpeed = 10 + skill * 2;
            if (sport == HorrorSport.DeadEnd) desiredSpeed = pursuerDistance < 65 ? 20 + skill : 14 + skill * 2;
            return new Vector2(Mathf.Clamp(angle / (28 + 4 - skill), -1, 1), Mathf.Min(throttle, truck.body.linearVelocity.magnitude > desiredSpeed ? .2f : 1));
        }
        public string CursedBayDiagnostic(int slot)
        {
            if (sport != HorrorSport.CursedCargo || slot < 0 || slot >= setup.count) return "not an active cursed rig";
            var truck = trucks[slot]; var vehicle = truck.tractorMotor.vehicle; var transmission = vehicle.powertrain.transmission;
            var input = vehicle.input; var load = cargo[slot].trailer; var trailerVehicle = load.Vehicle;
            float tractorWheelBrake = 0, trailerWheelBrake = 0;
            var wheelDetails = new StringBuilder();
            foreach (var wheel in truck.GetComponentsInChildren<NWH.WheelController3D.WheelController>(true))
            {
                tractorWheelBrake = Mathf.Max(tractorWheelBrake, wheel.BrakeTorque);
                wheelDetails.Append(wheel.name).Append(":M=").Append(wheel.MotorTorque.ToString("F1"))
                    .Append(",B=").Append(wheel.BrakeTorque.ToString("F1")).Append(",omega=").Append(wheel.wheel.angularVelocity.ToString("F1"))
                    .Append(",ground=").Append(wheel.IsGrounded).Append(",hit=").Append(wheel.HitCollider != null ? wheel.HitCollider.name : "none").Append(';');
            }
            var engine = vehicle.powertrain.engine;
            var modifierNames = new StringBuilder();
            foreach (var modifier in engine.runModifiers) modifierNames.Append(modifier.Method.DeclaringType != null ? modifier.Method.DeclaringType.Name : "unknown").Append('.').Append(modifier.Method.Name).Append(';');
            foreach (var modifier in engine.powerModifiers) modifierNames.Append(modifier.Method.DeclaringType != null ? modifier.Method.DeclaringType.Name : "unknown").Append('.').Append(modifier.Method.Name).Append(';');
            foreach (var wheel in load.GetComponentsInChildren<NWH.WheelController3D.WheelController>(true)) trailerWheelBrake = Mathf.Max(trailerWheelBrake, wheel.BrakeTorque);
            return "backing=" + backingFromWard[slot] + " ward=" + departedWard[slot] + " retreat=" + Vector3.Dot(truck.body.position - wardDeparture[slot], wardReverseDirection[slot]).ToString("F2")
                + " gear=" + transmission.Gear + " dnr=" + transmission.automaticTransmissionDNRShiftType + " signedSpeed=" + Vector3.Dot(truck.body.linearVelocity, truck.transform.forward).ToString("F2")
                + " heading=" + truck.transform.eulerAngles.y.ToString("F1") + " trailerUpY=" + load.transform.up.y.ToString("F2") + " trailerEulerX=" + load.transform.eulerAngles.x.ToString("F1") + " trailerEulerZ=" + load.transform.eulerAngles.z.ToString("F1") + " articulation=" + Vector3.SignedAngle(truck.transform.forward, load.transform.forward, Vector3.up).ToString("F1")
                + " lod=" + vehicle.activeLODIndex + " camDistance=" + vehicle.vehicleToCamDistance.ToString("F1") + " engineActive=" + engine.IsActive + " engine[running=" + engine.IsRunning + ",ignition=" + engine.ignition + ",stalled=" + engine.IsStalled + ",rpm=" + engine.OutputRPM.ToString("F1") + ",powerKW=" + engine.generatedPower.ToString("F2") + ",outTorque=" + engine.outputTorque.ToString("F1") + ",throttle=" + engine.ThrottlePosition.ToString("F2") + ",modifiers=" + modifierNames + "] wheels[" + wheelDetails + "]"
                + " park=" + aiParking[slot] + " powered=" + truck.Powered + " grounded=" + truck.Grounded + " kinematic=" + truck.body.isKinematic + " nativeEnabled=" + vehicle.enabled
                + " tractorRaw[T=" + input.states.throttleRaw.ToString("F2") + ",B=" + input.states.brakesRaw.ToString("F2") + ",H=" + input.states.handbrakeRaw.ToString("F2") + ",S=" + input.states.steeringRaw.ToString("F2") + "]"
                + " tractorEffective[T=" + input.InputSwappedThrottle.ToString("F2") + ",B=" + input.InputSwappedBrakes.ToString("F2") + ",H=" + input.Handbrake.ToString("F2") + ",wheelBrakeMax=" + tractorWheelBrake.ToString("F1") + ",handbrakeType=" + vehicle.brakes.handbrakeType + "]"
                + " trailer[gear=" + trailerVehicle.powertrain.transmission.Gear + ",T=" + trailerVehicle.input.InputSwappedThrottle.ToString("F2") + ",B=" + trailerVehicle.input.InputSwappedBrakes.ToString("F2") + ",H=" + trailerVehicle.input.Handbrake.ToString("F2") + ",wheelBrakeMax=" + trailerWheelBrake.ToString("F1") + "]";
        }
        bool OwnsWardApproach(int slot, int ward)
        {
            int first = slot;
            for (int other = 0; other < setup.count; other++)
            {
                if (other == slot || rules.outcome[other] != HorrorOutcome.Active) continue;
                if (backingFromWard[other] && departedWard[other] == ward) return false;
                if (NextObjective(other) != ward) continue;
                if (setup.players[other].source != BowlingSource.AI)
                { if (Vector3.Distance(trucks[other].body.position, objectives[ward].transform.position) < 16) return false; }
                else first = Mathf.Min(first, other);
            }
            return first == slot;
        }
        // Truck-sized clearance supplements the authored pedestrian-radius NavMesh. Only
        // intermediate steering corners move; scoring targets, vehicle poses and routes do not.
        void ClearAICorners(Vector3[] corners, float clearance)
        {
            for (int c = 1; c + 1 < corners.Length; c++)
            {
                Vector3 corner = corners[c], candidate = corner;
                int count = Physics.OverlapSphereNonAlloc(corner + Vector3.up, clearance, aiClearance, 1, QueryTriggerInteraction.Ignore);
                for (int n = 0; n < count; n++)
                {
                    var obstacle = aiClearance[n];
                    if (obstacle == null || obstacle is TerrainCollider || obstacle.bounds.max.y < corner.y + 1.2f) continue;
                    Vector3 closest = obstacle.bounds.ClosestPoint(candidate);
                    Vector3 away = Vector3.ProjectOnPlane(candidate - closest, Vector3.up);
                    float distance = away.magnitude;
                    if (distance > .05f && distance < clearance) candidate += away / distance * (clearance - distance);
                }
                if (NavMesh.SamplePosition(candidate, out var adjusted, 2, NavMesh.AllAreas)) corners[c] = adjusted.position;
            }
        }
        Vector2 DriveObjectiveAI(int slot, Vector3 destination, Vector3 target, float dt)
        {
            var truck = trucks[slot];
            float distance = Vector3.ProjectOnPlane(destination - truck.body.position, Vector3.up).magnitude;
            float forwardSpeed = Vector3.Dot(truck.body.linearVelocity, truck.transform.forward);
            bool atTask = NearestTask(slot) >= 0 || NearbyRescue(slot) >= 0;
            if (atTask && distance < 2.8f)
            {
                stuck[slot] = 0; aiParking[slot] = true;
                return new Vector2(0, forwardSpeed > .15f ? -1 : forwardSpeed < -.15f ? 1 : 0);
            }
            Vector3 local = truck.transform.InverseTransformPoint(target);
            float angle = Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg;
            bool native = truck.tractorMotor != null;
            float skill = (int)setup.players[slot].difficulty;
            float cruise = (native ? 8 : 7) + skill * .5f;
            float desired = cruise * Mathf.Lerp(1, .27f, Mathf.Clamp01(Mathf.Abs(angle) / 85));
            if (!ScarecrowSurvival) desired = Mathf.Min(desired, Mathf.Sqrt(2 * 1.8f * Mathf.Max(.05f, distance - 2)));
            float steering = Mathf.Clamp(angle / (native ? 38 : 34), -1, 1);
            bool blocked = false;
            // Yield to actual participant tractors crossing the approach/departure path.
            // This is steering/braking only: no collision suppression or ownership changes.
            for (int other = 0; other < setup.count; other++) if (other != slot && trucks[other].gameObject.activeInHierarchy)
            {
                Vector3 relative = truck.transform.InverseTransformPoint(trucks[other].body.position);
                float lookahead = native ? 12 : 6;
                float width = native ? 3.3f : 2.1f;
                if (relative.z > -.5f && relative.z < lookahead && Mathf.Abs(relative.x) < width)
                {
                    float otherSpeed = trucks[other].body.linearVelocity.magnitude;
                    bool crossing = Vector3.Dot(truck.transform.forward, trucks[other].transform.forward) < .5f;
                    if (!crossing || slot > other || otherSpeed > 1)
                    { desired = Mathf.Min(desired, Mathf.Max(0, relative.z - (native ? 6 : 3)) * .45f); blocked = true; }
                    steering += (relative.x >= 0 ? -.25f : .25f) * (crossing ? 1 : .4f);
                }
            }
            if (truck.body.linearVelocity.magnitude < 1.2f && distance > 4 && (!blocked || Mathf.Abs(angle) > 80)) stuck[slot] += dt;
            else stuck[slot] = Mathf.Max(0, stuck[slot] - dt * .5f);
            if (stuck[slot] > 2.5f)
            {
                if (stuck[slot] > 4.5f) { stuck[slot] = 0; nextPlan[slot] = 0; }
                return new Vector2(-Mathf.Sign(angle == 0 ? slot % 2 == 0 ? 1 : -1 : angle), -.8f);
            }
            // A negative control is a real NWH brake request. Keeping positive throttle above
            // cruise speed previously let rigs overshoot every turn and receiving bay.
            float longitudinal = Mathf.Clamp((desired - forwardSpeed) * .65f, -1, 1);
            return new Vector2(Mathf.Clamp(steering, -1, 1), longitudinal);
        }
        static void HoldAITaskBrake(SportsEventTruck truck)
        {
            if (truck.tractorMotor != null && truck.tractorMotor.vehicle != null)
            {
                var input = truck.tractorMotor.vehicle.input;
                input.states.throttle = input.states.throttleRaw = 0;
                // Native Auto DNR interprets the brake axis as reverse throttle at rest.
                // The handbrake holds the stopped task without feeding the engine.
                input.states.brakes = input.states.brakesRaw = 0;
                input.states.handbrake = input.states.handbrakeRaw = 1;
            }
            else if (truck.wheels != null)
                foreach (var wheel in truck.wheels) if (wheel != null) { wheel.MotorTorque = 0; wheel.BrakeTorque = truck.brakeTorque; }
        }
        void TriggerEvent(int stage)
        {
            if (stage == 0) return;
            if (sport == HorrorSport.GraveyardShift)
            { blackoutUntil = Age + 5 + stage * 2; Say(stage < 3 ? "THE PUMP IS RUNNING. NOBODY IS THERE." : "DISCONNECTED SPEAKERS: DO NOT LET IT IN.", 5); }
            else if (sport == HorrorSport.ScarecrowSeason) Say(stage < 3 ? "THE CORN HAS MOVED. KEEP YOUR LIGHTS ON IT." : "THOSE HEADLIGHTS ARE NOT YOUR TEAMMATE.", 5);
            else if (sport == HorrorSport.DeadEnd) Say(stage < 3 ? "YOU PASSED THIS EXIT ALREADY." : "THE LAST EXIT IS REAL. KEEP DRIVING.", 5);
            else Say("THE LOAD IS AWAKE · REACH A CONTAINMENT WARD", 5);
            if (encounterAudio != null && warning != null) encounterAudio.PlayOneShot(warning, settings.sfx * .45f);
        }
        public void Hit(int slot, float amount)
        {
            if (!Live || slot < 0 || slot >= setup.count || Age - lastDamage[slot] < 2) return;
            lastDamage[slot] = Age; rules.Damage(slot, amount); Pulse(slot, .8f, .3f);
            Say(setup.players[slot].name + " · IMPACT · " + rules.health[slot].ToString("F0") + "%", 3);
            SportsSessionTelemetry.GameplayEvent(gameObject.scene.name, "horror_impact", setup.players[slot].participantId, "physical encounter", amount);
        }
        public bool IsIlluminated(Vector3 position)
        {
            for (int i = 0; i < setup.count; i++) if (lights[i] && rules.outcome[i] == HorrorOutcome.Active)
            { Vector3 delta = position - trucks[i].body.position; if (delta.magnitude < 38 && Vector3.Dot(trucks[i].transform.forward, delta.normalized) > .65f && !Physics.Linecast(trucks[i].body.position + Vector3.up, position + Vector3.up, 1)) return true; }
            return false;
        }
        public void Say(string message, float seconds) { announcement = message; announcementUntil = Age + seconds; }
        void FinishNight()
        {
            if (Finished) return;
            Successful = false; for (int i = 0; i < setup.count; i++) Successful |= Escaped(i);
            Ending = SurvivalMode ? "THE NIGHT ENDS · SURVIVAL AND COMPLETED OBJECTIVES DECIDE" : Successful ? sport == HorrorSport.GraveyardShift ? "DAWN · THE LAST SHIFT IS OVER" : sport == HorrorSport.CursedCargo && option == 2 ? "THE LOAD IS SILENT · CONTAINMENT COMPLETE" : "YOU FOUND THE WAY OUT" : rules.TotalTasks > 0 ? "UNFINISHED BUSINESS · THE ROAD REMEMBERS" : "NO ONE ANSWERED THE LAST CALL";
            if (RoadPursuit) Ending = "THE PURSUIT ENDS · REAL HIGHWAY DISTANCE AND GATES";
            else if (TaskRush) Ending = "THE SHIFT CLOSES · ACTUAL COMPLETED JOBS DECIDE";
            else if (FreightSurvival) Ending = "THE FREIGHT NIGHT ENDS · ORIGINAL LOADS AND CONTAINMENT STOPS";
            else if (ConvoyMode) Ending = Successful ? "EVERY ORIGINAL LOAD ARRIVED · THE CONVOY IS SAFE" : "THE CONVOY DID NOT MAKE THE DELIVERY";
            RecordFinalMetrics();
            EndMatch();
        }
        void RecordFinalMetrics()
        {
            if (FinalMetricsRecorded) return;
            FinalMetricsRecorded = true;
            nightServices?.ReportFinal();
            convoyRoad?.ReportFinal();
            groundingRitual?.ReportFinal();
            if (antagonist != null) antagonist.ReportPursuitCues();
            string context = "match=" + MatchId + ";mode=" + option + ";cooperative=" + (Cooperative ? "true" : "false");
            SportsSessionTelemetry.GameplayEvent(gameObject.scene.name, "horror_final_match_seconds", null, context + ";unit=SEC", Age);
            for (int i = 0; i < setup.count; i++)
            {
                string participant = setup.players[i].participantId;
                bool alive = rules.outcome[i] != HorrorOutcome.Lost;
                string outcome = !alive ? "eliminated" : Cooperative && Successful ? "team_success" : physicallyExtracted[i] ? "escaped" : TaskRush ? "shift_completed" : SurvivalMode ? "survived" : "alive_unfinished";
                string reason = context + ";outcome=" + outcome;
                SportsSessionTelemetry.GameplayEvent(gameObject.scene.name, "horror_final_tasks_completed", participant, reason + ";unit=COUNT", Objectives(i));
                SportsSessionTelemetry.GameplayEvent(gameObject.scene.name, "horror_final_survival_seconds", participant, reason + ";unit=SEC", rules.survival[i]);
                SportsSessionTelemetry.GameplayEvent(gameObject.scene.name, "horror_final_physical_extraction", participant, reason + ";unit=BOOLEAN", physicallyExtracted[i] ? 1 : 0);
                SportsSessionTelemetry.GameplayEvent(gameObject.scene.name, "horror_final_survived", participant, reason + ";unit=BOOLEAN", alive ? 1 : 0);
                SportsSessionTelemetry.GameplayEvent(gameObject.scene.name, "horror_final_failed_objective", participant, reason + ";unit=BOOLEAN", !alive || !TaskRush && !SurvivalMode && !Escaped(i) ? 1 : 0);
                if (sport == HorrorSport.GraveyardShift)
                    SportsSessionTelemetry.GameplayEvent(gameObject.scene.name, "horror_final_owned_security_checks", participant, reason + ";unit=COUNT", securityChecks[i]);
                if (sport == HorrorSport.DeadEnd)
                { SportsSessionTelemetry.GameplayEvent(gameObject.scene.name, "horror_final_highway_metres", participant, reason + ";unit=METRES", rules.distance[i]); SportsSessionTelemetry.GameplayEvent(gameObject.scene.name, "horror_final_wrong_exit_signals", participant, reason + ";unit=COUNT", rules.wrongSignals[i]); }
                if (sport == HorrorSport.ScarecrowSeason && Cooperative)
                    SportsSessionTelemetry.GameplayEvent(gameObject.scene.name, "horror_final_teammates_rescued", participant, reason + ";unit=COUNT", rules.rescues[i]);
                if (cargo != null && i < cargo.Length && cargo[i] != null)
                {
                    SportsSessionTelemetry.GameplayEvent(gameObject.scene.name, "horror_final_haunt_surges", participant, reason + ";unit=COUNT", cargo[i].PhysicalSurges);
                    SportsSessionTelemetry.GameplayEvent(gameObject.scene.name, "horror_final_cargo_condition", participant, reason + ";unit=RATIO", cargo[i].Condition);
                }
            }
        }
        protected override void Present(float dt)
        {
            if (views == null || trucks == null || trucks.Length == 0) return;
            if (Running && !Finished)
            {
                if (views.Humans == 0) for (int i = 0; i < setup.count; i++) if (rules.outcome[i] == HorrorOutcome.Active) { views.Focus = i; break; }
                for (int i = 0; i < trucks.Length; i++) focusPositions[i] = PlayerPosition(i);
                views.Follow(dt, null, focusPositions);
                for (int v = 0; v < views.Humans; v++)
                { int owner = views.owners[v]; if (footAgents != null && owner < footAgents.Length && footAgents[owner] != null && footAgents[owner].Outside) { var foot = footAgents[owner]; views.views[v].Pose(foot.transform.position + Vector3.up * 1.5f + foot.transform.forward * 2, foot.transform.rotation * new Vector3(0, 1, -3.5f), 70); } }
                for (int v = 0; v < views.Humans; v++) if (rules.outcome[views.owners[v]] == HorrorOutcome.Lost)
                {
                    int survivor = -1; for (int i = 0; i < setup.count; i++) if (rules.outcome[i] == HorrorOutcome.Active) { survivor = i; break; }
                    if (survivor >= 0) views.views[v].Pose(trucks[survivor].body.position + Vector3.up, new Vector3(0, 7, -13), 65);
                }
            }
        }
        protected override void StopGame() { groundingRitual?.Freeze(); convoyRoad?.Freeze(); nightServices?.Freeze(); supplyJob?.FreezeJobs(); if (trucks != null) foreach (var t in trucks) if (t != null) t.Power(false); if (antagonist != null) antagonist.SetActive(false); }
        protected override void StopSounds() { groundingRitual?.Silence(); if (antagonist != null) antagonist.SilencePursuitCues(); if (antagonist != null) antagonist.GetComponent<ScarecrowHeadlightLure>()?.Silence(); convoyRoad?.Silence(); nightServices?.Silence(); if (ambience != null) { if (Paused) ambience.Pause(); else ambience.Stop(); } if (encounterAudio != null) { if (Paused) encounterAudio.Pause(); else encounterAudio.Stop(); } if (anomalies != null) foreach (var a in anomalies) if (a != null && a.spatialSound != null) { if (Paused) a.spatialSound.Pause(); else a.spatialSound.Stop(); } }
        protected override void SetPaused(bool paused) { if (ambience != null) { if (paused) ambience.Pause(); else ambience.UnPause(); } if (encounterAudio != null) { if (paused) encounterAudio.Pause(); else encounterAudio.UnPause(); } if (anomalies != null) foreach (var a in anomalies) if (a != null && a.spatialSound != null) { if (paused) a.spatialSound.Pause(); else a.spatialSound.UnPause(); } if (atmosphere != null) atmosphere.Pause(paused); }
    }
}

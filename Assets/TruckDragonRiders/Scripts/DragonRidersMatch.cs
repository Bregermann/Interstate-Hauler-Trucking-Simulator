using System;
using System.Text;
using LWS.TruckBowling;
using LWS.TruckDragonKingdom;
using LWS.TruckSports;
using UnityEngine;

namespace LWS.TruckDragonRiders
{
    // Race semantics only; Malbers owns dragon movement/animation, NWH owns the
    // original trucks, and Sports owns participants/input/results/cameras/UI.
    public sealed partial class DragonRidersMatch : SportsEventSession
    {
        public DragonKingdomWorld world;
        public DragonSpeciesCatalog catalog;
        public DragonRiderRacer[] racers = Array.Empty<DragonRiderRacer>();
        public SportsEventTruck[] trucks = Array.Empty<SportsEventTruck>();
        public SportsDrivingViews drivingViews;
        public string[] selectedSpeciesIds = { "dragon-inferno","dragon-fire","dragon-inferno","dragon-fire" };
        public TMPro.TMP_Text[] speciesLabels = Array.Empty<TMPro.TMP_Text>();
        public DragonRiderGateDefinition[] gates = Array.Empty<DragonRiderGateDefinition>();
        public int fixedSeed;
        public DragonRiderRace rules { get; } = new DragonRiderRace();
        public int MatchSeed { get; private set; }
        public float Clock => rules.Clock;
        public float Countdown { get; private set; }
        public string Announcement { get; private set; }
        readonly Vector3[] positions=new Vector3[4];
        readonly float[] actionHeldSeconds=new float[4];
        readonly int[] aiReapproachGate={-1,-1,-1,-1};
        readonly StringBuilder text=new StringBuilder(1000);
        float noticeUntil;string announcementParticipantId;
        int focus,lastViewMode=-1;
        bool lastAiViews;
        public override string Title=>"TRUCK DRAGON RIDERS";
        public override int MinimumPlayers=>1;
        public override int MaximumPlayers=>4;
        public override int OptionCount=>3;
        public override string OptionLabel=>(option+1)+" LAP"+(option==0?"":"S")+" · SKYFIRE GRAND PRIX";
        public override bool SupportsSplitAIViews=>true;
        public override bool MultiViewHUD=>drivingViews!=null&&!drivingViews.NeedsArena;
        public override bool SupportsRaceCountdown=>true;
        public override float RaceCountdownSeconds=>Countdown>0?Countdown:-1;
        public override float RaceStartSeconds=>Countdown<=0&&Running?Clock:-1;
        public override bool CompactBroadcastStatus=>true;
        public override bool CompactViewportHud=>true;
        public override int CompactScoreColumns=>4;
        public override string Help=>"SKYFIRE GRAND PRIX\nYour actual customized NWH tractor rides an animated dragon through every ordered gate. Clear the configured laps; actual crossings and elapsed time determine results. Inferno trades speed for stamina, while Fire trades endurance for speed and turning.\nKeyboard directional keys: turn and climb/descend. Tap your action to breathe fire; hold it to sprint. Hold forward+back together for the limited air brake; hold left+right together for a controlled dive. Whole-pad: left stick turns/climbs, RT accelerates/sprints, LT brakes, EAST dives, SOUTH attacks. Shared-stick crews use their own stick and trigger; stick click brakes.\nStrong actual collisions or repeated animated hits can dislodge the real truck. Action deploys its physical emergency parachute; the dragon visibly approaches and may grab it at an actual animation contact. A claw carry and visible return to the saddle adds4seconds. After a genuinely failed rescue, an announced last-checkpoint recovery adds12seconds; three failures retire that crew. No timer awards a finish.\nOne to four local human crews, with AI using the same flight, stamina, attack and gate rules. Pause/reconnect preserves the match; rematch resets it. Miles per hour and feet are shown. Offline local play.";
        public override string SourcePolicy=>"Each crew owns its assigned keyboard cluster, whole pad or shared stick. Direction controls flight; the assigned action attacks and deploys the chute. Hold action to sprint; opposing directional chords brake/dive on keyboard.";
        public override string Header=>Title+" · SKYFIRE GRAND PRIX · "+(presentationWeather!=null?presentationWeather.Description:"");
        public override string Status
        {
            get
            {
                if(Countdown>0)return "MOUNTED · "+Mathf.CeilToInt(Countdown);
                if(Running&&!Finished&&!MultiViewHUD){string prompt=SharedHumanRecoveryPrompt();if(prompt!=null)return prompt;}
                return Clock<noticeUntil?Announcement:Finished?"RACE COMPLETE":"ORDERED GATES · "+Clock.ToString("0.0")+" SEC";
            }
        }
        public override string Standings
        {
            get {text.Clear();for(int i=0;i<setup.count;i++)text.Append(ResultPlace(i)).Append("  ").Append(setup.players[i].name).Append(" · ").Append(rules.Passed[i]).Append('/').Append(gates.Length*(option+1)).Append(" GATES").Append(rules.Retired[i]?" · RETIRED":"").Append('\n');return text.ToString();}
        }
        public override string Results
        {
            get {text.Clear();bool winner=false;for(int i=0;i<setup.count;i++)if(rules.Finished(i)&&ResultPlace(i)==1){text.Append(setup.players[i].name).Append(" WINS SKYFIRE GRAND PRIX!\n");winner=true;}if(!winner)text.Append("NO CREW COMPLETED THE PHYSICAL COURSE\n");for(int i=0;i<setup.count;i++)text.Append(ResultPlace(i)).Append(" · ").Append(setup.players[i].name).Append(" · ").Append(rules.Finished(i)?rules.FinishTime[i].ToString("0.00")+" SEC":"RETIRED").Append(" · +").Append(rules.Penalty[i].ToString("0")).Append(" PENALTY SEC\n");return text.ToString();}
        }
        public override string ControlCaption(int slot)=>base.ControlCaption(slot).Replace("ACTION","BREATH / CHUTE · HOLD SPRINT");
        public override bool BroadcastParticipantActive(int slot)=>slot>=0&&slot<setup.count&&!rules.Retired[slot]&&!rules.Finished(slot);
        public override bool ParticipantLocked(int slot)=>slot>=0&&slot<rules.Count&&(rules.Finished(slot)||rules.Retired[slot]);
        public override float ParticipantProgress(int slot)=>slot>=0&&slot<rules.Count?rules.Progress(slot,racers[slot].Position):-1;
        public int ResultPlace(int slot)=>rules.Count>slot?rules.Place(slot,positions):slot+1;
        public override bool TryReadScoreCard(int slot,out string value,out string detail)
        {
            value=detail=null;if(slot<0||slot>=rules.Count)return false;
            value=ResultPlace(slot).ToString();detail=rules.Retired[slot]?"RETIRED":rules.Finished(slot)?rules.FinishTime[slot].ToString("0.00")+" SEC":"LAP "+rules.CurrentLap(slot)+"/"+rules.Laps+" · GATE "+(rules.NextGate(slot)+1)+"/"+rules.GateCount;return true;
        }
        public override bool TryReadViewportCard(int view,out int participant,out string score,out string detail)
            =>TryReadDrivingViewportCard(drivingViews,view,out participant,out score,out detail);
        int Owner(int view)=>drivingViews!=null&&view>=0&&view<drivingViews.rally.Humans?drivingViews.rally.owners[view]:-1;
        static string NeutralPublicAnnouncement(string value,string dislodgementReason)
            =>!string.IsNullOrEmpty(dislodgementReason)&&value==dislodgementReason+" · DEPLOY CHUTE"?dislodgementReason:value;
        static string ResolveOwnedViewportPrompt(string participantId,bool human,DragonRiderPhase phase,bool chuteOpen,string ownNotice,string reason,string eventParticipantId,bool eventActive,string publicEvent)
        {
            if(string.IsNullOrEmpty(participantId))return null;
            if(phase==DragonRiderPhase.Falling||phase==DragonRiderPhase.RescueApproach)
                return chuteOpen?"CHUTE OPEN · DRAGON RESCUE APPROACHING":human?reason+" · DEPLOY CHUTE":reason+" · RESCUE APPROACHING";
            if(phase==DragonRiderPhase.Mounted)return eventActive&&string.Equals(participantId,eventParticipantId,StringComparison.Ordinal)?publicEvent:null;
            return ownNotice;
        }
        static string NamedSharedHumanPrompt(int slot,string name,string participantId,bool human,DragonRiderPhase phase,bool chuteOpen,string notice,string reason)
        {
            if(!human||string.IsNullOrEmpty(participantId)||phase!=DragonRiderPhase.Falling&&phase!=DragonRiderPhase.RescueApproach)return null;
            return "CREW "+(slot+1)+" · "+name+" · "+ResolveOwnedViewportPrompt(participantId,true,phase,chuteOpen,notice,reason,null,false,null);
        }
        string SharedHumanRecoveryPrompt()
        {
            int owner=-1;
            for(int slot=0;slot<setup.count;slot++)
            {
                var racer=racers[slot];if(setup.players[slot].source==BowlingSource.AI||racer.Phase!=DragonRiderPhase.Falling&&racer.Phase!=DragonRiderPhase.RescueApproach)continue;
                if(owner<0)owner=slot;if(string.Equals(setup.players[slot].participantId,announcementParticipantId,StringComparison.Ordinal)){owner=slot;break;}
            }
            if(owner<0)return null;var actor=racers[owner];var player=setup.players[owner];return NamedSharedHumanPrompt(owner,player.name,player.participantId,true,actor.Phase,actor.ChuteOpen,actor.Notice,actor.LastDislodgementReason);
        }
        public override string ViewportContextPrompt(int view)
        {
            int slot=Owner(view);if(slot<0||slot>=setup.count)return null;var actor=racers[slot];var player=setup.players[slot];return ResolveOwnedViewportPrompt(player.participantId,player.source!=BowlingSource.AI,actor.Phase,actor.ChuteOpen,actor.Notice,actor.LastDislodgementReason,announcementParticipantId,Clock<noticeUntil,Announcement);
        }
        public override string ViewportTelemetry(int view)
        {
            int slot=Owner(view);if(slot<0||racers[slot].Species==null)return null;var racer=racers[slot];
            return racer.Species.displayName+" · "+(racer.Dragon.motor.Speed*2.236936f).ToString("0")+" MPH\nSTAMINA "+(100*racer.Budget.Stamina/racer.Budget.Capacity).ToString("0")+"% · BREATH "+racer.AttackReadyIn.ToString("0.0")+"s · "+Clock.ToString("0.0")+"s";
        }
        protected override void BeginGame()
        {
            if(world==null||catalog==null||racers.Length!=4||trucks.Length!=4||gates.Length<8||world.riderStarts.Length<4||drivingViews==null)
                throw new InvalidOperationException("The shared authored kingdom, four actual NWH crews, configured mounts, ordered gates and Sports views are required");
            MatchSeed=fixedSeed!=0?fixedSeed:Guid.NewGuid().GetHashCode();world.ResetWorld(MatchSeed);rules.Begin(setup.count,option+1,gates);
            Countdown=3;Announcement="MOUNT YOUR DRAGONS";announcementParticipantId=null;noticeUntil=0;Array.Clear(actionHeldSeconds,0,4);for(int i=0;i<aiReapproachGate.Length;i++)aiReapproachGate[i]=-1;ResetStraightGateEgress();
            for(int i=0;i<4;i++)
            {
                racers[i].Stop();trucks[i].gameObject.SetActive(i<setup.count);
                if(i>=setup.count){foreach(var actor in racers[i].choices)actor.gameObject.SetActive(false);continue;}
                string preference=SpeciesPreference(i);if(SportsPrefs.HasKey(preference))selectedSpeciesIds[i]=SportsPrefs.GetString(preference);
                var start=world.riderStarts[i];var heading=(gates[0].center-start.position).normalized;
                racers[i].Prepare(selectedSpeciesIds[i],new Pose(start.position,Quaternion.LookRotation(heading,Vector3.up)));
                positions[i]=trucks[i].body.position;
            }
            drivingViews.Layout();ConsumeActions();lastViewMode=drivingViews.Mode;lastAiViews=SportsCameraOptions.ShowAI;
        }
        protected override void Simulate(float dt)
        {
            if(Countdown>0)
            {
                Countdown=Mathf.Max(0,Countdown-dt);
                for(int i=0;i<setup.count;i++)racers[i].Step(default,dt);
                if(Countdown==0){for(int i=0;i<setup.count;i++)racers[i].ReleaseStart();feedback.Play(5,0,true);ConsumeActions();}
                return;
            }
            rules.Tick(dt);
            for(int i=0;i<setup.count;i++)
            {
                var command=setup.players[i].source==BowlingSource.AI?AiCommand(i):HumanCommand(i,dt);
                racers[i].Step(command,dt);positions[i]=racers[i].Position;
            }
            if(rules.Complete)EndMatch();
        }
        DragonRiderCommand HumanCommand(int slot,float dt)
        {
            var player=setup.players[slot];if(!player.released)return new DragonRiderCommand{throttle=.7f};
            bool held=ActionHeld(slot);actionHeldSeconds[slot]=held?actionHeldSeconds[slot]+dt:0;
            var result=new DragonRiderCommand{yaw=commands[slot].x,climb=commands[slot].y,throttle=.75f,attack=actions[slot],boost=actionHeldSeconds[slot]>.35f,parachute=held};
            if(player.source==BowlingSource.Keyboard)
            {
                result.brake=SportsDeviceInput.KeyHeld(player.forward)&&SportsDeviceInput.KeyHeld(player.back);
                result.dive=SportsDeviceInput.KeyHeld(player.left)&&SportsDeviceInput.KeyHeld(player.right);
            }
            else if(player.source==BowlingSource.WholeController&&player.pad!=null)
            {
                result.climb=player.pad.leftStick.ReadValue().y;result.throttle=.55f+player.pad.rightTrigger.ReadValue()*.45f;
                result.boost|=player.pad.rightTrigger.ReadValue()>.85f;result.brake=player.pad.leftTrigger.ReadValue()>.65f;
                result.dive=player.pad.buttonEast.isPressed;result.parachute|=player.pad.buttonWest.isPressed;
            }
            else if(player.pad!=null)
            {result.brake=player.source==BowlingSource.LeftStick?player.pad.leftStickButton.isPressed:player.pad.rightStickButton.isPressed;result.dive=held&&result.climb<-.75f;}
            return result;
        }
        DragonRiderCommand AiCommand(int slot)
        {
            var racer=racers[slot];int next=rules.NextGate(slot);Vector3 center=rules.GateCenter(next),normal=rules.GateRotation(next)*Vector3.forward;
            Vector3 target,flightOrigin=racer.Position;
            if(TryStraightGateEgress(slot,trucks[slot].shape.bounds.center,out target))flightOrigin=trucks[slot].shape.bounds.center;
            else
            {
                Vector3 approach=center-normal*Mathf.Max(60,rules.GateRadius(next)*1.5f);
                if(aiReapproachGate[slot]!=next)aiReapproachGate[slot]=-1;
                // A forward crossing outside the opening is a miss, not progress.
                // V116 proved crews orbiting the old +30m target on the front side
                // indefinitely. Physically return behind the same unpassed gate.
                if(aiReapproachGate[slot]<0&&!rules.NeedsApproach(slot)&&Vector3.Dot(racer.Position-center,normal)>10)
                {aiReapproachGate[slot]=next;rules.ClearPassageApproach(slot);}
                if(aiReapproachGate[slot]==next&&Vector3.Distance(racer.Position,approach)<=Mathf.Max(12,rules.GateRadius(next)*.5f))aiReapproachGate[slot]=-1;
                bool reapproach=aiReapproachGate[slot]==next;
                target=reapproach||rules.NeedsApproach(slot)?approach:center+normal*30;
                }
            Vector3 delta=target-flightOrigin, flat=Vector3.ProjectOnPlane(delta,Vector3.up), physicalHeading=Vector3.ProjectOnPlane(racer.Dragon.motor.transform.forward,Vector3.up);
            // Step integrates the requested flight heading, while Malbers
            // independently turns the actual animal toward that request.
            // Feeding the lagging body angle into that integrator winds it
            // past the gate and creates the observed endless orbit.
            float steeringAngle=Vector3.SignedAngle(racer.FlightHeading,flat,Vector3.up),bodyAngle=Vector3.SignedAngle(physicalHeading,flat,Vector3.up);
            var command=new DragonRiderCommand{yaw=Mathf.Clamp(steeringAngle/55,-1,1),climb=Mathf.Clamp(delta.y/Mathf.Max(25,flat.magnitude)*2,-1,1),throttle=Mathf.Abs(bodyAngle)>70?.4f:.95f,
                boost=flat.magnitude>180&&Mathf.Abs(bodyAngle)<25&&racer.Budget.Stamina>racer.Budget.Capacity*.3f,brake=Mathf.Abs(bodyAngle)>110,parachute=Clock>1};
            for(int i=0;i<setup.count;i++)if(CanAttack(slot,i)&&racer.Dragon.CanObserveTarget(i)&&Vector3.Distance(racer.Dragon.mouth.position,trucks[i].body.worldCenterOfMass)<racer.Dragon.breathRange)
            {command.attack=true;break;}
            return command;
        }
        public bool CanAttack(int attacker,int target)=>Live&&Countdown<=0&&attacker>=0&&attacker<setup.count&&target>=0&&target<setup.count&&attacker!=target
            &&!rules.Finished(attacker)&&!rules.Finished(target)&&!rules.Retired[attacker]&&!rules.Retired[target]&&racers[target].Phase==DragonRiderPhase.Mounted;
        public void GatePassed(int slot)
        {
            int total=rules.Passed[slot];
            ArmStraightGateEgress(slot,total);
            ConfirmScore("aerial-gate:"+slot+":"+total,slot,1,total,"ORDERED AERIAL GATE",SportsScoreTier.Small,"GATES");
            if(rules.Finished(slot))
            {
                racers[slot].Finish();ConfirmOutcome("aerial-finish:"+slot,slot,rules.FinishTime[slot],"SKYFIRE PHYSICAL FINISH");Announce(slot,"FINISHED · "+rules.FinishTime[slot].ToString("0.00")+" SEC");
                feedback.Play(6,0,true);
            }
            else {feedback.Play(4,0,true);Pulse(slot,.1f,.08f);}
        }
        public void Announce(int slot,string value){announcementParticipantId=setup.players[slot].participantId;Announcement=setup.players[slot].name+" · "+NeutralPublicAnnouncement(value,racers[slot].LastDislodgementReason);noticeUntil=Clock+4;Pulse(slot,.18f,.12f);}
        string SpeciesPreference(int slot)=>"DragonRiders.Species.v1."+setup.players[slot].participantId;
        public override void ExtraAction(string key)
        {
            if(key=="drivingview"){drivingViews.Cycle();return;}
            if(!key.StartsWith("mount:",StringComparison.Ordinal)||Running&&!Finished)return;
            if(!int.TryParse(key.Substring(6),out int slot)||slot<0||slot>=4)return;
            var choices=racers[slot].choiceIds;if(choices.Length==0)return;
            int next=(Array.IndexOf(choices,selectedSpeciesIds[slot])+1)%choices.Length;selectedSpeciesIds[slot]=choices[next];
            SportsPrefs.SetString(SpeciesPreference(slot),selectedSpeciesIds[slot]);SportsPrefs.Save();RefreshExtraUI();menus.Dirty();
        }
        public override void RefreshExtraUI()
        {
            for(int i=0;i<speciesLabels.Length&&i<4;i++)if(speciesLabels[i]!=null&&catalog!=null&&catalog.TryGet(selectedSpeciesIds[i],out var selected))
            {var button=speciesLabels[i].GetComponentInParent<Michsky.UI.Heat.ButtonManager>();if(button!=null){button.buttonText="MOUNT "+(i+1)+": "+selected.displayName;button.UpdateUI();}else speciesLabels[i].text=selected.displayName;}
        }
        protected override void Present(float dt)
        {
            if(drivingViews==null||!Running||racers.Length!=4)return;
            if(lastViewMode!=drivingViews.Mode||lastAiViews!=SportsCameraOptions.ShowAI){drivingViews.Layout();lastViewMode=drivingViews.Mode;lastAiViews=SportsCameraOptions.ShowAI;}
            for(int i=0;i<setup.count;i++)racers[i].Present();
            focus=0;for(int i=0;i<setup.count;i++)if(!rules.Finished(i)&&!rules.Retired[i]){focus=i;break;}
            if(drivingViews.NeedsArena)
            {var racer=racers[focus];Vector3 forward=racer.Dragon!=null?racer.Dragon.motor.transform.forward:Vector3.forward;var q=Quaternion.LookRotation(Vector3.ProjectOnPlane(forward,Vector3.up).normalized);drivingViews.rally.broadcast.Pose(racer.Position+Vector3.up*2,q*new Vector3(-20,14,-38),70);return;}
            for(int v=0;v<drivingViews.rally.Humans;v++)
            {
                int owner=drivingViews.rally.owners[v],slot=rules.Finished(owner)||rules.Retired[owner]?focus:owner;var racer=racers[slot];var camera=drivingViews.rally.views[v];
                var heading=Quaternion.LookRotation(Vector3.ProjectOnPlane(trucks[slot].transform.forward,Vector3.up).normalized);
                bool cab=drivingViews.ViewMode(v)==2&&slot==owner&&racer.Phase==DragonRiderPhase.Mounted;
                drivingViews.cabs[v].gameObject.SetActive(cab&&camera.view.enabled);
                if(cab)
                {
                    var truck=trucks[slot];Vector3 offset=drivingViews.cabMounts!=null&&slot<drivingViews.cabMounts.Length&&drivingViews.cabMounts[slot]!=null?truck.transform.InverseTransformPoint(drivingViews.cabMounts[slot].position):new Vector3(-.17f,1.3f,.65f);
                    camera.view.transform.position=truck.body.position+truck.transform.rotation*offset;
                    camera.view.transform.rotation=drivingViews.looks[v].Apply(truck.transform.rotation,setup.players[slot],slot,drivingViews.OwnsMouse(v),Live,SportsLookOptions.Sensitivity,SportsLookOptions.InvertY);
                    camera.view.fieldOfView=78;camera.view.nearClipPlane=.025f;camera.cabSteering?.Bind(truck);
                }
                else camera.Pose(racer.Position+Vector3.up*2,heading*(racer.Phase==DragonRiderPhase.Mounted?new Vector3(-13,10,-27):new Vector3(15,10,-24)),72);
            }
        }
        protected override void SetPaused(bool paused){foreach(var racer in racers)if(racer!=null)racer.Pause(paused);}
        protected override void StopGame(){foreach(var racer in racers)if(racer!=null)racer.SuspendResult();}
        protected override void StopSounds(){foreach(var truck in trucks)if(truck!=null)truck.StopSound();foreach(var racer in racers)if(racer!=null&&racer.Dragon!=null)racer.Dragon.voice?.Stop();}
        protected override void OnDestroy(){foreach(var racer in racers)if(racer!=null)racer.Stop();base.OnDestroy();}
    }
}

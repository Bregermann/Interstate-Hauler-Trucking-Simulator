using System.Linq;
using System.Text;
using UnityEngine;
using LWS.TruckSports;
using LWS.TruckBowling;
using LWS.TruckFieldGoal;
namespace LWS.TruckBallet
{
    public sealed class BalletMatch:SportsEventSession
    {
        public BalletTruckRig[] rigs;
        public SportsEventCamera broadcast;
        public BalletTheater theater;
        public FieldGoalCrowd audience;
        public AudioSource orchestra;
        public readonly BalletRules rules=new BalletRules();
        public float ArrivalSeconds{get;private set;}
        public bool IndoorVenue=>true;
        public override string Title=>"TRUCK BALLET";
        public override int MinimumPlayers=>1;
        public override int MaximumPlayers=>4;
        public override int OptionCount=>1;
        public override string OptionLabel=>"EIGHT-MOVE GRAND GALA";
        public override bool CompactBroadcastStatus=>true;
        public override bool BroadcastStandingsVisible=>false;
        public override bool SupportsSplitAIViews=>true;
        public override bool BroadcastParticipantActive(int slot)=>slot>=0&&slot<setup.count&&rigs[slot].gameObject.activeInHierarchy;
        public override string Header=>"THE GRAND DIESEL THEATER · "+BalletMoves.Names[(int)rules.Move];
        public override string Status=>ArrivalSeconds>0?"TAKE YOUR MARK · "+Mathf.CeilToInt(ArrivalSeconds):"MOVE "+((int)rules.Move+1)+" / 8 · "+Mathf.Max(0,BalletRules.MoveLimit-rules.MoveSeconds).ToString("0")+"s · SETTLE AND HOLD";
        public override string Help=>"GRACE UNDER DIESEL\n\nPerform eight deliberate cues: pirouette, plie, wheelie arabesque, side lean, controlled spin, small hop, balance and flourish. This is a held-pose gala, not a fast note-lane game.\n\nPOSTURE: forward/back pitches; left/right leans. Tap ACTION for TURN/LIFT: left/right adjusts turn speed; forward/back raises/lowers. Release to hold values. Swap to POSTURE to stop a turn.\n\nMatch live targets and hold1.8seconds. Pirouette needs a real full turn, controlled spin a half-turn, flourish a quarter-turn. SMALL HOP needs an actual rise above1.05m followed by a gentle return.\n\nEach cue lasts18seconds. Actual accuracy, stability, timing and smooth control earn points; a missed cue never completes automatically. AI uses the same limits. Highest total wins; ties remain tied.\n\nTheatrical plies lower the whole preserved truck; driving suspension is inactive. Cab, wheels, costume and selected body stay together. Pause/help freezes performance and music. Rematch keeps your identities and customization.";
        public override string Standings=>string.Join("\n",Enumerable.Range(0,setup.count).Select(i=>setup.players[i].name+" · "+rules.score[i].ToString("0")+" PTS · "+rules.completed[i]+" MOVES"));
        public override string Results
        {get{var result=new StringBuilder("THE GRAND DIESEL GALA\n\n");for(int i=0;i<setup.count;i++)result.AppendLine("#"+rules.Place(i)+" "+setup.players[i].name+" · "+rules.score[i].ToString("0")+" PTS · "+rules.completed[i]+" / 8 MOVES HELD");return result.ToString();}}
        public override bool TryReadScoreCard(int card,out string value,out string detail){value=rules.score[card].ToString("0")+" PTS";detail=rules.accuracy[card].ToString("P0")+" · HOLD "+Mathf.Min(BalletRules.HoldRequired,rules.hold[card]).ToString("0.0")+"/1.8s · "+rigs[card].BankLabel;return true;}
        public override void DescribeBroadcast(SportsBroadcastAction action,SportsBroadcastFraming framing){action.primary=theater==null?0:theater.Focus;action.preferHuman=true;action.reason="DELIBERATE BALLET CHOREOGRAPHY · "+BalletMoves.Names[(int)rules.Move];}
        protected override void BeginGame(){rules.Begin(setup.count);ArrivalSeconds=3;for(int i=0;i<rigs.Length;i++)rigs[i].Begin(i<setup.count);audience?.ResetAttempt();theater?.Begin();Gate();if(orchestra!=null){orchestra.time=0;orchestra.Play();}}
        protected override void Simulate(float dt)
        {
            if(ArrivalSeconds>0){ArrivalSeconds=Mathf.Max(0,ArrivalSeconds-dt);return;}
            for(int i=0;i<setup.count;i++)
            {
                bool bot=setup.players[i].source==BowlingSource.AI;var axes=bot?rigs[i].AI(rules.Goal(i)):commands[i];rigs[i].Step(axes,!bot&&actions[i],dt);
                if(rules.Observe(i,rigs[i].Achieved,rigs[i].Previous,dt)){feedback.Play(2,i,true);audience?.React(true,"Actual held ballet move",rigs[i].truck.body.worldCenterOfMass);}
            }
            var prior=rules.Move;if(!rules.Tick(dt))return;
            for(int i=0;i<setup.count;i++)ConfirmOutcome("ballet-held-"+(int)prior+"-"+i,i,rules.completed[i],BalletMoves.Names[(int)prior]+" · TOTAL MOVES HELD","MOVES",roundEnded:true);
            if(rules.Finished){for(int i=0;i<setup.count;i++){if(rules.score[i]>.001f)ConfirmScore("ballet-complete-points-"+i,i,rules.score[i],rules.score[i],"GALA · ACTUAL TIMING, STABILITY & CONTROL",SportsScoreTier.Major,roundEnded:true);if(rules.Place(i)==1)ConfirmOutcome("ballet-gala-champion-"+i,i,rules.score[i],"GRAND DIESEL GALA", "PTS",SportsScoreTier.MatchChampion,matchEnded:true);}EndMatch();}else Gate();
        }
        protected override void Present(float dt){theater?.Present(dt);if(orchestra!=null)orchestra.volume=Live?settings.music*.55f:0;}
        protected override void SetPaused(bool paused){if(orchestra==null)return;if(paused)orchestra.Pause();else if(Running&&!Finished)orchestra.UnPause();}
        protected override void StopGame(){if(rigs!=null)foreach(var rig in rigs)rig?.Stop();theater?.Stop();orchestra?.Stop();}
        protected override void StopSounds(){if(orchestra!=null&&!Paused)orchestra.Stop();}
    }
}

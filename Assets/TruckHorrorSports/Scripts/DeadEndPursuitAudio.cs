using System;
using UnityEngine;
using LWS.TruckSports;
namespace LWS.TruckHorrorSports
{
    /// <summary>Finite, physically located approach cues. Never controls pursuit, player input, damage or results.</summary>
    public sealed class DeadEndPursuitAudio : MonoBehaviour
    {
        public HorrorMatch match;
        public HorrorPursuer actor;
        public AudioSource emitter;
        public AudioClip horn,airBrake;
        readonly float[] nextWarning=new float[4],captionUntil=new float[4];
        readonly int[] warnings=new int[4],cleanPasses=new int[4];
        readonly string[] captions=new string[4];
        int encounter=-1;float minimumGap,encounterHealth,soundUntil,nextBrake;
        bool focused=true,reported;
        public bool Active {get;private set;}
        public int WarningCount {get;private set;}
        public int BrakeCues {get;private set;}
        public int LastWarningOwner {get;private set;}=-1;
        public float LastWarningDistance {get;private set;}
        public float LastWarningClosingSpeed {get;private set;}
        public int WarningsFor(int slot)=>warnings[slot];
        public int CleanPassesFor(int slot)=>cleanPasses[slot];
        public string CaptionFor(int slot)=>Active&&slot>=0&&slot<match.setup.count&&match.Age<captionUntil[slot]?captions[slot]:null;
        public void ResetWarnings()
        {
            Silence();Active=match.sport==HorrorSport.DeadEnd&&(match.option==1||match.option==3);reported=false;encounter=-1;WarningCount=BrakeCues=0;LastWarningOwner=-1;LastWarningDistance=LastWarningClosingSpeed=nextBrake=0;
            Array.Clear(nextWarning,0,nextWarning.Length);Array.Clear(captionUntil,0,captionUntil.Length);Array.Clear(warnings,0,warnings.Length);Array.Clear(cleanPasses,0,cleanPasses.Length);Array.Clear(captions,0,captions.Length);
        }
        public void TickWarnings(float dt)
        {
            if(!Active||!match.Live||!focused){Silence();return;}
            emitter.volume=match.settings.master>0?match.settings.sfx*.6f:0;
            int target=actor.Target;
            if(target<0||target>=match.setup.count||match.rules.outcome[target]!=HorrorOutcome.Active||actor.State==HorrorPursuitState.Watching||actor.State==HorrorPursuitState.Repelled)
            {if(match.Age>=soundUntil)Silence();ObserveClosePass();return;}
            Vector3 player=match.PlayerPosition(target),separation=player-actor.body.position;float distance=separation.magnitude;
            bool visible=!Physics.Linecast(actor.body.position+Vector3.up*2.2f,player+Vector3.up*2.2f,1,QueryTriggerInteraction.Ignore);
            // Walls/tunnel occlusion and intervals without an actual approach are
            // intentionally silent. Do not warn about an enemy through scenery.
            if(!visible||match.Age>=soundUntil)Silence();
            ObserveClosePass();
            float closing=distance>.01f?Vector3.Dot(actor.body.linearVelocity-match.trucks[target].body.linearVelocity,separation/distance):0;
            if(visible&&distance>14&&distance<105&&closing>1&&match.Age>=nextWarning[target]&&warnings[target]<6&&!emitter.isPlaying)
            {
                nextWarning[target]=match.Age+28;warnings[target]++;WarningCount++;LastWarningOwner=target;LastWarningDistance=distance;LastWarningClosingSpeed=closing;
                Vector3 relative=match.trucks[target].transform.InverseTransformPoint(actor.body.position);
                string direction=relative.z<0?"HEADLIGHTS CLOSING BEHIND":Mathf.Abs(relative.x)>Mathf.Abs(relative.z)?relative.x<0?"THE BLACK SEMI IS CLOSING FROM YOUR LEFT":"THE BLACK SEMI IS CLOSING FROM YOUR RIGHT":"ONCOMING HEADLIGHTS";
                captions[target]=direction+" · KEEP AN ESCAPE ROUTE";captionUntil[target]=match.Age+5;
                Play(horn,1.1f);match.Say(match.setup.players[target].name+" · "+captions[target],4);
                SportsSessionTelemetry.GameplayEvent(gameObject.scene.name,"dead_end_physical_approach_warning",match.setup.players[target].participantId,"actual_visible_gap_m="+distance.ToString("F1")+";direction="+direction,closing);
                encounter=target;minimumGap=distance;encounterHealth=match.rules.health[target];
            }
            // A reverse/brake command against genuine forward motion is the
            // existing wheel controller braking, not a guessed animation cue.
            float forwardSpeed=Vector3.Dot(actor.body.linearVelocity,actor.transform.forward);
            if(visible&&distance<38&&actor.semi!=null&&actor.semi.command.y<-.2f&&forwardSpeed>2&&match.Age>=nextBrake&&!emitter.isPlaying)
            {nextBrake=match.Age+18;BrakeCues++;Play(airBrake,.75f);captions[target]="AIR BRAKES HISS NEARBY";captionUntil[target]=match.Age+3;}
        }
        void ObserveClosePass()
        {
            if(encounter<0)return;
            if(match.rules.outcome[encounter]!=HorrorOutcome.Active){encounter=-1;return;}
            float gap=Vector3.Distance(actor.body.position,match.PlayerPosition(encounter));minimumGap=Mathf.Min(minimumGap,gap);
            if(gap>45&&minimumGap<22)
            {
                if(match.rules.health[encounter]>=encounterHealth)
                {cleanPasses[encounter]++;SportsSessionTelemetry.GameplayEvent(gameObject.scene.name,"dead_end_clean_physical_pass",match.setup.players[encounter].participantId,"Actual close approach under22m followed by separation over45m without damage; no score awarded",minimumGap);}
                encounter=-1;
            }
            else if(match.rules.health[encounter]<encounterHealth)encounter=-1;
        }
        void Play(AudioClip clip,float seconds)
        {if(clip==null||!emitter.isActiveAndEnabled)return;emitter.Stop();emitter.clip=clip;emitter.Play();soundUntil=match.Age+Mathf.Min(seconds,clip.length);}
        public void ReportFinal()
        {if(reported||!Active)return;reported=true;for(int slot=0;slot<match.setup.count;slot++)SportsSessionTelemetry.GameplayEvent(gameObject.scene.name,"dead_end_pursuit_cues",match.setup.players[slot].participantId,"visible_physical_approach_warnings="+warnings[slot]+";actual_clean_passes="+cleanPasses[slot],warnings[slot]);}
        public string Diagnostic()=>"active="+Active+" warnings="+WarningCount+" owner="+LastWarningOwner+" actualGap="+LastWarningDistance.ToString("F1")+" actualClosing="+LastWarningClosingSpeed.ToString("F1")+" brakes="+BrakeCues+" actor="+actor.body.position;
        public void Silence(){if(emitter!=null)emitter.Stop();soundUntil=0;}
        public void Freeze(){Silence();Active=false;encounter=-1;}
        void OnApplicationFocus(bool value){focused=value;if(!value)Silence();}
        void OnApplicationPause(bool value){if(value)Silence();}
        void OnDisable()=>Freeze();
    }
}

using System;
using UnityEngine;
using TMPro;
using PixelCrushers.DialogueSystem;
using LWS.TruckSports;
namespace LWS.TruckHorrorSports
{
    /// <summary>Contextual consequences of original objective receipts; never completes tasks or changes scores.</summary>
    public enum GraveyardServiceEnding { UnfinishedNight, OrdinaryHandover, EvidenceOfTheEmptyCab, TheCallFollowedYou }
    public sealed class GraveyardNightServices : MonoBehaviour
    {
        public HorrorMatch match;
        public GraveyardDriverlessArrival visitor;
        public GraveyardSecurityAudit securityAudit;
        public Transform[] animatedProps;
        public Animation[] animations;
        public Sequencer[] sequences;
        public AudioSource[] stations;
        public AudioSource pumpMotor,generatorMotor;
        public Light pumpLight,breakerLight,generatorLight;
        public TMP_Text pumpReadout,cbReadout,generatorReadout;
        public Transform cbInteraction;
        readonly bool[] secondCallOffered=new bool[4],secondCallAnswered=new bool[4],securityEvidence=new bool[4];
        readonly float[] radioWork=new float[4];
        public bool AnsweredSecondCall(int slot)=>secondCallAnswered[slot];
        public bool HasSecurityEvidence(int slot)=>securityEvidence[slot];
        public GraveyardServiceEnding OutcomeFor(int slot)=>!match.Escaped(slot)?GraveyardServiceEnding.UnfinishedNight:secondCallAnswered[slot]?GraveyardServiceEnding.TheCallFollowedYou:securityEvidence[slot]?GraveyardServiceEnding.EvidenceOfTheEmptyCab:GraveyardServiceEnding.OrdinaryHandover;
        public string EndingFor(int slot)
        {
            if(match.option==1)return "SHIFT CLOSED · "+match.Objectives(slot)+" ACTUAL JOBS BANKED";
            if(match.option==2)return "THE NIGHT ENDS · "+match.SurvivalSeconds(slot).ToString("F1")+"s SURVIVAL";
            switch(OutcomeFor(slot))
            {
                case GraveyardServiceEnding.TheCallFollowedYou:return "YOU ANSWERED AGAIN · THE CALL FOLLOWED YOU HOME";
                case GraveyardServiceEnding.EvidenceOfTheEmptyCab:return "SHIFT COMPLETE · YOU RECORDED THE EMPTY CAB";
                case GraveyardServiceEnding.OrdinaryHandover:return "SHIFT COMPLETE · YOU LEFT THE SECOND CALL UNANSWERED";
                default:return GeneratorRunning?"THE GENERATOR IS RUNNING · YOUR SHIFT IS UNFINISHED":"THE LAST STOP STILL NEEDS YOU";
            }
        }
        public bool CanAnswerSecondCall(int slot)
        {
            if(frozen||slot<0||slot>=match.setup.count||!secondCallOffered[slot]||secondCallAnswered[slot]||GeneratorRunning)return false;
            var foot=match.footAgents[slot];return foot.Outside&&Vector3.Distance(foot.transform.position,cbInteraction.position)<2.4f&&foot.controller.velocity.magnitude<.8f;
        }
        public string InteractionFor(int slot)=>secondCallOffered[slot]&&!secondCallAnswered[slot]&&!GeneratorRunning?"SECOND CALL · HOLD ACTION AT THE CB TO ANSWER, OR LEAVE IT RINGING":null;
        public bool PumpChecked {get;private set;}
        public bool PowerRestored {get;private set;}
        public bool GeneratorRunning {get;private set;}
        public int Responses {get;private set;}
        public int LastOwner {get;private set;}=-1;
        public float PumpGallons {get;private set;}
        readonly bool[,] observed=new bool[4,6];
        readonly string[] participantCaption=new string[4];
        readonly float[] captionUntil=new float[4];
        struct Cue {public int owner,station;public string caption;}
        readonly Cue[] queue=new Cue[32];
        int head,tail,queued,lastArrival;
        float nextCaption;
        Quaternion[] originalRotations;
        bool initialized,frozen,reported;
        public string CaptionFor(int slot)=>securityAudit!=null&&!string.IsNullOrEmpty(securityAudit.CaptionFor(slot))?securityAudit.CaptionFor(slot):slot>=0&&slot<match.setup.count&&match.Age<captionUntil[slot]?participantCaption[slot]:null;
        public bool HandlesSecurityConsole(int slot)=>securityAudit!=null&&securityAudit.HandlesConsole(slot);
        public string SecurityInstructionFor(int slot)=>securityAudit!=null?securityAudit.InstructionFor(slot):null;
        public bool Observed(int slot,int objective)=>observed[slot,objective];
        public void ResetServices()
        {
            if(!initialized){originalRotations=new Quaternion[animatedProps.Length];for(int i=0;i<animatedProps.Length;i++)originalRotations[i]=animatedProps[i].localRotation;initialized=true;}
            Freeze();frozen=reported=false;Array.Clear(observed,0,observed.Length);Array.Clear(participantCaption,0,participantCaption.Length);Array.Clear(captionUntil,0,captionUntil.Length);Array.Clear(secondCallOffered,0,secondCallOffered.Length);Array.Clear(secondCallAnswered,0,secondCallAnswered.Length);Array.Clear(securityEvidence,0,securityEvidence.Length);Array.Clear(radioWork,0,radioWork.Length);
            PumpChecked=PowerRestored=GeneratorRunning=false;Responses=0;LastOwner=-1;PumpGallons=0;head=tail=queued=lastArrival=0;nextCaption=0;
            for(int i=0;i<animatedProps.Length;i++)animatedProps[i].localRotation=originalRotations[i];
            pumpLight.enabled=true;breakerLight.enabled=false;generatorLight.enabled=false;
            pumpReadout.text="PUMP 03\nCHECK REQUIRED";cbReadout.text="CB 19 · NO CARRIER";generatorReadout.text="AUXILIARY POWER OFF";visitor.ResetArrival();
            securityAudit?.ResetAudit();
        }
        public void TickServices(float dt)
        {
            if(frozen)return;
            securityAudit?.TickAudit(dt);
            for(int slot=0;slot<match.setup.count;slot++)for(int task=0;task<6;task++)ObserveCompletedObjective(slot,task);
            if(match.Escalation>=1 && !PumpChecked)
            {
                PumpGallons+=dt*2.4f;pumpReadout.text="PUMP 03\n"+PumpGallons.ToString("F1")+" GAL · NO VEHICLE";
                pumpLight.intensity=1.6f+Mathf.Sin(match.Age*3.1f)*.35f;
                Loop(pumpMotor,true,.16f);
            }
            else{Loop(pumpMotor,false,0);if(PumpChecked)pumpReadout.text="PUMP 03 ISOLATED\n"+PumpGallons.ToString("F1")+" GAL AUDITED";}
            Loop(generatorMotor,GeneratorRunning,.2f);
            foreach(var source in stations)source.volume=match.settings.sfx*.42f;
            visitor.truck.tractorMotor.gripScale=match.atmosphere!=null?Mathf.Max(.65f,match.atmosphere.Grip):1;
            visitor.Tick(dt);
            if(visitor.Arrivals>lastArrival)
            {
                lastArrival=visitor.Arrivals;
                if(lastArrival<=2){int owner=Mathf.Clamp(LastOwner,0,match.setup.count-1);Enqueue(owner,3,lastArrival==1?"The arrival has no driver. Keep the service lane clear.":"The same registration came back. Nobody got out.");}
            }
            if(queued>0 && match.Age>=nextCaption)
            {
                Cue cue=queue[head];head=(head+1)%queue.Length;queued--;Responses++;LastOwner=cue.owner;
                string caption=match.setup.players[cue.owner].name+" · "+cue.caption;participantCaption[cue.owner]=caption;captionUntil[cue.owner]=match.Age+7;
                match.Say(caption,6);nextCaption=match.Age+8;
                foreach(var station in stations)station.Stop();
                var source=stations[cue.station];if(source.isActiveAndEnabled && source.clip!=null){source.Stop();source.Play();}
                SportsSessionTelemetry.GameplayEvent(gameObject.scene.name,"graveyard_context_response",match.setup.players[cue.owner].participantId,"station="+cue.station+";caption="+cue.caption,Responses);
            }
        }
        // Call at the existing CompleteTask receipt before TaskRush resets its cycle.
        // The existing rule receipt is mandatory; this never completes or creates a job.
        public void ObserveCompletedObjective(int slot,int task)
        {
            if(frozen||slot<0||slot>=match.setup.count||task<0||task>=6||observed[slot,task]||!match.rules.Done(slot,task))return;
                observed[slot,task]=true;LastOwner=slot;
                if(task==0){visitor.Arm();Enqueue(slot,0,"The manifest lists a delivery. There is no driver's name.");}
                if(task==1){PumpChecked=true;PlayProp(0,"PumpCheck");Enqueue(slot,1,"Pump 03 is feeding an empty bay. The meter is still running.");}
                if(task==2){PowerRestored=true;breakerLight.enabled=true;PlayProp(1,"BreakerOn");Enqueue(slot,2,"Breaker restored. The store is drawing power from a disconnected circuit.");}
                if(task==3){PlayProp(2,"CBAnswer");cbReadout.text="CHANNEL 19 · RETURNING CALL";Enqueue(slot,3,"CB 19: You checked me in already. I am outside again.");}
                if(task==4)
                {
                    var monitor=match.objectives[4].securityMonitor;
                    securityEvidence[slot]=monitor!=null&&monitor.HasFrame&&visitor.InSecurityView(monitor.securityCamera);
                    secondCallOffered[slot]=true;cbReadout.text="CB 19 · SECOND CALL\nANSWER OR IGNORE";
                    Enqueue(slot,4,securityEvidence[slot]?"Security receipt saved: an empty cab and a matching registration.":"Security check complete. The CB is ringing again. Answer it, or restart the generator and leave.");
                }
                if(task==5){GeneratorRunning=true;generatorLight.enabled=true;PlayProp(3,"GeneratorRun");generatorReadout.text="AUXILIARY POWER ONLINE\nSAFE TO LEAVE";Enqueue(slot,5,"Generator online. Leave through the service gate. Do not answer the next call.");}
        }
        // Existing Game Night input owns this action. This optional response changes
        // the encounter/epilogue, never the original objectives or competitive results.
        public void ObserveAction(int slot,bool held,float dt)
        {
            if(frozen||slot<0||slot>=match.setup.count||!secondCallOffered[slot]||secondCallAnswered[slot]||GeneratorRunning)return;
            radioWork[slot]=held&&CanAnswerSecondCall(slot)?radioWork[slot]+dt:0;
            if(radioWork[slot]<2)return;
            secondCallAnswered[slot]=true;visitor.AnsweredSecondCall();PlayProp(2,"CBAnswer");cbReadout.text="CHANNEL 19 · IT KNOWS YOUR NAME";
            Enqueue(slot,3,"CB 19: Thank you. Now I know which voice to follow.");
            SportsSessionTelemetry.GameplayEvent(gameObject.scene.name,"graveyard_second_call_answered",match.setup.players[slot].participantId,"Actual held interaction beside the CB; no objective or score changed",1);
        }
        void Enqueue(int owner,int station,string caption)
        {
            if(queued>=queue.Length)return;
            queue[tail]=new Cue{owner=owner,station=station,caption=caption};tail=(tail+1)%queue.Length;queued++;
        }
        void PlayProp(int index,string clip)
        {
            // Standalone Sports scenes intentionally have no DialogueManager.
            // The installed Sequencer requires its display settings even for prop-only
            // commands. Use its public API when that authority exists; otherwise
            // play the same authored legacy clip directly through Unity Animation.
            if(DialogueManager.hasInstance&&DialogueManager.displaySettings!=null&&DialogueManager.displaySettings.cameraSettings!=null)
            {sequences[index].Stop();sequences[index].PlaySequence("Animation("+clip+",speaker)",animatedProps[index],null,false,false);}
            else animations[index].Play(clip);
        }
        void Loop(AudioSource source,bool active,float gain)
        {
            source.volume=match.settings.sfx*gain;
            if(active && source.isActiveAndEnabled && !source.isPlaying)source.Play();else if(!active && source.isPlaying)source.Stop();
        }
        public void ReportFinal()
        {
            if(reported)return;reported=true;
            securityAudit?.ReportFinal();
            SportsSessionTelemetry.GameplayEvent(gameObject.scene.name,"graveyard_physical_visitor_arrivals",null,"A real NWH rig completed the authored arrival line",visitor.Arrivals);
            SportsSessionTelemetry.GameplayEvent(gameObject.scene.name,"graveyard_physical_visitor_distance",null,"Actual bounded horizontal movement; unit=M",visitor.DistanceDriven);
            for(int slot=0;slot<match.setup.count;slot++)
            {
                SportsSessionTelemetry.GameplayEvent(gameObject.scene.name,"graveyard_security_evidence",match.setup.players[slot].participantId,"Actual security task receipt with rendered feed and unobstructed visitor in its frustum",securityEvidence[slot]?1:0);
                if(match.option==0||match.option==3)SportsSessionTelemetry.GameplayEvent(gameObject.scene.name,"graveyard_service_epilogue",match.setup.players[slot].participantId,EndingFor(slot),(int)OutcomeFor(slot));
            }
        }
        public void Silence()
        {
            securityAudit?.Silence();
            if(stations!=null)foreach(var source in stations)if(source!=null)source.Stop();
            if(pumpMotor!=null)pumpMotor.Stop();if(generatorMotor!=null)generatorMotor.Stop();
            if(visitor!=null&&visitor.truck!=null)visitor.truck.StopSound();
        }
        public void Freeze()
        {
            frozen=true;Silence();
            securityAudit?.Freeze();
            if(sequences!=null)foreach(var sequence in sequences)if(sequence!=null)sequence.Stop();
            if(animations!=null)foreach(var animation in animations)if(animation!=null)animation.Stop();
            if(visitor!=null)visitor.Freeze();
        }
        void OnDisable(){if(initialized)Freeze();}
    }
}

using System;
using UnityEngine;
using TMPro;
using DG.Tweening;
using LWS.TruckSports;
using LWS.TruckBowling;
namespace LWS.TruckHorrorSports
{
    /// <summary>Optional contextual missing-rig inspection over the original live CCTV renderer.</summary>
    public sealed class GraveyardSecurityAudit : MonoBehaviour
    {
        public HorrorMatch match;
        public HorrorSecurityMonitor monitor;
        public Transform console,control;
        public Transform[] views;
        public float[] fieldOfViews;
        public string[] viewNames;
        public GameObject[] parkedBays;
        public TMP_Text label;
        public AudioSource switchSound;
        readonly bool[] reported=new bool[4],heldBefore=new bool[4],aiSelected=new bool[4];
        readonly float[] pressTime=new float[4],inspection=new float[4],captionUntil=new float[4],reportFrameTime=new float[4];
        readonly int[] reportedBay={-1,-1,-1,-1},reportedView={-1,-1,-1,-1};
        readonly string[] captions=new string[4];
        readonly Plane[] frustum=new Plane[6];
        Renderer[][] bayRenderers;
        Bounds[] bayBounds;
        float[] missingSince;
        int owner=-1,visibleMissing=-1,shownView=-1,shownOwner=-2;
        float ownerUntil,checkedFrame=-1;
        Quaternion originalControl;
        bool initialized,frozen,finalReported;
        public int SelectedView {get;private set;}
        public int ViewSelections {get;private set;}
        public int Reports {get;private set;}
        public int ConsoleOwner=>owner;
        public bool Reported(int slot)=>slot>=0&&slot<match.setup.count&&reported[slot];
        public int ReportedBay(int slot)=>reportedBay[slot];
        public int ReportedView(int slot)=>reportedView[slot];
        public float ReportFrameTime(int slot)=>reportFrameTime[slot];
        public string CaptionFor(int slot)=>slot>=0&&slot<match.setup.count&&match.Age<captionUntil[slot]?captions[slot]:null;
        public bool HandlesConsole(int slot)=>!frozen&&slot>=0&&slot<match.setup.count&&AtConsole(slot);
        public string InstructionFor(int slot)=>owner>=0&&owner!=slot?"CONSOLE IN USE · "+match.setup.players[owner].name:reported[slot]?"MISSING RIG RECORDED · TAP ACTION FOR NEXT CAMERA":"TAP ACTION · NEXT CAMERA\nHOLD ACTION · REPORT MISSING RIG";
        bool AtConsole(int slot)
        {
            var foot=match.footAgents[slot];return foot!=null&&foot.Outside&&Vector3.Distance(foot.transform.position,console.position)<2.2f&&foot.controller.velocity.magnitude<.8f;
        }
        public void ResetAudit()
        {
            Freeze();frozen=finalReported=false;owner=-1;ownerUntil=0;checkedFrame=-1;visibleMissing=-1;shownView=-1;shownOwner=-2;ViewSelections=Reports=0;
            Array.Clear(reported,0,4);Array.Clear(heldBefore,0,4);Array.Clear(aiSelected,0,4);Array.Clear(pressTime,0,4);Array.Clear(inspection,0,4);Array.Clear(captions,0,4);Array.Clear(captionUntil,0,4);Array.Clear(reportFrameTime,0,4);
            if(!initialized)
            {
                originalControl=control.localRotation;bayRenderers=new Renderer[parkedBays.Length][];bayBounds=new Bounds[parkedBays.Length];missingSince=new float[parkedBays.Length];
                for(int bay=0;bay<parkedBays.Length;bay++){bayRenderers[bay]=parkedBays[bay].GetComponentsInChildren<Renderer>(true);bayBounds[bay]=bayRenderers[bay][0].bounds;foreach(var renderer in bayRenderers[bay])bayBounds[bay].Encapsulate(renderer.bounds);}
                initialized=true;
            }
            for(int bay=0;bay<missingSince.Length;bay++)missingSince[bay]=float.PositiveInfinity;
            for(int slot=0;slot<4;slot++)reportedBay[slot]=reportedView[slot]=-1;
            control.localRotation=originalControl;SelectView(0,false);RefreshLabel();
        }
        public void TickAudit(float dt)
        {
            if(frozen||!match.Live)return;
            for(int bay=0;bay<parkedBays.Length;bay++)
            {
                bool missing=IsActuallyMissing(bay);
                if(!missing)missingSince[bay]=float.PositiveInfinity;
                else if(float.IsPositiveInfinity(missingSince[bay]))missingSince[bay]=Time.unscaledTime;
            }
            if(monitor.HasFrame&&monitor.LastRenderedTime!=checkedFrame)
            {
                checkedFrame=monitor.LastRenderedTime;visibleMissing=-1;GeometryUtility.CalculateFrustumPlanes(monitor.securityCamera,frustum);
                for(int bay=0;bay<parkedBays.Length;bay++)
                    if(IsActuallyMissing(bay)&&checkedFrame>=missingSince[bay]&&GeometryUtility.TestPlanesAABB(frustum,bayBounds[bay])&&!Physics.Linecast(monitor.LastRenderedPosition,bayBounds[bay].center,1,QueryTriggerInteraction.Ignore)){visibleMissing=bay;break;}
            }
            if(owner>=0&&(!AtConsole(owner)||match.Age>=ownerUntil))owner=-1;
            for(int slot=0;slot<match.setup.count;slot++)
            {
                bool ai=match.setup.players[slot].source==BowlingSource.AI;
                bool at=AtConsole(slot)&&match.rules.outcome[slot]==HorrorOutcome.Active&&!match.GraveyardWorkerRetreating(slot);
                bool held=at&&(ai?match.NextObjective(slot)==4&&!reported[slot]:match.ActionHeld(slot));
                if(!at){heldBefore[slot]=false;inspection[slot]=pressTime[slot]=0;continue;}
                if(held&&(owner<0||!heldBefore[slot]&&(owner==slot||!ai&&match.setup.players[owner].source==BowlingSource.AI)))
                {owner=slot;ownerUntil=match.Age+3;pressTime[slot]=inspection[slot]=0;}
                if(owner==slot)
                {
                    if(ai&&held&&!aiSelected[slot]){aiSelected[slot]=true;SelectView(1,true);}
                    if(held)
                    {
                        ownerUntil=match.Age+3;pressTime[slot]+=dt;
                        bool fresh=monitor.HasFrame&&visibleMissing>=0&&monitor.LastRenderedTime>=missingSince[visibleMissing]&&IsActuallyMissing(visibleMissing);
                        inspection[slot]=fresh?inspection[slot]+dt:0;
                        if(!reported[slot]&&inspection[slot]>=1.2f)
                        {
                            reported[slot]=true;reportedBay[slot]=visibleMissing;reportedView[slot]=SelectedView;reportFrameTime[slot]=monitor.LastRenderedTime;Reports++;
                            captions[slot]="PARKING BAY "+(visibleMissing+1)+" · MISSING RIG REPORTED";captionUntil[slot]=match.Age+6;match.Say(match.setup.players[slot].name+" · "+captions[slot],5);
                            SportsSessionTelemetry.GameplayEvent(gameObject.scene.name,"graveyard_missing_rig_report",match.setup.players[slot].participantId,"Actual on-foot held inspection of fresh live feed; bay="+visibleMissing+";view="+SelectedView,1);
                            if(ai)owner=-1;
                        }
                    }
                    else if(heldBefore[slot])
                    {if(pressTime[slot]<.35f)SelectView((SelectedView+1)%views.Length,true);inspection[slot]=pressTime[slot]=0;}
                }
                heldBefore[slot]=held;
            }
            RefreshLabel();
        }
        bool IsActuallyMissing(int bay)
        {
            if(parkedBays[bay]==null||!parkedBays[bay].activeInHierarchy||bayRenderers[bay].Length==0)return false;
            foreach(var renderer in bayRenderers[bay])if(renderer!=null&&renderer.enabled&&renderer.gameObject.activeInHierarchy)return false;
            return true;
        }
        void SelectView(int view,bool feedback)
        {
            SelectedView=view;visibleMissing=-1;checkedFrame=-1;monitor.SelectView(views[view],fieldOfViews[view]);
            for(int slot=0;slot<4;slot++)inspection[slot]=0;
            if(feedback)
            {ViewSelections++;control.DOKill();control.DOLocalRotate((originalControl*Quaternion.Euler(0,0,view*18)).eulerAngles,.15f);if(switchSound!=null&&switchSound.isActiveAndEnabled&&Application.isFocused&&match.settings.master>0&&match.settings.sfx>0){switchSound.volume=match.settings.sfx*.35f;switchSound.Play();}}
        }
        void RefreshLabel()
        {
            if(label==null||shownView==SelectedView&&shownOwner==owner)return;
            shownView=SelectedView;shownOwner=owner;
            label.text="LIVE CCTV · "+viewNames[SelectedView]+"\nTAP ACTION: NEXT CAMERA · HOLD: REPORT MISSING RIG"+(owner>=0?"\nOPERATOR: "+match.setup.players[owner].name:"");
            label.color=owner>=0?Color.Lerp(Color.white,match.setup.players[owner].color,.45f):new Color(.64f,.9f,.7f);
        }
        public string Diagnostic(int slot)=>"feed="+SelectedView+" switches="+ViewSelections+" owner="+owner+" frame="+monitor.LastRenderedTime.ToString("F2")+" visibleMissing="+visibleMissing+" reported="+Reported(slot)+" bay="+reportedBay[slot]+" capturedFrame="+reportFrameTime[slot].ToString("F2");
        public void ReportFinal()
        {if(finalReported)return;finalReported=true;for(int slot=0;slot<match.setup.count;slot++)SportsSessionTelemetry.GameplayEvent(gameObject.scene.name,"graveyard_missing_rig_audit_result",match.setup.players[slot].participantId,"reported_bay="+reportedBay[slot]+";actual_view="+reportedView[slot],reported[slot]?1:0);}
        public void Silence(){if(switchSound!=null)switchSound.Stop();}
        public void Freeze(){frozen=true;owner=-1;Silence();if(control!=null)control.DOKill();}
        void OnDisable()=>Freeze();
    }
}

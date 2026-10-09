using System;
using System.Collections.Generic;
using LWS.InterstateHauler;
using NWH.VehiclePhysics2.Modules.Trailer;
using TMPro;
using UnityEngine;

namespace LWS.TruckTaxi
{
    [DisallowMultipleComponent]
    public sealed class TruckTaxiMoralityWorldController : MonoBehaviour, ITruckTaxiMoralEventContextSource
    {
        [Tooltip("Optional explicit references to existing authored trailer events. No scene search or spawning.")]
        public TruckTaxiMoralTrailerEvent[] authoredEvents=Array.Empty<TruckTaxiMoralTrailerEvent>();
        private const string NavigationOwner="taxi-morality";
        private TruckTaxiBootstrap host;
        private TruckTaxiHud hud;
        private TruckTaxiSession session;
        private TruckTaxiHudModalBridge bridge;
        private RectTransform overlay, panel;
        private GameObject launcher, turnIn, deliver, complete;
        private TextMeshProUGUI heading, message, leftChoice, rightChoice, status;
        private IDisposable modalLease;
        private TruckTaxiMoralChoicePresentation presentation;
        private TruckTaxiAcceptedMoralChoice accepted;
        private readonly HashSet<string> completedEvents=new HashSet<string>(StringComparer.Ordinal);
        private readonly TruckTaxiMoralArrivalGate arrival=new TruckTaxiMoralArrivalGate();
        private string profileId, navigationTarget;
        private TruckTaxiGPSAdapter navigationOwner;
        private long navigationToken;
        private Vector3 navigationPoint;
        private bool subscribed;
        private float refreshAt, routeRetryAt;
        public string Diagnosis { get; private set; }="No authored trailer context.";
        public bool IsOpen => overlay!=null && overlay.gameObject.activeSelf;
        private bool OwnsModal => modalLease!=null && bridge!=null && bridge.IsOwnedBy(this) && bridge.IsOpen;

        public bool Initialize(TruckTaxiBootstrap owner,TruckTaxiHud view)
        {
            if(owner==null || owner.Session==null || owner.Player==null || view==null || view.Root==null || view.UIInput==null ||
                view.Root.GetComponentInParent<Canvas>()==null || host!=null && (host!=owner || hud!=view)) return false;
            if(owner.Session.MoralEventContextSource!=null && owner.Session.MoralEventContextSource!=this) return false;
            host=owner; hud=view; session=owner.Session;
            bridge=view.GetComponent<TruckTaxiHudModalBridge>() ?? view.gameObject.AddComponent<TruckTaxiHudModalBridge>();
            bridge.Initialize(owner,view);
            if(overlay==null) BuildUI();
            Attach(); return true;
        }
        private void Attach()
        {
            if(!isActiveAndEnabled || session==null || subscribed) return;
            if(session.MoralEventContextSource!=null && session.MoralEventContextSource!=this)
            { Diagnosis="Another context source owns morality."; return; }
            session.MoralEventContextSource=this;
            session.MoralityChanged+=ReloadState; session.CareerEquipmentChanged+=OnCareerChanged;
            subscribed=true; ReloadState();
        }
        private void OnCareerChanged()
        { if(profileId!=session.CareerRunProfileId) ReloadState(); }
        private void ReloadState()
        {
            Close(); ReleaseNavigation(); arrival.Reset(); routeRetryAt=0;
            profileId=session.CareerRunProfileId;
            accepted=session.CaptureActiveMoralChoice(); completedEvents.Clear();
            foreach(var choice in session.CaptureMoralityState().choices) if(choice.completed) completedEvents.Add(choice.eventId);
            refreshAt=0;
        }
        private bool Ready => isActiveAndEnabled && subscribed && host!=null && host.Session==session && host.CanResume &&
            host.Persistence?.HasActiveRun==true && !host.Persistence.IsBusy && !host.Persistence.ResumeBlocked &&
            session.HasCareerIdentity && session.CareerRunProfileId==host.Persistence.RunProfileId &&
            session.MoralEventContextSource==this && session.State==TruckTaxiState.Available && !session.RaceEventActive &&
            !session.HasStolenPassenger && (host.WorldLife==null || host.WorldLife.Mode==TruckTaxiPlayerControlMode.Driving) &&
            (!host.Paused || OwnsModal);
        private string ArrivalKey => accepted==null ? null : profileId+"|"+accepted.eventId+"|"+accepted.choiceId+"|"+accepted.completionObjectiveId;

        // The only runtime producer of evidence reads NWH's actual attached object, not just a matching string.
        private bool ReadPhysical(TruckTaxiMoralTrailerEvent authored,out TruckTaxiMoralPhysicalEvidence evidence,out string reason)
        {
            evidence=default; reason="Attach the same existing authored trailer to the active truck.";
            var player=host.Player;
            var vehicle=player!=null ? player.NwhAdapter?.VehicleController : null;
            var coupling=player!=null ? player.CouplingAdapter : null;
            if(vehicle==null || coupling==null || !coupling.isActiveAndEnabled || !coupling.IsTrailerAttached ||
                authored==null || !authored.ValidatePhysical(out reason)) return false;
            TrailerHitchModuleWrapper hitch=null;
            foreach(var candidate in vehicle.GetComponentsInChildren<TrailerHitchModuleWrapper>())
            {
                if(!candidate.isActiveAndEnabled || candidate.module==null || candidate.module.vehicleController!=vehicle || !candidate.module.attached) continue;
                if(hitch!=null) { reason="Ambiguous attached trailer hitch."; return false; }
                hitch=candidate;
            }
            var attached=hitch!=null ? hitch.module.attachedTrailerModule : null;
            var attachedVehicle=attached?.vehicleController;
            if(attached==null || !attached.attached || attachedVehicle==null)
            { reason="NWH does not report a live two-sided trailer attachment."; return false; }
            var identity=attachedVehicle.GetComponent<LwsTrailerGameplayIdentity>();
            var state=coupling.CurrentState;
            evidence=new TruckTaxiMoralPhysicalEvidence(state.attached,state.towingVehicleId,identity,
                vehicle.GetComponent<Rigidbody>(),attachedVehicle.GetComponent<Rigidbody>());
            if(state.trailerId!=authored.definition.trailerId || coupling.CurrentTrailerId!=state.trailerId ||
                !TruckTaxiMoralArrivalPolicy.Matches(authored.trailer,player.VehicleId,evidence))
            { reason="Attached physical trailer, towing vehicle or dynamic bodies do not match the authored event."; return false; }
            reason=null; return true;
        }
        private bool AcceptedArrival(out TruckTaxiMoralTrailerEvent authored,out TruckTaxiMoralDestination destination,out string reason)
        {
            authored=null; destination=null; reason="Trailer objective is unavailable in the current gameplay state.";
            if(!Ready || !TruckTaxiMoralTrailerEvent.TryResolveAccepted(authoredEvents,accepted,out authored,out destination,out reason)) return false;
            if(!ReadPhysical(authored,out var evidence,out reason)) return false;
            if(!TruckTaxiMoralArrivalPolicy.Arrived(destination,evidence,authored.maximumSpeed))
            { reason="Arrival pending: matching truck and trailer must be stationary inside the authored destination."; return false; }
            reason=null; return true;
        }
        private void FixedUpdate()
        {
            if(host==null || host.Paused) return;
            bool valid=AcceptedArrival(out var authored,out _,out _);
            arrival.Observe(ArrivalKey,valid,Time.fixedDeltaTime,authored!=null ? authored.stationarySeconds : 2);
        }
        public bool TryGetContext(string eventId,out TruckTaxiMoralEventContext context)
        {
            context=default;
            if(!Ready || completedEvents.Contains(eventId) || accepted!=null && accepted.eventId!=eventId) return false;
            if(!TruckTaxiMoralTrailerEvent.TryResolve(authoredEvents,eventId,out var authored,out var reason) ||
                !ReadPhysical(authored,out var evidence,out reason))
            { Diagnosis=reason; arrival.Reset(); return false; }
            if(!TruckTaxiMoralArrivalPolicy.Stationary(evidence,authored.maximumSpeed))
            { Diagnosis="Matching truck and trailer are not stationary."; arrival.Reset(); return false; }
            string objective=null;
            if(accepted!=null)
            {
                if(!AcceptedArrival(out _,out _,out reason)) { arrival.Reset(); Diagnosis=reason; }
                else if(arrival.IsReady(ArrivalKey,authored.stationarySeconds)) objective=accepted.completionObjectiveId;
                else Diagnosis="Arrival pending: stationary confirmation.";
            }
            context=new TruckTaxiMoralEventContext(eventId,authored.definition.town.initialState.townId,authored.trailer,objective);
            return true;
        }

        private void Update()
        {
            if(!subscribed || host==null) return;
            if(host.Session!=session || session.MoralEventContextSource!=this)
            {
                Diagnosis="Morality context ownership changed.";
                arrival.Reset(); Close(); ReleaseNavigation();
                if(launcher!=null) launcher.SetActive(false);
                return;
            }
            if(!Ready) { arrival.Reset(); if(IsOpen) Close(); }
            if(Time.unscaledTime<refreshAt) return;
            refreshAt=Time.unscaledTime+.25f;
            if(accepted!=null && !AcceptedArrival(out _,out _,out _)) arrival.Reset();
            RebindNavigation();
            if(launcher!=null) launcher.SetActive(!IsOpen && Ready && bridge.CanAcquire && (accepted!=null || FindPresentable()!=null));
            if(IsOpen) RefreshPanel();
        }
        private TruckTaxiMoralTrailerEvent FindPresentable()
        {
            foreach(var item in authoredEvents ?? Array.Empty<TruckTaxiMoralTrailerEvent>())
                if(item!=null && item.definition!=null && !completedEvents.Contains(item.definition.eventId) &&
                    TryGetContext(item.definition.eventId,out _)) return item;
            return null;
        }
        private void RebindNavigation()
        {
            if(accepted==null) { ReleaseNavigation(); return; }
            if(!TruckTaxiMoralTrailerEvent.TryResolveAccepted(authoredEvents,accepted,out var authored,out var destination,out var reason) ||
                !authored.ValidatePhysical(out reason))
            { Diagnosis=reason; ReleaseNavigation(); return; }
            if(navigationOwner!=null && navigationPoint!=destination.center.position) ReleaseNavigation();
            if(!Ready || host.Paused || host.GPS==null || host.GPS.HasNavigationTarget || Time.unscaledTime<routeRetryAt) return;
            routeRetryAt=Time.unscaledTime+5;
            ReleaseNavigation();
            var gps=host.GPS;
            // Lowest existing layer, admitted only into an empty stack. Never replace another owner.
            if(!gps.TrySetNavigationIntent(TruckTaxiNavigationLayer.RideFinal,NavigationOwner,destination.objectiveId,
                string.IsNullOrWhiteSpace(destination.displayName) ? "Trailer objective" : destination.displayName,
                destination.center.position,TruckTaxiMapMarkerType.ActiveObjective,destination.radius,true))
            { Diagnosis="Trailer objective exists; a valid GPS route is unavailable."; return; }
            navigationOwner=gps; navigationTarget=destination.objectiveId; navigationToken=gps.ActiveLeaseToken;
            navigationPoint=destination.center.position;
        }
        private void ReleaseNavigation()
        {
            if(navigationOwner!=null && navigationToken!=0)
                navigationOwner.ReleaseNavigationIntent(TruckTaxiNavigationLayer.RideFinal,NavigationOwner,navigationTarget,navigationToken);
            navigationOwner=null; navigationTarget=null; navigationToken=0;
        }

        private void BuildUI()
        {
            launcher=hud.Button(hud.Root,"TRAILER EVENT",new Vector2(.36f,.935f),new Vector2(.64f,.995f),()=>Open());
            launcher.SetActive(false);
            overlay=TruckTaxiHud.Rect(hud.Root,"Authored trailer choice overlay",Vector2.zero,Vector2.one);
            var blocker=overlay.gameObject.AddComponent<UnityEngine.UI.Image>(); blocker.color=new Color(0,0,0,.65f);
            panel=hud.Panel(overlay,"Authored trailer choice",new Vector2(.16f,.14f),new Vector2(.84f,.86f));
            heading=hud.Text(panel,"Trailer event heading",new Vector2(.05f,.88f),new Vector2(.95f,.97f),32);
            message=hud.Text(panel,"Trailer event details",new Vector2(.05f,.55f),new Vector2(.95f,.85f),24);
            leftChoice=hud.Text(panel,"Authority choice",new Vector2(.05f,.36f),new Vector2(.47f,.53f),22);
            rightChoice=hud.Text(panel,"Contact choice",new Vector2(.53f,.36f),new Vector2(.95f,.53f),22);
            status=hud.Text(panel,"Trailer event status",new Vector2(.05f,.15f),new Vector2(.95f,.24f),20);
            foreach(var label in new[] { heading,message,leftChoice,rightChoice,status })
            { label.richText=false; label.fontSizeMin=16; label.characterSpacing=0; }
            turnIn=hud.Button(panel,"TURN THEM IN",new Vector2(.05f,.25f),new Vector2(.47f,.32f),()=>AcceptChoice(TruckTaxiMoralChoiceDefinition.TurnIn));
            deliver=hud.Button(panel,"DELIVER TO DESTINATION",new Vector2(.53f,.25f),new Vector2(.95f,.32f),()=>AcceptChoice(TruckTaxiMoralChoiceDefinition.Deliver));
            complete=hud.Button(panel,"COMPLETE OBJECTIVE",new Vector2(.22f,.25f),new Vector2(.78f,.34f),CompleteChoice);
            hud.Button(panel,"CLOSE",new Vector2(.35f,.025f),new Vector2(.65f,.115f),Close);
            overlay.gameObject.SetActive(false);
        }
        public bool Open()
        {
            if(IsOpen || !Ready || bridge==null || !bridge.CanAcquire) return false;
            presentation=null;
            if(accepted==null)
            {
                var candidate=FindPresentable();
                if(candidate==null) return false;
                if(!session.TryPresentMoralChoice(candidate.definition,out presentation,out var reason)) { Diagnosis=reason; return false; }
            }
            modalLease=bridge.TryAcquire(this,panel,Close,Interrupt);
            if(modalLease==null) { presentation=null; return false; }
            overlay.gameObject.SetActive(true); overlay.SetAsLastSibling(); launcher.SetActive(false);
            RefreshPanel(); hud.UIInput.Focus(panel); return true;
        }
        private static string Effects(TruckTaxiMoralChoiceEffects value) =>
            "Payment: "+TruckTaxiHud.Money(value.payoutCents)+"\nSafety "+value.safety.ToString("+0;-0;0")+
            "   Crime "+value.crime.ToString("+0;-0;0")+"\nFares "+(value.fareMultiplier*100).ToString("+0.##;-0.##;0")+"%";
        private void RefreshPanel()
        {
            if(!OwnsModal) { Close(); return; }
            bool choosing=accepted==null && presentation!=null;
            heading.text=choosing ? "PEOPLE IN THE TRAILER" : "TRAILER OBJECTIVE";
            turnIn.SetActive(choosing); deliver.SetActive(choosing); leftChoice.gameObject.SetActive(choosing); rightChoice.gameObject.SetActive(choosing);
            complete.SetActive(false);
            if(choosing)
            {
                message.text=presentation.description;
                leftChoice.text=Effects(Array.Find(presentation.options,o=>o.choiceId==TruckTaxiMoralChoiceDefinition.TurnIn).effects);
                rightChoice.text=Effects(Array.Find(presentation.options,o=>o.choiceId==TruckTaxiMoralChoiceDefinition.Deliver).effects);
                status.text="PAYMENT ON OBJECTIVE COMPLETION";
            }
            else if(accepted!=null)
            {
                bool bound=TruckTaxiMoralTrailerEvent.TryResolveAccepted(authoredEvents,accepted,out _,out var destination,out var reason);
                string label=bound && !string.IsNullOrWhiteSpace(destination.displayName) ? destination.displayName : accepted.completionObjectiveId;
                message.text=label+"\n"+Effects(accepted.effects);
                bool ready=TryGetContext(accepted.eventId,out var context) && context.CompletedObjectiveId==accepted.completionObjectiveId;
                complete.SetActive(ready);
                status.text=ready ? "ARRIVAL CONFIRMED" : !bound ? reason : Diagnosis;
            }
        }
        private void AcceptChoice(string choice)
        {
            if(!OwnsModal || presentation==null || !Ready) return;
            string eventId=presentation.eventId;
            if(!session.TryAcceptMoralChoice(eventId,choice,out var reason)) { Diagnosis=reason; status.text=reason; return; }
            host.Persistence.RequestAutosave("Authored trailer choice accepted"); Close();
        }
        private void CompleteChoice()
        {
            if(!OwnsModal || accepted==null || !Ready) return;
            string eventId=accepted.eventId;
            if(!session.TryCompleteMoralChoice(eventId,out var reason)) { Diagnosis=reason; status.text=reason; return; }
            host.Persistence.RequestAutosave("Authored trailer choice completed"); Close();
        }
        private bool Interrupt() { arrival.Reset(); Close(); return true; }
        public void Close()
        {
            if(overlay!=null) overlay.gameObject.SetActive(false);
            presentation=null;
            var lease=modalLease; modalLease=null; lease?.Dispose();
        }
        private void OnEnable() => Attach();
        private void OnDisable()
        {
            Close(); ReleaseNavigation(); arrival.Reset();
            if(launcher!=null) launcher.SetActive(false);
            if(session!=null && subscribed)
            {
                session.MoralityChanged-=ReloadState; session.CareerEquipmentChanged-=OnCareerChanged;
                if(session.MoralEventContextSource==this) session.MoralEventContextSource=null;
            }
            subscribed=false;
        }
        private void OnDestroy()
        {
            if(overlay!=null) Destroy(overlay.gameObject);
            if(launcher!=null) Destroy(launcher);
        }
    }
}

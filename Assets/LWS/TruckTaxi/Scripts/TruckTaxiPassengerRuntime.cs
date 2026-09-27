using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LWS.TruckTaxi
{
    public sealed class TruckTaxiPassengerRuntime : MonoBehaviour
    {
        public TruckTaxiDialoguePlayer Dialogue { get; private set; }
        public TruckTaxiPassengerActor PrimaryActor { get; private set; }
        public TruckTaxiPassengerActor SecondaryActor { get; private set; }
        public TruckTaxiOversizedPassengerEffect Oversized { get; private set; }
        public float EjectionHoldProgress { get; private set; }
        public int EjectedBodies { get; private set; }
        public InputAction EjectAction { get; private set; }
        public bool uiEjectHeld;
        public const float EjectionHoldSeconds=1.2f;
        private TruckTaxiBootstrap host;
        private PassengerProfile passenger,second;
        private readonly TruckTaxiPassengerModifiers modifiers=new TruckTaxiPassengerModifiers();
        private TruckTaxiState observed=(TruckTaxiState)(-1);
        private Vector3 approachFrom;
        private float nextChatter, nextPair;
        private string pendingRequest;
        private bool arrived, destinationNotified;
        private bool revengeUsed, revengeActive;
        private Coroutine revengeRoutine, sketchyRoutine;
        private TaxiRequestProgress sketchyRequest;
        private bool sketchyReady;
        private GameObject sketchyContact, sketchyBag;
        public void Initialize(TruckTaxiBootstrap value)
        {
            host=value; Dialogue=gameObject.AddComponent<TruckTaxiDialoguePlayer>(); Oversized=gameObject.AddComponent<TruckTaxiOversizedPassengerEffect>();
            var driver=host.Player.GetComponent<TruckTaxiDriverPresentation>();
            if(driver==null) driver=host.Player.gameObject.AddComponent<TruckTaxiDriverPresentation>();
            driver.Initialize(host.Player.transform);
            EjectAction=new InputAction("Taxi Eject Passenger",InputActionType.Button,"<Keyboard>/f");
            EjectAction.AddBinding("<Gamepad>/select"); EjectAction.Enable();
            host.Session.Changed+=OnChanged; host.Session.PassengerEjected+=OnEjected;
            host.Session.RequestCreated+=OnRequest; host.Session.RequestResolved+=OnResolved;
            host.Session.DrivingEvent+=OnDriving; host.Session.DestinationChanged+=OnDestination;
            host.Session.StopStarted+=BeginSketchyPresentation;
            host.Session.StopPresentationReady=StopPresentationReady;
            host.Session.PassengerReadyToBoard=ReadyToBoard;
            OnChanged();
        }
        private void OnChanged()
        {
            var session=host.Session;
            if(observed==session.State) return;
            observed=session.State;
            switch(observed)
            {
                case TruckTaxiState.RideOffered:
                    Cleanup(); passenger=session.Passenger; second=passenger.pairPassenger!=null ? passenger.pairPassenger : passenger.companion;
                    PrimaryActor=Spawn(passenger); if(second!=null && second!=passenger) SecondaryActor=Spawn(second);
                    Vector3 point=session.Pickup.passengerSpawnPoint!=null ? session.Pickup.passengerSpawnPoint.position : session.Pickup.StopPosition+Vector3.right*(session.Pickup.detectionRadius*.8f);
                    PrimaryActor.transform.position=Ground(point);
                    PrimaryActor.WaitingTruckHit+=OnWaitingTruckHit;
                    PrimaryActor.ArmWaitingTruckHit(host.Player.GetComponent<Rigidbody>());
                    if(SecondaryActor!=null)
                    {
                        SecondaryActor.transform.position=Ground(point+Vector3.right*1.2f);
                        SecondaryActor.WaitingTruckHit+=OnWaitingTruckHit;
                        SecondaryActor.ArmWaitingTruckHit(host.Player.GetComponent<Rigidbody>());
                    }
                    arrived=false; destinationNotified=false; revengeUsed=revengeActive=false; Dialogue.ResetRide(); break;
                case TruckTaxiState.DrivingToPickup:
                    modifiers.Begin(host,passenger,SayMechanic);
                    Dialogue.Speak(passenger,session.IsRepeatPassenger ? TruckTaxiDialogueCategory.RepeatPickup : TruckTaxiDialogueCategory.PickupGreeting,
                        session,session.IsRepeatPassenger ? "Good to see you again. Same truck, next adventure." : "Your passenger is waiting at the pickup marker.",25);
                    if(PrimaryActor!=null) PrimaryActor.Animate(TruckTaxiPassengerAnimation.Wave_Taxi); break;
                case TruckTaxiState.PassengerBoarding:
                    PrimaryActor?.DisarmWaitingTruckHit();
                    SecondaryActor?.DisarmWaitingTruckHit();
                    if(PrimaryActor!=null) { approachFrom=PrimaryActor.transform.position; PrimaryActor.Animate(TruckTaxiPassengerAnimation.ApproachVehicle); }
                    break;
                case TruckTaxiState.DrivingToDestination:
                    if(!arrived)
                    {
                        Seat(PrimaryActor,passenger,false); Seat(SecondaryActor,second,true);
                        Oversized.Apply(host.Player.GetComponent<Rigidbody>(),passenger?.seatProfile);
                        host.Play(passenger?.seatProfile?.boardingAudio);
                        modifiers.Invoke(m=>m.OnPassengerBoarded());
                        Dialogue.Speak(passenger,TruckTaxiDialogueCategory.Boarding,session,"All aboard. Let's go.",30);
                        nextChatter=Time.time+14; nextPair=Time.time+7; arrived=true;
                    }
                    break;
                case TruckTaxiState.PassengerExiting:
                    if(!destinationNotified) { destinationNotified=true; modifiers.Invoke(m=>m.OnDestinationReached()); } break;
                case TruckTaxiState.RideComplete:
                    EndPresentations();
                    Oversized.Restore(); modifiers.Invoke(m=>m.OnRideCompleted()); modifiers.Cleanup();
                    Dialogue.Speak(passenger,TruckTaxiDialogueCategory.Arrival,session,"Thanks for the ride.",80);
                    ExitAtDestination(); break;
                case TruckTaxiState.RideFailed:
                    EndPresentations();
                    Oversized.Restore(); modifiers.Cleanup(); DestroyActors();
                    bool cancelled=session.RideHistory.Count>0 && session.RideHistory[session.RideHistory.Count-1].Outcome==TruckTaxiRideOutcome.PickupCancelled;
                    Dialogue.Speak(passenger,cancelled ? TruckTaxiDialogueCategory.PickupCancelled : TruckTaxiDialogueCategory.RideFailure,session,session.Reaction,85); break;
                case TruckTaxiState.Available:
                case TruckTaxiState.Inactive: Cleanup(); break;
            }
        }
        private TruckTaxiPassengerActor Spawn(PassengerProfile profile)
        {
            GameObject go;
            if(profile.runtimePrefab!=null) go=Instantiate(profile.runtimePrefab);
            else
            {
                go=new GameObject("Missing passenger model fallback");
                var body=GameObject.CreatePrimitive(PrimitiveType.Capsule); body.transform.SetParent(go.transform,false);
                body.transform.localPosition=Vector3.up*.9f; body.transform.localScale=new Vector3(.6f,.9f,.6f);
            }
            go.name="Taxi passenger "+profile.passengerId;
            var actor=go.GetComponent<TruckTaxiPassengerActor>(); if(actor==null) actor=go.AddComponent<TruckTaxiPassengerActor>();
            actor.Bind(profile); return actor;
        }
        private void Seat(TruckTaxiPassengerActor actor,PassengerProfile profile,bool secondary)
        {
            if(actor==null) return;
            TruckTaxiSeatProfile.PlaceActor(actor.transform,host.Player.transform,profile?.seatProfile,
                secondary,actor.StandingWorldScale);
            actor.Animate(TruckTaxiPassengerAnimation.BoardAbstraction);
        }
        private void ExitAtDestination()
        {
            if(PrimaryActor!=null)
            {
                PrimaryActor.transform.SetParent(null,true); PrimaryActor.transform.localScale=PrimaryActor.StandingWorldScale;
                var destination=host.Session.CurrentDesiredDestination ?? host.Session.Destination;
                PrimaryActor.transform.position=Ground(destination.StopPosition+host.Player.transform.right*6);
                PrimaryActor.Animate(TruckTaxiPassengerAnimation.ExitAbstraction);
            }
            if(SecondaryActor!=null) SecondaryActor.gameObject.SetActive(false);
        }
        private void OnEjected()
        {
            EndPresentations();
            Dialogue.Stop(); pendingRequest=null;
            modifiers.Invoke(m=>m.OnPassengerEjected()); modifiers.Cleanup(); Oversized.Restore();
            Dialogue.Speak(passenger,TruckTaxiDialogueCategory.EjectionReaction,host.Session,passenger.ejectionReaction,100);
            EjectActor(PrimaryActor,passenger,0); EjectActor(SecondaryActor,second,1);
            PrimaryActor=SecondaryActor=null; EjectionHoldProgress=0; uiEjectHeld=false;
        }
        private void EjectActor(TruckTaxiPassengerActor actor,PassengerProfile profile,int index)
        {
            if(actor==null) return;
            Transform truck=host.Player.transform;
            actor.transform.SetParent(null,true); actor.transform.localScale=actor.StandingWorldScale;
            actor.transform.SetPositionAndRotation(truck.position+truck.right*(2.8f+index)+Vector3.up*2,Quaternion.LookRotation(truck.forward,Vector3.up));
            actor.Eject(host.Player.GetComponent<Rigidbody>().linearVelocity+truck.right*(profile?.ejectionImpulse ?? 6)+Vector3.up*3,truck); EjectedBodies++;
        }
        private void OnRequest(TaxiRequestProgress request) { pendingRequest=request.Description; modifiers.Invoke(m=>m.OnRequestStarted(request)); }
        private void OnResolved(TaxiRequestProgress request)
        {
            if(request==sketchyRequest && sketchyRoutine!=null)
            {
                StopCoroutine(sketchyRoutine); sketchyRoutine=null;
                RestoreSketchyActor();
            }
            modifiers.Invoke(m=>m.OnRequestCompleted(request));
            Dialogue.Speak(passenger,request.State==TaxiRequestState.Succeeded ? TruckTaxiDialogueCategory.RequestSuccess : TruckTaxiDialogueCategory.RequestFailure,
                host.Session,request.State==TaxiRequestState.Succeeded ? "That's exactly what I asked for!" : "We missed that one.",75);
        }
        private void OnDestination()
        {
            if(host.Session.ActiveStop!=null) host.GPS.SetStopDestination(host.Session.ActiveStop.StopPoint);
            else host.GPS.SetRideDestination(host.Session.CurrentDesiredDestination);
            Dialogue.Speak(passenger,TruckTaxiDialogueCategory.GPSComplaint,host.Session,"I've updated our destination.",70);
        }
        private void OnDriving(TaxiEventType type)
        {
            modifiers.Invoke(m=>m.OnDrivingEvent(type));
            var category=type==TaxiEventType.PropDamage ? TruckTaxiDialogueCategory.PropertyDamageReaction :
                type==TaxiEventType.PedestrianHit ? TruckTaxiDialogueCategory.PedestrianHitReaction :
                type==TaxiEventType.NearMiss ? TruckTaxiDialogueCategory.NearMissReaction :
                type==TaxiEventType.Shortcut ? TruckTaxiDialogueCategory.ShortcutReaction :
                type==TaxiEventType.HardLanding ? TruckTaxiDialogueCategory.AirTimeReaction :
                passenger.chaosAffinity>0 ? TruckTaxiDialogueCategory.CollisionPositive : TruckTaxiDialogueCategory.CollisionNegative;
            Dialogue.Speak(passenger,category,host.Session,host.Session.Reaction,60);
        }
        public void SayMechanic(string text) => Dialogue.Speak(passenger,TruckTaxiDialogueCategory.UniqueMechanicReaction,host.Session,text,45);
        private void OnWaitingTruckHit(TruckTaxiPassengerActor struck,Vector3 velocity)
        {
            if(revengeActive || struck==null || host.Session.State!=TruckTaxiState.DrivingToPickup) return;
            bool throwJug=!revengeUsed;
            revengeUsed=true;
            revengeActive=true;
            if(throwJug) host.Session.SetPickupPatienceSuspended(this,true);
            revengeRoutine=StartCoroutine(WaitingRevenge(struck,struck==SecondaryActor ? second : passenger,velocity,throwJug));
        }
        private IEnumerator WaitingRevenge(TruckTaxiPassengerActor actor,PassengerProfile speaker,Vector3 velocity,bool throwJug)
        {
            Vector3 standing=actor.transform.position;
            actor.BeginLightTumble(velocity);
            yield return new WaitForSeconds(.8f);
            if(actor==null) yield break;
            actor.EndLightTumble(Ground(standing));
            if(throwJug)
            {
                Dialogue.Speak(speaker,TruckTaxiDialogueCategory.PickupRevenge,host.Session,
                    speaker.passengerName+": You hit me before I even got in. Catch this!",95);
                yield return new WaitForSeconds(.5f);
                if(actor!=null && host.Player!=null)
                {
                    Vector3 origin=actor.transform.position+Vector3.up*1.3f;
                    TruckTaxiNpcProjectileLauncher.LaunchNpcProjectile(origin,
                        host.Player.transform.position+Vector3.up*1.2f,null,TruckTaxiNpcProjectileStyle.FilledJug);
                    actor.Animate(TruckTaxiPassengerAnimation.Wave_Taxi);
                }
                yield return new WaitForSeconds(.75f);
            }
            revengeActive=false; revengeRoutine=null;
            if(throwJug) host.Session.SetPickupPatienceSuspended(this,false);
            if(actor!=null && host.Session.State==TruckTaxiState.DrivingToPickup)
            {
                actor.ArmWaitingTruckHit(host.Player.GetComponent<Rigidbody>());
                actor.Animate(TruckTaxiPassengerAnimation.Wave_Taxi);
            }
        }
        public void BeginSketchyPresentation(TaxiRequestProgress request)
        {
            if(request?.Definition?.requestType!=TaxiRequestType.IllicitStop ||
                request.StopPoint==null || sketchyRoutine!=null || PrimaryActor==null ||
                host.Session.State!=TruckTaxiState.DrivingToDestination) return;
            sketchyRequest=request; sketchyReady=false;
            sketchyRoutine=StartCoroutine(SketchyPresentation(request));
        }
        public bool StopPresentationReady(TaxiRequestProgress request) =>
            request?.Definition?.requestType!=TaxiRequestType.IllicitStop ||
            request==sketchyRequest && sketchyReady;
        private IEnumerator SketchyPresentation(TaxiRequestProgress request)
        {
            var actor=PrimaryActor;
            Transform truck=host.Player.transform;
            Vector3 exit=Ground(truck.position+truck.right*2.8f);
            Vector3 contactPoint=Ground(request.StopPoint.Position+request.StopPoint.viewDirection.normalized*3f);
            actor.transform.SetParent(null,true);
            actor.transform.localScale=actor.StandingWorldScale;
            actor.transform.position=exit;
            actor.Animate(TruckTaxiPassengerAnimation.Walk_Normal);
            Dialogue.Speak(passenger,TruckTaxiDialogueCategory.SketchyExit,host.Session,
                "Give me a moment. This is a very normal errand.",90);
            sketchyContact=CreateSketchyContact(contactPoint);
            while(actor!=null && Vector3.Distance(actor.transform.position,contactPoint)>1.3f)
            {
                if(!StillAtSketchyStop(request)) { RestoreSketchyActor(); sketchyRoutine=null; yield break; }
                actor.transform.position=Vector3.MoveTowards(actor.transform.position,contactPoint,2f*Time.deltaTime);
                actor.transform.rotation=Quaternion.LookRotation(Vector3.ProjectOnPlane(contactPoint-actor.transform.position,Vector3.up));
                yield return null;
            }
            if(actor==null) yield break;
            actor.Animate(TruckTaxiPassengerAnimation.Idle_Normal);
            sketchyContact.transform.rotation=Quaternion.LookRotation(Vector3.ProjectOnPlane(actor.transform.position-contactPoint,Vector3.up));
            sketchyBag=GameObject.CreatePrimitive(PrimitiveType.Cube);
            sketchyBag.name="Sketchy stop unspecified package";
            sketchyBag.transform.localScale=Vector3.one*.28f;
            sketchyBag.GetComponent<Collider>().enabled=false;
            Dialogue.Speak(passenger,TruckTaxiDialogueCategory.SketchyDance,host.Session,
                "Don't ask about the dance.",90);
            for(float elapsed=0;elapsed<3f;elapsed+=Time.deltaTime)
            {
                if(!StillAtSketchyStop(request)) { RestoreSketchyActor(); sketchyRoutine=null; yield break; }
                float sway=Mathf.Sin(elapsed*6f)*12f;
                actor.transform.rotation=Quaternion.LookRotation(Vector3.ProjectOnPlane(contactPoint-actor.transform.position,Vector3.up))*Quaternion.Euler(0,sway,0);
                sketchyContact.transform.rotation=Quaternion.LookRotation(Vector3.ProjectOnPlane(actor.transform.position-contactPoint,Vector3.up))*Quaternion.Euler(0,-sway,0);
                sketchyBag.transform.position=Vector3.Lerp(actor.transform.position,contactPoint,.5f)+Vector3.up*1.1f;
                yield return null;
            }
            Destroy(sketchyBag); sketchyBag=null;
            Destroy(sketchyContact); sketchyContact=null;
            actor.Animate(TruckTaxiPassengerAnimation.Walk_Normal);
            Dialogue.Speak(passenger,TruckTaxiDialogueCategory.SketchyReturn,host.Session,
                "All done. Let's get out of here.",90);
            while(actor!=null && Vector3.Distance(actor.transform.position,exit)>.25f)
            {
                if(!StillAtSketchyStop(request)) { RestoreSketchyActor(); sketchyRoutine=null; yield break; }
                actor.transform.position=Vector3.MoveTowards(actor.transform.position,exit,2f*Time.deltaTime);
                actor.transform.rotation=Quaternion.LookRotation(Vector3.ProjectOnPlane(exit-actor.transform.position,Vector3.up));
                yield return null;
            }
            if(actor!=null) Seat(actor,passenger,false);
            sketchyReady=true; sketchyRoutine=null;
        }
        private bool StillAtSketchyStop(TaxiRequestProgress request) =>
            host?.Player!=null && host.Session.State==TruckTaxiState.DrivingToDestination &&
            request?.State==TaxiRequestState.Active && request.StopPoint!=null &&
            request.StopPoint.IsValidStop(host.Player.transform.position,
                host.Player.GetComponent<Rigidbody>().linearVelocity.magnitude);
        private void RestoreSketchyActor()
        {
            if(PrimaryActor!=null && host?.Player!=null &&
                !PrimaryActor.transform.IsChildOf(host.Player.transform) && host.Session.HasPassenger)
                Seat(PrimaryActor,passenger,false);
            if(sketchyContact!=null) Destroy(sketchyContact);
            if(sketchyBag!=null) Destroy(sketchyBag);
            sketchyContact=sketchyBag=null;
            sketchyReady=false;
        }
        private GameObject CreateSketchyContact(Vector3 position)
        {
            var contact=new GameObject("Sketchy stop Wobble contact");
            contact.transform.position=position;
            var shader=Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var coat=new Material(shader) { color=new Color(.23f,.31f,.29f) };
            var dark=new Material(shader) { color=new Color(.13f,.13f,.16f) };
            var skin=new Material(shader) { color=new Color(.72f,.49f,.32f) };
            TruckTaxiWobbleVisual.CreateThemed(contact.transform,new TruckTaxiWobbleVisual.Design {
                silhouette=TruckTaxiWobbleVisual.Silhouette.Human,headwear=TruckTaxiWobbleVisual.Headwear.Hood,
                accessory=TruckTaxiWobbleVisual.Accessory.None,height=1.65f,width=1f,headScale=1f,
                upperBodyScale=1f,clothedAdult=true,body=coat,clothes=dark,accent=skin,skin=skin
            });
            foreach(var collider in contact.GetComponentsInChildren<Collider>()) collider.enabled=false;
            Destroy(coat,12f); Destroy(dark,12f); Destroy(skin,12f);
            return contact;
        }
        private void EndPresentations()
        {
            if(revengeRoutine!=null) { StopCoroutine(revengeRoutine); revengeRoutine=null; }
            if(PrimaryActor!=null && PrimaryActor.IsTumbling) PrimaryActor.EndLightTumble(Ground(PrimaryActor.transform.position));
            if(SecondaryActor!=null && SecondaryActor.IsTumbling) SecondaryActor.EndLightTumble(Ground(SecondaryActor.transform.position));
            PrimaryActor?.DisarmWaitingTruckHit();
            SecondaryActor?.DisarmWaitingTruckHit();
            if(sketchyRoutine!=null) { StopCoroutine(sketchyRoutine); sketchyRoutine=null; }
            RestoreSketchyActor();
            if(host?.Session!=null) host.Session.SetPickupPatienceSuspended(this,false);
            revengeActive=false; sketchyRequest=null; sketchyReady=false;
        }
        public bool RequestEjection()
        {
            if(host==null || host.Paused) return false;
            if(host.Session.HasPassenger) return host.Session.EjectPassenger();
            return host.Companions?.Eject()==true;
        }
        private void Update()
        {
            if(host?.Session==null) return;
            Dialogue.SetPaused(host.Paused && host.Session.State!=TruckTaxiState.RideComplete && host.Session.State!=TruckTaxiState.RideFailed);
            bool throwChord=Gamepad.current!=null && Gamepad.current.selectButton.isPressed && Gamepad.current.rightShoulder.isPressed;
            bool fareEjectable=host.Session.HasPassenger && passenger!=null && passenger.canBeEjected;
            bool companionEjectable=host.Companions?.CanEject==true;
            bool hold=!throwChord && !host.Paused && (fareEjectable || companionEjectable) &&
                (uiEjectHeld || EjectAction.IsPressed());
            EjectionHoldProgress=hold ? Mathf.Min(1,EjectionHoldProgress+Time.unscaledDeltaTime/EjectionHoldSeconds) : 0;
            if(EjectionHoldProgress>=1) RequestEjection();
            if(host.Paused) return;
            if(host.Session.State==TruckTaxiState.PassengerBoarding && PrimaryActor!=null && !revengeActive)
            {
                Vector3 near=Ground(host.Player.transform.position+host.Player.transform.right*2.5f);
                float speed=passenger.seatProfile!=null ? passenger.seatProfile.approachSpeed : 2.5f;
                PrimaryActor.transform.position=Vector3.MoveTowards(PrimaryActor.transform.position,near,speed*Time.deltaTime);
                if((near-approachFrom).sqrMagnitude>.1f) PrimaryActor.transform.rotation=Quaternion.LookRotation(Vector3.ProjectOnPlane(near-approachFrom,Vector3.up));
                if(SecondaryActor!=null) SecondaryActor.transform.position=Vector3.MoveTowards(SecondaryActor.transform.position,near+host.Player.transform.forward,speed*Time.deltaTime);
            }
            if(!host.Session.HasPassenger) return;
            modifiers.Invoke(m=>m.OnRideTick(Time.deltaTime));
            if(Dialogue.IsPlaying) return;
            if(!string.IsNullOrWhiteSpace(pendingRequest))
            { Dialogue.Speak(passenger,TruckTaxiDialogueCategory.RequestIntroduction,host.Session,pendingRequest,50); pendingRequest=null; }
            else if(second!=null && Time.time>=nextPair)
            { Dialogue.Speak(second,TruckTaxiDialogueCategory.PairPassengerReaction,host.Session,"I heard that. We are discussing your driving after this.",20); nextPair=Time.time+22; }
            else if(Time.time>=nextChatter)
            { Dialogue.Speak(passenger,TruckTaxiDialogueCategory.GeneralChatter,host.Session,passenger.personality,10); nextChatter=Time.time+18; }
        }
        private Vector3 Ground(Vector3 point)
        {
            if(Physics.Raycast(point+Vector3.up*12,Vector3.down,out var hit,40,~0,QueryTriggerInteraction.Ignore) && hit.collider.attachedRigidbody==null) return hit.point;
            return new Vector3(point.x,host.Session.Pickup!=null ? host.Session.Pickup.StopPosition.y : point.y,point.z);
        }
        private bool ReadyToBoard()
        {
            if(revengeActive) return false;
            if(PrimaryActor==null) return true;
            Vector3 near=host.Player.transform.position+host.Player.transform.right*2.5f;
            float threshold=passenger.seatProfile!=null ? passenger.seatProfile.boardingDistance : 2;
            return Vector3.ProjectOnPlane(PrimaryActor.transform.position-near,Vector3.up).magnitude<=threshold;
        }
        private void DestroyActors() { if(PrimaryActor!=null) { PrimaryActor.WaitingTruckHit-=OnWaitingTruckHit; Destroy(PrimaryActor.gameObject); } if(SecondaryActor!=null) { SecondaryActor.WaitingTruckHit-=OnWaitingTruckHit; Destroy(SecondaryActor.gameObject); } PrimaryActor=SecondaryActor=null; }
        private void Cleanup() { EndPresentations(); modifiers.Cleanup(); Oversized?.Restore(); Dialogue?.ResetRide(); DestroyActors(); passenger=second=null; pendingRequest=null; uiEjectHeld=false; EjectionHoldProgress=0; }
        private void OnDestroy()
        {
            if(host?.Session!=null)
            {
                host.Session.Changed-=OnChanged; host.Session.PassengerEjected-=OnEjected; host.Session.RequestCreated-=OnRequest;
                host.Session.RequestResolved-=OnResolved; host.Session.DrivingEvent-=OnDriving; host.Session.DestinationChanged-=OnDestination;
                host.Session.StopStarted-=BeginSketchyPresentation;
                if(host.Session.StopPresentationReady==StopPresentationReady) host.Session.StopPresentationReady=null;
                host.Session.PassengerReadyToBoard=null;
            }
            Cleanup(); EjectAction?.Dispose();
        }
    }
}

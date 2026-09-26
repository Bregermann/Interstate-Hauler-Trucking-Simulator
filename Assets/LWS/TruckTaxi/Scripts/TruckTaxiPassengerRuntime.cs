using System;
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
        public void Initialize(TruckTaxiBootstrap value)
        {
            host=value; Dialogue=gameObject.AddComponent<TruckTaxiDialoguePlayer>(); Oversized=gameObject.AddComponent<TruckTaxiOversizedPassengerEffect>();
            EjectAction=new InputAction("Taxi Eject Passenger",InputActionType.Button,"<Keyboard>/f");
            EjectAction.AddBinding("<Gamepad>/select"); EjectAction.Enable();
            host.Session.Changed+=OnChanged; host.Session.PassengerEjected+=OnEjected;
            host.Session.RequestCreated+=OnRequest; host.Session.RequestResolved+=OnResolved;
            host.Session.DrivingEvent+=OnDriving; host.Session.DestinationChanged+=OnDestination;
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
                    if(SecondaryActor!=null) SecondaryActor.transform.position=Ground(point+Vector3.right*1.2f);
                    arrived=false; destinationNotified=false; Dialogue.ResetRide(); break;
                case TruckTaxiState.DrivingToPickup:
                    modifiers.Begin(host,passenger,SayMechanic);
                    Dialogue.Speak(passenger,TruckTaxiDialogueCategory.PickupGreeting,session,"Your passenger is waiting at the pickup marker.",25);
                    if(PrimaryActor!=null) PrimaryActor.Animate(TruckTaxiPassengerAnimation.Wave_Taxi); break;
                case TruckTaxiState.PassengerBoarding:
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
                    Oversized.Restore(); modifiers.Invoke(m=>m.OnRideCompleted()); modifiers.Cleanup();
                    Dialogue.Speak(passenger,TruckTaxiDialogueCategory.Arrival,session,"Thanks for the ride.",80);
                    ExitAtDestination(); break;
                case TruckTaxiState.RideFailed:
                    Oversized.Restore(); modifiers.Cleanup(); DestroyActors();
                    Dialogue.Speak(passenger,TruckTaxiDialogueCategory.RideFailure,session,session.Reaction,85); break;
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
            var seat=profile?.seatProfile;
            Transform parent=seat!=null && !string.IsNullOrWhiteSpace(seat.mountPath) ? host.Player.transform.Find(seat.mountPath) : null;
            if(parent==null) parent=host.Player.transform;
            actor.transform.SetParent(parent,false);
            actor.transform.localPosition=(seat!=null ? seat.localPosition : new Vector3(.5f,1,0))+(secondary ? new Vector3(.15f,0,.12f) : Vector3.zero);
            actor.transform.localRotation=Quaternion.Euler(seat!=null ? seat.localEulerAngles : Vector3.zero);
            actor.transform.localScale=seat!=null ? seat.localScale : Vector3.one;
            actor.Animate(TruckTaxiPassengerAnimation.BoardAbstraction);
        }
        private void ExitAtDestination()
        {
            if(PrimaryActor!=null)
            {
                PrimaryActor.transform.SetParent(null,true); PrimaryActor.transform.localScale=Vector3.one;
                PrimaryActor.transform.position=Ground(host.Session.Destination.StopPosition+host.Player.transform.right*6);
                PrimaryActor.Animate(TruckTaxiPassengerAnimation.ExitAbstraction);
            }
            if(SecondaryActor!=null) SecondaryActor.gameObject.SetActive(false);
        }
        private void OnEjected()
        {
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
            actor.transform.SetParent(null,true); actor.transform.localScale=Vector3.one;
            actor.transform.SetPositionAndRotation(truck.position+truck.right*(2.8f+index)+Vector3.up*2,Quaternion.LookRotation(truck.forward,Vector3.up));
            actor.Eject(host.Player.GetComponent<Rigidbody>().linearVelocity+truck.right*(profile?.ejectionImpulse ?? 6)+Vector3.up*3,truck); EjectedBodies++;
        }
        private void OnRequest(TaxiRequestProgress request) { pendingRequest=request.Description; modifiers.Invoke(m=>m.OnRequestStarted(request)); }
        private void OnResolved(TaxiRequestProgress request)
        {
            modifiers.Invoke(m=>m.OnRequestCompleted(request));
            Dialogue.Speak(passenger,request.State==TaxiRequestState.Succeeded ? TruckTaxiDialogueCategory.RequestSuccess : TruckTaxiDialogueCategory.RequestFailure,
                host.Session,request.State==TaxiRequestState.Succeeded ? "That's exactly what I asked for!" : "We missed that one.",75);
        }
        private void OnDestination()
        {
            if(host.Session.ActiveStop!=null) host.GPS.SetStopDestination(host.Session.ActiveStop.StopPoint);
            else host.GPS.SetRideDestination(host.Session.Destination);
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
        public bool RequestEjection() => host!=null && !host.Paused && host.Session.EjectPassenger();
        private void Update()
        {
            if(host?.Session==null) return;
            Dialogue.SetPaused(host.Paused && host.Session.State!=TruckTaxiState.RideComplete && host.Session.State!=TruckTaxiState.RideFailed);
            bool hold=!host.Paused && host.Session.HasPassenger && passenger!=null && passenger.canBeEjected && (uiEjectHeld || EjectAction.IsPressed());
            EjectionHoldProgress=hold ? Mathf.Min(1,EjectionHoldProgress+Time.unscaledDeltaTime/EjectionHoldSeconds) : 0;
            if(EjectionHoldProgress>=1) RequestEjection();
            if(host.Paused) return;
            if(host.Session.State==TruckTaxiState.PassengerBoarding && PrimaryActor!=null)
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
            if(PrimaryActor==null) return true;
            Vector3 near=host.Player.transform.position+host.Player.transform.right*2.5f;
            float threshold=passenger.seatProfile!=null ? passenger.seatProfile.boardingDistance : 2;
            return Vector3.ProjectOnPlane(PrimaryActor.transform.position-near,Vector3.up).magnitude<=threshold;
        }
        private void DestroyActors() { if(PrimaryActor!=null) Destroy(PrimaryActor.gameObject); if(SecondaryActor!=null) Destroy(SecondaryActor.gameObject); PrimaryActor=SecondaryActor=null; }
        private void Cleanup() { modifiers.Cleanup(); Oversized?.Restore(); Dialogue?.ResetRide(); DestroyActors(); passenger=second=null; pendingRequest=null; uiEjectHeld=false; EjectionHoldProgress=0; }
        private void OnDestroy()
        {
            if(host?.Session!=null)
            {
                host.Session.Changed-=OnChanged; host.Session.PassengerEjected-=OnEjected; host.Session.RequestCreated-=OnRequest;
                host.Session.RequestResolved-=OnResolved; host.Session.DrivingEvent-=OnDriving; host.Session.DestinationChanged-=OnDestination;
                host.Session.PassengerReadyToBoard=null;
            }
            Cleanup(); EjectAction?.Dispose();
        }
    }
}

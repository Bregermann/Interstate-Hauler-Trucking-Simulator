using System;
using System.Collections.Generic;
using UnityEngine;

namespace LWS.TruckTaxi
{
    public interface ITruckTaxiPassengerModifier
    {
        void OnRideStarted(); void OnPassengerBoarded(); void OnRideTick(float dt);
        void OnDrivingEvent(TaxiEventType type); void OnRequestStarted(TaxiRequestProgress request);
        void OnRequestCompleted(TaxiRequestProgress request); void OnDestinationReached();
        void OnPassengerEjected(); void OnRideCompleted(); void OnRideCleanedUp();
    }
    public sealed class TruckTaxiPassengerModifiers
    {
        private readonly List<ITruckTaxiPassengerModifier> active=new List<ITruckTaxiPassengerModifier>();
        public void Begin(TruckTaxiBootstrap host,PassengerProfile passenger,Action<string> say)
        {
            Cleanup();
            foreach(var kind in passenger.uniqueMechanics ?? Array.Empty<TruckTaxiMechanic>())
                active.Add(new AuthoredModifier(kind,host,say));
            Invoke(m=>m.OnRideStarted());
        }
        public void Invoke(Action<ITruckTaxiPassengerModifier> call)
        {
            for(int i=active.Count-1;i>=0;i--)
                try { call(active[i]); }
                catch(Exception ex)
                {
                    try { active[i].OnRideCleanedUp(); } catch(Exception) { }
                    Debug.LogWarning("Truck Taxi disabled only the failed passenger modifier: "+ex.Message);
                    active.RemoveAt(i);
                }
        }
        public void Cleanup() { Invoke(m=>m.OnRideCleanedUp()); active.Clear(); }

        private sealed class AuthoredModifier : ITruckTaxiPassengerModifier
        {
            private readonly TruckTaxiMechanic kind;
            private readonly TruckTaxiBootstrap host;
            private readonly Action<string> say;
            private float age,nextComment;
            private int combo,power;
            private bool changedDestination;
            public AuthoredModifier(TruckTaxiMechanic kind,TruckTaxiBootstrap host,Action<string> say) { this.kind=kind; this.host=host; this.say=say; }
            public void OnRideStarted() { age=0; nextComment=12; combo=power=0; changedDestination=false; }
            public void OnPassengerBoarded()
            {
                if(kind==TruckTaxiMechanic.FeeCollector) { host.Session.ApplyMechanicReward(0,-150); say("There is a modest fee for the privilege of carrying me."); }
            }
            public void OnRideTick(float dt)
            {
                age+=dt;
                if(kind==TruckTaxiMechanic.DestinationChanger && age>=35 && !changedDestination)
                { changedDestination=host.Session.ChangeDestination(); if(changedDestination) say("New plan. I've changed the destination. Follow the updated GPS."); }
                if(age<nextComment) return;
                nextComment=age+18;
                if(kind==TruckTaxiMechanic.SportsCommentator) say("The tractor finds a lane. That is tremendous field position!");
                if(kind==TruckTaxiMechanic.DesignAnalyst) say("A fascinating compromise between turning radius and public confidence.");
                if(kind==TruckTaxiMechanic.BackseatDriver) say("Eyes on the GPS. Then the road. Preferably both.");
                if(kind==TruckTaxiMechanic.TimeObsessed && age>host.Session.Passenger.basePatience*.5f)
                { host.Session.ApplyMechanicReward(0,-25); say("The clock is charging us by the second."); }
            }
            public void OnDrivingEvent(TaxiEventType type)
            {
                if(kind==TruckTaxiMechanic.PropertyDestructionCat && type==TaxiEventType.PropDamage)
                { host.Session.ApplyMechanicReward(150,100); say("Mrrp. That object was in the wrong place."); }
                if(kind==TruckTaxiMechanic.FeeCollector && type==TaxiEventType.Shortcut) host.Session.ApplyMechanicReward(0,-100);
                if(kind==TruckTaxiMechanic.PowerLevel) { power=Mathf.Min(9000,power+100); host.Session.ApplyMechanicReward(25,0); say("Your driving power is now "+power+". Still inadequate!"); }
                if(kind==TruckTaxiMechanic.StyleCombo)
                { combo=Mathf.Min(10,combo+1); host.Session.ApplyMechanicReward(combo*10,combo*5); }
                if(kind==TruckTaxiMechanic.SportsCommentator) say(type==TaxiEventType.NearMiss ? "Threading the gap! Nobody lays a hand on that tractor!" : "A huge play! The insurance adjuster will review the replay.");
            }
            public void OnRequestStarted(TaxiRequestProgress request) { }
            public void OnRequestCompleted(TaxiRequestProgress request)
            { if(kind==TruckTaxiMechanic.StyleCombo && request.State==TaxiRequestState.Succeeded) host.Session.ApplyMechanicReward(50,50); }
            public void OnDestinationReached() { if(kind==TruckTaxiMechanic.StyleCombo) host.Session.ApplyMechanicReward(combo*20,combo*10); }
            public void OnPassengerEjected() { }
            public void OnRideCompleted() { }
            public void OnRideCleanedUp() { }
        }
    }
}

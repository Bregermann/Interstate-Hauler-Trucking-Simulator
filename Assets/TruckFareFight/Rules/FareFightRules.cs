using System;
using System.Collections.Generic;
namespace LWS.TruckFareFight
{
    public enum FareFightPhase { Waiting, Carried, Announcement, Complete }
    public enum FareFightEventKind { CarrierReset, Delivery, Theft, Pickup }
    public struct FareFightEvent
    {
        public FareFightEventKind kind;
        public int participant, victim, passenger;
        public float fraction, normalSpeed;
    }
    // One authority for the contested passenger. Physics supplies ordered,
    // validated events; navigation and presentation only observe this state.
    public sealed class FareFightRules
    {
        public readonly int[] score = new int[4], pickups = new int[4], steals = new int[4];
        public readonly bool[] winners = new bool[4], suddenDeathLeaders = new bool[4];
        public FareFightPhase Phase { get; private set; }
        public int Count { get; private set; }
        public int Target { get; private set; }
        public int Carrier { get; private set; } = -1;
        public int Passenger { get; private set; }
        public int PickupLocation { get; private set; } = -1;
        public int Destination { get; private set; } = -1;
        public int Revision { get; private set; }
        public int Deliveries { get; private set; }
        public int Resets { get; private set; }
        public float Clock { get; private set; }
        public float TimeLimit { get; private set; }
        public float TransferCooldown => Math.Max(0, transferReady - Clock);
        public bool SuddenDeath { get; private set; }
        public bool NeedsPassenger => Phase == FareFightPhase.Waiting && Destination < 0;
        public bool Practice { get; private set; }
        public float minimumStealSpeed = 1.5f, transferSeconds = 1.25f;
        float transferReady, announceUntil;
        public string LastEvent { get; private set; }
        public void Start(int count, int target = 5, float seconds = 0, bool practice = false)
        {
            if (count < 1 || count > 4 || target < 1 || seconds < 0 || !Finite(seconds)) throw new ArgumentOutOfRangeException();
            Count = count; Target = target; TimeLimit = seconds; Practice = practice && count == 1;
            Carrier = PickupLocation = Destination = -1; Passenger = Deliveries = Resets = 0;
            Clock = transferReady = announceUntil = 0; SuddenDeath = false; Phase = FareFightPhase.Waiting;
            Array.Clear(score, 0, 4); Array.Clear(pickups, 0, 4); Array.Clear(steals, 0, 4);
            Array.Clear(winners, 0, 4); Array.Clear(suddenDeathLeaders, 0, 4);
            LastEvent = "NEW FARE"; Revision++;
        }
        public bool AssignPassenger(int pickup, int destination)
        {
            if (!NeedsPassenger || pickup < 0 || destination < 0 || pickup == destination) return false;
            PickupLocation = pickup; Destination = destination; Passenger++; Revision++; return true;
        }
        public bool Eligible(int slot) => slot >= 0 && slot < Count && (!SuddenDeath || suddenDeathLeaders[slot]);
        public void Advance(float dt, List<FareFightEvent> events)
        {
            if (!Finite(dt) || dt < 0) throw new ArgumentOutOfRangeException(nameof(dt));
            if (Phase == FareFightPhase.Complete) return;
            float start = Clock, end = Clock + dt;
            events.Sort((a, b) => { int order = a.fraction.CompareTo(b.fraction); if (order != 0) return order; order = a.kind.CompareTo(b.kind); return order != 0 ? order : a.participant.CompareTo(b.participant); });
            foreach (var action in events)
            {
                if (!Finite(action.fraction) || action.fraction < 0 || action.fraction > 1) continue;
                Clock = start + action.fraction * dt; CheckTimer();
                if (Phase == FareFightPhase.Complete) break;
                if (action.passenger != Passenger || !Eligible(action.participant) || Destination < 0) continue;
                switch (action.kind)
                {
                    case FareFightEventKind.Pickup:
                        if (Phase != FareFightPhase.Waiting) break;
                        Carrier = action.participant; Phase = FareFightPhase.Carried; pickups[Carrier]++;
                        transferReady = Clock + transferSeconds; LastEvent = "PICKED UP"; Revision++; break;
                    case FareFightEventKind.Theft:
                        if (Phase != FareFightPhase.Carried || Carrier != action.victim || Carrier == action.participant ||
                            Clock < transferReady || !Finite(action.normalSpeed) || action.normalSpeed < minimumStealSpeed) break;
                        Carrier = action.participant; steals[Carrier]++; transferReady = Clock + transferSeconds;
                        LastEvent = "PASSENGER STOLEN!"; Revision++; break;
                    case FareFightEventKind.CarrierReset:
                        if (Phase != FareFightPhase.Carried || Carrier != action.participant) break;
                        Carrier = -1; Phase = FareFightPhase.Waiting; Resets++; transferReady = Clock;
                        LastEvent = "PASSENGER RESET"; Revision++; break;
                    case FareFightEventKind.Delivery:
                        if (Phase != FareFightPhase.Carried || Carrier != action.participant) break;
                        int scorer = Carrier; score[scorer]++; Deliveries++; Carrier = -1;
                        LastEvent = "DELIVERED"; Revision++;
                        if (SuddenDeath || !Practice && TimeLimit == 0 && score[scorer] >= Target)
                        { Phase = FareFightPhase.Complete; winners[scorer] = true; }
                        else { Phase = FareFightPhase.Announcement; announceUntil = Clock + 1.25f; }
                        break;
                }
            }
            Clock = end; CheckTimer();
            if (Phase == FareFightPhase.Announcement && Clock >= announceUntil)
            { Phase = FareFightPhase.Waiting; PickupLocation = Destination = -1; Revision++; }
        }
        void CheckTimer()
        {
            if (Phase == FareFightPhase.Complete || TimeLimit <= 0 || SuddenDeath || Clock < TimeLimit) return;
            int best = int.MinValue, leaders = 0;
            for (int i = 0; i < Count; i++) best = Math.Max(best, score[i]);
            for (int i = 0; i < Count; i++) { suddenDeathLeaders[i] = score[i] == best; if (suddenDeathLeaders[i]) leaders++; }
            if (leaders == 1 || Count == 1)
            { Phase = FareFightPhase.Complete; Array.Copy(suddenDeathLeaders, winners, 4); Carrier = -1; }
            else
            {
                SuddenDeath = true; LastEvent = "SUDDEN DEATH · TIED LEADERS · NEXT DELIVERY WINS";
                if (Carrier >= 0 && !suddenDeathLeaders[Carrier]) { Carrier = -1; Phase = FareFightPhase.Waiting; Resets++; }
            }
            Revision++;
        }
        static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}

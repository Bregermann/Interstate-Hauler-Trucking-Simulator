using System;
using UnityEngine;

namespace LWS.TruckTaxi
{
    public enum TruckTaxiPassengerAnimation { Idle_Normal, Idle_Impatient, Idle_Angry, Idle_Nervous, Idle_Drunk, Idle_Excited,
        Walk_Normal, Walk_Fast, Run, Wait_Phone, Wave_Taxi, Point, Complain, Cheer, Panic, Celebrate, AngryGesture,
        ApproachVehicle, BoardAbstraction, ExitAbstraction, Passenger_Ejected, Passenger_Ragdoll }
    [Serializable] public sealed class TruckTaxiAnimationBinding
    {
        public TruckTaxiPassengerAnimation action;
        public AnimationClip clip;
        public string stateName;
    }
    [CreateAssetMenu(menuName = "Truck Taxi/Passengers/Animation")]
    public sealed class TruckTaxiAnimatorProfile : ScriptableObject
    {
        public RuntimeAnimatorController controller;
        public TruckTaxiAnimationBinding[] animations = Array.Empty<TruckTaxiAnimationBinding>();
        public bool allowProceduralFallback = true;
        public string StateFor(TruckTaxiPassengerAnimation action)
        {
            foreach (var binding in animations) if (binding != null && binding.action == action) return binding.stateName;
            return "Idle_Normal";
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

namespace LWS.TruckDragonKingdom
{
    public enum DragonAnimationAction
    {
        GroundIdle, LookAround, Walk, Run, TurnLeft, TurnRight, Sleep, Wake, Roar,
        Takeoff, Fly, FlyIdle, Glide, BankLeft, BankRight, Dive, Land, Perch, Breath,
        ClawAttack, TailAttack, Grab, Carry, Drop, Eat, InspectEgg, Nest, Hatch,
        Damage, Defeat, Retreat,
        GroundGrab // append only: all previously serialized action IDs remain unchanged
    }

    [Flags]
    public enum DragonSpeciesRole
    {
        None = 0, Ground = 1, Flying = 2, Baby = 4, RiderMount = 8,
        HoardGuardian = 16, EggGuardian = 32, SheepCarrier = 64
    }

    public enum DragonElement { None, Fire, Ice, Lightning, Earth }
    public enum DragonTemperament { Curious, Protective, Territorial, Aggressive }

    [Serializable]
    public sealed class DragonAnimationMapping
    {
        public DragonAnimationAction action;
        public AnimationClip clip;
        public bool loop;
        [Min(0.01f)] public float playbackSpeed = 1;
        [Tooltip("Exact owned clip or authored rig-animation provenance; a mapping is not rendered verification.")]
        public string sourceNote;
    }

    [Serializable]
    public sealed class DragonSpeciesDefinition
    {
        public string id;
        public string displayName;
        public GameObject visualPrefab;
        public DragonSpeciesRole supportedRoles;
        [Min(0.001f)] public float visualScale = 1;
        [Min(0)] public float groundSpeed = 6;
        [Min(0)] public float flightSpeed = 30;
        [Min(0)] public float flightAcceleration = 8;
        [Min(0)] public float turnRate = 60;
        [Range(0, 80)] public float bankAngle = 30;
        public DragonElement element;
        [Min(0.01f)] public float maximumStamina = 100;
        public DragonTemperament temperament;
        public string mouthBone = "RigJaw";
        public string leftGripBone = "RigLArmPalm";
        public string rightGripBone = "RigRArmPalm";
        public string saddleBone = "RigSpine2";
        public GameObject breathEffect;
        public AudioClip[] sounds = Array.Empty<AudioClip>();
        public DragonAnimationMapping[] animations = Array.Empty<DragonAnimationMapping>();

        public bool Supports(DragonSpeciesRole role) => (supportedRoles & role) == role;

        public bool TryGetAnimation(DragonAnimationAction action, out DragonAnimationMapping mapping)
        {
            if (animations != null)
                for (int i = 0; i < animations.Length; i++)
                    if (animations[i] != null && animations[i].action == action && animations[i].clip != null)
                    {
                        mapping = animations[i];
                        return true;
                    }
            mapping = null;
            return false;
        }

        public bool HasAnimation(DragonAnimationAction action) => TryGetAnimation(action, out _);

        public void CollectValidationIssues(List<string> issues)
        {
            if (issues == null) throw new ArgumentNullException(nameof(issues));
            string label = string.IsNullOrWhiteSpace(id) ? "<unnamed dragon>" : id;
            if (string.IsNullOrWhiteSpace(id)) issues.Add(label + ": missing stable species ID.");
            if (visualPrefab == null) issues.Add(label + ": missing actual owned visual prefab.");
            if (!FinitePositive(visualScale) || !FinitePositive(maximumStamina))
                issues.Add(label + ": scale and stamina must be finite and positive.");
            if (!FiniteNonnegative(groundSpeed) || !FiniteNonnegative(flightSpeed) ||
                !FiniteNonnegative(flightAcceleration) || !FiniteNonnegative(turnRate) ||
                !FiniteNonnegative(bankAngle) || bankAngle > 80)
                issues.Add(label + ": invalid movement configuration.");
            if (supportedRoles == DragonSpeciesRole.None) issues.Add(label + ": no supported role selected.");
            var seen = new HashSet<DragonAnimationAction>();
            if (animations != null)
                foreach (var mapping in animations)
                {
                    if (mapping == null) { issues.Add(label + ": null animation mapping."); continue; }
                    if (!seen.Add(mapping.action)) issues.Add(label + ": duplicate mapping for " + mapping.action + ".");
                    if (mapping.clip == null) issues.Add(label + ": missing clip for " + mapping.action + ".");
                    if (!FinitePositive(mapping.playbackSpeed)) issues.Add(label + ": invalid playback speed for " + mapping.action + ".");
                }
            if (Supports(DragonSpeciesRole.Ground)) Require(issues, label, DragonAnimationAction.GroundIdle, DragonAnimationAction.Walk, DragonAnimationAction.Run, DragonAnimationAction.TurnLeft, DragonAnimationAction.TurnRight);
            if (Supports(DragonSpeciesRole.Flying)) Require(issues, label, DragonAnimationAction.Fly, DragonAnimationAction.FlyIdle);
            if (Supports(DragonSpeciesRole.RiderMount))
            {
                if (!Supports(DragonSpeciesRole.Flying)) issues.Add(label + ": rider mount requires Flying role.");
                Require(issues, label, DragonAnimationAction.Takeoff, DragonAnimationAction.Land, DragonAnimationAction.Glide, DragonAnimationAction.BankLeft, DragonAnimationAction.BankRight, DragonAnimationAction.Dive);
            }
            if (Supports(DragonSpeciesRole.HoardGuardian)) Require(issues, label, DragonAnimationAction.Sleep, DragonAnimationAction.Wake, DragonAnimationAction.Roar, DragonAnimationAction.GroundIdle, DragonAnimationAction.Walk);
            if (Supports(DragonSpeciesRole.EggGuardian)) Require(issues, label, DragonAnimationAction.InspectEgg, DragonAnimationAction.Nest, DragonAnimationAction.Land, DragonAnimationAction.Takeoff);
            if (Supports(DragonSpeciesRole.SheepCarrier)) Require(issues, label, DragonAnimationAction.Grab, DragonAnimationAction.Carry, DragonAnimationAction.Drop, DragonAnimationAction.Eat);
            if (Supports(DragonSpeciesRole.Baby)) Require(issues, label, DragonAnimationAction.GroundIdle, DragonAnimationAction.Walk, DragonAnimationAction.Hatch);
            if (element != DragonElement.None) Require(issues, label, DragonAnimationAction.Breath);
        }

        void Require(List<string> issues, string label, params DragonAnimationAction[] required)
        {
            foreach (var action in required)
                if (!HasAnimation(action)) issues.Add(label + ": required role clip missing: " + action + ".");
        }

        static bool FinitePositive(float value) => value > 0 && !float.IsNaN(value) && !float.IsInfinity(value);
        static bool FiniteNonnegative(float value) => value >= 0 && !float.IsNaN(value) && !float.IsInfinity(value);
    }

    [CreateAssetMenu(menuName = "LWS/Truck Dragon Kingdom/Species Catalog")]
    public sealed class DragonSpeciesCatalog : ScriptableObject
    {
        public DragonSpeciesDefinition[] species = Array.Empty<DragonSpeciesDefinition>();

        public bool TryGet(string id, out DragonSpeciesDefinition definition)
        {
            if (!string.IsNullOrWhiteSpace(id) && species != null)
                foreach (var candidate in species)
                    if (candidate != null && string.Equals(candidate.id, id, StringComparison.Ordinal))
                    {
                        definition = candidate;
                        return true;
                    }
            definition = null;
            return false;
        }

        public void CollectValidationIssues(List<string> issues)
        {
            if (issues == null) throw new ArgumentNullException(nameof(issues));
            var ids = new HashSet<string>(StringComparer.Ordinal);
            if (species == null || species.Length == 0) { issues.Add("Dragon species catalog is empty."); return; }
            foreach (var entry in species)
            {
                if (entry == null) { issues.Add("Dragon species catalog contains a null entry."); continue; }
                if (!string.IsNullOrWhiteSpace(entry.id) && !ids.Add(entry.id)) issues.Add("Duplicate dragon species ID: " + entry.id + ".");
                entry.CollectValidationIssues(issues);
            }
        }
    }
}

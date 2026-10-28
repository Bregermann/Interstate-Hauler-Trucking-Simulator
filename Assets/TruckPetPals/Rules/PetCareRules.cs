using System;
namespace LWS.TruckPetPals
{
    public enum PetKind { MiniatureTruck, ClothedHuman, Dog }
    public enum PetTrick { Sit, LieDown, RollOver, Shake, Dig, Stay, Come }
    public enum PetBathStep { Water, Soap, Scrub, Rinse, Dry, Complete }
    public enum PetMood { Following, Investigating, Sniffing, Looking, Playing, Returning, Resting, Sleeping, Peeing, Training, Bathing }
    [Serializable] public sealed class PetProfileRecord
    {
        public string profileId, selectedPetId;
        public PetRecord[] pets;
    }
    [Serializable] public sealed class PetArchive
    {
        public const int CurrentVersion = 1;
        public int version;
        public PetProfileRecord[] profiles;
        public static bool StableId(string id)
        {
            if (string.IsNullOrWhiteSpace(id) || id.Length > 100) return false;
            foreach (char c in id) if (!char.IsLetterOrDigit(c) && c != '.' && c != '_' && c != '-') return false; return true;
        }
        public static bool Valid(PetArchive data, out string reason)
        {
            reason = "";
            if (data == null || data.version != CurrentVersion || data.profiles == null || data.profiles.Length > 512) { reason = "Unknown pet record version or profile roster."; return false; }
            var profiles = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal); var pets = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
            foreach (var profile in data.profiles)
            {
                if (profile == null || !StableId(profile.profileId) || !profiles.Add(profile.profileId) || profile.pets == null || profile.pets.Length < 1 || profile.pets.Length > 12) { reason = "Invalid or duplicate stable pet-profile identity."; return false; }
                bool selected = false;
                foreach (var pet in profile.pets)
                {
                    if (!PetRecord.Valid(pet, profile.profileId) || !StableId(pet.id) || !pets.Add(pet.id)) { reason = "Invalid, duplicate or cross-owned pet data."; return false; }
                    selected |= pet.id == profile.selectedPetId;
                }
                if (!selected) { reason = "Selected companion is missing from its profile's archive."; return false; }
            }
            return true;
        }
    }
    [Serializable] public sealed class PetRecord
    {
        public string id, profileId, name = "Pip", bodyId = "semi.standard", accessoryId = "collar.teal";
        public PetKind kind;
        public float hunger = 85, cleanliness = 90, happiness = 85, energy = 90, affection = 35;
        public float ageSeconds, walked, playful, familiarEnergy = .7f, affectionate = .75f, stubborn = .2f;
        public int bondExperience, baths, discoveries, pees;
        public int[] familiarity = new int[7];
        public bool leashed = true;
        public int BondLevel => Math.Min(10, 1 + bondExperience / 100);
        public float Growth => Math.Clamp(ageSeconds / 10800 + bondExperience / 10000f, 0, 1);
        public void Tick(float dt, bool moving, bool muddy, bool sleeping)
        {
            if (!float.IsFinite(dt) || dt <= 0 || dt > .25f) return;
            ageSeconds += dt; hunger = Math.Max(30, hunger - dt * .008f);
            energy = Math.Clamp(energy + dt * (sleeping ? .65f : moving ? -.025f : .06f), 20, 100);
            if (moving && muddy) cleanliness = Math.Max(15, cleanliness - dt * .07f);
            if (moving) { walked += dt; happiness = Math.Min(100, happiness + dt * .01f); }
        }
        public void Feed() { hunger = Math.Min(100, hunger + 30); Affection(3); }
        public void Affection(int amount) { if (amount <= 0 || amount > 10) return; affection = Math.Min(100, affection + amount); happiness = Math.Min(100, happiness + amount); bondExperience = Math.Min(100000, bondExperience + amount); }
        public bool Learn(PetTrick trick, bool actualPerformance, bool correctlyTimed)
        {
            if ((int)trick < 0 || (int)trick >= 7 || !actualPerformance || !correctlyTimed) return false;
            familiarity[(int)trick] = Math.Min(100, familiarity[(int)trick] + 12); Affection(4); return true;
        }
        public static bool Valid(PetRecord pet, string profile)
        {
            if (pet == null || string.IsNullOrWhiteSpace(pet.id) || pet.id.Length > 80 || pet.profileId != profile || string.IsNullOrWhiteSpace(pet.name) || pet.name.Length > 32 || pet.kind < PetKind.MiniatureTruck || pet.kind > PetKind.Dog || pet.familiarity == null || pet.familiarity.Length != 7 || pet.bondExperience < 0 || pet.bondExperience > 100000 || pet.baths < 0 || pet.pees < 0 || pet.discoveries < 0) return false;
            foreach (float v in new[] { pet.hunger, pet.cleanliness, pet.happiness, pet.energy, pet.affection }) if (!float.IsFinite(v) || v < 0 || v > 100) return false;
            foreach (float v in new[] { pet.ageSeconds, pet.walked, pet.playful }) if (!float.IsFinite(v) || v < 0) return false;
            foreach (float v in new[] { pet.familiarEnergy, pet.affectionate, pet.stubborn }) if (!float.IsFinite(v) || v < 0 || v > 1) return false;
            foreach (int learned in pet.familiarity) if (learned < 0 || learned > 100) return false;
            return !string.IsNullOrWhiteSpace(pet.bodyId) && pet.bodyId.Length < 100 && !string.IsNullOrWhiteSpace(pet.accessoryId) && pet.accessoryId.Length < 100;
        }
    }
    public sealed class PetBathRules
    {
        public PetBathStep Step { get; private set; }
        public float Progress { get; private set; }
        public bool Complete => Step == PetBathStep.Complete;
        int direction;
        public void Begin() { Step = PetBathStep.Water; Progress = 0; direction = 0; }
        public bool Stroke(float horizontal, bool water, float dt)
        {
            if (Complete || !float.IsFinite(horizontal) || !float.IsFinite(dt) || dt <= 0 || dt > .25f) return false;
            if (Step == PetBathStep.Scrub)
            {
                int now = horizontal > .5f ? 1 : horizontal < -.5f ? -1 : 0;
                if (now != 0 && now != direction) { direction = now; Progress = Math.Min(1, Progress + .125f); }
            }
            else if (water) Progress = Math.Min(1, Progress + dt / 2);
            return Progress >= 1;
        }
        public bool Confirm(PetRecord pet)
        {
            if (Complete || Progress < 1 || pet == null) return false;
            if (Step == PetBathStep.Rinse) pet.cleanliness = 100;
            Step++; Progress = 0; direction = 0;
            if (Complete) { pet.baths++; pet.Affection(5); }
            return true;
        }
    }
}

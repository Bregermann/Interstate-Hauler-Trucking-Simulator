using System;
namespace LWS.TruckCamping
{
    public enum CampSupply { Sticks, Firewood, Forage, Fish, Meals }
    public enum CampActivity { Exploring, Gathering, BuildingFire, Cooking, Fishing, Resting, Sleeping, Photographing }
    [Serializable] public sealed class CampJournal
    {
        public string profileId;
        public int[] supplies = new int[5];
        public float hunger = 85, energy = 90;
        public int discovered, photos, mealsCooked, fishCaught, naps;
        public float walked, slept;
        public CampJournal() { }
        public CampJournal(string id) { profileId = id; supplies[(int)CampSupply.Meals] = 3; }
        public bool Consume(CampSupply kind, int count)
        {
            if (count < 1 || (int)kind < 0 || (int)kind >= supplies.Length || supplies[(int)kind] < count) return false;
            supplies[(int)kind] -= count; return true;
        }
        public bool Add(CampSupply kind, int count)
        {
            if (count < 1 || count > 20 || (int)kind < 0 || (int)kind >= supplies.Length) return false;
            supplies[(int)kind] = Math.Min(99, supplies[(int)kind] + count); return true;
        }
        public bool Eat()
        {
            if (!Consume(CampSupply.Meals, 1) && !Consume(CampSupply.Forage, 1)) return false;
            hunger = Math.Min(100, hunger + 35); energy = Math.Min(100, energy + 8); return true;
        }
        public void Tick(float dt, bool survival, bool moving, bool sleeping)
        {
            if (!float.IsFinite(dt) || dt <= 0 || dt > .25f) return;
            if (survival) hunger = Math.Max(25, hunger - dt * .012f);
            energy = Math.Clamp(energy + dt * (sleeping ? .45f : moving ? -.018f : .035f), 20, 100);
            if (sleeping) slept += dt;
        }
        public bool Discover(int landmark)
        {
            if (landmark < 0 || landmark > 20) return false;
            int bit = 1 << landmark; bool fresh = (discovered & bit) == 0; discovered |= bit; return fresh;
        }
        public static bool Valid(CampJournal state)
        {
            if (state == null || string.IsNullOrWhiteSpace(state.profileId) || state.supplies == null || state.supplies.Length != 5 || !float.IsFinite(state.hunger) || !float.IsFinite(state.energy) || state.hunger < 0 || state.hunger > 100 || state.energy < 0 || state.energy > 100 || state.photos < 0 || state.photos > 100000 || state.discovered < 0 || state.mealsCooked < 0 || state.fishCaught < 0 || state.naps < 0 || !float.IsFinite(state.walked) || !float.IsFinite(state.slept) || state.walked < 0 || state.slept < 0) return false;
            foreach (int item in state.supplies) if (item < 0 || item > 99) return false; return true;
        }
    }
    [Serializable] public sealed class CampfireState
    {
        public string id;
        public bool built;
        public float fuel;
        public int meals;
        public bool Burning => built && fuel > 0;
        public bool Build(CampJournal owner)
        {
            if (built || owner == null || owner.supplies[(int)CampSupply.Sticks] < 3 || owner.supplies[(int)CampSupply.Firewood] < 2) return false;
            owner.Consume(CampSupply.Sticks, 3); owner.Consume(CampSupply.Firewood, 2); built = true; fuel = 240; return true;
        }
        public bool AddFuel(CampJournal owner)
        {
            if (!built || fuel > 540 || !owner.Consume(CampSupply.Firewood, 1)) return false;
            fuel += 180; return true;
        }
        public bool Cook(CampJournal owner)
        {
            if (!Burning || owner == null || owner.supplies[(int)CampSupply.Fish] + owner.supplies[(int)CampSupply.Forage] < 1) return false;
            if (!owner.Consume(CampSupply.Fish, 1)) owner.Consume(CampSupply.Forage, 1);
            owner.Add(CampSupply.Meals, 2); owner.mealsCooked++; meals++; fuel = Math.Max(0, fuel - 10); return true;
        }
        public void Tick(float dt) { if (float.IsFinite(dt) && dt > 0 && dt <= .25f) fuel = Math.Max(0, fuel - dt); }
    }
}

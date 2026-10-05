using System;
using System.Collections.Generic;

namespace LWS.TruckTaxi
{
    public enum TruckTaxiPassengerGender { Unassigned, Male, Female }

    // Metadata only; no animation, audio, or presentation implementation.
    public static class TruckTaxiPassengerEligibilityPolicy
    {
        public const double AppreciationOfferChance = .33;
        public static TruckTaxiPassengerGender ResolveGender(TruckTaxiPassengerGender authored, string castingGender)
        {
            if(authored==TruckTaxiPassengerGender.Male || authored==TruckTaxiPassengerGender.Female) return authored;
            switch((castingGender ?? "").Trim().ToLowerInvariant())
            {
                case "male": case "man": return TruckTaxiPassengerGender.Male;
                case "female": case "woman": return TruckTaxiPassengerGender.Female;
                default: return TruckTaxiPassengerGender.Unassigned;
            }
        }
        public static bool IsAppreciationEligible(TruckTaxiPassengerGender gender, bool explicitlyAdult,
            int minimumAge, bool authoredOptIn) => gender==TruckTaxiPassengerGender.Female &&
            explicitlyAdult && minimumAge>=21 && authoredOptIn;
        public static bool ShouldOfferAppreciation(bool eligible, Random random) =>
            eligible && random.NextDouble()<AppreciationOfferChance;
        public static string BroadArchetype(string authoredCategory, string stableId, string archetype, string[] traits)
        {
            if(!string.IsNullOrWhiteSpace(authoredCategory)) return authoredCategory.Trim().ToLowerInvariant();
            string metadata=((archetype ?? "")+" "+string.Join(" ",traits ?? Array.Empty<string>())).ToLowerInvariant();
            if((stableId ?? "").StartsWith("racing-",StringComparison.OrdinalIgnoreCase) ||
                Contains(metadata,"racing","racer","nascar","stock car","stock-car")) return "racing";
            if(Contains(metadata,"commuter")) return "commuter";
            if(Contains(metadata,"business","executive","deadline")) return "business";
            if((stableId ?? "").StartsWith("glam-",StringComparison.OrdinalIgnoreCase) || Contains(metadata,"glam","flirtatious")) return "glamorous";
            if(Contains(metadata,"elder","old mentor","old lady")) return "elderly";
            if(Contains(metadata,"chaos","chaotic","lunatic")) return "chaos";
            if(Contains(metadata,"fantasy","alien","robot","dragon","android")) return "fantasy/special";
            return string.IsNullOrWhiteSpace(archetype) ? "commuter" : "game-parody";
        }
        private static bool Contains(string value, params string[] tokens)
        { foreach(var token in tokens) if(value.Contains(token)) return true; return false; }
    }

    [Serializable]
    public sealed class TruckTaxiPassengerExposure
    {
        public string passengerId, archetype;
        public int timesOffered, timesAccepted, timesCompleted, lastOfferedRideIndex;
        public double lastOfferedGameMinutes;
        public TruckTaxiPassengerExposure Copy() => (TruckTaxiPassengerExposure)MemberwiseClone();
    }
    [Serializable]
    public sealed class TruckTaxiPassengerSelectionState
    {
        public int version=1, offerIndex;
        public List<TruckTaxiPassengerExposure> passengers=new List<TruckTaxiPassengerExposure>();
        public List<string> recentArchetypes=new List<string>();
    }
    public readonly struct TruckTaxiPassengerCandidate
    {
        public readonly string Id, Archetype;
        public readonly double Weight;
        public TruckTaxiPassengerCandidate(string id, string archetype, double weight)
        { Id=id; Archetype=archetype; Weight=weight; }
    }

    // Stable IDs survive roster reorder/reload. Only presented offers advance exposure.
    public sealed class TruckTaxiPassengerDispatchPolicy
    {
        private readonly Dictionary<string,TruckTaxiPassengerExposure> exposure=
            new Dictionary<string,TruckTaxiPassengerExposure>(StringComparer.OrdinalIgnoreCase);
        private readonly List<string> recentArchetypes=new List<string>();
        private int offerIndex;
        public int OfferIndex => offerIndex;
        public TruckTaxiPassengerExposure Exposure(string id) => id!=null && exposure.TryGetValue(id,out var entry)
            ? entry.Copy() : new TruckTaxiPassengerExposure { passengerId=id };
        public string Select(IReadOnlyList<TruckTaxiPassengerCandidate> candidates, Random random, bool raceEvent, string avoidId=null)
        {
            var pool=new List<TruckTaxiPassengerCandidate>();
            var ids=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach(var candidate in candidates)
                if(!string.IsNullOrWhiteSpace(candidate.Id) && candidate.Weight>0 &&
                    !double.IsNaN(candidate.Weight) && !double.IsInfinity(candidate.Weight) && ids.Add(candidate.Id)) pool.Add(candidate);
            if(pool.Count==0) return null;
            int racing=CountRecent("racing",19), recentRacing=CountRecent("racing",4);
            if(pool.Exists(c=>c.Archetype!="racing") && (racing>=(raceEvent ? 6 : 3) || recentRacing>=2))
                pool.RemoveAll(c=>c.Archetype=="racing");
            if(pool.Exists(c=>!string.Equals(c.Id,avoidId,StringComparison.OrdinalIgnoreCase)))
                pool.RemoveAll(c=>string.Equals(c.Id,avoidId,StringComparison.OrdinalIgnoreCase));
            int minimum=int.MaxValue;
            foreach(var c in pool) minimum=Math.Min(minimum,Exposure(c.Id).timesOffered);
            // Before first coverage, at most two exposures may break category runs.
            // Later cycles prioritize avoiding consecutive category runs, then least exposure.
            if(minimum==0) pool.RemoveAll(c=>Exposure(c.Id).timesOffered>1);
            int end=recentArchetypes.Count-1;
            if(end>=1 && recentArchetypes[end]==recentArchetypes[end-1] && pool.Exists(c=>c.Archetype!=recentArchetypes[end]))
                pool.RemoveAll(c=>c.Archetype==recentArchetypes[end]);
            minimum=int.MaxValue;
            foreach(var c in pool) minimum=Math.Min(minimum,Exposure(c.Id).timesOffered);
            pool.RemoveAll(c=>Exposure(c.Id).timesOffered!=minimum);
            if(minimum>0)
            {
                // Randomize a least-recent window, not a fixed roster permutation.
                pool.Sort((a,b)=>Exposure(a.Id).lastOfferedRideIndex.CompareTo(Exposure(b.Id).lastOfferedRideIndex));
                int window=Math.Max(1,(int)Math.Ceiling(pool.Count*.25));
                if(pool.Count>window) pool.RemoveRange(window,pool.Count-window);
            }
            double total=0;
            foreach(var c in pool) total+=SelectionWeight(c);
            double roll=random.NextDouble()*total;
            foreach(var c in pool) { roll-=SelectionWeight(c); if(roll<0) return c.Id; }
            return pool[pool.Count-1].Id;
        }
        private double SelectionWeight(TruckTaxiPassengerCandidate candidate) =>
            Math.Max(.25,Math.Min(4,candidate.Weight))*(CountRecent(candidate.Archetype,4)>=2 ? .1 : 1);
        private int CountRecent(string archetype,int count)
        { int total=0; for(int i=Math.Max(0,recentArchetypes.Count-count);i<recentArchetypes.Count;i++) if(recentArchetypes[i]==archetype) total++; return total; }
        private TruckTaxiPassengerExposure Get(string id)
        {
            if(string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Passenger stable ID is required.",nameof(id));
            if(!exposure.TryGetValue(id,out var entry)) exposure.Add(id,entry=new TruckTaxiPassengerExposure { passengerId=id });
            return entry;
        }
        public void RecordOffered(string id,string archetype,double gameMinutes)
        {
            var entry=Get(id); entry.archetype=archetype; entry.timesOffered++;
            entry.lastOfferedRideIndex=++offerIndex; entry.lastOfferedGameMinutes=gameMinutes;
            recentArchetypes.Add(archetype);
            if(recentArchetypes.Count>20) recentArchetypes.RemoveAt(0);
        }
        public void RecordAccepted(string id) { Get(id).timesAccepted++; }
        public void RecordCompleted(string id) { Get(id).timesCompleted++; }
        public TruckTaxiPassengerSelectionState CaptureState()
        {
            var state=new TruckTaxiPassengerSelectionState { offerIndex=offerIndex, recentArchetypes=new List<string>(recentArchetypes) };
            foreach(var entry in exposure.Values) state.passengers.Add(entry.Copy());
            state.passengers.Sort((a,b)=>string.CompareOrdinal(a.passengerId,b.passengerId));
            return state;
        }
        public bool RestoreState(TruckTaxiPassengerSelectionState state)
        {
            if(state==null || state.version!=1 || state.offerIndex<0 || state.passengers==null || state.recentArchetypes==null || state.recentArchetypes.Count>20) return false;
            var restored=new Dictionary<string,TruckTaxiPassengerExposure>(StringComparer.OrdinalIgnoreCase);
            foreach(var entry in state.passengers)
            {
                if(entry==null || string.IsNullOrWhiteSpace(entry.passengerId) || restored.ContainsKey(entry.passengerId) ||
                    entry.timesOffered<0 || entry.timesAccepted<0 || entry.timesAccepted>entry.timesOffered ||
                    entry.timesCompleted<0 || entry.timesCompleted>entry.timesAccepted || entry.lastOfferedRideIndex<0 ||
                    entry.lastOfferedRideIndex>state.offerIndex || double.IsNaN(entry.lastOfferedGameMinutes) ||
                    double.IsInfinity(entry.lastOfferedGameMinutes) || entry.lastOfferedGameMinutes<0) return false;
                restored.Add(entry.passengerId,entry.Copy());
            }
            exposure.Clear(); foreach(var entry in restored) exposure.Add(entry.Key,entry.Value);
            offerIndex=state.offerIndex; recentArchetypes.Clear(); recentArchetypes.AddRange(state.recentArchetypes); return true;
        }
    }
}

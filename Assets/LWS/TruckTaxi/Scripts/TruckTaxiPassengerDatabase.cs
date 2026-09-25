using System;
using System.Collections.Generic;
using UnityEngine;

namespace LWS.TruckTaxi
{
    public enum TruckTaxiRarity { Common, Uncommon, Rare, Legendary }
    public enum TruckTaxiMechanic { SportsCommentator, FeeCollector, PowerLevel, PropertyDestructionCat, StyleCombo, TimeObsessed,
        DesignAnalyst, NeverTips, DestinationChanger, BackseatDriver, Oversized, DashboardCompanion }
    public sealed class TruckTaxiPassengerQuery
    {
        public bool availableOnly;
        public TruckTaxiRarity? rarity;
        public string district, trait, species, raceEthnicity;
        public TaxiRequestType? request;
        public TruckTaxiVoiceProfile voice;
        public TruckTaxiCharacterType? characterType;
        public TruckTaxiSeatType? seatType;
    }
    [CreateAssetMenu(menuName = "Truck Taxi/Passengers/Database")]
    public sealed class TruckTaxiPassengerDatabase : ScriptableObject
    {
        public PassengerProfile[] passengers = Array.Empty<PassengerProfile>();
        public IEnumerable<PassengerProfile> Query(TruckTaxiPassengerQuery query = null)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var p in passengers)
            {
                if (p == null || string.IsNullOrWhiteSpace(p.passengerId) || !seen.Add(p.passengerId)) continue;
                if (query != null)
                {
                    if (query.availableOnly && (!p.available || p.spawnWeight <= 0)) continue;
                    if (query.rarity.HasValue && p.rarity != query.rarity.Value) continue;
                    if (query.voice != null && p.voiceProfile != query.voice) continue;
                    if (query.characterType.HasValue && p.appearance?.characterType != query.characterType.Value) continue;
                    if (query.seatType.HasValue && p.seatProfile?.seatType != query.seatType.Value) continue;
                    if (!string.IsNullOrEmpty(query.species) && !string.Equals(p.casting?.species,query.species,StringComparison.OrdinalIgnoreCase)) continue;
                    if (!string.IsNullOrEmpty(query.raceEthnicity) && !string.Equals(p.casting?.raceEthnicity,query.raceEthnicity,StringComparison.OrdinalIgnoreCase)) continue;
                    if (!string.IsNullOrEmpty(query.trait) && !Array.Exists(p.specialTraits ?? Array.Empty<string>(),x=>string.Equals(x,query.trait,StringComparison.OrdinalIgnoreCase))) continue;
                    if (!string.IsNullOrEmpty(query.district) && p.districts != null && p.districts.Length > 0 && !Array.Exists(p.districts,x=>x==query.district)) continue;
                    if (query.request.HasValue && !Array.Exists(p.possibleRequests ?? Array.Empty<PassengerRequestDefinition>(),x=>x!=null && x.requestType==query.request)) continue;
                }
                yield return p;
            }
        }
    }
}

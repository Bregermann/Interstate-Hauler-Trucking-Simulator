using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace LWS.TruckTaxi
{
    // Owned by Session, embedded in the existing vendor participant. No separate profile or wallet.
    [Serializable]
    public sealed class TruckTaxiCareerProfileData
    {
        public int version=1;
        public string runProfileId, driverName, equippedTruckId;
        public bool firstGuideEligible, firstGuideClaimed;
        public List<TruckTaxiOwnedTruck> trucks=new List<TruckTaxiOwnedTruck>();
        public List<string> ownedHeadwearIds=new List<string>();
        public string equippedHeadwearId;
        public TruckTaxiDriverAppearanceData driverAppearance=new TruckTaxiDriverAppearanceData();

        public TruckTaxiCareerProfileData Copy()
        {
            var result=(TruckTaxiCareerProfileData)MemberwiseClone();
            result.trucks=trucks?.ConvertAll(t=>t?.Copy());
            result.ownedHeadwearIds=ownedHeadwearIds==null ? null : new List<string>(ownedHeadwearIds);
            result.driverAppearance=driverAppearance?.Copy();
            return result;
        }

        public static bool TryNormalizeDriverName(string value,out string normalized,out string reason)
        {
            normalized=null; reason="Enter a driver name of 1 to 40 characters.";
            if(string.IsNullOrWhiteSpace(value) || value.Length>256) return false;
            foreach(char c in value) if(char.IsControl(c)) return false;
            try { normalized=value.Trim().Normalize(NormalizationForm.FormC); }
            catch(ArgumentException) { return false; }
            if(new StringInfo(normalized).LengthInTextElements>40) return false;
            foreach(char c in normalized)
                if(char.IsControl(c) || char.GetUnicodeCategory(c)==UnicodeCategory.Format ||
                    c=='<' || c=='>' || c=='\u2028' || c=='\u2029')
                { normalized=null; reason="Driver names cannot contain markup or control characters."; return false; }
            reason=null; return true;
        }

        internal static bool ValidId(string id)
        {
            if(string.IsNullOrWhiteSpace(id) || id.Length>128 || id.Trim()!=id) return false;
            foreach(char c in id) if(char.IsControl(c) || char.IsWhiteSpace(c)) return false;
            return true;
        }
        internal static bool UniqueIds(List<string> ids)
        {
            if(ids==null) return false;
            var seen=new HashSet<string>(StringComparer.Ordinal);
            foreach(var id in ids) if(!ValidId(id) || !seen.Add(id)) return false;
            return true;
        }
        public bool Validate(out string reason)
        {
            reason="Invalid Career identity or ownership data.";
            if(driverAppearance!=null && !driverAppearance.Validate(out reason)) return false;
            if(version!=1 || !ValidId(runProfileId) ||
                !TryNormalizeDriverName(driverName,out var normalized,out _) || normalized!=driverName ||
                trucks==null || trucks.Count==0 || !ValidId(equippedTruckId) || !UniqueIds(ownedHeadwearIds) ||
                firstGuideClaimed && !firstGuideEligible) return false;
            var ids=new HashSet<string>(StringComparer.Ordinal);
            foreach(var truck in trucks)
                if(truck==null || !ValidId(truck.truckId) || !ids.Add(truck.truckId) ||
                    !UniqueIds(truck.ownedUpgradeIds) || !UniqueIds(truck.ownedCosmeticIds) ||
                    !ValidSelections(truck.upgrades,truck.ownedUpgradeIds,true) ||
                    !ValidSelections(truck.cosmetics,truck.ownedCosmeticIds,false)) return false;
            if(!ids.Contains(equippedTruckId) || !string.IsNullOrEmpty(equippedHeadwearId) &&
                !ownedHeadwearIds.Contains(equippedHeadwearId)) return false;
            reason=null; return true;
        }
        private static bool ValidSelections(List<TruckTaxiCareerSelection> selections,List<string> owned,bool upgrades)
        {
            if(selections==null) return false;
            var slots=new HashSet<string>(StringComparer.Ordinal);
            var items=new HashSet<string>(StringComparer.Ordinal);
            foreach(var selection in selections)
                if(selection==null || !owned.Contains(selection.itemId) || !slots.Add(selection.slotId ?? "") ||
                    !items.Add(selection.itemId) || !(upgrades ? TruckTaxiCareerCatalog.IsUpgradeSlot(selection.slotId) :
                    TruckTaxiCareerCatalog.IsTruckCosmeticSlot(selection.slotId))) return false;
            return true;
        }
    }

    [Serializable]
    public sealed class TruckTaxiOwnedTruck
    {
        public string truckId;
        public List<string> ownedUpgradeIds=new List<string>();
        public List<string> ownedCosmeticIds=new List<string>();
        public List<TruckTaxiCareerSelection> upgrades=new List<TruckTaxiCareerSelection>();
        public List<TruckTaxiCareerSelection> cosmetics=new List<TruckTaxiCareerSelection>();
        internal TruckTaxiOwnedTruck Copy() => new TruckTaxiOwnedTruck {
            truckId=truckId,ownedUpgradeIds=ownedUpgradeIds==null ? null : new List<string>(ownedUpgradeIds),
            ownedCosmeticIds=ownedCosmeticIds==null ? null : new List<string>(ownedCosmeticIds),
            upgrades=upgrades?.ConvertAll(s=>s?.Copy()),cosmetics=cosmetics?.ConvertAll(s=>s?.Copy())
        };
    }

    [Serializable]
    public sealed class TruckTaxiCareerSelection
    {
        public string slotId, itemId;
        internal TruckTaxiCareerSelection Copy() => (TruckTaxiCareerSelection)MemberwiseClone();
    }

    public readonly struct TruckTaxiFirstDayGuideLine
    {
        public string StableId => "taxi.guide.first-day.welcome";
        public string Subtitle => "Welcome hun";
        public string CharacterDescription => "Adult woman, 21+, Southern US, warm and friendly";
        public AudioClip Voice => null;
    }
}

using System;
using System.Collections.Generic;

namespace LWS.TruckSports
{
    // Stable content identifiers only. Catalog resolution and temporary sport
    // compatibility never rewrite these saved preferences.
    [Serializable]
    public sealed class SportsTruckCosmeticSelection
    {
        public string slotId = "", itemId = "", variantId = "";
        public SportsTruckCosmeticSelection Copy() => (SportsTruckCosmeticSelection)MemberwiseClone();
    }

    public static class SportsTruckAppearanceSlots
    {
        public const string Hat = "hat", Eyes = "face.eyes", Brows = "face.brows", Mouth = "face.mouth",
            Mustache = "face.mustache", Lashes = "face.lashes", Mirrors = "mirrors", Wheels = "wheels",
            RearOrnament = "rear.ornament", Dashboard = "dashboard";
    }

    [Serializable]
    public sealed class SportsTruckAppearanceData
    {
        public const int CurrentVersion = 1;
        public const int MaximumIdLength = 128, MaximumSelections = 64;
        public int version = CurrentVersion;
        public string bodyId = "";
        public List<SportsTruckCosmeticSelection> selections = new List<SportsTruckCosmeticSelection>();

        public SportsTruckAppearanceData Copy() => new SportsTruckAppearanceData
        {
            version = version, bodyId = bodyId,
            selections = selections?.ConvertAll(item => item?.Copy())
        };

        // Unknown catalog and future semantic slot IDs are valid preferences.
        // Empty item IDs mean unequipped, and require an empty variant ID.
        public bool Validate()
        {
            if (version != CurrentVersion || !ValidId(bodyId, true) || selections == null || selections.Count > MaximumSelections) return false;
            var slots = new HashSet<string>(StringComparer.Ordinal);
            foreach (var item in selections)
                if (item == null || !ValidId(item.slotId, false) || !slots.Add(item.slotId) ||
                    !ValidId(item.itemId, true) || !ValidId(item.variantId, true) ||
                    item.itemId.Length == 0 && item.variantId.Length != 0) return false;
            return true;
        }

        static bool ValidId(string value, bool allowEmpty)
        {
            if (value == null || value.Length > MaximumIdLength || !allowEmpty && value.Length == 0) return false;
            foreach (char character in value) if (char.IsControl(character) || char.IsWhiteSpace(character)) return false;
            return true;
        }

        // Salvage malformed nested appearance data without replacing its owner,
        // roster or results. The registry preserves the original JSON first.
        internal static SportsTruckAppearanceData Normalize(SportsTruckAppearanceData source, out bool repaired)
        {
            repaired = false;
            var result = new SportsTruckAppearanceData();
            if (source == null) return result;
            if (source.version != CurrentVersion) repaired = true;
            if (ValidId(source.bodyId, true)) result.bodyId = source.bodyId;
            else repaired = true;
            if (source.selections == null) { repaired = true; return result; }
            var slots = new HashSet<string>(StringComparer.Ordinal);
            foreach (var item in source.selections)
            {
                if (item == null || !ValidId(item.slotId, false) || !ValidId(item.itemId, true) ||
                    !ValidId(item.variantId, true) || item.itemId.Length == 0 && item.variantId.Length != 0 ||
                    !slots.Add(item.slotId)) { repaired = true; continue; }
                if (item.itemId.Length == 0) continue;
                if (result.selections.Count >= MaximumSelections) { repaired = true; continue; }
                result.selections.Add(item.Copy());
            }
            result.selections.Sort((a, b) => StringComparer.Ordinal.Compare(a.slotId, b.slotId));
            return result;
        }

        internal bool SameAs(SportsTruckAppearanceData other)
        {
            if (other == null || version != other.version || bodyId != other.bodyId || selections.Count != other.selections.Count) return false;
            for (int i = 0; i < selections.Count; i++)
                if (selections[i].slotId != other.selections[i].slotId || selections[i].itemId != other.selections[i].itemId ||
                    selections[i].variantId != other.selections[i].variantId) return false;
            return true;
        }
    }
}

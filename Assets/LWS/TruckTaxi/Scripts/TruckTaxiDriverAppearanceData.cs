using System;
using UnityEngine;

namespace LWS.TruckTaxi
{
    // Embedded in Career by its owner; never a separate file, profile, or PlayerPrefs record.
    [Serializable]
    public sealed class TruckTaxiDriverAppearanceData
    {
        public int version = 1;
        public Color shirt = new Color(.2f, .43f, .55f);
        public Color pantsAndCap = new Color(.12f, .15f, .18f);
        public Color trim = new Color(.87f, .26f, .18f);
        public Color skin = new Color(.76f, .54f, .38f);
        public bool capVisible = true;
        // Zero preserves pre-field saves and the original clothed adult silhouette.
        [Range(-1f, 1f)] public float chestShape;
        public TruckTaxiDriverAppearanceData Copy() => (TruckTaxiDriverAppearanceData)MemberwiseClone();
        public bool Validate(out string reason)
        {
            reason = "Invalid driver appearance colors or version.";
            if (version != 1 || !ValidColor(shirt) || !ValidColor(pantsAndCap) || !ValidColor(trim) || !ValidColor(skin)) return false;
            if (!float.IsFinite(chestShape) || chestShape < -1 || chestShape > 1)
            { reason = "Invalid adult chest shape."; return false; }
            reason = null; return true;
        }
        private static bool ValidColor(Color c) => float.IsFinite(c.r) && float.IsFinite(c.g) && float.IsFinite(c.b) &&
            c.r >= 0 && c.r <= 1 && c.g >= 0 && c.g <= 1 && c.b >= 0 && c.b <= 1 && c.a == 1;
        public bool SameAs(TruckTaxiDriverAppearanceData other) => other != null && version == other.version &&
            shirt.Equals(other.shirt) && pantsAndCap.Equals(other.pantsAndCap) && trim.Equals(other.trim) &&
            skin.Equals(other.skin) && capVisible == other.capVisible && chestShape.Equals(other.chestShape);
    }
}

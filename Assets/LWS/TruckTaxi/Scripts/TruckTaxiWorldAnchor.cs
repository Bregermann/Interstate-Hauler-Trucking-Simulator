using System;
using UnityEngine;

namespace LWS.TruckTaxi
{
    public enum TruckTaxiWorldAnchorType
    {
        Scenic, Illicit, Private, Store, Gas, Restroom, Pickup, Destination,
        PedArea, Crosswalk, TrafficLight, Shortcut, Property, Parking, Building, Road
    }

    [DisallowMultipleComponent]
    public sealed class TruckTaxiWorldAnchor : MonoBehaviour
    {
        [SerializeField] private string stableId;
        [SerializeField] private TruckTaxiWorldAnchorType anchorType;
        [SerializeField] private string authoredName;
        [SerializeField] private string roadType;

        public string StableId => stableId;
        public TruckTaxiWorldAnchorType AnchorType => anchorType;
        public string AuthoredName => authoredName;
        public string RoadType => roadType;

        public void Initialize(string id, TruckTaxiWorldAnchorType type, string name, string roadKind = null)
        {
            if (string.IsNullOrWhiteSpace(id) || !Guid.TryParse(id, out _))
                throw new ArgumentException("A UUID stable ID is required.", nameof(id));
            if (!string.IsNullOrEmpty(stableId))
                throw new InvalidOperationException("An anchor identity is assigned once and cannot be replaced.");
            stableId = id;
            anchorType = type;
            authoredName = name ?? string.Empty;
            roadType = roadKind ?? string.Empty;
        }
    }
}

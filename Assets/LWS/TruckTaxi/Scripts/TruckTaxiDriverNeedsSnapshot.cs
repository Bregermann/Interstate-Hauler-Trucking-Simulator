using System;

namespace LWS.TruckTaxi
{
    // Data only: LwsSaveService / Pixel Crushers own serialization and storage.
    [Serializable]
    public sealed class TruckTaxiDriverNeedsSnapshot
    {
        public int Version = 1;
        public float Pressure, Hunger, Thirst, Fatigue;
        public bool StarterInventoryGranted, CabMess;
        public float CycleGameSeconds, CrisisGameSeconds;
        public TruckTaxiNeedsInventoryEntry[] Inventory;
    }

    [Serializable]
    public sealed class TruckTaxiNeedsInventoryEntry
    {
        public string ItemId;
        public int Count;
    }
}

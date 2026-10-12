using System;

namespace LWS.TruckTaxi
{
    // Data only: LwsSaveService / Pixel Crushers own serialization and storage.
    [Serializable]
    public sealed class TruckTaxiDriverNeedsSnapshot
    {
        public int Version = 1;
        public float Pressure, Hunger, Thirst, Fatigue;
        public float BowelPressure;
        public float BowelCrisisGameSeconds;
        public int BowelAccidents;
        public float BowelDiscomfortGameSeconds;
        public string[] UsedToiletIds;
        public string[] AmbientChoiceIds;
        public string RentedMotelId;
        public double MotelAccessExpiresAtGameSeconds;
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

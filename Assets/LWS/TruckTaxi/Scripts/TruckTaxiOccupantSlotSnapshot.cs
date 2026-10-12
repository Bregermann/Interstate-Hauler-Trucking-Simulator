namespace LWS.TruckTaxi
{
    public enum TruckTaxiOccupantSlotState { Free, Reserved, Occupied }
    public readonly struct TruckTaxiOccupantSlotSnapshot
    {
        public readonly string SlotId, OccupantId;
        public readonly TruckTaxiCabOccupantKind Role;
        public readonly TruckTaxiOccupantSlotState State;
        public TruckTaxiOccupantSlotSnapshot(string slot,TruckTaxiCabOccupantKind role,string occupant,
            TruckTaxiOccupantSlotState state) { SlotId=slot; OccupantId=occupant; Role=role; State=state; }
        public override string ToString() => SlotId+" | "+Role+" | "+OccupantId+" | "+State;
    }
}

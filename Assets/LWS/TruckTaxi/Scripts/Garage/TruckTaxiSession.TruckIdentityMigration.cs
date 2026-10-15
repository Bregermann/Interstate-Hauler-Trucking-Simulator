namespace LWS.TruckTaxi
{
    public sealed partial class TruckTaxiSession
    {
        // Only these two historically authored IDs represent the same starter.
        // Never infer aliases from names, meshes or similar-looking identifiers.
        public bool TryMigrateLegacyStarterIdentity(out string reason)
        {
            reason=null;
            if(ownedProfile==null || !ownedProfile.trucks.Exists(t=>t.truckId==TruckTaxiEightOccupantLayout.StarterDefinitionId)) return true;
            if(!CanChangeCareer(out reason)) return false;
            if(ownedProfile.trucks.Exists(t=>t.truckId==TruckTaxiEightOccupantLayout.FallbackDefinitionId))
            {reason="Career contains both historical starter identities. Ownership was preserved; resolve that duplicate before changing trucks.";return false;}
            var next=ownedProfile.Copy();
            next.trucks.Find(t=>t.truckId==TruckTaxiEightOccupantLayout.StarterDefinitionId).truckId=TruckTaxiEightOccupantLayout.FallbackDefinitionId;
            if(next.equippedTruckId==TruckTaxiEightOccupantLayout.StarterDefinitionId) next.equippedTruckId=TruckTaxiEightOccupantLayout.FallbackDefinitionId;
            return CommitCareer(next,0,out reason);
        }
    }
}

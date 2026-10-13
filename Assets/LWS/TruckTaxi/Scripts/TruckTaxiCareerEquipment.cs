using UnityEngine;

namespace LWS.TruckTaxi
{
    // Applies only explicitly supported catalog items to existing LWS/NWH adapters.
    public sealed class TruckTaxiCareerEquipment : MonoBehaviour
    {
        private TruckTaxiBootstrap host;
        private float baseFuelCapacity;
        public TruckTaxiCareerCatalog Catalog { get; private set; }
        public string Feedback { get; private set; }
        public void Initialize(TruckTaxiBootstrap owner)
        {
            if (host != null) return;
            host=owner;
            Catalog=owner.Configuration.careerCatalog != null ? owner.Configuration.careerCatalog :
                Resources.Load<TruckTaxiCareerCatalog>("TruckTaxi/Career/Catalog");
            // Resolve only the existing canonical definition. Never mutate the shared catalog asset.
            var current=Catalog?.FindTruck(owner.Player.DefinitionId);
            if(current!=null && current.truck==null && owner.Player.Definition!=null && owner.Player.Definition.PlayerPrefab!=null)
            {
                Catalog=Instantiate(Catalog);
                ownsCatalog=true;
                Catalog.FindTruck(owner.Player.DefinitionId).truck=owner.Player.Definition;
            }
            baseFuelCapacity=owner.Fuel?.Fuel?.capacity ?? 0;
            host.Session.CareerEquipmentChanged+=Apply;
            Apply();
        }
        public static bool Supports(TruckTaxiCareerUpgradeDefinition definition) => definition != null &&
            definition.adapterId==TruckTaxiCareerUpgradeTuning.AdapterId &&
            definition.adapterConfiguration is TruckTaxiCareerUpgradeTuning tuning && tuning.IsValid &&
            (definition.category==TruckTaxiUpgradeCategory.Engine || definition.category==TruckTaxiUpgradeCategory.Brakes ||
                definition.category==TruckTaxiUpgradeCategory.Fuel);
        public bool IsOwned(string id)
        {
            var profile=host?.Session?.CaptureCareerProfile();
            return profile?.trucks.Find(t=>t.truckId==profile.equippedTruckId)?.ownedUpgradeIds.Contains(id)==true;
        }
        public bool IsEquipped(string id)
        {
            var profile=host?.Session?.CaptureCareerProfile();
            return profile?.trucks.Find(t=>t.truckId==profile.equippedTruckId)?.upgrades.Exists(u=>u.itemId==id)==true;
        }
        public bool PurchaseOrEquip(string id)
        {
            var definition=Catalog?.FindUpgrade(id);
            var body=host?.Player!=null ? host.Player.GetComponent<Rigidbody>() : null;
            if (!Supports(definition) || host?.Ready!=true || host.Persistence?.ResumeBlocked!=false ||
                !host.Persistence.HasActiveRun || host.Persistence.IsBusy || body==null ||
                host.Races?.IsActive==true || host.Companions?.IsBusy==true || host.WorldLife?.IsOnFoot==true ||
                body.linearVelocity.magnitude > .44704f)
            { Feedback="PARK THE TRUCK BEFORE CHANGING SUPPORTED EQUIPMENT"; return false; }
            bool success=IsOwned(id) ? host.Session.TryEquipUpgrade(Catalog,id,out string reason) :
                host.Session.TryPurchaseUpgrade(Catalog,id,out reason);
            Feedback=success ? IsEquipped(id) ? "UPGRADE EQUIPPED" : "UPGRADE PURCHASED - SELECT EQUIP" : reason;
            if(success) host.Persistence.RequestAutosave("Career equipment");
            return success;
        }
        private void Apply()
        {
            float power=1,brakes=1,fuel=1;
            var profile=host.Session.CaptureCareerProfile();
            var truck=profile?.trucks.Find(t=>t.truckId==profile.equippedTruckId);
            if(Catalog!=null && Catalog.Validate(out _) && truck!=null && profile.equippedTruckId==host.Player.DefinitionId)
                foreach(var selection in truck.upgrades)
                {
                    var definition=Catalog.FindUpgrade(selection.itemId);
                    if(!Supports(definition) || !TruckTaxiCareerCatalog.Compatible(definition.compatibleTruckIds,profile.equippedTruckId)) continue;
                    float value=((TruckTaxiCareerUpgradeTuning)definition.adapterConfiguration).multiplier;
                    switch(definition.category)
                    {
                        case TruckTaxiUpgradeCategory.Engine: power=value; break;
                        case TruckTaxiUpgradeCategory.Brakes: brakes=value; break;
                        case TruckTaxiUpgradeCategory.Fuel: fuel=value; break;
                    }
                }
            host.Handling.CareerPowerMultiplier=power;
            host.ComponentDamage?.SetCareerBrakeMultiplier(brakes);
            if(host.Fuel?.Fuel!=null && baseFuelCapacity>0)
            {
                host.Fuel.Fuel.capacity=baseFuelCapacity*fuel;
                host.Fuel.Fuel.amount=Mathf.Min(host.Fuel.Fuel.amount,host.Fuel.Fuel.capacity);
            }
        }
        private void OnDestroy()
        {
            if(host?.Session!=null) host.Session.CareerEquipmentChanged-=Apply;
            if(ownsCatalog && Catalog!=null) Destroy(Catalog);
        }
        private bool ownsCatalog;
    }
}

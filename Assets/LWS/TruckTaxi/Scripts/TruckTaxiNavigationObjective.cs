using UnityEngine;

namespace LWS.TruckTaxi
{
    // Read-only presentation/debug projection of the existing navigation lease, not another route owner.
    public readonly struct TruckTaxiNavigationObjective
    {
        public readonly TruckTaxiNavigationLayer Layer;
        public readonly string OwnerId, TargetId, Label;
        public readonly Vector3 Position;
        public readonly TruckTaxiMapMarkerType Type;
        public readonly long Token;
        public readonly bool IsForeground;
        public TruckTaxiNavigationObjective(TruckTaxiNavigationLayer layer,string owner,string id,string label,
            Vector3 position,TruckTaxiMapMarkerType type,long token,bool foreground)
        { Layer=layer; OwnerId=owner; TargetId=id; Label=label; Position=position; Type=type; Token=token; IsForeground=foreground; }

        public static bool IsPointObjective(TruckTaxiMapMarkerType type) =>
            type!=TruckTaxiMapMarkerType.Hazard && type!=TruckTaxiMapMarkerType.RoadClosure &&
            type!=TruckTaxiMapMarkerType.WorkArea;
    }
}

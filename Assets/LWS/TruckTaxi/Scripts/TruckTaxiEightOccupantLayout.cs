using System.Collections.Generic;
using UnityEngine;

namespace LWS.TruckTaxi
{
    // Explicit opt-in authoring for the Interstate tractor's Cab mount. Never inferred from
    // a truck name; does not enable private/sleeper events. Cabin fit requires rendered validation.
    public static class TruckTaxiEightOccupantLayout
    {
        public const string StarterDefinitionId="ih.truck.nwh.euro.semi.starter";
        public const string FallbackDefinitionId="ih.truck.nwh.euro.semi";

        public static bool TryApplyToConfiguredStarter(LWS.InterstateHauler.LwsPlayerTruck player,out string reason)
        {
            reason="No configured player truck.";
            if(player==null) return false;
            var rig=player.GetComponent<TruckTaxiCabOccupantRig>();
            bool explicitStarter=player.DefinitionId==StarterDefinitionId;
            // Both regions now bind the same canonical definition to the existing Taxi prefab.
            // Require its original authored cab contract, not just a generic truck identity.
            bool matchingPrefab=player.Definition==null || player.Definition.PlayerPrefab!=null &&
                HasTaxiFallbackAnchor(player.Definition.PlayerPrefab.GetComponent<TruckTaxiCabOccupantRig>());
            bool taxiFallback=matchingPrefab && player.DefinitionId==FallbackDefinitionId &&
                player.VehicleId=="player.truck.starter" && HasTaxiFallbackAnchor(rig);
            if(!explicitStarter && !taxiFallback)
            {
                reason="Truck '"+player.DefinitionId+"' has no matching Taxi starter layout contract; existing anchors retained.";
                return false;
            }
            if(rig==null) rig=player.gameObject.AddComponent<TruckTaxiCabOccupantRig>();
            return TryApplyToEmptyStarter(rig,out reason);
        }

        private static bool HasTaxiFallbackAnchor(TruckTaxiCabOccupantRig rig)
        {
            if(rig==null || rig.transform.Find("Cab")==null) return false;
            foreach(var anchor in rig.Anchors)
                if(anchor!=null && anchor.id==TruckTaxiCabAnchorId.SleeperRight && anchor.mountPath=="Cab" &&
                    (anchor.localPosition-new Vector3(.55f,.83f,-.95f)).sqrMagnitude<.000001f &&
                    (anchor.localScale-Vector3.one*.6f).sqrMagnitude<.000001f &&
                    anchor.allowedOccupants==TruckTaxiCabOccupantKind.Companion && anchor.secondaryOccupantSeat)
                    return true;
            return false;
        }

        public static TruckTaxiCabOccupantRig.Anchor[] CreateInterstateTractorAnchors()
        {
            var anchors=new TruckTaxiCabOccupantRig.Anchor[TruckTaxiCabOccupantRig.MaximumOccupants];
            anchors[0]=Seat(TruckTaxiCabAnchorId.DriverSeat,"driver",new Vector3(-.48f,.05f,.05f));
            anchors[0].allowedOccupants=TruckTaxiCabOccupantKind.Driver;
            anchors[0].secondaryOccupantSeat=false;
            anchors[0].localScale=Vector3.one;
            anchors[0].firstPersonVisible=false;
            anchors[0].occupantRadius=.4f;
            anchors[1]=Seat(TruckTaxiCabAnchorId.FrontPassengerSeat,"front-passenger",new Vector3(.48f,.05f,.05f));
            anchors[1].localScale=Vector3.one*.88f;
            anchors[1].occupantRadius=.4f;
            anchors[1].sharedPrimarySeat=true;
            var positions=new[] {
                new Vector3(-.55f,.83f,-.95f), new Vector3(.55f,.83f,-.95f),
                new Vector3(-.55f,.83f,-1.85f), new Vector3(.55f,.83f,-1.85f),
                new Vector3(-.55f,.83f,-2.75f), new Vector3(.55f,.83f,-2.75f)
            };
            for(int i=0;i<positions.Length;i++)
                anchors[i+2]=Seat((TruckTaxiCabAnchorId)(i+2),"rear-"+(i+1),positions[i]);
            return anchors;
        }
        private static TruckTaxiCabOccupantRig.Anchor Seat(TruckTaxiCabAnchorId id,string key,Vector3 position) =>
            new TruckTaxiCabOccupantRig.Anchor {
                id=id, slotId="interstate."+key, mountPath="Cab", localPosition=position,
                localScale=Vector3.one*.6f, occupantRadius=.3f, secondaryOccupantSeat=true,
                priority=id==TruckTaxiCabAnchorId.FrontPassengerSeat ? 10 : 0,
                allowedOccupants=TruckTaxiCabOccupantKind.Passenger|TruckTaxiCabOccupantKind.Companion|
                    TruckTaxiCabOccupantKind.StolenPassenger|TruckTaxiCabOccupantKind.BonusObjectiveCompanion|
                    TruckTaxiCabOccupantKind.ExtraPassenger,
                allowedSeatTypes=new[] { TruckTaxiSeatType.StandardPassenger,TruckTaxiSeatType.CompanionPassenger,
                    TruckTaxiSeatType.TinyPassenger,TruckTaxiSeatType.RobotPassenger }
            };
        // Call only for the explicitly selected starter, before passenger/driver creation.
        public static bool TryApplyToEmptyStarter(TruckTaxiCabOccupantRig rig,out string reason)
        {
            reason=null;
            if(rig==null || rig.transform.Find("Cab")==null)
            { reason="The explicitly selected starter requires its authored Cab mount."; return false; }
            if(rig.AuthoredCapacity==TruckTaxiCabOccupantRig.MaximumOccupants) return true;
            if(rig.UsedCapacity>1 || rig.GetComponent<TruckTaxiDriverPresentation>()?.Ready==true ||
                rig.GetComponentsInChildren<TruckTaxiPassengerActor>(true).Length!=0)
            { reason="Install the layout before creating occupants; live seats were not reset."; return false; }
            if(rig.AuthoredCapacity!=rig.Anchors.Count)
            { reason="Existing authored anchors are invalid or exceed capacity; they were not replaced."; return false; }
            var merged=new List<TruckTaxiCabOccupantRig.Anchor>(rig.Anchors);
            foreach(var candidate in CreateInterstateTractorAnchors())
            {
                if(merged.Count==TruckTaxiCabOccupantRig.MaximumOccupants) break;
                bool conflict=false;
                foreach(var existing in merged)
                {
                    if(existing.StableSlotId==candidate.StableSlotId ||
                        (candidate.id==TruckTaxiCabAnchorId.DriverSeat || candidate.id==TruckTaxiCabAnchorId.FrontPassengerSeat) &&
                        existing.id==candidate.id ||
                        Vector3.Distance(Position(rig,existing),Position(rig,candidate))<existing.occupantRadius+candidate.occupantRadius)
                    { conflict=true; break; }
                }
                if(!conflict) merged.Add(candidate);
            }
            if(merged.Count!=TruckTaxiCabOccupantRig.MaximumOccupants)
            { reason="Eight positions cannot be merged without moving existing authored anchors; author cabin-specific travel slots."; return false; }
            // Retain original objects: private flags, poses, restrictions, IDs, driver/front
            // transforms and capability are untouched. Nothing is committed on merge failure.
            rig.Configure(rig.AuthoredSleeperCapability,merged.ToArray());
            return true;
        }
        private static Vector3 Position(TruckTaxiCabOccupantRig rig,TruckTaxiCabOccupantRig.Anchor anchor)
        {
            var mount=string.IsNullOrWhiteSpace(anchor.mountPath) ? rig.transform : rig.transform.Find(anchor.mountPath);
            return mount.TransformPoint(anchor.localPosition);
        }
    }
}

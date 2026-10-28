using System.Collections.Generic;
using NWH.WheelController3D;
using UnityEngine;
namespace LWS.TruckCamping
{
    public sealed partial class CampExplorer
    {
        // Same public NWH cast-set ownership used by Moon/Nest. Gatherable
        // sticks/food are dynamic cargo, not suspension ground. Hull collision,
        // terrain/trails/hammocks and all original wheel configuration stay live.
        sealed class PickupMembership
        {
            public HashSet<Collider> set;public readonly HashSet<Collider> added=new HashSet<Collider>();
            public void Restore(){if(set!=null)foreach(var shape in added)set.Remove(shape);added.Clear();}
        }
        readonly Dictionary<WheelController,PickupMembership> pickupMembership=new Dictionary<WheelController,PickupMembership>();
        public void RefreshPickupGroundExclusions()
        {
            if(truck==null||match==null||match.world==null||match.world.resources==null)return;
            foreach(var wheel in truck.wheels)
            {
                var current=wheel.vehicleColliders;
                if(pickupMembership.TryGetValue(wheel,out var owned)&&!ReferenceEquals(owned.set,current)){owned.Restore();pickupMembership.Remove(wheel);owned=null;}
                if(current==null)continue; // Installed NWH Start/Initialize owns allocation.
                if(owned==null){owned=new PickupMembership{set=current};pickupMembership.Add(wheel,owned);}
                foreach(var resource in match.world.resources)
                    if(resource!=null&&resource.physicalPickup!=null&&current.Add(resource.physicalPickup))owned.added.Add(resource.physicalPickup);
            }
        }
        void RestorePickupGroundExclusions(){foreach(var owned in pickupMembership.Values)owned.Restore();pickupMembership.Clear();}
        void OnDisable(){RestorePickupGroundExclusions();}
        void OnDestroy(){RestorePickupGroundExclusions();}
    }
}

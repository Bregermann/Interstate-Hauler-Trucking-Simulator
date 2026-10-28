using System.Collections.Generic;
using NWH.WheelController3D;
using UnityEngine;
namespace LWS.TruckNestBuilding
{
    public sealed partial class NestBuilder
    {
        // Same public cast-set ownership pattern already tested in MoonTruck.
        // A one-kilogram loose supply is cargo, not a suspension foundation.
        // Genuine rooted nest structures remain normal physical wheel support.
        sealed class MaterialGroundMembership
        {
            public HashSet<Collider> originalSet;
            public readonly HashSet<Collider> added=new HashSet<Collider>();
            public void Restore(){if(originalSet!=null)foreach(var shape in added)originalSet.Remove(shape);added.Clear();}
        }
        readonly Dictionary<WheelController,MaterialGroundMembership> materialGroundMembership=new Dictionary<WheelController,MaterialGroundMembership>();
        public void RefreshMaterialGroundExclusions()
        {
            if(truck==null||match==null||match.materials==null)return;
            foreach(var wheel in truck.wheels)
            {
                var current=wheel.vehicleColliders;
                if(materialGroundMembership.TryGetValue(wheel,out var owned)&&!ReferenceEquals(owned.originalSet,current))
                {owned.Restore();materialGroundMembership.Remove(wheel);owned=null;}
                if(current==null)continue; // Native Start/Initialize owns this set.
                if(owned==null){owned=new MaterialGroundMembership{originalSet=current};materialGroundMembership.Add(wheel,owned);}
                foreach(var part in match.materials)
                {
                    if(part==null||part.shape==null)continue;
                    bool rooted=part.Site!=null&&part.Site.Rooted(part);
                    if(!rooted){if(current.Add(part.shape))owned.added.Add(part.shape);}
                    else if(owned.added.Remove(part.shape))current.Remove(part.shape);
                }
            }
        }
        void RestoreMaterialGroundExclusions()
        {
            foreach(var binding in materialGroundMembership.Values)binding.Restore();materialGroundMembership.Clear();
        }
        void OnDisable(){RestoreMaterialGroundExclusions();}
        void OnDestroy(){RestoreMaterialGroundExclusions();}
    }
}

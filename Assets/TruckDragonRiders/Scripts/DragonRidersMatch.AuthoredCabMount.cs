using System;
using System.Linq;
using LWS.TruckSports;
using UnityEngine;

namespace LWS.TruckDragonRiders
{
    public sealed partial class DragonRidersMatch
    {
        // Explicit candidate/authoring entry only. The original pinned Present,
        // normal initialization and saved Match configuration stay unchanged.
        // After a genuine StartMatch, an authorized caller may configure each
        // current (participant,viewport) pair. Existing Present consumes the
        // established cabMounts array; no new camera or simulation loop exists.
        public bool ConfigureAuthoredSourceCabMountCandidate(int slot,int view,bool enabled)
        {
            EnsureAuthoredSourceCabMount(slot,view,enabled);
            return slot>=0&&slot<ownedAuthoredCabMounts.Length&&ownedAuthoredCabMounts[slot]!=null&&drivingViews!=null&&drivingViews.cabMounts!=null&&slot<drivingViews.cabMounts.Length&&ReferenceEquals(drivingViews.cabMounts[slot],ownedAuthoredCabMounts[slot]);
        }
        const string AuthoredSourceCockpitCabName="Taxi cockpit \u00b7 Cab";
        readonly Transform[] ownedAuthoredCabMounts=new Transform[4];

        // SportsChaosAuthoring already placed the existing render-only cockpit
        // around the canonical NWH Vehicle Camera Driver. Reuse that authored
        // eye in the shared mesh's coordinates; never estimate a taller seat.
        public static bool TryMapAuthoredCabEye(MeshFilter cockpitCab,Transform cockpitEye,MeshFilter composedCab,out Vector3 eyeInSharedMesh)
        {
            eyeInSharedMesh=Vector3.zero;
            if(cockpitCab==null||cockpitCab.name!=AuthoredSourceCockpitCabName||cockpitEye==null||composedCab==null||cockpitCab.sharedMesh==null||!ReferenceEquals(cockpitCab.sharedMesh,composedCab.sharedMesh))return false;
            var eye=cockpitCab.transform.InverseTransformPoint(cockpitEye.position);
            if(!float.IsFinite(eye.x)||!float.IsFinite(eye.y)||!float.IsFinite(eye.z)||!cockpitCab.sharedMesh.bounds.Contains(eye))return false;
            eyeInSharedMesh=eye;return true;
        }
        void ClearOwnedAuthoredCabMount(int slot)
        {
            var owned=ownedAuthoredCabMounts[slot];
            if(drivingViews!=null&&drivingViews.cabMounts!=null&&slot<drivingViews.cabMounts.Length&&ReferenceEquals(drivingViews.cabMounts[slot],owned))drivingViews.cabMounts[slot]=null;
            ownedAuthoredCabMounts[slot]=null;
            if(owned!=null){if(Application.isPlaying)Destroy(owned.gameObject);else DestroyImmediate(owned.gameObject);}
        }
        void EnsureAuthoredSourceCabMount(int slot,int view,bool enabled)
        {
            if(slot<0||slot>=ownedAuthoredCabMounts.Length||drivingViews==null)return;
            var existing=drivingViews.cabMounts!=null&&slot<drivingViews.cabMounts.Length?drivingViews.cabMounts[slot]:null;
            if(existing!=null&&!ReferenceEquals(existing,ownedAuthoredCabMounts[slot]))return; // Preserve every explicit designer mount.
            if(!enabled||trucks==null||slot>=trucks.Length||trucks[slot]==null||drivingViews.cabs==null||view<0||view>=drivingViews.cabs.Length||drivingViews.cabs[view]==null||drivingViews.rally==null||view>=drivingViews.rally.views.Length||drivingViews.rally.views[view]==null)
            {ClearOwnedAuthoredCabMount(slot);return;}
            var host=trucks[slot].appearanceHost;
            if(host==null||!host.Applied||!string.Equals(host.EffectiveBodyId,"semi.standard",StringComparison.Ordinal))
            {ClearOwnedAuthoredCabMount(slot);return;}
            var reference=drivingViews.cabs[view].GetComponentsInChildren<MeshFilter>(true).Where(value=>value.name==AuthoredSourceCockpitCabName).ToArray();
            if(reference.Length!=1){ClearOwnedAuthoredCabMount(slot);return;}
            var matching=host.ComposedBodyRenderers.Where(value=>value!=null).Select(value=>value.GetComponent<MeshFilter>()).Where(value=>value!=null&&ReferenceEquals(value.sharedMesh,reference[0].sharedMesh)).ToArray();
            if(matching.Length!=1||!TryMapAuthoredCabEye(reference[0],drivingViews.rally.views[view].view.transform,matching[0],out var eye))
            {ClearOwnedAuthoredCabMount(slot);return;}
            var owned=ownedAuthoredCabMounts[slot];
            if(owned==null||owned.parent!=matching[0].transform)
            {
                ClearOwnedAuthoredCabMount(slot);owned=new GameObject("Riders authored compatible driver eye").transform;owned.SetParent(matching[0].transform,false);ownedAuthoredCabMounts[slot]=owned;
            }
            owned.localPosition=eye;owned.localRotation=Quaternion.identity;owned.localScale=Vector3.one;
            if(drivingViews.cabMounts==null)drivingViews.cabMounts=new Transform[4];
            else if(drivingViews.cabMounts.Length<4){var mounts=drivingViews.cabMounts;Array.Resize(ref mounts,4);drivingViews.cabMounts=mounts;}
            drivingViews.cabMounts[slot]=owned;
        }
    }
}

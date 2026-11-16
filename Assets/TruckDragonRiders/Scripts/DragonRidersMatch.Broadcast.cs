using System.Collections.Generic;
using LWS.TruckDragonKingdom;
using LWS.TruckSports;
using UnityEngine;

namespace LWS.TruckDragonRiders
{
    public sealed partial class DragonRidersMatch
    {
        readonly Dictionary<DragonCharacter,SkinnedMeshRenderer[]> broadcastSkins=new Dictionary<DragonCharacter,SkinnedMeshRenderer[]>();
        readonly List<Bounds> broadcastObstacles=new List<Bounds>(8);
        Quaternion broadcastOrbit;
        int broadcastSubject=-1;
        float broadcastClock=-1;
        static readonly float[] BroadcastYawSteps={0,45,-45,90,-90,135,-135,180};
        static readonly float[] BroadcastElevations={58,70,75};

        public override void DescribeBroadcast(SportsBroadcastAction action,SportsBroadcastFraming framing)
        {
            int human=-1,nearest=-1;float humanDistance=float.PositiveInfinity,nearestDistance=float.PositiveInfinity;
            for(int slot=0;slot<setup.count;slot++)if(BroadcastEligible(framing,slot))
            {
                float candidateDistance=(trucks[slot].body.position-framing.RequestedFocus).sqrMagnitude;
                if(candidateDistance<nearestDistance){nearest=slot;nearestDistance=candidateDistance;}
                if(framing.IsHuman(slot)&&candidateDistance<humanDistance){human=slot;humanDistance=candidateDistance;}
            }
            int subject=human>=0?human:BroadcastEligible(framing,framing.Primary)?framing.Primary:nearest;
            action.primary=subject;action.preferHuman=true;
            if(subject<0||subject>=racers.Length)return;
            action.reason=racers[subject].PhysicallyMounted?"MOUNTED CREW / LIVE SKYFIRE RACE":"ORIGINAL TRUCK / DRAGON RESCUE";

            // Dragon skins are separate from the NWH model hierarchy. The
            // ordinary truck-size guard cannot see a nearby wing in front of
            // the lens, and wing visuals intentionally have no giant collider.
            // Use conservative live bounds only to select an orbit; never hide
            // a dragon or replace the Cinemachine shot with a second camera.
            broadcastObstacles.Clear();
            for(int slot=0;slot<setup.count;slot++)
            {
                if(slot==subject||slot>=racers.Length)continue;
                var actor=racers[slot].Dragon;if(actor==null)continue;
                if(!broadcastSkins.TryGetValue(actor,out var skins))
                {skins=actor.motor.GetComponentsInChildren<SkinnedMeshRenderer>(true);broadcastSkins.Add(actor,skins);}
                foreach(var skin in skins)
                    if(skin!=null&&skin.enabled&&skin.gameObject.activeInHierarchy&&!skin.forceRenderingOff&&skin.bounds.size.sqrMagnitude>.01f)
                    {var bounds=skin.bounds;bounds.Expand(1.5f);broadcastObstacles.Add(bounds);}
            }
            Quaternion normal=Quaternion.Euler(framing.Configuration.elevation,framing.Configuration.azimuth,0);
            bool reset=broadcastSubject!=subject||Clock+.1f<broadcastClock;
            if(reset){broadcastOrbit=normal;broadcastSubject=subject;}
            broadcastClock=Clock;
            Vector3 target=framing.TruckBounds(subject).center;
            float length=Mathf.Max(2,Mathf.Max(framing.TruckBounds(subject).size.x,framing.TruckBounds(subject).size.z));
            float distance=Vector3.Distance(framing.cameraRig.view.transform.position,target);
            float maximum=SportsBroadcastFraming.MaximumReadableDistance(framing.TruckBounds(subject),target,broadcastOrbit,framing.Configuration.fieldOfView,framing.Configuration.minimumTruckHeight);
            distance=Mathf.Clamp(distance,length*framing.Configuration.minimumDistanceInTruckLengths,Mathf.Min(length*framing.Configuration.maximumDistanceInTruckLengths,Mathf.Max(length*framing.Configuration.minimumDistanceInTruckLengths,maximum)));
            float current=BroadcastObstruction(target,broadcastOrbit,distance);
            Quaternion desired=broadcastOrbit;
            if(current>0)
            {
                float best=current;
                foreach(float elevation in BroadcastElevations)foreach(float yaw in BroadcastYawSteps)
                {
                    var candidate=Quaternion.Euler(elevation,framing.Configuration.azimuth+yaw,0);
                    float obstruction=BroadcastObstruction(target,candidate,distance);
                    // A stable clear orbit wins over one that merely grazes a
                    // bound. Angular cost breaks ties without shot thrashing.
                    float score=obstruction+Quaternion.Angle(broadcastOrbit,candidate)*.0001f;
                    if(score<best){best=score;desired=candidate;}
                }
            }
            // Keep the current orbit while safe. Rotate smoothly only when a
            // live other-mount envelope actually obstructs its viewing corridor.
            broadcastOrbit=reset?desired:Quaternion.RotateTowards(broadcastOrbit,desired,90*Time.unscaledDeltaTime);
            action.PreferredViewRotation(broadcastOrbit);
        }

        bool BroadcastEligible(SportsBroadcastFraming framing,int slot)
            =>slot>=0&&slot<setup.count&&slot<rules.Count&&framing.Valid(slot)&&!rules.Finished(slot)&&!rules.Retired[slot];

        float BroadcastObstruction(Vector3 target,Quaternion rotation,float distance)
        {
            Vector3 end=target-(rotation*Vector3.forward)*distance;
            float score=0;
            foreach(var bounds in broadcastObstacles)
            {
                // Segment/AABB overlap includes a lens starting inside a skin
                // envelope; an ordinary linecast cannot detect visual wings.
                Vector3 direction=end-target;float enter=0,exit=1;
                for(int axis=0;axis<3&&enter<=exit;axis++)
                {
                    if(Mathf.Abs(direction[axis])<.00001f)
                    {if(target[axis]<bounds.min[axis]||target[axis]>bounds.max[axis]){enter=1;exit=0;}}
                    else
                    {
                        float a=(bounds.min[axis]-target[axis])/direction[axis],b=(bounds.max[axis]-target[axis])/direction[axis];
                        enter=Mathf.Max(enter,Mathf.Min(a,b));exit=Mathf.Min(exit,Mathf.Max(a,b));
                    }
                }
                if(enter<=exit)score+=1+Mathf.Max(0,exit-enter);
            }
            return score;
        }
    }
}

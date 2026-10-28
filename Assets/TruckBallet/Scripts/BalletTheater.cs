using System;
using DG.Tweening;
using UnityEngine;
using LWS.TruckSports;
namespace LWS.TruckBallet
{
    public sealed class BalletTheater:MonoBehaviour
    {
        public BalletMatch match;
        public Transform[] curtains=Array.Empty<Transform>();
        public Light[] followSpots=Array.Empty<Light>();
        public Transform[] curtainWings=Array.Empty<Transform>();
        public int Focus{get;private set;}
        public int PresentationSteps{get;private set;}
        Tween[] opening;
        Vector3[] scales;
        Vector3[] positions;
        float sweep;
        SportsBroadcastFraming interiorFraming;
        void ConfigureInteriorFraming()
        {
            if(match.broadcast==null)return;var framing=match.broadcast.GetComponent<SportsBroadcastFraming>();
            if(framing==null||interiorFraming==framing)return;
            // The installed Cinemachine group rig stays authoritative. Its outdoor
            // default shot was at y48.76, above this theater's opaque y29.6 ceiling.
            // Keep the actual stage shot below the proscenium using mode-local lens
            // and readability limits; do not remove the roof or alter actor poses.
            var local=JsonUtility.FromJson<SportsBroadcastSettings>(JsonUtility.ToJson(framing.Configuration));
            local.elevation=35;local.fieldOfView=55;local.minimumTruckHeight=.14f;local.preferredTruckHeight=.18f;
            local.maximumDistanceInTruckLengths=4.5f;local.nearbyTruckLengths=2.5f;
            framing.settings=local;framing.useSharedDefaults=false;interiorFraming=framing;
        }
        void Awake(){scales=Array.ConvertAll(curtains,t=>t.localScale);positions=Array.ConvertAll(curtains,t=>t.localPosition);opening=new Tween[curtains.Length];}
        public void Begin()
        {
            ConfigureInteriorFraming();Focus=PresentationSteps=0;sweep=0;
            for(int i=0;i<curtains.Length;i++){opening[i]?.Kill();curtains[i].localScale=scales[i];curtains[i].localPosition=positions[i];opening[i]=DOTween.Sequence().Join(curtains[i].DOScaleX(scales[i].x*.12f,1.8f).SetEase(Ease.InOutSine)).Join(curtains[i].DOLocalMoveX(Mathf.Sign(positions[i].x)*40,1.8f).SetEase(Ease.InOutSine)).SetLink(gameObject);}
        }
        public void Present(float dt)
        {
            if(!match.Running||match.Paused)return;PresentationSteps++;sweep+=dt;
            // One existing SportsEventCamera remains camera authority. When the
            // common Cinemachine framing owns the shot, Pose submits its focus.
            Focus=Mathf.Clamp((int)(sweep/9)%Mathf.Max(1,match.setup.count),0,match.setup.count-1);
            var subject=match.rigs[Focus].truck.body.worldCenterOfMass;float angle=Mathf.Sin(sweep*.085f)*13;
            var offset=Quaternion.Euler(0,angle,0)*new Vector3(0,15,-30);match.broadcast.Pose(subject+Vector3.up*.5f,offset,55);
            for(int i=0;i<followSpots.Length;i++)
            {
                bool active=i<match.setup.count;followSpots[i].enabled=active;if(active)followSpots[i].transform.rotation=Quaternion.Slerp(followSpots[i].transform.rotation,Quaternion.LookRotation(match.rigs[i].truck.body.worldCenterOfMass-followSpots[i].transform.position),1-Mathf.Exp(-dt*3));
            }
            for(int i=0;i<curtainWings.Length;i++)curtainWings[i].localRotation=Quaternion.Euler(0,Mathf.Sin(sweep*.4f+i)*.8f,0);
        }
        public void Stop(){if(opening!=null)foreach(var tween in opening)tween?.Kill();if(curtains!=null&&scales!=null)for(int i=0;i<curtains.Length;i++){curtains[i].localScale=scales[i];curtains[i].localPosition=positions[i];}}
        void OnDestroy(){if(opening!=null)foreach(var tween in opening)tween?.Kill();}
    }
}

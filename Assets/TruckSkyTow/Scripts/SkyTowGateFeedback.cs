using UnityEngine;

namespace LWS.TruckSkyTow
{
    // Four reusable instances of the owned Weatherade snow-burst visual.
    // Match feedback continues to own audio and scoring continues to own results.
    public sealed class SkyTowGateFeedback:MonoBehaviour
    {
        public ParticleSystem[] bursts;
        public void Play(int slot,Vector3 point)
        {
            if(bursts==null||slot<0||slot>=bursts.Length||bursts[slot]==null)return;
            var effect=bursts[slot];effect.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);effect.transform.position=point;effect.Play(true);
        }
        public void Clear(){if(bursts!=null)foreach(var effect in bursts)if(effect!=null)effect.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);}
        void OnDisable(){Clear();}
    }
}

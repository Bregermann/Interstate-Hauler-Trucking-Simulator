using UnityEngine;
namespace LWS.TruckArchaeology
{
    public sealed class ArchaeologyStaff:MonoBehaviour
    {
        public ArchaeologyMatch match;public Animator[] workers;Vector3[] origins;string[] states;float cheerUntil,lastClock;
        public void React(float clock){cheerUntil=clock+2.5f;}
        void Start(){origins=new Vector3[workers.Length];states=new string[workers.Length];for(int i=0;i<workers.Length;i++)origins[i]=workers[i].transform.position;}
        void Update()
        {
            if(origins==null||match==null)return;
            if(match.Clock<lastClock)cheerUntil=0;lastClock=match.Clock;
            for(int i=0;i<workers.Length;i++)
            {
                var actor=workers[i];actor.speed=match.Live?1:0;if(!match.Live)continue;
                float phase=(match.Clock+i*3)%24;bool walk=phase<12&&match.Clock>=cheerUntil;string state=match.Clock<cheerUntil?"Cheer":walk?"Walk":"Idle";if(states[i]!=state){actor.CrossFade(state,.15f);states[i]=state;}
                if(walk){float t=phase/12;actor.transform.position=origins[i]+new Vector3(Mathf.Sin(t*Mathf.PI)*3,0,t<.5f?t*8:(1-t)*8);actor.transform.rotation=Quaternion.LookRotation(new Vector3(Mathf.Cos(t*Mathf.PI),0,t<.5f?1:-1));}
            }
        }
    }
}

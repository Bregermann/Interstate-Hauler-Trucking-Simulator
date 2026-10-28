using UnityEngine;
namespace LWS.TruckFashionShow
{
    public sealed class FashionPeople:MonoBehaviour
    {
        public FashionMatch match;public Animator[] shoppers,judges,shopkeepers;Vector3[] origin;string[] states;float reactionUntil;bool applause;
        public int Reactions {get;private set;}
        void Start(){origin=new Vector3[shoppers.Length];for(int i=0;i<origin.Length;i++)origin[i]=shoppers[i].transform.position;states=new string[judges.Length+shoppers.Length+shopkeepers.Length];}
        public void ResetPresentation(){reactionUntil=0;Reactions=0;}
        public void React(bool success,float clock){reactionUntil=clock+1.5f;applause=success;Reactions++;}
        void Update()
        {
            if(match==null||origin==null)return;int k=0;bool live=match.Live&&!match.WardrobeOpen;
            for(int i=0;i<judges.Length;i++,k++)Animate(judges[i],k,live&&match.Clock<reactionUntil?(applause?"Cheer":"Shout"):"Idle",live);
            for(int i=0;i<shoppers.Length;i++,k++)
            {
                float phase=(match.Clock+i*2)%16;bool walking=phase<10;Animate(shoppers[i],k,walking?"Walk":"Idle",live);
                if(live&&walking){float t=phase/10;shoppers[i].transform.position=origin[i]+new Vector3(Mathf.Sin(t*Mathf.PI)*3,0,t<.5f?t*10:(1-t)*10);shoppers[i].transform.rotation=Quaternion.LookRotation(new Vector3(Mathf.Cos(t*Mathf.PI),0,t<.5f?1:-1));}
            }
            for(int i=0;i<shopkeepers.Length;i++,k++)Animate(shopkeepers[i],k,"Idle",live);
        }
        void Animate(Animator actor,int index,string next,bool live){actor.speed=live?1:0;if(states[index]!=next){states[index]=next;actor.CrossFade(next,.18f);}}
    }
}

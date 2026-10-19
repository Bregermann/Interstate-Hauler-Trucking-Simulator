using UnityEngine;
namespace LWS.TruckLimbo {
public sealed class LimboAudience:MonoBehaviour {public LimboMatch match;public Animator[] actors;LimboPhase prior;void Start(){for(int i=0;i<actors.Length;i++)if(actors[i]!=null){actors[i].speed=.85f+i%5*.07f;if(actors[i].HasState(0,Animator.StringToHash("Dance")))actors[i].Play("Dance",0,i*.13f%1);}}void Update(){if(match.Phase==prior)return;string state=match.Phase==LimboPhase.RoundResult?"Cheer":"Dance";foreach(var actor in actors)if(actor!=null&&actor.HasState(0,Animator.StringToHash(state)))actor.CrossFade(state,.25f);prior=match.Phase;}}
}

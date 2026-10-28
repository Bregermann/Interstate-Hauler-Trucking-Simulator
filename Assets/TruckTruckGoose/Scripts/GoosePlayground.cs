using UnityEngine;
namespace LWS.TruckTruckGoose
{
    // Cosmetic movement only: the gantries are outside the legal race annulus.
    public sealed class GoosePlayground : MonoBehaviour
    {
        public GooseMatch match;public Transform[] swingPivots;public Animator[] spectators;public TextMesh announcement;
        float time;int announcedRound=-1;GooseRules.Stage phase;
        void Update(){if(match==null)return;if(match.Live)time+=Time.deltaTime;for(int i=0;i<swingPivots.Length;i++)if(swingPivots[i]!=null)swingPivots[i].localRotation=Quaternion.Euler(Mathf.Sin(time*.85f+i)*22,0,0);
            if(match.Rules==null)return;bool changed=phase!=match.Rules.Phase||announcedRound!=match.Rules.Round;if(changed){phase=match.Rules.Phase;announcedRound=match.Rules.Round;foreach(var a in spectators)if(a!=null&&a.runtimeAnimatorController!=null)a.CrossFade(phase==GooseRules.Stage.Racing?"Cheer":"Idle",.18f);}
            foreach(var a in spectators)if(a!=null)a.speed=match.Live?1:0;
            if(announcement!=null)announcement.text=phase==GooseRules.Stage.Racing?"GOOSE!":phase==GooseRules.Stage.RoundResult?"CLAIMED!":"TRUCK · TRUCK · ...";
        }
    }
}

using System;
using DG.Tweening;
using UnityEngine;

namespace LWS.TruckDragonKingdom.Ambience
{
    public sealed class DragonAmbientBird : MonoBehaviour
    {
        public string regionId,sourceAsset;
        public Animator animator;
        public Transform[] route=Array.Empty<Transform>();
        public Renderer[] visuals=Array.Empty<Renderer>();
        public float duration=45;
        public Vector3 authoredPosition;
        public Quaternion authoredRotation=Quaternion.identity;
        public bool Active { get; private set; }
        public int Circuits { get; private set; }
        Tween flight;
        readonly RaycastHit[] hits=new RaycastHit[16];
        public void ValidateBindings()
        {
            if(route.Length<8||animator==null||animator.avatar==null||!animator.avatar.isValid||animator.runtimeAnimatorController==null||animator.runtimeAnimatorController.animationClips.Length==0||visuals.Length==0||!float.IsFinite(duration)||duration<20)throw new InvalidOperationException("Actual wing rig, compatible flight clip and clear closed air route required.");
        }
        public void ResetForMatch(int seed)
        {
            flight?.Kill();transform.SetPositionAndRotation(authoredPosition,authoredRotation);Circuits=0;
            var points=new Vector3[route.Length+1];for(int i=0;i<route.Length;i++)points[i]=route[i].position;points[points.Length-1]=route[0].position;
            // Same installed DOTween path presentation foundation as the
            // existing Sports audience, with a closed route and no play warp.
            flight=transform.DOPath(points,duration,DG.Tweening.PathType.Linear).SetEase(Ease.Linear).SetLoops(-1).SetLookAt(.03f,Vector3.forward,Vector3.up).SetLink(gameObject).OnStepComplete(()=>Circuits++).Pause();
            SetActivity(false,false);
        }
        public void SetActivity(bool live,bool visible)
        {
            Active=live&&visible;
            bool blocked=false;
            if(Active)
            {
                int count=Physics.SphereCastNonAlloc(transform.position,.5f,transform.forward,hits,2,~0,QueryTriggerInteraction.Ignore);blocked=count==hits.Length;
                for(int i=0;i<count;i++)if(hits[i].collider!=null&&!hits[i].collider.transform.IsChildOf(transform))blocked=true;
            }
            if(flight!=null){if(Active&&!blocked)flight.Play();else flight.Pause();}
            if(animator!=null)animator.enabled=Active;
            foreach(var renderer in visuals)if(renderer!=null)renderer.enabled=visible;
        }
        void OnDestroy(){flight?.Kill();flight=null;}
    }
}

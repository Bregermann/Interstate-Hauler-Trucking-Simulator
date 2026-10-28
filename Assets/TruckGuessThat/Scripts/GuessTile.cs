using UnityEngine;
using DG.Tweening;
namespace LWS.TruckGuessThat
{
    public sealed class GuessTile : MonoBehaviour
    {
        public Transform hinge; public int owner,identity;
        public bool Folded { get; private set; }
        Tween fold;
        public void SetFolded(bool value,bool immediate=false){if(hinge==null)return;if(Folded==value&&!immediate)return;Folded=value;if(fold!=null)fold.Kill();var target=new Vector3(value?88:0,0,0);if(immediate)hinge.localEulerAngles=target;else fold=hinge.DOLocalRotate(target,.42f).SetEase(Ease.InOutCubic).SetLink(gameObject);}
        public void Pause(bool paused){if(fold!=null){if(paused)fold.Pause();else fold.Play();}}
        void OnDisable(){if(fold!=null)fold.Kill();fold=null;}
    }
}

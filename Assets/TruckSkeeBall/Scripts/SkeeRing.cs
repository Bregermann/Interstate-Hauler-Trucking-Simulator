using UnityEngine;using DG.Tweening;
namespace LWS.TruckSkeeBall {public sealed class SkeeRing:MonoBehaviour {
 public int points;public float radius;public Vector2 center;public Renderer lightRing;MaterialPropertyBlock colors;Tween pulse;public void ResetLight(){pulse?.Kill();if(colors==null)colors=new MaterialPropertyBlock();colors.SetColor("_EmissionColor",Color.black);lightRing.SetPropertyBlock(colors);}
 public void Celebrate(){pulse?.Kill();float glow=0;pulse=DOTween.To(()=>glow,v=>{glow=v;colors.SetColor("_EmissionColor",new Color(1,.3f,.02f)*v);lightRing.SetPropertyBlock(colors);},5,.16f).SetLoops(6,LoopType.Yoyo);}void OnDestroy(){pulse?.Kill();}
}}

using DG.Tweening;
using UnityEngine;
namespace LWS.TruckTargetLaunch {
// A bounded presentation over the existing score authority, using DOTween and ring geometry.
public sealed class TargetScoreFeedback:MonoBehaviour {
 public TargetMatch match;public LineRenderer[] rings;readonly Tween[] tweens=new Tween[4];MaterialPropertyBlock tint;
 public void Show(int slot,int score){if(score<=0||rings==null||slot>=rings.Length)return;tweens[slot]?.Kill();var line=rings[slot];line.gameObject.SetActive(true);float radius=score==100?2.3f:score==50?8:score==25?18:30;Vector3 center=score>=50?match.PrecisionCenterAt(match.Clock):Vector3.zero;for(int i=0;i<line.positionCount;i++){float a=i*Mathf.PI*2/(line.positionCount-1);line.SetPosition(i,match.target.TransformPoint(center+new Vector3(Mathf.Sin(a)*radius,.12f+slot*.008f,Mathf.Cos(a)*radius)));}if(tint==null)tint=new MaterialPropertyBlock();tint.SetColor("_BaseColor",match.setup.players[slot].color);line.SetPropertyBlock(tint);line.startWidth=line.endWidth=.22f;tweens[slot]=DOVirtual.Float(.22f,0,1.6f,v=>line.startWidth=line.endWidth=v).SetEase(Ease.OutQuad).SetLink(gameObject).OnComplete(()=>line.gameObject.SetActive(false));}
 public void ResetRound(){for(int i=0;i<tweens.Length;i++){tweens[i]?.Kill();if(rings!=null&&i<rings.Length&&rings[i]!=null)rings[i].gameObject.SetActive(false);}}
 void OnDisable()=>ResetRound();
}
}

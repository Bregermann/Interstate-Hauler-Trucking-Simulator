using DG.Tweening;using UnityEngine;
namespace LWS.TruckDentist {
 public sealed class DentalPatient:MonoBehaviour {
  public DentistMatch match;public Transform jaw,tongue,lidLeft,lidRight,lip;public int Reactions{get;private set;}public float AnimationAngle{get;private set;}float reaction;Tween tween;
  public void Prepare(){tween?.Kill();reaction=0;Reactions=0;Tick(0);}
  public void React(bool correct){Reactions++;tween?.Kill();reaction=correct?1:-1;tween=DOTween.To(()=>reaction,x=>reaction=x,0,1.4f);}
  public void Tick(float dt){float clock=match.rules.Clock;AnimationAngle=Mathf.Sin(clock*.9f)*1.2f+reaction*4;jaw.localRotation=Quaternion.Euler(AnimationAngle,0,0);tongue.localRotation=Quaternion.Euler(0,0,Mathf.Sin(clock*1.3f)*1.8f);float blink=Mathf.Pow(Mathf.Max(0,Mathf.Cos(clock*.65f)),35);var scale=new Vector3(1,Mathf.Max(.08f,1-blink),1);lidLeft.localScale=lidRight.localScale=scale;lip.localRotation=Quaternion.Euler(0,0,reaction*3);}
  void OnDestroy(){tween?.Kill();}
 }
}

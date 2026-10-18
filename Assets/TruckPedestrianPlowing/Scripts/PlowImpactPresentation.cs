using UnityEngine;
namespace LWS.TruckPedestrianPlowing {
 public sealed class PlowImpactPresentation:MonoBehaviour {
  const string Preference="TruckPedestrianPlowing.BloodAmount";
  public PlowMatch match;public UnityEngine.UI.Slider slider;public ParticleSystem[] splashes;
  public float Amount{get;private set;}int next;bool dirty;
  void Start(){Amount=Mathf.Clamp01(LWS.TruckSports.SportsPrefs.GetFloat(Preference,match.crowdSettings.bloodAmount));slider.SetValueWithoutNotify(Amount*100);slider.onValueChanged.AddListener(Change);}
  void Change(float value){SetAmount(value*.01f);}
  public void SetAmount(float amount){Amount=Mathf.Clamp01(amount);LWS.TruckSports.SportsPrefs.SetFloat(Preference,Amount);dirty=true;if(slider!=null)slider.SetValueWithoutNotify(Amount*100);}
  public void Impact(Vector3 point){
   if(Amount<=0||splashes==null||splashes.Length==0)return;var splash=splashes[next++%splashes.Length];splash.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);splash.transform.position=point;
   splash.Emit(Mathf.RoundToInt(18*Amount));
  }
  public void ResetEffects(){if(splashes!=null)foreach(var splash in splashes)splash.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);}
  void OnDestroy(){if(slider!=null)slider.onValueChanged.RemoveListener(Change);if(dirty)LWS.TruckSports.SportsPrefs.Save();}
 }
}

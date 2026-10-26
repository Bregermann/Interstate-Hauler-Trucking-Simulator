using DG.Tweening;
using TMPro;
using UnityEngine;

namespace LWS.TruckRedLightGreenLight
{
 // Read-only world presentation. The original phase transition owns the laugh,
 // one-second deadline, master animation, penalties and enormous destruction.
 [DefaultExecutionOrder(950)]public sealed class RedLightCarnivalVenue:MonoBehaviour
 {
  public RedLightMatch match;public RedLightGroundCamera groundCamera;
  public Renderer[] redLenses,greenLenses;public TMP_Text[] warnings;
  public GameObject[] supersededFixtures;public Renderer[] supersededSignals;
  public bool WarningVisible{get;private set;}public int WarningPresentations{get;private set;}
  MaterialPropertyBlock block;Tween flash;float glow=1;RedLightPhase previous=(RedLightPhase)(-1);string observedMatch;
  void Awake(){block=new MaterialPropertyBlock();}
  void HideOld()
  {
   if(supersededFixtures!=null)foreach(var fixture in supersededFixtures)if(fixture!=null)fixture.SetActive(false);
   if(supersededSignals!=null)foreach(var signal in supersededSignals)if(signal!=null)signal.enabled=false;
  }
  void Lens(Renderer[] lamps,Color color,float intensity)
  {
   if(lamps==null)return;block??=new MaterialPropertyBlock();block.Clear();block.SetColor("_BaseColor",color*(intensity>.2f?1:.08f));block.SetColor("_EmissionColor",color*intensity);foreach(var lamp in lamps)if(lamp!=null)lamp.SetPropertyBlock(block);
  }
  void LateUpdate()
  {
   if(match==null)return;HideOld();bool running=match.Running&&!match.Finished&&!match.ReplayControls;
   if(observedMatch!=match.MatchId){previous=(RedLightPhase)(-1);WarningPresentations=0;observedMatch=match.MatchId;}
   WarningVisible=running&&match.rules.Phase==RedLightPhase.LaughWarning;
   if(previous!=match.rules.Phase||!running)
   {
    previous=match.rules.Phase;flash?.Kill();flash=null;glow=1;
    if(WarningVisible){WarningPresentations++;flash=DOVirtual.Float(.45f,1,.13f,value=>glow=value).SetEase(Ease.InOutSine).SetLoops(-1,LoopType.Yoyo).SetLink(gameObject);}
   }
   bool red=match.rules.Phase==RedLightPhase.Red;Lens(redLenses,Color.red,red?4:.04f);Lens(greenLenses,Color.green,red?.04f:WarningVisible?glow*4:match.rules.Phase==RedLightPhase.Countdown?.18f:4);
   if(warnings!=null)foreach(var warning in warnings)if(warning!=null)warning.gameObject.SetActive(WarningVisible);
   if(running&&match.menus!=null&&match.menus.status!=null){var status=match.menus.status;status.gameObject.SetActive(false);var plate=status.transform.parent.Find("Broadcast status backplate");if(plate!=null)plate.gameObject.SetActive(false);}
  }
  void OnDisable(){flash?.Kill();flash=null;WarningVisible=false;if(warnings!=null)foreach(var warning in warnings)if(warning!=null)warning.gameObject.SetActive(false);}
 }
}

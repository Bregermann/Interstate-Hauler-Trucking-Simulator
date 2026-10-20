using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using LWS.TruckSports;

namespace LWS.TruckFieldGoal {
// Read-only stadium presentation. FieldGoalRules remains the only scoring authority.
[DefaultExecutionOrder(240)]
public sealed class FieldGoalStadiumBoard : MonoBehaviour {
 public FieldGoalMatch match;
 public Transform fieldAssembly;
 public Collider[] runoff=System.Array.Empty<Collider>();
 public bool IsRunoff(Collider surface){foreach(var c in runoff)if(c==surface)return true;return false;}
 public Canvas screen;
 public TMP_Text title, attempt, current, status, weather, controls, record;
 public TMP_Text[] names, scores, kicks;
 public Image[] colors;
 public GameObject[] cards;
 public Michsky.UI.Heat.ButtonManager[] buttons;
 float next;
 bool wasReplay;
 string previousOutcome;
 readonly RectTransform[] replayRects=new RectTransform[4];
 readonly Vector2[] boardPositions=new Vector2[4],boardSizes=new Vector2[4];
 bool transportInHUD;

 void Start(){
  for(int i=0;i<4;i++){var rect=buttons[i].GetComponent<RectTransform>();replayRects[i]=rect;boardPositions[i]=rect.anchoredPosition;boardSizes[i]=rect.sizeDelta;}
  foreach(var button in buttons){
   string key=button.GetComponent<SportsMenuAction>().action;button.onClick.AddListener(()=>match.menus.Action(key));
   if(button.GetComponent<SportsBroadcastPlate>()==null)button.gameObject.AddComponent<SportsBroadcastPlate>().cut=10;
   var visual=button.GetComponent<LWS.TruckBilliards.BilliardsButtonVisual>();
   if(visual!=null){visual.normal=SportsBroadcastCanvas.Raised;visual.highlight=new Color(.65f,.20f,.035f,.99f);}
  }
  screen.worldCamera=match.cameraRig.view;
  AlignField();Refresh();
 }
 public void AlignField(){
  // Attempt distances still use the established ball-at-origin simulation.
  // Move the venue between attempts so its back line always coincides with the uprights.
  fieldAssembly.position=new Vector3(0,0,match.GoalZ);
 }
 void LateUpdate(){
  if(screen==null||match==null)return;
  bool replay=match.ReplayControls&&match.Live&&match.menus.Screen==SportsEventScreen.Play;
  PositionReplayTransport(replay);
  for(int i=0;i<4;i++)if(buttons[i].gameObject.activeSelf!=replay)buttons[i].gameObject.SetActive(replay);
  bool retry=match.Practice&&match.Live&&!replay&&match.menus.Screen==SportsEventScreen.Play;
  if(buttons[4].gameObject.activeSelf!=retry)buttons[4].gameObject.SetActive(retry);
  if(replay&&!wasReplay&&EventSystem.current!=null&&!match.AllAI)EventSystem.current.SetSelectedGameObject(buttons[0].gameObject);
  wasReplay=replay;
  if(Time.unscaledTime<next)return;
  next=Time.unscaledTime+.1f;Refresh();
 }
 // Close recorded-play angles deliberately frame the football rather than the
 // distant scoreboard. Move the SAME Heat controls into the existing HUD for
 // that presentation; never duplicate actions or change replay/scoring state.
 public void PositionReplayTransport(bool replay){
  if(replay){
   var hud=(RectTransform)match.menus.hud.transform;
   float width=Mathf.Min(290,Mathf.Max(120,(hud.rect.width-96)/4-14));
   for(int i=0;i<4;i++){
    var rect=replayRects[i];if(rect==null)continue;
    if(!transportInHUD)rect.SetParent(hud,false);
    rect.anchorMin=rect.anchorMax=new Vector2(.5f,0);rect.pivot=new Vector2(.5f,.5f);
    rect.localScale=Vector3.one;rect.localRotation=Quaternion.identity;
    rect.sizeDelta=new Vector2(width,58);rect.anchoredPosition3D=new Vector3((i-1.5f)*(width+14),54,0);
   }
   transportInHUD=true;
  }else if(transportInHUD&&screen!=null){
   for(int i=0;i<4;i++){
    var rect=replayRects[i];if(rect==null)continue;rect.SetParent(screen.transform,false);
    rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);
    rect.localScale=Vector3.one;rect.localRotation=Quaternion.identity;
    rect.sizeDelta=boardSizes[i];rect.anchoredPosition3D=new Vector3(boardPositions[i].x,boardPositions[i].y,0);
   }
   transportInHUD=false;
  }
 }
 public void Refresh(){
  int slot=Mathf.Clamp(match.rules.Current,0,match.setup.count-1);
  title.text="TRUCK SPORTS: PARTY!  /  FIELD GOAL";
  attempt.text=match.Practice?"SOLO PRACTICE":match.rules.Round<=5?"ATTEMPT "+match.rules.Round+" / 5":"TIEBREAK "+(match.rules.Round-5);
  current.text=match.setup.players[slot].name+"  /  "+Mathf.RoundToInt(match.GoalZ/.9144f)+" YARDS";
  current.color=match.setup.players[slot].color;
  status.text=match.Finished?"MATCH COMPLETE":match.Running?match.Status.Split('\n')[0]:"LINE UP. DRIVE IN. SEND IT.";
  var flow=match.CurrentWind;
  string bearing=flow.magnitude<.1f?"CALM":Mathf.Abs(flow.x)>Mathf.Abs(flow.z)?flow.x>0?"RIGHT CROSSWIND":"LEFT CROSSWIND":flow.z>0?"TAILWIND":"HEADWIND";
  weather.text=(match.Forecast!=null?match.atmosphere.Description:"MATCH FORECAST")+"  /  "+(flow.magnitude*2.236936f).ToString("F1")+" MPH  /  "+bearing;
  controls.text=match.ReplayControls?"RECORDED PLAY  /  "+match.replay.Speed.ToString("0.##")+"x  /  CAMERA "+(match.replay.Angle+1)+"  /  TAB OR D-PAD TO SELECT":match.Controls(slot)+"  /  ESC OR START: PAUSE";
  string key="TruckFieldGoal.Statistics.Foot";
  record.text="CAREER  "+SportsPrefs.GetInt(key+".Goals",0)+" GOOD / "+SportsPrefs.GetInt(key+".Attempts",0)+" KICKS  /  BEST "+SportsPrefs.GetInt("TruckFieldGoal.Best.Foot",0);
  for(int i=0;i<cards.Length;i++){
   bool active=i<match.setup.count;cards[i].SetActive(active);if(!active)continue;
   names[i].text=match.setup.players[i].name;names[i].color=colors[i].color=match.setup.players[i].color;
   scores[i].text=match.rules.goals[i].ToString();
   kicks[i].text=match.rules.attempts[i]+" KICKS"+(i==slot&&!match.Finished?"  /  UP NOW":"");
  }
  if(previousOutcome!=match.Outcome){previousOutcome=match.Outcome;if(!string.IsNullOrEmpty(previousOutcome)){status.transform.DOKill();status.transform.localScale=Vector3.one;status.transform.DOPunchScale(Vector3.one*.07f,.35f,2,.5f).SetUpdate(true).SetLink(gameObject);}}
 }
 void OnDisable(){PositionReplayTransport(false);if(status!=null){status.transform.DOKill();status.transform.localScale=Vector3.one;}}
}
}

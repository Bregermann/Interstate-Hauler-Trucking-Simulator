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

 void Start(){
  foreach(var button in buttons){string key=button.GetComponent<SportsMenuAction>().action;button.onClick.AddListener(()=>match.menus.Action(key));}
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
  for(int i=0;i<4;i++)if(buttons[i].gameObject.activeSelf!=replay)buttons[i].gameObject.SetActive(replay);
  bool retry=match.Practice&&match.Live&&!replay&&match.menus.Screen==SportsEventScreen.Play;
  if(buttons[4].gameObject.activeSelf!=retry)buttons[4].gameObject.SetActive(retry);
  if(replay&&!wasReplay&&EventSystem.current!=null&&!match.AllAI)EventSystem.current.SetSelectedGameObject(buttons[0].gameObject);
  wasReplay=replay;
  if(Time.unscaledTime<next)return;
  next=Time.unscaledTime+.1f;Refresh();
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
 void OnDisable(){if(status!=null){status.transform.DOKill();status.transform.localScale=Vector3.one;}}
}
}

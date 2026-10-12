using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using TMPro;
using Michsky.UI.Heat;
namespace LWS.TruckBilliards
{

 public sealed class BilliardsMenus:MonoBehaviour
 {
  public BilliardsSession session;public GameObject[] pages=new GameObject[12];
  public TMP_InputField name1,name2;public TMP_Text setupInfo,modeText,breakText,color1Text,color2Text,joinInfo,hotDeviceText,handoffText,resultText,helpText,confirmText,fullscreenText,resolutionText;
  public UnityEngine.UI.Slider masterSlider,sfxSlider,sensitivitySlider;public ButtonManager joinStart,disconnectResume;
  public TMP_Text formatText,difficultyText,vibrationText;public ButtonManager nextRackButton,rematchButton;
  public BilliardsScreen returnScreen=BilliardsScreen.Main;string confirmation;bool lastJoinReady;
  public readonly Color[] Palette={new Color(.15f,.8f,1),new Color(1,.55f,.13f),new Color(.85f,.25f,.7f),new Color(.6f,1,.3f),new Color(.9f,.85f,.25f)};
  int color1,color2=1;
  public const string RulesText="Drive your miniature truck into the WHITE cue ball with its front bumper. Run up for power; tap gently for position.\n\nOnly the current player can strike, once per shot. Both trucks are ghosts to moving balls and object balls.\n\nFirst legal pot assigns SOLIDS or STRIPES. Legal own-ball pots keep your turn. Wrong first contact or a miss passes the turn where the cue stops.\n\nOnly a cue-ball SCRATCH allows placement. Clear your group BEFORE the shot to win with the 8. Early/illegal 8 loses. Break 8 is respotted.\n\nHot seat: pass the device only when the turn changes after settling.";
  void Start()
  {
   foreach(var item in GetComponentsInChildren<BilliardsMenuAction>(true)){var key=item.action;item.button.onClick.AddListener(()=>Action(key));}
   name1.onEndEdit.AddListener(v=>{session.players[0].name=v;RefreshSetup();});
   name2.onEndEdit.AddListener(v=>{session.players[1].name=v;RefreshSetup();});
   masterSlider.onValueChanged.AddListener(v=>{session.settings.master=v/100;session.settings.ApplyAudio();});
   sfxSlider.onValueChanged.AddListener(v=>{session.settings.sfx=v/100;session.settings.ApplyAudio();});
   sensitivitySlider.onValueChanged.AddListener(v=>session.settings.sensitivity=v);
   session.menus=this;Show(session.Screen);
  }
  public void Show(BilliardsScreen screen)
  {
   for(int i=0;i<pages.Length;i++)if(pages[i]!=null)pages[i].SetActive(i==(int)screen);
   if(screen==BilliardsScreen.Setup)RefreshSetup();
   if(screen==BilliardsScreen.Join||screen==BilliardsScreen.Disconnect)RefreshJoin();
   if(screen==BilliardsScreen.Handoff)handoffText.text="PASS TO\n<color=#"+ColorUtility.ToHtmlStringRGB(session.players[session.match.rules.Turn].color)+">"+session.Name(session.match.rules.Turn).ToUpper()+"</color>\n\nRelease the controls, then press "+(session.hotSeatGamepad?"South / A / Cross":"Enter or Space")+" when ready.\n"+(session.match.rules.Phase==MatchPhase.Placement?"Then place the cue ball.":"Your truck will receive control.");
      if(screen==BilliardsScreen.Result){
    bool decided=session.series.Finished;var title=pages[(int)screen].transform.Find("Title").GetComponent<TMP_Text>();
    title.text=session.series.Required==1?"MATCH COMPLETE":decided?"SERIES WON":"RACK COMPLETE";title.color=session.players[session.match.rules.Winner].color;
    resultText.text=session.Name(session.match.rules.Winner).ToUpper()+" WINS "+(decided&&session.series.Required>1?"THE SERIES":"THE RACK")+"\n"+session.ScoreText+"\n\n"+session.match.rules.Notice;
    resultText.color=session.players[session.match.rules.Winner].color;resultText.fontSize=32;
    nextRackButton.gameObject.SetActive(!decided);rematchButton.gameObject.SetActive(decided);rematchButton.SetText(session.series.Required==1?"REMATCH • ALTERNATE BREAK":"REMATCH SERIES");
   }
   if(screen==BilliardsScreen.Help)helpText.text=session.IsPractice?"PRACTICE\nDrive into the cue ball once per shot. Keep driving as balls settle; the bumper is disarmed until still.\nObject balls, including the 8, stay pocketed. A scratch opens placement.\nPause > Reset Rack works mid-shot. Pause > Reposition Cue opens placement only while settled.\n\n"+session.Bindings(0)+"\nPlacement: "+session.Bindings(0,true):RulesText+"\n\n"+session.Name(0)+": "+session.Bindings(0)+"\n"+session.Name(1)+": "+session.Bindings(1)+"\nPLACEMENT\n"+session.Name(0)+": "+session.Bindings(0,true)+"\n"+session.Name(1)+": "+session.Bindings(1,true);
      if(screen==BilliardsScreen.Settings)RefreshSettings();
   if(screen==BilliardsScreen.Pause){
    foreach(var action in pages[(int)screen].GetComponentsInChildren<BilliardsMenuAction>(true)){
     if(action.action=="restart"||action.action=="restartseries")action.gameObject.SetActive(!session.IsPractice);
     if(action.action=="practicereset"||action.action=="reposition"){action.gameObject.SetActive(session.IsPractice);if(action.action=="reposition")SetInteractable(action.button,session.match.AreSettled&&session.match.rules.Phase==MatchPhase.Aiming);}
    }
   }
   Rewire();
   if(EventSystem.current!=null){
    EventSystem.current.SetSelectedGameObject(null);
    if(screen!=BilliardsScreen.Match&&pages[(int)screen]!=null){
     var first=pages[(int)screen].GetComponentsInChildren<UnityEngine.UI.Selectable>().FindFirstInteractable();
          if(screen==BilliardsScreen.Join)first=(session.mode==BilliardsMode.SeparateControllers?session.BothPadsReady:session.Ready(0))?joinStart.GetComponent<UnityEngine.UI.Button>():pages[(int)screen].transform.Find("back Button").GetComponent<UnityEngine.UI.Button>();
     if(screen==BilliardsScreen.Disconnect&&(session.mode==BilliardsMode.SeparateControllers?session.BothPadsReady:session.Ready(0)))first=disconnectResume.GetComponent<UnityEngine.UI.Button>();
     if(first!=null)EventSystem.current.SetSelectedGameObject(first.gameObject);
    }
   }
  }
  public void RefreshSetup()
  {
   string[] modes={"SINGLE PLAYER vs AI","TWO PLAYERS • SHARED KEYBOARD","TWO PLAYERS • SEPARATE CONTROLLERS","TWO PLAYERS • HOT SEAT","PRACTICE • ONE TRUCK"};
   modeText.text=modes[(int)session.mode];breakText.text="Opening break: "+(session.breakChoice==2?"RANDOM":session.Name(session.breakChoice));
   name1.SetTextWithoutNotify(session.players[0].name);name2.SetTextWithoutNotify(session.IsAI(1)?"AI":session.players[1].name);name2.interactable=!session.IsAI(1);
   color1Text.text="Truck color: "+ColorName(color1);color2Text.text="Truck color: "+ColorName(color2);color1Text.color=Palette[color1];color2Text.color=Palette[color2];
   hotDeviceText.text=(session.IsPractice?"Practice device: ":"Hot-seat device: ")+(session.hotSeatGamepad?"ONE CONTROLLER":"KEYBOARD + MOUSE");
   hotDeviceText.transform.parent.gameObject.SetActive(session.mode==BilliardsMode.HotSeat||session.IsPractice);
      modeText.transform.parent.gameObject.SetActive(!session.IsPractice);name2.transform.parent.Find("PLAYER 2 NAME").gameObject.SetActive(!session.IsPractice);
   name2.gameObject.SetActive(!session.IsPractice);color2Text.transform.parent.gameObject.SetActive(!session.IsPractice);breakText.transform.parent.gameObject.SetActive(!session.IsPractice);
   formatText.transform.parent.gameObject.SetActive(!session.IsPractice);difficultyText.transform.parent.gameObject.SetActive(session.mode==BilliardsMode.SinglePlayer);
   formatText.text="Length: "+new[]{"SINGLE RACK","BEST OF 3","BEST OF 5"}[(int)session.format];difficultyText.text="AI: "+session.difficulty.ToString().ToUpper();
   setupInfo.text=session.Message+"\nDevices: "+(session.mode==BilliardsMode.SeparateControllers?"TWO distinct gamepads required":session.OnePadMode?"ONE gamepad required":"ONE keyboard + mouse")+"\n\n"+session.Name(0)+": "+session.Bindings(0)+(session.IsPractice?"\nPractice: Esc / Start opens Reset Rack and Reposition Cue Ball.":"\n\n"+session.Name(1)+": "+session.Bindings(1));Rewire();
  }
  string ColorName(int i)=>new[]{"BLUE","ORANGE","MAGENTA","LIME","GOLD"}[i];
  public void RefreshJoin()
  {
   string Description(int side)=>session.Ready(side)?session.players[side].pad.displayName+" • ID "+session.players[side].pad.deviceId:"NOT JOINED / DISCONNECTED";
   string text=session.Message+"\nPress NORTH / Y / TRIANGLE on each controller to join.\n\n"+session.Name(0)+": "+Description(0);
   if(session.mode==BilliardsMode.SeparateControllers)text+="\n"+session.Name(1)+": "+Description(1);
   text+="\n\nNorth joins the first empty slot. Use Reassign to replace a slot.\nOne controller cannot occupy both slots in Separate Controllers.";
   foreach(var screen in new[]{BilliardsScreen.Join,BilliardsScreen.Disconnect}){
    var page=pages[(int)screen];page.transform.Find("reassign2 Button").gameObject.SetActive(session.mode==BilliardsMode.SeparateControllers);
    page.transform.Find("reassign1 Button").GetComponent<ButtonManager>().SetText(session.mode==BilliardsMode.HotSeat?"REASSIGN SHARED CONTROLLER":"REASSIGN PLAYER 1");
   }
   joinInfo.text=text;
   bool ready=session.mode==BilliardsMode.SeparateControllers?session.BothPadsReady:session.Ready(0);
   SetInteractable(joinStart,ready);SetInteractable(disconnectResume,ready);
   if(ready&&!lastJoinReady&&EventSystem.current!=null&&(session.Screen==BilliardsScreen.Join||session.Screen==BilliardsScreen.Disconnect))EventSystem.current.SetSelectedGameObject(session.Screen==BilliardsScreen.Join?joinStart.gameObject:disconnectResume.gameObject);
   lastJoinReady=ready;
   var disconnectPage=pages[(int)BilliardsScreen.Disconnect];var details=disconnectPage.transform.Find("Body").GetComponent<TMP_Text>();details.text=text+"\nReconnect the same device or explicitly reassign, then Resume.";Rewire();
  }
  void SetInteractable(ButtonManager button,bool value){button.isInteractable=value;var selectable=button.GetComponent<UnityEngine.UI.Button>();selectable.interactable=value;}
  public void RefreshSettings()
  {
   masterSlider.SetValueWithoutNotify(session.settings.master*100);sfxSlider.SetValueWithoutNotify(session.settings.sfx*100);sensitivitySlider.SetValueWithoutNotify(session.settings.sensitivity);
   foreach(var slider in new[]{masterSlider,sfxSlider,sensitivitySlider})slider.GetComponent<SliderManager>().UpdateUI();
   fullscreenText.text=session.settings.fullscreen?"Display: FULLSCREEN":"Display: WINDOWED";
   vibrationText.text="Vibration: "+(session.settings.vibration?"ON":"OFF");
   var r=session.settings.resolutions[session.settings.resolutionIndex];resolutionText.text="Resolution: "+r.x+" × "+r.y;
  }
  void Rewire(){
   var page=pages[(int)session.Screen];if(page==null)return;
   var list=new System.Collections.Generic.List<UnityEngine.UI.Selectable>();
   foreach(var item in page.GetComponentsInChildren<UnityEngine.UI.Selectable>())if(item.IsInteractable())list.Add(item);
   for(int i=0;i<list.Count;i++){
    var nav=new UnityEngine.UI.Navigation{mode=UnityEngine.UI.Navigation.Mode.Explicit,selectOnUp=list[(i+list.Count-1)%list.Count],selectOnDown=list[(i+1)%list.Count]};
    if(!(list[i] is UnityEngine.UI.Slider)){nav.selectOnLeft=nav.selectOnUp;nav.selectOnRight=nav.selectOnDown;}list[i].navigation=nav;
   }
  }
  void Update(){
   if(session.Screen==BilliardsScreen.Match||EventSystem.current==null)return;
   var selected=EventSystem.current.currentSelectedGameObject;
   var field=selected==null?null:selected.GetComponent<TMP_InputField>();
   if(Keyboard.current!=null&&Keyboard.current.tabKey.wasPressedThisFrame&&selected!=null){
    if(field!=null)field.DeactivateInputField();
    var item=selected.GetComponent<UnityEngine.UI.Selectable>();
    var next=Keyboard.current.shiftKey.isPressed?item.navigation.selectOnUp:item.navigation.selectOnDown;
    if(next!=null)EventSystem.current.SetSelectedGameObject(next.gameObject);
   }
   foreach(var pad in Gamepad.all)if(pad.buttonEast.wasPressedThisFrame){
    if(field!=null&&field.isFocused){field.DeactivateInputField();return;}Back();return;
   }
  }
  public void Back()
  {
   switch(session.Screen){
    case BilliardsScreen.Help:case BilliardsScreen.Settings:session.Show(returnScreen);break;
    case BilliardsScreen.Setup:if(LWS.TruckSports.SportsNavigation.Collection)LWS.TruckSports.SportsNavigation.FrontEnd();else session.Show(BilliardsScreen.Main);break;
    case BilliardsScreen.Join:session.Show(BilliardsScreen.Setup);break;
    case BilliardsScreen.Pause:session.Resume();break;
    case BilliardsScreen.Confirm:session.Show(BilliardsScreen.Pause);break;
    case BilliardsScreen.Disconnect:RequestConfirm("main");break;
   }
  }
  public void RequestConfirm(string what){confirmation=what;confirmText.text=what=="restart"?"RESTART THIS RACK?\nEarlier scores and the current breaker are preserved.":what=="restartseries"?"RESTART THIS SERIES?\nClear scores and restore the original opening player.":what=="practicereset"?"RESET PRACTICE RACK?\nRestore all balls, cue and truck.":"RETURN TO MAIN MENU?\nThe current match will be discarded.";session.Show(BilliardsScreen.Confirm);}
  public void Action(string key)
  {
   if(Debug.isDebugBuild)Debug.Log("TB_MENU_ACTION "+key+" screen="+session.Screen);
   if(session.match.impact!=null)session.match.audioSource.PlayOneShot(session.match.impact,.25f);
   switch(key){
    case "practice":session.SelectMode(BilliardsMode.Practice);session.Show(BilliardsScreen.Setup);break;
    case "play":if(session.IsPractice)session.SelectMode(BilliardsMode.SinglePlayer);session.Show(BilliardsScreen.Setup);break;
    case "help":returnScreen=session.Screen;session.Show(BilliardsScreen.Help);break;
    case "settings":returnScreen=session.Screen;session.Show(BilliardsScreen.Settings);break;
    case "back":Back();break;
    case "quit":Application.Quit();break;
    case "mode":session.SelectMode((BilliardsMode)(((int)session.mode+1)%4));break;
    case "break":session.breakChoice=(session.breakChoice+1)%3;RefreshSetup();break;
    case "color1":do{color1=(color1+1)%Palette.Length;}while(color1==color2);session.players[0].color=Palette[color1];RefreshSetup();break;
    case "color2":do{color2=(color2+1)%Palette.Length;}while(color2==color1);session.players[1].color=Palette[color2];RefreshSetup();break;
    case "hotdevice":session.hotSeatGamepad=!session.hotSeatGamepad;if(session.IsPractice)session.players[0].control=session.hotSeatGamepad?BilliardsControl.Gamepad:BilliardsControl.KeyboardWASD;RefreshSetup();break;
    case "start":if(session.mode==BilliardsMode.SeparateControllers||session.OnePadMode)session.Show(BilliardsScreen.Join);else session.StartMatch();break;
    case "joinedstart":session.StartMatch();break;
    case "reassign1":session.Reassign(0);break;
    case "reassign2":session.Reassign(1);break;
    case "resume":session.Resume();break;
        case "restart":RequestConfirm("restart");break;
    case "restartseries":RequestConfirm("restartseries");break;
    case "practicereset":RequestConfirm("practicereset");break;
    case "reposition":session.RepositionCue();break;
    case "nextrack":session.NextRack();break;
    case "format":session.format=(BilliardsFormat)(((int)session.format+1)%3);RefreshSetup();break;
    case "difficulty":session.difficulty=(BilliardsDifficulty)(((int)session.difficulty+1)%3);PlayerPrefs.SetInt("TruckBilliards.Settings.Difficulty",(int)session.difficulty);PlayerPrefs.Save();RefreshSetup();break;
    case "vibration":session.settings.vibration=!session.settings.vibration;PlayerPrefs.SetInt("TruckBilliards.Settings.Vibration",session.settings.vibration?1:0);PlayerPrefs.Save();if(!session.settings.vibration)session.feedback.StopMotors();RefreshSettings();break;
    case "main":RequestConfirm("main");break;
    case "yes":if(confirmation=="restart"||confirmation=="practicereset")session.RestartRack();else if(confirmation=="restartseries")session.RestartSeries();else session.MainMenu();break;
    case "ready":
     bool fresh= session.hotSeatGamepad?session.SharedPad!=null&&session.input.SouthPressed(session.SharedPad):
       Keyboard.current!=null&&(Keyboard.current.enterKey.wasPressedThisFrame||Keyboard.current.spaceKey.wasPressedThisFrame)||Mouse.current!=null&&Mouse.current.leftButton.wasPressedThisFrame;
     if(session.input.HandoffReady&&(fresh||Mouse.current!=null&&Mouse.current.leftButton.wasReleasedThisFrame))session.ConfirmHandoff();break;
    case "rematch":session.Rematch();break;
    case "resultmain":session.MainMenu();break;
    case "fullscreen":session.settings.fullscreen=!session.settings.fullscreen;RefreshSettings();break;
    case "resolution":session.settings.resolutionIndex=(session.settings.resolutionIndex+1)%session.settings.resolutions.Length;RefreshSettings();break;
    case "save":session.settings.Save();session.Show(returnScreen);break;
   }
  }
 }
 static class BilliardsSelectableExtensions
 {
  public static UnityEngine.UI.Selectable FindFirstInteractable(this UnityEngine.UI.Selectable[] items){foreach(var item in items)if(item.IsActive()&&item.IsInteractable())return item;return null;}
 }
}











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
  public TMP_Text formatText,difficultyText,vibrationText,control0Text,control1Text,difficulty0Text,groupChoiceInfo;public ButtonManager nextRackButton,rematchButton;
  public BilliardsScreen returnScreen=BilliardsScreen.Main;string confirmation;bool lastJoinReady;int joinSignature=int.MinValue;UnityEngine.UI.Selectable[][] selections;BilliardsMenuAction[][] pageActions;readonly System.Collections.Generic.List<UnityEngine.UI.Selectable> navigation=new System.Collections.Generic.List<UnityEngine.UI.Selectable>();
  public readonly Color[] Palette={new Color(.15f,.8f,1),new Color(1,.55f,.13f),new Color(.85f,.25f,.7f),new Color(.6f,1,.3f),new Color(.9f,.85f,.25f)};
  int color1,color2=1;readonly ButtonManager[,] reassignButtons=new ButtonManager[2,2];TMP_Text disconnectDetails;
  public const string RulesText="Drive your miniature truck into the WHITE cue ball with its front bumper. Run up for power; tap gently for position.\n\nOnly the current player can strike, once per shot. Both trucks are ghosts to moving balls and object balls.\n\nA legal shot potting one group assigns it automatically. Pot BOTH groups on an open table and the shooter chooses after settling. Legal own-ball pots keep your turn. Wrong first contact or a miss passes the turn where the cue stops.\n\nOnly a cue-ball SCRATCH allows placement. Clear your group BEFORE the shot to win with the 8. Early/illegal 8 loses. Break 8 is respotted.\n\nHot seat: pass the device only when the turn changes after settling.";
  void Start()
  {
   selections=new UnityEngine.UI.Selectable[pages.Length][];pageActions=new BilliardsMenuAction[pages.Length][];for(int i=0;i<pages.Length;i++)if(pages[i]!=null){selections[i]=pages[i].GetComponentsInChildren<UnityEngine.UI.Selectable>(true);pageActions[i]=pages[i].GetComponentsInChildren<BilliardsMenuAction>(true);}
   for(int j=0;j<2;j++){int page=j==0?(int)BilliardsScreen.Join:(int)BilliardsScreen.Disconnect;foreach(var a in pageActions[page]){if(a.action=="reassign1")reassignButtons[j,0]=a.button;if(a.action=="reassign2")reassignButtons[j,1]=a.button;}}disconnectDetails=pages[(int)BilliardsScreen.Disconnect].transform.Find("Body").GetComponent<TMP_Text>();foreach(var item in GetComponentsInChildren<BilliardsMenuAction>(true)){var key=item.action;item.button.onClick.AddListener(()=>Action(key));}
   name1.onEndEdit.AddListener(v=>{session.players[0].name=string.IsNullOrWhiteSpace(v)?"Player 1":v;session.SaveSetup();RefreshSetup();});
   name2.onEndEdit.AddListener(v=>{session.players[1].name=string.IsNullOrWhiteSpace(v)?"Player 2":v;session.SaveSetup();RefreshSetup();});
   masterSlider.onValueChanged.AddListener(v=>{session.settings.master=v/100;session.settings.ApplyAudio();});
   sfxSlider.onValueChanged.AddListener(v=>{session.settings.sfx=v/100;session.settings.ApplyAudio();});
   sensitivitySlider.onValueChanged.AddListener(v=>session.settings.sensitivity=v);
   session.menus=this;Show(session.Screen);
  }
  public void Show(BilliardsScreen screen)
  {
   for(int i=0;i<pages.Length;i++)if(pages[i]!=null)pages[i].SetActive(i==(int)screen);
   if(screen==BilliardsScreen.Setup)RefreshSetup();
   if(screen==BilliardsScreen.Join||screen==BilliardsScreen.Disconnect){joinSignature=int.MinValue;RefreshJoin();}
   if(screen==BilliardsScreen.GroupSelection)groupChoiceInfo.text=session.Name(session.match.rules.Turn)+" pocketed BOTH groups on a legal shot.\nThe balls remain pocketed. Choose your group; your turn is retained.\n\nSOLIDS remaining: "+session.match.rules.RemainingGroup(1)+"    •    STRIPES remaining: "+session.match.rules.RemainingGroup(2)+"\n\nActive keyboard: F / G (WASD) or Right Ctrl / Right Shift (arrows).\nAssigned controller: select a group, then South. Mouse: click your choice.";
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
    foreach(var action in pageActions[(int)screen]){
     if(action.action=="restart"||action.action=="restartseries")action.gameObject.SetActive(!session.IsPractice);
     if(action.action=="practicereset"||action.action=="reposition"){action.gameObject.SetActive(session.IsPractice);if(action.action=="reposition")SetInteractable(action.button,session.match.AreSettled&&session.match.rules.Phase==MatchPhase.Aiming);}
    }
   }
   Rewire();
   if(EventSystem.current!=null){
    EventSystem.current.SetSelectedGameObject(null);
    if(screen!=BilliardsScreen.Match&&pages[(int)screen]!=null){
     var first=pages[(int)screen].GetComponentsInChildren<UnityEngine.UI.Selectable>().FindFirstInteractable();
          if(screen==BilliardsScreen.Join)first=(session.DevicesAvailable())?joinStart.GetComponent<UnityEngine.UI.Button>():pages[(int)screen].transform.Find("back Button").GetComponent<UnityEngine.UI.Button>();
     if(screen==BilliardsScreen.Disconnect&&(session.DevicesAvailable()))first=disconnectResume.GetComponent<UnityEngine.UI.Button>();
     if(first!=null)EventSystem.current.SetSelectedGameObject(first.gameObject);
    }
   }
  }
  public void RefreshSetup(){
   modeText.text=session.mode==BilliardsMode.SinglePlayer?"HUMAN vs AI":session.mode==BilliardsMode.AIVsAI?"AI vs AI • SPECTATE":session.mode==BilliardsMode.Custom?"CUSTOM PARTICIPANTS":session.mode==BilliardsMode.HotSeat?"HOT SEAT":session.mode==BilliardsMode.SeparateControllers?"SEPARATE CONTROLLERS":session.IsPractice?"PRACTICE":"SHARED KEYBOARD";
   name1.SetTextWithoutNotify(session.players[0].name);name2.SetTextWithoutNotify(session.players[1].name);name2.interactable=true;
   control0Text.text="Participant: "+session.players[0].control;control1Text.text="Participant: "+session.players[1].control;
   difficulty0Text.text="AI: "+session.DifficultyFor(0).ToString().ToUpper();difficultyText.text="AI: "+session.DifficultyFor(1).ToString().ToUpper();difficulty0Text.transform.parent.gameObject.SetActive(session.IsAI(0));difficultyText.transform.parent.gameObject.SetActive(session.IsAI(1));
   color1=PaletteIndex(session.players[0].color);color2=PaletteIndex(session.players[1].color);color1Text.text="Truck color: "+ColorName(color1);color2Text.text="Truck color: "+ColorName(color2);color1Text.color=session.players[0].color;color2Text.color=session.players[1].color;
   breakText.text="Opening: "+(session.breakChoice==2?"RANDOM":session.Name(session.breakChoice));formatText.text="Length: "+session.format;
   hotDeviceText.text=(session.IsPractice?"Practice device: ":"Hot-seat device: ")+(session.hotSeatGamepad?"ONE CONTROLLER":"KEYBOARD + MOUSE");hotDeviceText.transform.parent.gameObject.SetActive(session.mode==BilliardsMode.HotSeat||session.IsPractice);
   modeText.transform.parent.gameObject.SetActive(!session.IsPractice);name2.gameObject.SetActive(!session.IsPractice);name2.transform.parent.Find("PLAYER 2 NAME").gameObject.SetActive(!session.IsPractice);color2Text.transform.parent.gameObject.SetActive(!session.IsPractice);control0Text.transform.parent.gameObject.SetActive(!session.IsPractice&&session.mode!=BilliardsMode.HotSeat);control1Text.transform.parent.gameObject.SetActive(!session.IsPractice&&session.mode!=BilliardsMode.HotSeat);breakText.transform.parent.gameObject.SetActive(!session.IsPractice);formatText.transform.parent.gameObject.SetActive(!session.IsPractice);
   setupInfo.text=session.Message+"\n"+session.Name(0)+": "+session.Bindings(0)+(session.IsPractice?"":"\n"+session.Name(1)+": "+session.Bindings(1));joinSignature=int.MinValue;Rewire();
  }
  static readonly string[] ColorNames={"BLUE","ORANGE","MAGENTA","LIME","GOLD"};string ColorName(int i)=>ColorNames[i];int PaletteIndex(Color c){int best=0;float distance=float.PositiveInfinity;for(int i=0;i<Palette.Length;i++){float d=((Vector4)c-(Vector4)Palette[i]).sqrMagnitude;if(d<distance){distance=d;best=i;}}return best;}
  public void RefreshJoin()
  {
   if(pageActions==null)return;int signature=session.Message.GetHashCode();for(int i=0;i<2;i++)signature=unchecked(signature*31+(session.players[i].pad?.deviceId??0)+(session.Ready(i)?1024:0)+(int)session.players[i].control*17);signature=unchecked(signature*31+(int)session.mode);if(signature==joinSignature)return;joinSignature=signature;
   string Description(int side)=>session.IsAI(side)?"AI":session.players[side].control!=BilliardsControl.Gamepad?session.players[side].control.ToString():session.Ready(side)?session.players[side].pad.displayName+" • ID "+session.players[side].pad.deviceId:"NOT JOINED / DISCONNECTED";
   string text=session.Message+"\nPress NORTH / Y / TRIANGLE on each controller to join.\n\n"+session.Name(0)+": "+Description(0);
   if(!session.IsPractice&&session.players[1].control==BilliardsControl.Gamepad)text+="\n"+session.Name(1)+": "+Description(1);
   text+="\n\nNorth joins the first empty slot. Use Reassign to replace a slot.\nOne controller cannot occupy both slots in Separate Controllers.";
   for(int j=0;j<2;j++){reassignButtons[j,0].gameObject.SetActive(session.players[0].control==BilliardsControl.Gamepad);reassignButtons[j,1].gameObject.SetActive(!session.IsPractice&&session.players[1].control==BilliardsControl.Gamepad&&session.mode!=BilliardsMode.HotSeat);reassignButtons[j,0].SetText(session.mode==BilliardsMode.HotSeat?"REASSIGN SHARED CONTROLLER":"REASSIGN PLAYER 1");}
   joinInfo.text=text;
   bool ready=session.DevicesAvailable();
   SetInteractable(joinStart,ready);SetInteractable(disconnectResume,ready);
   if(ready&&!lastJoinReady&&EventSystem.current!=null&&(session.Screen==BilliardsScreen.Join||session.Screen==BilliardsScreen.Disconnect))EventSystem.current.SetSelectedGameObject(session.Screen==BilliardsScreen.Join?joinStart.gameObject:disconnectResume.gameObject);
   lastJoinReady=ready;
   disconnectDetails.text=text+"\nReconnect the same device or explicitly reassign, then Resume.";Rewire();
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
  void Rewire(){if(selections==null||selections[(int)session.Screen]==null)return;navigation.Clear();foreach(var item in selections[(int)session.Screen])if(item.IsActive()&&item.IsInteractable())navigation.Add(item);for(int i=0;i<navigation.Count;i++){var item=navigation[i];var up=navigation[(i+navigation.Count-1)%navigation.Count];var down=navigation[(i+1)%navigation.Count];var nav=new UnityEngine.UI.Navigation{mode=UnityEngine.UI.Navigation.Mode.Explicit,selectOnUp=up,selectOnDown=down};if(!(item is UnityEngine.UI.Slider)){nav.selectOnLeft=up;nav.selectOnRight=down;}item.navigation=nav;var heat=item.GetComponent<ButtonManager>();if(heat!=null){heat.navigationMode=nav.mode;heat.selectOnUp=up.gameObject;heat.selectOnDown=down.gameObject;heat.selectOnLeft=nav.selectOnLeft?.gameObject;heat.selectOnRight=nav.selectOnRight?.gameObject;}}}
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
    case BilliardsScreen.Setup:session.SaveSetup();if(LWS.TruckSports.SportsNavigation.Collection)LWS.TruckSports.SportsNavigation.FrontEnd();else session.Show(BilliardsScreen.Main);break;
    case BilliardsScreen.Join:session.Show(BilliardsScreen.Setup);break;
    case BilliardsScreen.Pause:session.Resume();break;
    case BilliardsScreen.Confirm:session.Show(BilliardsScreen.Pause);break;
    case BilliardsScreen.Disconnect:RequestConfirm("main");break;
   }
  }
  public void RequestConfirm(string what){confirmation=what;confirmText.text=what=="restart"?"RESTART THIS RACK?\nEarlier scores and the current breaker are preserved.":what=="restartseries"?"RESTART THIS SERIES?\nClear scores and restore the original opening player.":what=="practicereset"?"RESET PRACTICE RACK?\nRestore all balls, cue and truck.":"RETURN TO MAIN MENU?\nThe current match will be discarded.";session.Show(BilliardsScreen.Confirm);}
  public void Action(string key)
  {
   if(session.Screen==BilliardsScreen.Disconnect&&(key=="resume"||key=="reassign1"||key=="reassign2")&&!session.input.DisconnectConfirm())return;
   if(Debug.isDebugBuild)Debug.Log("TB_MENU_ACTION "+key+" screen="+session.Screen);
   if(session.match.impact!=null)session.match.audioSource.PlayOneShot(session.match.impact,.25f);
   switch(key){
    case "practice":session.SelectMode(BilliardsMode.Practice);session.Show(BilliardsScreen.Setup);break;
    case "play":if(session.IsPractice)session.SelectMode(BilliardsMode.SinglePlayer);session.Show(BilliardsScreen.Setup);break;
    case "help":returnScreen=session.Screen;session.Show(BilliardsScreen.Help);break;
    case "settings":returnScreen=session.Screen;session.Show(BilliardsScreen.Settings);break;
    case "back":Back();break;
    case "quit":Application.Quit();break;
    case "mode":var options=new[]{BilliardsMode.SinglePlayer,BilliardsMode.SharedKeyboard,BilliardsMode.SeparateControllers,BilliardsMode.HotSeat,BilliardsMode.AIVsAI,BilliardsMode.Custom};int index=Array.IndexOf(options,session.mode);session.SelectMode(options[(index+1)%options.Length]);session.SaveSetup();break;
    case "participant0":session.CycleParticipant(0);break;case "participant1":session.CycleParticipant(1);break;case "difficulty0":session.players[0].difficulty=(BilliardsDifficulty)(((int)session.players[0].difficulty+1)%3);session.SaveSetup();RefreshSetup();break;
    case "solids":case "stripes":if(session.Screen==BilliardsScreen.GroupSelection&&session.input.GroupConfirm())session.ChooseGroup(session.match.rules.Turn,key=="solids"?1:2);break;
    case "break":session.breakChoice=(session.breakChoice+1)%3;RefreshSetup();break;
    case "color1":do{color1=(color1+1)%Palette.Length;}while(color1==color2);session.players[0].color=Palette[color1];RefreshSetup();break;
    case "color2":do{color2=(color2+1)%Palette.Length;}while(color2==color1);session.players[1].color=Palette[color2];RefreshSetup();break;
    case "hotdevice":session.hotSeatGamepad=!session.hotSeatGamepad;if(session.IsPractice)session.players[0].control=session.hotSeatGamepad?BilliardsControl.Gamepad:BilliardsControl.KeyboardWASD;RefreshSetup();break;
    case "start":session.SaveSetup();if(!session.DevicesAvailable())session.Show(BilliardsScreen.Join);else session.StartMatch();break;
    case "joinedstart":session.StartMatch();break;
    case "reassign1":session.Reassign(0);break;
    case "reassign2":session.Reassign(1);break;
    case "resume":session.Resume();break;case "pause":session.Pause();break;
        case "restart":RequestConfirm("restart");break;
    case "restartseries":RequestConfirm("restartseries");break;
    case "practicereset":RequestConfirm("practicereset");break;
    case "reposition":session.RepositionCue();break;
    case "nextrack":session.NextRack();break;
    case "format":session.format=(BilliardsFormat)(((int)session.format+1)%3);RefreshSetup();break;
    case "difficulty":session.difficulty=(BilliardsDifficulty)(((int)session.DifficultyFor(1)+1)%3);session.players[1].difficulty=session.difficulty;session.SaveSetup();PlayerPrefs.SetInt("TruckBilliards.Settings.Difficulty",(int)session.difficulty);PlayerPrefs.Save();RefreshSetup();break;
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











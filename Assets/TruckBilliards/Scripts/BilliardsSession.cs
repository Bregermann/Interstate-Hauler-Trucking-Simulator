using UnityEngine;
using UnityEngine.InputSystem;
namespace LWS.TruckBilliards
{
 public enum BilliardsMode { SinglePlayer, SharedKeyboard, SeparateControllers, HotSeat, Practice }
 public enum BilliardsControl { KeyboardWASD, KeyboardArrows, Gamepad, AI }
 public enum BilliardsScreen { Main, Setup, Join, Match, Help, Settings, Pause, Confirm, Handoff, Disconnect, Result }
 [System.Serializable]
 public sealed class BilliardsPlayerSlot
 {
  public string name="Player"; public Color color=Color.cyan; public BilliardsTruck truck;
  public int Group=>truck!=null&&truck.match!=null?truck.match.rules.Group(truck.side):0;
  public BilliardsControl control; [System.NonSerialized] public Gamepad pad;
  [System.NonSerialized] public bool released;
 }
 public sealed class BilliardsSession:MonoBehaviour
 {
  public BilliardsMatch match; public BilliardsInputRouter input; public BilliardsMenus menus; public BilliardsSettings settings;public BilliardsHUD hud;
  public BilliardsPlayerSlot[] players=new BilliardsPlayerSlot[2];
  public BilliardsMode mode; public int breakChoice,openingBreaker,sharedOwner;
  public bool HandoffPending{get;private set;}
  public BilliardsFormat format;public BilliardsDifficulty difficulty=BilliardsDifficulty.Normal;
  public readonly BilliardsSeries series=new BilliardsSeries();public BilliardsFeedback feedback;public BilliardsCelebration celebration;
  public bool IsPractice=>mode==BilliardsMode.Practice;public bool OnePadMode=>(mode==BilliardsMode.HotSeat||IsPractice)&&hotSeatGamepad;
  public bool hotSeatGamepad; public bool InMatch{get;private set;}
  public BilliardsScreen Screen{get;private set;}=BilliardsScreen.Main;
  public BilliardsScreen BackScreen{get;private set;}=BilliardsScreen.Main;
  public string Message{get;private set;}="";
  public bool GameplayEnabled=>InMatch&&Screen==BilliardsScreen.Match;
  public bool IsAI(int side)=>side==1&&mode==BilliardsMode.SinglePlayer;
  public bool BothPadsReady=>Ready(0)&&Ready(1)&&players[0].pad!=players[1].pad;
  public bool Ready(int side)=>players[side].pad!=null&&players[side].pad.added&&players[side].pad.enabled;
  public Gamepad SharedPad=>players[0].pad;
  void Awake(){players[0].truck=match.player;players[1].truck=match.ai;match.session=this;input.session=this;settings.session=this;}
  void Start(){settings.Load();SelectMode(mode);match.opponent.enabled=false;Show(LWS.TruckSports.SportsNavigation.Collection?BilliardsScreen.Setup:BilliardsScreen.Main);}
  public void Show(BilliardsScreen screen)
  {
   if(feedback!=null){feedback.StopMotors();if(screen!=BilliardsScreen.Match)feedback.StopAudio();}if(celebration!=null&&screen!=BilliardsScreen.Result)celebration.Clear();
   Screen=screen;Time.timeScale=GameplayEnabled?1:0;input.GateAll();
   if(menus!=null)menus.Show(screen);if(hud!=null)hud.Refresh();
  }
  public void SelectMode(BilliardsMode value)
  {
   mode=value;players[0].control=BilliardsControl.KeyboardWASD;
   players[1].control=value==BilliardsMode.SinglePlayer?BilliardsControl.AI:BilliardsControl.KeyboardArrows;
   if(value==BilliardsMode.SeparateControllers)players[0].control=players[1].control=BilliardsControl.Gamepad;
   if(value==BilliardsMode.HotSeat)players[1].control=BilliardsControl.KeyboardWASD;
   if(value==BilliardsMode.Practice)players[0].control=hotSeatGamepad?BilliardsControl.Gamepad:BilliardsControl.KeyboardWASD;
   if(value==BilliardsMode.SeparateControllers&&players[0].pad==players[1].pad)players[1].pad=null;Message="";if(menus!=null)menus.RefreshSetup();
  }
  public bool StartMatch()
  {
   if(mode!=BilliardsMode.SeparateControllers&&!OnePadMode&&Keyboard.current==null){Message="This mode requires a keyboard.";Show(BilliardsScreen.Setup);return false;}
   if(mode==BilliardsMode.SeparateControllers&&!BothPadsReady){Message="Join two distinct controllers before starting.";Show(BilliardsScreen.Join);return false;}
   if(OnePadMode&&!Ready(0)){Message="Join one controller for this mode.";Show(BilliardsScreen.Join);return false;}
   if(mode==BilliardsMode.HotSeat){players[0].control=players[1].control=hotSeatGamepad?BilliardsControl.Gamepad:BilliardsControl.KeyboardWASD;players[1].pad=hotSeatGamepad?players[0].pad:null;}
   if(IsPractice){players[0].control=hotSeatGamepad?BilliardsControl.Gamepad:BilliardsControl.KeyboardWASD;players[1].control=BilliardsControl.KeyboardArrows;players[1].pad=null;}
   HandoffPending=false;openingBreaker=IsPractice?0:breakChoice==2?Random.Range(0,2):breakChoice;series.Start(IsPractice?BilliardsFormat.SingleRack:format,openingBreaker);InMatch=true;
   match.FreshMatch(openingBreaker);sharedOwner=openingBreaker;ApplyIdentity();Show(BilliardsScreen.Match);return true;
  }
  public void Rematch(){if(!series.Finished||!rulesEnded)return;if(!DevicesAvailable()){Disconnect();return;}series.Start(format,1-series.OriginalBreaker);BeginRack(true);}
  public void Restart(){RestartRack();}
  public void RestartRack(){if(!IsPractice&&(rulesEnded||series.RackFinished))return;BeginRack(true);}
  bool rulesEnded=>match.rules.Phase==MatchPhase.Ended;
  public void RestartSeries(){series.Start(format,series.OriginalBreaker);BeginRack(true);}
  public void NextRack(){if(!DevicesAvailable()){Disconnect();return;}if(series.Next())BeginRack(true);}
  bool DevicesAvailable()=>mode==BilliardsMode.SeparateControllers?BothPadsReady:OnePadMode?Ready(0):Keyboard.current!=null;
  void BeginRack(bool readiness){
   if(IsPractice){players[0].control=hotSeatGamepad?BilliardsControl.Gamepad:BilliardsControl.KeyboardWASD;players[1].control=BilliardsControl.KeyboardArrows;players[1].pad=null;}
   HandoffPending=false;openingBreaker=IsPractice?0:series.Breaker;
   match.FreshMatch(openingBreaker);ApplyIdentity();
   if(readiness&&mode==BilliardsMode.HotSeat&&sharedOwner!=openingBreaker){HandoffPending=true;Show(BilliardsScreen.Handoff);}
   else{sharedOwner=openingBreaker;Show(BilliardsScreen.Match);}
  }
  public void RepositionCue(){if(!IsPractice||!match.AreSettled||!match.rules.PracticePlacement())return;Show(BilliardsScreen.Match);input.BeginPlacement();}
  public string ScoreText=>IsPractice?"PRACTICE • ONE TRUCK • NO OPPONENT":Name(0)+"  "+series.Wins[0]+" : "+series.Wins[1]+"  "+Name(1)+"   •   RACK "+series.RackNumber+"   •   FIRST TO "+series.Required+(mode==BilliardsMode.SinglePlayer?"   •   AI "+difficulty.ToString().ToUpper():"");
  public void MainMenu(){if(LWS.TruckSports.SportsNavigation.Collection){if(feedback!=null)feedback.StopAudio();LWS.TruckSports.SportsNavigation.FrontEnd();return;}HandoffPending=false;InMatch=false;match.opponent.enabled=false;match.opponent.ResetPlan();match.HidePlacement();match.player.Stop();match.ai.Stop();match.FreshMatch(0);match.ai.gameObject.SetActive(true);Show(BilliardsScreen.Main);}
  public void ApplyIdentity()
  {
   for(int i=0;i<2;i++){
    players[i].name=string.IsNullOrWhiteSpace(players[i].name)?"Player "+(i+1):players[i].name.Trim();
    var label=players[i].truck.transform.Find("Truck label");if(label!=null){var text=label.GetComponent<TextMesh>();text.text=Name(i);text.color=players[i].color;}
    foreach(var renderer in players[i].truck.GetComponentsInChildren<MeshRenderer>()){
     if(renderer.name=="Chassis")foreach(var material in renderer.materials)material.color=players[i].color;
    }
    var bumper=players[i].truck.transform.Find("Colored front bumper");if(bumper!=null)bumper.GetComponent<Renderer>().material.color=players[i].color;
   }
   match.ai.gameObject.SetActive(!IsPractice);match.opponent.enabled=IsAI(1);match.opponent.ResetPlan();
  }
  public string Name(int side)=>IsAI(side)?"AI":players[side].name;
  public void ShotResolved(int previousTurn)
  {
   if(match.rules.Phase==MatchPhase.Ended){if(series.Award(match.rules.Winner)){Show(BilliardsScreen.Result);if(feedback!=null){feedback.Cue(series.Finished&&series.Required>1?BilliardsSound.SeriesWin:BilliardsSound.RackWin);feedback.Pulse(match.rules.Winner,series.Finished?.38f:.23f,.3f);}if(celebration!=null)celebration.Play(players[match.rules.Winner].color,series.Finished&&series.Required>1);}return;}

   if(mode==BilliardsMode.HotSeat&&previousTurn!=match.rules.Turn){HandoffPending=true;Message="PASS TO "+Name(match.rules.Turn);Show(BilliardsScreen.Handoff);}
   else if(match.rules.Phase==MatchPhase.Placement)input.BeginPlacement();
   if(feedback!=null){if(match.rules.LastFoul)feedback.Cue(BilliardsSound.Foul);else if(previousTurn!=match.rules.Turn)feedback.Cue(BilliardsSound.Turn);if(match.rules.LastScored&&!match.rules.LastFoul)feedback.Pulse(previousTurn,.16f,.14f);}
  }
  public void ConfirmHandoff()
  {
   if(Screen!=BilliardsScreen.Handoff)return;HandoffPending=false;sharedOwner=match.rules.Turn;
   Show(BilliardsScreen.Match);if(match.rules.Phase==MatchPhase.Placement)input.BeginPlacement();
  }
  public void Pause()
  {
   if(!InMatch||Screen==BilliardsScreen.Pause||Screen==BilliardsScreen.Disconnect||Screen==BilliardsScreen.Result)return;
   BackScreen=Screen;Show(BilliardsScreen.Pause);
  }
  public void Resume()
  {
   if(mode!=BilliardsMode.SeparateControllers&&!OnePadMode&&Keyboard.current==null){Message="This mode requires a keyboard.";Show(BilliardsScreen.Pause);return;}
   if(mode==BilliardsMode.SeparateControllers&&!BothPadsReady){Disconnect();return;}
   if(OnePadMode&&!Ready(0)){Disconnect();return;}
   var target=!IsPractice&&series.RackFinished?BilliardsScreen.Result:HandoffPending?BilliardsScreen.Handoff:BilliardsScreen.Match;Show(target);
  }
  public void Disconnect()
  {
   if(Screen!=BilliardsScreen.Disconnect)BackScreen=Screen==BilliardsScreen.Pause?BackScreen:Screen;
   Message=OnePadMode?"Shared controller disconnected.":string.Join(" / ",LostNames())+" disconnected.";
   Show(BilliardsScreen.Disconnect);
  }
  System.Collections.Generic.IEnumerable<string> LostNames(){for(int i=0;i<2;i++)if(!Ready(i))yield return Name(i);}
  public bool Assign(int side,Gamepad pad)
  {
   if(mode==BilliardsMode.HotSeat||IsPractice)side=0;
   if(pad==null||!pad.added)return false;
   if(mode==BilliardsMode.SeparateControllers&&players[1-side].pad==pad)return false;
   players[side].pad=pad;if(mode==BilliardsMode.HotSeat)players[1].pad=pad;
   input.GateAll();if(menus!=null)menus.RefreshJoin();return true;
  }
  public void Reassign(int side){if(mode==BilliardsMode.HotSeat||IsPractice)side=0;players[side].pad=null;if(mode==BilliardsMode.HotSeat)players[1].pad=null;if(menus!=null)menus.RefreshJoin();}
  public string Bindings(int side,bool placement=false)
  {
   var p=players[side];bool controller=p.control==BilliardsControl.Gamepad||(mode==BilliardsMode.HotSeat&&hotSeatGamepad);
   if(IsAI(side))return "AI drives and places automatically.";
   if(controller)return placement?"Left stick: place • South/A/Cross: confirm":"Left stick: steer • RT: accelerate • LT: reverse\nSouth/A/Cross: brake • North/Y/Triangle: reset • Start/Menu: pause";
   if(mode==BilliardsMode.SinglePlayer&&!placement)return "WASD or arrows: drive • Space: brake • R: reset • Esc: pause";
   if(mode==BilliardsMode.HotSeat||IsPractice||side==0)return placement?"WASD or mouse: place • F / left click: confirm":"WASD: drive • Space: brake • R: reset • Esc: pause";
   return placement?"Arrow keys: place • Right Ctrl: confirm":"Arrow keys: drive • Right Shift: brake • Enter: reset • Esc: pause";
  }
  void OnDestroy(){Time.timeScale=1;}
 }
}















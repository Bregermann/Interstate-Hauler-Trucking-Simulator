using System;
using UnityEngine;
using UnityEngine.InputSystem;
using LWS.TruckBowling;
namespace LWS.TruckSports {
// Shared local-event lifecycle and source routing. Each sport retains its own physics/rules owner.
[DefaultExecutionOrder(-100)]public abstract class SportsEventSession:MonoBehaviour,ISportsScorePresentationSource {
 bool ISportsScorePresentationSource.ScoreRunning=>Running;
 bool ISportsScorePresentationSource.ScorePaused=>Paused;
 bool ISportsScorePresentationSource.ScoreFinished=>Finished;
 bool ISportsScorePresentationSource.ScoreResultPage=>menus.Screen==SportsEventScreen.Result;
 bool ISportsScorePresentationSource.ScorePlayPage=>menus.Screen==SportsEventScreen.Play;
 Camera ISportsScorePresentationSource.ScoreCamera(int view)=>cameras!=null&&view>=0&&view<cameras.Length?cameras[view]:null;
 LWS.TruckSports.SportsParticipantBinding participantBinding;SportsMatchColors matchColors;string participantMatchId;
 public SportsMatchColors MatchColors=>matchColors;
 public string MatchId=>participantMatchId;
 public SportsScoreJournal ScoreJournal{get;private set;}
 public event Action<SportsScoreEvent> ScoreConfirmed;
 protected bool ConfirmScore(string eventId,int slot,double delta,double total,string reason,SportsScoreTier tier=SportsScoreTier.Small,string unit="PTS",string team=null,bool roundEnded=false,bool matchEnded=false){
  if(!Running||Finished||ScoreJournal==null||slot<0||slot>=setup.count)return false;
  if(!ScoreJournal.TryConfirm(eventId,setup.players[slot],slot,delta,total,reason,tier,unit,team,roundEnded,matchEnded,out var receipt))return false;
  ScoreConfirmed?.Invoke(receipt);return true;
 }
 protected bool ConfirmTeamScore(string eventId,string teamId,string teamName,Color teamColor,double delta,double total,string reason,SportsScoreTier tier=SportsScoreTier.Major,string unit="PTS",bool roundEnded=false,bool matchEnded=false){
  if(!Running||Finished||ScoreJournal==null)return false;
  if(!ScoreJournal.TryConfirmTeam(eventId,teamId,teamName,teamColor,delta,total,reason,tier,unit,roundEnded,matchEnded,out var receipt))return false;
  ScoreConfirmed?.Invoke(receipt);return true;
 }

 protected bool ConfirmOutcome(string eventId,int slot,double value,string reason,string unit="SEC",SportsScoreTier tier=SportsScoreTier.Major,bool roundEnded=false,bool matchEnded=false){
  if(!Running||Finished||ScoreJournal==null||slot<0||slot>=setup.count)return false;
  if(!ScoreJournal.TryConfirmOutcome(eventId,setup.players[slot],slot,value,reason,tier,unit,roundEnded,matchEnded,out var receipt))return false;
  ScoreConfirmed?.Invoke(receipt);return true;
 }
 public void RefreshParticipants(bool saveEdits=false){using(matchColors?.Suspend()){if(participantBinding==null||!participantBinding.Matches(setup.players))participantBinding=new LWS.TruckSports.SportsParticipantBinding(setup.players);else participantBinding.Refresh(saveEdits);}}
 void RecordParticipants(string summary,int count){LWS.TruckSports.SportsParticipantRegistry.Record(participantMatchId,PreferenceKey,summary,setup.players,count);}

 // Assigned only in venues that had no existing weather owner. Existing sport-owned weather adapters remain authoritative.
 public SportsChaosWeather presentationWeather;
 // Explicit opt-in venue bindings. Unauthored/protected sports retain their existing presentation.
 public SportsTruckAppearanceHost[] appearanceHosts=Array.Empty<SportsTruckAppearanceHost>();
 public SportsBroadcastFraming[] appearanceFraming=Array.Empty<SportsBroadcastFraming>();
 public void PrepareAppearances(){foreach(var host in appearanceHosts??Array.Empty<SportsTruckAppearanceHost>()){if(host==null)continue;int slot=host.participantSlot;if(slot>=0&&slot<setup.count){var player=setup.players[slot];host.Prepare(player.participantId,player.color);}else host.RestoreOriginal();}foreach(var views in GetComponentsInChildren<SportsDrivingViews>(true))views.RefreshAppearanceExteriors();foreach(var framing in appearanceFraming??Array.Empty<SportsBroadcastFraming>())if(framing!=null&&framing.session==this)framing.RefreshAppearanceSubjects();}
 public BowlingSetup setup=new BowlingSetup();public SportsEventMenus menus;public SportsSettings settings;public SportsFeedback feedback;public Camera[] cameras;
 public Key[] actionKeys={Key.Space,Key.RightShift,Key.R,Key.U,Key.B,Key.Numpad0,Key.Digit0,Key.PageUp};
 public int option=1,teamA=1,teamB=1,weather;public bool Running{get;private set;}public bool Paused{get;private set;}public bool Finished{get;private set;}public bool Live=>Running&&!Paused&&!Finished;
 public readonly Vector2[] commands=new Vector2[12];public readonly bool[] actions=new bool[12];readonly bool[] held=new bool[12];readonly SportsDeviceInput input=new SportsDeviceInput();readonly Gamepad[] replacementSources=new Gamepad[12];bool menuReleased;int gateFrame,modalOwner;
 public abstract string Title{get;}public abstract string Help{get;}public abstract int MinimumPlayers{get;}public abstract int MaximumPlayers{get;}public virtual int SetupMaximumPlayers=>MaximumPlayers;public virtual int OptionCount=>3;public virtual int WeatherCount=>3;public virtual void RefreshExtraUI(){}public virtual bool TeamSport=>false;public virtual bool UsesAction=>true;
 public abstract string Header{get;}public abstract string Status{get;}public abstract string Standings{get;}public abstract string Results{get;}public virtual string SourcePolicy=>"Whole controllers have one owner; left/right sticks can share one pad.\nChoose AI per slot or ALL AI to spectate. Action bindings are shown with each source.";public virtual string OptionLabel=>"OPTIONS";public virtual bool MultiViewHUD=>false;public virtual bool ReplayControls=>false;public virtual string ViewText(int index)=>Header;public virtual void ExtraAction(string key){}
 public virtual bool SupportsSplitAIViews=>false;public virtual bool AISplitViewsApplicable=>SupportsSplitAIViews;public virtual bool BroadcastStandingsVisible=>true;public virtual bool BroadcastHeaderVisible=>true;public virtual bool BroadcastStatusVisible=>true;
 // Opt-in compact context: rules remain in Help; only genuine live events use the status plate.
 public virtual bool CompactBroadcastStatus=>false;
 protected bool TryReadDrivingViewportCard(SportsDrivingViews views,int view,out int participant,out string score,out string detail){participant=-1;score=detail=null;if(views==null||views.rally==null||view<0||view>=views.rally.Humans||view>=views.rally.owners.Length)return false;participant=views.rally.owners[view];return participant>=0&&participant<setup.count&&TryReadScoreCard(participant,out score,out detail);}
 public virtual bool BroadcastParticipantActive(int slot)=>true;
 public virtual bool BroadcastFramingApplicable=>!ReplayControls;
 public virtual void DescribeBroadcast(SportsBroadcastAction action,SportsBroadcastFraming framing){}
 public virtual bool TryReadScoreCard(int card,out string value,out string detail){value=detail=null;return false;}
 public virtual int CompactScoreColumns=>0;
 public virtual bool CompactEliminationHud=>false;
 public virtual string EliminationHudStatus=>Header;
 // Read-only viewport presentation. Camera/input owners remain sport-owned.
 public virtual bool CompactViewportHud=>false;
 public virtual string ViewportCardLabel(int view,int participant)=>setup.players[participant].name;
 public virtual Color ViewportCardColor(int view,int participant)=>setup.players[participant].color;
 public virtual string ViewportContextPrompt(int view)=>null;
 public virtual string ViewportTelemetry(int view)=>null;
 public virtual bool TryReadViewportCard(int view,out int participant,out string score,out string detail){participant=-1;score=detail=null;return false;}
 public virtual int ScoreCardCount=>setup.count;public virtual string ScoreCardLabel(int card)=>setup.players[card].name;public virtual int ScoreCardSource(int card)=>card;public virtual Color ScoreCardColor(int card)=>setup.players[ScoreCardSource(card)].color;public virtual float ParticipantProgress(int slot)=>-1;public virtual bool ParticipantLocked(int slot)=>false;
 // Read-only presentation values. Each race retains its existing countdown,
 // release, input gate and scoring clock as the sole simulation authority.
 public virtual bool SupportsRaceCountdown=>false;
 public virtual float RaceCountdownSeconds=>-1;
 public virtual float RaceStartSeconds=>-1;
 public virtual string ControlCaption(int slot)=>Controls(slot);
 public string PreferenceKey{get{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
 return (SportsProbeInputIsolation.Active?"Validation.":"")+GetType().Name+".Setup.v1";
#else
 return GetType().Name+".Setup.v1";
#endif
 }}
 // A mode can retire/reorder choices without reinterpreting an existing saved match.
 protected virtual int OptionVersion=>0;
 protected virtual int MigrateOption(int value,int version)=>value;
 [Serializable]sealed class Saved {public BowlingSetup setup;public Key[] actionKeys;public int option=1,optionVersion,teamA=1,teamB=1,weather;}
 protected virtual void Awake(){try{if(LWS.TruckSports.SportsPrefs.HasKey(PreferenceKey)){var s=JsonUtility.FromJson<Saved>(LWS.TruckSports.SportsPrefs.GetString(PreferenceKey));if(s?.setup?.players!=null&&s.setup.players.Length>=Mathf.Max(8,MinimumPlayers)&&s.setup.players.Length<=12){setup=s.setup;if(s.actionKeys!=null&&s.actionKeys.Length>=8&&s.actionKeys.Length<=12)actionKeys=s.actionKeys;option=MigrateOption(s.option,s.optionVersion);teamA=s.teamA;teamB=s.teamB;weather=s.weather;}}}catch(Exception){Debug.LogWarning(Title+" setup invalid; defaults retained.");}NormalizeSetup();RefreshParticipants();}
 public virtual void NormalizeSetup(){EnsureRoster();teamA=Mathf.Clamp(teamA,1,4);teamB=Mathf.Clamp(teamB,1,4);setup.count=TeamSport?teamA+teamB:Mathf.Clamp(setup.count,MinimumPlayers,MaximumPlayers);setup.mode=BowlingMode.Simultaneous;option=Mathf.Clamp(option,0,OptionCount-1);weather=Mathf.Clamp(weather,0,WeatherCount-1);}
 void EnsureRoster(){int size=Mathf.Max(8,MaximumPlayers);if(setup.players==null||setup.players.Length<size){var previous=setup.players;var defaults=BowlingSetup.Defaults();var roster=new BowlingPlayer[size];for(int i=0;i<size;i++){if(previous!=null&&i<previous.Length&&previous[i]!=null)roster[i]=previous[i];else{var key=defaults[i%4];roster[i]=new BowlingPlayer{name="Racer "+(i+1),color=Color.HSVToRGB(Mathf.Repeat(i*.618034f,1),.7f,1),source=BowlingSource.AI,difficulty=BowlingDifficulty.Normal,forward=key.forward,left=key.left,back=key.back,right=key.right};}}setup.players=roster;}if(actionKeys.Length<size){var old=actionKeys;actionKeys=new Key[size];for(int i=0;i<size;i++)actionKeys[i]=old[i%4];}}
 protected virtual void Start(){menus.Show(SportsEventScreen.Setup);}
 protected abstract void BeginGame();protected abstract void Simulate(float dt);protected virtual void Present(float dt){}protected virtual void StopSounds(){}protected virtual void StopGame(){}protected virtual void SetPaused(bool paused){}
 public bool Ready(int slot){var p=setup.players[slot];return p.source==BowlingSource.AI||p.source==BowlingSource.Keyboard&&Keyboard.current!=null&&Keyboard.current.enabled||p.Controller&&p.pad!=null&&p.pad.added&&p.pad.enabled;}
 public virtual bool ValidateSources(out string reason){if(!setup.Validate(out reason))return false;if(!UsesAction)return true;for(int i=0;i<setup.count;i++){var p=setup.players[i];if(p.source!=BowlingSource.Keyboard)continue;var k=actionKeys[i];if(k==Key.None||k==Key.Escape){reason="Assign an action key; Escape is reserved.";return false;}for(int j=0;j<setup.count;j++){var q=setup.players[j];if(q.source!=BowlingSource.Keyboard||setup.mode==BowlingMode.HotSeat&&setup.sharedHotSeat&&i!=j)continue;if(k==q.forward||k==q.back||k==q.left||k==q.right||i!=j&&k==actionKeys[j]){reason="Action key conflicts with slot "+(j+1);return false;}}}return true;}
 public bool DevicesReady{get{if(!ValidateSources(out _))return false;for(int i=0;i<setup.count;i++)if(!Ready(i))return false;return true;}}public bool AllAI{get{for(int i=0;i<setup.count;i++)if(setup.players[i].source!=BowlingSource.AI)return false;return true;}}
 public void Save(){using(matchColors?.Suspend()){RefreshParticipants(true);NormalizeSetup();LWS.TruckSports.SportsPrefs.SetString(PreferenceKey,JsonUtility.ToJson(new Saved{setup=setup,actionKeys=actionKeys,option=option,optionVersion=OptionVersion,teamA=teamA,teamB=teamB,weather=weather}));LWS.TruckSports.SportsPrefs.Save();menus?.Dirty();}}
 public bool StartMatch(){NormalizeSetup();if(!DevicesReady){menus.Show(SportsEventScreen.Join);return false;}Save();matchColors?.Restore();matchColors=TeamSport?null:new SportsMatchColors(setup.players,setup.count);try{PrepareAppearances();ScoreJournal?.Close();participantMatchId=System.Guid.NewGuid().ToString("N");ScoreJournal=new SportsScoreJournal(gameObject.scene.name,participantMatchId);Time.timeScale=1;Running=true;Paused=Finished=false;BeginGame();if(presentationWeather!=null){presentationWeather.Pause(false);presentationWeather.Configure(participantMatchId.GetHashCode(),0,0);}Gate();menus.Show(SportsEventScreen.Play);return true;}catch{matchColors?.Restore();matchColors=null;throw;}}
 public void EndMatch(){if(Finished)return;ScoreJournal?.Close();StopGame();Finished=true;RecordParticipants(Results,setup.count);feedback.StopAll();StopSounds();menus.Show(SportsEventScreen.Result);feedback.Play(6,0,true);}
 public void SetupScreen(){ScoreJournal?.Close();SetPaused(false);presentationWeather?.Pause(false);StopGame();matchColors?.Restore();matchColors=null;Running=Paused=Finished=false;Array.Clear(replacementSources,0,replacementSources.Length);Time.timeScale=1;Gate();StopSounds();feedback.StopAll();menus.Show(SportsEventScreen.Setup);}
 public void AllBots(){Array.Clear(replacementSources,0,replacementSources.Length);for(int i=0;i<setup.count;i++){setup.players[i].source=BowlingSource.AI;setup.players[i].pad=null;}Save();}
 public void RequestReplacement(int slot){if(slot<0||slot>=setup.count||!setup.players[slot].Controller)return;if(setup.players[slot].pad!=null)replacementSources[slot]=setup.players[slot].pad;setup.players[slot].pad=null;Gate();menus.Dirty();}
 public bool Assign(int slot,Gamepad pad){if(slot<0||slot>=setup.count||!setup.players[slot].Controller||pad==null||!pad.added||!pad.enabled)return false;var p=setup.players[slot];var old=p.pad??replacementSources[slot];int changed=1<<slot,nulls=0;if(old!=null&&setup.mode==BowlingMode.HotSeat&&setup.sharedHotSeat)for(int i=0;i<setup.count;i++)if(setup.players[i].Controller&&(setup.players[i].pad==old||setup.players[i].pad==null&&replacementSources[i]==old))changed|=1<<i;for(int i=0;i<setup.count;i++)if((changed&(1<<i))!=0){if(setup.players[i].pad==null)nulls|=1<<i;setup.players[i].pad=pad;}if(!ValidateSources(out _)){for(int i=0;i<setup.count;i++)if((changed&(1<<i))!=0)setup.players[i].pad=(nulls&(1<<i))!=0?null:old;return false;}for(int i=0;i<setup.count;i++)if((changed&(1<<i))!=0)replacementSources[i]=null;input.Forget(pad);Gate();menus.Dirty();return true;}
 public void CycleKeyboardCluster(int slot){var p=setup.players[slot];if(p.source!=BowlingSource.Keyboard)return;var defaults=BowlingSetup.Defaults();int current=-1;for(int i=0;i<8;i++)if(p.forward==defaults[i].forward&&p.left==defaults[i].left&&p.back==defaults[i].back&&p.right==defaults[i].right)current=i;int next=(current+1)%8;var keys=defaults[next];p.forward=keys.forward;p.left=keys.left;p.back=keys.back;p.right=keys.right;actionKeys[slot]=new[]{Key.Space,Key.RightShift,Key.R,Key.U,Key.B,Key.Numpad0,Key.Digit0,Key.PageUp}[next];Gate();Save();}
 public int KeyboardCluster(int slot){var p=setup.players[slot];var defaults=BowlingSetup.Defaults();for(int i=0;i<8;i++)if(p.forward==defaults[i].forward&&p.left==defaults[i].left&&p.back==defaults[i].back&&p.right==defaults[i].right)return i+1;return 0;}
 public void NextPad(int slot){foreach(var pad in Gamepad.all)if(pad!=setup.players[slot].pad&&Assign(slot,pad))return;RequestReplacement(slot);}
 public void CycleSource(int slot){replacementSources[slot]=null;var p=setup.players[slot];p.source=(BowlingSource)(((int)p.source+1)%5);p.pad=null;Save();Gate();}
 public Vector2 ReadDrive(int slot){if(!Ready(slot))return Vector2.zero;var p=setup.players[slot];if(p.source==BowlingSource.Keyboard)return new Vector2((SportsDeviceInput.KeyHeld(p.right)?1:0)-(SportsDeviceInput.KeyHeld(p.left)?1:0),(SportsDeviceInput.KeyHeld(p.forward)?1:0)-(SportsDeviceInput.KeyHeld(p.back)?1:0));if(p.source==BowlingSource.AI)return Vector2.zero;var stick=p.source==BowlingSource.RightStick?p.pad.rightStick.ReadValue():p.pad.leftStick.ReadValue();float y=p.source==BowlingSource.WholeController?p.pad.rightTrigger.ReadValue()-p.pad.leftTrigger.ReadValue():stick.y;return new Vector2(Mathf.Abs(stick.x)>.16f?Mathf.Clamp(stick.x*settings.sensitivity,-1,1):0,Mathf.Abs(y)>.12f?y:0);}
 public bool ActionHeld(int slot){if(!UsesAction||!Ready(slot))return false;var p=setup.players[slot];return p.source==BowlingSource.Keyboard?SportsDeviceInput.KeyHeld(actionKeys[slot]):p.source==BowlingSource.LeftStick?p.pad.leftTrigger.ReadValue()>.5f:p.source==BowlingSource.RightStick?p.pad.rightTrigger.ReadValue()>.5f:p.source==BowlingSource.WholeController&&p.pad.buttonSouth.isPressed;}
 static string KeyName(Key key)=>key==Key.UpArrow?"UP":key==Key.DownArrow?"DOWN":key==Key.LeftArrow?"LEFT":key==Key.RightArrow?"RIGHT":key==Key.RightShift?"R.SHIFT":key==Key.PageUp?"PGUP":key.ToString().Replace("Numpad","N").Replace("Digit","").ToUpperInvariant();
 public string Controls(int slot){var p=setup.players[slot];string drive=p.source==BowlingSource.AI?"AI · "+p.difficulty:p.source==BowlingSource.Keyboard?(p.forward==Key.UpArrow&&p.left==Key.LeftArrow&&p.back==Key.DownArrow&&p.right==Key.RightArrow?"ARROWS":KeyName(p.forward)+"/"+KeyName(p.left)+"/"+KeyName(p.back)+"/"+KeyName(p.right)):p.source==BowlingSource.WholeController?"LEFT STICK · RT/LT":p.source==BowlingSource.LeftStick?"LEFT STICK":"RIGHT STICK";return drive+(!UsesAction||p.source==BowlingSource.AI?"":" · "+(p.source==BowlingSource.Keyboard?KeyName(actionKeys[slot]):p.source==BowlingSource.LeftStick?"LT":p.source==BowlingSource.RightStick?"RT":"SOUTH")+" ACTION");}
 bool Neutral(int slot){var p=setup.players[slot];return p.source==BowlingSource.AI||Ready(slot)&&ReadDrive(slot).sqrMagnitude<.01f&&!ActionHeld(slot)&&(!p.Controller||!p.pad.buttonSouth.isPressed&&!p.pad.buttonNorth.isPressed&&!p.pad.startButton.isPressed);}
 public void ConsumeActions(){for(int i=0;i<setup.count;i++){actions[i]=false;held[i]=ActionHeld(i);}}
 public void Gate(){gateFrame=Time.frameCount;menuReleased=false;for(int i=0;i<commands.Length;i++){if(i<setup.players.Length)setup.players[i].released=false;commands[i]=Vector2.zero;actions[i]=false;held[i]=i<setup.players.Length&&ActionHeld(i);}}
 public bool MenuConfirm(){if(!Running||Finished)return true;if(!menuReleased)return false;return Mouse.current!=null&&Mouse.current.leftButton.wasReleasedThisFrame||SportsDeviceInput.KeyDown(Key.Enter)||SportsDeviceInput.KeyDown(Key.Space)||setup.players[modalOwner].Controller&&Ready(modalOwner)&&setup.players[modalOwner].pad.buttonSouth.wasPressedThisFrame;}
 public void Pause(int owner=0,bool disconnected=false){if(!Live)return;modalOwner=Mathf.Clamp(owner,0,setup.count-1);Paused=true;Time.timeScale=0;SetPaused(true);presentationWeather?.Pause(true);Gate();feedback.StopAll();StopSounds();menus.Show(disconnected?SportsEventScreen.Join:SportsEventScreen.Pause);}
 public void Resume(){if(!Running){StartMatch();return;}if(!DevicesReady){menus.Show(SportsEventScreen.Join);return;}SetPaused(false);presentationWeather?.Pause(false);Paused=false;Time.timeScale=1;Gate();menus.Show(SportsEventScreen.Play);}
 public void Pulse(int slot,float amount,float seconds){var p=setup.players[slot];if(p.source==BowlingSource.WholeController)feedback.Pulse(p.pad,amount,seconds);}
 protected virtual void OnEnable(){InputSystem.onDeviceChange+=Changed;}protected virtual void OnDisable(){InputSystem.onDeviceChange-=Changed;feedback.StopAll();StopSounds();}protected virtual void OnDestroy(){matchColors?.Restore();matchColors=null;ScoreJournal?.Close();Time.timeScale=1;}
 void Changed(InputDevice device,InputDeviceChange change){if(LWS.TruckSports.SportsFocusPolicy.Suspended(device,change))return;menus.Dirty();for(int i=0;i<setup.count;i++)if(setup.players[i].pad==device){input.Forget((Gamepad)device);Gate();if(change==InputDeviceChange.Removed||change==InputDeviceChange.Disconnected||change==InputDeviceChange.Disabled)Pause(i,true);break;}}
 void OnApplicationFocus(bool focus){if(!focus){feedback.StopAll();StopSounds();}}
 void Update(){input.Sample();if(menus.Screen==SportsEventScreen.Join)foreach(var pad in Gamepad.all)if(input.Down(pad,2))for(int i=0;i<setup.count;i++)if(setup.players[i].Controller&&!Ready(i)&&Assign(i,pad))break;
  if(!menuReleased&&Time.frameCount>gateFrame){bool neutral=!SportsDeviceInput.KeyHeld(Key.Enter)&&!SportsDeviceInput.KeyHeld(Key.Space)&&(Mouse.current==null||!Mouse.current.leftButton.isPressed&&!Mouse.current.leftButton.wasReleasedThisFrame);for(int i=0;i<setup.count;i++)if(Ready(i))neutral&=Neutral(i);foreach(var pad in Gamepad.all)neutral&=!pad.buttonSouth.isPressed;if(neutral)menuReleased=true;}
  if(Running&&!Finished){bool pause=SportsDeviceInput.KeyDown(Key.Escape);for(int i=0;i<setup.count;i++)if(setup.players[i].Controller&&Ready(i)&&input.Down(setup.players[i].pad,4)){modalOwner=i;pause=true;}if(pause){if(Paused){if(menuReleased)Resume();}else Pause(modalOwner);return;}if(!Paused)for(int i=0;i<setup.count;i++){var p=setup.players[i];if(p.source==BowlingSource.AI)continue;if(!Ready(i)){Pause(i,true);return;}bool now=ActionHeld(i);if(!p.released){held[i]=now;if(Neutral(i))p.released=true;continue;}commands[i]=ReadDrive(i);actions[i]|=now&&!held[i];held[i]=now;}}
  Present(Time.deltaTime);
 }
 void FixedUpdate(){if(!Live||menus.Screen!=SportsEventScreen.Play)return;presentationWeather?.Tick(Time.time);Simulate(Time.fixedDeltaTime);Array.Clear(actions,0,actions.Length);}
}
}

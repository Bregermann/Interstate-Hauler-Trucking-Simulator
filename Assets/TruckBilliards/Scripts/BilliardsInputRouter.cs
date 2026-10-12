using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
namespace LWS.TruckBilliards
{
 [DefaultExecutionOrder(-50)]
 public sealed class BilliardsInputRouter:MonoBehaviour
 {
  public BilliardsSession session;
  public bool HandoffReady=>handoffReleased;
  public float placementSpeed=5; public bool placementReleased; bool handoffReleased,groupReleased,disconnectReleased;float mouseX,mouseY;

  struct PadButtons{public uint previous,current;public int frame;}
  readonly System.Collections.Generic.Dictionary<Gamepad,PadButtons> buttons=new System.Collections.Generic.Dictionary<Gamepad,PadButtons>();
  static bool Raw(ButtonControl control)=>control.ReadValue()>=control.pressPointOrDefault;
  PadButtons Buttons(Gamepad pad){
   if(pad==null||!pad.added)return default;
   bool known=buttons.TryGetValue(pad,out var state);
   if(known&&state.frame==Time.frameCount)return state;
   uint mask=(Raw(pad.buttonSouth)?1u:0u)|(Raw(pad.buttonNorth)?2u:0u)|(Raw(pad.startButton)?4u:0u);
   state.previous=known?state.current:mask;state.current=mask;state.frame=Time.frameCount;buttons[pad]=state;return state;
  }
  bool PadHeld(Gamepad pad,uint mask)=>(Buttons(pad).current&mask)!=0;
  bool PadDown(Gamepad pad,uint mask){var state=Buttons(pad);return (state.current&mask)!=0&&(state.previous&mask)==0;}
  public bool SouthPressed(Gamepad pad)=>PadDown(pad,1);
  void OnEnable(){InputSystem.onDeviceChange+=DeviceChanged;}
  void OnDisable(){InputSystem.onDeviceChange-=DeviceChanged;}
  void DeviceChanged(InputDevice d,InputDeviceChange change)
  {
   if(session==null||!session.InMatch)return;
   if(change==InputDeviceChange.Added||change==InputDeviceChange.Reconnected||change==InputDeviceChange.Enabled){for(int i=0;i<(session.IsPractice?1:2);i++)if(!session.IsAI(i)&&session.players[i].control==BilliardsControl.Gamepad&&Pad(i)==d){buttons.Remove((Gamepad)d);GateAll();break;}}
   if(change==InputDeviceChange.Disconnected||change==InputDeviceChange.Removed||change==InputDeviceChange.Disabled){
    for(int i=0;i<(session.IsPractice?1:2);i++)if(!session.IsAI(i)&&session.players[i].control==BilliardsControl.Gamepad&&Pad(i)==d){session.Disconnect();break;}
   }
  }
  public void GateAll()
  {
   if(session==null||session.players==null)return;
   foreach(var p in session.players){p.released=false;if(p.truck!=null){p.truck.throttle=p.truck.steer=0;p.truck.brake=false;p.truck.Disarm();}}
   placementReleased=false;handoffReleased=false;groupReleased=false;disconnectReleased=false;
   if(Mouse.current!=null){var position=Mouse.current.position.ReadValue();mouseX=position.x;mouseY=position.y;}
  }
  bool Held(Key k)=>Keyboard.current!=null&&Keyboard.current[k].isPressed;
  bool Down(Key k)=>Keyboard.current!=null&&Keyboard.current[k].wasPressedThisFrame;
  Gamepad Pad(int side)=>session.mode==BilliardsMode.HotSeat?session.SharedPad:session.players[side].pad;
  bool Controller(int side)=>session.players[side].control==BilliardsControl.Gamepad;
  public bool Neutral(int side)
  {
   if(Controller(side)){var p=Pad(side);return p!=null&&p.added&&p.leftStick.ReadValue().magnitude<.15f&&p.rightTrigger.ReadValue()<.1f&&p.leftTrigger.ReadValue()<.1f&&!PadHeld(p,1)&&!PadHeld(p,2)&&!PadHeld(p,4);}
   bool second=session.mode!=BilliardsMode.HotSeat&&!session.IsPractice&&session.players[side].control==BilliardsControl.KeyboardArrows;
   if(session.mode==BilliardsMode.SinglePlayer&&(Held(Key.UpArrow)||Held(Key.DownArrow)||Held(Key.LeftArrow)||Held(Key.RightArrow)))return false;
   return second?!Held(Key.UpArrow)&&!Held(Key.DownArrow)&&!Held(Key.LeftArrow)&&!Held(Key.RightArrow)&&!Held(Key.RightShift)&&!Held(Key.Enter)&&!Held(Key.RightCtrl):
    !Held(Key.W)&&!Held(Key.S)&&!Held(Key.A)&&!Held(Key.D)&&!Held(Key.Space)&&!Held(Key.R)&&!Held(Key.F)&&!Held(Key.G)&&!Held(Key.Enter)&&!(Mouse.current!=null&&Mouse.current.leftButton.isPressed);
  }
  void Update()
  {
   if(session==null)return;
   foreach(var pad in Gamepad.all)Buttons(pad);
   var screen=session.Screen;
   if(screen==BilliardsScreen.Disconnect&&!disconnectReleased){bool neutral=!Held(Key.Enter)&&!Held(Key.Space)&&(Mouse.current==null||(!Mouse.current.leftButton.isPressed&&!Mouse.current.leftButton.wasReleasedThisFrame));foreach(var pad in Gamepad.all)neutral&=!PadHeld(pad,1);if(neutral)disconnectReleased=true;}
   if(screen==BilliardsScreen.Join||screen==BilliardsScreen.Disconnect){
    foreach(var pad in Gamepad.all)if(PadDown(pad,2)){
     int slot=-1;for(int i=0;i<(session.IsPractice?1:2);i++)if(!session.IsAI(i)&&session.players[i].control==BilliardsControl.Gamepad&&!session.Ready(i)){slot=i;break;}
     if(slot>=0)session.Assign(slot,pad);
    }
    if(screen==BilliardsScreen.Disconnect&&session.menus!=null)session.menus.RefreshJoin();
   }
   bool pause=Down(Key.Escape);if(session.AIOnly)foreach(var spectator in Gamepad.all)if(PadDown(spectator,4))pause=true;
   if(session.InMatch)foreach(var p in session.players)if(p.control==BilliardsControl.Gamepad&&p.pad!=null&&p.pad.added&&PadDown(p.pad,4))pause=true;
   if(pause){
    if(screen==BilliardsScreen.Pause)session.Resume();
    else if(screen==BilliardsScreen.Match||screen==BilliardsScreen.Handoff||screen==BilliardsScreen.GroupSelection)session.Pause();
    else session.menus.Back();
    return;
   }
   if(screen==BilliardsScreen.Handoff){
    int incoming=session.match.rules.Turn;
    bool held=Controller(incoming)?Pad(incoming)!=null&&PadHeld(Pad(incoming),1):Held(Key.Enter)||Held(Key.Space);
    if(!held)handoffReleased=true;
    bool pressed=Controller(incoming)?Pad(incoming)!=null&&PadDown(Pad(incoming),1):Down(Key.Enter)||Down(Key.Space);
    if(handoffReleased&&pressed)session.ConfirmHandoff();return;
   }
   if(screen==BilliardsScreen.GroupSelection){GroupSelection();return;}
   if(!session.GameplayEnabled)return;
   if(session.match.rules.Phase==MatchPhase.Placement){Placement();return;}
   for(int i=0;i<2;i++){
    if(session.IsAI(i)||(session.IsPractice&&i==1))continue;
    if(session.mode==BilliardsMode.HotSeat&&session.sharedOwner!=i){session.players[i].truck.Stop();continue;}
    Drive(i);
   }
  }
  void Drive(int side)
  {
   var slot=session.players[side];var truck=slot.truck;
   if(!slot.released){truck.Stop();if(Neutral(side))slot.released=true;return;}
   if(Controller(side)){
    var p=Pad(side);if(p==null||!p.added){session.Disconnect();return;}
    truck.throttle=p.rightTrigger.ReadValue()-p.leftTrigger.ReadValue();truck.steer=Mathf.Clamp(p.leftStick.x.ReadValue()*session.settings.sensitivity,-1,1);
    truck.brake=PadHeld(p,1);if(PadDown(p,2))session.match.ResetTruck(side);
   }else{
    bool second=session.mode!=BilliardsMode.HotSeat&&!session.IsPractice&&session.players[side].control==BilliardsControl.KeyboardArrows;
    truck.throttle=(Held(second?Key.UpArrow:Key.W)?1:0)-(Held(second?Key.DownArrow:Key.S)?1:0);
    truck.steer=(Held(second?Key.RightArrow:Key.D)?1:0)-(Held(second?Key.LeftArrow:Key.A)?1:0);
    if(session.mode==BilliardsMode.SinglePlayer){
     truck.throttle=(Held(Key.W)||Held(Key.UpArrow)?1:0)-(Held(Key.S)||Held(Key.DownArrow)?1:0);
     truck.steer=(Held(Key.D)||Held(Key.RightArrow)?1:0)-(Held(Key.A)||Held(Key.LeftArrow)?1:0);
    }
    truck.brake=Held(second?Key.RightShift:Key.Space);if(Down(second?Key.Enter:Key.R))session.match.ResetTruck(side);
   }
  }
  void GroupSelection(){int owner=session.match.rules.Turn;if(session.IsAI(owner))return;if(!groupReleased){if(Neutral(owner)&&(Mouse.current==null||(!Mouse.current.leftButton.isPressed&&!Mouse.current.leftButton.wasReleasedThisFrame)))groupReleased=true;return;}if(Controller(owner)&&PadDown(Pad(owner),1)){var selected=UnityEngine.EventSystems.EventSystem.current!=null?UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject:null;var action=selected!=null?selected.GetComponent<BilliardsMenuAction>():null;if(action==null||action.action=="solids"||action.action=="stripes")session.ChooseGroup(owner,action!=null&&action.action=="stripes"?2:1);return;}if(!Controller(owner)){bool arrows=session.players[owner].control==BilliardsControl.KeyboardArrows;if(Down(arrows?Key.RightCtrl:Key.F))session.ChooseGroup(owner,1);else if(Down(arrows?Key.RightShift:Key.G))session.ChooseGroup(owner,2);}}
  public bool DisconnectConfirm(){bool held=Held(Key.Enter)||Held(Key.Space)||(Mouse.current!=null&&Mouse.current.leftButton.isPressed);foreach(var pad in Gamepad.all)held|=PadHeld(pad,1);if(!disconnectReleased&&Mouse.current!=null&&Mouse.current.leftButton.wasReleasedThisFrame)return false;if(!held)return true;if(!disconnectReleased)return false;if(Down(Key.Enter)||Down(Key.Space)||(Mouse.current!=null&&Mouse.current.leftButton.wasReleasedThisFrame))return true;foreach(var pad in Gamepad.all)if(PadDown(pad,1))return true;return false;}
  public bool GroupConfirm(){int owner=session.match.rules.Turn;if(!groupReleased||session.IsAI(owner))return false;if(Mouse.current!=null&&Mouse.current.leftButton.wasReleasedThisFrame)return true;return Controller(owner)&&PadDown(Pad(owner),1);}
  public void BeginPlacement()
  {
   GateAll();session.match.placementPosition=session.match.FindPlacement(0);session.match.placementPosition.y=.5f;
   if(Mouse.current!=null){var mouse=Mouse.current.position.ReadValue();mouseX=mouse.x;mouseY=mouse.y;}
   UpdatePreview();
  }
  void Placement()
  {
   int owner=session.match.rules.Turn;
   if(session.IsAI(owner))return;
   foreach(var p in session.players)p.truck.Stop();
   if(!placementReleased){if(Neutral(owner))placementReleased=true;UpdatePreview();return;}
   Vector2 motion=Vector2.zero;bool confirm=false;
   if(Controller(owner)){
    var p=Pad(owner);if(p==null||!p.added){session.Disconnect();return;}
    motion=p.leftStick.ReadValue();confirm=PadDown(p,1);
   }else{
    bool second=session.mode!=BilliardsMode.HotSeat&&!session.IsPractice&&session.players[owner].control==BilliardsControl.KeyboardArrows;
    motion=new Vector2((Held(second?Key.RightArrow:Key.D)?1:0)-(Held(second?Key.LeftArrow:Key.A)?1:0),(Held(second?Key.UpArrow:Key.W)?1:0)-(Held(second?Key.DownArrow:Key.S)?1:0));
    confirm=Down(second?Key.RightCtrl:Key.F);
    // In shared keyboard only Player 1 owns mouse placement; Player 2 uses their keys.
    if(!second&&Mouse.current!=null){
     var m=Mouse.current.position.ReadValue();
     if(Mathf.Abs(m.x-mouseX)+Mathf.Abs(m.y-mouseY)>.2f){var ray=session.match.tableCamera.ScreenPointToRay(m);var plane=new Plane(Vector3.up,new Vector3(0,.5f,0));if(plane.Raycast(ray,out float dist))session.match.placementPosition=ray.GetPoint(dist);}
     mouseX=m.x;mouseY=m.y;confirm|=Mouse.current.leftButton.wasPressedThisFrame;
    }
   }
   // Screen-space left/right/up map to the elevated camera's felt plane.
   var right=session.match.tableCamera.transform.right;right.y=0;right.Normalize();
   var up=Vector3.Cross(right,Vector3.up).normalized;
   session.match.placementPosition+=(right*motion.x+up*motion.y)*placementSpeed*Time.unscaledDeltaTime;session.match.placementPosition.y=.5f;
   UpdatePreview();if(confirm&&session.match.placementValid)session.match.PlaceCue(session.match.placementPosition);
  }
  public void UpdatePreview()
  {
   var match=session.match;match.placementValid=match.ValidPlacement(match.placementPosition,0);match.preview.position=match.placementPosition;
   match.preview.GetComponent<Renderer>().material.color=match.placementValid?new Color(.2f,1,.3f,.45f):new Color(1,.15f,.15f,.45f);
   match.preview.gameObject.SetActive(session.GameplayEnabled&&match.rules.Phase==MatchPhase.Placement&&!session.IsAI(match.rules.Turn));
  }
 }
}









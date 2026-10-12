using UnityEngine;
using UnityEngine.InputSystem;
using LWS.TruckSports;
namespace LWS.TruckHockey {
[System.Serializable] public sealed class HockeyPlayer {public string name="Player";public Color color=Color.cyan;[System.NonSerialized]public Gamepad pad;[System.NonSerialized]public bool controller,released;}
[DefaultExecutionOrder(-100)]
public sealed class HockeyMatch:MonoBehaviour {
 public Rigidbody puck;public HockeyPuck puckContact;public HockeyTruck[] trucks;public HockeyAI ai;public HockeyMenus menus;public SportsSettings settings;public SportsFeedback feedback;public Collider[] truckGoalBarriers;public Renderer[] goalCreases;
 public HockeyPlayer[] players=new[]{new HockeyPlayer{name="Player 1",color=new Color(.15f,.8f,1)},new HockeyPlayer{name="Player 2",color=new Color(1,.55f,.13f)}};
 public HockeyMode mode;public HockeyDifficulty difficulty=HockeyDifficulty.Normal;public int durationMinutes=3;public bool soloController;
 public float goalLine=12,goalHalfWidth=2.1f,halfWidth=6,cornerRadius=2.5f,deadlockSeconds=10;
 public readonly HockeyRules rules=new HockeyRules();public bool InMatch{get;private set;}public bool MenuOpen=>menus.Screen!=HockeyScreen.Match;
 public string Notice{get;private set;}="Drive into the puck. No shooting button.";public int EmergencyFaceoffs{get;private set;}public float PuckDeadlockElapsed=>deadlock;
 public readonly SportsDeviceInput input=new SportsDeviceInput();bool swapped,pendingRematch,haveSegment;Vector3 previous;float stepDuration,deadlock,invalidTime;int lastCount;readonly Collider[] overlaps=new Collider[16];Collider puckCollider;readonly System.Collections.Generic.HashSet<Material> ownedColors=new System.Collections.Generic.HashSet<Material>();
 public bool AIEnabled=>mode==HockeyMode.PlayerAI;
 public string PlayerName(int side)=>side==1&&AIEnabled?"AI":players[side].name;
 public bool Ready(int side)=>!players[side].controller||(players[side].pad!=null&&players[side].pad.added&&players[side].pad.enabled);
 public bool DevicesReady=>Ready(0)&&Ready(1)&&(!(players[0].controller&&players[1].controller)||players[0].pad!=players[1].pad)&&((players[0].controller&&(players[1].controller||AIEnabled))||(Keyboard.current!=null&&Keyboard.current.enabled));
 void Awake(){puckCollider=puck.GetComponent<Collider>();foreach(var barrier in truckGoalBarriers)Physics.IgnoreCollision(barrier,puck.GetComponent<Collider>());difficulty=(HockeyDifficulty)Mathf.Clamp(PlayerPrefs.GetInt("TruckHockey.Difficulty",1),0,2);Configure();Freeze(true);}
 void Start(){menus.Show(HockeyScreen.Setup);}
 void OnEnable(){InputSystem.onDeviceChange+=DeviceChanged;}
 void OnDisable(){InputSystem.onDeviceChange-=DeviceChanged;feedback.StopAll();}
 void OnDestroy(){foreach(var material in ownedColors)if(material!=null)Destroy(material);Time.timeScale=1;}
 void DeviceChanged(InputDevice device,InputDeviceChange change){if(!InMatch)return;if(change!=InputDeviceChange.Removed&&change!=InputDeviceChange.Disconnected&&change!=InputDeviceChange.Disabled)return;for(int i=0;i<2;i++)if(players[i].controller&&players[i].pad==device){Pause(true);break;}}
 public void Configure(){players[0].controller=mode==HockeyMode.SeparateControllers||(mode==HockeyMode.PlayerAI&&soloController);players[1].controller=mode==HockeyMode.SeparateControllers||mode==HockeyMode.KeyboardController;if(!players[0].controller)players[0].pad=null;if(!players[1].controller)players[1].pad=null;ai.enabled=AIEnabled;Gate();}
 public bool Assign(int side,Gamepad pad){if(side<0||side>1||!players[side].controller||pad==null||!pad.added||!pad.enabled||players[1-side].pad==pad)return false;players[side].pad=pad;Gate();return true;}
 public void Unassign(int side){players[side].pad=null;Gate();}
 public void Gate(){foreach(var p in players)p.released=false;foreach(var t in trucks)t.ClearInput();}
 public bool StartMatch(bool rematch=false){rematch=rematch||pendingRematch;Configure();if(!DevicesReady){pendingRematch=rematch;menus.Show(HockeyScreen.Join);return false;}pendingRematch=false;if(rematch)swapped=!swapped;else swapped=false;InMatch=true;rules.Start(durationMinutes*60,swapped);EmergencyFaceoffs=0;Notice="FACE OFF • drive into the puck";ApplyIdentity();ResetPositions();menus.Show(HockeyScreen.Match);Time.timeScale=1;return true;}
 public void Restart(){if(!DevicesReady){Pause(true);return;}InMatch=true;rules.Start(durationMinutes*60,swapped);Notice="MATCH RESTARTED";ResetPositions();menus.Show(HockeyScreen.Match);Time.timeScale=1;}
 public void ChangeSetup(){pendingRematch=false;InMatch=false;feedback.StopAll();Freeze(true);menus.Show(HockeyScreen.Setup);}
 public void Pause(bool disconnected=false){if(!InMatch||rules.Phase==HockeyPhase.Finished)return;rules.Pause();Gate();feedback.StopAll();menus.Show(disconnected?HockeyScreen.Disconnect:HockeyScreen.Pause);Time.timeScale=0;}
 public void Resume(){if(!DevicesReady){Pause(true);return;}rules.Resume();Gate();menus.Show(HockeyScreen.Match);Time.timeScale=1;}
 public void Notify(string text){Notice=text;}
 public Quaternion SpawnRotation(int side)=>Quaternion.Euler(0,-rules.DefendingSign(side)*90,0);
 public Vector3 SpawnPosition(int side)=>new Vector3(rules.DefendingSign(side)*3.2f,.36f,0);
 public bool SafeRecover(int side,out Vector3 p){int sign=rules.DefendingSign(side);for(int x=0;x<4;x++)for(int z=0;z<5;z++){float zz=z==0?0:(z%2==1?1:-1)*((z+1)/2)*1.7f;p=new Vector3(sign*(4+x*1.5f),.36f,zz);if(Vector3.Distance(p,puck.position)>2&&Vector3.Distance(p,trucks[1-side].body.position)>2.3f)return true;}p=default;return false;}
 void Freeze(bool freeze){if(freeze){foreach(var t in trucks){if(!t.body.isKinematic){t.body.linearVelocity=Vector3.zero;t.body.angularVelocity=Vector3.zero;}t.body.isKinematic=true;t.ClearInput();}if(!puck.isKinematic){puck.linearVelocity=Vector3.zero;puck.angularVelocity=Vector3.zero;}puck.isKinematic=true;}else{foreach(var t in trucks)t.body.isKinematic=false;puck.isKinematic=false;}}
 public void ResetPositions(){Freeze(true);puck.position=new Vector3(0,.22f,0);puck.rotation=Quaternion.identity;puckContact.ClearContacts();for(int i=0;i<2;i++)trucks[i].ResetBody(SpawnPosition(i),SpawnRotation(i));feedback.StopAll();ai.ResetPlan();haveSegment=false;deadlock=invalidTime=0;lastCount=4;Gate();Physics.SyncTransforms();}
 void ApplyIdentity(){if(goalCreases!=null&&goalCreases.Length==2){for(int net=0;net<2;net++){int defender=rules.DefendingSign(0)==(net==0?-1:1)?0:1;var paint=goalCreases[net].material;ownedColors.Add(paint);paint.color=players[defender].color;}}for(int i=0;i<2;i++){if(string.IsNullOrWhiteSpace(players[i].name))players[i].name="Player "+(i+1);foreach(var r in trucks[i].GetComponentsInChildren<MeshRenderer>())if(r.name=="Chassis"||r.name=="Team bumper")foreach(var m in r.materials){ownedColors.Add(m);m.color=players[i].color;}}}
 void Update(){input.Sample();if(menus.Screen==HockeyScreen.Join||menus.Screen==HockeyScreen.Disconnect){foreach(var pad in Gamepad.all)if(input.Down(pad,2))for(int i=0;i<2;i++)if(players[i].controller&&!Ready(i)){if(Assign(i,pad))break;}}
 bool pause=SportsDeviceInput.KeyDown(Key.Escape);foreach(var p in players)if(p.controller&&input.Down(p.pad,4))pause=true;if(pause){if(menus.Screen==HockeyScreen.Match)Pause();else if(menus.Screen==HockeyScreen.Pause)Resume();else menus.Back();return;}
 if(!InMatch||!rules.Live||MenuOpen){foreach(var t in trucks)t.ClearInput();return;}
 for(int i=0;i<2;i++){if(i==1&&AIEnabled)continue;var p=players[i];if(!Ready(i)){Pause(true);return;}bool neutral=p.controller?input.Neutral(p.pad):SportsDeviceInput.NeutralKeys(i==1);if(!p.released){trucks[i].ClearInput();if(neutral)p.released=true;continue;}float gas,turn;bool brake,recover;if(p.controller){gas=p.pad.rightTrigger.ReadValue()-p.pad.leftTrigger.ReadValue();turn=p.pad.leftStick.x.ReadValue()*settings.sensitivity;brake=input.Pressed(p.pad,1);recover=input.Down(p.pad,2);}else{bool second=i==1;gas=(SportsDeviceInput.KeyHeld(second?Key.UpArrow:Key.W)?1:0)-(SportsDeviceInput.KeyHeld(second?Key.DownArrow:Key.S)?1:0);turn=(SportsDeviceInput.KeyHeld(second?Key.RightArrow:Key.D)?1:0)-(SportsDeviceInput.KeyHeld(second?Key.LeftArrow:Key.A)?1:0);brake=SportsDeviceInput.KeyHeld(second?Key.RightShift:Key.Space);recover=SportsDeviceInput.KeyDown(second?Key.Enter:Key.R);}trucks[i].Control(gas,turn,brake);if(recover)trucks[i].Recover();}}
 void FixedUpdate(){if(!InMatch||MenuOpen||rules.Phase==HockeyPhase.Paused)return;
 if(rules.Live){if(haveSegment){bool goal=rules.Step(stepDuration,previous,puck.position,goalLine,goalHalfWidth,puckContact.radius);if(goal){Freeze(true);haveSegment=false;Gate();Notice="GOAL • "+PlayerName(rules.LastScorer);menus.Celebrate(players[rules.LastScorer].color,rules.Phase==HockeyPhase.Finished);feedback.Play(5,0,true);feedback.Pulse(players[rules.LastScorer].pad,.22f,.2f);Debug.Log("TH_PHYSICAL_GOAL scorer="+rules.LastScorer+" score="+rules.Score[0]+":"+rules.Score[1]);}
 if(rules.Phase==HockeyPhase.Finished){Finish();return;}if(!rules.Live){if(!goal){ResetPositions();Notice="SUDDEN DEATH • NEXT GOAL WINS";}return;}}
 if(InvalidPuck()){EmergencyFaceoff();return;}previous=puck.position;stepDuration=Time.fixedDeltaTime;haveSegment=true;
 }else{var before=rules.Phase;int count=Mathf.CeilToInt(rules.PhaseTimer);if((before==HockeyPhase.Pregame||before==HockeyPhase.Faceoff)&&count!=lastCount){lastCount=count;feedback.Play(4,0,true);}rules.Countdown(Time.fixedDeltaTime);if(before==HockeyPhase.Goal&&rules.Phase==HockeyPhase.Faceoff){rules.EndAfterGoal();ResetPositions();}if(rules.Phase==HockeyPhase.Finished){Finish();return;}if(rules.Live){Freeze(false);Gate();Notice=rules.Overtime?"SUDDEN DEATH • NEXT GOAL WINS":"GO!";feedback.Play(4,0,true);previous=puck.position;haveSegment=false;}}}
 void Finish(){Freeze(true);haveSegment=false;feedback.StopAll();menus.Show(HockeyScreen.Result);feedback.Play(6,0,true);feedback.Pulse(players[rules.Winner].pad,.32f,.3f);menus.Celebrate(players[rules.Winner].color,true);Time.timeScale=0;Debug.Log("TH_FINISHED winner="+rules.Winner);}
 bool InvalidPuck(){var p=puck.position;bool outside=Mathf.Abs(p.x)>goalLine+2.7f||Mathf.Abs(p.z)>halfWidth+.7f;bool embedded=false;int n=Physics.OverlapSphereNonAlloc(p,puckContact.radius*.8f,overlaps,~0,QueryTriggerInteraction.Ignore);for(int i=0;i<n;i++){var c=overlaps[i];if(c.attachedRigidbody==null&&c!=truckGoalBarriers[0]&&c!=truckGoalBarriers[1]&&Physics.ComputePenetration(puckCollider,p,puck.rotation,c,c.transform.position,c.transform.rotation,out _,out float depth)&&depth>.12f)embedded=true;}invalidTime=outside||embedded?invalidTime+Time.fixedDeltaTime:0;bool crowded=puckContact.ContactBothTrucks;deadlock=crowded&&puck.linearVelocity.sqrMagnitude<.0064f?deadlock+Time.fixedDeltaTime:0;return invalidTime>.6f||deadlock>deadlockSeconds;}
 void EmergencyFaceoff(){EmergencyFaceoffs++;rules.Faceoff();ResetPositions();Notice="PUCK RECOVERY • FAIR FACEOFF • SCORE/TIME PRESERVED";Debug.Log("TH_EMERGENCY_FACEOFF");}
}
}



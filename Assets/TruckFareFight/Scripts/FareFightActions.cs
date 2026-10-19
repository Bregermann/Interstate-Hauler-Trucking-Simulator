using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using LWS.TruckBowling;
using LWS.TruckSports;
using LWS.TruckTaxi;
using LWS.InterstateHauler;
namespace LWS.TruckFareFight {
 public sealed class FareFightActions:MonoBehaviour {
  public FareFightMatch match;public TruckTaxiEnvironmentSettings needsSettings;
  public FareFightJug[] jugs;public ParticleSystem[] blasts;public Transform[] cabJugs;
  public LwsPlayerGestureController[] gestures;public AudioClip horn,blastSound;
  public float blastRadius=28,blastVelocity=13,throwCooldown=25;
  public readonly TruckTaxiDriverNeedsState[] needs=new TruckTaxiDriverNeedsState[4];
  readonly TruckTaxiJugLiquidLoop[] loops=new TruckTaxiJugLiquidLoop[4];readonly AudioSource[] voices=new AudioSource[4];
  readonly float[] nextThrow=new float[4],nextGesture=new float[4],nextHorn=new float[4];
  readonly bool[] ready=new bool[4],priorHold=new bool[4],priorCue=new bool[4],priorThrow=new bool[4],priorGesture=new bool[4],priorCamera=new bool[4];
  readonly Vector3[] lastVelocity=new Vector3[4];readonly Collider[] overlap=new Collider[512];readonly HashSet<Rigidbody> affected=new HashSet<Rigidbody>();
  readonly Key[,] keys=new Key[4,6];
  static readonly Key[] holdKeys={Key.Space,Key.RightShift,Key.R,Key.U},cueKeys={Key.LeftCtrl,Key.RightCtrl,Key.Y,Key.O},throwKeys={Key.Q,Key.Delete,Key.B,Key.P},gestureKeys={Key.E,Key.Insert,Key.N,Key.Semicolon},cameraKeys={Key.C,Key.End,Key.M,Key.Quote},hornKeys={Key.V,Key.Home,Key.Comma,Key.Slash};
  void Awake(){for(int i=0;i<4;i++){voices[i]=match.trucks[i].gameObject.AddComponent<AudioSource>();voices[i].playOnAwake=false;voices[i].spatialBlend=.8f;voices[i].minDistance=6;voices[i].maxDistance=90;loops[i]=new TruckTaxiJugLiquidLoop(voices[i],null);}}
  int Cluster(int slot)=>Mathf.Clamp(match.KeyboardCluster(slot)-1,0,3);
  public void ResetMatch(){ConfigureKeys();for(int i=0;i<4;i++){loops[i].Stop();needs[i]=new TruckTaxiDriverNeedsState(needsSettings,match.matchSeed+i);needs[i].SetPressure(.7f);needs[i].AddItem(TruckTaxiNeedsItem.EmptyPissJug,12);nextThrow[i]=nextGesture[i]=nextHorn[i]=0;ready[i]=priorHold[i]=priorCue[i]=priorThrow[i]=priorGesture[i]=priorCamera[i]=false;lastVelocity[i]=Vector3.zero;jugs[i].ResetJug();}}
  public string Caption(int slot){var p=match.setup.players[slot];var n=needs[slot];string state=n==null?"":n.JugActive?"JUG "+Mathf.RoundToInt(n.JugProgress*100)+"% · "+(n.CueIsOpen(match.trucks[slot].body.linearVelocity.magnitude>1)?"TAP":"HOLD"):n.FilledJug?"FILLED JUG · "+Mathf.CeilToInt(Mathf.Max(0,nextThrow[slot]-Time.time))+"s":"EMPTY JUG";
   if(p.source==BowlingSource.AI)return state;
   if(p.source==BowlingSource.WholeController)return state+"\nLB HOLD / RB CUE · WEST THROW · EAST GESTURE · NORTH VIEW · L3 HORN";
   if(p.source==BowlingSource.LeftStick||p.source==BowlingSource.RightStick)return state+"\nTRIGGER HOLD / STICK CLICK CUE · BOTH THROW · CLICK GESTURE · SHOULDER HORN";
   return state+"\n"+keys[slot,0]+" HOLD / "+keys[slot,1]+" CUE · "+keys[slot,2]+" THROW · "+keys[slot,3]+" GESTURE · "+keys[slot,4]+" VIEW · "+keys[slot,5]+" HORN";
  }
  public void Tick(float dt){
   for(int i=0;i<match.setup.count;i++){
    var p=match.setup.players[i];var n=needs[i];n.AdvanceGameSeconds(dt*16);bool hold=false,cue=false,throwing=false,gesture=false,camera=false,honking=false,ai=p.source==BowlingSource.AI;
    if(ai){hold=true;if(!n.JugActive&&n.Pressure>.6f)n.StartJug();cue=n.CueIsOpen(match.trucks[i].body.linearVelocity.magnitude>1);throwing=n.FilledJug&&Time.time>nextThrow[i]&&EnemyNear(i,18);}
    else if(p.source==BowlingSource.Keyboard){hold=SportsDeviceInput.KeyHeld(keys[i,0]);cue=SportsDeviceInput.KeyHeld(keys[i,1]);throwing=SportsDeviceInput.KeyHeld(keys[i,2]);gesture=SportsDeviceInput.KeyHeld(keys[i,3]);camera=SportsDeviceInput.KeyHeld(keys[i,4]);honking=SportsDeviceInput.KeyHeld(keys[i,5]);}
    else if(p.pad!=null){if(p.source==BowlingSource.WholeController){hold=p.pad.leftShoulder.isPressed;cue=p.pad.rightShoulder.isPressed;throwing=p.pad.buttonWest.isPressed;gesture=p.pad.buttonEast.isPressed;camera=p.pad.buttonNorth.isPressed;honking=p.pad.leftStickButton.isPressed;}else{hold=p.source==BowlingSource.LeftStick?p.pad.leftTrigger.isPressed:p.pad.rightTrigger.isPressed;cue=p.source==BowlingSource.LeftStick?p.pad.leftStickButton.isPressed:p.pad.rightStickButton.isPressed;throwing=hold&&cue&&n.FilledJug&&!n.JugActive;gesture=cue&&!hold&&!n.JugActive;honking=p.source==BowlingSource.LeftStick?p.pad.leftShoulder.isPressed:p.pad.rightShoulder.isPressed;}}
    if(!ready[i]){if(ai||!hold&&!cue&&!throwing&&!gesture&&!camera&&!honking)ready[i]=true;Remember(i,hold,cue,throwing,gesture,camera);continue;}
    if(!ai&&!n.JugActive&&hold&&!priorHold[i]&&p.released&&!throwing)n.StartJug();
    Vector3 velocity=match.trucks[i].body.linearVelocity;float lateral=Mathf.Abs(Vector3.Dot((velocity-lastVelocity[i])/Mathf.Max(.001f,dt),match.trucks[i].transform.right));lastVelocity[i]=velocity;
    n.TickJug(dt,hold,ai?cue:cue&&!priorCue[i],velocity.magnitude>1,lateral);voices[i].volume=match.settings.sfx*.25f;
    if(n.JugActive){loops[i].SetPaused(false);if(!loops[i].IsPlaying)loops[i].Play();}else loops[i].Stop();
    if(throwing&&(ai||!priorThrow[i])&&Time.time>=nextThrow[i]&&n.FilledJug&&!n.JugActive&&n.TryConsumeItem(TruckTaxiNeedsItems.Find(TruckTaxiNeedsItem.FilledJug).StableId,1)){
     nextThrow[i]=Time.time+throwCooldown;var truck=match.trucks[i];jugs[i].Launch(truck.body.position+truck.transform.forward*4+Vector3.up*2,truck.body.linearVelocity+truck.transform.forward*12+Vector3.up*5);match.Pulse(i,.18f,.1f);
    }
    if(gesture&&!priorGesture[i]&&Time.time>=nextGesture[i]&&gestures[i].RequestFlipOff(out _)){nextGesture[i]=Time.time+4;match.pedestrians?.ReactToHazard(match.trucks[i].body.position,20,5);}
    if(camera&&!priorCamera[i])match.drivingViews.CycleParticipant(i);
    if(honking&&Time.time>=nextHorn[i]&&horn!=null){nextHorn[i]=Time.time+Mathf.Max(.4f,horn.length*.8f);voices[i].PlayOneShot(horn,.7f);match.pedestrians?.ReactToHazard(match.trucks[i].body.position,15,3);}
    Remember(i,hold,cue,throwing,gesture,camera);
   }
   for(int v=0;v<cabJugs.Length;v++){int owner=match.drivingViews.rally.owners[v];bool show=v<match.drivingViews.rally.Humans&&match.drivingViews.ViewMode(v)==2;cabJugs[v].gameObject.SetActive(show);if(show)cabJugs[v].localRotation=Quaternion.Euler(0,0,needs[owner].JugActive?12:0);}
  }
  void ConfigureKeys(){
   var used=new HashSet<Key>{Key.Escape,Key.Enter};for(int i=0;i<match.setup.count;i++){var p=match.setup.players[i];if(p.source==BowlingSource.Keyboard){used.Add(p.forward);used.Add(p.back);used.Add(p.left);used.Add(p.right);}}
   Key[][] preferred={holdKeys,cueKeys,throwKeys,gestureKeys,cameraKeys,hornKeys};var fallback=new[]{Key.F1,Key.F2,Key.F3,Key.F4,Key.F5,Key.F6,Key.F7,Key.F8,Key.F9,Key.F10,Key.F11,Key.F12,Key.Digit1,Key.Digit2,Key.Digit3,Key.Digit4,Key.Digit5,Key.Digit6,Key.Digit7,Key.Digit8,Key.Digit9,Key.Digit0,Key.Numpad1,Key.Numpad2,Key.Numpad3,Key.Numpad4,Key.Numpad5,Key.Numpad6,Key.Numpad7,Key.Numpad8,Key.Numpad9,Key.Numpad0};
   for(int i=0;i<match.setup.count;i++)if(match.setup.players[i].source==BowlingSource.Keyboard)for(int a=0;a<6;a++){var key=preferred[a][Cluster(i)];if(used.Contains(key))foreach(var candidate in fallback)if(!used.Contains(candidate)){key=candidate;break;}keys[i,a]=key;used.Add(key);}
  }
  void Remember(int i,bool hold,bool cue,bool throwing,bool gesture,bool camera){priorHold[i]=hold;priorCue[i]=cue;priorThrow[i]=throwing;priorGesture[i]=gesture;priorCamera[i]=camera;}
  bool EnemyNear(int owner,float radius){for(int i=0;i<match.setup.count;i++)if(i!=owner&&Vector3.Distance(match.trucks[i].body.position,match.trucks[owner].body.position)<radius)return true;return false;}
  public void Blast(Vector3 center,int owner,float radius=0){
   float range=radius>0?radius:blastRadius;
   if(!match.Live)return;var effect=blasts[owner];effect.transform.position=center;effect.Play();voices[owner].PlayOneShot(blastSound,match.settings.sfx);affected.Clear();
   int count=Physics.OverlapSphereNonAlloc(center,range,overlap,~0,QueryTriggerInteraction.Ignore);
   if(count==overlap.Length){Debug.LogWarning("Fare Fight blast collider budget reached; querying the complete rare blast.");foreach(var c in Physics.OverlapSphere(center,range,~0,QueryTriggerInteraction.Ignore))Affect(c,center,range);}else for(int i=0;i<count;i++)Affect(overlap[i],center,range);
   for(int i=0;i<match.setup.count;i++){float distance=Vector3.Distance(center,match.trucks[i].body.worldCenterOfMass);if(distance<8)match.DestroyTruck(i);}
   match.pedestrians?.ReactToHazard(center,range+12,24);match.feedback.Play(0,20,true);
  }
  void Affect(Collider collider,Vector3 center,float range){
   var ped=collider.GetComponentInParent<TruckTaxiPedestrian>();if(ped!=null&&!ped.IsRagdoll){var delta=ped.transform.position-center;ped.TryStrike((delta.normalized+Vector3.up*.6f)*Mathf.Lerp(6,16,1-Mathf.Clamp01(delta.magnitude/range)),collider.ClosestPoint(center));}
   var body=collider.attachedRigidbody;if(body==null||body.isKinematic||!affected.Add(body))return;var direction=body.worldCenterOfMass-center;float falloff=1-Mathf.Clamp01(direction.magnitude/range);Vector3 impulse=(direction.normalized+Vector3.up*.45f)*blastVelocity*falloff;body.AddForce(Vector3.ClampMagnitude(impulse,blastVelocity),ForceMode.VelocityChange);body.AddTorque(Vector3.ClampMagnitude(Vector3.Cross(direction,Vector3.up)*falloff*.13f,2),ForceMode.VelocityChange);
  }
  public void Pause(bool paused){for(int i=0;i<4;i++){loops[i]?.SetPaused(paused);if(paused){if(voices[i]!=null)voices[i].Pause();ready[i]=false;}else if(voices[i]!=null)voices[i].UnPause();}}
  void OnApplicationFocus(bool focus){if(!focus)Pause(true);}void OnDisable(){Pause(true);}void OnDestroy(){foreach(var loop in loops)loop?.Dispose();}
 }
}

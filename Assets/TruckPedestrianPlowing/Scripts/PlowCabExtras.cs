using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.AI;
using CompassNavigatorPro;
using LWS.TruckSports;
using LWS.TruckBowling;
using LWS.TruckTaxi;
namespace LWS.TruckPedestrianPlowing {
 // Per-participant semantics over Taxi's existing needs/QTE and CNP's actual GPS renderer.
 public sealed class PlowCabExtras:MonoBehaviour {
  public PlowMatch match;public TruckTaxiEnvironmentSettings needsSettings;
  public CompassPro[] gps;public Transform[] gpsDestinations,jugVisuals;
  public readonly TruckTaxiDriverNeedsState[] needs=new TruckTaxiDriverNeedsState[8];
  readonly TruckTaxiJugLiquidLoop[] loops=new TruckTaxiJugLiquidLoop[8];readonly AudioSource[] audioSources=new AudioSource[8];
  readonly bool[] priorHold=new bool[8],priorCue=new bool[8],ready=new bool[8];readonly Vector3[] lastVelocity=new Vector3[8];
  readonly NavMeshPath[] paths=new NavMeshPath[4];readonly List<Vector3> route=new List<Vector3>(32);readonly Vector3[] corners=new Vector3[64];float nextMap;
  public Key[] holdKeys={Key.Space,Key.RightShift,Key.R,Key.U,Key.B,Key.M,Key.Numpad0,Key.Insert};
  public Key[] cueKeys={Key.LeftCtrl,Key.RightCtrl,Key.Y,Key.O,Key.N,Key.P,Key.Numpad1,Key.PageUp};
  public bool Enabled=>match.cabExperience;
  void Awake(){for(int i=0;i<8;i++){audioSources[i]=gameObject.AddComponent<AudioSource>();loops[i]=new TruckTaxiJugLiquidLoop(audioSources[i],null);}for(int i=0;i<4;i++)paths[i]=new NavMeshPath();}
  public void ResetNeeds(){for(int i=0;i<8;i++){loops[i].Stop();needs[i]=new TruckTaxiDriverNeedsState(needsSettings,17419+i);needs[i].SetPressure(.55f);needs[i].AddItem(TruckTaxiNeedsItem.EmptyPissJug,8);priorHold[i]=priorCue[i]=ready[i]=false;lastVelocity[i]=Vector3.zero;}}
  int Cluster(int slot)=>Mathf.Clamp(match.KeyboardCluster(slot)-1,0,7);
  public string Prompt(int slot){var p=match.setup.players[slot];if(p.source==BowlingSource.WholeController)return "JUG: HOLD LB · TAP RB AT MARKERS";if(p.source==BowlingSource.LeftStick)return "JUG: HOLD LT · CLICK LEFT STICK AT MARKERS";if(p.source==BowlingSource.RightStick)return "JUG: HOLD RT · CLICK RIGHT STICK AT MARKERS";int k=Cluster(slot);return "JUG: HOLD "+holdKeys[k]+" · TAP "+cueKeys[k]+" AT MARKERS";}
  public bool ValidBindings(out string reason){reason=null;if(!Enabled)return true;for(int i=0;i<match.setup.count;i++){var p=match.setup.players[i];if(p.source!=BowlingSource.Keyboard)continue;int k=Cluster(i);for(int j=0;j<match.setup.count;j++){var q=match.setup.players[j];if(q.source!=BowlingSource.Keyboard)continue;foreach(var key in new[]{holdKeys[k],cueKeys[k]})if(key==q.forward||key==q.back||key==q.left||key==q.right){reason="Jug binding conflicts with keyboard movement in slot "+(j+1);return false;}if(j!=i&&Cluster(j)==k){reason="Keyboard jug cluster is already owned.";return false;}}}return true;}
  public string ShortPrompt(int slot){var p=match.setup.players[slot];return p.source==BowlingSource.WholeController?"JUG LB hold / RB tap":p.source==BowlingSource.LeftStick?"JUG LT / L3":p.source==BowlingSource.RightStick?"JUG RT / R3":"JUG "+holdKeys[Cluster(slot)]+" / "+cueKeys[Cluster(slot)];}
  public string Status(int slot){var n=needs[slot];if(n==null)return "";return n.JugActive?"JUG "+Mathf.RoundToInt(n.JugProgress*100)+"%   "+(n.CueIsOpen(match.trucks[slot].body.linearVelocity.magnitude>1)?"TAP!":"HOLD")+"   "+n.JugCuesCompleted+" / 3": "BLADDER "+Mathf.RoundToInt(n.Pressure*100)+"%"+(n.CabMess?" · SPILLED!":"");}
  public void Tick(float dt){if(!Enabled||needs[0]==null)return;for(int i=0;i<match.setup.count;i++){var n=needs[i];bool live=match.Live&&match.Intro<=0&&!match.ReplayControls;if(!live){loops[i].SetPaused(true);ready[i]=false;continue;}n.AdvanceGameSeconds(dt*10);var p=match.setup.players[i];bool hold=false,cue=false;
    if(p.source==BowlingSource.AI){hold=true;if(!n.JugActive&&n.Pressure>.7f)n.StartJug();cue=n.CueIsOpen(match.trucks[i].body.linearVelocity.magnitude>1);}
    else if(p.source==BowlingSource.Keyboard){int k=Cluster(i);hold=SportsDeviceInput.KeyHeld(holdKeys[k]);cue=SportsDeviceInput.KeyHeld(cueKeys[k]);}
    else if(p.pad!=null){hold=p.source==BowlingSource.LeftStick?p.pad.leftTrigger.isPressed:p.source==BowlingSource.RightStick?p.pad.rightTrigger.isPressed:p.pad.leftShoulder.isPressed;cue=p.source==BowlingSource.LeftStick?p.pad.leftStickButton.isPressed:p.source==BowlingSource.RightStick?p.pad.rightStickButton.isPressed:p.pad.rightShoulder.isPressed;}
    bool ai=p.source==BowlingSource.AI;if(!ready[i]){if(ai||!hold&&!cue)ready[i]=true;priorHold[i]=hold;priorCue[i]=cue;continue;}
    if(!n.JugActive&&hold&&!priorHold[i]&&p.released)n.StartJug();Vector3 velocity=match.trucks[i].body.linearVelocity;float lateral=dt>0?Mathf.Abs(Vector3.Dot((velocity-lastVelocity[i])/dt,match.trucks[i].transform.right)):0;lastVelocity[i]=velocity;
    n.TickJug(dt,hold,ai?cue:cue&&!priorCue[i],velocity.magnitude>1,lateral);priorHold[i]=hold;priorCue[i]=cue;
    audioSources[i].volume=match.settings.sfx*.1f;if(n.JugActive){if(!loops[i].IsPlaying){loops[i].SetPaused(false);if(!loops[i].IsPlaying)loops[i].Play();}}else loops[i].Stop();
   }
   for(int v=0;v<4;v++){bool active=Enabled&&match.Running&&!match.Finished&&!match.ReplayControls&&match.option==2&&v<match.cameras.Length;gps[v].gameObject.SetActive(active);jugVisuals[v].gameObject.SetActive(active);if(!active)continue;int slot=match.views.Owner(v);if(slot<0)continue;gps[v].follow=match.trucks[slot].transform;gps[v].cameraMain=match.cameras[v];jugVisuals[v].localRotation=Quaternion.Euler(0,0,needs[slot].JugActive?12:0);}
   if(Time.time<nextMap)return;nextMap=Time.time+.5f;for(int v=0;v<4;v++){if(!gps[v].gameObject.activeInHierarchy)continue;int slot=match.views.Owner(v),target=-1;float closest=float.PositiveInfinity;for(int j=0;j<match.Round.TargetCount;j++){var ped=match.Crowd[j];if(ped.Claimed||ped.role==PlowTargetKind.PenaltyMarshal)continue;float d=(ped.transform.position-match.trucks[slot].body.position).sqrMagnitude;if(d<closest){closest=d;target=j;}}if(target<0){gps[v].ClearRoute();continue;}var destination=match.Crowd[target].transform.position;gpsDestinations[v].position=destination;if(NavMesh.SamplePosition(match.trucks[slot].body.position,out var start,8,-1)&&NavMesh.CalculatePath(start.position,destination,-1,paths[v])){int count=paths[v].GetCornersNonAlloc(corners);route.Clear();for(int k=0;k<count;k++)route.Add(corners[k]);gps[v].SetRoute(route);}}
  }
  public void StopAudio(){foreach(var loop in loops)loop?.SetPaused(true);}
  void OnApplicationFocus(bool focused){if(!focused){StopAudio();for(int i=0;i<ready.Length;i++)ready[i]=false;}}
  void OnDisable(){StopAudio();}void OnDestroy(){foreach(var loop in loops)loop?.Dispose();}
 }
}

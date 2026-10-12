using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
namespace LWS.TruckBilliards
{
 public enum BilliardsSound { Ball, Cushion, Pocket, Strike, Turn, Foul, RackWin, SeriesWin }
 // Six reusable voices; gameplay remains authoritative even without audio/haptics support.
 public sealed class BilliardsFeedback:MonoBehaviour
 {
  public BilliardsSession session;public AudioClip[] clips;
  public int maxVoices=6;public float minimumImpact=.35f,impactGain=.25f,cueGain=.5f;
  AudioSource[] voices;readonly Dictionary<Gamepad,float> motors=new Dictionary<Gamepad,float>();
  public readonly int[] SoundCounts=new int[8];
  public int Played{get;private set;} public int Dropped{get;private set;}public int PulseCount{get;private set;}
  public int LastDeviceId{get;private set;}=-1;public int RunningMotors=>motors.Count;public int ActiveVoices{get{int n=0;if(voices!=null)foreach(var v in voices)if(v.isPlaying)n++;return n;}}
  public float LastVolume{get;private set;} public BilliardsSound LastSound{get;private set;}
  void Awake(){voices=new AudioSource[Mathf.Clamp(maxVoices,1,8)];for(int i=0;i<voices.Length;i++){var source=gameObject.AddComponent<AudioSource>();source.playOnAwake=false;source.spatialBlend=0;source.loop=false;voices[i]=source;}}
  public void ApplyVolume(){if(voices!=null)foreach(var v in voices)v.volume=session.settings.sfx;}
  public void Impact(BilliardsSound kind,float speed){if(speed<minimumImpact)return;Play(kind,(Mathf.Sqrt(Mathf.Clamp01((speed-minimumImpact)/12))*.7f+.015f)*(kind==BilliardsSound.Strike?.6f:kind==BilliardsSound.Cushion?.8f:1)*impactGain,true);}
  public void BreakCue(){Play(BilliardsSound.Strike,.16f,false);if(voices!=null)foreach(var source in voices)if(source.isPlaying&&source.clip==clips[(int)BilliardsSound.Strike])source.pitch=.72f;}
  public void Cue(BilliardsSound kind){Play(kind,(kind==BilliardsSound.SeriesWin?.65f:kind==BilliardsSound.RackWin?.5f:.28f)*cueGain,false);}
  void Play(BilliardsSound kind,float volume,bool vary){
   if(session==null||voices==null||clips==null||(int)kind>=clips.Length||clips[(int)kind]==null)return;
   if(session.settings.master<=0||session.settings.sfx<=0){Dropped++;return;}
   AudioSource source=null;foreach(var v in voices)if(!v.isPlaying){source=v;break;}
   if(source==null){if(vary){Dropped++;return;}source=voices[0];source.Stop();}
   source.clip=clips[(int)kind];source.volume=session.settings.sfx;source.pitch=vary?Random.Range(.96f,1.04f):1;source.PlayOneShot(source.clip,volume);Played++;SoundCounts[(int)kind]++;LastVolume=volume;LastSound=kind;
  }
  public void Pulse(int side,float strength,float seconds){
   if(session==null||!session.InMatch||(session.Screen!=BilliardsScreen.Match&&session.Screen!=BilliardsScreen.Result)||!session.settings.vibration||session.IsAI(side))return;
   var slot=session.players[side];var pad=session.mode==BilliardsMode.HotSeat?session.SharedPad:slot.pad;
   if(slot.control!=BilliardsControl.Gamepad||pad==null||!pad.added||!pad.enabled)return;
   try{pad.SetMotorSpeeds(Mathf.Clamp01(strength),Mathf.Clamp01(strength*.65f));motors[pad]=Time.realtimeSinceStartup+Mathf.Min(seconds,.5f);PulseCount++;LastDeviceId=pad.deviceId;}catch(System.Exception){/* Optional device command; never a gameplay dependency. */}
  }
  public void StopMotors(){foreach(var pair in motors)Stop(pair.Key);motors.Clear();}
  static void Stop(Gamepad pad){try{if(pad!=null)pad.SetMotorSpeeds(0,0);}catch(System.Exception){}}
  public void StopAudio(){if(voices!=null)foreach(var v in voices)v.Stop();}
  void Update(){if(motors.Count==0)return;var expired=new List<Gamepad>();foreach(var pair in motors)if(!pair.Key.added||!pair.Key.enabled||Time.realtimeSinceStartup>=pair.Value)expired.Add(pair.Key);foreach(var pad in expired){Stop(pad);motors.Remove(pad);}}
  void OnEnable(){InputSystem.onDeviceChange+=DeviceChange;}
  void DeviceChange(InputDevice d,InputDeviceChange change){if(change==InputDeviceChange.Disconnected||change==InputDeviceChange.Removed||change==InputDeviceChange.Disabled)StopMotors();}
  void OnDisable(){InputSystem.onDeviceChange-=DeviceChange;StopMotors();StopAudio();}
  void OnApplicationQuit(){StopMotors();}
  void OnApplicationFocus(bool focused){if(!focused)StopMotors();}
  void OnApplicationPause(bool paused){if(paused)StopMotors();}
 }
}




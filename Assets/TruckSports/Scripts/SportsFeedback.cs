using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
namespace LWS.TruckSports {
public sealed class SportsFeedback:MonoBehaviour {
 public SportsSettings settings;public AudioClip[] clips;public int voices=6;public float impactThreshold=.35f,impactGain=.25f;AudioSource[] sources;
 readonly Dictionary<Gamepad,float> motors=new Dictionary<Gamepad,float>();readonly List<Gamepad> expired=new List<Gamepad>(4);
 public int Played{get;private set;}public int PulseCount{get;private set;}public int LastDeviceId{get;private set;}=-1;public int RunningMotors=>motors.Count;
 void Awake(){sources=new AudioSource[Mathf.Clamp(voices,1,8)];for(int i=0;i<sources.Length;i++){sources[i]=gameObject.AddComponent<AudioSource>();sources[i].playOnAwake=false;sources[i].spatialBlend=0;}}
 public void Play(int kind,float speed=0,bool cue=false){if(settings==null||settings.master<=0||settings.sfx<=0||clips==null||kind<0||kind>=clips.Length||clips[kind]==null||(!cue&&speed<impactThreshold))return;AudioSource voice=null;foreach(var s in sources)if(!s.isPlaying){voice=s;break;}if(voice==null){if(!cue)return;voice=sources[0];voice.Stop();}voice.pitch=cue?1:Random.Range(.96f,1.04f);voice.volume=settings.sfx;voice.PlayOneShot(clips[kind],cue?.25f:Mathf.Sqrt(Mathf.Clamp01(speed/16))*impactGain);Played++;}
 public void Pulse(Gamepad p,float strength,float seconds){if(settings==null||!settings.vibration||p==null||!p.added||!p.enabled)return;try{p.SetMotorSpeeds(strength,strength*.6f);motors[p]=Time.realtimeSinceStartup+Mathf.Min(.5f,seconds);PulseCount++;LastDeviceId=p.deviceId;}catch(System.Exception){}}
 public void StopMotors(){foreach(var p in motors.Keys)Stop(p);motors.Clear();}static void Stop(Gamepad p){try{p.SetMotorSpeeds(0,0);}catch(System.Exception){}}
 public void StopAll(){StopMotors();if(sources!=null)foreach(var s in sources)s.Stop();}
 void Update(){if(!settings.vibration){StopMotors();return;}expired.Clear();foreach(var p in motors)if(!p.Key.added||!p.Key.enabled||Time.realtimeSinceStartup>=p.Value)expired.Add(p.Key);foreach(var p in expired){Stop(p);motors.Remove(p);}}
 void OnEnable(){InputSystem.onDeviceChange+=Changed;}void OnDisable(){InputSystem.onDeviceChange-=Changed;StopAll();}void Changed(InputDevice d,InputDeviceChange c){if(c==InputDeviceChange.Removed||c==InputDeviceChange.Disconnected||c==InputDeviceChange.Disabled)StopMotors();}
 void OnApplicationFocus(bool focus){if(!focus)StopMotors();}void OnApplicationPause(bool pause){if(pause)StopMotors();}void OnApplicationQuit(){StopAll();}
}
}

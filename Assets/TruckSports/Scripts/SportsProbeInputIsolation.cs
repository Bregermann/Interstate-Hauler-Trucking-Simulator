using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
namespace LWS.TruckSports {
// Only explicit development probes mute physical devices. Normal play is unaffected.
public sealed class SportsProbeInputIsolation:MonoBehaviour {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
 readonly List<InputDevice> muted=new List<InputDevice>();
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]static void Launch(){var args=Environment.GetCommandLineArgs();foreach(var flag in new[]{"-tsCornhole","-tsCornholeAI","-tsCornholeViews","-tsCornholeGuards","-tsPolishPerf","-tsPolishLayouts","-tsPolishVisual","-tsPolishMotion","-tsPolishBaseline","-tsPolishAI","-tsPolish","-tsWallball","-tsWallballAI","-tsArenaUpdate","-tsAirAI","-tsAirCorner","-tsSixAI","-tsHockeyUpdate","-tsBilliardsUpdate","-tsBilliardsAI","-tsBilliardsGroup","-tsBilliardsFeature","-tsBilliardsRegression","-tsSmoke","-tbSmoke","-tsBowlingQuick","-tsBowlingUpdate","-tsBowlingFourAI","-tsBowlingEightAI"})if(Array.IndexOf(args,flag)>=0){var g=new GameObject("Development probe physical input isolation");DontDestroyOnLoad(g);g.AddComponent<SportsProbeInputIsolation>();return;}}
 void Awake(){InputSystem.onDeviceChange+=Changed;foreach(var device in InputSystem.devices)Mute(device);Debug.Log("SPORTS_PROBE_NATIVE_INPUT_MUTED count="+muted.Count);}
 void Mute(InputDevice device){if(!device.native||!device.enabled||muted.Contains(device))return;muted.Add(device);InputSystem.DisableDevice(device);}
 void Changed(InputDevice device,InputDeviceChange change){if(change==InputDeviceChange.Added||change==InputDeviceChange.Reconnected)Mute(device);}
 void OnDestroy(){InputSystem.onDeviceChange-=Changed;foreach(var device in muted)if(device.added&&!device.enabled)InputSystem.EnableDevice(device);}
#endif
}
}

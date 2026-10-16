using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
namespace LWS.TruckSports {
// Only explicit development probes mute physical devices. Normal play is unaffected.
public sealed class SportsProbeInputIsolation:MonoBehaviour {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
 public static bool Active{get;private set;}
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void ResetScope(){Active=false;}
 bool previousBackground;readonly List<InputDevice> muted=new List<InputDevice>();
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]static void Launch(){var args=Environment.GetCommandLineArgs();foreach(var flag in new[]{"-tsFishing","-tsFishingFocused","-tsBungee","-tsBungeeSlice","-tsBungeeFocused","-tbFeatureSmoke","-tbHandoffProbe","-tbLocalSmoke","-tbSmoke","-tsAirAI","-tsAirCorner","-tsArenaUpdate","-tsBase","-tsBaseSplit","-tsBaseView","-tsBilliardsAI","-tsBilliardsFeature","-tsBilliardsGroup","-tsBilliardsRegression","-tsBilliardsUpdate","-tsBog","-tsBogFocused","-tsBowlingEightAI","-tsBowlingFourAI","-tsBowlingQuick","-tsBowlingUpdate","-tsBuzFocused","-tsBuzkashi","-tsCabSteering","-tsCheese","-tsCheeseGuards","-tsCheeseViews","-tsCornhole","-tsCornholeAI","-tsCornholeGuards","-tsCornholeViews","-tsCorrections","-tsCurling","-tsDelivery","-tsDock","-tsExpansionExisting","-tsExpansionIntegration","-tsFocusedSports","-tsHockeyUpdate","-tsIceContact","-tsKeepaway","-tsKeepawayRegression","-tsMattress","-tsMattressFocused","-tsMinefield","-tsMinefieldFocused","-tsParking","-tsParkingFocused","-tsPolish","-tsPolishAI","-tsPolishBaseline","-tsPolishLayouts","-tsPolishMotion","-tsPolishPerf","-tsPolishVisual","-tsSixAI","-tsSmoke","-tsSwing","-tsSwingFocused","-tsTerrier","-tsTerrierDessert","-tsTerrierFinish","-tsTerrierInput","-tsTransport","-tsWallball","-tsWallballAI","-tsWife","-tsWifeFocused"})if(Array.IndexOf(args,flag)>=0){var g=new GameObject("Development probe physical input isolation");DontDestroyOnLoad(g);g.AddComponent<SportsProbeInputIsolation>();return;}}
 void Awake(){previousBackground=Application.runInBackground;Application.runInBackground=true;Active=true;InputSystem.onDeviceChange+=Changed;foreach(var device in InputSystem.devices)Mute(device);Debug.Log("SPORTS_PROBE_NATIVE_INPUT_MUTED count="+muted.Count);}
 void Mute(InputDevice device){if(!device.native||!device.enabled||muted.Contains(device))return;muted.Add(device);InputSystem.DisableDevice(device);}
 void Changed(InputDevice device,InputDeviceChange change){if(change==InputDeviceChange.Added||change==InputDeviceChange.Reconnected)Mute(device);}
 void OnDestroy(){Application.runInBackground=previousBackground;InputSystem.onDeviceChange-=Changed;foreach(var device in muted)if(device.added&&!device.enabled)InputSystem.EnableDevice(device);}
#endif
}
}

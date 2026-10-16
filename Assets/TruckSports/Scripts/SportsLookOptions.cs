using UnityEngine;
namespace LWS.TruckSports {public static class SportsLookOptions {
 const string Prefix="TruckSports.Look.";public static float Sensitivity{get;private set;}=1;public static bool InvertY{get;private set;}
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]static void Boot(){Load();}
 public static void Load(){Sensitivity=Mathf.Clamp(SportsPrefs.GetFloat(Prefix+"Sensitivity",1),.4f,2.5f);InvertY=SportsPrefs.GetInt(Prefix+"InvertY",0)!=0;}
 public static void Set(float sensitivity,bool invert){Sensitivity=Mathf.Clamp(sensitivity,.4f,2.5f);InvertY=invert;SportsPrefs.SetFloat(Prefix+"Sensitivity",Sensitivity);SportsPrefs.SetInt(Prefix+"InvertY",invert?1:0);SportsPrefs.Save();}
 public static void CycleSensitivity(){float[] values={.5f,.75f,1,1.25f,1.5f,2,2.5f};int at=System.Array.FindIndex(values,v=>v>=Sensitivity-.01f);Set(values[(at+1)%values.Length],InvertY);}
}}

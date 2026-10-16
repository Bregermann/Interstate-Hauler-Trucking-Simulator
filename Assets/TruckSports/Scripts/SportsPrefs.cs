using UnityEngine;
namespace LWS.TruckSports {
// Explicit development probes use private keys in the final executable. Normal play forwards unchanged keys.
public static class SportsPrefs {
 static string Key(string key){
#if UNITY_EDITOR || DEVELOPMENT_BUILD
  if(SportsProbeInputIsolation.Active&&key!=null&&!key.StartsWith("Validation.",System.StringComparison.Ordinal))return "Validation."+key;
#endif
  return key;
 }
 public static bool HasKey(string key)=>PlayerPrefs.HasKey(Key(key));
 public static int GetInt(string key,int fallback=0)=>PlayerPrefs.GetInt(Key(key),fallback);
 public static float GetFloat(string key,float fallback=0)=>PlayerPrefs.GetFloat(Key(key),fallback);
 public static string GetString(string key,string fallback="")=>PlayerPrefs.GetString(Key(key),fallback);
 public static void SetInt(string key,int value)=>PlayerPrefs.SetInt(Key(key),value);
 public static void SetFloat(string key,float value)=>PlayerPrefs.SetFloat(Key(key),value);
 public static void SetString(string key,string value)=>PlayerPrefs.SetString(Key(key),value);
 public static void DeleteKey(string key)=>PlayerPrefs.DeleteKey(Key(key));
 public static void Save()=>PlayerPrefs.Save();
}
}

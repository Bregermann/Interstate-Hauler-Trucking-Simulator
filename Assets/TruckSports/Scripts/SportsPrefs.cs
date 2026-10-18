using UnityEngine;
namespace LWS.TruckSports {
// Explicit development probes use private keys in the final executable. Normal play forwards unchanged keys.
public static class SportsPrefs {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
 static string testScope;
 sealed class ValidationLease:System.IDisposable {readonly string previous;public ValidationLease(string value){previous=testScope;testScope="Validation."+value;}public void Dispose(){testScope=previous;}}
 public static System.IDisposable IsolateValidation(string label){if(string.IsNullOrEmpty(label)||!System.Text.RegularExpressions.Regex.IsMatch(label,"^[A-Za-z0-9]+$"))throw new System.ArgumentException("Use an alphanumeric validation scope.");return new ValidationLease(label);}
#endif
 public static string StorageScope {
  get {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
   return testScope??(SportsProbeInputIsolation.Active?"Validation":"Player");
#else
   return "Player";
#endif
  }
 }
 static string Key(string key){
#if UNITY_EDITOR || DEVELOPMENT_BUILD
  if(testScope!=null)return testScope+"."+(key!=null&&key.StartsWith("Validation.",System.StringComparison.Ordinal)?key.Substring(11):key);
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

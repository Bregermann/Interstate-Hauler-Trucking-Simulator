#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;using UnityEngine;
namespace LWS.TruckSports {
// Test-owned preference transaction; production gameplay never creates this scope.
internal sealed class SportsTestPreferences:IDisposable {
 readonly string[] keys={"DockMatch.Setup.v1","KeepawayMatch.Setup.v1","CurlingMatch.Setup.v1","BaseJumpMatch.Setup.v1","TerrierMatch.Setup.v1","Keepaway.Chaos.v1"};readonly string[] values=new string[6];readonly bool[] existed=new bool[6];readonly bool recordExisted;readonly int record;bool disposed;
 public SportsTestPreferences(){for(int i=0;i<keys.Length;i++){existed[i]=PlayerPrefs.HasKey(keys[i]);values[i]=PlayerPrefs.GetString(keys[i]);}recordExisted=PlayerPrefs.HasKey(LWS.TruckDockDiving.DockMatch.RecordKey);record=PlayerPrefs.GetInt(LWS.TruckDockDiving.DockMatch.RecordKey);}
 public void Dispose(){if(disposed)return;disposed=true;for(int i=0;i<keys.Length;i++)if(existed[i])PlayerPrefs.SetString(keys[i],values[i]);else PlayerPrefs.DeleteKey(keys[i]);if(recordExisted)PlayerPrefs.SetInt(LWS.TruckDockDiving.DockMatch.RecordKey,record);else PlayerPrefs.DeleteKey(LWS.TruckDockDiving.DockMatch.RecordKey);PlayerPrefs.Save();}
}}
#endif

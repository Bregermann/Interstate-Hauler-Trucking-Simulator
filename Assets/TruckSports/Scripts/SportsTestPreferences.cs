#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;using UnityEngine;
namespace LWS.TruckSports {
// Test-owned preference transaction; production gameplay never creates this scope.
internal sealed class SportsTestPreferences:IDisposable {
 readonly string[] keys={"DockMatch.Setup.v1","KeepawayMatch.Setup.v1","CurlingMatch.Setup.v1","BaseJumpMatch.Setup.v1","TerrierMatch.Setup.v1","Keepaway.Chaos.v1"};readonly string[] values=new string[6];readonly bool[] existed=new bool[6];readonly bool recordExisted;readonly int record;bool disposed;
 public SportsTestPreferences(){for(int i=0;i<keys.Length;i++){existed[i]=LWS.TruckSports.SportsPrefs.HasKey(keys[i]);values[i]=LWS.TruckSports.SportsPrefs.GetString(keys[i]);}recordExisted=LWS.TruckSports.SportsPrefs.HasKey(LWS.TruckDockDiving.DockMatch.RecordKey);record=LWS.TruckSports.SportsPrefs.GetInt(LWS.TruckDockDiving.DockMatch.RecordKey);}
 public void Dispose(){if(disposed)return;disposed=true;for(int i=0;i<keys.Length;i++)if(existed[i])LWS.TruckSports.SportsPrefs.SetString(keys[i],values[i]);else LWS.TruckSports.SportsPrefs.DeleteKey(keys[i]);if(recordExisted)LWS.TruckSports.SportsPrefs.SetInt(LWS.TruckDockDiving.DockMatch.RecordKey,record);else LWS.TruckSports.SportsPrefs.DeleteKey(LWS.TruckDockDiving.DockMatch.RecordKey);LWS.TruckSports.SportsPrefs.Save();}
}}
#endif

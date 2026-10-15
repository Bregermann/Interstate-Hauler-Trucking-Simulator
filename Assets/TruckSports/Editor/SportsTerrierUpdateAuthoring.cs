using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using LWS.TruckCheeseRolling;
using LWS.TruckCurling;
namespace LWS.TruckSports.Editor {
public static class SportsTerrierUpdateAuthoring {
 public static void UpgradeExisting(){
  EditorSceneManager.OpenScene(LWS.TruckCheeseRolling.Editor.CheeseAuthoring.Scene);
  var cheese=UnityEngine.Object.FindAnyObjectByType<CheeseMatch>();
  foreach(var truck in cheese.trucks){truck.bottomMotorTorque=42;truck.bottomGrip=.92f;truck.bottomBrakeTorque=110;truck.bottomSteerAngle=42;truck.lateralScrub=1.8f;truck.groundYawAuthority=2.1f;truck.uprightAuthority=5.5f;EditorUtility.SetDirty(truck);}
  EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
  LWS.TruckCheeseRolling.Editor.CheeseFixtures.Run();
  EditorSceneManager.OpenScene(LWS.TruckCurling.Editor.CurlingAuthoring.Scene);
  var curl=UnityEngine.Object.FindAnyObjectByType<CurlingMatch>();
  var bank=LWS.TruckCurling.Editor.CurlingAuthoring.BankMaterial();
  foreach(var collider in UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsInactive.Include))if(collider.name=="Outer arena boards")collider.sharedMaterial=bank;
  foreach(var label in UnityEngine.Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include))if(label.name=="Rules and controls")label.text=curl.Help;
  EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
  LWS.TruckCurling.Editor.CurlingAuthoring.Fixtures();AssetDatabase.SaveAssets();AuditDock();
  Debug.Log("TERRIER_UPDATE_EXISTING_AUTHORED");
 }
 public static void AuditDock(){
  EditorSceneManager.OpenScene(LWS.TruckDockDiving.Editor.DockAuthoring.Scene);
  var report=new System.Text.StringBuilder();
  foreach(var renderer in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include)){
   if(renderer.name.IndexOf("Pool",StringComparison.OrdinalIgnoreCase)<0&&renderer.name.IndexOf("water plane",StringComparison.OrdinalIgnoreCase)<0&&renderer.name.IndexOf("Distance mark",StringComparison.OrdinalIgnoreCase)<0)continue;
   report.Append(renderer.name).Append(" bounds=").Append(renderer.bounds).Append(" scale=").Append(renderer.transform.lossyScale).AppendLine();
   foreach(var material in renderer.sharedMaterials){if(material==null){report.AppendLine("NULL MATERIAL");continue;}report.Append("  material=").Append(AssetDatabase.GetAssetPath(material)).Append(" shader=").Append(material.shader.name).Append(" supported=").Append(material.shader.isSupported).Append(" texture=").Append(AssetDatabase.GetAssetPath(material.mainTexture)).Append(" scale=").Append(material.mainTextureScale).AppendLine();}
  }
  Directory.CreateDirectory("Logs/TruckSports/TerrierUpdate");File.WriteAllText("Logs/TruckSports/TerrierUpdate/dock-material-audit.txt",report.ToString());
  Debug.Log("DOCK_MATERIAL_AUDIT_COMPLETE");
 }
}
}

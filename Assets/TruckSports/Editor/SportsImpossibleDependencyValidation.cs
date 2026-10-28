using System;

using System.Collections.Generic;

using System.IO;

using System.Linq;

using System.Security.Cryptography;

using System.Text.RegularExpressions;

using UnityEditor;

using UnityEditor.PackageManager;

using UnityEngine;

namespace LWS.TruckSports.Editor {

 // Read-only AssetDatabase evidence for the existing generated-asset export plan.

 // This neither copies data nor imports/saves/edits any project/vendor asset.

 public static class SportsImpossibleDependencyValidation {

  const string Input="Logs/TruckSports/Impossible07/GeneratedAssetExportPreparation/unresolved-guid-references-v2.json";

  [Serializable]public sealed class Row {public string guid,assetPath,roundtripGuid,mainAssetType,classification,packageName,packageVersion,resolvedFile,sha256;public bool roundtrip,fileExists,directoryExists;}

  [Serializable]public sealed class Report {public string evidence="READ-ONLY NATIVE AssetDatabase mapping; no export/apply/import/save invoked",input,inputSha256;public int expectedGuids=52,mapped,unresolved,roundtripFailures;public Row[] rows;}

  static string Hash(string path){using(var sha=SHA256.Create())using(var stream=File.OpenRead(path))return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-","").ToLowerInvariant();}

  public static void MapPreparedUnresolvedGuids(){MapPreparedUnresolvedGuidsCore(Input,52,null);}
  public static void MapPreparedUnresolvedGuidsV4(){MapPreparedUnresolvedGuidsCore("Logs/TruckSports/Impossible07/DependencyAliasReviewV4/unresolved-guid-references-v4.json",53,"c58f2da670ad6b80eb405338c4e046729bc8dce435ed9f3f3ad40755145cbf9b");}
  public static void MapFinalTransitiveGuidsV8(){MapPreparedUnresolvedGuidsCore("Logs/TruckSports/Impossible07/DependencyAliasReviewV8/current54-native-input.json",54,"ca44df2f21f06b6766f6053b150c05d1efa07e506847ecf3a5bdb0bee1562569");}
  static void MapPreparedUnresolvedGuidsCore(string inputRelative,int expected,string requiredInputSha256){if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new InvalidOperationException("Run read-only mapping in a stopped compiled Editor.");string project=Directory.GetParent(Application.dataPath).FullName;string input=Path.Combine(project,inputRelative);if(requiredInputSha256!=null&&Hash(input)!=requiredInputSha256)throw new InvalidOperationException("Exact pinned V4 GUID-input bytes required; no mapping or asset mutation performed.");string json=File.ReadAllText(input);var keys=Regex.Matches(json,@"(?m)^\s*""([0-9a-fA-F]{32})""\s*:").Cast<Match>().Select(m=>m.Groups[1].Value.ToLowerInvariant()).ToArray();if(keys.Length!=expected||keys.Distinct().Count()!=expected)throw new InvalidOperationException("Exact prepared"+expected+"-GUID input required; preserve reviewed inventory and regenerate a separately reviewed mapping for a changed plan.");var rows=new List<Row>(keys.Length);

   foreach(var guid in keys.OrderBy(g=>g,StringComparer.Ordinal)){string path=AssetDatabase.GUIDToAssetPath(guid);var row=new Row{guid=guid,assetPath=path,classification="UNRESOLVED"};if(!string.IsNullOrEmpty(path)){row.roundtripGuid=AssetDatabase.AssetPathToGUID(path);row.roundtrip=string.Equals(row.roundtripGuid,guid,StringComparison.OrdinalIgnoreCase);var type=AssetDatabase.GetMainAssetTypeAtPath(path);row.mainAssetType=type!=null?type.AssemblyQualifiedName:null;if(path.StartsWith("Assets/",StringComparison.Ordinal)){row.classification="PROJECT_ASSET";row.resolvedFile=Path.GetFullPath(Path.Combine(project,path));}else if(path.StartsWith("Packages/",StringComparison.Ordinal)){row.classification="PACKAGE_ASSET";var package=UnityEditor.PackageManager.PackageInfo.FindForAssetPath(path);if(package!=null){row.packageName=package.name;row.packageVersion=package.version;string prefix="Packages/"+package.name;if(path==prefix||path.StartsWith(prefix+"/",StringComparison.Ordinal))row.resolvedFile=Path.GetFullPath(Path.Combine(package.resolvedPath,path.Substring(prefix.Length).TrimStart('/')));}}else row.classification="NATIVE_INTERNAL_REQUIRES_REVIEW";if(!string.IsNullOrEmpty(row.resolvedFile)){row.fileExists=File.Exists(row.resolvedFile);row.directoryExists=Directory.Exists(row.resolvedFile);if(row.fileExists)row.sha256=Hash(row.resolvedFile);}}rows.Add(row);}

   var report=new Report{expectedGuids=expected,input=inputRelative,inputSha256=Hash(input),mapped=rows.Count(r=>!string.IsNullOrEmpty(r.assetPath)),unresolved=rows.Count(r=>string.IsNullOrEmpty(r.assetPath)),roundtripFailures=rows.Count(r=>!string.IsNullOrEmpty(r.assetPath)&&!r.roundtrip),rows=rows.ToArray()};string dir=Path.Combine(project,"Logs/TruckSports/Impossible07/GeneratedAssetExportPreparation");Directory.CreateDirectory(dir);string output=Path.Combine(dir,"native-guid-map-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff")+".json");if(File.Exists(output))throw new IOException("Preserve existing native mapping receipt.");File.WriteAllText(output,JsonUtility.ToJson(report,true));Debug.Log("IMPOSSIBLE_NATIVE_GUID_MAP "+output+" mapped="+report.mapped+"/"+expected+" unresolved="+report.unresolved+" roundtripFailures="+report.roundtripFailures+"; no assets/scenes/vendor/prefs changed. Internal paths require explicit review, not silent approval.");if(report.unresolved>0||report.roundtripFailures>0)throw new InvalidOperationException("Native GUID mapping retains unresolved/roundtrip review; see durable report. No export performed.");

  }

 }

}


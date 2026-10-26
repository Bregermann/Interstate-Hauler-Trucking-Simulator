using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;

namespace LWS.TruckSports.Editor
{
    // Read-only export evidence from Unity's own dependency resolver, including
    // binary Mesh/Animation assets that a text-GUID scan cannot inspect.
    public static class SportsCarnivalPreservationAudit
    {
        [Serializable] sealed class Entry { public string path, sha256, type; }
        [Serializable] sealed class Report { public string[] seeds; public Entry[] assets; }
        public static void WriteDependencies()
        {
            var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"-tsPreservationManifest");
            if(index<0||index+1>=args.Length)throw new ArgumentException("Provide an explicit -tsPreservationManifest output path.");
            string output=Path.GetFullPath(args[index+1]);
            var seeds=SportsBuilder.SceneList().Concat(new[]{"Assets/TruckSports/Resources/SportsTruckAppearanceCatalog.asset","Assets/TruckSports/Resources/TruckSports/SportsTruckCustomizerMenu.prefab"}).Distinct().ToArray();
            var project=Directory.GetParent(Application.dataPath).FullName;
            var entries=AssetDatabase.GetDependencies(seeds,true).Where(path=>path.StartsWith("Assets/Truck",StringComparison.Ordinal)&&File.Exists(Path.Combine(project,path))).OrderBy(path=>path,StringComparer.Ordinal).Select(path=>
            {
                using(var hash=SHA256.Create())
                    return new Entry{path=path,sha256=BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(Path.Combine(project,path)))).Replace("-","").ToLowerInvariant(),type=AssetDatabase.GetMainAssetTypeAtPath(path)?.FullName??""};
            }).ToArray();
            Directory.CreateDirectory(Path.GetDirectoryName(output));File.WriteAllText(output,JsonUtility.ToJson(new Report{seeds=seeds,assets=entries},true));
            Debug.Log("CARNIVAL_PRESERVATION_DEPENDENCIES "+entries.Length+" real owned asset files from Unity AssetDatabase; no scenes, assets or preferences modified.");
        }
    }
}

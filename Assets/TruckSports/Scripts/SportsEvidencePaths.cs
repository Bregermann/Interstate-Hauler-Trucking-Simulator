#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.IO;
using UnityEngine;
namespace LWS.TruckSports {
public static class SportsEvidencePaths {
 public static string Folder(string leaf){string root=Path.GetFullPath(Path.Combine(Application.dataPath,Application.isEditor?"..":"../../.."));string folder=Path.Combine(root,"Logs","TruckSports","TerrierUpdate",leaf);Directory.CreateDirectory(folder);return folder;}
}
}
#endif

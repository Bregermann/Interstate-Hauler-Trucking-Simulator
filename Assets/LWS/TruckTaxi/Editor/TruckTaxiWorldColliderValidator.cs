using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LWS.TruckTaxi.Editor
{
    public static class TruckTaxiWorldColliderValidator
    {
        [MenuItem("Truck Taxi/Validation/Review Solid World Colliders")]
        public static void ReviewWorld()
        {
            if(EditorApplication.isPlaying) throw new InvalidOperationException("Use runtime collider gizmos during Play Mode.");
            var report=new StringBuilder("# Solid world collider review\n\nReport only. Triggers, disabled objects and legitimate visible terrain are excluded.\n\n");
            int reviewed=0, flagged=0;
            var paths=new List<string>();
            foreach(var guid in AssetDatabase.FindAssets("t:Scene",new[]{"Assets/LWS/TruckTaxi/Regional/Scenes"})) paths.Add(AssetDatabase.GUIDToAssetPath(guid));
            paths.Sort(StringComparer.Ordinal);
            foreach(var path in paths)
            {
                var scene=SceneManager.GetSceneByPath(path); bool opened=!scene.isLoaded;
                if(opened) scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
                try
                {
                    report.AppendLine("## "+Path.GetFileNameWithoutExtension(path));
                    int count=0;
                    foreach(var root in scene.GetRootGameObjects()) foreach(var collider in root.GetComponentsInChildren<Collider>(false))
                    {
                        if(collider.isTrigger || !collider.enabled) continue;
                        reviewed++;
                        string reason=TruckTaxiWorldColliderReview.Suspicion(collider);
                        if(reason==null) continue;
                        flagged++; count++;
                        report.AppendLine("- "+TruckTaxiWorldColliderReview.Hierarchy(collider.transform)+": "+reason+"; bounds "+collider.bounds);
                    }
                    if(count==0) report.AppendLine("No suspicious solids detected.");
                }
                finally { if(opened) EditorSceneManager.CloseScene(scene,true); }
            }
            report.AppendLine($"\nReviewed {reviewed}; flagged {flagged}. This is not a human drive-around collision acceptance pass.");
            Directory.CreateDirectory("Logs"); File.WriteAllText("Logs/TruckTaxiSolidWorldColliderReview.md",report.ToString());
            Debug.Log($"Solid world collider review: {reviewed} checked, {flagged} for manual review. Logs/TruckTaxiSolidWorldColliderReview.md. No geometry changed.");
        }
    }
}

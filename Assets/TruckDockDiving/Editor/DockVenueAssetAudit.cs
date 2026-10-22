using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LWS.TruckDockDiving.Editor
{
    public static class DockVenueAssetAudit
    {
        public static readonly string[] Characters = {
            "Womans/business01_f_highpoly", "Womans/casual03_f_highpoly", "Womans/casual05_f_highpoly", "Womans/casual07_f_highpoly",
            "Womans/sportive02_f_highpoly", "Womans/sportive05_f_highpoly", "Mans/sportive03_m", "Mans/sportive07_m"
        };
        [MenuItem("Truck Sports/Dock Diving/Audit owned venue assets")]
        public static void Audit()
        {
            EditorSceneManager.OpenScene(DockAuthoring.Scene); var m = UnityEngine.Object.FindAnyObjectByType<DockMatch>(); var log = new StringBuilder();
            log.AppendLine("DOCK VENUE READ-ONLY AUDIT; no scene/source assets saved.");
            foreach (var key in Characters)
            {
                string path = "Assets/UTS_FullPack/Models/People/Prefabs/" + key + ".prefab"; var p = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (p == null) { log.AppendLine("MISSING " + path); continue; } var anim = p.GetComponentInChildren<Animator>(true);
                log.AppendLine(path + " avatar=" + (anim == null ? "none" : anim.avatar == null ? "missing" : anim.avatar.isHuman ? "humanoid" : "generic") + " skins=" + p.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length);
                if (anim != null) log.AppendLine("  rig=" + string.Join(",", anim.GetComponentsInChildren<Transform>(true).Take(8).Select(t => t.name)) + " controller=" + AssetDatabase.GetAssetPath(anim.runtimeAnimatorController));
            }
            foreach (string name in new[] { "idle1_f", "cheer_f", "dance_f" }) { string path = "Assets/UTS_FullPack/Models/People/Animations/Girl no Heel/" + name + ".FBX"; var clip = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().FirstOrDefault(c => !c.name.StartsWith("__")); log.AppendLine(path + " exists=" + (clip != null) + (clip == null ? "" : " humanoid=" + clip.humanMotion + " bindings=" + string.Join(",", AnimationUtility.GetCurveBindings(clip).Take(3).Select(b => b.path)))); }
            var crowd = GameObject.Find("UTS spectator crowd · render and animation only"); log.AppendLine("Existing crowd=" + (crowd == null ? "absent" : crowd.transform.childCount.ToString()));
            var surface = GameObject.Find("NWH water plane · URP pool surface"); var water = surface.GetComponent<Renderer>(); log.AppendLine("Water shader=" + water.sharedMaterial.shader.name + " bounds=" + water.bounds + " components=" + string.Join(",", surface.GetComponents<Component>().Select(c => c.GetType().Name)));
            var fx = m.GetComponent<LWS.TruckSports.SportsWaterInteraction>(); log.AppendLine("Existing shared water interaction=" + (fx != null) + " spray=" + (fx == null ? 0 : fx.spray.Length) + " ripples=" + (fx == null ? 0 : fx.ripples.Length));
            log.AppendLine("Reflection probes=" + UnityEngine.Object.FindObjectsByType<ReflectionProbe>(FindObjectsSortMode.None).Length);
            foreach (var filter in UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(f => f.name.Contains("Pool") || f.name.Contains("Run-up") || f.name.Contains("Dock"))) log.AppendLine(filter.name + " mesh=" + filter.sharedMesh?.name + " bounds=" + filter.GetComponent<Renderer>()?.bounds);
            string folder = "Logs/TruckSports/PlaytestRepair"; Directory.CreateDirectory(folder); File.WriteAllText(folder + "/dock-owned-venue-asset-audit.txt", log.ToString()); Debug.Log("DOCK_VENUE_ASSET_AUDIT " + log.ToString());
        }
    }
}

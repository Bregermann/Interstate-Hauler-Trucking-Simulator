using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace LWS.TruckDockDiving.Editor
{
    public static class DockOwnedWaterAudit
    {
        public static void Audit()
        {
            var log=new StringBuilder("Read-only owned GAPH water-splash prefab audit; original sources retained.\n");
            foreach(var guid in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/TruckDockDiving/PrivateLicensedVfx/Source"}))
            {
                string path=AssetDatabase.GUIDToAssetPath(guid);var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);log.AppendLine(path);
                foreach(var ps in prefab.GetComponentsInChildren<ParticleSystem>(true))
                {
                    var r=ps.GetComponent<ParticleSystemRenderer>();var m=ps.main;var shape=ps.shape;
                    log.AppendLine("  "+ps.name+" local="+ps.transform.localPosition+" rotation="+ps.transform.localEulerAngles+" scale="+ps.transform.localScale+" duration="+m.duration+" lifetime="+m.startLifetime.constant+" speed="+m.startSpeed.constant+" size="+m.startSize.constant+" gravity="+m.gravityModifier.constant+" shape="+shape.shapeType+" radius="+shape.radius+" angle="+shape.angle+" renderer="+r.renderMode+" mesh="+AssetDatabase.GetAssetPath(r.mesh)+" bursts="+ps.emission.burstCount);
                    foreach(var mat in r.sharedMaterials)log.AppendLine("    material="+AssetDatabase.GetAssetPath(mat)+" texture="+(mat==null?"MISSING":AssetDatabase.GetAssetPath(mat.mainTexture)));
                }
            }
            Directory.CreateDirectory("Logs/TruckSports/PlaytestRepair");File.WriteAllText("Logs/TruckSports/PlaytestRepair/dock-owned-water-prefabs.txt",log.ToString());Debug.Log(log.ToString());
        }
    }
}

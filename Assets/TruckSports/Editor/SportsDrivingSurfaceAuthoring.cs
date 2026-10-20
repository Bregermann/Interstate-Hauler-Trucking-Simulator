using UnityEngine;
using UnityEditor;
using LWS.TruckTaxi;
namespace LWS.TruckSports.Editor
{
    // Explicit sport-owned driving-surface metadata for the existing
    // Weatherade/Taxi adapter. No snow renderer or track system is duplicated.
    public static class SportsDrivingSurfaceAuthoring
    {
        public static int MarkHierarchy(Transform root)
        {
            int count = 0;
            foreach (var collider in root.GetComponentsInChildren<Collider>(true))
            {
                if (collider.isTrigger || collider.GetComponentInParent<Rigidbody>() != null || collider.GetComponent<Renderer>() == null) continue;
                var marker = collider.GetComponent<TruckTaxiSurface>();
                if (marker == null) marker = Undo.AddComponent<TruckTaxiSurface>(collider.gameObject);
                marker.isRoad = false; EditorUtility.SetDirty(marker); count++;
            }
            return count;
        }
        public static void UpgradeExpansionSurfaces()
        {
            foreach (string folder in new[] { "TruckRedLightGreenLight", "TruckHotPotato", "TruckBalloonBattle", "TruckWhackAMole", "TruckKingOfTheHill" })
            {
                string path = "Assets/" + folder + "/Scenes/" + folder + ".unity"; if (!System.IO.File.Exists(path)) continue;
                var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path); int count = 0;
                // The visible party deck used to be a noncolliding overlay over
                // the base. Its actual top now owns contact/track depth as well.
                foreach (var renderer in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include)) if (renderer.gameObject.scene == scene && renderer.name == "Broad plum battle pad" && renderer.GetComponent<Collider>() == null)
                {
                    var collider = Undo.AddComponent<MeshCollider>(renderer.gameObject); collider.sharedMesh = renderer.GetComponent<MeshFilter>().sharedMesh;
                    collider.sharedMaterial = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>("Assets/TruckBalloonBattle/Materials/Party track contact.asset");
                }
                foreach (var collider in Object.FindObjectsByType<Collider>(FindObjectsInactive.Include))
                {
                    if (collider.gameObject.scene != scene || collider.GetComponentInParent<Rigidbody>() != null || collider.GetComponent<Renderer>() == null) continue;
                    string n = collider.name.ToLowerInvariant();
                    bool selected = n.Contains("continuous") && (n.Contains("base") || n.Contains("arena") || n.Contains("runway") || n.Contains("ground")) || n.Contains("easyroads physical surface") || n.StartsWith("physical stratified") || n == "broad plum battle pad";
                    if (selected) count += MarkHierarchy(collider.transform);
                }
                UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene); Debug.Log("SPORTS_EXPLICIT_WEATHERADE_DRIVING_SURFACES " + folder + " count=" + count);
            }
            AssetDatabase.SaveAssets();
        }
    }
}

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using LWS.TruckSports.Editor;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LWS.TruckHitchSports.Editor
{
    public static class UnhitchedRecoveryShoulderAuthoring
    {
        const string RootName = "Authored physical Unhitched recovery shoulders";
        const string Floor = "Assets/LeartesStudios/Garage Warehouse Environment/Art/Prefabs/SM_Floor_4x6m_01.prefab";
        const string Cone = "Assets/RoadPropsPack/URP/Prefabs/TrafficCone_URP.prefab";
        public static void AuthorSavedUnhitchedRecoveryShoulders()
        {
            EditorSceneManager.OpenScene("Assets/TruckHitchSports/Scenes/Unhitched.unity", OpenSceneMode.Single);
            UpgradeActiveUnhitchedRecoveryShoulders();
        }

        [MenuItem("Truck Sports/Alpha 0.10/Install active Unhitched physical recovery shoulders")]
        public static void UpgradeActiveUnhitchedRecoveryShoulders()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) throw new InvalidOperationException("Stopped compiled Editor required.");
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.path != "Assets/TruckHitchSports/Scenes/Unhitched.unity") throw new InvalidOperationException("Open the saved Unhitched venue explicitly.");
            var match = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<UnhitchedMatch>(true)).Single();
            var existing = match.transform.Find(RootName);
            if (existing != null)
            {
                if (match.recoveryShoulders != null && match.recoveryShoulders.Length == 4 && match.recoveryShoulders.All(l => l != null && l.GuidePoints.Length == 6 && l.DrivingSurfaces.Length > 0))
                { Debug.Log("HH10_UNHITCHED_RECOVERY_SHOULDERS_PRESERVED"); return; }
                throw new InvalidOperationException("Partial/customized recovery shoulders preserved; inspect before repairing.");
            }
            if (match.trucks.Length != 4 || match.trailers.Length != 4 || match.deckSurfaces.Length != 4) throw new InvalidOperationException("Preserve the original four physical surf rigs.");
            if (AssetDatabase.LoadAssetAtPath<GameObject>(Floor) == null || AssetDatabase.LoadAssetAtPath<GameObject>(Cone) == null) throw new InvalidOperationException("Owned production floor modules and road props are required.");
            var road = AssetDatabase.LoadAssetAtPath<Material>("Assets/TruckHitchSports/Materials/Textured haul asphalt.mat");
            var contact = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>("Assets/TruckHitchSports/Materials/Hauling venue contact.asset");
            if (road == null || contact == null) throw new InvalidOperationException("Reuse the existing surf venue material/contact assets.");
            var poses = match.trucks.Select(t => t.transform.localToWorldMatrix).Concat(match.trailers.Select(t => t.transform.localToWorldMatrix)).ToArray();
            var originalRamps = scene.GetRootGameObjects().Where(g => g.name == "Trailer surf launch ramp").Select(g => g.transform.localToWorldMatrix).ToArray();
            var root = new GameObject(RootName).transform; root.SetParent(match.transform, false);
            match.recoveryShoulders = new UnhitchedRecoveryShoulder[4];
            var font = SportsBroadcastAuthoring.Theme().headingFont;
            for (int slot = 0; slot < 4; slot++)
            {
                float laneX = match.trucks[slot].transform.position.x;
                var lane = new GameObject("Physical lower recovery lane " + (slot + 1)).AddComponent<UnhitchedRecoveryShoulder>();
                lane.transform.SetParent(root, false); lane.transform.position = new Vector3(laneX + 5.5f, 0, 0);
                Vector3[] points = { new Vector3(laneX + 5.5f, .05f, -12), new Vector3(laneX + 5.5f, 2.25f, 8), new Vector3(laneX + 5.5f, 2.25f, 50), new Vector3(laneX + 5.5f, 4.5f, 76), new Vector3(laneX + 5.5f, 4.5f, 108), new Vector3(laneX + 5.5f, .05f, 132) };
                lane.GuidePoints = new Transform[points.Length];
                for (int n = 0; n < points.Length; n++) { var marker = new GameObject("Recovery road guide " + n).transform; marker.SetParent(lane.transform, false); marker.position = points[n]; lane.GuidePoints[n] = marker; }
                var surface = SportsEasyRoadsAuthoring.Bake("TruckHitchSports", "Unhitched recovery shoulder " + slot, points, 4, road, contact, true);
                surface.SetParent(lane.transform, true); lane.DrivingSurfaces = surface.GetComponentsInChildren<Collider>(true).Where(c => c.enabled && !c.isTrigger).ToArray();
                Physics.SyncTransforms();
                foreach (var shape in lane.DrivingSurfaces) if (shape.bounds.min.x < laneX + 3.4f) throw new InvalidOperationException("Recovery road must remain outside the original surf deck and its supported straight trajectory.");
                for (int n = 1; n < points.Length; n++)
                {
                    Vector3 delta = points[n] - points[n - 1]; var foundation = new GameObject("Owned concrete recovery-road foundation").transform;
                    foundation.SetParent(lane.transform, false); foundation.SetPositionAndRotation((points[n] + points[n - 1]) * .5f, Quaternion.LookRotation(delta, Vector3.up));
                    // Existing production mesh/materials provide visible solid
                    // foundations; the single EasyRoads mesh owns road contact.
                    LWS.TruckKeepaway.Editor.KeepawayPresentationAuthoring.Fit(foundation, Floor, new Vector3(4, .4f, delta.magnitude), -.42f, true, "TruckHitchSports");
                }
                foreach (float z in new[] { 18f, 48f, 84f, 106f })
                {
                    Vector3 point = lane.PointAtForwardPosition(z);
                    var marker = new GameObject("Owned outer recovery-lane cone").transform; marker.SetParent(lane.transform, false); marker.position = point + Vector3.right * 1.8f;
                    LWS.TruckKeepaway.Editor.KeepawayPresentationAuthoring.Fit(marker, Cone, new Vector3(.6f, .8f, .6f), 0, true, "TruckHitchSports");
                }
                var label = new GameObject("Recovery lane instructions").AddComponent<TextMeshPro>(); label.transform.SetParent(lane.transform, false); label.transform.position = new Vector3(laneX + 8.8f, 4.2f, 25); label.transform.rotation = Quaternion.Euler(0, 180, 0);
                label.font = font; label.text = "RECOVERY LANE\nMATCH SPEED · HOP INWARD"; label.fontSize = 3.8f; label.alignment = TextAlignmentOptions.Center; label.color = new Color(1, .86f, .25f); label.rectTransform.sizeDelta = new Vector2(13, 5);
                match.recoveryShoulders[slot] = lane;
            }
            var after = match.trucks.Select(t => t.transform.localToWorldMatrix).Concat(match.trailers.Select(t => t.transform.localToWorldMatrix)).ToArray();
            if (!poses.SequenceEqual(after) || !originalRamps.SequenceEqual(scene.GetRootGameObjects().Where(g => g.name == "Trailer surf launch ramp").Select(g => g.transform.localToWorldMatrix))) throw new InvalidOperationException("Original actor or stunt-ramp pose changed unexpectedly.");
            EditorUtility.SetDirty(match); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            Debug.Log("HH10_UNHITCHED_RECOVERY_SHOULDERS_ADDED four owned-surface/EasyRoads physical lower lanes, original rigs/ramps/starts preserved; real gameplay acceptance pending.");
        }
    }
}
#endif

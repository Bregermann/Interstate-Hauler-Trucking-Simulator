#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using LWS.TruckSports.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LWS.TruckHitchSports.Editor
{
    public static class UnhitchedRecoveryWidthAuthoring
    {
        const string ScenePath = "Assets/TruckHitchSports/Scenes/Unhitched.unity";
        const string RootName = "Authored physical Unhitched recovery shoulders";
        static readonly float[] Forward = { -12, 8, 50, 76, 108, 132 };
        static readonly float[] Height = { .05f, 2.25f, 2.25f, 4.5f, 4.5f, .05f };
        static string WideName(int slot) => "Unhitched outer-wide recovery shoulder " + slot;

        public static void AuthorSavedUnhitchedOuterRecoveryWidth()
        {
            RequireStopped();
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            WidenActiveUnhitchedOuterRecoveryRoads();
        }

        [MenuItem("Truck Sports/Alpha 0.10/Widen active Unhitched outer recovery roads")]
        public static void WidenActiveUnhitchedOuterRecoveryRoads()
        {
            RequireStopped();
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.path != ScenePath) throw new InvalidOperationException("Open the saved Unhitched venue explicitly.");
            var match = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<UnhitchedMatch>(true)).Single();
            if (match.recoveryShoulders == null || match.recoveryShoulders.Length != 4 || match.trucks.Length != 4 || match.trailers.Length != 4 || match.transform.Find(RootName) == null)
                throw new InvalidOperationException("The four original explicitly authored recovery roads and native rigs are required.");
            var road = AssetDatabase.LoadAssetAtPath<Material>("Assets/TruckHitchSports/Materials/Textured haul asphalt.mat");
            var contact = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>("Assets/TruckHitchSports/Materials/Hauling venue contact.asset");
            if (road == null || contact == null) throw new InvalidOperationException("Preserve the existing recovery-road material and physical contact asset.");
            var alreadyWide = new bool[4];
            // Reject customized geometry before altering any lane or asset.
            for (int slot = 0; slot < 4; slot++)
            {
                var lane = match.recoveryShoulders[slot]; float laneX = match.trucks[slot].transform.position.x;
                if (lane == null || lane.transform.parent != match.transform.Find(RootName) || lane.GuidePoints == null || lane.GuidePoints.Length != 6 || lane.DrivingSurfaces == null || lane.DrivingSurfaces.Length == 0 || lane.DrivingSurfaces.Any(shape => shape == null || !shape.enabled || shape.isTrigger))
                    throw new InvalidOperationException("Missing/customized recovery lane " + slot + " preserved.");
                Bounds bounds = lane.DrivingSurfaces[0].bounds;
                foreach (var shape in lane.DrivingSurfaces) bounds.Encapsulate(shape.bounds);
                bool wide = Mathf.Abs(bounds.size.x - 8) < .1f;
                float centerX = laneX + (wide ? 7.5f : 5.5f);
                string sourceName = wide ? WideName(slot) : "Unhitched recovery shoulder " + slot;
                if (Mathf.Abs(bounds.min.x - (laneX + 3.5f)) > .1f || Mathf.Abs(bounds.size.x - (wide ? 8 : 4)) > .1f || Mathf.Abs(lane.transform.position.x - centerX) > .01f || Mathf.Abs(lane.InnerLaunchOffset - (wide ? 0 : -.7f)) > .01f)
                    throw new InvalidOperationException("Unrecognized road width/inner edge/landing strip for lane " + slot + "; preserved.");
                for (int point = 0; point < 6; point++)
                    if (lane.GuidePoints[point] == null || Vector3.Distance(lane.GuidePoints[point].position, new Vector3(centerX, Height[point], Forward[point])) > .01f)
                        throw new InvalidOperationException("Customized road route " + slot + " preserved.");
                foreach (var shape in lane.DrivingSurfaces)
                    if (!(shape is MeshCollider mesh) || mesh.sharedMesh == null || !AssetDatabase.GetAssetPath(mesh.sharedMesh).StartsWith("Assets/TruckHitchSports/Models/" + sourceName + " EasyRoads mesh ", StringComparison.Ordinal) || shape.transform.parent == null || shape.transform.parent.name != sourceName + " · EasyRoads generated course")
                        throw new InvalidOperationException("Unrecognized original road mesh " + slot + " preserved.");
                var foundations = lane.transform.Cast<Transform>().Where(child => child.name == "Owned concrete recovery-road foundation").ToArray();
                if (foundations.Length != 5 || foundations.Any(child => child.GetComponentsInChildren<Collider>(true).Length != 0 || Mathf.Abs(child.localScale.x - (wide ? 2 : 1)) > .01f))
                    throw new InvalidOperationException("Customized production foundations " + slot + " preserved.");
                alreadyWide[slot] = wide;
            }
            if (alreadyWide.All(value => value)) { Debug.Log("HH10_UNHITCHED_OUTER_WIDTH_PRESERVED width8 inner+3.5 outer+11.5 landing+7.5"); return; }
            var protectedTransforms = match.trucks.Select(value => value.transform).Concat(match.trailers.Select(value => value.transform))
                .Concat(scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Camera>(true)).Select(camera => camera.transform))
                .Concat(scene.GetRootGameObjects().Where(root => root.name == "Trailer surf launch ramp").Select(root => root.transform)).ToArray();
            var protectedMatrices = protectedTransforms.Select(value => value.localToWorldMatrix).ToArray();
            var baked = new List<Transform>();
            try
            {
                for (int slot = 0; slot < 4; slot++)
                {
                    if (alreadyWide[slot]) { baked.Add(null); continue; }
                    float centerX = match.trucks[slot].transform.position.x + 7.5f;
                    var points = Forward.Select((z, point) => new Vector3(centerX, Height[point], z)).ToArray();
                    // New immutable paths: never overwrite the original narrow
                    // meshes, a copied mesh or an unrelated vendor asset.
                    var replacement = SportsEasyRoadsAuthoring.Bake("TruckHitchSports", WideName(slot), points, 8, road, contact, true);
                    baked.Add(replacement); Physics.SyncTransforms();
                    var shapes = replacement.GetComponentsInChildren<Collider>(true);
                    Bounds bounds = shapes[0].bounds; foreach (var shape in shapes) bounds.Encapsulate(shape.bounds);
                    if (Mathf.Abs(bounds.min.x - (centerX - 4)) > .1f || Mathf.Abs(bounds.max.x - (centerX + 4)) > .1f)
                        throw new InvalidOperationException("Generated road must retain the exact original inner clearance and finite outer width.");
                }
                for (int slot = 0; slot < 4; slot++)
                {
                    if (alreadyWide[slot]) continue;
                    var lane = match.recoveryShoulders[slot];
                    var originals = lane.DrivingSurfaces.Select(shape => shape.transform.parent.gameObject).Distinct().ToArray();
                    lane.transform.position += Vector3.right * 2;
                    lane.InnerLaunchOffset = 0; // Actual wide-road landing center at original lane +7.5.
                    foreach (Transform child in lane.transform)
                    {
                        if (child.name == "Owned concrete recovery-road foundation") { var scale = child.localScale; scale.x *= 2; child.localScale = scale; }
                        else if (child.name == "Owned outer recovery-lane cone") child.position += Vector3.right * 2;
                    }
                    baked[slot].SetParent(lane.transform, true);
                    lane.DrivingSurfaces = baked[slot].GetComponentsInChildren<Collider>(true).Where(shape => shape.enabled && !shape.isTrigger).ToArray();
                    foreach (var original in originals) UnityEngine.Object.DestroyImmediate(original);
                    EditorUtility.SetDirty(lane);
                }
                Physics.SyncTransforms();
                if (!protectedMatrices.SequenceEqual(protectedTransforms.Select(value => value.localToWorldMatrix))) throw new InvalidOperationException("Original native rigs, cameras or surf ramp transforms changed unexpectedly.");
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
                Debug.Log("HH10_UNHITCHED_OUTER_WIDTH_ADDED physicalWidth8 inner+3.5 outer+11.5 landing+7.5; original meshes retained; native actors/ramps/cameras unchanged; recovery gameplay still requires natural validation.");
            }
            catch
            {
                foreach (var replacement in baked) if (replacement != null && replacement.parent == null) UnityEngine.Object.DestroyImmediate(replacement.gameObject);
                throw;
            }
        }
        static void RequireStopped()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) throw new InvalidOperationException("Stopped compiled Editor required.");
        }
    }
}
#endif

#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LWS.TruckHitchSports.Editor
{
    public static class TrailerblazersPrecisionPadAuthoring
    {
        const string ScenePath = "Assets/TruckHitchSports/Scenes/Trailerblazers.unity";
        public static void AuthorSavedTrailerblazersPrecisionPads()
        {
            RequireStoppedEditor();
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            RepairActiveTrailerblazersPrecisionPadLanes();
        }

        [MenuItem("Truck Sports/Alpha 0.10/Repair active Trailerblazers precision pad lanes")]
        public static void RepairActiveTrailerblazersPrecisionPadLanes()
        {
            RequireStoppedEditor();
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.path != ScenePath) throw new InvalidOperationException("Open the saved Trailerblazers venue explicitly.");
            var match = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<TrailerblazersMatch>(true)).Single();
            if (match.trucks == null || match.trucks.Length != 4 || match.trailers == null || match.trailers.Length != 4 || match.landingPads == null || match.landingPads.Length != 4 || match.landingPads.Distinct().Count() != 4)
                throw new InvalidOperationException("The four original independent rocket rigs and landing pads are required.");
            // Validate all four before making any change. Preserve customized
            // targets rather than guessing where a modified venue should land.
            for (int slot = 0; slot < 4; slot++)
            {
                var pad = match.landingPads[slot];
                if (pad == null || match.trucks[slot] == null || match.trailers[slot] == null || pad.gameObject.scene != scene || pad.name != "Precision recovery pad " + (slot + 1))
                    throw new InvalidOperationException("The original assigned precision pad identity is missing for slot " + slot + ".");
                var shape = pad.GetComponent<BoxCollider>(); var position = pad.position;
                float laneX = match.trucks[slot].transform.position.x;
                if (shape == null || shape.isTrigger || Quaternion.Angle(pad.rotation, Quaternion.identity) > .01f || Mathf.Abs(position.y - .12f) > .01f || Mathf.Abs(position.z - 130) > .01f || Vector3.Distance(Vector3.Scale(shape.size, pad.lossyScale), new Vector3(24, .25f, 24)) > .01f || Mathf.Abs(position.x + 10) > .01f && Mathf.Abs(position.x - laneX) > .01f)
                    throw new InvalidOperationException("Customized pad " + slot + " preserved; only the confirmed original stale X=-10 layout or its repaired lane is recognized.");
            }
            var originalActorPoses = match.trucks.Select(t => t.transform.localToWorldMatrix).Concat(match.trailers.Select(t => t.transform.localToWorldMatrix)).ToArray();
            int changed = 0;
            for (int slot = 0; slot < 4; slot++)
            {
                var pad = match.landingPads[slot]; Vector3 position = pad.position;
                float laneX = match.trucks[slot].transform.position.x;
                if (Mathf.Abs(position.x - laneX) <= .0001f) continue;
                position.x = laneX; pad.position = position; EditorUtility.SetDirty(pad); changed++;
            }
            Physics.SyncTransforms();
            if (!originalActorPoses.SequenceEqual(match.trucks.Select(t => t.transform.localToWorldMatrix).Concat(match.trailers.Select(t => t.transform.localToWorldMatrix))))
                throw new InvalidOperationException("Native actor transforms must remain unchanged while repairing target centers.");
            if (changed > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            }
            Debug.Log("HH10_ROCKET_PRECISION_PAD_LANES changed=" + changed + " laneX=[" + string.Join(",", match.landingPads.Select(p => p.position.x.ToString("F2"))) + "] original Y/Z, dimensions, colliders, materials, native rigs and physical starts preserved.");
        }

        static void RequireStoppedEditor()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) throw new InvalidOperationException("Stopped compiled Editor required.");
        }
    }
}
#endif

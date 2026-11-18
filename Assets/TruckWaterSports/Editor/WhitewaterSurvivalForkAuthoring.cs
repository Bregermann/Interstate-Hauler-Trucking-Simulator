using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using LWS.TruckSports.Editor;

namespace LWS.TruckWaterSports.Editor
{
    public static class WhitewaterSurvivalForkAuthoring
    {
        const string ScenePath = "Assets/TruckWaterSports/Scenes/WhitewaterWipeout.unity";
        const string Boulder = "Assets/TruckFishing/Prefabs/River boulder 0.prefab";

        [MenuItem("Truck Sports/Alpha 0.10/Install active Whitewater survival rock notch")]
        public static void InstallActiveNotch() => UpgradeActiveScene();

        public static void InstallSavedNotch()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath);
            UpgradeActiveScene();
            if (scene.isDirty && !EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException("Could not save the Whitewater Survival equipment.");
            AssetDatabase.SaveAssets();
        }


        public static WhitewaterSurvivalFork UpgradeActiveScene()
        {
            if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before authoring the survival river route.");
            var scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath) throw new InvalidOperationException("Open the existing Whitewater Wipeout venue.");
            var match = UnityEngine.Object.FindAnyObjectByType<WhitewaterWipeoutMatch>();
            if (match == null || match.gameObject.scene != scene || match.course == null || match.course.lake || match.course.stations.Length < 22 || match.course.width < 28)
                throw new InvalidOperationException("Preserve the complete original 29m Whitewater river.");
            if (match.survivalFork != null) return match.survivalFork;
            if (match.GetComponent<WhitewaterSurvivalFork>() != null)
                throw new InvalidOperationException("An unbound existing survival route must be reviewed, not silently duplicated.");
            var field = match.course.whirlpoolField;
            if (field == null || (field.variants & RiverWhirlpoolVariants.Survival) == 0)
                throw new InvalidOperationException("Install the existing authored Survival whirlpool first. No substitute water/current system is created.");
            if (AssetDatabase.LoadAssetAtPath<GameObject>(Boulder) == null)
                throw new InvalidOperationException("The existing production Fishing river-boulder prefab is required.");
            Vector3 center = Vector3.Lerp(match.course.Point(20), match.course.Point(21), .5f);
            Vector3 forward = Vector3.ProjectOnPlane(match.course.Point(21) - match.course.Point(20), Vector3.up).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            bool actualWhirlpool = false;
            foreach (var vortex in field.whirlpools) if (vortex != null && vortex.center != null &&
                Vector3.ProjectOnPlane(vortex.center.position - center, Vector3.up).magnitude < 8 && vortex.radius > 5) actualWhirlpool = true;
            if (!actualWhirlpool || forward.sqrMagnitude < .9f || Mathf.Abs(match.course.Point(21).y - match.course.Point(20).y) > 2)
                throw new InvalidOperationException("The intended lower whirlpool reach and shallow grade must remain intact.");

            var contact = SportsEventAuthoring.Contact("TruckWaterSports", "Survival rock notch contact", .2f, .02f);
            var equipment = new GameObject("Survival-only lower whirlpool rock notch").transform;
            equipment.SetParent(match.transform, false);
            WhitewaterSurvivalFork fork = null;
            try
            {
                var inner = Rock(equipment, "Visible inner notch island", center + right * -6.8f, forward, new Vector3(2.8f,1.8f,10), contact);
                var outer = Rock(equipment, "Visible bank-side notch boulder", center + right * -13.2f, forward, new Vector3(2,1.8f,8), contact);
                Vector2 inside = LateralExtents(inner, center, right), outside = LateralExtents(outer, center, right);
                float gap = inside.x - outside.y;
                // Match actual model vertices/colliders, not stale renderer AABBs.
                // Preserve an unobstructed ±4m original central route and keep
                // both boulders within the existing real river footprint.
                if (inside.y > -4.25f || outside.x < -match.course.width * .5f + .2f || gap < 3.7f || gap > 5)
                    throw new InvalidOperationException("Owned boulder geometry does not preserve the central recovery channel and a kayak-sized narrow passage.");
                float narrowCenter = (inside.x + outside.y) * .5f;
                Physics.SyncTransforms();
                RequireSupportedPassage(match, center, forward, right, narrowCenter, "narrow rock notch");
                RequireSupportedPassage(match, center, forward, right, 0, "original central recovery channel");
                fork = match.gameObject.AddComponent<WhitewaterSurvivalFork>(); fork.match = match; fork.equipment = equipment;
                fork.center = center; fork.forward = forward; fork.right = right;
                fork.narrowOffset = (inside.x + outside.y) * .5f; fork.narrowHalfWidth = gap * .5f;
                equipment.gameObject.SetActive(false); match.survivalFork = fork;
                EditorUtility.SetDirty(match); EditorUtility.SetDirty(fork); EditorSceneManager.MarkSceneDirty(scene);
                Debug.Log("WHITEWATER_SURVIVAL_NOTCH_AUTHORED: real owned boulder meshes/colliders, narrow gap=" + gap.ToString("F2") +
                    "m, original central ±4m route protected. Existing lower whirlpool/current/boat support unchanged; saved installation and natural acceptance are separate.");
                return fork;
            }
            catch
            {
                if (fork != null) UnityEngine.Object.DestroyImmediate(fork);
                UnityEngine.Object.DestroyImmediate(equipment.gameObject); throw;
            }
        }

        static Transform Rock(Transform parent, string name, Vector3 position, Vector3 forward, Vector3 size, PhysicsMaterial contact)
        {
            var root = new GameObject(name).transform; root.SetParent(parent, false); root.position = position; root.rotation = Quaternion.LookRotation(forward);
            LWS.TruckKeepaway.Editor.KeepawayPresentationAuthoring.Fit(root, Boulder, size, -.7f, true, "TruckWaterSports");
            foreach (var filter in root.GetComponentsInChildren<MeshFilter>(true)) if (filter.sharedMesh != null)
            {
                var collider = filter.gameObject.AddComponent<MeshCollider>(); collider.sharedMesh = filter.sharedMesh; collider.sharedMaterial = contact;
            }
            return root;
        }

        static Vector2 LateralExtents(Transform root, Vector3 center, Vector3 right)
        {
            float min = float.PositiveInfinity, max = float.NegativeInfinity;
            foreach (var filter in root.GetComponentsInChildren<MeshFilter>(true)) if (filter.sharedMesh != null)
                foreach (var vertex in filter.sharedMesh.vertices)
                {
                    float lateral = Vector3.Dot(filter.transform.TransformPoint(vertex) - center, right);
                    min = Mathf.Min(min, lateral); max = Mathf.Max(max, lateral);
                }
            if (float.IsInfinity(min) || float.IsInfinity(max)) throw new InvalidOperationException("Production boulder requires readable actual mesh vertices.");
            return new Vector2(min, max);
        }

        static void RequireSupportedPassage(WhitewaterWipeoutMatch match, Vector3 center, Vector3 forward, Vector3 right, float lateral, string label)
        {
            Vector3 start = center + right * lateral - forward * 24, end = center + right * lateral + forward * 24;
            start.y = match.course.SurfacePointXZ(start).y - .05f; end.y = match.course.SurfacePointXZ(end).y - .05f;
            Vector3 delta = end - start; Quaternion rotation = Quaternion.LookRotation(forward, Vector3.up);
            // This stopped-scene query includes existing riverbanks/boulders as
            // well as the new visible rocks. It prevents saving a nominal gate
            // whose real hull-sized passage is already blocked by owned scenery.
            Vector3 half = new Vector3(1.7f,.35f,1);
            foreach (var collider in Physics.OverlapBox(start, half, rotation, ~0, QueryTriggerInteraction.Ignore))
                if (collider.GetComponentInParent<WaterVessel>(true) == null)
                    throw new InvalidOperationException("Preserve a supported " + label + "; entry hull overlaps actual collider: " + collider.name);
            foreach (var hit in Physics.BoxCastAll(start, half, delta.normalized, rotation, delta.magnitude, ~0, QueryTriggerInteraction.Ignore))
                if (hit.collider.GetComponentInParent<WaterVessel>(true) == null)
                    throw new InvalidOperationException("The " + label + " is physically blocked by " + hit.collider.name + " at " + hit.point + ". No route was installed.");
        }
    }
}

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
            if(Application.isPlaying||EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before opening the saved Whitewater route.");
            var scene = EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
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
            // At the former Z693 midpoint, the station20 left bank and
            // station21 right bank overlap. A western notch there is solid land.
            // The preceding straight reach has the full original 28m physical
            // corridor. Its narrow outlet reaches the EXISTING downstream vortex.
            Vector3 center = match.course.Point(20);
            Vector3 forward = Vector3.forward, right = Vector3.right;
            if(Mathf.Abs(match.course.Point(21).y-center.y)>2||Mathf.Abs(center.z-match.course.Point(19).z-42)>.1f)
                throw new InvalidOperationException("Preserve the authored shallow lower reach and adjacent bank spacing.");
            RiverWhirlpool lower=null;
            foreach(var vortex in field.whirlpools)if(vortex!=null&&vortex.center!=null&&vortex.center.position.z>center.z+10&&
                vortex.center.position.z<center.z+30&&vortex.radius>5){lower=vortex;break;}
            if(lower==null)throw new InvalidOperationException("The existing downstream Survival whirlpool is required.");

            var contact = SportsEventAuthoring.Contact("TruckWaterSports", "Survival rock notch contact", .2f, .02f);
            var equipment = new GameObject("Survival-only supported rock notch and existing whirlpool approach").transform;
            equipment.SetParent(match.transform, false);
            WhitewaterSurvivalFork fork = null;
            try
            {
                var inner = Rock(equipment, "Visible inner notch island", center + right * -6.1f, forward, new Vector3(2,1.8f,4), contact);
                var outer = Rock(equipment, "Visible bank-side notch boulder", center + right * -11.8f, forward, new Vector3(1.6f,1.8f,4), contact);
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
                Vector3 inlet=center-forward*12;
                Vector3 narrowEntry=center+right*narrowCenter-forward*6,narrowExit=center+right*narrowCenter+forward*6;
                Vector3 openEntry=center+right*1.5f-forward*6,openExit=center+right*1.5f+forward*6;
                Vector3 narrowOutlet=new Vector3(center.x+narrowCenter,center.y,lower.center.position.z-.5f);
                Vector3 openOutlet=center+new Vector3(-6,0,13);
                // Check the actual full hull along both direction-independent
                // approaches, not just a nominal point-sized gap between rocks.
                RequireSupportedPath(match,inlet,narrowEntry,"narrow ingress around the upstream bank");
                RequireSupportedPath(match,narrowExit,narrowOutlet,"narrow exit into the original whirlpool");
                RequireSupportedPath(match,inlet,openEntry,"open-channel ingress");
                RequireSupportedPath(match,openExit,openOutlet,"open-channel exit before the downstream bank");
                RequireSupportedPath(match,narrowOutlet,match.course.Point(21),"narrow return to the original course");
                RequireSupportedPath(match,openOutlet,match.course.Point(21),"open return to the original course");
                float outletSurface=match.course.SurfacePointXZ(narrowOutlet).y;
                narrowOutlet.y=outletSurface;
                if(RiverWhirlpoolCurrentField.Sample(lower,lower.center.position,narrowOutlet,outletSurface,0).magnitude<.05f)
                    throw new InvalidOperationException("The actual narrow outlet must encounter the existing whirlpool current.");
                fork = match.gameObject.AddComponent<WhitewaterSurvivalFork>(); fork.match = match; fork.equipment = equipment;
                fork.center = center; fork.forward = forward; fork.right = right;
                fork.narrowOffset = (inside.x + outside.y) * .5f; fork.narrowHalfWidth = gap * .5f;
                fork.stagedWhirlpoolApproach=true;fork.upstreamInlet=inlet;fork.narrowOutlet=narrowOutlet;fork.openOutlet=openOutlet;
                equipment.gameObject.SetActive(false); match.survivalFork = fork;
                EditorUtility.SetDirty(match); EditorUtility.SetDirty(fork); EditorSceneManager.MarkSceneDirty(scene);
                Debug.Log("WHITEWATER_SURVIVAL_NOTCH_AUTHORED: real owned boulder meshes/colliders, narrow gap=" + gap.ToString("F2") +
                    "m, original central ±4m route protected; full-hull ingress/egress checked around both bank corners. Narrow outlet uses the original lower whirlpool/current; boat support unchanged. Saved installation and natural acceptance are separate.");
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
            Vector3 start = center + right * lateral - forward * 12, end = center + right * lateral + forward * 12;
            RequireSupportedPath(match,start,end,label);
        }

        static void RequireSupportedPath(WhitewaterWipeoutMatch match,Vector3 start,Vector3 end,string label)
        {
            start.y = match.course.SurfacePointXZ(start).y - .05f; end.y = match.course.SurfacePointXZ(end).y - .05f;
            for(int sample=0;sample<=16;sample++)
            {
                Vector3 at=Vector3.Lerp(start,end,sample/16f),water=match.course.SurfacePointXZ(at);
                if(Vector3.ProjectOnPlane(at-water,Vector3.up).magnitude>match.course.width*.5f)
                    throw new InvalidOperationException("The "+label+" leaves the actual existing river footprint.");
            }
            Vector3 delta = end - start; Quaternion rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(delta,Vector3.up), Vector3.up);
            // This stopped-scene query includes existing riverbanks/boulders as
            // well as the new visible rocks. It prevents saving a nominal gate
            // whose real hull-sized passage is already blocked by owned scenery.
            Vector3 half = new Vector3(1.7f,.35f,3.05f);
            foreach (var collider in Physics.OverlapBox(start, half, rotation, ~0, QueryTriggerInteraction.Ignore))
                if (collider.GetComponentInParent<WaterVessel>(true) == null)
                    throw new InvalidOperationException("Preserve a supported " + label + "; entry hull overlaps actual collider: " + collider.name);
            foreach (var hit in Physics.BoxCastAll(start, half, delta.normalized, rotation, delta.magnitude, ~0, QueryTriggerInteraction.Ignore))
                if (hit.collider.GetComponentInParent<WaterVessel>(true) == null)
                    throw new InvalidOperationException("The " + label + " is physically blocked by " + hit.collider.name + " at " + hit.point + ". No route was installed.");
        }
    }
}

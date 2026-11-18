using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using LWS.TruckSports;
using LWS.TruckSports.Editor;

namespace LWS.TruckWaterSports.Editor
{
    public static class PaddleChaosHazardsAuthoring
    {
        const string ScenePath = "Assets/TruckWaterSports/Scenes/PaddlePals.unity";
        const string MeshPath = "Assets/TruckWaterSports/Art/Technical river driftwood closed hull.asset";
        const string WoodPath = "Assets/TruckLogRolling/Materials/Owned visible timber endgrain.mat";
        const string RockPath = "Assets/TruckFishing/Prefabs/River boulder 1.prefab";

        [MenuItem("Truck Sports/Alpha 0.10/Install active Paddle Pals Chaos timber hazards")]
        public static PaddleChaosHazards UpgradeActiveScene()
        {
            if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before authoring Canoe Chaos equipment.");
            var scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath) throw new InvalidOperationException("Open the existing saved Paddle Pals venue.");
            var match = UnityEngine.Object.FindAnyObjectByType<PaddlePalsMatch>();
            if (match == null || match.gameObject.scene != scene || match.course == null || match.course.lake || match.course.stations.Length < 8)
                throw new InvalidOperationException("Preserve the original Paddle Pals river, boats and match.");
            var existing = match.GetComponent<PaddleChaosHazards>();
            if (existing != null) return existing; // Retain editable authored equipment.
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath);
            var wood = AssetDatabase.LoadAssetAtPath<Material>(WoodPath);
            var rock = AssetDatabase.LoadAssetAtPath<GameObject>(RockPath);
            if (mesh == null || wood == null || rock == null)
                throw new InvalidOperationException("Existing closed timber hull, owned timber material and production river boulder are required; install the existing technical river fork first.");
            if (mesh.bounds.size.z < 3 || mesh.bounds.size.z > 6)
                throw new InvalidOperationException("Retain the existing four-metre technical driftwood mesh baked in metres.");

            var contact = SportsEventAuthoring.Contact("TruckWaterSports", "Chaos moored timber contact", .3f, .05f);
            var ropeMaterial = SportsEventAuthoring.Mat("TruckWaterSports", "Chaos physical timber mooring rope", new Color(.34f,.22f,.1f), .1f);
            var owner = match.gameObject.AddComponent<PaddleChaosHazards>(); owner.match = match;
            var equipment = new GameObject("Canoe Chaos · physically moored buoyant timber").transform;
            equipment.SetParent(match.transform, false); owner.equipment = equipment;
            owner.logs = new Rigidbody[4]; owner.moorings = new Transform[4]; owner.ropes = new LineRenderer[4];
            for (int i = 0; i < 4; i++)
            {
                int segment = i < 2 ? 4 : 6; int side = i % 2 == 0 ? -1 : 1;
                Vector3 center = Vector3.Lerp(match.course.Point(segment), match.course.Point(segment + 1), .55f);
                Vector3 forward = Vector3.ProjectOnPlane(match.course.Point(segment + 1) - match.course.Point(segment), Vector3.up).normalized;
                Vector3 right = Vector3.Cross(Vector3.up, forward);
                Vector3 station = center + right * side * 6 + forward * (side < 0 ? -3 : 3);
                station.y = match.course.SurfacePoint(station).y;

                var bedrock = new GameObject("Visible riverbed timber mooring " + (i + 1)).transform;
                bedrock.SetParent(equipment, false); bedrock.position = station;
                LWS.TruckKeepaway.Editor.KeepawayPresentationAuthoring.Fit(bedrock, RockPath, new Vector3(2.5f,1.4f,3), -2.6f, false, "TruckWaterSports");
                foreach (var filter in bedrock.GetComponentsInChildren<MeshFilter>(true)) if (filter.sharedMesh != null)
                {
                    var collider = filter.gameObject.AddComponent<MeshCollider>(); collider.sharedMesh = filter.sharedMesh; collider.sharedMaterial = contact;
                }
                var anchor = new GameObject("Actual fixed riverbed tether anchor").transform;
                anchor.SetParent(bedrock, false); anchor.position = station + Vector3.down * 1.25f;
                owner.moorings[i] = anchor;

                var log = new GameObject("Moored native Chaos driftwood " + (i + 1), typeof(MeshFilter), typeof(MeshRenderer));
                log.transform.SetParent(equipment, false); log.transform.position = station + Vector3.down * .05f;
                log.transform.rotation = Quaternion.LookRotation(right + forward * side * .2f, Vector3.up);
                log.GetComponent<MeshFilter>().sharedMesh = mesh; log.GetComponent<MeshRenderer>().sharedMaterial = wood;
                var hull = log.AddComponent<MeshCollider>(); hull.sharedMesh = mesh; hull.convex = true; hull.sharedMaterial = contact;
                var body = log.AddComponent<Rigidbody>(); body.mass = 360; body.linearDamping = .25f; body.angularDamping = .5f;
                body.interpolation = RigidbodyInterpolation.Interpolate; body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                body.solverIterations = 16; body.isKinematic = true; owner.logs[i] = body;
                var floating = log.AddComponent<SportsFloatingBody>(); floating.body = body; floating.proxy = mesh; floating.session = match; floating.waterHeight = station.y;
                var tether = log.AddComponent<SpringJoint>(); tether.autoConfigureConnectedAnchor = false; tether.anchor = Vector3.zero;
                tether.connectedBody = null; tether.connectedAnchor = anchor.position; tether.minDistance = 0; tether.maxDistance = 2.4f; tether.tolerance = .03f;
                tether.spring = body.mass * 12; tether.damper = 1.4f * Mathf.Sqrt(tether.spring * body.mass); tether.enableCollision = true;
                var line = new GameObject("Visible actual timber tether " + (i + 1)).AddComponent<LineRenderer>(); line.transform.SetParent(equipment, false);
                line.sharedMaterial = ropeMaterial; line.useWorldSpace = true; line.positionCount = 2; line.widthMultiplier = .07f; line.numCapVertices = 3;
                line.SetPosition(0, anchor.position); line.SetPosition(1, body.position); owner.ropes[i] = line;
            }
            equipment.gameObject.SetActive(false);
            EditorUtility.SetDirty(owner); EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("Canoe Chaos-only moored timber authored: four real buoyant/colliding logs, native SpringJoint tethers and owned visible boulders. Sprint/Rally/Team equipment remains inactive. Save explicitly; natural contact/gameplay acceptance is unverified.");
            return owner;
        }
    }
}

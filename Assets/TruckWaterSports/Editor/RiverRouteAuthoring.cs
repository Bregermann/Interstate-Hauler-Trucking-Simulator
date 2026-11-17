using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using LWS.TruckSports;
using LWS.TruckSports.Editor;

namespace LWS.TruckWaterSports.Editor
{
    public static class RiverRouteAuthoring
    {
        const string Folder = "TruckWaterSports";
        [MenuItem("Truck Sports/Alpha 0.10/Install active technical river fork")]
        public static void InstallActiveRiverFork() => InstallActiveScene();

        public static RiverForkChallenge InstallActiveScene()
        {
            if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before authoring the river fork.");
            var scene = SceneManager.GetActiveScene();
            if (scene.path != "Assets/TruckWaterSports/Scenes/PaddlePals.unity" && scene.path != "Assets/TruckWaterSports/Scenes/WhitewaterWipeout.unity")
                throw new InvalidOperationException("Open the saved PaddlePals or WhitewaterWipeout scene.");
            var match = UnityEngine.Object.FindAnyObjectByType<WaterMatch>();
            if (match == null || match.gameObject.scene != scene || match.course == null || match.course.lake)
                throw new InvalidOperationException("The original river course and match are required.");
            var existing = match.GetComponent<RiverForkChallenge>();
            if (existing != null) return existing; // Preserve designer-authored choices.

            Vector3 forward = Vector3.ProjectOnPlane(match.course.Point(3) - match.course.Point(2), Vector3.up).normalized;
            if (forward.sqrMagnitude < .9f || Mathf.Abs(match.course.Point(3).y - match.course.Point(2).y) > 2)
                throw new InvalidOperationException("The intended calm upper fork sector must remain intact.");
            var fork = match.gameObject.AddComponent<RiverForkChallenge>();
            fork.match = match; fork.forward = forward; fork.right = Vector3.Cross(Vector3.up, forward);
            fork.center = Vector3.Lerp(match.course.Point(2), match.course.Point(3), .5f);
            fork.laneOffset = Mathf.Min(8.5f, match.course.width * .3f);
            var equipment = new GameObject("Rapid Rally and Slalom · real fork island, logs and shallows").transform;
            equipment.SetParent(match.transform, false); fork.equipment = equipment;
            var contact = SportsEventAuthoring.Contact(Folder, "Technical river rock and timber", .35f, .06f);
            var shortcut = SportsEventAuthoring.Mat(Folder, "Fork narrow shortcut enamel", new Color(1,.47f,.08f), .3f);
            var safe = SportsEventAuthoring.Mat(Folder, "Fork shallow channel enamel", new Color(.12f,.85f,.68f), .3f);

            Rock(equipment, "Physical river fork island", fork.center, new Vector3(3.5f,2,11), -.8f, forward, contact);
            Rock(equipment, "Visible shallow river shelf", fork.center + fork.right * (fork.laneOffset + 2) + forward * 6,
                new Vector3(4,.4f,7), -1.05f, forward, contact);
            for (int lane = -1; lane <= 1; lane += 2)
            {
                float half = lane < 0 ? fork.shortcutHalfWidth : fork.safeHalfWidth;
                for (int edge = -1; edge <= 1; edge += 2)
                {
                    var buoy = GameObject.CreatePrimitive(PrimitiveType.Sphere); buoy.name = lane < 0 ? "Orange shortcut gate buoy" : "Green shallow channel gate buoy";
                    buoy.transform.SetParent(equipment, false);
                    buoy.transform.position = fork.center + fork.right * (lane * fork.laneOffset + edge * half) + Vector3.up * .35f;
                    buoy.transform.localScale = new Vector3(.6f,1,.6f);
                    buoy.GetComponent<Renderer>().sharedMaterial = lane < 0 ? shortcut : safe;
                    UnityEngine.Object.DestroyImmediate(buoy.GetComponent<Collider>());
                }
            }
            fork.logs = new Rigidbody[2];
            var mesh = DriftwoodMesh();
            var wood = AssetDatabase.LoadAssetAtPath<Material>("Assets/TruckLogRolling/Materials/Owned visible timber endgrain.mat");
            if (wood == null) throw new InvalidOperationException("The existing owned timber material is required.");
            for (int i = 0; i < fork.logs.Length; i++)
            {
                var log = new GameObject("Native buoyant shortcut driftwood " + (i + 1), typeof(MeshFilter), typeof(MeshRenderer));
                log.transform.SetParent(equipment, false);
                log.transform.position = fork.center - fork.right * fork.laneOffset + forward * (i == 0 ? -9 : 11);
                log.transform.rotation = Quaternion.LookRotation(fork.right + forward * (i == 0 ? .4f : -.4f));
                log.GetComponent<MeshFilter>().sharedMesh = mesh; log.GetComponent<Renderer>().sharedMaterial = wood;
                var collider = log.AddComponent<MeshCollider>(); collider.sharedMesh = mesh; collider.convex = true; collider.sharedMaterial = contact;
                var body = log.AddComponent<Rigidbody>(); body.mass = 360; body.linearDamping = .25f; body.angularDamping = .5f;
                body.interpolation = RigidbodyInterpolation.Interpolate; body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; body.isKinematic = true;
                var floating = log.AddComponent<SportsFloatingBody>(); floating.body = body; floating.proxy = mesh; floating.session = match;
                floating.waterHeight = match.course.SurfacePoint(body.position).y; fork.logs[i] = body;
            }
            equipment.gameObject.SetActive(false); // Default race remains intact.
            EditorUtility.SetDirty(fork); EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("Technical river fork authored for option1 only; actual island contacts, buoyant logs, alternate gates and shallow shelf. Save explicitly; runtime acceptance remains unverified.");
            return fork;
        }

        static void Rock(Transform parent, string label, Vector3 position, Vector3 size, float bottom, Vector3 forward, PhysicsMaterial contact)
        {
            var root = new GameObject(label).transform; root.SetParent(parent, false); root.position = position; root.rotation = Quaternion.LookRotation(forward);
            LWS.TruckKeepaway.Editor.KeepawayPresentationAuthoring.Fit(root, "Assets/TruckFishing/Prefabs/River boulder 0.prefab", size, bottom, true, Folder);
            var collider = root.gameObject.AddComponent<BoxCollider>(); collider.center = Vector3.up * (bottom + size.y * .5f); collider.size = size; collider.sharedMaterial = contact;
        }

        static Mesh DriftwoodMesh()
        {
            const string path = "Assets/TruckWaterSports/Art/Technical river driftwood closed hull.asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path); if (mesh != null) return mesh;
            var source = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/TruckLogRolling/Models/Owned timber log baked in metres.asset");
            if (source == null) throw new InvalidOperationException("Existing closed timber mesh required.");
            mesh = UnityEngine.Object.Instantiate(source); mesh.name = "Owned timber scaled to physical river driftwood";
            var vertices = mesh.vertices;
            for (int i = 0; i < vertices.Length; i++) vertices[i] = Vector3.Scale(vertices[i], new Vector3(.09f,.09f,.18f));
            mesh.vertices = vertices; mesh.RecalculateBounds(); mesh.RecalculateNormals();
            Directory.CreateDirectory("Assets/TruckWaterSports/Art"); AssetDatabase.CreateAsset(mesh, path); return mesh;
        }
    }
}

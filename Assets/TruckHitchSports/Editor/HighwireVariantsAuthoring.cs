using System;
using System.Collections.Generic;
using System.Linq;
using LWS.TruckSports;
using LWS.TruckSports.Editor;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LWS.TruckHitchSports.Editor
{
    public static class HighwireVariantsAuthoring
    {
        const string RootName = "Authored alternate Highwire challenges";
        const string Scaffold = "Assets/ScaffoldsPack/URP/Prefabs/SM_StairTower.prefab";
        const string RoadBlock = "Assets/RoadPropsPack/URP/Prefabs/RoadBlock02_URP.prefab";
        [MenuItem("Truck Sports/Alpha 0.10/Upgrade active Highwire physical variants")]
        public static void UpgradeActiveHighwireVariants()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) throw new InvalidOperationException("Stopped compiled Editor required.");
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.path != "Assets/TruckHitchSports/Scenes/JackknifeHighwire.unity") throw new InvalidOperationException("Open the saved JackknifeHighwire scene explicitly.");
            var roots = scene.GetRootGameObjects();
            var match = roots.SelectMany(g => g.GetComponentsInChildren<HighwireMatch>(true)).Single();
            var existing = match.transform.Find(RootName);
            if (existing != null)
            {
                if (match.courseVariants.Length == 2 && match.courseVariants.All(c => c != null && c.Points.Length > 4) && match.crossingBarriers.Length == 12)
                { Debug.Log("HH10_HIGHWIRE_VARIANTS_PRESERVED"); return; }
                throw new InvalidOperationException("Partial/customized Highwire additions retained; inspect before explicitly repairing them.");
            }
            if (AssetDatabase.LoadAssetAtPath<GameObject>(Scaffold) == null || AssetDatabase.LoadAssetAtPath<GameObject>(RoadBlock) == null) throw new InvalidOperationException("Installed scaffold and roadblock production assets required.");
            var originalDecks = roots.Where(g => g.name == "Suspended narrow physical roadway").ToArray();
            var originalReceivers = roots.Where(g => g.name == "Finish receiving platform").ToArray();
            if (originalDecks.Length != 20 || originalReceivers.Length != 4) throw new InvalidOperationException("Preserve the original four-lane five-segment course.");
            var originalPoses = match.trucks.Select(t => t.transform.localToWorldMatrix).ToArray();
            var road = originalDecks[0].GetComponent<Renderer>().sharedMaterial;
            var contact = originalDecks[0].GetComponent<Collider>().sharedMaterial;
            var cable = AssetDatabase.LoadAssetAtPath<Material>("Assets/TruckHitchSports/Materials/Safety orange painted steel.mat");
            if (cable == null) cable = road;
            var font = SportsBroadcastAuthoring.Theme().headingFont;
            var root = new GameObject(RootName).transform; root.SetParent(match.transform, false);
            var barriers = new List<HighwireMovingBarrier>();
            var precision = Course(root, match, 1, "Graded mountain precision bridge", new[] { new Vector3(0,60,30), new Vector3(7,63,90), new Vector3(-7,66,155), new Vector3(-4,64,220), new Vector3(0,60,285) }, new[] { false, false, true, false, false }, new bool[5], 210);
            var technical = Course(root, match, 3, "Elevated reversing and articulation bridge", new[] { new Vector3(0,60,30), new Vector3(6,61,95), new Vector3(0,62,165), new Vector3(0,62,230), new Vector3(0,62,195), new Vector3(0,62,250), new Vector3(0,60,285) }, new[] { false, false, false, true, false, false, false }, new[] { false, false, false, false, true, false, false }, 240);
            foreach (var course in new[] { precision, technical })
            {
                var ownBarriers = new List<HighwireMovingBarrier>();
                var centerline = new List<Vector3> { new Vector3(0,60,-45) };
                // The signed reverse checkpoint lies on the same real straight
                // bridge; it is not baked as an overlapping reversed road mesh.
                for (int n = 0; n < course.Points.Length; n++) if (!course.RequiresReverse(n)) centerline.Add(course.Points[n]);
                centerline.Add(new Vector3(0,60,315));
                for (int lane = 0; lane < 4; lane++)
                {
                    Vector3 offset = Vector3.right * lane * 24;
                    var markers = centerline.Select(p => p + offset).ToArray();
                    var surface = SportsEasyRoadsAuthoring.Bake("TruckHitchSports", "Highwire option " + course.Mode + " lane " + lane, markers, 6.5f, road, contact, true);
                    surface.SetParent(course.transform, true);
                    for (int n = 0; n < course.Points.Length; n++)
                    {
                        var point = course.Points[n] + offset;
                        string text = course.RequiresReverse(n) ? "REVERSE · GATE " + (n + 1) : course.RequiresStop(n) ? "STOP BOTH RIGS · BALANCE CHECK" : "GATE " + (n + 1);
                        Label(course.transform, font, text, point + new Vector3(-5, 5, 0), course.RequiresReverse(n) ? new Color(.65f,.86f,1) : new Color(1,.86f,.35f));
                    }
                    for (int n = 1; n < markers.Length - 1; n += 2) Suspension(course.transform, markers[n], cable);
                    Vector3 closed = Vector3.Lerp(course.Points[0], course.Points[1], .5f) + offset;
                    var obstacle = Barrier(course.transform, match, closed, course.Points[1] - course.Points[0], font, lane * 1.2f);
                    ownBarriers.Add(obstacle); barriers.Add(obstacle);
                }
                course.Barriers = ownBarriers.ToArray();
                course.gameObject.SetActive(false);
            }
            var survival = new GameObject("Survival timed crossing gates"); survival.transform.SetParent(root, false); survival.AddComponent<HitchModeObstacle>().ModeMask = 1 << 2;
            for (int lane = 0; lane < 4; lane++) barriers.Add(Barrier(survival.transform, match, Vector3.Lerp(match.route[1], match.route[2], .5f) + Vector3.right * lane * 24, match.route[2] - match.route[1], font, lane * 1.2f));
            survival.SetActive(false);
            foreach (var deck in originalDecks.Concat(originalReceivers))
            {
                var mask = deck.GetComponent<HitchModeObstacle>(); if (mask == null) mask = deck.AddComponent<HitchModeObstacle>(); mask.ModeMask = (1 << 0) | (1 << 2);
            }
            match.courseVariants = new[] { precision, technical }; match.crossingBarriers = barriers.ToArray(); match.originalRoadway = originalDecks.Concat(originalReceivers).ToArray();
            for (int i = 0; i < match.trucks.Length; i++) if (originalPoses[i] != match.trucks[i].transform.localToWorldMatrix) throw new InvalidOperationException("Vehicle start changed unexpectedly.");
            EditorUtility.SetDirty(match); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        }
        static HighwireCourseVariant Course(Transform parent, HighwireMatch match, int mode, string name, Vector3[] points, bool[] stops, bool[] reverses, float seconds)
        {
            var root = new GameObject(name); root.transform.SetParent(parent, false); root.AddComponent<HitchModeObstacle>().ModeMask = 1 << mode;
            var course = root.AddComponent<HighwireCourseVariant>(); course.Mode = mode; course.Points = points; course.StopNodes = stops; course.ReverseNodes = reverses; course.RecommendedSeconds = seconds; return course;
        }
        static void Suspension(Transform parent, Vector3 center, Material cable)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                var tower = new GameObject("Owned scaffold suspension tower").transform; tower.SetParent(parent, false); tower.position = center + new Vector3(side * 5.5f, -12, 0);
                LWS.TruckKeepaway.Editor.KeepawayPresentationAuthoring.Fit(tower, Scaffold, new Vector3(2.5f, 24, 3), 0, true, "TruckHitchSports");
                var line = new GameObject("Visible suspension tension cable").AddComponent<LineRenderer>(); line.transform.SetParent(parent, false); line.useWorldSpace = true; line.positionCount = 3; line.startWidth = line.endWidth = .1f; line.sharedMaterial = cable;
                line.SetPosition(0, center + new Vector3(side * 4, .2f, -18)); line.SetPosition(1, center + new Vector3(side * 5.5f, 12, 0)); line.SetPosition(2, center + new Vector3(side * 4, .2f, 18));
            }
        }
        [MenuItem("Truck Sports/Alpha 0.10/Bind active Highwire actual gate collision leaves")]
        public static void BindActiveHighwireGateLeaves()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) throw new InvalidOperationException("Stopped compiled Editor required.");
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.path != "Assets/TruckHitchSports/Scenes/JackknifeHighwire.unity") throw new InvalidOperationException("Open the saved JackknifeHighwire scene explicitly.");
            var match = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<HighwireMatch>(true)).Single();
            foreach (var gate in match.crossingBarriers)
            {
                if (gate == null || gate.Body == null) throw new InvalidOperationException("Preserve every existing physical gate.");
                var leaves = gate.Body.GetComponentsInChildren<Collider>(true).Where(shape => !shape.isTrigger && shape.GetComponentInParent<Rigidbody>(true) == gate.Body).ToArray();
                if (leaves.Length == 0) throw new InvalidOperationException("An actual solid physical leaf is required.");
                gate.LeafColliders = leaves; EditorUtility.SetDirty(gate);
            }
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        }
        static HighwireMovingBarrier Barrier(Transform parent, HighwireMatch match, Vector3 roadCenter, Vector3 roadDirection, TMP_FontAsset font, float phase)
        {
            Vector3 forward = Vector3.ProjectOnPlane(roadDirection, Vector3.up).normalized, right = Vector3.Cross(Vector3.up, forward);
            var go = new GameObject("Physical owned roadblock crossing"); go.transform.SetParent(parent, false); go.transform.SetPositionAndRotation(roadCenter + Vector3.up * .65f, Quaternion.LookRotation(forward));
            LWS.TruckKeepaway.Editor.KeepawayPresentationAuthoring.Fit(go.transform, RoadBlock, new Vector3(7, 1.3f, 1.1f), -.65f, true, "TruckHitchSports");
            var collider = go.AddComponent<BoxCollider>(); collider.size = new Vector3(7, 1.3f, 1.1f);
            var body = go.AddComponent<Rigidbody>(); body.isKinematic = true; body.mass = 800; body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            var barrier = go.AddComponent<HighwireMovingBarrier>(); barrier.Match = match; barrier.Body = body; barrier.ClosedPosition = go.transform.position; barrier.OpenPosition = go.transform.position + right * 15.5f; barrier.ApproachForward = forward; barrier.PhaseOffset = phase;
            barrier.LeafColliders = go.GetComponentsInChildren<Collider>(true).Where(shape => !shape.isTrigger && shape.GetComponentInParent<Rigidbody>(true) == body).ToArray();
            if (barrier.LeafColliders.Length != 1 || barrier.LeafColliders[0] != collider) throw new InvalidOperationException("Keepaway production-art Fit must strip its source collision before the explicitly sized physical leaf is added.");
            barrier.Signal = Label(parent, font, "WAIT · CROSSING CLOSED", roadCenter - forward * 17 + new Vector3(-5, 5, 0), new Color(1,.36f,.1f));
            return barrier;
        }
        static TMP_Text Label(Transform parent, TMP_FontAsset font, string text, Vector3 point, Color color)
        {
            var go = new GameObject(text); go.transform.SetParent(parent, false); go.transform.position = point;
            var label = go.AddComponent<TextMeshPro>(); label.font = font; label.text = text; label.fontSize = 6; label.richText = false; label.alignment = TextAlignmentOptions.Center; label.rectTransform.sizeDelta = new Vector2(16, 3); label.color = color;
            var billboard = go.AddComponent<SportsPerCameraBillboard>(); billboard.distanceSizing = true; billboard.referenceDistance = 50; billboard.maximumPixelWidth = 240; billboard.maximumPixelHeight = 30; billboard.maximumLabelDistance = 160;
            return label;
        }
    }
}

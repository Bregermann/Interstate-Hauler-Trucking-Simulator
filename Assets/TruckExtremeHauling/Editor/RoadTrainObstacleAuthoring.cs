using System;
using System.Collections.Generic;
using System.Linq;
using LWS.TruckSports.Editor;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LWS.TruckExtremeHauling.Editor
{
    public static class RoadTrainObstacleAuthoring
    {
        const string RootName = "Authored Road Train breakaway slalom";
        const string Block = "Assets/RoadPropsPack/URP/Prefabs/RoadBlock02_URP.prefab";
        const string Cone = "Assets/RoadPropsPack/URP/Prefabs/TrafficCone_URP.prefab";
        [MenuItem("Truck Sports/Alpha 0.10/Add active Road Train obstacle race")]
        public static void UpgradeActiveRoadTrainObstacleRace()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) throw new InvalidOperationException("Stopped compiled Editor required.");
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.path != "Assets/TruckExtremeHauling/Scenes/RoadTrainWreck.unity") throw new InvalidOperationException("Open the saved RoadTrainWreck scene explicitly.");
            var roots = scene.GetRootGameObjects();
            var match = roots.SelectMany(g => g.GetComponentsInChildren<RoadTrainWreckMatch>(true)).Single();
            var existing = match.transform.Find(RootName);
            if (existing != null)
            {
                var authored = existing.GetComponent<RoadTrainObstacleCourse>();
                if (authored != null && match.obstacleCourse == authored && authored.Lanes.Length == 4 && Enumerable.Range(0, 4).All(i => authored.GateCount(i) == 3) && authored.MovableRoadblocks.Length == 48)
                { Debug.Log("HH10_ROAD_TRAIN_OBSTACLE_COURSE_PRESERVED"); return; }
                throw new InvalidOperationException("Partial/customized obstacle additions preserved; inspect before repair.");
            }
            if (match.trucks.Length != 4 || match.objectives.Length != 4) throw new InvalidOperationException("Four original native train lanes are required.");
            foreach (string asset in new[] { Block, Cone }) if (AssetDatabase.LoadAssetAtPath<GameObject>(asset) == null) throw new InvalidOperationException("Installed production road prop required: " + asset);
            var support = roots.Single(g => g.name == "Continuous venue support").GetComponent<BoxCollider>();
            if (support == null || support.transform.rotation != Quaternion.identity) throw new InvalidOperationException("Preserve unrecognized/customized venue support.");
            float top = support.bounds.max.y;
            var poses = match.trucks.Select(t => t.transform.localToWorldMatrix).Concat(match.trailers.Select(t => t.transform.localToWorldMatrix)).ToArray();
            for (int driver = 0; driver < 4; driver++)
            {
                float x = match.trucks[driver].transform.position.x;
                if (!support.bounds.Contains(new Vector3(x - 25, top - .01f, 0)) || !support.bounds.Contains(new Vector3(x + 25, top - .01f, 175))) throw new InvalidOperationException("Existing physical apron must support all obstacle lanes; no automatic floor replacement.");
            }
            var root = new GameObject(RootName).transform; root.SetParent(match.transform, false);
            var course = root.gameObject.AddComponent<RoadTrainObstacleCourse>();
            course.Content = new GameObject("Obstacle Race only · real movable props"); course.Content.transform.SetParent(root, false);
            course.Lanes = new RoadTrainObstacleCourse.Lane[4];
            var bodies = new List<Rigidbody>();
            var font = SportsBroadcastAuthoring.Theme().headingFont;
            var paint = SportsEventAuthoring.Mat("TruckExtremeHauling", "Road Train slalom safety paint", new Color(1, .6f, .05f), .2f);
            var contact = support.sharedMaterial;
            for (int driver = 0; driver < 4; driver++)
            {
                float laneX = match.trucks[driver].transform.position.x;
                var gates = new RoadTrainObstacleCourse.Gate[3];
                for (int n = 0; n < gates.Length; n++)
                {
                    Vector3 center = new Vector3(laneX + (n == 1 ? -10 : 10), top, n * 75);
                    var marker = new GameObject("Player " + (driver + 1) + " · tail gate " + (n + 1)).transform;
                    marker.SetParent(course.Content.transform, false); marker.position = center;
                    gates[n] = new RoadTrainObstacleCourse.Gate { Marker = marker, HalfWidth = 9 };
                    for (int side = -1; side <= 1; side += 2)
                    {
                        bodies.Add(Prop(marker, Block, new Vector3(side * 11, 0, 0), new Vector3(4, 1.4f, 1), 350, contact));
                        bodies.Add(Prop(marker, Cone, new Vector3(side * 9, 0, -2), new Vector3(1, 1.3f, 1), 8, contact));
                    }
                    for (int dash = -4; dash <= 4; dash++)
                    {
                        var stripe = new GameObject("Painted tail passage marking"); stripe.transform.SetParent(marker, false);
                        var line = stripe.AddComponent<LineRenderer>(); line.useWorldSpace = false; line.sharedMaterial = paint; line.startWidth = line.endWidth = .24f; line.positionCount = 2;
                        line.SetPositions(new[] { new Vector3(dash * 2, .035f, -.45f), new Vector3(dash * 2, .035f, .45f) });
                    }
                    var caption = new GameObject("Numbered freight gate caption"); caption.transform.SetParent(marker, false); caption.transform.localPosition = new Vector3(-14, 3.5f, 0); caption.transform.localRotation = Quaternion.Euler(0, 180, 0);
                    var text = caption.AddComponent<TextMeshPro>(); text.font = font; text.fontSize = 5; text.text = "TAIL GATE " + (n + 1); text.color = new Color(1, .84f, .3f); text.alignment = TextAlignmentOptions.Center; text.rectTransform.sizeDelta = new Vector2(15, 3);
                }
                course.Lanes[driver] = new RoadTrainObstacleCourse.Lane { Gates = gates };
            }
            course.MovableRoadblocks = bodies.ToArray(); course.Content.SetActive(false); match.obstacleCourse = course;
            var after = match.trucks.Select(t => t.transform.localToWorldMatrix).Concat(match.trailers.Select(t => t.transform.localToWorldMatrix)).ToArray();
            if (!poses.SequenceEqual(after)) throw new InvalidOperationException("Native starts must remain untouched.");
            EditorUtility.SetDirty(match); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            Debug.Log("HH10_ROAD_TRAIN_OBSTACLE_COURSE_ADDED option=1 gates=12 movableProductionProps=48 existingNativeStartsAndSupportPreserved=true");
        }
        static Rigidbody Prop(Transform parent, string path, Vector3 local, Vector3 size, float mass, PhysicsMaterial contact)
        {
            var root = new GameObject("Physical breakaway " + System.IO.Path.GetFileNameWithoutExtension(path)); root.transform.SetParent(parent, false); root.transform.localPosition = local;
            LWS.TruckKeepaway.Editor.KeepawayPresentationAuthoring.Fit(root.transform, path, size, 0, true, "TruckExtremeHauling");
            var box = root.AddComponent<BoxCollider>(); box.center = Vector3.up * size.y * .5f; box.size = size; box.sharedMaterial = contact;
            var body = root.AddComponent<Rigidbody>(); body.mass = mass; body.isKinematic = true; body.interpolation = RigidbodyInterpolation.Interpolate; body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            return body;
        }
    }
}

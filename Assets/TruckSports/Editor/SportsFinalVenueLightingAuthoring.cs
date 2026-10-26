using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LWS.TruckSports.Editor
{
    public static partial class SportsEnvironmentAuthoring
    {
        // Explicit additive authoring only. Existing artistic lava/ring lights
        // remain independent; this list contains only the new venue fixtures.
        public static void InstallFinalCourseVenueLighting()
        {
            foreach (string name in new[] { "TruckFloorIsLava", "TruckTugOfWar", "TruckCornhole", "TruckTargetLaunch", "TruckTerrierRacing", "TruckBaseJumping", "TruckSkyTow" })
            {
                var scene = EditorSceneManager.OpenScene(SportsBuilder.SceneList().Single(path => Path.GetFileNameWithoutExtension(path) == name), OpenSceneMode.Single);
                var clock = All<SportsMatchTime>(scene).Single();
                if (All<Transform>(scene).Any(item => item.name == "Authored final course night fixtures")) continue;
                if (clock.nightLights.Length != 0) throw new InvalidOperationException("Preserve independently authored venue lamps: " + name);
                var lights = new List<Light>();
                var root = Holder(clock.transform);
                var warm = new Color(1, .94f, .82f); var cool = new Color(.86f, .94f, 1);
                if (name == "TruckFloorIsLava")
                {
                    var terrain = All<Terrain>(scene).Single();
                    foreach (int side in new[] { -1, 1 }) foreach (float z in new[] { -96f, -48f, 0f, 48f, 96f })
                        Pole(root, Ground(terrain, new Vector3(side * 150, 0, z)), new Vector3(side * 48, 42, z), 80, 220, 30000, warm);
                }
                else if (name == "TruckTugOfWar")
                {
                    // The original beveled ledge extends nine metres outboard
                    // at these z coordinates; no lamps are mounted over lava.
                    foreach (int side in new[] { -1, 1 }) foreach (int z in new[] { -7, 7 })
                        Pole(root, new Vector3(side * 34, 0, z), new Vector3(side * 23, 0, 0), 19, 100, 1250, warm);
                }
                else if (name == "TruckCornhole")
                {
                    foreach (var wall in All<MeshRenderer>(scene).Where(item => item.name == "Course isolation wall visible surface"))
                        foreach (float z in new[] { -15f, 20f, 55f, 90f })
                            Mounted(root, new Vector3(wall.bounds.center.x, wall.bounds.max.y, z), new Vector3(Mathf.Round(wall.bounds.center.x / 110) * 110, 0, z), 125, 1250, warm);
                }
                else if (name == "TruckTargetLaunch")
                {
                    foreach (var gantry in All<Transform>(scene).Where(item => item.name.StartsWith("Reused modeled launch gantry ", StringComparison.Ordinal)))
                        Mounted(root, HighestVertex(gantry), gantry.name.EndsWith(" 0", StringComparison.Ordinal) ? new Vector3(0, 34, -8) : new Vector3(0, 24, gantry.position.z + 12), 125, 4000, warm);
                    var target = All<LWS.TruckTargetLaunch.TargetMatch>(scene).Single().target;
                    var platform = target.GetComponentsInChildren<MeshRenderer>().Single(item => item.name == "Modeled beveled physical target platform");
                    foreach (int side in new[] { -1, 1 })
                        Mounted(root, HighestVertex(platform.transform, new Vector3(side * 28, 4, 130)), new Vector3(0, 4, 130), 110, 650, cool);
                }
                else if (name == "TruckTerrierRacing")
                {
                    var course = All<LWS.TruckTerrierRacing.TerrierCourse>(scene).Single(); var deck = Holder(course.geometry);
                    // These stations precede the earliest randomized funnel.
                    // Parent to the original deck, never its runtime-scaled rails.
                    foreach (int side in new[] { -1, 1 }) foreach (float z in new[] { -5f, 30f, 65f, 100f })
                        Pole(deck, course.World(new Vector3(side * 33, 0, z)), course.World(new Vector3(0, 0, z)), 20, 125, 1200, warm);
                }
                else if (name == "TruckBaseJumping")
                {
                    foreach (var rail in All<MeshRenderer>(scene).Where(item => item.name == "Cargo inner rail")) foreach (float z in new[] { -20f, -38f })
                        Mounted(root, new Vector3(rail.bounds.center.x, rail.bounds.max.y, z), new Vector3(0, 259.68f, z), 45, 70, warm);
                    var terrain = All<Terrain>(scene).Single();
                    foreach (int side in new[] { -1, 1 }) foreach (int z in new[] { -45, 45 })
                        Pole(root, Ground(terrain, new Vector3(side * 45, 0, z)), Vector3.zero, 19, 125, 1100, cool);
                }
                else
                {
                    var course = All<LWS.TruckSkyTow.SkyTowCourse>(scene).Single(); var placed = new List<Vector3>();
                    for (float seconds = 0; seconds <= course.duration; seconds += 4)
                    {
                        course.Frame(seconds, 1, out var center, out var rotation);
                        foreach (int side in new[] { -1, 1 })
                        {
                            Vector3? supported = null;
                            foreach (float distance in new[] { 95f, 120f, 150f, 185f })
                            {
                                var point = center + rotation * Vector3.right * side * distance;
                                var origin = course.terrain.transform.position; var size = course.terrain.terrainData.size;
                                if (point.x < origin.x || point.z < origin.z || point.x > origin.x + size.x || point.z > origin.z + size.z) continue;
                                point = Ground(course.terrain, point);
                                if (point.y <= course.water.position.y + 2 || placed.Any(other => Vector3.Distance(other, point) < 45)) continue;
                                supported = point; break;
                            }
                            if (!supported.HasValue) continue;
                            placed.Add(supported.Value); Pole(root, supported.Value, center + Vector3.up * 18, 27, 220, 2000, cool);
                        }
                    }
                    if (placed.Count < 20) throw new InvalidOperationException("Inspect actual supported fjord shores before saving an incomplete lighting layout.");
                }
                if (lights.Count == 0) throw new InvalidOperationException("Expected the original venue supports: " + name);
                clock.nightLights = lights.ToArray(); clock.morningLampMultiplier = .65f; clock.coveredLampMultiplier = .3f;
                // Volcanic venues can intentionally have no accumulation
                // surface. Reuse one where authored; do not invent a second
                // snow authority just to install venue lights.
                clock.lampSnowSurface = All<SportsSnowSurface>(scene).SingleOrDefault(); EditorUtility.SetDirty(clock);
                CompleteCourseLightBudget(scene); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
                Debug.Log("FINAL_COURSE_VENUE_LIGHTS " + name + " lamps=" + lights.Count);
                Transform Holder(Transform parent) { var holder = new GameObject("Authored final course night fixtures").transform; holder.SetParent(parent, false); return holder; }
                void Mounted(Transform parent, Vector3 point, Vector3 target, float range, float power, Color color)
                {
                    var fixture = new GameObject("Supported mounted course fixture " + lights.Count).transform; fixture.SetParent(parent, false); fixture.position = point;
                    fixture.rotation = Quaternion.LookRotation(target - point); InstallMountedLampModel(fixture);
                    LightAt(fixture, point + fixture.forward * .35f, target, range, power, color);
                }
                void Pole(Transform parent, Vector3 point, Vector3 target, float height, float range, float power, Color color)
                {
                    var pole = new GameObject("Supported final course pole " + lights.Count).transform; pole.SetParent(parent, false); pole.position = point; pole.rotation = Quaternion.Euler(0, target.x < point.x ? 180 : 0, 0);
                    LWS.TruckKeepaway.Editor.KeepawayPresentationAuthoring.Fit(pole, "Assets/Toon Gas Station/Prefabs/Streetlight_1C.prefab", new Vector3(100, height, 100), 0, false, "TruckSports");
                    var renderers = pole.GetComponentsInChildren<Renderer>(); var bounds = renderers[0].bounds; foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
                    LightAt(pole, new Vector3(bounds.center.x, bounds.max.y - .4f, bounds.center.z), target, range, power, color);
                }
                void LightAt(Transform parent, Vector3 point, Vector3 target, float range, float power, Color color)
                {
                    var lamp = new GameObject("Budgeted final course night light").AddComponent<Light>(); lamp.transform.SetParent(parent, false); lamp.transform.position = point; lamp.transform.rotation = Quaternion.LookRotation(target - point);
                    lamp.type = LightType.Spot; lamp.range = range; lamp.spotAngle = 112; lamp.innerSpotAngle = 76; lamp.intensity = power; lamp.color = color; lamp.shadows = LightShadows.None; lights.Add(lamp);
                }
            }
            AssetDatabase.SaveAssets();
        }
        public static void CorrectReviewedFinalVenueLighting()
        {
            var scene = EditorSceneManager.OpenScene("Assets/TruckFloorIsLava/Scenes/TruckFloorIsLava.unity", OpenSceneMode.Single);
            var lamps = All<SportsMatchTime>(scene).Single().nightLights;
            if (lamps.Length != 10 || lamps.Any(light => light.name != "Budgeted final course night light" || !Mathf.Approximately(light.intensity, 2000) || light.transform.parent.localScale != Vector3.one))
                throw new InvalidOperationException("Retain customized Lava lights; this correction requires the reviewed initial layout.");
            foreach (var lamp in lamps)
            {
                var pole = lamp.transform.parent; pole.localScale = Vector3.one * 2.5f;
                lamp.transform.rotation = Quaternion.LookRotation(new Vector3(Mathf.Sign(pole.position.x) * 48, 42, pole.position.z) - lamp.transform.position); lamp.intensity = 5000;
                EditorUtility.SetDirty(pole); EditorUtility.SetDirty(lamp);
            }
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            scene = EditorSceneManager.OpenScene("Assets/TruckTargetLaunch/Scenes/TruckTargetLaunch.unity", OpenSceneMode.Single);
            var gantry = All<Transform>(scene).Single(item => item.name == "Reused modeled launch gantry 0"); var mount = HighestVertex(gantry);
            var first = All<SportsMatchTime>(scene).Single().nightLights.Single(light => Vector3.Distance(light.transform.parent.position, mount) < .01f);
            var fixture = first.transform.parent; fixture.rotation = Quaternion.LookRotation(new Vector3(0, 34, -8) - fixture.position); first.transform.position = fixture.position + fixture.forward * .35f; first.transform.rotation = fixture.rotation;
            EditorUtility.SetDirty(fixture); EditorUtility.SetDirty(first.transform); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        }
        public static void CompleteReviewedVolcanicFixtureExposure()
        {
            foreach (string name in new[] { "TruckFloorIsLava", "TruckTargetLaunch" })
            {
                var scene = EditorSceneManager.OpenScene("Assets/" + name + "/Scenes/" + name + ".unity", OpenSceneMode.Single);
                var lamps = All<SportsMatchTime>(scene).Single().nightLights;
                var selected = name == "TruckFloorIsLava" ? lamps : lamps.Where(light => Mathf.Approximately(light.intensity, 1350)).ToArray();
                if (selected.Length != (name == "TruckFloorIsLava" ? 10 : 3) || selected.Any(light => !Mathf.Approximately(light.intensity, name == "TruckFloorIsLava" ? 5000 : 1350)))
                    throw new InvalidOperationException("Preserve customized fixtures; apply exposure only to the reviewed original powers.");
                foreach (var lamp in selected) { lamp.intensity = name == "TruckFloorIsLava" ? 30000 : 4000; EditorUtility.SetDirty(lamp); }
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            }
        }
        static Vector3 HighestVertex(Transform support, Vector3? near = null)
        {
            var points = support.GetComponentsInChildren<MeshFilter>().Where(filter => filter.sharedMesh != null).SelectMany(filter => filter.sharedMesh.vertices.Select(filter.transform.TransformPoint)).ToArray();
            if (points.Length == 0) throw new InvalidOperationException("Retain the actual modeled support geometry.");
            float top = points.Max(point => point.y); var vertices = points.Where(point => point.y >= top - .02f);
            return near.HasValue ? vertices.OrderBy(point => (point - near.Value).sqrMagnitude).First() : vertices.OrderBy(point => (point - support.position).sqrMagnitude).First();
        }
    }
}

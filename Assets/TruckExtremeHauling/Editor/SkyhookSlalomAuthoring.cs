#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LWS.TruckExtremeHauling.Editor
{
    // Explicit and idempotent: this adds mode-specific sporting equipment only.
    // It never rebuilds the scene or changes saved trolley/load/hitch poses.
    public static class SkyhookSlalomAuthoring
    {
        const string ScenePath = "Assets/TruckExtremeHauling/Scenes/SkyhookSprint.unity";
        const string RootName = "Authored suspended slalom openings";
        [MenuItem("LWS/Truck Sports/Alpha 0.10/Upgrade Active Skyhook Slalom")]
        public static void UpgradeActiveSkyhookSlalom()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Upgrade the saved Skyhook scene outside Play Mode.");
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.path != ScenePath) throw new InvalidOperationException("Open the saved SkyhookSprint scene before applying this additive upgrade.");
            var match = UnityEngine.Object.FindAnyObjectByType<SkyhookSprintMatch>();
            if (match == null || match.gameObject.scene != scene) throw new InvalidOperationException("The active scene must own its Skyhook match.");
            var paint = AssetDatabase.LoadAssetAtPath<Material>("Assets/TruckExtremeHauling/Materials/Freight safety orange.mat");
            var steel = AssetDatabase.LoadAssetAtPath<Material>("Assets/TruckExtremeHauling/Materials/Freight tower gunmetal.mat");
            var contact = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>("Assets/TruckExtremeHauling/Materials/Freight concrete.asset");
            if (paint == null || steel == null || contact == null) throw new InvalidOperationException("Reuse the existing authored freight venue materials before adding slalom equipment.");
            var root = Child(match.transform, RootName);
            match.slalomGatesPerLane = 4; match.slalomHalfOpening = new Vector2(3.5f, 5);
            match.slalomGates = new Transform[match.cableCars.Length * match.slalomGatesPerLane];
            match.slalomMarkers = new GameObject[match.cableCars.Length];
            for (int slot = 0; slot < match.cableCars.Length; slot++)
            {
                var car = match.cableCars[slot]; var lane = Child(root, "Slalom lane " + (slot + 1));
                match.slalomMarkers[slot] = lane.gameObject;
                // Native NWH's COM marker is authoritative before its runtime
                // VehicleController initialization has populated Rigidbody COM.
                Transform marker = null;
                foreach (var candidate in car.load.GetComponentsInChildren<Transform>(true)) if (candidate.name == "COM") { marker = candidate; break; }
                Vector3 start = marker != null ? marker.position : car.load.worldCenterOfMass;
                Vector3 direction = Vector3.ProjectOnPlane(car.routeEnd - car.trolley.position, Vector3.up).normalized;
                if (direction.sqrMagnitude < .5f) throw new InvalidOperationException("A real forward cable route is required.");
                Vector3 right = Vector3.Cross(Vector3.up, direction);
                float length = Vector3.Dot(car.routeEnd - start, direction);
                float sag = car.cable.spring > 0 ? car.load.mass * Physics.gravity.magnitude / car.cable.spring : 1;
                for (int gate = 0; gate < match.slalomGatesPerLane; gate++)
                {
                    var center = Child(lane, "Physical opening " + (gate + 1));
                    center.SetPositionAndRotation(start + direction * (length * (.22f + gate * .18f)) + right * (gate % 2 == 0 ? -2.75f : 2.75f) - Vector3.up * sag, Quaternion.LookRotation(direction, Vector3.up));
                    match.slalomGates[slot * match.slalomGatesPerLane + gate] = center;
                    const float thickness = .3f;
                    float width = match.slalomHalfOpening.x, height = match.slalomHalfOpening.y;
                    Box(center, "Left physical frame", new Vector3(-width - thickness / 2, 0, 0), new Vector3(thickness, height * 2 + thickness * 2, thickness), paint, contact, true);
                    Box(center, "Right physical frame", new Vector3(width + thickness / 2, 0, 0), new Vector3(thickness, height * 2 + thickness * 2, thickness), paint, contact, true);
                    Box(center, "Upper physical frame", new Vector3(0, height + thickness / 2, 0), new Vector3(width * 2, thickness, thickness), paint, contact, true);
                    Box(center, "Lower physical frame", new Vector3(0, -height - thickness / 2, 0), new Vector3(width * 2, thickness, thickness), paint, contact, true);
                    float railHeight = car.trolley.position.y - center.position.y;
                    Box(center, "Overhead slalom crosshead", new Vector3(0, railHeight, 0), new Vector3(width * 2 + 2, .35f, .6f), steel, contact, false);
                    float tieLength = Mathf.Max(.1f, railHeight - height - thickness);
                    foreach (int side in new[] { -1, 1 })
                        Box(center, side < 0 ? "Left hanging support" : "Right hanging support", new Vector3(side * (width + thickness / 2), height + thickness + tieLength / 2, 0), new Vector3(.09f, tieLength, .09f), steel, contact, false);
                }
                // Other modes keep their already-verified collision geometry.
                lane.gameObject.SetActive(false);
            }
            EditorUtility.SetDirty(match); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Debug.Log("HH10_SKYHOOK_SLALOM_AUTHORED four actual swept-clearance openings per lane; existing trolley/load poses and Sprint geometry preserved.");
        }
        static Transform Child(Transform parent, string name)
        {
            var existing = parent.Find(name); if (existing != null) return existing;
            var child = new GameObject(name).transform; child.SetParent(parent, false); return child;
        }
        static void Box(Transform parent, string name, Vector3 center, Vector3 scale, Material material, PhysicsMaterial contact, bool collidable)
        {
            Transform child = parent.Find(name);
            if (child == null) { var shape = GameObject.CreatePrimitive(PrimitiveType.Cube); shape.name = name; child = shape.transform; child.SetParent(parent, false); }
            child.localPosition = center; child.localRotation = Quaternion.identity; child.localScale = scale;
            child.GetComponent<Renderer>().sharedMaterial = material;
            var collider = child.GetComponent<BoxCollider>(); collider.sharedMaterial = contact; collider.enabled = collidable;
        }
    }
}
#endif

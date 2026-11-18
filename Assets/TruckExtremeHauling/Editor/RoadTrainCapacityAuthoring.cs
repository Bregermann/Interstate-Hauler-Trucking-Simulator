using System;
using System.Linq;
using LWS.TruckHitchSports;
using LWS.TruckHitchSports.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LWS.TruckExtremeHauling.Editor
{
    // Explicit additive capacity upgrade. Existing vehicles, links, course and
    // three-trailer starts are preserved; NWH still owns every real axle/body.
    public static class RoadTrainCapacityAuthoring
    {
        [MenuItem("Truck Sports/Alpha 0.10/Extend active Road Train capacity to eight")]
        public static void UpgradeActiveRoadTrainCapacityToEight() => UpgradeActiveRoadTrainCapacity(8);

        public static void UpgradeActiveRoadTrainCapacity(int capacity)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) throw new InvalidOperationException("Stopped compiled Editor required.");
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.path != "Assets/TruckExtremeHauling/Scenes/RoadTrainWreck.unity") throw new InvalidOperationException("Open the saved RoadTrainWreck scene explicitly.");
            var match = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<RoadTrainWreckMatch>(true)).Single();
            int oldCapacity = match.TrailerCapacityPerDriver, drivers = match.trucks.Length;
            if (capacity <= oldCapacity) { Debug.Log("HH10_ROAD_TRAIN_CAPACITY_PRESERVED " + oldCapacity); return; }
            if (drivers != 4 || match.trailers.Length != drivers * oldCapacity) throw new InvalidOperationException("Preserve the existing four driver columns.");
            var oldTrailers = match.trailers; var oldJoints = match.additionalHitches;
            if (oldJoints == null || oldJoints.Length != drivers * (oldCapacity - 1)) throw new InvalidOperationException("Existing physical link layout is incomplete.");
            var existingPoses = oldTrailers.Select(t => t.transform.localToWorldMatrix).ToArray();
            var truckPoses = match.trucks.Select(t => t.transform.localToWorldMatrix).ToArray();
            var trailers = new TrailerBody[drivers * capacity];
            var joints = new ConfigurableJoint[drivers * (capacity - 1)];
            var definitions = new RoadTrainWreckMatch.Link[joints.Length];
            float minimumZ = float.PositiveInfinity;
            for (int driver = 0; driver < drivers; driver++)
            {
                for (int piece = 0; piece < capacity; piece++)
                {
                    int index = driver * capacity + piece;
                    if (piece < oldCapacity) trailers[index] = oldTrailers[driver * oldCapacity + piece];
                    else
                    {
                        var previous = trailers[index - 1];
                        var next = HitchAuthoring.CreateTrailer(previous.transform.position - previous.transform.forward * 16, previous.transform.rotation, "RoadTrainWreck physical trailer " + driver + ":" + piece);
                        var bounds = HitchAuthoring.Bounds(previous.gameObject);
                        Vector3 pin = next.transform.InverseTransformPoint(next.Kingpin.position);
                        Vector3 anchor = new Vector3(bounds.center.x, pin.y, bounds.min.z - .25f);
                        next.transform.position += previous.transform.TransformPoint(anchor) - next.Kingpin.position;
                        if (next.Module.module.trailerStand != null) next.Module.module.trailerStand.SetActive(false);
                        trailers[index] = next;
                    }
                    var localBounds = HitchAuthoring.Bounds(trailers[index].gameObject);
                    minimumZ = Mathf.Min(minimumZ, trailers[index].transform.TransformPoint(localBounds.min).z);
                    if (piece == 0) continue;
                    int link = driver * (capacity - 1) + piece - 1;
                    if (piece < oldCapacity)
                    {
                        int oldLink = driver * (oldCapacity - 1) + piece - 1;
                        joints[link] = oldJoints[oldLink];
                        if (match.authoredLinkDefinitions != null && oldLink < match.authoredLinkDefinitions.Length && match.authoredLinkDefinitions[oldLink].owner != null) definitions[link] = match.authoredLinkDefinitions[oldLink];
                        else if (oldJoints[oldLink] != null) definitions[link] = Read(oldJoints[oldLink]);
                        else throw new InvalidOperationException("Preserve an authored anchor definition for each existing link.");
                    }
                    else
                    {
                        var front = trailers[index - 1]; var next = trailers[index]; var bounds = HitchAuthoring.Bounds(front.gameObject);
                        Vector3 pin = next.transform.InverseTransformPoint(next.Kingpin.position);
                        definitions[link] = new RoadTrainWreckMatch.Link
                        {
                            owner = front.Body, next = next.Body,
                            anchor = new Vector3(bounds.center.x, pin.y, bounds.min.z - .25f), connectedAnchor = pin,
                            low = -15, high = 15, roll = 12,
                            force = 2500000, torque = 1800000
                        };
                        // No live joint joins the default third trailer to an
                        // inactive spare. Selection creates it at round start.
                    }
                }
            }
            match.trailers = trailers; match.additionalHitches = joints; match.authoredLinkDefinitions = definitions; match.authoredTrailersPerDriver = capacity;
            for (int driver = 0; driver < drivers; driver++)
            for (int piece = oldCapacity; piece < capacity; piece++) trailers[driver * capacity + piece].gameObject.SetActive(false);
            ExtendStartingApronIfNeeded(match, minimumZ - 12);
            for (int i = 0; i < oldTrailers.Length; i++) if (existingPoses[i] != oldTrailers[i].transform.localToWorldMatrix) throw new InvalidOperationException("Existing three-trailer pose changed unexpectedly.");
            for (int i = 0; i < drivers; i++) if (truckPoses[i] != match.trucks[i].transform.localToWorldMatrix) throw new InvalidOperationException("Existing tractor start changed unexpectedly.");
            EditorUtility.SetDirty(match); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Debug.Log("HH10_ROAD_TRAIN_CAPACITY_ADDED capacity=" + capacity + " per driver; default selected length remains " + match.trailersPerDriver);
        }

        static RoadTrainWreckMatch.Link Read(ConfigurableJoint joint) => new RoadTrainWreckMatch.Link
        {
            owner = joint.GetComponent<Rigidbody>(), next = joint.connectedBody, anchor = joint.anchor, connectedAnchor = joint.connectedAnchor,
            low = joint.lowAngularXLimit.limit, high = joint.highAngularXLimit.limit, roll = joint.angularZLimit.limit, force = joint.breakForce, torque = joint.breakTorque
        };

        static void ExtendStartingApronIfNeeded(RoadTrainWreckMatch match, float requiredMinimumZ)
        {
            var floor = match.gameObject.scene.GetRootGameObjects().Single(g => g.name == "Continuous venue support");
            var support = floor.GetComponent<Collider>(); var bounds = support.bounds;
            if (requiredMinimumZ >= bounds.min.z) return;
            var existing = match.transform.Find("Visible extended native road-train starting apron");
            if (existing != null) throw new InvalidOperationException("Existing extended apron retained; inspect its capacity before extending again.");
            float length = bounds.min.z - requiredMinimumZ;
            var root = new GameObject("Visible extended native road-train starting apron").transform; root.SetParent(match.transform, false);
            root.position = new Vector3(120, bounds.max.y - .5f, requiredMinimumZ + length * .5f);
            var shape = root.gameObject.AddComponent<BoxCollider>(); shape.size = new Vector3(310, 1, length + .2f); shape.sharedMaterial = support.sharedMaterial;
            // Actual owned concrete floor geometry visibly fills the added
            // collision footprint; this is not a hidden safety floor.
            LWS.TruckKeepaway.Editor.KeepawayPresentationAuthoring.Fit(root, "Assets/LeartesStudios/Garage Warehouse Environment/Art/Prefabs/SM_Floor_4x6m_01.prefab", new Vector3(310, 1, length + .2f), -.5f, true, "TruckExtremeHauling");
        }
    }
}

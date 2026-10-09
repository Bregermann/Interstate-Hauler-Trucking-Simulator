using System;
using System.Collections.Generic;
using LWS.InterstateHauler;
using sc.splines.spawner.runtime;
using Unity.Collections;
using UnityEngine;

namespace LWS.TruckTaxi
{
    [ExecuteAlways, DisallowMultipleComponent]
    public sealed class TruckTaxiDressingBinding : MonoBehaviour
    {
        public string stableId;
        public TruckTaxiDressingProfile profile;
        public SplineSpawner spawner;
        public LwsRoadGraphProvider roadGraph;
        public TruckTaxiRideLocation[] parkingBays = Array.Empty<TruckTaxiRideLocation>();
        public Transform[] entranceApproaches = Array.Empty<Transform>();
        public BoxCollider[] protectedVolumes = Array.Empty<BoxCollider>();
        [Tooltip("Designer confirms all relevant lanes, bays, entrances and protected route geometry are referenced.")]
        public bool exclusionScopeReviewed;
        public int LastRejected { get; private set; }
        public string LastFailure { get; private set; }
        private SplineSpawner subscribed;
        private bool generationAuthorized;
        private int filteredSplines;
        private readonly List<Bounds> exclusions = new List<Bounds>();
        private readonly List<Bounds> prefabBounds = new List<Bounds>();
        private void OnEnable() => Subscribe();
        private void OnValidate() { if (isActiveAndEnabled) Subscribe(); else Unsubscribe(); }
        private void OnDisable() => Unsubscribe();
        private void OnDestroy() => Unsubscribe();
        private void Subscribe()
        {
            if (subscribed == spawner) return;
            Unsubscribe(); subscribed = spawner;
            if (subscribed != null) subscribed.onAfterModifiers += Filter;
        }
        private void Unsubscribe()
        { if (subscribed != null) subscribed.onAfterModifiers -= Filter; subscribed = null; }

        public bool Validate(out string reason)
        {
            reason = "Assign same-object vendor spawner, spline, dedicated unscaled output root, profile and stable binding ID.";
            if (string.IsNullOrWhiteSpace(stableId) || profile == null || spawner == null || spawner.gameObject != gameObject ||
                !spawner.isActiveAndEnabled || spawner.SplineContainer == null || spawner.SplineContainer.Splines.Count == 0 ||
                spawner.root == null || spawner.root != transform || !Finite(transform.lossyScale) ||
                (transform.lossyScale - Vector3.one).sqrMagnitude > .000001f) return false;
            if (!profile.Validate(out reason)) return false;
            foreach (Transform child in transform)
            {
                var container = child.GetComponent<SplineInstanceContainer>();
                if (container == null || container.owner != spawner)
                { reason = "Output root contains unrelated objects or another spawner's container; refusing regeneration."; return false; }
            }
            reason = "Review and reference the complete exclusion scope; missing graph or anchors fails closed.";
            if (!exclusionScopeReviewed || roadGraph == null || roadGraph.Graph?.edges == null ||
                roadGraph.Graph.nodes == null || roadGraph.Graph.edges.Count == 0 || roadGraph.Graph.nodes.Count == 0 ||
                parkingBays == null || entranceApproaches == null || protectedVolumes == null) return false;
            try { if (!roadGraph.ValidateGraph().IsValid) return false; }
            catch (Exception) { return false; }
            foreach (var bay in parkingBays) if (bay == null || bay.truckStopPoint == null || !Finite(bay.StopPosition)) return false;
            foreach (var entrance in entranceApproaches) if (entrance == null || !Finite(entrance.position)) return false;
            foreach (var volume in protectedVolumes) if (volume == null || !Finite(volume.transform.position) ||
                !Finite(volume.size) || volume.size.x <= 0 || volume.size.y <= 0 || volume.size.z <= 0) return false;
            foreach (var edge in roadGraph.Graph.edges)
                if (edge == null || edge.samples == null || edge.samples.Count < 2 || edge.laneCount < 1 ||
                    !float.IsFinite(edge.laneWidthMeters) || edge.laneWidthMeters <= 0 ||
                    !float.IsFinite(edge.leftShoulderWidthMeters) || !float.IsFinite(edge.rightShoulderWidthMeters) ||
                    !float.IsFinite(edge.medianWidthMeters)) return false;
            foreach (var edge in roadGraph.Graph.edges)
                foreach (var sample in edge.samples) if (sample == null || !Finite(sample.position)) return false;
            reason = null; return true;
        }

        // Configuration only. The editor's explicit command remains the only LWS Respawn caller.
        public bool Configure(out string reason)
        {
            if (!Validate(out reason)) return false;
            Subscribe();
            spawner.startupBehaviour = SplineSpawner.StartupBehaviour.None;
            spawner.respawnTriggers = SplineSpawner.RespawnTriggers.None;
            spawner.spawnObjects = false;
            var pool = new List<SplineSpawner.SpawnableObject>();
            foreach (var item in profile.prefabs) pool.Add(new SplineSpawner.SpawnableObject(item.prefab, item.probability) {
                pivot = item.pivot, forwardDirection = item.forwardDirection, baseScale = item.baseScale,
                selectBySplineDistance = item.selectBySplineDistance });
            spawner.inputObjects = pool;
            var distribution = new DistributionSettings();
            distribution.SetSeed(TruckTaxiDressingProfile.StableSeed(profile.seed, stableId));
            distribution.mode = profile.insideArea ? DistributionSettings.DistributionMode.InsideArea : DistributionSettings.DistributionMode.OnCurve;
            distribution.onCurve.spacing = profile.spacingMeters;
            distribution.insideArea.spacing = profile.spacingMeters;
            distribution.insideArea.allowOverflow = false;
            spawner.distributionSettings = distribution;
            return true;
        }

        public bool GenerateExplicitly(out string reason)
        {
            reason = "Explicit dressing generation is authoring-only and requires an enabled binding.";
            if (Application.isPlaying || !isActiveAndEnabled || generationAuthorized) return false;
            if (!Configure(out reason)) return false;
            LastFailure = null; LastRejected = 0; filteredSplines = 0;
            generationAuthorized = true; spawner.spawnObjects = true;
            try
            {
                spawner.Respawn();
                if (filteredSplines == 0 && string.IsNullOrEmpty(LastFailure))
                    LastFailure = "Vendor did not run final exclusions; inspect spline validity and vendor warnings.";
                reason = LastFailure; return string.IsNullOrEmpty(reason);
            }
            finally { generationAuthorized = false; spawner.spawnObjects = false; }
        }

        private void BuildExclusions()
        {
            exclusions.Clear(); prefabBounds.Clear();
            foreach (var item in spawner.inputObjects)
            {
                if (!TruckTaxiDressingProfile.TryPrefabBounds(item.prefab, out var bounds)) throw new InvalidOperationException("Missing prefab bounds.");
                prefabBounds.Add(bounds);
            }
            foreach (var edge in roadGraph.Graph.edges)
            {
                float halfWidth = edge.laneCount * edge.laneWidthMeters * .5f + Mathf.Max(0, edge.leftShoulderWidthMeters) +
                    Mathf.Max(0, edge.rightShoulderWidthMeters) + Mathf.Max(0, edge.medianWidthMeters) * .5f;
                for (int i = 1; i < edge.samples.Count; i++)
                    exclusions.Add(RoadEnvelope(edge.samples[i - 1].position, edge.samples[i].position, halfWidth));
            }
            foreach (var bay in parkingBays) exclusions.Add(TruckTaxiWorldColliderReview.TransformBounds(
                new Bounds(Vector3.up * 2.25f, new Vector3(4, 4.5f, 12)), bay.truckStopPoint.localToWorldMatrix));
            foreach (var entrance in entranceApproaches) exclusions.Add(TruckTaxiWorldColliderReview.TransformBounds(
                new Bounds(Vector3.up, new Vector3(2, 2.5f, 3)), entrance.localToWorldMatrix));
            foreach (var volume in protectedVolumes) exclusions.Add(TruckTaxiWorldColliderReview.TransformBounds(
                new Bounds(volume.center, volume.size), volume.transform.localToWorldMatrix));
        }
        public static Bounds RoadEnvelope(Vector3 start, Vector3 end, float halfWidth)
        {
            if (!Finite(start) || !Finite(end) || !float.IsFinite(halfWidth) || halfWidth <= 0)
                throw new ArgumentException("Invalid road exclusion geometry.");
            var bounds = new Bounds(start, Vector3.zero); bounds.Encapsulate(end);
            bounds.Expand(new Vector3(halfWidth * 2, 9, halfWidth * 2)); return bounds;
        }
        private static bool Finite(Vector3 v) => float.IsFinite(v.x) && float.IsFinite(v.y) && float.IsFinite(v.z);
        public static bool IntersectsExclusion(Bounds footprint, IReadOnlyList<Bounds> volumes, float clearance)
        {
            if (!Finite(footprint.center) || !Finite(footprint.size) || !float.IsFinite(clearance) || clearance < 0) return true;
            footprint.Expand(clearance * 2);
            foreach (var volume in volumes)
                if (!Finite(volume.center) || !Finite(volume.size) || volume.size.x <= 0 || volume.size.y <= 0 || volume.size.z <= 0 ||
                    footprint.Intersects(volume)) return true;
            return false;
        }
        private void Filter(NativeList<SpawnPoint> points)
        {
            try
            {
                if (!generationAuthorized) throw new InvalidOperationException("Use the explicit dressing generation command.");
                filteredSplines++;
                if (!Validate(out string reason)) throw new InvalidOperationException(reason);
                if (spawner.startupBehaviour != SplineSpawner.StartupBehaviour.None || spawner.respawnTriggers != SplineSpawner.RespawnTriggers.None)
                    throw new InvalidOperationException("Automatic generation must remain disabled for authored dressing.");
                BuildExclusions();
                for (int i = 0; i < points.Length; i++)
                {
                    var point = points[i]; if (!point.isValid) continue;
                    bool reject = point.prefabIndex < 0 || point.prefabIndex >= prefabBounds.Count;
                    if (!reject)
                    {
                        var footprint = TruckTaxiWorldColliderReview.TransformBounds(prefabBounds[point.prefabIndex],
                            Matrix4x4.TRS((Vector3)point.position, (Quaternion)point.rotation, (Vector3)point.scale));
                        reject = IntersectsExclusion(footprint, exclusions, profile.clearanceMeters);
                    }
                    if (reject) { point.isValid = false; points[i] = point; LastRejected++; }
                }
            }
            catch (Exception error)
            {
                LastFailure = error.Message;
                for (int i = 0; i < points.Length; i++) { var point = points[i]; if (point.isValid) LastRejected++; point.isValid = false; points[i] = point; }
                Debug.LogError("Dressing exclusion failed closed: " + LastFailure, this);
            }
        }
    }
}

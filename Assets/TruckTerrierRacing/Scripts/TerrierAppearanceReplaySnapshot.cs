using System;
using System.Collections.Generic;
using UnityEngine;
using LWS.TruckSports;
namespace LWS.TruckTerrierRacing
{
    public sealed partial class TerrierAppearanceBridge
    {
        // Renderer-only static hierarchy snapshot. Only the four actual Host
        // wheel pivots join the original recorder's fixed34 transform channels.
        void BuildIntactSnapshot(int slot)
        {
            ClearIntactSnapshot(slot); var state = Owner(slot); var binding = owners[slot]; var host = binding.host;
            if (binding.originalParts.Length != 34 || binding.originalGhostParts.Length != 34) throw new InvalidOperationException("Exact original34 Terrier replay channels required.");
            for (int wheel = 0; wheel < 4; wheel++)
            {
                state.wheelSources[wheel] = binding.wheels.meshes[wheel]; int index = Array.IndexOf(binding.originalParts, state.wheelSources[wheel]);
                if (index < 0) throw new InvalidOperationException("Original Terrier wheel pose source is not in its original34 channels."); state.wheelTargets[wheel] = binding.originalGhostParts[index];
            }
            if (host.Applied)
            {
                var map = new Dictionary<Transform, Transform>();
                state.snapshot = new GameObject("Terrier selected recorded hierarchy").transform; state.snapshot.SetParent(binding.originalGhostParts[0], false); CloneNode(host.AppearanceRoot, state.snapshot, state, map);
                var used = new bool[4];
                foreach (var tag in host.AppearanceRoot.GetComponentsInChildren<SportsTruckAppearanceMountTag>(true))
                {
                    if (tag.wheelIndex < 0) continue; int wheel = tag.wheelIndex;
                    if (wheel >= 4 || used[wheel] || !map.TryGetValue(tag.transform, out var target)) throw new InvalidOperationException("Exactly four explicit selected wheel pivot identities required.");
                    used[wheel] = true; state.wheelSources[wheel] = tag.transform; state.wheelTargets[wheel] = target;
                }
                // A supported body can deliberately retain original rolling
                // meshes, in which case the four original channels remain valid.
                for (int wheel = 1; wheel < 4; wheel++) if (used[wheel] != used[0]) throw new InvalidOperationException("Selected wheel mapping must be complete or use all four original wheels.");
            }
            state.snapshotRevision = host.AppearanceRevision;
        }
        Transform CloneNode(Transform source, Transform parent, State state, Dictionary<Transform, Transform> map)
        {
            if (source.GetComponent<LODGroup>() != null) throw new InvalidOperationException("This static Terrier snapshot requires explicitly normalized LOD0 content.");
            var item = new GameObject(source.name); item.layer = 0; item.transform.SetParent(parent, false); item.transform.localPosition = source.localPosition; item.transform.localRotation = source.localRotation; item.transform.localScale = source.localScale; item.SetActive(source.gameObject.activeSelf); map.Add(source, item.transform);
            var renderer = source.GetComponent<Renderer>();
            if (renderer != null)
            {
                var filter = source.GetComponent<MeshFilter>(); if (!(renderer is MeshRenderer) || filter == null || filter.sharedMesh == null) throw new InvalidOperationException("Only original readable static body rendering is supported.");
                item.AddComponent<MeshFilter>().sharedMesh = filter.sharedMesh; var target = item.AddComponent<MeshRenderer>(); target.shadowCastingMode = renderer.shadowCastingMode; target.receiveShadows = renderer.receiveShadows;
                state.sources.Add(renderer); state.targets.Add(target); CopyMetadata(renderer, target);
            }
            foreach (Transform child in source) CloneNode(child, item.transform, state, map); return item.transform;
        }
        void RefreshIntactMetadata(int slot)
        {
            var binding = owners[slot]; var state = Owner(slot);
            for (int index = 0; index < binding.originalParts.Length; index++)
            {
                var source = binding.originalParts[index].GetComponent<Renderer>(); if (source == null) continue; var target = binding.originalGhostParts[index].GetComponent<Renderer>(); if (target != null) CopyMetadata(source, target);
            }
            for (int index = 0; index < state.sources.Count; index++) if (state.sources[index] != null && state.targets[index] != null) CopyMetadata(state.sources[index], state.targets[index]);
        }
        public void ReplayChannels(int slot, out Transform[] source, out Transform[] ghost)
        {
            var binding = owners[slot]; var state = Owner(slot); source = new Transform[38]; ghost = new Transform[38];
            Array.Copy(binding.originalParts, source, 34); Array.Copy(binding.originalGhostParts, ghost, 34);
            for (int wheel = 0; wheel < 4; wheel++) { source[34 + wheel] = state.wheelSources[wheel]; ghost[34 + wheel] = state.wheelTargets[wheel]; if (source[34 + wheel] == null || ghost[34 + wheel] == null) throw new InvalidOperationException("Prepare actual selected wheel snapshots before resetting Terrier recording."); }
        }
        void ClearIntactSnapshot(int slot)
        {
            var state = Owner(slot); state.sources.Clear(); state.targets.Clear();
            if (state.snapshot != null) { state.snapshot.gameObject.SetActive(false); state.snapshot.SetParent(null, true); Dispose(state.snapshot.gameObject); state.snapshot = null; }
            state.snapshotRevision = int.MinValue;
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;
namespace LWS.TruckDockDiving
{
    public sealed partial class DockReplay
    {
        Transform snapshotRoot;
        readonly List<Renderer> snapshotSources = new List<Renderer>(), snapshotTargets = new List<Renderer>();
        public int AppearanceSnapshots { get; private set; }
        // Called only at the original stopped Begin boundary. Frames, clock,
        // physical root and original replay interpolation are never replaced.
        public void RefreshSelectedSnapshot(DockMatch match)
        {
            if (Playing || count != 0) throw new InvalidOperationException("Dock rendering snapshots require the original empty recording boundary.");
            ClearSnapshot();
            foreach (var renderer in ghost.GetComponentsInChildren<Renderer>(true)) renderer.enabled = false;
            snapshotRoot = new GameObject("Dock selected recorded rendering").transform; snapshotRoot.SetParent(ghost, false);
            DockSelectedSplitPresentation.SetLocal(snapshotRoot, match.truck.transform.worldToLocalMatrix * match.truck.visuals.localToWorldMatrix);
            var upper = new GameObject("Dock recorded upper pivot").transform; upper.SetParent(snapshotRoot, false);
            foreach (var source in match.truck.visuals.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (!source.enabled || !source.gameObject.activeInHierarchy) continue;
                var filter = source.GetComponent<MeshFilter>(); if (filter == null || filter.sharedMesh == null) throw new InvalidOperationException("Dock replay requires actual mesh rendering.");
                bool isUpper = match.appearanceSplit.UpperRoot != null && source.transform.IsChildOf(match.appearanceSplit.UpperRoot);
                if (!isUpper) foreach (var original in match.split.upper) if (source.transform == original || source.transform.IsChildOf(original)) { isUpper = true; break; }
                var item = new GameObject(source.name, typeof(MeshFilter), typeof(MeshRenderer)); item.layer = source.gameObject.layer; item.transform.SetParent(isUpper ? upper : snapshotRoot, false);
                DockSelectedSplitPresentation.SetLocal(item.transform, match.truck.visuals.worldToLocalMatrix * source.localToWorldMatrix);
                item.GetComponent<MeshFilter>().sharedMesh = filter.sharedMesh; var target = item.GetComponent<MeshRenderer>(); target.shadowCastingMode = source.shadowCastingMode; target.receiveShadows = source.receiveShadows;
                snapshotSources.Add(source); snapshotTargets.Add(target);
            }
            ghostSplit.BindPresentation(upper); live = match.truck.visuals.GetComponentsInChildren<Renderer>(true); RefreshSnapshotMetadata(); AppearanceSnapshots++;
        }
        void RefreshSnapshotMetadata()
        {
            var block = new MaterialPropertyBlock();
            for (int index = 0; index < snapshotSources.Count; index++)
            {
                var source = snapshotSources[index]; var target = snapshotTargets[index]; if (source == null || target == null) continue;
                target.sharedMaterials = source.sharedMaterials; source.GetPropertyBlock(block); target.SetPropertyBlock(block);
                for (int material = 0; material < target.sharedMaterials.Length; material++) { block.Clear(); source.GetPropertyBlock(block, material); target.SetPropertyBlock(block.isEmpty ? null : block, material); }
            }
        }
        void ClearSnapshot()
        {
            if (ghostSplit != null) ghostSplit.BindPresentation(null); snapshotSources.Clear(); snapshotTargets.Clear();
            if (snapshotRoot != null) { snapshotRoot.gameObject.SetActive(false); snapshotRoot.SetParent(null, true); Destroy(snapshotRoot.gameObject); snapshotRoot = null; }
        }
        void OnDestroy() { ClearSnapshot(); }
    }
}

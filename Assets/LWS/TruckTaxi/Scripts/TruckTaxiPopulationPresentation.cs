using UnityEngine;
using UnityEngine.Rendering;

namespace LWS.TruckTaxi
{
    // Cached per spawned instance; existing population adapters own its lifetime and maintenance cadence.
    // No movement, visibility toggles, spawns, cameras, global quality settings or vendor AI are owned here.
    public sealed class TruckTaxiPopulationPresentation
    {
        private readonly Transform root;
        private readonly bool pedestrian;
        private readonly Animator[] animators;
        private readonly AnimatorCullingMode[] culling;
        private readonly Renderer[] renderers;
        private readonly ShadowCastingMode[] shadows;
        private readonly SkinnedMeshRenderer[] skins;
        private readonly SkinQuality[] quality;
        private readonly bool[] motionVectors;
        private readonly LODGroup[] lodGroups;
        private readonly LOD[][] lods;
        private float appliedLodMultiplier = 1;
        private bool applied;
        public bool ShadowsReduced { get; private set; }
        public bool SkinningReduced { get; private set; }
        public bool Applied => applied;

        public TruckTaxiPopulationPresentation(GameObject instance, bool isPedestrian)
        {
            root = instance.transform; pedestrian = isPedestrian;
            animators = instance.GetComponentsInChildren<Animator>(true);
            culling = new AnimatorCullingMode[animators.Length];
            for (int i = 0; i < animators.Length; i++) culling[i] = animators[i].cullingMode;
            renderers = instance.GetComponentsInChildren<Renderer>(true);
            shadows = new ShadowCastingMode[renderers.Length];
            for (int i = 0; i < renderers.Length; i++) shadows[i] = renderers[i].shadowCastingMode;
            skins = instance.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            quality = new SkinQuality[skins.Length]; motionVectors = new bool[skins.Length];
            for (int i = 0; i < skins.Length; i++) { quality[i] = skins[i].quality; motionVectors[i] = skins[i].skinnedMotionVectors; }
            lodGroups = instance.GetComponentsInChildren<LODGroup>(true);
            lods = new LOD[lodGroups.Length][];
            for (int i = 0; i < lodGroups.Length; i++) lods[i] = lodGroups[i].GetLODs();
        }

        public void Refresh(TruckTaxiPopulationProfile profile, Vector3 observer)
        {
            if (root == null) return;
            if (profile == null || !profile.optimizePresentation) { Restore(); return; }
            applied = true;
            float distanceSquared = (root.position - observer).sqrMagnitude;
            ShadowsReduced = BeyondDistance(distanceSquared, pedestrian ? profile.pedestrianShadowDistance : profile.trafficShadowDistance,
                profile.presentationDistanceHysteresis, ShadowsReduced);
            SkinningReduced = pedestrian && BeyondDistance(distanceSquared, profile.pedestrianReducedSkinningDistance,
                profile.presentationDistanceHysteresis, SkinningReduced);
            for (int i = 0; i < animators.Length; i++)
            {
                var animator = animators[i]; if (animator == null) continue;
                // Root-motion-driven content must keep evaluating even outside a camera frustum.
                var mode = profile.cullOffscreenAnimation && !animator.applyRootMotion ? AnimatorCullingMode.CullCompletely : culling[i];
                if (animator.cullingMode != mode) animator.cullingMode = mode;
            }
            for (int i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i]; if (renderer == null) continue;
                // Preserve explicitly shadows-only authored geometry rather than making it disappear entirely.
                var mode = ShadowsReduced && shadows[i] != ShadowCastingMode.ShadowsOnly ? ShadowCastingMode.Off : shadows[i];
                if (renderer.shadowCastingMode != mode) renderer.shadowCastingMode = mode;
            }
            for (int i = 0; i < skins.Length; i++)
            {
                var skin = skins[i]; if (skin == null) continue;
                var target = SkinningReduced && quality[i] != SkinQuality.Bone1 ? SkinQuality.Bone2 : quality[i];
                if (skin.quality != target) skin.quality = target;
                bool vectors = motionVectors[i] && !profile.disableSkinnedMotionVectors;
                if (skin.skinnedMotionVectors != vectors) skin.skinnedMotionVectors = vectors;
            }
            float multiplier = Mathf.Clamp(profile.lodTransitionMultiplier, 1, 3);
            if (!Mathf.Approximately(multiplier, appliedLodMultiplier))
            {
                for (int i = 0; i < lodGroups.Length; i++)
                    if (lodGroups[i] != null) lodGroups[i].SetLODs(AdjustedLods(lods[i], multiplier));
                appliedLodMultiplier = multiplier;
            }
        }

        public static bool BeyondDistance(float distanceSquared, float threshold, float hysteresis, bool wasReduced)
        {
            if (threshold <= 0 || float.IsNaN(threshold) || float.IsNaN(distanceSquared)) return false;
            float boundary = wasReduced ? Mathf.Max(0, threshold - Mathf.Max(0, hysteresis)) : threshold;
            return distanceSquared > boundary * boundary;
        }

        public static LOD[] AdjustedLods(LOD[] original, float multiplier)
        {
            var result = (LOD[])original.Clone();
            if (float.IsNaN(multiplier) || float.IsInfinity(multiplier)) multiplier = 1;
            multiplier = Mathf.Clamp(multiplier, 1, 3);
            // Never increase the final culling threshold: all existing meshes and their visibility range survive.
            for (int i = 0; i < result.Length - 1; i++)
                result[i].screenRelativeTransitionHeight = Mathf.Min(.99f, original[i].screenRelativeTransitionHeight * multiplier);
            return result;
        }

        public void Restore()
        {
            if (!applied) return;
            for (int i = 0; i < animators.Length; i++) if (animators[i] != null) animators[i].cullingMode = culling[i];
            for (int i = 0; i < renderers.Length; i++) if (renderers[i] != null) renderers[i].shadowCastingMode = shadows[i];
            for (int i = 0; i < skins.Length; i++) if (skins[i] != null)
            { skins[i].quality = quality[i]; skins[i].skinnedMotionVectors = motionVectors[i]; }
            if (!Mathf.Approximately(appliedLodMultiplier, 1))
                for (int i = 0; i < lodGroups.Length; i++) if (lodGroups[i] != null) lodGroups[i].SetLODs(lods[i]);
            appliedLodMultiplier = 1; applied = ShadowsReduced = SkinningReduced = false;
        }
    }
}

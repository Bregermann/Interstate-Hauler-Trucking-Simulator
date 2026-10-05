using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace LWS.TruckTaxi
{
    // Copies only the render skeleton. No source scripts, colliders, joints or rigidbodies are cloned.
    public sealed class TruckTaxiCustomVisualProxy : IDisposable
    {
        private readonly Dictionary<Renderer, bool> visibility = new Dictionary<Renderer, bool>();
        private PlayableGraph graph;
        public GameObject Root { get; private set; }
        public Animator Animator { get; private set; }

        public static TruckTaxiCustomVisualProxy Create(Transform source, Transform parent, AnimationClip clip)
        {
            if (source == null) return null;
            var result = new TruckTaxiCustomVisualProxy();
            try { Populate(result, source, parent, clip); return result; }
            catch { result.Dispose(); throw; }
        }
        private static void Populate(TruckTaxiCustomVisualProxy result, Transform source, Transform parent, AnimationClip clip)
        {
            var transforms = new Dictionary<Transform, Transform>();
            Transform Copy(Transform original, Transform destination)
            {
                var copy = new GameObject(original.name).transform;
                copy.SetParent(destination, false);
                copy.localPosition = original.localPosition; copy.localRotation = original.localRotation;
                copy.localScale = original.localScale; copy.gameObject.layer = original.gameObject.layer;
                transforms.Add(original, copy);
                foreach (Transform child in original) Copy(child, copy);
                copy.gameObject.SetActive(original.gameObject.activeSelf);
                return copy;
            }
            var root = Copy(source, parent);
            result.Root = root.gameObject;
            root.name = source.name + " Custom Presentation Proxy";
            root.SetPositionAndRotation(source.position, source.rotation);
            var scale = parent != null ? parent.lossyScale : Vector3.one;
            root.localScale = new Vector3(source.lossyScale.x / Nonzero(scale.x), source.lossyScale.y / Nonzero(scale.y), source.lossyScale.z / Nonzero(scale.z));
            foreach (var original in source.GetComponentsInChildren<Renderer>(true))
            {
                Renderer renderer;
                if (original is SkinnedMeshRenderer skin)
                {
                    var copy = transforms[original.transform].gameObject.AddComponent<SkinnedMeshRenderer>();
                    copy.sharedMesh = skin.sharedMesh; copy.localBounds = skin.localBounds;
                    copy.updateWhenOffscreen = true;
                    var bones = skin.bones; var mapped = new Transform[bones.Length];
                    for (int i = 0; i < bones.Length; i++)
                        if (bones[i] != null) transforms.TryGetValue(bones[i], out mapped[i]);
                    copy.bones = mapped;
                    if (skin.rootBone != null && transforms.TryGetValue(skin.rootBone, out var bone)) copy.rootBone = bone;
                    if (skin.sharedMesh != null)
                        for (int i = 0; i < skin.sharedMesh.blendShapeCount; i++) copy.SetBlendShapeWeight(i, skin.GetBlendShapeWeight(i));
                    renderer = copy;
                }
                else if (original is MeshRenderer)
                {
                    var filter = original.GetComponent<MeshFilter>();
                    if (filter == null) continue;
                    var target = transforms[original.transform].gameObject;
                    target.AddComponent<MeshFilter>().sharedMesh = filter.sharedMesh;
                    renderer = target.AddComponent<MeshRenderer>();
                }
                else continue;
                renderer.sharedMaterials = original.sharedMaterials;
                renderer.enabled = original.enabled;
                renderer.shadowCastingMode = original.shadowCastingMode;
                renderer.receiveShadows = original.receiveShadows;
                renderer.lightProbeUsage = original.lightProbeUsage;
                renderer.reflectionProbeUsage = original.reflectionProbeUsage;
                renderer.sortingLayerID = original.sortingLayerID;
                renderer.sortingOrder = original.sortingOrder;
                var properties = new MaterialPropertyBlock();
                original.GetPropertyBlock(properties); renderer.SetPropertyBlock(properties);
                result.visibility.Add(original, original.enabled);
                original.enabled = false;
            }
            var sourceAnimator = source.GetComponentInChildren<Animator>(true);
            var animatorRoot = sourceAnimator != null ? transforms[sourceAnimator.transform] : root;
            result.Animator = animatorRoot.gameObject.AddComponent<Animator>();
            if (sourceAnimator != null) result.Animator.avatar = sourceAnimator.avatar;
            result.Animator.applyRootMotion = false;
            result.Animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            result.Animator.updateMode = AnimatorUpdateMode.UnscaledTime;
            if (clip != null && !clip.legacy)
            {
                result.graph = PlayableGraph.Create("Truck Taxi visual-only clip");
                result.graph.SetTimeUpdateMode(DirectorUpdateMode.UnscaledGameTime);
                var playable = AnimationClipPlayable.Create(result.graph, clip);
                var output = AnimationPlayableOutput.Create(result.graph, "Visual", result.Animator);
                output.SetSourcePlayable(playable); result.graph.Play();
            }
        }
        private static float Nonzero(float value) => Mathf.Abs(value) < .0001f ? 1 : value;
        public void Dispose()
        {
            if (graph.IsValid()) graph.Destroy();
            foreach (var pair in visibility) if (pair.Key != null) pair.Key.enabled = pair.Value;
            visibility.Clear();
            if (Root != null) { Root.SetActive(false); UnityEngine.Object.Destroy(Root); }
            Root = null;
        }
    }
}

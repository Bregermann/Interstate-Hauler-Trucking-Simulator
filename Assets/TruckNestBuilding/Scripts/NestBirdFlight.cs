using UnityEngine;
namespace LWS.TruckNestBuilding
{
    public sealed class NestBirdFlight : MonoBehaviour
    {
        public NestMatch match;
        public Animator animator;
        public Vector3 center;
        public float radius = 18, phase;
        [Min(.25f)] public float targetWingspan = 2.8f;
        public int AnimatedFrames { get; private set; }
        public bool VisualFitReady { get; private set; }
        public int VisualFitSamples { get; private set; }
        public float MaximumSourceSpan { get; private set; }
        SkinnedMeshRenderer[] skins;
        Mesh[] baked;
        readonly System.Collections.Generic.List<Vector3> vertices = new System.Collections.Generic.List<Vector3>();
        Vector3 authoredScale;
        float fittingSeconds, sampleSeconds = 2;
        void Awake()
        {
            authoredScale = transform.localScale;
            skins = GetComponentsInChildren<SkinnedMeshRenderer>(true); baked = new Mesh[skins.Length];
            for (int i = 0; i < skins.Length; i++) baked[i] = new Mesh { name = "Owned Nest animated bird fit sample" };
            if (animator != null && animator.runtimeAnimatorController != null)
                foreach (var clip in animator.runtimeAnimatorController.animationClips) sampleSeconds = Mathf.Max(sampleSeconds, clip.length * 1.25f);
        }
        // The existing actor is outside the imported Animator, so its uniform
        // presentation fit cannot be overwritten by original FBX scale curves.
        // Sample actual skinned vertices after animation, never an inflated bind
        // AABB. BakeMesh(true) compensates renderer Transform scale; the
        // matrix below applies that scale exactly once. The default false also
        // carries Transform scale and would invalidate the fit after parenting.
        // A maximum envelope avoids making wings inflate as they flap.
        void LateUpdate()
        {
            if (animator == null || !animator.isInitialized || skins == null || skins.Length == 0 || VisualFitReady && match != null && match.Paused) return;
            if (!VisualFitReady || fittingSeconds < sampleSeconds)
            {
                Bounds bounds = default; bool found = false;
                for (int i = 0; i < skins.Length; i++)
                {
                    var skin = skins[i]; if (skin == null || skin.sharedMesh == null) continue;
                    skin.BakeMesh(baked[i], true); baked[i].GetVertices(vertices);
                    var matrix = transform.worldToLocalMatrix * skin.localToWorldMatrix;
                    foreach (var vertex in vertices)
                    {
                        var point = Vector3.Scale(matrix.MultiplyPoint3x4(vertex), authoredScale);
                        if (!float.IsFinite(point.x) || !float.IsFinite(point.y) || !float.IsFinite(point.z)) continue;
                        if (!found) { bounds = new Bounds(point, Vector3.zero); found = true; } else bounds.Encapsulate(point);
                    }
                }
                float span = found ? Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z)) : 0;
                if (span > .001f) { MaximumSourceSpan = Mathf.Max(MaximumSourceSpan, span); VisualFitReady = true; VisualFitSamples++; }
                fittingSeconds += Time.deltaTime;
            }
            if (VisualFitReady)
            {
                float target = float.IsFinite(targetWingspan) ? Mathf.Max(.25f, targetWingspan) : 2.8f;
                var scale = authoredScale * (target / MaximumSourceSpan); if (transform.localScale != scale) transform.localScale = scale;
            }
        }
        void OnDestroy()
        {
            if (baked != null) foreach (var mesh in baked) if (mesh != null) Destroy(mesh);
        }
        void Update()
        {
            if (match == null || !match.Live) return;
            float t = Time.time * .16f + phase; Vector3 next = center + new Vector3(Mathf.Sin(t) * radius, Mathf.Sin(t * 1.7f) * 2, Mathf.Cos(t) * radius);
            var direction = new Vector3(Mathf.Cos(t), Mathf.Cos(t * 1.7f) * .12f, -Mathf.Sin(t));
            transform.SetPositionAndRotation(next, Quaternion.LookRotation(direction, Vector3.up)); AnimatedFrames++;
        }
    }
}

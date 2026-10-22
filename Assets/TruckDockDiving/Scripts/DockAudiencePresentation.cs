using System;
using System.Collections.Generic;
using System.Linq;
using LWS.TruckFieldGoal;
using UnityEngine;
using UnityEngine.Rendering;

namespace LWS.TruckDockDiving
{
    [Serializable] public sealed class DockAudienceVariant
    {
        public string sourceName;
        public Transform root;
        public Animator animator;
        public SkinnedMeshRenderer skin;
        public AnimationClip idle, cheer;
        public MeshFilter[] fans;
    }
    // Reuses the existing Field Goal crowd's pose/reaction lifecycle. Far fans
    // receive two baked poses once, rather than hundreds of live rig updates.
    // No new vendor geometry assets are generated or published by this adapter.
    [DefaultExecutionOrder(-80)] public sealed class DockAudiencePresentation : MonoBehaviour
    {
        public int authoredVersion = 1;
        public DockMatch match;
        public FieldGoalCrowd crowd;
        public DockAudienceVariant[] variants;
        readonly List<Mesh> owned = new List<Mesh>();
        int launches = -1, attempts = -1;
        bool running;
        public int BakedMeshes => owned.Count;
        public int Spectators => crowd == null ? 0 : crowd.Spectators;
        void Awake()
        {
            crowd.batches = new FieldGoalCrowd.Batch[variants.Length];
            for (int i = 0; i < variants.Length; i++)
            {
                var v = variants[i]; v.skin.forceRenderingOff = true; v.root.gameObject.SetActive(true); v.animator.enabled = false;
                var idle = Bake(v, v.idle, i * .071f + .2f); float floor = idle.bounds.min.y; Ground(idle, floor);
                var cheer = Bake(v, v.cheer, i * .053f + .35f); Ground(cheer, floor);
                foreach (var fan in v.fans) fan.sharedMesh = idle;
                crowd.batches[i] = new FieldGoalCrowd.Batch { idle = idle, cheer = cheer, fans = v.fans, materials = v.skin.sharedMaterials, seats = v.fans.Select(f => f.transform.localToWorldMatrix).ToArray() };
                v.root.gameObject.SetActive(false);
            }
        }
        Mesh Bake(DockAudienceVariant v, AnimationClip clip, float phase)
        {
            clip.SampleAnimation(v.animator.gameObject, clip.length * Mathf.Repeat(phase, 1));
            var raw = new Mesh(); v.skin.BakeMesh(raw);
            var mesh = new Mesh { name = "Owned runtime Dock audience pose · " + v.sourceName + " · " + clip.name, indexFormat = IndexFormat.UInt32 };
            var parts = new CombineInstance[raw.subMeshCount];
            for (int i = 0; i < parts.Length; i++) parts[i] = new CombineInstance { mesh = raw, subMeshIndex = i, transform = v.root.worldToLocalMatrix * v.skin.transform.localToWorldMatrix };
            mesh.CombineMeshes(parts, false, true); Destroy(raw); owned.Add(mesh); return mesh;
        }
        static void Ground(Mesh mesh, float floor)
        { var vertices = mesh.vertices; for (int i = 0; i < vertices.Length; i++) vertices[i].y -= floor; mesh.vertices = vertices; mesh.RecalculateBounds(); }
        void Update()
        {
            if (match == null || crowd == null || match.Paused) return;
            if (!match.Running) { if (running) crowd.ResetAttempt(); running = false; launches = attempts = -1; return; }
            if (!running || match.Launches < launches || match.CompletedAttempts < attempts) { crowd.ResetAttempt(); attempts = match.CompletedAttempts; }
            running = true;
            if (match.Launches != launches) { launches = match.Launches; crowd.ResetAttempt(); }
            if (match.CompletedAttempts > attempts) { attempts = match.CompletedAttempts; crowd.React(match.LastInches > 0, match.Timing, match.truck.body.position); }
        }
        void OnDestroy() { foreach (var mesh in owned) if (mesh != null) Destroy(mesh); owned.Clear(); }
    }
}

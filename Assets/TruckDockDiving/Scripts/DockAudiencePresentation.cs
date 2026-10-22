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
        public AnimationClip[] ambientClips = Array.Empty<AnimationClip>();
        public MeshFilter[] fans;
    }
    // Reuses the existing Field Goal crowd's pose/reaction lifecycle. Far fans
    // share bounded reusable animated meshes, rather than hundreds of live rigs.
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
        readonly List<Vector3> sampledVertices = new List<Vector3>();
        readonly List<Vector3> sampledNormals = new List<Vector3>();
        Mesh[] sampling;
        float[] sampledFloors;
        float nextPose;
        int poseIndex;
        public int AmbientSamples { get; private set; }
        public int BakedMeshes => owned.Count;
        public int Spectators => crowd == null ? 0 : crowd.Spectators;
        void Awake()
        {
            sampling = new Mesh[variants.Length];
            sampledFloors = new float[variants.Length];
            crowd.batches = new FieldGoalCrowd.Batch[variants.Length];
            for (int i = 0; i < variants.Length; i++)
            {
                var v = variants[i]; v.skin.forceRenderingOff = true; v.root.gameObject.SetActive(true); v.animator.enabled = false;
                var idle = Bake(v, v.idle, i * .071f + .2f); float floor = idle.bounds.min.y; sampledFloors[i]=floor; Ground(idle, floor);
                var cheer = Bake(v, v.cheer, i * .053f + .35f); Ground(cheer, floor);
                foreach (var fan in v.fans) fan.sharedMesh = idle;
                crowd.batches[i] = new FieldGoalCrowd.Batch { idle = idle, cheer = cheer, fans = v.fans, materials = v.skin.sharedMaterials, seats = v.fans.Select(f => f.transform.localToWorldMatrix).ToArray() };
                v.root.gameObject.SetActive(false);
                if(v.ambientClips.Length>0){sampling[i]=new Mesh{name="Reusable Dock source sampling"};owned.Add(sampling[i]);}
            }
        }
        Mesh Bake(DockAudienceVariant v, AnimationClip clip, float phase)
        {
            clip.SampleAnimation(v.animator.gameObject, clip.length * Mathf.Repeat(phase, 1));
            var mesh = new Mesh { name = "Owned runtime Dock audience pose · " + v.sourceName + " · " + clip.name }; v.skin.BakeMesh(mesh);
            // Preserve the source's shared vertex/submesh indexing. Combining a
            // copy for each material duplicates vertices; later skin samples do
            // not, which caused material seams in the first Windows close-up.
            var matrix=v.root.worldToLocalMatrix*v.skin.transform.localToWorldMatrix;var vertices=mesh.vertices;var normals=mesh.normals;
            for(int i=0;i<vertices.Length;i++){vertices[i]=matrix.MultiplyPoint3x4(vertices[i]);normals[i]=matrix.MultiplyVector(normals[i]).normalized;}
            mesh.vertices=vertices;mesh.normals=normals;mesh.RecalculateBounds();owned.Add(mesh);return mesh;
        }
        static void Ground(Mesh mesh, float floor)
        { var vertices = mesh.vertices; for (int i = 0; i < vertices.Length; i++) vertices[i].y -= floor; mesh.vertices = vertices; mesh.RecalculateBounds(); }
        void Update()
        {
            if (match == null || crowd == null || match.Paused) return;
            // One reusable source sampled per frame, at most eight source bakes
            // per second each. Hundreds of distant spectators share these meshes.
            // No new meshes, arrays, characters or materials in the update path.
            if(Time.time>=nextPose&&sampling!=null&&sampling.Length>0)
            {
                nextPose=Time.time+1f/64;int i=poseIndex++%variants.Length;var v=variants[i];
                if(sampling[i]!=null)
                {
                    bool reaction=crowd.Mood!="IDLE";
                    int activity=(Mathf.FloorToInt(Time.time/(9+i*.47f))+i)%v.ambientClips.Length;
                    var clip=reaction?v.cheer:v.ambientClips[activity];
                    clip.SampleAnimation(v.animator.gameObject,Mathf.Repeat(Time.time*(.84f+i*.031f)+i*.317f,clip.length));
                    v.skin.BakeMesh(sampling[i]);sampling[i].GetVertices(sampledVertices);sampling[i].GetNormals(sampledNormals);
                    var matrix=v.root.worldToLocalMatrix*v.skin.transform.localToWorldMatrix;
                    for(int n=0;n<sampledVertices.Count;n++){sampledVertices[n]=matrix.MultiplyPoint3x4(sampledVertices[n])-Vector3.up*sampledFloors[i];sampledNormals[n]=matrix.MultiplyVector(sampledNormals[n]).normalized;}
                    var destination=reaction?crowd.batches[i].cheer:crowd.batches[i].idle;
                    destination.SetVertices(sampledVertices);destination.SetNormals(sampledNormals);destination.RecalculateBounds();AmbientSamples++;
                }
            }
            if (!match.Running) { if (running) crowd.ResetAttempt(); running = false; launches = attempts = -1; return; }
            if (!running || match.Launches < launches || match.CompletedAttempts < attempts) { crowd.ResetAttempt(); attempts = match.CompletedAttempts; }
            running = true;
            if (match.Launches != launches) { launches = match.Launches; crowd.ResetAttempt(); }
            if (match.CompletedAttempts > attempts) { attempts = match.CompletedAttempts; crowd.React(match.LastInches > 0, match.Timing, match.truck.body.position); }
        }
        void OnDestroy() { foreach (var mesh in owned) if (mesh != null) Destroy(mesh); owned.Clear(); }
    }
}

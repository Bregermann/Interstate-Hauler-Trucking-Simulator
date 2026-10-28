using UnityEngine;
namespace LWS.TruckPetPals
{
    // A small authored activity patch, explicitly allowed by21.10. This is not
    // a voxel excavation system. Only a runtime clone receives a few terrain
    // holes; an actual shallow bowl mesh/collider supplies the displaced soil.
    public sealed class PetDigPatch : MonoBehaviour
    {
        public string id;
        public PetDigGround ground;
        public MeshFilter soil;
        public MeshCollider collision;
        public ParticleSystem dirt;
        public GameObject discovery;
        public float Progress { get; private set; }
        public int ActualDigSteps { get; private set; }
        public bool Discovered => Progress >= 1;
        Mesh owned;
        bool opened;
        float nextMesh;
        float lastDig;
        Vector3[] vertices;
        Vector2[] uv;
        int[] triangles;
        const int Rings = 8, Sides = 32;
        public bool Dig(Vector3 actualPet, bool actualScraping, float dt)
        {
            if (!actualScraping || Discovered || Vector3.Distance(actualPet, transform.position) > 5 || !float.IsFinite(dt) || dt <= 0 || dt > .25f) return false;
            Progress = Mathf.Min(1, Progress + dt / 4); ActualDigSteps++; lastDig=Time.time;Apply();
            if(dirt!=null){if(Discovered)dirt.Stop();else if(!dirt.isPlaying)dirt.Play();}return Discovered;
        }
        public void Restore(float progress) { Progress = progress; ActualDigSteps = 0; opened = false; nextMesh = 0;if(dirt!=null)dirt.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear); Apply(); }
        void Update(){if(dirt!=null&&dirt.isPlaying&&Time.time-lastDig>.25f)dirt.Stop();}
        void Apply()
        {
            if (discovery != null) discovery.SetActive(Discovered);
            soil.gameObject.SetActive(Progress > 0); if (Progress <= 0) return;
            if (!opened) { ground.OpenPatch(transform.position, 2.1f); opened = true; }
            if (Time.time < nextMesh && !Discovered) return; nextMesh = Time.time + .12f;
            if (owned == null)
            {
                owned = new Mesh { name = "Runtime shallow pet hole " + id }; owned.MarkDynamic(); soil.sharedMesh = owned;
                vertices = new Vector3[1 + Rings * (Sides + 1)]; uv = new Vector2[vertices.Length]; triangles = new int[Sides * 3 + (Rings - 1) * Sides * 6];
                for (int side = 0; side < Sides; side++) { int k = side * 3; triangles[k] = 0; triangles[k + 1] = 1 + side; triangles[k + 2] = 2 + side; }
                for (int ring = 1; ring < Rings; ring++) for (int side = 0; side < Sides; side++)
                {
                    int a = 1 + (ring - 1) * (Sides + 1) + side, b = a + Sides + 1, k = Sides * 3 + ((ring - 1) * Sides + side) * 6;
                    triangles[k] = a; triangles[k + 1] = b; triangles[k + 2] = a + 1; triangles[k + 3] = a + 1; triangles[k + 4] = b; triangles[k + 5] = b + 1;
                }
            }
            var center = transform.position; center.y = ground.Height(center) - .35f * Progress + .015f; vertices[0] = soil.transform.InverseTransformPoint(center);
            for (int ring = 1; ring <= Rings; ring++) for (int side = 0; side <= Sides; side++)
            {
                float r = ring / (float)Rings, angle = side / (float)Sides * Mathf.PI * 2; int k = 1 + (ring - 1) * (Sides + 1) + side;
                var p = transform.position + new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle)) * (r * 3.6f);
                p.y = ground.Height(p) - (1 - r * r) * .35f * Progress + .015f;
                vertices[k] = soil.transform.InverseTransformPoint(p); uv[k] = new Vector2(r * Mathf.Sin(angle), r * Mathf.Cos(angle));
            }
            owned.vertices = vertices; owned.uv = uv; owned.triangles = triangles; owned.RecalculateNormals(); owned.RecalculateBounds(); collision.sharedMesh = null; collision.sharedMesh = owned;
        }
        void OnDestroy() { if (owned != null) Destroy(owned); }
    }
}

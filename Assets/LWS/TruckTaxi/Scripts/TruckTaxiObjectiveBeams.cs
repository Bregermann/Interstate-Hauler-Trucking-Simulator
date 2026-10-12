using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace LWS.TruckTaxi
{
    // Reuses Compass's beacon art. Its one-shot BeaconAnimator destroys on disable, so persistent
    // objectives retain only the vendor mesh/material in a bounded, reusable renderer pool.
    [DisallowMultipleComponent]
    public sealed class TruckTaxiObjectiveBeams : MonoBehaviour
    {
        [Min(100)] public float height = 800;
        [Min(1)] public float diameter = 12;
        private readonly List<TruckTaxiNavigationObjective> objectives = new List<TruckTaxiNavigationObjective>(8);
        private readonly List<MeshRenderer> pool = new List<MeshRenderer>(8);
        private readonly HashSet<string> shown = new HashSet<string>();
        private MaterialPropertyBlock properties;
        private TruckTaxiGPSAdapter gps;
        private Mesh mesh;
        private Material material;
        private float nextRefresh;
        private bool attemptedLoad;
        public int VisibleCount { get; private set; }
        public string Diagnostic { get; private set; } = "Not initialized";

        public void Initialize(TruckTaxiGPSAdapter source)
        {
            // Native Unity allocation must not run in a MonoBehaviour constructor. Explicit
            // initialization also supports inactive objects whose Awake has not run yet.
            if(properties==null) properties=new MaterialPropertyBlock();
            gps=source;
            if(attemptedLoad) return;
            attemptedLoad=true;
            var prefab=Resources.Load<GameObject>("CNPro/Prefabs/POIBeacon");
            mesh=prefab!=null ? prefab.GetComponent<MeshFilter>()?.sharedMesh : null;
            material=prefab!=null ? prefab.GetComponent<Renderer>()?.sharedMaterial : null;
            Diagnostic=mesh!=null && material!=null ? "Compass beacon assets ready; rendered validation pending" :
                "Compass POIBeacon mesh/material unavailable";
        }

        private void LateUpdate()
        {
            if(Time.unscaledTime<nextRefresh) return;
            nextRefresh=Time.unscaledTime+.1f;
            Refresh();
        }

        public void Refresh()
        {
            VisibleCount=0;
            shown.Clear();
            if(isActiveAndEnabled && gps!=null && gps.isActiveAndEnabled && mesh!=null && material!=null)
            {
                gps.CopyNavigationObjectives(objectives);
                foreach(var objective in objectives)
                {
                    if(!TruckTaxiNavigationObjective.IsPointObjective(objective.Type) || !shown.Add(objective.TargetId)) continue;
                    if(VisibleCount==pool.Count) pool.Add(CreateBeam());
                    var beam=pool[VisibleCount++];
                    var bounds=mesh.bounds;
                    float width=Mathf.Max(1,diameter), tall=Mathf.Max(100,height);
                    Vector3 scale=new Vector3(width/Mathf.Max(.001f,bounds.size.x),
                        tall/Mathf.Max(.001f,bounds.size.y),width/Mathf.Max(.001f,bounds.size.z));
                    beam.transform.rotation=Quaternion.identity;
                    Vector3 parentScale=transform.lossyScale;
                    beam.transform.localScale=new Vector3(scale.x/Mathf.Max(.001f,Mathf.Abs(parentScale.x)),
                        scale.y/Mathf.Max(.001f,Mathf.Abs(parentScale.y)),scale.z/Mathf.Max(.001f,Mathf.Abs(parentScale.z)));
                    beam.transform.position=objective.Position-Vector3.Scale(bounds.center,scale)+Vector3.up*(tall*.5f);
                    var registry=gps.MapMarkers?.Registry;
                    Color color=registry!=null ? registry.Tint(objective.Type,TruckTaxiMapMarkerState.Active) : Color.cyan;
                    properties.Clear();
                    properties.SetColor("_Color",color*3);
                    properties.SetColor("_BaseColor",color*3);
                    properties.SetColor("_EmissionColor",color*4);
                    beam.SetPropertyBlock(properties);
                    beam.enabled=isActiveAndEnabled;
                }
            }
            for(int i=VisibleCount;i<pool.Count;i++) if(pool[i]!=null) pool[i].enabled=false;
        }

        private MeshRenderer CreateBeam()
        {
            var go=new GameObject("Truck Taxi objective beam (Compass art)");
            go.transform.SetParent(transform,false);
            go.AddComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial=material;
            renderer.shadowCastingMode=ShadowCastingMode.Off;
            renderer.receiveShadows=false;
            renderer.lightProbeUsage=LightProbeUsage.Off;
            renderer.reflectionProbeUsage=ReflectionProbeUsage.Off;
            return renderer;
        }
        private void OnDisable()
        {
            foreach(var beam in pool) if(beam!=null) beam.enabled=false;
            VisibleCount=0;
        }
        private void OnDestroy()
        {
            foreach(var beam in pool) if(beam!=null)
            { if(Application.isPlaying) Destroy(beam.gameObject); else DestroyImmediate(beam.gameObject); }
            pool.Clear();
        }
    }
}

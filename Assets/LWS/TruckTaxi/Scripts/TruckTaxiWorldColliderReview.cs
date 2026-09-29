using UnityEngine;

namespace LWS.TruckTaxi
{
    // Conservative review only: a flagged collider is never automatically deleted.
    public static class TruckTaxiWorldColliderReview
    {
        public static string Suspicion(Collider collider)
        {
            if(collider==null || collider.isTrigger || !collider.enabled || !collider.gameObject.activeInHierarchy) return null;
            if(collider is TerrainCollider)
            {
                var terrain=collider.GetComponent<Terrain>();
                return terrain!=null && terrain.enabled && terrain.terrainData!=null ? null : "Terrain collision without visible terrain";
            }
            Transform owner=collider.attachedRigidbody!=null ? collider.attachedRigidbody.transform : collider.transform;
            bool found=false; Bounds visible=default;
            foreach(var renderer in owner.GetComponentsInChildren<Renderer>(false))
            {
                if(!renderer.enabled || renderer.forceRenderingOff || renderer is ParticleSystemRenderer || renderer is LineRenderer) continue;
                bool transparent=true;
                foreach(var material in renderer.sharedMaterials)
                {
                    if(material==null) continue;
                    bool blends=material.HasProperty("_Surface") && material.GetFloat("_Surface")>.5f;
                    float alpha=material.HasProperty("_BaseColor") ? material.GetColor("_BaseColor").a : material.HasProperty("_Color") ? material.color.a : 1;
                    if(!blends || alpha>.01f) { transparent=false; break; }
                }
                if(transparent) continue;
                if(!found) { visible=renderer.bounds; found=true; } else visible.Encapsulate(renderer.bounds);
            }
            if(!found) return "Solid collider has no enabled corresponding geometry";
            visible.Expand(new Vector3(.6f,.6f,.6f)+visible.size*.2f);
            var physical=collider.bounds;
            if(!visible.Contains(physical.min) || !visible.Contains(physical.max)) return "Solid bounds exceed visible geometry by more than tolerance";
            return null;
        }
        public static string Hierarchy(Transform value)
        {
            string path=value.name;
            while(value.parent!=null) { value=value.parent; path=value.name+"/"+path; }
            return path;
        }
    }
}

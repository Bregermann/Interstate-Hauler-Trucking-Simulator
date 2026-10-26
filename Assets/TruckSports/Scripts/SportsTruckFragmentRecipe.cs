using System;
using UnityEngine;

namespace LWS.TruckSports
{
    // Derived rendering only. Physical roles remain the existing twelve rig
    // pieces; no prefab, collider, mass or force is stored in this recipe.
    public sealed class SportsTruckFragmentRecipe : ScriptableObject
    {
        [Serializable] public sealed class Part
        {
            public int rendererIndex, region;
            public Mesh sourceMesh, mesh;
            public int sourceMaterialCount, sourceMaterialIndex;
        }
        public int version=1;
        public string bodyId;
        public string sourceDependencyHash;
        public GameObject sourcePrefab;
        public Material capMaterial;
        public Part[] parts=Array.Empty<Part>();
        public static readonly string[] BodyRoles={"Chassis","Cab","door rt","doors lft","Exhaust","interior","mirror lft.001","mirror rt.001"};
        public static readonly string[] WheelRoles={"Whl HD FL","Whl HD FR","Whl HD RL","Whl HD RR"};
        public bool ValidFor(SportsTruckAppearanceCatalog.Body body,out string reason)
        {
            reason="";
            if(version!=1||body==null||body.id!=bodyId||body.visualPrefab!=sourcePrefab||capMaterial==null||parts==null||parts.Length==0){reason="The body has no validated wreck visual recipe.";return false;}
            var renderers=sourcePrefab.GetComponentsInChildren<Renderer>(true);var occupied=new bool[8];
            foreach(var part in parts)
            {
                if(part==null||part.region<0||part.region>=8||part.rendererIndex<0||part.rendererIndex>=renderers.Length||part.mesh==null||!part.mesh.isReadable||part.mesh.vertexCount<3||part.sourceMesh==null||part.sourceMaterialCount<1){reason="The wreck recipe contains incomplete rendering.";return false;}
                var source=renderers[part.rendererIndex].GetComponent<MeshFilter>();
                if(source==null||source.sharedMesh!=part.sourceMesh||renderers[part.rendererIndex].sharedMaterials.Length!=part.sourceMaterialCount||part.sourceMaterialIndex<0||part.sourceMaterialIndex>=part.sourceMaterialCount||part.mesh.subMeshCount>2){reason="The body rendering changed after its wreck recipe was authored.";return false;}
                occupied[part.region]=true;
            }
            foreach(bool present in occupied)if(!present){reason="The wreck recipe has an empty body region.";return false;}
            return true;
        }
    }
}

#if UNITY_EDITOR && VISTA
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace LWS.TruckDragonKingdom.Editor
{
    public static partial class DragonKingdomRoadSurfaceProbe
    {
        [Serializable] public sealed class BaryRgbCavernEvidence
        {
            public string scope="REVERSIBLE_BARYCENTRIC_RGB_ZERO_DIAGNOSTIC_NOT_GEOMETRY_SHADER_OR_PRODUCTION_REPAIR";
            public Frame[] frames=Array.Empty<Frame>();
            public bool performed,stateRestored,copiesDestroyed,onlyRgbChanged,originalMeshPayloadExact,meshBindingsExact,materialsExact,collidersExact,cameraLightsLodsExact,propertyBlocksAbsent;
            public int copiedMeshes,changedRgbVertices,changedRgbComponents;
        }

        static byte[] CavernZeroBaryRgbBytes(Mesh source,out int changedVertices,out int changedComponents)
        {
            if(source==null||source.vertexCount==0||source.vertexBufferCount!=1||source.GetVertexAttributeFormat(VertexAttribute.Color)!=VertexAttributeFormat.Float32||source.GetVertexAttributeDimension(VertexAttribute.Color)!=4)
                throw new InvalidOperationException("Actual native single-stream Float32 RGBA mesh required; no vertex-layout conversion.");
            int stride=source.GetVertexBufferStride(0),offset=source.GetVertexAttributeOffset(VertexAttribute.Color);
            if(offset<0||offset+16>stride)throw new InvalidOperationException("Complete native color payload required.");
            changedVertices=changedComponents=0;
            using(var data=Mesh.AcquireReadOnlyMeshData(source))
            {
                var bytes=data[0].GetVertexData<byte>(0).ToArray();
                if(bytes.Length!=source.vertexCount*stride)throw new InvalidOperationException("Complete original raw vertex stream required.");
                for(int vertex=0;vertex<source.vertexCount;vertex++)
                {
                    bool changed=false;
                    for(int component=0;component<3;component++)
                    {
                        int at=vertex*stride+offset+component*4;
                        bool nonzero=bytes[at]!=0||bytes[at+1]!=0||bytes[at+2]!=0||bytes[at+3]!=0;
                        if(nonzero){changedComponents++;changed=true;}
                        Array.Clear(bytes,at,4);
                    }
                    if(changed)changedVertices++;
                }
                return bytes;
            }
        }
        public static Mesh CreateCavernZeroBaryRgbMesh(Mesh source)
        {
            var bytes=CavernZeroBaryRgbBytes(source,out _,out _);
            var copy=UnityEngine.Object.Instantiate(source);copy.name=source.name+" temporary barycentric RGB zero";copy.hideFlags=HideFlags.HideAndDontSave;
            try{copy.SetVertexBufferData(bytes,0,0,bytes.Length,0,MeshUpdateFlags.DontRecalculateBounds|MeshUpdateFlags.DontValidateIndices);return copy;}
            catch{UnityEngine.Object.DestroyImmediate(copy);throw;}
        }
        public static bool CavernBaryRgbOnlyChangeExact(Mesh original,Mesh candidate)
        {
            if(original==null||candidate==null||original==candidate)return false;
            var expected=CreateCavernZeroBaryRgbMesh(original);
            try{return StandardLitMeshPayload(expected)==StandardLitMeshPayload(candidate);}
            finally{UnityEngine.Object.DestroyImmediate(expected);}
        }
        static void CaptureCavernBaryRgbDiagnostic(Camera camera,DragonKingdomWorld world,HashSet<Collider> roads,Shader shader,string output,MeshRenderer[] renderers,BaryRgbCavernEvidence proof)
        {
            proof.performed=true;RequireCavernStandardLitNoPropertyBlocks(renderers);proof.propertyBlocksAbsent=true;
            var filters=renderers.Select(r=>r.GetComponent<MeshFilter>()).ToArray();var bindings=filters.ToDictionary(f=>f,f=>f.sharedMesh);
            var payload=bindings.Values.Distinct().ToDictionary(m=>m,StandardLitMeshPayload);
            var materials=renderers.ToDictionary(r=>r,r=>r.sharedMaterials);var materialJson=materials.Values.SelectMany(m=>m).Distinct().ToDictionary(m=>m,m=>EditorJsonUtility.ToJson(m));
            var rendererJson=renderers.ToDictionary(r=>r,r=>EditorJsonUtility.ToJson(r)+EditorJsonUtility.ToJson(r.transform));
            var physical=world.GetComponentsInChildren<MeshCollider>(true).ToDictionary(c=>c,c=>c.sharedMesh);var physicalJson=physical.Keys.ToDictionary(c=>c,c=>EditorJsonUtility.ToJson(c)+EditorJsonUtility.ToJson(c.transform));
            var lods=world.GetComponentsInChildren<LODGroup>(true).ToDictionary(g=>g,g=>EditorJsonUtility.ToJson(g)+EditorJsonUtility.ToJson(g.transform));
            var lights=world.gameObject.scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Light>(true)).ToDictionary(l=>l,l=>EditorJsonUtility.ToJson(l)+EditorJsonUtility.ToJson(l.transform));
            string cameraJson=EditorJsonUtility.ToJson(camera)+EditorJsonUtility.ToJson(camera.transform);
            var copies=new Dictionary<Mesh,Mesh>();var frames=new List<Frame>();proof.onlyRgbChanged=true;
            bool Restored()
            {
                proof.meshBindingsExact=bindings.All(p=>p.Key!=null&&p.Key.sharedMesh==p.Value);
                proof.originalMeshPayloadExact=payload.All(p=>p.Key!=null&&p.Value==StandardLitMeshPayload(p.Key));
                proof.materialsExact=materials.All(p=>p.Key!=null&&p.Key.sharedMaterials.SequenceEqual(p.Value)&&rendererJson[p.Key]==EditorJsonUtility.ToJson(p.Key)+EditorJsonUtility.ToJson(p.Key.transform))&&materialJson.All(p=>p.Key!=null&&p.Value==EditorJsonUtility.ToJson(p.Key));
                proof.collidersExact=physical.All(p=>p.Key!=null&&p.Key.sharedMesh==p.Value&&physicalJson[p.Key]==EditorJsonUtility.ToJson(p.Key)+EditorJsonUtility.ToJson(p.Key.transform));
                proof.cameraLightsLodsExact=cameraJson==EditorJsonUtility.ToJson(camera)+EditorJsonUtility.ToJson(camera.transform)&&lights.All(p=>p.Key!=null&&p.Value==EditorJsonUtility.ToJson(p.Key)+EditorJsonUtility.ToJson(p.Key.transform))&&lods.All(p=>p.Key!=null&&p.Value==EditorJsonUtility.ToJson(p.Key)+EditorJsonUtility.ToJson(p.Key.transform));
                RequireCavernStandardLitNoPropertyBlocks(renderers);proof.copiesDestroyed=copies.Values.All(m=>m==null);
                return proof.meshBindingsExact&&proof.originalMeshPayloadExact&&proof.materialsExact&&proof.collidersExact&&proof.cameraLightsLodsExact&&proof.copiesDestroyed;
            }
            try
            {
                foreach(var original in payload.Keys)
                {
                    CavernZeroBaryRgbBytes(original,out int vertices,out int components);proof.changedRgbVertices+=vertices;proof.changedRgbComponents+=components;
                    var copy=CreateCavernZeroBaryRgbMesh(original);copies.Add(original,copy);proof.onlyRgbChanged&=CavernBaryRgbOnlyChangeExact(original,copy);
                }
                proof.copiedMeshes=copies.Count;
                if(!proof.onlyRgbChanged||proof.changedRgbVertices==0)throw new InvalidOperationException("Actual nonzero original barycentric RGB and exact all-other-payload preservation required.");
                foreach(var entry in bindings)entry.Key.sharedMesh=copies[entry.Value];
                frames.Add(Capture(camera,world,roads,shader,output,"rounded-entry-bary-rgb-diagnostic","barycentric-rgb-zero"));proof.frames=frames.ToArray();
                proof.onlyRgbChanged&=copies.All(p=>CavernBaryRgbOnlyChangeExact(p.Key,p.Value));
                if(!proof.onlyRgbChanged||frames[0].shaderErrors.Length!=0)throw new InvalidOperationException("Barycentric RGB-only pair altered another payload channel or reported shader errors.");
            }
            finally
            {
                foreach(var entry in bindings)if(entry.Key!=null)entry.Key.sharedMesh=entry.Value;
                foreach(var copy in copies.Values)if(copy!=null)UnityEngine.Object.DestroyImmediate(copy);
                proof.stateRestored=Restored();
            }
            if(!proof.stateRestored)throw new InvalidOperationException("Barycentric RGB pair failed exact saved mesh/material/collider/light/camera restoration.");
            frames.Add(Capture(camera,world,roads,shader,output,"rounded-entry-bary-rgb-diagnostic","original-barycentric-rgb-restored"));proof.frames=frames.ToArray();proof.stateRestored&=Restored();
            if(!proof.stateRestored||frames[1].shaderErrors.Length!=0)throw new InvalidOperationException("Restored barycentric capture changed original state or reported shader errors.");
        }
    }
}
#endif

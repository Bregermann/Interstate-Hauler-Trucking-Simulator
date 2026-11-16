#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace LWS.TruckDragonKingdom.Editor
{
    public static partial class DragonKingdomWorldAuthoring
    {
        // Exact saved-render authority remains unchanged. This helper only
        // compares a disposable SDK grading mesh against that authority.
        static void CompareOwnedMountainMeshPayload(Mesh actualMesh,Mesh savedMesh,OwnedMountainNativeMeshComparison comparison)
        {
                string MeshDigest(Mesh mesh)=>DragonKingdomMountainRoadLayoutAuthoring.Digest(System.Text.Encoding.UTF8.GetBytes(EditorJsonUtility.ToJson(mesh)));comparison.actualMeshPayloadBefore=MeshDigest(actualMesh);comparison.savedMeshPayloadBefore=MeshDigest(savedMesh);
                var actualVertices=actualMesh.vertices;var savedVertices=savedMesh.vertices;var actualNormals=actualMesh.normals;var savedNormals=savedMesh.normals;comparison.positionsExact=actualVertices.SequenceEqual(savedVertices);comparison.normalsExact=actualNormals.SequenceEqual(savedNormals);comparison.tangentsExact=actualMesh.tangents.SequenceEqual(savedMesh.tangents);comparison.colorsExact=actualMesh.colors.SequenceEqual(savedMesh.colors);comparison.actualBounds=actualMesh.bounds;comparison.savedBounds=savedMesh.bounds;comparison.boundsExact=comparison.actualBounds.Equals(comparison.savedBounds);if(actualVertices.Length==savedVertices.Length)for(int vertex=0;vertex<actualVertices.Length;vertex++)comparison.maximumVertexDelta=Mathf.Max(comparison.maximumVertexDelta,Vector3.Distance(actualVertices[vertex],savedVertices[vertex]));if(actualNormals.Length==savedNormals.Length)for(int normal=0;normal<actualNormals.Length;normal++)comparison.maximumNormalDelta=Mathf.Max(comparison.maximumNormalDelta,Vector3.Distance(actualNormals[normal],savedNormals[normal]));
                comparison.meshExact=LWS.TruckSports.Editor.SportsEasyRoadsAuthoring.EquivalentPreservedGeometry(actualMesh,savedMesh,out comparison.reason);comparison.physicalEquivalentAfterOnlyUv4Normalization=comparison.meshExact;
                if(!comparison.meshExact&&comparison.reason=="UV channel 3"&&comparison.positionsExact&&comparison.normalsExact&&comparison.tangentsExact&&comparison.colorsExact&&comparison.boundsExact)
                {
                    // EasyRoads UV4 carries terrain/detail render coordinates.
                    // Preserve both real meshes. Only a disposable comparison
                    // clone receives the saved UV4; FULL comparison then still
                    // checks UV5-8, bindposes, topology and every index.
                    var actualUv=new List<Vector4>();var savedUv=new List<Vector4>();actualMesh.GetUVs(3,actualUv);savedMesh.GetUVs(3,savedUv);comparison.actualUv4Count=actualUv.Count;comparison.savedUv4Count=savedUv.Count;comparison.actualUv4First=actualUv.Count>0?actualUv[0]:Vector4.zero;comparison.savedUv4First=savedUv.Count>0?savedUv[0]:Vector4.zero;comparison.actualUv4Dimension=(actualMesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.TexCoord3)?actualMesh.GetVertexAttributeDimension(UnityEngine.Rendering.VertexAttribute.TexCoord3):0);comparison.savedUv4Dimension=(savedMesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.TexCoord3)?savedMesh.GetVertexAttributeDimension(UnityEngine.Rendering.VertexAttribute.TexCoord3):0);
                    string UvDigest(List<Vector4> values)=>DragonKingdomMountainRoadLayoutAuthoring.Digest(System.Text.Encoding.UTF8.GetBytes(string.Join("|",values.Select(v=>v.ToString("R")))));comparison.actualUv4Sha256=UvDigest(actualUv);comparison.savedUv4Sha256=UvDigest(savedUv);
                    var temporary=UnityEngine.Object.Instantiate(actualMesh);temporary.hideFlags=HideFlags.HideAndDontSave;
                    try{temporary.SetUVs(3,savedUv);comparison.physicalEquivalentAfterOnlyUv4Normalization=LWS.TruckSports.Editor.SportsEasyRoadsAuthoring.EquivalentPreservedGeometry(temporary,savedMesh,out comparison.normalizedComparisonFailure);}finally{UnityEngine.Object.DestroyImmediate(temporary);}
                }
                comparison.worldPoseExact=comparison.actualPosition.Equals(comparison.savedPosition)&&comparison.actualRotation.Equals(comparison.savedRotation)&&comparison.actualScale.Equals(comparison.savedScale);comparison.actualMeshPayloadAfter=MeshDigest(actualMesh);comparison.savedMeshPayloadAfter=MeshDigest(savedMesh);comparison.inputMeshesUnchanged=comparison.actualMeshPayloadBefore==comparison.actualMeshPayloadAfter&&comparison.savedMeshPayloadBefore==comparison.savedMeshPayloadAfter;if(!comparison.inputMeshesUnchanged)throw new InvalidOperationException("Physical comparison changed an actual or saved native mesh; no grading/adoption permitted.");
        }
    }
}
#endif

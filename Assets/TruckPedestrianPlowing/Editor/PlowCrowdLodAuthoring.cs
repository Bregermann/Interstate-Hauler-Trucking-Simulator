using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
namespace LWS.TruckPedestrianPlowing.Editor {
 // Native Unity mesh/LOD authoring over the existing original colored meshes; close and ragdoll art is unchanged.
 public static class PlowCrowdLodAuthoring {
  public static void Apply(PlowPedestrian ped){
   var old=ped.transform.Find("Distance crowd mesh");if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);
   var detailed=ped.GetComponentsInChildren<MeshRenderer>(true);var source=detailed.Where(r=>!r.transform.IsChildOf(ped.vest.transform)&&r!=ped.costumeNose&&r.GetComponent<TextMesh>()==null&&r.GetComponent<TMPro.TMP_Text>()==null&&r.GetComponent<MeshFilter>()?.sharedMesh!=null).ToArray();
   if(source.Length==0)return;ped.useDistanceRendering&=source.All(r=>r.sharedMaterials.Length==1&&(r.sharedMaterial==null||r.sharedMaterial.mainTexture==null||r.sharedMaterial.mainTexture==Texture2D.whiteTexture));var instances=new List<CombineInstance>();var temporary=new List<Mesh>();
   try{foreach(var r in source){var filter=r.GetComponent<MeshFilter>();var mesh=UnityEngine.Object.Instantiate(filter.sharedMesh);temporary.Add(mesh);var colors=new Color[mesh.vertexCount];Color tint=Color.white;var mat=r.sharedMaterial;if(mat!=null&&mat.HasProperty("_BaseColor"))tint=mat.GetColor("_BaseColor");else if(mat!=null&&mat.HasProperty("_Color"))tint=mat.GetColor("_Color");if(PlayerSettings.colorSpace==ColorSpace.Linear)tint=tint.linear;var original=mesh.colors;for(int i=0;i<colors.Length;i++)colors[i]=tint*(original.Length==colors.Length?original[i]:Color.white);mesh.colors=colors;for(int sub=0;sub<mesh.subMeshCount;sub++)instances.Add(new CombineInstance{mesh=mesh,subMeshIndex=sub,transform=ped.transform.worldToLocalMatrix*filter.transform.localToWorldMatrix});}
    var combined=new Mesh{name=ped.name+" original distant geometry",indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};combined.CombineMeshes(instances.ToArray(),true,true,false);combined.RecalculateBounds();string path=PlowAuthoring.Root+"/Models/"+ped.name+" distance.asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(saved==null){AssetDatabase.CreateAsset(combined,path);saved=combined;}else{EditorUtility.CopySerialized(combined,saved);UnityEngine.Object.DestroyImmediate(combined);EditorUtility.SetDirty(saved);}
    string materialPath=PlowAuthoring.Root+"/Materials/Crowd distance colors.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);if(material==null){material=new Material(Shader.Find("Universal Render Pipeline/Particles/Simple Lit"));material.SetColor("_BaseColor",Color.white);material.SetFloat("_Surface",0);material.SetFloat("_ZWrite",1);material.SetFloat("_SrcBlend",1);material.SetFloat("_DstBlend",0);material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");material.enableInstancing=true;AssetDatabase.CreateAsset(material,materialPath);}
    var model=new GameObject("Distance crowd mesh");model.transform.SetParent(ped.transform,false);model.AddComponent<MeshFilter>().sharedMesh=saved;var low=model.AddComponent<MeshRenderer>();low.sharedMaterial=material;low.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;model.layer=ped.gameObject.layer;
    var lod=ped.GetComponent<LODGroup>();if(lod==null)lod=ped.gameObject.AddComponent<LODGroup>();var distant=new List<Renderer>{low};distant.AddRange(detailed.Where(r=>(r.transform.IsChildOf(ped.vest.transform)&&r.GetComponent<TextMesh>()==null&&r.GetComponent<TMPro.TMP_Text>()==null)||r==ped.beacon.GetComponent<Renderer>()||r==ped.costumeNose));lod.SetLODs(new[]{new LOD(.035f,detailed),new LOD(.003f,distant.ToArray())});lod.localReferencePoint=Vector3.up;lod.size=2.2f;lod.fadeMode=LODFadeMode.None;ped.presentationLOD=lod;ped.distanceModel=low;ped.detailedRenderers=detailed;
   }finally{foreach(var mesh in temporary)UnityEngine.Object.DestroyImmediate(mesh);}
  }
 }
}

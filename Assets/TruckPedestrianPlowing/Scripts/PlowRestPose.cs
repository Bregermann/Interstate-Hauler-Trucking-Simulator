using System.Collections.Generic;
using UnityEngine;
namespace LWS.TruckPedestrianPlowing {
 // Native mesh combining freezes the actual settled pose, preserving the original flat-color character art.
 public static class PlowRestPose {
  public static Mesh Bake(PlowPedestrian ped){
   var sources=new List<MeshRenderer>();
   foreach(var renderer in ped.detailedRenderers){
    if(!(renderer is MeshRenderer source)||!source.enabled||!source.gameObject.activeInHierarchy||source.GetComponent<TMPro.TMP_Text>()!=null)continue;
    // TMP removes its generated MeshFilter when its label is disabled; Unity's destroyed-object null is not C# null.
    var filter=source.GetComponent<MeshFilter>();if(filter==null)continue;var mesh=filter.sharedMesh;if(mesh==null)continue;
    if(!mesh.isReadable||source.sharedMaterials.Length!=1)return null;
    var material=source.sharedMaterial;if(material!=null&&material.mainTexture!=null&&material.mainTexture!=Texture2D.whiteTexture)return null;
    sources.Add(source);
   }
   if(sources.Count==0)return null;
   var parts=new List<CombineInstance>();var temporary=new List<Mesh>();var properties=new MaterialPropertyBlock();
   try{
    foreach(var source in sources){
     var filter=source.GetComponent<MeshFilter>();var mesh=Object.Instantiate(filter.sharedMesh);temporary.Add(mesh);
     var material=source.sharedMaterial;Color tint=Color.white;
     if(material!=null&&material.HasProperty("_BaseColor"))tint=material.GetColor("_BaseColor");else if(material!=null&&material.HasProperty("_Color"))tint=material.GetColor("_Color");
     properties.Clear();source.GetPropertyBlock(properties);if(properties.HasProperty(Shader.PropertyToID("_BaseColor")))tint=properties.GetColor("_BaseColor");
     if(QualitySettings.activeColorSpace==ColorSpace.Linear)tint=tint.linear;
     var original=mesh.colors;var colors=new Color[mesh.vertexCount];for(int i=0;i<colors.Length;i++)colors[i]=tint*(original.Length==colors.Length?original[i]:Color.white);mesh.colors=colors;
     for(int sub=0;sub<mesh.subMeshCount;sub++)parts.Add(new CombineInstance{mesh=mesh,subMeshIndex=sub,transform=ped.distanceModel.transform.worldToLocalMatrix*source.transform.localToWorldMatrix});
    }
    var combined=new Mesh{name="Settled cartoon ragdoll pose",indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};combined.CombineMeshes(parts.ToArray(),true,true,false);combined.RecalculateBounds();return combined;
   }finally{foreach(var mesh in temporary)Object.Destroy(mesh);}
  }
 }
}

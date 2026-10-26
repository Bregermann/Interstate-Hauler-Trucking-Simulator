using System.Collections.Generic;
using UnityEngine;
namespace LWS.TruckSports {
 // Clone only authored render geometry and bones; never instantiate gameplay
 // scripts and attempt to tear down RequireComponent physics dependencies.
 public static class SportsReplayVisuals {
  public static GameObject Copy(Transform source){
   var map=new Dictionary<Transform,Transform>();var root=Tree(source,null,map);
   foreach(var pair in map){var original=pair.Key;var copy=pair.Value;var mesh=original.GetComponent<MeshFilter>();var renderer=original.GetComponent<MeshRenderer>();
    if(mesh!=null&&renderer!=null){copy.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh.sharedMesh;Apply(renderer,copy.gameObject.AddComponent<MeshRenderer>());}
    var skin=original.GetComponent<SkinnedMeshRenderer>();if(skin!=null){var target=copy.gameObject.AddComponent<SkinnedMeshRenderer>();target.enabled=false;target.quality=skin.quality;target.updateWhenOffscreen=true;
     var bones=skin.bones;var mirrored=new Transform[bones.Length];for(int i=0;i<bones.Length;i++)if(bones[i]!=null&&map.TryGetValue(bones[i],out var bone))mirrored[i]=bone;target.bones=mirrored;
     if(skin.rootBone!=null&&map.TryGetValue(skin.rootBone,out var rootBone))target.rootBone=rootBone;
     target.sharedMesh=skin.sharedMesh;target.localBounds=skin.localBounds;Apply(skin,target);if(skin.sharedMesh!=null)for(int i=0;i<skin.sharedMesh.blendShapeCount;i++)target.SetBlendShapeWeight(i,skin.GetBlendShapeWeight(i));}
    var line=original.GetComponent<LineRenderer>();if(line!=null){var target=copy.gameObject.AddComponent<LineRenderer>();target.useWorldSpace=line.useWorldSpace;target.positionCount=line.positionCount;var points=new Vector3[line.positionCount];line.GetPositions(points);target.SetPositions(points);target.widthCurve=line.widthCurve;target.widthMultiplier=line.widthMultiplier;target.colorGradient=line.colorGradient;target.numCapVertices=line.numCapVertices;target.numCornerVertices=line.numCornerVertices;target.alignment=line.alignment;Apply(line,target);}
   }
   return root.gameObject;
  }
  static Transform Tree(Transform source,Transform parent,Dictionary<Transform,Transform> map){var copy=new GameObject(source.name).transform;copy.SetParent(parent,false);copy.localPosition=parent==null?source.position:source.localPosition;copy.localRotation=parent==null?source.rotation:source.localRotation;copy.localScale=parent==null?source.lossyScale:source.localScale;map.Add(source,copy);foreach(Transform child in source)Tree(child,copy,map);copy.gameObject.SetActive(source.gameObject.activeSelf);return copy;}
  static void Apply(Renderer source,Renderer target){target.sharedMaterials=source.sharedMaterials;target.enabled=source.enabled;target.shadowCastingMode=source.shadowCastingMode;target.receiveShadows=source.receiveShadows;var block=new MaterialPropertyBlock();source.GetPropertyBlock(block);target.SetPropertyBlock(block);for(int index=0;index<source.sharedMaterials.Length;index++){block.Clear();source.GetPropertyBlock(block,index);if(!block.isEmpty)target.SetPropertyBlock(block,index);}}
 }
}

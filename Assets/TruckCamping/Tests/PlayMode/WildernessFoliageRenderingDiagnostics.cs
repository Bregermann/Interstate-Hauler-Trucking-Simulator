#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

// Test-owned GPU comparison. Does not reseed/edit the saved wilderness or
// change licensed source prefabs/materials. A diagnostic pass is not visual QA.
public sealed class WildernessFoliageRenderingDiagnostics
{
    const string Pine="Assets/Toby Fredson/Rocky Hills Environment - Whitebark Pine/RHEWP_Demo/Prefabs/Prefabs_Vegetation/PV_Trees/WhitebarkPineRHEP_B.prefab";
    const int Layer=31;
    [Serializable] sealed class RendererFact { public string name,mesh,material,shader,texture,textureFormat;public bool enabled,shaderSupported,instancing;public int vertices,subMeshes;public Vector3 boundsCenter,boundsSize; }
    [Serializable] sealed class Frame { public string mode,path;public float distance;public int nonBackgroundPixels,greenDominantPixels; }
    [Serializable] sealed class Report { public string scope="DIAGNOSTIC_ONLY_NO_FOLIAGE_ACCEPTANCE",pipeline,prefab;public int originalTerrainInstances;public RendererFact[] renderers;public Frame[] frames; }
    static object Get(object o,string n)=>WildernessNativeScope.Get(o,n);
    static Component[] Components(object o)=>WildernessNativeScope.Components(o);
    static void Layers(Transform root){root.gameObject.layer=Layer;foreach(Transform child in root)Layers(child);}
    static Frame Draw(Camera camera,RenderTexture target,string folder,string name,float distance)
    {
        var forward=new Vector3(-.4f,-.15f,1).normalized;var focus=new Vector3(10000,7.8f,10000);camera.transform.position=focus-forward*distance;camera.transform.rotation=Quaternion.LookRotation(forward);
        camera.Render();var previous=RenderTexture.active;var pixels=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
        try
        {
            RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,target.width,target.height),0,0);pixels.Apply();
            string path=Path.Combine(folder,name+".png");File.WriteAllBytes(path,pixels.EncodeToPNG());var colors=pixels.GetPixels32();var bg=(Color32)camera.backgroundColor;
            return new Frame{mode=name,path=path,distance=distance,nonBackgroundPixels=colors.Count(c=>Mathf.Abs(c.r-bg.r)+Mathf.Abs(c.g-bg.g)+Mathf.Abs(c.b-bg.b)>24),greenDominantPixels=colors.Count(c=>c.g>c.r*1.08f&&c.g>c.b*1.08f&&c.g>18)};
        }
        finally {RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(pixels);}
    }
    [UnityTest,Timeout(40000)] public IEnumerator DiagnosticOnlyActualWhitebarkLeavesManualPrefabVersusTerrainTreeUnderSavedPipeline()
    {
        using(var scope=new WildernessNativeScope("foliage-render-inputs"))
        {
            yield return scope.Load("TruckCamping","LWS.TruckCamping.CampingMatch");
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(Pine);Assert.That(source,Is.Not.Null);
            var world=Get(scope.match,"world");var originals=Components(Get(world,"terrains")).Cast<Terrain>().ToArray();
            var originalData=originals.Select(t=>t.terrainData).ToArray();var originalTrees=originalData.Select(t=>t.treeInstances).ToArray();
            var transientMaterials=new List<Material>();var root=new GameObject("Read-only transient foliage GPU fixture");var target=new RenderTexture(512,512,24,RenderTextureFormat.ARGB32);var terrainData=new TerrainData();
            string folder=Path.GetFullPath("Logs/TruckSports/Impossible07/WildernessFoliageDiagnosis");Directory.CreateDirectory(folder);
            try
            {
                var model=UnityEngine.Object.Instantiate(source,root.transform);model.transform.position=new Vector3(10000,0,10000);Layers(model.transform);
                var group=model.GetComponent<LODGroup>();Assert.That(group,Is.Not.Null);group.ForceLOD(0);
                var renderers=model.GetComponentsInChildren<MeshRenderer>(true);var lod=group.GetLODs()[0].renderers;
                var cameraObject=new GameObject("Owned foliage comparison camera");cameraObject.transform.SetParent(root.transform,false);var camera=cameraObject.AddComponent<Camera>();camera.enabled=false;camera.cullingMask=1<<Layer;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.035f,.055f,.075f,1);camera.fieldOfView=35;camera.nearClipPlane=.1f;camera.farClipPlane=500;camera.targetTexture=target;
                var facts=renderers.Select(renderer=>
                {
                    var filter=renderer.GetComponent<MeshFilter>();var material=renderer.sharedMaterial;var texture=material.HasProperty("_AlbedoMap")?material.GetTexture("_AlbedoMap"):material.mainTexture;
                    return new RendererFact{name=renderer.name,mesh=filter==null||filter.sharedMesh==null?"":AssetDatabase.GetAssetPath(filter.sharedMesh)+"/"+filter.sharedMesh.name,material=AssetDatabase.GetAssetPath(material),shader=material.shader.name,shaderSupported=material.shader.isSupported,texture=AssetDatabase.GetAssetPath(texture),textureFormat=texture is Texture2D t?t.format.ToString():"",instancing=material.enableInstancing,enabled=renderer.enabled,vertices=filter==null||filter.sharedMesh==null?0:filter.sharedMesh.vertexCount,subMeshes=filter==null||filter.sharedMesh==null?0:filter.sharedMesh.subMeshCount,boundsCenter=renderer.bounds.center,boundsSize=renderer.bounds.size};
                }).ToArray();
                var frames=new List<Frame>();
                // A real instantiated copy preserves original vendor LOD/material/
                // UV channels. ForceLOD changes only this transient clone.
                frames.Add(Draw(camera,target,folder,"manual-original-lod0-full",36));
                foreach(var renderer in lod)renderer.enabled=renderer.name.IndexOf("Leave",StringComparison.OrdinalIgnoreCase)>=0;
                frames.Add(Draw(camera,target,folder,"manual-original-lod0-leaves-only",36));
                var leaves=lod.Where(r=>r.name.IndexOf("Leave",StringComparison.OrdinalIgnoreCase)>=0).ToArray();var originalLeafMaterials=leaves.Select(r=>r.sharedMaterial).ToArray();
                try
                {
                    for(int i=0;i<leaves.Length;i++){var tint=new Material(originalLeafMaterials[i]);tint.SetColor("_AlbedoColor",new Color(.03f,.4f,.01f,1));transientMaterials.Add(tint);leaves[i].sharedMaterial=tint;}
                    frames.Add(Draw(camera,target,folder,"manual-leaves-ttfe-owned-copy-green-tint-control",36));
                    for(int i=0;i<leaves.Length;i++)
                    {
                        var material=new Material(Shader.Find("Universal Render Pipeline/Unlit"));material.SetTexture("_BaseMap",originalLeafMaterials[i].GetTexture("_AlbedoMap"));material.SetColor("_BaseColor",Color.white);material.SetFloat("_AlphaClip",1);material.SetFloat("_Cutoff",.3f);material.SetFloat("_Cull",0);material.EnableKeyword("_ALPHATEST_ON");transientMaterials.Add(material);leaves[i].sharedMaterial=material;
                    }
                    frames.Add(Draw(camera,target,folder,"manual-leaves-installed-urp-unlit-same-uv-atlas-alpha-control",36));
                }
                finally {for(int i=0;i<leaves.Length;i++)leaves[i].sharedMaterial=originalLeafMaterials[i];}

                foreach(var renderer in lod)renderer.enabled=renderer.name.IndexOf("Leave",StringComparison.OrdinalIgnoreCase)<0;
                frames.Add(Draw(camera,target,folder,"manual-original-lod0-bark-only",36));
                foreach(var renderer in lod)renderer.enabled=true;group.ForceLOD(-1);
                frames.Add(Draw(camera,target,folder,"manual-original-natural-lod-near",36));
                frames.Add(Draw(camera,target,folder,"manual-original-natural-lod-far",220));model.SetActive(false);
                // This is NEW transient fixture data, not any saved terrain.
                terrainData.heightmapResolution=33;terrainData.size=new Vector3(64,10,64);terrainData.treePrototypes=new[]{new TreePrototype{prefab=source,bendFactor=.2f}};
                terrainData.SetTreeInstances(new[]{new TreeInstance{position=new Vector3(.5f,0,.5f),prototypeIndex=0,heightScale=1,widthScale=1,color=Color.white,lightmapColor=Color.white,rotation=0}},false);
                var terrainObject=Terrain.CreateTerrainGameObject(terrainData);terrainObject.transform.SetParent(root.transform,false);terrainObject.transform.position=new Vector3(9968,0,9968);terrainObject.layer=Layer;
                var terrain=terrainObject.GetComponent<Terrain>();terrain.drawHeightmap=false;terrain.drawTreesAndFoliage=true;terrain.treeDistance=1100;terrain.treeBillboardDistance=180;terrain.treeCrossFadeLength=30;terrain.treeMaximumFullLODCount=60;
                yield return null;
                frames.Add(Draw(camera,target,folder,"terrain-original-near",36));frames.Add(Draw(camera,target,folder,"terrain-original-billboard-far",220));
                terrainObject.SetActive(false);
                foreach(var candidate in new[]{"Assets/PinwheelStudio/Vista/Personal/Samples/Prefabs/VistaSample_PineTree.prefab","Assets/NWH/Common/Vegetation/Trees/FirTree1.prefab"})
                {
                    var asset=AssetDatabase.LoadAssetAtPath<GameObject>(candidate);Assert.That(asset,Is.Not.Null,candidate);var alternative=UnityEngine.Object.Instantiate(asset,root.transform);alternative.transform.position=Vector3.zero;Layers(alternative.transform);
                    var alternativeLod=alternative.GetComponent<LODGroup>();if(alternativeLod!=null)alternativeLod.ForceLOD(0);
                    var rs=alternative.GetComponentsInChildren<Renderer>(true);Assert.That(rs.Length,Is.GreaterThan(0));var bounds=rs[0].bounds;foreach(var r in rs)bounds.Encapsulate(r.bounds);alternative.transform.localScale*=16.6f/bounds.size.y;
                    bounds=rs[0].bounds;foreach(var r in rs)bounds.Encapsulate(r.bounds);alternative.transform.position=new Vector3(10000-bounds.center.x,-bounds.min.y,10000-bounds.center.z);
                    frames.Add(Draw(camera,target,folder,"alternative-"+asset.name+"-near",36));frames.Add(Draw(camera,target,folder,"alternative-"+asset.name+"-far",220));
                    File.AppendAllText(Path.Combine(folder,"alternative-original-materials.txt"),candidate+"\n"+string.Join("\n",rs.SelectMany(r=>r.sharedMaterials).Where(m=>m!=null).Distinct().Select(m=>AssetDatabase.GetAssetPath(m)+" | "+m.shader.name+" | supported="+m.shader.isSupported))+"\n");alternative.SetActive(false);
                }
                var report=new Report{pipeline=GraphicsSettings.currentRenderPipeline==null?"Builtin":AssetDatabase.GetAssetPath(GraphicsSettings.currentRenderPipeline),prefab=Pine,originalTerrainInstances=originalTrees.Sum(t=>t.Length),renderers=facts,frames=frames.ToArray()};
                File.WriteAllText(Path.Combine(folder,"actual-vendor-manual-versus-terrain.json"),JsonUtility.ToJson(report,true));
                Assert.That(frames.Count,Is.EqualTo(13));Assert.That(facts.Any(f=>f.name.IndexOf("Leave",StringComparison.OrdinalIgnoreCase)>=0),Is.True);
                for(int i=0;i<originals.Length;i++){Assert.That(originals[i].terrainData,Is.SameAs(originalData[i]));CollectionAssert.AreEqual(originalTrees[i],originalData[i].treeInstances,"Saved original tree placements must remain byte-value equivalent; this fixture never reseeds them.");}
                TestContext.WriteLine("WILDERNESS_FOLIAGE_DIAGNOSTIC_ONLY frames=13 manual/terrain/leaf/bark/TTFE-tint/URP-unlit-atlas/Vista/NWH controls; inspect actual PNG+shader metadata before repair; no beauty/foliage pass claimed.");
            }
            finally {UnityEngine.Object.DestroyImmediate(root);foreach(var material in transientMaterials)UnityEngine.Object.DestroyImmediate(material);target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(terrainData);}
        }
    }
}
#endif

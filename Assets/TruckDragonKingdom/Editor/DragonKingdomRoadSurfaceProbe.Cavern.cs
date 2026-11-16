#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LWS.TruckDragonKingdom.Editor
{
    public static partial class DragonKingdomRoadSurfaceProbe
    {
        [Serializable]public sealed class SettlementWaterSample
        {public string region;public int samples,underSea;public float minimumTerrain,maximumTerrain,seaLevel;public Vector3 minimumPosition;}
        [Serializable]sealed class ProbeFinish{public bool singleSourceLightPerLantern=false;}
        [Serializable]sealed class ProbeComposition{public ProbeFinish finish=null;}
        [Serializable]public sealed class CavernProbeReceipt
        {
            public string scope="READ_ONLY_ACTUAL_CAVERN_LIGHT_RANGE_AND_MATERIAL_ABA_PLUS_SETTLEMENT_WATER_MEASUREMENT",preview,status="IN_PROGRESS",error;
            public bool sourcesExact,physicsAndHeightmapsExact,originalBindingsRestored,originalLightsRestored,diggerRenderersRestored;public int actualDiggerRenderers;
            public int cavernPointLights,sourceLampRoots,expectedSourcePointLights;public string sourceLampPrefab,sourceLightPrefab;public bool lampMeshSourcesExact,singleSourceLightPerLantern;public float originalMaximumRange;public Frame[] frames;public SettlementWaterSample[] settlements;
        }
        public static CavernProbeReceipt LastCavernReceipt{get;private set;}
        public static void CaptureActualCavernRoadLightingComparison()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new InvalidOperationException("Stopped compiled Editor required.");
            string[] args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonWorldCandidate");
            if(at<0||at+1>=args.Length||args[at+1].Any(c=>!char.IsLetterOrDigit(c)&&c!='-'&&c!='_'))throw new InvalidOperationException("Exact retained isolated candidate required.");
            string preview=Root+"VisualCandidates/"+args[at+1]+"/DragonKingdomPreview.unity";
            if(!File.Exists(IoPath(preview)))throw new InvalidOperationException("Retained native candidate preview required.");
            for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty||string.IsNullOrEmpty(SceneManager.GetSceneAt(i).path))throw new InvalidOperationException("Preserve user scene edits before read-only diagnosis.");
            var files=AssetDatabase.GetDependencies(preview,true).Concat(new[]{preview}).SelectMany(p=>new[]{p,p+".meta"}).Where(p=>File.Exists(IoPath(p))).Distinct().ToDictionary(p=>p,p=>Hash(File.ReadAllBytes(IoPath(p))));
            string manifest=Path.GetDirectoryName(preview)+"/Integration Receipt.json";if(!File.Exists(IoPath(manifest)))throw new InvalidOperationException("Actual retained candidate composition receipt required for source-backed lamp ownership.");files[manifest]=Hash(File.ReadAllBytes(IoPath(manifest)));
            var setup=EditorSceneManager.GetSceneManagerSetup();Scene scene=default;Camera camera=null;DragonKingdomWorld world=null;string physics="";string[] heightmaps=null;
            var bindings=new Dictionary<Renderer,Material[]>();var lights=new Dictionary<Light,float>();var diggerRenderers=new Dictionary<MeshRenderer,bool>();var temporary=new List<Material>();bool async=ShaderUtil.allowAsyncCompilation;
            var receipt=new CavernProbeReceipt{preview=preview};LastCavernReceipt=receipt;
            try
            {
                ShaderUtil.allowAsyncCompilation=false;scene=EditorSceneManager.OpenScene(preview,OpenSceneMode.Additive);EditorSceneManager.SetActiveScene(scene);
                world=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<DragonKingdomWorld>(true)).Single();
                physics=DragonKingdomWorldVisualMigration.PhysicsAndAnchorHash(world.gameObject);heightmaps=world.GetComponentsInChildren<Terrain>(true).Select(t=>DragonKingdomWorldVisualMigration.HeightHash(t.terrainData)).ToArray();
                var roads=new HashSet<Collider>(world.GetComponentsInChildren<MeshCollider>(true).Where(c=>c.transform.parent!=null&&c.transform.parent.name.StartsWith("Kingdom ",StringComparison.Ordinal)));
                foreach(var renderer in world.GetComponentsInChildren<Digger.Modules.Core.Sources.DiggerSystem>(true).SelectMany(s=>s.GetComponentsInChildren<MeshRenderer>(true)))diggerRenderers[renderer]=renderer.enabled;
                receipt.actualDiggerRenderers=diggerRenderers.Count;if(diggerRenderers.Count==0)throw new InvalidOperationException("Actual saved Digger floor/roof renderers required for the render-only occlusion comparison.");
                foreach(var road in roads){var renderer=road.GetComponent<Renderer>();if(renderer==null)throw new InvalidOperationException("Actual baked road renderer required.");bindings.Add(renderer,renderer.sharedMaterials);}
                var cavern=world.Region("caverns");foreach(var light in cavern.root.GetComponentsInChildren<Light>(true).Where(l=>l.type==LightType.Point&&l.enabled))lights.Add(light,light.range);
                receipt.cavernPointLights=lights.Count;receipt.originalMaximumRange=lights.Count==0?0:lights.Values.Max();
                receipt.sourceLampPrefab=DragonKingdomWorldAuthoring.Village+"props/P_PROP_lantern_04.prefab";receipt.sourceLightPrefab=DragonKingdomWorldAuthoring.Village+"light/Light_lantern.prefab";
                var lampSource=AssetDatabase.LoadAssetAtPath<GameObject>(receipt.sourceLampPrefab);var lightSource=AssetDatabase.LoadAssetAtPath<GameObject>(receipt.sourceLightPrefab);
                if(lampSource==null||lightSource==null)throw new InvalidOperationException("Actual retained source lantern and light prefabs required.");
                var lampRoots=cavern.root.Cast<Transform>().Where(t=>t.name.StartsWith("Owned working lantern caverns ",StringComparison.Ordinal)).ToArray();receipt.sourceLampRoots=lampRoots.Length;
                var sourceMeshes=new HashSet<Mesh>(lampSource.GetComponentsInChildren<MeshFilter>(true).Select(f=>f.sharedMesh));
                receipt.lampMeshSourcesExact=lampRoots.All(t=>new HashSet<Mesh>(t.GetComponentsInChildren<MeshFilter>(true).Select(f=>f.sharedMesh)).SetEquals(sourceMeshes));
                int sourceLampLights=lampSource.GetComponentsInChildren<Light>(true).Count(l=>l.type==LightType.Point&&l.enabled),sourceExtraLights=lightSource.GetComponentsInChildren<Light>(true).Count(l=>l.type==LightType.Point&&l.enabled);
                var composition=JsonUtility.FromJson<ProbeComposition>(File.ReadAllText(IoPath(manifest)));receipt.singleSourceLightPerLantern=composition.finish!=null&&composition.finish.singleSourceLightPerLantern;
                receipt.expectedSourcePointLights=12*(sourceLampLights+(receipt.singleSourceLightPerLantern?0:sourceExtraLights));
                var ownedLights=new HashSet<Light>(lampRoots.SelectMany(t=>t.GetComponentsInChildren<Light>(true)).Where(l=>l.type==LightType.Point&&l.enabled));
                if(lampRoots.Length!=12||!receipt.lampMeshSourcesExact||sourceLampLights!=1||sourceExtraLights!=1||lights.Count!=receipt.expectedSourcePointLights||!ownedLights.SetEquals(lights.Keys))throw new InvalidOperationException("Twelve source-mesh-backed lantern roots and their exact composition-declared native light count required; no ignored or fabricated light ownership.");
                var lit=Shader.Find("Universal Render Pipeline/Lit");if(lit==null||!lit.isSupported)throw new InvalidOperationException("Actual installed URP Lit required.");
                var variants=new Dictionary<Material,Material>();
                foreach(var source in bindings.Values.SelectMany(m=>m).Distinct())
                {
                    var material=new Material(lit){name="Temporary same-albedo native URP road diagnosis",hideFlags=HideFlags.HideAndDontSave,renderQueue=source.renderQueue};temporary.Add(material);
                    string albedo=source.HasProperty("_BaseMap")?"_BaseMap":"_MainTex";material.SetTexture("_BaseMap",source.GetTexture(albedo));material.SetTextureScale("_BaseMap",source.GetTextureScale(albedo));material.SetTextureOffset("_BaseMap",source.GetTextureOffset(albedo));
                    material.SetColor("_BaseColor",source.HasProperty("_BaseColor")?source.GetColor("_BaseColor"):source.GetColor("_Color"));
                    if(source.HasProperty("_BumpMap")){material.SetTexture("_BumpMap",source.GetTexture("_BumpMap"));material.SetTextureScale("_BumpMap",source.GetTextureScale("_BumpMap"));material.SetFloat("_BumpScale",source.GetFloat("_BumpScale"));material.EnableKeyword("_NORMALMAP");}
                    material.SetFloat("_Smoothness",source.HasProperty("_MainSmoothnessPower")?source.GetFloat("_MainSmoothnessPower"):.1f);material.SetFloat("_Metallic",0);variants.Add(source,material);
                }
                var go=new GameObject("Temporary controlled cavern road camera");SceneManager.MoveGameObjectToScene(go,scene);camera=go.AddComponent<Camera>();camera.enabled=false;camera.nearClipPlane=.3f;camera.farClipPlane=6500;camera.fieldOfView=58;camera.aspect=(float)Width/Height;camera.clearFlags=CameraClearFlags.Skybox;
                var entrance=world.definition.graph.edges.Single(e=>e.edgeId=="forest-caverns");Vector3 origin=cavern.root.position;
                camera.transform.position=entrance.samples.Select(s=>s.position).OrderBy(p=>(p-(origin+Vector3.left*80)).sqrMagnitude).First()+Vector3.up*6;camera.transform.LookAt(origin+new Vector3(0,15,0));Physics.SyncTransforms();
                var frames=new List<Frame>();string output=Output();Directory.CreateDirectory(output);
                foreach(string intervention in new[]{"saved-baseline","lantern-range120","vendor-ao2","vendor-white-packed","vendor-flat-normal","same-albedo-urp-lit-range120","vendor-no-digger-rendering","same-albedo-urp-lit-no-digger-rendering","restored-baseline"})
                {
                    bool expanded=intervention=="lantern-range120"||intervention=="same-albedo-urp-lit-range120",replacement=intervention.StartsWith("same-albedo-urp-lit",StringComparison.Ordinal),hideDigger=intervention.EndsWith("no-digger-rendering",StringComparison.Ordinal);
                    foreach(var pair in lights)pair.Key.range=expanded?120:pair.Value;
                    foreach(var pair in diggerRenderers)pair.Key.enabled=hideDigger?false:pair.Value;
                    var configured=new Dictionary<Material,Material>();
                    if(intervention=="vendor-ao2"||intervention=="vendor-white-packed"||intervention=="vendor-flat-normal")foreach(var source in bindings.Values.SelectMany(m=>m).Distinct())
                    {
                        var material=new Material(source){name="Temporary public vendor material parameter "+intervention,hideFlags=HideFlags.HideAndDontSave};temporary.Add(material);
                        if(intervention=="vendor-ao2")material.SetFloat("_OcclusionStrength",2);
                        else if(intervention=="vendor-white-packed")material.SetTexture("_Metallic",Texture2D.whiteTexture);
                        else material.SetFloat("_BumpScale",0);
                        configured.Add(source,material);
                    }
                    foreach(var pair in bindings)pair.Key.sharedMaterials=replacement?pair.Value.Select(m=>variants[m]).ToArray():configured.Count>0?pair.Value.Select(m=>configured[m]).ToArray():pair.Value;
                    var shader=replacement?lit:bindings.Values.First()[0].shader;var frame=Capture(camera,world,roads,shader,output,"cavern",intervention);frames.Add(frame);
                    if(frame.roadSamples<100||frame.shaderErrors.Length!=0)throw new InvalidOperationException("Actual controlled cavern frame must contain physical road pixels and compile correctly.");
                }
                receipt.frames=frames.ToArray();var sea=world.transform.Find("Owned Vista coastal outskirts/Actual RiverModeler coastal sea");if(sea==null)throw new InvalidOperationException("Actual retained coastal sea required.");float seaLevel=sea.GetComponent<MeshRenderer>().bounds.center.y;
                var measurements=new List<SettlementWaterSample>();foreach(string id in new[]{"village","harbor"})
                {
                    var region=world.Region(id);var record=new SettlementWaterSample{region=id,minimumTerrain=float.PositiveInfinity,maximumTerrain=float.NegativeInfinity,seaLevel=seaLevel};
                    for(int z=-140;z<=140;z+=10)for(int x=-180;x<=180;x+=10)
                    {Vector3 p=region.root.position+new Vector3(x,0,z);float y=DragonKingdomTerrainShoulderAuthoring.TerrainHeight(world,p);record.samples++;if(y<seaLevel-.05f)record.underSea++;if(y<record.minimumTerrain){record.minimumTerrain=y;record.minimumPosition=p;}record.maximumTerrain=Mathf.Max(record.maximumTerrain,y);}
                    measurements.Add(record);
                }
                receipt.settlements=measurements.ToArray();receipt.status="CAPTURED_MEASURED_REQUIRES_CAUSAL_IMAGE_REVIEW_NOT_A_FIX";
            }
            catch(Exception e){receipt.error=e.ToString();receipt.status="FAILED_READ_ONLY_DIAGNOSIS";throw;}
            finally
            {
                foreach(var pair in bindings)if(pair.Key!=null)pair.Key.sharedMaterials=pair.Value;foreach(var pair in lights)if(pair.Key!=null)pair.Key.range=pair.Value;foreach(var pair in diggerRenderers)if(pair.Key!=null)pair.Key.enabled=pair.Value;
                receipt.originalBindingsRestored=bindings.All(p=>p.Key!=null&&p.Key.sharedMaterials.SequenceEqual(p.Value));receipt.originalLightsRestored=lights.All(p=>p.Key!=null&&p.Key.range==p.Value);
                receipt.diggerRenderersRestored=diggerRenderers.All(p=>p.Key!=null&&p.Key.enabled==p.Value);
                receipt.physicsAndHeightmapsExact=world!=null&&physics==DragonKingdomWorldVisualMigration.PhysicsAndAnchorHash(world.gameObject)&&heightmaps.SequenceEqual(world.GetComponentsInChildren<Terrain>(true).Select(t=>DragonKingdomWorldVisualMigration.HeightHash(t.terrainData)));
                if(camera!=null)UnityEngine.Object.DestroyImmediate(camera.gameObject);foreach(var material in temporary)UnityEngine.Object.DestroyImmediate(material);ShaderUtil.allowAsyncCompilation=async;
                if(scene.IsValid()&&scene.isLoaded)EditorSceneManager.CloseScene(scene,true);EditorSceneManager.RestoreSceneManagerSetup(setup);
                receipt.sourcesExact=files.All(p=>File.Exists(IoPath(p.Key))&&Hash(File.ReadAllBytes(IoPath(p.Key)))==p.Value);Directory.CreateDirectory(Output());File.WriteAllText(Path.Combine(Output(),"actual-cavern-light-and-coast-probe.json"),JsonUtility.ToJson(receipt,true));
                if(!receipt.sourcesExact||!receipt.physicsAndHeightmapsExact||!receipt.originalBindingsRestored||!receipt.originalLightsRestored||!receipt.diggerRenderersRestored)throw new InvalidOperationException("Read-only source/physical/binding/light/renderer preservation failed.");
            }
        }
    }
}
#endif

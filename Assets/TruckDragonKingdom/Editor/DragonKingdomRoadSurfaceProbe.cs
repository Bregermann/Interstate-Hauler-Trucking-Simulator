#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LWS.TruckDragonKingdom.Editor
{
    // Read-only same-camera comparisons of real saved road/terrain geometry.
    // No synthetic surfaces, no saved material migration, no collider changes.
    public static partial class DragonKingdomRoadSurfaceProbe
    {
        const string Root="Assets/TruckDragonKingdom/";
        const string ShaderPath=Root+"PrivateLicensedEnvironment/DerivedEasyRoads/URPRoadReview/Owned ER Road URP.shader";
        const int Width=1280,Height=720;
        [Serializable]public sealed class Frame
        {public string view,intervention,png;public int roadSamples,greenRoadSamples,blackRoadSamples;public float meanRoadLuminance;public string[] shaderErrors;}
        [Serializable]public sealed class Receipt
        {
            public string scope="CONTROLLED_NATIVE_SAVED_ROAD_SHADER_COMPARISON_NO_WORLD_OR_MATERIAL_ADOPTION",preview,shaderPath=ShaderPath,status="IN_PROGRESS",error;
            public bool shaderSupported,sourceBytesPreserved,physicsAndHeightsExact,asynchronousCompilationDisabled;
            public int actualRoadRenderers;public Frame[] frames;
        }
        public static Receipt LastReceipt{get;private set;}
        public static void CaptureActualRoadSurfaceComparison()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new InvalidOperationException("Stopped compiled Editor required.");
            string preview=Root+"Scenes/DragonKingdomPreview.unity";
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonWorldCandidate");
            if(at>=0&&at+1<args.Length)
            {
                string tag=args[at+1];if(string.IsNullOrEmpty(tag)||tag.Any(c=>!char.IsLetterOrDigit(c)&&c!='-'&&c!='_'))throw new ArgumentException("Short actual candidate tag required.");
                preview=Root+"VisualCandidates/"+tag+"/DragonKingdomPreview.unity";
            }
            if(!File.Exists(preview)||!File.Exists(ShaderPath))throw new InvalidOperationException("Real saved preview and separately audited private vendor shader required.");
            for(int i=0;i<SceneManager.sceneCount;i++){var s=SceneManager.GetSceneAt(i);if(s.path==preview||s.isDirty||string.IsNullOrEmpty(s.path))throw new InvalidOperationException("Preserve and close user scenes before read-only shader comparison.");}
            var protectedPaths=AssetDatabase.GetDependencies(preview,true).Concat(new[]{preview,ShaderPath}).SelectMany(p=>new[]{p,p+".meta"}).Where(p=>File.Exists(IoPath(p))).Distinct().ToDictionary(p=>p,p=>Hash(File.ReadAllBytes(IoPath(p))));
            var setup=EditorSceneManager.GetSceneManagerSetup();Scene scene=default;Camera camera=null;var temporary=new List<Material>();
            var bindings=new Dictionary<Renderer,Material[]>();bool async=ShaderUtil.allowAsyncCompilation;var receipt=new Receipt{preview=preview};LastReceipt=receipt;
            string physicsBefore="";string[] heightsBefore=null;DragonKingdomWorld world=null;
            try
            {
                ShaderUtil.allowAsyncCompilation=false;receipt.asynchronousCompilationDisabled=true;
                scene=EditorSceneManager.OpenScene(preview,OpenSceneMode.Additive);EditorSceneManager.SetActiveScene(scene);
                world=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<DragonKingdomWorld>(true)).Single();
                physicsBefore=DragonKingdomWorldVisualMigration.PhysicsAndAnchorHash(world.gameObject);
                heightsBefore=world.GetComponentsInChildren<Terrain>(true).Select(t=>DragonKingdomWorldVisualMigration.HeightHash(t.terrainData)).ToArray();
                var roads=new HashSet<Collider>(world.GetComponentsInChildren<MeshCollider>(true).Where(c=>c.transform.parent!=null&&c.transform.parent.name.StartsWith("Kingdom ",StringComparison.Ordinal)));
                foreach(var road in roads){var renderer=road.GetComponent<Renderer>();if(renderer==null)throw new InvalidOperationException("Actual baked road mesh has no paired renderer.");bindings[renderer]=renderer.sharedMaterials;}
                receipt.actualRoadRenderers=bindings.Count;if(bindings.Count<17)throw new InvalidOperationException("Actual complete baked shared road network required.");
                var shader=AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);receipt.shaderSupported=shader!=null&&shader.isSupported;
                if(!receipt.shaderSupported)throw new InvalidOperationException("Actual packaged vendor URP road shader is unsupported on this native renderer.");
                var variants=new Dictionary<Material,Material>();
                foreach(var source in bindings.Values.SelectMany(m=>m).Distinct())
                {
                    if(source==null||source.mainTexture==null)throw new InvalidOperationException("Original road albedo binding required.");
                    var material=new Material(shader){name="Temporary same-texture vendor road comparison",hideFlags=HideFlags.HideAndDontSave,renderQueue=2450};temporary.Add(material);
                    material.SetTexture("_MainTex",source.mainTexture);material.SetTextureScale("_MainTex",source.mainTextureScale);material.SetTextureOffset("_MainTex",source.mainTextureOffset);
                    material.SetColor("_Color",source.HasProperty("_BaseColor")?source.GetColor("_BaseColor"):Color.white);
                    material.SetFloat("_MainMetallicPower",0);material.SetFloat("_MainSmoothnessPower",.1f);material.SetFloat("_OcclusionStrength",1);
                    if(source.HasProperty("_BumpMap")&&source.GetTexture("_BumpMap")!=null)material.SetTexture("_BumpMap",source.GetTexture("_BumpMap"));
                    variants.Add(source,material);
                }
                var go=new GameObject("Temporary actual road review camera");SceneManager.MoveGameObjectToScene(go,scene);camera=go.AddComponent<Camera>();camera.enabled=false;camera.nearClipPlane=.3f;camera.farClipPlane=6500;camera.fieldOfView=58;camera.aspect=(float)Width/Height;camera.clearFlags=CameraClearFlags.Skybox;
                var frames=new List<Frame>();string output=Output();Directory.CreateDirectory(output);
                foreach(string edgeId in new[]{"village-castle","village-forest"})
                {
                    var edge=world.definition.graph.edges.Single(e=>e.edgeId==edgeId);var sample=edge.samples.OrderBy(s=>Mathf.Abs(s.distanceFromStartMeters-115)).First();
                    Vector3 right=Vector3.Cross(Vector3.up,sample.forward).normalized;
                    camera.transform.position=sample.position-sample.forward*32+right*12+Vector3.up*14;camera.transform.LookAt(sample.position+sample.forward*24+Vector3.up);
                    foreach(int intervention in new[]{0,1,2})
                    {
                        foreach(var binding in bindings)binding.Key.sharedMaterials=intervention==0?binding.Value:binding.Value.Select(m=>variants[m]).ToArray();
                        foreach(var material in temporary){material.SetFloat("_OffsetFactor",intervention==2?-4:0);material.SetFloat("_OffsetUnit",intervention==2?-4:0);}
                        string label=intervention==0?"original-urp-lit":intervention==1?"vendor-urp-zero-offset":"vendor-urp-minus4-offset";
                        var frame=Capture(camera,world,roads,shader,output,edgeId,label);frames.Add(frame);receipt.frames=frames.ToArray();
                        if(frame.shaderErrors.Length!=0)throw new InvalidOperationException("Actual vendor road shader compilation failed: "+string.Join(" | ",frame.shaderErrors));
                        if(frame.roadSamples<100)throw new InvalidOperationException("Capture failed to include sufficient actual physical road pixels.");
                    }
                }
                receipt.status="CAPTURED_REQUIRES_ACTUAL_IMAGE_AND_ROAD_PIXEL_REVIEW";
            }
            catch(Exception exception){receipt.status="REJECTED_PROBE";receipt.error=exception.ToString();throw;}
            finally
            {
                foreach(var binding in bindings)if(binding.Key!=null)binding.Key.sharedMaterials=binding.Value;
                if(camera!=null)UnityEngine.Object.DestroyImmediate(camera.gameObject);foreach(var material in temporary)UnityEngine.Object.DestroyImmediate(material);
                receipt.physicsAndHeightsExact=world!=null&&physicsBefore==DragonKingdomWorldVisualMigration.PhysicsAndAnchorHash(world.gameObject)&&heightsBefore.SequenceEqual(world.GetComponentsInChildren<Terrain>(true).Select(t=>DragonKingdomWorldVisualMigration.HeightHash(t.terrainData)));
                ShaderUtil.allowAsyncCompilation=async;
                if(scene.IsValid()&&scene.isLoaded)EditorSceneManager.CloseScene(scene,true);EditorSceneManager.RestoreSceneManagerSetup(setup);
                receipt.sourceBytesPreserved=protectedPaths.All(p=>File.Exists(IoPath(p.Key))&&Hash(File.ReadAllBytes(IoPath(p.Key)))==p.Value);
                string output=Output();Directory.CreateDirectory(output);File.WriteAllText(Path.Combine(output,"actual-road-surface-probe.json"),JsonUtility.ToJson(receipt,true));
                if(!receipt.sourceBytesPreserved||!receipt.physicsAndHeightsExact)throw new InvalidOperationException("Read-only road probe failed source/physical preservation.");
            }
        }
        static Frame Capture(Camera camera,DragonKingdomWorld world,HashSet<Collider> roads,Shader shader,string output,string view,string label)
        {
            var rt=new RenderTexture(Width,Height,24,RenderTextureFormat.ARGB32);var prior=RenderTexture.active;var texture=new Texture2D(Width,Height,TextureFormat.RGB24,false);
            try
            {
                camera.targetTexture=rt;camera.Render();camera.Render();RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,Width,Height),0,0);texture.Apply();
                var pixels=texture.GetPixels32();var record=new Frame{view=view,intervention=label,png=Path.Combine(output,view+"-"+label+".png")};float sum=0;
                for(int y=4;y<Height;y+=8)for(int x=4;x<Width;x+=8)
                {
                    var ray=camera.ScreenPointToRay(new Vector3(x,y,0));var hits=Physics.RaycastAll(ray,6500,~0,QueryTriggerInteraction.Ignore).Where(h=>h.collider.gameObject.scene==world.gameObject.scene).OrderBy(h=>h.distance).ToArray();
                    var road=hits.FirstOrDefault(h=>roads.Contains(h.collider));if(road.collider==null||hits[0].distance<road.distance-.5f)continue;
                    var color=pixels[y*Width+x];float r=color.r/255f,g=color.g/255f,b=color.b/255f,luma=.2126f*r+.7152f*g+.0722f*b;
                    record.roadSamples++;sum+=luma;if(luma<.055f)record.blackRoadSamples++;if(g>r*1.15f&&g>b*1.15f)record.greenRoadSamples++;
                }
                record.meanRoadLuminance=record.roadSamples==0?0:sum/record.roadSamples;
                record.shaderErrors=ShaderUtil.GetShaderMessages(shader).Where(m=>m.severity==UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error).Select(m=>m.message).ToArray();
                File.WriteAllBytes(record.png,texture.EncodeToPNG());return record;
            }
            finally{camera.targetTexture=null;RenderTexture.active=prior;UnityEngine.Object.DestroyImmediate(texture);rt.Release();UnityEngine.Object.DestroyImmediate(rt);}
        }
        static string Hash(byte[] data){using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(data));}
        static string IoPath(string path){string full=Path.GetFullPath(path);return Application.platform==RuntimePlatform.WindowsEditor&&full.Length>=248&&!full.StartsWith(@"\\?\",StringComparison.Ordinal)?@"\\?\"+full:full;}
        static string Output(){var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonOutput");return Path.Combine(at>=0&&at+1<args.Length?Path.GetFullPath(args[at+1]):"Logs/TruckSports/DragonKingdom","ActualRoadSurface");}
    }
}
#endif

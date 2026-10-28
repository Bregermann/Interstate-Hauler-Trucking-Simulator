#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

// A measurement fixture: success means the original GUI control and input matrix were acquired,
// never acceptance of the depth adapter. The unchanged original occlusion test remains authoritative.
public sealed class JengaFontInputDiagnostics
{
    const BindingFlags Any=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
    static Type Runtime(string n)=>Type.GetType(n+", Assembly-CSharp",true);
    static object Get(object o,string n)=>o.GetType().GetField(n,Any) is FieldInfo f?f.GetValue(o):o.GetType().GetProperty(n,Any).GetValue(o);
    static object Call(object o,string n,params object[] args)=>o.GetType().GetMethods(Any).Single(m=>m.Name==n&&m.GetParameters().Length==args.Length).Invoke(o,args);
    [Serializable] sealed class Frame { public string stage,material,shader,texture;public int white,nonBlack,queue;public float maximum,mean;public bool occluder,propertyBlock;public Color tint,blockTint; }
    [Serializable] sealed class Report
    {
        public string pipeline,graphicsDevice,font,fontMaterial,fontShader,atlas,atlasPath,atlasFormat,text,originalFogMode;
        public int atlasWidth,atlasHeight,textFontSize,characterInfoCount,sharedCacheBefore,sharedCacheAfter;
        public bool fog,shaderSupported,meshFilterPresent,fontDynamic;public float fogDensity;public Color fogColor,textColor,fontTint;
        public List<Frame> frames=new List<Frame>();public List<string> glyphs=new List<string>();
    }
    static Frame Capture(Camera camera,RenderTexture rt,MeshRenderer renderer,GameObject opaque,string stage,string dir)
    {
        camera.Render();var old=RenderTexture.active;var image=new Texture2D(rt.width,rt.height,TextureFormat.RGBA32,false);
        try
        {
            RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);image.Apply();File.WriteAllBytes(Path.Combine(dir,stage+".png"),image.EncodeToPNG());
            var colors=image.GetPixels();var m=renderer.sharedMaterial;var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block);
            return new Frame{stage=stage,material=m.name,shader=m.shader.name,texture=m.mainTexture==null?"NULL":m.mainTexture.name,white=colors.Count(c=>c.grayscale>.75f),nonBlack=colors.Count(c=>Mathf.Max(c.r,c.g,c.b)>.03f),maximum=colors.Max(c=>Mathf.Max(c.r,c.g,c.b)),mean=colors.Average(c=>c.grayscale),queue=m.renderQueue,occluder=opaque.activeSelf,propertyBlock=renderer.HasPropertyBlock(),tint=m.HasProperty("_Color")?m.GetColor("_Color"):Color.white,blockTint=block.GetColor("_Color")};
        }
        finally{RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(image);}
    }
    [UnityTest,Timeout(65000)] public IEnumerator DiagnosticOnlyActualFontTextureVertexTintFogAndPipelineInputMatrix()
    {
        IDisposable prefs=(IDisposable)Runtime("LWS.TruckSports.SportsPrefs").GetMethod("IsolateValidation").Invoke(null,new object[]{"JengaActualFontInputs"});
        var isolation=new GameObject("Font input diagnostic isolation");UnityEngine.Object.DontDestroyOnLoad(isolation);isolation.AddComponent(Runtime("LWS.TruckSports.SportsProbeInputIsolation"));
        GameObject root=null;Camera camera=null;RenderTexture rt=null;Material probe=null,opaqueMaterial=null,vendorSample=null;Behaviour match=null;bool oldFog=RenderSettings.fog;
        var report=new Report();string dir=Path.GetFullPath("Logs/TruckSports/PlaytestRepair/Impossible07JengaFontInputMatrix");Directory.CreateDirectory(dir);
        var cache=Runtime("LWS.TruckSports.SportsDepthTestedWorldText").GetProperty("SharedMaterialCount");
        try
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/TruckJenga/Scenes/TruckJenga.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;
            match=UnityEngine.Object.FindObjectsByType(Runtime("LWS.TruckJenga.JengaMatch")).Cast<Behaviour>().Single();Call(match,"AllBots");Assert.That((bool)Call(match,"StartMatch"),Is.True);Call(match,"Pause",0,false);yield return null;yield return null;
            oldFog=RenderSettings.fog;var bodies=((Array)Get(match,"containers")).Cast<Rigidbody>().ToArray();Assert.That(bodies.Length,Is.EqualTo(18));
            var poses=bodies.Select(b=>new[]{b.position.x,b.position.y,b.position.z,b.rotation.x,b.rotation.y,b.rotation.z,b.rotation.w,b.linearVelocity.x,b.linearVelocity.y,b.linearVelocity.z,b.angularVelocity.x,b.angularVelocity.y,b.angularVelocity.z}).ToArray();
            report.sharedCacheBefore=(int)cache.GetValue(null);
            var source=((Component)match).GetComponentsInChildren<TextMesh>(true).First(t=>t.name=="Real container number");
            root=new GameObject("Read-only actual font GPU input matrix");root.transform.position=new Vector3(0,-15000,0);
            var label=UnityEngine.Object.Instantiate(source.gameObject,root.transform).GetComponent<TextMesh>();label.gameObject.layer=31;label.transform.localPosition=Vector3.zero;label.transform.localRotation=Quaternion.identity;label.transform.localScale=Vector3.one;
            label.font.RequestCharactersInTexture(label.text,label.fontSize,label.fontStyle);yield return null;
            var renderer=label.GetComponent<MeshRenderer>();var owned=renderer.sharedMaterial;var font=label.font;var original=font.material;var atlas=original.mainTexture;
            report.pipeline=GraphicsSettings.currentRenderPipeline==null?"builtin":GraphicsSettings.currentRenderPipeline.GetType().FullName;report.graphicsDevice=SystemInfo.graphicsDeviceType+"/"+SystemInfo.graphicsDeviceName;
            report.font=font.name;report.fontDynamic=font.dynamic;report.fontMaterial=original.name;report.fontShader=original.shader.name;report.fontTint=original.HasProperty("_Color")?original.GetColor("_Color"):Color.white;
            report.atlas=atlas==null?"NULL":atlas.name;report.atlasPath=AssetDatabase.GetAssetPath(atlas);report.atlasWidth=atlas==null?0:atlas.width;report.atlasHeight=atlas==null?0:atlas.height;report.atlasFormat=atlas is Texture2D t?t.format.ToString():atlas==null?"NULL":atlas.GetType().Name;
            report.text=label.text;report.textColor=label.color;report.textFontSize=label.fontSize;report.characterInfoCount=font.characterInfo.Length;report.fog=oldFog;report.originalFogMode=RenderSettings.fogMode.ToString();report.fogDensity=RenderSettings.fogDensity;report.fogColor=RenderSettings.fogColor;report.meshFilterPresent=label.GetComponent<MeshFilter>()!=null;
            foreach(char ch in label.text){CharacterInfo info;if(font.GetCharacterInfo(ch,out info,label.fontSize,label.fontStyle))report.glyphs.Add(ch+" min="+info.minX+","+info.minY+" max="+info.maxX+","+info.maxY+" uv="+info.uvBottomLeft.ToString("R")+"/"+info.uvTopRight.ToString("R"));else report.glyphs.Add(ch+" MISSING");}
            var eye=new GameObject("Temporary Game Base font diagnostic camera");eye.transform.SetParent(root.transform,false);eye.transform.localPosition=new Vector3(0,0,-8);camera=eye.AddComponent<Camera>();camera.enabled=false;camera.cameraType=CameraType.Game;camera.orthographic=true;camera.orthographicSize=2;camera.nearClipPlane=.1f;camera.farClipPlane=20;camera.cullingMask=1<<31;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;camera.allowHDR=camera.allowMSAA=false;
            var dataType=Type.GetType("UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime",true);var data=eye.AddComponent(dataType);dataType.GetProperty("renderPostProcessing").SetValue(data,false);dataType.GetProperty("volumeLayerMask").SetValue(data,(LayerMask)0);
            opaqueMaterial=new Material(Shader.Find("Universal Render Pipeline/Unlit"));opaqueMaterial.SetColor("_BaseColor",Color.black);var opaque=GameObject.CreatePrimitive(PrimitiveType.Cube);opaque.transform.SetParent(root.transform,false);opaque.transform.localPosition=new Vector3(0,0,-2);opaque.transform.localScale=new Vector3(8,8,.2f);opaque.layer=31;UnityEngine.Object.DestroyImmediate(opaque.GetComponent<Collider>());opaque.GetComponent<Renderer>().sharedMaterial=opaqueMaterial;
            rt=new RenderTexture(256,256,24,RenderTextureFormat.ARGB32);rt.Create();camera.targetTexture=rt;
            opaque.SetActive(false);renderer.sharedMaterial=original;report.frames.Add(Capture(camera,rt,renderer,opaque,"00-original-GUI-visible",dir));opaque.SetActive(true);report.frames.Add(Capture(camera,rt,renderer,opaque,"01-original-GUI-occluded",dir));
                        var sampleShader=AssetDatabase.LoadAssetAtPath<Shader>("Assets/TruckJenga/Tests/PlayMode/JengaUrpSample3DText.shader");Assert.That(sampleShader,Is.Not.Null);vendorSample=new Material(sampleShader);vendorSample.mainTexture=atlas;vendorSample.SetColor("_Color",report.fontTint);renderer.sharedMaterial=vendorSample;opaque.SetActive(false);report.frames.Add(Capture(camera,rt,renderer,opaque,"vendor-URP-3DText-visible",dir));opaque.SetActive(true);report.frames.Add(Capture(camera,rt,renderer,opaque,"vendor-URP-3DText-occluded",dir));
            renderer.sharedMaterial=owned;opaque.SetActive(false);report.frames.Add(Capture(camera,rt,renderer,opaque,"02-owned-current-visible",dir));RenderSettings.fog=false;report.frames.Add(Capture(camera,rt,renderer,opaque,"03-owned-current-fog-disabled",dir));RenderSettings.fog=oldFog;
            var shader=AssetDatabase.LoadAssetAtPath<Shader>("Assets/TruckJenga/Tests/PlayMode/JengaFontInputDiagnostic.shader");Assert.That(shader,Is.Not.Null);report.shaderSupported=shader.isSupported;probe=new Material(shader);probe.mainTexture=atlas;probe.SetColor("_Color",report.fontTint);renderer.sharedMaterial=probe;
            string[] stages={"geometry-white","vertex-RGB","material-tint-RGB","atlas-RGB","atlas-alpha","alpha-only-font","old-RGBA-product-no-fog"};
            for(int i=0;i<stages.Length;i++){probe.SetFloat("_ProbeMode",i);report.frames.Add(Capture(camera,rt,renderer,opaque,"stage-"+i+"-"+stages[i],dir));}
            probe.SetFloat("_ProbeMode",5);opaque.SetActive(true);report.frames.Add(Capture(camera,rt,renderer,opaque,"alpha-only-font-behind-opaque",dir));renderer.sharedMaterial=owned;
            for(int i=0;i<bodies.Length;i++)CollectionAssert.AreEqual(poses[i],new[]{bodies[i].position.x,bodies[i].position.y,bodies[i].position.z,bodies[i].rotation.x,bodies[i].rotation.y,bodies[i].rotation.z,bodies[i].rotation.w,bodies[i].linearVelocity.x,bodies[i].linearVelocity.y,bodies[i].linearVelocity.z,bodies[i].angularVelocity.x,bodies[i].angularVelocity.y,bodies[i].angularVelocity.z},"All18 paused original physics poses/velocities remain exact.");
            UnityEngine.Object.DestroyImmediate(root);root=null;report.sharedCacheAfter=(int)cache.GetValue(null);File.WriteAllText(Path.Combine(dir,"actual-font-inputs.json"),JsonUtility.ToJson(report,true));
            TestContext.WriteLine("FONT_INPUT_MEASUREMENT_ONLY "+string.Join("; ",report.frames.Select(f=>f.stage+" white="+f.white+" nonBlack="+f.nonBlack+" max="+f.maximum)));
            Assert.That(report.frames[0].white,Is.GreaterThan(10),"Original native GUI font must provide a visible positive control.");Assert.That(report.sharedCacheAfter,Is.EqualTo(report.sharedCacheBefore),"Diagnostic clone must release its shared material binding.");Assert.That(report.frames.Count,Is.EqualTo(14));
        }
        finally
        {
            RenderSettings.fog=oldFog;if(camera!=null)camera.targetTexture=null;if(rt!=null){rt.Release();UnityEngine.Object.DestroyImmediate(rt);}if(root!=null)UnityEngine.Object.DestroyImmediate(root);if(probe!=null)UnityEngine.Object.DestroyImmediate(probe);if(vendorSample!=null)UnityEngine.Object.DestroyImmediate(vendorSample);if(opaqueMaterial!=null)UnityEngine.Object.DestroyImmediate(opaqueMaterial);Time.timeScale=1;if(match!=null)Call(match,"SetupScreen");UnityEngine.Object.DestroyImmediate(isolation);prefs.Dispose();
        }
    }
}
#endif

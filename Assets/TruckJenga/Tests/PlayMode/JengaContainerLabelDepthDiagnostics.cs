#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
public sealed class JengaContainerLabelDepthDiagnostics
{
    const BindingFlags Any=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
    static Type Runtime(string name)=>Type.GetType(name+", Assembly-CSharp",true);
    static object Get(object o,string name)=>o.GetType().GetField(name,Any) is FieldInfo field?field.GetValue(o):o.GetType().GetProperty(name,Any).GetValue(o);
    static object Call(object o,string name,params object[] args)=>o.GetType().GetMethods(Any).Single(m=>m.Name==name&&m.GetParameters().Length==args.Length).Invoke(o,args);
    static int RenderWhite(Camera camera,RenderTexture target,string path)
    {
        camera.Render();var old=RenderTexture.active;var pixels=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
        try{RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,target.width,target.height),0,0);pixels.Apply();File.WriteAllBytes(path,pixels.EncodeToPNG());return pixels.GetPixels().Count(c=>c.grayscale>.75f);}finally{RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(pixels);}
    }
    [UnityTest,Timeout(65000)]public IEnumerator OriginalActualContainerNumberFontMustRespectAnOpaqueDepthOccluder()
    {
        IDisposable prefs=(IDisposable)Runtime("LWS.TruckSports.SportsPrefs").GetMethod("IsolateValidation").Invoke(null,new object[]{"JengaOwnedFontDepthDiagnostic"});var isolate=new GameObject("Jenga font diagnostic input isolation");UnityEngine.Object.DontDestroyOnLoad(isolate);isolate.AddComponent(Runtime("LWS.TruckSports.SportsProbeInputIsolation"));GameObject root=null;Material material=null;RenderTexture target=null;Camera camera=null;Behaviour match=null;
        try
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/TruckJenga/Scenes/TruckJenga.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;match=UnityEngine.Object.FindObjectsByType(Runtime("LWS.TruckJenga.JengaMatch")).Cast<Behaviour>().Single();Call(match,"AllBots");Assert.That((bool)Call(match,"StartMatch"),Is.True);Call(match,"Pause",0,false);yield return null;yield return null;
            var pieces=((Array)Get(match,"containers")).Cast<Rigidbody>().ToArray();var positions=pieces.Select(b=>b.position).ToArray();var velocities=pieces.Select(b=>b.linearVelocity).ToArray();var source=((Component)match).GetComponentsInChildren<TextMesh>(true).First(t=>t.name=="Real container number");Assert.That(source.GetComponents<Component>().All(c=>c is Transform||c is TextMesh||c is MeshRenderer||c.GetType().FullName=="LWS.TruckSports.SportsDepthTestedWorldText"),Is.True,"Copy only the actual original render components and their depth-material adapter, never gameplay scripts/physics.");
            root=new GameObject("Controlled actual-font depth-only render diagnostic");root.transform.position=new Vector3(0,-15000,0);var label=UnityEngine.Object.Instantiate(source.gameObject,root.transform).GetComponent<TextMesh>();label.gameObject.layer=31;label.transform.localPosition=Vector3.zero;label.transform.localRotation=Quaternion.identity;label.transform.localScale=Vector3.one;label.font.RequestCharactersInTexture(label.text,label.fontSize,label.fontStyle);
            var eye=new GameObject("Temporary diagnostic Game/Base camera (not participant/no listener)");eye.transform.SetParent(root.transform,false);eye.transform.localPosition=new Vector3(0,0,-8);camera=eye.AddComponent<Camera>();camera.enabled=false;camera.cameraType=CameraType.Game;camera.orthographic=true;camera.orthographicSize=2;camera.nearClipPlane=.1f;camera.farClipPlane=20;camera.cullingMask=1<<31;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;camera.allowHDR=camera.allowMSAA=false;
            var dataType=Type.GetType("UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime",true);var data=eye.AddComponent(dataType);dataType.GetProperty("renderPostProcessing").SetValue(data,false);dataType.GetProperty("volumeLayerMask").SetValue(data,(LayerMask)0);
            var shader=Shader.Find("Universal Render Pipeline/Unlit");Assert.That(shader,Is.Not.Null);material=new Material(shader);material.SetColor("_BaseColor",Color.black);var opaque=GameObject.CreatePrimitive(PrimitiveType.Cube);opaque.name="Controlled opaque depth occluder";opaque.transform.SetParent(root.transform,false);opaque.transform.localPosition=new Vector3(0,0,-2);opaque.transform.localScale=new Vector3(8,8,.2f);opaque.layer=31;UnityEngine.Object.DestroyImmediate(opaque.GetComponent<Collider>());opaque.GetComponent<Renderer>().sharedMaterial=material;
            target=new RenderTexture(256,256,24,RenderTextureFormat.ARGB32);target.Create();camera.targetTexture=target;yield return null;string dir=Path.GetFullPath("Logs/TruckSports/PlaytestRepair/Impossible07JengaLabelDepth");Directory.CreateDirectory(dir);
            opaque.SetActive(false);int visible=RenderWhite(camera,target,Path.Combine(dir,"actual-font-visible.png"));opaque.SetActive(true);int occluded=RenderWhite(camera,target,Path.Combine(dir,"actual-font-behind-opaque-depth.png"));var font=source.GetComponent<Renderer>().sharedMaterial;
            TestContext.WriteLine("JENGA_FONT_DEPTH actualText="+source.text+" shader="+font.shader.name+" queue="+font.renderQueue+" visibleWhite="+visible+" occludedWhite="+occluded+"; isolated controlled actual font/mesh with ordinary Game/Base camera, no original gameplay geometry or state writes.");
            Assert.That(visible,Is.GreaterThan(10),"Actual native font must first be visibly rendered for the depth test to discriminate.");Assert.That(occluded,Is.LessThanOrEqualTo(Mathf.Max(3,visible*.03f)),"Opaque geometry must hide original container lettering; mirrored/back-face numbers through a tower cannot identify a legal piece reliably.");CollectionAssert.AreEqual(positions,pieces.Select(b=>b.position).ToArray());CollectionAssert.AreEqual(velocities,pieces.Select(b=>b.linearVelocity).ToArray());
        }
        finally{if(camera!=null)camera.targetTexture=null;if(target!=null){target.Release();UnityEngine.Object.DestroyImmediate(target);}if(root!=null)UnityEngine.Object.DestroyImmediate(root);if(material!=null)UnityEngine.Object.DestroyImmediate(material);Time.timeScale=1;if(match!=null)Call(match,"SetupScreen");UnityEngine.Object.DestroyImmediate(isolate);prefs.Dispose();}
    }
}
#endif

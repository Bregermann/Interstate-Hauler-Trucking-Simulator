#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class DockWorldLetteringTests
{
    static int Dark(Camera camera,RenderTexture target,Texture2D image)
    {
        camera.Render();var previous=RenderTexture.active;RenderTexture.active=target;image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);image.Apply();RenderTexture.active=previous;
        return image.GetPixels32().Count(p=>p.r<60&&p.g<60&&p.b<60);
    }
    [UnityTest]public IEnumerator ActualSavedWorldFontIsOccludedBySolidGeometry()
    {
        var prefs=Type.GetType("LWS.TruckSports.SportsPrefs, Assembly-CSharp",true);
        using((IDisposable)prefs.GetMethod("IsolateValidation").Invoke(null,new object[]{"DockWorldLettering"}))
        {
            GameObject root=null;RenderTexture target=null;Texture2D image=null;Material material=null;
            try
            {
                EditorSceneManager.LoadSceneInPlayMode("Assets/TruckDockDiving/Scenes/TruckDockDiving.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;
                var template=GameObject.Find("Depth-tested pool distance");Assert.That(template,Is.Not.Null);
                root=new GameObject("Explicit isolated world-font depth fixture");
                var clone=UnityEngine.Object.Instantiate(template,root.transform,false);clone.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);clone.transform.localScale=Vector3.one;
                var text=clone.GetComponents<Component>().Single(c=>c.GetType().FullName=="TMPro.TextMeshPro");text.GetType().GetProperty("text").SetValue(text,"50 FEET");text.GetType().GetMethod("ForceMeshUpdate",new[]{typeof(bool),typeof(bool)}).Invoke(text,new object[]{true,false});
                foreach(var t in clone.GetComponentsInChildren<Transform>(true))t.gameObject.layer=30;
                var camera=new GameObject("Isolated actual material camera").AddComponent<Camera>();camera.transform.SetParent(root.transform);camera.enabled=false;camera.transform.SetPositionAndRotation(new Vector3(0,0,-10),Quaternion.identity);camera.orthographic=true;camera.orthographicSize=1.2f;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.gray;camera.cullingMask=1<<30;camera.nearClipPlane=.1f;camera.farClipPlane=20;
                target=new RenderTexture(256,128,24);camera.targetTexture=target;image=new Texture2D(256,128,TextureFormat.RGBA32,false);
                yield return null;int visible=Dark(camera,target,image);Assert.That(visible,Is.GreaterThan(20),"The real saved SDF font must render before its occlusion is tested.");
                var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.SetParent(root.transform);wall.layer=30;wall.transform.position=new Vector3(0,0,-1);wall.transform.localScale=new Vector3(4.8f,2.4f,.1f);material=new Material(Shader.Find("Universal Render Pipeline/Unlit"));material.color=Color.white;wall.GetComponent<Renderer>().sharedMaterial=material;
                yield return null;int hidden=Dark(camera,target,image);Debug.Log("DOCK_WORLD_LETTER_DEPTH visibleDark="+visible+" occludedDark="+hidden);Assert.That(hidden,Is.LessThan(visible*.05f),"Distance lettering must not draw over an opaque spectator/geometry surface.");
            }
            finally{if(root!=null)UnityEngine.Object.Destroy(root);if(target!=null){target.Release();UnityEngine.Object.Destroy(target);}if(image!=null)UnityEngine.Object.Destroy(image);if(material!=null)UnityEngine.Object.Destroy(material);Time.timeScale=1;}
        }
    }
}
#endif

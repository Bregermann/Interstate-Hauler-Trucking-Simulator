using System;
using System.IO;
using System.Linq;
using LWS.TruckSports;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace LWS.TruckDerby.Editor
{
    public static class DerbyHudAuthoring
    {
        public const string Scene="Assets/TruckDemolitionDerby/Scenes/TruckDemolitionDerby.unity";
        [MenuItem("Truck Sports/Demolition Derby/Repair saved damage cards and branding")]
        public static void Upgrade()
        {
            var scene=EditorSceneManager.OpenScene(Scene);var match=UnityEngine.Object.FindAnyObjectByType<DerbyMatch>();
            if(match==null)throw new InvalidOperationException("Existing Derby match required.");
            var prior=match.GetComponent<DerbyDamageHud>();
            if(prior!=null)
            {
                if(prior.authoredVersion<2){var floor=GameObject.Find("Original Derby artwork · no collision").transform.Find("Center dirt logo");var texture=floor.GetComponent<Renderer>().sharedMaterial.mainTexture;floor.position=new Vector3(0,.05f,-27);floor.localScale=new Vector3(26,26f*texture.height/texture.width,1);prior.authoredVersion=2;EditorUtility.SetDirty(prior);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);}
                Debug.Log("DERBY_HUD_PRESERVED saved designer configuration.");return;
            }
            var theme=AssetDatabase.LoadAssetAtPath<SportsBroadcastTheme>("Assets/TruckSports/Resources/SportsBroadcastTheme.asset");
            var engine=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/NWH/Vehicle Physics 2/UI/Icons/Dashboard/CheckEngineLight.png");
            var body=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Heat - Complete Modern UI/Textures/Icons/HUD/Shield (64x).png");
            var logo=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/TruckSports/Art/Logos/TruckDemolitionDerby.png");
            if(theme==null||theme.headingFont==null||engine==null||body==null||logo==null)throw new InvalidOperationException("Existing Heat/NWH icons, shared font and original Derby PNG are required.");
            var hud=match.gameObject.AddComponent<DerbyDamageHud>();hud.match=match;hud.font=theme.headingFont;hud.engineIcon=engine;hud.bodyIcon=body;hud.authoredVersion=2;
            if(match.menus.GetComponent<SportsCompactViewportHud>()==null)match.menus.gameObject.AddComponent<SportsCompactViewportHud>().menus=match.menus;
            int hidden=0;foreach(var text in UnityEngine.Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                if(text.name=="Event screen readable rules"&&text.text.Contains("TRUCK SPORTS: PARTY!")&&text.text.Contains("DEMOLITION DERBY")){text.enabled=false;hidden++;}
            if(hidden!=2)throw new InvalidOperationException("Expected the two exact obsolete side text signs; found "+hidden);
            const string materialPath="Assets/TruckDemolitionDerby/Materials/Original Derby venue logo.mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if(material==null){material=new Material(Shader.Find("Universal Render Pipeline/Unlit"));material.SetTexture("_BaseMap",logo);material.SetColor("_BaseColor",Color.white);material.SetFloat("_Surface",1);material.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);material.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);material.SetFloat("_ZWrite",0);material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");material.renderQueue=(int)RenderQueue.Transparent;AssetDatabase.CreateAsset(material,materialPath);}
            var root=new GameObject("Original Derby artwork · no collision");
            Logo(root.transform,"Center dirt logo",new Vector3(0,.05f,-27),Quaternion.Euler(90,0,0),26,logo,material);
            Logo(root.transform,"Far stadium logo",new Vector3(0,11,45.2f),Quaternion.identity,29,logo,material);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Logs/TruckSports/PlaytestRepair");File.WriteAllText("Logs/TruckSports/PlaytestRepair/derby-hud-authoring.txt","Saved existing Derby scene; two exact side text renderers disabled, shared cards retained with owned damage/icon adapter, two original PNG signs without collision. Original physics, AI, replay and rules unchanged.");
        }
        static void Logo(Transform parent,string name,Vector3 at,Quaternion rotation,float width,Texture2D texture,Material material)
        {
            var node=GameObject.CreatePrimitive(PrimitiveType.Quad);node.name=name;node.transform.SetParent(parent);node.transform.SetPositionAndRotation(at,rotation);node.transform.localScale=new Vector3(width,width*texture.height/texture.width,1);UnityEngine.Object.DestroyImmediate(node.GetComponent<Collider>());
            var renderer=node.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
        }
    }
}

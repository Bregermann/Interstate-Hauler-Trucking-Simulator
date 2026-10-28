using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using LWS.TruckSports;
namespace LWS.TruckMoonMadness.Editor
{
    // Explicit render-label repair only. Never opens/saves/rebuilds a scene or touches physical records.
    public static class MoonPresentationAuthoring
    {
        const string Scene="Assets/TruckMoonMadness/Scenes/TruckMoonMadness.unity";
        [MenuItem("Truck Sports/Alpha 0.7/Repair OPEN Owned Moon Habitat Signs")]
        public static void RepairOpenOwnedHabitatLabels()
        {
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(EditorApplication.isPlaying||EditorApplication.isCompiling||scene.path!=Scene)throw new InvalidOperationException("Stopped exact original Moon scene required; caller owns open/save.");
            var match=UnityEngine.Object.FindObjectsByType<MoonMatch>(FindObjectsInactive.Include,FindObjectsSortMode.None).Single(m=>m.gameObject.scene==scene);
            var labels=match.GetComponentsInChildren<TextMesh>(true).Where(t=>t.name=="Habitat identity").OrderBy(t=>t.text,StringComparer.Ordinal).ToArray();if(labels.Length!=8||labels.Select(t=>t.transform.parent!=null?t.transform.parent.name:"").Distinct().Count()!=8)throw new InvalidOperationException("Exactly eight original native-authored habitat labels required.");
            // Full preflight before first component addition. Preserve partial/custom artwork for review.
            foreach(var label in labels)ValidateOriginal(label);
            int present=labels.Count(t=>t.GetComponent<SportsPerCameraBillboard>()!=null);
            if(present!=0&&present!=8)throw new InvalidOperationException("Partial sign configuration retained; no automatic overwrite.");
            if(present==8){foreach(var label in labels)if(!OwnedConfiguration(label.GetComponent<SportsPerCameraBillboard>()))throw new InvalidOperationException("Custom sign configuration retained.");Debug.Log("MOON_OWNED_SIGNS_ALREADY_CONFIGURED 8");return;}
            foreach(var label in labels)ConfigureNewLabel(label);
            EditorSceneManager.MarkSceneDirty(scene);Debug.Log("MOON_OWNED_SIGN_REPAIR8: original label text/position/rotation/scale and all physics preserved. Existing per-camera billboard caps actual144x24pixels,120m range; caller explicitly saves.");
        }
        static void ValidateOriginal(TextMesh label)
        {
            var t=label.transform;string prefix="Purpose-authored lunar habitat ";if(t.parent==null||!t.parent.name.StartsWith(prefix,StringComparison.Ordinal)||!int.TryParse(t.parent.name.Substring(prefix.Length),out int id)||id<1||id>8||label.text!="LUNAR RESEARCH / "+id||Mathf.Abs(label.characterSize-.38f)>.000001f||label.fontSize!=64||Vector3.Distance(t.localPosition,new Vector3(0,8,10))>.0001f||Vector3.Distance(t.localScale,Vector3.one)>.0001f||Quaternion.Angle(t.localRotation,Quaternion.identity)>.001f||label.GetComponent<Renderer>()==null)throw new InvalidOperationException("Original habitat label pose/text/font geometry changed; preserve designer edits: "+t.parent);
            if(label.gameObject.GetComponents<Component>().Any(c=>!(c is Transform)&&!(c is TextMesh)&&!(c is MeshRenderer)&&!(c is SportsPerCameraBillboard)))throw new InvalidOperationException("Additional custom sign component retained.");
        }
        static bool OwnedConfiguration(SportsPerCameraBillboard b)=>b!=null&&!b.distanceSizing&&b.maximumPixelWidth==144&&b.maximumPixelHeight==24&&b.maximumLabelDistance==120&&b.cameraFacingOffset==0;
        public static void ConfigureNewLabel(TextMesh label)
        {
            ValidateOriginal(label);if(label.GetComponent<SportsPerCameraBillboard>()!=null)throw new InvalidOperationException("Existing sign component retained.");
            var b=label.gameObject.AddComponent<SportsPerCameraBillboard>();b.distanceSizing=false;b.maximumPixelWidth=144;b.maximumPixelHeight=24;b.maximumLabelDistance=120;b.cameraFacingOffset=0;EditorUtility.SetDirty(b);
        }
    }
}

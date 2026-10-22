using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LWS.TruckBaseJumping.Editor
{
    // Explicit additive migration. Do not call the original scene generator.
    public static class BaseJumpPlaytestRepairAuthoring
    {
        const string Vendor="Assets/NWH/Vehicle Physics 2/Effects/Prefabs/ExhaustSmoke.prefab";
        const string Owned="Assets/TruckBaseJumping/Prefabs/Flare Smoke.prefab";
        const string SmokeMaterial="Assets/TruckBaseJumping/Materials/Flare smoke.mat";
        static void AlphaBlend(Material material)
        {
            // Lit transparent surfaces can preserve specular even at zero
            // alpha. On smoke billboards that reveals the square quad edges.
            material.SetFloat("_BlendModePreserveSpecular",0);
            UnityEditor.BaseShaderGUI.SetupMaterialBlendMode(material);
            EditorUtility.SetDirty(material);
        }
        static void Flipbook(ParticleSystem smoke)
        {
            var material=AssetDatabase.LoadAssetAtPath<Material>(SmokeMaterial);
            if(material==null)
            {
                material=new Material(smoke.GetComponent<ParticleSystemRenderer>().sharedMaterial){name="Flare smoke"};
                material.SetColor("_BaseColor",Color.white);material.SetColor("_Color",Color.white);material.SetFloat("_ColorMode",0);
                foreach(string keyword in new[]{"_COLORADDSUBDIFF_ON","_COLOROVERLAY_ON","_COLORCOLOR_ON"})material.DisableKeyword(keyword);
                AlphaBlend(material);
                AssetDatabase.CreateAsset(material,SmokeMaterial);
            }
            smoke.GetComponent<ParticleSystemRenderer>().sharedMaterial=material;
            // The installed SmokeTextureSheet contains SIX columns and FIVE
            // rows. Its exhaust prefab leaves UV animation disabled, which
            // renders all thirty cells on every billboard when used directly.
            var sheet=smoke.textureSheetAnimation;sheet.enabled=true;sheet.mode=ParticleSystemAnimationMode.Grid;sheet.animation=ParticleSystemAnimationType.WholeSheet;sheet.numTilesX=6;sheet.numTilesY=5;sheet.timeMode=ParticleSystemAnimationTimeMode.Lifetime;sheet.cycleCount=1;sheet.frameOverTime=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,0,1,.9999f));sheet.startFrame=0;
        }
        static GameObject SmokePrefab()
        {
            var ownedMaterial=AssetDatabase.LoadAssetAtPath<Material>(SmokeMaterial);
            if(ownedMaterial!=null&&ownedMaterial.GetFloat("_ColorMode")==0&&ownedMaterial.GetFloat("_BlendModePreserveSpecular")==1)AlphaBlend(ownedMaterial);
            var existing=AssetDatabase.LoadAssetAtPath<GameObject>(Owned);if(existing!=null){
                var prior=existing.GetComponent<ParticleSystem>();
                // Explicit migration of our known stock adaptation only. A
                // designer's enabled/custom flipbook or material is retained.
                if(!prior.textureSheetAnimation.enabled&&prior.GetComponent<ParticleSystemRenderer>().sharedMaterial==AssetDatabase.LoadAssetAtPath<Material>("Assets/NWH/Vehicle Physics 2/Effects/Materials/ExhaustSmoke.mat")){
                    var contents=PrefabUtility.LoadPrefabContents(Owned);try{Flipbook(contents.GetComponent<ParticleSystem>());PrefabUtility.SaveAsPrefabAsset(contents,Owned);}finally{PrefabUtility.UnloadPrefabContents(contents);}
                    existing=AssetDatabase.LoadAssetAtPath<GameObject>(Owned);
                }
                return existing;
            }
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(Vendor);if(source==null)throw new FileNotFoundException("Installed NWH smoke prefab is required",Vendor);
            var variant=(GameObject)PrefabUtility.InstantiatePrefab(source);
            try
            {
                variant.name="Flare Smoke";variant.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
                var smoke=variant.GetComponent<ParticleSystem>();if(smoke==null)throw new InvalidOperationException("NWH smoke asset has no ParticleSystem");
                var main=smoke.main;main.loop=true;main.playOnAwake=false;main.simulationSpace=ParticleSystemSimulationSpace.World;main.stopAction=ParticleSystemStopAction.None;main.startLifetime=new ParticleSystem.MinMaxCurve(2.5f,3.5f);main.startSpeed=new ParticleSystem.MinMaxCurve(.3f,.7f);main.startSize=new ParticleSystem.MinMaxCurve(.7f,1.3f);main.startColor=new Color(.68f,.63f,.54f,.65f);main.gravityModifier=-.05f;main.maxParticles=96;
                var emission=smoke.emission;emission.enabled=true;emission.rateOverTime=20;
                var collision=smoke.collision;collision.enabled=false;
                var inherit=smoke.inheritVelocity;inherit.enabled=true;inherit.mode=ParticleSystemInheritVelocityMode.Initial;inherit.curve=.12f;
                Flipbook(smoke);
                smoke.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
                Directory.CreateDirectory("Assets/TruckBaseJumping/Prefabs");AssetDatabase.Refresh();return PrefabUtility.SaveAsPrefabAsset(variant,Owned);
            }
            finally{UnityEngine.Object.DestroyImmediate(variant);}
        }
        [MenuItem("Truck Sports/BASE Jumping/Repair flare smoke without rebuilding scene")]
        public static void Upgrade()
        {
            var scene=EditorSceneManager.OpenScene("Assets/TruckBaseJumping/Scenes/TruckBaseJumping.unity");var match=UnityEngine.Object.FindAnyObjectByType<BaseJumpMatch>();if(match==null||match.skyField==null)throw new InvalidOperationException("Saved BASE Jumping sky pool is required");
            var source=SmokePrefab();int added=0,total=0;
            foreach(var item in match.skyField.bodies)
            {
                if(item.kind!=2)continue;total++;
                if(item.smoke==null){var root=(GameObject)PrefabUtility.InstantiatePrefab(source,scene);root.name="Pooled NWH flare smoke";root.transform.SetParent(item.transform,false);root.transform.localPosition=Vector3.zero;root.transform.localRotation=Quaternion.identity;item.smoke=root.GetComponent<ParticleSystem>();EditorUtility.SetDirty(item);added++;}
            }
            if(total!=4)throw new InvalidOperationException("Expected the saved four-flare pool, found "+total);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Debug.Log("BASE_FLARE_SMOKE_REPAIR existing flare pool="+total+" added="+added+" owned variant="+Owned+" native source="+Vendor);
        }
    }
}

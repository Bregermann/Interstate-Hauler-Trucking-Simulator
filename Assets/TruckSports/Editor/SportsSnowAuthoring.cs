using System;using System.IO;using System.Linq;using UnityEngine;using UnityEditor;using UnityEngine.Rendering.Universal;using LWS.TruckTaxi;using LWS.InterstateHauler;
namespace LWS.TruckSports.Editor {public static class SportsSnowAuthoring {
 public static void InstallScene(){if(UnityEngine.Object.FindAnyObjectByType<LWS.TruckTerrierRacing.TerrierSnow>()!=null)return;var weather=UnityEngine.Object.FindAnyObjectByType<SportsChaosWeather>();var cheese=UnityEngine.Object.FindAnyObjectByType<LWS.TruckCheeseRolling.CheeseWeather>();var jump=UnityEngine.Object.FindAnyObjectByType<LWS.TruckBaseJumping.BaseJumpMatch>();if(weather==null&&cheese==null&&jump==null)return;var owner=weather!=null?weather.gameObject:cheese!=null?cheese.gameObject:jump.gameObject;var snow=owner.GetComponent<SportsSnowSurface>();if(snow!=null){ConfigureDepthMask(snow);return;}snow=owner.AddComponent<SportsSnowSurface>();snow.weather=weather;snow.cheese=cheese;snow.baseJump=jump;snow.session=owner.GetComponent<SportsEventSession>();snow.cornhole=owner.GetComponent<LWS.TruckCornhole.CornholeMatch>();snow.surface=owner.AddComponent<TruckTaxiSnowSurface>();snow.adapter=owner.AddComponent<LwsWeatheradeAdapter>();var settings=new SerializedObject(snow.adapter);settings.FindProperty("attachOnEnable").boolValue=false;settings.FindProperty("autoBindWeatheradeRoadMaterials").boolValue=false;bool compact=owner.scene.name.StartsWith("TruckKeepaway",StringComparison.Ordinal);settings.FindProperty("areaSizeMeters").floatValue=compact?104:800;settings.FindProperty("areaDepthMeters").floatValue=compact?90:500;settings.FindProperty("depthLayerMask").intValue=~((1<<5)|(1<<24)|(1<<25)|(1<<26)|(1<<27)|(1<<28)|(1<<29)|(1<<30)|(1<<31));settings.ApplyModifiedPropertiesWithoutUndo();var rendering=owner.GetComponent<LWS.TruckBaseJumping.BaseJumpRendering>();var sourcePipeline=cheese!=null?cheese.match.look.pipeline:rendering!=null?rendering.pipeline:null;if(sourcePipeline==null)throw new Exception("Existing scoped weather pipeline required");string folder="Assets/TruckSports/Rendering";Directory.CreateDirectory(folder);AssetDatabase.Refresh();string path=folder+"/"+owner.scene.name+" snow pipeline.asset";var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);if(pipeline==null){pipeline=UnityEngine.Object.Instantiate((UniversalRenderPipelineAsset)sourcePipeline);AssetDatabase.CreateAsset(pipeline,path);}var so=new SerializedObject(pipeline);var data=so.FindProperty("m_RendererDataList");var depth=AssetDatabase.LoadAssetAtPath<ScriptableRendererData>("Assets/NOT_Lonely/Weatherade SRS/Resources/SRS_DepthRenderer.asset");snow.depthRenderer=-1;for(int i=0;i<data.arraySize;i++)if(data.GetArrayElementAtIndex(i).objectReferenceValue==depth)snow.depthRenderer=i;if(snow.depthRenderer<0){snow.depthRenderer=data.arraySize;data.arraySize++;data.GetArrayElementAtIndex(snow.depthRenderer).objectReferenceValue=depth;}so.ApplyModifiedPropertiesWithoutUndo();if(cheese!=null)cheese.match.look.pipeline=pipeline;else rendering.pipeline=pipeline;
 snow.wheels=UnityEngine.Object.FindObjectsByType<NWH.WheelController3D.WheelController>(FindObjectsInactive.Include);if(snow.wheels.Length>128)throw new Exception("Explicit snow contact capacity exceeded");snow.broadBodies=UnityEngine.Object.FindObjectsByType<LWS.TruckMattressRacing.MattressRacer>(FindObjectsInactive.Include).Select(r=>r.truck.body).ToArray();string textures="Assets/NOT_Lonely/Weatherade SRS/Textures/";snow.snowTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(textures+"Snow_01_n_h_sm.tif");snow.detailTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(textures+"SnowDetail.tif");snow.sparkleTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(textures+"SparkleMask.tif");snow.retainedShaders=new[]{"NOT_Lonely/Weatherade/Snow Coverage","Hidden/NOT_Lonely/NL_GaussianBlur","Hidden/NOT_Lonely/Weatherade/NL_TexturePacking","Hidden/NOT_Lonely/Weatherade/NL_TraceMaskGen","Hidden/NOT_Lonely/Weatherade/DepthRenderer","NOT_Lonely/Weatherade/Extra/NL_DepthOccluder"}.Select(Shader.Find).ToArray();snow.spray=new ParticleSystem[8];var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/NWH/Vehicle Physics 2/Effects/Prefabs/ExhaustSmoke.prefab");for(int i=0;i<8;i++){var g=UnityEngine.Object.Instantiate(source);g.name="Pooled actual-contact snow spray "+i;g.transform.SetParent(owner.transform);var p=g.GetComponent<ParticleSystem>();p.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);var main=p.main;main.loop=false;main.playOnAwake=false;main.startColor=new Color(.88f,.94f,1,.65f);main.startSize=new ParticleSystem.MinMaxCurve(.08f,.25f);main.startLifetime=.5f;main.gravityModifier=.4f;main.simulationSpace=ParticleSystemSimulationSpace.World;main.maxParticles=96;var emission=p.emission;emission.rateOverTime=0;emission.SetBursts(Array.Empty<ParticleSystem.Burst>());p.GetComponent<ParticleSystemRenderer>().sharedMaterial=SportsChaosAuthoring.Flare("TruckSports");snow.spray[i]=p;}
 foreach(var r in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include)){if(r.GetComponentInParent<Rigidbody>()!=null||r.GetComponent<Collider>()==null)continue;string n=r.name.ToLowerInvariant();if(!n.Contains("ramp")&&!n.Contains("ground")&&!n.Contains("floor")&&!n.Contains("bridge")&&!n.Contains("bank")&&!n.Contains("rounded arena surface"))continue;if(n.Contains("indoor")||n.Contains("water"))continue;var marker=r.GetComponent<TruckTaxiSurface>();if(marker==null)marker=r.gameObject.AddComponent<TruckTaxiSurface>();marker.isRoad=false;}EditorUtility.SetDirty(snow);}
 [MenuItem("Truck Sports/Environment/Bind Existing Urban Roads to Weatherade Snow")]
 public static void InstallUrbanRoadSnow()
 {
  // Explicit additive migration, using the established Hide/Frogger shared-
  // material integration. Never called by builds, Play Mode or scene loading.
  foreach(string name in new[]{"TruckFareFight","TruckSnake"})
  {
   var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/"+name+"/Scenes/"+name+".unity");
   var snow=UnityEngine.Object.FindObjectsByType<SportsSnowSurface>(FindObjectsInactive.Include).Single(item=>item.gameObject.scene==scene);
   string folder="Assets/"+name+"/Materials/Urban Road Snow";Directory.CreateDirectory(folder);AssetDatabase.Refresh();
   var shared=new System.Collections.Generic.Dictionary<Material,Material>();int count=0;
   foreach(var road in UnityEngine.Object.FindObjectsByType<LwsRoadSurface>(FindObjectsInactive.Include).Where(item=>item.gameObject.scene==scene))
   foreach(var renderer in road.GetComponentsInChildren<MeshRenderer>(true))
   {
    var materials=renderer.sharedMaterials;bool changed=false;
    for(int i=0;i<materials.Length;i++)
    {
     var source=materials[i];if(source==null||!TruckTaxiSnowSurface.IsSnowEligibleMaterial(source))continue;
     if(LwsWeatheradeMaterialFactory.IsWeatheradeMaterialForMode(source,LwsWeatheradeSurfaceMaterialMode.Snow))continue;
     if(!shared.TryGetValue(source,out var material))
     {
      string sourcePath=AssetDatabase.GetAssetPath(source),guid=AssetDatabase.AssetPathToGUID(sourcePath);
      if(string.IsNullOrEmpty(guid))throw new InvalidOperationException("Preserve authored road materials; an asset-backed source is required.");
      string path=folder+"/"+guid+".mat";material=AssetDatabase.LoadAssetAtPath<Material>(path);
      if(material==null)
      {
       if(!LwsWeatheradeMaterialFactory.TryCreateWeatheradeRoadSurfaceMaterial(source.name+" ("+name+" accumulating snow)",source,Color.gray,LwsWeatheradeSurfaceMaterialMode.Snow,out material))throw new InvalidOperationException("Installed Weatherade snow shader required.");
       material.SetFloat("_CoverageAmount",0);material.SetFloat("_Smoothness",.18f);
       string map=source.HasProperty("_BaseMap")?"_BaseMap":source.HasProperty("_MainTex")?"_MainTex":null;
       if(map!=null){material.SetTextureScale("_BaseMap",source.GetTextureScale(map));material.SetTextureOffset("_BaseMap",source.GetTextureOffset(map));}
       AssetDatabase.CreateAsset(material,path);
      }
      if(!LwsWeatheradeMaterialFactory.IsWeatheradeMaterialForMode(material,LwsWeatheradeSurfaceMaterialMode.Snow))throw new InvalidOperationException("Inspect customized urban snow material before migration: "+path);
      shared.Add(source,material);
     }
     materials[i]=material;changed=true;
    }
    if(changed){renderer.sharedMaterials=materials;EditorUtility.SetDirty(renderer);count++;}
   }
   snow.traceExposedRoads=true;EditorUtility.SetDirty(snow);
   UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
   Debug.Log("URBAN_EXISTING_WEATHERADE_ROADS "+name+" renderers="+count+" sharedMaterials="+shared.Count);
  }
  AssetDatabase.SaveAssets();
 }
 static void ConfigureDepthMask(SportsSnowSurface snow){if(snow.adapter==null)return;var settings=new SerializedObject(snow.adapter);settings.FindProperty("depthLayerMask").intValue=~((1<<5)|(1<<24)|(1<<25)|(1<<26)|(1<<27)|(1<<28)|(1<<29)|(1<<30)|(1<<31));settings.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(snow.adapter);}
}}

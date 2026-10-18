using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using DigitalRuby.WeatherMaker;

namespace LWS.TruckSports.Editor {
// Opt-in installation. Never mutate a host/shared renderer or change other sports.
public static class SportsWeatherRenderingAuthoring {
 public static void Install(SportsEventSession match,string folder){
  var weather=match.GetComponent<SportsChaosWeather>();
  var binding=match.GetComponent<LWS.TruckBaseJumping.BaseJumpRendering>();
  if(weather==null||binding==null)throw new System.InvalidOperationException("Scene-owned Weather Maker and scoped pipeline are required.");
  string directory="Assets/"+folder+"/Rendering";Directory.CreateDirectory(directory);AssetDatabase.Refresh();
  string pipelinePath=directory+"/Weather pipeline.asset",rendererPath=directory+"/Weather renderer.asset";
  var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(pipelinePath);
  if(pipeline==null){AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(binding.pipeline),pipelinePath);pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(pipelinePath);}
  var settings=new SerializedObject(pipeline);var list=settings.FindProperty("m_RendererDataList");int index=settings.FindProperty("m_DefaultRendererIndex").intValue;
  var data=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
  if(data==null){AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(list.GetArrayElementAtIndex(index).objectReferenceValue),rendererPath);data=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);}
#if UNITY_URP
  if(!data.rendererFeatures.OfType<WeatherMakerURPRenderFeatureScript>().Any()){
   var feature=ScriptableObject.CreateInstance<WeatherMakerURPRenderFeatureScript>();feature.name="Weather Maker vendor Render Graph weather";AssetDatabase.AddObjectToAsset(feature,data);data.rendererFeatures.Add(feature);feature.SetActive(true);EditorUtility.SetDirty(feature);
  }
#else
  throw new System.InvalidOperationException("Enable the installed Weather Maker URP support before installing its renderer feature.");
#endif
  // Vendor fog is authoritative; avoid a second deferred fog path. Match vendor demo's intermediate texture requirement.
  var rendererSettings=new SerializedObject(data);rendererSettings.FindProperty("m_RenderingMode").intValue=0;rendererSettings.FindProperty("m_IntermediateTextureMode").intValue=1;rendererSettings.ApplyModifiedPropertiesWithoutUndo();
  data.SetDirty();EditorUtility.SetDirty(data);list.GetArrayElementAtIndex(index).objectReferenceValue=data;settings.ApplyModifiedPropertiesWithoutUndo();binding.pipeline=pipeline;EditorUtility.SetDirty(binding);
  // Weather Maker draws this mesh through its camera command buffer. Enabling the
  // ordinary renderer draws its ZTest Always material over foreground geometry.
  var sky=weather.vendor.GetComponentInChildren<WeatherMakerSkySphereScript>(true);if(sky!=null){sky.gameObject.SetActive(true);sky.enabled=true;var renderer=sky.GetComponent<MeshRenderer>();if(renderer!=null){renderer.enabled=false;EditorUtility.SetDirty(renderer);}EditorUtility.SetDirty(sky);}
  foreach(var camera in match.cameras){camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.clear;camera.farClipPlane=Mathf.Max(10000,camera.farClipPlane);EditorUtility.SetDirty(camera);}
  AssetDatabase.SaveAssets();
 }
}
}

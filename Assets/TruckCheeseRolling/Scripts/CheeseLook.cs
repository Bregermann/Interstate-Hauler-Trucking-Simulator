﻿using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using LWS.TruckTaxi;
namespace LWS.TruckCheeseRolling {
// Reuses Taxi's exact mushroom wave pass. Pipeline/sky/profile copies belong only to this scene.
[DefaultExecutionOrder(-200)]public sealed class CheeseLook:MonoBehaviour {
 public CheeseMatch match;public UniversalRenderPipelineAsset pipeline;public Material skyTemplate;public Volume volume;public float intensity=.65f;
 RenderPipelineAsset oldDefault,oldQuality;Material oldSky,sky;Light oldSun;VolumeProfile profile;ColorAdjustments grade;LensDistortion lens;bool bound;
 public float Intensity=>intensity;
 void Awake(){oldDefault=GraphicsSettings.defaultRenderPipeline;oldQuality=QualitySettings.renderPipeline;oldSky=RenderSettings.skybox;oldSun=RenderSettings.sun;GraphicsSettings.defaultRenderPipeline=pipeline;QualitySettings.renderPipeline=pipeline;sky=Instantiate(skyTemplate);RenderSettings.skybox=sky;profile=ScriptableObject.CreateInstance<VolumeProfile>();foreach(var component in volume.sharedProfile.components)profile.components.Add(Instantiate(component));volume.sharedProfile=profile;profile.TryGet(out grade);profile.TryGet(out lens);intensity=PlayerPrefs.GetFloat("TruckCheeseRolling.Look",.65f);bound=true;foreach(var c in match.cameras){var data=c.view.GetUniversalAdditionalCameraData();data.SetRenderer(0);data.renderPostProcessing=true;data.volumeLayerMask|=1<<31;}}
 public void Cycle(){intensity=intensity>.5f?.25f:intensity>0?0:.65f;PlayerPrefs.SetFloat("TruckCheeseRolling.Look",intensity);PlayerPrefs.Save();}
 void LateUpdate(){RenderSettings.sun=match.weather.sun;volume.weight=intensity;grade.hueShift.value=Mathf.Sin(Time.time*.48f)*24;lens.intensity.value=-.1f+Mathf.Sin(Time.time*1.1f)*.02f;sky.SetColor("_SkyTint",Color.Lerp(new Color(.3f,.48f,.72f),Color.HSVToRGB(Mathf.Repeat(.7f+Mathf.Sin(Time.time*.09f)*.12f,1),.6f,.75f),intensity));foreach(var c in match.cameras)TruckTaxiMushroomWaveFeature.SetCameraStrength(c.view,c.view.enabled?intensity:0);}
 void OnDisable(){if(!bound)return;bound=false;foreach(var c in match.cameras)if(c!=null)TruckTaxiMushroomWaveFeature.SetCameraStrength(c.view,0);if(GraphicsSettings.defaultRenderPipeline==pipeline)GraphicsSettings.defaultRenderPipeline=oldDefault;if(QualitySettings.renderPipeline==pipeline)QualitySettings.renderPipeline=oldQuality;if(RenderSettings.skybox==sky)RenderSettings.skybox=oldSky;if(RenderSettings.sun==match.weather.sun)RenderSettings.sun=oldSun;}
 void OnDestroy(){if(sky!=null)Destroy(sky);if(profile!=null){foreach(var c in profile.components)if(c!=null)Destroy(c);Destroy(profile);}}
}
}

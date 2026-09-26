using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;

namespace LWS.TruckTaxi.Editor
{
    public static class TruckTaxiPresentationSetup
    {
        public const string ScenePath="Assets/LWS/TruckTaxi/Scenes/TruckTaxi_DemoCity.unity";
        public const string AudioRoot="Assets/LWS/TruckTaxi/Audio";
        public const string MixerPath=AudioRoot+"/TruckTaxi.mixer";
        [MenuItem("Truck Taxi/Configure Presentation Assets")]
        public static void Configure()
        {
            if(EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode before configuring presentation assets.");
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if(scene.path!=ScenePath)
            {
                if(scene.isDirty) throw new InvalidOperationException("Save the current scene before configuring Truck Taxi.");
                scene=EditorSceneManager.OpenScene(ScenePath);
            }
            var host=UnityEngine.Object.FindFirstObjectByType<TruckTaxiBootstrap>();
            ConfigureHost(host);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            Debug.Log("Truck Taxi presentation assets configured. Audio ready: "+(host.configuration.audio!=null));
        }
        public static void ConfigureHost(TruckTaxiBootstrap host)
        {
            if(host==null) throw new InvalidOperationException("Truck Taxi bootstrap is missing.");
            host.hud.offerCountdownStroke=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Heat - Complete Modern UI/Textures/Borders/Radial/128px/Radial Outline 128px - 6x.png");
            if(host.hud.offerCountdownStroke==null) throw new InvalidOperationException("Heat radial stroke sprite missing.");
            if(host.configuration.offerSound==null)
                host.configuration.offerSound=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Heat - Complete Modern UI/Audio/Notification.wav");
            ConfigureAudio(host.configuration);
            EditorUtility.SetDirty(host.hud); EditorUtility.SetDirty(host.configuration);
        }
        private static void ConfigureAudio(TruckTaxiConfiguration configuration)
        {
            var mixer=AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
            if(mixer==null)
            { Debug.LogWarning("Audio group authoring pending: "+MixerPath+". No mixer structure was fabricated."); return; }
            var groups=mixer.FindMatchingGroups("");
            var buses=new System.Collections.Generic.List<TruckTaxiAudioBus>();
            foreach(TruckTaxiAudioCategory category in Enum.GetValues(typeof(TruckTaxiAudioCategory)))
            {
                var group=groups.FirstOrDefault(g=>g.name==category.ToString());
                string parameter=category+"Volume";
                if(group==null || !mixer.GetFloat(parameter,out float db))
                    throw new InvalidOperationException("Missing AudioMixer group/exposed Volume: "+category+" / "+parameter);
                buses.Add(new TruckTaxiAudioBus {category=category,label=category==TruckTaxiAudioCategory.Voices ? "PASSENGER VOICES" : category.ToString().ToUpperInvariant(),group=group,volumeParameter=parameter,defaultDecibels=db});
            }
            string configPath=AudioRoot+"/TruckTaxiAudioRouting.asset";
            var settings=AssetDatabase.LoadAssetAtPath<TruckTaxiAudioConfiguration>(configPath);
            bool isNew=settings==null;
            if(isNew) { settings=ScriptableObject.CreateInstance<TruckTaxiAudioConfiguration>(); AssetDatabase.CreateAsset(settings,configPath); }
            if(isNew || settings.buses==null || settings.buses.Length==0) settings.buses=buses.ToArray();
            string vehiclePath=AudioRoot+"/TruckTaxiVehicle.mixer";
            if(AssetDatabase.LoadAssetAtPath<AudioMixer>(vehiclePath)==null)
                AssetDatabase.CopyAsset("Assets/NWH/Vehicle Physics 2/Resources/NWH Vehicle Physics 2/Defaults/Sound/VehicleAudioMixer.mixer",vehiclePath);
            settings.vehicleMixer=AssetDatabase.LoadAssetAtPath<AudioMixer>(vehiclePath);
            configuration.audio=settings; EditorUtility.SetDirty(settings);
        }
    }
}

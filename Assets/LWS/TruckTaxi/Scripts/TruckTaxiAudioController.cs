using System;
using System.Collections.Generic;
using NWH.VehiclePhysics2;
using UnityEngine;
using UnityEngine.Audio;

namespace LWS.TruckTaxi
{
    public sealed class TruckTaxiAudioController : MonoBehaviour
    {
        public static TruckTaxiAudioController Instance { get; private set; }
        public const string PreferencePrefix="LWS.TruckTaxi.Audio.v1.";
        private TruckTaxiAudioConfiguration configuration;
        private readonly Dictionary<TruckTaxiAudioCategory,float> volumes=new Dictionary<TruckTaxiAudioCategory,float>();
        private readonly Dictionary<AudioSource,AudioMixerGroup> previousRoutes=new Dictionary<AudioSource,AudioMixerGroup>();
        private readonly HashSet<TruckTaxiAudioCategory> warned=new HashSet<TruckTaxiAudioCategory>();
        private readonly List<Transform> worldRoots=new List<Transform>();
        private readonly List<AudioSource> worldSources=new List<AudioSource>();
        private float nextWorldScan;
        private VehicleController routedVehicle;
        private AudioMixer previousVehicleMixer;
        private AudioMixerGroup previousVehicleOutput;
        private AudioMixer routedVehicleMixer;
        private AudioMixerGroup[] previousVehicleGroups;
        private readonly Dictionary<string,float> previousVehicleParameters=new Dictionary<string,float>();
        public event Action Changed;
        public IReadOnlyList<TruckTaxiAudioBus> Buses => configuration!=null && configuration.buses!=null ? configuration.buses : Array.Empty<TruckTaxiAudioBus>();
        public bool Ready { get; private set; }
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        public static float ToDecibels(float volume) => !Finite(volume) || volume<=.0001f ? -80 : Mathf.Clamp(20*Mathf.Log10(Mathf.Clamp01(volume)),-80,0);
        public static float FromDecibels(float db) => !Finite(db) || db<=-80 ? 0 : Mathf.Clamp01(Mathf.Pow(10,db/20));
        public void Initialize(TruckTaxiAudioConfiguration value)
        {
            if(Instance!=null && Instance!=this) throw new InvalidOperationException("Truck Taxi already has an audio settings authority.");
            RestoreRouting(); volumes.Clear(); warned.Clear();
            Instance=this; configuration=value; Ready=value!=null && value.buses!=null && value.buses.Length>0;
            if(!Ready) { Debug.LogWarning("Truck Taxi audio routing awaits the project AudioMixer configuration.",this); return; }
            var categories=new HashSet<TruckTaxiAudioCategory>();
            foreach(var bus in value.buses)
            {
                if(bus?.group==null || string.IsNullOrWhiteSpace(bus.volumeParameter) || !Finite(bus.defaultDecibels) ||
                    !categories.Add(bus.category) || !bus.group.audioMixer.GetFloat(bus.volumeParameter,out _))
                { Ready=false; Debug.LogError("Truck Taxi audio bus is missing its group/exposed volume: "+bus?.category,this); continue; }
            }
            // Validate the complete configuration before touching a mixer, route or stored preference.
            if(!Ready) return;
            foreach(var bus in value.buses)
            {
                float saved=PlayerPrefs.GetFloat(PreferencePrefix+bus.category,FromDecibels(bus.defaultDecibels));
                volumes[bus.category]=Finite(saved) ? Mathf.Clamp01(saved) : FromDecibels(bus.defaultDecibels);
                Apply(bus);
            }
        }
        public float GetVolume(TruckTaxiAudioCategory category) => volumes.TryGetValue(category,out var value) ? value : 1;
        public void SetVolume(TruckTaxiAudioCategory category,float value)
        {
            if(!Ready || !Finite(value)) return;
            var bus=configuration?.Find(category); if(bus?.group==null) return;
            value=Mathf.Clamp01(value);
            if(!bus.group.audioMixer.SetFloat(bus.volumeParameter,ToDecibels(value)))
            { Debug.LogError("Missing mixer volume parameter: "+bus.volumeParameter,this); return; }
            volumes[category]=value;
            PlayerPrefs.SetFloat(PreferencePrefix+category,volumes[category]);
            Changed?.Invoke();
        }
        private void Apply(TruckTaxiAudioBus bus)
        {
            if(!bus.group.audioMixer.SetFloat(bus.volumeParameter,ToDecibels(GetVolume(bus.category))))
                Debug.LogError("Missing mixer volume parameter: "+bus.volumeParameter,this);
        }
        public void ResetDefaults()
        {
            if(!Ready) return;
            foreach(var bus in Buses) if(bus?.group!=null) SetVolume(bus.category,FromDecibels(bus.defaultDecibels));
            PlayerPrefs.Save();
        }
        public bool Route(AudioSource source,TruckTaxiAudioCategory category)
        {
            if(source==null) return false;
            var bus=configuration?.Find(category);
            if(!Ready || bus?.group==null)
            {
                if(warned.Add(category)) Debug.LogWarning("Truck Taxi audio category is not routed yet: "+category,this);
                return false;
            }
            if(!previousRoutes.ContainsKey(source)) previousRoutes.Add(source,source.outputAudioMixerGroup);
            source.outputAudioMixerGroup=bus.group;
            return true;
        }
        public void RouteWorldTree(Transform root)
        {
            if(root==null) return;
            if(!worldRoots.Contains(root)) worldRoots.Add(root);
            root.GetComponentsInChildren(true,worldSources);
            foreach(var source in worldSources) Route(source,TruckTaxiAudioCategory.World);
        }
        private void Update()
        {
            if(Time.unscaledTime<nextWorldScan) return;
            nextWorldScan=Time.unscaledTime+2;
            for(int i=worldRoots.Count-1;i>=0;i--)
            {
                if(worldRoots[i]==null) { worldRoots.RemoveAt(i); continue; }
                RouteWorldTree(worldRoots[i]);
            }
        }
        public bool RouteVehicle(VehicleController vehicle)
        {
            var output=configuration?.Find(TruckTaxiAudioCategory.Vehicle)?.group;
            var mixer=configuration?.vehicleMixer;
            if(!Ready || vehicle==null || output==null || mixer==null) return false;
            var sound=vehicle.soundManager;
            if(routedVehicle==vehicle) return true;
            var previous=sound.mixer;
            if(previous==null || mixer==previous)
            { Debug.LogError("Taxi vehicle audio requires a project-owned mixer copy and an initialized NWH sound mixer.",this); return false; }
            string[] required={"Master","Engine","Transmission","SurfaceNoise","Other"};
            foreach(string name in required)
                if(Group(mixer,name)==null) { Debug.LogError("NWH audio group missing from Taxi mixer: "+name,this); return false; }
            var sources=vehicle.GetComponentsInChildren<AudioSource>(true);
            foreach(var source in sources)
                if(source.outputAudioMixerGroup!=null && source.outputAudioMixerGroup.audioMixer==previous && Group(mixer,source.outputAudioMixerGroup.name)==null)
                { Debug.LogError("NWH audio group missing from Taxi mixer: "+source.outputAudioMixerGroup.name,this); return false; }
            // Restore an earlier canonical vehicle before transferring the route to its replacement.
            if(routedVehicle!=null || routedVehicleMixer!=null) RestoreVehicleRouting();
            routedVehicle=vehicle; previousVehicleMixer=previous; routedVehicleMixer=mixer; previousVehicleOutput=mixer.outputAudioMixerGroup;
            previousVehicleGroups=new[]{sound.masterGroup,sound.engineMixerGroup,sound.transmissionMixerGroup,sound.surfaceNoiseMixerGroup,sound.otherMixerGroup};
            // Preserve the current NWH interior/exterior filter state when moving already-playing sources to its copy.
            foreach(string parameter in new[]{"attenuation","lowPassFrequency","lowPassQ","engineDistortion"})
                if(mixer.GetFloat(parameter,out float copyValue) && previous.GetFloat(parameter,out float currentValue))
                { previousVehicleParameters[parameter]=copyValue; mixer.SetFloat(parameter,currentValue); }
            mixer.outputAudioMixerGroup=output;
            foreach(var source in sources)
            {
                if(source.outputAudioMixerGroup==null || source.outputAudioMixerGroup.audioMixer!=previous) continue;
                var group=Group(mixer,source.outputAudioMixerGroup.name);
                if(group==null) { Debug.LogError("NWH audio group missing from Taxi mixer: "+source.outputAudioMixerGroup.name,this); continue; }
                if(!previousRoutes.ContainsKey(source)) previousRoutes.Add(source,source.outputAudioMixerGroup);
                source.outputAudioMixerGroup=group;
            }
            sound.mixer=mixer;
            sound.masterGroup=Group(mixer,"Master"); sound.engineMixerGroup=Group(mixer,"Engine");
            sound.transmissionMixerGroup=Group(mixer,"Transmission"); sound.surfaceNoiseMixerGroup=Group(mixer,"SurfaceNoise");
            sound.otherMixerGroup=Group(mixer,"Other");
            return true;
        }
        private static AudioMixerGroup Group(AudioMixer mixer,string name)
        {
            foreach(var group in mixer.FindMatchingGroups("")) if(group.name==name) return group;
            return null;
        }
        private void OnDestroy()
        {
            RestoreRouting();
            if(Instance==this) { Instance=null; PlayerPrefs.Save(); }
        }
        private void RestoreRouting()
        {
            foreach(var pair in previousRoutes) if(pair.Key!=null) pair.Key.outputAudioMixerGroup=pair.Value;
            previousRoutes.Clear();
            RestoreVehicleRouting();
        }
        private void RestoreVehicleRouting()
        {
            if(routedVehicle!=null && previousVehicleMixer!=null)
            {
                foreach(var source in routedVehicle.GetComponentsInChildren<AudioSource>(true))
                    if(previousRoutes.TryGetValue(source,out var original))
                    { source.outputAudioMixerGroup=original; previousRoutes.Remove(source); }
                var sound=routedVehicle.soundManager; sound.mixer=previousVehicleMixer;
                sound.masterGroup=previousVehicleGroups[0]; sound.engineMixerGroup=previousVehicleGroups[1];
                sound.transmissionMixerGroup=previousVehicleGroups[2]; sound.surfaceNoiseMixerGroup=previousVehicleGroups[3];
                sound.otherMixerGroup=previousVehicleGroups[4];
            }
            // A pending/unused configuration never acquired this output and must not reset it.
            if(routedVehicleMixer!=null)
            {
                routedVehicleMixer.outputAudioMixerGroup=previousVehicleOutput;
                foreach(var parameter in previousVehicleParameters) routedVehicleMixer.SetFloat(parameter.Key,parameter.Value);
            }
            previousVehicleParameters.Clear(); previousVehicleGroups=null;
            routedVehicle=null; previousVehicleMixer=null; routedVehicleMixer=null; previousVehicleOutput=null;
        }
    }
}

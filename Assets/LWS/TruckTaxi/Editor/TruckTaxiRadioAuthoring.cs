using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

namespace LWS.TruckTaxi.Editor
{
    // Creates original instrumental demo beds, never downloads stations or generates voices.
    public static class TruckTaxiRadioAuthoring
    {
        public const string CatalogPath="Assets/LWS/TruckTaxi/Resources/TruckTaxi/TruckTaxiRadioCatalog.asset";
        [MenuItem("Truck Taxi/Radio/Author Original Prototype Stations")]
        public static void Author()
        {
            if(EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
            var catalog=AssetDatabase.LoadAssetAtPath<TruckTaxiRadioCatalog>(CatalogPath);
            if(catalog==null) { catalog=ScriptableObject.CreateInstance<TruckTaxiRadioCatalog>(); AssetDatabase.CreateAsset(catalog,CatalogPath); }
            if(catalog.stations.Length>0) { BindMixerIfPresent(); return; }
            const string folder="Assets/LWS/TruckTaxi/Audio/Radio";
            if(!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/LWS/TruckTaxi/Audio","Radio");
            string[] names={"THE HAUL","NIGHT DRIVE","PUBLIC HAULING","COUNTRY MILE","CHAOS FM","ROAD REPORT"};
            string[] freq={"93.7","101.1","88.5","104.9","97.3","AM 740"};
            string[] formats={"Driving","Electronic","Talk / news","Road","Heavy / weird","Traffic / weather"};
            catalog.stations=new TruckTaxiRadioStation[names.Length];
            for(int i=0;i<names.Length;i++)
            {
                string path=folder+"/OriginalRadioBed"+i+".wav";
                if(!File.Exists(path)) WriteOriginalBed(path,i);
                AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
                var importer=(AudioImporter)AssetImporter.GetAtPath(path);
                var settings=importer.defaultSampleSettings; settings.loadType=AudioClipLoadType.Streaming;
                settings.compressionFormat=AudioCompressionFormat.Vorbis; settings.quality=.7f; importer.defaultSampleSettings=settings;
                importer.forceToMono=true; importer.SaveAndReimport();
                catalog.stations[i]=new TruckTaxiRadioStation {stableId="taxi.radio."+i,displayName=names[i],frequency=freq[i],format=formats[i],
                    playlist=new[]{new TruckTaxiRadioTrack {title="Original prototype instrumental "+(i+1),clip=AssetDatabase.LoadAssetAtPath<AudioClip>(path)}}};
            }
            catalog.alertTone=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Heat - Complete Modern UI/Audio/Notification.wav");
            catalog.attribution="Original LWS prototype synthesized compositions; no sampled or streamed music. Heat notification tone under installed asset license. News uses live-state text; authored spoken programs may be assigned later.";
            EditorUtility.SetDirty(catalog); BindMixerIfPresent(); AssetDatabase.SaveAssets();
        }
        [MenuItem("Truck Taxi/Radio/Bind Authored Radio Mixer Group")]
        public static void BindMixerIfPresent()
        {
            var mixer=AssetDatabase.LoadAssetAtPath<AudioMixer>(TruckTaxiPresentationSetup.MixerPath);
            var configuration=AssetDatabase.LoadAssetAtPath<TruckTaxiAudioConfiguration>("Assets/LWS/TruckTaxi/Audio/TruckTaxiAudioRouting.asset");
            var group=mixer!=null ? mixer.FindMatchingGroups("").FirstOrDefault(g=>g.name=="Radio") : null;
            if(group==null || configuration==null || !mixer.GetFloat("RadioVolume",out float db))
            { Debug.Log("Radio mixer routing pending user authoring: Radio child of Master, exposed RadioVolume. Existing mixer untouched."); return; }
            if(configuration.Find(TruckTaxiAudioCategory.Radio)==null)
                configuration.buses=configuration.buses.Concat(new[]{new TruckTaxiAudioBus {category=TruckTaxiAudioCategory.Radio,label="RADIO",group=group,volumeParameter="RadioVolume",defaultDecibels=db}}).ToArray();
            EditorUtility.SetDirty(configuration); AssetDatabase.SaveAssets();
        }
        private static void WriteOriginalBed(string path,int variation)
        {
            const int rate=22050,seconds=32;
            int[] melody={0,7,12,10,7,3,5,7,0,3,10,12,7,5,3,-2};
            using(var stream=File.Create(path)) using(var w=new BinaryWriter(stream))
            {
                int bytes=rate*seconds*2;
                w.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); w.Write(36+bytes);
                w.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); w.Write(16); w.Write((short)1); w.Write((short)1);
                w.Write(rate); w.Write(rate*2); w.Write((short)2); w.Write((short)16);
                w.Write(System.Text.Encoding.ASCII.GetBytes("data")); w.Write(bytes);
                for(int sample=0;sample<rate*seconds;sample++)
                {
                    double t=sample/(double)rate, beat=t*2, phase=beat-Math.Floor(beat);
                    int note=melody[((int)beat+variation*2)%melody.Length];
                    double frequency=110*Math.Pow(2,(note+variation*2)/12d);
                    double envelope=Math.Min(1,phase*30)*Math.Exp(-phase*3);
                    double lead=Math.Sin(2*Math.PI*frequency*t)*.13*envelope;
                    double bass=Math.Sin(2*Math.PI*55*Math.Pow(2,variation/12d)*t)*.08*Math.Exp(-phase*5);
                    double harmonic=Math.Sin(2*Math.PI*frequency*2*t)*.035*envelope;
                    double fade=Math.Min(1,Math.Min(t,seconds-t)*2);
                    w.Write((short)(Math.Clamp((lead+bass+harmonic)*fade,-.8,.8)*short.MaxValue));
                }
            }
        }
    }
}

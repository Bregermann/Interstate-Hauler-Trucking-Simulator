using System;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace LWS.TruckBallet.Editor
{
    public static class BalletAudioAuthoring
    {
        public const string PathName="Assets/TruckBallet/Audio/Original Grand Diesel Waltz.wav";
        // Original twelve-bar melody/harmony. No copied classical composition,
        // proprietary sample, new audio engine or gameplay clock is involved.
        public static AudioClip Create()
        {
            if(File.Exists(PathName)){var prior=AssetDatabase.LoadAssetAtPath<AudioClip>(PathName);if(prior==null)throw new InvalidOperationException("Existing custom/partial Ballet music retained.");return prior;}
            const int rate=44100;const float beat=60f/84;int frames=(int)(36*beat*rate);var samples=new short[frames*2];
            int[] melody={72,76,79,77,76,74,71,74,79,76,74,72,69,72,77,76,72,69,67,71,74,72,71,67,65,69,72,74,72,69,67,71,74,76,74,72};int[] roots={48,48,43,43,45,45,41,41,43,43,48,48};
            for(int i=0;i<frames;i++)
            {
                double t=i/(double)rate;int note=Math.Min(35,(int)(t/beat)),bar=note/3;double age=t-note*beat,envelope=Math.Min(1,age/.06)*Math.Exp(-age*1.6);double f=440*Math.Pow(2,(melody[note]-69)/12.0);
                double flute=(Math.Sin(t*f*2*Math.PI+.008*Math.Sin(t*35))+Math.Sin(t*f*4*Math.PI)*.12)*envelope*.17;
                double chord=0;foreach(int interval in new[]{0,4,7}){double hz=440*Math.Pow(2,(roots[bar]+12+interval-69)/12.0);for(int harmonic=1;harmonic<=4;harmonic++)chord+=Math.Sin(t*hz*harmonic*2*Math.PI+Math.Sin(t*28)*.012)/Math.Pow(harmonic,1.6);}
                chord*=.035*(.85+.15*Math.Sin(t/beat*Math.PI*2));double bass=note%3==0?Math.Sin(t*440*Math.Pow(2,(roots[bar]-69)/12.0)*2*Math.PI)*Math.Exp(-age*4)*.12:0;
                double fade=Math.Min(1,t/.2)*Math.Min(1,(frames/(double)rate-t)/.3);double left=(flute+chord+bass)*fade,right=(flute*.92+chord*1.08+bass)*fade;
                samples[i*2]=(short)(Math.Clamp(left,-.9,.9)*32767);samples[i*2+1]=(short)(Math.Clamp(right,-.9,.9)*32767);
            }
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(PathName));using(var file=new BinaryWriter(File.Create(PathName)))
            {int bytes=samples.Length*2;file.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));file.Write(36+bytes);file.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));file.Write(16);file.Write((short)1);file.Write((short)2);file.Write(rate);file.Write(rate*4);file.Write((short)4);file.Write((short)16);file.Write(System.Text.Encoding.ASCII.GetBytes("data"));file.Write(bytes);foreach(short value in samples)file.Write(value);}
            AssetDatabase.ImportAsset(PathName,ImportAssetOptions.ForceSynchronousImport);var importer=(AudioImporter)AssetImporter.GetAtPath(PathName);var setting=importer.defaultSampleSettings;setting.loadType=AudioClipLoadType.Streaming;setting.compressionFormat=AudioCompressionFormat.Vorbis;setting.quality=.85f;importer.defaultSampleSettings=setting;importer.SaveAndReimport();
            string license="Assets/TruckBallet/Audio/OriginalAudioLicense.txt";if(!File.Exists(license))File.WriteAllText(license,"Original Grand Diesel Waltz: original project-authored twelve-bar melody, harmony and additive synthesis in BalletAudioAuthoring.cs. No sampled vendor recording or copied classical composition. Listening/quality acceptance is separate from source generation.\n");return AssetDatabase.LoadAssetAtPath<AudioClip>(PathName);
        }
    }
}

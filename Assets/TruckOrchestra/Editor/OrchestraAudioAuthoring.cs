using System;
using System.Collections.Generic;
using System.IO;
using LWS.TruckDanceOff;
using UnityEditor;
using UnityEngine;

namespace LWS.TruckOrchestra.Editor
{
    // Original authored compositions and sampled instrument timbres. Unity's
    // existing AudioSource/DSP engine remains playback and synchronization owner.
    public static class OrchestraAudioAuthoring
    {
        const string Folder = "Assets/TruckOrchestra/Audio/";
        const int Rate = 22050;
        public static DanceSongLibrary CreateLibrary(out OrchestraScore score)
        {
            Directory.CreateDirectory(Folder);
            if (File.Exists(Folder + "Original symphony library.asset") || File.Exists(Folder + "Original instrumental parts.asset"))
                throw new InvalidOperationException("Existing authored Orchestra scores are preserved; use an explicit reviewed update.");
            var library = ScriptableObject.CreateInstance<DanceSongLibrary>(); library.songs = new DanceSongLibrary.Song[7];
            score = ScriptableObject.CreateInstance<OrchestraScore>();
            string[] names = { "Diesel Overture", "Moonlit Motor Waltz", "Brass on the Bypass", "The Tiny Truck Sonata", "Midnight Cargo Concerto", "March of the Mufflers", "Six-Ton Finale" };
            string[] parts = { "GRAND PIANO", "DRUMS & CYMBALS", "BRASS HORNS", "XYLOPHONE & BELLS" };
            for (int song = 0; song < 7; song++)
            {
                float bpm = 96 + song * 4; double beat = 60.0 / bpm, duration = beat * 68 + 1;
                var s = library.songs[song] = new DanceSongLibrary.Song { title = names[song], genre = "ORIGINAL ARCADE SYMPHONY", bpm = bpm, duration = duration, freestyleStart = duration + 1, freestyleEnd = duration + 2 };
                var arrangement = score.arrangements[song] = new OrchestraScore.Arrangement();
                for (int part = 0; part < 4; part++)
                {
                    var p = arrangement.parts[part] = new OrchestraScore.Part { instrument = parts[part] };
                    for (int difficulty = 0; difficulty < 4; difficulty++)
                    {
                        var notes = new List<DanceNote>();
                        double interval = part == 2 ? 4 : difficulty == 0 ? 2 : difficulty == 1 ? 1 : .5;
                        for (double at = 2 + part * .25; at < 63; at += interval)
                        {
                            int lane = ((int)Math.Floor(at) + part + song) % 4;
                            int mask = difficulty >= 2 && ((int)(at * 2) % 8 == 0) ? new[] { 3, 5, 10, 12 }[lane] : 1 << lane;
                            double hold = difficulty > 0 && (part == 2 || (int)at % 8 == 4) ? Math.Min(interval * .65, 1.5) * beat : 0;
                            notes.Add(new DanceNote(at * beat, mask, hold));
                        }
                        p.difficulties[difficulty] = new DanceSongLibrary.Chart { notes = notes.ToArray() };
                    }
                }
                for (int difficulty = 0; difficulty < 4; difficulty++) s.charts[difficulty] = arrangement.parts[0].difficulties[difficulty];
                var pcm = new float[(int)Math.Ceiling(duration * Rate)];
                // Quiet strings and bass form a coherent harmonic bed. The four
                // playable sections themselves sound only from accepted input.
                for (int bar = 0; bar < 16; bar++)
                {
                    int root = OrchestraScore.MidiFor(0, 0, bar, song % 5) - 24;
                    for (int voice = 0; voice < 3; voice++) Add(pcm, (2 + bar * 4) * beat, beat * 3.8, root + new[] { 0, 7, 16 }[voice], 4, .055f);
                    for (int step = 0; step < 4; step++) Add(pcm, (2 + bar * 4 + step) * beat, .13, root, 1, .035f);
                }
                s.music = Save(names[song], pcm, true);
            }
            for (int instrument = 0; instrument < 4; instrument++)
            {
                var pcm = new float[(int)(Rate * 1.8)]; Add(pcm, 0, 1.75, 60, instrument, .65f); Save("Original instrument " + instrument + " C4", pcm, false);
            }
            var error = new float[Rate / 4]; Add(error, 0, .24, 47, 2, .25f); Add(error, 0, .24, 48, 2, .18f); Save("Playful wrong-note blip", error, false);
            library.Validate(); score.Validate(library);
            AssetDatabase.CreateAsset(library, Folder + "Original symphony library.asset"); AssetDatabase.CreateAsset(score, Folder + "Original instrumental parts.asset"); return library;
        }
        static void Add(float[] pcm, double at, double length, int midi, int instrument, float amplitude)
        {
            double frequency = 440 * Math.Pow(2, (midi - 69) / 12.0); int begin = (int)(at * Rate), count = (int)(length * Rate);
            uint noise = (uint)(midi * 7919 + instrument * 199 + 1);
            for (int i = 0; i < count && begin + i < pcm.Length; i++)
            {
                double t = i / (double)Rate, phase = t * frequency * Math.PI * 2;
                noise = noise * 1664525 + 1013904223; double n = (noise / (double)uint.MaxValue * 2 - 1);
                double wave;
                switch (instrument)
                {
                    case 0: wave = (Math.Sin(phase) + .36 * Math.Sin(phase * 2.003) + .14 * Math.Sin(phase * 3.01)) * Math.Exp(-t * 3.3); break;
                    case 1: wave = (Math.Sin(phase * Math.Exp(-t * 8)) * .65 + n * .35) * Math.Exp(-t * 16); break;
                    case 2: wave = (Math.Sin(phase) + .33 * Math.Sin(phase * 2) + .2 * Math.Sin(phase * 3)) * Math.Min(1, t * 40) * Math.Exp(-t * 3); break;
                    case 3: wave = (Math.Sin(phase) + .34 * Math.Sin(phase * 2.76) + .18 * Math.Sin(phase * 5.4)) * Math.Exp(-t * 4.4); break;
                    default: wave = (Math.Sin(phase) + .2 * Math.Sin(phase * 2 + Math.Sin(t * 7) * .1)) * Math.Min(1, t * 5) * Math.Min(1, (length - t) * 5); break;
                }
                double fade = Math.Min(1, i / 100.0) * Math.Min(1, (count - i) / 180.0);
                pcm[begin + i] += (float)(wave * amplitude * fade);
            }
        }
        static AudioClip Save(string name, float[] pcm, bool music)
        {
            string path = Folder + name + ".wav";
            if (File.Exists(path)) throw new InvalidOperationException("Preserve original audio asset: " + path);
            using (var file = new BinaryWriter(File.Create(path)))
            {
                file.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); file.Write(36 + pcm.Length * 2); file.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
                file.Write(16); file.Write((short)1); file.Write((short)1); file.Write(Rate); file.Write(Rate * 2); file.Write((short)2); file.Write((short)16);
                file.Write(System.Text.Encoding.ASCII.GetBytes("data")); file.Write(pcm.Length * 2);
                foreach (float value in pcm) file.Write((short)(Mathf.Clamp(value, -.95f, .95f) * 32767));
            }
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (AudioImporter)AssetImporter.GetAtPath(path); var settings = importer.defaultSampleSettings;
            settings.loadType = music ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad; settings.compressionFormat = AudioCompressionFormat.Vorbis; settings.quality = .85f;
            importer.defaultSampleSettings = settings; importer.forceToMono = true; importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }
    }
}

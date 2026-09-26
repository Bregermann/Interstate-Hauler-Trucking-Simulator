using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;

namespace LWS.TruckTaxi.Editor
{
    // Editor-only discovery. Filenames identify existing assets, never character likenesses.
    public static class TruckTaxiVoiceReferenceDiscovery
    {
        public enum MatchStatus { Matched, NoSample, UnknownPassengerId, UnknownEmotion, DuplicateMatch }
        public sealed class Match
        {
            public PassengerProfile Passenger;
            public TruckTaxiVoiceProfile Voice;
            public TruckTaxiVoiceEmotion Emotion;
            public string Prefix, Tone, Path, Detail;
            public MatchStatus Status;
            public string StatusText => Status==MatchStatus.Matched ? "MATCHED" : Status==MatchStatus.NoSample ? "NO SAMPLE" :
                Status==MatchStatus.UnknownPassengerId ? "UNKNOWN PASSENGER ID" : Status==MatchStatus.UnknownEmotion ? "UNKNOWN EMOTION" : "DUPLICATE MATCH";
        }
        public sealed class Report
        {
            public readonly List<Match> Rows=new List<Match>();
            public bool HasErrors => Rows.Any(r=>r.Status!=MatchStatus.Matched && r.Status!=MatchStatus.NoSample);
            public int MatchedCount => Rows.Count(r=>r.Status==MatchStatus.Matched);
            public string ToMarkdown()
            {
                var text=new StringBuilder("# Truck Taxi Voice Reference Matches\n\nExact stable IDs only. Source WAVs are read-only. NO SAMPLE leaves existing references and neutral fallback intact.\n\nPassenger ID | Passenger Name | Emotion | Matched WAV | Status\n---|---|---|---|---\n");
                foreach(var r in Rows) text.AppendLine(string.Join(" | ",Escape(r.Passenger?.passengerId ?? r.Prefix),Escape(r.Passenger?.passengerName ?? r.Voice?.displayName),Escape(r.Tone),Escape(r.Path),r.StatusText+(string.IsNullOrEmpty(r.Detail) ? "" : " ("+Escape(r.Detail)+")")));
                return text.ToString();
            }
            private static string Escape(string text) => (text ?? "-").Replace("|","\\|").Replace("\r"," ").Replace("\n"," ");
        }
        public static string Root => System.IO.Path.Combine(TruckTaxiChatterboxSettings.instance.workHere,"voice_refs");
        public static Report Scan()
        {
            if(!Directory.Exists(Root)) throw new DirectoryNotFoundException("Voice reference folder not found: "+Root);
            var voices=AssetDatabase.FindAssets("t:TruckTaxiVoiceProfile",new[]{"Assets/LWS/TruckTaxi"})
                .Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<TruckTaxiVoiceProfile>);
            return MatchFiles(Directory.EnumerateFiles(Root,"*",SearchOption.AllDirectories)
                .Where(p=>string.Equals(System.IO.Path.GetExtension(p),".wav",StringComparison.OrdinalIgnoreCase)),TruckTaxiPassengerDialogueAuthoring.AllPassengers(),voices);
        }
        public static Report MatchFiles(IEnumerable<string> paths,IEnumerable<PassengerProfile> passengers,IEnumerable<TruckTaxiVoiceProfile> voices)
        {
            var report=new Report(); var roster=passengers.Where(p=>p!=null).Distinct().ToArray();
            var profiles=voices.Concat(roster.Select(p=>p.voiceProfile)).Where(v=>v!=null).Distinct().ToArray();
            foreach(string file in paths.Select(System.IO.Path.GetFullPath).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(p=>p,StringComparer.OrdinalIgnoreCase))
            {
                string name=System.IO.Path.GetFileNameWithoutExtension(file); int split=name.LastIndexOf("__",StringComparison.Ordinal);
                string prefix=split>0 ? name.Substring(0,split) : name;
                string suffix=split>0 ? name.Substring(split+2) : "";
                var people=roster.Where(p=>string.Equals(p.passengerId,prefix,StringComparison.OrdinalIgnoreCase)).ToArray();
                var candidates=profiles.Where(v=>string.Equals(v.voiceProfileId,prefix,StringComparison.OrdinalIgnoreCase))
                    .Concat(people.Select(p=>p.voiceProfile)).Where(v=>v!=null).Distinct().ToArray();
                var row=new Match { Prefix=prefix,Tone=suffix,Path=file,Passenger=people.FirstOrDefault(),Voice=candidates.Length==1 ? candidates[0] : null };
                report.Rows.Add(row);
                if(people.Length>1 || candidates.Length>1) { row.Status=MatchStatus.DuplicateMatch; row.Detail="ID resolves to multiple assets"; continue; }
                if(candidates.Length==0) { row.Status=MatchStatus.UnknownPassengerId; row.Detail=people.Length==1 ? "Passenger has no Voice Profile" : "No exact stable ID"; continue; }
                row.Passenger=row.Passenger ?? roster.FirstOrDefault(p=>p.voiceProfile==row.Voice);
                // Enum.TryParse alone also accepts numeric values; suffix must be an actual authored name.
                if(!Enum.GetNames(typeof(TruckTaxiVoiceEmotion)).Any(n=>string.Equals(n,suffix,StringComparison.OrdinalIgnoreCase)) ||
                    !Enum.TryParse(suffix,true,out row.Emotion) || !row.Voice.SupportsTone(row.Emotion))
                { row.Status=MatchStatus.UnknownEmotion; row.Detail="Unknown or unsupported authored tone"; continue; }
                row.Status=MatchStatus.Matched;
            }
            foreach(var group in report.Rows.Where(r=>r.Status==MatchStatus.Matched).GroupBy(r=>new { r.Voice,r.Emotion }))
            {
                bool duplicate=group.Count()>1 || (group.Key.Voice.additionalReferences ?? Array.Empty<TruckTaxiVoiceProfile.VoiceReference>()).Count(r=>r!=null && r.emotion==group.Key.Emotion)>1;
                if(duplicate) foreach(var row in group) { row.Status=MatchStatus.DuplicateMatch; row.Detail="Multiple files or authored entries for this voice/tone; no selection made"; }
            }
            foreach(var voice in profiles.OrderBy(v=>v.voiceProfileId,StringComparer.OrdinalIgnoreCase))
                foreach(TruckTaxiVoiceEmotion emotion in Enum.GetValues(typeof(TruckTaxiVoiceEmotion)))
                    if(voice.SupportsTone(emotion) && !report.Rows.Any(r=>r.Voice==voice && string.Equals(r.Tone,emotion.ToString(),StringComparison.OrdinalIgnoreCase)))
                        report.Rows.Add(new Match { Voice=voice,Passenger=roster.FirstOrDefault(p=>p.voiceProfile==voice),Prefix=voice.voiceProfileId,Emotion=emotion,Tone=emotion.ToString(),Status=MatchStatus.NoSample,Path="" });
            return report;
        }
        public static int ApplyValidated(Report report)
        {
            if(report==null || report.HasErrors) throw new InvalidOperationException("Resolve UNKNOWN/DUPLICATE rows before assignment or generation. Source files are untouched.");
            foreach(var row in report.Rows.Where(r=>r.Status==MatchStatus.Matched))
                if(!File.Exists(row.Path)) throw new FileNotFoundException("Reference changed since scan. Scan again.",row.Path);
            int changes=0;
            foreach(var row in report.Rows.Where(r=>r.Status==MatchStatus.Matched))
            {
                string current=row.Voice.FindReference(row.Emotion);
                if(!string.IsNullOrEmpty(current) && string.Equals(System.IO.Path.GetFullPath(current),row.Path,StringComparison.OrdinalIgnoreCase)) continue;
                Undo.RecordObject(row.Voice,"Auto-assign voice reference"); row.Voice.SetReference(row.Emotion,row.Path);
                EditorUtility.SetDirty(row.Voice); changes++;
            }
            AssetDatabase.SaveAssets(); TruckTaxiPassengerDialogueAuthoring.WriteMissingVoiceReport(); return changes;
        }
        public static string WriteReport(Report report)
        {
            const string path="Assets/LWS/TruckTaxi/Documentation/TruckTaxi_VoiceReferenceMatches.md";
            File.WriteAllText(path,report.ToMarkdown()); AssetDatabase.ImportAsset(path); return path;
        }
        public static void ScanAndWriteReport() { var report=Scan(); WriteReport(report); UnityEngine.Debug.Log("VOICE DISCOVERY: "+report.MatchedCount+" matched; errors="+report.HasErrors); }
    }
}

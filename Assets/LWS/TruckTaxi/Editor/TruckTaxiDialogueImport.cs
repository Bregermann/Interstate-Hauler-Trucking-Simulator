using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace LWS.TruckTaxi.Editor
{
    public static class TruckTaxiDialogueImport
    {
        public static Func<string,List<List<string>>> ReadVendorCsv;
        public static Action<List<List<string>>,string> WriteVendorCsv;
        public const string ExportFolder = "Tools/TruckTaxiPassengerFactory/DialogueCSV";
        [Serializable] public sealed class Entry
        {
            public string passengerId,lineId,category,emotion,text,subtitle,tags;
            public float weight=1,cooldown=8;
            public int priority;
            public bool oneShot;
        }
        [Serializable] public sealed class Document { public Entry[] lines; }
        public static string Export(IEnumerable<PassengerProfile> selected, string filename)
        {
            if (WriteVendorCsv == null) throw new InvalidOperationException("Pixel Crushers CSV bridge is unavailable.");
            Directory.CreateDirectory(ExportFolder);
            string path=Path.Combine(ExportFolder,filename);
            var rows=new List<List<string>> { new List<string> { "PassengerID","LineID","Category","Emotion","Text","Subtitle","Weight","Priority","Cooldown","OneShot","Tags","GeneratedAudioStatus" } };
            foreach(var p in selected.Where(p=>p!=null).OrderBy(p=>p.passengerId,StringComparer.Ordinal))
                foreach(var line in (p.authoredDialogue?.lines ?? Array.Empty<TruckTaxiDialogueLine>()).Where(l=>l!=null).OrderBy(l=>l.lineId,StringComparer.Ordinal))
                    rows.Add(new List<string> { p.passengerId,line.lineId,line.category.ToString(),line.emotion.ToString(),line.text,line.subtitle,
                        line.weight.ToString("R",CultureInfo.InvariantCulture),line.interruptPriority.ToString(CultureInfo.InvariantCulture),
                        line.cooldown.ToString("R",CultureInfo.InvariantCulture),line.oneShot.ToString(),string.Join(";",line.gameplayTags ?? Array.Empty<string>()),
                        line.generatedAudio==null ? "MISSING" : string.IsNullOrEmpty(line.generationRequestHash) ? "STALE" : "GENERATED" });
            WriteVendorCsv(rows,path);
            return path;
        }
        public static int Import(string path, string selectedPassengerId=null)
        {
            Entry[] entries;
            if(Path.GetExtension(path).Equals(".json",StringComparison.OrdinalIgnoreCase)) entries=JsonUtility.FromJson<Document>(File.ReadAllText(path)).lines;
            else
            {
                List<List<string>> rows;
                if(Path.GetExtension(path).Equals(".csv",StringComparison.OrdinalIgnoreCase))
                { if(ReadVendorCsv==null) throw new InvalidOperationException("Pixel Crushers CSV bridge is unavailable."); rows=ReadVendorCsv(path); }
                else rows=File.ReadAllLines(path).Where(l=>!string.IsNullOrWhiteSpace(l) && !l.StartsWith("#")).Select(l=>l.Split('\t').ToList()).ToList();
                if(rows==null || rows.Count<2) throw new InvalidOperationException("Header and at least one data row required.");
                var header=rows[0].Select(h=>h.Trim().ToLowerInvariant()).ToArray();
                string Cell(List<string> row,string name) { int i=Array.IndexOf(header,name); return i>=0 && i<row.Count ? row[i] : ""; }
                entries=rows.Skip(1).Select(row=>new Entry { passengerId=Cell(row,"passengerid"),lineId=Cell(row,"lineid"),category=Cell(row,"category"),emotion=Cell(row,"emotion"),
                    text=Cell(row,"text"),subtitle=Cell(row,"subtitle"),tags=Cell(row,"tags"),
                    weight=float.TryParse(Cell(row,"weight"),NumberStyles.Float,CultureInfo.InvariantCulture,out var w) ? w : 1,
                    cooldown=float.TryParse(Cell(row,"cooldown"),NumberStyles.Float,CultureInfo.InvariantCulture,out var c) ? c : 8,
                    priority=int.TryParse(Cell(row,"priority"),NumberStyles.Integer,CultureInfo.InvariantCulture,out var priority) ? priority : 0,
                    oneShot=bool.TryParse(Cell(row,"oneshot"),out var oneShot) && oneShot }).ToArray();
            }
            if(entries==null) throw new InvalidOperationException("JSON requires a lines array.");
            if(entries.GroupBy(e=>(e?.passengerId ?? "")+"\n"+(e?.lineId ?? ""),StringComparer.OrdinalIgnoreCase).Any(g=>g.Count()>1))
                throw new InvalidOperationException("Duplicate passenger/line IDs in import. Nothing imported.");
            var passengers=TruckTaxiPassengerDialogueAuthoring.AllPassengers().ToDictionary(p=>p.passengerId);
            foreach(var e in entries)
                if(e==null || !passengers.ContainsKey(e.passengerId ?? "") || (selectedPassengerId!=null && e.passengerId!=selectedPassengerId) ||
                    !TruckTaxiChatterboxQueue.IsSafeId(e.lineId) || string.IsNullOrWhiteSpace(e.text) || e.weight<0 || e.cooldown<0 ||
                    !Enum.TryParse(e.category,out TruckTaxiDialogueCategory _) || !Enum.TryParse(string.IsNullOrEmpty(e.emotion) ? "Neutral" : e.emotion,out TruckTaxiVoiceEmotion _))
                    throw new InvalidOperationException("Invalid passenger, line ID, text, category or emotion. Nothing imported.");
            foreach(var e in entries)
            {
                var p=passengers[e.passengerId]; TruckTaxiPassengerFactoryBuilder.BuildMissing(p);
                var lines=p.authoredDialogue.lines.ToList(); var line=lines.FirstOrDefault(l=>l.lineId==e.lineId);
                if(line==null) { line=new TruckTaxiDialogueLine { lineId=e.lineId,passengerId=e.passengerId }; lines.Add(line); }
                if(line.text!=e.text) { line.generationHash=line.generationRequestHash=""; }
                line.text=e.text; line.subtitle=string.IsNullOrWhiteSpace(e.subtitle) ? e.text : e.subtitle;
                line.category=(TruckTaxiDialogueCategory)Enum.Parse(typeof(TruckTaxiDialogueCategory),e.category);
                line.emotion=(TruckTaxiVoiceEmotion)Enum.Parse(typeof(TruckTaxiVoiceEmotion),string.IsNullOrEmpty(e.emotion) ? "Neutral" : e.emotion);
                line.weight=Mathf.Max(0,e.weight); line.gameplayTags=(e.tags ?? "").Split(new[]{';'},StringSplitOptions.RemoveEmptyEntries);
                line.cooldown=e.cooldown; line.interruptPriority=e.priority; line.oneShot=e.oneShot;
                p.authoredDialogue.lines=lines.ToArray(); EditorUtility.SetDirty(p.authoredDialogue);
            }
            AssetDatabase.SaveAssets(); return entries.Length;
        }
    }
}

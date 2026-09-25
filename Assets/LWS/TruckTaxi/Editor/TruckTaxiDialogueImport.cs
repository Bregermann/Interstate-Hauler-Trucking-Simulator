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
        [Serializable] public sealed class Entry { public string passengerId,lineId,category,emotion,text,subtitle,tags; public float weight=1; }
        [Serializable] public sealed class Document { public Entry[] lines; }
        public static int Import(string path)
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
                    text=Cell(row,"text"),subtitle=Cell(row,"subtitle"),tags=Cell(row,"tags"),weight=float.TryParse(Cell(row,"weight"),NumberStyles.Float,CultureInfo.InvariantCulture,out var w) ? w : 1 }).ToArray();
            }
            if(entries==null) throw new InvalidOperationException("JSON requires a lines array.");
            var passengers=TruckTaxiPassengerDialogueAuthoring.AllPassengers().ToDictionary(p=>p.passengerId);
            foreach(var e in entries)
                if(e==null || !passengers.ContainsKey(e.passengerId ?? "") || !TruckTaxiChatterboxQueue.IsSafeId(e.lineId) || string.IsNullOrWhiteSpace(e.text) ||
                    !Enum.TryParse(e.category,out TruckTaxiDialogueCategory _) || !Enum.TryParse(string.IsNullOrEmpty(e.emotion) ? "Neutral" : e.emotion,out TruckTaxiVoiceEmotion _))
                    throw new InvalidOperationException("Invalid passenger, line ID, text, category or emotion. Nothing imported.");
            foreach(var e in entries)
            {
                var p=passengers[e.passengerId]; TruckTaxiPassengerFactoryBuilder.BuildMissing(p);
                var lines=p.authoredDialogue.lines.ToList(); var line=lines.FirstOrDefault(l=>l.lineId==e.lineId);
                if(line==null) { line=new TruckTaxiDialogueLine { lineId=e.lineId,passengerId=e.passengerId }; lines.Add(line); }
                if(line.text!=e.text) { line.generatedAudio=null; line.generationHash=line.generationRequestHash=""; }
                line.text=e.text; line.subtitle=string.IsNullOrWhiteSpace(e.subtitle) ? e.text : e.subtitle;
                line.category=(TruckTaxiDialogueCategory)Enum.Parse(typeof(TruckTaxiDialogueCategory),e.category);
                line.emotion=(TruckTaxiVoiceEmotion)Enum.Parse(typeof(TruckTaxiVoiceEmotion),string.IsNullOrEmpty(e.emotion) ? "Neutral" : e.emotion);
                line.weight=Mathf.Max(0,e.weight); line.gameplayTags=(e.tags ?? "").Split(new[]{';'},StringSplitOptions.RemoveEmptyEntries);
                p.authoredDialogue.lines=lines.ToArray(); EditorUtility.SetDirty(p.authoredDialogue);
            }
            AssetDatabase.SaveAssets(); return entries.Length;
        }
    }
}

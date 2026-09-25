using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LWS.TruckTaxi
{
    public interface ITruckTaxiBarkOutput
    {
        bool IsPlaying { get; }
        bool AudioPlaying { get; }
        string Subtitle { get; }
        void Play(string speaker, string subtitle, AudioClip clip);
        void Stop();
        void SetPaused(bool paused);
    }
    // The vendor lives in Assembly-CSharp. A project-owned bridge registers once,
    // keeping the runtime asmdef free of reflection and vendor source changes.
    public static class TruckTaxiBarkBridge
    {
        public static Func<GameObject,ITruckTaxiBarkOutput> Create;
    }
    public sealed class TruckTaxiDialoguePlayer : MonoBehaviour
    {
        public string Subtitle => output?.Subtitle ?? fallbackSubtitle;
        public bool AudioPlaying => output?.AudioPlaying == true;
        public bool IsPlaying => output?.IsPlaying == true || Time.time < fallbackUntil;
        public int SpokenLines { get; private set; }
        public TruckTaxiDialogueLine LastLine { get; private set; }
        private ITruckTaxiBarkOutput output;
        private readonly Dictionary<string,float> cooldowns = new Dictionary<string,float>();
        private readonly HashSet<string> used = new HashSet<string>();
        private string fallbackSubtitle = "";
        private float fallbackUntil;
        private int priority;
        private void Awake()
        {
            output=TruckTaxiBarkBridge.Create?.Invoke(gameObject);
            if(output==null) Debug.LogWarning("Truck Taxi Pixel Crushers bark bridge unavailable; subtitles remain active.",this);
        }
        public bool Speak(PassengerProfile passenger, TruckTaxiDialogueCategory category, TruckTaxiSession session,
            string fallback = null, int requestedPriority = 10)
        {
            if(passenger==null) return false;
            var set=category==TruckTaxiDialogueCategory.EjectionReaction && passenger.ejectionReactions!=null ? passenger.ejectionReactions : passenger.authoredDialogue;
            var eligible=new List<TruckTaxiDialogueLine>();
            bool authoredCategory=false;
            if(set?.lines!=null) foreach(var line in set.lines)
            {
                if(line==null || line.category!=category || string.IsNullOrWhiteSpace(line.Subtitle) || line.weight<=0) continue;
                authoredCategory=true;
                string key=passenger.passengerId+":"+line.lineId;
                if(line.oneShot && used.Contains(key)) continue;
                if(cooldowns.TryGetValue(key,out var until) && Time.time<until) continue;
                if(line.rideStateRequirements?.Length>0 && !Array.Exists(line.rideStateRequirements,s=>s==session.State)) continue;
                if(line.requestRequirements?.Length>0 && !session.Requests.Any(r=>r.State==TaxiRequestState.Active && Array.IndexOf(line.requestRequirements,r.Definition.requestType)>=0)) continue;
                eligible.Add(line);
            }
            if(authoredCategory && eligible.Count==0) return false;
            TruckTaxiDialogueLine chosen=null;
            float total=0; foreach(var line in eligible) total+=line.weight;
            float roll=UnityEngine.Random.value*total;
            foreach(var line in eligible) { roll-=line.weight; if(roll<=0) { chosen=line; break; } }
            if(chosen==null && string.IsNullOrWhiteSpace(fallback)) return false;
            int nextPriority=Mathf.Max(requestedPriority,chosen?.interruptPriority ?? 0);
            if(IsPlaying && nextPriority<priority) return false;
            Stop(); priority=nextPriority; LastLine=chosen;
            string text=chosen?.Subtitle ?? fallback;
            if(chosen!=null)
            {
                string key=passenger.passengerId+":"+chosen.lineId;
                cooldowns[key]=Time.time+chosen.cooldown; used.Add(key);
            }
            if(output!=null) output.Play(passenger.passengerName,text,chosen?.generatedAudio);
            else { fallbackSubtitle=passenger.passengerName+": "+text; fallbackUntil=Time.time+Mathf.Clamp(text.Length/14f,3,12); }
            SpokenLines++; return true;
        }
        public void ResetRide() { Stop(); cooldowns.Clear(); used.Clear(); }
        public void Stop() { output?.Stop(); fallbackUntil=0; fallbackSubtitle=""; priority=0; }
        public void SetPaused(bool paused) => output?.SetPaused(paused);
        private void Update() { if(Time.time>=fallbackUntil) fallbackSubtitle=""; }
        private void OnDisable() => Stop();
    }
}

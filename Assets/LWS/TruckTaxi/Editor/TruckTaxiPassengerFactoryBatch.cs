using System;
using System.IO;
using System.Linq;
using UnityEditor;

namespace LWS.TruckTaxi.Editor
{
    [InitializeOnLoad]
    public static class TruckTaxiPassengerFactoryBatch
    {
        private static PassengerProfile[] queue;
        private static int index,stage;
        private static bool rebuild,cancel,waitingModel;
        public static bool Running=>queue!=null;
        public static bool Paused { get; private set; }
        public static int Completed { get; private set; }
        public static int Warnings { get; private set; }
        public static int Failures { get; private set; }
        public static int Cached { get; private set; }
        public static int Skipped { get; private set; }
        public static string Status { get; private set; }="Factory ready";
        public static string Log { get; private set; }="";
        static TruckTaxiPassengerFactoryBatch()=>EditorApplication.update+=Tick;
        public static void Start(PassengerProfile[] passengers,bool force=false)
        {
            if(Running || TruckTaxiModelPipeline.Busy || TruckTaxiChatterboxQueue.IsRunning) throw new InvalidOperationException("Wait for the current factory operation.");
            queue=passengers.Where(p=>p!=null).Distinct().ToArray(); index=stage=0; Completed=Warnings=Failures=Cached=Skipped=0; rebuild=force; cancel=Paused=false; Log="";
        }
        public static void Pause()=>Paused=true;
        public static void Resume()=>Paused=false;
        public static void CancelAfterCurrent()=>cancel=true;
        private static void Tick()
        {
            if(queue==null || TruckTaxiModelPipeline.Busy || TruckTaxiChatterboxQueue.IsRunning) return;
            if(waitingModel) { waitingModel=false; if(!TruckTaxiModelPipeline.LastSucceeded) { Failures++; Log+=TruckTaxiModelPipeline.Status+"\n"; } }
            if(cancel || index>=queue.Length)
            {
                queue=null; TruckTaxiPassengerFactoryBuilder.RegisterAll(); TruckTaxiPassengerFactoryBuilder.WriteReports();
                Status=(cancel ? "Cancelled after current operation" : "Factory batch complete")+$" | completed {Completed}, warnings {Warnings}, failures {Failures}, cached {Cached}, skipped {Skipped}"; return;
            }
            if(Paused) return;
            var p=queue[index];
            Status=$"{index+1}/{queue.Length} {p.passengerName} | {TruckTaxiPassengerFactoryBuilder.Stages[stage]} | Completed {Completed}, warnings {Warnings}, failures {Failures}, cached {Cached}, skipped {Skipped}, remaining {queue.Length-index}";
            try
            {
                switch(stage)
                {
                    case 0: TruckTaxiPassengerFactoryBuilder.BuildMissing(p); break;
                    case 1: TruckTaxiPassengerFactoryBuilder.WriteManifest(p); break;
                    case 2:
                        if(File.Exists(p.appearance.sourceModelPath))
                        {
                            if(rebuild || p.modelPrefab==null || !File.Exists(p.appearance.processedModelPath) || p.appearance.generationHash!=TruckTaxiModelPipeline.Signature(p)) { TruckTaxiModelPipeline.ProcessModel(p); waitingModel=true; }
                            else Cached++;
                        }
                        else { Skipped++; Warnings++; }
                        break;
                    case 3: if(p.animatorProfile==null) p.animatorProfile=TruckTaxiPassengerFactoryBuilder.SharedAnimations(); break;
                    case 4: if(p.runtimePrefab==null || rebuild) TruckTaxiPassengerFactoryBuilder.BuildPrefab(p); else Cached++; break;
                    case 5:
                        foreach(var issue in TruckTaxiPassengerFactoryBuilder.Validate(p)) { if(issue.StartsWith("ERROR")) Failures++; else Warnings++; Log+=p.passengerId+": "+issue+"\n"; }
                        break;
                    case 6: TruckTaxiPassengerFactoryBuilder.RegisterAll(); break;
                    case 7:
                        if(File.Exists(p.voiceProfile.ResolveReference(TruckTaxiVoiceEmotion.Neutral,out _))) TruckTaxiChatterboxQueue.Generate(new[]{p},true);
                        else Skipped++;
                        break;
                }
            }
            catch(Exception ex) { Failures++; Log+=p.passengerId+" / "+TruckTaxiPassengerFactoryBuilder.Stages[stage]+": "+ex.Message+"\n"; }
            stage++; if(stage>=TruckTaxiPassengerFactoryBuilder.Stages.Length) { stage=0; index++; Completed++; }
        }
    }
}

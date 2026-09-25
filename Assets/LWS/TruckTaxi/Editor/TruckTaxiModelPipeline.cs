using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;
using Debug=UnityEngine.Debug;

namespace LWS.TruckTaxi.Editor
{
    public interface ITruckTaxiModelGenerationBackend { string Generate(PassengerProfile passenger); }
    public sealed class ManualDropFolderGenerator : ITruckTaxiModelGenerationBackend
    {
        public string Generate(PassengerProfile passenger)
        {
            string folder=TruckTaxiModelPipeline.SourceFolder(passenger); Directory.CreateDirectory(folder);
            return "Place FBX, GLB or OBJ in "+folder+" and assign Appearance > Source Model Path. Sources remain untouched.";
        }
    }
    public sealed class PlaceholderGenerator : ITruckTaxiModelGenerationBackend
    {
        public string Generate(PassengerProfile passenger) { TruckTaxiPassengerFactoryBuilder.BuildPrefab(passenger); return "Working fallback prefab built; final model still missing."; }
    }
    public sealed class ConfiguredExternalGenerator : ITruckTaxiModelGenerationBackend
    {
        public string Generate(PassengerProfile passenger)
        {
            var settings=TruckTaxiFactorySettings.instance;
            if(!File.Exists(settings.generatorExecutable)) return "No external 3D generator configured. Manifest/manual-drop/fallback remain available.";
            string manifest=TruckTaxiPassengerFactoryBuilder.WriteManifest(passenger);
            TruckTaxiModelPipeline.StartExternal(settings.generatorExecutable,settings.generatorArguments.Replace("{manifest}",TruckTaxiModelPipeline.Quote(Path.GetFullPath(manifest))),()=>{});
            return "Configured external generator started using manifest; inspect source output before processing.";
        }
    }
    [FilePath("UserSettings/TruckTaxiFactory.asset",FilePathAttribute.Location.ProjectFolder)]
    public sealed class TruckTaxiFactorySettings : ScriptableSingleton<TruckTaxiFactorySettings>
    {
        public string blender="C:/Program Files/Blender Foundation/Blender 3.5/blender.exe";
        public string generatorExecutable, generatorArguments="{manifest}";
        public void Persist()=>Save(true);
    }
    [InitializeOnLoad]
    public static class TruckTaxiModelPipeline
    {
        [Serializable] private sealed class Job { public string passengerId,source,output; public float heightMeters,lodReduction; public int targetPolygons; }
        private static Process process;
        private static Action completed;
        private static readonly ConcurrentQueue<string> lines=new ConcurrentQueue<string>();
        public static bool Busy=>process!=null;
        public static string Status { get; private set; }="Ready";
        public static string Log { get; private set; }="";
        public static bool LastSucceeded { get; private set; }=true;
        static TruckTaxiModelPipeline() { EditorApplication.update+=Poll; EditorApplication.quitting+=Quit; }
        public static string SourceFolder(PassengerProfile p)=>"Tools/TruckTaxiPassengerFactory/Models/Source/"+p.passengerId;
        public static string Signature(PassengerProfile p)
        {
            // Output/status fields are excluded so completion does not invalidate its own cache.
            string values=TruckTaxiChatterboxQueue.HashFile(p.appearance.sourceModelPath)+"|"+p.appearance.heightMeters.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+"|"+
                p.appearance.lodReduction.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+"|"+p.appearance.targetPolygons+"|"+p.appearance.rigType+"|model-pipeline-2";
            using(var sha=SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(values))).Replace("-","").ToLowerInvariant();
        }
        public static string Generate(PassengerProfile p)
        {
            TruckTaxiPassengerFactoryBuilder.WriteManifest(p);
            ITruckTaxiModelGenerationBackend backend=p.appearance.generationBackend==TruckTaxiModelBackend.ConfiguredExternal ? new ConfiguredExternalGenerator() :
                p.appearance.generationBackend==TruckTaxiModelBackend.Placeholder ? new PlaceholderGenerator() : new ManualDropFolderGenerator();
            return backend.Generate(p);
        }
        public static void ProcessModel(PassengerProfile p)
        {
            if(p?.appearance==null || !File.Exists(p.appearance.sourceModelPath)) throw new InvalidOperationException("Assign a source model first. The current fallback stays usable.");
            if(!File.Exists(TruckTaxiFactorySettings.instance.blender)) throw new FileNotFoundException("Configure installed Blender in Passenger Factory.");
            string folder="Assets/LWS/TruckTaxi/Passengers/Models/Processed/"+p.passengerId;
            TruckTaxiPassengerDialogueAuthoring.EnsureFolder(folder);
            string target=folder+"/"+p.passengerId+".fbx";
            if(Path.GetFullPath(target)==Path.GetFullPath(p.appearance.sourceModelPath)) throw new InvalidOperationException("Source and processed file must differ.");
            string jobs="Tools/TruckTaxiPassengerFactory/Jobs"; Directory.CreateDirectory(jobs);
            string job=jobs+"/model-"+p.passengerId+".json";
            File.WriteAllText(job,JsonUtility.ToJson(new Job { passengerId=p.passengerId,source=Path.GetFullPath(p.appearance.sourceModelPath),output=Path.GetFullPath(target),
                heightMeters=p.appearance.heightMeters,lodReduction=p.appearance.lodReduction,targetPolygons=p.appearance.targetPolygons },true));
            StartExternal(TruckTaxiFactorySettings.instance.blender,"--background --factory-startup --python "+Quote(Path.GetFullPath("Tools/TruckTaxiPassengerFactory/Models/process_model.py"))+" -- --job "+Quote(Path.GetFullPath(job)),()=>
            {
                AssetDatabase.ImportAsset(target,ImportAssetOptions.ForceSynchronousImport);
                p.appearance.processedModelPath=target; p.appearance.generationStatus="Processed; verify rig report";
                p.appearance.generationHash=Signature(p);
                if(!SetupRig(p)) throw new InvalidOperationException(p.appearance.generationStatus);
                p.modelPrefab=AssetDatabase.LoadAssetAtPath<GameObject>(target);
                EditorUtility.SetDirty(p); EditorUtility.SetDirty(p.appearance);
                TruckTaxiPassengerFactoryBuilder.BuildPrefab(p); AssetDatabase.SaveAssets();
            });
        }
        public static bool SetupRig(PassengerProfile p)
        {
            string path=p.appearance?.processedModelPath;
            if(string.IsNullOrEmpty(path) || !path.StartsWith("Assets/LWS/TruckTaxi/Passengers/Models/Processed/",StringComparison.Ordinal))
                throw new InvalidOperationException("Rig setup only modifies project-owned processed models, never vendor/source import settings.");
            if(!(AssetImporter.GetAtPath(path) is ModelImporter importer)) throw new InvalidOperationException("Processed model not imported.");
            importer.animationType=p.appearance.rigType==TruckTaxiRigType.Humanoid ? ModelImporterAnimationType.Human : ModelImporterAnimationType.Generic;
            importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel; importer.SaveAndReimport();
            var avatar=AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<Animator>()?.avatar;
            if(p.appearance.rigType==TruckTaxiRigType.Humanoid && (avatar==null || !avatar.isValid || !avatar.isHuman))
            { p.appearance.generationStatus="NEEDS MANUAL HUMANOID MAPPING; fallback retained"; EditorUtility.SetDirty(p.appearance); AssetDatabase.SaveAssets(); return false; }
            return true;
        }
        public static string Quote(string value) { if(value.Contains("\"") || value.Contains("\n")) throw new ArgumentException("Invalid path"); return "\""+value.TrimEnd('\\')+"\""; }
        public static void StartExternal(string exe,string args,Action onComplete)
        {
            if(Busy) throw new InvalidOperationException("Wait for the current model operation.");
            var start=new ProcessStartInfo(exe,args) { UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true,WorkingDirectory=TruckTaxiChatterboxQueue.ProjectRoot };
            process=new Process { StartInfo=start }; process.OutputDataReceived+=(_,e)=>{if(e.Data!=null) lines.Enqueue(e.Data);}; process.ErrorDataReceived+=(_,e)=>{if(e.Data!=null) lines.Enqueue(e.Data);};
            try { process.Start(); LastSucceeded=false; process.BeginOutputReadLine(); process.BeginErrorReadLine(); completed=onComplete; Status="Processing model"; EditorApplication.LockReloadAssemblies(); }
            catch { process.Dispose(); process=null; throw; }
        }
        private static void Poll()
        {
            while(lines.TryDequeue(out string line)) { Log+=line+"\n"; if(Log.Length>24000) Log=Log.Substring(Log.Length-24000); }
            if(process==null || !process.HasExited) return;
            int code=process.ExitCode; process.Dispose(); process=null; EditorApplication.UnlockReloadAssemblies();
            var done=completed; completed=null;
            try { if(code!=0) throw new InvalidOperationException("Model tool exit "+code+". See factory log."); done?.Invoke(); LastSucceeded=true; Status="Model operation complete"; }
            catch(Exception ex) { Status=ex.Message; Debug.LogWarning(Status); }
        }
        private static void Quit() { if(process==null) return; if(!process.HasExited) process.Kill(); process.Dispose(); process=null; EditorApplication.UnlockReloadAssemblies(); }
    }
}

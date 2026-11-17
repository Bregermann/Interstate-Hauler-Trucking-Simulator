using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LWS.TruckSports.Editor
{
    // Entry adapter for the existing saved-scene capture probe. It does not
    // substitute a short opening for a completed match or Windows acceptance.
    [InitializeOnLoad]
    public static class SportsHitchesCaptureJobs
    {
        const string Active="Sports.HH10.Openings.Active", Entered="Sports.HH10.Openings.Entered", Deadline="Sports.HH10.Openings.Deadline";
        static SportsHitchesCaptureJobs() { EditorApplication.update += Tick; }
        public static void CaptureOpenings()
        {
            foreach(var entry in SportsHitchesGames.All) if(!File.Exists(entry.ScenePath)) throw new FileNotFoundException("Saved new venue required",entry.ScenePath);
            var args=Environment.GetCommandLineArgs();
            string batch=Arg(args,"-hitchesCaptureBatch","Alpha10Openings02");
            string keys=Arg(args,"-hitchesCaptureScenes","");
            var selected=SportsHitchesGames.All.Where(e=>string.IsNullOrEmpty(keys)||keys.Split(',').Contains(e.scene)).ToArray();
            if(selected.Length==0)throw new InvalidOperationException("No requested expansion scenes matched.");
            SessionState.SetString("Sports.HH10.Openings.Batch",batch); SessionState.SetInt("Sports.HH10.Openings.Count",selected.Length);
            EditorSceneManager.OpenScene(SportsBuilder.FrontScene);
            SessionState.SetString("Sports.MenuCapture.Batch",batch);
            SessionState.SetString("Sports.MenuCapture.Paths",JsonUtility.ToJson(new SportsMenuCaptureProbe.Manifest { paths=selected.Select(e=>e.ScenePath).ToArray() }));
            SessionState.SetBool("Sports.MenuCapture.Run",true);SessionState.SetBool(Active,true);SessionState.SetBool(Entered,false);
            SessionState.SetFloat(Deadline,(float)EditorApplication.timeSinceStartup+600);
            EditorApplication.isPlaying=true;
        }
        static string Arg(string[] args,string key,string fallback){int i=Array.IndexOf(args,key);return i>=0&&i+1<args.Length?args[i+1]:fallback;}
        static void Tick()
        {
            if(!SessionState.GetBool(Active,false)) return;
            if(EditorApplication.isPlaying)SessionState.SetBool(Entered,true);
            if(EditorApplication.timeSinceStartup>SessionState.GetFloat(Deadline,float.MaxValue))
            { SessionState.SetBool(Active,false);Debug.LogError("HH10 opening capture timed out; retain existing partial receipt.");EditorApplication.Exit(1);return; }
            if(!SessionState.GetBool(Entered,false)||EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating)return;
            SessionState.SetBool(Active,false);
            string path="Logs/TruckSports/EssentialRelease/"+SessionState.GetString("Sports.HH10.Openings.Batch", "Alpha10Openings02")+"/receipt.json";
            var receipt=File.Exists(path)?JsonUtility.FromJson<SportsMenuCaptureProbe.Receipt>(File.ReadAllText(path)):null;
            bool passed=receipt!=null&&receipt.scenes.Length==SessionState.GetInt("Sports.HH10.Openings.Count",22)&&receipt.errors==0&&receipt.scenes.All(e=>e.result=="OPENING CAPTURED");
            Debug.Log("HH10_OPENING_CAPTURE="+(passed?"PASS":"FAIL")+"; short actual AI openings and controlled Editor images only.");EditorApplication.Exit(passed?0:1);
        }
    }
}

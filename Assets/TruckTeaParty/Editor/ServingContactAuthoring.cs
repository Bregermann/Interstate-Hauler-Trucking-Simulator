using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using LWS.TruckBakeOff;
using LWS.TruckDentist;
using LWS.TruckSports;
namespace LWS.TruckTeaParty.Editor
{
    // Only repairs a proven missing ORIGINAL wrist listener in the saved Bake scene.
    // The native callback/Grab implementation and every physical component stay original.
    public static class ServingContactAuthoring
    {
        const string ScenePath="Assets/TruckBakeOff/Scenes/TruckBakeOff.unity";
        const string ScriptPath="Assets/TruckTeaParty/Scripts/ServingContact.cs";
        sealed class Record {public int type;public string text;}
        static Dictionary<ulong,Record> Records(string text)
        {
            var result=new Dictionary<ulong,Record>();
            foreach(Match m in Regex.Matches(text,@"(?ms)^--- !u!(\d+) &(\d+)(?: stripped)?\r?\n(.*?)(?=^--- |\z)"))
                result.Add(ulong.Parse(m.Groups[2].Value),new Record{type=int.Parse(m.Groups[1].Value),text=m.Groups[3].Value});
            return result;
        }
        static bool Field(string text,string field,string value)=>Regex.IsMatch(text,@"(?m)^  "+Regex.Escape(field)+@": "+Regex.Escape(value)+@"\r?$");
        static ulong Id(UnityEngine.Object value)=>GlobalObjectId.GetGlobalObjectIdSlow(value).targetObjectId;
        static void Require(bool condition,string reason){if(!condition)throw new InvalidOperationException(reason);}
        public static void RepairSavedBakeListeners()=>RepairSaved(ScenePath);
        // Optional explicit shared-consumer repair ONLY if native loading proves
        // the same single missing original listener; valid bindings are no-ops.
        public static void RepairSavedTeaListenersIfMissing()=>RepairSaved("Assets/TruckTeaParty/Scenes/TruckTeaParty.unity");
        public static void RepairSavedDentistListenersIfMissing()=>RepairSaved("Assets/TruckDentist/Scenes/TruckDentist.unity");
        static void RepairSaved(string path)
        {
            Require(!EditorApplication.isPlayingOrWillChangePlaymode&&!EditorApplication.isCompiling,"Stopped compiled Editor required.");
            for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)Require(!UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty,"Preserve dirty scene before explicit wrist-listener repair.");
            var scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Single);
            int changed=RepairOpenOriginalListeners();
            if(changed>0)Require(EditorSceneManager.SaveScene(scene),"Could not save the explicitly repaired Bake listener bindings.");
        }
        public static int RepairOpenBakeListeners(){Require(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path==ScenePath,"Exact Bake scene required.");return RepairOpenOriginalListeners();}
        static int RepairOpenOriginalListeners()
        {
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            Require(!EditorApplication.isPlayingOrWillChangePlaymode&&!EditorApplication.isCompiling&&(scene.path==ScenePath||scene.path=="Assets/TruckTeaParty/Scenes/TruckTeaParty.unity"||scene.path=="Assets/TruckDentist/Scenes/TruckDentist.unity"),"Exact stopped known saved serving scene required.");
            var script=AssetDatabase.LoadAssetAtPath<MonoScript>(ScriptPath);
            Require(script!=null&&script.GetClass()==typeof(ServingContact)&&typeof(ServingContact).FullName=="LWS.TruckTeaParty.ServingContact"&&typeof(ServingContact).Assembly.GetName().Name=="Assembly-CSharp","The same original public listener must have its correctly named imported MonoScript.");
            var match=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<SportsEventSession>(true)).Single();
            Require(scene.path==ScenePath?match is BakeMatch:scene.path.Contains("TruckTeaParty")?match is TeaMatch:match is DentistMatch,"Exact corresponding original typed mode required.");
            var arms=match is BakeMatch bake?bake.arms:match is TeaMatch tea?tea.arms:((DentistMatch)match).arms;
            Require(arms!=null&&arms.Length==4,"Preserve custom original serving roster.");
            var records=Records(File.ReadAllText(scene.path));var missing=new List<ServingArm>();
            for(int i=0;i<4;i++)
            {
                var arm=arms[i];
                Require(arm!=null&&arm.truck!=null&&arm.truck.session==match&&arm.truck.slot==i&&arm.truck.tractorMotor!=null&&arm.grip!=null&&arm.grip.transform.IsChildOf(arm.gear),"Exact original full NWH/wrist/arm ownership required.");
                var sensor=arm.grip.GetComponent<SphereCollider>();
                Require(sensor!=null&&sensor.attachedRigidbody==arm.grip&&sensor.isTrigger&&Mathf.Abs(sensor.radius-1.2f)<.00001f,"Preserve custom native wrist sensor.");
                var existing=arm.grip.GetComponents<ServingContact>();int count=GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(arm.grip.gameObject);
                if(existing.Length==1){Require(existing[0].arm==arm&&count==0,"Preserve mixed/custom listener bindings.");continue;}
                Require(existing.Length==0&&count==1,"Only the single proven original missing listener may be replaced.");
                Require(records.TryGetValue(Id(arm.grip.gameObject),out var go)&&go.type==1,"Saved original wrist identity required.");
                var originals=new List<Record>();
                foreach(Match c in Regex.Matches(go.text,@"(?m)^  - component: \{fileID: (\d+)\}\r?$"))
                    if(records.TryGetValue(ulong.Parse(c.Groups[1].Value),out var record)&&record.type==114&&Field(record.text,"m_EditorClassIdentifier","Assembly-CSharp::LWS.TruckTeaParty.ServingContact"))originals.Add(record);
                Require(originals.Count==1,"Exactly one saved original ServingContact record required; unknown missing scripts are preserved.");
                var original=originals[0];Require(Field(original.text,"arm","{fileID: "+Id(arm)+"}"),"Preserve wrong-arm/custom listener payload.");
                var reference=Regex.Match(original.text,@"(?m)^  m_Script: \{fileID: (\d+)\}\r?$");
                Require(reference.Success&&records.TryGetValue(ulong.Parse(reference.Groups[1].Value),out var local)&&local.type==115&&Field(local.text,"m_ClassName","ServingContact")&&Field(local.text,"m_Namespace","LWS.TruckTeaParty")&&Field(local.text,"m_AssemblyName","Assembly-CSharp"),"Only the proven local MonoScript identity may be rebound; no arbitrary missing component removal.");
                missing.Add(arm);
            }
            // All four scene/source/type/owner guards precede the first component edit.
            foreach(var arm in missing)
            {
                Require(GameObjectUtility.RemoveMonoBehavioursWithMissingScript(arm.grip.gameObject)==1,"Listener preflight changed before repair.");
                var listener=arm.grip.gameObject.AddComponent<ServingContact>();listener.arm=arm;
                Require(MonoScript.FromMonoBehaviour(listener)==script,"Repaired listener must bind the real external MonoScript.");
                EditorUtility.SetDirty(listener);
            }
            if(missing.Count>0)EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("ORIGINAL_WRIST_LISTENERS_REBOUND scene="+scene.path+" changed="+missing.Count+"; same type/assembly/API/native callback semantics, only proven missing listener components. Existing valid bindings and all physical fields preserved.");
            return missing.Count;
        }
    }
}

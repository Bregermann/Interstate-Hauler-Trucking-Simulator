#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using LWS.TruckSports;
using LWS.TruckSports.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LWS.TruckDragonKingdom.Editor
{
    // Explicit additive opt-in to the collection's real Heat/DOTween board.
    // Normal play/builds do not regenerate or replace designer-authored UI.
    public static class DragonFinalResultsAuthoring
    {
        static readonly string[] Modes={"TruckHoardHeist","TruckDragonEggDelivery","TruckDragonEscape","TruckDragonRiders"};
        [Serializable]public sealed class SceneFact
        {
            public string scene,guid,before,after;
            public int originalDocuments,addedDocuments,restoredAutosizeCaches,restoredSkinBoundsCaches;
            public bool originalDocumentsPreserved,metadataPreserved,changed;
        }
        [Serializable]public sealed class Receipt
        {
            public string scope="EXPLICIT_FOUR_OWNED_SCENE_READ_ONLY_RESULTS_BOARDS_NOT_RUNTIME_MATCH_ACCEPTANCE";
            public SceneFact[] scenes;
        }
        sealed class Before
        {
            public string path,guid,hash;public byte[] meta;public Dictionary<long,string> documents;
            public long pageRect;public long[] children;
        }
        public static void UpgradeSavedModes()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)
                throw new InvalidOperationException("Run the explicit results upgrade in a stopped, compiled Editor.");
            for(int i=0;i<SceneManager.sceneCount;i++)
            {
                var scene=SceneManager.GetSceneAt(i);
                if(scene.isDirty||string.IsNullOrEmpty(scene.path))throw new InvalidOperationException("Save all existing scenes before this explicit upgrade.");
            }
            var setup=EditorSceneManager.GetSceneManagerSetup();var before=new List<Before>();
            try
            {
                // Validate every target before modifying any of them.
                foreach(string mode in Modes)
                {
                    string path="Assets/"+mode+"/Scenes/"+mode+".unity";
                    if(!File.Exists(path))throw new FileNotFoundException("The actual saved mode is required; this upgrade never creates a replacement scene.",path);
                    var scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Single);var session=Session(scene);
                    var page=session.menus.pages[(int)SportsEventScreen.Result];
                    var boards=page.GetComponentsInChildren<SportsResultsPresentation>(true);
                    if(boards.Length>1||boards.Any(board=>board.source==null||board.source.GetType()!=typeof(DragonFinalResultsSource)||board.source.session!=session))
                        throw new InvalidOperationException("Preserve the existing custom results board and review its source explicitly: "+path);
                    var rect=page.GetComponent<RectTransform>();
                    before.Add(new Before{path=path,guid=AssetDatabase.AssetPathToGUID(path),hash=Hash(path),meta=File.ReadAllBytes(path+".meta"),documents=Documents(path),pageRect=Id(rect),children=rect.Cast<Transform>().Select(Id).ToArray()});
                }
                SportsResultsPresentationAuthoring.InstallOwnedSceneResults(Modes,typeof(DragonFinalResultsSource));
                var facts=new List<SceneFact>();
                foreach(var original in before)
                {
                    int caches=RestoreUnrelatedDerivedCaches(original,out int skinCaches);
                    var after=Documents(original.path);
                    foreach(var entry in original.documents)
                    {
                        if(!after.TryGetValue(entry.Key,out var actual))throw new InvalidOperationException("Original scene identity removed: "+original.path+" #"+entry.Key);
                        string expected=entry.Value;
                        if(entry.Key==original.pageRect){expected=WithoutChildren(expected);actual=WithoutChildren(actual);}
                        // In older serialization versions, inserting the backing
                        // before existing direct children updates sibling order.
                        if(original.children.Contains(entry.Key)){expected=WithoutSiblingOrder(expected);actual=WithoutSiblingOrder(actual);}
                        if(expected!=actual)throw new InvalidOperationException("Unrelated original scene document changed: "+original.path+" #"+entry.Key);
                    }
                    var scene=EditorSceneManager.OpenScene(original.path,OpenSceneMode.Single);var session=Session(scene);var page=session.menus.pages[(int)SportsEventScreen.Result];
                    var board=page.GetComponentsInChildren<SportsResultsPresentation>(true).Single();
                    if(board.source.GetType()!=typeof(DragonFinalResultsSource)||board.source.session!=session||board.rows.Length<4||board.visibility.interactable||board.visibility.blocksRaycasts||page.transform.Find("Results readability backing")==null)
                        throw new InvalidOperationException("Complete noninteractive saved board and correct original match binding required: "+original.path);
                    if(!page.transform.Cast<Transform>().Select(Id).Where(original.children.Contains).SequenceEqual(original.children))
                        throw new InvalidOperationException("Original result controls were reordered: "+original.path);
                    if(AssetDatabase.AssetPathToGUID(original.path)!=original.guid||!File.ReadAllBytes(original.path+".meta").SequenceEqual(original.meta))
                        throw new InvalidOperationException("Original scene GUID/metadata changed: "+original.path);
                    string hash=Hash(original.path);
                    facts.Add(new SceneFact{scene=original.path,guid=original.guid,before=original.hash,after=hash,changed=hash!=original.hash,originalDocuments=original.documents.Count,addedDocuments=after.Count-original.documents.Count,restoredAutosizeCaches=caches,restoredSkinBoundsCaches=skinCaches,originalDocumentsPreserved=true,metadataPreserved=true});
                }
                string[] args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonOutput");
                if(at>=0&&at+1<args.Length)
                {
                    string folder=Path.Combine(args[at+1],"ResultBoards");Directory.CreateDirectory(folder);string path=Path.Combine(folder,"saved-upgrade.json");
                    if(facts.Any(fact=>fact.changed)||!File.Exists(path))File.WriteAllText(path,JsonUtility.ToJson(new Receipt{scenes=facts.ToArray()},true));
                }
                Debug.Log("DRAGON_RESULTS_BOARDS_READY scenes=4 originalDocumentsExact=true originalControlsPreserved=true; runtime acceptance remains separate.");
            }
            finally{if(setup.Length>0)EditorSceneManager.RestoreSceneManagerSetup(setup);}
        }
        static SportsEventSession Session(Scene scene)=>scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<SportsEventSession>(true)).Single();
        static long Id(UnityEngine.Object value)=>unchecked((long)GlobalObjectId.GetGlobalObjectIdSlow(value).targetObjectId);
        static string Hash(string path){using(var sha=SHA256.Create())return string.Concat(sha.ComputeHash(File.ReadAllBytes(path)).Select(value=>value.ToString("x2")));}
        static string WithoutChildren(string text)=>Regex.Replace(text,@"(?m)^  m_Children:(?: \[\])?\n(?:  - \{fileID: -?\d+\}\n)*","  m_Children: <owned UI extension>\n");
        static string WithoutSiblingOrder(string text)=>Regex.Replace(text,@"(?m)^  m_RootOrder: \d+\n","  m_RootOrder: <same original child order>\n");
        static string WithoutAutosizeCache(string text)=>Regex.Replace(text,@"(?m)^  m_fontSize: [^\n]+\n","  m_fontSize: <derived autosize cache>\n");
        static string WithoutDirtySkinBounds(string text)=>Regex.Replace(text,@"(?m)^  m_AABB:\n    m_Center: \{[^\n]+\}\n    m_Extent: \{[^\n]+\}\n  m_DirtyAABB: [01](?=\n|$)","  m_AABB: <derived dirty skin bounds>");
        static int RestoreUnrelatedDerivedCaches(Before original,out int skinCaches)
        {
            // Adding a Canvas child can make TMP resolve an unrelated hidden
            // setup label's cached size during Editor SaveScene (V168:20->26).
            // Restore only that derived field when ALL authoritative font and
            // component data are exact; never forgive or erase another change.
            var after=Documents(original.path);string text=null;int restored=0;skinCaches=0;
            foreach(var entry in original.documents)
            {
                if(!after.TryGetValue(entry.Key,out var actual)||actual==entry.Value)continue;
                bool font=entry.Value.Contains("TMPro.TextMeshProUGUI")&&entry.Value.Contains("  m_enableAutoSizing: 1\n")&&WithoutAutosizeCache(actual)==WithoutAutosizeCache(entry.Value);
                // Unity also resolves a previously dirty imported baby-mesh
                // bound on opening/saving (V169). Restore only that cache when
                // every bone, mesh, material and renderer setting is identical.
                bool skin=entry.Value.Contains("\nSkinnedMeshRenderer:\n")&&entry.Value.Contains("  m_DirtyAABB: 1")&&WithoutDirtySkinBounds(actual)==WithoutDirtySkinBounds(entry.Value);
                if(!font&&!skin)continue;
                if(text==null)text=File.ReadAllText(original.path).Replace("\r\n","\n");
                if(!text.Contains(actual))throw new InvalidOperationException("Missing exact derived-cache document: "+original.path);
                text=text.Replace(actual,entry.Value);if(font)restored++;else skinCaches++;
            }
            if(restored+skinCaches>0)File.WriteAllText(original.path,text,new System.Text.UTF8Encoding(false));
            return restored;
        }
        static Dictionary<long,string> Documents(string path)
        {
            string text=File.ReadAllText(path).Replace("\r\n","\n");var headers=Regex.Matches(text,@"(?m)^--- !u!\d+ &(-?\d+)(?: stripped)?$");var result=new Dictionary<long,string>();
            for(int i=0;i<headers.Count;i++){var h=headers[i];int end=i+1<headers.Count?headers[i+1].Index:text.Length;result.Add(long.Parse(h.Groups[1].Value),text.Substring(h.Index,end-h.Index));}
            if(result.Count==0)throw new InvalidOperationException("Actual serialized Unity scene documents required: "+path);
            return result;
        }
    }
}
#endif

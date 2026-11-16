#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class DragonMayhemAuthoringTests
{
    static Type Runtime(string name)=>Type.GetType(name+", Assembly-CSharp",true);
    static object Read(object value,string name)=>value.GetType().GetField(name)?.GetValue(value)??value.GetType().GetProperty(name).GetValue(value);
    [Test]public void ExplicitActualGroundGrabContactCalibrationPreservesDropSceneSourcesGuidsAndRepeatBytes()
    {
        Assert.That(EditorApplication.isPlayingOrWillChangePlaymode,Is.False);
        const string root="Assets/TruckDragonKingdom/Mayhem/Configuration",actorPath="Assets/TruckDragonKingdom/Prefabs/Dragons/dragon-inferno/dragon-inferno Dragon Actor.prefab";
        var configurations=AssetDatabase.FindAssets("t:DragonMayhemConfiguration",new[]{root}).Select(AssetDatabase.GUIDToAssetPath).ToArray();Assert.That(configurations.Length,Is.EqualTo(4));
        var changedFields=new HashSet<string>(new[]{"groundGrabSpeciesId","groundGrabContactClip","groundGrabContactNormalizedTime","groundGrabGripLocalOffset"});
        var originalValues=configurations.ToDictionary(path=>path,path=>{var asset=AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);return asset.GetType().GetFields().Where(field=>!changedFields.Contains(field.Name)).ToDictionary(field=>field.Name,field=>field.GetValue(asset));});
        var guids=configurations.ToDictionary(path=>path,AssetDatabase.AssetPathToGUID);
        var watched=Directory.GetFiles("Assets/TruckDragonKingdom/Prefabs/Dragons","*",SearchOption.AllDirectories).Concat(Directory.GetFiles("Assets/TruckDragonKingdom/Animations/Authored","*",SearchOption.AllDirectories)).Concat(new[]{"Assets/TruckDragonKingdom/Settings/DragonSpeciesCatalog.asset","Assets/TruckKeepaway/Scenes/TruckKeepaway.unity","Assets/TruckBalloonBattle/Scenes/TruckBalloonBattle.unity","Assets/TruckCornhole/Scenes/TruckCornhole.unity","Assets/TruckCheeseRolling/Scenes/TruckCheeseRolling.unity"}).ToDictionary(path=>path,Digest);
        var author=Type.GetType("LWS.TruckDragonKingdom.Mayhem.Editor.DragonMayhemAuthoring, Assembly-CSharp-Editor",true);var upgrade=author.GetMethod("UpgradeActualGroundGrabContactGeometry");Assert.That(upgrade.Invoke(null,null),Is.InRange(0,4));
        var contents=PrefabUtility.LoadPrefabContents(actorPath);
        try
        {
            var actor=contents.GetComponents<Component>().Single(value=>value!=null&&value.GetType().Name=="DragonCharacter");var motor=(Component)Read(actor,"motor");
            var binding=((Array)Read(motor,"actionBindings")).Cast<object>().Single(value=>Read(value,"action").ToString()=="GroundGrab");float normalized=(float)Read(binding,"contactNormalizedTime");
            var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/TruckDragonKingdom/Animations/Authored/dragon-inferno/GroundGrab.anim");Assert.That(clip,Is.Not.Null,"Explicit accepted action migration must run before configuration calibration.");
            ((Animator)Read(motor,"animator")).enabled=false;clip.SampleAnimation(motor.gameObject,clip.length*normalized);
            var grip=(Transform)Read(actor,"leftGrip");Vector3 measured=motor.transform.InverseTransformPoint(grip.position);
            Assert.That(measured.y*motor.transform.lossyScale.y,Is.InRange(.2f,1.5f),"Configuration uses actual ground-reaching contact pose, not the airborne Grab or idle palm.");
            foreach(var path in configurations)
            {
                var asset=AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);var type=asset.GetType();Assert.That(AssetDatabase.AssetPathToGUID(path),Is.EqualTo(guids[path]));
                foreach(var pair in originalValues[path])Assert.That(type.GetField(pair.Key).GetValue(asset),Is.EqualTo(pair.Value),"All Drop/custom fields remain unchanged: "+path+"/"+pair.Key);
                Assert.That(Read(asset,"groundGrabSpeciesId"),Is.EqualTo("dragon-inferno"));Assert.That(Read(asset,"groundGrabContactClip"),Is.EqualTo(clip));Assert.That(Read(asset,"groundGrabContactNormalizedTime"),Is.EqualTo(normalized));Assert.That(Vector3.Distance((Vector3)Read(asset,"groundGrabGripLocalOffset"),measured),Is.LessThan(.000001f));
            }
        }
        finally{PrefabUtility.UnloadPrefabContents(contents);}
        foreach(var pair in watched)Assert.That(Digest(pair.Key),Is.EqualTo(pair.Value),"Contact configuration cannot rewrite actual actors, source clips/catalog or original scenes: "+pair.Key);
        var repaired=Directory.GetFiles(root,"*",SearchOption.AllDirectories).ToDictionary(path=>path,Digest);Assert.That(upgrade.Invoke(null,null),Is.EqualTo(0));foreach(var pair in repaired)Assert.That(Digest(pair.Key),Is.EqualTo(pair.Value),"Repeated selected GroundGrab contact calibration is byte-idempotent: "+pair.Key);
    }
    [Test]public void ExplicitActualDropContactCalibrationPreservesOriginalAssetsGuidsCustomValuesAndRepeatBytes()
    {
        Assert.That(EditorApplication.isPlayingOrWillChangePlaymode,Is.False);
        const string root="Assets/TruckDragonKingdom/Mayhem/Configuration";
        var configurations=AssetDatabase.FindAssets("t:DragonMayhemConfiguration",new[]{root}).Select(AssetDatabase.GUIDToAssetPath).ToArray();Assert.That(configurations.Length,Is.EqualTo(4));
        var changedFields=new HashSet<string>(new[]{"dropSpeciesId","dropContactClip","dropContactNormalizedTime","dropMouthLocalOffset"});
        var originalValues=configurations.ToDictionary(path=>path,path=>{var asset=AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);return asset.GetType().GetFields().Where(field=>!changedFields.Contains(field.Name)).ToDictionary(field=>field.Name,field=>field.GetValue(asset));});
        var guids=configurations.ToDictionary(path=>path,AssetDatabase.AssetPathToGUID);
        var watched=Directory.GetFiles("Assets/TruckDragonKingdom/Prefabs/Dragons","*",SearchOption.AllDirectories).Concat(Directory.GetFiles("Assets/TruckDragonKingdom/Animations/Authored","*",SearchOption.AllDirectories)).Concat(new[]{"Assets/TruckDragonKingdom/Settings/DragonSpeciesCatalog.asset","Assets/TruckKeepaway/Scenes/TruckKeepaway.unity","Assets/TruckBalloonBattle/Scenes/TruckBalloonBattle.unity","Assets/TruckCornhole/Scenes/TruckCornhole.unity","Assets/TruckCheeseRolling/Scenes/TruckCheeseRolling.unity"}).ToDictionary(path=>path,Digest);
        var author=Type.GetType("LWS.TruckDragonKingdom.Mayhem.Editor.DragonMayhemAuthoring, Assembly-CSharp-Editor",true);var upgrade=author.GetMethod("UpgradeActualDropContactGeometry");Assert.That(upgrade.Invoke(null,null),Is.InRange(0,4));
        foreach(var path in configurations)
        {
            var asset=AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);var type=asset.GetType();Assert.That(AssetDatabase.AssetPathToGUID(path),Is.EqualTo(guids[path]));
            foreach(var pair in originalValues[path])Assert.That(type.GetField(pair.Key).GetValue(asset),Is.EqualTo(pair.Value),"Only measured Drop geometry fields may change: "+path+"/"+pair.Key);
            Assert.That((string)type.GetField("dropSpeciesId").GetValue(asset),Is.Not.Empty);var clip=(AnimationClip)type.GetField("dropContactClip").GetValue(asset);Assert.That(clip,Is.Not.Null);Assert.That(AssetDatabase.GetAssetPath(clip),Does.Contain("/Animations/"));
            Assert.That((float)type.GetField("dropContactNormalizedTime").GetValue(asset),Is.InRange(.1f,.9f));var offset=(Vector3)type.GetField("dropMouthLocalOffset").GetValue(asset);Assert.That(float.IsFinite(offset.x)&&float.IsFinite(offset.y)&&float.IsFinite(offset.z)&&offset.sqrMagnitude>.01f,Is.True);
        }
        foreach(var pair in watched)Assert.That(Digest(pair.Key),Is.EqualTo(pair.Value),"Actual source/configured actors/clips/catalog/original scenes remain byte-identical: "+pair.Key);
        var repaired=Directory.GetFiles(root,"*",SearchOption.AllDirectories).ToDictionary(path=>path,Digest);Assert.That(upgrade.Invoke(null,null),Is.EqualTo(0));foreach(var pair in repaired)Assert.That(Digest(pair.Key),Is.EqualTo(pair.Value),"Repeated selected calibration must not rewrite assets: "+pair.Key);
    }
    [Test]public void ExplicitResultTallyUpgradeChangesOnlyItsTwoUiDocumentsAndPreservesCustomLayoutsAndRepeatBytes()
    {
        Assert.That(EditorApplication.isPlayingOrWillChangePlaymode,Is.False);
        for(int index=0;index<SceneManager.sceneCount;index++)Assert.That(SceneManager.GetSceneAt(index).isDirty,Is.False,"Preserve unsaved editor work.");
        var author=Type.GetType("LWS.TruckDragonKingdom.Mayhem.Editor.DragonMayhemAuthoring, Assembly-CSharp-Editor",true);
        var upgrade=author.GetMethod("UpgradeResultTallyLayout");
        foreach(string mode in new[]{"TruckKeepaway","TruckBalloonBattle","TruckCornhole","TruckCheeseRolling"})
        {
            string path="Assets/"+mode+"/Scenes/"+mode+".unity";byte[] before=File.ReadAllBytes(path);string meta=Digest(path+".meta");bool accepted=false;
            var scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            try
            {
                var director=scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren(Runtime("LWS.TruckDragonKingdom.Mayhem.DragonMayhemDirector"),true)).Single();
                var view=director.GetComponent(Runtime("LWS.TruckDragonKingdom.Mayhem.DragonMayhemPresentation"));
                var rect=(RectTransform)((GameObject)Read(view,"resultPanel")).transform;var text=(Component)Read(view,"result");
                string rectId=GlobalObjectId.GetGlobalObjectIdSlow(rect).targetObjectId.ToString(),textId=GlobalObjectId.GetGlobalObjectIdSlow(text).targetObjectId.ToString();
                bool changed=(bool)upgrade.Invoke(null,new object[]{director});
                if(changed)author.GetMethod("PersistOnlyResultTallyFields").Invoke(null,new object[]{director});
                Assert.That(rect.anchoredPosition,Is.EqualTo(new Vector2(0,-165)));Assert.That((float)Read(text,"fontSize"),Is.EqualTo(20));
                var oldDocuments=ResultDocuments(System.Text.Encoding.UTF8.GetString(before));var newDocuments=ResultDocuments(File.ReadAllText(path));
                Assert.That(newDocuments.Keys.OrderBy(value=>value),Is.EqualTo(oldDocuments.Keys.OrderBy(value=>value)),"No added/removed scene identities.");
                foreach(var item in oldDocuments)
                {
                    string old=item.Value,current=newDocuments[item.Key];
                    if(item.Key==rectId){old=NormalizeResultField(old,"m_AnchoredPosition");current=NormalizeResultField(current,"m_AnchoredPosition");}
                    if(item.Key==textId)foreach(string field in new[]{"m_fontSize","m_fontSizeBase","m_fontSizeMin","m_fontSizeMax"}){old=NormalizeResultField(old,field);current=NormalizeResultField(current,field);}
                    Assert.That(current,Is.EqualTo(old),"Unrelated authored data must remain exact: "+mode+"/"+item.Key);
                }
                Assert.That(Digest(path+".meta"),Is.EqualTo(meta));byte[] saved=File.ReadAllBytes(path);
                Assert.That(upgrade.Invoke(null,new object[]{director}),Is.False);CollectionAssert.AreEqual(saved,File.ReadAllBytes(path),"Explicit repeat does not save/rewrite the scene.");
                var desired=rect.anchoredPosition;rect.anchoredPosition+=Vector2.up;
                try{var error=Assert.Throws<System.Reflection.TargetInvocationException>(()=>upgrade.Invoke(null,new object[]{director}));Assert.That(error.InnerException,Is.TypeOf<InvalidOperationException>());Assert.That(rect.anchoredPosition,Is.EqualTo(desired+Vector2.up),"A custom designer position is never overwritten.");}
                finally{rect.anchoredPosition=desired;}
                accepted=true;
            }
            finally
            {
                EditorSceneManager.CloseScene(scene,true);
                if(!accepted){File.WriteAllBytes(path,before);AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);}
            }
            var reloaded=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            try
            {
                var view=reloaded.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren(Runtime("LWS.TruckDragonKingdom.Mayhem.DragonMayhemPresentation"),true)).Single();
                Assert.That(((RectTransform)((GameObject)Read(view,"resultPanel")).transform).anchoredPosition,Is.EqualTo(new Vector2(0,-165)),"Actual saved/reloaded layout.");
                Assert.That((float)Read(Read(view,"result"),"fontSize"),Is.EqualTo(20));
            }
            finally{EditorSceneManager.CloseScene(reloaded,true);}
        }
    }
    static Dictionary<string,string> ResultDocuments(string value)=>System.Text.RegularExpressions.Regex.Matches(value.Replace("\r\n","\n"),@"(?ms)^--- !u!\d+ &(-?\d+)[^\n]*\n.*?(?=^--- !u!|\z)").Cast<System.Text.RegularExpressions.Match>().ToDictionary(match=>match.Groups[1].Value,match=>match.Value);
    static string NormalizeResultField(string document,string field)
    {
        string pattern=@"(?m)^  "+System.Text.RegularExpressions.Regex.Escape(field)+@": [^\n]*$";
        Assert.That(System.Text.RegularExpressions.Regex.Matches(document,pattern).Count,Is.EqualTo(1),"Exact one serialized UI field: "+field);
        return System.Text.RegularExpressions.Regex.Replace(document,pattern,"  "+field+": <explicit-result-layout>");
    }
    static string Digest(string path){using(var hash=System.Security.Cryptography.SHA256.Create())return string.Concat(hash.ComputeHash(File.ReadAllBytes(@"\\?\"+Path.GetFullPath(path))).Select(value=>value.ToString("x2")));}
    [TestCase("TruckKeepaway","LWS.TruckKeepaway.KeepawayMatch")]
    [TestCase("TruckBalloonBattle","LWS.TruckBalloonBattle.BalloonMatch")]
    [TestCase("TruckCornhole","LWS.TruckCornhole.CornholeMatch")]
    [TestCase("TruckCheeseRolling","LWS.TruckCheeseRolling.CheeseMatch")]
    public void AdditiveVariantUsesConfiguredOwnedActorsAndPreservesOriginalSerializedController(string folder,string type)
    {
        string path="Assets/"+folder+"/Scenes/"+folder+".unity";
        Assert.That(File.Exists(path),Is.True,"Original sport scene is required; no substitute scene is constructed.");
        for(int i=0;i<SceneManager.sceneCount;i++)
        {
            var scene=SceneManager.GetSceneAt(i);
            if(Application.isBatchMode&&string.IsNullOrEmpty(scene.path))
            {
                const string generated="Assets/TruckDragonKingdom/Mayhem/Tests/Editor/Generated";
                Directory.CreateDirectory(generated);AssetDatabase.Refresh();
                Assert.That(EditorSceneManager.SaveScene(scene,generated+"/RunnerScene-"+Guid.NewGuid().ToString("N")+".unity"),Is.True);
            }
            Assert.That(scene.isDirty,Is.False,"Preserve GUI edits before running explicit authoring acceptance.");
            Assert.That(!string.IsNullOrEmpty(scene.path)||scene.rootCount==0,Is.True);
        }
        var setup=EditorSceneManager.GetSceneManagerSetup();var bytes=File.ReadAllBytes(path);
        try
        {
            EditorSceneManager.OpenScene(path,OpenSceneMode.Single);
            var original=(Component)UnityEngine.Object.FindAnyObjectByType(Runtime(type));Assert.That(original,Is.Not.Null);
            var json=EditorJsonUtility.ToJson(original);
            var bodies=UnityEngine.Object.FindObjectsByType<Rigidbody>(FindObjectsInactive.Include).ToDictionary(b=>b,b=>b.mass);
            var collisions=UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsInactive.Include).ToDictionary(c=>c,c=>new[]{c.enabled,c.isTrigger});
            var directorType=Runtime("LWS.TruckDragonKingdom.Mayhem.DragonMayhemDirector");
            var director=original.GetComponentInChildren(directorType,true);
            if(director==null)
            {
                var author=Type.GetType("LWS.TruckDragonKingdom.Mayhem.Editor.DragonMayhemAuthoring, Assembly-CSharp-Editor",true);
                director=(Component)author.GetMethod("Install").Invoke(null,new object[]{original});
            }
            Assert.That(EditorJsonUtility.ToJson(original),Is.EqualTo(json),"Additive installation must not edit original controller rules, trucks, cameras, HUD, or weather references.");
            foreach(var pair in bodies)Assert.That(pair.Key.mass,Is.EqualTo(pair.Value));
            foreach(var pair in collisions){Assert.That(pair.Key.enabled,Is.EqualTo(pair.Value[0]));Assert.That(pair.Key.isTrigger,Is.EqualTo(pair.Value[1]));}
            foreach(string field in new[]{"actorPrefab","goldPrefab","sheepPrefab","catalog"})Assert.That(directorType.GetField(field).GetValue(director),Is.Not.Null,field);
            Assert.That(UnityEngine.Object.FindObjectsByType(Runtime("LWS.TruckDragonKingdom.Mayhem.DragonMayhemGold"),FindObjectsInactive.Include),Is.Empty,"OFF scenes serialize prefab references, never gold actors.");
            Assert.That(UnityEngine.Object.FindObjectsByType(Runtime("LWS.TruckDragonKingdom.Mayhem.DragonMayhemSheep"),FindObjectsInactive.Include),Is.Empty);
            Assert.That(Directory.GetFiles("Assets/TruckDragonKingdom/Mayhem","*.unity",SearchOption.AllDirectories).Where(p=>!p.Replace('\\','/').Contains("/Tests/")),Is.Empty,"Mayhem is not a fifth sport scene.");
        }
        finally
        {
            EditorSceneManager.RestoreSceneManagerSetup(setup);
            CollectionAssert.AreEqual(bytes,File.ReadAllBytes(path),"The authoring fixture never saves over an original scene.");
        }
    }
}
#endif

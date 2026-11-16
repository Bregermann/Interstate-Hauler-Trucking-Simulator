#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class DragonKingdomNestAnchorMigrationTests
{
    const string Root="Assets/TruckDragonKingdom/";
    [UnityTest,Timeout(300000)]
    public IEnumerator FourSavedNestAnchorsUseRealGeometryWorldMetersAndRepeatByteExactly()
    {
        var migration=Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomNestAnchorMigration, Assembly-CSharp-Editor",true);
        string prefab=(string)migration.GetProperty("PrefabPath").GetValue(null),preview=(string)migration.GetProperty("PreviewPath").GetValue(null);
        Assert.That(File.Exists(prefab)&&File.Exists(preview),Is.True,"Use the existing accepted world.");
        string generated=Root+"Tests/World/Generated/";Directory.CreateDirectory(generated);AssetDatabase.Refresh();
        for(int i=0;i<SceneManager.sceneCount;i++)
        {
            var s=SceneManager.GetSceneAt(i);
            if(Application.isBatchMode&&string.IsNullOrEmpty(s.path))Assert.That(EditorSceneManager.SaveScene(s,generated+"NestRunner-"+Guid.NewGuid().ToString("N")+".unity"),Is.True);
            Assert.That(s.isDirty||string.IsNullOrEmpty(s.path),Is.False,"Preserve original user scenes before isolation.");
        }
        var prior=EditorSceneManager.GetSceneManagerSetup();var original=prior.Where(s=>s.path!=preview&&File.Exists(s.path)).ToDictionary(s=>s.path,s=>File.ReadAllBytes(s.path));
        var isolated=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        Assert.That(EditorSceneManager.SaveScene(isolated,generated+"NestIsolation-"+Guid.NewGuid().ToString("N")+".unity"),Is.True);
        try
        {
            migration.GetMethod("UpgradeAcceptedNestAnchors").Invoke(null,null);yield return null;
            var first=new[]{prefab,preview}.ToDictionary(p=>p,p=>File.ReadAllBytes(p));
            migration.GetMethod("UpgradeAcceptedNestAnchors").Invoke(null,null);yield return null;
            foreach(var file in first)Assert.That(File.ReadAllBytes(file.Key),Is.EqualTo(file.Value),"Repeat anchor migration changed saved bytes: "+file.Key);
            var scene=EditorSceneManager.OpenScene(preview,OpenSceneMode.Additive);
            try
            {
                var type=Type.GetType("LWS.TruckDragonKingdom.DragonKingdomWorld, Assembly-CSharp",true);
                var world=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren(type,true)).Single();
                object Field(object o,string n)=>o.GetType().GetField(n).GetValue(o);
                var region=((Array)Field(world,"regions")).Cast<object>().Single(r=>(string)Field(r,"id")=="nests");
                var anchors=(Transform[])Field(region,"nests");Assert.That(anchors.Length,Is.EqualTo(4));
                foreach(var anchor in anchors)
                {
                    var target=(Vector3)migration.GetMethod("MeasuredNestSeat").Invoke(null,new object[]{anchor.parent});
                    Assert.That(Vector3.Distance(anchor.position,target),Is.LessThan(.002f),"Anchor inherits scaled local meters instead of actual owned geometry.");
                    var renderers=anchor.parent.GetComponentsInChildren<Renderer>(true);var bounds=renderers[0].bounds;foreach(var r in renderers.Skip(1))bounds.Encapsulate(r.bounds);
                    Assert.That(anchor.position.y,Is.InRange(bounds.min.y-.1f,bounds.max.y+.2f));
                }
                var author=Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomWorldAuthoring, Assembly-CSharp-Editor",true);
                Physics.SyncTransforms();author.GetMethod("ValidatePhysicalRoads").Invoke(null,new[]{(object)world.transform,Field(Field(world,"definition"),"graph")});
                author.GetMethod("ValidateTruckStarts").Invoke(null,new[]{(object)world,false});
            }
            finally{EditorSceneManager.CloseScene(scene,true);}
            foreach(var file in original)Assert.That(File.ReadAllBytes(file.Key),Is.EqualTo(file.Value));
        }
        finally{EditorSceneManager.RestoreSceneManagerSetup(prior);}
    }
}
#endif

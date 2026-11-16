#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class DragonKingdomWorldVisualMigrationTests
{
    const string Root="Assets/TruckDragonKingdom/";
    const string Preview=Root+"Scenes/DragonKingdomPreview.unity";
    const string Prefab=Root+"Prefabs/Shared Dragon Kingdom.prefab";
    static Type Migration=>Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomWorldVisualMigration, Assembly-CSharp-Editor",true);
    static object Field(object o,string name)=>o.GetType().GetField(name).GetValue(o);
    static void Upgrade()=>Migration.GetMethod("UpgradeAcceptedTerrainAndFoliage").Invoke(null,null);

    [UnityTest,Timeout(1200000)]
    public IEnumerator AcceptedWorldAppearancePreservesPhysicsAndHeightsAndRepeatsByteExactly()
    {
        Assert.That(File.Exists(Prefab)&&File.Exists(Preview),Is.True,"Use the accepted physical world; never generate one in this fixture.");
        for(int i=0;i<SceneManager.sceneCount;i++)
        {
            var current=SceneManager.GetSceneAt(i);
            if(Application.isBatchMode&&string.IsNullOrEmpty(current.path))
            {
                Directory.CreateDirectory(Root+"Tests/World/Generated");AssetDatabase.Refresh();
                Assert.That(EditorSceneManager.SaveScene(current,Root+"Tests/World/Generated/AppearanceRunner-"+Guid.NewGuid().ToString("N")+".unity"),Is.True);
            }
            Assert.That(current.isDirty,Is.False,"Preserve original unsaved user edits: "+current.name);
            Assert.That(current.path,Is.Not.Empty,"Save the original scene before isolated appearance validation.");
        }
        var prior=EditorSceneManager.GetSceneManagerSetup();
        var originals=prior.Where(p=>File.Exists(p.path)).ToDictionary(p=>p.path,p=>File.ReadAllBytes(p.path));
        var isolated=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        Directory.CreateDirectory(Root+"Tests/World/Generated");AssetDatabase.Refresh();
        Assert.That(EditorSceneManager.SaveScene(isolated,Root+"Tests/World/Generated/AppearanceIsolation-"+Guid.NewGuid().ToString("N")+".unity"),Is.True);
        try
        {
            Upgrade();yield return null;
            var receipt=Migration.GetProperty("LastReceipt").GetValue(null);
            Assert.That((bool)Field(receipt,"exactHeightSamplesPreserved"),Is.True);
            Assert.That((bool)Field(receipt,"geometryFilesPreserved"),Is.True);
            Assert.That((bool)Field(receipt,"physicsAndAnchorsPreserved"),Is.True);
            var terrains=(Array)Field(receipt,"terrains");Assert.That(terrains.Length,Is.EqualTo(4));
            foreach(var terrain in terrains)
            {
                Assert.That((int)Field(terrain,"zeroSumCells"),Is.Zero);
                Assert.That((int)Field(terrain,"nonFinite"),Is.Zero);
                Assert.That((float)Field(terrain,"maximumSumError"),Is.LessThanOrEqualTo(2f/255+.00001f));
                Assert.That(Field(terrain,"heightHashAfter"),Is.EqualTo(Field(terrain,"heightHashBefore")));
                var data=AssetDatabase.LoadAssetAtPath<TerrainData>((string)Field(terrain,"path"));
                Assert.That(data.terrainLayers.Length,Is.EqualTo(4));
                Assert.That(data.terrainLayers.All(l=>l!=null&&l.diffuseTexture!=null&&AssetDatabase.GetAssetPath(l).StartsWith(Root+"Materials/TerrainAppearance/",StringComparison.Ordinal)),Is.True);
            }
            var bark=AssetDatabase.LoadAssetAtPath<Material>(Root+"Materials/Environment/d768d77458b842644b856380df9a044f URP.mat");
            Assert.That(bark,Is.Not.Null);Assert.That(bark.GetTexture("_BaseMap"),Is.Not.Null,"Real Tidal _BaseTexture must reach the owned URP bark material.");
            var leaves=AssetDatabase.LoadAssetAtPath<Material>(Root+"Materials/Environment/a35168ec5f8b41447ba5916c93191534 URP.mat");
            Assert.That(leaves,Is.Not.Null);Assert.That(leaves.IsKeywordEnabled("_ALPHATEST_ON"),Is.True);
            Assert.That(leaves.HasProperty("_BaseTexture")?leaves.GetTexture("_BaseTexture"):leaves.GetTexture("_BaseMap"),Is.Not.Null);
            var nestPlants=AssetDatabase.LoadAssetAtPath<Material>(Root+"Materials/Environment/13c5ffec06712084e9da2f4239ceebdb URP.mat");
            Assert.That(nestPlants,Is.Not.Null);Assert.That(nestPlants.IsKeywordEnabled("_ALPHATEST_ON"),Is.True,"The nest uses the real cutout plant atlas too.");
            var paths=Directory.GetFiles(Root+"Materials","*",SearchOption.AllDirectories)
                .Concat(Directory.GetFiles(Root+"Terrain","*",SearchOption.AllDirectories))
                .Concat(Directory.GetFiles(Root+"Models","*",SearchOption.AllDirectories))
                .Concat(new[]{Prefab,Preview,Root+"Configuration/Dragon Kingdom.asset"}).ToArray();
            var frozen=paths.ToDictionary(p=>p,p=>File.ReadAllBytes(p));
            Upgrade();yield return null;
            foreach(var file in frozen)Assert.That(File.ReadAllBytes(file.Key),Is.EqualTo(file.Value),"Repeat appearance migration rewrote accepted bytes: "+file.Key);
            var scene=EditorSceneManager.OpenScene(Preview,OpenSceneMode.Additive);
            try
            {
                var type=Type.GetType("LWS.TruckDragonKingdom.DragonKingdomWorld, Assembly-CSharp",true);
                var world=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren(type,true)).Single();
                var definition=Field(world,"definition");var graph=Field(definition,"graph");
                var author=Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomWorldAuthoring, Assembly-CSharp-Editor",true);
                Physics.SyncTransforms();
                author.GetMethod("ValidatePhysicalRoads").Invoke(null,new[]{(object)world.transform,graph});
                author.GetMethod("ValidateTruckStarts").Invoke(null,new[]{world,(object)false});
                world.GetType().GetMethod("ValidateReferences").Invoke(world,null);
            }
            finally{EditorSceneManager.CloseScene(scene,true);}
            foreach(var file in originals.Where(p=>p.Key!=Preview))Assert.That(File.ReadAllBytes(file.Key),Is.EqualTo(file.Value),"An unrelated original scene changed: "+file.Key);
        }
        finally{EditorSceneManager.RestoreSceneManagerSetup(prior);}
    }
}
#endif

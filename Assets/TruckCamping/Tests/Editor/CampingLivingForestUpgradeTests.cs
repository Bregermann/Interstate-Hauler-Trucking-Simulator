using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public sealed class CampingLivingForestUpgradeTests
{
    static Type Author=>Type.GetType("LWS.TruckCamping.Editor.CampingLivingForestAuthoring, Assembly-CSharp-Editor",true);
    static string Digest(string path)=>(string)Author.GetMethod("DigestFile").Invoke(null,new object[]{path});
    static string Semantic(TerrainData data)=>(string)Author.GetMethod("PreservedTerrainDigest").Invoke(null,new object[]{data});
    [Serializable]sealed class Receipt{public string backup,source,owned;public Row[] files;}
    [Serializable]sealed class Row{public string path,guid,originalSha256,afterSha256,metaSha256,preservedDigest;public int trees;}
    [Serializable]sealed class NextReceipt{public string backup,previousOwned,owned;public NextRow[] files;}
    [Serializable]sealed class NextRow{public string path,beforeSha256,afterSha256,metaSha256,preservedDigest;public int trees;}
    [Test]public void ActualUpgradedFourTilesMatchOriginalBinaryBackupsForEveryInstanceHeightPaintHoleLayerAndGuid()
    {
        var receipt=JsonUtility.FromJson<Receipt>(File.ReadAllText("Logs/TruckSports/Impossible07/CampingLivingForestUpgrade/latest-upgrade-receipt.json"));
        const string nextPath="Logs/TruckSports/Impossible07/CampingSolidPineUpgrade/latest-upgrade-receipt.json";var next=File.Exists(nextPath)?JsonUtility.FromJson<NextReceipt>(File.ReadAllText(nextPath)):null;
        if(next!=null)Assert.That(next.previousOwned,Is.EqualTo(receipt.owned),"Explicit retained Whitebark->Vista->JC07 artifact chain; never discard the original stage proof.");Assert.That(receipt.files.Length,Is.EqualTo(4));Assert.That(receipt.files.Sum(f=>f.trees),Is.EqualTo(2297));
        string folder="LivingForestValidation"+Guid.NewGuid().ToString("N"),temp="Assets/TruckCamping/Tests/"+folder;AssetDatabase.CreateFolder("Assets/TruckCamping/Tests",folder);
        try
        {
            for(int i=0;i<4;i++)
            {
                var row=receipt.files[i];string currentOwned=receipt.owned,currentHash=row.afterSha256;
                if(next!=null){var nr=next.files.Single(f=>f.path==row.path);Assert.That(nr.beforeSha256,Is.EqualTo(row.afterSha256));Assert.That(Digest(Path.Combine(next.backup,i+"-vista.asset")),Is.EqualTo(row.afterSha256),"Retain exact intermediate Vista-stage binary while current prototype legitimately advances.");Assert.That(nr.preservedDigest,Is.EqualTo(row.preservedDigest));Assert.That(nr.metaSha256,Is.EqualTo(row.metaSha256));currentOwned=next.owned;currentHash=nr.afterSha256;}
                var original=Path.Combine(receipt.backup,i+"-original.asset");Assert.That(Digest(original),Is.EqualTo(row.originalSha256));
                var path=temp+"/original"+i+".asset";File.Copy(original,path);AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);var prior=AssetDatabase.LoadAssetAtPath<TerrainData>(path);var current=AssetDatabase.LoadAssetAtPath<TerrainData>(row.path);Assert.That(prior,Is.Not.Null);Assert.That(current,Is.Not.Null);
                Assert.That(Semantic(current),Is.EqualTo(Semantic(prior)),"Independently import exact original binary backup with fresh temporary GUID; compare all original public terrain/tree payloads.");
                CollectionAssert.AreEqual(prior.treeInstances,current.treeInstances);Assert.That(current.treeInstanceCount,Is.EqualTo(row.trees));Assert.That(current.treePrototypes.Length,Is.EqualTo(1));Assert.That(AssetDatabase.GetAssetPath(current.treePrototypes[0].prefab),Is.EqualTo(currentOwned));Assert.That(current.treePrototypes[0].bendFactor,Is.EqualTo(.2f));
                Assert.That(AssetDatabase.AssetPathToGUID(row.path),Is.EqualTo(row.guid));Assert.That(Digest(row.path+".meta"),Is.EqualTo(row.metaSha256));Assert.That(Digest(row.path),Is.EqualTo(currentHash));
            }
        }
        finally {AssetDatabase.DeleteAsset(temp);}
    }
    [Test]public void RepeatingTheExactOriginalUpgradeRefusesAlreadyUpgradedDataWithoutAnyPersistentMutation()
    {
        var paths=(string[])Author.GetField("TerrainPaths").GetValue(null);var before=paths.Select(Digest).ToArray();string prefab=(string)Author.GetField("LivingPrefab").GetRawConstantValue();string art=Digest(prefab),meta=Digest(prefab+".meta");
        var error=Assert.Throws<TargetInvocationException>(()=>Author.GetMethod("UpgradeExactReviewedWhitebarkPrototypes").Invoke(null,null));Assert.That(error.InnerException,Is.TypeOf<InvalidOperationException>());
        CollectionAssert.AreEqual(before,paths.Select(Digest).ToArray());Assert.That(Digest(prefab),Is.EqualTo(art));Assert.That(Digest(prefab+".meta"),Is.EqualTo(meta));
    }
    [Test]public void ActualOwnedLivingVariantKeepsVendorMeshesMaterialsAndOriginalHeightWithIdentityRoot()
    {
        string owned=(string)Author.GetField("LivingPrefab").GetRawConstantValue(),source=(string)Author.GetField("SourcePrefab").GetRawConstantValue(),old=(string)Author.GetField("OldPrefab").GetRawConstantValue();var asset=AssetDatabase.LoadAssetAtPath<GameObject>(owned);Assert.That(asset,Is.Not.Null);Assert.That(PrefabUtility.GetPrefabAssetType(asset),Is.EqualTo(PrefabAssetType.Variant));Assert.That(asset.transform.localScale,Is.EqualTo(Vector3.one));Assert.That(asset.transform.localPosition,Is.EqualTo(Vector3.zero));
        var sourceAsset=AssetDatabase.LoadAssetAtPath<GameObject>(source);CollectionAssert.AreEqual(sourceAsset.GetComponentsInChildren<Renderer>(true).SelectMany(r=>r.sharedMaterials),asset.GetComponentsInChildren<Renderer>(true).SelectMany(r=>r.sharedMaterials));CollectionAssert.AreEqual(sourceAsset.GetComponentsInChildren<MeshFilter>(true).Select(f=>f.sharedMesh),asset.GetComponentsInChildren<MeshFilter>(true).Select(f=>f.sharedMesh));Assert.That(asset.GetComponentsInChildren<Rigidbody>(true),Is.Empty);Assert.That(asset.GetComponentsInChildren<Collider>(true),Is.Empty);
        var scene=EditorSceneManager.NewPreviewScene();try{var a=(GameObject)PrefabUtility.InstantiatePrefab(asset,scene);var b=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(old),scene);Func<GameObject,Bounds> bounds=g=>{var rs=g.GetComponentsInChildren<Renderer>(true);var value=rs[0].bounds;foreach(var r in rs)value.Encapsulate(r.bounds);return value;};Assert.That(bounds(a).size.y,Is.EqualTo(bounds(b).size.y).Within(.001f));Assert.That(bounds(a).min.y,Is.EqualTo(bounds(b).min.y).Within(.001f));}finally{EditorSceneManager.ClosePreviewScene(scene);}
    }
}

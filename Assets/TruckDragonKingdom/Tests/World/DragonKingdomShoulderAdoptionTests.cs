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

public sealed class DragonKingdomShoulderAdoptionTests
{
    [UnityTest,Timeout(1200000)]
    public IEnumerator FunctionalShouldersRetainOriginalPrefabIdsModeOverridesAmbientGroundAndTractors()
    {
        const string root="Assets/TruckDragonKingdom/",generated=root+"Tests/World/Generated/";
        Directory.CreateDirectory(generated);AssetDatabase.Refresh();
        for(int i=0;i<SceneManager.sceneCount;i++)
        {
            var scene=SceneManager.GetSceneAt(i);
            if(Application.isBatchMode&&string.IsNullOrEmpty(scene.path))Assert.That(EditorSceneManager.SaveScene(scene,generated+"ShoulderRunner-"+Guid.NewGuid().ToString("N")+".unity"),Is.True);
            Assert.That(scene.isDirty||string.IsNullOrEmpty(scene.path),Is.False,"Preserve current saved user scenes before narrow shoulder adoption.");
        }
        var prior=EditorSceneManager.GetSceneManagerSetup();
        var isolated=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        Assert.That(EditorSceneManager.SaveScene(isolated,generated+"ShoulderIsolation-"+Guid.NewGuid().ToString("N")+".unity"),Is.True);
        try
        {
            var type=Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomShoulderAdoption, Assembly-CSharp-Editor",true);
            type.GetMethod("AdoptValidatedShouldersOnly",Type.EmptyTypes).Invoke(null,null);yield return null;
            var receipt=type.GetProperty("LastReceipt").GetValue(null);object Field(object o,string n)=>o.GetType().GetField(n).GetValue(o);
            foreach(string field in new[]{"allOriginalPrefabLocalIdsPreserved","allOriginalPreviewLocalIdsPreserved","oldTerrainModelDefinitionMaterialBytesPreserved","allModeSceneBytesOverridesAmbientAndTractorsPreserved","originalPhysicalGatesPassed"})Assert.That((bool)Field(receipt,field),Is.True,field);
            Assert.That((int)Field(receipt,"originalPrefabObjects"),Is.GreaterThan(500));
            var modes=(Array)Field(receipt,"modes");Assert.That(modes.Length,Is.GreaterThanOrEqualTo(3));
            foreach(var mode in modes){Assert.That((int)Field(mode,"trucks"),Is.EqualTo(4));Assert.That((int)Field(mode,"groundRouteChecks"),Is.GreaterThan(64));}
            var files=new[]{root+"Prefabs/Shared Dragon Kingdom.prefab",root+"Scenes/DragonKingdomPreview.unity",root+"Configuration/Dragon Kingdom.asset"}.ToDictionary(p=>p,p=>File.ReadAllBytes(p));
            type.GetMethod("AdoptValidatedShouldersOnly",Type.EmptyTypes).Invoke(null,null);yield return null;
            foreach(var file in files)Assert.That(File.ReadAllBytes(file.Key),Is.EqualTo(file.Value),"Repeated adoption rewrote an accepted asset: "+file.Key);
            var scene=EditorSceneManager.OpenScene(root+"Scenes/DragonKingdomPreview.unity",OpenSceneMode.Single);
            Physics.SyncTransforms();Vector3 failedFoot=new Vector3(-284.66f,22.5f,-6.95f);
            var supported=Physics.RaycastAll(failedFoot+Vector3.up*5,Vector3.down,10,~0,QueryTriggerInteraction.Ignore);
            Assert.That(supported.Any(hit=>hit.collider is TerrainCollider&&hit.normal.y>.8f&&Mathf.Abs(hit.point.y-failedFoot.y)<3),Is.True,"V87's actual offroad tree foot still has no supported authored shoulder.");
        }
        finally{EditorSceneManager.RestoreSceneManagerSetup(prior);}
    }
}
#endif

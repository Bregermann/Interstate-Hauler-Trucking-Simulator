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

public sealed class DragonKingdomRegionalWorldIntegrationTests
{
    [UnityTest,Timeout(1200000)]
    public IEnumerator MeasuredRegionalVistaCandidatePreservesAcceptedAssetsAndPassesPhysicalAndContinuousExtractionGates()
    {
        const string generated="Assets/TruckDragonKingdom/Tests/World/Generated/";
        Directory.CreateDirectory(generated);AssetDatabase.Refresh();
        for(int i=0;i<SceneManager.sceneCount;i++)
        {
            var scene=SceneManager.GetSceneAt(i);
            if(Application.isBatchMode&&string.IsNullOrEmpty(scene.path))Assert.That(EditorSceneManager.SaveScene(scene,generated+"RegionalVistaRunner-"+Guid.NewGuid().ToString("N")+".unity"),Is.True);
            Assert.That(scene.isDirty||string.IsNullOrEmpty(scene.path),Is.False,"Preserve current user scenes before isolated candidate authoring.");
        }
        var setup=EditorSceneManager.GetSceneManagerSetup();var before=setup.ToDictionary(s=>s.path,s=>File.ReadAllBytes(s.path));
        var isolated=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        Assert.That(EditorSceneManager.SaveScene(isolated,generated+"RegionalVistaIsolation-"+Guid.NewGuid().ToString("N")+".unity"),Is.True);
        try
        {
            var type=Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomRegionalWorldIntegration, Assembly-CSharp-Editor",true);
            type.GetMethod("CreateReviewCandidate",Type.EmptyTypes).Invoke(null,null);yield return null;
            var receipt=type.GetProperty("LastReceipt").GetValue(null);object Field(object o,string n)=>o.GetType().GetField(n).GetValue(o);
            Assert.That((string)Field(receipt,"status"),Is.EqualTo("PHYSICAL_GATES_PASSED_REQUIRES_RENDER_AND_ACTUAL_DRIVING_REVIEW"));
            foreach(string n in new[]{"sourceBytesPreserved","roadsAndCriticalPosesExact","originalPhysicalGatesPassed","broadVendorHighlandMassRetained"})Assert.That((bool)Field(receipt,n),Is.True,n);
            var terrains=(Array)Field(receipt,"terrains");Assert.That(terrains.Length,Is.EqualTo(4));
            foreach(var tile in terrains)
            {
                Assert.That((int)Field(tile,"resolution"),Is.EqualTo(513));Assert.That((int)Field(tile,"protectedSamples"),Is.GreaterThan(0));
                Assert.That((bool)Field(tile,"protectedSamplesExact"),Is.True);
            }
            Assert.That((int)Field(receipt,"extractionSweepQueries"),Is.GreaterThan(400));
            Assert.That(((IList)Field(receipt,"extractionFailures")).Count,Is.Zero);
            Assert.That(((Array)Field(receipt,"placements")).Length,Is.GreaterThan(50),"Actual existing forest/props/outcrops were re-grounded rather than omitted.");
            foreach(string n in new[]{"prefab","preview","definition"})Assert.That(File.Exists((string)Field(receipt,n)),Is.True,n);
            foreach(var source in before)Assert.That(File.ReadAllBytes(source.Key),Is.EqualTo(source.Value));
        }
        finally{EditorSceneManager.RestoreSceneManagerSetup(setup);}
    }
}
#endif

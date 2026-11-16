#if UNITY_EDITOR && VISTA
using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class DragonKingdomCavernFinishTests
{
    static Type Integration=>Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomRegionalWorldIntegration, Assembly-CSharp-Editor",true);
    [Test,Timeout(120000)]
    public void ActualOwnedCrystalsRocksAndLanternSourcesHaveNativeGeometryMaterialsAndOneSourceLight()
    {
        var proof=Integration.GetMethod("CaptureCavernFinishSourceAudit").Invoke(null,null);object Field(object value,string name)=>value.GetType().GetField(name).GetValue(value);
        Assert.That((bool)Field(proof,"sourceBytesPreserved"),Is.True);Assert.That((bool)Field(proof,"sourcesUsable"),Is.True);var rows=(Array)Field(proof,"sources");Assert.That(rows.Length,Is.EqualTo(10));int meshes=0,lamps=0;
        foreach(var row in rows)
        {
            string path=(string)Field(row,"source");if(path.EndsWith("/Light_lantern.prefab")||path.EndsWith("/P_PROP_lantern_04.prefab")){Assert.That((int)Field(row,"embeddedLights"),Is.EqualTo(1));lamps++;}
            if(!path.EndsWith("/Light_lantern.prefab")){Assert.That((int)Field(row,"primaryVertices"),Is.GreaterThan(0));Assert.That(((string[])Field(row,"materials")).Length,Is.GreaterThan(0));meshes++;}
        }
        Assert.That(lamps,Is.EqualTo(2),"One source PROP and its one nested source Light, never two lights per placed lantern.");Assert.That(meshes,Is.EqualTo(9));TestContext.WriteLine(JsonUtility.ToJson(proof,true));
    }
    [Test]
    public void SharedCavernFinishingCannotModifyThePersistentCanonicalWorld()
    {
        const string path="Assets/TruckDragonKingdom/Prefabs/Shared Dragon Kingdom.prefab";var before=System.IO.File.ReadAllBytes(path);var source=AssetDatabase.LoadAssetAtPath<GameObject>(path);Assert.That(source,Is.Not.Null);var worldType=Type.GetType("LWS.TruckDragonKingdom.DragonKingdomWorld, Assembly-CSharp",true);var world=source.GetComponent(worldType);int count=source.GetComponentsInChildren<Transform>(true).Length;
        var failure=Assert.Throws<TargetInvocationException>(()=>Integration.GetMethod("InstallSharedCavernFinish").Invoke(null,new object[]{world,"Assets/TruckDragonKingdom/VisualCandidates/NotCreatedByThisTest",null,null}));Assert.That(failure.InnerException,Is.TypeOf<InvalidOperationException>());Assert.That(failure.InnerException.Message,Does.Contain("stopped isolated native Digger candidate"));Assert.That(source.GetComponentsInChildren<Transform>(true).Length,Is.EqualTo(count));Assert.That(System.IO.File.ReadAllBytes(path),Is.EqualTo(before));
    }
    [Test,Timeout(180000)]
    public void ActualSavedNativeCavernAcceptsSourceFinishAndClearanceWithoutReexcavatingOrChangingVoxelBytes()
    {
        const string generated="Assets/TruckDragonKingdom/Tests/World/Generated/";System.IO.Directory.CreateDirectory(generated);AssetDatabase.Refresh();
        for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)
        {
            var scene=UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);if(Application.isBatchMode&&string.IsNullOrEmpty(scene.path))Assert.That(UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene,generated+"CaveFinishRunner-"+Guid.NewGuid().ToString("N")+".unity"),Is.True);Assert.That(scene.isDirty||string.IsNullOrEmpty(scene.path),Is.False);
        }
        string proof=(string)Integration.GetMethod("CaptureSharedCavernFinishSavedProbe").Invoke(null,null);Assert.That(proof,Does.Contain("ACTUAL_SAVED_NATIVE_CAVE_FINISHING_TRIAL_PASSED"));Assert.That(proof,Does.Contain("\"originalSourceAndVoxelBytesExact\": true"));Assert.That(proof,Does.Contain("\"temporaryVariantsRemoved\": true"));TestContext.WriteLine(proof);
    }
}
#endif

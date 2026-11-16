#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class DragonKingdomRidersCourseMigrationTests
{
    [Test,Timeout(120000)]
    public void ActualMeasuredCourseChangesOnlyOriginalGatePosesAndRepeatsSavedSceneBytes()
    {
        const string generated="Assets/TruckDragonKingdom/Tests/World/Generated/";Directory.CreateDirectory(generated);AssetDatabase.Refresh();
        for(int i=0;i<SceneManager.sceneCount;i++)
        {
            var scene=SceneManager.GetSceneAt(i);
            if(Application.isBatchMode&&string.IsNullOrEmpty(scene.path))Assert.That(EditorSceneManager.SaveScene(scene,generated+"CourseRunner-"+Guid.NewGuid().ToString("N")+".unity"),Is.True);
            Assert.That(scene.isDirty||string.IsNullOrEmpty(scene.path),Is.False,"Preserve real user scenes before explicit course geometry migration.");
        }
        var setup=EditorSceneManager.GetSceneManagerSetup();var isolated=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        Assert.That(EditorSceneManager.SaveScene(isolated,generated+"CourseIsolation-"+Guid.NewGuid().ToString("N")+".unity"),Is.True);
        try
        {
            var type=Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomRidersCourseMigration, Assembly-CSharp-Editor",true);
            type.GetMethod("ApplyMeasuredCourse").Invoke(null,null);var proof=type.GetProperty("LastReceipt").GetValue(null);object Field(string n)=>proof.GetType().GetField(n).GetValue(proof);
            Assert.That((string)Field("status"),Is.EqualTo("SAVED_ORIGINAL_GATE_IDENTITIES_REQUIRES_ACTUAL_FLIGHT_AND_REMATCH"));
            Assert.That((int)Field("gates"),Is.EqualTo(12));Assert.That((int)Field("changedDocuments"),Is.InRange(1,13));
            foreach(string field in new[]{"originalLocalIdsPreserved","allOtherDocumentsExact","matchRulesExact","originalMetaExact"})Assert.That((bool)Field(field),Is.True,field);
            string[] args=Environment.GetCommandLineArgs();int appearance=Array.IndexOf(args,"-tsDragonRoadAppearanceReceipt");string expectedAppearance="";
            if(appearance>=0){Assert.That(appearance+1,Is.LessThan(args.Length));using(var sha=SHA256.Create())expectedAppearance=BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(args[appearance+1]))).Replace("-","").ToLowerInvariant();}
            Assert.That((string)Field("roadAppearanceEvidenceSha256")??"",Is.EqualTo(expectedAppearance),"Any authorized preceding clear-road binding refresh must remain part of exact course provenance.");
            string path=(string)Field("scene");byte[] before=File.ReadAllBytes(path);type.GetMethod("ApplyMeasuredCourse").Invoke(null,null);Assert.That(File.ReadAllBytes(path),Is.EqualTo(before),"Explicit repeat must not rewrite the original saved scene.");
        }
        finally{EditorSceneManager.RestoreSceneManagerSetup(setup);}
    }
}
#endif

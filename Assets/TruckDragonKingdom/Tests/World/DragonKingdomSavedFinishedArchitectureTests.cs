#if UNITY_EDITOR && VISTA
using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class DragonKingdomSavedFinishedArchitectureTests
{
    [Test]
    public void MissingSavedArchitectureCannotAuthorizeAnAdoptionReceipt()
    {
        var type=Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomRegionalWorldIntegration, Assembly-CSharp-Editor",true);
        var method=type.GetMethod("ValidateFinishedArchitectureSavePrerequisites");
        var failure=Assert.Throws<TargetInvocationException>(()=>method.Invoke(null,new object[]{null,null,null}));
        Assert.That(failure.InnerException,Is.TypeOf<InvalidOperationException>());
        Assert.That(failure.InnerException.Message,Does.Contain("all678"));
    }

    [Test,Timeout(120000)]
    public void ActualCompleteSourceStageStillRejectsDisposableMaterialsAndCannotAuthorizeSavedArchitecture()
    {
        var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonFinishedArchitectureReceipt");
        Assert.That(at>=0&&at+1<args.Length,Is.True,"Supply the actual complete native four-frame source receipt; no synthetic successful world.");
        string path=args[at+1];byte[] before=File.ReadAllBytes(path);string json=File.ReadAllText(path);
        var type=Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomRegionalWorldIntegration, Assembly-CSharp-Editor",true);
        var proofType=type.GetNestedType("FinishedArchitectureComposition");var proof=JsonUtility.FromJson(json,proofType);var method=type.GetMethod("ValidateFinishedArchitectureSavePrerequisites");
        Assert.That((int)proofType.GetField("instances").GetValue(proof),Is.EqualTo(678));
        Assert.That((int)proofType.GetField("footings").GetValue(proof),Is.EqualTo(525));
        string ReadFailure()
        {
            var failure=Assert.Throws<TargetInvocationException>(()=>method.Invoke(null,new object[]{proof,null,null}));Assert.That(failure.InnerException,Is.TypeOf<InvalidOperationException>());return failure.InnerException.Message;
        }
        Assert.That(ReadFailure(),Does.Contain("persistent public-upgraded materials"),"A genuinely passed source stage must not masquerade as saved terrain/waterfront acceptance.");
        foreach(string field in new[]{"instances","footings","ownedMeshRepairs"})
        {
            var member=proofType.GetField(field);object original=member.GetValue(proof);try{member.SetValue(proof,0);Assert.That(ReadFailure(),Does.Contain("all678"),field);}finally{member.SetValue(proof,original);}
        }
        var groups=proofType.GetField("groups");object saved=groups.GetValue(proof);try{groups.SetValue(proof,null);Assert.That(ReadFailure(),Does.Contain("all678"));}finally{groups.SetValue(proof,saved);}
        Assert.That(JsonUtility.ToJson(proof),Is.EqualTo(JsonUtility.ToJson(JsonUtility.FromJson(json,proofType))),"Validation and rejected mutations must leave the actual source-stage object unchanged.");
        Assert.That(File.ReadAllBytes(path),Is.EqualTo(before),"Read-only rejection must preserve the complete original native evidence.");
        TestContext.WriteLine("ACTUAL_SOURCE_STAGE_IS_NOT_SAVED_WORLD_PROOF instances=678 sourceFootings=525 disposableMaterialsRejected=true missingCountsRejected=true sourceReceiptExact=true noAdoptionEnabled=true");
    }
}
#endif

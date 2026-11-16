#if UNITY_EDITOR && VISTA
using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

public sealed class DragonKingdomArchitectureRemainingTargetsTests
{
    [Test,Timeout(300000)]
    public void ActualRemainingArchitectureTargetsHaveCompleteReadOnlyNativeIdentityAndAbsenceInventoryBeforeAnyNewClassification()
    {
        var type=Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomRegionalWorldIntegration, Assembly-CSharp-Editor",true);
        var proof=type.GetMethod("CaptureRemainingArchitectureTargetAbsenceWithoutWritingSources",BindingFlags.Public|BindingFlags.Static).Invoke(null,null);
        object Field(object value,string name)=>value.GetType().GetField(name).GetValue(value);
        foreach(string key in new[]{"sourceBytesPreserved","temporaryPreviewDestroyed"})Assert.That((bool)Field(proof,key),Is.True,key);
        Assert.That((int)Field(proof,"unknownFields"),Is.EqualTo(85),"This is an exact diagnostic, not a generic missing-metadata allowlist.");
        foreach(var source in ((Array)Field(proof,"sources")).Cast<object>())
        {
            Assert.That((int)Field(source,"missingScripts"),Is.Zero);Assert.That((int)Field(source,"sourceObjects"),Is.EqualTo((int)Field(source,"instanceObjects")));
            foreach(string key in new[]{"everyRequestedIdAbsentFromRawDocuments","allCorrespondenceChainsAbsent","allExactNestedAliasesAbsent"})Assert.That((bool)Field(source,key),Is.True,key);
            Assert.That(((Array)Field(source,"originalIdentities")).Length,Is.EqualTo((int)Field(source,"sourceObjects")));
            Assert.That(((Array)Field(source,"instanceIdentities")).Length,Is.EqualTo((int)Field(source,"instanceObjects")));
        }
    }
}
#endif

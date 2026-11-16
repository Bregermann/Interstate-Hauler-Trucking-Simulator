#if UNITY_EDITOR && VISTA
using System;
using System.Linq;
using NUnit.Framework;

public sealed class DragonKingdomArchitectureMeshReferenceTests
{
    [Test,Timeout(120000)]
    public void ExactOwnedLadderPrefabAndFbxNativeSubassetsExposeActualRequestedAndAvailableMeshIdsWithoutSourceWrites()
    {
        var type=Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomRegionalWorldIntegration, Assembly-CSharp-Editor",true);var proof=type.GetMethod("CaptureExactLadderMeshReferenceAudit").Invoke(null,null);object Field(object value,string name)=>value.GetType().GetField(name).GetValue(value);
        foreach(string flag in new[]{"exactSources","rawPrefabReferenceConfirmed","sourceBytesPreserved"})Assert.That((bool)Field(proof,flag),Is.True,flag);Assert.That((int)Field(proof,"missingPrefabScripts"),Is.Zero);Assert.That((int)Field(proof,"missingModelScripts"),Is.Zero);Assert.That((string)Field(proof,"requestedLocalId"),Is.EqualTo("4300000"));
        Assert.That(((Array)Field(proof,"prefabReferences")).Length,Is.EqualTo(1));Assert.That(((Array)Field(proof,"allNativeSubassets")).Cast<object>().Any(a=>(string)Field(a,"type")=="UnityEngine.Mesh"&&(int)Field(a,"vertices")>0),Is.True,"A source reference defect must not be reclassified as authored invisible art.");
    }
}
#endif

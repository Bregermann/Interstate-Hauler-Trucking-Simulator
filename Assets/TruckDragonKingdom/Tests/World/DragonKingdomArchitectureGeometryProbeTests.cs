#if UNITY_EDITOR && VISTA
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class DragonKingdomArchitectureGeometryProbeTests
{
    [Serializable]sealed class SnapshotJsonRow
    {public string layout,expectedScene,actualScene,expectedMetadata,actualMetadata,nestedType,legacyRecursiveMetadataSha256,legacyRecursiveControlError;public bool metadataDescriptorPresent,legacyRecursiveMetadataDescriptorPresent,exact;}
    [Serializable]sealed class LegacyRecursiveSnapshotControl
    {public string path,sha256,guid,metaSha256,source,sourceGuid,sourceMetaSha256;public LegacyRecursiveSnapshotControl originalMetadata;}
    [Serializable]sealed class LegacyRecursiveLayoutControl
    {public LegacyRecursiveSnapshotControl sourceSceneSnapshot;}
    [Serializable]sealed class SnapshotJsonProof
    {public string scope="ACTUAL_NATIVE_JSONUTILITY_FOUR_PINNED_SCENE_SNAPSHOT_AND_CONCRETE_METADATA_DESCRIPTOR_READBACK_NOT_WORLD_OR_MATERIAL_ACCEPTANCE";public bool passed;public SnapshotJsonRow[] rows;}
    [Test,Timeout(120000)]
    public void ActualFourArchitectureSceneSnapshotsDeserializeWithExactOriginalMetadataUsingConcreteNativeDto()
    {
        var type=Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomRegionalWorldIntegration, Assembly-CSharp-Editor",true);
        var layoutType=type.GetNestedType("ArchitectureMaterialLayout",BindingFlags.Public);Assert.That(layoutType,Is.Not.Null);
        var paths=(string[])type.GetField("ArchitectureLayoutPaths",BindingFlags.Public|BindingFlags.Static).GetValue(null);Assert.That(paths.Length,Is.EqualTo(4));
        object Value(object source,string name)=>source.GetType().GetField(name).GetValue(source);
        var nativeSha=type.GetMethod("ArchitectureGeometrySha",BindingFlags.NonPublic|BindingFlags.Static);Assert.That(nativeSha,Is.Not.Null);
        string Sha(string path)=>(string)nativeSha.Invoke(null,new object[]{path});
        var args=Environment.GetCommandLineArgs();int outputIndex=Array.IndexOf(args,"-tsDragonOutput");Assert.That(outputIndex,Is.GreaterThanOrEqualTo(0));string output=Path.GetFullPath(args[outputIndex+1]);var proof=new SnapshotJsonProof();var rows=new List<SnapshotJsonRow>();
        try
        {
            foreach(string path in paths)
            {
                string raw=File.ReadAllText(path);var layout=JsonUtility.FromJson(raw,layoutType);var snapshot=Value(layout,"sourceSceneSnapshot");var metadata=snapshot==null?null:Value(snapshot,"originalMetadata");
                var row=new SnapshotJsonRow{layout=path,expectedScene=(string)Value(layout,"sourceSceneSha256"),actualScene=snapshot==null?null:(string)Value(snapshot,"sha256"),expectedMetadata=(string)Value(layout,"sourceSceneMetaSha256"),actualMetadata=metadata==null?null:(string)Value(metadata,"sha256"),metadataDescriptorPresent=metadata!=null,nestedType=metadata?.GetType().FullName};rows.Add(row);
                try{var legacy=JsonUtility.FromJson<LegacyRecursiveLayoutControl>(raw)?.sourceSceneSnapshot?.originalMetadata;row.legacyRecursiveMetadataDescriptorPresent=legacy!=null;row.legacyRecursiveMetadataSha256=legacy?.sha256;}catch(Exception legacyError){row.legacyRecursiveControlError=legacyError.ToString();}
                Assert.That(snapshot,Is.Not.Null,path);Assert.That(metadata,Is.Not.Null,"Native JsonUtility must retain the actual original metadata descriptor: "+path);
                row.exact=row.expectedScene==row.actualScene&&row.expectedMetadata==row.actualMetadata&&Sha((string)Value(snapshot,"path"))==row.expectedScene&&Sha((string)Value(metadata,"path"))==row.expectedMetadata;
                Assert.That(row.exact,Is.True,JsonUtility.ToJson(row));
            }
            proof.passed=true;
        }
        finally{proof.rows=rows.ToArray();Directory.CreateDirectory(output);File.WriteAllText(Path.Combine(output,"authored-architecture-snapshot-jsonutility.json"),JsonUtility.ToJson(proof,true));}
    }
    [Test,Timeout(600000)]
    public void ActualAll9637ArchitectureOverridesRequireResolvedNativeAncestryOrExistingExactObsoleteProofBeforePlacement()
    {
        var type=Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomRegionalWorldIntegration, Assembly-CSharp-Editor",true);
        string controls=(string)type.GetMethod("InspectArchitectureResolverControlsWithoutSavingAssets").Invoke(null,null);TestContext.WriteLine(controls);
        string absentControls=(string)type.GetMethod("InspectAuditedAbsentMetadataRejectionControlsWithoutSourceWrites").Invoke(null,null);TestContext.WriteLine(absentControls);
        type.GetMethod("CaptureAuthoredArchitectureGeometryFromCommandLine",BindingFlags.Public|BindingFlags.Static).Invoke(null,null);
        var proof=type.GetProperty("LastArchitectureGeometry",BindingFlags.Public|BindingFlags.Static).GetValue(null);
        object Field(object value,string name)=>value.GetType().GetField(name).GetValue(value);
        foreach(string name in new[]{"materialPreparationInvoked","sourceBaselineCaptured","inspectionCompleted","sourceBytesPreserved","world29AndCanonicalBytesPreserved","incomingSceneContextRestored","temporaryPreviewDestroyed","allGeometryOverridesReconciled"})Assert.That((bool)Field(proof,name),Is.True,name);
        Assert.That((int)Field(proof,"instances"),Is.EqualTo(678));Assert.That((int)Field(proof,"rawOverrideFields"),Is.EqualTo(9637));Assert.That((int)Field(proof,"unresolvedTargets"),Is.Zero,"Unresolved metadata and geometry both remain rejections; no placement approval from a partial diagnostic.");
        Assert.That((int)Field(proof,"auditedAbsentMetadataFields"),Is.EqualTo(85));
        Assert.That((int)Field(proof,"existingExactAbsentFields"),Is.EqualTo(20));Assert.That((int)Field(proof,"nativeResolvedTargetFields"),Is.EqualTo(9532));
        var nativeIdentities=((Array)Field(proof,"nativeResolvedIdentities")).Cast<object>().ToDictionary(v=>(string)Field(v,"id"));Assert.That(nativeIdentities.Count,Is.GreaterThan(0));
        foreach(var identity in nativeIdentities.Values){Assert.That((bool)Field(identity,"actualPersistentSourceMember"),Is.True);Assert.That((bool)Field(identity,"exactDefaultSourceIdentityAuthority"),Is.True);Assert.That(new[]{"ACTUAL_PUBLIC_ASSET_GUID_AND_LOCAL_FILE_ID","ACTUAL_PUBLIC_GLOBAL_OBJECT_ID","ACTUAL_PUBLIC_GLOBAL_OBJECT_AND_PREFAB_ID_XOR"}.Contains((string)Field(identity,"method")),Is.True);}

        var rows=((Array)Field(proof,"overrides")).Cast<object>().ToArray();int aliases=0,oldAbsentGeometry=0,accountedMetadata=0;
        foreach(var row in rows)
        {
            var applied=Field(row,"appliedEvidence");bool oldAbsent=applied!=null&&(string)Field(applied,"nativeIdentityMethod")=="EXACT_PINNED_PREFAB_SHA_AND_COMPLETE_NATIVE_SOURCE_INSTANCE_ID_ABSENCE";
            bool auditedMetadata=applied!=null&&(string)Field(applied,"nativeIdentityMethod")=="V309_PINNED_LAYOUT_INSTANCE_FIELD_RAW_VALUE_SOURCE_SHA_AND_COMPLETE_NATIVE_ALIAS_ABSENCE";
            Assert.That((bool)Field(row,"paired")||oldAbsent||auditedMetadata,Is.True,"Every record needs native identity or explicit separately recorded exact native absence evidence.");
            if(auditedMetadata){accountedMetadata++;Assert.That((bool)Field(row,"geometryField"),Is.False);Assert.That((bool)Field(row,"applied"),Is.False);Assert.That((int)Field(applied,"sourceObjectCount"),Is.EqualTo((int)Field(applied,"instanceObjectCount")));Assert.That((int)Field(applied,"missingScripts"),Is.Zero);}
            if((bool)Field(row,"geometryField")){Assert.That((bool)Field(row,"applied")||oldAbsent,Is.True);if(oldAbsent)oldAbsentGeometry++;}
            if((bool)Field(row,"paired")){string nativeId=(string)Field(row,"nativeResolvedIdentityId");Assert.That(nativeId,Is.Not.Null.And.Not.Empty);Assert.That(nativeIdentities.ContainsKey(nativeId),Is.True,"Each actual paired record needs its measured native identity method and concrete public GUID/ID values.");}
            var alias=Field(row,"nativeAlias");if(alias==null)continue;aliases++;
            foreach(string name in new[]{"persistentOriginalComponent","actualDescendant","serializedSourcePrefabExact","serializedRootCorrespondenceExact","requestedAliasExact"})Assert.That((bool)Field(alias,name),Is.True,name);
            foreach(string name in new[]{"rawNestedInstanceSha256","rawStrippedTransformSha256","sourcePrefabSha256"})Assert.That((string)Field(alias,name),Has.Length.EqualTo(64),name);
        }
        Assert.That(accountedMetadata,Is.EqualTo(85));Assert.That(rows.Count(r=>(bool)Field(r,"paired"))+accountedMetadata+20,Is.EqualTo(9637));TestContext.WriteLine("Actual independently serialized nested fallback witnesses: "+aliases+"; native default GUID/GlobalObject identity witnesses: "+nativeIdentities.Count+". Existing20 absence records are not aliases.");Assert.That(oldAbsentGeometry,Is.EqualTo(8),"Only the already measured seven House13 TRS fields and one House07 inactive target use the existing exact obsolete authority.");
    }
    [Test,Timeout(600000)]
    public void ActualFourAuthoredEnsemblesEnumerateAll678InstancesAnd9637OriginalFieldsWithoutWorldOrSourceWrites()
    {
        var type=Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomRegionalWorldIntegration, Assembly-CSharp-Editor",true);
        type.GetMethod("CaptureAuthoredArchitectureGeometryFromCommandLine",BindingFlags.Public|BindingFlags.Static).Invoke(null,null);
        var proof=type.GetProperty("LastArchitectureGeometry",BindingFlags.Public|BindingFlags.Static).GetValue(null);
        object Value(object source,string name)=>source.GetType().GetField(name).GetValue(source);
        foreach(string name in new[]{"materialPreparationInvoked","sourceBaselineCaptured","inspectionCompleted","sourceBytesPreserved","world29AndCanonicalBytesPreserved","incomingSceneContextRestored","temporaryPreviewDestroyed"})Assert.That((bool)Value(proof,name),Is.True,name);
        Assert.That((int)Value(proof,"layouts"),Is.EqualTo(4));Assert.That((int)Value(proof,"instances"),Is.EqualTo(678));Assert.That((int)Value(proof,"rawOverrideFields"),Is.EqualTo(9637));
        var ensembles=((Array)Value(proof,"ensembles")).Cast<object>().ToArray();Assert.That(ensembles.Select(e=>(int)Value(e,"instances")),Is.EqualTo(new[]{110,156,382,30}));
        foreach(var ensemble in ensembles)
        {
            Assert.That((int)Value(ensemble,"primaryMeshes"),Is.GreaterThan(0));Assert.That((int)Value(ensemble,"primaryVertices"),Is.GreaterThan(0));
            Assert.That(((Array)Value(ensemble,"parts")).Length,Is.EqualTo((int)Value(ensemble,"instances")));Assert.That(((Array)Value(ensemble,"parentMapping")).Length,Is.EqualTo((int)Value(ensemble,"parents")));
        }
        // This case verifies complete inspection and preservation, never source
        // override/placement acceptance. The receipt retains every unresolved
        // field and labels its geometry provisional instead of fabricating it.
        Assert.That(((Array)Value(proof,"overrides")).Length,Is.EqualTo(9637));
    }
}
#endif

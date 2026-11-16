#if UNITY_EDITOR && VISTA
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace LWS.TruckDragonKingdom.Editor
{
    public static partial class DragonKingdomRegionalWorldIntegration
    {
        // Exactly the85 individually observed absent metadata records from
        // V309, including instance and original raw value. No geometry fields,
        // guessed targets, property-wide exemptions or automatic regeneration.
        static readonly Dictionary<string,string> ExactArchitectureAbsentMetadata=new Dictionary<string,string>(StringComparer.Ordinal)
        {
            {"158236386/3678c734ae173ef42a03e2f2d3a28010/2206738799322266227/m_StaticEditorFlags","4294967295"},
            {"158236386/3678c734ae173ef42a03e2f2d3a28010/2749237017225053597/m_StaticEditorFlags","4294967295"},
            {"158236386/3678c734ae173ef42a03e2f2d3a28010/6380340857545017640/m_BoundingSphereOverride.x","27034.734"},
            {"158236386/3678c734ae173ef42a03e2f2d3a28010/6380340857545017640/m_BoundingSphereOverride.y","0.000000011733592"},
            {"158236386/3678c734ae173ef42a03e2f2d3a28010/6380340857545017640/m_BoundingSphereOverride.z","3.398643e-17"},
            {"158236386/3678c734ae173ef42a03e2f2d3a28010/6380340857545017640/m_BoundingSphereOverride.w","1.53585e+13"},
            {"158236386/3678c734ae173ef42a03e2f2d3a28010/6931936529078089414/m_BoundingSphereOverride.y","68.94593"},
            {"158236386/3678c734ae173ef42a03e2f2d3a28010/6931936529078089414/m_BoundingSphereOverride.z","0"},
            {"158236386/3678c734ae173ef42a03e2f2d3a28010/6931936529078089414/m_BoundingSphereOverride.w","0"},
            {"158236386/3678c734ae173ef42a03e2f2d3a28010/6931936529078089414/m_BoundingSphereOverride.x","134.33655"},
            {"906895438/3678c734ae173ef42a03e2f2d3a28010/2206738799322266227/m_StaticEditorFlags","4294967295"},
            {"906895438/3678c734ae173ef42a03e2f2d3a28010/2749237017225053597/m_StaticEditorFlags","4294967295"},
            {"906895438/3678c734ae173ef42a03e2f2d3a28010/6380340857545017640/m_BoundingSphereOverride.y","0"},
            {"906895438/3678c734ae173ef42a03e2f2d3a28010/6380340857545017640/m_BoundingSphereOverride.z","0"},
            {"906895438/3678c734ae173ef42a03e2f2d3a28010/6380340857545017640/m_BoundingSphereOverride.w","0"},
            {"906895438/3678c734ae173ef42a03e2f2d3a28010/6380340857545017640/m_BoundingSphereOverride.x","1"},
            {"906895438/3678c734ae173ef42a03e2f2d3a28010/6931936529078089414/m_BoundingSphereOverride.x","0.0703125"},
            {"906895438/3678c734ae173ef42a03e2f2d3a28010/6931936529078089414/m_BoundingSphereOverride.y","11"},
            {"906895438/3678c734ae173ef42a03e2f2d3a28010/6931936529078089414/m_BoundingSphereOverride.z","0"},
            {"906895438/3678c734ae173ef42a03e2f2d3a28010/6931936529078089414/m_BoundingSphereOverride.w","0"},
            {"1260168225/3678c734ae173ef42a03e2f2d3a28010/2206738799322266227/m_StaticEditorFlags","4294967295"},
            {"1260168225/3678c734ae173ef42a03e2f2d3a28010/2749237017225053597/m_StaticEditorFlags","4294967295"},
            {"1260168225/3678c734ae173ef42a03e2f2d3a28010/6380340857545017640/m_BoundingSphereOverride.x","-103153.36"},
            {"1260168225/3678c734ae173ef42a03e2f2d3a28010/6380340857545017640/m_BoundingSphereOverride.y","-1.7229883e-17"},
            {"1260168225/3678c734ae173ef42a03e2f2d3a28010/6380340857545017640/m_BoundingSphereOverride.z","3.841146e+24"},
            {"1260168225/3678c734ae173ef42a03e2f2d3a28010/6380340857545017640/m_BoundingSphereOverride.w","1.5229338"},
            {"1260168225/3678c734ae173ef42a03e2f2d3a28010/6931936529078089414/m_BoundingSphereOverride.x","-103153.36"},
            {"1260168225/3678c734ae173ef42a03e2f2d3a28010/6931936529078089414/m_BoundingSphereOverride.y","-1.7229883e-17"},
            {"1260168225/3678c734ae173ef42a03e2f2d3a28010/6931936529078089414/m_BoundingSphereOverride.z","3.841146e+24"},
            {"1260168225/3678c734ae173ef42a03e2f2d3a28010/6931936529078089414/m_BoundingSphereOverride.w","1.5229338"},
            {"283566843/ba43096d063f9e141bc66d23a6aad4b6/6191784876018045591/m_StaticEditorFlags","4294967295"},
            {"986446797/ba43096d063f9e141bc66d23a6aad4b6/6191784876018045591/m_StaticEditorFlags","4294967295"},
            {"986446797/ba43096d063f9e141bc66d23a6aad4b6/1439406413011755468/m_BoundingSphereOverride.y","1.4033e-41"},
            {"986446797/ba43096d063f9e141bc66d23a6aad4b6/1439406413011755468/m_BoundingSphereOverride.w","7.0696675e-31"},
            {"986446797/ba43096d063f9e141bc66d23a6aad4b6/1439406413011755468/m_BoundingSphereOverride.x","9.6e-43"},
            {"986446797/ba43096d063f9e141bc66d23a6aad4b6/1439406413011755468/m_BoundingSphereOverride.z","0"},
            {"811748479/df8bb62ddc295d2409aee3edecf02124/3645100103038393958/m_StaticEditorFlags","4294967295"},
            {"811748479/df8bb62ddc295d2409aee3edecf02124/3379506654408939690/m_StaticEditorFlags","4294967295"},
            {"811748479/df8bb62ddc295d2409aee3edecf02124/6365860685320147688/m_StaticEditorFlags","4294967295"},
            {"811748479/df8bb62ddc295d2409aee3edecf02124/7562111594907315185/m_BoundingSphereOverride.x","9"},
            {"811748479/df8bb62ddc295d2409aee3edecf02124/7562111594907315185/m_BoundingSphereOverride.y","0"},
            {"811748479/df8bb62ddc295d2409aee3edecf02124/7562111594907315185/m_BoundingSphereOverride.z","1.4e-44"},
            {"811748479/df8bb62ddc295d2409aee3edecf02124/7562111594907315185/m_BoundingSphereOverride.w","2.3510647e-38"},
            {"811748479/df8bb62ddc295d2409aee3edecf02124/8395149719417580861/m_BoundingSphereOverride.x","0"},
            {"811748479/df8bb62ddc295d2409aee3edecf02124/8395149719417580861/m_BoundingSphereOverride.y","0"},
            {"811748479/df8bb62ddc295d2409aee3edecf02124/8395149719417580861/m_BoundingSphereOverride.w","0"},
            {"811748479/df8bb62ddc295d2409aee3edecf02124/8395149719417580861/m_BoundingSphereOverride.z","0"},
            {"811748479/df8bb62ddc295d2409aee3edecf02124/2183255826434417075/m_BoundingSphereOverride.x","9.6e-43"},
            {"811748479/df8bb62ddc295d2409aee3edecf02124/2183255826434417075/m_BoundingSphereOverride.y","7.67e-43"},
            {"811748479/df8bb62ddc295d2409aee3edecf02124/2183255826434417075/m_BoundingSphereOverride.w","1.435e-42"},
            {"811748479/df8bb62ddc295d2409aee3edecf02124/2183255826434417075/m_BoundingSphereOverride.z","0"},
            {"1371505563/df8bb62ddc295d2409aee3edecf02124/3645100103038393958/m_StaticEditorFlags","4294967295"},
            {"1371505563/df8bb62ddc295d2409aee3edecf02124/3379506654408939690/m_StaticEditorFlags","4294967295"},
            {"1371505563/df8bb62ddc295d2409aee3edecf02124/6365860685320147688/m_StaticEditorFlags","4294967295"},
            {"1371505563/df8bb62ddc295d2409aee3edecf02124/7562111594907315185/m_BoundingSphereOverride.x","0"},
            {"1371505563/df8bb62ddc295d2409aee3edecf02124/7562111594907315185/m_BoundingSphereOverride.y","4e-45"},
            {"1371505563/df8bb62ddc295d2409aee3edecf02124/7562111594907315185/m_BoundingSphereOverride.z","0"},
            {"1371505563/df8bb62ddc295d2409aee3edecf02124/7562111594907315185/m_BoundingSphereOverride.w","1.7334872e-28"},
            {"1371505563/df8bb62ddc295d2409aee3edecf02124/8395149719417580861/m_BoundingSphereOverride.x","11"},
            {"1371505563/df8bb62ddc295d2409aee3edecf02124/8395149719417580861/m_BoundingSphereOverride.y","0"},
            {"1371505563/df8bb62ddc295d2409aee3edecf02124/8395149719417580861/m_BoundingSphereOverride.z","0"},
            {"1371505563/df8bb62ddc295d2409aee3edecf02124/8395149719417580861/m_BoundingSphereOverride.w","0"},
            {"1371505563/df8bb62ddc295d2409aee3edecf02124/2183255826434417075/m_BoundingSphereOverride.x","1"},
            {"1371505563/df8bb62ddc295d2409aee3edecf02124/2183255826434417075/m_BoundingSphereOverride.y","0"},
            {"1371505563/df8bb62ddc295d2409aee3edecf02124/2183255826434417075/m_BoundingSphereOverride.z","0"},
            {"1371505563/df8bb62ddc295d2409aee3edecf02124/2183255826434417075/m_BoundingSphereOverride.w","0"},
            {"2070470104/df8bb62ddc295d2409aee3edecf02124/3645100103038393958/m_StaticEditorFlags","4294967295"},
            {"2070470104/df8bb62ddc295d2409aee3edecf02124/3379506654408939690/m_StaticEditorFlags","4294967295"},
            {"2070470104/df8bb62ddc295d2409aee3edecf02124/6365860685320147688/m_StaticEditorFlags","4294967295"},
            {"2070470104/df8bb62ddc295d2409aee3edecf02124/7562111594907315185/m_BoundingSphereOverride.x","9.6e-43"},
            {"2070470104/df8bb62ddc295d2409aee3edecf02124/7562111594907315185/m_BoundingSphereOverride.y","1.1838e-41"},
            {"2070470104/df8bb62ddc295d2409aee3edecf02124/7562111594907315185/m_BoundingSphereOverride.z","0"},
            {"2070470104/df8bb62ddc295d2409aee3edecf02124/7562111594907315185/m_BoundingSphereOverride.w","0"},
            {"2070470104/df8bb62ddc295d2409aee3edecf02124/8395149719417580861/m_BoundingSphereOverride.x","-4"},
            {"2070470104/df8bb62ddc295d2409aee3edecf02124/8395149719417580861/m_BoundingSphereOverride.y","-3.0072228e+23"},
            {"2070470104/df8bb62ddc295d2409aee3edecf02124/8395149719417580861/m_BoundingSphereOverride.z","9.6e-43"},
            {"2070470104/df8bb62ddc295d2409aee3edecf02124/8395149719417580861/m_BoundingSphereOverride.w","1e-45"},
            {"2070470104/df8bb62ddc295d2409aee3edecf02124/2183255826434417075/m_BoundingSphereOverride.x","11"},
            {"2070470104/df8bb62ddc295d2409aee3edecf02124/2183255826434417075/m_BoundingSphereOverride.z","0"},
            {"2070470104/df8bb62ddc295d2409aee3edecf02124/2183255826434417075/m_BoundingSphereOverride.w","0"},
            {"2070470104/df8bb62ddc295d2409aee3edecf02124/2183255826434417075/m_BoundingSphereOverride.y","0"},
            {"1408219951/c0cacda008ac5324598ff77198b809b2/7892989606626299767/m_LocalEulerAnglesHint.z","0"},
            {"1598034780/8930c424f7d0037468691ac2f225bae8/8586730360510306313/m_StaticEditorFlags","4294967295"},
            {"1598034780/8930c424f7d0037468691ac2f225bae8/1795742448316540891/m_StaticEditorFlags","4294967295"},
            {"1598034780/8930c424f7d0037468691ac2f225bae8/6481620312478966445/m_StaticEditorFlags","4294967295"},
        };
        sealed class ArchitectureAbsentSourceContract
        {public string sha;public int objects;}
        static readonly Dictionary<string,ArchitectureAbsentSourceContract> ExactArchitectureAbsentSources=new Dictionary<string,ArchitectureAbsentSourceContract>(StringComparer.Ordinal)
        {
            {"3678c734ae173ef42a03e2f2d3a28010",new ArchitectureAbsentSourceContract{sha="367ac67587a66311efb05705fa12d7cf1658d6e9589dbcd9a26f667722b1df0b",objects=126}},
            {"ba43096d063f9e141bc66d23a6aad4b6",new ArchitectureAbsentSourceContract{sha="3a3b5a6f7a64a67e7d99b70ecd9f5d641f12527a64ed1cec258b52bcbab5bf36",objects=125}},
            {"df8bb62ddc295d2409aee3edecf02124",new ArchitectureAbsentSourceContract{sha="2adeeffff32935342ab7b20ec5af94707912405dd9dd686b402d7dab879c30b6",objects=156}},
            {"c0cacda008ac5324598ff77198b809b2",new ArchitectureAbsentSourceContract{sha="c6d8fa4eca07072c6e8555b5490232dacb712cdf09ca55a0c3c426fc47637232",objects=81}},
            {"8930c424f7d0037468691ac2f225bae8",new ArchitectureAbsentSourceContract{sha="cfbc9055e88c85a7880a8cccfb89f720f48a53b703e8793883aa2062e138358a",objects=112}},
        };
        static bool ObserveAuditedExactArchitectureAbsentMetadata(ArchitectureProbeInstance item,ArchitectureModification change,ArchitectureTargetObservation observation)
        {
            string key=item.row.sceneInstanceId+"/"+change.targetGuid+"/"+change.targetId+"/"+change.property;
            if(!ExactArchitectureAbsentMetadata.TryGetValue(key,out string raw)||raw!=change.sourceValue||change.objectReference!="fileID: 0"||change.targetGuid!=item.row.prefabGuid||observation.geometryField)return false;
            if(item.ensemble.layout!=ArchitectureLayoutPaths[2]||item.ensemble.layoutSha256!=ArchitectureLayoutHashes[2]||AssetSha(item.ensemble.layout)!=ArchitectureLayoutHashes[2]||!ExactArchitectureAbsentSources.TryGetValue(item.row.prefabGuid,out var contract)||item.row.sourceSha256!=contract.sha||AssetSha(item.row.prefab)!=contract.sha||AssetSha(item.row.prefab+".meta")!=item.row.sourceMetaSha256||AssetDatabase.AssetPathToGUID(item.row.prefab)!=item.row.prefabGuid)throw new InvalidOperationException("The exact audited absent architecture metadata cannot apply to any changed source/layout/identity.");
            if(change.property!="m_StaticEditorFlags"&&change.property!="m_LocalEulerAnglesHint.z"&&!Regex.IsMatch(change.property,@"^m_BoundingSphereOverride\.[xyzw]$"))throw new InvalidOperationException("No unaudited or geometric field is eligible for native absent-metadata preservation.");
            var originals=AssemblyObjects(item.source);var clones=AssemblyObjects(item.instance);int missing=item.source.GetComponentsInChildren<Transform>(true).Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject))+item.instance.GetComponentsInChildren<Transform>(true).Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
            string text=File.ReadAllText(Io(item.row.prefab));bool header=Regex.IsMatch(text,@"(?m)^--- !u!\d+ &"+Regex.Escape(change.targetId)+@"(?:\s|$)");
            if(originals.Length!=contract.objects||clones.Length!=contract.objects||missing!=0||header||originals.Any(o=>AssemblyChainIdentityMatches(o,change.targetGuid,change.targetId))||clones.Any(o=>AssemblyChainIdentityMatches(o,change.targetGuid,change.targetId))||ResolveArchitectureSourceTargets(item.source,change.targetGuid,change.targetId,out _).Length!=0)throw new InvalidOperationException("The individually audited original target is no longer absent from the complete native current/original/generated/inherited/serialized nested hierarchy.");
            observation.state="EXACT_AUDITED_ABSENT_ARCHITECTURE_EDITOR_METADATA_PRESERVED_RAW_NO_WRITE";observation.actualType="EXACT_NATIVE_ABSENT_ORIGINAL_METADATA_TARGET";
            observation.appliedEvidence=new AssemblyOverrideEvidence{instance=item.row.sceneInstanceId,targetGuid=change.targetGuid,targetId=change.targetId,property=change.property,rawValue=raw,state=observation.state,nativeIdentityMethod="V309_PINNED_LAYOUT_INSTANCE_FIELD_RAW_VALUE_SOURCE_SHA_AND_COMPLETE_NATIVE_ALIAS_ABSENCE",sourceObjectCount=originals.Length,instanceObjectCount=clones.Length,missingScripts=0,targetMatches=0,applied=false};return true;
        }
        [Serializable]public sealed class ArchitectureAbsentMetadataControls
        {public bool exactObservedRecordAccounted,changedRawValueRejected,changedInstanceRejected,changedTargetRejected,changedSourceHashRejected,changedMetadataHashRejected,geometryRecordRejected,unknownPropertyRejected,sourceBytesPreserved,temporaryPreviewDestroyed;}
        public static string InspectAuditedAbsentMetadataRejectionControlsWithoutSourceWrites()
        {
            string path=ArchitectureLayoutPaths[2];if(AssetSha(path)!=ArchitectureLayoutHashes[2])throw new InvalidOperationException("Exact pinned architecture recipe required for controls.");var layout=JsonUtility.FromJson<ArchitectureGeometryLayout>(File.ReadAllText(Io(path)));var pair=layout.instances.SelectMany(r=>r.modifications.Select(c=>new{row=r,change=c})).First(p=>ExactArchitectureAbsentMetadata.ContainsKey(p.row.sceneInstanceId+"/"+p.change.targetGuid+"/"+p.change.targetId+"/"+p.change.property));
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(pair.row.prefab);string before=AssetSha(pair.row.prefab);var scene=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();GameObject clone=null;var proof=new ArchitectureAbsentMetadataControls();
            try
            {
                clone=(GameObject)PrefabUtility.InstantiatePrefab(source,scene);var item=new ArchitectureProbeInstance{row=pair.row,source=source,instance=clone,ensemble=new ArchitectureEnsembleGeometry{layout=path,layoutSha256=ArchitectureLayoutHashes[2]}};
                ArchitectureModification Change()=>JsonUtility.FromJson<ArchitectureModification>(JsonUtility.ToJson(pair.change));
                ArchitectureInstanceSource Row()=>JsonUtility.FromJson<ArchitectureInstanceSource>(JsonUtility.ToJson(pair.row));
                bool Attempt(ArchitectureModification change,bool geometry=false)=>ObserveAuditedExactArchitectureAbsentMetadata(item,change,new ArchitectureTargetObservation{geometryField=geometry});
                proof.exactObservedRecordAccounted=Attempt(pair.change);var field=Change();field.sourceValue="UNAPPROVED_RAW_VALUE";proof.changedRawValueRejected=!Attempt(field);
                item.row=Row();item.row.sceneInstanceId="UNAPPROVED_INSTANCE";proof.changedInstanceRejected=!Attempt(pair.change);item.row=pair.row;
                field=Change();field.targetId="123456789";proof.changedTargetRejected=!Attempt(field);
                item.row=Row();item.row.sourceSha256=new string('0',64);try{Attempt(pair.change);}catch(InvalidOperationException){proof.changedSourceHashRejected=true;}item.row=pair.row;
                item.row=Row();item.row.sourceMetaSha256=new string('0',64);try{Attempt(pair.change);}catch(InvalidOperationException){proof.changedMetadataHashRejected=true;}item.row=pair.row;
                proof.geometryRecordRejected=!Attempt(pair.change,true);field=Change();field.property="m_LocalScale.x";proof.unknownPropertyRejected=!Attempt(field);
            }
            finally{if(clone!=null)UnityEngine.Object.DestroyImmediate(clone);UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);proof.temporaryPreviewDestroyed=clone==null;proof.sourceBytesPreserved=AssetSha(pair.row.prefab)==before;}
            if(!proof.exactObservedRecordAccounted||!proof.changedRawValueRejected||!proof.changedInstanceRejected||!proof.changedTargetRejected||!proof.changedSourceHashRejected||!proof.changedMetadataHashRejected||!proof.geometryRecordRejected||!proof.unknownPropertyRejected||!proof.sourceBytesPreserved||!proof.temporaryPreviewDestroyed)throw new InvalidOperationException("Exact85 absent-metadata controls failed: "+JsonUtility.ToJson(proof));return JsonUtility.ToJson(proof,true);
        }
        static bool ArchitectureExactAuditedAbsentMetadata(ArchitectureTargetObservation row)
            =>row!=null&&!row.geometryField&&!row.applied&&row.appliedEvidence!=null&&row.appliedEvidence.nativeIdentityMethod=="V309_PINNED_LAYOUT_INSTANCE_FIELD_RAW_VALUE_SOURCE_SHA_AND_COMPLETE_NATIVE_ALIAS_ABSENCE"&&row.state=="EXACT_AUDITED_ABSENT_ARCHITECTURE_EDITOR_METADATA_PRESERVED_RAW_NO_WRITE";
    }
}
#endif

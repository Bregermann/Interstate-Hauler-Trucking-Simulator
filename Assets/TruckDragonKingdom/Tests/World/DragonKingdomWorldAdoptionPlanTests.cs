#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine;
using NUnit.Framework;
public sealed class DragonKingdomWorldAdoptionPlanTests
{
    [Serializable]sealed class NativePlanPreflightEvidence
    {
        public string scope="TEST_ONLY_APPROVED_SOURCE_IMPORTS_BEFORE_READONLY_PLAN_SNAPSHOTS_NO_WORLD_GENERATION_OR_CANDIDATE_WRITES",stage,artistReceipt,artistSha256,assemblyReceipt,assemblySha256,nativeReceipt,nativeSha256;
        public int approvedArtistImports,artistProtectedProperties,artistConversions,approvedAssemblyImports,assemblyProtectedProperties,assemblyConversions,assemblyAddedProperties,assemblyMetadataChanges,protectedModels,sourceFilesWatched;
        public bool passed,candidateAssetsUnchanged,preparedBeforePlanAndNoWriteSnapshots;
    }
    static string PlanFileSha(string path){using(var file=File.OpenRead(path))using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(file)).Replace("-","").ToLowerInvariant();}
    static void PrepareValidatedPlanNativeImports(string stage)
    {
        string[] args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonOutput"),tagAt=Array.IndexOf(args,"-tsDragonWorldCandidate");Assert.That(at>=0&&at+1<args.Length&&tagAt>=0&&tagAt+1<args.Length,Is.True,"Explicit actual saved candidate and unique output required.");string output=Path.GetFullPath(args[at+1]),folder="Assets/TruckDragonKingdom/VisualCandidates/"+args[tagAt+1];Directory.CreateDirectory(output);
        var pins=new[]{"Shared Dragon Kingdom.prefab","DragonKingdomPreview.unity","Dragon Kingdom.asset","Integration Receipt.json"}.SelectMany(name=>new[]{folder+"/"+name,folder+"/"+name+".meta"}).Where(File.Exists).ToDictionary(path=>path,PlanFileSha);
        Assert.That(pins.ContainsKey(folder+"/Shared Dragon Kingdom.prefab")&&pins.ContainsKey(folder+"/Integration Receipt.json"),Is.True,"Preflight is not an authoring substitute.");
        var type=Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomRegionalWorldIntegration, Assembly-CSharp-Editor",true);object Field(object o,string name)=>o.GetType().GetField(name).GetValue(o);int Count(object o,string name)=>(int)Field(o,name);void Flag(object o,string name)=>Assert.That((bool)Field(o,name),Is.True,name);
        var evidence=new NativePlanPreflightEvidence{stage=stage};
        type.GetMethod("CaptureArtistSourcePreflight").Invoke(null,null);var artist=type.GetProperty("LastArtistSourcePreflight").GetValue(null);Flag(artist,"sourceBytesPreserved");Assert.That(Count(artist,"exactInstances"),Is.EqualTo(61));Assert.That(Count(artist,"inactiveOverrides"),Is.EqualTo(12));Assert.That(Count(artist,"appliedOverrides"),Is.Zero);var artistImports=Field(artist,"materialImports");Flag(artistImports,"passed");
        evidence.approvedArtistImports=Count(artistImports,"approvedNativeImports");evidence.artistProtectedProperties=Count(artistImports,"protectedProperties");evidence.artistConversions=Count(artistImports,"convertedProperties");Assert.That(evidence.approvedArtistImports,Is.EqualTo(19));Assert.That(evidence.artistProtectedProperties,Is.EqualTo(769));Assert.That(evidence.artistConversions,Is.EqualTo(2));evidence.artistReceipt=Path.Combine(output,stage+"-artist-source-preflight.json");File.WriteAllText(evidence.artistReceipt,JsonUtility.ToJson(artist,true));evidence.artistSha256=PlanFileSha(evidence.artistReceipt);
        type.GetMethod("CaptureExact51AssemblyMaterialProof").Invoke(null,null);var assembly=type.GetProperty("LastAssemblyMaterialEvidence").GetValue(null);Flag(assembly,"passed");evidence.approvedAssemblyImports=Count(assembly,"approvedNativeImports");evidence.assemblyProtectedProperties=Count(assembly,"protectedProperties");evidence.assemblyConversions=Count(assembly,"convertedProperties");evidence.assemblyAddedProperties=Count(assembly,"addedProperties");evidence.assemblyMetadataChanges=Count(assembly,"metadataChanges");Assert.That(evidence.approvedAssemblyImports,Is.EqualTo(51));Assert.That(evidence.assemblyProtectedProperties,Is.EqualTo(2544));Assert.That(evidence.assemblyConversions,Is.EqualTo(6));Assert.That(evidence.assemblyAddedProperties,Is.EqualTo(347));Assert.That(evidence.assemblyMetadataChanges,Is.EqualTo(358));Assert.That(Count(assembly,"exactNonmaterialDependencies"),Is.EqualTo(267));Assert.That(Count(assembly,"exactMetadataAndGuids"),Is.EqualTo(318));evidence.assemblyReceipt=Path.Combine(output,stage+"-exact51-material-preflight.json");File.WriteAllText(evidence.assemblyReceipt,JsonUtility.ToJson(assembly,true));evidence.assemblySha256=PlanFileSha(evidence.assemblyReceipt);
        var native=type.GetMethod("PrepareValidatedQuayNativeImports").Invoke(null,null);foreach(string flag in new[]{"passed","allUnknownChangesRejected","protectedModelIdentitiesExact","nonMaterialSourceBytesExact"})Flag(native,flag);evidence.protectedModels=Count(native,"protectedModels");evidence.sourceFilesWatched=Count(native,"sourceFilesWatched");Assert.That(evidence.protectedModels,Is.EqualTo(3));Assert.That(evidence.sourceFilesWatched,Is.GreaterThan(0));evidence.nativeReceipt=Path.Combine(output,stage+"-validated-native-import-preflight.json");File.WriteAllText(evidence.nativeReceipt,JsonUtility.ToJson(native,true));evidence.nativeSha256=PlanFileSha(evidence.nativeReceipt);
        evidence.candidateAssetsUnchanged=pins.All(pair=>File.Exists(pair.Key)&&PlanFileSha(pair.Key)==pair.Value);Assert.That(evidence.candidateAssetsUnchanged,Is.True,"Approved source imports must not modify the physically passed saved candidate.");evidence.preparedBeforePlanAndNoWriteSnapshots=evidence.passed=true;File.WriteAllText(Path.Combine(output,stage+"-plan-import-preflight.json"),JsonUtility.ToJson(evidence,true));
    }
    [Serializable]sealed class ActivationRow{public string branch;public bool savedAuthoredActive,liveActive;public int savedPrimary,livePrimary;public string[] savedRoles,liveRoles;}
    [Serializable]sealed class ActivationEvidence{public string scope="ACTUAL_SAVED_CANDIDATE_GRASS_PLUS_OWNED_PREFAB_LIVE_EQUIVALENCE_PRIMARY_LOD_AND_ALL_AUTHORED_ACTIVATION_FLAGS_NO_WORLD_ADOPTION",sourcePart,incomingSceneContext,afterSceneContext;public bool passed,sourceBytesExact,sceneContextRestored;public int actualSavedPrimary,originalSceneCount;public ActivationRow[] rows;}
    [Test,Timeout(300000)]public void SavedPrefabAndLiveInstancePreserveAuthoredPrimaryMeshActivationWithoutWrites()
    {
        PrepareValidatedPlanNativeImports("saved-live-activation");var args=Environment.GetCommandLineArgs();int tag=Array.IndexOf(args,"-tsDragonWorldCandidate"),outAt=Array.IndexOf(args,"-tsDragonOutput");string folder="Assets/TruckDragonKingdom/VisualCandidates/"+args[tag+1],prefab=folder+"/Shared Dragon Kingdom.prefab",output=Path.GetFullPath(args[outAt+1]);var asset=AssetDatabase.LoadAssetAtPath<GameObject>(prefab);Assert.That(asset,Is.Not.Null);
        var grass=asset.GetComponentsInChildren<Transform>(true).First(t=>t.name=="Artist source 4436662 P_ENV_PLANT_grass_village");Assert.That(EditorUtility.IsPersistent(grass),Is.True);var type=Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomRegionalWorldIntegration, Assembly-CSharp-Editor",true);const BindingFlags flags=BindingFlags.Static|BindingFlags.NonPublic;var geometry=type.GetMethod("RidgeGeometryBindings",flags);var filters=type.GetMethod("RidgePrimaryMeshFilters",flags);var active=type.GetMethod("RidgeGeometryObjectActive",flags);Assert.That(geometry,Is.Not.Null);Assert.That(filters,Is.Not.Null);Assert.That(active,Is.Not.Null);
        object Field(object o,string name)=>o.GetType().GetField(name).GetValue(o);
        int Primary(Transform t)=>((System.Collections.IEnumerable)filters.Invoke(null,new object[]{t})).Cast<MeshFilter>().Count();
        string[] Roles(Transform t)=>((Array)geometry.Invoke(null,new object[]{t,new Dictionary<string,string>()})).Cast<object>().Select(row=>string.Join("|",new[]{"key","type","shape","mesh","meshGuid","meshLocalId","meshSha256","meshMetaSha256","materialGuid","materialLocalId","enabled","trigger","primaryLod"}.Select(name=>Convert.ToString(Field(row,name))))).ToArray();
        var sources=grass.GetComponentsInChildren<MeshFilter>(true).Select(filter=>AssetDatabase.GetAssetPath(filter.sharedMesh)).Concat(new[]{prefab,folder+"/Integration Receipt.json"}).SelectMany(path=>new[]{path,path+".meta"}).Distinct().Where(File.Exists).ToDictionary(path=>path,PlanFileSha);
        var evidence=new ActivationEvidence{sourcePart=grass.name,actualSavedPrimary=Primary(grass),originalSceneCount=SceneManager.sceneCount};Assert.That(evidence.actualSavedPrimary,Is.EqualTo(2),"The actual reported saved grass retains its two authored primary mesh filters.");
        string Context()=>string.Join("|",Enumerable.Range(0,SceneManager.sceneCount).Select(i=>{var item=SceneManager.GetSceneAt(i);return item.handle+":"+item.path+":"+item.isDirty+":"+string.Join(",",item.GetRootGameObjects().Select(root=>root.GetInstanceID()).OrderBy(id=>id));}));evidence.incomingSceneContext=Context();
        string owned="Assets/TruckDragonKingdom/VisualCandidates/TemporaryNativeActivation-"+Guid.NewGuid().ToString("N");var previous=SceneManager.GetActiveScene();Scene scene=default;GameObject prototype=null,live=null;var rows=new List<ActivationRow>();
        try
        {
            scene=EditorSceneManager.NewPreviewScene();prototype=new GameObject("Owned actual source activation proof");SceneManager.MoveGameObjectToScene(prototype,scene);
            GameObject Clone(string name,Transform parent){var value=UnityEngine.Object.Instantiate(grass.gameObject,parent,false);value.name=name;value.SetActive(true);value.transform.localPosition=Vector3.zero;value.transform.localRotation=Quaternion.identity;value.transform.localScale=Vector3.one;foreach(var renderer in value.GetComponentsInChildren<MeshRenderer>(true))renderer.enabled=true;return value;}
            Clone("active",prototype.transform);var disabled=new GameObject("disabled ancestor");disabled.transform.SetParent(prototype.transform,false);Clone("disabled child",disabled.transform);disabled.SetActive(false);
            var rendererOff=Clone("disabled renderers",prototype.transform);foreach(var renderer in rendererOff.GetComponentsInChildren<MeshRenderer>(true))renderer.enabled=false;
            var lod=Clone("explicit primary and secondary LOD",prototype.transform);var renderers=lod.GetComponentsInChildren<MeshRenderer>(true);Assert.That(renderers.Length,Is.EqualTo(2));var group=lod.GetComponent<LODGroup>();if(group==null)group=lod.AddComponent<LODGroup>();Assert.That(group,Is.Not.Null,"Reuse the actual source LODGroup; never add a disallowed duplicate.");group.SetLODs(new[]{new LOD(.6f,new Renderer[]{renderers[0]}),new LOD(.1f,new Renderer[]{renderers[1]})});group.RecalculateBounds();
            Assert.That(AssetDatabase.CreateFolder("Assets/TruckDragonKingdom/VisualCandidates",Path.GetFileName(owned)),Is.Not.Empty);var saved=PrefabUtility.SaveAsPrefabAsset(prototype,owned+"/Actual source activation.prefab");Assert.That(saved,Is.Not.Null);UnityEngine.Object.DestroyImmediate(prototype);prototype=null;live=(GameObject)PrefabUtility.InstantiatePrefab(saved,scene);
            foreach(var name in new[]{"active","disabled ancestor/disabled child","disabled renderers","explicit primary and secondary LOD"})
            {
                var savedBranch=saved.transform.Find(name);var liveBranch=live.transform.Find(name);var row=new ActivationRow{branch=name,savedAuthoredActive=(bool)active.Invoke(null,new object[]{savedBranch}),liveActive=(bool)active.Invoke(null,new object[]{liveBranch}),savedPrimary=Primary(savedBranch),livePrimary=Primary(liveBranch),savedRoles=Roles(savedBranch),liveRoles=Roles(liveBranch)};rows.Add(row);CollectionAssert.AreEqual(row.savedRoles,row.liveRoles,name+" changed actual source mesh/collider/state/primary roles between asset and scene contexts.");Assert.That(row.savedAuthoredActive,Is.EqualTo(row.liveActive));Assert.That(row.savedPrimary,Is.EqualTo(row.livePrimary));int expected=name=="active"?2:name=="explicit primary and secondary LOD"?1:0;Assert.That(row.savedPrimary,Is.EqualTo(expected),name);
            }
            live.SetActive(false);Assert.That((bool)active.Invoke(null,new object[]{live.transform.Find("active")}),Is.False,"A genuinely disabled live ancestor must remain disabled.");Assert.That(Primary(live.transform.Find("active")),Is.Zero);evidence.passed=true;
        }
        finally
        {
            if(live!=null)UnityEngine.Object.DestroyImmediate(live);if(prototype!=null)UnityEngine.Object.DestroyImmediate(prototype);if(scene.IsValid()&&scene.isLoaded)EditorSceneManager.ClosePreviewScene(scene);if(AssetDatabase.IsValidFolder(owned))Assert.That(AssetDatabase.DeleteAsset(owned),Is.True,"Remove only the exact fixture-owned generated prefab folder.");evidence.sourceBytesExact=sources.All(pair=>File.Exists(pair.Key)&&PlanFileSha(pair.Key)==pair.Value);evidence.afterSceneContext=Context();evidence.sceneContextRestored=SceneManager.sceneCount==evidence.originalSceneCount&&SceneManager.GetActiveScene()==previous&&evidence.incomingSceneContext==evidence.afterSceneContext;evidence.rows=rows.ToArray();File.WriteAllText(Path.Combine(output,"saved-live-primary-activation.json"),JsonUtility.ToJson(evidence,true));Assert.That(evidence.sourceBytesExact&&evidence.sceneContextRestored,Is.True,"Native activation proof must preserve the original source/candidate bytes and every incoming scene.");
        }
    }
    [Test]public void ReadOnlyPlanMapsEveryNonobsoleteOriginalObjectWithoutReplacingCanonicalIdentities()
    {
        PrepareValidatedPlanNativeImports("identity-plan");
        var type=Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomWorldAdoptionPlan, Assembly-CSharp-Editor",true);type.GetMethod("CapturePlan").Invoke(null,null);
        var plan=type.GetProperty("LastPlan").GetValue(null);object Field(string n)=>plan.GetType().GetField(n).GetValue(plan);
        Assert.That((string)Field("status"),Is.EqualTo("MAPPED_ORIGINAL_IDENTITIES_REQUIRES_VISUAL_COURSE_APPROVAL_AND_EXPLICIT_APPLICATION"));
        Assert.That((bool)Field("allRetainedOriginalObjectsMapped"),Is.True);Assert.That((int)Field("originalObjects"),Is.GreaterThan(500));Assert.That((int)Field("mappedOriginalObjects"),Is.GreaterThan(500));
        Assert.That(((Array)Field("obsolete")).Length,Is.EqualTo(5));int finish=(int)Field("ownedFinishRoots");Assert.That(finish,Is.EqualTo(0).Or.EqualTo(334).Or.EqualTo(335).Or.EqualTo(343));
        int artist=(int)Field("ownedArtistMarketRoots");Assert.That(artist,Is.EqualTo(finish==335||finish==343?1:0));
        int architecture=(int)Field("ownedArchitectureRoots"),schema=(int)Field("compositionSchema");Assert.That(architecture,Is.EqualTo(schema==4?5:0),"Only schema4 adds its exact four source frames and physical waterfront support root.");Assert.That(((Array)Field("retiredComponents")).Length,Is.EqualTo(schema==4?26:0),"Retirement preserves exactly26 original component identities; it never enlarges obsolete objects.");
        int mountain=(int)Field("ownedMountainRoots");Assert.That(mountain,Is.InRange(0,1));Assert.That((int)Field("retainedInactiveMountainRoots"),Is.EqualTo(mountain*4),"The one qualified physical road retains exactly four inactive original roots and all their object IDs.");
        int coast=(int)Field("ownedCoastalRoots");Assert.That(coast,Is.InRange(0,1));Assert.That(((Array)Field("additions")).Length-finish-coast-artist-architecture-mountain,Is.InRange(4,5),"Only exact measured source-backed Finish/coast/support and schema4 roots may extend windmill/Digger additions.");
        Assert.That((string)Field("canonicalGuid"),Is.Not.EqualTo((string)Field("candidateGuid")),"Planning never turns the separate review prefab into the canonical identity.");
    }
    [Test,Timeout(300000)]public void ActualSavedOuterPaletteRejectsMissingDuplicateOrDriftedCloneProvenanceWithoutWrites()
    {
        PrepareValidatedPlanNativeImports("outer-clone-guard");
        var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonWorldCandidate");Assert.That(at>=0&&at+1<args.Length,Is.True,"Actual newly saved candidate required; no synthetic world approval.");string folder="Assets/TruckDragonKingdom/VisualCandidates/"+args[at+1],prefab=folder+"/Shared Dragon Kingdom.prefab",receiptPath=folder+"/Integration Receipt.json";
        var planType=Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomWorldAdoptionPlan, Assembly-CSharp-Editor",true);var receiptType=Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomRegionalWorldIntegration+Receipt, Assembly-CSharp-Editor",true);var worldType=Type.GetType("LWS.TruckDragonKingdom.DragonKingdomWorld, Assembly-CSharp",true);
        string json=File.ReadAllText(receiptPath);object Read(object o,string name)=>o.GetType().GetField(name).GetValue(o);void Write(object o,string name,object value)=>o.GetType().GetField(name).SetValue(o,value);
        var actual=JsonUtility.FromJson(json,receiptType);Assert.That(Read(actual,"palette"),Is.Not.Null,"Actual current authored palette required for this focused guard.");var palette=Read(actual,"palette");var tiles=((Array)Read(palette,"tiles")).Cast<object>().ToArray();Assert.That(tiles.Count(t=>(bool)Read(t,"externalDataCloned")),Is.EqualTo(12));
        var saved=AssetDatabase.LoadAssetAtPath<GameObject>(prefab);Assert.That(saved,Is.Not.Null);var world=saved.GetComponent(worldType);var root=saved.transform.Find("Owned Vista coastal outskirts");Assert.That(root,Is.Not.Null);
        var watchedPaths=new[]{prefab,prefab+".meta",receiptPath}.Concat(tiles.SelectMany(t=>new[]{(string)Read(t,"sourceData"),(string)Read(t,"candidate")})).SelectMany(path=>File.Exists(path+".meta")?new[]{path,path+".meta"}:new[]{path}).Distinct().OrderBy(path=>path,StringComparer.Ordinal).ToArray();
        var watchedFolders=watchedPaths.Select(path=>Path.GetFullPath(Path.GetDirectoryName(path))).Distinct().OrderBy(path=>path,StringComparer.Ordinal).ToArray();
        Dictionary<string,Tuple<long,string>> SnapshotFiles()
        {
            var result=new Dictionary<string,Tuple<long,string>>(StringComparer.Ordinal);
            foreach(string path in watchedPaths){using(var stream=File.OpenRead(path))using(var sha=SHA256.Create())result.Add(path,Tuple.Create(stream.Length,BitConverter.ToString(sha.ComputeHash(stream))));}return result;
        }
        Dictionary<string,string[]> SnapshotMembership()=>watchedFolders.ToDictionary(path=>path,path=>Directory.GetFiles(path).OrderBy(file=>file,StringComparer.Ordinal).ToArray(),StringComparer.Ordinal);
        var watched=SnapshotFiles();var membership=SnapshotMembership();
        void AssertNoWrites(string stage)
        {
            foreach(string folderPath in watchedFolders)Assert.That(Directory.GetFiles(folderPath).OrderBy(file=>file,StringComparer.Ordinal).ToArray(),Is.EqualTo(membership[folderPath]),stage+" changed actual saved-folder membership: "+folderPath);
            var after=SnapshotFiles();Assert.That(after.Keys,Is.EquivalentTo(watched.Keys),stage+" changed watched file membership.");
            foreach(var pair in watched){Assert.That(after[pair.Key].Item1,Is.EqualTo(pair.Value.Item1),stage+" changed actual saved-file length: "+pair.Key);Assert.That(after[pair.Key].Item2,Is.EqualTo(pair.Value.Item2),stage+" changed actual saved-file SHA256: "+pair.Key);}
        }
        var method=planType.GetMethod("ValidateCoastalAddition",BindingFlags.NonPublic|BindingFlags.Static);Assert.That(method,Is.Not.Null);
        // One authoritative full receipt and genuine positive validation. Never
        // deserialize131MB of unrelated group/trial evidence for each fault.
        method.Invoke(null,new object[]{new[]{root},world,actual});AssertNoWrites("Actual positive coastal validation");
        var narrow=Activator.CreateInstance(receiptType);Write(narrow,"tag",Read(actual,"tag"));Write(narrow,"outerBand",Read(actual,"outerBand"));Write(narrow,"palette",palette);
        var fullFinish=Read(actual,"finish");var finishType=receiptType.GetField("finish").FieldType;var narrowFinish=Activator.CreateInstance(finishType);Write(narrowFinish,"compositionSchema",Read(fullFinish,"compositionSchema"));Write(narrow,"finish",narrowFinish);
        string narrowJson=JsonUtility.ToJson(narrow);Assert.That(narrowJson.Length,Is.LessThan(json.Length),"Fault payload must exclude unrelated authoritative placement histories.");actual=null;json=null;fullFinish=null;
        foreach(string fault in new[]{"missing-palette","missing-tile","duplicate-source","source-guid","source-hash","clone-guid"})
        {
            var broken=JsonUtility.FromJson(narrowJson,receiptType);var copy=Read(broken,"palette");var array=(Array)Read(copy,"tiles");var clone=array.Cast<object>().First(t=>(bool)Read(t,"externalDataCloned"));
            if(fault=="missing-palette")Write(broken,"palette",null);
            else if(fault=="missing-tile"){var shorter=Array.CreateInstance(array.GetType().GetElementType(),array.Length-1);Array.Copy(array,shorter,shorter.Length);Write(copy,"tiles",shorter);}
            else if(fault=="duplicate-source")Write(clone,"sourceData",(string)Read(array.Cast<object>().Last(t=>(bool)Read(t,"externalDataCloned")),"sourceData"));
            else Write(clone,fault=="source-guid"?"sourceDataGuid":fault=="source-hash"?"sourceDataSha256":"candidateGuid",new string('0',fault=="source-hash"?64:32));
            var failure=Assert.Throws<TargetInvocationException>(()=>method.Invoke(null,new object[]{new[]{root},world,broken}),fault);Assert.That(failure.InnerException,Is.TypeOf<InvalidOperationException>(),fault);
            AssertNoWrites("Actual clone rejection "+fault);
        }
        TestContext.WriteLine("ACTUAL_OUTER_PALETTE_GUARD twelveSourceClonePairs=true missingDuplicateAndDriftRejected=true savedShaLengthAndMembershipExact=true fullReceiptDeserializations=1 faults=6 noAdoptionPerformed=true");
    }

}
#endif

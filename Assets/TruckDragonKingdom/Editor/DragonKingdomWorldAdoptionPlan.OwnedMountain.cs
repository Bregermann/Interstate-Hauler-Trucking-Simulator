#if UNITY_EDITOR && VISTA
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using LWS.InterstateHauler;
using UnityEditor;
using UnityEngine;

namespace LWS.TruckDragonKingdom.Editor
{
    public static partial class DragonKingdomWorldAdoptionPlan
    {
        const string MountainRoot="Kingdom caverns-volcano · qualified saved physical authority";
        static readonly string[] MountainRetiredNames={"Kingdom caverns-volcano · EasyRoads generated course","Kingdom hairpin caverns-volcano 6 · EasyRoads generated course","Kingdom hairpin caverns-volcano 9 · EasyRoads generated course","Kingdom hairpin caverns-volcano 12 · EasyRoads generated course"};
        static readonly long[] MountainRetiredIds={3637298366336087090L,4520377880275211363L,8847699847918239384L,1382603855827951494L};
        const string UndergroundPad="Kingdom hairpin caverns-volcano 2 · EasyRoads generated course";
        [Serializable]sealed class MountainNativeReceipt{public string status,error,layoutPath,layoutSha256,reloadedPointsSha256;public MountainNativeAuthoring authoring;}
        [Serializable]sealed class MountainNativeAuthoring{public bool originalSourcesExact,randomStateRestored;}
        [Serializable]sealed class MountainArchive{public string status;public bool privateOnly;public DragonKingdomRegionalWorldIntegration.OwnedMountainSource[] files;}

        // A bounded road-only receipt can never authorize this adoption branch.
        static void RequirePassedMountainWorld(DragonKingdomRegionalWorldIntegration.Receipt proof)
        {
            if(proof==null||proof.status!="PHYSICAL_GATES_PASSED_REQUIRES_RENDER_AND_ACTUAL_DRIVING_REVIEW"||!string.IsNullOrEmpty(proof.error)||!proof.sourceBytesPreserved||!proof.originalPhysicalGatesPassed||!proof.candidateRoadAuthorityAndCriticalPosesExact)
                throw new InvalidOperationException("Owned-road adoption requires the genuinely passed full-world/source/critical-pose receipt; a bounded road proof is insufficient.");
        }
        static void RequireActualPassedMountainWorld(DragonKingdomRegionalWorldIntegration.Receipt proof)
        {
            RequirePassedMountainWorld(proof);string evidence=Arg("-tsDragonOwnedMountainWorldEvidence","");string xmlPath=Path.Combine(evidence,"tests.xml"),nativePath=Path.Combine(evidence,"rendered-evidence","regional-world-"+proof.tag+".json"),savedPath=Root+"VisualCandidates/"+proof.tag+"/Integration Receipt.json";
            if(string.IsNullOrEmpty(evidence)||!File.Exists(xmlPath)||!File.Exists(nativePath)||!File.Exists(savedPath))throw new InvalidOperationException("Actual passed full-world native run and saved receipt required through -tsDragonOwnedMountainWorldEvidence.");
            var xml=new XmlDocument();xml.Load(xmlPath);var cases=xml.SelectNodes("//test-case").Cast<XmlNode>().Where(n=>n.Attributes["fullname"]?.Value=="DragonKingdomOwnedMountainIntegrationTests.ExplicitQualifiedMountainProfileRetainsExistingFullWorldAcceptanceGates").ToArray();
            if(cases.Length!=1||cases[0].Attributes["result"]?.Value!="Passed"||MountainFileDigest(nativePath)!=MountainFileDigest(savedPath)||JsonUtility.ToJson(JsonUtility.FromJson<DragonKingdomRegionalWorldIntegration.Receipt>(File.ReadAllText(nativePath)))!=JsonUtility.ToJson(proof)||proof.prefab!=Root+"VisualCandidates/"+proof.tag+"/Shared Dragon Kingdom.prefab"||proof.preview!=Root+"VisualCandidates/"+proof.tag+"/DragonKingdomPreview.unity"||proof.definition!=Root+"VisualCandidates/"+proof.tag+"/Dragon Kingdom.asset")throw new InvalidOperationException("Owned-road adoption requires the actual passed full-world test and identical native/saved source-bound receipt.");
        }
        static Transform ValidateOwnedMountainAdoption(DragonKingdomWorld original,DragonKingdomWorld candidate,DragonKingdomRegionalWorldIntegration.Receipt proof,bool allowAppliedDefinition)
        {
            var road=proof?.ownedMountainRoad;if(road==null)return null;
            RequirePassedMountainWorld(proof);RequireActualPassedMountainWorld(proof);
            if(road.profile!="CircularMountainPass01"||!string.IsNullOrEmpty(road.error)||road.points!=646||road.physicalPieces<1||road.physicalQueries!=1920||road.physicalMisses!=0||!float.IsFinite(road.maximumPhysicalHeightError)||road.maximumPhysicalHeightError>.3f||road.navigationQueries!=5||!road.intentionalTargetDelta||!road.otherEdgesAndNodesExact||!road.prefixAndSemanticEndpointsExact||!road.physicalAuthorityExact||!road.savedAuthorityExact||!road.sourceBytesPreserved||!road.legacySelectionRestored||!road.randomStateRestored||road.gradeInput==null||!road.gradeInput.exact||!road.gradeInput.nativePhysicalMeshMatchesSaved||!road.gradeInput.nativeWorldPosesMatchSaved||road.gradeInput.markers!=646||road.gradeInput.authoritativePointsSha256!=road.pointsSha256||road.gradeInput.actualSdkInputPointsSha256!=road.pointsSha256)
                throw new InvalidOperationException("Complete exact646 saved road, physical/SDK/navigation/restoration evidence required.");
            var layout=RequireMountainSources(road);
            ValidateMountainDefinitionDelta(original.definition,candidate.definition,layout,allowAppliedDefinition);
            string currentGraph=MountainDigest(JsonUtility.ToJson(original.definition.graph));
            if((currentGraph!=road.originalGraphSha256&&(!allowAppliedDefinition||currentGraph!=road.candidateGraphSha256))||MountainDigest(JsonUtility.ToJson(candidate.definition.graph))!=road.candidateGraphSha256||MountainDigest(JsonUtility.ToJson(candidate.definition.graph.edges.Single(e=>e.edgeId==layout.roadId)))!=road.candidateTargetEdgeSha256)
                throw new InvalidOperationException("Actual original/candidate graph differs from the passed full-world road receipt.");
            if(currentGraph==road.originalGraphSha256&&MountainDigest(JsonUtility.ToJson(original.definition.graph.edges.Single(e=>e.edgeId==layout.roadId)))!=road.sourceTargetEdgeSha256)
                throw new InvalidOperationException("Original target edge differs from source-bound adoption evidence.");
            var roots=candidate.transform.Cast<Transform>().Where(t=>t.name==MountainRoot).ToArray();if(roots.Length!=1)throw new InvalidOperationException("Exactly one root-level qualified physical mountain authority required.");var root=roots[0];
            if(!root.gameObject.activeSelf||root.GetComponents<Component>().Length!=1||!root.position.Equals(Vector3.zero)||!root.rotation.Equals(Quaternion.identity)||!root.lossyScale.Equals(Vector3.one)||root.childCount!=layout.physicalPieces.Length||road.physicalPieces!=layout.physicalPieces.Length)
                throw new InvalidOperationException("Qualified physical root frame/membership differs from saved native layout.");
            var originalMain=original.GetComponentsInChildren<Transform>(true).Single(t=>t.name==MountainRetiredNames[0]);var material=originalMain.GetComponentsInChildren<MeshRenderer>(true).Select(r=>r.sharedMaterial).First(m=>m!=null);var contact=originalMain.GetComponentsInChildren<MeshCollider>(true).Select(c=>c.sharedMaterial).FirstOrDefault(m=>m!=null);
            for(int i=0;i<root.childCount;i++)
            {
                var t=root.GetChild(i);var piece=layout.physicalPieces[i];var f=t.GetComponent<MeshFilter>();var r=t.GetComponent<MeshRenderer>();var c=t.GetComponent<MeshCollider>();
                if(t.childCount!=0||t.GetComponents<Component>().Length!=4||!t.gameObject.activeSelf||t.name!=piece.nativeSource||!t.position.Equals(piece.position)||!t.rotation.Equals(piece.rotation)||!t.lossyScale.Equals(piece.scale)||f==null||f.sharedMesh!=piece.mesh||r==null||!r.enabled||r.sharedMaterials.Length!=1||r.sharedMaterial!=material||c==null||!c.enabled||c.isTrigger||c.convex||c.sharedMesh!=piece.mesh||c.sharedMaterial!=contact)
                    throw new InvalidOperationException("Saved mountain render/collider/source material/world matrix changed at native piece"+i);
            }
            RequireMountainCriticalPoses(original,candidate);
            return root;
        }
        static DragonKingdomMountainRoadLayout RequireMountainSources(DragonKingdomRegionalWorldIntegration.OwnedMountainRoadEvidence road)
        {
            string xmlPath=Path.Combine(road.nativeEvidence??"","tests.xml"),nativePath=Path.Combine(road.nativeEvidence??"","rendered-evidence","actual-owned-mountain-road-layout.json"),archivePath=Arg("-tsDragonOwnedMountainRoadArchiveEvidence","");
            if(!File.Exists(xmlPath)||!File.Exists(nativePath)||!File.Exists(archivePath)||MountainFileDigest(xmlPath)!=road.nativeTestsSha256||MountainFileDigest(nativePath)!=road.nativeReceiptSha256||MountainFileDigest(archivePath)!=road.archiveSha256)throw new InvalidOperationException("Exact recorded native road and explicit private archive source receipts required.");
            var xml=new XmlDocument();xml.Load(xmlPath);var tests=xml.SelectNodes("//test-case").Cast<XmlNode>().Where(n=>n.Attributes["fullname"]?.Value=="DragonKingdomOwnedMountainRoadTests.ActualVendorMountainRoutePreservesCavePrefixAndBakesOneSavedPhysicalAuthority").ToArray();
            var native=JsonUtility.FromJson<MountainNativeReceipt>(File.ReadAllText(nativePath));var archive=JsonUtility.FromJson<MountainArchive>(File.ReadAllText(archivePath));
            if(tests.Length!=1||tests[0].Attributes["result"]?.Value!="Passed"||native==null||native.status!="ACTUAL_VENDOR_ROUTE_MESH_AND_SAVED_AUTHORITY_PASSED_TERRAIN_AND_WORLD_PENDING"||!string.IsNullOrEmpty(native.error)||native.authoring==null||!native.authoring.originalSourcesExact||!native.authoring.randomStateRestored||native.layoutPath!=road.layout||native.layoutSha256!=road.layoutSha256||native.reloadedPointsSha256!=road.pointsSha256||archive==null||!archive.privateOnly||archive.status!="ROAD_GEOMETRY_AND_SAVED_AUTHORITY_QUALIFIED_TERRAIN_WORLD_AND_CANONICAL_ADOPTION_PENDING")throw new InvalidOperationException("Actual passed native road/archive provenance no longer qualifies.");
            var layout=AssetDatabase.LoadAssetAtPath<DragonKingdomMountainRoadLayout>(road.layout);if(layout==null||MountainFileDigest(road.layout)!=road.layoutSha256||layout.authoritativePointsSha256!=road.pointsSha256||layout.sdkSha256!=MountainFileDigest("Assets/EasyRoads3D/lib/EasyRoads3Dv3.dll"))throw new InvalidOperationException("Qualified layout/native SDK bytes changed.");DragonKingdomMountainRoadLayoutAuthoring.ValidateLayout(layout);
            string folder=Path.GetDirectoryName(road.layout).Replace('\\','/')+"/";var expected=AssetDatabase.GetDependencies(road.layout,true).Where(p=>p==road.layout||p.StartsWith(folder,StringComparison.Ordinal)).SelectMany(p=>new[]{p,p+".meta"}).Where(File.Exists).Distinct().OrderBy(p=>p,StringComparer.Ordinal).ToArray();
            if(road.protectedSources==null||!expected.SequenceEqual(road.protectedSources.Select(p=>p.path).OrderBy(p=>p,StringComparer.Ordinal))||road.protectedSources.Any(p=>MountainFileDigest(p.path)!=p.sha256||archive.files.Count(a=>a.path==p.path&&a.sha256==p.sha256)!=1))throw new InvalidOperationException("Every qualified layout/mesh/meta dependency must retain exact native and archive bytes.");return layout;
        }
        static void ValidateMountainDefinitionDelta(DragonKingdomDefinition original,DragonKingdomDefinition candidate,DragonKingdomMountainRoadLayout layout,bool allowApplied)
        {
            if(original==null||candidate==null||layout==null||layout.roadId!="caverns-volcano"||layout.from!="caverns"||layout.to!="volcano"||layout.authoritativePoints==null||layout.authoritativePoints.Length!=646||layout.preservedPrefix==null||layout.preservedPrefix.Length!=6||!layout.authoritativePoints.Take(6).SequenceEqual(layout.preservedPrefix)||DragonKingdomMountainRoadLayoutAuthoring.PointsDigest(layout.authoritativePoints)!=layout.authoritativePointsSha256)throw new InvalidOperationException("Only the exact646-point cave-prefix single-target definition delta is supported.");
            var old=original.graph.edges.Single(e=>e.edgeId==layout.roadId);var actual=candidate.graph.edges.Single(e=>e.edgeId==layout.roadId);var expected=LwsRoadGraph.FromJson(original.graph.ToJson());var target=expected.edges.Single(e=>e.edgeId==layout.roadId);
            if(old.fromNodeId!=layout.from||old.toNodeId!=layout.to||old.samples.Count<2||old.samples[0].position!=layout.authoritativePoints[0]||old.samples.Last().position!=layout.sourceEndpoint||layout.authoritativePoints[0]!=original.graph.nodes.Single(n=>n.nodeId==layout.from).position||layout.authoritativePoints[645]!=original.graph.nodes.Single(n=>n.nodeId==layout.to).position||Mathf.Abs(old.laneCount*old.laneWidthMeters+old.leftShoulderWidthMeters+old.rightShoulderWidthMeters-layout.width)>.001f)throw new InvalidOperationException("Original semantic endpoints/road width changed; the six-point source-specification prefix remains pinned by the qualified layout.");
            if(!allowApplied&&old.samples.Count==646&&DragonKingdomMountainRoadLayoutAuthoring.PointsDigest(old.samples.Select(s=>s.position).ToArray())==layout.authoritativePointsSha256)throw new InvalidOperationException("Planning requires the original pre-adoption target graph.");
            target.samples.Clear();float distance=0;for(int i=0;i<646;i++){var point=layout.authoritativePoints[i];if(i>0)distance+=Vector3.Distance(layout.authoritativePoints[i-1],point);target.samples.Add(new LwsRoadSample{roadId=target.roadId,segmentId=target.segmentId,position=point,forward=(layout.authoritativePoints[Math.Min(i+1,645)]-layout.authoritativePoints[Math.Max(0,i-1)]).normalized,up=Vector3.up,distanceFromStartMeters=distance,direction=target.direction,roadWidthMeters=layout.width,laneWidthMeters=target.laneWidthMeters,laneCount=target.laneCount,speedLimitMph=target.speedLimitMph});}target.distanceMeters=target.travelCost=distance;
            if(JsonUtility.ToJson(expected)!=JsonUtility.ToJson(candidate.graph)||!candidate.graph.Validate().IsValid)throw new InvalidOperationException("Only target samples/distance/travelCost may change; all other edge/node semantics and ordering must remain exact.");
            var normalized=UnityEngine.Object.Instantiate(candidate);var source=UnityEngine.Object.Instantiate(original);try{normalized.name=source.name;normalized.graph=LwsRoadGraph.FromJson(original.graph.ToJson());foreach(var r in normalized.regions)r.bounds=new Bounds();foreach(var r in source.regions)r.bounds=new Bounds();if(JsonUtility.ToJson(normalized)!=JsonUtility.ToJson(source))throw new InvalidOperationException("Single-road adoption changed region IDs/centers, extraction/closure rules or other definition fields.");}finally{UnityEngine.Object.DestroyImmediate(normalized);UnityEngine.Object.DestroyImmediate(source);}
        }
        static void ApplyQualifiedOwnedMountainDefinition(DragonKingdomDefinition original,DragonKingdomDefinition candidate,DragonKingdomRegionalWorldIntegration.Receipt proof)
        {
            RequirePassedMountainWorld(proof);var layout=RequireMountainSources(proof.ownedMountainRoad);ValidateMountainDefinitionDelta(original,candidate,layout,false);
            var replacement=LwsRoadGraph.FromJson(candidate.graph.ToJson()).edges.Single(e=>e.edgeId==layout.roadId);int index=original.graph.edges.FindIndex(e=>e.edgeId==layout.roadId);original.graph.edges[index]=replacement;
            if(MountainDigest(JsonUtility.ToJson(original.graph))!=proof.ownedMountainRoad.candidateGraphSha256)throw new InvalidOperationException("Canonical single-target graph copy differs from exact approved candidate.");
        }
        static void AuthorizeOwnedMountainRetirements(Mapping map,DragonKingdomWorld original,DragonKingdomWorld candidate,DragonKingdomRegionalWorldIntegration.Receipt proof,bool allowAlreadyRetired)
        {
            ValidateOwnedMountainAdoption(original,candidate,proof,allowAlreadyRetired);var rows=proof.ownedMountainRoad.retired;string guid=AssetDatabase.AssetPathToGUID(CanonicalPrefab);var canonical=AssetDatabase.LoadAssetAtPath<GameObject>(CanonicalPrefab);
            if(rows==null||rows.Count!=4||rows.Select(r=>r.name).Distinct().Count()!=4||proof.ownedMountainRoad.preservedUndergroundPads==null||!proof.ownedMountainRoad.preservedUndergroundPads.SequenceEqual(new[]{UndergroundPad}))throw new InvalidOperationException("Exactly four known aboveground road retirements and unchanged cave pad2 required.");
            for(int i=0;i<4;i++)
            {
                var row=rows.Single(r=>r.name==MountainRetiredNames[i]);var a=original.GetComponentsInChildren<Transform>(true).Single(t=>t.name==row.name);var b=candidate.GetComponentsInChildren<Transform>(true).Single(t=>t.name==row.name);var source=canonical.GetComponentsInChildren<Transform>(true).Single(t=>t.name==row.name);
                if(row.sourcePrefab!=CanonicalPrefab||row.sourceGuid!=guid||row.sourceFileId!=MountainRetiredIds[i]||!row.beforeActive||row.afterActive||!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(source.gameObject,out string sourceGuid,out long id)||sourceGuid!=guid||id!=row.sourceFileId||(!allowAlreadyRetired&&!a.gameObject.activeSelf)||b.gameObject.activeSelf||row.renderers!=a.GetComponentsInChildren<Renderer>(true).Length||row.colliders!=a.GetComponentsInChildren<Collider>(true).Length||!map.candidateToOriginal.TryGetValue(b,out var mapped)||mapped!=a)throw new InvalidOperationException("Unreviewed road retirement identity/state/count; every original object must remain mapped.");
                RequireMountainSubtreeExact(a,b,original.transform,candidate.transform,true);map.mountainRetirements.Add(b,a);
            }
            foreach(var pair in map.candidateToOriginal)if(pair.Key is GameObject current&&pair.Value is GameObject old&&current.activeSelf!=old.activeSelf&&!map.mountainRetirements.Keys.Any(t=>t.gameObject==current))throw new InvalidOperationException("Unreviewed existing GameObject active-state change outside four preserved road roots: "+current.name);
            var oldPad=original.GetComponentsInChildren<Transform>(true).Single(t=>t.name==UndergroundPad);var newPad=candidate.GetComponentsInChildren<Transform>(true).Single(t=>t.name==UndergroundPad);RequireMountainSubtreeExact(oldPad,newPad,original.transform,candidate.transform,false);
        }
        static void RequireMountainCriticalPoses(DragonKingdomWorld original,DragonKingdomWorld candidate)
        {
            void Pairs(Transform[] a,Transform[] b){if(a==null||b==null||a.Length!=b.Length)throw new InvalidOperationException("Original critical anchor membership changed.");for(int i=0;i<a.Length;i++)if(a[i]==null||b[i]==null||PathOf(a[i],original.transform)!=PathOf(b[i],candidate.transform)||!a[i].position.Equals(b[i].position)||!a[i].rotation.Equals(b[i].rotation)||!a[i].lossyScale.Equals(b[i].lossyScale))throw new InvalidOperationException("Owned road changed an original critical anchor/reference.");}
            void References(Transform[] a,Transform[] b){if(a==null||b==null||a.Length!=b.Length||Enumerable.Range(0,a.Length).Any(i=>a[i]==null||b[i]==null||PathOf(a[i],original.transform)!=PathOf(b[i],candidate.transform)))throw new InvalidOperationException("Original region anchor identity/order changed.");}
            if(!original.regions.Select(r=>r.id).SequenceEqual(candidate.regions.Select(r=>r.id)))throw new InvalidOperationException("Original region identity/order changed.");foreach(var a in original.regions){var b=candidate.Region(a.id);Pairs(new[]{a.root},new[]{b.root});Pairs(a.truckStarts,b.truckStarts);References(a.perches,b.perches);References(a.nests,b.nests);References(a.treasureCaches,b.treasureCaches);}Pairs(original.riderStarts,candidate.riderStarts);Pairs(original.aerialCheckpoints,candidate.aerialCheckpoints);
            // Match the existing integration CriticalTransforms authority.
            // Nest seating and loose scenery remain governed by their existing
            // measured migrations rather than an invented immobility rule.
            var current=candidate.GetComponentsInChildren<Transform>(true).ToDictionary(t=>PathOf(t,candidate.transform),StringComparer.Ordinal);foreach(var t in original.GetComponentsInChildren<Transform>(true).Where(t=>!original.regions.Any(r=>t.IsChildOf(r.root)))){if(!current.TryGetValue(PathOf(t,original.transform),out var other))throw new InvalidOperationException("Original critical world transform disappeared.");Pairs(new[]{t},new[]{other});}
            if(!original.extractionSites.Select(e=>e.id).SequenceEqual(candidate.extractionSites.Select(e=>e.id)))throw new InvalidOperationException("Original extraction binding identity/order changed.");for(int i=0;i<original.extractionSites.Length;i++){var a=original.extractionSites[i];var b=candidate.extractionSites[i];Pairs(new[]{a.approach,a.secureZone.transform},new[]{b.approach,b.secureZone.transform});if((a.vessel==null)!=(b.vessel==null))throw new InvalidOperationException("Original evacuation vessel binding changed.");if(a.vessel!=null)Pairs(new[]{a.vessel.transform},new[]{b.vessel.transform});}
        }
        static void RequireMountainSubtreeExact(Transform original,Transform candidate,Transform oldWorld,Transform newWorld,bool retireRoot)
        {
            string Token(UnityEngine.Object value,Transform world){if(value==null)return "null";Transform t=value is GameObject go?go.transform:value is Component c?c.transform:null;if(t!=null&&(t==world||t.IsChildOf(world)))return (value is GameObject?"GO:":"component:"+value.GetType().FullName+":"+Array.IndexOf(t.GetComponents(value.GetType()),value)+":")+PathOf(t,world);if(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(value,out string guid,out long id))return guid+":"+id;throw new InvalidOperationException("Unknown road subtree external reference.");}
            var aa=original.GetComponentsInChildren<Transform>(true);var bb=candidate.GetComponentsInChildren<Transform>(true);if(aa.Length!=bb.Length)throw new InvalidOperationException("Retained road subtree membership changed.");
            for(int i=0;i<aa.Length;i++)
            {
                if(PathOf(aa[i],oldWorld)!=PathOf(bb[i],newWorld))throw new InvalidOperationException("Retained road subtree path/order changed.");var a=new UnityEngine.Object[]{aa[i].gameObject}.Concat(aa[i].GetComponents<Component>()).ToArray();var b=new UnityEngine.Object[]{bb[i].gameObject}.Concat(bb[i].GetComponents<Component>()).ToArray();if(a.Length!=b.Length)throw new InvalidOperationException("Retained road component membership changed.");
                for(int j=0;j<a.Length;j++){if(a[j]==null||b[j]==null||a[j].GetType()!=b[j].GetType())throw new InvalidOperationException("Retained road component type changed.");var x=new SerializedObject(a[j]).GetIterator();var y=new SerializedObject(b[j]).GetIterator();bool descend=true;while(x.Next(descend)){if(!y.Next(descend)||x.propertyPath!=y.propertyPath||x.propertyType!=y.propertyType)throw new InvalidOperationException("Retained road serialized layout changed.");descend=true;if(new[]{"m_CorrespondingSourceObject","m_PrefabInstance","m_PrefabAsset"}.Any(p=>x.propertyPath==p||x.propertyPath.StartsWith(p+".",StringComparison.Ordinal))){descend=false;continue;}if(retireRoot&&i==0&&j==0&&x.propertyPath=="m_IsActive")continue;if(x.propertyType==SerializedPropertyType.Generic)continue;bool equal=x.propertyType==SerializedPropertyType.ObjectReference?Token(x.objectReferenceValue,oldWorld)==Token(y.objectReferenceValue,newWorld):SerializedProperty.DataEquals(x,y);if(x.propertyType==SerializedPropertyType.ObjectReference)descend=false;if(!equal)throw new InvalidOperationException("Retained road authored property changed: "+aa[i].name+"/"+x.propertyPath);}if(y.Next(descend))throw new InvalidOperationException("Extra retained road serialized property.");}
            }
        }
        static string MountainDigest(string text)=>DragonKingdomMountainRoadLayoutAuthoring.Digest(System.Text.Encoding.UTF8.GetBytes(text));
        static string MountainFileDigest(string path)=>DragonKingdomMountainRoadLayoutAuthoring.Digest(File.ReadAllBytes(path));
    }
}
#endif

#if UNITY_EDITOR && VISTA
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using EasyRoads3Dv3;
using LWS.InterstateHauler;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LWS.TruckDragonKingdom.Editor
{
    public static partial class DragonKingdomRegionalWorldIntegration
    {
        const string OwnedMountainFlag="-tsDragonOwnedMountainRoad",OwnedMountainProfile="CircularMountainPass01";
        [Serializable]sealed class QualifiedOwnedMountainNative{public string status,error,layoutPath,layoutSha256,reloadedPointsSha256;public OwnedMountainAuthoring authoring;}
        [Serializable]sealed class OwnedMountainAuthoring{public bool originalSourcesExact,randomStateRestored;}
        [Serializable]sealed class OwnedMountainArchive{public string status;public bool privateOnly;public OwnedMountainSource[] files;}
        [Serializable]public sealed class OwnedMountainSource{public string path,sha256;}
        [Serializable]public sealed class OwnedMountainRetirement{public string name,sourcePrefab,sourceGuid;public long sourceFileId;public bool beforeActive,afterActive;public int renderers,colliders;}
        [Serializable]public sealed class OwnedMountainRoadEvidence
        {
            public string scope="EXPLICIT_QUALIFIED_SINGLE_OWNED_ROAD_REPLACEMENT_NOT_ORIGINAL_TARGET_GRAPH_IDENTITY_OR_TERRAIN_WORLD_ACCEPTANCE",profile=OwnedMountainProfile,nativeEvidence,nativeTestsSha256,nativeReceiptSha256,archiveSha256,layout,layoutSha256,pointsSha256,originalGraphSha256,candidateGraphSha256,sourceTargetEdgeSha256,candidateTargetEdgeSha256,error;
            public int points,physicalPieces,physicalQueries,physicalMisses,navigationQueries;public float maximumPhysicalHeightError;public bool intentionalTargetDelta,otherEdgesAndNodesExact,prefixAndSemanticEndpointsExact,physicalAuthorityExact,savedAuthorityExact,sourceBytesPreserved,legacySelectionRestored,randomStateRestored;
            public DragonKingdomWorldAuthoring.OwnedMountainGradeInput gradeInput;public List<OwnedMountainRetirement> retired=new List<OwnedMountainRetirement>();public string[] preservedUndergroundPads;public OwnedMountainSource[] protectedSources;
        }
        sealed class OwnedMountainContext:IDisposable
        {
            public readonly DragonKingdomMountainRoadLayout Layout;public readonly OwnedMountainRoadEvidence evidence;public DragonKingdomDefinition Definition{get;private set;}
            DragonKingdomDefinition originalDefinition;string originalDefinitionJson,expectedDefinitionJson,originalDefinitionPath,originalDefinitionSha,originalDefinitionMetaSha;readonly IDisposable selection;readonly UnityEngine.Random.State random;readonly string legacy;string nodes;Dictionary<string,string> otherEdges;Transform physicalRoot,retainedPad;string retainedPadState;bool disposed;
            public OwnedMountainContext(string nativeEvidence,string archivePath)
            {
                random=UnityEngine.Random.state;legacy=RoadSpecsJson();string xmlPath=Path.Combine(nativeEvidence,"tests.xml"),receiptPath=Path.Combine(nativeEvidence,"rendered-evidence","actual-owned-mountain-road-layout.json");var xml=new XmlDocument();xml.Load(xmlPath);var cases=xml.SelectNodes("//test-case").Cast<XmlNode>().Where(node=>node.Attributes["fullname"].Value=="DragonKingdomOwnedMountainRoadTests.ActualVendorMountainRoutePreservesCavePrefixAndBakesOneSavedPhysicalAuthority").ToArray();if(cases.Length!=1||cases[0].Attributes["result"].Value!="Passed")throw new InvalidOperationException("Exact actual passed V391 owned road case required.");var native=JsonUtility.FromJson<QualifiedOwnedMountainNative>(File.ReadAllText(receiptPath));var archive=JsonUtility.FromJson<OwnedMountainArchive>(File.ReadAllText(archivePath));
                if(native.status!="ACTUAL_VENDOR_ROUTE_MESH_AND_SAVED_AUTHORITY_PASSED_TERRAIN_AND_WORLD_PENDING"||!string.IsNullOrEmpty(native.error)||native.authoring==null||!native.authoring.originalSourcesExact||!native.authoring.randomStateRestored||archive.status!="ROAD_GEOMETRY_AND_SAVED_AUTHORITY_QUALIFIED_TERRAIN_WORLD_AND_CANONICAL_ADOPTION_PENDING"||!archive.privateOnly)throw new InvalidOperationException("Qualified native saved road and exact private archive receipt required; no terrain/world acceptance inferred.");
                Layout=AssetDatabase.LoadAssetAtPath<DragonKingdomMountainRoadLayout>(native.layoutPath);if(Layout==null||AssetSha(native.layoutPath)!=native.layoutSha256)throw new InvalidOperationException("Exact qualified saved road layout must be restored before composition.");var archived=archive.files.ToDictionary(source=>source.path,StringComparer.Ordinal);var paths=AssetDatabase.GetDependencies(native.layoutPath,true).Where(path=>path==native.layoutPath||path.StartsWith(Path.GetDirectoryName(native.layoutPath).Replace('\\','/')+"/",StringComparison.Ordinal)).SelectMany(path=>new[]{path,path+".meta"}).Where(File.Exists).Distinct().ToArray();
                foreach(string path in paths)if(!archived.TryGetValue(path,out var original)||AssetSha(path)!=original.sha256)throw new InvalidOperationException("Saved qualified layout/mesh source differs from actual V391 archived bytes: "+path);
                DragonKingdomMountainRoadLayoutAuthoring.ValidateLayout(Layout);if(Layout.authoritativePoints.Length!=646||Layout.physicalMisses!=0||Layout.physicalQueries!=1920||Layout.maximumPhysicalHeightError>.3f||Layout.authoritativePointsSha256!=native.reloadedPointsSha256||Layout.sdkSha256!=AssetSha("Assets/EasyRoads3D/lib/EasyRoads3Dv3.dll"))throw new InvalidOperationException("Actual646-point/nativeSDK/physical authority is incomplete.");
                evidence=new OwnedMountainRoadEvidence{nativeEvidence=nativeEvidence,nativeTestsSha256=AssetSha(xmlPath),nativeReceiptSha256=AssetSha(receiptPath),archiveSha256=AssetSha(archivePath),layout=native.layoutPath,layoutSha256=native.layoutSha256,pointsSha256=Layout.authoritativePointsSha256,points=646,protectedSources=paths.Select(path=>new OwnedMountainSource{path=path,sha256=AssetSha(path)}).ToArray()};selection=DragonKingdomWorldAuthoring.SelectQualifiedOwnedMountainCandidate(Layout);
            }
            public void Apply(DragonKingdomWorld world)
            {
                evidence.originalGraphSha256=DigestText(JsonUtility.ToJson(world.definition.graph));nodes=JsonUtility.ToJson(new GraphNodes{nodes=world.definition.graph.nodes});otherEdges=world.definition.graph.edges.Where(edge=>edge.edgeId!=Layout.roadId).ToDictionary(edge=>edge.edgeId,JsonUtility.ToJson,StringComparer.Ordinal);var old=world.definition.graph.edges.Single(edge=>edge.edgeId==Layout.roadId);evidence.sourceTargetEdgeSha256=DigestText(JsonUtility.ToJson(old));if(old.fromNodeId!=Layout.from||old.toNodeId!=Layout.to||old.samples.Count<2||old.samples[0].position!=Layout.authoritativePoints[0]||old.samples[old.samples.Count-1].position!=Layout.sourceEndpoint||Mathf.Abs(old.laneCount*old.laneWidthMeters+old.leftShoulderWidthMeters+old.rightShoulderWidthMeters-Layout.width)>.001f)throw new InvalidOperationException("Exact original target semantic endpoints/width required; preserve changed designer configurations.");
                originalDefinition=world.definition;originalDefinitionJson=JsonUtility.ToJson(originalDefinition);originalDefinitionPath=AssetDatabase.GetAssetPath(originalDefinition);if(!RidgeDefinitionPath(originalDefinitionPath))throw new InvalidOperationException("Exact original definition asset must precede the explicit owned road clone.");originalDefinitionSha=AssetSha(originalDefinitionPath);originalDefinitionMetaSha=AssetSha(originalDefinitionPath+".meta");
                Definition=UnityEngine.Object.Instantiate(world.definition);Definition.graph=LwsRoadGraph.FromJson(world.definition.graph.ToJson());var edge=Definition.graph.edges.Single(item=>item.edgeId==Layout.roadId);edge.samples.Clear();float distance=0;
                for(int i=0;i<Layout.authoritativePoints.Length;i++){var point=Layout.authoritativePoints[i];if(i>0)distance+=Vector3.Distance(Layout.authoritativePoints[i-1],point);edge.samples.Add(new LwsRoadSample{roadId=edge.roadId,segmentId=edge.segmentId,position=point,forward=(Layout.authoritativePoints[Math.Min(i+1,645)]-Layout.authoritativePoints[Math.Max(0,i-1)]).normalized,up=Vector3.up,distanceFromStartMeters=distance,direction=edge.direction,roadWidthMeters=Layout.width,laneWidthMeters=edge.laneWidthMeters,laneCount=edge.laneCount,speedLimitMph=edge.speedLimitMph});}edge.distanceMeters=edge.travelCost=distance;world.definition=Definition;expectedDefinitionJson=JsonUtility.ToJson(Definition);
                var main=RequireSourceRoot(world,"Kingdom caverns-volcano · EasyRoads generated course",3637298366336087090L);var material=main.GetComponentsInChildren<MeshRenderer>(true).Select(renderer=>renderer.sharedMaterial).First(value=>value!=null);var contact=main.GetComponentsInChildren<MeshCollider>(true).Select(collider=>collider.sharedMaterial).FirstOrDefault(value=>value!=null);
                retainedPad=RequireSourceRoot(world,"Kingdom hairpin caverns-volcano 2 · EasyRoads generated course",843743417208279572L);retainedPadState=PadState(retainedPad);evidence.preservedUndergroundPads=new[]{retainedPad.name};
                Retire(main,3637298366336087090L);Retire(RequireSourceRoot(world,"Kingdom hairpin caverns-volcano 6 · EasyRoads generated course",4520377880275211363L),4520377880275211363L);Retire(RequireSourceRoot(world,"Kingdom hairpin caverns-volcano 9 · EasyRoads generated course",8847699847918239384L),8847699847918239384L);Retire(RequireSourceRoot(world,"Kingdom hairpin caverns-volcano 12 · EasyRoads generated course",1382603855827951494L),1382603855827951494L);
                physicalRoot=new GameObject("Kingdom caverns-volcano · qualified saved physical authority").transform;physicalRoot.SetParent(world.transform,false);physicalRoot.SetPositionAndRotation(Vector3.zero,Quaternion.identity);if(Vector3.Distance(physicalRoot.lossyScale,Vector3.one)>.000001f)throw new InvalidOperationException("Original unit world frame required; never silently distort the qualified native meshes.");
                foreach(var piece in Layout.physicalPieces){var part=new GameObject(piece.nativeSource);part.transform.SetParent(physicalRoot,false);part.transform.SetPositionAndRotation(piece.position,piece.rotation);part.transform.localScale=piece.scale;part.AddComponent<MeshFilter>().sharedMesh=piece.mesh;part.AddComponent<MeshRenderer>().sharedMaterial=material;var collider=part.AddComponent<MeshCollider>();collider.sharedMesh=piece.mesh;collider.sharedMaterial=contact;}evidence.physicalPieces=Layout.physicalPieces.Length;evidence.intentionalTargetDelta=true;Validate(world,false);
            }
            public OwnedDefinitionProvenance DefinitionProvenance(DragonKingdomWorld world)
            {
                Validate(world,false);var proof=new OwnedDefinitionProvenance{original=originalDefinition,candidate=Definition,originalJson=originalDefinitionJson,candidateJson=expectedDefinitionJson,path=originalDefinitionPath,sha=originalDefinitionSha,metaSha=originalDefinitionMetaSha,originalGraphSha=evidence.originalGraphSha256,candidateGraphSha=evidence.candidateGraphSha256};proof.ValidateAndPath(world);return proof;
            }
            void Retire(Transform root,long fileId){var source=OriginalOwnedRoadSource(root,fileId);AssetDatabase.TryGetGUIDAndLocalFileIdentifier(source,out string guid,out long actual);evidence.retired.Add(new OwnedMountainRetirement{name=root.name,sourcePrefab=AssetDatabase.GetAssetPath(source),sourceGuid=guid,sourceFileId=actual,beforeActive=root.gameObject.activeSelf,afterActive=false,renderers=root.GetComponentsInChildren<Renderer>(true).Length,colliders=root.GetComponentsInChildren<Collider>(true).Length});root.gameObject.SetActive(false);}
            public void Validate(DragonKingdomWorld world,bool saved)
            {
                var graph=world.definition.graph;var edge=graph.edges.Single(item=>item.edgeId==Layout.roadId);evidence.otherEdgesAndNodesExact=JsonUtility.ToJson(new GraphNodes{nodes=graph.nodes})==nodes&&graph.edges.Count==otherEdges.Count+1&&graph.edges.Where(item=>item.edgeId!=Layout.roadId).All(item=>otherEdges.TryGetValue(item.edgeId,out var original)&&JsonUtility.ToJson(item)==original);var points=edge.samples.Select(sample=>sample.position).ToArray();evidence.prefixAndSemanticEndpointsExact=points.Length==646&&points.Take(6).SequenceEqual(Layout.preservedPrefix)&&points[0]==graph.nodes.Single(node=>node.nodeId==Layout.from).position&&points[645]==graph.nodes.Single(node=>node.nodeId==Layout.to).position&&DragonKingdomMountainRoadLayoutAuthoring.PointsDigest(points)==Layout.authoritativePointsSha256;
                var root=world.transform.Cast<Transform>().Single(item=>item.name=="Kingdom caverns-volcano · qualified saved physical authority");evidence.physicalAuthorityExact=root.childCount==Layout.physicalPieces.Length;for(int i=0;i<root.childCount;i++){var part=root.GetChild(i);var piece=Layout.physicalPieces[i];evidence.physicalAuthorityExact&=part.GetComponent<MeshFilter>().sharedMesh==piece.mesh&&part.GetComponent<MeshRenderer>()!=null&&part.GetComponent<MeshRenderer>().enabled&&part.GetComponent<MeshRenderer>().sharedMaterial!=null&&part.GetComponent<MeshCollider>().sharedMesh==piece.mesh&&part.GetComponent<MeshCollider>().enabled&&!part.GetComponent<MeshCollider>().convex&&!part.GetComponent<MeshCollider>().isTrigger&&part.position.Equals(piece.position)&&part.rotation.Equals(piece.rotation)&&part.lossyScale.Equals(piece.scale);}
                var pad=world.GetComponentsInChildren<Transform>(true).Single(item=>item.name=="Kingdom hairpin caverns-volcano 2 · EasyRoads generated course");if(PadState(pad)!=retainedPadState)throw new InvalidOperationException("Original underground/cave-floor pad2 changed.");foreach(var retired in evidence.retired){var item=world.GetComponentsInChildren<Transform>(true).Single(part=>part.name==retired.name);if(item.gameObject.activeSelf)throw new InvalidOperationException("An explicitly retired obsolete target remains active.");}
                evidence.candidateGraphSha256=DigestText(JsonUtility.ToJson(graph));evidence.candidateTargetEdgeSha256=DigestText(JsonUtility.ToJson(edge));if(!evidence.otherEdgesAndNodesExact||!evidence.prefixAndSemanticEndpointsExact||!evidence.physicalAuthorityExact||!graph.Validate().IsValid)throw new InvalidOperationException("Single intentional target delta split graph/physical authority or changed another edge/node.");if(saved)evidence.savedAuthorityExact=true;
            }
            public void ProbeSavedPhysicalAndNavigation(DragonKingdomWorld world)
            {
                var colliders=world.transform.Cast<Transform>().Single(item=>item.name=="Kingdom caverns-volcano · qualified saved physical authority").GetComponentsInChildren<MeshCollider>();Physics.SyncTransforms();
                for(int i=6;i<Layout.authoritativePoints.Length;i++){var point=Layout.authoritativePoints[i];var forward=Layout.authoritativePoints[Math.Min(i+1,645)]-Layout.authoritativePoints[i-1];forward.y=0;var right=Vector3.Cross(Vector3.up,forward.normalized);foreach(float lateral in new[]{-.35f*Layout.width,0,.35f*Layout.width}){evidence.physicalQueries++;var hits=new List<RaycastHit>();foreach(var collider in colliders)if(collider.Raycast(new Ray(point+right*lateral+Vector3.up*20,Vector3.down),out var hit,40)&&hit.normal.y>.25f)hits.Add(hit);if(hits.Count==0)evidence.physicalMisses++;else evidence.maximumPhysicalHeightError=Mathf.Max(evidence.maximumPhysicalHeightError,hits.Min(hit=>Mathf.Abs(hit.point.y-point.y)));}}
                foreach(int index in new[]{17,139,271,403,535}){if(!LwsRoadGraphQuery.TryFindNearestRoad(world.definition.graph,Layout.authoritativePoints[index],.05f,out var road)||road.EdgeId!=Layout.roadId||Vector3.Distance(road.NearestPosition,Layout.authoritativePoints[index])>.001f)throw new InvalidOperationException("Existing navigation/GPS graph query does not expose the actual new route at station"+index);evidence.navigationQueries++;}if(evidence.physicalQueries!=1920||evidence.physicalMisses!=0||evidence.maximumPhysicalHeightError>.3f)throw new InvalidOperationException("Reloaded physical road does not support the same authoritative outdoor stations within original0.3m acceptance.");
            }
            public void Dispose(){if(disposed)return;selection.Dispose();evidence.legacySelectionRestored=RoadSpecsJson()==legacy;UnityEngine.Random.state=random;evidence.randomStateRestored=JsonUtility.ToJson(UnityEngine.Random.state)==JsonUtility.ToJson(random);evidence.sourceBytesPreserved=evidence.protectedSources.All(source=>AssetSha(source.path)==source.sha256);disposed=true;if(!evidence.legacySelectionRestored||!evidence.sourceBytesPreserved||!evidence.randomStateRestored)throw new InvalidOperationException("Candidate selection/RNG/qualified source restoration failed.");}
            public void DestroyUnsavedDefinition(){if(Definition!=null&&!AssetDatabase.Contains(Definition)&&!DragonKingdomDiggerCavernAuthoring.IsBusy)UnityEngine.Object.DestroyImmediate(Definition);}
        }
        // A private certificate created only by the source-closed owned-road
        // context. Building placement uses the actual candidate graph while
        // original native-building provenance remains the canonical asset.
        sealed class OwnedDefinitionProvenance
        {
            public DragonKingdomDefinition original,candidate;public string originalJson,candidateJson,path,sha,metaSha,originalGraphSha,candidateGraphSha;
            public string ValidateAndPath(DragonKingdomWorld world)
            {
                if(world==null||original==null||candidate==null||world.definition!=candidate||!RidgeDefinitionPath(path)||AssetDatabase.GetAssetPath(original)!=path||JsonUtility.ToJson(original)!=originalJson||JsonUtility.ToJson(candidate)!=candidateJson||AssetSha(path)!=sha||AssetSha(path+".meta")!=metaSha)throw new InvalidOperationException("Owned-road provenance must retain the exact original definition asset and only its prequalified candidate data; unknown definition changes rejected.");return path;
            }
        }
        [Serializable]sealed class GraphNodes{public List<LwsRoadNode> nodes;}
        [Serializable]sealed class RoadSpecIdentity{public string id,from,to;public float width;public Vector3[] points;}
        [Serializable]sealed class RoadSpecList{public RoadSpecIdentity[] roads;}
        static string RoadSpecsJson()=>JsonUtility.ToJson(new RoadSpecList{roads=DragonKingdomWorldAuthoring.Roads().Select(road=>new RoadSpecIdentity{id=road.id,from=road.from,to=road.to,width=road.width,points=road.points}).ToArray()});
        static string DigestText(string text)=>DragonKingdomMountainRoadLayoutAuthoring.Digest(Encoding.UTF8.GetBytes(text));
        static string PadState(Transform pad)=>pad.gameObject.activeSelf+"|"+Pose(pad)+"|"+string.Join(";",pad.GetComponentsInChildren<MeshFilter>(true).Select(filter=>AssetDatabase.GetAssetPath(filter.sharedMesh)+"|"+Pose(filter.transform)))+"|"+string.Join(";",pad.GetComponentsInChildren<Collider>(true).Select(collider=>collider.enabled+"|"+collider.isTrigger+"|"+Pose(collider.transform)));
        static Transform RequireSourceRoot(DragonKingdomWorld world,string name,long fileId)
        {
            var root=world.GetComponentsInChildren<Transform>(true).Single(item=>item.name==name);OriginalOwnedRoadSource(root,fileId);return root;
        }
        static GameObject OriginalOwnedRoadSource(Transform current,long fileId)
        {
            // The authored preview deliberately stores plain scene objects.
            // SaveAsPrefabAsset does not connect them. Compare their complete
            // serialized subtree to the exact canonical fileID, rather than
            // requiring a nonexistent prefab link or trusting the display name.
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Prefab);var source=prefab.GetComponentsInChildren<Transform>(true).Single(t=>t.name==current.name);
            if(!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(source.gameObject,out string _,out long actual)||actual!=fileId)throw new InvalidOperationException("Canonical owned road identity changed: "+current.name);
            var currentWorld=current.GetComponentInParent<DragonKingdomWorld>().transform;var originalWorld=prefab.GetComponent<DragonKingdomWorld>().transform;
            var a=current.GetComponentsInChildren<Transform>(true);var b=source.GetComponentsInChildren<Transform>(true);if(a.Length!=b.Length)throw new InvalidOperationException("Owned road subtree membership changed: "+current.name);
            string SiblingIdentity(Transform value,Transform root)
            {
                var parts=new Stack<string>();for(var cursor=value;cursor!=root;cursor=cursor.parent){if(cursor==null)throw new InvalidOperationException("Authored subtree reference escaped its original world.");parts.Push(cursor.GetSiblingIndex()+":"+cursor.name);}return string.Join("/",parts);
            }
            string Token(UnityEngine.Object value,Transform world)
            {
                if(value==null)return "null";Transform t=value is GameObject go?go.transform:value is Component component?component.transform:null;
                if(t!=null&&(t==world||t.IsChildOf(world))){string path=SiblingIdentity(t,world);return value is GameObject?"gameObject:"+path:"component:"+path+":"+value.GetType().FullName+":"+Array.IndexOf(t.GetComponents(value.GetType()),value);}
                if(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(value,out string guid,out long id))return "asset:"+guid+":"+id;throw new InvalidOperationException("Unknown external road reference: "+value.name);
            }
            void Equal(UnityEngine.Object left,UnityEngine.Object right)
            {
                if(left.GetType()!=right.GetType())throw new InvalidOperationException("Owned road component type changed: "+current.name);var l=new SerializedObject(left).GetIterator();var rr=new SerializedObject(right).GetIterator();bool descend=true;
                while(l.Next(descend))
                {
                    if(!rr.Next(descend)||l.propertyPath!=rr.propertyPath||l.propertyType!=rr.propertyType)throw new InvalidOperationException("Owned road serialized layout changed: "+current.name);descend=true;
                    if(new[]{"m_CorrespondingSourceObject","m_PrefabInstance","m_PrefabAsset"}.Any(prefix=>l.propertyPath==prefix||l.propertyPath.StartsWith(prefix+".",StringComparison.Ordinal))){descend=false;continue;}
                    bool same=l.propertyType==SerializedPropertyType.ObjectReference?Token(l.objectReferenceValue,currentWorld)==Token(rr.objectReferenceValue,originalWorld):SerializedProperty.DataEquals(l,rr);
                    // Generic containers include object references; their leaf
                    // values are compared individually with the same authority.
                    if(l.propertyType==SerializedPropertyType.ObjectReference)descend=false;
                    if(l.propertyType==SerializedPropertyType.Generic)continue;
                    if(!same)throw new InvalidOperationException("Owned road authored property changed: "+current.name+"/"+left.GetType().Name+"/"+l.propertyPath);
                }
                if(rr.Next(descend))throw new InvalidOperationException("Owned road has extra source properties.");
            }
            for(int i=0;i<a.Length;i++){if(SiblingIdentity(a[i],current)!=SiblingIdentity(b[i],source))throw new InvalidOperationException("Owned road child order/path changed.");Equal(a[i].gameObject,b[i].gameObject);var ac=a[i].GetComponents<Component>();var bc=b[i].GetComponents<Component>();if(ac.Length!=bc.Length||ac.Any(c=>c==null)||bc.Any(c=>c==null))throw new InvalidOperationException("Owned road component membership changed.");for(int c=0;c<ac.Length;c++)Equal(ac[c],bc[c]);}
            return source.gameObject;
        }
        static OwnedMountainContext SelectOwnedMountainFromArguments()
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,OwnedMountainFlag);if(at<0){if(args.Contains("-tsDragonOwnedMountainRoadEvidence")||args.Contains("-tsDragonOwnedMountainRoadArchiveEvidence"))throw new InvalidOperationException("Owned road evidence requires its explicit candidate profile flag.");return null;}if(at+1>=args.Length||args[at+1]!=OwnedMountainProfile)throw new InvalidOperationException("Only the explicitly qualified owned mountain candidate profile is supported.");string Required(string flag){int index=Array.IndexOf(args,flag);if(index<0||index+1>=args.Length)throw new InvalidOperationException("Explicit owned road evidence required: "+flag);return args[index+1];}return new OwnedMountainContext(Required("-tsDragonOwnedMountainRoadEvidence"),Required("-tsDragonOwnedMountainRoadArchiveEvidence"));
        }
        [Serializable]sealed class OwnedMountainSavedProbe
        {
            public string scope="SAVED_SINGLE_TARGET_GRAPH_RENDER_COLLIDER_AND_ACTUAL_SDK_INPUT_CONTROLS_NOT_TERRAIN_GRADE_WORLD_PORTAL_DIGGING_DRIVING_OR_ADOPTION_ACCEPTANCE",candidatePrefab,candidatePreview,error,cleanupError;public bool emptyIncomingSetupHandled;public int incomingSetupScenes;public OwnedMountainRoadEvidence road;public bool originalWorldSourcesExact,originalTerrainMemoryExact,savedReloaded,legacyRestoredAfterForcedException,nestedSelectionRejected,ownedDefinitionProvenanceExact,ownedDefinitionUnknownChangeRejected;
        }
        public static string CreateSavedOwnedMountainAuthorityCandidateFromCommandLine()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new InvalidOperationException("Stopped compiled Editor required.");for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty||SceneManager.GetSceneAt(i).path==Preview)throw new InvalidOperationException("Preserve user scenes; close original preview before isolated candidate proof.");string tag=Argument("-tsDragonOwnedMountainAuthorityCandidate","");if(string.IsNullOrEmpty(tag)||tag.Length>40||tag.Any(character=>!char.IsLetterOrDigit(character)&&character!='-'&&character!='_'))throw new InvalidOperationException("Explicit bounded unique candidate tag required.");string folder=Root+"VisualCandidates/"+tag;if(Directory.Exists(folder))throw new InvalidOperationException("Preserve existing candidate output; choose a new tag.");var sources=new[]{Prefab,Preview,Root+"Configuration/Dragon Kingdom.asset","Assets/EasyRoads3D/lib/EasyRoads3Dv3.dll","Assets/EasyRoads3D/Resources/ERRoadNetwork.prefab","Assets/EasyRoads3D/Resources/Materials/roads/road material.mat"}.SelectMany(path=>new[]{path,path+".meta"}).ToDictionary(path=>path,AssetSha,StringComparer.Ordinal);var setup=EditorSceneManager.GetSceneManagerSetup();Scene scene=default;OwnedMountainContext context=null;var report=new OwnedMountainSavedProbe{candidatePrefab=folder+"/Shared Dragon Kingdom.prefab",candidatePreview=folder+"/DragonKingdomPreview.unity"};Exception failure=null;
            try
            {
                context=SelectOwnedMountainFromArguments();if(context==null)throw new InvalidOperationException("The owned mountain authority proof requires its explicit profile/evidence.");report.road=context.evidence;scene=EditorSceneManager.OpenScene(Preview,OpenSceneMode.Additive);EditorSceneManager.SetActiveScene(scene);var world=scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<DragonKingdomWorld>(true)).Single();var terrainHashes=world.GetComponentsInChildren<Terrain>(true).Select(terrain=>terrain.terrainData).Distinct().ToDictionary(terrain=>terrain,DragonKingdomWorldVisualMigration.HeightHash);context.Apply(world);var definitionProvenance=context.DefinitionProvenance(world);report.ownedDefinitionProvenanceExact=definitionProvenance.ValidateAndPath(world)==AssetDatabase.GetAssetPath(definitionProvenance.original);var testRegion=world.definition.FindRegion("village");var originalBounds=testRegion.bounds;try{var changed=originalBounds;changed.Expand(1);testRegion.bounds=changed;try{definitionProvenance.ValidateAndPath(world);}catch(InvalidOperationException){report.ownedDefinitionUnknownChangeRejected=true;}}finally{testRegion.bounds=originalBounds;}definitionProvenance.ValidateAndPath(world);if(!report.ownedDefinitionProvenanceExact||!report.ownedDefinitionUnknownChangeRejected)throw new InvalidOperationException("Original-definition provenance or unknown-data rejection control failed.");context.evidence.gradeInput=DragonKingdomWorldAuthoring.InspectSelectedOwnedMountainActualSdkInput(scene);report.originalTerrainMemoryExact=terrainHashes.All(pair=>DragonKingdomWorldVisualMigration.HeightHash(pair.Key)==pair.Value);if(!report.originalTerrainMemoryExact)throw new InvalidOperationException("SDK input-only proof changed original terrain memory.");
                try{using(DragonKingdomWorldAuthoring.SelectQualifiedOwnedMountainCandidate(context.Layout)){}throw new InvalidOperationException("Nested selection was unexpectedly accepted.");}catch(InvalidOperationException error){report.nestedSelectionRejected=error.Message.Contains("Only one stopped explicit candidate");}if(!report.nestedSelectionRejected)throw new InvalidOperationException("Sole authoring authority/nesting control failed.");
                Directory.CreateDirectory(folder);AssetDatabase.Refresh();AssetDatabase.CreateAsset(context.Definition,folder+"/Dragon Kingdom.asset");AssetDatabase.SaveAssetIfDirty(context.Definition);PrefabUtility.SaveAsPrefabAsset(world.gameObject,report.candidatePrefab);if(!EditorSceneManager.SaveScene(scene,report.candidatePreview))throw new InvalidOperationException("Owned road candidate scene save failed.");EditorSceneManager.CloseScene(scene,true);scene=EditorSceneManager.OpenScene(report.candidatePreview,OpenSceneMode.Additive);world=scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<DragonKingdomWorld>(true)).Single();context.Validate(world,true);context.ProbeSavedPhysicalAndNavigation(world);report.savedReloaded=true;
            }
            catch(Exception error){failure=error;report.error=error.ToString();if(report.road!=null)report.road.gradeInput=DragonKingdomWorldAuthoring.LastOwnedMountainGradeInput;}
            finally
            {
                // Always retain the first substantive failure. Cleanup must not
                // hide it behind an invalid empty native-runner scene setup.
                void Cleanup(string stage,Action action){try{action();}catch(Exception error){report.cleanupError+=(stage+": "+error+"\n");if(failure==null){failure=error;report.error=error.ToString();}}}
                Cleanup("close-candidate",()=>{if(scene.IsValid()&&scene.isLoaded)EditorSceneManager.CloseScene(scene,true);});
                Cleanup("release-road-selection",()=>context?.Dispose());Cleanup("release-unsaved-definition",()=>context?.DestroyUnsavedDefinition());
                report.incomingSetupScenes=setup.Length;
                Cleanup("restore-scene-setup",()=>{if(setup.Length==0){report.emptyIncomingSetupHandled=true;return;}EditorSceneManager.RestoreSceneManagerSetup(setup);});
                Cleanup("verify-original-sources",()=>{report.originalWorldSourcesExact=sources.All(pair=>AssetSha(pair.Key)==pair.Value);});
                Cleanup("forced-exception-scope-control",()=>{if(context!=null){string legacy=RoadSpecsJson();try{using(DragonKingdomWorldAuthoring.SelectQualifiedOwnedMountainCandidate(context.Layout)){throw new ApplicationException("INTENTIONAL_SCOPE_RESTORATION_CONTROL");}}catch(ApplicationException error){if(error.Message!="INTENTIONAL_SCOPE_RESTORATION_CONTROL")throw;}report.legacyRestoredAfterForcedException=RoadSpecsJson()==legacy;}});
                Directory.CreateDirectory(Output());File.WriteAllText(Path.Combine(Output(),"saved-owned-mountain-world-authority.json"),JsonUtility.ToJson(report,true));
            }
            if(failure!=null)System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();if(!report.originalWorldSourcesExact||!report.legacyRestoredAfterForcedException)throw new InvalidOperationException("Saved owned road authority proof changed original sources or leaked candidate scope.");return JsonUtility.ToJson(report,true);
        }
    }
}
#endif

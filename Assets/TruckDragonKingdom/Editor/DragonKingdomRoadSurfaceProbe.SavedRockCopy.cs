#if UNITY_EDITOR && VISTA
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using Digger.Modules.Core.Sources;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LWS.TruckDragonKingdom.Editor
{
    public static partial class DragonKingdomRoadSurfaceProbe
    {
        [Serializable]public sealed class SavedRockCopyReceipt
        {
            public string scope="ISOLATED_ACTUAL_CAVE17_SAVED_MATERIAL_PROVIDER_RELOAD_NOT_WORLD_QUALITY_NEW_DIGGING_RUNTIME_RENDER_OR_CANONICAL_ACCEPTANCE",sourcePreview,sourceData,folder,prefab,preview,failure,geometryBefore,geometryAfter,geometryReloaded,providerConfiguration;
            public bool originalSourcesExact,sourceVoxelMembershipExact,terrainCopiesExact,providerCopiesExact,savedReloaded,stableGeometryExact,stableProviderExact,sourceMeshesReferencedExact,copiedTerrainFilesExact,sourceRasterClearanceVerified;
            public int terrainCopies,providerMaterialCopies,nativeMeshReferences;public string[] sourcePaths,sourceSha256,copiedTerrainPaths,copiedTerrainSha256;public CavernSavedRockMaterialsReceipt materials;
        }
        public static SavedRockCopyReceipt InspectIsolatedActualCavernSavedRockCopy(string sourceTag,string candidateTag)
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||DragonKingdomDiggerCavernAuthoring.IsBusy)throw new InvalidOperationException("Stopped compiled Editor without a Digger operation required.");
            bool Tag(string tag)=>tag!=null&&Regex.IsMatch(tag,"^[A-Za-z0-9][A-Za-z0-9_-]{0,39}$");if(!Tag(sourceTag)||!Tag(candidateTag)||candidateTag==sourceTag)throw new InvalidOperationException("Actual bounded source and a unique bounded diagnostic tag required.");
            var rasterAuthority=sourceTag=="CavernRoundedProbe17"?Array.Empty<string>():RequirePassedRasterCaveCopySource(sourceTag);
            string sourcePreview=Root+"VisualCandidates/"+sourceTag+"/Cavern Input Probe.unity",sourceData="Assets/DiggerData/Scenes/DK-"+sourceTag,folder=Root+"VisualCandidates/"+candidateTag,data="Assets/DiggerData/Scenes/DK-"+candidateTag;
            if(!File.Exists(IoPath(sourcePreview))||!Directory.Exists(sourceData)||Directory.Exists(folder)||Directory.Exists(data)||File.Exists(folder+".meta")||File.Exists(data+".meta"))throw new InvalidOperationException("Actual source must exist; diagnostic world/material outputs must be absent.");
            for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty||string.IsNullOrEmpty(SceneManager.GetSceneAt(i).path)||SceneManager.GetSceneAt(i).path==sourcePreview)throw new InvalidOperationException("Preserve incoming saved scenes and close the original source-cave preview.");
            var voxelMembers=Directory.GetFiles(sourceData,"*",SearchOption.AllDirectories).OrderBy(path=>path,StringComparer.Ordinal).ToArray();
            var paths=AssetDatabase.GetDependencies(sourcePreview,true).Concat(new[]{sourcePreview,CavernRoughRockDiffuse,CavernRoughRockNormal}).Concat(voxelMembers).Concat(rasterAuthority).SelectMany(path=>new[]{path,path+".meta"}).Where(path=>File.Exists(IoPath(path))).Distinct().OrderBy(path=>path,StringComparer.Ordinal).ToArray();
            var proof=new SavedRockCopyReceipt{sourcePreview=sourcePreview,sourceData=sourceData,folder=folder,prefab=folder+"/Saved rock material persistence TEST ONLY.prefab",preview=folder+"/Cavern Input Probe.unity",sourcePaths=paths,sourceSha256=paths.Select(path=>Hash(File.ReadAllBytes(IoPath(path)))).ToArray()};
            if(rasterAuthority.Length!=0)proof.scope=proof.scope.Replace("CAVE17","SOURCE_QUALIFIED_RASTER_CAVE");
            var setup=EditorSceneManager.GetSceneManagerSetup();Scene sourceScene=default,candidateScene=default;GameObject loaded=null;Exception failure=null;
            try
            {
                Directory.CreateDirectory(folder+"/Terrain");Directory.CreateDirectory(data+"/Materials");AssetDatabase.Refresh();sourceScene=EditorSceneManager.OpenScene(sourcePreview,OpenSceneMode.Additive);
                var sourceWorld=sourceScene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<DragonKingdomWorld>(true)).Single();
                if(rasterAuthority.Length!=0)
                {
                    var expected=JsonUtility.FromJson<DragonKingdomDiggerCavernAuthoring.NativeDoorwayClearance>(File.ReadAllText(IoPath(rasterAuthority[2])));var actual=DragonKingdomDiggerCavernAuthoring.MeasureNativeDoorwaySpace(sourceWorld);if(!actual.allJoinsClear||JsonUtility.ToJson(actual)!=JsonUtility.ToJson(expected))throw new InvalidOperationException("Actual saved raster-cave native geometry no longer matches its passed source-space measurement.");proof.sourceRasterClearanceVerified=true;
                }
                candidateScene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
                // Unity names a saved prefab root after its output filename.
                // Give this owned copy that identity before the geometry baseline;
                // the original world and every geometry field remain untouched.
                var clone=UnityEngine.Object.Instantiate(sourceWorld.gameObject);clone.name=Path.GetFileNameWithoutExtension(proof.prefab);SceneManager.MoveGameObjectToScene(clone,candidateScene);if(PrefabUtility.IsPartOfPrefabInstance(clone))PrefabUtility.UnpackPrefabInstance(clone,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
                var world=clone.GetComponent<DragonKingdomWorld>();var terrains=world.GetComponentsInChildren<Terrain>(true);if(terrains.Length!=4||terrains.Any(t=>t.terrainData==null)||terrains.Select(t=>t.terrainData).Distinct().Count()!=4)throw new InvalidOperationException("Exactly four original Cave17 terrain assets required.");
                foreach(var terrain in terrains)
                {
                    var original=terrain.terrainData;var collider=terrain.GetComponent<TerrainCollider>();if(collider==null||collider.terrainData!=original)throw new InvalidOperationException("Original terrain collider must share the terrain data authority.");
                    string target=folder+"/Terrain/Tile "+proof.terrainCopies+".asset";if(!AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(original),target))throw new InvalidOperationException("Terrain copy failed.");var copy=AssetDatabase.LoadAssetAtPath<TerrainData>(target);
                    if(TerrainContent(original)!=TerrainContent(copy))throw new InvalidOperationException("Copied terrain changed actual height/holes/surface state.");terrain.terrainData=copy;collider.terrainData=copy;proof.terrainCopies++;
                }
                proof.terrainCopiesExact=proof.terrainCopies==4;proof.copiedTerrainPaths=terrains.Select(t=>AssetDatabase.GetAssetPath(t.terrainData)).SelectMany(path=>new[]{path,path+".meta"}).ToArray();proof.copiedTerrainSha256=proof.copiedTerrainPaths.Select(path=>Hash(File.ReadAllBytes(IoPath(path)))).ToArray();
                var systems=world.GetComponentsInChildren<DiggerSystem>(true);var renderers=systems.SelectMany(d=>d.GetComponentsInChildren<MeshRenderer>(true)).Where(r=>r.GetComponent<MeshFilter>()?.sharedMesh!=null).ToArray();
                var originals=renderers.SelectMany(r=>r.sharedMaterials).Concat(systems.SelectMany(d=>d.Materials??Array.Empty<Material>())).Distinct().ToArray();if(systems.Length==0||renderers.Length==0||originals.Any(m=>m==null||!AssetDatabase.GetAssetPath(m).StartsWith(sourceData+"/",StringComparison.Ordinal)))throw new InvalidOperationException("Actual Cave17 native material providers required.");
                var copies=new Dictionary<Material,Material>();foreach(var original in originals){string target=data+"/Materials/Native source "+copies.Count+".mat";if(!AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(original),target))throw new InvalidOperationException("Native material source copy failed.");var copy=AssetDatabase.LoadAssetAtPath<Material>(target);copy.name=original.name;EditorUtility.SetDirty(copy);AssetDatabase.SaveAssetIfDirty(copy);if(EditorJsonUtility.ToJson(copy)!=EditorJsonUtility.ToJson(original))throw new InvalidOperationException("Native provider material copy changed serialized properties.");copies.Add(original,copy);}
                foreach(var digger in systems){if(digger.Terrain==null||!terrains.Contains(digger.Terrain)||digger.Materials==null)throw new InvalidOperationException("Cloned Digger terrain/provider reference changed.");digger.Materials=digger.Materials.Select(m=>copies[m]).ToArray();EditorUtility.SetDirty(digger);}
                foreach(var renderer in renderers){renderer.sharedMaterials=renderer.sharedMaterials.Select(m=>copies[m]).ToArray();EditorUtility.SetDirty(renderer);}proof.providerMaterialCopies=copies.Count;proof.providerCopiesExact=true;
                // Explicit copy/remap preparation ends here. No Digger Init,
                // rebuild, paint, excavation or source persistence is called.
                proof.geometryBefore=StableSavedRockGeometry(world);var sourceMeshPaths=world.GetComponentsInChildren<MeshFilter>(true).Where(f=>f.sharedMesh!=null).Select(f=>AssetDatabase.GetAssetPath(f.sharedMesh)).OrderBy(p=>p,StringComparer.Ordinal).ToArray();proof.nativeMeshReferences=sourceMeshPaths.Length;
                try{proof.materials=ApplyOwnedCavernRockMaterials(world,folder);}finally{proof.materials=LastSavedRockMaterials;}proof.geometryAfter=StableSavedRockGeometry(world);proof.providerConfiguration=StableSavedRockProviders(world);proof.stableGeometryExact=proof.geometryBefore==proof.geometryAfter;
                if(!proof.stableGeometryExact)throw new InvalidOperationException("Material-only operation changed stable copied terrain/geometry/physics/anchors.");
                var saved=PrefabUtility.SaveAsPrefabAsset(clone,proof.prefab,out bool savedOkay);if(!savedOkay||saved==null)throw new InvalidOperationException("Diagnostic-owned prefab did not save.");if(!EditorSceneManager.SaveScene(candidateScene,proof.preview))throw new InvalidOperationException("Diagnostic-owned preview did not save.");EditorSceneManager.CloseScene(candidateScene,true);candidateScene=default;EditorSceneManager.CloseScene(sourceScene,true);sourceScene=default;
                AssetDatabase.ImportAsset(proof.prefab,ImportAssetOptions.ForceSynchronousImport|ImportAssetOptions.ForceUpdate);loaded=PrefabUtility.LoadPrefabContents(proof.prefab);var reloaded=loaded.GetComponent<DragonKingdomWorld>();ValidateSavedOwnedCavernRockMaterials(reloaded,proof.materials);
                proof.geometryReloaded=StableSavedRockGeometry(reloaded);proof.stableGeometryExact&=proof.geometryBefore==proof.geometryReloaded;proof.stableProviderExact=proof.providerConfiguration==StableSavedRockProviders(reloaded);proof.sourceMeshesReferencedExact=sourceMeshPaths.SequenceEqual(reloaded.GetComponentsInChildren<MeshFilter>(true).Where(f=>f.sharedMesh!=null).Select(f=>AssetDatabase.GetAssetPath(f.sharedMesh)).OrderBy(p=>p,StringComparer.Ordinal));proof.savedReloaded=true;
                if(!proof.stableGeometryExact||!proof.stableProviderExact||!proof.sourceMeshesReferencedExact)throw new InvalidOperationException("Saved/reloaded owned prefab changed original geometry/provider fields/source mesh references.");
            }
            catch(Exception error){failure=error;proof.failure=error.ToString();}
            finally
            {
                if(loaded!=null)PrefabUtility.UnloadPrefabContents(loaded);if(candidateScene.IsValid()&&candidateScene.isLoaded)EditorSceneManager.CloseScene(candidateScene,true);if(sourceScene.IsValid()&&sourceScene.isLoaded)EditorSceneManager.CloseScene(sourceScene,true);if(setup.Length>0)EditorSceneManager.RestoreSceneManagerSetup(setup);
                proof.sourceVoxelMembershipExact=voxelMembers.SequenceEqual(Directory.GetFiles(sourceData,"*",SearchOption.AllDirectories).OrderBy(path=>path,StringComparer.Ordinal));proof.originalSourcesExact=paths.Select((path,index)=>File.Exists(IoPath(path))&&Hash(File.ReadAllBytes(IoPath(path)))==proof.sourceSha256[index]).All(value=>value);proof.copiedTerrainFilesExact=proof.copiedTerrainPaths!=null&&proof.copiedTerrainPaths.Select((path,index)=>File.Exists(IoPath(path))&&Hash(File.ReadAllBytes(IoPath(path)))==proof.copiedTerrainSha256[index]).All(value=>value);
                string output=Path.Combine(Output(),rasterAuthority.Length==0?"actual-cave17-saved-rock-copy-persistence.json":"actual-saved-raster-cavern-rock-copy-persistence.json");Directory.CreateDirectory(Path.GetDirectoryName(output));File.WriteAllText(output,JsonUtility.ToJson(proof,true));
            }
            if(failure!=null)System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();if(!proof.originalSourcesExact||!proof.sourceVoxelMembershipExact||!proof.copiedTerrainFilesExact)throw new InvalidOperationException("Isolated material persistence changed an original Cave17/source/voxel asset or copied terrain payload.");return proof;
        }
        static string[] RequirePassedRasterCaveCopySource(string tag)
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonCavernRasterSourceEvidence");if(at<0||at+1>=args.Length||!Path.IsPathRooted(args[at+1]))throw new InvalidOperationException("A new raster cave requires explicit passed excavation/render/native-space evidence; arbitrary saved worlds cannot replace Cave17.");
            string folder=args[at+1];var paths=new[]{Path.Combine(folder,"tests.xml"),Path.Combine(folder,"rendered-evidence","isolated-rounded-cavern-"+tag+".json"),Path.Combine(folder,"rendered-evidence","actual-raster-native-doorway-space.json")};if(paths.Any(p=>!File.Exists(IoPath(p))))throw new InvalidOperationException("Exact passed raster source files missing.");
            var xml=new XmlDocument();xml.Load(IoPath(paths[0]));foreach(string name in new[]{"DragonKingdomRoundedCavernExcavationTests.ActualRoundedDiggerExcavationUsesFreshFoundationAndPreservesOriginalSourcesWithoutPretendingWholeWorldAcceptance","DragonKingdomRoundedCavernRenderTests.ActualSavedRoundedCavernRendersThreeGeometryViewsWithExactSourcesAndPhysicalState"})
            {var cases=xml.GetElementsByTagName("test-case").Cast<XmlElement>().Where(n=>n.GetAttribute("fullname")==name).ToArray();if(cases.Length!=1||cases[0].GetAttribute("result")!="Passed")throw new InvalidOperationException("Actual raster source excavation and rendering must pass.");}
            var saved=JsonUtility.FromJson<DragonKingdomRegionalWorldIntegration.RoundedCavernProbeEvidence>(File.ReadAllText(IoPath(paths[1])));var space=JsonUtility.FromJson<DragonKingdomDiggerCavernAuthoring.NativeDoorwayClearance>(File.ReadAllText(IoPath(paths[2])));
            if(saved.tag!=tag||saved.preview!=Root+"VisualCandidates/"+tag+"/Cavern Input Probe.unity"||!saved.saved||!saved.sourcesExact||!saved.criticalPosesExact||saved.cavern?.roundedProfile?.rasterClearancePolicy!="OneMeasuredHorizontalVoxelDiagonalEachSide01"||!saved.cavern.roundedProfile.nativeRequiredColumnsPassed||!space.allJoinsClear||!space.nativeWallControlsPassed||space.joins?.Length!=2||space.joins.Any(j=>!j.clear||j.nativeWallRayHits!=2||j.obstructingNativeMeshes.Length!=0))throw new InvalidOperationException("Matching saved raster-cave source, original columns and native doorway controls required.");return paths;
        }
        static string RockStablePath(Transform value,Transform root)=>value==root?"@":RockStablePath(value.parent,root)+"/"+value.GetSiblingIndex()+":"+value.name;
        static string RockStableJson(UnityEngine.Object value,Transform root)
        {
            string Token(UnityEngine.Object obj){if(obj==null)return "null";if(AssetDatabase.Contains(obj)&&AssetDatabase.TryGetGUIDAndLocalFileIdentifier(obj,out string guid,out long id))return guid+":"+id;if(obj is GameObject go)return "GO:"+RockStablePath(go.transform,root);if(obj is Component c)return c.GetType().FullName+":"+RockStablePath(c.transform,root)+":"+Array.IndexOf(c.GetComponents(c.GetType()),c);throw new InvalidOperationException("Unknown transient saved-cave reference: "+obj.GetType().Name);}
            string json=Regex.Replace(EditorJsonUtility.ToJson(value),"\\\"m_(CorrespondingSourceObject|PrefabInstance|PrefabAsset)\\\"\\s*:\\s*\\{[^{}]*\\}","\"prefabProvenance\":null");return Regex.Replace(json,"\\\"instanceID\\\"\\s*:\\s*(-?[0-9]+)",match=>"\"stableObject\":\""+Token(EditorUtility.InstanceIDToObject(int.Parse(match.Groups[1].Value))).Replace("\\","\\\\").Replace("\"","\\\"")+"\"");
        }
        static string TerrainContent(TerrainData data)
        {
            if(data==null)throw new InvalidOperationException("Actual terrain data required.");using(var stream=new MemoryStream()){using(var writer=new BinaryWriter(stream,Encoding.UTF8,true)){writer.Write(data.heightmapResolution);writer.Write(data.holesResolution);writer.Write(data.size.x);writer.Write(data.size.y);writer.Write(data.size.z);writer.Write(DragonKingdomWorldVisualMigration.HeightHash(data));foreach(bool hole in data.GetHoles(0,0,data.holesResolution,data.holesResolution))writer.Write(hole);foreach(float blend in data.GetAlphamaps(0,0,data.alphamapWidth,data.alphamapHeight))writer.Write(blend);foreach(var layer in data.terrainLayers){AssetDatabase.TryGetGUIDAndLocalFileIdentifier(layer,out string guid,out long id);writer.Write(guid);writer.Write(id);}}return Hash(stream.ToArray());}
        }
        static string StableSavedRockProviders(DragonKingdomWorld world)=>Hash(Encoding.UTF8.GetBytes(string.Join("\n",world.GetComponentsInChildren<DiggerSystem>(true).Select(d=>RockStableJson(d,world.transform)))));
        static string StableSavedRockGeometry(DragonKingdomWorld world)
        {
            var root=world.transform;var parts=new List<string>();parts.AddRange(world.GetComponentsInChildren<Transform>(true).Select(t=>RockStablePath(t,root)+"|"+t.gameObject.name+"|"+t.gameObject.activeSelf+"|"+t.gameObject.layer+"|"+t.gameObject.tag+"|"+GameObjectUtility.GetStaticEditorFlags(t.gameObject)+"|"+RockStableJson(t,root)));
            parts.AddRange(world.GetComponentsInChildren<Collider>(true).Select(c=>RockStableJson(c,root)));parts.AddRange(world.GetComponentsInChildren<Rigidbody>(true).Select(body=>RockStableJson(body,root)));parts.AddRange(world.GetComponentsInChildren<LODGroup>(true).Select(lod=>RockStableJson(lod,root)));
            var payload=new Dictionary<Mesh,string>();foreach(var filter in world.GetComponentsInChildren<MeshFilter>(true)){if(filter.sharedMesh==null)continue;if(!payload.TryGetValue(filter.sharedMesh,out string mesh)){mesh=StandardLitMeshPayload(filter.sharedMesh);payload.Add(filter.sharedMesh,mesh);}parts.Add(RockStablePath(filter.transform,root)+"|"+AssetDatabase.GetAssetPath(filter.sharedMesh)+"|"+mesh);}
            foreach(var terrain in world.GetComponentsInChildren<Terrain>(true))parts.Add(RockStablePath(terrain.transform,root)+"|"+TerrainContent(terrain.terrainData));return Hash(Encoding.UTF8.GetBytes(string.Join("\n",parts)));
        }
    }
}
#endif

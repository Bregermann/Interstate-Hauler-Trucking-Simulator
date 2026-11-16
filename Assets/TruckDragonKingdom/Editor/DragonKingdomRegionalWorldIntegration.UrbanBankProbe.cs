#if UNITY_EDITOR && VISTA
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LWS.TruckDragonKingdom.Editor
{
    public static partial class DragonKingdomRegionalWorldIntegration
    {
        [Serializable]public sealed class UrbanBankProjection
        {public string scope="READ_ONLY_ACTUAL_SAVED23_NATIVE_HEIGHTFIELD_12M_BANK_PROJECTION_NOT_NEW_WORLD_OR_ADOPTION",preview,status="IN_PROGRESS",error;public UrbanBankProof projectedBank;public int coreTiles,originalColliders;public bool sourcesExact,originalDataBindingsRestored,colliderDataBindingsUnchanged,heightsAndPhysicsExact;}
        public static UrbanBankProjection LastUrbanBankProjection{get;private set;}
        public static void CaptureActualSavedUrbanBankProjection()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new InvalidOperationException("Stopped Editor required.");string tag=Argument("-tsDragonWorldCandidate","");if(tag!="RoundedIntegrated23")throw new InvalidOperationException("Exact immutable physically verified23 baseline required for this diagnostic.");string preview=Root+"VisualCandidates/"+tag+"/DragonKingdomPreview.unity";
            for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty||string.IsNullOrEmpty(SceneManager.GetSceneAt(i).path))throw new InvalidOperationException("Preserve user scenes before isolated read-only bank projection.");
            PrepareApprovedArtistMaterialImports();var paths=AssetDatabase.GetDependencies(preview,true).SelectMany(p=>new[]{p,p+".meta"}).Where(p=>File.Exists(Io(p))).Distinct().ToDictionary(p=>p,AssetSha);var setup=EditorSceneManager.GetSceneManagerSetup();Scene scene=default;DragonKingdomWorld world=null;var originals=new Dictionary<Terrain,TerrainData>();var colliders=new Dictionary<TerrainCollider,TerrainData>();var copies=new List<TerrainData>();string physics=null;string[] heights=null;var proof=new UrbanBankProjection{preview=preview};LastUrbanBankProjection=proof;
            try
            {
                scene=EditorSceneManager.OpenScene(preview,OpenSceneMode.Additive);EditorSceneManager.SetActiveScene(scene);world=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<DragonKingdomWorld>(true)).Single();physics=DragonKingdomWorldVisualMigration.PhysicsAndAnchorHash(world.gameObject);var terrains=world.GetComponentsInChildren<Terrain>(true);heights=terrains.Select(t=>DragonKingdomWorldVisualMigration.HeightHash(t.terrainData)).ToArray();foreach(var t in terrains)originals.Add(t,t.terrainData);foreach(var c in world.GetComponentsInChildren<TerrainCollider>(true))colliders.Add(c,c.terrainData);proof.originalColliders=colliders.Count;
                var core=terrains.Where(t=>AssetDatabase.GetAssetPath(t.terrainData).StartsWith(Root+"VisualCandidates/"+tag+"/Terrain/",StringComparison.Ordinal)).ToArray();proof.coreTiles=core.Length;if(core.Length!=4||terrains.Length!=16||colliders.Count!=16)throw new InvalidOperationException("Exact actual4core+12coast terrain foundation required.");var tiles=new List<TileState>();
                foreach(var t in core){var copy=UnityEngine.Object.Instantiate(t.terrainData);copy.hideFlags=HideFlags.HideAndDontSave;copies.Add(copy);t.terrainData=copy;int n=copy.heightmapResolution;tiles.Add(new TileState{terrain=t,candidate=copy,original=originals[t],before=copy.GetHeights(0,0,n,n),record=new TileRecord{resolution=n},fieldTags=new byte[n,n],fieldOwners=new string[n,n]});}
                proof.projectedBank=CreateMeasuredUrbanRetainingBanks(world,tiles.ToArray());proof.status="NATIVE_PROJECTED_12M_BANK_PASSED_REQUIRES_COMPOSITION_AND_FRESH_FULL_WORLD_GATES";
            }
            catch(Exception exception){proof.projectedBank=LastMeasuredUrbanBank;proof.status="REJECTED_NATIVE_BANK_PROJECTION";proof.error=exception.ToString();throw;}
            finally
            {
                foreach(var pair in originals)if(pair.Key!=null){pair.Key.terrainData=pair.Value;pair.Key.Flush();}proof.originalDataBindingsRestored=originals.All(p=>p.Key!=null&&p.Key.terrainData==p.Value);proof.colliderDataBindingsUnchanged=colliders.All(p=>p.Key!=null&&p.Key.terrainData==p.Value);proof.heightsAndPhysicsExact=world!=null&&heights!=null&&physics==DragonKingdomWorldVisualMigration.PhysicsAndAnchorHash(world.gameObject)&&heights.SequenceEqual(world.GetComponentsInChildren<Terrain>(true).Select(t=>DragonKingdomWorldVisualMigration.HeightHash(t.terrainData)));foreach(var copy in copies)UnityEngine.Object.DestroyImmediate(copy);if(scene.IsValid()&&scene.isLoaded)EditorSceneManager.CloseScene(scene,true);EditorSceneManager.RestoreSceneManagerSetup(setup);proof.sourcesExact=paths.All(p=>File.Exists(Io(p.Key))&&AssetSha(p.Key)==p.Value);Directory.CreateDirectory(Output());File.WriteAllText(Path.Combine(Output(),"actual23-urban-bank-projection.json"),JsonUtility.ToJson(proof,true));if(!proof.sourcesExact||!proof.originalDataBindingsRestored||!proof.colliderDataBindingsUnchanged||!proof.heightsAndPhysicsExact)throw new InvalidOperationException("Read-only bank projection changed a native source/collider/heightfield or failed exact restoration.");
            }
        }
    }
}
#endif

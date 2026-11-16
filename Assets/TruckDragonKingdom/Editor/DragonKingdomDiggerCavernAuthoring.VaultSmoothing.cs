#if UNITY_EDITOR && VISTA
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Digger.Modules.Core.Sources;
using Digger.Modules.Core.Sources.Operations;
using Unity.Mathematics;
using UnityEngine;

namespace LWS.TruckDragonKingdom.Editor
{
    public static partial class DragonKingdomDiggerCavernAuthoring
    {
        [Serializable]public sealed class VaultSmoothingEvidence
        {
            public string scope="FRESH_CANDIDATE_THREE_MEASURED_6M_UPPER_SURFACE_NATIVE_KERNEL_SMOOTH_TRIAL_NOT_FINISHED_CAVE_OR_WORLD_ACCEPTANCE";
            public float radius=6,opacity=.15f;public int nativeOperations;public VaultSmoothingSample[] samples;
            public int rockPaintOperations;public bool paintSdfAndAlterationExact,paintMeshAndCollisionExact,paintHeightsAndHolesExact,paintRawMeshAndCollisionExact;public VaultMeshComparison paintGeometryComparison;
        }
        [Serializable]public sealed class VaultSmoothingSample
        {public Vector3 center,normalBefore;public float roofBefore,roofAfter;public string actualSystem;}
        static HashSet<Collider> VaultColliders(IEnumerable<DiggerSystem> systems)=>new HashSet<Collider>(systems.SelectMany(s=>s.GetComponentsInChildren<MeshCollider>(true)).Where(c=>c.enabled&&!c.isTrigger&&c.sharedMesh!=null));
        static RaycastHit ActualVaultHit(HashSet<Collider> colliders,Vector3 point)
        {
            var hits=Physics.RaycastAll(new Vector3(point.x,110,point.z),Vector3.up,100,~0,QueryTriggerInteraction.Ignore).Where(h=>colliders.Contains(h.collider)).OrderBy(h=>h.distance).ToArray();
            if(hits.Length==0)throw new InvalidOperationException("Actual pre/post native upper surface is required for the bounded smoothing trial.");return hits[0];
        }
        static string VaultHash(Action<BinaryWriter> write)
        {using(var stream=new MemoryStream()){using(var writer=new BinaryWriter(stream,System.Text.Encoding.UTF8,true))write(writer);using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(stream.GetBuffer(),0,checked((int)stream.Length)));}}
        static Dictionary<VoxelChunk,string> VaultSdfSnapshot(IEnumerable<DiggerSystem> systems)
            =>systems.SelectMany(s=>s.GetComponentsInChildren<VoxelChunk>(true)).Where(c=>c.IsLoaded).ToDictionary(c=>c,c=>VaultHash(w=>{foreach(var voxel in c.VoxelArray){w.Write(voxel.Value);w.Write(voxel.Alteration);}}));
        static Dictionary<string,VaultMeshGeometryState> VaultMeshSnapshot(IEnumerable<DiggerSystem> systems)=>CaptureVaultMeshStates(systems);
        static string VaultTerrainSnapshot(IEnumerable<DiggerSystem> systems)=>VaultHash(w=>{foreach(var terrain in systems.Select(s=>s.Terrain).Distinct()){w.Write(DragonKingdomWorldVisualMigration.HeightHash(terrain.terrainData));foreach(bool hole in terrain.terrainData.GetHoles(0,0,terrain.terrainData.holesResolution,terrain.terrainData.holesResolution))w.Write(hole);}});
        static async Task PaintMeasuredVaultRock(List<DiggerSystem> systems,VaultSmoothingEvidence proof)
        {
            var sdf=VaultSdfSnapshot(systems);var meshes=VaultMeshSnapshot(systems);string terrain=VaultTerrainSnapshot(systems);
            foreach(var row in proof.samples)
            {
                var owner=systems.Single(s=>s.BasePathData.Replace('\\','/')==row.actualSystem);
                // Match the installed URP PaintOperationEditor. AddTexture
                // saturates rock whether it is first, second or a new index;
                // OpacityIsTarget=true would incorrectly select the other slot
                // when rock is already the first texture.
                var paint=new BasicOperation{Params=new ModificationParameters{Action=ActionType.Paint,Brush=BrushType.Sphere,Position=(float3)row.center,Size=new float3(proof.radius),TextureIndex=3,Opacity=1,OpacityIsTarget=false}};
                await owner.ModifyWithoutMeshes(paint);proof.rockPaintOperations++;
            }
            foreach(var system in systems)await system.BuildPendingMeshesAsync(false);Physics.SyncTransforms();var afterSdf=VaultSdfSnapshot(systems);var afterMeshes=VaultMeshSnapshot(systems);
            proof.paintSdfAndAlterationExact=sdf.Count==afterSdf.Count&&sdf.All(p=>afterSdf.TryGetValue(p.Key,out string value)&&value==p.Value);
            proof.paintGeometryComparison=CompareVaultMeshStates(meshes,afterMeshes);proof.paintRawMeshAndCollisionExact=proof.paintGeometryComparison.rawOrderExact;proof.paintMeshAndCollisionExact=proof.paintGeometryComparison.canonicalExact;
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonOutput");string output=at>=0&&at+1<args.Length?args[at+1]:"Logs/TruckSports/DragonKingdom";Directory.CreateDirectory(output);File.WriteAllText(Path.Combine(output,"vault-paint-exact-geometry-comparison.json"),UnityEngine.JsonUtility.ToJson(proof.paintGeometryComparison,true));
            proof.paintHeightsAndHolesExact=terrain==VaultTerrainSnapshot(systems);
            if(proof.rockPaintOperations!=3||!proof.paintSdfAndAlterationExact||!proof.paintMeshAndCollisionExact||!proof.paintHeightsAndHolesExact)throw new InvalidOperationException("Native rock paint rejects: operations="+proof.rockPaintOperations+", sdfAndAlterationExact="+proof.paintSdfAndAlterationExact+" (chunks "+sdf.Count+"/"+afterSdf.Count+"), rawMeshOrderExact="+proof.paintRawMeshAndCollisionExact+", canonicalMeshNormalsTopologyCollisionExact="+proof.paintMeshAndCollisionExact+" (bindings "+meshes.Count+"/"+afterMeshes.Count+"), terrainExact="+proof.paintHeightsAndHolesExact);
        }
        static async Task<VaultSmoothingEvidence> SmoothMeasuredUpperVault(List<DiggerSystem> systems,Evidence evidence)
        {
            var proof=evidence.vaultSmoothing=new VaultSmoothingEvidence();var rows=new List<VaultSmoothingSample>();Physics.SyncTransforms();var before=VaultColliders(systems);
            // Actual collider contacts choose Y. All three hints stay thirty
            // metres from the tile seam; only a six-metre native sphere edits
            // each measured upper surface. No floor or whole-room smoothing.
            foreach(var hint in new[]{new Vector3(435,0,770),new Vector3(490,0,770),new Vector3(550,0,830)})
            {
                var hit=ActualVaultHit(before,hint);var center=hit.point;
                if(center.y-proof.radius<=115||hit.normal.y>=-.1f)throw new InvalidOperationException("Measured smoothing sphere must remain well above the original105m floor on a genuine underside surface.");
                var owners=systems.Where(s=>center.x>s.Terrain.transform.position.x&&center.x<s.Terrain.transform.position.x+s.Terrain.terrainData.size.x&&center.z>s.Terrain.transform.position.z&&center.z<s.Terrain.transform.position.z+s.Terrain.terrainData.size.z).ToArray();
                if(owners.Length!=1)throw new InvalidOperationException("One actual tile must own each measured smoothing center.");var owner=owners[0];var terrain=owner.Terrain;var local=center-terrain.transform.position;float margin=proof.radius+2*Mathf.Max(owner.HeightmapScale.x,owner.HeightmapScale.z);
                if(local.x<=margin||local.z<=margin||terrain.terrainData.size.x-local.x<=margin||terrain.terrainData.size.z-local.z<=margin)throw new InvalidOperationException("Preserve native tile-edge neighborhood; no smoothing sphere may touch a Digger seam.");
                float top=terrain.transform.position.y+terrain.terrainData.GetInterpolatedHeight(local.x/terrain.terrainData.size.x,local.z/terrain.terrainData.size.z);
                if(top-center.y-proof.radius<10)throw new InvalidOperationException("The whole native smoothing sphere must retain ten metres of actual overhead terrain mass.");
                var operation=new KernelOperation{Params=new ModificationParameters{Action=ActionType.Smooth,Brush=BrushType.Sphere,Position=(float3)center,Size=new float3(proof.radius),Opacity=proof.opacity}};
                if(!operation.GetAreaToModify(owner).NeedsModification)throw new InvalidOperationException("Measured native smoothing target is not modifiable.");
                await owner.ModifyWithoutMeshes(operation);proof.nativeOperations++;rows.Add(new VaultSmoothingSample{center=center,normalBefore=hit.normal,roofBefore=center.y,actualSystem=owner.BasePathData.Replace('\\','/')});
            }
            foreach(var system in systems)await system.BuildPendingMeshesAsync(false);Physics.SyncTransforms();var after=VaultColliders(systems);foreach(var row in rows)row.roofAfter=ActualVaultHit(after,row.center).point.y;
            proof.samples=rows.ToArray();if(proof.nativeOperations!=3)throw new InvalidOperationException("Exactly three bounded vendor operations required; original full floor/vault/source gates still follow.");
            if(Environment.GetCommandLineArgs().Contains("-tsDragonCavernPaintSmoothedRock"))await PaintMeasuredVaultRock(systems,proof);return proof;
        }
    }
}
#endif

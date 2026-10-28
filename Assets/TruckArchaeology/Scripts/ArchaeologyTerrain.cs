using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Digger.Modules.Core.Sources;
using Digger.Modules.Runtime.Sources;

namespace LWS.TruckArchaeology
{
    // Digger alone owns voxels, mesh generation, terrain holes, collider baking and disk format.
    [DefaultExecutionOrder(-30)]public sealed class ArchaeologyTerrain : MonoBehaviour
    {
        public DiggerMasterRuntime runtime; public DiggerSystem system; public Terrain terrain;
        public int ActualEdits { get; private set; }
        public Vector3 FirstEditPosition {get;private set;}public float FirstEditOriginalY {get;private set;}
        public int ExposurePasses {get;private set;}public int ExposureRayQueries {get;private set;}
        readonly ArchaeologyMeshContact meshContact=new ArchaeologyMeshContact();public int ResidualMeshTargets{get;private set;}public int BottomSoilTargets{get;private set;}public int PhysicalPenetrationSamples{get;private set;}public int UnresolvedContactSamples{get;private set;}public Vector3 LastResidualSurface{get;private set;}public Vector3 LastResidualNormal{get;private set;}public int LastResidualTriangle{get;private set;}public int LastResidualSubmesh{get;private set;}
        bool pendingCut,pauseRequested;int lastEditFrame;
        public bool Quiescent=>Ready&&!pendingCut;
        public bool Ready => runtime!=null&&system!=null&&system.IsInitialized&&!runtime.IsRunningAsync;
        public string LivePrefix { get; private set; }
        void Awake(){LivePrefix="TruckArchaeology/Live/"+Guid.NewGuid().ToString("N");if(runtime!=null)runtime.SetPersistenceDataPathPrefix(LivePrefix);}
        public void Fresh(string matchId,bool clear=true)
        {
            if(!Quiescent)throw new InvalidOperationException("Actual initialized quiescent Digger PRO terrain is required; no excavation fallback.");
            SetPaused(false);LivePrefix="TruckArchaeology/Live/"+matchId;runtime.SetPersistenceDataPathPrefix(LivePrefix);if(clear)runtime.ClearScene();ActualEdits=ExposurePasses=ExposureRayQueries=ResidualMeshTargets=BottomSoilTargets=PhysicalPenetrationSamples=UnresolvedContactSamples=0;LastResidualSurface=LastResidualNormal=Vector3.zero;LastResidualTriangle=LastResidualSubmesh=-1;
        }
        public void SetPaused(bool value)
        {
            pauseRequested=value;if(runtime!=null)runtime.enabled=!value;
            // A synchronous edit can leave its original TerrainCutter mask application
            // queued for this frame's Update. Let that authorized edit finish once.
            if(system!=null&&system.Cutter!=null)system.Cutter.enabled=!value||pendingCut;
        }
        void LateUpdate()
        {
            if(pendingCut&&(Time.frameCount>lastEditFrame||pauseRequested))pendingCut=false;
            if(pauseRequested&&!pendingCut&&system!=null&&system.Cutter!=null)system.Cutter.enabled=false;
        }
        public bool IsTerrain(Collider collider)=>collider!=null&&(collider==terrain.GetComponent<TerrainCollider>()||collider.GetComponentInParent<DiggerSystem>()==system);
        public bool Surface(Vector3 point,out RaycastHit hit)
        {
            var hits=Physics.RaycastAll(new Vector3(point.x,24,point.z),Vector3.down,60,~0,QueryTriggerInteraction.Ignore).Where(h=>IsTerrain(h.collider)).OrderBy(h=>h.distance).ToArray();
            hit=hits.Length>0?hits[0]:default;return hits.Length>0;
        }
        public bool Cut(Vector3 point,float radius,float opacity)
        {
            if(!Ready||radius<.2f||radius>2.6f||opacity<=0||opacity>1)return false;
            if(ActualEdits==0){if(!Surface(point,out RaycastHit before))return false;FirstEditPosition=point;FirstEditOriginalY=before.point.y;}
            runtime.Modify(new ModificationParameters{Position=point,Brush=BrushType.Sphere,Action=ActionType.Dig,TextureIndex=0,Size=radius,Opacity=opacity});
            ActualEdits++;pendingCut=true;lastEditFrame=Time.frameCount;return true;
        }
        // Original outer-nine probes remain, supplemented by the actual box
        // footprint including its exact edges. Called only at exposure cadence,
        // not by AI/Reach every physics step (Artifact caches the work point).
        public static IEnumerable<Vector3> FootprintPoints(Bounds b)
        {
            int nx=Mathf.Clamp(Mathf.CeilToInt(b.size.x/.25f),1,24),nz=Mathf.Clamp(Mathf.CeilToInt(b.size.z/.25f),1,24);
            for(int x=0;x<=nx;x++)for(int z=0;z<=nz;z++)yield return new Vector3(Mathf.Lerp(b.min.x,b.max.x,x/(float)nx),b.center.y,Mathf.Lerp(b.min.z,b.max.z,z/(float)nz));
            for(int x=-1;x<=1;x++)for(int z=-1;z<=1;z++)yield return b.center+new Vector3(x*(b.extents.x+.25f),0,z*(b.extents.z+.25f));
        }
        public bool HasActualGroundHole(Vector3 point)
        {
            if(!Ready||terrain==null||terrain.terrainData==null||!terrain.gameObject.activeInHierarchy||!terrain.enabled||system.Cutter==null)return false;
            var original=terrain.GetComponent<TerrainCollider>();if(original==null||!original.enabled||original.terrainData!=terrain.terrainData)return false;
            Vector3 p=terrain.transform.InverseTransformPoint(point),size=terrain.terrainData.size;if(p.x<0||p.z<0||p.x>=size.x||p.z>=size.z)return false;
            int n=terrain.terrainData.holesResolution;int x=Mathf.Clamp(Mathf.FloorToInt(p.x/size.x*n),0,n-1),z=Mathf.Clamp(Mathf.FloorToInt(p.z/size.z*n),0,n-1);
            return terrain.terrainData.IsHole(x,z);
        }
        bool ContactPoint(BoxCollider artifact,Collider terrainCollider,Vector3 direction,float depth,out Vector3 point)
        {
            if(terrainCollider is MeshCollider mesh&&meshContact.TryFindSurface(artifact,mesh,out Vector3 surface,out Vector3 normal,out int submesh,out int triangle)){ResidualMeshTargets++;LastResidualSurface=surface;LastResidualNormal=normal;LastResidualTriangle=triangle;LastResidualSubmesh=submesh;point=surface-normal*.2f;return true;}
            Vector3 scale=artifact.transform.lossyScale;scale=new Vector3(Mathf.Abs(scale.x),Mathf.Abs(scale.y),Mathf.Abs(scale.z));Vector3 half=Vector3.Scale(artifact.size*.5f,scale),center=artifact.transform.TransformPoint(artifact.center);
            // A virtual query box, never a new collider or a moved live artifact.
            // Start just beyond PhysX's real minimum-separation vector, cast
            // back to that SAME terrain collider and cut the actual hit surface.
            var hits=Physics.BoxCastAll(center+direction*(depth+.08f),half,-direction,artifact.transform.rotation,depth+.16f,~0,QueryTriggerInteraction.Ignore);
            foreach(var hit in hits.OrderBy(h=>h.distance))if(hit.collider==terrainCollider&&hit.distance>1e-6f&&hit.normal.sqrMagnitude>.5f){point=hit.point-hit.normal*.2f;return true;}
            point=default;return false;
        }
        public float Exposure(BoxCollider artifact,out bool clear)=>Exposure(artifact,out clear,out _);
        public float Exposure(BoxCollider artifact,out bool clear,out Vector3 workPoint)
        {
            ExposurePasses++;Bounds b=artifact.bounds;int visible=0,total=0;bool hasSoil=false;float highest=float.NegativeInfinity;workPoint=b.center;
            foreach(Vector3 p in FootprintPoints(b))
            {
                total++;ExposureRayQueries++;if(Surface(p,out RaycastHit hit))
                {
                    if(hit.point.y<b.min.y+.12f)visible++;
                    // Visible is not physically clear: soil between actual
                    // bottom and bottom+.12 can still block the .015m veto.
                    if(hit.point.y>=b.min.y&&hit.point.y>highest){highest=hit.point.y;workPoint=hit.point-Vector3.up*.2f;hasSoil=true;}
                }
                else if(HasActualGroundHole(p))visible++;
            }
            clear=visible==total;var overlaps=Physics.OverlapBox(b.center,b.extents+Vector3.one*.08f,Quaternion.identity,~0,QueryTriggerInteraction.Ignore);float deepest=0;bool contact=false;Vector3 contactTarget=default;
            foreach(var other in overlaps)if(IsTerrain(other)&&Physics.ComputePenetration(artifact,artifact.transform.position,artifact.transform.rotation,other,other.transform.position,other.transform.rotation,out Vector3 direction,out float depth)&&depth>.015f)
            {
                clear=false;PhysicalPenetrationSamples++;if(!hasSoil&&depth>deepest&&ContactPoint(artifact,other,direction,depth,out Vector3 point)){deepest=depth;contactTarget=point;contact=true;}
            }
            if(hasSoil&&highest<b.min.y+.12f)BottomSoilTargets++;if(!hasSoil&&contact)workPoint=contactTarget;else if(!hasSoil&&!clear){UnresolvedContactSamples++;workPoint=Vector3.positiveInfinity;}
            return visible/(float)total;
        }
        public string PersistSnapshot()
        {
            if(Application.isEditor)throw new InvalidOperationException("Digger PersistAll is an Editor no-op. Actual disk acceptance requires the opt-in Windows fixture.");
            if(!Quiescent)throw new InvalidOperationException("Digger must complete its already-issued cutter frame before saving.");
            var chunks=system.GetComponentsInChildren<VoxelChunk>(true);foreach(var chunk in chunks)if(!chunk.IsLoaded)chunk.Load();
            string prefix="TruckArchaeology/Saves/"+Guid.NewGuid().ToString("N");runtime.SetPersistenceDataPathPrefix(prefix);
            try
            {
                // PersistAll saves its dirty set; a new immutable snapshot requires every existing voxel chunk.
                foreach(var chunk in chunks)system.ChunksToPersist.Add(chunk);
                runtime.PersistAll();
                if(!File.Exists(system.TerrainHolesRuntimePath))throw new IOException("Actual Digger terrain hole data was not persisted.");
                return prefix;
            }
            finally{runtime.SetPersistenceDataPathPrefix(LivePrefix);}
        }
        public bool ValidateSnapshot(string prefix,out string reason)
        {
            reason="";if(Application.isEditor){reason="Vendor persistence has no Editor implementation; run actual Windows fixture.";return false;}
            if(prefix==null||!System.Text.RegularExpressions.Regex.IsMatch(prefix,"^TruckArchaeology/Saves/[a-f0-9]{32}$")){reason="Unknown terrain snapshot ID.";return false;}
            string folder=Path.Combine(Application.persistentDataPath,"DiggerData",prefix,system.Guid);
            if(!File.Exists(Path.Combine(folder,"terrain.holes"))){reason="Digger terrain snapshot missing.";return false;}return true;
        }
        public void RestoreSnapshot(string prefix)
        {
            if(!Quiescent)throw new InvalidOperationException("Actual Digger must be quiescent before restore.");
            if(!ValidateSnapshot(prefix,out string reason))throw new InvalidOperationException(reason);
            runtime.SetPersistenceDataPathPrefix(prefix);system.Init(LoadType.Minimal_and_LoadVoxels_and_RebuildMeshes);runtime.RefreshTerrainList();runtime.SetPersistenceDataPathPrefix(LivePrefix);
        }
    }
}

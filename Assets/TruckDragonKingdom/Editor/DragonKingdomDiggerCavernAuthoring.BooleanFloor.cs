#if UNITY_EDITOR && VISTA
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Digger.Modules.Core.Sources;
using Digger.Modules.Core.Sources.Operations;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace LWS.TruckDragonKingdom.Editor
{
    public static partial class DragonKingdomDiggerCavernAuthoring
    {
        [Serializable] public sealed class AuthoredFloorEvidence
        {
            public string scope = "SUPPORTED_DIGGER_SIGNED_HALFSPACE_INTERSECTION_NOT_VISUAL_ACCEPTANCE";
            public Bounds originalFloorFootprint;
            public float worldFloorHeight, lowerBound, maximumExteriorBand;
            public int appliedPasses = 1, modifiedChunks, changedAbovePlaneSamples, changedBelowPlaneSamples;
            public bool usesBothSignedSides = true, sourceOutsideFootprintUntouchedByJob = true;
        }

        static async Task<AuthoredFloorEvidence> ApplyAuthoredCavernFloor(List<DiggerSystem> systems, Bounds floorFootprint)
        {
            if (systems == null || systems.Count == 0 || systems.Any(s => !s))
                throw new InvalidOperationException("Actual candidate Digger systems required for signed authored floor.");
            var operation = new BooleanCavernFloorOperation(floorFootprint);
            foreach (var system in systems) await system.ModifyWithoutMeshes(operation);
            return operation.evidence;
        }

        public sealed class BooleanCavernFloorOperation : IOperation<BooleanCavernJob>
        {
            internal readonly AuthoredFloorEvidence evidence;
            readonly Bounds footprint;
            public BooleanCavernFloorOperation(Bounds floorFootprint)
            {
                if (!math.all(math.isfinite((float3)floorFootprint.center)) ||
                    !math.all(math.isfinite((float3)floorFootprint.size)) || math.any((float3)floorFootprint.size <= 0) ||
                    floorFootprint.max.y != 105)
                    throw new InvalidOperationException("Original finite measured floor footprint with exact authored 105m maximum required.");
                footprint = floorFootprint;
                evidence = new AuthoredFloorEvidence { originalFloorFootprint = footprint, worldFloorHeight = footprint.max.y, lowerBound = footprint.min.y };
            }

            public ModificationArea GetAreaToModify(DiggerSystem digger)
            {
                float band = 3 * math.cmax((float3)digger.HeightmapScale);
                float top = footprint.max.y + band;
                var center = new float3(footprint.center.x, (footprint.min.y + top) * .5f, footprint.center.z);
                var half = new float3(footprint.extents.x, (top - footprint.min.y) * .5f, footprint.extents.z);
                return ModificationAreaUtils.GetAABBAreaToModify(digger, center, half);
            }

            public BooleanCavernJob Do(VoxelChunk chunk)
            {
                float band = 3 * math.cmax(chunk.HeightmapScale);
                evidence.modifiedChunks++;
                evidence.maximumExteriorBand = math.max(evidence.maximumExteriorBand, band);
                return new BooleanCavernJob
                {
                    SizeVox = chunk.SizeVox, SizeVox2 = chunk.SizeVox * chunk.SizeVox,
                    HeightmapScale = chunk.HeightmapScale, ChunkAltitude = chunk.WorldPosition.y,
                    ClipAuthoredFloor = true, AbsoluteWorldOrigin = chunk.AbsoluteWorldPosition,
                    FloorMinimumXZ = new float2(footprint.min.x, footprint.min.z),
                    FloorMaximumXZ = new float2(footprint.max.x, footprint.max.z),
                    FloorHeight = footprint.max.y, FloorMinimumY = footprint.min.y, ExteriorBand = band,
                    TextureIndex = 3,
                    InputVoxels = new NativeArray<Voxel>(0, Allocator.Persistent),
                    Voxels = new NativeArray<Voxel>(chunk.VoxelArray, Allocator.Persistent),
                    Heights = new NativeArray<float>(chunk.HeightArray, Allocator.Persistent),
                    Holes = new NativeArray<int>(chunk.HolesArray, Allocator.Persistent),
                    NewHolesConcurrentCounter = new NativeArray<int>(1, Allocator.Persistent),
                    ChangedSamples = new NativeArray<int>(2, Allocator.Persistent)
                };
            }

            public void Complete(BooleanCavernJob job, VoxelChunk chunk)
            {
                try
                {
                    job.Voxels.CopyTo(chunk.VoxelArray);
                    evidence.changedAbovePlaneSamples += job.ChangedSamples[0];
                    evidence.changedBelowPlaneSamples += job.ChangedSamples[1];
                    if (job.NewHolesConcurrentCounter[0] > 0)
                        chunk.Cutter.Cut(job.Holes, chunk.VoxelPosition, chunk.ChunkPosition);
                }
                finally { job.Dispose(); }
            }
        }
    }
}
#endif

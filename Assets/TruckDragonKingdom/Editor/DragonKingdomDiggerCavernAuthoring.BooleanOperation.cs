#if UNITY_EDITOR && VISTA
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Digger.Modules.Core.Sources;
using Digger.Modules.Core.Sources.Operations;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;

namespace LWS.TruckDragonKingdom.Editor
{
    public static partial class DragonKingdomDiggerCavernAuthoring
    {
        [Serializable] public sealed class AuthoredUnionEvidence
        {
            public string scope = "SUPPORTED_DIGGER_SIGNED_BOOLEAN_OPERATION_NOT_VISUAL_OR_RUNTIME_ACCEPTANCE";
            public int sourceOperations, analyticLobes, vendorCustomCorridors, appliedPasses = 1;
            public bool usesSignedExteriorBand = true, vendorCustomInputsExact;
            public AuthoredUnionOperandEvidence[] operands;
        }
        [Serializable] public sealed class AuthoredUnionOperandEvidence
        {
            public string brush, inputBeforeSha256, inputAfterSha256;
            public UnityEngine.Vector3 position, size;
            public int vendorInputVoxels, modifiedChunks, changedInteriorSamples, changedExteriorSamples;
        }
        static string BooleanInputHash(Voxel[] input)
        {
            if (input == null) return null;
            using (var copy = new NativeArray<Voxel>(input, Allocator.Temp))
            using (var hash = SHA256.Create())
                return BitConverter.ToString(hash.ComputeHash(copy.Reinterpret<byte>(8).ToArray())).Replace("-", "").ToLowerInvariant();
        }
        static async Task<AuthoredUnionEvidence> ApplyAuthoredCavernUnion(List<DiggerSystem> systems, List<BasicOperation> operations)
        {
            if (systems == null || systems.Count == 0 || systems.Any(s => !s) || operations == null || operations.Count == 0)
                throw new InvalidOperationException("Fresh actual Digger systems and complete authored operation list required.");
            // Validate ALL source operands before any candidate mutation.
            var adapters = operations.Select(op => new BooleanCavernOperation(op)).ToArray();
            var rows = new List<AuthoredUnionOperandEvidence>();
            foreach (var operation in adapters)
            {
                foreach (var system in systems) await system.ModifyWithoutMeshes(operation);
                operation.evidence.inputAfterSha256 = BooleanInputHash(operation.input.CustomBrush ? operation.input.CustomBrush.InputVoxels : null);
                if (operation.evidence.inputBeforeSha256 != operation.evidence.inputAfterSha256)
                    throw new InvalidOperationException("Vendor signed brush input changed during Boolean candidate operation.");
                rows.Add(operation.evidence);
            }
            return new AuthoredUnionEvidence
            {
                sourceOperations = adapters.Length,
                analyticLobes = operations.Count(op => op.Params.Brush == BrushType.Sphere),
                vendorCustomCorridors = operations.Count(op => op.Params.Brush == BrushType.Custom),
                vendorCustomInputsExact = true,
                operands = rows.ToArray()
            };
        }
        // Authored Boolean subtraction semantics through Digger's supported
        // operation extension. Digger still owns chunk scheduling, mesh/hole/
        // collision generation, undo and persistence. Never applied implicitly.
        public sealed class BooleanCavernOperation : IOperation<BooleanCavernJob>
        {
            internal readonly ModificationParameters input;
            internal readonly AuthoredUnionOperandEvidence evidence;
            public BooleanCavernOperation(BasicOperation source)
            {
                if (source == null) throw new ArgumentNullException(nameof(source));
                input = source.Params;
                if (input.Action != ActionType.Dig ||
                    (input.Brush != BrushType.Sphere && input.Brush != BrushType.Custom) ||
                    !math.all(math.isfinite(input.Position)) || !math.all(math.isfinite(input.Size)) ||
                    math.any(input.Size <= 0) || input.TextureIndex < 0 || input.TextureIndex > 31 || input.Opacity != 1 || input.OpacityIsTarget)
                    throw new InvalidOperationException("Explicit finite Sphere/Custom Dig source and valid rock texture required for candidate Boolean union.");
                if (input.Brush == BrushType.Custom &&
                    (!input.CustomBrush || input.CustomBrush.InputVoxels == null ||
                     input.CustomBrush.InputVoxels.Length == 0 ||
                     input.CustomBrush.InputVoxels.Length != (long)input.CustomBrush.InputSizeVox.x * input.CustomBrush.InputSizeVox.y * input.CustomBrush.InputSizeVox.z ||
                     input.CustomBrush.InputVoxels.Any(v => !float.IsFinite(v.Value))))
                    throw new InvalidOperationException("Actual vendor-computed complete signed CustomBrush input required; no synthetic missing-data replacement.");
                evidence = new AuthoredUnionOperandEvidence
                {
                    brush = input.Brush.ToString(), position = input.Position, size = input.Size,
                    vendorInputVoxels = input.CustomBrush ? input.CustomBrush.InputVoxels.Length : 0,
                    inputBeforeSha256 = BooleanInputHash(input.CustomBrush ? input.CustomBrush.InputVoxels : null)
                };
            }

            public ModificationArea GetAreaToModify(DiggerSystem digger)
                => new BasicOperation { Params = input }.GetAreaToModify(digger);

            public BooleanCavernJob Do(VoxelChunk chunk)
            {
                evidence.modifiedChunks++;
                var custom = input.Brush == BrushType.Custom ? input.CustomBrush : null;
                return new BooleanCavernJob
                {
                    SizeVox = chunk.SizeVox,
                    SizeVox2 = chunk.SizeVox * chunk.SizeVox,
                    HeightmapScale = chunk.HeightmapScale,
                    ChunkAltitude = chunk.WorldPosition.y,
                    Center = input.Position - chunk.AbsoluteWorldPosition,
                    Size = input.Size,
                    Brush = input.Brush,
                    TextureIndex = (uint)input.TextureIndex,
                    // Three lattice steps include the negative side needed by
                    // interpolation and the native forward-difference stencil.
                    ExteriorBand = 3 * math.cmax(chunk.HeightmapScale),
                    InputOriginVox = custom ? Utils.UnityToVoxelPosition(input.Position, chunk.HeightmapScale) - chunk.AbsoluteVoxelPosition - (int3)math.round(custom.InputOriginVox * input.Size) : int3.zero,
                    InputSizeVox = custom ? custom.InputSizeVox : int3.zero,
                    InputVoxels = custom ? new NativeArray<Voxel>(custom.InputVoxels, Allocator.Persistent) : new NativeArray<Voxel>(0, Allocator.Persistent),
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
                    evidence.changedInteriorSamples += job.ChangedSamples[0];
                    evidence.changedExteriorSamples += job.ChangedSamples[1];
                    if (job.NewHolesConcurrentCounter[0] > 0)
                        chunk.Cutter.Cut(job.Holes, chunk.VoxelPosition, chunk.ChunkPosition);
                }
                finally { job.Dispose(); }
            }
        }

        public struct BooleanCavernJob : IJobParallelFor, IDisposable
        {
            public int SizeVox, SizeVox2;
            public float3 HeightmapScale, Center, Size;
            public float ChunkAltitude, ExteriorBand;
            public bool ClipAuthoredFloor;
            public float3 AbsoluteWorldOrigin;
            public float2 FloorMinimumXZ, FloorMaximumXZ;
            public float FloorHeight, FloorMinimumY;
            public BrushType Brush;
            public uint TextureIndex;
            public int3 InputOriginVox, InputSizeVox;
            [ReadOnly] public NativeArray<float> Heights;
            [ReadOnly] public NativeArray<Voxel> InputVoxels;
            public NativeArray<Voxel> Voxels;
            [NativeDisableParallelForRestriction] public NativeArray<int> Holes;
            [NativeDisableParallelForRestriction] public NativeArray<int> NewHolesConcurrentCounter;
            [NativeDisableParallelForRestriction] public NativeArray<int> ChangedSamples;

            public void Execute(int index)
            {
                var pi = Utils.IndexToXYZ(index, SizeVox, SizeVox2);
                var point = pi * HeightmapScale;
                float field;
                if (ClipAuthoredFloor)
                {
                    var world = point + AbsoluteWorldOrigin;
                    if (world.x < FloorMinimumXZ.x || world.x > FloorMaximumXZ.x ||
                        world.z < FloorMinimumXZ.y || world.z > FloorMaximumXZ.y ||
                        world.y < FloorMinimumY || world.y > FloorHeight + ExteriorBand) return;
                    field = world.y - FloorHeight;
                }
                else if (Brush == BrushType.Sphere)
                {
                    // The existing native Sphere's vertical-ellipsoid field.
                    var delta = point - Center;
                    float ratio = Size.x / math.max(Size.y, .01f);
                    field = Size.x - math.sqrt(delta.x * delta.x + delta.y * delta.y * ratio * ratio + delta.z * delta.z);
                }
                else
                {
                    // Identical public CustomBrush lattice correspondence to
                    // BasicOperation; retain BOTH signed sides of its output.
                    var ip = (int3)math.round((pi - InputOriginVox) / Size);
                    if (math.any(ip < 0) || math.any(ip >= InputSizeVox)) return;
                    field = InputVoxels[ip.x * InputSizeVox.y * InputSizeVox.z + ip.y * InputSizeVox.z + ip.z].Value;
                }
                if (!math.isfinite(field) || (!ClipAuthoredFloor && field < -ExteriorBand)) return;
                var voxel = Voxels[index];
                float united = ClipAuthoredFloor ? math.min(voxel.Value, field) : math.max(voxel.Value, field);
                if (ClipAuthoredFloor ? !(united < voxel.Value) : !(united > voxel.Value)) return;
                Digger.Modules.Core.Sources.NativeCollections.Utils.IncrementAt(ChangedSamples, field >= 0 ? 0 : 1);
                voxel.Value = united;
                voxel.Alteration = Voxel.FarAboveSurface;
                voxel.AddTexture(TextureIndex, 1);
                float terrainValue = point.y + ChunkAltitude - Heights[Utils.XYZToHeightIndex(pi, SizeVox)];
                voxel = Utils.AdjustAlteration(voxel, pi, HeightmapScale.y, point.y + ChunkAltitude, terrainValue, SizeVox, Heights);
                if (voxel.IsAlteredNearBelowSurface || voxel.IsAlteredNearAboveSurface)
                {
                    Digger.Modules.Core.Sources.NativeCollections.Utils.IncrementAt(NewHolesConcurrentCounter, 0);
                    Digger.Modules.Core.Sources.NativeCollections.Utils.IncrementAt(Holes, Utils.XZToHoleIndex(pi.x, pi.z, SizeVox));
                    if (pi.x >= 1)
                    {
                        Digger.Modules.Core.Sources.NativeCollections.Utils.IncrementAt(Holes, Utils.XZToHoleIndex(pi.x - 1, pi.z, SizeVox));
                        if (pi.z >= 1) Digger.Modules.Core.Sources.NativeCollections.Utils.IncrementAt(Holes, Utils.XZToHoleIndex(pi.x - 1, pi.z - 1, SizeVox));
                    }
                    if (pi.z >= 1) Digger.Modules.Core.Sources.NativeCollections.Utils.IncrementAt(Holes, Utils.XZToHoleIndex(pi.x, pi.z - 1, SizeVox));
                }
                Voxels[index] = voxel;
            }

            public void Dispose()
            {
                if (Voxels.IsCreated) Voxels.Dispose();
                if (Heights.IsCreated) Heights.Dispose();
                if (InputVoxels.IsCreated) InputVoxels.Dispose();
                if (Holes.IsCreated) Holes.Dispose();
                if (NewHolesConcurrentCounter.IsCreated) NewHolesConcurrentCounter.Dispose();
                if (ChangedSamples.IsCreated) ChangedSamples.Dispose();
            }
        }
    }
}
#endif

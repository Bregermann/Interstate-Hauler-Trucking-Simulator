#if UNITY_EDITOR && VISTA
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Digger.Modules.Core.Sources;
using Digger.Modules.Core.Sources.Operations;
using Digger.Modules.Core.Sources.Jobs;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace LWS.TruckDragonKingdom.Editor
{
    public static partial class DragonKingdomDiggerCavernAuthoring
    {
        [Serializable]public sealed class RoundedProfileEvidence
        {
            public string profile="RoundedLobes01",scope="ACTUAL_DIGGER_INPUT_AND_MEASURED_RECORDED_OCCUPANCY_COLUMNS_NOT_FULL_LIVE_PURSUIT_OR_VISUAL_ACCEPTANCE";
            public string sourceEvidence,sourceSha256,liveEvidence,liveSha256;
            public int sourceActions,sourceFrames,liveFrames,liveFlightFrames,requiredColumns;
            public bool partialVisitedLiveEvidence=true,nativeRequiredColumnsPassed;
            public Vector3[] lobeCenters;public Bounds excavationBounds;
            public int[] vendorInputVoxelCounts;
            public bool archedCorridorsContainEntireOriginalClearanceBox;public float archedCorridorRise;
            public string rasterClearancePolicy="OriginalNominalWidth";public float measuredRoadSideGuardBand;
            public bool nativeAnalyticLobeInputs;public Vector3[] analyticLobeCenters=Array.Empty<Vector3>(),analyticLobeRadii=Array.Empty<Vector3>();
        }
        sealed class RoundedColumn{public Vector3 position;public float floor=105,requiredTop;}
        [Serializable]sealed class LiveSource{public string path,beforeSha256,afterSha256;}
        [Serializable]sealed class LiveFrame{public Bounds actualGuardianSkin;}
        [Serializable]sealed class LiveOccupancy
        {public int sampledFrames,actualFlightFrames;public bool telemetryTruncated,sourceBytesExact;public LiveSource[] sources;public LiveFrame[] frames;}
        static readonly Vector3[] RoundedCenters={new Vector3(490,132.5f,790),new Vector3(550,132.5f,790)};
        static string ProfileArgument(string flag)
        {var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,flag);return at<0?null:at+1<args.Length?args[at+1]:throw new ArgumentException("Missing explicit cavern profile argument: "+flag);}
        static bool RoundedProfileRequested()
        {string mode=ProfileArgument("-tsDragonCavernProfile");if(mode!=null&&mode!="RoundedLobes01"&&mode!="RoundedLobes02")throw new ArgumentException("Unknown explicit Digger cavern profile: "+mode);return mode!=null;}
        static bool RoundedArchedCorridorsRequested()=>ProfileArgument("-tsDragonCavernProfile")=="RoundedLobes02";
        static bool NativeAnalyticLobesRequested()=>Environment.GetCommandLineArgs().Contains("-tsDragonCavernAnalyticLobes");
        static IEnumerable<Vector3> LegacyRoofMassColumns()
        {for(int z=-50;z<=50;z+=10)for(int x=-80;x<=80;x+=10)yield return new Vector3(520+x,0,780+z);}
        static float RoundedCeiling(Vector3 p)
            =>RoundedCenters.Max(c=>DragonKingdomRoundedCavernBrushProfile.AnalyticCeiling(65,130,160,Vector2.Distance(new Vector2(p.x,p.z),new Vector2(c.x,c.z))));
        static IEnumerable<Vector3> RoundedRoofMassColumns()
        {
            // Actual entire lobe footprint, including its outer wall: no
            // rectangular old probe grid is substituted for rounded roof mass.
            foreach(var c in RoundedCenters)for(int z=-65;z<=65;z+=5)for(int x=-65;x<=65;x+=5)
                if(x*x+z*z<=65*65)yield return new Vector3(c.x+x,0,c.z+z);
        }
        static string FileSha(string path)
        {using(var sha=System.Security.Cryptography.SHA256.Create())return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(CavernIo(path)))).Replace("-","").ToLowerInvariant();}
        static string CavernIo(string path)
        {string full=Path.GetFullPath(path);return Application.platform==RuntimePlatform.WindowsEditor&&full.Length>=248&&!full.StartsWith(@"\\?\",StringComparison.Ordinal)?@"\\?\"+full:full;}
        static void RequireSource(string path,string hash)
        {
            // Package source snapshots describe the prior renderer, not dragon
            // geometry. All actual Assets dependencies (including FBX import
            // metadata) remain an exact guard in this isolated experiment.
            if(!path.StartsWith("Assets/",StringComparison.Ordinal))return;
            if(!File.Exists(CavernIo(path))||FileSha(path)!=hash)throw new InvalidOperationException("Recorded actual cavern occupancy source changed: "+path);
        }
        static RoundedColumn[] PrepareRoundedProfile(Evidence evidence)
        {
            var proof=evidence.roundedProfile=new RoundedProfileEvidence{sourceEvidence=ProfileArgument("-tsDragonCavernOccupancyEvidence"),liveEvidence=ProfileArgument("-tsDragonCavernLiveEvidence"),lobeCenters=RoundedCenters.ToArray(),excavationBounds=new Bounds(new Vector3(520,132.5f,790),new Vector3(190,55,130))};
            proof.profile=ProfileArgument("-tsDragonCavernProfile")??"RoundedLobes01";proof.archedCorridorsContainEntireOriginalClearanceBox=RoundedArchedCorridorsRequested();proof.archedCorridorRise=RoundedArchedCorridorsRequested()?8:0;
            if(string.IsNullOrEmpty(proof.sourceEvidence)||string.IsNullOrEmpty(proof.liveEvidence))throw new InvalidOperationException("Rounded profile requires both explicit original-action and recorded actual-world-skin evidence.");
            proof.sourceSha256=FileSha(proof.sourceEvidence);proof.liveSha256=FileSha(proof.liveEvidence);
            // These immutable, previously inspected native receipts establish
            // the precise input cohort. They are partial occupancy evidence,
            // never authority to exclude unvisited supported pursuit/flight.
            if(proof.sourceSha256!="e416c05571a5639224e62d5b37b52f84d256e26037b112a3c772fc8598bc7ed3"||proof.liveSha256!="3eaa23ac338e7dbd0f28c179ccace11eaad51c8b0adbc3ce4def2b418b5b5cff")throw new InvalidOperationException("Rounded authoring input differs from the inspected original-action/live-world-skin cohort.");
            var source=JsonUtility.FromJson<DragonKingdomRegionalWorldIntegration.CavernOccupancyEvidence>(File.ReadAllText(CavernIo(proof.sourceEvidence)));
            var live=JsonUtility.FromJson<LiveOccupancy>(File.ReadAllText(CavernIo(proof.liveEvidence)));
            if(!source.sourceBytesPreserved||!source.originalGuardianMeshMembershipExact||source.actions==null||source.actions.Length!=31||source.protectedSources==null||live.frames==null||live.frames.Length!=2913||live.sampledFrames!=2913||live.actualFlightFrames!=2622||live.telemetryTruncated||!live.sourceBytesExact||live.sources==null)throw new InvalidOperationException("Complete recorded source-action and untruncated visited-world skin inputs required; no wrapper-pose substitution.");
            foreach(var item in source.protectedSources)RequireSource(item.path,item.sha256);
            foreach(var item in live.sources){if(item.beforeSha256!=item.afterSha256)throw new InvalidOperationException("Visited-world input changed source bytes.");RequireSource(item.path,item.afterSha256);}
            proof.sourceActions=source.actions.Length;proof.sourceFrames=source.actionSamples;proof.liveFrames=live.sampledFrames;proof.liveFlightFrames=live.actualFlightFrames;
            var columns=new List<RoundedColumn>();
            // Interior floor/variable-height vault columns. The rounded outer
            // wall is intentional; a flat 42m rectangular ceiling is not.
            for(int z=735;z<=845;z+=10)for(int x=440;x<=605;x+=10)
            {var p=new Vector3(x,0,z);if(RoundedCenters.Any(c=>Vector2.Distance(new Vector2(x,z),new Vector2(c.x,c.z))<=57))columns.Add(new RoundedColumn{position=p,requiredTop=RoundedCeiling(p)-1.5f});}
            void Skin(Bounds b)
            {
                if(!float.IsFinite(b.center.x)||!float.IsFinite(b.center.y)||!float.IsFinite(b.center.z)||!float.IsFinite(b.extents.x)||!float.IsFinite(b.extents.y)||!float.IsFinite(b.extents.z)||b.extents.x<=0||b.extents.y<=0||b.extents.z<=0)throw new InvalidOperationException("Actual finite nonempty skin bounds required.");
                foreach(float x in new[]{b.min.x,b.max.x})foreach(float z in new[]{b.min.z,b.max.z})
                {var p=new Vector3(x,0,z);if(RoundedCeiling(p)<b.max.y+3)throw new InvalidOperationException("Rounded input cannot contain the recorded actual source/live skin with3m overhead margin.");columns.Add(new RoundedColumn{position=p,requiredTop=b.max.y+3});}
            }
            foreach(var action in source.actions)Skin(action.actualWorldSkinBounds);
            foreach(var frame in live.frames)Skin(frame.actualGuardianSkin);
            return columns.ToArray();
        }
        static IEnumerable<BasicOperation> RoundedVolumes(List<GameObject> temporary,List<Mesh> meshes,Evidence evidence)
        {
            if(NativeAnalyticLobesRequested())
            {
                if(!RoundedArchedCorridorsRequested())throw new InvalidOperationException("Native lobe comparison requires the original explicit RoundedLobes02 clearance contract.");
                var centers=new List<Vector3>();var radii=new List<Vector3>();
                foreach(var center in RoundedCenters)
                {
                    centers.Add(new Vector3(center.x,130,center.z));radii.Add(new Vector3(65,30,65));
                    centers.Add(new Vector3(center.x,115,center.z));radii.Add(new Vector3(65,25,65));
                }
                evidence.roundedProfile.nativeAnalyticLobeInputs=true;evidence.roundedProfile.analyticLobeCenters=centers.ToArray();evidence.roundedProfile.analyticLobeRadii=radii.ToArray();evidence.roundedProfile.vendorInputVoxelCounts=Array.Empty<int>();
                // Native Sphere supports a genuine vertical ellipsoid. The
                // upper cap retains exactly the original analytic roof; the
                // lower lobe provides clearance down to the existing105m floor.
                // Existing native Reset, all11946 contacts and unchanged road,
                // source and roof-mass gates remain mandatory, not assumed.
                evidence.roundedProfile.excavationBounds=new Bounds(new Vector3(520,125,790),new Vector3(190,70,130));
                for(int i=0;i<centers.Count;i++)yield return new BasicOperation{Params=new ModificationParameters{Action=ActionType.Dig,Brush=BrushType.Sphere,Position=centers[i],Size=(float3)radii[i],Opacity=1,TextureIndex=3}};
                yield break;
            }
            var geometry=DragonKingdomRoundedCavernBrushProfile.Create(65,105,130,160,64,12);meshes.Add(geometry.mesh);var counts=new List<int>();
            foreach(var center in RoundedCenters)
            {
                var go=new GameObject("Temporary Digger original rounded lobe input"){hideFlags=HideFlags.HideAndDontSave};temporary.Add(go);go.transform.position=center;
                go.AddComponent<MeshFilter>().sharedMesh=geometry.mesh;
                var brush=go.AddComponent<CustomBrush>();brush.autoRefresh=false;brush.enabled=false;brush.ComputeVoxels();
                if(brush.InputVoxels==null||brush.InputVoxels.Length==0)throw new InvalidOperationException("Installed Digger did not voxelize actual closed rounded lobe input.");
                counts.Add(brush.InputVoxels.Length);
                yield return new BasicOperation{Params=new ModificationParameters{Action=ActionType.Dig,Brush=BrushType.Custom,Position=center,Size=new float3(1),Opacity=1,TextureIndex=3,CustomBrush=brush}};
            }
            evidence.roundedProfile.vendorInputVoxelCounts=counts.ToArray();
        }
        static BasicOperation RoundedRoadVolume(List<GameObject> temporary,List<Mesh> meshes,Segment segment,float measuredSideGuard=0)
        {
            var geometry=DragonKingdomRoundedTunnelBrushProfile.Create(segment.width+14+2*measuredSideGuard,Vector3.Distance(segment.a,segment.b)+24);meshes.Add(geometry.mesh);
            var rotation=Quaternion.LookRotation((segment.b-segment.a).normalized,Vector3.up);var center=(segment.a+segment.b)*.5f+rotation*Vector3.up*geometry.centerAboveRoad;
            var go=new GameObject("Temporary Digger closed arched road clearance input"){hideFlags=HideFlags.HideAndDontSave};temporary.Add(go);go.transform.SetPositionAndRotation(center,rotation);go.AddComponent<MeshFilter>().sharedMesh=geometry.mesh;
            var brush=go.AddComponent<CustomBrush>();brush.autoRefresh=false;brush.enabled=false;brush.ComputeVoxels();if(brush.InputVoxels==null||brush.InputVoxels.Length==0)throw new InvalidOperationException("Installed Digger did not voxelize actual closed arched road clearance.");
            return new BasicOperation{Params=new ModificationParameters{Action=ActionType.Dig,Brush=BrushType.Custom,Position=center,Size=new float3(1),Opacity=1,TextureIndex=3,CustomBrush=brush}};
        }
        [Serializable]public sealed class RoundedVoxelProbe
        {public int vertices,triangles,voxels;public float center,aboveApex,belowFloor,outsideWall;public bool finite;}
        public static RoundedVoxelProbe ProbeInstalledRoundedInputVoxelizer()
            =>ProbeRoundedInputAtActualGrid(new float3(2/3.125f,1/.5f,2/3.125f));
        public static RoundedVoxelProbe ProbeInstalledRoundedCubicInputVoxelizer()
            =>ProbeRoundedInputAtActualGrid(new float3(4/3.125f));
        static RoundedVoxelProbe ProbeRoundedInputAtActualGrid(float3 inverseGrid)
        {
            // Execute the installed public job used by CustomBrush.ComputeVoxels
            // on the same actual mesh at quarter scale. This bounded input-sign
            // test does not pretend to excavate, persist or validate a chamber.
            using(var geometry=DragonKingdomRoundedCavernBrushProfile.Create(65,105,130,160,64,12))
            {
                float3 scale=.25f*inverseGrid;
                var points=geometry.mesh.vertices.Select(v=>(float3)v*scale).ToArray();var indices=geometry.mesh.triangles.Select(i=>(ushort)i).ToArray();
                var bounds=new Bounds();foreach(var v in points)bounds.Encapsulate((Vector3)v);
                int3 size=(int3)math.round((float3)bounds.size)+new int3(4),origin=size/2;
                using(var vertices=new NativeArray<float3>(points,Allocator.TempJob))
                using(var triangles=new NativeArray<ushort>(indices,Allocator.TempJob))
                using(var voxels=new NativeArray<Voxel>(size.x*size.y*size.z,Allocator.TempJob))
                {
                    new MeshToVoxelsJob{SizeVox=size,Origin=origin,Vertices=vertices,Triangles=triangles,Voxels=voxels}.Schedule(voxels.Length,64).Complete();
                    float At(int x,int y,int z)=>voxels[x*size.y*size.z+y*size.z+z].Value;
                    return new RoundedVoxelProbe{vertices=vertices.Length,triangles=triangles.Length/3,voxels=voxels.Length,center=At(origin.x,origin.y,origin.z),aboveApex=At(origin.x,size.y-1,origin.z),belowFloor=At(origin.x,0,origin.z),outsideWall=At(0,origin.y,origin.z),finite=voxels.ToArray().All(v=>float.IsFinite(v.Value))};
                }
            }
        }
    }
}
#endif

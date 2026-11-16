#if UNITY_EDITOR && VISTA

using System;

using System.Collections.Generic;

using System.Linq;

using UnityEngine;

namespace LWS.TruckDragonKingdom.Editor

{

    public static partial class DragonKingdomRegionalWorldIntegration

    {

        sealed class NativeVistaPhysicalMaskContext

        {

            public float[,] weight;public List<Bounds> bounds;public List<string> boundOwners;public int physicalBoundCount;

            public Dictionary<long,List<FieldSegment>> roadBins;public NativeFootingIndex footings;public NativeFootingSamplingSupportContext footingSampling;

            public QualifiedCavernRoadAuthority undergroundColliderAuthority;
            public NativeConnectedDryApronContext connectedAprons;

        }

        [Serializable]public sealed class NativeConnectedDryApronRow
        {
            public string region;public Bounds originalDistrict,gridSamplingEnvelope;public float originalFooting;
            public int semanticDryVertices,semanticExcludedVertices,basisVertices,additionalInterpolationVertices,semanticallyWetSupportVertices,creekBankSupportVertices,hydrologySupportVertices;
        }
        [Serializable]public sealed class NativeConnectedDryApronEvidence
        {
            public string profile="ORIGINAL_CONNECTED_DRY_APRON_NATIVE_VERTEX_AND_INCIDENT_CELL_BASIS01";
            public string scope="EXISTING_SETTLEMENT_FIELDS_AND_UNCHANGED_BANK12_CLASSIFICATION_INPUT_HEIGHT_PRESERVATION_NOT_NEW_FOOTINGS_OR_POST_GPU_FLATTENING";
            public float gridStep=3.125f,featherMeters=12.5f,retainingBank=12;public Vector2 gridOrigin=new Vector2(-1600,-800);public int resolution=1025;
            public int totalSemanticDryVertices,totalBasisVertices,totalAdditionalInterpolationVertices,newProtectedVertices,alreadyProtectedVertices,nonzeroBasisWeights;
            public string dryCoreFlagsSha256,interpolationBasisFlagsSha256;public NativeConnectedDryApronRow[] districts;
        }
        sealed class NativeConnectedDryApronContext
        {public byte[,] dryCore,basis;public float[,] distance;public NativeConnectedDryApronEvidence evidence;}
        static NativeConnectedDryApronContext BuildNativeConnectedDryApronSupports(DragonKingdomWorld world)
        {
            var fields=CoherentSettlementFields(world);if(fields.Length!=2||fields.Select(f=>f.region).Distinct().Count()!=2)throw new InvalidOperationException("Exact existing village/harbor connected settlement fields required.");
            var context=new NativeConnectedDryApronContext{dryCore=new byte[1025,1025],basis=new byte[1025,1025],distance=new float[1025,1025],evidence=new NativeConnectedDryApronEvidence()};var rows=new List<NativeConnectedDryApronRow>();
            for(int z=0;z<1025;z++)for(int x=0;x<1025;x++)context.distance[z,x]=float.PositiveInfinity;
            for(int index=0;index<fields.Length;index++)
            {
                var field=fields[index];byte bit=(byte)(1<<index);var envelope=NativeFootingTerrainSamplingBounds(field.bounds);var row=new NativeConnectedDryApronRow{region=field.region,originalDistrict=field.bounds,gridSamplingEnvelope=envelope,originalFooting=field.footing};
                int x0=Mathf.RoundToInt((envelope.min.x+1600)/3.125f),x1=Mathf.RoundToInt((envelope.max.x+1600)/3.125f),z0=Mathf.RoundToInt((envelope.min.z+800)/3.125f),z1=Mathf.RoundToInt((envelope.max.z+800)/3.125f);
                for(int z=z0;z<=z1;z++)for(int x=x0;x<=x1;x++)
                {
                    var point=new Vector3(-1600+x*3.125f,0,-800+z*3.125f);if(!InsideXZ(field.bounds,point))continue;
                    if(!IntendedDrySettlementPosition(field.region,point)){row.semanticExcludedVertices++;continue;}
                    context.dryCore[z,x]|=bit;row.semanticDryVertices++;
                    // The original semantic apron is rasterized by the existing
                    // native coordinator. Every incident cell's four original
                    // lattice vertices form its minimal one-cell sampling basis.
                    // No synthetic architecture footing or new height target.
                    for(int dz=-1;dz<=1;dz++)for(int dx=-1;dx<=1;dx++){int xx=x+dx,zz=z+dz;if(xx>=x0&&xx<=x1&&zz>=z0&&zz<=z1)context.basis[zz,xx]|=bit;}
                }
                if(row.semanticDryVertices==0)throw new InvalidOperationException("Original connected dry district has no native dry vertices: "+field.region);rows.Add(row);
            }
            var featherOffsets=new List<Vector2Int>();for(int z=-4;z<=4;z++)for(int x=-4;x<=4;x++)if(x*x+z*z<=16)featherOffsets.Add(new Vector2Int(x,z));
            for(int z=0;z<1025;z++)for(int x=0;x<1025;x++)
            {
                if(context.dryCore[z,x]!=0)context.evidence.totalSemanticDryVertices++;if(context.basis[z,x]==0)continue;context.evidence.totalBasisVertices++;if(context.dryCore[z,x]==0)context.evidence.totalAdditionalInterpolationVertices++;
                var point=new Vector3(-1600+x*3.125f,0,-800+z*3.125f);for(int index=0;index<rows.Count;index++)if((context.basis[z,x]&(1<<index))!=0){var row=rows[index];row.basisVertices++;if((context.dryCore[z,x]&(1<<index))==0)row.additionalInterpolationVertices++;if(!IntendedDrySettlementPosition(row.region,point))row.semanticallyWetSupportVertices++;if(CreekBankDistance(point)<UrbanRetainingBank&&!HydrologyCore(point))row.creekBankSupportVertices++;if(HydrologyCore(point))row.hydrologySupportVertices++;}
                foreach(var offset in featherOffsets){int xx=x+offset.x,zz=z+offset.y;if(xx<0||xx>=1025||zz<0||zz>=1025)continue;float distance=Mathf.Sqrt(offset.x*offset.x+offset.y*offset.y)*3.125f;context.distance[zz,xx]=Mathf.Min(context.distance[zz,xx],distance);}
            }
            byte[] Bytes(byte[,] flags){var bytes=new byte[flags.Length];Buffer.BlockCopy(flags,0,bytes,0,bytes.Length);return bytes;}context.evidence.dryCoreFlagsSha256=FinishDigest(Bytes(context.dryCore));context.evidence.interpolationBasisFlagsSha256=FinishDigest(Bytes(context.basis));context.evidence.districts=rows.ToArray();return context;
        }
        static float NativeConnectedDryApronDistance(NativeConnectedDryApronContext apron,int x,int z,List<string> owners)
        {
            if(apron==null)return float.PositiveInfinity;byte flags=apron.basis[z,x];if(owners!=null&&flags!=0)for(int index=0;index<apron.evidence.districts.Length;index++)if((flags&(1<<index))!=0)owners.Add(((apron.dryCore[z,x]&(1<<index))!=0?"ORIGINAL_CONNECTED_DRY_STREETSCAPE_APRON:":"ORIGINAL_CONNECTED_APRON_INTERPOLATION_BASIS:")+apron.evidence.districts[index].region);return apron.distance[z,x];
        }
        // Explicit opt-in composition only. The hardness weights and raw physical

        // owner inventory remain exactly the ordinary shared mask. This token

        // separates a qualified underground collider from overhead heightfield

        // authority; unrelated starts/extraction/footings/surface roads remain.

        static NativeVistaPhysicalMaskContext BuildQualifiedCavernRoofPhysicalMask(DragonKingdomWorld world,Bounds roof,IReadOnlyList<NativeArchitectureFooting> footings,NativeVistaPostGradeEvidence proof,QualifiedCavernRoadAuthority authority)

        {

            if(authority==null)throw new InvalidOperationException("Passed typed native underground authority required; no bool waiver.");authority.ValidateWorld(world,roof);var mask=BuildNativeVistaPhysicalMask(world,roof,footings,proof);mask.undergroundColliderAuthority=authority;return mask;

        }

        static CavernRoofAuthorityRoles ClassifyNativeVistaTerrainConflictOwners(NativeVistaPhysicalMaskContext mask,Vector3 point,IEnumerable<string> rawOwners)

            =>ClassifyCavernRoofAuthorityRoles(mask.undergroundColliderAuthority,point,rawOwners.ToArray());

        static long VistaPhysicalMaskKey(int x,int z)=>((long)x<<32)^(uint)z;

        // One original LWS mask predicate shared by vendor input and strict

        // roof conflict audit. Audit may omit ONLY roof as an authority while

        // seeking contradictions; it never supplies an alternate vendor mask.

        static float NativeVistaProtectionDistance(NativeVistaPhysicalMaskContext mask,Vector3 p,int x,int z,bool includeRoof,List<string> owners=null)

        {

            float edge=Mathf.Min(Mathf.Min(x,z,1024-x,1024-z)*3.125f,HydrologyDistance(p));float outside=edge;if(owners!=null&&Mathf.Min(x,z,1024-x,1024-z)==0)owners.Add("ORIGINAL_OUTER_TILE_SEAM");if(owners!=null&&HydrologyDistance(p)==0)owners.Add("ORIGINAL_HYDROLOGY_CORE");

            for(int i=0;i<(includeRoof?mask.bounds.Count:mask.physicalBoundCount);i++){float distance=DistanceTo(mask.bounds[i],p);outside=Mathf.Min(outside,distance);if(owners!=null&&distance==0)owners.Add(mask.boundOwners[i]);}

            foreach(var footing in mask.footings.At(p)){float distance=DistanceTo(mask.footingSampling.For(footing),p);outside=Mathf.Min(outside,distance);if(owners!=null&&distance==0)owners.Add((DistanceTo(footing.physicalFootprint,p)==0?"EXPLICIT_SOURCE_PHYSICAL_ARCHITECTURE_FOOTING:":"EXPLICIT_FOOTING_HEIGHT_INTERPOLATION_SUPPORT:")+footing.id);}

            if(mask.roadBins.TryGetValue(VistaPhysicalMaskKey(Mathf.FloorToInt(p.x/64),Mathf.FloorToInt(p.z/64)),out var nearby))foreach(var segment in nearby){var d=new Vector2(segment.b.x-segment.a.x,segment.b.z-segment.a.z);if(d.sqrMagnitude<.001f)continue;float projected=Vector2.Dot(new Vector2(p.x-segment.a.x,p.z-segment.a.z),d)/d.sqrMagnitude,t=Mathf.Clamp01(projected);var q=Vector3.Lerp(segment.a,segment.b,t);float radial=Mathf.Max(0,Vector2.Distance(new Vector2(p.x,p.z),new Vector2(q.x,q.z))-segment.width*.5f-3.125f),beyond=(projected<0&&segment.suppressStartCap||projected>1&&segment.suppressEndCap)?Mathf.Abs(projected-t)*d.magnitude:0;float distance=Mathf.Max(radial,beyond);outside=Mathf.Min(outside,distance);if(owners!=null&&distance==0)owners.Add("ORIGINAL_PHYSICAL_ROAD_CORE:"+segment.owner);}

            return Mathf.Min(outside,NativeConnectedDryApronDistance(mask.connectedAprons,x,z,owners));

        }

        static NativeVistaPhysicalMaskContext BuildNativeVistaPhysicalMask(DragonKingdomWorld world,Bounds? roof,IReadOnlyList<NativeArchitectureFooting> footings,NativeVistaPostGradeEvidence proof)

        {

            var segments=NativeReliefRoadSegments(world);proof.roadSegments=segments.Count;const float feather=12.5f,bin=64;var roadBins=new Dictionary<long,List<FieldSegment>>();

            foreach(var segment in segments){float influence=segment.width*.5f+3.125f+feather;for(int z=Mathf.FloorToInt((Mathf.Min(segment.a.z,segment.b.z)-influence)/bin);z<=Mathf.FloorToInt((Mathf.Max(segment.a.z,segment.b.z)+influence)/bin);z++)for(int x=Mathf.FloorToInt((Mathf.Min(segment.a.x,segment.b.x)-influence)/bin);x<=Mathf.FloorToInt((Mathf.Max(segment.a.x,segment.b.x)+influence)/bin);x++){long key=VistaPhysicalMaskKey(x,z);if(!roadBins.TryGetValue(key,out var list))roadBins.Add(key,list=new List<FieldSegment>());list.Add(segment);}}

            var bounds=new List<Bounds>();var boundOwners=new List<string>();var authorities=segments.Select(s=>new NativeVistaMaskAuthority{kind="ORIGINAL_PHYSICAL_ROAD_CORE",owner=s.owner,a=s.a,b=s.b,width=s.width+6.25f}).ToList();

            foreach(var region in world.regions)foreach(var start in region.truckStarts){var b=new Bounds(start.position,Vector3.zero);foreach(float x in new[]{-2.2f,2.2f})foreach(float z in new[]{-8f,8f})b.Encapsulate(start.position+start.right*x+start.forward*z);b.Expand(6.25f);bounds.Add(b);boundOwners.Add("ORIGINAL_NWH_START:"+region.id+":"+start.name);authorities.Add(new NativeVistaMaskAuthority{kind="ORIGINAL_NWH_START",owner=region.id+":"+start.name,bounds=b});}

            foreach(var site in world.extractionSites.Where(s=>s.id!="boat")){var b=site.secureZone.bounds;b.Expand(6.25f);bounds.Add(b);boundOwners.Add("ORIGINAL_PHYSICAL_EXTRACTION:"+site.id);authorities.Add(new NativeVistaMaskAuthority{kind="ORIGINAL_PHYSICAL_EXTRACTION",owner=site.id,bounds=b});}proof.startExtractionBounds=bounds.Count;

            foreach(Transform t in ActivePhysicalStagingRoots(world))foreach(var mesh in t.GetComponentsInChildren<MeshFilter>(true).Where(f=>f.sharedMesh!=null&&f.GetComponent<MeshRenderer>()!=null)){var b=DragonKingdomWorldAuthoring.MeasureOwnedModelBounds(mesh.transform);if(segments.Any(s=>s.bridge&&t.name.StartsWith("Kingdom hairpin "+s.owner+" ",StringComparison.Ordinal)&&BridgePad(s,b)))continue;b.Expand(new Vector3(6.25f,0,6.25f));bounds.Add(b);boundOwners.Add("ACTUAL_PHYSICAL_STAGING_MESH:"+PathOf(mesh.transform,world.transform));authorities.Add(new NativeVistaMaskAuthority{kind="ACTUAL_PHYSICAL_STAGING_MESH",owner=PathOf(mesh.transform,world.transform),bounds=b});proof.physicalPads++;}

            var connectedAprons=BuildNativeConnectedDryApronSupports(world);proof.connectedDryApronSupport=connectedAprons.evidence;foreach(var row in connectedAprons.evidence.districts){authorities.Add(new NativeVistaMaskAuthority{kind="ORIGINAL_CONNECTED_DRY_STREETSCAPE_APRON",owner=row.region,bounds=row.originalDistrict,width=UrbanRetainingBank});authorities.Add(new NativeVistaMaskAuthority{kind="ORIGINAL_CONNECTED_APRON_INTERPOLATION_BASIS",owner=row.region,bounds=row.gridSamplingEnvelope,width=3.125f});}
            var footingSampling=BuildNativeFootingSamplingSupports(footings);foreach(var footing in footings)authorities.Add(new NativeVistaMaskAuthority{kind="EXPLICIT_FOOTING_HEIGHT_INTERPOLATION_SUPPORT",owner=footing.id,bounds=footingSampling.For(footing)});
            int physicalBoundCount=bounds.Count;if(roof.HasValue){bounds.Add(roof.Value);boundOwners.Add("ORIGINAL_UNDERGROUND_ROOF:CavernUndergroundBounds");authorities.Add(new NativeVistaMaskAuthority{kind="ORIGINAL_UNDERGROUND_ROOF",owner="CavernUndergroundBounds",bounds=roof.Value});}foreach(var footing in footings)authorities.Add(new NativeVistaMaskAuthority{kind="EXPLICIT_SOURCE_PHYSICAL_ARCHITECTURE_FOOTING",owner=footing.id,bounds=footing.physicalFootprint});proof.physicalMaskAuthorities=authorities.ToArray();var footingIndex=new NativeFootingIndex(footings,feather+NativeFootingSamplingMaximumExpansion);

            var mask=new NativeVistaPhysicalMaskContext{bounds=bounds,boundOwners=boundOwners,physicalBoundCount=physicalBoundCount,roadBins=roadBins,footings=footingIndex,footingSampling=footingSampling,connectedAprons=connectedAprons,weight=new float[1025,1025]};

            for(int z=0;z<1025;z++)for(int x=0;x<1025;x++){var p=new Vector3(-1600+x*3.125f,0,-800+z*3.125f);var owners=connectedAprons.basis[z,x]==0?null:new List<string>();float outside=NativeVistaProtectionDistance(mask,p,x,z,true,owners);mask.weight[z,x]=Mathf.SmoothStep(0,1,outside/feather);if(mask.weight[z,x]==0)proof.protectedSamples++;if(owners!=null){if(mask.weight[z,x]!=0)connectedAprons.evidence.nonzeroBasisWeights++;if(owners.Any(o=>!o.StartsWith("ORIGINAL_CONNECTED_",StringComparison.Ordinal)))connectedAprons.evidence.alreadyProtectedVertices++;else connectedAprons.evidence.newProtectedVertices++;}}return mask;

        }

    }

}

#endif


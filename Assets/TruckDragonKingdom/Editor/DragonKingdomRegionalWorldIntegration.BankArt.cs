#if UNITY_EDITOR && VISTA
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace LWS.TruckDragonKingdom.Editor
{
    public static partial class DragonKingdomRegionalWorldIntegration
    {
        public const string RetainingBankName="Owned village stone retaining bank";
        [Serializable]public sealed class RetainingBankArt
        {public string name=RetainingBankName,scope="SOURCE_OWNED_VISIBLE_STONE_EMBEDDED_AT_RETAINED_TWELVE_METRE_URBAN_BANK_WITH_UNCHANGED_CREEK_TERRAIN_ROADS";public int stones,acceptedParts,rejectedParts,originalAcceptedBeforeAlternate;public string[] failures;public List<BankPlacementTrial> trials=new List<BankPlacementTrial>();public List<BankRefinementSeed> refinementSeeds=new List<BankRefinementSeed>();public List<BankAlternateParcel> alternateParcels=new List<BankAlternateParcel>();public bool passed,creekCoreClear,roadsObjectivesAndWingsClear,actualMeshSupport;public RidgeCompositionPart[] parts;}
        [Serializable]public sealed class BankPlacementTrial
        {public string name,phase,rejectedReason;public int trial,refinementSeedTrial=-1,oppositeBankTrial=-1;public Vector3 point,refinementOffset,parcelHint;public float searchRadius,ground,range,allowedRange,halfFootprint,minimumCreekDistance;public bool supportFound,boundsMeasured;public Bounds bounds;public AuthoredClearanceWitness blocker;}
        [Serializable]public sealed class BankRefinementSeed
        {public string name;public int trial;public Vector3 point;}
        [Serializable]public sealed class BankAlternateParcel
        {public string name;public Vector3 originalHint,alternateHint,station;public float originalSide;public int firstTrial,attempts;}
        static BankRefinementSeed[] SelectBankNearMarginSeeds(IEnumerable<BankPlacementTrial> trials,string name)=>trials.Where(t=>t.name==name&&t.trial<480&&t.supportFound&&t.range<=t.allowedRange&&t.boundsMeasured&&t.minimumCreekDistance>=.25f&&t.rejectedReason=="ACTUAL_ROAD_START_OBJECTIVE_CLEARANCE"&&t.blocker!=null&&t.blocker.kind=="ROAD_GRAPH_ENVELOPE"&&t.blocker.margin>0&&t.blocker.distance<t.blocker.margin&&t.blocker.margin-t.blocker.distance<=1).OrderBy(t=>t.blocker.margin-t.blocker.distance).ThenBy(t=>t.trial).Take(16).Select(t=>new BankRefinementSeed{name=t.name,trial=t.trial,point=t.point}).ToArray();
        static bool InsideBankRefinementFringe(Vector3 point,Vector3 hint,Vector3 along,Vector3 right,float side)
        {var delta=point-hint;float longitudinal=Vector3.Dot(delta,along),outward=Vector3.Dot(delta,right*side);return Mathf.Abs(longitudinal)<=52.001f&&outward>=-4.001f&&outward<=12.001f;}
        public static RetainingBankArt LastRetainingBankArt{get;private set;}
        static float RetainingBankSearchRadius(int trial)=>trial<96?(trial==0?0:2*Mathf.Sqrt(trial)):Mathf.Sqrt(20*20+(48*48-20*20)*((trial-96)/383f));
        static Vector3 RetainingBankCandidate(Vector3 hint,Vector3 along,Vector3 right,float side,int trial)
        {float radius=RetainingBankSearchRadius(trial),angle=trial*2.399963f;return hint+along*Mathf.Cos(angle)*radius+right*side*Mathf.Abs(Mathf.Sin(angle))*Mathf.Min(radius,8);}
        static RetainingBankArt InstallRetainingBankArt(DragonKingdomWorld world,string folder,LandmarkFinish finish,List<Placement> records,bool configureMaterials=true,bool diagnoseAll=false)
        {
            var root=new GameObject(RetainingBankName).transform;root.SetParent(world.Region("village").root,false);var proof=new RetainingBankArt();LastRetainingBankArt=proof;finish.retainingBank=proof;var parts=new List<RidgeCompositionPart>();var trials=proof.trials;var failures=new List<string>();var terrains=world.GetComponentsInChildren<Terrain>(true);var accepted=new List<Bounds>();var hashes=new Dictionary<string,string>();var a=new Vector3(-800,0,-50);var b=new Vector3(-600,0,-450);var along=(b-a).normalized;var right=Vector3.Cross(Vector3.up,along);var own=new HashSet<Collider>();var deferred=new List<Action>();
            for(int index=0;index<24;index++)
            {
                int ordinal=index/2;float side=index%2==0?-1:1;var station=Vector3.Lerp(a,b,.37f+ordinal/11f*.61f);var hint=station+right*side*49;string source=DragonKingdomWorldAuthoring.Cliff+"RockFormation_0"+(index%4+1)+".prefab";var t=DragonKingdomWorldAuthoring.PlaceOwnedArtForReview(root,source,hint,index*137,6+index%3,true);t.name="Owned source village bank stone "+(index+1);if(configureMaterials)ConfigureFinishMaterials(t,folder);foreach(var collider in t.GetComponentsInChildren<Collider>(true))own.Add(collider);
                var initial=t.localPosition;var scale=t.localScale;var rotation=t.rotation;var modelBounds=BoundsOf(t);var item=new RidgeItem{root=t,localSamples=RidgeMeshSamples(t),rock=true};var part=new RidgeCompositionPart{region="village",name=t.name,source=source,before=hint,hint=hint,after=t.position,scale=scale,rotation=rotation,bounds=modelBounds,originalBounds=modelBounds,modelHeight=modelBounds.size.y,paddedModelHeight=modelBounds.size.y,maximumAllowedSupportRange=modelBounds.size.y*.28f,primaryGeometryMethod="EXISTING_STATIC_MODEL_BOUNDS_WITH_PRIMARY_MESH_EXPOSURE"};
                RidgeMeshSamples(t,out var actualPrimary,out int primaryMeshes,out int primaryVertices);part.originalPrimaryMeshBounds=actualPrimary;part.primaryMeshes=primaryMeshes;part.primaryVertices=primaryVertices;
                var originalSource=PrefabUtility.GetCorrespondingObjectFromSource(t.gameObject);if(originalSource==null||AssetDatabase.GetAssetPath(originalSource)!=source||!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(originalSource,out string guid,out long id))throw new InvalidOperationException("Persistent exact source bank identity required.");part.sourceGuid=guid;part.sourceLocalId=id.ToString(System.Globalization.CultureInfo.InvariantCulture);part.sourceSha256=AssetSha(source);part.sourceMetaSha256=AssetSha(source+".meta");part.geometry=RidgeGeometryBindings(t,hashes);parts.Add(part);proof.parts=parts.ToArray();proof.stones=parts.Count;finish.sources.Add(source);bool placed=false;
                BankRefinementSeed[] localSeeds=Array.Empty<BankRefinementSeed>();
                bool Search(bool opposite)
                {
                int firstTrial=opposite?480+localSeeds.Length*AuthoredParcelRefinementOffsets.Length:0;
                var parcelHint=opposite?station-right*side*49:hint;
                BankAlternateParcel parcel=null;
                if(opposite){parcel=new BankAlternateParcel{name=t.name,originalHint=hint,alternateHint=parcelHint,station=station,originalSide=side,firstTrial=firstTrial};proof.alternateParcels.Add(parcel);}
                for(int trial=firstTrial;trial<(opposite?firstTrial+480:480+16*AuthoredParcelRefinementOffsets.Length);trial++)
                {
                    if(!opposite&&trial==480){localSeeds=SelectBankNearMarginSeeds(trials,t.name);proof.refinementSeeds.AddRange(localSeeds);}if(!opposite&&trial>=480+localSeeds.Length*AuthoredParcelRefinementOffsets.Length)break;bool refinement=!opposite&&trial>=480;var seed=refinement?localSeeds[(trial-480)/AuthoredParcelRefinementOffsets.Length]:null;var offset=refinement?AuthoredParcelRefinementOffsets[(trial-480)%AuthoredParcelRefinementOffsets.Length]:Vector2.zero;int oppositeTrial=opposite?trial-firstTrial:-1;int radialTrial=opposite?oppositeTrial:refinement?seed.trial:trial;var p=refinement?seed.point+new Vector3(offset.x,0,offset.y):RetainingBankCandidate(parcelHint,along,right,opposite?-side:side,radialTrial);float half=Mathf.Clamp(Mathf.Min(modelBounds.size.x,modelBounds.size.z)*.3f,.5f,5);var attempt=new BankPlacementTrial{name=t.name,trial=trial,refinementSeedTrial=refinement?seed.trial:-1,oppositeBankTrial=oppositeTrial,parcelHint=parcelHint,refinementOffset=new Vector3(offset.x,0,offset.y),phase=opposite?"OPPOSITE_BANK_SAME_STATION_0_TO_48M_480":refinement?"SAME_BANK_NEAR_MARGIN_HALF_METRE_FOUR_METRE_FRINGE":trial<96?"ORIGINAL_96":"SAME_BANK_ALONG_20_TO_48M_384",point=p,halfFootprint=half,allowedRange=part.maximumAllowedSupportRange,searchRadius=RetainingBankSearchRadius(radialTrial)};if(parcel!=null)parcel.attempts++;trials.Add(attempt);part.trial=trial;part.candidatePhase=attempt.phase;part.candidateDistanceFromHint=Vector2.Distance(new Vector2(p.x,p.z),new Vector2(hint.x,hint.z));
                    if(refinement&&!InsideBankRefinementFringe(p,hint,along,right,side)){part.clearanceRejects++;part.lastRejectedReason=attempt.rejectedReason="SAME_BANK_LOCAL_REFINEMENT_OUTSIDE_FOUR_METRE_FRINGE";continue;}
                    bool found=TryGround(world,p,half,out float ground,out float range);attempt.supportFound=found;attempt.ground=found?ground:0;attempt.range=found?range:0;part.supportHeightRange=attempt.range;if(!found||range>part.maximumAllowedSupportRange){part.supportRejects++;part.lastRejectedReason=attempt.rejectedReason="ACTUAL_TERRAIN_SUPPORT_RANGE";continue;}
                    AlignBase(t,new Vector3(p.x,ground-part.modelHeight*.08f,p.z));Physics.SyncTransforms();var bounds=BoundsOf(t);attempt.bounds=bounds;attempt.boundsMeasured=true;part.after=t.position;part.bounds=bounds;float minimumCreek=float.PositiveInfinity;foreach(float z in new[]{bounds.min.z,bounds.center.z,bounds.max.z})foreach(float x in new[]{bounds.min.x,bounds.center.x,bounds.max.x})minimumCreek=Mathf.Min(minimumCreek,CreekBankDistance(new Vector3(x,0,z)));attempt.minimumCreekDistance=minimumCreek;
                    if(minimumCreek<.25f){part.clearanceRejects++;part.lastRejectedReason=attempt.rejectedReason="ACTUAL_CREEK_CORE_CLEARANCE";continue;}
                    if(!FinishBoundsClearOfRoads(world,bounds,w=>attempt.blocker=w)||!ClearRoadEnvelope(world,t,false,w=>attempt.blocker=w)){part.clearanceRejects++;part.lastRejectedReason=attempt.rejectedReason="ACTUAL_ROAD_START_OBJECTIVE_CLEARANCE";continue;}if(!RidgeWingClear(world,bounds)){part.clearanceRejects++;part.lastRejectedReason=attempt.rejectedReason="ACTUAL_WING_CLEARANCE";continue;}
                    if(accepted.Any(other=>RidgeInterior(bounds).Intersects(RidgeInterior(other)))){part.overlapRejects++;part.lastRejectedReason=attempt.rejectedReason="ACTUAL_ACCEPTED_BANK_STONE_OVERLAP";continue;}
                    var peer=Physics.OverlapBox(bounds.center,bounds.extents*.75f,Quaternion.identity,~0,QueryTriggerInteraction.Ignore).FirstOrDefault(c=>!own.Contains(c)&&!(c is TerrainCollider)&&!c.name.StartsWith("Chunk",StringComparison.Ordinal)&&c.transform.IsChildOf(world.transform));if(peer!=null){attempt.blocker=new AuthoredClearanceWitness{kind="NATIVE_OTHER_ART_COLLIDER",path=PathOf(peer.transform,world.transform),bounds=peer.bounds,point=bounds.center,queryExtents=bounds.extents*.75f,queryRotation=Quaternion.identity};part.overlapRejects++;part.lastRejectedReason=attempt.rejectedReason="ACTUAL_OTHER_ART_COLLIDER_OVERLAP";continue;}
                    MeasureRidgeMesh(item,terrains,part);if(!part.visibleAndSupported){part.meshRejects++;part.lastRejectedReason=attempt.rejectedReason="ACTUAL_PRIMARY_MESH_EXPOSURE_OR_SUPPORT";continue;}
                    part.sourceIdentityExact=t.localScale.Equals(scale)&&t.rotation.Equals(rotation);if(!part.sourceIdentityExact)throw new InvalidOperationException("Bank placement changed source instance scale/orientation.");part.roadStartObjectiveClear=part.wingClear=part.otherArtClear=part.accepted=true;part.geometry=RidgeGeometryBindings(t,hashes);part.lastRejectedReason="";accepted.Add(bounds);placed=true;proof.acceptedParts++;return true;
                }
                return false;
                }
                void Reject()
                {
                    part.failure="No supported creek/road/wing/peer-clear bank placement: "+part.name+" support="+part.supportRejects+",clearance="+part.clearanceRejects+",peer="+part.overlapRejects+",mesh="+part.meshRejects;failures.Add(part.failure);proof.failures=failures.ToArray();proof.rejectedParts=failures.Count;if(diagnoseAll){t.localPosition=initial;Physics.SyncTransforms();return;}
                }
                placed=Search(false);
                if(!placed){t.localPosition=initial;Physics.SyncTransforms();deferred.Add(()=>{if(!Search(true))Reject();});continue;}
            }
            // Reserve every successful original parcel before relocating any genuinely exhausted source instance.
            proof.originalAcceptedBeforeAlternate=proof.acceptedParts;
            foreach(var search in deferred)search();
            proof.failures=failures.ToArray();proof.rejectedParts=failures.Count;proof.passed=proof.stones==24&&proof.acceptedParts==24&&proof.rejectedParts==0&&parts.All(p=>p.visibleAndSupported&&p.sourceIdentityExact&&p.accepted);proof.creekCoreClear=proof.roadsObjectivesAndWingsClear=proof.actualMeshSupport=proof.passed;if(!proof.passed)throw new InvalidOperationException("Actual bank24 exhaustive diagnosis rejected "+proof.rejectedParts+" source placements; no omission or accepted partial bank.");records.Add(new Placement{region="village",name=root.name,source=DragonKingdomWorldAuthoring.Cliff,operation="Retain actual creek/core and roads, place actual owned supported visible stone on the measured urban retaining bank",after=root.position,bounds=BoundsOf(root)});return proof;
        }
        static void ValidateSavedBankSearch(DragonKingdomWorld world,RetainingBankArt proof,RidgeCompositionPart part,Transform child,int index)
        {
            var a=new Vector3(-800,0,-50);var b=new Vector3(-600,0,-450);var along=(b-a).normalized;var right=Vector3.Cross(Vector3.up,along);float side=index%2==0?-1:1;var hint=Vector3.Lerp(a,b,.37f+(index/2)/11f*.61f)+right*side*49;
            var rows=proof.trials.Where(t=>t.name==part.name).ToArray();var seeds=proof.refinementSeeds.Where(t=>t.name==part.name).ToArray();
            if(part.trial<0||seeds.Length>16||part.trial>=480+seeds.Length*AuthoredParcelRefinementOffsets.Length+480||!part.accepted||!part.sourceIdentityExact||!string.IsNullOrEmpty(part.failure)||part.name!="Owned source village bank stone "+(index+1)||Vector3.Distance(part.hint,hint)>.002f||Mathf.Abs(part.modelHeight-(6+index%3))>.002f||Mathf.Abs(part.maximumAllowedSupportRange-part.modelHeight*.28f)>.001f||part.primaryGeometryMethod!="EXISTING_STATIC_MODEL_BOUNDS_WITH_PRIMARY_MESH_EXPOSURE"||rows.Length!=part.trial+1||rows.Where((t,i)=>t.trial!=i).Any())throw new InvalidOperationException("Saved exact bank source/search identity differs.");
            Vector3 expected;string phase;float searchRadius;var last=rows.Last();
            int alternateStart=480+seeds.Length*AuthoredParcelRefinementOffsets.Length;
            bool opposite=part.trial>=alternateStart;
            if(part.trial>=480)
            {
                var eligible=SelectBankNearMarginSeeds(rows,part.name);if(eligible.Length!=seeds.Length)throw new InvalidOperationException("Saved bank local refinement lost its exact ordered near-margin source witnesses.");
                for(int i=0;i<seeds.Length;i++)if(seeds[i].trial!=eligible[i].trial||!seeds[i].point.Equals(eligible[i].point)||!world.definition.graph.edges.Any(e=>e.edgeId==rows[seeds[i].trial].blocker.path))throw new InvalidOperationException("Saved bank seed changed actual graph/point/order witness.");
                if(opposite)
                {
                    var station=Vector3.Lerp(a,b,.37f+(index/2)/11f*.61f);var mirrored=station-right*side*49;int radial=part.trial-alternateStart;
                    var parcel=proof.alternateParcels.Single(p=>p.name==part.name);
                    if(proof.originalAcceptedBeforeAlternate!=24-proof.alternateParcels.Count||parcel.firstTrial!=alternateStart||parcel.attempts!=radial+1||!parcel.originalHint.Equals(hint)||!parcel.alternateHint.Equals(mirrored)||!parcel.station.Equals(station)||parcel.originalSide!=side||rows.Take(alternateStart).Any(t=>string.IsNullOrEmpty(t.rejectedReason)))throw new InvalidOperationException("Saved alternate bank lost its exhaustive original failure and reserved original-placement witness.");
                    expected=RetainingBankCandidate(mirrored,along,right,-side,radial);searchRadius=RetainingBankSearchRadius(radial);phase="OPPOSITE_BANK_SAME_STATION_0_TO_48M_480";
                    if(last.oppositeBankTrial!=radial||last.refinementSeedTrial!=-1||!last.refinementOffset.Equals(Vector3.zero)||!last.parcelHint.Equals(mirrored))throw new InvalidOperationException("Saved mirrored same-station candidate recipe differs.");
                }
                else
                {
                int local=part.trial-480;var seed=seeds[local/AuthoredParcelRefinementOffsets.Length];var offset=AuthoredParcelRefinementOffsets[local%AuthoredParcelRefinementOffsets.Length];expected=seed.point+new Vector3(offset.x,0,offset.y);searchRadius=RetainingBankSearchRadius(seed.trial);phase="SAME_BANK_NEAR_MARGIN_HALF_METRE_FOUR_METRE_FRINGE";
                if(last.refinementSeedTrial!=seed.trial||!last.refinementOffset.Equals(new Vector3(offset.x,0,offset.y))||!InsideBankRefinementFringe(expected,hint,along,right,side))throw new InvalidOperationException("Saved bank local refinement left its exact four-metre/half-metre/bounded bank-fringe envelope.");
                }
            }
            else
            {
                if(seeds.Length!=0||last.refinementSeedTrial!=-1||!last.refinementOffset.Equals(Vector3.zero))throw new InvalidOperationException("Original bank candidate claimed unrelated local refinement.");expected=RetainingBankCandidate(hint,along,right,side,part.trial);searchRadius=RetainingBankSearchRadius(part.trial);phase=part.trial<96?"ORIGINAL_96":"SAME_BANK_ALONG_20_TO_48M_384";
            }
            for(int i=0;i<rows.Length;i++)
            {
                var row=rows[i];bool alt=i>=alternateStart;bool local=!alt&&i>=480;int radial=alt?i-alternateStart:local?seeds[(i-480)/AuthoredParcelRefinementOffsets.Length].trial:i;
                var station=Vector3.Lerp(a,b,.37f+(index/2)/11f*.61f);var parcelHint=alt?station-right*side*49:hint;
                var offset=local?AuthoredParcelRefinementOffsets[(i-480)%AuthoredParcelRefinementOffsets.Length]:Vector2.zero;
                var point=local?seeds[(i-480)/AuthoredParcelRefinementOffsets.Length].point+new Vector3(offset.x,0,offset.y):RetainingBankCandidate(parcelHint,along,right,alt?-side:side,radial);
                string rowPhase=alt?"OPPOSITE_BANK_SAME_STATION_0_TO_48M_480":local?"SAME_BANK_NEAR_MARGIN_HALF_METRE_FOUR_METRE_FRINGE":i<96?"ORIGINAL_96":"SAME_BANK_ALONG_20_TO_48M_384";
                if(row.phase!=rowPhase||row.oppositeBankTrial!=(alt?radial:-1)||!row.parcelHint.Equals(parcelHint)||Vector3.Distance(row.point,point)>.002f||Mathf.Abs(row.searchRadius-RetainingBankSearchRadius(radial))>.002f||!row.refinementOffset.Equals(new Vector3(offset.x,0,offset.y))||row.refinementSeedTrial!=(local?radial:-1)||(i<part.trial&&string.IsNullOrEmpty(row.rejectedReason)))throw new InvalidOperationException("Saved bank history changed exact original/local/mirrored source recipe.");
            }
            var bounds=BoundsOf(child);if(Vector2.Distance(new Vector2(expected.x,expected.z),new Vector2(bounds.center.x,bounds.center.z))>.01f||Vector3.Distance(last.point,expected)>.002f||Mathf.Abs(last.searchRadius-searchRadius)>.002f||last.phase!=phase||part.candidatePhase!=phase||!string.IsNullOrEmpty(last.rejectedReason))throw new InvalidOperationException("Saved exact bounded same-bank native point/phase differs.");float half=Mathf.Clamp(Mathf.Min(bounds.size.x,bounds.size.z)*.3f,.5f,5);if(!TryGround(world,expected,half,out _,out float range)||range>part.maximumAllowedSupportRange||Mathf.Abs(range-part.supportHeightRange)>.002f)throw new InvalidOperationException("Saved original bank actual support-range gate differs.");
        }
        public static void ValidateSavedRetainingBankArt(DragonKingdomWorld world,RetainingBankArt proof)
        {
            if(proof==null||!proof.passed||proof.stones!=24||proof.acceptedParts!=24||proof.rejectedParts!=0||proof.failures==null||proof.failures.Length!=0||proof.trials==null||proof.refinementSeeds==null||proof.alternateParcels==null||proof.alternateParcels.Count>24||proof.alternateParcels.Select(p=>p.name).Distinct().Count()!=proof.alternateParcels.Count||proof.parts.Length!=24||!proof.creekCoreClear||!proof.roadsObjectivesAndWingsClear||!proof.actualMeshSupport)throw new InvalidOperationException("Saved source bank stone proof incomplete.");if(proof.originalAcceptedBeforeAlternate!=24-proof.alternateParcels.Count||!proof.alternateParcels.Select(p=>p.name).OrderBy(n=>n).SequenceEqual(proof.parts.Where(p=>p.candidatePhase=="OPPOSITE_BANK_SAME_STATION_0_TO_48M_480").Select(p=>p.name).OrderBy(n=>n)))throw new InvalidOperationException("Saved alternate bank list does not bijectively identify the genuinely exhausted preserved source instances.");var root=world.Region("village").root.Cast<Transform>().Single(t=>t.name==RetainingBankName);if(root.childCount!=24)throw new InvalidOperationException("Saved source bank stone count changed.");var hashes=new Dictionary<string,string>();foreach(var part in proof.parts){var child=root.Cast<Transform>().Single(t=>t.name==part.name);ValidateSavedBankSearch(world,proof,part,child,Array.IndexOf(proof.parts,part));if(child.position!=part.after||Quaternion.Angle(child.rotation,part.rotation)>.0001f||child.localScale!=part.scale||AssetSha(part.source)!=part.sourceSha256||AssetSha(part.source+".meta")!=part.sourceMetaSha256||AssetDatabase.AssetPathToGUID(part.source)!=part.sourceGuid||JsonUtility.ToJson(new RidgeGeometryContainer{bindings=RidgeGeometryBindings(child,hashes)})!=JsonUtility.ToJson(new RidgeGeometryContainer{bindings=part.geometry})||!FinishBoundsClearOfRoads(world,part.bounds)||!RidgeWingClear(world,part.bounds))throw new InvalidOperationException("Saved original bank stone source geometry/pose/buffers changed.");var actual=new RidgeCompositionPart{modelHeight=part.modelHeight,supportHeightRange=part.supportHeightRange};MeasureRidgeMesh(new RidgeItem{root=child,localSamples=RidgeMeshSamples(child),rock=true},world.GetComponentsInChildren<Terrain>(true),actual);if(!actual.visibleAndSupported)throw new InvalidOperationException("Saved source bank stone lost actual exposed supported mesh.");foreach(float z in new[]{part.bounds.min.z,part.bounds.center.z,part.bounds.max.z})foreach(float x in new[]{part.bounds.min.x,part.bounds.center.x,part.bounds.max.x})if(CreekBankDistance(new Vector3(x,0,z))<.25f)throw new InvalidOperationException("Saved source bank stone intrudes into actual creek core.");}
        }
    }
}
#endif

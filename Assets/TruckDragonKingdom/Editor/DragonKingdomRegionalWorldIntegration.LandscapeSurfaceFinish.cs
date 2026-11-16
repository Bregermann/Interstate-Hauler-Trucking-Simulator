#if UNITY_EDITOR && VISTA
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Pinwheel.Vista.Graph;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LWS.TruckDragonKingdom.Editor
{
    public static partial class DragonKingdomRegionalWorldIntegration
    {
        [Serializable] public sealed class LandscapeSurfaceTile
        {
            public string hierarchy,source,candidate,heightBefore,heightAfter,holesBefore,holesAfter,alphaBefore,alphaAfter,originalDominantTerrainSha256,savedDominantTerrainSha256;
            public Vector3 origin,size; public int changedPixels,meadowPixels,rockFacePixels,dominantLayerMismatches,nwhSampledPoints,nwhTransitionPoints,nwhIndexMismatches;
            public float meadowGreenMinimum=1,meadowGreenMaximum,maximumWeightSumError;
            public bool sameVisualPhysicalProvider,interpolationPayloadExact,layersExact;
            public int grassClassBoundaryEdges;public float maximumGrassRatioCrossProductError,sourceMeanBoundaryGrassRatioJump,savedMeanBoundaryGrassRatioJump,sourceMaximumBoundaryGrassRatioJump,savedMaximumBoundaryGrassRatioJump;public bool originalGrassRatioPreserved,boundaryGrassRatiosSmooth;
        }
        [Serializable] public sealed class LandscapeSurfaceReview
        {
            public string scope="SAVED_FOUR_EXPLICIT_TERRAIN_RENDER_COLLIDER_PROVIDER_COPIES_VENDOR_VISTA_SPLATS_EXACT_PHYSICAL_PAYLOAD_NOT_ADOPTION",status="IN_PROGRESS",error,sourcePreview,preview,folder,originalWorldBefore,originalWorldAfter,originalWorldReloaded,noiseNodeJson,clampNodeJson,slopeNodeJson,noiseSha256,clampedNoiseSha256,slopeSha256;
            public int originalTerrains,originalColliders; public bool savedReloaded,originalWorldExact,originalSourcesExact,incomingSceneContextRestored,vendorResourcesDisposed;
            public LandscapeSurfaceTile[] tiles; public LandscapeOriginalField[] allOriginalFields; public LandscapeMaskBounds maskBounds,nativeMaskBounds; public TowerAppearanceFrame[] frames; public TowerAppearanceSource[] watchedSources; public string[] changedSources;
        }
        [Serializable] public sealed class LandscapeMaskBounds
        {
            public int samples,rawBelowZero,rawAboveOne;public float rawMinimum=float.PositiveInfinity,rawMaximum=float.NegativeInfinity,clampedMinimum=float.PositiveInfinity,clampedMaximum=float.NegativeInfinity,maximumNativeClampError;public string rawSha256,clampedSha256,noiseNodeJson,clampNodeJson;public bool rawAndClampedFinite,clampedNormalized,nativeTexelClampExact,resourcesDisposed;
            public LandscapeMaskResolution resolution;
        }
        [Serializable] public sealed class LandscapeMaskResolution
        {
            public int requestedGraphResolution,configuredGraphResolution,observedGraphArgumentResolution,installedMinimumResolution,installedMaximumResolution,noiseCalculatedResolution,clampCalculatedResolution,rawWidth,rawHeight,clampedWidth,clampedHeight,rawArrayWidth,rawArrayHeight,clampedArrayWidth,clampedArrayHeight;
            public string noiseResolutionOverride,clampResolutionOverride;public float noiseResolutionMultiplier,clampResolutionMultiplier;public bool configuredResolutionMatchesVendorContract,graphArgumentExact,nodeOutputsExact,readbackShapesExact;
        }
        public static LandscapeMaskBounds LastLandscapeVendorClampProof{get;private set;}
        [Serializable] public sealed class LandscapeOriginalField{public string hierarchy,source,heightHash,holeHash,alphaHash,layerProperties;public Vector3 position,size;public int resolution,holesResolution;public bool originalBytesAndMemoryExact,savedProviderPayloadExact;}
        sealed class LandscapeSurfaceBinding
        { public Terrain terrain; public TerrainData source,copy; public LandscapeSurfaceTile proof; public Vector3[] nwhPoints;public int[] originalNwhIndices; }
        public static LandscapeSurfaceReview LastLandscapeSurfaceReview{get;private set;}

        public static LandscapeMaskBounds ValidateLandscapeClampedMasks(float[,] raw,float[,] clamped)
        {
            if(raw==null||clamped==null||raw.GetLength(0)!=clamped.GetLength(0)||raw.GetLength(1)!=clamped.GetLength(1)||raw.Length==0)throw new InvalidOperationException("Actual equal nonempty raw and vendor-clamped mask fields required.");var proof=new LandscapeMaskBounds{samples=raw.Length};
            for(int z=0;z<raw.GetLength(0);z++)for(int x=0;x<raw.GetLength(1);x++)
            {
                float a=raw[z,x],b=clamped[z,x];if(!float.IsFinite(a)||!float.IsFinite(b))throw new InvalidOperationException("Nonfinite raw or vendor-clamped mask cannot reach appearance writes; no missing-data fill.");if(b<0||b>1)throw new InvalidOperationException("Actual vendor-clamped mask outside declared domain.");proof.rawMinimum=Mathf.Min(proof.rawMinimum,a);proof.rawMaximum=Mathf.Max(proof.rawMaximum,a);proof.clampedMinimum=Mathf.Min(proof.clampedMinimum,b);proof.clampedMaximum=Mathf.Max(proof.clampedMaximum,b);if(a<0)proof.rawBelowZero++;if(a>1)proof.rawAboveOne++;
            }
            proof.rawSha256=FinishFloatHash(raw);proof.clampedSha256=FinishFloatHash(clamped);proof.rawAndClampedFinite=true;proof.clampedNormalized=true;return proof;
        }
        static LandscapeMaskBounds LandscapeExactNativeClampProof(float[,] raw,float[,] clamped)
        {
            var proof=ValidateLandscapeClampedMasks(raw,clamped);for(int z=0;z<raw.GetLength(0);z++)for(int x=0;x<raw.GetLength(1);x++)proof.maximumNativeClampError=Mathf.Max(proof.maximumNativeClampError,Mathf.Abs(clamped[z,x]-Mathf.Clamp01(raw[z,x])));proof.nativeTexelClampExact=proof.maximumNativeClampError<=.000001f;if(!proof.nativeTexelClampExact)throw new InvalidOperationException("Actual vendor GPU Clamp does not match its declared bounded native-texel operation.");return proof;
        }
        static NoiseNode LandscapeVendorNoise()=>new NoiseNode{seed=8108,scale=150,layerCount=3,lacunarity=2,persistence=.42f,offset=new Vector2(813,297)};
        static float[,] LandscapeReadExactMaskTexels(RenderTexture rt)
        {
            var previous=RenderTexture.active;var texture=new Texture2D(rt.width,rt.height,TextureFormat.RFloat,false,true);try{RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0,false);texture.Apply(false,false);var native=texture.GetRawTextureData<float>().ToArray();var values=new float[rt.height,rt.width];Buffer.BlockCopy(native,0,values,0,native.Length*sizeof(float));return values;}finally{RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(texture);}
        }
        public static IEnumerator CaptureLandscapeVendorClampRegression()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new InvalidOperationException("Stopped compiled native Editor required.");string output=Path.Combine(Output(),"LandscapeVendorClampRegression");if(Directory.Exists(Io(output)))throw new InvalidOperationException("Fresh actual vendor clamp evidence required.");Directory.CreateDirectory(Io(output));TerrainGraph graph=null;ExecutionHandle handle=null;bool outstanding=false,async=ShaderUtil.allowAsyncCompilation;LandscapeMaskBounds proof=null;
            try
            {
                ShaderUtil.allowAsyncCompilation=false;graph=ScriptableObject.CreateInstance<TerrainGraph>();graph.hideFlags=HideFlags.HideAndDontSave;graph.Reset();var noise=LandscapeVendorNoise();var clamp=new ClampNode{min=0,max=1};graph.AddNode(noise);graph.AddNode(clamp);graph.AddEdge(new Edge{outputSlot=new SlotRef(noise.id,noise.outputSlot.id),inputSlot=new SlotRef(clamp.id,clamp.inputSlot.id)});const int requestedResolution=1024;var config=TerrainGenerationConfigs.Create();config.resolution=requestedResolution;config.worldBounds=new Rect(-1600,-800,3200,3200);config.terrainHeight=600;config.seed=28082026;int graphArgumentResolution=0;handle=graph.Execute(new[]{noise.id,clamp.id},config,new GraphInputContainer(),(actualGraph,args)=>graphArgumentResolution=args[Args.RESOLUTION].intValue);outstanding=true;double start=EditorApplication.timeSinceStartup;while(!handle.isCompleted){if(EditorApplication.timeSinceStartup-start>120)throw new TimeoutException("Actual public vendor noise-to-clamp GPU regression timed out.");yield return null;}outstanding=false;
                var rawRt=handle.data.GetRT(new SlotRef(noise.id,noise.outputSlot.id));var boundedRt=handle.data.GetRT(new SlotRef(clamp.id,clamp.outputSlot.id));if(rawRt?.renderTexture==null||boundedRt?.renderTexture==null)throw new InvalidOperationException("Actual raw and vendor-clamped GPU mask outputs required.");var raw=LandscapeReadExactMaskTexels(rawRt.renderTexture);var bounded=LandscapeReadExactMaskTexels(boundedRt.renderTexture);proof=LastLandscapeVendorClampProof=LandscapeExactNativeClampProof(raw,bounded);proof.noiseNodeJson=JsonUtility.ToJson(noise);proof.clampNodeJson=JsonUtility.ToJson(clamp);
                var resolution=proof.resolution=new LandscapeMaskResolution{requestedGraphResolution=requestedResolution,configuredGraphResolution=config.resolution,observedGraphArgumentResolution=graphArgumentResolution,installedMinimumResolution=Pinwheel.Vista.Constants.RES_MIN,installedMaximumResolution=Pinwheel.Vista.Constants.RES_MAX,noiseCalculatedResolution=noise.CalculateResolution(graphArgumentResolution,graphArgumentResolution),clampCalculatedResolution=clamp.CalculateResolution(graphArgumentResolution,rawRt.renderTexture.width),rawWidth=rawRt.renderTexture.width,rawHeight=rawRt.renderTexture.height,clampedWidth=boundedRt.renderTexture.width,clampedHeight=boundedRt.renderTexture.height,rawArrayWidth=raw.GetLength(1),rawArrayHeight=raw.GetLength(0),clampedArrayWidth=bounded.GetLength(1),clampedArrayHeight=bounded.GetLength(0),noiseResolutionOverride=noise.resolutionOverride.ToString(),clampResolutionOverride=clamp.resolutionOverride.ToString(),noiseResolutionMultiplier=noise.resolutionMultiplier,clampResolutionMultiplier=clamp.resolutionMultiplier};
                resolution.configuredResolutionMatchesVendorContract=config.resolution==Pinwheel.Vista.Utilities.MultipleOf8(Mathf.Clamp(requestedResolution,resolution.installedMinimumResolution,resolution.installedMaximumResolution));resolution.graphArgumentExact=graphArgumentResolution==config.resolution;resolution.nodeOutputsExact=resolution.rawWidth==resolution.noiseCalculatedResolution&&resolution.rawHeight==resolution.noiseCalculatedResolution&&resolution.clampedWidth==resolution.clampCalculatedResolution&&resolution.clampedHeight==resolution.clampCalculatedResolution;resolution.readbackShapesExact=resolution.rawWidth==resolution.rawArrayWidth&&resolution.rawHeight==resolution.rawArrayHeight&&resolution.clampedWidth==resolution.clampedArrayWidth&&resolution.clampedHeight==resolution.clampedArrayHeight&&proof.samples==resolution.rawWidth*resolution.rawHeight;
                if(!resolution.configuredResolutionMatchesVendorContract||!resolution.graphArgumentExact||!resolution.nodeOutputsExact||!resolution.readbackShapesExact)throw new InvalidOperationException("Requested/configured/actual graph arguments, installed node resolution, native textures and exact readback dimensions must agree.");File.WriteAllBytes(Io(Path.Combine(output,"actual-raw-noise-native.float32")),FinishFloatBytes(raw));File.WriteAllBytes(Io(Path.Combine(output,"actual-vendor-clamped-native.float32")),FinishFloatBytes(bounded));if(proof.rawBelowZero==0)throw new InvalidOperationException("Regression must exercise actual alternating-octave negative noise, not an already-bounded substitute.");
            }
            finally{if(!outstanding){handle?.Dispose();if(graph!=null)UnityEngine.Object.DestroyImmediate(graph);if(proof!=null)proof.resourcesDisposed=true;}ShaderUtil.allowAsyncCompilation=async;if(proof!=null)File.WriteAllText(Io(Path.Combine(output,"actual-vendor-clamp-regression.json")),JsonUtility.ToJson(proof,true));}
        }

        // Project composition of two actual Vista masks. Texture generation,
        // terrain geometry, shader rendering and collision stay vendor/Unity owned.
        public static float[] ComposeLandscapeSurfaceWeights(float meadow,float rockMask,float worldHeight)
        {
            if(!float.IsFinite(meadow)||!float.IsFinite(rockMask)||!float.IsFinite(worldHeight)||meadow<0||meadow>1||rockMask<0||rockMask>1)throw new InvalidOperationException("Finite normalized actual Vista masks required.");
            float upland=Mathf.SmoothStep(0,1,Mathf.InverseLerp(170,360,worldHeight));
            float rock=Mathf.Clamp01(rockMask*.88f),soil=(1-rock)*Mathf.Lerp(.035f,.24f,Mathf.Max(upland,1-meadow));
            // The mask controls the secondary soil budget. The continuous
            // ORIGINAL green:yellow ratio is restored before any native write;
            // a categorical NWH index never chooses a grass color role.
            float grass=1-rock-soil;var values=new[]{grass*.5f,grass*.5f,soil,rock};var quantized=new int[4];var remainder=new float[4];int used=0;
            for(int i=0;i<4;i++){float scaled=values[i]*255;quantized[i]=Mathf.FloorToInt(scaled);remainder[i]=scaled-quantized[i];used+=quantized[i];}
            while(used++<255){int best=0;for(int i=1;i<4;i++)if(remainder[i]>remainder[best])best=i;quantized[best]++;remainder[best]=-1;}
            return quantized.Select(v=>v/255f).ToArray();
        }
        static int LandscapeDominant(float[] values){int best=0;for(int i=1;i<values.Length;i++)if(values[i]>values[best])best=i;return best;}
        public static float[] ConserveLandscapeSurfaceClassAndGrassRatio(float[] proposed,float[] original)
        {
            if(proposed==null||original==null||proposed.Length!=4||original.Length!=4||proposed.Concat(original).Any(v=>!float.IsFinite(v)||v<0||v>1)||Mathf.Abs(proposed.Sum()-1)>.00001f||Mathf.Abs(original.Sum()-1)>.00001f)throw new InvalidOperationException("Actual finite normalized four-channel source and proposed maps required.");
            int required=LandscapeDominant(original);float oldGrass=original[0]+original[1],newGrass=proposed[0]+proposed[1];var target=proposed.ToArray();
            if(oldGrass>0){target[0]=newGrass*original[0]/oldGrass;target[1]=newGrass*original[1]/oldGrass;}
            else{target[0]=target[1]=0;float secondary=target[2]+target[3];if(secondary<=0)throw new InvalidOperationException("A source with no grass needs an actual secondary surface budget.");target[2]/=secondary;target[3]/=secondary;}
            // Move from the original towards the desired soil/rock budget only
            // as far as the ORIGINAL dominant index allows. Both grass channels
            // scale together throughout this convex move; no channel swapping.
            float blend=1;for(int i=0;i<4;i++)if(i!=required){float before=original[required]-original[i],after=target[required]-target[i];if(after<0)blend=before<=0?0:Mathf.Min(blend,before/(before-after));}
            var bytes=new int[4];var remainder=new float[4];int used=0;for(int i=0;i<4;i++){float scaled=Mathf.Lerp(original[i],target[i],blend)*255;bytes[i]=Mathf.FloorToInt(scaled);remainder[i]=scaled-bytes[i];used+=bytes[i];}
            while(used++<255){int best=0;for(int i=1;i<4;i++)if(remainder[i]>remainder[best])best=i;bytes[best]++;remainder[best]=-1;}
            // Only quantization can now disturb the allowed winner. Correct at
            // most two UNorm8 units, rather than exchanging arbitrary weights.
            int corrections=0;while(LandscapeDominant(bytes.Select(v=>(float)v).ToArray())!=required){if(++corrections>2)throw new InvalidOperationException("Dominance preservation needs more than a bounded quantization correction.");int best=LandscapeDominant(bytes.Select(v=>(float)v).ToArray());bytes[best]--;bytes[required]++;}
            var result=bytes.Select(v=>v/255f).ToArray();if(LandscapeDominant(result)!=required||bytes.Sum()!=255)throw new InvalidOperationException("Preserve actual NWH dominant terrain index exactly.");return result;
        }
        static float[] LandscapeKeepGroundClass(float[] proposed,float[] original)=>ConserveLandscapeSurfaceClassAndGrassRatio(proposed,original);
        static void LandscapeMeasureGrassContinuity(float[,,] original,float[,,] saved,LandscapeSurfaceTile proof)
        {
            int height=original.GetLength(0),width=original.GetLength(1);var classes=LandscapeGroundClasses(original);double oldSum=0,newSum=0;
            float Ratio(float[,,] values,int z,int x){float sum=values[z,x,0]+values[z,x,1];return sum>0?values[z,x,0]/sum:0;}
            void Edge(int z,int x,int qz,int qx){int a=classes[z*width+x],b=classes[qz*width+qx];if(a>1||b>1||a==b)return;float oldJump=Mathf.Abs(Ratio(original,z,x)-Ratio(original,qz,qx)),newJump=Mathf.Abs(Ratio(saved,z,x)-Ratio(saved,qz,qx));proof.grassClassBoundaryEdges++;oldSum+=oldJump;newSum+=newJump;proof.sourceMaximumBoundaryGrassRatioJump=Mathf.Max(proof.sourceMaximumBoundaryGrassRatioJump,oldJump);proof.savedMaximumBoundaryGrassRatioJump=Mathf.Max(proof.savedMaximumBoundaryGrassRatioJump,newJump);}
            for(int z=0;z<height;z++)for(int x=0;x<width;x++){float a=original[z,x,0],b=original[z,x,1],c=saved[z,x,0],d=saved[z,x,1];proof.maximumGrassRatioCrossProductError=Mathf.Max(proof.maximumGrassRatioCrossProductError,Mathf.Abs(c*(a+b)-a*(c+d)));if(x+1<width)Edge(z,x,z,x+1);if(z+1<height)Edge(z,x,z+1,x);}
            if(proof.grassClassBoundaryEdges>0){proof.sourceMeanBoundaryGrassRatioJump=(float)(oldSum/proof.grassClassBoundaryEdges);proof.savedMeanBoundaryGrassRatioJump=(float)(newSum/proof.grassClassBoundaryEdges);}proof.originalGrassRatioPreserved=proof.maximumGrassRatioCrossProductError<=2.01f/255;proof.boundaryGrassRatiosSmooth=proof.grassClassBoundaryEdges>0&&proof.savedMeanBoundaryGrassRatioJump<=proof.sourceMeanBoundaryGrassRatioJump+.02f&&proof.savedMaximumBoundaryGrassRatioJump<=proof.sourceMaximumBoundaryGrassRatioJump+.04f;
            if(!proof.originalGrassRatioPreserved||!proof.boundaryGrassRatiosSmooth)throw new InvalidOperationException("Original continuous grass ratio or actual saved0/1boundary continuity failed; categorical color-role swapping is rejected.");
        }
        static byte[] LandscapeGroundClasses(float[,,] alpha)
        {int height=alpha.GetLength(0),width=alpha.GetLength(1);var values=new byte[height*width];for(int z=0;z<height;z++)for(int x=0;x<width;x++){int best=0;for(int i=1;i<4;i++)if(alpha[z,x,i]>alpha[z,x,best])best=i;values[z*width+x]=(byte)best;}return values;}
        static Vector3[] LandscapeNwhPoints(Terrain terrain,float[,,] original,out int transitionPoints)
        {
            var points=new List<Vector3>();var data=terrain.terrainData;int height=original.GetLength(0),width=original.GetLength(1);var classes=LandscapeGroundClasses(original);var origin=terrain.transform.position;
            for(int z=0;z<33;z++)for(int x=0;x<33;x++)points.Add(origin+new Vector3(data.size.x*x/32f,0,data.size.z*z/32f));transitionPoints=0;
            // Sample both sides of actual source dominance transitions across
            // the complete field, using the same floor-to-cell API as NWH.
            int stride=Mathf.Max(1,width*height/65536);for(int at=0;at<width*height&&transitionPoints<512;at+=stride){int x=at%width,z=at/width;if(x==0||classes[at]==classes[at-1])continue;float boundary=(float)x/width*data.size.x,zz=(z+.5f)/height*data.size.z;points.Add(origin+new Vector3(boundary-.001f,0,zz));points.Add(origin+new Vector3(boundary+.001f,0,zz));transitionPoints+=2;}
            return points.ToArray();
        }
        static int[] LandscapeNwhIndices(Terrain terrain,Vector3[] points)
        {var native=new NWH.VehiclePhysics2.GroundDetection.GroundDetection();return points.Select(p=>native.GetDominantTerrainTexture(p,terrain)).ToArray();}
        static string LandscapePhysicalWorld(DragonKingdomWorld world,IEnumerable<LandscapeSurfaceBinding> bindings)
        {
            var actual=new Dictionary<Terrain,TerrainData>();
            try
            {
                // The ONLY permitted identity delta is the four explicitly
                // recorded provider references, shared by renderer AND collider.
                // Compare all other serialized components with those four
                // references projected back to their immutable originals.
                foreach(var binding in bindings){var terrain=world.GetComponentsInChildren<Terrain>(true).Single(t=>PathOf(t.transform,world.transform)==binding.proof.hierarchy);if(terrain.GetComponent<TerrainCollider>().terrainData!=terrain.terrainData)throw new InvalidOperationException("Visual/physical provider divergence rejected.");actual.Add(terrain,terrain.terrainData);terrain.terrainData=binding.source;terrain.GetComponent<TerrainCollider>().terrainData=binding.source;}
                return DressingStableWorld(world);
            }
            finally{foreach(var pair in actual){pair.Key.terrainData=pair.Value;pair.Key.GetComponent<TerrainCollider>().terrainData=pair.Value;pair.Key.Flush();}}
        }
        static bool LandscapeExactPhysicalPayload(TerrainData source,TerrainData copy)
        {
            if(source==null||copy==null||source.size!=copy.size||source.heightmapResolution!=copy.heightmapResolution||source.holesResolution!=copy.holesResolution||HashHeights(source)!=HashHeights(copy)||PaletteHolesHash(source)!=PaletteHolesHash(copy))return false;
            for(int z=0;z<33;z++)for(int x=0;x<33;x++){float u=x/32f,v=z/32f;if(source.GetInterpolatedHeight(u,v)!=copy.GetInterpolatedHeight(u,v)||!source.GetInterpolatedNormal(u,v).Equals(copy.GetInterpolatedNormal(u,v))||source.GetSteepness(u,v)!=copy.GetSteepness(u,v))return false;}return true;
        }
        static Shader[] LandscapeActualShaders(Scene scene)=>scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Renderer>(true)).SelectMany(r=>r.sharedMaterials).Concat(scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Terrain>(true)).Select(t=>t.materialTemplate)).Where(m=>m!=null).Select(m=>m.shader).Where(s=>s!=null).Distinct().ToArray();
        static void SaveLandscapeSurfaceReview(LandscapeSurfaceReview review)
        {string output=Path.Combine(Output(),"LandscapeSurfaceFinish");Directory.CreateDirectory(Io(output));File.WriteAllText(Io(Path.Combine(output,"landscape-surface-finish-review.json")),JsonUtility.ToJson(review,true));}
        public static IEnumerator CaptureLandscapeSurfaceFinishReview()
        {
            var review=LastLandscapeSurfaceReview=new LandscapeSurfaceReview();var stack=new Stack<IEnumerator>();stack.Push(LandscapeSurfaceRoutine(review));
            try{while(stack.Count>0){bool moved;object current=null;try{moved=stack.Peek().MoveNext();if(moved)current=stack.Peek().Current;else(stack.Pop() as IDisposable)?.Dispose();}catch(Exception error){while(stack.Count>0)(stack.Pop() as IDisposable)?.Dispose();review.error=error.ToString();review.status="REJECTED_LANDSCAPE_SURFACE_FINISH";SaveLandscapeSurfaceReview(review);throw;}if(moved){if(current is IEnumerator nested)stack.Push(nested);else yield return current;}}}finally{while(stack.Count>0)(stack.Pop() as IDisposable)?.Dispose();}
        }
        static IEnumerator LandscapeSurfaceRoutine(LandscapeSurfaceReview review)
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new InvalidOperationException("Stopped compiled native Editor required.");
            string parentTag=TowerAppearanceArg("-tsDragonWorldCandidate",""),owned=TowerAppearanceArg("-tsDragonLandscapeCandidate","");
            if(new[]{parentTag,owned}.Any(t=>string.IsNullOrEmpty(t)||t.Length>50||t.Any(c=>!char.IsLetterOrDigit(c)&&c!='-'&&c!='_'))||parentTag==owned)throw new InvalidOperationException("Explicit passed parent and fresh bounded landscape candidate required.");
            string parentFolder=Root+"VisualCandidates/"+parentTag,receipt=parentFolder+"/Integration Receipt.json";var parent=JsonUtility.FromJson<TowerAppearanceParent>(File.ReadAllText(Io(receipt)));
            if(parent.tag!=parentTag||!parent.originalPhysicalGatesPassed||!parent.sourceBytesPreserved||parent.status!="PHYSICAL_GATES_PASSED_REQUIRES_RENDER_AND_ACTUAL_DRIVING_REVIEW")throw new InvalidOperationException("Actual physically passed parent required.");
            review.sourcePreview=TowerAppearanceArg("-tsDragonLandscapePreview",parent.preview);
            if(!review.sourcePreview.StartsWith(Root+"VisualCandidates/",StringComparison.Ordinal)||!review.sourcePreview.EndsWith(".unity",StringComparison.Ordinal))throw new InvalidOperationException("Explicit existing owned preview required.");
            review.folder=Root+"VisualCandidates/"+owned;review.preview=review.folder+"/Landscape surface preview.unity";string output=Path.Combine(Output(),"LandscapeSurfaceFinish");
            if(Directory.Exists(Io(review.folder))||File.Exists(Io(review.folder+".meta"))||Directory.Exists(Io(output)))throw new InvalidOperationException("Fresh candidate and evidence folders required; retain previous trials.");
            Directory.CreateDirectory(Io(output));var watched=new Dictionary<string,TowerAppearanceSource>(StringComparer.Ordinal);
            foreach(string p in AssetDatabase.GetDependencies(new[]{review.sourcePreview,receipt}.Concat(AuthoredPaletteSources()).ToArray(),true).Concat(AuthoredPaletteSources()).Append(receipt).Distinct())TowerWatchSource(watched,p,true);
            foreach(string p in Directory.GetFiles(Io(parent.cavern.dataFolder),"*",SearchOption.AllDirectories))TowerWatchSource(watched,p,false);
            foreach(string p in new[]{"Assets/PinwheelStudio/Vista/Personal/Runtime/Scripts/Graph/Nodes/NoiseNode.cs","Assets/PinwheelStudio/Vista/Personal/Runtime/Scripts/Graph/Nodes/ClampNode.cs","Assets/PinwheelStudio/Vista/Personal/Runtime/Scripts/Graph/Nodes/NodeLibraryUtilities.cs","Assets/PinwheelStudio/Vista/Personal/Runtime/Resources/Vista/Shaders/Graph/Noise.shader","Assets/PinwheelStudio/Vista/Personal/Runtime/Resources/Vista/Shaders/Graph/Clamp.shader","Assets/PinwheelStudio/Vista/Personal/Runtime/Scripts/Graph/Nodes/SlopeMaskNode.cs","Assets/PinwheelStudio/Vista/Personal/Runtime/Resources/Vista/Shaders/Graph/SlopeMask.shader"})TowerWatchSource(watched,p,true);
            TowerWatchPipelineShaderSources(watched);var setup=PrepareValidQuayProbeSceneSetup();var bindings=new List<LandscapeSurfaceBinding>();var frames=new List<TowerAppearanceFrame>();Scene scene=default;GameObject cameraObject=null;TerrainGraph graph=null;ExecutionHandle execution=null;Texture2D heightTexture=null;RenderTexture heightRt=null;bool outstanding=false,async=ShaderUtil.allowAsyncCompilation;
            try
            {
                ShaderUtil.allowAsyncCompilation=false;Directory.CreateDirectory(Io(review.folder+"/Terrain"));AssetDatabase.Refresh();scene=EditorSceneManager.OpenScene(review.sourcePreview,OpenSceneMode.Additive);
                if(!EditorSceneManager.SaveScene(scene,review.preview,true))throw new InvalidOperationException("Fresh landscape scene copy failed.");EditorSceneManager.CloseScene(scene,true);scene=EditorSceneManager.OpenScene(review.preview,OpenSceneMode.Additive);SceneManager.SetActiveScene(scene);
                var world=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<DragonKingdomWorld>(true)).Single();var terrains=world.GetComponentsInChildren<Terrain>(true);review.originalTerrains=terrains.Length;review.originalColliders=world.GetComponentsInChildren<Collider>(true).Length;
                if(terrains.Length!=16||world.GetComponentsInChildren<TerrainCollider>(true).Length!=16||review.originalColliders!=5957)throw new InvalidOperationException("Actual unchanged sixteen-terrain/5957-collider world required.");review.originalWorldBefore=DressingStableWorld(world);
                review.allOriginalFields=terrains.Select(t=>new LandscapeOriginalField{hierarchy=PathOf(t.transform,world.transform),source=AssetDatabase.GetAssetPath(t.terrainData),heightHash=HashHeights(t.terrainData),holeHash=PaletteHolesHash(t.terrainData),alphaHash=PaletteAlphaHash(t.terrainData),layerProperties=string.Join("\n",t.terrainData.terrainLayers.Select(PaletteProperties)),position=t.transform.position,size=t.terrainData.size,resolution=t.terrainData.heightmapResolution,holesResolution=t.terrainData.holesResolution}).ToArray();
                var inner=terrains.Where(t=>AssetDatabase.GetAssetPath(t.terrainData).StartsWith(parentFolder+"/Terrain/",StringComparison.Ordinal)&&t.terrainData.size==new Vector3(1600,600,1600)).OrderBy(t=>t.transform.position.z).ThenBy(t=>t.transform.position.x).ToArray();
                if(inner.Length!=4)throw new InvalidOperationException("Four exact current parent core fields required; do not substitute stale source terrain.");var mosaic=new float[1025,1025];var seen=new bool[1025,1025];
                foreach(var terrain in inner)
                {
                    var data=terrain.terrainData;if(data.heightmapResolution!=513||terrain.transform.position.y!=-20||terrain.GetComponent<TerrainCollider>().terrainData!=data||data.terrainLayers.Length!=4)throw new InvalidOperationException("Original terrain frame, collider and four vendor layers required.");
                    var sourceLayers=AuthoredPaletteSources().Select(p=>AssetDatabase.LoadAssetAtPath<TerrainLayer>(p)).ToArray();if(!data.terrainLayers.Select(PaletteProperties).SequenceEqual(sourceLayers.Select(PaletteProperties)))throw new InvalidOperationException("Exact installed authored Polyart layer properties required; no arbitrary tint or texture substitution.");
                    int ox=Mathf.RoundToInt((terrain.transform.position.x+1600)/3.125f),oz=Mathf.RoundToInt((terrain.transform.position.z+800)/3.125f);if((ox!=0&&ox!=512)||(oz!=0&&oz!=512))throw new InvalidOperationException("Exact current core coverage required.");var heights=data.GetHeights(0,0,513,513);
                    for(int z=0;z<513;z++)for(int x=0;x<513;x++){if(seen[oz+z,ox+x]&&Mathf.Abs(mosaic[oz+z,ox+x]-heights[z,x])*600>.04f)throw new InvalidOperationException("Original current native terrain seam exceeds unchanged limit.");mosaic[oz+z,ox+x]=heights[z,x];seen[oz+z,ox+x]=true;}
                    bindings.Add(new LandscapeSurfaceBinding{terrain=terrain,source=data,proof=new LandscapeSurfaceTile{hierarchy=PathOf(terrain.transform,world.transform),source=AssetDatabase.GetAssetPath(data),origin=terrain.transform.position,size=data.size,heightBefore=HashHeights(data),holesBefore=PaletteHolesHash(data),alphaBefore=PaletteAlphaHash(data)}});
                }
                if(seen.Cast<bool>().Any(v=>!v))throw new InvalidOperationException("Complete actual current core mosaic required.");
                cameraObject=new GameObject("Temporary landscape surface comparison camera");SceneManager.MoveGameObjectToScene(cameraObject,scene);var camera=cameraObject.AddComponent<Camera>();camera.enabled=false;camera.nearClipPlane=.3f;camera.farClipPlane=6500;camera.fieldOfView=52;camera.aspect=16f/9;camera.clearFlags=CameraClearFlags.Skybox;
                foreach(var view in DressingViews()){yield return TowerSettle(camera,view.Item2,view.Item3);frames.Add(TowerCapture(camera,output,view.Item1+"-before",1920,1080,view.Item3,LandscapeActualShaders(scene)));}
                graph=ScriptableObject.CreateInstance<TerrainGraph>();graph.Reset();var input=new InputNode{inputName="Exact unchanged current terrain height"};var slope=new SlopeMaskNode{maxAngle=90,minAngle=15,transition=AnimationCurve.EaseInOut(0,0,.48f,1)};var noise=LandscapeVendorNoise();var clamp=new ClampNode{min=0,max=1};
                graph.AddNode(input);graph.AddNode(slope);graph.AddNode(noise);graph.AddNode(clamp);graph.AddEdge(new Edge{outputSlot=new SlotRef(input.id,input.outputSlot.id),inputSlot=new SlotRef(slope.id,slope.inputSlot.id)});graph.AddEdge(new Edge{outputSlot=new SlotRef(noise.id,noise.outputSlot.id),inputSlot=new SlotRef(clamp.id,clamp.inputSlot.id)});review.noiseNodeJson=JsonUtility.ToJson(noise);review.clampNodeJson=JsonUtility.ToJson(clamp);review.slopeNodeJson=JsonUtility.ToJson(slope);
                AssetDatabase.CreateAsset(graph,review.folder+"/Actual vendor surface masks.asset");AssetDatabase.SaveAssetIfDirty(graph);heightTexture=FinishInputTexture(mosaic,false);heightRt=FinishInputRt(heightTexture);var inputs=new GraphInputContainer();inputs.AddTexture(input.inputName,heightRt);var config=TerrainGenerationConfigs.Create();config.resolution=1024;config.worldBounds=new Rect(-1600,-800,3200,3200);config.terrainHeight=600;config.seed=28082026;
                execution=graph.Execute(new[]{slope.id,noise.id,clamp.id},config,inputs);outstanding=true;double started=EditorApplication.timeSinceStartup;while(!execution.isCompleted){if(EditorApplication.timeSinceStartup-started>120)throw new TimeoutException("Actual vendor surface masks timed out; native process owns outstanding resources.");yield return null;}outstanding=false;
                var noiseRt=execution.data.GetRT(new SlotRef(noise.id,noise.outputSlot.id));var clampedRt=execution.data.GetRT(new SlotRef(clamp.id,clamp.outputSlot.id));var slopeRt=execution.data.GetRT(new SlotRef(slope.id,slope.outputSlot.id));if(noiseRt?.renderTexture==null||clampedRt?.renderTexture==null||slopeRt?.renderTexture==null)throw new InvalidOperationException("Actual Vista raw noise, vendor-clamped noise and slope outputs required.");var raw=FinishReadHeights(noiseRt.renderTexture,1025);var meadow=FinishReadHeights(clampedRt.renderTexture,1025);var rock=FinishReadHeights(slopeRt.renderTexture,1025);review.maskBounds=ValidateLandscapeClampedMasks(raw,meadow);var rawNative=LandscapeReadExactMaskTexels(noiseRt.renderTexture);var clampedNative=LandscapeReadExactMaskTexels(clampedRt.renderTexture);review.nativeMaskBounds=LandscapeExactNativeClampProof(rawNative,clampedNative);review.noiseSha256=FinishFloatHash(raw);review.clampedNoiseSha256=FinishFloatHash(meadow);review.slopeSha256=FinishFloatHash(rock);File.WriteAllBytes(Io(Path.Combine(output,"actual-vista-meadow-raw.float32")),FinishFloatBytes(raw));File.WriteAllBytes(Io(Path.Combine(output,"actual-vista-meadow.float32")),FinishFloatBytes(meadow));File.WriteAllBytes(Io(Path.Combine(output,"actual-vista-meadow-raw-native.float32")),FinishFloatBytes(rawNative));File.WriteAllBytes(Io(Path.Combine(output,"actual-vista-meadow-clamped-native.float32")),FinishFloatBytes(clampedNative));File.WriteAllBytes(Io(Path.Combine(output,"actual-vista-rock-slope.float32")),FinishFloatBytes(rock));
                foreach(var binding in bindings)
                {
                    var data=binding.source;var row=binding.proof;binding.copy=UnityEngine.Object.Instantiate(data);row.candidate=review.folder+"/Terrain/"+Path.GetFileName(row.source);AssetDatabase.CreateAsset(binding.copy,row.candidate);int width=data.alphamapWidth,height=data.alphamapHeight;var original=data.GetAlphamaps(0,0,width,height);binding.nwhPoints=LandscapeNwhPoints(binding.terrain,original,out row.nwhTransitionPoints);binding.originalNwhIndices=LandscapeNwhIndices(binding.terrain,binding.nwhPoints);row.nwhSampledPoints=binding.nwhPoints.Length;var originalClasses=LandscapeGroundClasses(original);row.originalDominantTerrainSha256=Hash(originalClasses);var alpha=new float[height,width,4];int ox=Mathf.RoundToInt((row.origin.x+1600)/3.125f),oz=Mathf.RoundToInt((row.origin.z+800)/3.125f);
                    for(int z=0;z<height;z++)for(int x=0;x<width;x++)
                    {
                        float u=(float)x/(width-1),v=(float)z/(height-1);int mx=ox+Mathf.RoundToInt(u*512),mz=oz+Mathf.RoundToInt(v*512);var priorWeights=new[]{original[z,x,0],original[z,x,1],original[z,x,2],original[z,x,3]};var weights=LandscapeKeepGroundClass(ComposeLandscapeSurfaceWeights(meadow[mz,mx],rock[mz,mx],data.GetInterpolatedHeight(u,v)+row.origin.y),priorWeights);bool changed=false;
                        for(int i=0;i<4;i++){alpha[z,x,i]=weights[i];changed|=Mathf.Abs(weights[i]-original[z,x,i])>.01f;}if(changed)row.changedPixels++;if(rock[mz,mx]<.05f){row.meadowPixels++;row.meadowGreenMinimum=Mathf.Min(row.meadowGreenMinimum,weights[0]);row.meadowGreenMaximum=Mathf.Max(row.meadowGreenMaximum,weights[0]);}if(rock[mz,mx]>.7f)row.rockFacePixels++;
                    }
                    binding.copy.SetAlphamaps(0,0,alpha);binding.copy.SetBaseMapDirty();EditorUtility.SetDirty(binding.copy);AssetDatabase.SaveAssetIfDirty(binding.copy);binding.terrain.terrainData=binding.copy;binding.terrain.GetComponent<TerrainCollider>().terrainData=binding.copy;binding.terrain.Flush();row.sameVisualPhysicalProvider=binding.terrain.GetComponent<TerrainCollider>().terrainData==binding.terrain.terrainData;row.interpolationPayloadExact=LandscapeExactPhysicalPayload(data,binding.copy);row.heightAfter=HashHeights(binding.copy);row.holesAfter=PaletteHolesHash(binding.copy);row.alphaAfter=PaletteAlphaHash(binding.copy);row.layersExact=binding.copy.terrainLayers.SequenceEqual(data.terrainLayers);
                    var readback=binding.copy.GetAlphamaps(0,0,width,height);for(int z=0;z<height;z++)for(int x=0;x<width;x++){float sum=0;for(int i=0;i<4;i++){if(!float.IsFinite(readback[z,x,i])||readback[z,x,i]<0||readback[z,x,i]>1)throw new InvalidOperationException("Invalid saved native splat readback.");sum+=readback[z,x,i];}row.maximumWeightSumError=Mathf.Max(row.maximumWeightSumError,Mathf.Abs(sum-1));}
                    var savedClasses=LandscapeGroundClasses(readback);row.savedDominantTerrainSha256=Hash(savedClasses);row.dominantLayerMismatches=originalClasses.Zip(savedClasses,(a,b)=>a!=b).Count(v=>v);row.nwhIndexMismatches=binding.originalNwhIndices.Zip(LandscapeNwhIndices(binding.terrain,binding.nwhPoints),(a,b)=>a!=b).Count(v=>v);
                    LandscapeMeasureGrassContinuity(original,readback,row);
                    if(row.heightAfter!=row.heightBefore||row.holesAfter!=row.holesBefore||!row.layersExact||!row.sameVisualPhysicalProvider||!row.interpolationPayloadExact||row.maximumWeightSumError>1.01f/255||row.changedPixels<1000||row.dominantLayerMismatches!=0||row.nwhIndexMismatches!=0)throw new InvalidOperationException("Saved appearance copy changed physical payload/NWH ground classification/layers, split provider authority, failed normalization or had no actual effect.");
                }
                if(bindings.Sum(b=>b.proof.meadowPixels)<1000||bindings.Max(b=>b.proof.meadowGreenMaximum-b.proof.meadowGreenMinimum)<.25f||bindings.Sum(b=>b.proof.rockFacePixels)<1000)throw new InvalidOperationException("Real vendor outputs must produce both varied meadow and rocky grade-face composition.");
                review.originalWorldAfter=LandscapePhysicalWorld(world,bindings);review.originalWorldExact=review.originalWorldBefore==review.originalWorldAfter;if(!review.originalWorldExact)throw new InvalidOperationException("Original world identity/physics changed outside four explicit renderer data bindings.");
                UnityEngine.Object.DestroyImmediate(cameraObject);cameraObject=null;if(!EditorSceneManager.SaveScene(scene,review.preview))throw new InvalidOperationException("Landscape copy did not save.");EditorSceneManager.CloseScene(scene,true);scene=EditorSceneManager.OpenScene(review.preview,OpenSceneMode.Additive);SceneManager.SetActiveScene(scene);world=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<DragonKingdomWorld>(true)).Single();review.originalWorldReloaded=LandscapePhysicalWorld(world,bindings);
                foreach(var field in review.allOriginalFields){var t=world.GetComponentsInChildren<Terrain>(true).Single(v=>PathOf(v.transform,world.transform)==field.hierarchy);var original=AssetDatabase.LoadAssetAtPath<TerrainData>(field.source);field.originalBytesAndMemoryExact=HashHeights(original)==field.heightHash&&PaletteHolesHash(original)==field.holeHash&&PaletteAlphaHash(original)==field.alphaHash&&string.Join("\n",original.terrainLayers.Select(PaletteProperties))==field.layerProperties&&original.size==field.size&&original.heightmapResolution==field.resolution&&original.holesResolution==field.holesResolution;var declared=bindings.SingleOrDefault(b=>b.proof.hierarchy==field.hierarchy);string expected=declared==null?field.source:declared.proof.candidate;field.savedProviderPayloadExact=AssetDatabase.GetAssetPath(t.terrainData)==expected&&t.GetComponent<TerrainCollider>().terrainData==t.terrainData&&t.transform.position==field.position&&LandscapeExactPhysicalPayload(original,t.terrainData);}
                review.savedReloaded=review.originalWorldReloaded==review.originalWorldBefore&&world.GetComponentsInChildren<Terrain>(true).Length==16&&world.GetComponentsInChildren<Collider>(true).Length==review.originalColliders&&review.allOriginalFields.All(f=>f.originalBytesAndMemoryExact&&f.savedProviderPayloadExact)&&bindings.All(b=>{var t=world.GetComponentsInChildren<Terrain>(true).Single(v=>PathOf(v.transform,world.transform)==b.proof.hierarchy);b.proof.nwhIndexMismatches+=b.originalNwhIndices.Zip(LandscapeNwhIndices(t,b.nwhPoints),(a,c)=>a!=c).Count(v=>v);return AssetDatabase.GetAssetPath(t.terrainData)==b.proof.candidate&&HashHeights(t.terrainData)==b.proof.heightBefore&&PaletteHolesHash(t.terrainData)==b.proof.holesBefore&&PaletteAlphaHash(t.terrainData)==b.proof.alphaAfter&&t.GetComponent<TerrainCollider>().terrainData==t.terrainData&&b.proof.nwhIndexMismatches==0;});if(!review.savedReloaded)throw new InvalidOperationException("Saved/reloaded four provider transitions, original field payloads, NWH indices or original collision identity differs.");
                cameraObject=new GameObject("Temporary saved landscape comparison camera");SceneManager.MoveGameObjectToScene(cameraObject,scene);camera=cameraObject.AddComponent<Camera>();camera.enabled=false;camera.nearClipPlane=.3f;camera.farClipPlane=6500;camera.fieldOfView=52;camera.aspect=16f/9;camera.clearFlags=CameraClearFlags.Skybox;
                foreach(var view in DressingViews()){yield return TowerSettle(camera,view.Item2,view.Item3);frames.Add(TowerCapture(camera,output,view.Item1+"-saved-after",1920,1080,view.Item3,LandscapeActualShaders(scene)));}
                if(frames.Any(f=>f.shaderErrors.Length!=0))throw new InvalidOperationException("Actual rendered landscape shader compiler errors require correction before visual review.");
                review.status="SAVED_VENDOR_LANDSCAPE_SURFACES_CAPTURED_REQUIRES_DIRECT_VISUAL_REVIEW_NOT_ADOPTION";
            }
            finally
            {
                if(cameraObject!=null)UnityEngine.Object.DestroyImmediate(cameraObject);if(!outstanding){execution?.Dispose();if(heightTexture!=null)UnityEngine.Object.DestroyImmediate(heightTexture);if(heightRt!=null){heightRt.Release();UnityEngine.Object.DestroyImmediate(heightRt);}review.vendorResourcesDisposed=true;}
                if(scene.IsValid()&&scene.isLoaded)EditorSceneManager.CloseScene(scene,true);if(setup.Length>0)EditorSceneManager.RestoreSceneManagerSetup(setup);ShaderUtil.allowAsyncCompilation=async;review.incomingSceneContextRestored=setup.Select(s=>s.path+"|"+s.isLoaded+"|"+s.isActive).SequenceEqual(EditorSceneManager.GetSceneManagerSetup().Select(s=>s.path+"|"+s.isLoaded+"|"+s.isActive));review.changedSources=watched.Values.Where(p=>p.resourceKind!="UNITY_BUILTIN_VIRTUAL_RESOURCE_NO_DISK_FILE"&&(!File.Exists(p.resolvedPath)||TowerFileHash(p.resolvedPath)!=p.sha256)).Select(p=>p.path).ToArray();review.originalSourcesExact=review.changedSources.Length==0;review.watchedSources=watched.Values.ToArray();review.tiles=bindings.Select(b=>b.proof).ToArray();review.frames=frames.ToArray();SaveLandscapeSurfaceReview(review);
                if(!review.originalSourcesExact||!review.incomingSceneContextRestored)throw new InvalidOperationException("Original source bytes or incoming scene context changed during landscape appearance trial.");
            }
        }
    }
}
#endif

#if UNITY_EDITOR && VISTA
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Pinwheel.Vista;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
namespace LWS.TruckDragonKingdom.Editor
{
    public static partial class DragonKingdomRegionalWorldIntegration
    {
        const string SmoothLibrarySource="Assets/PinwheelStudio/Vista/Personal/Runtime/Scripts/Core/VistaLib.cs",SmoothShaderSource="Assets/PinwheelStudio/Vista/Personal/Runtime/Resources/Vista/Shaders/Graph/Smooth.shader";
        [Serializable]public sealed class MaskedSmoothReadback
        {public int iteration,pixels,nonfinite,belowZero,aboveOne,protectedChanged,firstFailureIndex=-1;public float minimum=float.PositiveInfinity,maximum=float.NegativeInfinity,maximumProtectedDelta;public string rawFloatPath,sha256;public bool fullInputIdentityRequired,fullInputIdentityExact=true,passed;}
        [Serializable]public sealed class PublicMaskedFeedbackStage{public string stage,utc;public double elapsedSeconds;}
        [Serializable]public sealed class PublicMaskedFeedbackEvidence
        {
            public string scope="PUBLIC_VENDOR_MASKED_CURRENT_INPUT_FEEDBACK_ONLY_NOT_SOURCE_ASSET_SELECTION_WORLD_GEOMETRY_OR_ADOPTION",status="IN_PROGRESS",error,publicApi="Pinwheel.Vista.VistaLib.SmoothProgressive",receipt,inputSha256,maskSha256,outputSha256,targetFormat,maskFormat;
            public int iterations,iterationPerFrame,readbackEveryIterations,canvasResolution=1025;
            public bool maskIdentityExact,everyGpuReadbackExact,inputArraysExact=true,sourceBytesExact=true,resourcesRestored=true;
            public List<MaskedSmoothReadback> readbacks=new List<MaskedSmoothReadback>();public List<PublicMaskedFeedbackStage> stages=new List<PublicMaskedFeedbackStage>();public RetainedThermalSourceFile[] watchedSources;
            [NonSerialized]public float[,] actualOutput;
        }
        static int[] MaskedSmoothIterations(string selection)
        {if(selection=="All")return new[]{50,100,200};if(selection=="Extended")return new[]{500,1000};if(selection=="50"||selection=="100"||selection=="200"||selection=="500"||selection=="1000")return new[]{int.Parse(selection,System.Globalization.CultureInfo.InvariantCulture)};throw new InvalidOperationException("Explicit vendor masked-smooth iterations50,100,200,500,1000,All orExtended required.");}
        static int MaskedSmoothReadbackCadence(int iterations)
        {MaskedSmoothIterations(iterations.ToString(System.Globalization.CultureInfo.InvariantCulture));return iterations>=500?50:10;}
        static MaskedSmoothReadback InspectMaskedSmoothPixels(float[] pixels,float[,] input,float[,] weight,int iteration,bool requireIdentity)
        {
            if(pixels==null||pixels.Length!=1025*1025||input?.GetLength(0)!=1025||input.GetLength(1)!=1025||weight?.GetLength(0)!=1025||weight.GetLength(1)!=1025)throw new InvalidOperationException("Exact1025 vertex-resolution input/mask/GPU readback required.");var proof=new MaskedSmoothReadback{iteration=iteration,pixels=pixels.Length,sha256=FinishDigest(FloatPixelBytes(pixels)),fullInputIdentityRequired=requireIdentity};
            for(int z=0;z<1025;z++)for(int x=0;x<1025;x++){int i=z*1025+x;float value=pixels[i];bool same=BitConverter.SingleToInt32Bits(value)==BitConverter.SingleToInt32Bits(input[z,x]);if(!float.IsFinite(value)){proof.nonfinite++;if(proof.firstFailureIndex<0)proof.firstFailureIndex=i;}else{proof.minimum=Mathf.Min(proof.minimum,value);proof.maximum=Mathf.Max(proof.maximum,value);if(value<0){proof.belowZero++;if(proof.firstFailureIndex<0)proof.firstFailureIndex=i;}if(value>1){proof.aboveOne++;if(proof.firstFailureIndex<0)proof.firstFailureIndex=i;}}if(!same)proof.fullInputIdentityExact=false;if(weight[z,x]==0&&!same){proof.protectedChanged++;proof.maximumProtectedDelta=Mathf.Max(proof.maximumProtectedDelta,Mathf.Abs(value-input[z,x])*600);if(proof.firstFailureIndex<0)proof.firstFailureIndex=i;}}
            proof.passed=proof.nonfinite==0&&proof.belowZero==0&&proof.aboveOne==0&&proof.protectedChanged==0&&(!requireIdentity||proof.fullInputIdentityExact);return proof;
        }
        static byte[] FloatPixelBytes(float[] values){var bytes=new byte[values.Length*4];Buffer.BlockCopy(values,0,bytes,0,bytes.Length);return bytes;}
        static MaskedSmoothReadback CaptureMaskedSmoothReadback(RenderTexture rt,float[,] input,float[,] weight,int iteration,bool identity,string output,out float[] values)
        {values=ReadActualVistaGpuPixels(rt);var proof=InspectMaskedSmoothPixels(values,input,weight,iteration,identity);proof.rawFloatPath=Path.Combine(output,"gpu-"+iteration.ToString("D4")+".float32");File.WriteAllBytes(Io(proof.rawFloatPath),FloatPixelBytes(values));return proof;}
        static RenderTexture NewMaskedSmoothTexture()
        {var rt=new RenderTexture(1025,1025,0,RenderTextureFormat.RFloat,RenderTextureReadWrite.Linear){wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Point,hideFlags=HideFlags.HideAndDontSave};rt.Create();return rt;}
        static void RequirePublicMaskedFeedbackParameters(int iterations,int iterationPerFrame,int readbackCadence)
        {
            MaskedSmoothIterations(iterations.ToString(System.Globalization.CultureInfo.InvariantCulture));if(iterationPerFrame!=10||readbackCadence!=MaskedSmoothReadbackCadence(iterations))throw new InvalidOperationException("Preserve the explicit proven public vendor yield/readback cadence; no implicit kernel tuning.");
        }
        // The caller supplies CURRENT actual native fields and the one physical
        // mask authority. This helper knows no historical run/terrain asset and
        // never selects or writes TerrainData or rebuilds any world/SDK road.
        static IEnumerator ExecutePublicMaskedCurrentFeedback(float[,] input,float[,] weight,int iterations,int iterationPerFrame,int readbackCadence,string output,PublicMaskedFeedbackEvidence proof,Action<string> observe=null)
        {
            if(proof==null)throw new ArgumentNullException(nameof(proof));if(proof.readbacks.Count!=0||proof.actualOutput!=null||!string.IsNullOrEmpty(proof.receipt))throw new InvalidOperationException("Fresh per-invocation feedback evidence required; never reuse or overwrite prior input/output history.");RequirePublicMaskedFeedbackParameters(iterations,iterationPerFrame,readbackCadence);if(input?.GetLength(0)!=1025||input.GetLength(1)!=1025||weight?.GetLength(0)!=1025||weight.GetLength(1)!=1025)throw new InvalidOperationException("Current actual1025 native vertex mosaic/mask required.");foreach(float value in input)if(!float.IsFinite(value)||value<0||value>1)throw new InvalidOperationException("Current actual native input must be finite in[0,1], never clamped.");foreach(float value in weight)if(!float.IsFinite(value)||value<0||value>1)throw new InvalidOperationException("Exact physical editable mask must be finite in[0,1].");
            proof.iterations=iterations;proof.iterationPerFrame=iterationPerFrame;proof.readbackEveryIterations=readbackCadence;proof.inputSha256=FinishFloatHash(input);proof.maskSha256=FinishFloatHash(weight);proof.receipt=Path.Combine(output,"public-masked-feedback.json");if(File.Exists(Io(proof.receipt)))throw new InvalidOperationException("Preserve prior public masked-feedback receipts; fresh output required.");Directory.CreateDirectory(Io(output));double started=EditorApplication.timeSinceStartup;
            void Stage(string name){proof.stages.Add(new PublicMaskedFeedbackStage{stage=name,utc=DateTime.UtcNow.ToString("O"),elapsedSeconds=EditorApplication.timeSinceStartup-started});File.WriteAllText(Io(proof.receipt),JsonUtility.ToJson(proof,true));observe?.Invoke(name);}
            var paths=new[]{SmoothLibrarySource,SmoothShaderSource,Root+"Editor/DragonKingdomRegionalWorldIntegration.PublicMaskedFeedback.cs",Root+"Editor/DragonKingdomRegionalWorldIntegration.VistaReadback.cs",Root+"Editor/DragonKingdomRegionalWorldIntegration.VistaFinishProbe.cs"}.SelectMany(p=>new[]{p,p+".meta"}).Distinct().ToArray();var sources=paths.ToDictionary(p=>p,AssetSha);Texture2D heightTexture=null,maskTexture=null;RenderTexture target=null,temp=null,maskCheck=null;IEnumerator routine=null;var materialIds=new HashSet<int>(Resources.FindObjectsOfTypeAll<Material>().Select(m=>m.GetInstanceID()));Stage("EXPLICIT_CURRENT_INPUT_PARAMETERS_AND_VENDOR_SOURCES_QUALIFIED");
            try
            {
                if(!SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RFloat)||(SystemInfo.copyTextureSupport&CopyTextureSupport.Basic)==0)throw new InvalidOperationException("Public exact RFloat texture copy unsupported; no resampled replacement input permitted.");proof.resourcesRestored=false;heightTexture=FinishInputTexture(input,false);heightTexture.filterMode=FilterMode.Point;maskTexture=FinishInputTexture(weight,false);maskTexture.filterMode=FilterMode.Point;maskCheck=NewMaskedSmoothTexture();Graphics.CopyTexture(maskTexture,maskCheck);var maskPixels=ReadActualVistaGpuPixels(maskCheck);proof.maskIdentityExact=FinishDigest(FloatPixelBytes(maskPixels))==proof.maskSha256;if(!proof.maskIdentityExact){proof.error="Native1025 mask texture differs from exact current physical mask bytes.";throw new InvalidOperationException(proof.error);}maskCheck.Release();UnityEngine.Object.DestroyImmediate(maskCheck);maskCheck=null;target=NewMaskedSmoothTexture();temp=NewMaskedSmoothTexture();proof.targetFormat=target.graphicsFormat+"/"+target.filterMode;proof.maskFormat=maskTexture.graphicsFormat+"/"+maskTexture.filterMode;Graphics.CopyTexture(heightTexture,target);var identity=CaptureMaskedSmoothReadback(target,input,weight,0,true,output,out _);proof.readbacks.Add(identity);if(!identity.passed){proof.error="Exact current input GPU copy/readback failed; no smoothing or posthoc core correction permitted.";throw new InvalidOperationException(proof.error);}Stage("FULL_CURRENT_INPUT_AND_MASK_GPU_IDENTITY_EXACT");routine=VistaLib.SmoothProgressive(target,temp,maskTexture,iterations,iterationPerFrame);int yieldIndex=0;Exception readbackFailure=null;double gpuStarted=EditorApplication.timeSinceStartup;Stage("PUBLIC_VENDOR_SMOOTH_"+iterations+"_START");
                while(true)
                {
                    bool more;try{more=routine.MoveNext();}catch(Exception error){proof.error=error.ToString();Stage("PUBLIC_VENDOR_API_EXCEPTION");throw;}if(!more)break;
                    int completed=Math.Min(iterations,1+yieldIndex*iterationPerFrame);yieldIndex++;if(completed==1||(completed-1)%readbackCadence==0){var latest=completed%2==0?target:temp;var sample=CaptureMaskedSmoothReadback(latest,input,weight,completed,false,output,out _);proof.readbacks.Add(sample);if(!sample.passed&&readbackFailure==null)readbackFailure=new InvalidOperationException("Vendor feedback changed exact physical core or height domain at iteration"+completed+"; no core pullback/clamp permitted.");Stage("PUBLIC_GPU_READBACK_"+completed);}if(EditorApplication.timeSinceStartup-gpuStarted>240){proof.error="Bounded public masked feedback exceeded240seconds.";throw new TimeoutException(proof.error);}yield return routine.Current;
                }
                routine=null;var final=CaptureMaskedSmoothReadback(target,input,weight,iterations,false,output,out var finalPixels);proof.readbacks.Add(final);proof.everyGpuReadbackExact=proof.readbacks.All(r=>r.passed);Stage("PUBLIC_VENDOR_SMOOTH_"+iterations+"_END");if(readbackFailure!=null||!final.passed){proof.error=(readbackFailure??new InvalidOperationException("Final public vendor feedback failed exact core/domain guard.")).ToString();throw readbackFailure??new InvalidOperationException(proof.error);}proof.actualOutput=new float[1025,1025];Buffer.BlockCopy(finalPixels,0,proof.actualOutput,0,finalPixels.Length*4);proof.outputSha256=FinishFloatHash(proof.actualOutput);if(proof.outputSha256!=final.sha256)throw new InvalidOperationException("Native-transfer output differs from measured final GPU readback.");proof.status="ACTUAL_PUBLIC_CURRENT_INPUT_MASKED_FEEDBACK_RECORDED_REQUIRES_CALLER_NATIVE_PHYSICAL_AND_WORLD_GATES";
            }
            finally
            {
                (routine as IDisposable)?.Dispose();foreach(var rt in new[]{target,temp,maskCheck})if(rt!=null){rt.Release();UnityEngine.Object.DestroyImmediate(rt);}foreach(var texture in new[]{heightTexture,maskTexture})if(texture!=null)UnityEngine.Object.DestroyImmediate(texture);foreach(var material in Resources.FindObjectsOfTypeAll<Material>().Where(m=>!materialIds.Contains(m.GetInstanceID())&&!EditorUtility.IsPersistent(m)&&m.shader!=null&&m.shader.name=="Hidden/Vista/Graph/Smooth"))UnityEngine.Object.DestroyImmediate(material);proof.resourcesRestored=new[]{target,temp,maskCheck}.All(rt=>rt==null)&&new[]{heightTexture,maskTexture}.All(t=>t==null)&&!Resources.FindObjectsOfTypeAll<Material>().Any(m=>!materialIds.Contains(m.GetInstanceID())&&!EditorUtility.IsPersistent(m)&&m.shader!=null&&m.shader.name=="Hidden/Vista/Graph/Smooth");proof.inputArraysExact=FinishFloatHash(input)==proof.inputSha256&&FinishFloatHash(weight)==proof.maskSha256;proof.sourceBytesExact=sources.All(p=>File.Exists(Io(p.Key))&&AssetSha(p.Key)==p.Value);proof.watchedSources=sources.Select(p=>new RetainedThermalSourceFile{path=p.Key,sha256=p.Value}).ToArray();if(proof.status=="IN_PROGRESS")proof.status="REJECTED_PUBLIC_MASKED_FEEDBACK_INSPECT_NATIVE_EXCEPTION";Stage("CURRENT_INPUT_VENDOR_SOURCES_AND_TRANSIENT_RESOURCES_RESTORED");if(!proof.resourcesRestored||!proof.inputArraysExact||!proof.sourceBytesExact)throw new InvalidOperationException("Public masked-feedback adapter changed current inputs/vendor sources or leaked owned resources.");
            }
        }
    }
}
#endif

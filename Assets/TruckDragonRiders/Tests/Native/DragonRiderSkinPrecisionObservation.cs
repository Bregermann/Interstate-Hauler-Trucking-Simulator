#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;

// Standalone, read-only numeric experiment. The original render observer and
// its thresholds remain byte-exact; its old prediction failure is evidence.
public sealed class DragonRiderSkinPrecisionObservation : IDisposable
{
    const BindingFlags Any=BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
    readonly Component match;readonly object original;readonly string directory;readonly Type authority=typeof(DragonRiderCabSightlineObservation);
    readonly List<Frame> frames=new List<Frame>();readonly Dictionary<int,Vector3> last=new Dictionary<int,Vector3>();
    public int Count=>frames.Count;
    public bool Ready=>frames.Count>=16&&frames.Select(f=>f.species).Distinct().Count()==2&&frames.Any(f=>!f.oldFloatPass);
    public string Failure {get;private set;}
    [Serializable]sealed class Weight{public int a,b,c,d;public float wa,wb,wc,wd;}
    [Serializable]sealed class Bone{public string path;public Matrix4x4 world,local,bindpose;}
    [Serializable]sealed class MeshCapture{public string source,guid,localId;public Vector3[] hierarchyLocal,hierarchyWorld;public double[] rendererToCommon;public Vector3 rendererLocalScale;public string commonAncestor;public LocalPose[] hierarchy;public Vector3[] sourceVertices,nativeLocal,nativeWorld,worldFloat,worldDouble,relativeFloat,relativeDouble;public Weight[] weights;public Bone[] bones;public int[] triangles;public Matrix4x4 rendererWorld;}
    [Serializable]sealed class LocalPose{public string path,parent;public Matrix4x4 local;}
    [Serializable]sealed class Frame
    {
        public string scope="REAL_NATIVE_TRUE_BAKE_AND_UNCHANGED_IMPORTED_SKIN_NUMERIC_COMPARISON_NOT_PROFILE_OR_HUMAN_ACCEPTANCE",species,sourceClips,oldPredictionError,oldPredictionJson,beforeState,afterState,failure;
        public float hierarchyLocalRmse,hierarchyWorldRmse;public int owner,frame,triangles,boxes,nativeSatPairs,doubleSatPairs;public float clock,horizontalDistance,worldFloatRmse,worldDoubleRmse,relativeFloatRmse,relativeDoubleRmse;
        public bool actualOwnedStateExact,otherOriginalPredictionGuardsPassed,oldFloatPass,newDoublePass;public Vector3 position,commonOrigin;public MeshCapture geometry;
        public string[] actual25Rays;public Matrix4x4[] boxWorld;public Vector3[] boxCenters,boxSizes;public float[] bodyMasses;
    }
    [Serializable]sealed class Receipt{public string scope="STANDALONE_NATIVE_FAR_WORLD_PRECISION_EXPERIMENT_ORIGINAL_OBSERVER_UNCHANGED",failure;public int frames,oldFloatFailures;public Frame[] samples;}
    static object Read(object source,string name)=>source.GetType().GetField(name,Any) is FieldInfo field?field.GetValue(source):source.GetType().GetProperty(name,Any).GetValue(source);
    static void Set(object source,string name,object value)=>source.GetType().GetField(name,Any).SetValue(source,value);
    object Call(string name,params object[] args)=>authority.GetMethod(name,Any).Invoke(original,args);
    public DragonRiderSkinPrecisionObservation(Component session,string output)
    {
        RequireAnalyticControls();RequireHierarchyControls();match=session;directory=output;Directory.CreateDirectory(directory);
        // Dispose immediately, before any render: never intercept/suppress an
        // exception from the original observer's RenderPipeline callback.
        original=Activator.CreateInstance(authority,match,Path.Combine(directory,"unused-original-callback-receipt.json"));((IDisposable)original).Dispose();
        RenderPipelineManager.beginCameraRendering+=Capture;
    }
    void Capture(ScriptableRenderContext context,Camera camera)
    {
        if(frames.Count>=32||Failure!=null||!(bool)Read(match,"Live")||(float)Read(match,"Clock")<10)return;
        var cameras=(Camera[])Read(match,"cameras");int view=Array.IndexOf(cameras,camera);if(view<0)return;
        int owner=((int[])Read(Read(Read(match,"drivingViews"),"rally"),"owners"))[view];var racer=((Array)Read(match,"racers")).GetValue(owner);
        if(Read(racer,"Phase").ToString()!="Mounted")return;
        var actor=(Component)Read(racer,"Dragon");var motor=(Component)Read(actor,"motor");var position=motor.transform.position;float distance=new Vector2(position.x,position.z).magnitude;
        if(distance<600||last.TryGetValue(owner,out var previous)&&Vector3.Distance(position,previous)<50||frames.Count(f=>f.owner==owner)>=8)return;
        last[owner]=position;var row=new Frame{owner=owner,frame=Time.frameCount,clock=(float)Read(match,"Clock"),position=position,horizontalDistance=distance,species=(string)Read(Read(motor,"Species"),"id")};
        row.beforeState=(string)Call("SnapshotOwnedState");frames.Add(row);
        try
        {
            var skins=Call("ReadActualSkins",camera);object skin=((IEnumerable)skins).Cast<object>().Single(value=>(int)Read(value,"slot")==owner);var renderer=(SkinnedMeshRenderer)Read(skin,"actualRenderer");var native=(Vector3[])Read(skin,"world");var mesh=renderer.sharedMesh;var weights=mesh.boneWeights;var bones=renderer.bones;var bindposes=mesh.bindposes;
            Vector3[] points;using(var data=UnityEditor.MeshUtility.AcquireReadOnlyMeshData(mesh))using(var buffer=new Unity.Collections.NativeArray<Vector3>(data[0].vertexCount,Unity.Collections.Allocator.Temp)){data[0].GetVertices(buffer);points=buffer.ToArray();}
            var baked=new Mesh();Vector3[] nativeLocal;try{renderer.BakeMesh(baked,true);nativeLocal=baked.vertices;for(int i=0;i<native.Length;i++)if(!renderer.transform.TransformPoint(nativeLocal[i]).Equals(native[i]))throw new InvalidOperationException("Two same-frame native true bakes differ.");}finally{UnityEngine.Object.Destroy(baked);}
            row.commonOrigin=renderer.transform.position;var matrices=bones.Select(b=>b.localToWorldMatrix).ToArray();var wf=new Vector3[points.Length];var wd=new Vector3[points.Length];var rf=new Vector3[points.Length];var rd=new Vector3[points.Length];
            for(int i=0;i<points.Length;i++)Evaluate(points[i],weights[i],bindposes,matrices,row.commonOrigin,out wf[i],out wd[i],out rf[i],out rd[i]);
            float Rmse(Vector3[] values){double sum=0;for(int i=0;i<native.Length;i++){double x=(double)values[i].x-native[i].x,y=(double)values[i].y-native[i].y,z=(double)values[i].z-native[i].z;sum+=x*x+y*y+z*z;}return(float)Math.Sqrt(sum/native.Length);}
            row.worldFloatRmse=Rmse(wf);row.worldDoubleRmse=Rmse(wd);row.relativeFloatRmse=Rmse(rf);row.relativeDoubleRmse=Rmse(rd);row.oldFloatPass=row.worldFloatRmse<.0001f;
            var common=renderer.transform;while(common!=null&&!bones.All(b=>b==common||b.IsChildOf(common)))common=common.parent;if(common==null)throw new InvalidOperationException("Actual renderer and every source bone require a common hierarchy root.");
            var chains=new Dictionary<Transform,double[]>();chains.Add(common,DoubleMatrix(Matrix4x4.identity));
            double[] ToCommon(Transform t){if(chains.TryGetValue(t,out var cached))return cached;var matrix=MultiplyDouble(ToCommon(t.parent),DoubleMatrix(Matrix4x4.TRS(t.localPosition,t.localRotation,t.localScale)));chains.Add(t,matrix);return matrix;}
            var rendererToCommon=ToCommon(renderer.transform);var inverseRenderer=InverseAffineDouble(rendererToCommon);var localBones=bones.Select((bone,i)=>MultiplyDouble(MultiplyDouble(inverseRenderer,ToCommon(bone)),DoubleMatrix(bindposes[i]))).ToArray();var hierarchyLocal=new Vector3[points.Length];var hierarchyWorld=new Vector3[points.Length];
            for(int i=0;i<points.Length;i++){hierarchyLocal[i]=Vector3.Scale(BlendRendererLocal(points[i],weights[i],localBones),renderer.transform.localScale);hierarchyWorld[i]=renderer.transform.TransformPoint(hierarchyLocal[i]);}
            double localError=0;for(int i=0;i<nativeLocal.Length;i++){var delta=hierarchyLocal[i]-nativeLocal[i];localError+=(double)delta.x*delta.x+(double)delta.y*delta.y+(double)delta.z*delta.z;}row.hierarchyLocalRmse=(float)Math.Sqrt(localError/nativeLocal.Length);row.hierarchyWorldRmse=Rmse(hierarchyWorld);row.newDoublePass=row.hierarchyWorldRmse<.0001f;
            var animator=(Animator)Read(motor,"animator");row.sourceClips=string.Join(";",animator.GetCurrentAnimatorClipInfo(0).Select(c=>c.clip.name+"@"+c.weight.ToString("R")));
            // Reuse the original SAT and ray implementations with the actual
            // native skin. No predicted geometry replaces the 25-ray authority.
            var truck=(Component)((Array)Read(match,"trucks")).GetValue(owner);var boxes=truck.GetComponentsInChildren<BoxCollider>(true).Where(b=>b.enabled&&!b.isTrigger&&b.gameObject.activeInHierarchy).ToArray();if(boxes.Length!=2)throw new InvalidOperationException("Both original NWH boxes required.");
            row.boxes=boxes.Length;row.boxWorld=boxes.Select(b=>b.transform.localToWorldMatrix).ToArray();row.boxCenters=boxes.Select(b=>b.center).ToArray();row.boxSizes=boxes.Select(b=>b.size).ToArray();row.bodyMasses=truck.GetComponentsInChildren<Rigidbody>(true).Select(b=>b.mass).ToArray();var triangles=(int[])Read(skin,"triangles");row.triangles=triangles.Length/3;
            int Intersections(Vector3[] vertices){int count=0;foreach(var box in boxes){var local=vertices.Select(p=>box.transform.InverseTransformPoint(p)-box.center).ToArray();for(int t=0;t<triangles.Length;t+=3)if((bool)Call("TriangleBoxIntersects",local[triangles[t]],local[triangles[t+1]],local[triangles[t+2]],box.size*.5f))count++;}return count;}
            row.nativeSatPairs=Intersections(native);row.doubleSatPairs=Intersections(hierarchyWorld);
            var rayType=authority.GetNestedType("RayWitness",Any);var rays=Array.CreateInstance(rayType,25);var rayJson=new List<string>();for(int y=0;y<5;y++)for(int x=0;x<5;x++){var uv=new Vector2(.3f+x*.1f,.3f+y*.1f);var value=Call("TraceActualSkin",camera.ViewportPointToRay(new Vector3(uv.x,uv.y)),uv,owner,skins);rays.SetValue(value,y*5+x);rayJson.Add(JsonUtility.ToJson(value));}row.actual25Rays=rayJson.ToArray();
            var viewRow=Activator.CreateInstance(authority.GetNestedType("View",Any),true);Set(viewRow,"owner",owner);Set(viewRow,"actualBaseClips",row.sourceClips);var virtualType=authority.GetNestedType("VirtualCabPose",Any);var actual=Activator.CreateInstance(virtualType,true);Set(actual,"name","ACTUAL_CAMERA_ROUND_TRIP_CONTROL");Set(actual,"centralWindscreenRays",rays);var virtuals=Array.CreateInstance(virtualType,1);virtuals.SetValue(actual,0);Set(viewRow,"sameFrameVirtualCabCandidates",virtuals);
            try{Call("ScreenConnectedNeckPostures",viewRow,skins,truck,actor,motor);}catch(TargetInvocationException error){if(!(error.InnerException is InvalidOperationException))throw;row.oldPredictionError=error.InnerException.ToString();}
            var old=((IEnumerable)Read(viewRow,"pureConnectedNeckCandidates")).Cast<object>().Single();row.oldPredictionJson=JsonUtility.ToJson(old);row.otherOriginalPredictionGuardsPassed=(bool)Read(old,"nonSelectedWorldMatricesExact")&&(float)Read(old,"headWorldAimError")<=.5f&&(float)Read(old,"mouthWorldAimError")<=.5f&&(float)Read(old,"maximumChangedLocalTranslation")<=.0002f&&(float)Read(old,"maximumUnchangedLocalMatrixError")<=.0002f;
            if(Mathf.Abs((float)Read(old,"zeroOffsetTrueBakeRmse")-row.worldFloatRmse)>.0000001f)throw new InvalidOperationException("Standalone old-float math disagrees with the unchanged original zero-offset control.");
            UnityEditor.AssetDatabase.TryGetGUIDAndLocalFileIdentifier(mesh,out string guid,out long id);row.geometry=new MeshCapture{hierarchyLocal=hierarchyLocal,hierarchyWorld=hierarchyWorld,rendererToCommon=rendererToCommon,rendererLocalScale=renderer.transform.localScale,commonAncestor=PathOf(common),hierarchy=chains.Keys.Select(t=>new LocalPose{path=PathOf(t),parent=t.parent==null?null:PathOf(t.parent),local=Matrix4x4.TRS(t.localPosition,t.localRotation,t.localScale)}).ToArray(),source=UnityEditor.AssetDatabase.GetAssetPath(mesh),guid=guid,localId=id.ToString(),sourceVertices=points,nativeLocal=nativeLocal,nativeWorld=native,worldFloat=wf,worldDouble=wd,relativeFloat=rf,relativeDouble=rd,triangles=triangles,rendererWorld=renderer.transform.localToWorldMatrix,weights=weights.Select(w=>new Weight{a=w.boneIndex0,b=w.boneIndex1,c=w.boneIndex2,d=w.boneIndex3,wa=w.weight0,wb=w.weight1,wc=w.weight2,wd=w.weight3}).ToArray(),bones=bones.Select((b,i)=>new Bone{path=PathOf(b),world=matrices[i],local=Matrix4x4.TRS(b.localPosition,b.localRotation,b.localScale),bindpose=bindposes[i]}).ToArray()};
            if(!row.newDoublePass||!row.otherOriginalPredictionGuardsPassed||row.nativeSatPairs!=0||row.doubleSatPairs!=0)throw new InvalidOperationException("Native same-frame precision/geometry gate failed; retain all algorithms and native evidence.");
        }
        catch(Exception error){row.failure=error.ToString();Failure=row.failure;throw;}
        finally{row.afterState=(string)Call("SnapshotOwnedState");row.actualOwnedStateExact=row.beforeState==row.afterState;Write();if(!row.actualOwnedStateExact)throw new InvalidOperationException("Standalone numeric observation changed actual owned state.");}
    }
    static string PathOf(Transform value)=>value.parent==null?value.name:PathOf(value.parent)+"/"+value.GetSiblingIndex()+":"+value.name;
    static void Evaluate(Vector3 point,BoneWeight weight,Matrix4x4[] bindposes,Matrix4x4[] bones,Vector3 origin,out Vector3 worldFloat,out Vector3 worldDouble,out Vector3 relativeFloat,out Vector3 relativeDouble)
    {
        Vector3 wf=Vector3.zero,rf=Vector3.zero;double dx=0,dy=0,dz=0,rx=0,ry=0,rz=0,total=0;
        Add(weight.boneIndex0,weight.weight0);Add(weight.boneIndex1,weight.weight1);Add(weight.boneIndex2,weight.weight2);Add(weight.boneIndex3,weight.weight3);
        worldFloat=wf;relativeFloat=rf+origin*(float)total;worldDouble=new Vector3((float)dx,(float)dy,(float)dz);relativeDouble=new Vector3((float)(rx+origin.x*total),(float)(ry+origin.y*total),(float)(rz+origin.z*total));
        void Add(int index,float amount)
        {
            if(amount<=0)return;var bind=bindposes[index];var bone=bones[index];var q=bind.MultiplyPoint3x4(point);wf+=bone.MultiplyPoint3x4(q)*amount;var local=bone;local.m03-=origin.x;local.m13-=origin.y;local.m23-=origin.z;rf+=local.MultiplyPoint3x4(q)*amount;
            // Keep original float bind-pose inputs; isolate world accumulation
            // precision without renormalizing weights or inventing source data.
            double x=(double)bone.m00*q.x+(double)bone.m01*q.y+(double)bone.m02*q.z,y=(double)bone.m10*q.x+(double)bone.m11*q.y+(double)bone.m12*q.z,z=(double)bone.m20*q.x+(double)bone.m21*q.y+(double)bone.m22*q.z;
            dx+=(x+bone.m03)*amount;dy+=(y+bone.m13)*amount;dz+=(z+bone.m23)*amount;rx+=(x+((double)bone.m03-origin.x))*amount;ry+=(y+((double)bone.m13-origin.y))*amount;rz+=(z+((double)bone.m23-origin.z))*amount;total+=amount;
        }
    }
    static double[] DoubleMatrix(Matrix4x4 matrix){var values=new double[16];for(int r=0;r<4;r++)for(int c=0;c<4;c++)values[r*4+c]=matrix[r,c];return values;}
    static double[] MultiplyDouble(double[] a,double[] b){var result=new double[16];for(int r=0;r<4;r++)for(int c=0;c<4;c++)for(int k=0;k<4;k++)result[r*4+c]+=a[r*4+k]*b[k*4+c];return result;}
    static double[] InverseAffineDouble(double[] m)
    {
        double det=m[0]*(m[5]*m[10]-m[6]*m[9])-m[1]*(m[4]*m[10]-m[6]*m[8])+m[2]*(m[4]*m[9]-m[5]*m[8]);if(double.IsNaN(det)||double.IsInfinity(det)||Math.Abs(det)<1e-12)throw new InvalidOperationException("Actual renderer-local matrix is singular.");
        var n=new double[16];n[0]=(m[5]*m[10]-m[6]*m[9])/det;n[1]=(m[2]*m[9]-m[1]*m[10])/det;n[2]=(m[1]*m[6]-m[2]*m[5])/det;n[4]=(m[6]*m[8]-m[4]*m[10])/det;n[5]=(m[0]*m[10]-m[2]*m[8])/det;n[6]=(m[2]*m[4]-m[0]*m[6])/det;n[8]=(m[4]*m[9]-m[5]*m[8])/det;n[9]=(m[1]*m[8]-m[0]*m[9])/det;n[10]=(m[0]*m[5]-m[1]*m[4])/det;n[15]=1;for(int r=0;r<3;r++)n[r*4+3]=-(n[r*4]*m[3]+n[r*4+1]*m[7]+n[r*4+2]*m[11]);return n;
    }
    static Vector3 BlendRendererLocal(Vector3 source,BoneWeight weight,double[][] matrices)
    {
        double x=0,y=0,z=0;Add(weight.boneIndex0,weight.weight0);Add(weight.boneIndex1,weight.weight1);Add(weight.boneIndex2,weight.weight2);Add(weight.boneIndex3,weight.weight3);return new Vector3((float)x,(float)y,(float)z);
        void Add(int index,float w){if(w<=0)return;var m=matrices[index];x+=(m[0]*source.x+m[1]*source.y+m[2]*source.z+m[3])*w;y+=(m[4]*source.x+m[5]*source.y+m[6]*source.z+m[7])*w;z+=(m[8]*source.x+m[9]*source.y+m[10]*source.z+m[11])*w;}
    }
    public static void RequireHierarchyControls()
    {
        // Non-unit source weights remain unchanged in renderer space. The
        // renderer's outer world translation is applied once, as TransformPoint
        // does for actual BakeMesh output; it is not multiplied by weight sum.
        var a=Matrix4x4.Translate(new Vector3(2,3,4));var b=Matrix4x4.Translate(new Vector3(-1,2,0));var weights=new BoneWeight{boneIndex0=0,boneIndex1=1,weight0=.5f,weight1=.25f};var point=BlendRendererLocal(new Vector3(.125f,.25f,.5f),weights,new[]{DoubleMatrix(a),DoubleMatrix(b)});if(point!=new Vector3(.84375f,2.1875f,2.375f))throw new InvalidOperationException("Renderer-local non-unit source weighting changed.");
        var world=Matrix4x4.identity;world.m00=0;world.m02=2;world.m11=2;world.m20=-2;world.m22=0;world.m03=80000;world.m13=50000;world.m23=90000;if(world.MultiplyPoint3x4(point)!=new Vector3(80004.75f,50004.375f,89998.3125f))throw new InvalidOperationException("Known outer renderer transform must apply once.");
        var identity=MultiplyDouble(InverseAffineDouble(DoubleMatrix(world)),DoubleMatrix(world));for(int r=0;r<4;r++)for(int c=0;c<4;c++)if(Math.Abs(identity[r*4+c]-(r==c?1:0))>1e-9)throw new InvalidOperationException("Affine hierarchy inverse failed its independent round trip.");
    }
    public static void RequireAnalyticControls()
    {
        // Deliberately include a non-unit weight sum: moving to a common
        // origin must not silently normalize source weights or translations.
        foreach(float lastWeight in new[]{.125f,.1f})
        {
            var origin=new Vector3(80000,-50000,90000);var basis=Matrix4x4.TRS(origin,Quaternion.Euler(23,57,-13),new Vector3(2,3,4));var point=new Vector3(.125f,.25f,.5f);var matrices=new[]{basis,basis,basis,basis};var offsets=new[]{Vector3.zero,new Vector3(2,-3,4),new Vector3(-5,7,9),new Vector3(13,-17,19)};
            for(int i=0;i<4;i++){var m=matrices[i];m.m03+=offsets[i].x;m.m13+=offsets[i].y;m.m23+=offsets[i].z;matrices[i]=m;}
            var w=new BoneWeight{boneIndex0=0,boneIndex1=1,boneIndex2=2,boneIndex3=3,weight0=.5f,weight1=.25f,weight2=.125f,weight3=lastWeight};var weights=new[]{.5,.25,.125,(double)lastWeight};double total=weights.Sum();
            var expected=new Vector3();for(int axis=0;axis<3;axis++){double linear=(double)basis[axis,0]*point.x+(double)basis[axis,1]*point.y+(double)basis[axis,2]*point.z;double value=(linear+origin[axis])*total;for(int i=0;i<4;i++)value+=offsets[i][axis]*weights[i];expected[axis]=(float)value;}
            Evaluate(point,w,Enumerable.Repeat(Matrix4x4.identity,4).ToArray(),matrices,origin,out _,out var world,out _,out var relative);
            if(!world.Equals(expected)||!relative.Equals(expected))throw new InvalidOperationException("Known far-world rotated/nonunit source-weight accumulation changed mathematical semantics.");
        }
    }
    void Write()=>File.WriteAllText(Path.Combine(directory,"actual-far-world-native-skin-precision.json"),JsonUtility.ToJson(new Receipt{failure=Failure,frames=frames.Count,oldFloatFailures=frames.Count(f=>!f.oldFloatPass),samples=frames.ToArray()},true));
    public void RequirePositiveEvidence()
    {
        Write();if(Failure!=null||frames.Count<16||frames.Select(f=>f.species).Distinct().Count()!=2||frames.Any(f=>!f.actualOwnedStateExact||!f.newDoublePass)||!frames.Any(f=>!f.oldFloatPass))throw new InvalidOperationException("Require both actual species,16 far-world real frames, retained old-float negative and unchanged .1mm new/native geometry/source gates. Inspect evidence; do not manufacture a precision success.");
    }
    public void Dispose(){RenderPipelineManager.beginCameraRendering-=Capture;Write();}
}
#endif

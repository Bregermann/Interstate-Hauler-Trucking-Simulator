#if UNITY_EDITOR && VISTA
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace LWS.TruckDragonKingdom.Editor
{
    public static partial class DragonKingdomRegionalWorldIntegration
    {
        const string OriginalRoofSdkRole="ACTUAL_SDK_RAW_BEFORE_NATIVE_COORDINATOR";
        [Serializable]public sealed class OriginalRoofBoundaryWitness
        {public Vector3 a,b;public float beforeStep,afterStep,beforeAngle,afterAngle;public bool newSteep65,worsenedSteep65;}
        [Serializable]public sealed class OriginalRoofCopyEvidence
        {
            public string scope="EXACT_ORIGINAL_VISTA_ROOF_CELL_COPY_ONLY_NOT_SMOOTHING_NATIVE_COLLIDER_DIGGER_OR_FULL_WORLD_ACCEPTANCE";
            public string observedInputRole,recordedActualSdkRawHeightSha256,observedInputHeightSha256,foundationHeightSha256,protectedCompositionHeightSha256,beforeOutsideSha256,afterOutsideSha256;
            public Bounds originalRoof;public Vector3 origin,size;public RectInt changedRectangle;
            public int roofSamples,changedRoofSamples,outsideSamples,boundaryEdges,beforeBoundarySteep65,afterBoundarySteep65,newBoundarySteep65,worsenedBoundarySteep65;
            public float maximumBeforeBoundaryStep,maximumAfterBoundaryStep;
            public bool originalInputArraysExact,outsideMaskBitExact,originalRoofBitExact;
            public OriginalRoofBoundaryWitness[] boundaryWitnesses;
        }
        // LWS source-protection semantics only. Vendor SDK relief remains a
        // separately recorded input. Never use a custom smoothing kernel here.
        // Full-world callers must separately prove source assets, graph/poses,
        // road contacts, native SetHeights readback, holes/layers and Digger gates.
        // The original copy/quality implementation below is unchanged. Only an
        // explicit independently native-qualified collider authority can enter
        // this production profile; final current-world Digger/road gates remain.
        static OriginalRoofCopyEvidence CopyQualifiedOriginalUndergroundRoof(DragonKingdomWorld world,QualifiedCavernRoadAuthority authority,string profile,float[,] observed,float[,] foundation,Vector3 origin,Vector3 size,Bounds declaredRoof,string recordedActualSdkSha256,string expectedObservedSha256,string expectedFoundationSha256,string observedRole,out float[,] protectedComposition)
        {
            if(profile!=CavernRoadAuthorityProfile||authority==null)throw new InvalidOperationException("Explicit CavernRoadColliderRoof01 typed native authority required.");authority.ValidateWorld(world,declaredRoof);return CopyOriginalUndergroundRoof(observed,foundation,origin,size,declaredRoof,recordedActualSdkSha256,expectedObservedSha256,expectedFoundationSha256,observedRole,out protectedComposition);
        }
        static OriginalRoofCopyEvidence CopyOriginalUndergroundRoof(float[,] observed,float[,] foundation,Vector3 origin,Vector3 size,Bounds declaredRoof,string recordedActualSdkSha256,string expectedObservedSha256,string expectedFoundationSha256,string observedRole,out float[,] protectedComposition)
        {
            void Require(bool condition,string detail){if(!condition)throw new InvalidOperationException("Original Vista roof source protection rejected: "+detail);}
            bool Digest(string value)=>value!=null&&value.Length==64&&value.All(c=>(c>='0'&&c<='9')||(c>='a'&&c<='f'));
            bool Same(float a,float b)=>BitConverter.SingleToInt32Bits(a)==BitConverter.SingleToInt32Bits(b);
            Require(observed!=null&&foundation!=null&&observed.GetLength(0)==513&&observed.GetLength(1)==513&&foundation.GetLength(0)==513&&foundation.GetLength(1)==513,"actual 513 vertex grids required");
            Require(size.Equals(new Vector3(1600,600,1600))&&origin.y==-20&&(origin.x==-1600||origin.x==0)&&(origin.z==-800||origin.z==800),"exact original four-tile coordinate frame required");
            var roof=DragonKingdomWorldAuthoring.CavernUndergroundBounds();Require(declaredRoof.Equals(roof),"declared original underground roof may not be shifted or enlarged");
            Require(Digest(recordedActualSdkSha256)&&Digest(expectedObservedSha256)&&Digest(expectedFoundationSha256)&&(observedRole==OriginalRoofSdkRole||observedRole==RetainedThermalSourceRole||observedRole==CurrentFinalTerrainInputRole),"explicit actual SDK versus retained-final stage provenance required");
            string inputHash=FinishFloatHash(observed),foundationHash=FinishFloatHash(foundation);Require(foundationHash==expectedFoundationSha256,"original foundation differs from its independently qualified source hash");Require(inputHash==expectedObservedSha256,"observed input differs from its independently qualified stage hash");Require(observedRole!=OriginalRoofSdkRole||recordedActualSdkSha256==inputHash,"raw SDK role cannot relabel a later composition");
            foreach(float value in observed)Require(float.IsFinite(value)&&value>=0&&value<=1,"finite observed normalized heights required");foreach(float value in foundation)Require(float.IsFinite(value)&&value>=0&&value<=1,"finite original normalized heights required");
            var proof=new OriginalRoofCopyEvidence{observedInputRole=observedRole,recordedActualSdkRawHeightSha256=recordedActualSdkSha256,observedInputHeightSha256=inputHash,foundationHeightSha256=foundationHash,originalRoof=roof,origin=origin,size=size};protectedComposition=(float[,])observed.Clone();var mask=new bool[513,513];int minX=513,minZ=513,maxX=-1,maxZ=-1;
            for(int z=0;z<513;z++)for(int x=0;x<513;x++)
            {
                var point=origin+new Vector3(x*size.x/512,0,z*size.z/512);bool inside=InsideXZ(roof,point);mask[z,x]=inside;
                if(!inside){proof.outsideSamples++;continue;}proof.roofSamples++;protectedComposition[z,x]=foundation[z,x];if(!Same(observed[z,x],foundation[z,x])){proof.changedRoofSamples++;minX=Math.Min(minX,x);maxX=Math.Max(maxX,x);minZ=Math.Min(minZ,z);maxZ=Math.Max(maxZ,z);}
            }
            proof.changedRectangle=maxX<0?new RectInt(0,0,0,0):new RectInt(minX,minZ,maxX-minX+1,maxZ-minZ+1);
            string OutsideHash(float[,] values){using(var stream=new MemoryStream()){using(var writer=new BinaryWriter(stream)){for(int z=0;z<513;z++)for(int x=0;x<513;x++)if(!mask[z,x])writer.Write(values[z,x]);writer.Flush();return FinishDigest(stream.ToArray());}}}
            proof.beforeOutsideSha256=OutsideHash(observed);proof.afterOutsideSha256=OutsideHash(protectedComposition);proof.outsideMaskBitExact=proof.beforeOutsideSha256==proof.afterOutsideSha256;proof.originalRoofBitExact=true;
            for(int z=0;z<513;z++)for(int x=0;x<513;x++)if(mask[z,x]&&!Same(protectedComposition[z,x],foundation[z,x]))proof.originalRoofBitExact=false;
            // Record actual boundary risk; an exact source copy is not permission
            // to conceal a new portal cliff or weaken any original quality gate.
            var output=protectedComposition;var witnesses=new List<OriginalRoofBoundaryWitness>();
            void Edge(int x,int z,int nx,int nz,float spacing)
            {
                if(mask[z,x]==mask[nz,nx])return;proof.boundaryEdges++;float before=Mathf.Abs(observed[z,x]-observed[nz,nx])*size.y,after=Mathf.Abs(output[z,x]-output[nz,nx])*size.y;float beforeAngle=Mathf.Atan2(before,spacing)*Mathf.Rad2Deg,afterAngle=Mathf.Atan2(after,spacing)*Mathf.Rad2Deg;bool fresh=beforeAngle<=65&&afterAngle>65,worse=afterAngle>65&&after>before;
                if(beforeAngle>65)proof.beforeBoundarySteep65++;if(afterAngle>65)proof.afterBoundarySteep65++;if(fresh)proof.newBoundarySteep65++;if(worse)proof.worsenedBoundarySteep65++;proof.maximumBeforeBoundaryStep=Mathf.Max(proof.maximumBeforeBoundaryStep,before);proof.maximumAfterBoundaryStep=Mathf.Max(proof.maximumAfterBoundaryStep,after);
                if(fresh||worse)witnesses.Add(new OriginalRoofBoundaryWitness{a=origin+new Vector3(x*size.x/512,observed[z,x]*size.y,z*size.z/512),b=origin+new Vector3(nx*size.x/512,observed[nz,nx]*size.y,nz*size.z/512),beforeStep=before,afterStep=after,beforeAngle=beforeAngle,afterAngle=afterAngle,newSteep65=fresh,worsenedSteep65=worse});
            }
            for(int z=0;z<513;z++)for(int x=0;x<513;x++){if(x<512)Edge(x,z,x+1,z,size.x/512);if(z<512)Edge(x,z,x,z+1,size.z/512);}
            proof.boundaryWitnesses=witnesses.OrderByDescending(w=>w.afterStep-w.beforeStep).Take(32).ToArray();proof.protectedCompositionHeightSha256=FinishFloatHash(output);proof.originalInputArraysExact=inputHash==FinishFloatHash(observed)&&foundationHash==FinishFloatHash(foundation);Require(proof.outsideMaskBitExact&&proof.originalRoofBitExact&&proof.originalInputArraysExact,"input, outside mask or original source roof changed unexpectedly");return proof;
        }
    }
}
#endif

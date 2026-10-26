#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class KickballSnowBankGeometryTests
{
    static Type Author=>Type.GetType("LWS.TruckKickball.Editor.KickballShoveledSnowAuthoring, Assembly-CSharp-Editor",true);
    static Vector3[] Bases=>(Vector3[])Type.GetType("LWS.TruckKickball.KickballMatch, Assembly-CSharp",true).GetField("Bases").GetValue(null);
    static object Call(string name,params object[] args)=>Author.GetMethods(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static).Single(method=>method.Name==name&&method.GetParameters().Length==args.Length).Invoke(null,args);
    static Mesh Disc(int index,bool bank,bool legacy=false)=>(Mesh)Call(legacy?"LegacyDisc":"Disc",Bases[index],bank?6.2f:5.8f,bank,index);
    static Mesh Path(int index,int sign,bool bank,bool legacy=false)=>(Mesh)Call(legacy?"LegacyRibbon":"Ribbon",Bases[index],Bases[(index+1)%4],bank?sign*2.75f:2.2f,bank,bank?index+sign*11:index);
    static Mesh Prior(int index,int side)=>(Mesh)(side==0?Call("MoundDiscV1",Bases[index],6.2f,index):Call("MoundRibbonV1",Bases[index],Bases[(index+1)%4],side,index));
    static float Cross(Vector3 a,Vector3 b,Vector3 point)=>(b.x-a.x)*(point.z-a.z)-(b.z-a.z)*(point.x-a.x);
    static bool Inside(Vector3 point,Mesh mesh)
    {
        var bounds=mesh.bounds;if(point.x<bounds.min.x||point.x>bounds.max.x||point.z<bounds.min.z||point.z>bounds.max.z)return false;var vertices=mesh.vertices;var triangles=mesh.triangles;
        for(int i=0;i<triangles.Length;i+=3){float a=Cross(vertices[triangles[i]],vertices[triangles[i+1]],point),b=Cross(vertices[triangles[i+1]],vertices[triangles[i+2]],point),c=Cross(vertices[triangles[i+2]],vertices[triangles[i]],point);if((a>=0&&b>=0&&c>=0)||(a<=0&&b<=0&&c<=0))return true;}return false;
    }
    public static int Components(Mesh mesh)
    {
        var parents=Enumerable.Range(0,mesh.vertexCount).ToArray();int Find(int at){while(parents[at]!=at){parents[at]=parents[parents[at]];at=parents[at];}return at;}var triangles=mesh.triangles;for(int i=0;i<triangles.Length;i+=3){parents[Find(triangles[i+1])]=Find(triangles[i]);parents[Find(triangles[i+2])]=Find(triangles[i]);}return triangles.Select(Find).Distinct().Count();
    }
    static void WideProfile(Mesh mesh,int index,int side)
    {
        var from=Bases[index];var forward=(Bases[(index+1)%4]-from).normalized;var lateral=Vector3.Cross(Vector3.up,forward);var groups=mesh.vertices.GroupBy(point=>side==0?Mathf.RoundToInt(Mathf.Atan2(point.z-from.z,point.x-from.x)*1000):Mathf.RoundToInt(Vector3.Dot(point-from,forward)*1000)).Where(group=>group.Count()==9).ToArray();Assert.That(groups.Length,Is.GreaterThanOrEqualTo(4),"The soft cross profile must have nine rows, rather than a narrow triangular pipe.");
        var widths=groups.Select(group=>{var span=group.Select(point=>side==0?new Vector2(point.x-from.x,point.z-from.z).magnitude:Mathf.Abs(Vector3.Dot(point-from,lateral))).ToArray();return span.Max()-span.Min();}).ToArray();Assert.That(widths.Max(),Is.InRange(1.499f,2.001f),"Body widths remain broad.");Assert.That(widths.Min(),Is.LessThan(widths.Max()*.8f),"Heap tips taper in plan as well as height; no full-width white tile ends.");Assert.That(widths.Count(width=>width>=1.499f),Is.GreaterThanOrEqualTo(3));Assert.That(widths.All(width=>width>0&&width<=2.001f),Is.True);
        if(side!=0){var prior=Prior(index,side);try{Assert.That(Components(mesh),Is.LessThan(Components(prior)),"Fewer irregular heaps replace regular V1 tile breaks.");}finally{UnityEngine.Object.DestroyImmediate(prior);}}
    }
    [Test]public void NewBanksAreBroadLowBrokenAndOutsideTheActualUnchangedClearedMeshes()
    {
        var clear=new List<Mesh>();var banks=new List<Mesh>();var random=JsonUtility.ToJson(UnityEngine.Random.state);
        try
        {
            for(int i=0;i<4;i++){clear.Add(Disc(i,false));clear.Add(Path(i,1,false));}
            for(int i=0;i<4;i++)for(int kind=0;kind<3;kind++)
            {
                int side=kind==0?0:kind==1?-1:1;var mesh=side==0?Disc(i,true):Path(i,side,true);banks.Add(mesh);Assert.That(mesh.vertexCount,Is.GreaterThan(100));Assert.That(mesh.bounds.min.y,Is.GreaterThanOrEqualTo(.0239f));Assert.That(mesh.bounds.max.y,Is.LessThanOrEqualTo(.2401f));Assert.That(mesh.bounds.max.y,Is.GreaterThan(.075f));Assert.That(Components(mesh),Is.GreaterThanOrEqualTo(3),"Real gaps must separate irregular snow lobes.");WideProfile(mesh,i,side);var vertices=mesh.vertices;foreach(var point in vertices)foreach(var path in clear)Assert.That(Inside(point,path),Is.False,"A snow bank may not cover a maintained base or route.");var indices=mesh.triangles;for(int triangle=0;triangle<indices.Length;triangle+=3){var center=(vertices[indices[triangle]]+vertices[indices[triangle+1]]+vertices[indices[triangle+2]])/3;foreach(var path in clear)Assert.That(Inside(center,path),Is.False,"A connecting face may not bridge over a maintained path.");}
            }
            Assert.That(JsonUtility.ToJson(UnityEngine.Random.state),Is.EqualTo(random),"Mesh authoring must not consume the match/weather random state.");
        }
        finally{foreach(var mesh in clear.Concat(banks))UnityEngine.Object.DestroyImmediate(mesh);}
    }
    [Test]public void ExactMigrationGuardRejectsSingleBitVertexTriangleNormalAndExtraChannelEdits()
    {
        var legacy=Disc(0,true,true);var clone=UnityEngine.Object.Instantiate(legacy);
        try
        {
            Assert.That(Call("MatchesOwnedGeometry",clone,legacy),Is.True);var vertices=clone.vertices;vertices[0].x=BitConverter.Int32BitsToSingle(BitConverter.SingleToInt32Bits(vertices[0].x)+1);clone.vertices=vertices;Assert.That(Call("MatchesOwnedGeometry",clone,legacy),Is.False,"Even a one-bit custom vertex edit is preserved/refused.");clone.vertices=legacy.vertices;
            var triangles=clone.triangles;int swap=triangles[0];triangles[0]=triangles[1];triangles[1]=swap;clone.triangles=triangles;Assert.That(Call("MatchesOwnedGeometry",clone,legacy),Is.False);clone.triangles=legacy.triangles;
            var normals=clone.normals;normals[0]=Vector3.up;clone.normals=normals;Assert.That(Call("MatchesOwnedGeometry",clone,legacy),Is.False);clone.normals=legacy.normals;clone.colors=Enumerable.Repeat(Color.white,clone.vertexCount).ToArray();Assert.That(Call("MatchesOwnedGeometry",clone,legacy),Is.False);
        }
        finally{UnityEngine.Object.DestroyImmediate(clone);UnityEngine.Object.DestroyImmediate(legacy);}
    }
    [Test]public void V1AndV2SignaturesRefuseEvenSingleBitCustomChanges()
    {
        foreach(bool old in new[]{true,false})
        {
            var expected=old?Prior(0,1):Path(0,1,true);var changed=UnityEngine.Object.Instantiate(expected);
            try{Assert.That(Call("MatchesOwnedGeometry",changed,expected),Is.True);var points=changed.vertices;points[0].z=BitConverter.Int32BitsToSingle(BitConverter.SingleToInt32Bits(points[0].z)+1);changed.vertices=points;Assert.That(Call("MatchesOwnedGeometry",changed,expected),Is.False,"Unknown/custom "+(old?"V1":"V2")+" geometry must be preserved/refused.");}
            finally{UnityEngine.Object.DestroyImmediate(changed);UnityEngine.Object.DestroyImmediate(expected);}
        }
    }
    [Test]public void PackedBankDetailReusesInstalledNonTessellatedSnowAndRefusesCustomMaterialChanges()
    {
        var expected=(Material)Call("PackedBankMaterial");var copy=UnityEngine.Object.Instantiate(expected);copy.name=expected.name;
        try
        {
            Assert.That(expected.shader.name,Is.EqualTo("NOT_Lonely/Weatherade/Snow Coverage"));Assert.That(expected.GetTexture("_CoverageTex0"),Is.Not.Null);Assert.That(expected.GetTexture("_CoverageDetailTex"),Is.Not.Null);foreach(string property in new[]{"_Coverage","_CoverageAmount","_UseCoverageDetail"}){Assert.That(expected.GetFloat(property),Is.EqualTo(1));Assert.That(expected.GetFloat(property+"Override"),Is.EqualTo(1));}foreach(string property in new[]{"_Displacement","_Tessellation","_Sparkle","_Sss","_Traces"}){Assert.That(expected.GetFloat(property),Is.Zero);Assert.That(expected.GetFloat(property+"Override"),Is.EqualTo(1));}Assert.That(Call("MatchesPackedBankMaterial",copy,expected),Is.True);copy.SetFloat("_Smoothness",.123f);Assert.That(Call("MatchesPackedBankMaterial",copy,expected),Is.False,"Designer material changes are refused rather than silently overwritten.");
        }
        finally{UnityEngine.Object.DestroyImmediate(copy);UnityEngine.Object.DestroyImmediate(expected);}
    }
    [Test]public void SavedOwnedAssetsMatchOnlyTheExactLegacyV1OrV2Output()
    {
        for(int i=0;i<4;i++)for(int kind=0;kind<3;kind++)
        {
            int side=kind==0?0:kind==1?-1:1;string name=side==0?"Base "+i+" irregular shoveled rim":"Broken snow edge "+i+" "+side;string path="Assets/TruckKickball/SnowVisibility/"+name+".asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);var old=side==0?Disc(i,true,true):Path(i,side,true,true);var prior=Prior(i,side);var refined=side==0?Disc(i,true):Path(i,side,true);
            try{Assert.That(saved,Is.Not.Null);Assert.That((bool)Call("MatchesOwnedGeometry",saved,old)||(bool)Call("MatchesOwnedGeometry",saved,prior)||(bool)Call("MatchesOwnedGeometry",saved,refined),Is.True,"Designer changes must cause refinement to refuse: "+path);Assert.That(AssetDatabase.AssetPathToGUID(path),Has.Length.EqualTo(32));var repeat=side==0?Disc(i,true):Path(i,side,true);try{Assert.That(Call("MatchesOwnedGeometry",repeat,refined),Is.True,"V2 is deterministic and recognizes already-refined assets.");}finally{UnityEngine.Object.DestroyImmediate(repeat);}}
            finally{UnityEngine.Object.DestroyImmediate(old);UnityEngine.Object.DestroyImmediate(prior);UnityEngine.Object.DestroyImmediate(refined);}
        }
    }
}
#endif

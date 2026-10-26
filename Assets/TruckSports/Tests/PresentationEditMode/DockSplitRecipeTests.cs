#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
public sealed class DockSplitRecipeTests
{
    const BindingFlags Any=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
    static object Get(object owner,string name)=>owner.GetType().GetField(name,Any) is FieldInfo f?f.GetValue(owner):owner.GetType().GetProperty(name,Any).GetValue(owner);
    static Type Author=>Type.GetType("LWS.TruckSports.Editor.SportsTruckAppearanceHostAuthoring, Assembly-CSharp-Editor",true);
    static Type Attributes=>Type.GetType("LWS.TruckDockDiving.Editor.DockSplitSurfaceAttributes, Assembly-CSharp-Editor",true);
    static Mesh Slice(Mesh mesh,Vector3 normal,Vector3 point)=> (Mesh)Author.GetMethod("SliceDockSurface").Invoke(null,new object[]{mesh,0,Matrix4x4.identity,point,normal});
    static double Area(Mesh mesh,int sub,Matrix4x4 matrix){var v=mesh.vertices;var t=mesh.GetTriangles(sub);double result=0;for(int i=0;i<t.Length;i+=3)result+=Vector3.Cross(matrix.MultiplyVector(v[t[i+1]]-v[t[i]]),matrix.MultiplyVector(v[t[i+2]]-v[t[i]])).magnitude*.5;return result;}
    static Mesh Cube()
    {
        var go=GameObject.CreatePrimitive(PrimitiveType.Cube);var mesh=UnityEngine.Object.Instantiate(go.GetComponent<MeshFilter>().sharedMesh);UnityEngine.Object.DestroyImmediate(go);
        mesh.SetUVs(1,mesh.vertices.Select(v=>new Vector4(v.x+2,v.y+3,v.z+4,.75f)).ToList());mesh.colors=mesh.vertices.Select(v=>new Color(v.x+.5f,v.y+.5f,v.z+.5f,1)).ToArray();return mesh;
    }
    [Test]public void VendorHorizontalCutReconstructsActualExtraUvAndColorsWithIndependentCapChannels()
    {
        var source=Cube();Mesh upper=null,lower=null;var vertices=source.vertices;var uv=source.uv;var extra=new List<Vector4>();source.GetUVs(1,extra);var colors=source.colors;
        try
        {
            upper=Slice(source,Vector3.up,Vector3.zero);lower=Slice(source,Vector3.down,Vector3.zero);Assert.That(upper,Is.Not.Null);Assert.That(lower,Is.Not.Null);
            Assert.That(Area(upper,0,Matrix4x4.identity)+Area(lower,0,Matrix4x4.identity),Is.EqualTo(6).Within(1e-5));
            foreach(var mesh in new[]{upper,lower})
            {
                Assert.That(mesh.subMeshCount,Is.EqualTo(2));Assert.That(Area(mesh,1,Matrix4x4.identity),Is.EqualTo(1).Within(1e-5));var values=new List<Vector4>();mesh.GetUVs(1,values);Assert.That(values.Count,Is.EqualTo(mesh.vertexCount));Assert.That(mesh.GetVertexAttributeDimension(VertexAttribute.TexCoord1),Is.EqualTo(4));
                foreach(int index in mesh.GetTriangles(0).Distinct()){var p=mesh.vertices[index];Assert.That(Vector4.Distance(values[index],new Vector4(p.x+2,p.y+3,p.z+4,.75f)),Is.LessThan(1e-5));Assert.That(Vector4.Distance((Vector4)mesh.colors[index],new Vector4(p.x+.5f,p.y+.5f,p.z+.5f,1)),Is.LessThan(1e-5));}
                foreach(int index in mesh.GetTriangles(1).Distinct()){var p=mesh.vertices[index];Assert.That(Mathf.Abs(p.y),Is.LessThan(1e-5));Assert.That(values[index],Is.EqualTo(new Vector4(p.x,p.z,0,0)));Assert.That(mesh.colors[index],Is.EqualTo(Color.white));}
                Assert.That(mesh.uv.Length,Is.EqualTo(mesh.vertexCount));Assert.That(mesh.normals.Length,Is.EqualTo(mesh.vertexCount));
            }
            CollectionAssert.AreEqual(vertices,source.vertices);CollectionAssert.AreEqual(uv,source.uv);CollectionAssert.AreEqual(colors,source.colors);var after=new List<Vector4>();source.GetUVs(1,after);CollectionAssert.AreEqual(extra,after);
        }
        finally{if(upper!=null)UnityEngine.Object.DestroyImmediate(upper);if(lower!=null)UnityEngine.Object.DestroyImmediate(lower);UnityEngine.Object.DestroyImmediate(source);}
    }
    [Test]public void UncutSurfaceRetainsEveryOriginalAttributeExactly()
    {
        var source=Cube();Mesh result=null;
        try{result=Slice(source,Vector3.up,new Vector3(0,-2,0));var a=new List<Vector4>();var b=new List<Vector4>();source.GetUVs(1,a);result.GetUVs(1,b);var old=source.triangles;var next=result.GetTriangles(0);Assert.That(next.Length,Is.EqualTo(old.Length));for(int i=0;i<old.Length;i++){Assert.That(result.vertices[next[i]],Is.EqualTo(source.vertices[old[i]]));Assert.That(result.normals[next[i]],Is.EqualTo(source.normals[old[i]]));Assert.That(result.tangents[next[i]],Is.EqualTo(source.tangents[old[i]]));Assert.That(result.uv[next[i]],Is.EqualTo(source.uv[old[i]]));Assert.That(result.colors[next[i]],Is.EqualTo(source.colors[old[i]]));Assert.That(b[next[i]],Is.EqualTo(a[old[i]]));}}
        finally{if(result!=null)UnityEngine.Object.DestroyImmediate(result);UnityEngine.Object.DestroyImmediate(source);}
    }
    [Test]public void AmbiguousOverlappingUv0NormalSurfacesWithConflictingExtraDataAreRefused()
    {
        var source=new Mesh();var output=new Mesh();
        try
        {
            var points=new[]{Vector3.zero,Vector3.right,Vector3.up};source.vertices=points.Concat(points).ToArray();source.normals=Enumerable.Repeat(Vector3.forward,6).ToArray();source.uv=new[]{Vector2.zero,Vector2.right,Vector2.up,Vector2.zero,Vector2.right,Vector2.up};source.SetUVs(1,new List<Vector2>{Vector2.zero,Vector2.zero,Vector2.zero,Vector2.one,Vector2.one,Vector2.one});source.triangles=new[]{0,1,2,3,4,5};
            output.vertices=points;output.normals=Enumerable.Repeat(Vector3.forward,3).ToArray();output.uv=new[]{Vector2.zero,Vector2.right,Vector2.up};output.triangles=new[]{0,1,2};
            var error=Assert.Throws<TargetInvocationException>(()=>Attributes.GetMethod("Restore").Invoke(null,new object[]{output,source,0,Matrix4x4.identity}));Assert.That(error.InnerException,Is.TypeOf<InvalidOperationException>());Assert.That(error.InnerException.Message,Does.Contain("Ambiguous"));
        }
        finally{UnityEngine.Object.DestroyImmediate(source);UnityEngine.Object.DestroyImmediate(output);}
    }
    [TestCase("semi.standard")][TestCase("truck.monster")][TestCase("semi.lowrider")][TestCase("truck.lifted")][TestCase("truck.fire")][TestCase("novelty.hotdog")][TestCase("novelty.icecream")]
    public void ActualPrivateYRecipesPreserveSourceAreasMaterialsPlaneCapsAndSelectedIcecreamLod0Uv1(string bodyId)
    {
        var catalog=AssetDatabase.LoadAssetAtPath<ScriptableObject>("Assets/TruckSports/Resources/SportsTruckAppearanceCatalog.asset");var body=catalog.GetType().GetMethod("FindBody").Invoke(catalog,new object[]{bodyId});var prefab=(GameObject)Get(body,"visualPrefab");
        var recipe=AssetDatabase.LoadAssetAtPath<ScriptableObject>("Assets/TruckDockDiving/Customization/PrivateLicensedSplitBodies/"+bodyId+".asset");Assert.That(recipe,Is.Not.Null,"Run explicit Dock plane/private recipe author first.");Assert.That(recipe.GetType().GetMethod("ValidFor").Invoke(recipe,new[]{body}),Is.EqualTo(true));Assert.That(Get(recipe,"sourceDependencyHash"),Is.EqualTo(AssetDatabase.GetAssetDependencyHash(AssetDatabase.GetAssetPath(prefab)).ToString()));
        var parts=((IEnumerable)Get(recipe,"parts")).Cast<object>().ToArray();var transform=(Matrix4x4)Get(recipe,"canonicalToVehicle");float plane=(float)Get(recipe,"vehiclePlaneY");var renderers=prefab.GetComponentsInChildren<Renderer>(true);bool icecreamUv1=false;int icecreamExactSourceVertices=0;double topCap=0,bottomCap=0;
        foreach(var group in parts.GroupBy(part=>((int)Get(part,"rendererIndex"),(int)Get(part,"materialIndex"))))
        {
            var renderer=renderers[group.Key.Item1];var source=renderer.GetComponent<MeshFilter>().sharedMesh;var toBody=prefab.transform.worldToLocalMatrix*renderer.localToWorldMatrix;double area=0;
            foreach(var part in group)
            {
                var mesh=(Mesh)Get(part,"mesh");Assert.That(AssetDatabase.Contains(mesh),Is.True);Assert.That(AssetDatabase.GetAssetPath(mesh),Is.EqualTo(AssetDatabase.GetAssetPath(recipe)));Assert.That(Get(part,"sourceMesh"),Is.SameAs(source));area+=Area(mesh,0,toBody);
                Assert.That(mesh.uv.Length,Is.EqualTo(source.uv.Length==source.vertexCount?mesh.vertexCount:0));
                for(int channel=1;channel<8;channel++)if(source.HasVertexAttribute((VertexAttribute)((int)VertexAttribute.TexCoord0+channel))){var values=new List<Vector4>();mesh.GetUVs(channel,values);Assert.That(values.Count,Is.EqualTo(mesh.vertexCount));Assert.That(mesh.GetVertexAttributeDimension((VertexAttribute)((int)VertexAttribute.TexCoord0+channel)),Is.EqualTo(source.GetVertexAttributeDimension((VertexAttribute)((int)VertexAttribute.TexCoord0+channel))));}
                if(bodyId=="novelty.icecream"&&source.HasVertexAttribute(VertexAttribute.TexCoord1)){Assert.That(renderer.name+"/"+source.name,Does.Contain("LOD0"));icecreamUv1=true;var originalUv1=new List<Vector4>();var resultUv1=new List<Vector4>();source.GetUVs(1,originalUv1);mesh.GetUVs(1,resultUv1);var sourceVertices=source.vertices;var sourceUv=source.uv;var sourceNormals=source.normals;var resultVertices=mesh.vertices;var resultUv=mesh.uv;var resultNormals=mesh.normals;var sourceIndices=source.GetTriangles(group.Key.Item2).Distinct().ToArray();var exterior=mesh.GetTriangles(0).Distinct().ToArray();int verified=0;foreach(int vertex in exterior.Where((value,index)=>index%Mathf.Max(1,exterior.Length/64)==0)){var matching=sourceIndices.Where(index=>Vector3.Distance(sourceVertices[index],resultVertices[vertex])<1e-6f&&Vector2.Distance(sourceUv[index],resultUv[vertex])<1e-6f&&Vector3.Dot(sourceNormals[index].normalized,resultNormals[vertex].normalized)>.9999f).ToArray();if(matching.Length==0)continue;Assert.That(matching.Any(index=>Vector4.Distance(originalUv1[index],resultUv1[vertex])<1e-5f),Is.True,"Surviving actual LOD0 seam vertices must retain original imported UV1 values.");verified++;}icecreamExactSourceVertices+=verified;TestContext.WriteLine("ACTUAL_SELECTED_ICECREAM_UV1 "+renderer.name+"/"+source.name+" vertices="+mesh.vertexCount+" exactImportedSeamSamples="+verified);}
                bool upper=(bool)Get(part,"upper");foreach(var vertex in mesh.vertices){float y=(transform*toBody).MultiplyPoint3x4(vertex).y;Assert.That(upper?y-plane:plane-y,Is.GreaterThanOrEqualTo(-.0002f));}
                if(mesh.subMeshCount>1){double cap=Area(mesh,1,toBody);if(upper)topCap+=cap;else bottomCap+=cap;foreach(int index in mesh.GetTriangles(1).Distinct())Assert.That((transform*toBody).MultiplyPoint3x4(mesh.vertices[index]).y,Is.EqualTo(plane).Within(.0002f));}
            }
            double original=Area(source,group.Key.Item2,toBody);Assert.That(area,Is.EqualTo(original).Within(Math.Max(1e-4,original*.005)));
        }
        for(int index=0;index<renderers.Length;index++){var renderer=renderers[index];if(!renderer.enabled||((string[])Get(body,"wheelRendererNames")).Contains(renderer.name))continue;var filter=renderer.GetComponent<MeshFilter>();Assert.That(filter,Is.Not.Null);for(int material=0;material<filter.sharedMesh.subMeshCount;material++)if(Area(filter.sharedMesh,material,Matrix4x4.identity)>1e-8)Assert.That(parts.Any(part=>(int)Get(part,"rendererIndex")==index&&(int)Get(part,"materialIndex")==material),Is.True,"Every actual source material surface must be represented.");}
        Assert.That(topCap,Is.GreaterThan(0));Assert.That(topCap,Is.EqualTo(bottomCap).Within(Math.Max(1e-4,topCap*.005)));if(bodyId=="novelty.icecream"){Assert.That(icecreamExactSourceVertices,Is.GreaterThan(0));Assert.That(icecreamUv1,Is.True,"Actual selected imported MiniBus LOD0 UV1 must be tested, not a lower LOD fixture.");}
    }
}
#endif

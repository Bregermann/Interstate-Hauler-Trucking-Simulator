#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class TruckFragmentRecipeTests
{
    const BindingFlags Any=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
    static object Get(object value,string field)=>value.GetType().GetField(field,Any) is FieldInfo member?member.GetValue(value):value.GetType().GetProperty(field,Any).GetValue(value);
    static double Area(Mesh mesh,int submesh,Matrix4x4 transform)
    {
        var vertices=mesh.vertices;var indices=mesh.GetTriangles(submesh);double area=0;
        for(int i=0;i<indices.Length;i+=3){var a=transform.MultiplyPoint3x4(vertices[indices[i]]);var b=transform.MultiplyPoint3x4(vertices[indices[i+1]]);var c=transform.MultiplyPoint3x4(vertices[indices[i+2]]);area+=Vector3.Cross(b-a,c-a).magnitude*.5;}
        return area;
    }
    [TestCase("semi.standard")][TestCase("truck.monster")][TestCase("semi.lowrider")][TestCase("truck.lifted")][TestCase("truck.fire")][TestCase("novelty.hotdog")][TestCase("novelty.icecream")]
    public void PrivateVendorRecipePreservesEveryOriginalMaterialSurfaceUvAndEightBodyRegions(string bodyId)
    {
        var catalog=AssetDatabase.LoadAssetAtPath<ScriptableObject>("Assets/TruckSports/Resources/SportsTruckAppearanceCatalog.asset");Assert.That(catalog,Is.Not.Null);var body=catalog.GetType().GetMethod("FindBody").Invoke(catalog,new object[]{bodyId});var prefab=(GameObject)Get(body,"visualPrefab");
        var recipe=AssetDatabase.LoadAssetAtPath<ScriptableObject>("Assets/TruckSports/Customization/PrivateLicensedFragments/"+bodyId+".asset");Assert.That(recipe,Is.Not.Null,"Run the explicit private licensed recipe author first.");
        var args=new object[]{body,null};Assert.That(recipe.GetType().GetMethod("ValidFor").Invoke(recipe,args),Is.EqualTo(true),args[1] as string);var parts=((IEnumerable)Get(recipe,"parts")).Cast<object>().ToArray();
        string recipePath=AssetDatabase.GetAssetPath(recipe);var embedded=AssetDatabase.LoadAllAssetsAtPath(recipePath).OfType<Mesh>().ToArray();Assert.That(embedded.Length,Is.EqualTo(parts.Select(part=>(Mesh)Get(part,"mesh")).Distinct().Count()));string serialized=System.IO.File.ReadAllText(recipePath);Assert.That(serialized,Does.StartWith("%YAML"));Assert.That(System.Text.RegularExpressions.Regex.IsMatch(serialized,@"(?m)^\s+mesh: \{fileID: 0\}"),Is.False,"Every independently committed recipe must contain persisted embedded mesh references before another body is authored.");Assert.That(System.Text.RegularExpressions.Regex.Matches(serialized,@"(?m)^--- !u!43 &").Count,Is.EqualTo(embedded.Length),"Native Mesh subassets must exist on disk, not only in the Editor's unsaved object cache.");
        Assert.That(Get(recipe,"sourceDependencyHash"),Is.EqualTo(AssetDatabase.GetAssetDependencyHash(AssetDatabase.GetAssetPath(prefab)).ToString()),"Source geometry/material/normal changes require an explicit private recipe repair, never silently stale wreck art.");
        CollectionAssert.AreEquivalent(Enumerable.Range(0,8),parts.Select(part=>(int)Get(part,"region")).Distinct());var renderers=prefab.GetComponentsInChildren<Renderer>(true);var tires=(string[])Get(body,"wheelRendererNames");int surfaces=0;
        for(int index=0;index<renderers.Length;index++)
        {
            var renderer=renderers[index];if(!renderer.enabled||tires.Contains(renderer.name))continue;var filter=renderer.GetComponent<MeshFilter>();Assert.That(filter,Is.Not.Null);Assert.That(filter.sharedMesh.isReadable,Is.True);var matrix=prefab.transform.worldToLocalMatrix*renderer.localToWorldMatrix;
            for(int material=0;material<filter.sharedMesh.subMeshCount;material++)
            {
                double source=Area(filter.sharedMesh,material,matrix);if(source<1e-8)continue;var derived=parts.Where(part=>(int)Get(part,"rendererIndex")==index&&(int)Get(part,"sourceMaterialIndex")==material).ToArray();Assert.That(derived,Is.Not.Empty,bodyId+" "+renderer.name+" material="+material);
                double sliced=0;foreach(var part in derived)
                {
                    var mesh=(Mesh)Get(part,"mesh");Assert.That(AssetDatabase.GetAssetPath(mesh),Does.StartWith("Assets/TruckSports/Customization/PrivateLicensedFragments/"));Assert.That(Get(part,"sourceMesh"),Is.SameAs(filter.sharedMesh));Assert.That(mesh.subMeshCount,Is.InRange(1,2));Assert.That(mesh.normals.Length,Is.EqualTo(mesh.vertexCount));
                    if(filter.sharedMesh.uv.Length==filter.sharedMesh.vertexCount)Assert.That(mesh.uv.Length,Is.EqualTo(mesh.vertexCount),"Vendor slice must retain atlas UVs for each independently mapped original material surface.");
                    Assert.That(mesh.vertices.All(Finite),Is.True);Assert.That(mesh.triangles.All(vertex=>Finite(mesh.normals[vertex])),Is.True,"Every referenced original/cut normal must remain finite.");if(mesh.uv.Length==mesh.vertexCount)Assert.That(mesh.uv.All(uv=>float.IsFinite(uv.x)&&float.IsFinite(uv.y)),Is.True);if(mesh.tangents.Length==mesh.vertexCount)Assert.That(mesh.tangents.All(tangent=>Finite(new Vector3(tangent.x,tangent.y,tangent.z))&&float.IsFinite(tangent.w)),Is.True);sliced+=Area(mesh,0,matrix);
                }
                Assert.That(sliced,Is.EqualTo(source).Within(Math.Max(1e-4,source*.005)),"Partitioned original outer-surface area must remain complete; cut caps are a separate material submesh.");surfaces++;
            }
        }
        Assert.That(surfaces,Is.GreaterThan(0));TestContext.WriteLine("PRIVATE_VENDOR_FRAGMENT_RECIPE "+bodyId+" originalMaterialSurfaces="+surfaces+" chunks="+parts.Length+" eight occupied regions, original submesh atlas UVs/material indices and surface area preserved. Derived commercial geometry stays private; runtime/capture proof separate.");
    }
    static bool Finite(Vector3 value)=>float.IsFinite(value.x)&&float.IsFinite(value.y)&&float.IsFinite(value.z);
    static Type Author=>Type.GetType("LWS.TruckSports.Editor.SportsTruckFragmentRecipeAuthoring, Assembly-CSharp-Editor",true);
    static Mesh Fixture()
    {
        var mesh=new Mesh{name="Synthetic original hard-edge atlas surface plus collinear face"};mesh.vertices=new[]{new Vector3(-1,-1,0),new Vector3(1,-1,0),new Vector3(0,1,0),new Vector3(-1,0,0),new Vector3(1,0,0),new Vector3(2,0,0)};mesh.normals=new[]{Vector3.back,Vector3.back,Vector3.back,Vector3.up,Vector3.up,Vector3.up};mesh.uv=new[]{new Vector2(.1f,.2f),new Vector2(.8f,.2f),new Vector2(.4f,.9f),Vector2.zero,Vector2.right,Vector2.one};mesh.tangents=Enumerable.Repeat(new Vector4(1,0,0,-1),6).ToArray();mesh.SetUVs(1,Enumerable.Range(0,6).Select(index=>new Vector4(index,.25f,.5f,.75f)).ToList());mesh.colors=Enumerable.Repeat(Color.magenta,6).ToArray();mesh.subMeshCount=2;mesh.SetTriangles(new[]{0,1,2},0);mesh.SetTriangles(new[]{3,4,5},1);return mesh;
    }
    [Test]public void OwnedCleanupDropsOnlyZeroAreaFacesAndRetainsExactSurvivingAtlasAndHardEdgeAttributes()
    {
        var original=Fixture();var working=UnityEngine.Object.Instantiate(original);
        try
        {
            var sourceVertices=original.vertices;var sourceNormals=original.normals;var sourceUv=original.uv;var sourceTangents=original.tangents;double area=Area(original,0,Matrix4x4.identity);
            Assert.That(Author.GetMethod("RemoveDegenerateTriangles",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{working,"synthetic material-preservation fixture"}),Is.EqualTo(1));Assert.That(working.vertexCount,Is.EqualTo(3));Assert.That(working.subMeshCount,Is.EqualTo(2));CollectionAssert.AreEqual(new[]{0,1,2},working.GetTriangles(0));Assert.That(working.GetTriangles(1),Is.Empty);Assert.That(Area(working,0,Matrix4x4.identity),Is.EqualTo(area));CollectionAssert.AreEqual(sourceVertices.Take(3),working.vertices);CollectionAssert.AreEqual(sourceNormals.Take(3),working.normals);CollectionAssert.AreEqual(sourceUv.Take(3),working.uv);CollectionAssert.AreEqual(sourceTangents.Take(3),working.tangents);CollectionAssert.AreEqual(original.colors.Take(3),working.colors);var uv1=new System.Collections.Generic.List<Vector4>();working.GetUVs(1,uv1);CollectionAssert.AreEqual(Enumerable.Range(0,3).Select(index=>new Vector4(index,.25f,.5f,.75f)),uv1);
            CollectionAssert.AreEqual(sourceVertices,original.vertices);CollectionAssert.AreEqual(sourceNormals,original.normals);CollectionAssert.AreEqual(sourceUv,original.uv);CollectionAssert.AreEqual(sourceTangents,original.tangents);Assert.That(original.GetTriangles(1).Length,Is.EqualTo(3));
        }
        finally{UnityEngine.Object.DestroyImmediate(working);UnityEngine.Object.DestroyImmediate(original);}
    }
    [Test]public void VendorCollinearBarycentricFailureIsExcludedBeforeActualOwnedSliceWithoutChangingSource()
    {
        var type=Type.GetType("Assets.CoreCollapse.EzySlice.Framework.Triangle, Assembly-CSharp",true);var triangle=Activator.CreateInstance(type,new object[]{new Vector3(-1,0,0),new Vector3(1,0,0),new Vector3(2,0,0)});type.GetMethod("SetNormal").Invoke(triangle,new object[]{Vector3.up,Vector3.up,Vector3.up});var generated=(Vector3)type.GetMethod("GenerateNormal").Invoke(triangle,new object[]{Vector3.zero});Assert.That(Finite(generated),Is.False,"Installed vendor barycentric math demonstrably cannot interpolate a collinear source face.");
        var original=Fixture();var working=UnityEngine.Object.Instantiate(original);Mesh clipped=null;
        try
        {
            // Preserve both actual material slots; cleanup removes the zero-area
            // second slot without altering the surviving first slot's UVs.
            Author.GetMethod("RemoveDegenerateTriangles",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{working,"synthetic before-vendor fixture"});clipped=(Mesh)Author.GetMethod("Keep",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{working,Matrix4x4.identity,Vector3.zero,Vector3.right,1,"synthetic actual vendor cut"});Assert.That(clipped,Is.Not.Null);Assert.That(clipped.triangles.All(index=>Finite(clipped.normals[index])),Is.True);Assert.That(clipped.uv.All(uv=>float.IsFinite(uv.x)&&float.IsFinite(uv.y)),Is.True);Assert.That(Area(clipped,0,Matrix4x4.identity),Is.EqualTo(Area(original,0,Matrix4x4.identity)*.5).Within(1e-6));Assert.That(original.vertexCount,Is.EqualTo(6));Assert.That(original.GetTriangles(1).Length,Is.EqualTo(3));
        }
        finally{if(clipped!=null)UnityEngine.Object.DestroyImmediate(clipped);if(working!=null)UnityEngine.Object.DestroyImmediate(working);UnityEngine.Object.DestroyImmediate(original);}
    }
    [Test]public void ActualMonsterPropShaftCutRepairsOnlyInvalidGeneratedTangentsPreservingFiniteAtlasSurfaceData()
    {
        var catalog=AssetDatabase.LoadAssetAtPath<ScriptableObject>("Assets/TruckSports/Resources/SportsTruckAppearanceCatalog.asset");var body=catalog.GetType().GetMethod("FindBody").Invoke(catalog,new object[]{"truck.monster"});var prefab=(GameObject)Get(body,"visualPrefab");var renderer=prefab.GetComponentsInChildren<Renderer>(true).Single(part=>part.name=="PropShaftRear");var source=renderer.GetComponent<MeshFilter>().sharedMesh;Assert.That(source.name,Is.EqualTo("PropShaftFront"));var originalVertices=source.vertices;var originalNormals=source.normals;var originalUv=source.uv;var originalTangents=source.tangents;Assert.That(originalTangents.All(tangent=>float.IsFinite(tangent.x)&&float.IsFinite(tangent.y)&&float.IsFinite(tangent.z)&&float.IsFinite(tangent.w)),Is.True,"Malformed imported tangents must be diagnosed as source data, never silently repaired.");
        var input=UnityEngine.Object.Instantiate(source);Mesh upper=null,lower=null;
        try
        {
            input.subMeshCount=1;input.SetTriangles(source.GetTriangles(0),0);Author.GetMethod("RemoveDegenerateTriangles",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{input,"actual Monster PropShaftRear source"});var transform=prefab.transform.worldToLocalMatrix*renderer.localToWorldMatrix;var bounds=(Bounds)Get(body,"visualEnvelope");var point=transform.inverse.MultiplyPoint3x4(new Vector3(bounds.center.x,0,0));var normal=transform.transpose.MultiplyVector(Vector3.right).normalized;var planeType=Type.GetType("Assets.CoreCollapse.EzySlice.Framework.Plane, Assembly-CSharp",true);var regionType=Type.GetType("Assets.CoreCollapse.EzySlice.Framework.TextureRegion, Assembly-CSharp",true);var slicer=Type.GetType("Assets.CoreCollapse.EzySlice.Slicer, Assembly-CSharp",true);var plane=Activator.CreateInstance(planeType,new object[]{point,normal});var region=Activator.CreateInstance(regionType,new object[]{0f,0f,1f,1f});var hull=slicer.GetMethods().Single(method=>method.Name=="Slice"&&method.GetParameters()[0].ParameterType==typeof(Mesh)).Invoke(null,new object[]{input,plane,region,1});Assert.That(hull,Is.Not.Null);upper=(Mesh)Get(hull,"upperHull");lower=(Mesh)Get(hull,"lowerHull");Assert.That(upper,Is.Not.Null);Author.GetMethod("RemoveDegenerateTriangles",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{upper,"actual Monster PropShaftRear region1 x-min"});var before=upper.tangents;Assert.That(before.Any(tangent=>!float.IsFinite(tangent.x)||!float.IsFinite(tangent.y)||!float.IsFinite(tangent.z)||!float.IsFinite(tangent.w)),Is.True,"This reproduces the retained native vendor cut failure, rather than supplying invented bad vectors.");var vertices=upper.vertices;var normals=upper.normals;var uv=upper.uv;var triangles=Enumerable.Range(0,upper.subMeshCount).Select(index=>upper.GetTriangles(index)).ToArray();
            Assert.That((int)Author.GetMethod("RepairGeneratedTangents",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{upper,"actual Monster PropShaftRear region1 x-min"}),Is.GreaterThan(0));var after=upper.tangents;Assert.That(after.All(tangent=>float.IsFinite(tangent.x)&&float.IsFinite(tangent.y)&&float.IsFinite(tangent.z)&&float.IsFinite(tangent.w)),Is.True);for(int i=0;i<before.Length;i++)if(float.IsFinite(before[i].x)&&float.IsFinite(before[i].y)&&float.IsFinite(before[i].z)&&float.IsFinite(before[i].w))Assert.That(after[i],Is.EqualTo(before[i]),"Existing finite tangent must remain exact.");CollectionAssert.AreEqual(vertices,upper.vertices);CollectionAssert.AreEqual(normals,upper.normals);CollectionAssert.AreEqual(uv,upper.uv);Assert.That(upper.subMeshCount,Is.EqualTo(triangles.Length));for(int i=0;i<triangles.Length;i++)CollectionAssert.AreEqual(triangles[i],upper.GetTriangles(i));CollectionAssert.AreEqual(originalVertices,source.vertices);CollectionAssert.AreEqual(originalNormals,source.normals);CollectionAssert.AreEqual(originalUv,source.uv);CollectionAssert.AreEqual(originalTangents,source.tangents);
        }
        finally{if(upper!=null)UnityEngine.Object.DestroyImmediate(upper);if(lower!=null)UnityEngine.Object.DestroyImmediate(lower);UnityEngine.Object.DestroyImmediate(input);}
    }
}
#endif

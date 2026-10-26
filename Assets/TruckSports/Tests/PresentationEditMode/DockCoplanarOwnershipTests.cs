#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
public sealed class DockCoplanarOwnershipTests
{
    const BindingFlags Any=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static object Get(object value,string name)=>value.GetType().GetField(name,Any) is FieldInfo field?field.GetValue(value):value.GetType().GetProperty(name,Any).GetValue(value);
    static Type Author=>Type.GetType("LWS.TruckSports.Editor.SportsTruckAppearanceHostAuthoring, Assembly-CSharp-Editor",true);
    static Mesh Half(Mesh source,Matrix4x4 matrix,Vector3 point,Vector3 normal,bool upper)=>(Mesh)Author.GetMethod("SliceDockSurfaceHalf").Invoke(null,new object[]{source,0,matrix,point,normal,upper});
    static double Area(Mesh mesh,int sub,Matrix4x4 matrix){var v=mesh.vertices;var t=mesh.GetTriangles(sub);double result=0;for(int face=0;face<t.Length;face+=3)result+=Vector3.Cross(matrix.MultiplyVector(v[t[face+1]]-v[t[face]]),matrix.MultiplyVector(v[t[face+2]]-v[t[face]])).magnitude*.5;return result;}
    [Test]public void VendorOnBandFacesBelongOnceBySignedCentroidAndExactPlaneBelongsOnlyToOriginalUpper()
    {
        float epsilon=(float)Type.GetType("Assets.CoreCollapse.EzySlice.Framework.Intersector, Assembly-CSharp",true).GetField("Epsilon").GetRawConstantValue();var source=new Mesh();Mesh upper=null,lower=null;
        try
        {
            float[] heights={-epsilon*.25f,epsilon*.25f,0};var points=heights.SelectMany((y,index)=>new[]{new Vector3(index*3-1,y,0),new Vector3(index*3,y,1),new Vector3(index*3+1,y,0)}).ToArray();source.vertices=points;source.normals=Enumerable.Repeat(Vector3.up,9).ToArray();source.uv=points.Select(v=>new Vector2(v.x,v.z)).ToArray();source.SetUVs(1,points.Select(v=>new Vector4(v.x+7,v.y+2,v.z+9,.25f)).ToList());source.colors=Enumerable.Repeat(Color.magenta,9).ToArray();source.triangles=Enumerable.Range(0,9).ToArray();var originalIndices=source.triangles;var originalUv=source.uv;var originalColors=source.colors;var originalExtra=new List<Vector4>();source.GetUVs(1,originalExtra);
            var planeType=Type.GetType("Assets.CoreCollapse.EzySlice.Framework.Plane, Assembly-CSharp",true);var plane=Activator.CreateInstance(planeType,new object[]{Vector3.zero,Vector3.up});foreach(var point in points)Assert.That(planeType.GetMethod("SideOf").Invoke(plane,new object[]{point}).ToString(),Is.EqualTo("ON"),"The installed vendor classifies each actual epsilon-band vertex as ON.");
            upper=Half(source,Matrix4x4.identity,Vector3.zero,Vector3.up,true);lower=Half(source,Matrix4x4.identity,Vector3.zero,Vector3.down,false);Assert.That(upper,Is.Not.Null);Assert.That(lower,Is.Not.Null);Assert.That(Area(upper,0,Matrix4x4.identity),Is.EqualTo(2).Within(1e-6));Assert.That(Area(lower,0,Matrix4x4.identity),Is.EqualTo(1).Within(1e-6));Assert.That(Area(upper,0,Matrix4x4.identity)+Area(lower,0,Matrix4x4.identity),Is.EqualTo(Area(source,0,Matrix4x4.identity)).Within(1e-6));Assert.That(upper.GetTriangles(0).All(index=>upper.vertices[index].y>=0),Is.True);Assert.That(lower.GetTriangles(0).All(index=>lower.vertices[index].y<0),Is.True);
            foreach(var mesh in new[]{upper,lower}){var extra=new List<Vector4>();mesh.GetUVs(1,extra);foreach(int index in mesh.GetTriangles(0).Distinct()){int original=Array.IndexOf(points,mesh.vertices[index]);Assert.That(original,Is.GreaterThanOrEqualTo(0));Assert.That(mesh.uv[index],Is.EqualTo(originalUv[original]));Assert.That(mesh.colors[index],Is.EqualTo(originalColors[original]));Assert.That(extra[index],Is.EqualTo(originalExtra[original]));}}CollectionAssert.AreEqual(points,source.vertices);CollectionAssert.AreEqual(originalIndices,source.triangles);CollectionAssert.AreEqual(originalUv,source.uv);CollectionAssert.AreEqual(originalColors,source.colors);
        }
        finally{if(upper!=null)UnityEngine.Object.DestroyImmediate(upper);if(lower!=null)UnityEngine.Object.DestroyImmediate(lower);UnityEngine.Object.DestroyImmediate(source);}
    }
    [Test]public void ActualHotdogRightDoorMaterialTwoPreservesSourceAreaAndUnchangedCapsWithoutDuplicatingOriginalQuad209()
    {
        var catalog=AssetDatabase.LoadAssetAtPath<ScriptableObject>("Assets/TruckSports/Resources/SportsTruckAppearanceCatalog.asset");var body=catalog.GetType().GetMethod("FindBody").Invoke(catalog,new object[]{"novelty.hotdog"});var prefab=(GameObject)Get(body,"visualPrefab");var renderers=prefab.GetComponentsInChildren<Renderer>(true);int rendererIndex=Array.FindIndex(renderers,r=>r.name=="door rt");Assert.That(rendererIndex,Is.EqualTo(18));var renderer=renderers[rendererIndex];var source=renderer.GetComponent<MeshFilter>().sharedMesh;var matrix=prefab.transform.worldToLocalMatrix*renderer.localToWorldMatrix;var recipe=AssetDatabase.LoadAssetAtPath<ScriptableObject>("Assets/TruckDockDiving/Customization/PrivateLicensedSplitBodies/novelty.hotdog.asset");Assert.That(recipe,Is.Not.Null,"Keep the original native-generated asset for cap comparison before explicit reviewed repair.");var canonical=(Matrix4x4)Get(recipe,"canonicalToVehicle");var point=canonical.inverse.MultiplyPoint3x4(new Vector3(0,(float)Get(recipe,"vehiclePlaneY"),0));var normal=canonical.transpose.MultiplyVector(Vector3.up).normalized;
        Mesh upper=null,lower=null;try
        {
            var method=Author.GetMethod("SliceDockSurfaceHalf");upper=(Mesh)method.Invoke(null,new object[]{source,2,matrix,point,normal,true});lower=(Mesh)method.Invoke(null,new object[]{source,2,matrix,point,-normal,false});Assert.That(upper,Is.Not.Null);Assert.That(lower,Is.Not.Null);double original=Area(source,2,matrix),sliced=Area(upper,0,matrix)+Area(lower,0,matrix);Assert.That(sliced,Is.EqualTo(original).Within(Math.Max(1e-4,original*.005)),"Keep the original0.5% whole source-surface bound.");
            var old=((IEnumerable)Get(recipe,"parts")).Cast<object>().Where(part=>(int)Get(part,"rendererIndex")==rendererIndex&&(int)Get(part,"materialIndex")==2).ToArray();Assert.That(old.Length,Is.EqualTo(2));foreach(var part in old){var mesh=(Mesh)Get(part,"mesh");var current=(bool)Get(part,"upper")?upper:lower;Assert.That(Area(current,1,matrix),Is.EqualTo(Area(mesh,1,matrix)).Within(1e-6),"Full vendor-ON triangles contributed no intersections; original cap topology/area stays unchanged.");}
            var localPoint=matrix.inverse.MultiplyPoint3x4(point);var localNormal=matrix.transpose.MultiplyVector(normal).normalized;var vendorPlane=Activator.CreateInstance(Type.GetType("Assets.CoreCollapse.EzySlice.Framework.Plane, Assembly-CSharp",true),new object[]{localPoint,localNormal});var classify=vendorPlane.GetType().GetMethod("SideOf");var v=source.vertices;var indices=source.GetTriangles(2);int belowFaces=0;
            for(int face=0;face<indices.Length;face+=3){int[] tri={indices[face],indices[face+1],indices[face+2]};if(tri.All(index=>classify.Invoke(vendorPlane,new object[]{v[index]}).ToString()=="ON")&&tri.Average(index=>Vector3.Dot(localNormal,v[index]-localPoint))<0)belowFaces++;}Assert.That(belowFaces,Is.EqualTo(2),"Exact current FBX polygon209 is triangulated into two full vendor-ON exterior faces below the real plane.");
            TestContext.WriteLine("ACTUAL_DOCK_HOTDOG_DOOR material2 sourceArea="+original.ToString("R")+" correctedArea="+sliced.ToString("R")+" vendorOnBelowFaces="+belowFaces+"; original FBX geometry713367782 polygon209 vertices215,219,250,246; caps unchanged.");
        }
        finally{if(upper!=null)UnityEngine.Object.DestroyImmediate(upper);if(lower!=null)UnityEngine.Object.DestroyImmediate(lower);}
    }
}
#endif

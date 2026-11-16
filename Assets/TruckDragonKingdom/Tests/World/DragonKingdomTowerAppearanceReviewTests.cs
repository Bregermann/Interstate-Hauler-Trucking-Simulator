#if UNITY_EDITOR
using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class DragonKingdomTowerAppearanceReviewTests
{
    static Type Owner=>Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomRegionalWorldIntegration, Assembly-CSharp-Editor",true);
    static object Call(string method,params object[] args)=>Owner.GetMethod(method).Invoke(null,args);
    static object Field(object value,string name)=>value.GetType().GetField(name).GetValue(value);
    static Mesh Specimen(bool nativeUv1)
    {
        var mesh=new Mesh{name="Temporary oriented palette mesh"};mesh.vertices=new[]{Vector3.zero,Vector3.right,Vector3.up};mesh.normals=new[]{Vector3.forward,Vector3.forward,Vector3.forward};mesh.tangents=new[]{new Vector4(1,0,0,1),new Vector4(1,0,0,1),new Vector4(1,0,0,1)};mesh.colors32=new[]{new Color32(90,100,110,255),new Color32(90,100,110,255),new Color32(90,100,110,255)};mesh.uv=new[]{Vector2.one*.25f,Vector2.one*.25f,Vector2.one*.25f};if(nativeUv1)mesh.uv2=new[]{Vector2.zero,Vector2.right,Vector2.up};mesh.triangles=new[]{0,1,2};mesh.RecalculateBounds();return mesh;
    }
    [TestCase(true)][TestCase(false)]
    public void DetailBindingReusesValidNativeUvOrUsesUnityUnwrapperWithoutChangingOriginalCorners(bool nativeUv1)
    {
        var mesh=Specimen(nativeUv1);object binding=null;
        try{binding=Call("CreateTowerDetailUvBinding",mesh);Assert.That(Field(binding,"originalUv1Usable"),Is.EqualTo(nativeUv1));Assert.That(Field(binding,"generatedSecondaryUv"),Is.EqualTo(!nativeUv1));Assert.That(Field(binding,"orientedTriangleCornersExceptUv1Exact"),Is.True);Assert.That(Call("TowerMeshOnlyDetailUvDiffers",mesh,(Mesh)Field(binding,"mesh")),Is.True);if(nativeUv1)Assert.That(Field(binding,"mesh"),Is.SameAs(mesh));}
        finally{(binding as IDisposable)?.Dispose();UnityEngine.Object.DestroyImmediate(mesh);}
    }
    [TestCase("position")][TestCase("winding")][TestCase("normal")][TestCase("tangent")][TestCase("uv0")][TestCase("color")]
    public void DetailUvContractRejectsEveryOriginalGeometryOrSourcePaletteAttributeChange(string fault)
    {
        var source=Specimen(true);var changed=UnityEngine.Object.Instantiate(source);
        try
        {
            switch(fault){case "position":var p=changed.vertices;p[0].x=.0001f;changed.vertices=p;break;case "winding":changed.triangles=new[]{0,2,1};break;case "normal":var n=changed.normals;n[0]=Vector3.back;changed.normals=n;break;case "tangent":var t=changed.tangents;t[0].w=-1;changed.tangents=t;break;case "uv0":var uv=changed.uv;uv[0].x+=.001f;changed.uv=uv;break;case "color":var colors=changed.colors32;colors[0].r++;changed.colors32=colors;break;}
            Assert.That(Call("TowerMeshOnlyDetailUvDiffers",source,changed),Is.False);
        }
        finally{UnityEngine.Object.DestroyImmediate(changed);UnityEngine.Object.DestroyImmediate(source);}
    }
    [Test]
    public void DegenerateNativeDetailUvRequiresNativeUnwrapInsteadOfMistakingPaletteCoordinatesForDetail()
    {
        var mesh=Specimen(true);mesh.uv2=new[]{Vector2.one*.25f,Vector2.one*.25f,Vector2.one*.25f};object binding=null;
        try{binding=Call("CreateTowerDetailUvBinding",mesh);Assert.That(Field(binding,"originalUv1Usable"),Is.False);Assert.That(Field(binding,"generatedSecondaryUv"),Is.True);Assert.That(Field(binding,"orientedTriangleCornersExceptUv1Exact"),Is.True);}
        finally{(binding as IDisposable)?.Dispose();UnityEngine.Object.DestroyImmediate(mesh);}
    }
    [TestCase(-.01f,12)][TestCase(.21f,12)][TestCase(float.NaN,12)][TestCase(.12f,0)][TestCase(.12f,33)][TestCase(.12f,float.PositiveInfinity)]
    public void AppearanceReviewRejectsUnboundedOrInvalidDetailBeforeAssetMutation(float mask,float repeat)
    {var error=Assert.Throws<TargetInvocationException>(()=>Call("ValidateTowerDetailParameters",mask,repeat));Assert.That(error.InnerException,Is.TypeOf<ArgumentException>());}
    [TestCase("Resources/unity_builtin_extra")][TestCase("Library/unity default resources")]
    public void KnownUnityVirtualResourcesAreExplicitlyReportedInsteadOfInventingDiskHashes(string path)
    {var proof=Call("ResolveTowerAppearanceSource",path);Assert.That(Field(proof,"resourceKind"),Is.EqualTo("UNITY_BUILTIN_VIRTUAL_RESOURCE_NO_DISK_FILE"));Assert.That(Field(proof,"path"),Is.EqualTo(path));Assert.That(Field(proof,"sha256"),Is.Null);Assert.That(Field(proof,"resolvedPath"),Is.Null);}
    [TestCase("Assets/__missing_tower_review_dependency__.hlsl")][TestCase("Packages/__unregistered_tower_review__/missing.hlsl")][TestCase("Library/__unknown_virtual_resource__")][TestCase("")]
    public void MissingRealOrUnknownVirtualDependenciesRejectRatherThanDisappearingFromTheWatchList(string path)
    {var error=Assert.Throws<TargetInvocationException>(()=>Call("ResolveTowerAppearanceSource",path));Assert.That(error.InnerException,Is.TypeOf<InvalidOperationException>());}
    [Test]
    public void ActualRegisteredPackageIncludeResolvesToInstalledSourceAndReportsExactBytes()
    {
        const string path="Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl";var proof=Call("ResolveTowerAppearanceSource",path);var package=UnityEditor.PackageManager.PackageInfo.FindForAssetPath(path);
        Assert.That(Field(proof,"resourceKind"),Is.EqualTo("REGISTERED_PACKAGE_DISK_SOURCE"));Assert.That(Field(proof,"packageName"),Is.EqualTo(package.name));Assert.That(Field(proof,"packageVersion"),Is.EqualTo(package.version));
        string actual=System.IO.Path.GetFullPath((string)Field(proof,"resolvedPath")),expected=System.IO.Path.GetFullPath(System.IO.Path.Combine(package.resolvedPath,"ShaderLibrary/Core.hlsl"));Assert.That(actual,Is.EqualTo(expected).IgnoreCase);Assert.That(System.IO.File.Exists(actual),Is.True);using(var hash=System.Security.Cryptography.SHA256.Create()){string sha=BitConverter.ToString(hash.ComputeHash(System.IO.File.ReadAllBytes(actual))).Replace("-","").ToLowerInvariant();Assert.That(Field(proof,"sha256"),Is.EqualTo(sha));}
    }
    [Test]
    public void ActualVendorGraphExposesEveryControlUsedByBothComparisonMaterials()
    {
        string path=(string)Owner.GetField("TowerDetailVendorGraph").GetValue(null);var shader=UnityEditor.AssetDatabase.LoadAssetAtPath<Shader>(path);Assert.That(shader,Is.Not.Null);var material=new Material(shader);try{var names=(string[])Call("TowerDetailVendorProperties");Assert.That(names,Has.Length.EqualTo(27));Assert.That(new System.Collections.Generic.HashSet<string>(names).Count,Is.EqualTo(names.Length));foreach(string name in names)Assert.That(material.HasProperty(name),Is.True,"Required active comparison control "+name);}finally{UnityEngine.Object.DestroyImmediate(material);}
    }
}
#endif

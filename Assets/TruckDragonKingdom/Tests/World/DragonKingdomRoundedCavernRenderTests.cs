#if UNITY_EDITOR
using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using System.Linq;
using System.Reflection;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public sealed class DragonKingdomRoundedCavernRenderTests
{
    static Type Probe=>Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomRoadSurfaceProbe, Assembly-CSharp-Editor",true);
    [TestCase(false,false)][TestCase(true,false)][TestCase(false,true)][TestCase(true,true)]
    public void NormalDiagnosticsRejectSimultaneousAmplitudeAndKeywordChanges(bool amplitude,bool keyword)
    {
        Action call=()=>Probe.GetMethod("ValidateCavernNormalDiagnosticModes").Invoke(null,new object[]{amplitude,keyword});
        if(amplitude&&keyword)Assert.That(Assert.Throws<TargetInvocationException>(()=>call()).InnerException,Is.TypeOf<InvalidOperationException>());else Assert.DoesNotThrow(()=>call());
    }
    [TestCase("none")][TestCase("scalar")][TestCase("texture")][TestCase("other-keyword")][TestCase("shader")]
    public void KeywordOnlyClonePreservesOriginalInputsAndRejectsOtherMutations(string mutation)
    {
        var shader=Shader.Find("Digger/Mesh/URP/Lit-Pass0");Assert.That(shader,Is.Not.Null);var source=new Material(shader);var texture=new Texture2D(1,1);source.SetTexture("_Splat3",texture);source.SetTexture("_Normal3",texture);source.SetTextureScale("_Splat3",new Vector2(2.5f,3.25f));source.SetTextureOffset("_Splat3",new Vector2(.1f,.2f));source.SetFloat("_NormalScale3",3.75f);source.EnableKeyword("_NORMALMAP");source.EnableKeyword("_MASKMAP");source.EnableKeyword("_TERRAIN_BLEND_HEIGHT");string original=EditorJsonUtility.ToJson(source);
        try{for(int repeat=0;repeat<2;repeat++){var copy=(Material)Probe.GetMethod("CreateCavernNormalKeywordDiagnosticMaterial").Invoke(null,new object[]{source});try{bool Exact()=>(bool)Probe.GetMethod("CavernNormalKeywordOnlyPropertiesExact").Invoke(null,new object[]{source,copy});Assert.That(Exact(),Is.True);Assert.That(copy.IsKeywordEnabled("_NORMALMAP"),Is.False);Assert.That(copy.GetFloat("_NormalScale3"),Is.EqualTo(3.75f));Assert.That(copy.GetTexture("_Normal3"),Is.SameAs(texture));if(mutation=="scalar")copy.SetFloat("_NormalScale3",4.75f);if(mutation=="texture"){copy.SetTexture("_Splat3",Texture2D.blackTexture);Assert.That(copy.GetTexture("_Splat3"),Is.SameAs(Texture2D.blackTexture));Assert.That(copy.GetTexture("_Splat3"),Is.Not.SameAs(source.GetTexture("_Splat3")),"The negative control must replace the real native texture reference, even when transient-object JSON is identical.");}if(mutation=="other-keyword")copy.DisableKeyword("_MASKMAP");if(mutation=="shader")copy.shader=Shader.Find("Universal Render Pipeline/Lit");Assert.That(Exact(),Is.EqualTo(mutation=="none"));}finally{UnityEngine.Object.DestroyImmediate(copy);}Assert.That(EditorJsonUtility.ToJson(source),Is.EqualTo(original));}}
        finally{UnityEngine.Object.DestroyImmediate(source);UnityEngine.Object.DestroyImmediate(texture);}
    }
    [TestCase("none")][TestCase("position")][TestCase("normal")][TestCase("uv")]
    public void BaryRgbCopyPreservesAlphaAndAllOtherNativePayloadAndRejectsOtherMutations(string mutation)
    {
        var mesh=new Mesh();var layout=Type.GetType("Digger.Modules.Core.Sources.VertexData, Digger.Core",true);mesh.SetVertexBufferParams(3,(VertexAttributeDescriptor[])layout.GetField("Layout").GetValue(null));int stride=mesh.GetVertexBufferStride(0);var data=new byte[3*stride];void Write(int at,float value)=>Array.Copy(BitConverter.GetBytes(value),0,data,at,4);
        for(int vertex=0;vertex<3;vertex++){Write(vertex*stride,vertex==1?1:0);Write(vertex*stride+8,vertex==2?1:0);Write(vertex*stride+16,1);Write(vertex*stride+24+vertex*4,1);Write(vertex*stride+36,.25f*(vertex+1));Write(vertex*stride+40,.1f*(vertex+1));Write(vertex*stride+44,.2f*(vertex+1));Write(vertex*stride+76,1);}
        mesh.SetVertexBufferData(data,0,0,data.Length);mesh.SetIndexBufferParams(3,IndexFormat.UInt16);mesh.SetIndexBufferData(new ushort[]{0,2,1},0,0,3);mesh.subMeshCount=1;mesh.SetSubMesh(0,new SubMeshDescriptor(0,3));mesh.RecalculateBounds();string before=EditorJsonUtility.ToJson(mesh);Mesh copy=null;
        try{copy=(Mesh)Probe.GetMethod("CreateCavernZeroBaryRgbMesh").Invoke(null,new object[]{mesh});bool Exact()=>(bool)Probe.GetMethod("CavernBaryRgbOnlyChangeExact").Invoke(null,new object[]{mesh,copy});Assert.That(Exact(),Is.True);for(int i=0;i<3;i++){Assert.That(copy.colors[i].r,Is.Zero);Assert.That(copy.colors[i].g,Is.Zero);Assert.That(copy.colors[i].b,Is.Zero);Assert.That(copy.colors[i].a,Is.EqualTo(mesh.colors[i].a));}if(mutation=="position"){var p=copy.vertices;p[0].x=1;copy.vertices=p;}if(mutation=="normal")copy.normals=new[]{Vector3.forward,Vector3.forward,Vector3.forward};if(mutation=="uv")copy.uv=new[]{Vector2.one,Vector2.one,Vector2.one};Assert.That(Exact(),Is.EqualTo(mutation=="none"));Assert.That(EditorJsonUtility.ToJson(mesh),Is.EqualTo(before));}
        finally{if(copy!=null)UnityEngine.Object.DestroyImmediate(copy);UnityEngine.Object.DestroyImmediate(mesh);}
    }
    [TestCase("none")][TestCase("other-scale")][TestCase("offset")][TestCase("texture")][TestCase("keyword")][TestCase("normal-scale")]
    public void RockRepeatCopyChangesOnlyLayerThreeScaleAndRejectsOtherNativeProperties(string mutation)
    {
        var shader=Shader.Find("Digger/Mesh/URP/Lit-Pass0");Assert.That(shader,Is.Not.Null);var source=new Material(shader);var texture=new Texture2D(2,2);source.SetTexture("_Splat0",texture);source.SetTexture("_Splat3",texture);source.SetTextureScale("_Splat0",new Vector2(2,3));source.SetTextureScale("_Splat3",new Vector2(1f/3f,1f/3f));source.SetTextureOffset("_Splat3",new Vector2(.1f,.2f));source.SetFloat("_NormalScale3",.75f);source.EnableKeyword("_NORMALMAP");string original=EditorJsonUtility.ToJson(source);
        try{for(int repeat=0;repeat<2;repeat++){var copy=(Material)Probe.GetMethod("CreateCavernDoubleRockRepeatMaterial").Invoke(null,new object[]{source});try{bool Exact()=>(bool)Probe.GetMethod("CavernRockRepeatOnlyPropertiesExact").Invoke(null,new object[]{source,copy});Assert.That(Exact(),Is.True);Assert.That(copy.GetTextureScale("_Splat3").Equals(source.GetTextureScale("_Splat3")*.5f),Is.True);Assert.That(copy.GetTextureOffset("_Splat3").Equals(source.GetTextureOffset("_Splat3")),Is.True);Assert.That(copy.GetTexture("_Splat3"),Is.SameAs(texture));if(mutation=="other-scale")copy.SetTextureScale("_Splat0",Vector2.one);if(mutation=="offset")copy.SetTextureOffset("_Splat3",Vector2.zero);if(mutation=="texture"){copy.SetTexture("_Splat3",Texture2D.whiteTexture);Assert.That(copy.GetTexture("_Splat3"),Is.Not.SameAs(texture));}if(mutation=="keyword")copy.DisableKeyword("_NORMALMAP");if(mutation=="normal-scale")copy.SetFloat("_NormalScale3",1.75f);Assert.That(Exact(),Is.EqualTo(mutation=="none"));}finally{UnityEngine.Object.DestroyImmediate(copy);}Assert.That(EditorJsonUtility.ToJson(source),Is.EqualTo(original));}}
        finally{UnityEngine.Object.DestroyImmediate(source);UnityEngine.Object.DestroyImmediate(texture);}
    }
    [TestCase("none")][TestCase("diffuse")][TestCase("normal")][TestCase("scale")][TestCase("offset")][TestCase("keyword")][TestCase("other-layer")]
    public void OwnedRockVariantRetainsNativeShaderAndRejectsUnapprovedChanges(string mutation)
    {
        var source=new Material(Shader.Find("Digger/Mesh/URP/Lit-Pass0"));source.EnableKeyword("_NORMALMAP");source.SetTexture("_Splat3",Texture2D.whiteTexture);source.SetTexture("_Normal3",Texture2D.normalTexture);source.SetTextureOffset("_Splat3",new Vector2(.1f,.2f));string before=EditorJsonUtility.ToJson(source);Material copy=null;
        try
        {
            copy=(Material)Probe.GetMethod("CreateCavernOwnedRoughRockMaterial").Invoke(null,new object[]{source});
            Assert.That(copy.shader,Is.SameAs(source.shader));Assert.That(copy.GetTextureScale("_Splat3"),Is.EqualTo(Vector2.one/12f));Assert.That(AssetDatabase.GetAssetPath(copy.GetTexture("_Splat3")),Is.EqualTo("Assets/Digger/Misc/Textures/Rough_rock_019_COLOR.png"));Assert.That(AssetDatabase.GetAssetPath(copy.GetTexture("_Normal3")),Is.EqualTo("Assets/Digger/Misc/Textures/Rough_rock_019_NRM.png"));
            if(mutation=="diffuse")copy.SetTexture("_Splat3",Texture2D.blackTexture);if(mutation=="normal")copy.SetTexture("_Normal3",Texture2D.normalTexture);if(mutation=="scale")copy.SetTextureScale("_Splat3",Vector2.one);if(mutation=="offset")copy.SetTextureOffset("_Splat3",Vector2.zero);if(mutation=="keyword")copy.DisableKeyword("_NORMALMAP");if(mutation=="other-layer")copy.SetTexture("_Splat0",Texture2D.blackTexture);
            Assert.That((bool)Probe.GetMethod("CavernOwnedRockPropertiesExact").Invoke(null,new object[]{source,copy}),Is.EqualTo(mutation=="none"));Assert.That(EditorJsonUtility.ToJson(source),Is.EqualTo(before));
        }
        finally{if(copy!=null)UnityEngine.Object.DestroyImmediate(copy);UnityEngine.Object.DestroyImmediate(source);}
    }
    [Test,Timeout(360000)]
    public void ActualSavedRoundedCavernRendersThreeGeometryViewsWithExactSourcesAndPhysicalState()
    {
        const string generated="Assets/TruckDragonKingdom/Tests/World/Generated/";Directory.CreateDirectory(generated);AssetDatabase.Refresh();
        for(int i=0;i<SceneManager.sceneCount;i++)
        {
            var scene=SceneManager.GetSceneAt(i);if(UnityEngine.Application.isBatchMode&&string.IsNullOrEmpty(scene.path))Assert.That(EditorSceneManager.SaveScene(scene,generated+"RoundedRenderRunner-"+Guid.NewGuid().ToString("N")+".unity"),Is.True);
            Assert.That(scene.isDirty||string.IsNullOrEmpty(scene.path),Is.False);
        }
        var type=Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomRoadSurfaceProbe, Assembly-CSharp-Editor",true);type.GetMethod("CaptureSavedRoundedCavern").Invoke(null,null);
        var proof=type.GetProperty("LastRoundedCavernFrames").GetValue(null);object Field(string name)=>proof.GetType().GetField(name).GetValue(proof);
        Assert.That((bool)Field("sourcesExact"),Is.True);Assert.That((bool)Field("physicsAndHeightsExact"),Is.True);Assert.That(((Array)Field("frames")).Length,Is.EqualTo(3));Assert.That((int)Field("actualSavedDiggerRenderers"),Is.GreaterThan(0));
        Assert.That((bool)Field("voxelMembershipExact"),Is.True);
        bool shadowDiagnostic=Array.IndexOf(Environment.GetCommandLineArgs(),"-tsDragonCavernShadowDiagnostic")>=0;Assert.That((bool)Field("shadowDiagnosticRequested"),Is.EqualTo(shadowDiagnostic));Assert.That(((Array)Field("shadowFrames")).Length,Is.EqualTo(shadowDiagnostic?2:0),"Optional shadow views must never replace or enlarge the original three baseline frames.");
        if(shadowDiagnostic)
        {
            Assert.That((bool)Field("shadowDiagnosticPerformed"),Is.True);Assert.That((bool)Field("directionalLightRestored"),Is.True);Assert.That((bool)Field("shadowCameraPoseExact"),Is.True);Assert.That((string)Field("shadowLightJsonAfter"),Is.EqualTo((string)Field("shadowLightJsonBefore")));Assert.That((string)Field("shadowLightPoseAfter"),Is.EqualTo((string)Field("shadowLightPoseBefore")));Assert.That((string)Field("shadowLightJsonWhileDisabled"),Is.Not.EqualTo((string)Field("shadowLightJsonBefore")),"The actual source light's shadow state must genuinely change for the diagnostic.");
            foreach(var frame in (Array)Field("shadowFrames"))Assert.That(File.Exists((string)frame.GetType().GetField("png").GetValue(frame)),Is.True);
        }
        bool normalDiagnostic=Array.IndexOf(Environment.GetCommandLineArgs(),"-tsDragonCavernNormalDiagnostic")>=0;Assert.That((bool)Field("normalDiagnosticRequested"),Is.EqualTo(normalDiagnostic));Assert.That(((Array)Field("normalFrames")).Length,Is.EqualTo(normalDiagnostic?2:0),"Normal-amplitude diagnostic images remain separate from the original three views.");
        if(normalDiagnostic)
        {
            foreach(string field in new[]{"normalDiagnosticPerformed","normalStateRestored","normalTemporaryCopiesDestroyed","normalTexturesUvExact","normalNonAmplitudePropertiesExact","normalCameraAndLightsExact","normalMeshBindingsExact","normalRendererBindingsExact","normalOriginalMaterialsExact"})Assert.That((bool)Field(field),Is.True,field);
            Assert.That((int)Field("normalTemporaryCopies"),Is.GreaterThan(0));Assert.That((int)Field("normalScaleChannelsZeroed"),Is.GreaterThan(0));Assert.That((int)Field("normalAuthoredMapsWithNonzeroScale"),Is.GreaterThan(0));Assert.That(((Array)Field("normalMaterials")).Length,Is.EqualTo((int)Field("normalTemporaryCopies")));
            foreach(var frame in (Array)Field("normalFrames"))Assert.That(File.Exists((string)frame.GetType().GetField("png").GetValue(frame)),Is.True);
        }
        bool keywordDiagnostic=Array.IndexOf(Environment.GetCommandLineArgs(),"-tsDragonCavernNormalKeywordDiagnostic")>=0;
        Assert.That((bool)Field("normalKeywordDiagnosticRequested"),Is.EqualTo(keywordDiagnostic));Assert.That(((Array)Field("normalKeywordFrames")).Length,Is.EqualTo(keywordDiagnostic?2:0));
        if(keywordDiagnostic)
        {
            Assert.That(normalDiagnostic,Is.False,"Amplitude and keyword changes must never be combined.");Assert.That((bool)Field("normalDiagnosticPerformed"),Is.False);Assert.That((bool)Field("normalKeywordDiagnosticPerformed"),Is.True);Assert.That((string)Field("normalDiagnosticMode"),Is.EqualTo("KEYWORD_ONLY_NORMALMAP_DISABLED"));
            foreach(string field in new[]{"normalStateRestored","normalTemporaryCopiesDestroyed","normalTexturesUvExact","normalNonAmplitudePropertiesExact","normalCameraAndLightsExact","normalMeshBindingsExact","normalRendererBindingsExact","normalOriginalMaterialsExact","normalKeywordAllPropertiesExact","normalKeywordMeshPayloadExact","normalKeywordPropertyBlocksAbsent"})Assert.That((bool)Field(field),Is.True,field);
            Assert.That((int)Field("normalScaleChannelsZeroed"),Is.Zero);Assert.That((int)Field("normalKeywordsDisabled"),Is.EqualTo((int)Field("normalTemporaryCopies")).And.GreaterThan(0));
            foreach(var row in (Array)Field("normalMaterials")){object Row(string name)=>row.GetType().GetField(name).GetValue(row);Assert.That((bool)Row("keywordOnly")&&(bool)Row("allOtherPropertiesExact"),Is.True);Assert.That((string[])Row("originalKeywords"),Does.Contain("_NORMALMAP"));Assert.That((string[])Row("diagnosticKeywords"),Does.Not.Contain("_NORMALMAP"));}
            foreach(var frame in (Array)Field("normalKeywordFrames"))Assert.That(File.Exists((string)frame.GetType().GetField("png").GetValue(frame)),Is.True);
        }
        bool tilingDiagnostic=Array.IndexOf(Environment.GetCommandLineArgs(),"-tsDragonCavernRockTilingDiagnostic")>=0;Assert.That((bool)Field("rockTilingDiagnosticRequested"),Is.EqualTo(tilingDiagnostic));var tiling=Field("rockTilingDiagnostic");
        if(tilingDiagnostic)
        {
            Assert.That(tiling,Is.Not.Null);object Tiling(string name)=>tiling.GetType().GetField(name).GetValue(tiling);
            foreach(string name in new[]{"performed","onlyRockScaleChanged","restoredImageByteExact","samplerMetadataExact","stateRestored","copiesDestroyed","originalMaterialsExact","meshBindingsExact","meshPayloadExact","collidersExact","cameraLightsLodsExact","rendererStateExact","propertyBlocksAbsent"})Assert.That((bool)Tiling(name),Is.True,name);
            Assert.That((int)Tiling("temporaryMaterials"),Is.GreaterThan(0));Assert.That((int)Tiling("rendererSlotsChanged"),Is.GreaterThan(0));Assert.That(((Array)Tiling("frames")).Length,Is.EqualTo(2));Assert.That(((Array)Tiling("materials")).Length,Is.EqualTo((int)Tiling("temporaryMaterials")));
            foreach(var row in (Array)Tiling("materials")){object Row(string name)=>row.GetType().GetField(name).GetValue(row);Assert.That(((Vector2)Row("diagnosticScale")).Equals((Vector2)Row("originalScale")*.5f),Is.True);var textures=(Array)Row("actualTextures");Assert.That(textures.Length,Is.EqualTo(2));foreach(var texture in textures){object Sampler(string name)=>texture.GetType().GetField(name).GetValue(texture);Assert.That((bool)Sampler("present"),Is.True);Assert.That((string)Sampler("path"),Does.StartWith("Assets/"));Assert.That((string)Sampler("guid"),Has.Length.EqualTo(32));Assert.That((string)Sampler("format"),Is.Not.Empty);Assert.That((int)Sampler("mipCount"),Is.GreaterThan(0));foreach(string name in new[]{"filterMode","wrapU","wrapV"})Assert.That((string)Sampler(name),Is.Not.Empty);}}
            foreach(var frame in (Array)Tiling("frames"))Assert.That(File.Exists((string)frame.GetType().GetField("png").GetValue(frame)),Is.True);
        }
        else Assert.That(tiling,Is.Null);
        bool ownedRockDiagnostic=Array.IndexOf(Environment.GetCommandLineArgs(),"-tsDragonCavernOwnedRockMaterialDiagnostic")>=0;Assert.That((bool)Field("ownedRockMaterialDiagnosticRequested"),Is.EqualTo(ownedRockDiagnostic));var ownedRock=Field("ownedRockMaterialDiagnostic");
        if(ownedRockDiagnostic)
        {
            Assert.That(ownedRock,Is.Not.Null);object Rock(string name)=>ownedRock.GetType().GetField(name).GetValue(ownedRock);
            foreach(string name in new[]{"performed","ownedRockMapsRequested","onlyApprovedRockInputsChanged","diagnosticImageChanged","ownedRockSourceBytesExact","restoredImageByteExact","samplerMetadataExact","stateRestored","copiesDestroyed","originalMaterialsExact","meshBindingsExact","meshPayloadExact","collidersExact","cameraLightsLodsExact","rendererStateExact","propertyBlocksAbsent"})Assert.That((bool)Rock(name),Is.True,name);
            Assert.That(((string[])Rock("ownedRockSourcePaths")).Length,Is.EqualTo(4));Assert.That(((string[])Rock("ownedRockSourceSha256")).Length,Is.EqualTo(4));Assert.That((string)Rock("diagnosticPngSha256"),Is.Not.EqualTo((string)Rock("baselinePngSha256")));
            Assert.That((bool)Rock("onlyRockScaleChanged"),Is.False,"This variant changes the paired layer3 maps and repeat; never report scale-only isolation.");Assert.That(((Array)Rock("frames")).Length,Is.EqualTo(2));
            foreach(var row in (Array)Rock("materials")){object Row(string name)=>row.GetType().GetField(name).GetValue(row);Assert.That((Vector2)Row("diagnosticScale"),Is.EqualTo(Vector2.one/12f));var textures=(Array)Row("candidateTextures");Assert.That(textures.Length,Is.EqualTo(2));foreach(var texture in textures){object Texture(string name)=>texture.GetType().GetField(name).GetValue(texture);Assert.That((bool)Texture("present"),Is.True);Assert.That((string)Texture("path"),Does.StartWith("Assets/Digger/Misc/Textures/Rough_rock_019_"));}}
            foreach(var frame in (Array)Rock("frames"))Assert.That(File.Exists((string)frame.GetType().GetField("png").GetValue(frame)),Is.True);
        }
        else Assert.That(ownedRock,Is.Null);
        bool baryDiagnostic=Array.IndexOf(Environment.GetCommandLineArgs(),"-tsDragonCavernBaryRgbDiagnostic")>=0;Assert.That((bool)Field("baryRgbDiagnosticRequested"),Is.EqualTo(baryDiagnostic));var bary=Field("baryRgbDiagnostic");
        if(baryDiagnostic)
        {
            Assert.That(bary,Is.Not.Null);object Bary(string name)=>bary.GetType().GetField(name).GetValue(bary);
            foreach(string field in new[]{"performed","stateRestored","copiesDestroyed","onlyRgbChanged","originalMeshPayloadExact","meshBindingsExact","materialsExact","collidersExact","cameraLightsLodsExact","propertyBlocksAbsent"})Assert.That((bool)Bary(field),Is.True,field);
            Assert.That((int)Bary("copiedMeshes"),Is.GreaterThan(0));Assert.That((int)Bary("changedRgbVertices"),Is.GreaterThan(0));Assert.That((int)Bary("changedRgbComponents"),Is.GreaterThan(0));Assert.That(((Array)Bary("frames")).Length,Is.EqualTo(2));
            foreach(var frame in (Array)Bary("frames"))Assert.That(File.Exists((string)frame.GetType().GetField("png").GetValue(frame)),Is.True);
        }
        else Assert.That(bary,Is.Null);
        bool lodDiagnostic=Array.IndexOf(Environment.GetCommandLineArgs(),"-tsDragonCavernLodDiagnostic")>=0;Assert.That((bool)Field("lodDiagnosticRequested"),Is.EqualTo(lodDiagnostic));Assert.That(((Array)Field("lodFrames")).Length,Is.EqualTo(lodDiagnostic?2:0),"LOD diagnostic images remain separate from the original three baseline views.");
        if(lodDiagnostic)
        {
            foreach(string field in new[]{"lodDiagnosticPerformed","lodStateRestored","lodRendererReferencesExact","lodCameraAndLightsExact","lodMeshMaterialBindingsExact"})Assert.That((bool)Field(field),Is.True,field);
            Assert.That((int)Field("lodGroupsChanged"),Is.GreaterThan(0));Assert.That((int)Field("lodLevelsObserved"),Is.EqualTo(3*(int)Field("lodGroupsChanged")));Assert.That((int)Field("lodNonemptyFirstLevelMeshes"),Is.GreaterThan(0));Assert.That(((Array)Field("lodGroups")).Length,Is.EqualTo((int)Field("lodGroupsChanged")));
            foreach(var row in (Array)Field("lodGroups"))
            {
                object GroupField(string name)=>row.GetType().GetField(name).GetValue(row);Assert.That((string)GroupField("afterJson"),Is.EqualTo((string)GroupField("beforeJson")));Assert.That((string)GroupField("poseAfter"),Is.EqualTo((string)GroupField("poseBefore")));
                var before=(Array)GroupField("before");var high=(Array)GroupField("highDetail");var after=(Array)GroupField("after");Assert.That(before.Length,Is.EqualTo(3));Assert.That(high.Length,Is.EqualTo(3));Assert.That(after.Length,Is.EqualTo(3));
                for(int i=0;i<3;i++)
                {
                    object LevelField(Array levels,string name)=>levels.GetValue(i).GetType().GetField(name).GetValue(levels.GetValue(i));
                    Assert.That((float)LevelField(high,"transition"),Is.EqualTo(i==0?.000001f:i==1?.0000005f:0));Assert.That((float)LevelField(after,"transition"),Is.EqualTo((float)LevelField(before,"transition")));
                    foreach(string field in new[]{"renderers","meshes","vertices"}){CollectionAssert.AreEqual((Array)LevelField(before,field),(Array)LevelField(high,field),field);CollectionAssert.AreEqual((Array)LevelField(before,field),(Array)LevelField(after,field),field);}Assert.That((float)LevelField(high,"fadeWidth"),Is.EqualTo((float)LevelField(before,"fadeWidth")));
                }
            }
            foreach(var frame in (Array)Field("lodFrames"))Assert.That(File.Exists((string)frame.GetType().GetField("png").GetValue(frame)),Is.True);
        }
        bool neutralDiagnostic=Array.IndexOf(Environment.GetCommandLineArgs(),"-tsDragonCavernNeutralSurfaceDiagnostic")>=0;Assert.That((bool)Field("neutralDiagnosticRequested"),Is.EqualTo(neutralDiagnostic));Assert.That(((Array)Field("neutralFrames")).Length,Is.EqualTo(neutralDiagnostic?2:0),"Neutral-surface pair remains separate from the original three baseline views.");
        if(neutralDiagnostic)
        {
            foreach(string field in new[]{"neutralDiagnosticPerformed","neutralStateRestored","neutralOriginalMaterialsExact","neutralRendererMeshBindingsExact","neutralCameraLightsLodsExact","neutralCopiesDestroyed"})Assert.That((bool)Field(field),Is.True,field);Assert.That((int)Field("neutralTemporaryCopies"),Is.GreaterThan(0));Assert.That((int)Field("neutralAuthoredDiffuseMaps"),Is.GreaterThan(0));Assert.That((int)Field("neutralRendererSlotsChanged"),Is.GreaterThan(0));Assert.That(((Array)Field("neutralMaterials")).Length,Is.EqualTo((int)Field("neutralTemporaryCopies")));
            foreach(var row in (Array)Field("neutralMaterials")){object MaterialField(string name)=>row.GetType().GetField(name).GetValue(row);foreach(string field in new[]{"sameShader","uniformDiffuse","zeroNormalsMasksMetallicSmoothness","uvExact"})Assert.That((bool)MaterialField(field),Is.True,field);Assert.That((string)MaterialField("shader"),Does.StartWith("Digger/Mesh/URP/Lit-Pass"));Assert.That((string[])MaterialField("neutralKeywords"),Does.Not.Contain("_NORMALMAP").And.Not.Contain("_MASKMAP").And.Not.Contain("_TERRAIN_BLEND_HEIGHT"));}
            foreach(var frame in (Array)Field("neutralFrames"))Assert.That(File.Exists((string)frame.GetType().GetField("png").GetValue(frame)),Is.True);
        }
        bool weldedDiagnostic=Array.IndexOf(Environment.GetCommandLineArgs(),"-tsDragonCavernWeldedNormalDiagnostic")>=0;Assert.That((bool)Field("weldedNormalDiagnosticRequested"),Is.EqualTo(weldedDiagnostic));Assert.That(((Array)Field("weldedNormalFrames")).Length,Is.EqualTo(weldedDiagnostic?2:0),"Native welded-normal images must remain separate from the original three saved views.");
        if(weldedDiagnostic)
        {
            foreach(string field in new[]{"weldedNormalDiagnosticPerformed","weldedNormalStateRestored","weldedNormalTemporaryObjectsDestroyed"})Assert.That((bool)Field(field),Is.True,field);Assert.That((int)Field("weldedNormalMeshes"),Is.EqualTo((int)Field("actualSavedDiggerRenderers")));Assert.That((int)Field("weldedNormalChangedMeshes"),Is.GreaterThan(0));Assert.That(((Array)Field("weldedNormalGroups")).Length,Is.EqualTo(3));
            foreach(var group in (Array)Field("weldedNormalGroups")){object GroupField(string name)=>group.GetType().GetField(name).GetValue(group);foreach(string field in new[]{"unityRecalculateNormalsCalled","proxyPositionsAndOrientedTrianglesExact","allCopyNonNormalAttributesExact","allCopiedNormalsMatchNativeProxy"})Assert.That((bool)GroupField(field),Is.True,field);Assert.That((int)GroupField("exactDuplicateVerticesWelded"),Is.GreaterThan(0));Assert.That((float)GroupField("maximumUnitSquaredError"),Is.LessThanOrEqualTo(.00001f));}
            foreach(var frame in (Array)Field("weldedNormalFrames"))Assert.That(File.Exists((string)frame.GetType().GetField("png").GetValue(frame)),Is.True);
        }
        bool standardDiagnostic=Array.IndexOf(Environment.GetCommandLineArgs(),"-tsDragonCavernStandardLitDiagnostic")>=0;Assert.That((bool)Field("standardLitDiagnosticRequested"),Is.EqualTo(standardDiagnostic));
        var standard=Field("standardLitDiagnostic");
        if(standardDiagnostic)
        {
            Assert.That(standard,Is.Not.Null);object Standard(string name)=>standard.GetType().GetField(name).GetValue(standard);
            foreach(string field in new[]{"performed","stateRestored","temporaryMaterialsDestroyed","meshBindingsExact","propertyBlocksRestored","meshPayloadExact","propertyBlocksProvenAbsent","originalMaterialsExact","rendererStateExact","physicalBindingsExact","cameraLightsLodsExact"})Assert.That((bool)Standard(field),Is.True,field);
            Assert.That((string)Standard("shader"),Is.EqualTo("Universal Render Pipeline/Lit"));Assert.That((string[])Standard("shaderErrors"),Is.Empty);Assert.That((int)Standard("rendererSlotsChanged"),Is.GreaterThan(0));Assert.That(((Array)Standard("frames")).Length,Is.EqualTo(2),"Standard shader pair stays separate from the three original saved views.");
            foreach(var frame in (Array)Standard("frames"))Assert.That(File.Exists((string)frame.GetType().GetField("png").GetValue(frame)),Is.True);
        }
        else Assert.That(standard,Is.Null);
        Assert.That((string)Field("scope"),Does.Contain("NOT_PLAYER_BUILD"));
    }
}
#endif

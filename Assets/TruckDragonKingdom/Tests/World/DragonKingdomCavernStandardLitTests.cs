#if UNITY_EDITOR && VISTA
using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class DragonKingdomCavernStandardLitTests
{
    static Type Probe => Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomRoadSurfaceProbe, Assembly-CSharp-Editor", true);

    [Test] public void StandardMaterialUsesActualUrpLitOpaqueNeutralDiffuseWithoutMaps()
    {
        var material = (Material)Probe.GetMethod("CreateStandardLitCavernDiagnosticMaterial").Invoke(null, null);
        try
        {
            Assert.That(material.shader.name, Is.EqualTo("Universal Render Pipeline/Lit"));
            Assert.That(material.shader.isSupported, Is.True);
            Assert.That(material.GetTexturePropertyNames().All(p => material.GetTexture(p) == null), Is.True);
            var actualColor = material.GetColor("_BaseColor");
            TestContext.WriteLine("Actual stored neutral RGBA: " + actualColor.ToString("R"));
            // Shader color properties may round-trip through color-space conversion.
            // Keep the visible neutral-color contract without requiring bit identity.
            Assert.That(actualColor.r, Is.EqualTo(.55f).Within(.000001f));
            Assert.That(actualColor.g, Is.EqualTo(.55f).Within(.000001f));
            Assert.That(actualColor.b, Is.EqualTo(.55f).Within(.000001f));
            Assert.That(actualColor.a, Is.EqualTo(1f).Within(.000001f));
            foreach (string name in new[] { "_Surface", "_AlphaClip", "_Metallic", "_Smoothness", "_SpecularHighlights", "_EnvironmentReflections" }) Assert.That(material.GetFloat(name), Is.Zero);
            Assert.That(material.GetFloat("_ZWrite"), Is.EqualTo(1));
            Assert.That(material.renderQueue, Is.EqualTo(2000));
            Assert.That(material.hideFlags, Is.EqualTo(HideFlags.HideAndDontSave));
        }
        finally { UnityEngine.Object.DestroyImmediate(material); }
    }

    [TestCase(false)] [TestCase(true)]
    public void PropertyBlockAbsenceIsProvenAndNonemptyBlocksAreRejected(bool nonempty)
    {
        var preview = EditorSceneManager.NewPreviewScene();
        var go = new GameObject("Temporary standard shader property-block control"); SceneManager.MoveGameObjectToScene(go, preview);
        var renderer = go.AddComponent<MeshRenderer>();
        try
        {
            if (nonempty) { var block = new MaterialPropertyBlock(); block.SetFloat("_SourceAuditControl", 123); renderer.SetPropertyBlock(block); }
            Action invoke = () => Probe.GetMethod("RequireCavernStandardLitNoPropertyBlocks").Invoke(null, new object[] { new[] { renderer } });
            if (nonempty) Assert.That(Assert.Throws<TargetInvocationException>(() => invoke()).InnerException, Is.TypeOf<InvalidOperationException>());
            else Assert.DoesNotThrow(() => invoke());
        }
        finally { UnityEngine.Object.DestroyImmediate(go); EditorSceneManager.ClosePreviewScene(preview); }
    }

    [Test] public void RawPayloadWitnessIncludesNormalBytesAndRestoresExactly()
    {
        var mesh = new Mesh { vertices = new[] { Vector3.zero, Vector3.right, Vector3.forward }, triangles = new[] { 0, 2, 1 }, normals = new[] { Vector3.up, Vector3.up, Vector3.up } };
        var method = Probe.GetMethod("StandardLitMeshPayload", BindingFlags.NonPublic | BindingFlags.Static);
        string Hash() => (string)method.Invoke(null, new object[] { mesh });
        try
        {
            string before = Hash();
            mesh.normals = new[] { Vector3.forward, Vector3.forward, Vector3.forward };
            Assert.That(Hash(), Is.Not.EqualTo(before), "Normal payload mutations must not disappear behind a non-normal geometry hash.");
            mesh.normals = new[] { Vector3.up, Vector3.up, Vector3.up };
            Assert.That(Hash(), Is.EqualTo(before));
        }
        finally { UnityEngine.Object.DestroyImmediate(mesh); }
    }
}
#endif

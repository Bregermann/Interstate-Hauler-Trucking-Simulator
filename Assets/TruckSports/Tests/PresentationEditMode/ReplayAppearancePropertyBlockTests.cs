#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class ReplayAppearancePropertyBlockTests
{
    static void StoredColor(Color actual, Color intended)
    {
        Assert.That(actual.r, Is.EqualTo(intended.r).Within(2e-6f)); Assert.That(actual.g, Is.EqualTo(intended.g).Within(2e-6f)); Assert.That(actual.b, Is.EqualTo(intended.b).Within(2e-6f)); Assert.That(actual.a, Is.EqualTo(intended.a).Within(2e-6f));
    }
    [TestCase("truck.fire")]
    [TestCase("novelty.icecream")]
    public void FreshRecordedAtlasBodyCopiesParticipantLiveryAndRendererWideBlocksWithoutChangingSharedArt(string bodyId)
    {
        var catalog = AssetDatabase.LoadAssetAtPath<ScriptableObject>("Assets/TruckSports/Resources/SportsTruckAppearanceCatalog.asset"); Assert.That(catalog, Is.Not.Null, "Author the existing appearance catalog and owned atlas livery first.");
        var body = catalog.GetType().GetMethod("FindBody").Invoke(catalog, new object[] { bodyId }); Assert.That(body, Is.Not.Null);
        var prefab = (GameObject)body.GetType().GetField("visualPrefab").GetValue(body); Assert.That(prefab, Is.Not.Null);
        Assert.That(prefab.GetComponentsInChildren<Rigidbody>(true), Is.Empty); Assert.That(prefab.GetComponentsInChildren<Collider>(true), Is.Empty); Assert.That(prefab.GetComponentsInChildren<MonoBehaviour>(true), Is.Empty);
        var source = UnityEngine.Object.Instantiate(prefab); GameObject copy = null;
        try
        {
            var renderers = source.GetComponentsInChildren<Renderer>(true); var materials = renderers.Select(renderer => renderer.sharedMaterials).ToArray(); var meshes = source.GetComponentsInChildren<MeshFilter>(true).Select(filter => filter.sharedMesh).ToArray();
            var livery = renderers.Single(renderer => renderer.name == "Owned surface-following participant livery"); int paintSlot = Array.FindIndex(livery.sharedMaterials, material => material != null && material.name == "Sports participant livery"); Assert.That(paintSlot, Is.GreaterThanOrEqualTo(0));
            var copyMethod = Type.GetType("LWS.TruckSports.SportsReplayVisuals, Assembly-CSharp", true).GetMethod("Copy", BindingFlags.Public | BindingFlags.Static);
            Color[] palette = { Color.red, Color.green, Color.blue, Color.yellow, Color.cyan, Color.magenta, new Color(1, .35f, .05f), Color.white };
            for (int participant = 0; participant < palette.Length; participant++)
            {
                for (int i = 0; i < renderers.Length; i++)
                {
                    var block = new MaterialPropertyBlock(); block.SetFloat("_SportsReplayRendererProbe", 10 + i); block.SetColor("_EmissionColor", new Color(.02f * i, .1f, .2f)); renderers[i].SetPropertyBlock(block);
                }
                var paint = new MaterialPropertyBlock(); paint.SetColor("_BaseColor", palette[participant]); paint.SetColor("_Color", palette[participant]); paint.SetFloat("_SportsReplaySlotProbe", 100 + participant); livery.SetPropertyBlock(paint, paintSlot);
                copy = (GameObject)copyMethod.Invoke(null, new object[] { source.transform }); var ghosts = copy.GetComponentsInChildren<Renderer>(true);
                Assert.That(copy.GetComponentsInChildren<Rigidbody>(true), Is.Empty); Assert.That(copy.GetComponentsInChildren<Collider>(true), Is.Empty); Assert.That(ghosts.Length, Is.EqualTo(renderers.Length));
                CollectionAssert.AreEqual(meshes, copy.GetComponentsInChildren<MeshFilter>(true).Select(filter => filter.sharedMesh));
                for (int i = 0; i < ghosts.Length; i++)
                {
                    Assert.That(ghosts[i].name, Is.EqualTo(renderers[i].name)); CollectionAssert.AreEqual(materials[i], ghosts[i].sharedMaterials); CollectionAssert.AreEqual(materials[i], renderers[i].sharedMaterials);
                    var wide = new MaterialPropertyBlock(); ghosts[i].GetPropertyBlock(wide); var sourceWide = new MaterialPropertyBlock(); renderers[i].GetPropertyBlock(sourceWide); Assert.That(wide.GetFloat("_SportsReplayRendererProbe"), Is.EqualTo(10 + i));
                    StoredColor(sourceWide.GetColor("_EmissionColor"), new Color(.02f * i, .1f, .2f)); Assert.That(wide.GetColor("_EmissionColor"), Is.EqualTo(sourceWide.GetColor("_EmissionColor")), "A clone must preserve the exact native-stored source block.");
                    for (int slot = 0; slot < materials[i].Length; slot++)
                    {
                        var actual = new MaterialPropertyBlock(); ghosts[i].GetPropertyBlock(actual, slot);
                        if (renderers[i] == livery && slot == paintSlot)
                        {
                            var original = new MaterialPropertyBlock(); livery.GetPropertyBlock(original, slot); StoredColor(original.GetColor("_BaseColor"), palette[participant]); StoredColor(original.GetColor("_Color"), palette[participant]);
                            Assert.That(actual.GetColor("_BaseColor"), Is.EqualTo(original.GetColor("_BaseColor"))); Assert.That(actual.GetColor("_Color"), Is.EqualTo(original.GetColor("_Color"))); Assert.That(actual.GetFloat("_SportsReplaySlotProbe"), Is.EqualTo(100 + participant));
                        }
                        else Assert.That(actual.isEmpty, Is.True, "Untouched atlas/glass/chrome slots must not receive a livery property block.");
                    }
                }
                UnityEngine.Object.DestroyImmediate(copy); copy = null;
            }
            TestContext.WriteLine("FRESH_RECORDED_ATLAS_LIVERY " + bodyId + " eight participant colors retain exact per-material livery plus independent renderer-wide emission/probes; original shared atlas/glass/chrome/mesh identities unchanged. Property-block data test, not rendered visual acceptance.");
        }
        finally { if (copy != null) UnityEngine.Object.DestroyImmediate(copy); UnityEngine.Object.DestroyImmediate(source); }
    }
}
#endif

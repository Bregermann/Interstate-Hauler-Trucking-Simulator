using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class CampingHammockRampTests
{
    static Type Author => Type.GetType("LWS.TruckCamping.Editor.CampingHammockRampAuthoring, Assembly-CSharp-Editor", true);
    static float Derive(Transform frame, BoxCollider deck, BoxCollider ramp) => (float)Author.GetMethod("DeriveLength").Invoke(null, new object[] { frame, deck, ramp });
    static float Gap(Transform frame, BoxCollider deck, BoxCollider ramp, float length) => (float)Author.GetMethod("InnerGap").Invoke(null, new object[] { frame, deck, ramp, length });
    static BoxCollider Box(Transform parent, Vector3 position, Vector3 size, Quaternion rotation)
    {
        var go = new GameObject("Known original shape"); go.transform.SetParent(parent, false); go.transform.localPosition = position;
        go.transform.localScale = size; go.transform.localRotation = rotation; return go.AddComponent<BoxCollider>();
    }
    [Test] public void ActualBoxCornersDeriveSmallSeamForBothApproachesWithoutChangingDeckOrPitch()
    {
        var root = new GameObject("Controlled geometry, not native driving acceptance");
        try
        {
            root.transform.SetPositionAndRotation(new Vector3(45, 9.7574526f, -65), Quaternion.Euler(0, 37, 0));
            var deck = Box(root.transform, new Vector3(0, .9f, 0), new Vector3(10, .35f, 16), Quaternion.identity);
            foreach (int side in new[] { -1, 1 })
            {
                var ramp = Box(root.transform, new Vector3(0, .4f, side * 11), new Vector3(10, .25f, 7), Quaternion.Euler(side * 7, 0, 0));
                Assert.That(Gap(root.transform, deck, ramp, 7), Is.LessThan(-.48f), "Original ramp actually intrudes under the deck.");
                Physics.SyncTransforms();
                Assert.That(Physics.ComputePenetration(deck, deck.transform.position, deck.transform.rotation, ramp, ramp.transform.position, ramp.transform.rotation, out _, out float oldDepth), Is.True);
                Assert.That(oldDepth, Is.GreaterThan(.22f).And.LessThan(.23f), "R42 measured jacking mechanism reproduced with original authored boxes.");
                float length = Derive(root.transform, deck, ramp);
                Assert.That(length, Is.InRange(5.9f, 6.1f)); Assert.That(Gap(root.transform, deck, ramp, length), Is.EqualTo(.03f).Within(1e-5));
                var originalRotation = ramp.transform.localRotation; ramp.transform.localScale = new Vector3(10, .25f, length);
                Physics.SyncTransforms();
                Assert.That(Physics.ComputePenetration(deck, deck.transform.position, deck.transform.rotation, ramp, ramp.transform.position, ramp.transform.rotation, out _, out _), Is.False, "No static ramp support under the original deck volume.");
                Assert.That(ramp.transform.localRotation, Is.EqualTo(originalRotation)); Assert.That(deck.transform.localPosition, Is.EqualTo(new Vector3(0, .9f, 0)));
            }
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }
    [Test] public void CustomizedRampColliderOrDeckFrameIsRefusedWithoutMutation()
    {
        var root = new GameObject("Customized guard fixture");
        try
        {
            var deck = Box(root.transform, new Vector3(0, .9f, 0), new Vector3(10, .35f, 16), Quaternion.identity);
            var ramp = Box(root.transform, new Vector3(0, .4f, 11), new Vector3(10, .25f, 7), Quaternion.Euler(7, 0, 0));
            ramp.size = new Vector3(1, 1, 1.1f); var original = EditorJsonUtility.ToJson(ramp);
            Assert.That(Assert.Throws<TargetInvocationException>(() => Derive(root.transform, deck, ramp)).InnerException, Is.TypeOf<InvalidOperationException>());
            Assert.That(EditorJsonUtility.ToJson(ramp), Is.EqualTo(original)); ramp.size = Vector3.one;
            deck.transform.localPosition += Vector3.up * .02f;
            Assert.That(Assert.Throws<TargetInvocationException>(() => Derive(root.transform, deck, ramp)).InnerException, Is.TypeOf<InvalidOperationException>());
            Assert.That(deck.transform.localPosition.y, Is.EqualTo(.92f).Within(1e-6));
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }
    [TestCase("\n")]
    [TestCase("\r\n")]
    public void NativeTransformScaleExtractionAcceptsLfAndCrLfAndCannotReadAdjacentDocument(string newline)
    {
        string yaml = string.Join(newline, new[] {
            "%YAML 1.1", "--- !u!4 &42", "Transform:",
            "  m_LocalScale: {x: 10, y: 0.25, z: 7}",
            "--- !u!4 &43", "Transform:", "  m_LocalScale: {x: 1, y: 1, z: 3}", "" });
        var extract = Author.GetMethod("ExtractScaleZ");
        Assert.That(extract.Invoke(null, new object[] { yaml, 42L }), Is.EqualTo("7"));
        Assert.That(extract.Invoke(null, new object[] { yaml, 43L }), Is.EqualTo("3"));
        Assert.That(Assert.Throws<TargetInvocationException>(() => extract.Invoke(null, new object[] { yaml, 44L })).InnerException,
            Is.TypeOf<InvalidOperationException>(), "Missing Transform identity must not fall through to another scale.");
        string noScale = yaml.Replace("  m_LocalScale: {x: 10, y: 0.25, z: 7}", "  m_LocalPosition: {x: 0, y: 0, z: 0}");
        Assert.That(Assert.Throws<TargetInvocationException>(() => extract.Invoke(null, new object[] { noScale, 42L })).InnerException,
            Is.TypeOf<InvalidOperationException>(), "A known Transform without its scale must not consume the following document.");
    }
    [Serializable] sealed class Edit { public long transformId; public string oldZ, newZ; }
    [Serializable] sealed class Receipt { public string status, asset, beforeFile, afterFile; public int ramps, platforms, trees; public bool allNonRampBytesExact, metaExact; public Edit[] edits; }
    [Test] public void SavedSharedPrefabChangesOnlyEightRampScalarsAndRetainsAllOriginalWorldPhysicsTreesAndMeta()
    {
        string result = (string)Author.GetMethod("VerifyRetainedShorteningAndCurrentChain").Invoke(null, null);
        Assert.That(result, Does.Contain("RETAINED_R44_EIGHT_SCALAR_ARTIFACT_EXACT_CURRENT_STAGE_STRICTLY_VALIDATED"));
        TestContext.WriteLine("RETAINED_R44_SHORTENING_AND_CURRENT_STRICT_CHAIN " + result);
        string folder = Path.GetFullPath("Logs/TruckSports/Impossible07/CampingHammockRampRepair");
        var path = Directory.GetFiles(folder, "receipt.json", SearchOption.AllDirectories).OrderBy(File.GetLastWriteTimeUtc).Last();
        var receipt = JsonUtility.FromJson<Receipt>(File.ReadAllText(path, Encoding.UTF8));
        Assert.That(receipt.status, Is.EqualTo("ONLY_EIGHT_RAMP_Z_SCALES_CHANGED_ALL_OTHER_PREFAB_META_BYTES_EXACT"));
        Assert.That(receipt.ramps, Is.EqualTo(8)); Assert.That(receipt.platforms, Is.EqualTo(4)); Assert.That(receipt.trees, Is.EqualTo(2297));
        Assert.That(receipt.allNonRampBytesExact && receipt.metaExact, Is.True);
        var utf8 = new UTF8Encoding(false, true);
        string retainedAfter = utf8.GetString(File.ReadAllBytes(receipt.afterFile)), original = utf8.GetString(File.ReadAllBytes(receipt.beforeFile)), reversed = retainedAfter;
        foreach (var edit in receipt.edits)
        {
            var doc = Regex.Match(reversed, @"(?m)^--- !u!4 &" + edit.transformId + @"\r?$[\s\S]*?(?=^--- !u!|\z)"); Assert.That(doc.Success, Is.True);
            var scale = Regex.Match(doc.Value, @"(?m)^  m_LocalScale: \{x: [^,]+, y: [^,]+, z: ([^}]+)\}\r?$");
            Assert.That(scale.Success, Is.True); Assert.That(scale.Groups[1].Value, Is.EqualTo(edit.newZ));
            reversed = reversed.Remove(doc.Index + scale.Groups[1].Index, edit.newZ.Length).Insert(doc.Index + scale.Groups[1].Index, edit.oldZ);
        }
        Assert.That(reversed, Is.EqualTo(original), "Every other original R44 artifact byte/record, including all dynamic physics and dressing, must remain unchanged; the current transition is validated separately by its strict hash chain.");
        CollectionAssert.AreEqual(File.ReadAllBytes(receipt.beforeFile), utf8.GetBytes(reversed));
        CollectionAssert.AreEqual(File.ReadAllBytes(receipt.beforeFile + ".meta"), File.ReadAllBytes(receipt.asset + ".meta"));
        Assert.That(receipt.edits.Select(e => e.transformId).Distinct().Count(), Is.EqualTo(8));
    }
}

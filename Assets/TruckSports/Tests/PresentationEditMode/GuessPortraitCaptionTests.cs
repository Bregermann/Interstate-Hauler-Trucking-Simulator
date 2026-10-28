#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

public sealed class GuessPortraitCaptionTests
{
    [Test]
    public void AllNinetySixOriginalIdentityCaptionsStayInsideTheirOwnPortraitFooters()
    {
        string path = "Assets/TruckGuessThat/Scenes/TruckGuessThat.unity";
        byte[] before = File.ReadAllBytes(path); var previous = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            var type = Type.GetType("LWS.TruckGuessThat.GuessTile, Assembly-CSharp", true);
            var matchType = Type.GetType("LWS.TruckGuessThat.GuessMatch, Assembly-CSharp", true);
            var all = scene.GetRootGameObjects(); var match = all.SelectMany(root => root.GetComponentsInChildren(matchType, true)).Single();
            var identities = (Array)matchType.GetField("identities").GetValue(match);
            var tiles = all.SelectMany(root => root.GetComponentsInChildren(type, true)).ToArray(); Assert.That(tiles, Has.Length.EqualTo(96));
            var caption = Type.GetType("LWS.TruckGuessThat.Editor.GuessPortraitLabelAuthoring, Assembly-CSharp-Editor", true).GetMethod("Caption", BindingFlags.Public | BindingFlags.Static);
            foreach (var tile in tiles)
            {
                int index = (int)type.GetField("identity").GetValue(tile);
                var hinge = (Transform)type.GetField("hinge").GetValue(tile);
                var label = hinge.Find("Truthful identity label").GetComponent<TextMesh>(); var identity = identities.GetValue(index);
                string name = (string)identity.GetType().GetField("name").GetValue(identity);
                Assert.That(label.text, Is.EqualTo((string)caption.Invoke(null, new object[] { index, name })));
                Assert.That(label.text.Split('\n'), Has.Length.EqualTo(2));
                Assert.That((Color32)label.color, Is.EqualTo(new Color32(41, 20, 8, 255)), "Original serialized TextMesh Color32 remains exact.");
                var bounds = label.GetComponent<Renderer>().bounds;
                Vector3 lower = hinge.InverseTransformPoint(bounds.min), upper = hinge.InverseTransformPoint(bounds.max);
                Assert.That(lower.x, Is.GreaterThanOrEqualTo(-1.55f), name + " enters previous tile");
                Assert.That(upper.x, Is.LessThanOrEqualTo(1.55f), name + " enters next tile");
                Assert.That(lower.y, Is.GreaterThanOrEqualTo(.015f), name + " falls beneath portrait frame");
                Assert.That(upper.y, Is.LessThanOrEqualTo(.72f), name + " overlaps the miniature truck");
                var portrait = Enumerable.Range(0, hinge.childCount).Select(child => hinge.GetChild(child)).Single(child => child.name.StartsWith("Real catalog portrait · ", StringComparison.Ordinal));
                var boundsMethod = caption.DeclaringType.GetMethod("PortraitBounds", BindingFlags.Public | BindingFlags.Static);
                var model = (Bounds)boundsMethod.Invoke(null, new object[] { portrait });
                Assert.That(model.min.y, Is.GreaterThanOrEqualTo(.849f), name + " model enters the actual caption footer");
                Assert.That(model.max.y, Is.LessThanOrEqualTo(3.101f), name + " model leaves its own portrait frame");
                Assert.That(model.min.x, Is.GreaterThanOrEqualTo(-1.551f)); Assert.That(model.max.x, Is.LessThanOrEqualTo(1.551f));
                Assert.That(Quaternion.Angle(portrait.localRotation, Quaternion.Euler(0, 155, 0)), Is.LessThan(.001f));
                Assert.That(portrait.GetComponentsInChildren<Collider>(true), Is.Empty); Assert.That(portrait.GetComponentsInChildren<Rigidbody>(true), Is.Empty);
            }
            CollectionAssert.AreEqual(before, File.ReadAllBytes(path)); Assert.That(scene.isDirty, Is.False);
            TestContext.WriteLine("GUESS_CAPTION_BOUNDS all96 exact original identities; two truthful dark lines inside own footer; saved scene unchanged by validation.");
        }
        finally
        {
            if (previous.Any(item => item.isLoaded && item.isActive && !string.IsNullOrEmpty(item.path))) EditorSceneManager.RestoreSceneManagerSetup(previous);
        }
    }
}
#endif

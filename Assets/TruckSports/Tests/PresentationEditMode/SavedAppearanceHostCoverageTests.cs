#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;

// Reads the actual saved YAML without opening, rebuilding or saving venues.
// Runtime composition/ownership remains covered by the PlayMode integrations.
public sealed class SavedAppearanceHostCoverageTests
{
    static string Reference(string data, string field) => Regex.Match(data, @"(?m)^  " + field + @": \{fileID: (-?\d+)").Groups[1].Value;
    [TestCase("TruckMusicalParking", 8)]
    [TestCase("TruckBowling", 4)]
    [TestCase("TruckBuzkashi", 6)]
    [TestCase("TruckFishing", 4)]
    [TestCase("TruckDanceOff", 4)]
    public void ApprovedVenueHasSavedExplicitHostsOnSeparateRenderingRoots(string mode, int expected)
    {
        string path = "Assets/" + mode + "/Scenes/" + mode + ".unity";
        string source = File.ReadAllText(path);
        string script = AssetDatabase.AssetPathToGUID("Assets/TruckSports/Scripts/SportsTruckAppearanceHost.cs");
        string catalog = AssetDatabase.AssetPathToGUID("Assets/TruckSports/Resources/SportsTruckAppearanceCatalog.asset");
        Assert.That(script, Is.Not.Empty); Assert.That(catalog, Is.Not.Empty);
        var blocks = Regex.Matches(source, @"(?ms)^--- !u!\d+ &(-?\d+)[^\r\n]*\r?\n(.*?)(?=^--- !u!|\z)").Cast<Match>().ToDictionary(match => match.Groups[1].Value, match => match.Groups[2].Value);
        var hosts = blocks.Where(block => block.Value.StartsWith("MonoBehaviour:") && block.Value.Contains("guid: " + script)).ToArray();
        Assert.That(hosts.Length, Is.EqualTo(expected), "Explicit host authoring must be saved in this approved venue: " + path);
        var actors = new HashSet<string>();
        foreach (var host in hosts)
        {
            string actor = Reference(host.Value, "m_GameObject"), presentation = Reference(host.Value, "presentationParent");
            Assert.That(actors.Add(actor), Is.True, "One explicit host per physical actor.");
            Assert.That(blocks.ContainsKey(presentation), Is.True, "The saved presentation subtree must resolve locally.");
            Assert.That(Reference(blocks[presentation], "m_GameObject"), Is.Not.EqualTo(actor), "The rendering root must be separate from the physical actor.");
            Assert.That(host.Value, Does.Contain("guid: " + catalog));
            Assert.That(Regex.IsMatch(source, @"(?m)^  appearanceHost: \{fileID: " + Regex.Escape(host.Key) + @"\}"), Is.True, "The existing truck must reference its saved host.");
            string wheelList = Regex.Match(host.Value, @"(?ms)^  wheels:\r?\n(.*?)(?=^  \w|\z)").Groups[1].Value;
            var wheels = Regex.Matches(wheelList, @"fileID: (-?\d+)").Cast<Match>().Select(match => match.Groups[1].Value).ToArray();
            Assert.That(wheels.Length, Is.EqualTo(4)); Assert.That(wheels.All(id => id != "0" && blocks.ContainsKey(id)), Is.True, "Use the four existing saved NWH controllers.");
        }
        TestContext.WriteLine("SAVED_APPEARANCE_HOST_COVERAGE " + mode + " hosts=" + expected + "; explicit actor links, separate rendering roots, catalog and four-wheel references checked without scene mutation.");
    }
}
#endif

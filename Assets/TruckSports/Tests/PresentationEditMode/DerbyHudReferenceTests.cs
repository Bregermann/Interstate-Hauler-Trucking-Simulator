#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

public sealed class DerbyHudReferenceTests
{
    [Test]
    public void SavedDerbyUsesOwnedDamageAdapterOriginalIconsAndCollisionFreeOriginalLogos()
    {
        EditorSceneManager.OpenScene("Assets/TruckDemolitionDerby/Scenes/TruckDemolitionDerby.unity");
        var type=Type.GetType("LWS.TruckDerby.DerbyDamageHud, Assembly-CSharp",true);var hud=(Component)UnityEngine.Object.FindObjectsByType(type,FindObjectsSortMode.None).Single();
        foreach(string name in new[]{"match","font","engineIcon","bodyIcon"})Assert.That(type.GetField(name).GetValue(hud),Is.Not.Null,name);
        Assert.That((int)type.GetField("authoredVersion").GetValue(hud),Is.EqualTo(2));
        var textType=Type.GetType("TMPro.TMP_Text, Unity.TextMeshPro",true);var signs=UnityEngine.Object.FindObjectsByType(textType,FindObjectsInactive.Include,FindObjectsSortMode.None).Cast<Behaviour>().Where(t=>t.name=="Event screen readable rules").ToArray();Assert.That(signs.Length,Is.EqualTo(2));Assert.That(signs.All(t=>!t.enabled),Is.True);
        var logo=GameObject.Find("Original Derby artwork · no collision");Assert.That(logo,Is.Not.Null);Assert.That(logo.GetComponentsInChildren<Collider>().Length,Is.Zero);Assert.That(logo.GetComponentsInChildren<MeshRenderer>().Length,Is.EqualTo(2));
        Assert.That(logo.GetComponentsInChildren<MeshRenderer>().All(r=>r.sharedMaterial.mainTexture.name=="TruckDemolitionDerby"),Is.True);
        var floor=logo.transform.Find("Center dirt logo");Assert.That(floor.position,Is.EqualTo(new Vector3(0,.05f,-27)));Assert.That(floor.localScale.x,Is.EqualTo(26));Assert.That(floor.GetComponent<Renderer>().bounds.max.z,Is.LessThan(-16),"Floor artwork must clear the original raised roll banks.");
        var path="Assets/TruckDemolitionDerby/Scenes/TruckDemolitionDerby.unity";var before=File.ReadAllBytes(path);
        Type.GetType("LWS.TruckDerby.Editor.DerbyHudAuthoring, Assembly-CSharp-Editor",true).GetMethod("Upgrade").Invoke(null,null);CollectionAssert.AreEqual(before,File.ReadAllBytes(path),"Repeated explicit authoring must preserve designer edits.");
    }
}
#endif

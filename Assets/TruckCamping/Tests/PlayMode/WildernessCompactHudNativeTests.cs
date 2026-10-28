#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using static WildernessNativeScope;

public sealed class WildernessCompactHudNativeTests
{
    static string Text(Transform root, string name)
    {
        var text = root.Find(name).GetComponent(RuntimeText);
        return (string)text.GetType().GetProperty("text").GetValue(text);
    }
    static readonly Type RuntimeText = Type.GetType("TMPro.TMP_Text, Unity.TextMeshPro", true);

    [UnityTest, Timeout(90000)] public IEnumerator CampingCompactCardsKeepActualOneToFourOwnersAndActionsInsideTheirViewports()
    { yield return Check("TruckCamping", "LWS.TruckCamping.CampingMatch"); }

    [UnityTest, Timeout(90000)] public IEnumerator PetCompactCardsKeepActualOneToFourCompanionNamesAndCareInsideTheirViewports()
    { yield return Check("TruckPetPals", "LWS.TruckPetPals.PetPalsMatch"); }

    static IEnumerator Check(string scene, string authority)
    {
        using (var scope = new WildernessNativeScope("compact-" + scene))
        {
            yield return scope.Load(scene, authority);
            var match = scope.match;
            Assert.That(Get(match, "CompactViewportHud"), Is.EqualTo(true));
            for (int count = 1; count <= 4; count++)
            {
                scope.Configure(count); Assert.That((bool)Call(match, "StartMatch"), Is.True);
                Call(Get(match, "drivingViews"), "Select", 1);
                yield return scope.Advance(.4f);
                var menus = Get(match, "menus");
                var panels = ((IEnumerable)Get(menus, "viewPanels")).Cast<RectTransform>().ToArray();
                var views = Get(Get(match, "drivingViews"), "rally");
                var owners = (int[])Get(views, "owners");
                var players = (Array)Get(Get(match, "setup"), "players");
                for (int view = 0; view < count; view++)
                {
                    int owner = owners[view];
                    var card = (RectTransform)panels[view].Find("Compact owned viewport score");
                    Assert.That(card, Is.Not.Null); Assert.That(card.gameObject.activeInHierarchy, Is.True);
                    Assert.That(Text(card, "Participant name"), Is.EqualTo(Get(players.GetValue(owner), "name")));
                    Assert.That(card.sizeDelta.x, Is.LessThanOrEqualTo(270));
                    Assert.That(card.sizeDelta.y, Is.LessThanOrEqualTo(76));
                    var corners = new Vector3[4]; card.GetWorldCorners(corners);
                    foreach (var corner in corners)
                    {
                        var local = panels[view].InverseTransformPoint(corner);
                        Assert.That(local.x, Is.InRange(panels[view].rect.xMin - .1f, panels[view].rect.xMax + .1f));
                        Assert.That(local.y, Is.InRange(panels[view].rect.yMin - .1f, panels[view].rect.yMax + .1f));
                    }
                    Assert.That(panels[view].Find("View data backing").gameObject.activeSelf, Is.False);
                    var legacy = ((IEnumerable)Get(menus, "viewTexts")).Cast<Component>().ElementAt(view);
                    Assert.That(legacy.gameObject.activeSelf, Is.False);
                    if (scene == "TruckPetPals")
                    {
                        var pet = Components(Get(match, "companions"))[owner];
                        Assert.That(Text(card, "Owned score"), Is.EqualTo(Get(Get(pet, "Record"), "name")));
                    }
                    var explorer = Components(Get(match, "explorers"))[owner];
                    Set(explorer, "ContextOpen", true);
                    yield return null; yield return null;
                    var prompt = card.Find("Contextual action");
                    Assert.That(prompt, Is.Not.Null); Assert.That(prompt.gameObject.activeInHierarchy, Is.True);
                    Assert.That(Text(prompt, "Assigned action"), Does.Contain((string)Call(match, "ChoiceLabel", Get(explorer, "Choice"))));
                    Assert.That(Text(prompt, "Assigned action"), Does.Contain((string)Call(match, "Controls", owner)));
                    Set(explorer, "ContextOpen", false);
                    yield return null; yield return null;
                    string actualHint = (string)Call(match, "ViewportContextPrompt", view);
                    if (string.IsNullOrEmpty(actualHint)) Assert.That(prompt.gameObject.activeSelf, Is.False);
                    else Assert.That(Text(prompt, "Assigned action"), Is.EqualTo(actualHint));
                }
                scope.Capture(scene + "-compact-" + count, count == 4 ? 1920 : 1280, count == 4 ? 1080 : 720);
                Call(match, "SetupScreen"); yield return null;
            }
            Assert.That(Get(match, "Results").ToString(), Does.Contain("NO WINNER OR POINTS"));
        }
    }
}
#endif

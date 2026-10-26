#if UNITY_EDITOR
using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public sealed class CarnivalScrapHeapTests
{
    static object Get(object owner,string name)=>owner.GetType().GetField(name).GetValue(owner);
    static Type Sports(string name)=>Type.GetType("LWS.TruckSports."+name+", Assembly-CSharp",true);
    [Test]
    public void SavedScrapHeapRetainsAllFiveOriginalLaunchesArtworkAndSeparateCrewEntries()
    {
        EditorSceneManager.OpenScene("Assets/TruckSports/Scenes/TruckSports.unity");
        var front=UnityEngine.Object.FindAnyObjectByType(Sports("SportsFrontEnd")) as Component;var pages=(GameObject[])Get(front,"pages");var catalogue=Get(front,"catalogue");var cards=(GameObject[])Get(catalogue,"cards");
        foreach(string key in new[]{"wallball","curling","swing","logrolling","dodgeball"})
        {
            var matches=front.GetComponentsInChildren(Sports("SportsMenuAction"),true).Where(item=>(string)Get(item,"action")==key).ToArray();
            Assert.That(matches.Length,Is.EqualTo(1),key+" must retain its one original launch action.");var action=matches[0];Assert.That(action.transform.IsChildOf(pages[4].transform),Is.True);
            Assert.That(cards.Any(card=>action.transform.IsChildOf(card.transform)),Is.False,"Retired sport must not remain in active catalogue: "+key);
            var root=action.transform.parent;foreach(string name in new[]{"Scrap sport logo","Scrap gameplay preview"})
            {var image=root.Find(name).GetComponent<Image>();Assert.That(image.sprite,Is.Not.Null,key+" "+name);Assert.That(image.preserveAspect,Is.True);}
        }
        var crew=front.GetComponentsInChildren(Sports("SportsMenuAction"),true).Where(item=>((string)Get(item,"action")).StartsWith("peoplecustom",StringComparison.Ordinal)).ToArray();
        Assert.That(crew.Length,Is.EqualTo(4));Assert.That(crew.All(item=>!item.transform.IsChildOf(pages[4].transform)),Is.True,"Scrap upgrade must preserve Your Crew integration.");
    }
}
#endif

using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using LWS.TruckGuessThat;
public sealed class GuessCatalogTests
{
    static object Get(object o,string name)=>o.GetType().GetField(name).GetValue(o);
    [Test]public void All24IdentitiesResolveToRealCompatibleCatalogGeometryAndFourActualWheelMounts()
    {
        var catalogType=Type.GetType("LWS.TruckSports.SportsTruckAppearanceCatalog, Assembly-CSharp",true);var catalog=AssetDatabase.LoadAssetAtPath("Assets/TruckSports/Resources/SportsTruckAppearanceCatalog.asset",catalogType);Assert.That(catalog,Is.Not.Null);
        var author=Type.GetType("LWS.TruckGuessThat.Editor.GuessAuthoring, Assembly-CSharp-Editor",true);var roster=(GuessIdentity[])author.GetMethod("Roster").Invoke(null,new object[]{catalog});Assert.That(roster.Length,Is.EqualTo(24));Assert.That(roster.Select(i=>i.body+"/"+i.hat).Distinct().Count(),Is.EqualTo(24));
        foreach(var identity in roster){var body=catalogType.GetMethod("FindBody").Invoke(catalog,new object[]{identity.body});Assert.That(body,Is.Not.Null);Assert.That(((Array)Get(body,"wheelMounts")).Length,Is.EqualTo(4));var prefab=(GameObject)Get(body,"visualPrefab");Assert.That(prefab.GetComponentsInChildren<Renderer>(true).Length,Is.GreaterThan(0));string[] slots={"hat","face.eyes","face.mouth","mirrors","wheels"};string[] values={identity.hat,identity.eyes,identity.mouth,identity.mirrors,identity.wheels};for(int i=0;i<slots.Length;i++){var cosmetic=catalogType.GetMethod("FindCosmetic").Invoke(catalog,new object[]{values[i],slots[i],identity.body});Assert.That(cosmetic,Is.Not.Null,values[i]);Assert.That(((GameObject)Get(cosmetic,"visualPrefab")).GetComponentsInChildren<Renderer>(true).Length,Is.GreaterThan(0));}for(int family=0;family<6;family++)Assert.That(identity.TraitLabel(family),Is.Not.Empty);}
    }
}

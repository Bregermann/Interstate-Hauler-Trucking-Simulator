#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace LWS.TruckTornadoRodeo.Tests
{
    public sealed class TornadoStormCatalogTests
    {
        static object Get(object value, string member) => value.GetType().GetField(member) is FieldInfo field ? field.GetValue(value) : value.GetType().GetProperty(member).GetValue(value);
        [Test] public void CurrentActualCatalogOffersFiveSevereProfilesWithoutCatalogOrPreferenceWrites()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ScriptableObject>("Assets/TruckSports/Settings/SportsWeatherPresets.asset"); Assert.That(catalog, Is.Not.Null);
            string before = JsonUtility.ToJson(catalog), selection = (string)Get(catalog, "Selection");
            var policy = Type.GetType("LWS.TruckTornadoRodeo.TornadoStormPolicy, Assembly-CSharp", true); var suitable = policy.GetMethod("Suitable");
            var actual = ((IEnumerable)Get(catalog, "entries")).Cast<object>().Where(entry =>
            {
                var profile = (UnityEngine.Object)Get(entry, "profile"); if (profile == null) return false; var rain = Get(profile, "PrecipitationProfile"); var wind = Get(profile, "WindProfile"); string precipitation = rain != null ? Get(rain, "PrecipitationType").ToString() : "None";
                float maximum = wind != null ? (float)Get(Get(wind, "WindSpeedRange"), "Maximum") : 0;
                return (bool)suitable.Invoke(null, new object[] { profile.name, Get(profile, "Disabled"), precipitation == "Rain", precipitation == "Snow", Get(profile, "LightningProfile") != null, Get(entry, "legacyKind").ToString() == "Hurricane", maximum });
            }).ToArray();
            Assert.That(actual.Select(entry => (string)Get(entry, "displayName")), Is.EquivalentTo(new[] { "Thunderstorm", "Hurricane", "Blizzard", "Heavy Rain", "Heavy Snow" }));
            var ids = actual.Select(entry => (string)Get(entry, "id")).ToArray(); string chosen = (string)policy.GetMethod("Choose").Invoke(null, new object[] { "catalog-readonly-check", ids }); Assert.That(ids, Does.Contain(chosen));
            Assert.That(JsonUtility.ToJson(catalog), Is.EqualTo(before)); Assert.That((string)Get(catalog, "Selection"), Is.EqualTo(selection));
        }
    }
}
#endif

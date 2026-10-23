#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public sealed class TimeOfDayPreferenceTests
{
    static Type Runtime(string name) => Type.GetType("LWS.TruckSports." + name + ", Assembly-CSharp", true);
    static readonly Type Time = Runtime("SportsTimeOfDay");
    static readonly Type Choice = Runtime("SportsTimeChoice");
    static object Value(string name) => Enum.Parse(Choice, name);
    static object Call(string name, params object[] arguments) => Time.GetMethod(name).Invoke(null, arguments);

    [Test]
    public void TimePreferenceIsIndependentOfWeatherAndSurvivesFreshReads()
    {
        using ((IDisposable)Runtime("SportsPrefs").GetMethod("IsolateValidation").Invoke(null, new object[] { "TimePreference" }))
        {
            var prefs = Runtime("SportsPrefs");
            prefs.GetMethod("SetString").Invoke(null, new object[] { "TruckSports.Weather.Preset.v1", "unchanged-weather-id" });
            foreach (string name in Enum.GetNames(Choice))
            {
                Call("Select", Value(name));
                Assert.That(Time.GetProperty("Selection").GetValue(null).ToString(), Is.EqualTo(name));
                Assert.That(prefs.GetMethod("GetInt").Invoke(null, new object[] { "TruckSports.Environment.TimeOfDay.v1", -1 }), Is.EqualTo((int)Value(name)));
                Assert.That(prefs.GetMethod("GetString").Invoke(null, new object[] { "TruckSports.Weather.Preset.v1", "" }), Is.EqualTo("unchanged-weather-id"));
            }
            prefs.GetMethod("DeleteKey").Invoke(null, new object[] { "TruckSports.Environment.TimeOfDay.v1" });
            Assert.That(Time.GetProperty("Selection").GetValue(null).ToString(), Is.EqualTo("Day"), "Existing installations retain daylight by default.");
        }
    }

    [Test]
    public void RandomCoversEveryConcreteTimeWithoutConsumingGameplayRandomState()
    {
        using ((IDisposable)Runtime("SportsPrefs").GetMethod("IsolateValidation").Invoke(null, new object[] { "TimeRandom" }))
        {
            Call("Select", Value("Random")); var state = JsonUtility.ToJson(UnityEngine.Random.state); var found = new HashSet<string>();
            for (int seed = 0; seed < 256; seed++)
            {
                var resolved = Call("Resolve", seed); Assert.That(Call("Resolve", seed), Is.EqualTo(resolved));
                Assert.That(resolved.ToString(), Is.Not.EqualTo("Random")); found.Add(resolved.ToString());
                Assert.That((float)Call("Seconds", resolved), Is.InRange(0, 86400));
            }
            Assert.That(found.Count, Is.EqualTo(4)); Assert.That(JsonUtility.ToJson(UnityEngine.Random.state), Is.EqualTo(state));
        }
    }

    [Test]
    public void InvalidStoredTimeFallsBackAndInvalidPublicChoiceIsRejected()
    {
        using ((IDisposable)Runtime("SportsPrefs").GetMethod("IsolateValidation").Invoke(null, new object[] { "TimeInvalid" }))
        {
            Runtime("SportsPrefs").GetMethod("SetInt").Invoke(null, new object[] { "TruckSports.Environment.TimeOfDay.v1", 999 });
            Assert.That(Time.GetProperty("Selection").GetValue(null).ToString(), Is.EqualTo("Day"));
            var error = Assert.Throws<System.Reflection.TargetInvocationException>(() => Call("Select", Enum.ToObject(Choice, 999)));
            Assert.That(error.InnerException, Is.TypeOf<ArgumentOutOfRangeException>());
            Assert.Throws<System.Reflection.TargetInvocationException>(() => Call("Seconds", Value("Random")));
        }
    }
}
#endif

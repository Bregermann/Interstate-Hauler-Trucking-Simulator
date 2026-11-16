using System;
using System.Reflection;
using LWS.TruckSports;
using NUnit.Framework;
public sealed class SportsProfileStatisticsPersistenceTests
{
    const BindingFlags Any = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    static Type T(string name) => Type.GetType("LWS.TruckSports." + name + ", Assembly-CSharp", true);
    static object Registry(string name, params object[] args) => T("SportsParticipantRegistry").GetMethod(name, Any).Invoke(null, args);
    static object Field(object o, string name) => o.GetType().GetField(name, Any).GetValue(o);
    IDisposable scope;
    [SetUp] public void Setup()
    {
        scope = (IDisposable)T("SportsPrefs").GetMethod("IsolateValidation").Invoke(null, new object[] { "FoundationStats" + Guid.NewGuid().ToString("N") });
        Registry("ForgetCache");
    }
    [TearDown] public void Cleanup() { Registry("ForgetCache"); scope?.Dispose(); }
    [Test] public void FinalStatisticsPersistExactlyOnceByIdAndExcludeAiAndGuests()
    {
        var first = Registry("Create", "Same name"); var second = Registry("Create", "Same name"); var bot = Registry("Default", 4);
        string a = (string)Field(first, "id"), b = (string)Field(second, "id"), ai = (string)Field(bot, "id");
        Type rowType = T("SportsResultRow"); var rows = Array.CreateInstance(rowType, 4);
        rows.SetValue(Row(a, false, true), 0); rows.SetValue(Row(b, false, false, 1), 1); rows.SetValue(Row(ai, true, true, 2), 2); rows.SetValue(Row("guest:test", false, true, 3), 3);
        var snapshot = Activator.CreateInstance(T("SportsResultsSnapshot"), "Final", "Actual contract fixture", rows, SportsMatchKind.Competitive);
        object[] args = { "night", "match", "TruckDanceOff", "song0", snapshot, new[] { a, b, "guest:test" }, 60d, "TRUCK DANCE-OFF", "FIRST SONG" };
        Assert.That(Registry("RecordFinalStatistics", args), Is.EqualTo(true));
        Assert.That(Registry("RecordFinalStatistics", args), Is.EqualTo(true));
        Registry("ForgetCache");
        var winner = (SportsProfileStatistics)Field(Registry("Find", a), "statistics");
        var loser = (SportsProfileStatistics)Field(Registry("Find", b), "statistics");
        var aiStats = (SportsProfileStatistics)Field(Registry("Find", ai), "statistics");
        Assert.That(winner.matches, Is.EqualTo(1)); Assert.That(winner.winnerPoints, Is.EqualTo(1)); Assert.That(winner.sessionIds.Count, Is.EqualTo(1));
        Assert.That(loser.losses, Is.EqualTo(1)); Assert.That(loser.winnerPoints, Is.Zero); Assert.That(aiStats.matches, Is.Zero);
        Assert.That(Registry("Find", "guest:test"), Is.Null); Assert.That(winner.sports[0].title, Is.EqualTo("TRUCK DANCE-OFF"));
    }
    [Test] public void VersionThreeProfilesMigrateWithoutInventingHistoricalStatistics()
    {
        string json = "{\"version\":3,\"participants\":[{\"id\":\"old-driver\",\"displayName\":\"Old driver\"}],\"roster\":[\"old-driver\"],\"legacy\":[],\"results\":[]}";
        string key = (string)T("SportsParticipantRegistry").GetField("Key").GetRawConstantValue();
        T("SportsPrefs").GetMethod("SetString").Invoke(null, new object[] { key, json }); Registry("ForgetCache");
        var profile = Registry("Find", "old-driver"); Assert.That(profile, Is.Not.Null);
        var stats = (SportsProfileStatistics)Field(profile, "statistics"); Assert.That(stats, Is.Not.Null); Assert.That(stats.matches, Is.Zero); Assert.That(stats.winnerPoints, Is.Zero);
        Assert.That(Field(T("SportsParticipantRegistry").GetProperty("Current").GetValue(null), "version"), Is.EqualTo(4));
    }
    static object Row(string id, bool ai, bool winner, int slot = 0)
    {
        var player = Activator.CreateInstance(Type.GetType("LWS.TruckBowling.BowlingPlayer, Assembly-CSharp", true));
        player.GetType().GetField("participantId").SetValue(player, id); player.GetType().GetField("name").SetValue(player, "Same name");
        var source = player.GetType().GetField("source"); source.SetValue(player, Enum.Parse(source.FieldType, ai ? "AI" : "Keyboard"));
        return Activator.CreateInstance(T("SportsResultRow"), player, slot, winner ? 1 : 2, winner, 100d, "PTS", "Fixture", "—", "0", SportsRecordPreference.Higher);
    }
}

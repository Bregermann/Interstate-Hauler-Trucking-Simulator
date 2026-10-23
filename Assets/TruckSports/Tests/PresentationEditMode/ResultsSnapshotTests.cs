#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class ResultsSnapshotTests
{
    static Type Runtime(string name) => Type.GetType("LWS.TruckSports." + name + ", Assembly-CSharp", true);
    static object Field(object owner, string name) => owner.GetType().GetField(name).GetValue(owner);
    static object Player(string id)
    {
        var player = Activator.CreateInstance(Type.GetType("LWS.TruckBowling.BowlingPlayer, Assembly-CSharp", true));
        player.GetType().GetField("participantId").SetValue(player, id); player.GetType().GetField("name").SetValue(player, "Same Name"); player.GetType().GetField("color").SetValue(player, Color.cyan); return player;
    }
    static object Row(string id, int slot, int place, bool winner, double? value = 12.125, string fallback = "DNF") => Activator.CreateInstance(Runtime("SportsResultRow"), Player(id), slot, place, winner, value, "SEC", "ACTUAL DETAIL", fallback, "0.000");
    static Array Rows(params object[] rows) { var result = Array.CreateInstance(Runtime("SportsResultRow"), rows.Length); for (int i = 0; i < rows.Length; i++) result.SetValue(rows[i], i); return result; }
    static object Snapshot(Array rows) => Activator.CreateInstance(Runtime("SportsResultsSnapshot"), "REAL RESULTS", "ACTUAL SUMMARY", rows);
    [Test]
    public void AuthoritativeTiesAndDuplicateDisplayNamesKeepSeparateStableIdentities()
    {
        var snapshot = Snapshot(Rows(Row("b", 1, 1, true), Row("c", 2, 3, false), Row("a", 0, 1, true)));
        var rows = ((IEnumerable)snapshot.GetType().GetProperty("Rows").GetValue(snapshot)).Cast<object>().ToArray();
        CollectionAssert.AreEqual(new[] { "a", "b", "c" }, rows.Select(row => Field(row, "ParticipantId")).ToArray());
        CollectionAssert.AreEqual(new[] { 1, 1, 3 }, rows.Select(row => Field(row, "Place")).ToArray());
        Assert.That(rows.Count(row => (bool)Field(row, "Winner")), Is.EqualTo(2));
        Assert.That(Runtime("SportsResultRow").GetFields().All(field => field.IsInitOnly), Is.True);
    }
    [Test]
    public void IncompleteAttemptsHaveNoInventedTimeOrChampion()
    {
        var row = Row("dnf", 0, 0, false, null); Assert.That(row.GetType().GetProperty("Value").GetValue(row), Is.EqualTo("DNF"));
        Assert.That(Field(row, "Measurement"), Is.Null); Assert.That(Field(row, "Winner"), Is.False);
        var measured = Row("finished", 1, 1, true, 42.125); Assert.That(measured.GetType().GetProperty("Value").GetValue(measured), Is.EqualTo("42.125 SEC"));
    }
    [Test]
    public void SnapshotCopiesTheRosterAndSortsUnrankedEntriesLast()
    {
        var source = Rows(Row("dnf", 0, 0, false, null), Row("winner", 1, 1, true)); var snapshot = Snapshot(source); source.SetValue(Row("replacement", 0, 1, true), 0);
        var rows = ((IEnumerable)snapshot.GetType().GetProperty("Rows").GetValue(snapshot)).Cast<object>().ToArray();
        CollectionAssert.AreEqual(new[] { "winner", "dnf" }, rows.Select(row => Field(row, "ParticipantId")).ToArray());
    }
    [Test]
    public void DuplicateIdentitiesOrSlotsCannotCreateAmbiguousResults()
    {
        Assert.That(Assert.Throws<TargetInvocationException>(() => Snapshot(Rows(Row("same", 0, 1, true), Row("same", 1, 1, true)))).InnerException, Is.TypeOf<ArgumentException>());
        Assert.That(Assert.Throws<TargetInvocationException>(() => Snapshot(Rows(Row("a", 0, 1, true), Row("b", 0, 2, false)))).InnerException, Is.TypeOf<ArgumentException>());
        Assert.That(Assert.Throws<TargetInvocationException>(() => Snapshot(Rows())).InnerException, Is.TypeOf<ArgumentException>());
    }
    [TestCase(double.NaN)] [TestCase(double.PositiveInfinity)] [TestCase(double.NegativeInfinity)]
    public void InvalidMeasurementsAreRejected(double value) => Assert.That(Assert.Throws<TargetInvocationException>(() => Row("bad", 0, 1, true, value)).InnerException, Is.TypeOf<ArgumentException>());
    [Test]
    public void WinningStatusCannotBeAssignedToASecondPlaceRow() => Assert.That(Assert.Throws<TargetInvocationException>(() => Row("bad", 0, 2, true)).InnerException, Is.TypeOf<ArgumentException>());
}
#endif

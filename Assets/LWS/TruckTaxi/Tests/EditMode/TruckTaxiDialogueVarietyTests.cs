using System;
using System.Linq;
using System.Text.RegularExpressions;
using LWS.TruckTaxi.Editor;
using NUnit.Framework;
using UnityEngine;

namespace LWS.TruckTaxi.Tests.EditMode
{
    [Category("TaxiIntegrated")]
    public sealed class TruckTaxiDialogueVarietyTests
    {
        [Test]
        public void IntegratedCopyCoversEveryRosterPassengerWithDistinctCommonLines()
        {
            var entries = TruckTaxiDialogueVariety.ReadEntries(TruckTaxiDialogueVariety.ContentPath);
            var events = TruckTaxiDialogueVariety.ReadEventEntries(TruckTaxiDialogueVariety.EventContentPath);
            var reactions = TruckTaxiDialogueVariety.ReadReactionEntries(TruckTaxiDialogueVariety.ReactionContentPath);
            var roster = TruckTaxiPassengerDialogueAuthoring.AllPassengers();
            Assert.That(roster.Length, Is.EqualTo(114));
            Assert.That(entries.Length, Is.EqualTo(1368));
            Assert.That(entries.Select(e => e.passengerId).Distinct().Count(), Is.EqualTo(114));
            CollectionAssert.AreEquivalent(roster.Select(p => p.passengerId), entries.Select(e => e.passengerId).Distinct());
            Assert.That(events.Length, Is.EqualTo(684));
            Assert.That(reactions.Length, Is.EqualTo(684));
            CollectionAssert.AreEquivalent(roster.Select(p => p.passengerId), events.Select(e => e.passengerId).Distinct());
            CollectionAssert.AreEquivalent(roster.Select(p => p.passengerId), reactions.Select(e => e.passengerId).Distinct());
            foreach (var group in events.GroupBy(e => e.passengerId))
            {
                Assert.That(group.Count(), Is.EqualTo(6), group.Key);
                Assert.That(group.Select(e => e.category).Distinct().Count(), Is.EqualTo(6), group.Key);
                Assert.That(group.Select(e => e.lineId).Distinct().Count(), Is.EqualTo(6), group.Key);
            }
            foreach (var group in reactions.GroupBy(e => e.passengerId))
            {
                Assert.That(group.Count(), Is.EqualTo(6), group.Key);
                Assert.That(group.Select(e => e.category).Distinct().Count(), Is.EqualTo(6), group.Key);
                Assert.That(group.Select(e => e.lineId).Distinct().Count(), Is.EqualTo(6), group.Key);
            }
            foreach (var group in entries.GroupBy(e => e.passengerId))
            {
                Assert.That(group.Count(e => e.category == "ScenicView"), Is.EqualTo(3), group.Key);
                Assert.That(group.Count(e => e.category == "PickupGreeting"), Is.EqualTo(3), group.Key);
                Assert.That(group.Count(e => e.category == "GeneralChatter"), Is.EqualTo(3), group.Key);
                Assert.That(group.Count(e => e.category == "Arrival"), Is.EqualTo(3), group.Key);
                Assert.That(group.Select(e => e.lineId).Distinct().Count(), Is.EqualTo(group.Count()), group.Key);
            }
            foreach (string category in new[] { "ScenicView", "PickupGreeting", "GeneralChatter", "Arrival" })
            {
                var normalized = entries.Where(e => e.category == category)
                    .Select(e => Regex.Replace(e.text.ToLowerInvariant(), "[^a-z0-9]+", " ").Trim()).ToArray();
                Assert.That(normalized.Distinct().Count(), Is.EqualTo(normalized.Length), category);
            }
            var eventCopy = events.Select(e => Regex.Replace(e.text.ToLowerInvariant(), "[^a-z0-9]+", " ").Trim()).ToArray();
            Assert.That(eventCopy.Distinct().Count(), Is.EqualTo(eventCopy.Length));
            var reactionCopy = reactions.Select(e => Regex.Replace(e.text.ToLowerInvariant(), "[^a-z0-9]+", " ").Trim()).ToArray();
            Assert.That(reactionCopy.Distinct().Count(), Is.EqualTo(reactionCopy.Length));
            foreach (var entry in entries.Concat(events).Concat(reactions))
                Assert.That(Enum.TryParse(entry.category, false, out TruckTaxiDialogueCategory _), Is.True,
                    entry.passengerId + "/" + entry.lineId + ": " + entry.category);
        }

        [Test]
        public void ValidatorFlagsSharedLongCopyAndMissingCoverage()
        {
            var first = ScriptableObject.CreateInstance<PassengerProfile>();
            var second = ScriptableObject.CreateInstance<PassengerProfile>();
            var firstSet = ScriptableObject.CreateInstance<TruckTaxiDialogueSet>();
            var secondSet = ScriptableObject.CreateInstance<TruckTaxiDialogueSet>();
            try
            {
                first.passengerId = "first";
                second.passengerId = "second";
                first.authoredDialogue = firstSet;
                second.authoredDialogue = secondSet;
                const string shared = "This long sentence is deliberately identical between both passengers.";
                firstSet.lines = new[] { new TruckTaxiDialogueLine { category = TruckTaxiDialogueCategory.PickupGreeting, text = shared } };
                secondSet.lines = new[] { new TruckTaxiDialogueLine { category = TruckTaxiDialogueCategory.PickupGreeting, text = shared } };
                var issues = TruckTaxiDialogueVariety.ValidateRoster(new[] { first, second });
                Assert.That(issues.Any(issue => issue.Contains("duplicate long line")), Is.True);
                Assert.That(issues.Any(issue => issue.Contains("missing Boarding")), Is.True);
                Assert.That(issues.Any(issue => issue.Contains("fallback-heavy")), Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(firstSet);
                UnityEngine.Object.DestroyImmediate(secondSet);
                UnityEngine.Object.DestroyImmediate(first);
                UnityEngine.Object.DestroyImmediate(second);
            }
        }
    }
}

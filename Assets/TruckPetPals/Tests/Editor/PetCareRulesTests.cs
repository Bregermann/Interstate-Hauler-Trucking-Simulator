using System;
using NUnit.Framework;
using LWS.TruckPetPals;

public sealed class PetCareRulesTests
{
    [Test] public void ActualPerformanceAndForgivingTimingAreBothRequiredForEveryTrick()
    {
        var pet = new PetRecord { id = "pet.a", profileId = "person.a" };
        foreach (PetTrick trick in Enum.GetValues(typeof(PetTrick)))
        {
            Assert.That(pet.Learn(trick, false, true), Is.False); Assert.That(pet.Learn(trick, true, false), Is.False);
            Assert.That(pet.familiarity[(int)trick], Is.Zero); Assert.That(pet.Learn(trick, true, true), Is.True);
            Assert.That(pet.familiarity[(int)trick], Is.EqualTo(12));
        }
        Assert.That(pet.affection, Is.GreaterThan(35));
    }
    [Test] public void BathNeedsWaterSoapAlternatingActualScrubRinseAndDryBeforeCompletion()
    {
        var pet = new PetRecord { cleanliness = 20 }; var bath = new PetBathRules(); bath.Begin();
        Assert.That(bath.Confirm(pet), Is.False);
        for (int stage = 0; stage < 5; stage++)
        {
            Assert.That((int)bath.Step, Is.EqualTo(stage));
            if (bath.Step == PetBathStep.Scrub)
            {
                for (int i = 0; i < 100; i++) bath.Stroke(1, false, .02f);
                Assert.That(bath.Progress, Is.LessThan(.2f), "A held direction cannot fake repeated scrub strokes.");
                for (int i = 0; i < 8; i++) bath.Stroke(i % 2 == 0 ? -1 : 1, false, .02f);
            }
            else for (int i = 0; i < 101; i++) bath.Stroke(0, true, .02f);
            Assert.That(bath.Confirm(pet), Is.True);
        }
        Assert.That(bath.Complete, Is.True); Assert.That(pet.cleanliness, Is.EqualTo(100)); Assert.That(pet.baths, Is.EqualTo(1)); Assert.That(bath.Confirm(pet), Is.False);
    }
    [Test] public void ForgivingNeedsNeverStarveOrPunishAndSleepActuallyRestoresEnergy()
    {
        var pet = new PetRecord { hunger = 31, energy = 21, cleanliness = 16 };
        for (int i = 0; i < 100000; i++) pet.Tick(.02f, true, true, false);
        Assert.That(pet.hunger, Is.EqualTo(30)); Assert.That(pet.energy, Is.EqualTo(20)); Assert.That(pet.cleanliness, Is.EqualTo(15));
        for (int i = 0; i < 1000; i++) pet.Tick(.02f, false, false, true);
        Assert.That(pet.energy, Is.GreaterThan(32)); pet.Feed(); Assert.That(pet.hunger, Is.EqualTo(60));
        float age = pet.ageSeconds; pet.Tick(float.NaN, true, true, false); Assert.That(pet.ageSeconds, Is.EqualTo(age));
    }
    [Test] public void StablePetAndProfileIdsAllowDuplicateNamesAndRejectCrossOwnershipOrFutureData()
    {
        var a = new PetRecord { id = "pet.a", profileId = "person.a", name = "Pip" };
        var b = new PetRecord { id = "pet.b", profileId = "person.b", name = "Pip", kind = PetKind.Dog };
        var data = new PetArchive { version = 1, profiles = new[] { new PetProfileRecord { profileId = "person.a", selectedPetId = a.id, pets = new[] { a } }, new PetProfileRecord { profileId = "person.b", selectedPetId = b.id, pets = new[] { b } } } };
        Assert.That(PetArchive.Valid(data, out _), Is.True); a.name = "Miso"; Assert.That(PetArchive.Valid(data, out _), Is.True);
        b.id = a.id; Assert.That(PetArchive.Valid(data, out _), Is.False); b.id = "pet.b";
        b.profileId = "person.a"; Assert.That(PetArchive.Valid(data, out _), Is.False); b.profileId = "person.b";
        data.version = 999; Assert.That(PetArchive.Valid(data, out _), Is.False);
    }
}

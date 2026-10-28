using NUnit.Framework;
using LWS.TruckCamping;
public sealed class CampingRulesTests
{
    [Test]public void FireRequiresActualCollectedSuppliesThenCookingConsumesIngredientAndProducesMeals()
    {
        var journal=new CampJournal("profile.a");var fire=new CampfireState{id="camp.a"};int meals=journal.supplies[(int)CampSupply.Meals];
        Assert.That(fire.Build(journal),Is.False);journal.Add(CampSupply.Sticks,3);journal.Add(CampSupply.Firewood,1);Assert.That(fire.Build(journal),Is.False);journal.Add(CampSupply.Firewood,1);Assert.That(fire.Build(journal),Is.True);
        Assert.That(journal.supplies[(int)CampSupply.Sticks],Is.Zero);Assert.That(journal.supplies[(int)CampSupply.Firewood],Is.Zero);Assert.That(fire.Cook(journal),Is.False);
        journal.Add(CampSupply.Fish,1);Assert.That(fire.Cook(journal),Is.True);Assert.That(journal.supplies[(int)CampSupply.Fish],Is.Zero);Assert.That(journal.supplies[(int)CampSupply.Meals],Is.EqualTo(meals+2));Assert.That(journal.mealsCooked,Is.EqualTo(1));Assert.That(fire.meals,Is.EqualTo(1));
        for(int i=0;i<1200;i++)fire.Tick(.2f);Assert.That(fire.Burning,Is.False);Assert.That(fire.Cook(journal),Is.False);
    }
    [Test]public void ForgivingSurvivalAndUniqueDiscoveriesNeverTurnIntoForcedWinnerOrPunishment()
    {
        var journal=new CampJournal("profile.a");for(int i=0;i<100000;i++)journal.Tick(.2f,true,true,false);Assert.That(journal.hunger,Is.EqualTo(25));Assert.That(journal.energy,Is.EqualTo(20));
        Assert.That(journal.Discover(3),Is.True);Assert.That(journal.Discover(3),Is.False);Assert.That(journal.Discover(30),Is.False);
        for(int i=0;i<100;i++)journal.Tick(.2f,true,false,true);Assert.That(journal.energy,Is.GreaterThan(28));Assert.That(journal.slept,Is.GreaterThan(19));Assert.That(journal.Eat(),Is.True);Assert.That(CampJournal.Valid(journal),Is.True);
    }
    [Test]public void InvalidOrUnknownJournalDataIsRejectedInsteadOfResettingLegitimateState()
    {
        var journal=new CampJournal("profile.a");Assert.That(CampJournal.Valid(journal),Is.True);journal.energy=float.NaN;Assert.That(CampJournal.Valid(journal),Is.False);journal.energy=50;journal.supplies=new[]{1,2};Assert.That(CampJournal.Valid(journal),Is.False);
        Assert.That(new CampJournal("profile.a").Add((CampSupply)99,1),Is.False);Assert.That(new CampJournal("profile.a").Consume(CampSupply.Meals,-1),Is.False);
    }
}

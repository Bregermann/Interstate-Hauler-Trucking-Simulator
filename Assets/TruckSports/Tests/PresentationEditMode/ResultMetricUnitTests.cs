using System;
using NUnit.Framework;
public sealed class ResultMetricUnitTests
{
    static Type Runtime(string name)=>Type.GetType("LWS.TruckSports."+name+", Assembly-CSharp",true);
    [TestCase("WINS","WIN")][TestCase("TEAM WINS","TEAM WIN")][TestCase("TEAM DELIVERIES","TEAM DELIVERY")]
    [TestCase("CUPS HIT","CUP HIT")][TestCase("TEAM CUPS HIT","TEAM CUP HIT")][TestCase("FARES","FARE")]
    public void OneCountUsesReadableSingularWithoutChangingAuthoritativeData(string raw,string expected)
    {
        var player=Activator.CreateInstance(Type.GetType("LWS.TruckBowling.BowlingPlayer, Assembly-CSharp",true));player.GetType().GetField("participantId").SetValue(player,"actual-stable-id");
        var row=Activator.CreateInstance(Runtime("SportsResultRow"),player,0,0,false,(double?)1,raw,"DETAIL","DNF","0");
        Assert.That(row.GetType().GetField("Unit").GetValue(row),Is.EqualTo(raw));Assert.That(row.GetType().GetField("Measurement").GetValue(row),Is.EqualTo(1d));
        Assert.That(row.GetType().GetProperty("Value").GetValue(row),Is.EqualTo("1 "+expected));
    }
    [TestCase(0)][TestCase(2)][TestCase(1.5)]
    public void NonSingularCountsRetainOriginalUnit(double amount)
    {
        var player=Activator.CreateInstance(Type.GetType("LWS.TruckBowling.BowlingPlayer, Assembly-CSharp",true));player.GetType().GetField("participantId").SetValue(player,"stable");
        var row=Activator.CreateInstance(Runtime("SportsResultRow"),player,0,0,false,(double?)amount,"WINS","DETAIL","DNF","0.###");
        Assert.That(row.GetType().GetProperty("Value").GetValue(row),Does.EndWith(" WINS"));
    }
    [TestCase("PTS")][TestCase("MPH")][TestCase("SEC")][TestCase("LB")][TestCase("SHEEP")][TestCase("UNRECOGNIZED")]
    public void UnitsAndUnknownTermsAreNotArbitrarilySingularized(string unit)
    {
        var player=Activator.CreateInstance(Type.GetType("LWS.TruckBowling.BowlingPlayer, Assembly-CSharp",true));player.GetType().GetField("participantId").SetValue(player,"stable");
        var row=Activator.CreateInstance(Runtime("SportsResultRow"),player,0,0,false,(double?)1,unit,"DETAIL","DNF","0");
        Assert.That(row.GetType().GetProperty("Value").GetValue(row),Is.EqualTo("1 "+unit));
    }
}

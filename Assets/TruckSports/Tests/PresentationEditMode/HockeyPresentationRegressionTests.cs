#if UNITY_EDITOR
using System;
using NUnit.Framework;

public sealed class HockeyPresentationRegressionTests
{
    [Test]
    public void ExistingHockeyGoalClockOvertimeAndDeadlineRulesStillPass()
    {
        Type.GetType("LWS.TruckHockey.Editor.HockeyRuleFixtures, Assembly-CSharp-Editor",true).GetMethod("Run").Invoke(null,null);
    }
    [Test]
    public void ExistingSixSlotKeyboardWholePadAndSharedStickOwnershipStillPass()
    {
        Type.GetType("LWS.TruckHockey.Editor.HockeyParticipantFixtures, Assembly-CSharp-Editor",true).GetMethod("Run").Invoke(null,null);
    }
}
#endif

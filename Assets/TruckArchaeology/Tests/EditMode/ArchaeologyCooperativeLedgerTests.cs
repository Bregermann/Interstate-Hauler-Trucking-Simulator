using System.Linq;
using NUnit.Framework;
using LWS.TruckArchaeology;
public sealed class ArchaeologyCooperativeLedgerTests
{
 [Test]public void SharedFindsCreditActualDeliveringOwnerWhileTeamPresentationAggregatesAndDuplicateCannotScore()
 {
  var rules=new ArchaeologyRules(4,true);
  Assert.That(rules.Scan(3,0,true),Is.True);Assert.That(rules.Expose(3,true),Is.True);Assert.That(rules.Lift(3,2,true),Is.True);Assert.That(rules.Load(3,2,true),Is.True);rules.Damage(3,.125);Assert.That(rules.Deliver(3,2,true,140),Is.EqualTo(123));
  Assert.That(rules.Scan(0,3,true),Is.True);Assert.That(rules.Expose(0,true),Is.True);Assert.That(rules.Lift(0,1,true),Is.True);Assert.That(rules.Load(0,1,true),Is.True);rules.Damage(0,.2);Assert.That(rules.Deliver(0,1,true,180),Is.EqualTo(144));
  CollectionAssert.AreEqual(new[]{0,144,123,0},rules.Scores,"Cooperation changes access, not the actual delivering participant's ledger; the team view uses Sum");Assert.That(rules.Scores.Sum(),Is.EqualTo(267));Assert.That(rules.Owners[3],Is.EqualTo(2));Assert.That(rules.Owners[0],Is.EqualTo(1));Assert.That(rules.Deliver(3,2,true,140),Is.EqualTo(-1));Assert.That(rules.Scores.Sum(),Is.EqualTo(267));
 }
}

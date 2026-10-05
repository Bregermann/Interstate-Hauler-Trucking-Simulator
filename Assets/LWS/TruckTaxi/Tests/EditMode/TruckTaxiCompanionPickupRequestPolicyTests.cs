using NUnit.Framework;

namespace LWS.TruckTaxi.Tests
{
    [Category("TaxiCompanionPickupRequest")]
    public sealed class TruckTaxiCompanionPickupRequestPolicyTests
    {
        [TestCase(false, true)]
        [TestCase(true, false)]
        public void OnlyExplicitEligibilityAndSafeAvailableTargetPermitARequest(bool eligible, bool available)
        {
            var policy = new TruckTaxiCompanionPickupRequestPolicy();
            Assert.IsFalse(policy.TryOffer("fare", "actual.actor", eligible, 1, 0, available));
            Assert.IsTrue(policy.CanEvaluate("fare"), "Temporary capacity failure does not roll a chance.");
        }

        [Test]
        public void FifteenPercentAppliesOnlyToEligibleFaresAndDoesNotReroll()
        {
            int offers = 0;
            var policy = new TruckTaxiCompanionPickupRequestPolicy();
            for (int i = 0; i < 1000; i++)
            {
                string fare = "eligible." + i;
                if (policy.TryOffer(fare, "actual.actor", true, .15f, (i + .5d) / 1000d, true))
                { offers++; Assert.IsTrue(policy.Decline()); }
                Assert.IsFalse(policy.TryOffer(fare, "another.actor", true, 1, 0, true));
            }
            Assert.AreEqual(150, offers);
        }

        [Test]
        public void DeclineOrCancellationConsumesOnlyOneRequestForThatFare()
        {
            var policy = new TruckTaxiCompanionPickupRequestPolicy();
            Assert.IsTrue(policy.TryOffer("fare.1", "actor.1", true, 1, 0, true));
            Assert.IsTrue(policy.Decline());
            Assert.IsFalse(policy.TryOffer("fare.1", "actor.2", true, 1, 0, true));
            Assert.IsTrue(policy.TryOffer("fare.2", "actor.2", true, 1, 0, true));
            Assert.IsTrue(policy.Accept());
            Assert.IsTrue(policy.Cancel());
            Assert.IsFalse(policy.TryOffer("fare.2", "actor.3", true, 1, 0, true));
        }

        [Test]
        public void CompletionRequiresAcceptedMatchingFareAndActorAndIsExactlyOnce()
        {
            var policy = new TruckTaxiCompanionPickupRequestPolicy();
            Assert.IsTrue(policy.TryOffer("fare", "actor", true, 1, 0, true));
            Assert.IsFalse(policy.Complete("fare", "actor"));
            Assert.IsTrue(policy.Accept());
            Assert.IsFalse(policy.Complete("another fare", "actor"));
            Assert.IsFalse(policy.Complete("fare", "another actor"));
            Assert.IsTrue(policy.Complete("fare", "actor"));
            Assert.IsFalse(policy.Complete("fare", "actor"));
            Assert.IsFalse(policy.Cancel());
        }

        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(float.NegativeInfinity)]
        public void NonfiniteChanceNeverOffers(float chance)
        {
            Assert.IsFalse(new TruckTaxiCompanionPickupRequestPolicy().TryOffer("fare", "actor", true, chance, 0, true));
        }
    }
}

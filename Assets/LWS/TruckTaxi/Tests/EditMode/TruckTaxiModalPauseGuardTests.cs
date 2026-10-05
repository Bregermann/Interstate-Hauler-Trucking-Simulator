using NUnit.Framework;

namespace LWS.TruckTaxi.Tests
{
    public sealed class TruckTaxiModalPauseGuardTests
    {
        [Test] public void AcquisitionRequiresUnpausedUnblockedExclusiveOwnership()
        {
            var guard = new TruckTaxiModalPauseGuard(); var owner = new object();
            Assert.That(guard.TryAcquire(owner, "ride", true, false, 1), Is.Zero);
            Assert.That(guard.TryAcquire(owner, "ride", false, true, 1), Is.Zero);
            Assert.That(guard.TryAcquire(null, "ride", false, false, 1), Is.Zero);
            Assert.That(guard.TryAcquire(owner, "ride", false, false, 1), Is.GreaterThan(0));
            Assert.That(guard.TryAcquire(new object(), "ride", false, false, 1), Is.Zero);
            Assert.That(guard.IsOwnedBy(owner), Is.True);
        }
        [Test] public void NormalReleaseResumesExactlyOnce()
        {
            var guard = new TruckTaxiModalPauseGuard(); var owner = new object();
            long token = guard.TryAcquire(owner, "ride", false, false, 1);
            Assert.That(guard.Release(owner, token, "ride", true, false, 2), Is.True);
            Assert.That(guard.Release(owner, token, "ride", true, false, 2), Is.False);
            Assert.That(guard.IsHeld, Is.False);
        }
        [Test] public void WrongOwnerOrStaleTokenCannotReleaseANewModal()
        {
            var guard = new TruckTaxiModalPauseGuard(); var owner = new object();
            long old = guard.TryAcquire(owner, "ride", false, false, 1);
            Assert.That(guard.Release(new object(), old, "ride", true, false, 2), Is.False);
            Assert.That(guard.IsHeld, Is.True);
            guard.Release(owner, old, "ride", true, false, 2);
            long current = guard.TryAcquire(owner, "ride", false, false, 3);
            Assert.That(guard.Release(owner, old, "ride", true, false, 4), Is.False);
            Assert.That(guard.IsHeld, Is.True);
            Assert.That(guard.Release(owner, current, "ride", true, false, 4), Is.True);
        }
        [TestCase("different ride", true, false)]
        [TestCase("ride", true, true)]
        [TestCase("ride", false, false)]
        public void InterruptedOwnershipNeverUnpausesTheNewOperation(string context, bool paused, bool blocked)
        {
            var guard = new TruckTaxiModalPauseGuard(); var owner = new object();
            long token = guard.TryAcquire(owner, "ride", false, false, 1);
            guard.Observe(context, paused, blocked);
            Assert.That(guard.Release(owner, token, "ride", true, false, 3), Is.False,
                "Interruption remains sticky even if the foreign operation closed before this lease.");
            Assert.That(guard.IsHeld, Is.False);
        }
        [Test] public void TeardownInvalidationPreservesHostPause()
        {
            var guard = new TruckTaxiModalPauseGuard(); var owner = new object();
            long token = guard.TryAcquire(owner, "ride", false, false, 1);
            guard.InvalidateResume();
            Assert.That(guard.Release(owner, token, "ride", true, false, 2), Is.False);
        }
        [Test] public void SubmitIsDispatchedOnceAndCannotFallThroughAfterClosing()
        {
            var guard = new TruckTaxiModalPauseGuard(); var owner = new object();
            Assert.That(guard.Dispatch(10), Is.Zero);
            long token = guard.TryAcquire(owner, "ride", false, false, 10);
            Assert.That(guard.Dispatch(10), Is.EqualTo(-1), "Opening input must not also accept the request.");
            Assert.That(guard.Dispatch(11), Is.EqualTo(1));
            Assert.That(guard.Dispatch(11), Is.EqualTo(-1), "HUD dispatch is idempotent within a frame.");
            guard.Release(owner, token, "ride", true, false, 11);
            Assert.That(guard.Dispatch(11), Is.EqualTo(-1), "Do not forward the same submit/cancel to driving or another modal.");
            Assert.That(guard.Dispatch(12), Is.Zero);
        }
        [Test] public void ReplacingAClosedLeaseInSameFrameStillConsumesOpeningInput()
        {
            var guard = new TruckTaxiModalPauseGuard(); var owner = new object();
            long token = guard.TryAcquire(owner, "ride", false, false, 1);
            guard.Release(owner, token, "ride", true, false, 2);
            guard.TryAcquire(new object(), "ride", false, false, 2);
            Assert.That(guard.Dispatch(2), Is.EqualTo(-1));
            Assert.That(guard.Dispatch(3), Is.EqualTo(1));
        }
    }
}

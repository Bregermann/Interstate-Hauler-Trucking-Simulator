using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace LWS.TruckTaxi.Tests
{
    public sealed class TruckTaxiSoakProbeTests
    {
        private static TruckTaxiDrivingPerformanceProbe.SoakCourse Course() =>
            new TruckTaxiDrivingPerformanceProbe.SoakCourse(new[] {
                Vector3.zero, new Vector3(10,0,0), new Vector3(10,0,10), new Vector3(0,0,10), Vector3.zero
            }, 0);

        [Test]
        public void ProgressRequiresOrderedLoopAndNeverNearestPointReselection()
        {
            var course = Course();
            course.Observe(Vector3.zero); course.Observe(Vector3.zero);
            Assert.AreEqual(0, course.ProgressMeters); Assert.AreEqual(0, course.Laps);
            Assert.Throws<InvalidOperationException>(() => course.Observe(new Vector3(0,0,10)));
            course.Observe(new Vector3(10,0,0)); course.Observe(new Vector3(10,0,10));
            course.Observe(new Vector3(0,0,10)); course.Observe(Vector3.zero);
            Assert.AreEqual(1, course.Laps); Assert.AreEqual(40, course.ProgressMeters);
            Assert.AreEqual(new Vector3(10,0,0), course.Target);
        }

        [Test]
        public void OffRoadAndReverseProgressFailWithoutReanchor()
        {
            var course = Course(); course.Observe(new Vector3(6,0,0));
            Assert.Throws<InvalidOperationException>(() => course.Observe(Vector3.zero));
            Assert.Throws<InvalidOperationException>(() => course.Observe(new Vector3(6,0,5)));
            Assert.AreEqual(0, course.Laps);
        }

        [Test]
        public void ApproachMustBeDrivenBeforeEnteringLoop()
        {
            var course = new TruckTaxiDrivingPerformanceProbe.SoakCourse(new[] {
                new Vector3(-10,0,0), Vector3.zero, new Vector3(10,0,0), new Vector3(10,0,10), Vector3.zero
            }, 1);
            Assert.IsFalse(course.OnLoop);
            course.Observe(Vector3.zero);
            Assert.IsTrue(course.OnLoop); Assert.AreEqual(0, course.Laps);
        }

        [Test]
        public void GlobalMotionSurvivesOriginShiftButLocalDiscontinuityDoesNot()
        {
            var previousGlobal = new Vector3(750,0,0);
            var newLocal = new Vector3(250.2f,0,0);
            var newOffset = new Vector3(500,0,0);
            Assert.DoesNotThrow(() => TruckTaxiDrivingPerformanceProbe.ValidateSoakMotion(previousGlobal,newLocal+newOffset,10,10,.02f));
            Assert.Throws<InvalidOperationException>(() => TruckTaxiDrivingPerformanceProbe.ValidateSoakMotion(previousGlobal,newLocal,10,10,.02f));
        }

        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(1.1f)]
        [TestCase(float.NaN)]
        public void InvalidElapsedTimeCannotProduceDrivingEvidence(float seconds)
        {
            Assert.Throws<InvalidOperationException>(() => TruckTaxiDrivingPerformanceProbe.ValidateSoakMotion(Vector3.zero,Vector3.zero,0,0,seconds));
        }

        [Test]
        public void TeleportAndNonfinitePositionsAreRejected()
        {
            Assert.Throws<InvalidOperationException>(() => TruckTaxiDrivingPerformanceProbe.ValidateSoakMotion(Vector3.zero,Vector3.right*100,10,10,.02f));
            Assert.Throws<InvalidOperationException>(() => TruckTaxiDrivingPerformanceProbe.ValidateSoakMotion(new Vector3(float.NaN,0,0),Vector3.zero,0,0,.02f));
        }

        private sealed class DisposableWait : IEnumerator, IDisposable
        {
            private readonly Action dispose;
            public DisposableWait(Action dispose) { this.dispose=dispose; }
            public object Current => null;
            public bool MoveNext() => true;
            public void Reset() => throw new NotSupportedException();
            public void Dispose() => dispose();
        }

        [TestCase(false)]
        [TestCase(true)]
        public void IteratorFailureDisposesInnerThenOuterExactlyOnce(bool innerThrows)
        {
            var order=new List<int>(); var stack=new Stack<IEnumerator>();
            stack.Push(new DisposableWait(() => order.Add(1)));
            stack.Push(new DisposableWait(() => { order.Add(2); if(innerThrows) throw new InvalidOperationException("fixture"); }));
            var error=TruckTaxiRestartHarness.DisposeIterators(stack);
            CollectionAssert.AreEqual(new[] {2,1},order); Assert.IsEmpty(stack);
            Assert.AreEqual(innerThrows,error!=null);
            Assert.IsNull(TruckTaxiRestartHarness.DisposeIterators(stack));
            CollectionAssert.AreEqual(new[] {2,1},order);
        }
    }
}

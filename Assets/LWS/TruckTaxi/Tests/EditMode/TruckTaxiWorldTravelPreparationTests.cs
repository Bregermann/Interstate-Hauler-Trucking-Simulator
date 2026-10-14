using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace LWS.TruckTaxi.Tests
{
    public sealed class TruckTaxiWorldTravelPreparationTests
    {
        [Test]
        public void SuccessDrivesNestedIteratorsAndPreservesNativeYields()
        {
            var steps = new List<string>();
            var nativeYield = new WaitForEndOfFrame();
            IEnumerator Child()
            {
                steps.Add("child");
                yield return nativeYield;
                steps.Add("child finished");
            }
            IEnumerator Root()
            {
                steps.Add("root");
                yield return null;
                yield return Child();
                steps.Add("root finished");
            }
            int completions = 0;
            Exception failure = null;
            IEnumerator run = Wrap(Root, error => { failure = error; completions++; }, () => 0);
            Assert.IsTrue(run.MoveNext());
            Assert.IsNull(run.Current);
            Assert.IsTrue(run.MoveNext());
            Assert.AreSame(nativeYield, run.Current);
            Assert.IsFalse(run.MoveNext());
            Assert.IsNull(failure);
            Assert.AreEqual(1, completions);
            CollectionAssert.AreEqual(new[] { "root", "child", "child finished", "root finished" }, steps);
        }

        [Test]
        public void FactoryFailureIsCapturedBeforePreparationStarts()
        {
            var expected = new InvalidOperationException("Preparation point could not be resolved.");
            Exception failure = null;
            int completions = 0;
            IEnumerator run = Wrap(() => throw expected, error => { failure = error; completions++; }, () => 0);
            Assert.IsFalse(run.MoveNext());
            Assert.AreSame(expected, failure);
            Assert.AreEqual(1, completions);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(3)]
        public void MoveNextFailureAtAnyDepthUnwindsEveryEnteredIterator(int depth)
        {
            var expected = new InvalidOperationException("Streaming preparation failed.");
            var disposed = new List<int>();
            Exception failure = null;
            int completions = 0;
            IEnumerator run = Wrap(() => Failing(depth, expected, disposed),
                error => { failure = error; completions++; }, () => 0);
            for (int i = 0; i <= depth; i++) Assert.IsTrue(run.MoveNext());
            Assert.IsFalse(run.MoveNext());
            Assert.AreSame(expected, failure);
            Assert.AreEqual(1, completions);
            Assert.AreEqual(depth + 1, disposed.Count);
            for (int i = 0; i <= depth; i++) Assert.AreEqual(i, disposed[i]);
            (run as IDisposable)?.Dispose();
            Assert.AreEqual(1, completions);
        }

        [Test]
        public void CurrentFailureIsCapturedAndCleanupFailureDoesNotReplaceIt()
        {
            var expected = new InvalidOperationException("Current failed.");
            var iterator = new BrokenCurrent(expected);
            Exception failure = null;
            IEnumerator run = Wrap(() => iterator, error => failure = error, () => 0);
            Assert.IsFalse(run.MoveNext());
            Assert.AreSame(expected, failure);
            Assert.IsTrue(iterator.Disposed);
        }

        [Test]
        public void RealTimeDeadlineTimesOutNestedPreparationAndUnwindsIt()
        {
            float now = 0;
            var disposed = new List<string>();
            IEnumerator Child()
            {
                try { while (true) yield return null; }
                finally { disposed.Add("child"); }
            }
            IEnumerator Root()
            {
                try { yield return Child(); }
                finally { disposed.Add("root"); }
            }
            Exception failure = null;
            int completions = 0;
            IEnumerator run = Wrap(Root, error => { failure = error; completions++; }, () => now);
            Assert.IsTrue(run.MoveNext());
            now = 59;
            Assert.IsTrue(run.MoveNext());
            now = 60;
            Assert.IsFalse(run.MoveNext());
            Assert.IsInstanceOf<TimeoutException>(failure);
            Assert.AreEqual(1, completions);
            CollectionAssert.AreEqual(new[] { "child", "root" }, disposed);
        }

        private static IEnumerator Wrap(Func<IEnumerator> create, Action<Exception> completed, Func<float> realtime) =>
            (IEnumerator)typeof(TruckTaxiWorldTravel).GetMethod("RunPreparation", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { create, completed, realtime, 60f });

        private static IEnumerator Failing(int depth, Exception error, List<int> disposed)
        {
            try
            {
                yield return null;
                if (depth > 0) yield return Failing(depth - 1, error, disposed);
                else throw error;
            }
            finally { disposed.Add(depth); }
        }

        private sealed class BrokenCurrent : IEnumerator, IDisposable
        {
            private readonly Exception error;
            public bool Disposed { get; private set; }
            public BrokenCurrent(Exception error) { this.error = error; }
            public object Current => throw error;
            public bool MoveNext() => true;
            public void Reset() => throw new NotSupportedException();
            public void Dispose() { Disposed = true; throw new InvalidOperationException("Cleanup also failed."); }
        }
    }
}

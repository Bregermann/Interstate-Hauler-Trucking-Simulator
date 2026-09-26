using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace LWS.TruckTaxi.Tests
{
    public sealed class TruckTaxiJugLiquidAudioPlayModeTests
    {
        [UnityTest]
        public IEnumerator GeneratedJugLoopPlaysPausesStopsAndReleasesClip()
        {
            var go = new GameObject("Jug loop test");
            var source = go.AddComponent<AudioSource>();
            var loop = new TruckTaxiJugLiquidLoop(source, null);
            AudioClip generated = null;
            try
            {
                Assert.IsFalse(loop.IsPlaying);
                loop.Play();
                yield return null;
                generated = source.clip;
                Assert.IsNotNull(generated);
                Assert.IsTrue(loop.UsesGeneratedClip);
                Assert.IsTrue(source.loop);
                Assert.IsTrue(loop.IsPlaying, "QTE loop should be playing after start.");
                loop.SetPaused(true);
                yield return null;
                Assert.IsFalse(loop.IsPlaying);
                loop.SetPaused(false);
                yield return null;
                Assert.IsTrue(loop.IsPlaying);
                loop.Stop();
                Assert.IsFalse(loop.IsPlaying);
                loop.Dispose();
                yield return null;
                Assert.IsNull(source.clip);
                Assert.IsTrue(generated == null, "Generated clip must be released.");
            }
            finally
            {
                loop.Dispose();
                Object.Destroy(go);
            }
        }
    }
}

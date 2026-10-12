using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace LWS.TruckTaxi.Tests
{
    public sealed class TruckTaxiSnowCacheDiagnosticsTests
    {
        private GameObject root;
        private TruckTaxiSnowSurface surface;
        private readonly List<MeshRenderer> inputs=new List<MeshRenderer>();
        private MethodInfo discover;
        private List<MeshRenderer> cached;
        private readonly List<MeshRenderer> baseline=new List<MeshRenderer>();
        private int baselineRefreshes;

        [SetUp] public void SetUp()
        {
            Assert.IsFalse(Application.isPlaying,"Run this EditMode fixture only after Main's PlayMode run finishes.");
            // Do not save, replace, or close an existing untitled user scene.
            root=new GameObject("Fixed snow cache fixture");
            surface=root.AddComponent<TruckTaxiSnowSurface>();
            discover=typeof(TruckTaxiSnowSurface).GetMethod("CacheTargets",BindingFlags.Instance|BindingFlags.NonPublic);
            cached=(List<MeshRenderer>)typeof(TruckTaxiSnowSurface)
                .GetField("groundTargets",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(surface);
            Assert.IsNotNull(discover);
            discover.Invoke(surface,null);
            baseline.AddRange(cached);
            baselineRefreshes=surface.CacheRefreshCount;
            Assert.AreEqual(baseline.Count,surface.UniqueCacheTargetCount);
            for(int i=0;i<3;i++) AddGround("Ground "+i);
            surface.MarkDirty();
        }

        [TearDown] public void TearDown()
        {
            if(root!=null) Object.DestroyImmediate(root);
            inputs.Clear();baseline.Clear();
        }

        [Test] public void TwentyFixedInputDiscoveriesKeepUniqueTargetsAndBoundCandidateTraversal()
        {
            const int passes=20,unique=3;
            var legacy=new List<MeshRenderer>();
            var legacyIds=new HashSet<int>();
            var before=new int[passes];
            var after=new int[passes];
            int beforeVisits=0,afterVisits=0,totalAfterVisits=0;
            surface.SetCoverage(.65f);
            for(int pass=1;pass<=passes;pass++)
            {
                // Exact pre-fix ground-cache append rule, replayed on the same fixed renderer references.
                // This control is not an old Windows player run or a frame-time measurement.
                foreach(var renderer in inputs)
                {
                    legacy.Add(renderer);
                    legacyIds.Add(renderer.GetInstanceID());
                }
                surface.MarkDirty();
                discover.Invoke(surface,null);
                before[pass-1]=legacy.Count;
                after[pass-1]=surface.CacheTargetCount-baseline.Count;
                foreach(var renderer in legacy) { Assert.IsNotNull(renderer);beforeVisits++; }
                foreach(var renderer in cached)
                { Assert.IsNotNull(renderer);totalAfterVisits++;if(inputs.Contains(renderer)) afterVisits++; }

                Assert.AreEqual(unique*pass,legacy.Count,"The pre-fix reference rule reproduces repeated discovery growth.");
                Assert.AreEqual(unique,legacyIds.Count);
                AssertCache(unique,pass);
                Assert.AreEqual(.65f,surface.Coverage01,"Discovery must not reset snow coverage.");
            }

            // BindGround visits each cache entry before testing its existing material binding.
            // These are counted candidate visits, not elapsed CPU time, allocation bytes, or hitch duration.
            Assert.AreEqual(630,beforeVisits);
            Assert.AreEqual(60,afterVisits);
            Assert.AreEqual((baseline.Count+unique)*passes,totalAfterVisits,"Existing scene targets must stay bounded too.");
            Debug.Log("Snow cache fixed-input evidence: inputs=3 passes=20; BEFORE legacy-control entries=["+
                string.Join(",",before)+"] unique=3 candidate-visits="+beforeVisits+
                "; AFTER production fixture entries=["+string.Join(",",after)+"] unique="+(surface.UniqueCacheTargetCount-baseline.Count)+
                " fixture-candidate-visits="+afterVisits+"; existing-scene unique="+baseline.Count+
                " total-production-visits="+totalAfterVisits+". Algorithmic counts only; no Windows hitch claim.");
        }

        [Test] public void CachedReadsDoNotRescanAndDirtyDiscoveryStillFindsNewGround()
        {
            discover.Invoke(surface,null);
            AssertCache(3,1);
            AddGround("New streamed ground");
            discover.Invoke(surface,null);
            Assert.AreEqual(baseline.Count+3,surface.CacheTargetCount);
            Assert.AreEqual(baselineRefreshes+1,surface.CacheRefreshCount);
            surface.MarkDirty();discover.Invoke(surface,null);
            AssertCache(4,2);
            surface.MarkDirty();discover.Invoke(surface,null);
            AssertCache(4,3);
        }

        [Test] public void StreamRefreshClearsDiscoveryStateAndRebuildsWithoutChangingCoverage()
        {
            surface.SetCoverage(.65f);
            discover.Invoke(surface,null);
            surface.MarkDirty();discover.Invoke(surface,null);
            AssertCache(3,2);
            surface.RefreshStreamedSurfaces();
            Assert.Zero(surface.CacheTargetCount);Assert.Zero(surface.UniqueCacheTargetCount);
            Assert.AreEqual(baselineRefreshes+2,surface.CacheRefreshCount);
            Assert.AreEqual(.65f,surface.Coverage01);
            discover.Invoke(surface,null);
            AssertCache(3,3);
        }

        private void AssertCache(int fixtureCount,int refreshes)
        {
            Assert.AreEqual(baseline.Count+fixtureCount,surface.CacheTargetCount,"Reverting the production append guard must fail this regression.");
            Assert.AreEqual(baseline.Count+fixtureCount,surface.UniqueCacheTargetCount);
            Assert.AreEqual(baselineRefreshes+refreshes,surface.CacheRefreshCount);
            var expected=new List<MeshRenderer>(baseline);expected.AddRange(inputs);
            CollectionAssert.AreEquivalent(expected,cached);
        }

        private void AddGround(string name)
        {
            var ground=new GameObject(name,typeof(MeshRenderer),typeof(TruckTaxiSurface));
            ground.transform.SetParent(root.transform);
            inputs.Add(ground.GetComponent<MeshRenderer>());
        }
    }
}

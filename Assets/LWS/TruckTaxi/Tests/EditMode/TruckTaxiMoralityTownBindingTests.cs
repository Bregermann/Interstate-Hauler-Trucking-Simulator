using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Object=UnityEngine.Object;

namespace LWS.TruckTaxi.Tests
{
    [Category("TaxiMoralityWorld")]
    public sealed class TruckTaxiMoralityTownBindingTests
    {
        private readonly List<Object> created=new List<Object>();
        private TruckTaxiRegionalWorld regional;
        private TruckTaxiMoralityTownBinding binding;
        private TruckTaxiSession session;
        [SetUp] public void Setup()
        {
            var world=new GameObject("authored regional fixture"); created.Add(world);
            regional=world.AddComponent<TruckTaxiRegionalWorld>();
            regional.regions=new[] {
                new TruckTaxiRegion { id="taxi.town01outskirts",bounds=new Bounds(Vector3.zero,new Vector3(100,100,100)) },
                new TruckTaxiRegion { id="taxi.town02",bounds=new Bounds(Vector3.right*1000,new Vector3(100,100,100)) }
            };
            var owner=new GameObject("town consumer fixture"); created.Add(owner); binding=owner.AddComponent<TruckTaxiMoralityTownBinding>();
            var config=ScriptableObject.CreateInstance<TruckTaxiConfiguration>(); created.Add(config);
            session=new TruckTaxiSession(config,Array.Empty<TruckTaxiRideLocation>(),33);
        }
        [TearDown] public void Cleanup()
        { for(int i=created.Count-1;i>=0;i--) if(created[i]!=null) Object.DestroyImmediate(created[i]); created.Clear(); }
        [Test] public void UsesCanonicalAuthoredTownAndLeavesUnknownPositionNeutral()
        {
            Assert.IsTrue(binding.Initialize(session,regional));
            Assert.AreEqual("taxi.town01",session.TownIdAtPosition(Vector3.zero));
            Assert.AreEqual("taxi.town02",session.TownIdAtPosition(Vector3.right*1000));
            Assert.AreEqual(string.Empty,session.TownIdAtPosition(Vector3.right*500));
            Assert.AreEqual(string.Empty,session.TownIdAtPosition(new Vector3(float.NaN,0,0)));
        }
        [Test] public void DoesNotReplaceOrClearAnotherTownProvider()
        {
            Func<Vector3,string> foreign=position=>"town.other";
            session.TownIdAtPosition=foreign;
            Assert.IsFalse(binding.Initialize(session,regional)); Assert.AreSame(foreign,session.TownIdAtPosition);
            session.TownIdAtPosition=null; Assert.IsTrue(binding.Initialize(session,regional));
            session.TownIdAtPosition=foreign; binding.enabled=false;
            Assert.AreSame(foreign,session.TownIdAtPosition);
        }
        [Test] public void TownEffectsResolveThroughTheActualPositionDelegate()
        {
            Assert.IsTrue(binding.Initialize(session,regional));
            Assert.IsTrue(session.BindCareerRun("profile.town-test",out _));
            Assert.IsTrue(session.TryInitializeCareer("Driver","truck.fixture",out _));
            var town=ScriptableObject.CreateInstance<TruckTaxiTownDefinition>(); created.Add(town);
            town.initialState.townId="taxi.town01"; town.initialState.fareMultiplier=1.05f;
            Assert.IsTrue(session.TryRegisterTown(town,out _));
            Assert.That(session.TownFareMultiplier(session.TownIdAtPosition(Vector3.zero)),Is.EqualTo(1.05f));
            Assert.AreEqual(1,session.TownFareMultiplier(session.TownIdAtPosition(Vector3.right*1000)));
        }
    }
}

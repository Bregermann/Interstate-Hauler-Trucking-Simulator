using System;
using System.Linq;
using LWS.TruckTaxi.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

namespace LWS.TruckTaxi.Tests
{
    public sealed class TruckTaxiPassengerFactoryTests
    {
        [Test] public void RosterIsRegisteredUniqueAndEveryPrefabIsUsable()
        {
            var db=AssetDatabase.LoadAssetAtPath<TruckTaxiPassengerDatabase>(TruckTaxiPassengerFactoryBuilder.DatabasePath);
            Assert.IsNotNull(db); Assert.GreaterOrEqual(db.passengers.Length,85);
            Assert.AreEqual(db.passengers.Length,db.passengers.Select(p=>p.passengerId).Distinct().Count());
            foreach(var p in db.passengers)
            {
                Assert.IsEmpty(TruckTaxiPassengerFactoryBuilder.Validate(p).Where(x=>x.StartsWith("ERROR")),p.passengerId);
                Assert.IsTrue(System.IO.File.Exists("Tools/TruckTaxiPassengerFactory/Manifests/Passengers/"+p.passengerId+".json"));
                var instance=Object.Instantiate(p.runtimePrefab);
                try
                {
                    var actor=instance.GetComponent<TruckTaxiPassengerActor>(); Assert.IsNotNull(actor,p.passengerId);
                    actor.Bind(p);
                    Assert.IsTrue(instance.GetComponentsInChildren<Renderer>().Any(r=>r.bounds.size.magnitude>.1f),p.passengerId);
                    Assert.IsTrue(instance.GetComponentsInChildren<Rigidbody>().All(r=>r.isKinematic),p.passengerId);
                    Assert.IsTrue(instance.GetComponentsInChildren<Collider>().All(c=>!c.enabled),p.passengerId);
                }
                finally { Object.DestroyImmediate(instance); }
            }
            Assert.GreaterOrEqual(db.passengers.Count(p=>p.pairPassenger!=null),5);
        }
        [Test] public void EverySharedAnimationHasAnInstalledClip()
        {
            var profile=TruckTaxiPassengerFactoryBuilder.SharedAnimations();
            foreach(TruckTaxiPassengerAnimation action in Enum.GetValues(typeof(TruckTaxiPassengerAnimation)))
                Assert.IsTrue(profile.animations.Any(b=>b.action==action && b.clip!=null),action.ToString());
        }
        [Test] public void DatabaseFiltersAndDeduplicatesWithoutEditorApis()
        {
            DatabaseFilters();
        }
        [Test] public void HumanFallbacksUseValidSharedHumanoidAndRagdoll()
        {
            var db=AssetDatabase.LoadAssetAtPath<TruckTaxiPassengerDatabase>(TruckTaxiPassengerFactoryBuilder.DatabasePath);
            foreach(var p in db.passengers.Where(p=>p.casting.human))
            {
                var animator=p.runtimePrefab.GetComponentInChildren<Animator>(true);
                Assert.IsNotNull(animator,p.passengerId); Assert.IsTrue(animator.isHuman,p.passengerId);
                Assert.IsTrue(animator.avatar.isValid,p.passengerId);
                Assert.Greater(p.runtimePrefab.GetComponent<TruckTaxiPassengerActor>().ragdollBodies.Length,5,p.passengerId);
            }
        }
        [Test] public void RebuildingDataAndPrefabRetainsIdsAndAuthoredValues()
        {
            var p=TruckTaxiPassengerDialogueAuthoring.AllPassengers().First(x=>x.passengerId=="analytical-robot");
            string before=JsonUtility.ToJson(p),dialogue=JsonUtility.ToJson(p.authoredDialogue);
            string guid=AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(p.runtimePrefab));
            TruckTaxiPassengerFactoryBuilder.BuildMissing(p); TruckTaxiPassengerFactoryBuilder.BuildPrefab(p);
            Assert.AreEqual(before,JsonUtility.ToJson(p)); Assert.AreEqual(dialogue,JsonUtility.ToJson(p.authoredDialogue));
            Assert.AreEqual(guid,AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(p.runtimePrefab)));
        }
        private static void DatabaseFilters()
        {
            var db=ScriptableObject.CreateInstance<TruckTaxiPassengerDatabase>();
            var a=ScriptableObject.CreateInstance<PassengerProfile>(); var b=ScriptableObject.CreateInstance<PassengerProfile>();
            try
            {
                a.passengerId="a"; a.available=true; a.districts=new[]{"harbor"}; a.rarity=TruckTaxiRarity.Rare;
                b.passengerId="b"; b.available=false; db.passengers=new[]{a,a,b};
                Assert.AreEqual(2,db.Query().Count());
                Assert.AreEqual(1,db.Query(new TruckTaxiPassengerQuery { availableOnly=true,district="harbor",rarity=TruckTaxiRarity.Rare }).Count());
                Assert.AreEqual(0,db.Query(new TruckTaxiPassengerQuery { availableOnly=true,district="industrial" }).Count());
            }
            finally { Object.DestroyImmediate(db); Object.DestroyImmediate(a); Object.DestroyImmediate(b); }
        }
    }
}

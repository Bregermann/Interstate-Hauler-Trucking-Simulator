using System;
using System.IO;
using System.Linq;
using LWS.TruckTaxi.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LWS.TruckTaxi.Tests
{
    public sealed class TruckTaxiWobbleAuthoringTests
    {
        [Test] public void RequestedCastHasUniqueStableIdsAndAdultGlamAges()
        {
            string json=File.ReadAllText("Tools/TruckTaxiPassengerFactory/requested-cast.json");
            var source=JsonUtility.FromJson<Cast>(json);
            Assert.AreEqual(29,source.passengers.Length);
            Assert.AreEqual(29,source.passengers.Select(p=>p.id).Distinct(StringComparer.OrdinalIgnoreCase).Count());
            Assert.AreEqual(5,source.passengers.Count(p=>p.id.StartsWith("racing-",StringComparison.Ordinal)));
            Assert.AreEqual(24,source.passengers.Count(p=>p.id.StartsWith("glam-",StringComparison.Ordinal)));
            foreach(var p in source.passengers.Where(p=>p.id.StartsWith("glam-",StringComparison.Ordinal)))
                Assert.GreaterOrEqual(int.Parse(p.age),21,p.id);
        }

        [Test] public void JugCoverageIsAdditiveAndIdempotent()
        {
            var passenger=ScriptableObject.CreateInstance<PassengerProfile>();
            var dialogue=ScriptableObject.CreateInstance<TruckTaxiDialogueSet>();
            try
            {
                passenger.passengerId="test-person"; passenger.personality="Analytical engineer";
                passenger.authoredDialogue=dialogue;
                dialogue.lines=new[] { new TruckTaxiDialogueLine { lineId="custom",passengerId=passenger.passengerId,
                    category=TruckTaxiDialogueCategory.JugStarted,text="Keep my custom line." } };
                Assert.AreEqual(5,TruckTaxiPassengerDialogueAuthoring.EnsureRequestedCoverage(passenger));
                string snapshot=JsonUtility.ToJson(dialogue);
                Assert.AreEqual(0,TruckTaxiPassengerDialogueAuthoring.EnsureRequestedCoverage(passenger));
                Assert.AreEqual(snapshot,JsonUtility.ToJson(dialogue));
                Assert.AreEqual("Keep my custom line.",dialogue.lines[0].text);
                foreach(var category in new[] { TruckTaxiDialogueCategory.JugStarted,TruckTaxiDialogueCategory.JugSucceeded,
                    TruckTaxiDialogueCategory.JugSpilled,TruckTaxiDialogueCategory.JugThrownFromWindow })
                    Assert.IsTrue(dialogue.lines.Any(l=>l.category==category),category.ToString());
            }
            finally { Object.DestroyImmediate(dialogue); Object.DestroyImmediate(passenger); }
        }

        [Test] public void PedestrianBindingChangesOnlyVisuals()
        {
            var original=GameObject.CreatePrimitive(PrimitiveType.Capsule);
            try
            {
                var originalRenderer=original.GetComponent<Renderer>();
                var originalCollider=original.GetComponent<Collider>();
                var binding=TruckTaxiWobbleVisual.BindPedestrian(original.transform,null,null);
                Assert.IsFalse(originalRenderer.enabled);
                Assert.IsTrue(originalCollider.enabled);
                Assert.IsNotNull(original.transform.Find(TruckTaxiWobbleVisual.VisualName));
                binding.SetWobbleVisible(false);
                Assert.IsTrue(originalRenderer.enabled);
                Assert.IsTrue(originalCollider.enabled);
                binding.SetWobbleVisible(true);
                Assert.IsFalse(originalRenderer.enabled);
                Assert.IsTrue(originalCollider.enabled);
            }
            finally { Object.DestroyImmediate(original); }
        }

        [Test] public void WobbleVisualHasDistinctClothesSkinAndNoAddedColliders()
        {
            var root=new GameObject("Wobble test");
            var shader=Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var clothes=new Material(shader);
            var detail=new Material(shader);
            var skin=new Material(shader);
            try
            {
                var visual=TruckTaxiWobbleVisual.Create(root.transform,1.7f,1.25f,clothes,detail,skin);
                Assert.AreSame(clothes,visual.transform.Find("Body").GetComponent<Renderer>().sharedMaterial);
                Assert.AreSame(detail,visual.transform.Find("Pants").GetComponent<Renderer>().sharedMaterial);
                Assert.AreSame(skin,visual.transform.Find("Head").GetComponent<Renderer>().sharedMaterial);
                Assert.IsNotNull(visual.transform.Find("Left sleeve"));
                Assert.IsNotNull(visual.transform.Find("Right hand"));
                Assert.IsEmpty(visual.GetComponentsInChildren<Collider>(true));
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(clothes);
                Object.DestroyImmediate(detail);
                Object.DestroyImmediate(skin);
            }
        }

        [Test] public void NewAppearanceDefaultsToWobbleWithUtsAsAlternate()
        {
            var appearance=ScriptableObject.CreateInstance<TruckTaxiAppearanceProfile>();
            try
            {
                Assert.AreEqual(TruckTaxiVisualStyle.WobblePeople,appearance.visualStyle);
                appearance.visualStyle=TruckTaxiVisualStyle.OriginalUts;
                Assert.AreEqual(TruckTaxiVisualStyle.OriginalUts,appearance.visualStyle);
            }
            finally { Object.DestroyImmediate(appearance); }
        }

        [Test, Explicit("Run after Configure Requested Cast and Wobble Visuals in the Unity Editor.")]
        public void ConfiguredAssetsHaveRequestedCastAndJugCoverage()
        {
            var db=AssetDatabase.LoadAssetAtPath<TruckTaxiPassengerDatabase>(TruckTaxiPassengerFactoryBuilder.DatabasePath);
            Assert.IsNotNull(db);
            var requested=JsonUtility.FromJson<Cast>(File.ReadAllText("Tools/TruckTaxiPassengerFactory/requested-cast.json"));
            foreach(var entry in requested.passengers)
            {
                var passenger=db.passengers.Single(p=>p.passengerId==entry.id);
                Assert.IsTrue(passenger.available,entry.id);
                Assert.IsNotNull(passenger.voiceProfile,entry.id);
                Assert.IsNotNull(passenger.runtimePrefab,entry.id);
                if(passenger.modelPrefab==null && passenger.appearance.visualStyle==TruckTaxiVisualStyle.WobblePeople)
                    Assert.IsNotNull(passenger.runtimePrefab.transform.Find(TruckTaxiWobbleVisual.VisualName),entry.id);
                foreach(var category in new[] { TruckTaxiDialogueCategory.JugStarted,TruckTaxiDialogueCategory.JugSucceeded,
                    TruckTaxiDialogueCategory.JugSpilled,TruckTaxiDialogueCategory.JugThrownFromWindow })
                    Assert.IsTrue(passenger.authoredDialogue.lines.Any(l=>l.category==category),entry.id+" "+category);
                if(entry.id.StartsWith("glam-",StringComparison.Ordinal))
                {
                    Assert.IsTrue(passenger.CanOfferAppreciation,entry.id);
                    Assert.AreEqual(.35f,passenger.appreciationChance,entry.id);
                }
            }
            foreach(var passenger in db.passengers)
                foreach(var category in new[] { TruckTaxiDialogueCategory.JugStarted,TruckTaxiDialogueCategory.JugSucceeded,
                    TruckTaxiDialogueCategory.JugSpilled,TruckTaxiDialogueCategory.JugThrownFromWindow })
                    Assert.IsTrue(passenger.authoredDialogue.lines.Any(l=>l.category==category),passenger.passengerId+" "+category);
        }

        [Serializable] private sealed class Cast { public Entry[] passengers; }
        [Serializable] private sealed class Entry { public string id,age; }
    }
}

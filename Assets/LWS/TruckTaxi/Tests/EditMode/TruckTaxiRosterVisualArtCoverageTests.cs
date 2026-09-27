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
    [Category("TaxiIntegrated")]
    public sealed class TruckTaxiRosterVisualArtCoverageTests
    {
        [Serializable] private sealed class Source { public Cast[] passengers; }
        [Serializable] private sealed class Cast { public string id; }

        [Test] public void EveryAuthoredRosterIdHasOneIntentionalDesign()
        {
            var initial = JsonUtility.FromJson<Source>(File.ReadAllText("Tools/TruckTaxiPassengerFactory/roster.json"));
            var requested = JsonUtility.FromJson<Source>(File.ReadAllText("Tools/TruckTaxiPassengerFactory/requested-cast.json"));
            var sourceIds = initial.passengers.Concat(requested.passengers).Select(p => p.id).ToArray();
            Assert.AreEqual(104, sourceIds.Length);
            var db = AssetDatabase.LoadAssetAtPath<TruckTaxiPassengerDatabase>(TruckTaxiPassengerFactoryBuilder.DatabasePath);
            Assert.IsNotNull(db);
            var ids = db.passengers.Select(p => p.passengerId).ToArray();
            Assert.AreEqual(114, ids.Length);
            CollectionAssert.IsSubsetOf(sourceIds, ids);
            CollectionAssert.AreEquivalent(ids, TruckTaxiRosterVisualCatalog.Entries.Select(e => e.id).ToArray());
            Assert.AreEqual(ids.Length, TruckTaxiRosterVisualCatalog.Entries.Select(e => e.id).Distinct(StringComparer.OrdinalIgnoreCase).Count());
            Assert.AreEqual(24, ids.Count(id => id.StartsWith("glam-", StringComparison.Ordinal)));
            Assert.AreEqual(5, ids.Count(id => id.StartsWith("racing-", StringComparison.Ordinal)));
            Assert.AreEqual(10, ids.Count(id => id.StartsWith("passenger-", StringComparison.Ordinal)));
            foreach (var entry in TruckTaxiRosterVisualCatalog.Entries)
            {
                Assert.Greater(entry.width, .5f, entry.id);
                Assert.Greater(entry.headScale, .5f, entry.id);
                Assert.IsTrue(TruckTaxiRosterVisualCatalog.TryGet(entry.id, out _), entry.id);
                Assert.DoesNotThrow(() => TruckTaxiRosterVisualCatalog.Color(entry.bodyHex), entry.id);
                Assert.DoesNotThrow(() => TruckTaxiRosterVisualCatalog.Color(entry.clothesHex), entry.id);
                Assert.DoesNotThrow(() => TruckTaxiRosterVisualCatalog.Color(entry.accentHex), entry.id);
            }
        }

        [Test] public void ThemedVisualsKeepDistinctShapesAndNoPhysics()
        {
            var root = new GameObject("Art test");
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var body = new Material(shader);
            var cloth = new Material(shader);
            var accent = new Material(shader);
            try
            {
                var dragon = TruckTaxiWobbleVisual.CreateThemed(root.transform, new TruckTaxiWobbleVisual.Design {
                    silhouette = TruckTaxiWobbleVisual.Silhouette.Dragon, headwear = TruckTaxiWobbleVisual.Headwear.Horns,
                    accessory = TruckTaxiWobbleVisual.Accessory.None, height = 1.1f, width = .8f,
                    headScale = 1.2f, upperBodyScale = 1f, body = body, clothes = cloth, accent = accent
                });
                Assert.IsNotNull(dragon.transform.Find("Dragon tail"));
                Assert.IsNotNull(dragon.transform.Find("Left Wing"));
                Assert.IsEmpty(dragon.GetComponentsInChildren<Collider>(true));
                Object.DestroyImmediate(dragon);
                var adult = TruckTaxiWobbleVisual.CreateThemed(root.transform, new TruckTaxiWobbleVisual.Design {
                    silhouette = TruckTaxiWobbleVisual.Silhouette.Human, headwear = TruckTaxiWobbleVisual.Headwear.Braid,
                    accessory = TruckTaxiWobbleVisual.Accessory.Medical, height = 1.7f, width = .9f,
                    headScale = 1f, upperBodyScale = 1.15f, clothedAdult = true,
                    body = body, clothes = cloth, accent = accent
                });
                Assert.IsNotNull(adult.transform.Find("Left Clothed chest"));
                Assert.IsNotNull(adult.transform.Find("Medical"));
                Assert.IsEmpty(adult.GetComponentsInChildren<Collider>(true));
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(body);
                Object.DestroyImmediate(cloth);
                Object.DestroyImmediate(accent);
            }
        }
        [Test] public void AmbientActorsShareOneThreeMaterialMeshWithoutChangingCollision()
        {
            var a=GameObject.CreatePrimitive(PrimitiveType.Capsule);
            var b=GameObject.CreatePrimitive(PrimitiveType.Capsule);
            try
            {
                var binding=TruckTaxiWobbleVisual.BindPedestrian(a.transform,null,null);
                TruckTaxiWobbleVisual.BindPedestrian(b.transform,null,null,1.7f,1);
                var av=a.transform.Find(TruckTaxiWobbleVisual.VisualName);
                var bv=b.transform.Find(TruckTaxiWobbleVisual.VisualName);
                Assert.AreEqual(1,av.GetComponentsInChildren<Renderer>().Length);
                Assert.AreSame(av.GetComponent<MeshFilter>().sharedMesh,bv.GetComponent<MeshFilter>().sharedMesh);
                Assert.AreEqual(3,av.GetComponent<MeshFilter>().sharedMesh.subMeshCount);
                Assert.IsEmpty(av.GetComponentsInChildren<Collider>());
                Assert.IsTrue(a.GetComponent<Collider>().enabled);
                binding.SetWobbleVisible(false); Assert.IsTrue(a.GetComponent<Renderer>().enabled);
            }
            finally { Object.DestroyImmediate(a); Object.DestroyImmediate(b); }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using LWS.TruckTaxi.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace LWS.TruckTaxi.Tests
{
    public sealed class TruckTaxiMapMarkerTests
    {
        [TestCase(640,340)]
        [TestCase(60,60)]
        [TestCase(240,240)]
        [TestCase(360,360)]
        public void CabAndHudUseMatchingRouteAndIconProportions(float width,float height)
        {
            float shortSide=Mathf.Min(width,height),scale=TruckTaxiGPSAdapter.MapElementScale(new Vector2(width,height));
            Assert.That(12*scale/shortSide,Is.EqualTo(.05f).Within(.0001f),"Default route must not be a HUD slab or a cab hairline.");
            Assert.That(24*.8f*scale/shortSide,Is.EqualTo(.08f).Within(.0001f),"Normal POI proportions.");
            Assert.That(12*1.6f*scale/shortSide,Is.EqualTo(.08f).Within(.0001f),"Player arrow proportions.");
        }

        [TestCase(TruckTaxiStopCategory.Scenic,TruckTaxiMapMarkerType.ScenicStop)]
        [TestCase(TruckTaxiStopCategory.IllicitPickup,TruckTaxiMapMarkerType.IllicitStop)]
        [TestCase(TruckTaxiStopCategory.PrivateMeeting,TruckTaxiMapMarkerType.PrivateEventStop)]
        [TestCase(TruckTaxiStopCategory.CollectionStop,TruckTaxiMapMarkerType.Collectible)]
        [TestCase(TruckTaxiStopCategory.FoodStop,TruckTaxiMapMarkerType.FoodStop)]
        [TestCase(TruckTaxiStopCategory.PhotoStop,TruckTaxiMapMarkerType.PhotoStop)]
        [TestCase(TruckTaxiStopCategory.SpecialEvent,TruckTaxiMapMarkerType.SpecialEvent)]
        public void StopsHaveExplicitSemanticTypes(TruckTaxiStopCategory category,TruckTaxiMapMarkerType type) =>
            Assert.AreEqual(type,TruckTaxiMapIconRegistry.ForStop(category));

        [Test]
        public void RegistryHasOneDistinctSpritePerType()
        {
            var registry=AssetDatabase.LoadAssetAtPath<TruckTaxiMapIconRegistry>(TruckTaxiMapIconSetup.AssetPath);
            Assert.NotNull(registry,"Run TruckTaxiMapIconSetup.EnsureAssets before integration validation.");
            var sprites=new HashSet<Sprite>();
            foreach(TruckTaxiMapMarkerType type in Enum.GetValues(typeof(TruckTaxiMapMarkerType)))
            {
                Assert.AreEqual(1,registry.entries.Count(e=>e.type==type),type.ToString());
                var entry=registry.Find(type); Assert.NotNull(entry.icon,type.ToString());
                Assert.IsTrue(sprites.Add(entry.icon),"Shared generic sprite: "+type);
                Assert.Greater(entry.icon.rect.width,1); Assert.Greater(entry.icon.rect.height,1);
            }
        }
        [Test]
        public void GeneratedSilhouettesAreNonemptyDistinctAndBoldAtMinimapScale()
        {
            var silhouettes=new HashSet<string>();
            var generated=new[]{TruckTaxiMapMarkerType.PassengerPickup,TruckTaxiMapMarkerType.Destination,
                TruckTaxiMapMarkerType.ScenicStop,TruckTaxiMapMarkerType.IllicitStop,TruckTaxiMapMarkerType.Shortcut,
                TruckTaxiMapMarkerType.DiscoveredShortcut,TruckTaxiMapMarkerType.TargetVehicle,TruckTaxiMapMarkerType.ActiveObjective,
                TruckTaxiMapMarkerType.Dropoff,TruckTaxiMapMarkerType.FoodStop,TruckTaxiMapMarkerType.PhotoStop,TruckTaxiMapMarkerType.Danger,TruckTaxiMapMarkerType.Bathroom};
            foreach(var type in generated)
            {
                var pixels=new char[24*24]; int filled=0;
                for(int y=0;y<24;y++) for(int x=0;x<24;x++)
                { bool solid=TruckTaxiMapIconSetup.Glyph(type,new Vector2((x+.5f)/24,(y+.5f)/24)); pixels[y*24+x]=solid ? '#' : '.'; if(solid) filled++; }
                Assert.Greater(filled,35,type.ToString()); Assert.Less(filled,400,type.ToString());
                Assert.IsTrue(silhouettes.Add(new string(pixels)),type.ToString());
            }
        }
        [Test]
        public void ActiveKnownCompletedDoNotDependOnlyOnColor()
        {
            var registry=ScriptableObject.CreateInstance<TruckTaxiMapIconRegistry>();
            try
            {
                registry.entries=new[]{new TruckTaxiMapIconRegistry.Entry { type=TruckTaxiMapMarkerType.ScenicStop,color=Color.green }};
                Assert.Greater(registry.Scale(TruckTaxiMapMarkerType.ScenicStop,TruckTaxiMapMarkerState.Active),registry.Scale(TruckTaxiMapMarkerType.ScenicStop,TruckTaxiMapMarkerState.Known));
                Assert.Less(registry.Scale(TruckTaxiMapMarkerType.ScenicStop,TruckTaxiMapMarkerState.Completed),registry.Scale(TruckTaxiMapMarkerType.ScenicStop,TruckTaxiMapMarkerState.Known));
                Assert.AreEqual(0,registry.Tint(TruckTaxiMapMarkerType.ScenicStop,TruckTaxiMapMarkerState.Hidden).a);
            }
            finally { UnityEngine.Object.DestroyImmediate(registry); }
        }
        [Test]
        public void RegistrationIsIdempotentAndDuplicateIdsAreRejected()
        {
            var root=new GameObject("Marker test"); var source=new GameObject("Source"); var other=new GameObject("Duplicate");
            try
            {
                var map=root.AddComponent<TruckTaxiMapMarkers>(); var marker=source.AddComponent<TruckTaxiMapMarker>();
                marker.stableId="test.id"; Assert.IsTrue(map.Register(marker)); Assert.IsTrue(map.Register(marker));
                var duplicate=other.AddComponent<TruckTaxiMapMarker>(); duplicate.stableId=marker.stableId;
                LogAssert.Expect(LogType.Warning,"Duplicate Truck Taxi map marker ID: test.id");
                Assert.IsFalse(map.Register(duplicate)); Assert.AreEqual(1,map.Markers.Count);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(source); UnityEngine.Object.DestroyImmediate(other); }
        }
        [Test]
        public void ShortcutDiscoveryIsIdempotentChangesTypeAndDoesNotDiscoverOtherPoints()
        {
            string id="test-shortcut-"+Guid.NewGuid().ToString("N"); var root=new GameObject("Discovery test");
            try
            {
                var map=root.AddComponent<TruckTaxiMapMarkers>(); var marker=root.AddComponent<TruckTaxiMapMarker>();
                marker.stableId=id; marker.markerType=TruckTaxiMapMarkerType.Shortcut; map.Register(marker);
                Assert.IsFalse(map.DiscoverShortcut("unknown")); Assert.IsTrue(map.DiscoverShortcut(id)); Assert.IsFalse(map.DiscoverShortcut(id));
                Assert.IsTrue(map.IsShortcutDiscovered(id)); Assert.AreEqual(TruckTaxiMapMarkerType.DiscoveredShortcut,marker.markerType);
                Assert.AreEqual(1,PlayerPrefs.GetInt("TruckTaxi.Map.Shortcut.v1."+id));
            }
            finally { PlayerPrefs.DeleteKey("TruckTaxi.Map.Shortcut.v1."+id); PlayerPrefs.Save(); UnityEngine.Object.DestroyImmediate(root); }
        }
        [TestCase(true)]
        [TestCase(false)]
        public void UnregisterAndDestroyedMarkerAllowStableIdentityReuse(bool active)
        {
            var root=new GameObject("Lifecycle test"); var first=new GameObject("First"); var second=new GameObject("Replacement");
            var poi=new GameObject("Owned presentation");
            try
            {
                first.SetActive(active);
                var map=root.AddComponent<TruckTaxiMapMarkers>(); var marker=first.AddComponent<TruckTaxiMapMarker>(); marker.stableId="test.reuse";
                Assert.IsTrue(map.Register(marker)); Assert.IsTrue(map.Unregister(marker)); Assert.IsFalse(map.Unregister(marker));
                Assert.IsFalse(marker.Presented); Assert.AreEqual(0,map.Markers.Count);
                Assert.IsTrue(map.Register(marker)); marker.BindPresentation(null,poi.transform,true);
                UnityEngine.Object.DestroyImmediate(first);
                Assert.AreEqual(0,map.Markers.Count);
                Assert.IsTrue(poi==null,"Destroyed markers must also release their owned presentation.");
                var replacement=second.AddComponent<TruckTaxiMapMarker>(); replacement.stableId="test.reuse";
                Assert.IsTrue(map.Register(replacement)); Assert.AreEqual(1,map.Markers.Count);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); if(first!=null) UnityEngine.Object.DestroyImmediate(first); UnityEngine.Object.DestroyImmediate(second); if(poi!=null) UnityEngine.Object.DestroyImmediate(poi); }
        }
        [Test]
        public void RegistrationPrunesDestroyedMarkersWithoutAReadOrRuntimeRefresh()
        {
            var root=new GameObject("Registration cleanup test"); var first=new GameObject("Never activated"); var second=new GameObject("Replacement");
            var poi=new GameObject("Owned presentation");
            try
            {
                first.SetActive(false);
                var map=root.AddComponent<TruckTaxiMapMarkers>(); var marker=first.AddComponent<TruckTaxiMapMarker>(); marker.stableId="test.reuse";
                Assert.IsTrue(map.Register(marker)); marker.BindPresentation(null,poi.transform,true);
                UnityEngine.Object.DestroyImmediate(first);
                var replacement=second.AddComponent<TruckTaxiMapMarker>(); replacement.stableId="test.reuse";
                Assert.IsTrue(map.Register(replacement)); Assert.AreEqual(1,map.Markers.Count);
                Assert.AreSame(replacement,map.Markers[0]); Assert.IsTrue(poi==null);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); if(first!=null) UnityEngine.Object.DestroyImmediate(first); UnityEngine.Object.DestroyImmediate(second); if(poi!=null) UnityEngine.Object.DestroyImmediate(poi); }
        }
        [Test]
        public void ProbeDiscoveryResetDoesNotModifyExistingPreference()
        {
            string id="test-preserve-"+Guid.NewGuid().ToString("N"),key=TruckTaxiMapMarkers.ShortcutPreferenceKey(id);
            var root=new GameObject("Discovery restore test");
            try
            {
                PlayerPrefs.SetInt(key,7);
                var map=root.AddComponent<TruckTaxiMapMarkers>(); var marker=root.AddComponent<TruckTaxiMapMarker>();
                marker.stableId=id; marker.markerType=TruckTaxiMapMarkerType.Shortcut; map.Register(marker);
                map.RestoreShortcutDiscoveryForProbe(id,true); Assert.IsTrue(map.IsShortcutDiscovered(id));
                map.RestoreShortcutDiscoveryForProbe(id,false); Assert.IsFalse(map.IsShortcutDiscovered(id));
                Assert.AreEqual(7,PlayerPrefs.GetInt(key));
            }
            finally { PlayerPrefs.DeleteKey(key); PlayerPrefs.Save(); UnityEngine.Object.DestroyImmediate(root); }
        }
        [Test]
        public void MarkerTracksSourceWithoutPersistingATransientWorldPosition()
        {
            var root=new GameObject("Moving source"); var go=new GameObject("Marker");
            try
            {
                var marker=go.AddComponent<TruckTaxiMapMarker>(); marker.source=root.transform; marker.sourceLocalOffset=new Vector3(0,0,2);
                root.transform.SetPositionAndRotation(new Vector3(30,2,60),Quaternion.Euler(0,90,0));
                Assert.That(Vector3.Distance(new Vector3(32,2,60),marker.Position),Is.LessThan(.001f));
            }
            finally { UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(go); }
        }
    }
}

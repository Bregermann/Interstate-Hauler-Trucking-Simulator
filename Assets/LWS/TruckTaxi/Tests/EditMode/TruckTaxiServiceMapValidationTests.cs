using System.Collections.Generic;
using LWS.TruckTaxi.Editor;
using NUnit.Framework;
using UnityEngine;

namespace LWS.TruckTaxi.Tests
{
    public sealed class TruckTaxiServiceMapValidationTests
    {
        [TestCase(TruckTaxiMapMarkerType.FoodStop,TruckTaxiServiceCapability.Food)]
        [TestCase(TruckTaxiMapMarkerType.Drink,TruckTaxiServiceCapability.Drink)]
        [TestCase(TruckTaxiMapMarkerType.Gas,TruckTaxiServiceCapability.Refuel)]
        [TestCase(TruckTaxiMapMarkerType.Bathroom,TruckTaxiServiceCapability.Restroom)]
        [TestCase(TruckTaxiMapMarkerType.Repair,TruckTaxiServiceCapability.RepairEngine)]
        [TestCase(TruckTaxiMapMarkerType.Sleep,TruckTaxiServiceCapability.Sleep)]
        [TestCase(TruckTaxiMapMarkerType.Store,TruckTaxiServiceCapability.Store)]
        public void EachServiceIconRequiresTheMatchingActualCapability(TruckTaxiMapMarkerType type,TruckTaxiServiceCapability capability)
        {
            var root=new GameObject("Service");
            try
            {
                var service=root.AddComponent<TruckTaxiServicePoint>(); service.stableId="actual";
                Assert.IsFalse(TruckTaxiServicePresentation.Matches(service,type));
                service.capabilities=capability;
                Assert.IsTrue(TruckTaxiServicePresentation.Matches(service,type));
                service.enabled=false;
                Assert.IsFalse(TruckTaxiServicePresentation.Matches(service,type));
            }
            finally { Object.DestroyImmediate(root); }
        }
        [Test]
        public void CombinedLocationUsesOneMarkerAndListsOnlyRealCapabilities()
        {
            var capabilities=TruckTaxiServiceCapability.Refuel|TruckTaxiServiceCapability.Restroom|TruckTaxiServiceCapability.Sleep;
            Assert.AreEqual(TruckTaxiMapMarkerType.ServiceArea,TruckTaxiServicePresentation.MarkerType(capabilities));
            Assert.AreEqual("GAS / RESTROOM / SLEEP",TruckTaxiServicePresentation.Label(capabilities));
            Assert.AreEqual(TruckTaxiMapMarkerType.Place,TruckTaxiServicePresentation.MarkerType(TruckTaxiServiceCapability.None));
        }
        [Test]
        public void RingUsesActualBayAndStoreExpandedInteractionRadius()
        {
            var building=new GameObject("Building"); var bay=new GameObject("Bay");
            try
            {
                bay.transform.position=new Vector3(40,2,60);
                var location=building.AddComponent<TruckTaxiRideLocation>(); location.truckStopPoint=bay.transform;
                var service=building.AddComponent<TruckTaxiServicePoint>(); service.location=location; service.stableId="store";
                service.capabilities=TruckTaxiServiceCapability.Store; service.radius=7;
                Assert.AreEqual(18,service.InteractionRadius);
                Assert.AreEqual(TruckTaxiStopRingState.Valid,TruckTaxiServiceRings.Evaluate(service,bay.transform.position,0,false,false));
                Assert.AreEqual(TruckTaxiStopRingState.InsideTooFast,TruckTaxiServiceRings.Evaluate(service,bay.transform.position,5,true,false));
                Assert.AreEqual(TruckTaxiStopRingState.Interacting,TruckTaxiServiceRings.Evaluate(service,bay.transform.position,0,false,true));
                Assert.AreEqual(TruckTaxiStopRingState.Selected,TruckTaxiServiceRings.Evaluate(service,Vector3.zero,0,true,false));
                Assert.AreEqual(TruckTaxiStopRingState.Ambient,TruckTaxiServiceRings.Evaluate(service,Vector3.one*500,0,false,false));
                service.enabled=false;
                Assert.AreEqual(TruckTaxiStopRingState.Unavailable,TruckTaxiServiceRings.Evaluate(service,bay.transform.position,0,true,false));
            }
            finally { Object.DestroyImmediate(building); Object.DestroyImmediate(bay); }
        }
        [Test]
        public void ValidatorFindsPhantomFoodMissingRingsAndMisalignedGeometry()
        {
            var root=new GameObject("Validation"); var line=new GameObject("Incorrect ring");
            try
            {
                var service=root.AddComponent<TruckTaxiServicePoint>(); service.stableId="bathroom"; service.capabilities=TruckTaxiServiceCapability.Restroom;
                var marker=root.AddComponent<TruckTaxiMapMarker>(); marker.stableId=service.stableId; marker.markerType=TruckTaxiMapMarkerType.FoodStop;
                var findings=TruckTaxiServiceMapValidation.Validate(new[] {service},new[] {marker},new Dictionary<string,TruckTaxiServiceRings.Ring>());
                Assert.That(findings,Has.Some.Contains("no matching usable capability"));
                Assert.That(findings,Has.Some.Contains("no world ring"));
                var rings=new Dictionary<string,TruckTaxiServiceRings.Ring> {
                    [service.stableId]=new TruckTaxiServiceRings.Ring { Line=line.AddComponent<LineRenderer>(),Center=Vector3.right*100,Radius=1 }
                };
                findings=TruckTaxiServiceMapValidation.Validate(new[] {service},new[] {marker},rings);
                Assert.That(findings,Has.Some.Contains("Ring center not aligned"));
                Assert.That(findings,Has.Some.Contains("Ring radius inconsistent"));
            }
            finally { Object.DestroyImmediate(line); Object.DestroyImmediate(root); }
        }
        [Test]
        public void AppendedServiceGlyphsAreDistinctAndNonempty()
        {
            var shapes=new HashSet<string>();
            foreach(var type in new[] {TruckTaxiMapMarkerType.Sleep,TruckTaxiMapMarkerType.Drink,TruckTaxiMapMarkerType.Place})
            {
                var pixels=new char[24*24]; int count=0;
                for(int y=0;y<24;y++) for(int x=0;x<24;x++)
                { bool ink=TruckTaxiServiceMapValidator.Glyph(type,new Vector2((x+.5f)/24,(y+.5f)/24)); pixels[y*24+x]=ink ? '#' : '.'; if(ink) count++; }
                Assert.Greater(count,30); Assert.IsTrue(shapes.Add(new string(pixels)));
            }
        }
    }
}

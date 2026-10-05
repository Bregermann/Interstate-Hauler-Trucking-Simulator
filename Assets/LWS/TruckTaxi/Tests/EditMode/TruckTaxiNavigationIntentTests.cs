using System.Collections.Generic;
using System.Reflection;
using LWS.InterstateHauler;
using NUnit.Framework;
using UnityEngine;

namespace LWS.TruckTaxi.Tests
{
    public sealed class TruckTaxiNavigationIntentTests
    {
        [Test]
        public void FinalScenicRestroomCompletionRestoresEachSuspendedOwner()
        {
            var stack=new TruckTaxiNavigationIntents<string>();
            var trace=new List<string>(); stack.Trace+=trace.Add;
            stack.Set(TruckTaxiNavigationLayer.RideFinal,"ride","foundry","final");
            stack.Set(TruckTaxiNavigationLayer.PassengerDiversion,"passenger","skyline","scenic");
            stack.Set(TruckTaxiNavigationLayer.PlayerService,"player-service","restroom","restroom");
            Assert.AreEqual("restroom",stack.Active.TargetId);
            Assert.IsTrue(stack.Release(TruckTaxiNavigationLayer.PlayerService,"player-service","restroom"));
            Assert.AreEqual("skyline",stack.Active.TargetId);
            Assert.IsTrue(stack.Release(TruckTaxiNavigationLayer.PassengerDiversion,"passenger","skyline"));
            Assert.AreEqual("foundry",stack.Active.TargetId);
            Assert.That(trace,Has.Some.StartsWith("NAV SUSPEND PassengerDiversion"));
            Assert.That(trace,Has.Some.StartsWith("NAV RESTORE PassengerDiversion"));
        }
        [Test]
        public void ReplacementAndLateCompletionCannotPopNewServiceLease()
        {
            var stack=new TruckTaxiNavigationIntents<string>();
            var old=stack.Set(TruckTaxiNavigationLayer.PlayerService,"player-service","stop","old");
            var next=stack.Set(TruckTaxiNavigationLayer.PlayerService,"player-service","stop","new");
            Assert.IsFalse(stack.Release(old.Layer,old.OwnerId,old.TargetId,old.Token));
            Assert.AreSame(next,stack.Active);
            Assert.IsFalse(stack.Release(next.Layer,"unrelated",next.TargetId));
            Assert.IsTrue(stack.Release(next.Layer,next.OwnerId,next.TargetId,next.Token));
            Assert.IsNull(stack.Active);
        }
        [Test]
        public void ReplacingServiceUnderEmergencyDoesNotDisplaceEmergency()
        {
            var stack=new TruckTaxiNavigationIntents<string>();
            stack.Set(TruckTaxiNavigationLayer.RideFinal,"ride","final","final");
            stack.Set(TruckTaxiNavigationLayer.PlayerService,"player-service","restroom","restroom");
            var emergency=stack.Set(TruckTaxiNavigationLayer.Emergency,"recovery","tow","tow");
            stack.Set(TruckTaxiNavigationLayer.PlayerService,"player-service","gas","gas");
            Assert.AreSame(emergency,stack.Active);
            stack.Release(emergency.Layer,emergency.OwnerId);
            Assert.AreEqual("gas",stack.Active.TargetId);
        }
        [Test]
        public void AdapterPreservesDiversionAndFinalWhenServiceIsSelectedAndCompleted()
        {
            var root=new GameObject("Navigation fixture"); var bay=new GameObject("Restroom bay");
            TruckTaxiServicePoint service=null;
            try
            {
                var gps=root.AddComponent<TruckTaxiGPSAdapter>();
                Set(gps,"navigation",new LwsNavigationService()); Set(gps,"player",root.transform);
                var final=root.AddComponent<TruckTaxiRideLocation>(); final.locationId="foundry";
                var stop=root.AddComponent<TruckTaxiStopObjectivePoint>(); stop.stableId="skyline";
                service=bay.AddComponent<TruckTaxiServicePoint>(); service.stableId="restroom";
                service.capabilities=TruckTaxiServiceCapability.Restroom;
                NotifyServiceLifecycle(service,"OnEnable");
                gps.SetRideDestination(final); gps.SetStopDestination(stop);
                Assert.IsTrue(gps.TrySetServiceDestination("restroom","Restroom",bay.transform.position,TruckTaxiMapMarkerType.Bathroom));
                Assert.IsTrue(gps.CanSetMapServiceDestination,"Passenger presence must not block manual routing.");
                Assert.AreEqual(TruckTaxiNavigationLayer.PlayerService,gps.ActiveLayer);
                gps.SetRideDestination(final);
                Assert.AreEqual("restroom",gps.TargetId);
                Assert.IsFalse(gps.CompleteServiceDestination("other"));
                Assert.IsTrue(gps.CompleteServiceDestination("restroom"));
                Assert.AreEqual("skyline",gps.TargetId);
                gps.ClearStopDestination("skyline");
                Assert.AreEqual("foundry",gps.TargetId);
            }
            finally
            {
                if(service!=null) NotifyServiceLifecycle(service,"OnDisable");
                Object.DestroyImmediate(bay); Object.DestroyImmediate(root);
            }
        }
        [Test]
        public void ServiceArrivalDoesNotCompleteTheInteractionAndFailedReplacementKeepsOwner()
        {
            var root=new GameObject("Navigation arrival"); var bay=new GameObject("Service");
            TruckTaxiServicePoint service=null;
            try
            {
                var gps=root.AddComponent<TruckTaxiGPSAdapter>();
                Set(gps,"navigation",new LwsNavigationService()); Set(gps,"player",root.transform);
                service=bay.AddComponent<TruckTaxiServicePoint>(); service.stableId="restroom";
                service.capabilities=TruckTaxiServiceCapability.Restroom;
                NotifyServiceLifecycle(service,"OnEnable");
                Assert.IsTrue(gps.TrySetServiceDestination("restroom","Restroom",Vector3.zero,TruckTaxiMapMarkerType.Bathroom));
                typeof(TruckTaxiGPSAdapter).GetMethod("LateUpdate",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(gps,null);
                Assert.AreEqual("restroom",gps.TargetId,"Arrival is not successful restroom use.");
                Assert.IsFalse(gps.TrySetMapServiceDestination("unreachable","Unreachable",Vector3.right*100));
                Assert.AreEqual("restroom",gps.TargetId);
                gps.ClearRideDestination();
                Assert.AreEqual("restroom",gps.TargetId,"Offer decline cannot clear services.");
            }
            finally
            {
                if(service!=null) NotifyServiceLifecycle(service,"OnDisable");
                Object.DestroyImmediate(bay); Object.DestroyImmediate(root);
            }
        }
        [TestCase(false)]
        [TestCase(true)]
        public void CanonicalHeadingIgnoresSteeringAndCabSway(bool northUp)
        {
            var truck=new GameObject("Truck"); var wheel=new GameObject("Steering wheel"); var target=new GameObject("Objective");
            try
            {
                truck.AddComponent<Rigidbody>(); wheel.transform.SetParent(truck.transform);
                target.transform.position=Vector3.forward*200;
                var marker=target.AddComponent<TruckTaxiMapMarker>(); marker.source=target.transform;
                Transform heading=TruckTaxiGPSAdapter.CanonicalTruck(wheel.transform);
                var before=Project(marker.Position,heading,northUp);
                wheel.transform.localRotation=Quaternion.Euler(20,90,180);
                Assert.AreSame(truck.transform,heading);
                Assert.AreEqual(before,Project(marker.Position,heading,northUp));
                truck.transform.rotation=Quaternion.Euler(0,40,0);
                if(northUp) Assert.AreEqual(before,Project(marker.Position,heading,northUp));
                else Assert.AreNotEqual(before,Project(marker.Position,heading,northUp));
            }
            finally { Object.DestroyImmediate(target); Object.DestroyImmediate(truck); }
        }
        private static Vector3 Project(Vector3 point,Transform truck,bool northUp) =>
            (northUp ? Quaternion.identity : Quaternion.Euler(0,-truck.eulerAngles.y,0))*(point-truck.position);
        [Test]
        public void MapFocusDoesNotRouteSubmitDoesAndReopenRestoresActiveIdentity()
        {
            var root=new GameObject("Map selection");
            try
            {
                var gps=root.AddComponent<TruckTaxiGPSAdapter>();
                Set(gps,"navigation",new LwsNavigationService()); Set(gps,"player",root.transform);
                var markers=root.AddComponent<TruckTaxiMapMarkers>();
                Set(gps,"<MapMarkers>k__BackingField",markers);
                markers.RegisterRegionalPoint("first","First","Place",Vector3.zero,TruckTaxiMapMarkerType.Place,true);
                markers.RegisterRegionalPoint("second","Second","Place",Vector3.zero,TruckTaxiMapMarkerType.Place,true);
                var map=root.AddComponent<TruckTaxiFullMapPresenter>(); Set(map,"gps",gps);
                typeof(TruckTaxiFullMapPresenter).GetMethod("RebuildPlaces",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(map,null);
                Assert.IsTrue(map.FocusPlace("first"));
                Assert.IsFalse(gps.HasNavigationTarget,"Focus/hover is not route submission.");
                Assert.IsTrue(map.TryRoutePlace("first"));
                Assert.AreEqual("first",gps.TargetId);
                Assert.IsTrue(map.FocusPlace("second"));
                Assert.AreEqual("first",gps.TargetId,"Focus must leave active GPS untouched.");
                map.RestoreActiveSelection();
                Assert.AreEqual("first",map.FocusedPlaceId);
                Assert.IsTrue(map.TryRoutePlace("second"));
                Assert.AreEqual("second",gps.TargetId);
            }
            finally { Object.DestroyImmediate(root); }
        }
        private static void Set(object target,string name,object value) =>
            target.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(target,value);
        private static void NotifyServiceLifecycle(TruckTaxiServicePoint service,string method) =>
            typeof(TruckTaxiServicePoint).GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(service,null);
    }
}

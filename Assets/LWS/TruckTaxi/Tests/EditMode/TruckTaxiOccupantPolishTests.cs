using NUnit.Framework;
using UnityEngine;

namespace LWS.TruckTaxi.Tests
{
    [Category("TaxiPolish")]
    public sealed class TruckTaxiOccupantPolishTests
    {
        [Test]
        public void SeatPlacementPreservesApparentScaleUnderScaledCab()
        {
            var truck=new GameObject("Truck");
            var cab=new GameObject("Cab");
            var actor=GameObject.CreatePrimitive(PrimitiveType.Cube);
            var profile=ScriptableObject.CreateInstance<TruckTaxiSeatProfile>();
            try
            {
                cab.transform.SetParent(truck.transform,false);
                cab.transform.localScale=new Vector3(1.6f,1.4f,1.3f);
                profile.mountPath="Cab";
                var standing=actor.transform.lossyScale;
                float standingBounds=actor.GetComponent<Renderer>().bounds.size.y;
                TruckTaxiSeatProfile.PlaceActor(actor.transform,truck.transform,profile,false,standing);
                Assert.AreEqual(.88f,actor.transform.lossyScale.x,.001f);
                Assert.AreEqual(.88f,actor.transform.lossyScale.y,.001f);
                Assert.AreEqual(.88f,actor.transform.lossyScale.z,.001f);
                Assert.AreEqual(standingBounds*.88f,actor.GetComponent<Renderer>().bounds.size.y,.001f);
                Assert.AreEqual(profile.localPosition,actor.transform.localPosition);
            }
            finally
            {
                Object.DestroyImmediate(actor);
                Object.DestroyImmediate(truck);
                Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void PairedSeatUsesSeparateCenterPerchAndSmallerScale()
        {
            var truck=new GameObject("Truck");
            var first=new GameObject("Primary");
            var second=new GameObject("Secondary");
            var profile=ScriptableObject.CreateInstance<TruckTaxiSeatProfile>();
            try
            {
                profile.mountPath="";
                TruckTaxiSeatProfile.PlaceActor(first.transform,truck.transform,profile,false,Vector3.one);
                TruckTaxiSeatProfile.PlaceActor(second.transform,truck.transform,profile,true,Vector3.one);
                Assert.AreNotEqual(first.transform.localPosition,second.transform.localPosition);
                Assert.AreEqual(profile.localPosition+profile.secondarySeatOffset,second.transform.localPosition);
                Assert.AreEqual(.84f,second.transform.lossyScale.x,.001f);
            }
            finally
            {
                Object.DestroyImmediate(first);
                Object.DestroyImmediate(second);
                Object.DestroyImmediate(truck);
                Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void IllicitStopCannotCompleteBeforeControlledPresentation()
        {
            var owner=new GameObject("Runtime");
            var definition=ScriptableObject.CreateInstance<PassengerRequestDefinition>();
            try
            {
                definition.requestType=TaxiRequestType.IllicitStop;
                var request=new TaxiRequestProgress(definition,1f);
                var runtime=owner.AddComponent<TruckTaxiPassengerRuntime>();
                Assert.IsFalse(runtime.StopPresentationReady(request));
            }
            finally
            {
                Object.DestroyImmediate(owner);
                Object.DestroyImmediate(definition);
            }
        }
    }
}

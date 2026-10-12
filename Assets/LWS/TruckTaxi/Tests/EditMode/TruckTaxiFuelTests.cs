using NUnit.Framework;
using UnityEngine;

namespace LWS.TruckTaxi.Tests
{
    public sealed class TruckTaxiFuelTests
    {
        [Test] public void RescueCostsDoubleNormalFullTank()
        { Assert.AreEqual(24000,TruckTaxiFuelController.RescueCost(80,150)); }
        [Test] public void RefuelRequiresStationarySameLevelInsideBay()
        {
            var go=new GameObject("Fuel test");
            try
            {
                var station=go.AddComponent<TruckTaxiGasStationPoint>(); station.stableId="test.gas";
                Assert.IsTrue(station.CanRefuel(Vector3.zero,.4f));
                Assert.IsFalse(station.CanRefuel(Vector3.zero,1));
                Assert.IsFalse(station.CanRefuel(Vector3.up*20,0));
                Assert.IsFalse(station.CanRefuel(Vector3.right*20,0));
                Assert.IsFalse(station.CanRefuel(Vector3.zero,float.NaN));
            }
            finally { Object.DestroyImmediate(go); }
        }
        [Test] public void NamedPatienceLevelsPreserveNormalAndOrdering()
        {
            var passenger=ScriptableObject.CreateInstance<PassengerProfile>();
            try
            {
                Assert.AreEqual(1,passenger.EffectivePickupPatience);
                passenger.pickupPatience=TruckTaxiPickupPatience.VeryImpatient;
                Assert.Less(passenger.EffectivePickupPatience,1);
                passenger.pickupPatience=TruckTaxiPickupPatience.VeryPatient;
                Assert.Greater(passenger.EffectivePickupPatience,1);
            }
            finally { Object.DestroyImmediate(passenger); }
        }
        [Test] public void ActualFillerHighlightRestoresMaterialsOnReturn()
        {
            var fillerObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard")) { color = Color.red };
            try
            {
                var renderer = fillerObject.GetComponent<Renderer>(); renderer.sharedMaterial = material;
                var filler = fillerObject.AddComponent<TruckTaxiFuelFiller>();
                filler.SetHighlighted(true);
                Assert.IsTrue(filler.Highlighted);
                Assert.AreNotSame(material, renderer.sharedMaterial);
                var highlight = renderer.sharedMaterial;
                filler.SetHighlighted(true);
                Assert.AreSame(highlight, renderer.sharedMaterial, "Holding the nozzle must not churn materials.");
                Assert.AreEqual(Color.red, material.color, "Never edit the authored/shared material.");
                filler.SetHighlighted(false);
                Assert.IsFalse(filler.Highlighted);
                Assert.AreSame(material, renderer.sharedMaterial);
                Assert.IsTrue(highlight == null, "EditMode return must immediately dispose the owned clone.");
                // Automatic MonoBehaviour disable delivery is covered in PlayMode, not EditMode.
                filler.SetHighlighted(true);
                highlight = renderer.sharedMaterial;
                filler.SetHighlighted(false);
                filler.SetHighlighted(false);
                Assert.IsFalse(filler.Highlighted);
                Assert.AreSame(material, renderer.sharedMaterial);
                Assert.IsTrue(highlight == null);
                Assert.AreEqual(Color.red, material.color);
            }
            finally
            {
                if (fillerObject != null)
                {
                    fillerObject.GetComponent<TruckTaxiFuelFiller>()?.SetHighlighted(false);
                    Object.DestroyImmediate(fillerObject);
                }
                Object.DestroyImmediate(material);
            }
        }
    }
}

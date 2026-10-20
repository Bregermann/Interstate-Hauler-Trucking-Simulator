using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEditor.SceneManagement;
namespace LWS.TruckSports.Tests
{
    public sealed class HillSurfaceTests
    {
        [Test] public void AllVisibleHillBridgeEdgeRailsHaveMatchingSolidColliders()
        {
            var scene=EditorSceneManager.OpenScene("Assets/TruckKingOfTheHill/Scenes/TruckKingOfTheHill.unity");
            var rails=UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include).Where(r=>r.gameObject.scene==scene&&r.name=="Visible continuous bridge edge rail").ToArray();
            Assert.That(rails.Length,Is.EqualTo(16));foreach(var rail in rails){var collider=rail.GetComponent<BoxCollider>();Assert.That(collider,Is.Not.Null);Assert.That(collider.isTrigger,Is.False);Assert.That(Vector3.Distance(collider.bounds.center,rail.bounds.center),Is.LessThan(.001f));Assert.That(Vector3.Distance(collider.bounds.size,rail.bounds.size),Is.LessThan(.002f));}
        }
        [TestCase("TruckRedLightGreenLight", "Continuous competition runway")]
        [TestCase("TruckHotPotato", "Continuous modeled stadium arena base")]
        [TestCase("TruckBalloonBattle", "Broad plum battle pad")]
        [TestCase("TruckWhackAMole", "Continuous navigable dirt arena")]
        [TestCase("TruckKingOfTheHill", "Physical stratified CROWN PLATEAU")]
        [TestCase("TruckSeesaw", "Physical weight-responsive suspended wobble deck")]
        public void ActualDrivingSurfaceUsesExistingSnowMetadata(string folder, string name)
        {
            var scene = EditorSceneManager.OpenScene("Assets/" + folder + "/Scenes/" + folder + ".unity");
            var marker = Type.GetType("LWS.TruckTaxi.TruckTaxiSurface, LWS.TruckTaxi.Runtime", true);
            var surfaces = UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsInactive.Include).Where(c => c.gameObject.scene == scene && c.name.StartsWith(name, StringComparison.Ordinal) && !c.isTrigger && (c.attachedRigidbody == null || folder == "TruckSeesaw") && c.GetComponent<Renderer>() != null).ToArray();
            Assert.That(surfaces.Length, Is.GreaterThan(0), folder + " actual collider");
            foreach (var surface in surfaces) Assert.That(surface.GetComponent(marker), Is.Not.Null, surface.name + " must be recognized by the existing Weatherade adapter.");
        }
    }
}

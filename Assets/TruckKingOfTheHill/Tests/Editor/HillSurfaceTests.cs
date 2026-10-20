using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEditor.SceneManagement;
namespace LWS.TruckSports.Tests
{
    public sealed class HillSurfaceTests
    {
        [TestCase("TruckRedLightGreenLight", "Continuous competition runway")]
        [TestCase("TruckHotPotato", "Continuous modeled stadium arena base")]
        [TestCase("TruckBalloonBattle", "Broad plum battle pad")]
        [TestCase("TruckWhackAMole", "Continuous navigable dirt arena")]
        [TestCase("TruckKingOfTheHill", "Physical stratified CROWN PLATEAU")]
        public void ActualDrivingSurfaceUsesExistingSnowMetadata(string folder, string name)
        {
            var scene = EditorSceneManager.OpenScene("Assets/" + folder + "/Scenes/" + folder + ".unity");
            var marker = Type.GetType("LWS.TruckTaxi.TruckTaxiSurface, LWS.TruckTaxi.Runtime", true);
            var surfaces = UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsInactive.Include).Where(c => c.gameObject.scene == scene && c.name.StartsWith(name, StringComparison.Ordinal) && !c.isTrigger && c.attachedRigidbody == null && c.GetComponent<Renderer>() != null).ToArray();
            Assert.That(surfaces.Length, Is.GreaterThan(0), folder + " actual collider");
            foreach (var surface in surfaces) Assert.That(surface.GetComponent(marker), Is.Not.Null, surface.name + " must be recognized by the existing Weatherade adapter.");
        }
    }
}

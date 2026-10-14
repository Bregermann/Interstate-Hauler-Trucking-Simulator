using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LWS.TruckTaxi.Tests
{
    public sealed class TruckTaxiEnvironmentSnowTests
    {
        private Scene scene;
        private GameObject root;
        private TruckTaxiSnowSurface surface;
        private readonly List<Object> owned = new List<Object>();

        [SetUp]
        public void SetUp()
        {
            Assert.IsFalse(Application.isPlaying);
            // Unity Test Runner owns/restores the scene setup and supplies an untitled test scene.
            scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            root = new GameObject("Snow environment fixture");
            SceneManager.MoveGameObjectToScene(root, scene);
            surface = root.AddComponent<TruckTaxiSnowSurface>();
        }

        [TearDown]
        public void TearDown()
        {
            if (root != null) Object.DestroyImmediate(root);
            foreach (Object item in owned) if (item != null) Object.DestroyImmediate(item);
            owned.Clear();
            if (scene.IsValid()) EditorSceneManager.CloseScene(scene, true);
        }

        [Test]
        public void SharedAdaptationPreservesSlotsSourceAndMaterialDetail()
        {
            Material source = Material("Universal Render Pipeline/Lit", "Stone");
            source.SetFloat("_BumpScale", .73f);
            source.SetFloat("_DetailAlbedoMapScale", .41f);
            source.SetColor("_BaseColor", Color.green);
            source.EnableKeyword("_NORMALMAP");
            Material glass = Material("Universal Render Pipeline/Lit", "Glass");
            MeshRenderer first = Renderer("Outdoor wall", source, glass);
            MeshRenderer second = Renderer("Another wall", source);
            Bind();
            Material adapted = first.sharedMaterials[0];
            Assert.AreNotSame(source, adapted);
            Assert.AreSame(adapted, second.sharedMaterial);
            Assert.AreSame(glass, first.sharedMaterials[1]);
            Assert.AreEqual(.73f, adapted.GetFloat("_BumpScale"));
            Assert.AreEqual(.41f, adapted.GetFloat("_DetailAlbedoMapScale"));
            Assert.IsTrue(adapted.IsKeywordEnabled("_NORMALMAP"));
            Assert.AreEqual(Color.green, source.GetColor("_BaseColor"));
            Assert.AreEqual("Universal Render Pipeline/Lit", source.shader.name);
            Assert.AreEqual(1, surface.SharedEnvironmentMaterialCount);
            surface.RefreshStreamedSurfaces();
            Assert.AreSame(source, first.sharedMaterials[0]);
            Assert.AreSame(glass, first.sharedMaterials[1]);
            Assert.AreSame(source, second.sharedMaterial);
            Assert.Zero(surface.SharedEnvironmentMaterialCount);
        }

        [Test]
        public void GlassTransparentAndInteriorStayAuthored()
        {
            Material transparent = Material("Universal Render Pipeline/Lit", "Clear panel");
            transparent.SetFloat("_Surface", 1);
            Assert.IsFalse(TruckTaxiSnowSurface.IsSnowEligibleMaterial(transparent));
            Material opaque = Material("Universal Render Pipeline/Lit", "Brick");
            MeshRenderer interior = Renderer("Interior ceiling", opaque);
            MeshRenderer glass = Renderer("Window", Material("Universal Render Pipeline/Lit", "Window glass"));
            Material originalGlass = glass.sharedMaterial;
            Bind();
            Assert.AreSame(opaque, interior.sharedMaterial);
            Assert.AreSame(originalGlass, glass.sharedMaterial);
            opaque.renderQueue = 2450;
            opaque.SetFloat("_AlphaClip", 1);
            Assert.IsTrue(TruckTaxiSnowSurface.IsSnowEligibleMaterial(opaque));
        }

        [Test]
        public void TerrainUsesNativeSnowWithoutChangingTerrainDataAndRestoresDensity()
        {
            var data = new TerrainData { heightmapResolution = 33, size = new Vector3(32, 10, 32) };
            owned.Add(data);
            var terrainObject = new GameObject("Snow terrain", typeof(Terrain), typeof(TerrainCollider));
            terrainObject.transform.SetParent(root.transform);
            Terrain terrain = terrainObject.GetComponent<Terrain>();
            terrain.terrainData = data;
            terrainObject.GetComponent<TerrainCollider>().terrainData = data;
            Material original = Material("Universal Render Pipeline/Terrain/Lit", "Terrain source");
            terrain.materialTemplate = original;
            terrain.detailObjectDensity = .75f;
            surface.SetCoverage(1);
            Bind();
            Assert.AreSame(data, terrain.terrainData);
            Assert.AreEqual("NOT_Lonely/Weatherade/Snow Coverage (Terrain)", terrain.materialTemplate.shader.name);
            Assert.Less(terrain.detailObjectDensity, .75f);
            Assert.AreEqual(1, surface.TerrainSnowSurfaceCount);
            Physics.SyncTransforms();
            Assert.IsTrue(surface.IsSnowCoveredOffRoad(terrainObject.GetComponent<Collider>(), new Vector3(8, 0, 8), null));
            surface.RefreshStreamedSurfaces();
            Assert.AreSame(original, terrain.materialTemplate);
            Assert.AreEqual(.75f, terrain.detailObjectDensity);
            Assert.AreSame(data, terrain.terrainData);
        }

        [Test]
        public void UnmarkedGroundContactQualifiesButShelterAndRoadsDoNot()
        {
            MeshRenderer ground = Renderer("Grass ground", Material("Universal Render Pipeline/Lit", "Grass"));
            ground.gameObject.AddComponent<BoxCollider>().size = new Vector3(10, .1f, 10);
            Bind();
            Physics.SyncTransforms();
            Collider contact = ground.GetComponent<Collider>();
            Assert.IsTrue(surface.IsSnowCoveredOffRoad(contact, Vector3.up * .05f, null));
            var roof = new GameObject("Shelter", typeof(BoxCollider));
            roof.transform.SetParent(root.transform);
            roof.transform.position = Vector3.up * 3;
            roof.GetComponent<BoxCollider>().size = new Vector3(10, .2f, 10);
            Physics.SyncTransforms();
            Assert.IsFalse(surface.IsSnowCoveredOffRoad(contact, Vector3.up * .05f, null));
            Object.DestroyImmediate(roof);
            ground.gameObject.AddComponent<TruckTaxiSurface>().isRoad = true;
            Assert.IsFalse(surface.IsSnowCoveredOffRoad(contact, Vector3.up * .05f, null));
        }

        [Test]
        public void RestoredCoverageRebindsCurrentGeometryAndDisableReleasesAdapters()
        {
            Material source = Material("Universal Render Pipeline/Lit", "Stone");
            MeshRenderer first = Renderer("Ground", source);
            surface.SetCoverage(.8f);
            Bind();
            Assert.IsTrue(surface.EnvironmentBindingsReady);
            MeshRenderer streamed = Renderer("Streamed ground", source);
            surface.RefreshStreamedSurfaces();
            Assert.AreSame(source, first.sharedMaterial);
            Assert.IsFalse(surface.EnvironmentBindingsReady);
            surface.SetCoverage(.3f);
            Bind();
            Assert.AreEqual(.3f, surface.Coverage01);
            Assert.AreEqual(.3f, Shader.GetGlobalFloat("_TaxiSnowCoverage"));
            Assert.AreNotSame(source, first.sharedMaterial);
            Assert.AreSame(first.sharedMaterial, streamed.sharedMaterial);
            Assert.IsTrue(surface.EnvironmentBindingsReady);
            Assert.Zero(surface.PendingEnvironmentBindingCount);
            surface.enabled = false;
            // Non-ExecuteAlways behaviours do not receive runtime lifecycle messages in EditMode.
            typeof(TruckTaxiSnowSurface).GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(surface, null);
            Assert.AreSame(source, first.sharedMaterial);
            Assert.AreSame(source, streamed.sharedMaterial);
            Assert.Zero(surface.SharedEnvironmentMaterialCount);
            Assert.Zero(Shader.GetGlobalFloat("_TaxiSnowCoverage"));
        }

        [Test]
        public void BindingBudgetAndDryTrackThresholdRemainBounded()
        {
            Material source = Material("Universal Render Pipeline/Lit", "Ground");
            for (int i = 0; i < 70; i++) Renderer("Grass " + i, source);
            Bind();
            Assert.AreEqual(64, surface.GroundSnowSurfaceCount);
            Bind();
            Assert.AreEqual(70, surface.GroundSnowSurfaceCount);
            Assert.AreEqual(1, surface.SharedEnvironmentMaterialCount);
            Assert.Zero(TruckTaxiSnowSurface.TrackStrength(.6f, 0));
            Assert.Zero(TruckTaxiSnowSurface.TrackStrength(0, 1));
            Assert.Greater(TruckTaxiSnowSurface.TrackStrength(.2f, .5f), 0);
        }

        [Test]
        public void GroundAndRoofSharingSourceDoNotShareTrackPresentation()
        {
            Material source = Material("Universal Render Pipeline/Lit", "Concrete");
            MeshRenderer ground = Renderer("Parking ground", source);
            MeshRenderer roof = Renderer("Roof", source);
            roof.gameObject.AddComponent<BoxCollider>();
            Bind();
            Assert.AreEqual("LWS/TruckTaxi/Snow/EnvironmentLitGround", ground.sharedMaterial.shader.name);
            Assert.AreEqual("LWS/TruckTaxi/Snow/EnvironmentLitExterior", roof.sharedMaterial.shader.name);
            Assert.AreNotSame(ground.sharedMaterial, roof.sharedMaterial);
            Assert.IsFalse(surface.IsSnowCoveredOffRoad(roof.GetComponent<Collider>(), Vector3.zero, null));
        }

        [Test]
        public void ContactVolumeFollowsSlopeRatherThanFlatteningWheelSegment()
        {
            MeshRenderer tracer = Renderer("Contact", Material("Universal Render Pipeline/Lit", "Trace"));
            MethodInfo place = typeof(TruckTaxiSnowSurface).GetMethod("PlaceTrace", BindingFlags.Static | BindingFlags.NonPublic);
            Vector3 from = Vector3.zero, to = new Vector3(0, 1, 2);
            place.Invoke(null, new object[] { tracer, from, to, .35f, .3f, 0f });
            Assert.Less(Vector3.Angle(tracer.transform.forward, to - from), .01f);
            Assert.AreEqual(.35f, tracer.transform.localScale.x, .001f);
        }

        [TestCase("EnvironmentLit")]
        [TestCase("EnvironmentLitExterior")]
        [TestCase("EnvironmentLitGround")]
        [TestCase("EnvironmentFoliage")]
        [TestCase("EnvironmentTreeFoliage")]
        [TestCase("EnvironmentTreeBark")]
        [TestCase("EnvironmentTreeBillboard")]
        public void AdaptationRetainsVendorDepthShadowAndMotionPasses(string resource)
        {
            Shader shader = Resources.Load<Shader>("TruckTaxi/Snow/" + resource);
            Assert.IsNotNull(shader);
            var material = new Material(shader);
            owned.Add(material);
            Assert.GreaterOrEqual(material.FindPass("ShadowCaster"), 0);
            Assert.GreaterOrEqual(material.FindPass("DepthOnly"), 0);
            Assert.GreaterOrEqual(material.FindPass("MotionVectors"), 0);
        }

        private void Bind() => typeof(TruckTaxiSnowSurface).GetMethod("BindGround",
            BindingFlags.Instance | BindingFlags.NonPublic).Invoke(surface, null);

        private Material Material(string shader, string name)
        {
            var material = new Material(Shader.Find(shader)) { name = name };
            owned.Add(material);
            return material;
        }

        private MeshRenderer Renderer(string name, params Material[] materials)
        {
            var item = new GameObject(name, typeof(MeshRenderer));
            item.transform.SetParent(root.transform);
            var renderer = item.GetComponent<MeshRenderer>();
            renderer.sharedMaterials = materials;
            return renderer;
        }
    }
}

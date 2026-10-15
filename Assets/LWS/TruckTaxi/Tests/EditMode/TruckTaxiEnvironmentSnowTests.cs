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
        public void SceneUnloadPrunesDeadTargetsWithoutResettingLiveSnowOrTraces()
        {
            Material source = Material("Universal Render Pipeline/Lit", "Grass");
            Material terrainSource = Material("Universal Render Pipeline/Terrain/Lit", "Terrain source");
            MeshRenderer live = Renderer("Live ground", source);
            live.gameObject.AddComponent<TruckTaxiSurface>();
            MeshRenderer dead = Renderer("Dead ground", Material("Universal Render Pipeline/Lit", "Departing grass"));
            dead.gameObject.AddComponent<TruckTaxiSurface>();
            MeshRenderer departing = Renderer("Departing ground", source);
            departing.gameObject.AddComponent<TruckTaxiSurface>();
            Terrain liveTerrain = CreateTerrain("Live terrain", terrainSource);
            Terrain deadTerrain = CreateTerrain("Departing terrain",
                Material("Universal Render Pipeline/Terrain/Lit", "Departing terrain source"));
            surface.SetCoverage(.7f);
            Bind();
            Material liveSnow = live.sharedMaterial;
            Material liveTerrainSnow = liveTerrain.materialTemplate;
            Material deadSnow = dead.sharedMaterial;
            Material deadTerrainSnow = deadTerrain.materialTemplate;
            float liveDensity = liveTerrain.detailObjectDensity;
            Assert.AreSame(liveSnow, departing.sharedMaterial);
            Assert.AreEqual(3, surface.GroundSnowSurfaceCount);
            Assert.AreEqual(2, surface.TerrainSnowSurfaceCount);
            Assert.AreEqual(4, surface.SharedEnvironmentMaterialCount);

            MeshRenderer tracer = Renderer("Wheel trace", source);
            tracer.gameObject.layer = 29;
            Field<Dictionary<int, MeshRenderer>>("wheelTracers")[0] = tracer;
            Scene unloaded = EditorSceneManager.NewPreviewScene();
            try
            {
                departing.transform.SetParent(null);
                SceneManager.MoveGameObjectToScene(departing.gameObject, unloaded);
                deadTerrain.transform.SetParent(null);
                SceneManager.MoveGameObjectToScene(deadTerrain.gameObject, unloaded);
                Object.DestroyImmediate(dead.gameObject);
                EditorSceneManager.ClosePreviewScene(unloaded);
                // EditMode does not dispatch this non-ExecuteAlways behaviour's runtime callbacks.
                NotifySceneUnloaded(unloaded);
                NotifySceneUnloaded(unloaded);

                Assert.AreSame(liveSnow, live.sharedMaterial);
                Assert.IsTrue(liveSnow != null);
                Assert.AreSame(liveTerrainSnow, liveTerrain.materialTemplate);
                Assert.AreEqual(liveDensity, liveTerrain.detailObjectDensity);
                Assert.AreEqual(.7f, surface.Coverage01);
                Assert.AreEqual(.7f, Shader.GetGlobalFloat("_TaxiSnowCoverage"));
                Assert.AreEqual(1, surface.ActiveWheelTraceSourceCount);
                Assert.AreEqual(1, surface.GroundSnowSurfaceCount);
                Assert.AreEqual(1, surface.TerrainSnowSurfaceCount);
                Assert.AreEqual(1, surface.CacheTargetCount);
                Assert.AreEqual(1, surface.UniqueCacheTargetCount);
                Assert.AreEqual(1, Field<List<MeshRenderer>>("environmentTargets").Count);
                Assert.AreEqual(2, surface.SharedEnvironmentMaterialCount);
                Assert.IsTrue(deadSnow == null);
                Assert.IsTrue(deadTerrainSnow == null);
                Assert.IsFalse(surface.EnvironmentBindingsReady, "Discovery must be invalidated without removing live bindings.");
                Bind();
                Assert.IsTrue(surface.EnvironmentBindingsReady);
                Assert.AreSame(liveSnow, live.sharedMaterial);
                Assert.AreEqual(1, surface.ActiveWheelTraceSourceCount);

                surface.RefreshStreamedSurfaces();
                Assert.AreSame(source, live.sharedMaterial);
                Assert.AreSame(terrainSource, liveTerrain.materialTemplate);
                Assert.AreEqual(.75f, liveTerrain.detailObjectDensity);
                Assert.Zero(surface.ActiveWheelTraceSourceCount);
                Assert.Zero(surface.SharedEnvironmentMaterialCount);
            }
            finally
            {
                if (unloaded.IsValid()) EditorSceneManager.ClosePreviewScene(unloaded);
            }
        }

        [Test]
        public void SceneUnloadCompactsPendingBindingsWithoutSkippingOrRebindingLiveTargets()
        {
            Material source = Material("Universal Render Pipeline/Lit", "Grass");
            for (int i = 0; i < 70; i++) Renderer("Grass " + i, source);
            surface.SetCoverage(.6f);
            Bind();
            List<MeshRenderer> targets = Field<List<MeshRenderer>>("environmentTargets");
            MeshRenderer live = targets[1];
            Material liveSnow = live.sharedMaterial;
            Object.DestroyImmediate(targets[0].gameObject);
            Object.DestroyImmediate(targets[69].gameObject);
            Scene unloaded = EditorSceneManager.NewPreviewScene();
            try
            {
                EditorSceneManager.ClosePreviewScene(unloaded);
                NotifySceneUnloaded(unloaded);
                NotifySceneUnloaded(unloaded);
                Assert.AreEqual(68, targets.Count);
                Assert.AreEqual(63, surface.GroundSnowSurfaceCount);
                Assert.AreEqual(5, surface.PendingEnvironmentBindingCount);
                Assert.AreSame(liveSnow, live.sharedMaterial);

                Renderer("New streamed grass", source);
                Bind();
                Assert.AreEqual(69, targets.Count);
                Assert.AreEqual(69, surface.GroundSnowSurfaceCount);
                Assert.Zero(surface.PendingEnvironmentBindingCount);
                Assert.IsTrue(surface.EnvironmentBindingsReady);
                Assert.AreSame(liveSnow, live.sharedMaterial);
                Assert.AreEqual(1, surface.SharedEnvironmentMaterialCount);
                NotifySceneUnloaded(unloaded);
                Bind();
                Assert.AreEqual(69, targets.Count);
                Assert.AreEqual(69, Field<HashSet<int>>("environmentTargetIds").Count);
                Assert.AreEqual(69, surface.GroundSnowSurfaceCount);
            }
            finally
            {
                if (unloaded.IsValid()) EditorSceneManager.ClosePreviewScene(unloaded);
            }
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
        public void ReusedDepthCameraTargetsAreReboundEvenWhenItsRendererWasAlreadyBound()
        {
            System.Type feature = VendorType("SRS_RenderDepthWithReplacement");
            FieldInfo field = feature.GetField("rtHandles", BindingFlags.Static | BindingFlags.Public);
            object previous = field.GetValue(null);
            var targets = (System.Collections.IDictionary)System.Activator.CreateInstance(field.FieldType);
            field.SetValue(null, targets);
            var cameraObject = new GameObject("SRS_TraceObjsCam", typeof(Camera));
            cameraObject.transform.SetParent(root.transform);
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.enabled = false;
            var stale = new RenderTexture(16, 16, 16, RenderTextureFormat.Depth);
            var current = new RenderTexture(16, 16, 16, RenderTextureFormat.Depth);
            var replacement = new RenderTexture(16, 16, 16, RenderTextureFormat.Depth);
            owned.Add(stale); owned.Add(current); owned.Add(replacement);
            object staleHandle = AllocateDepthHandle(stale);
            targets[camera] = staleHandle;
            camera.targetTexture = current;
            Field<HashSet<Camera>>("boundDepthCameras").Add(camera);
            MethodInfo bind = typeof(TruckTaxiSnowSurface).GetMethod("BindDepthCameraRenderer", BindingFlags.Instance | BindingFlags.NonPublic);
            try
            {
                bind.Invoke(surface, new object[] { camera });
                object repaired = targets[camera];
                Assert.AreNotSame(staleHandle, repaired);
                Assert.AreSame(current, HandleTexture(repaired));
                Assert.AreSame(stale, HandleTexture(staleHandle), "Do not release vendor-owned handles.");
                bind.Invoke(surface, new object[] { camera });
                Assert.AreSame(repaired, targets[camera]);

                // Vendor UpdateCameraRTHandlePairs can republish its old camera-keyed registration.
                targets[camera] = staleHandle;
                bind.Invoke(surface, new object[] { camera });
                Assert.AreSame(repaired, targets[camera]);
                Assert.AreEqual(1, Field<System.Collections.IDictionary>("ownedDepthTargets").Count);

                camera.targetTexture = replacement;
                bind.Invoke(surface, new object[] { camera });
                Assert.AreSame(replacement, HandleTexture(targets[camera]));
                Assert.IsNull(HandleTexture(repaired));
                Assert.AreEqual(1, Field<System.Collections.IDictionary>("ownedDepthTargets").Count);
                typeof(TruckTaxiSnowSurface).GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(surface, null);
                Assert.IsFalse(targets.Contains(camera));
                Assert.Zero(Field<System.Collections.IDictionary>("ownedDepthTargets").Count);
                Assert.IsTrue(current != null && replacement != null, "Handle cleanup must not destroy vendor RTs.");
            }
            finally
            {
                typeof(TruckTaxiSnowSurface).GetMethod("ReleaseDepthTargets", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(surface, null);
                staleHandle.GetType().GetMethod("Release").Invoke(staleHandle, null);
                field.SetValue(null, previous);
            }
        }

        [Test]
        public void DepthTargetCleanupLeavesAReplacementVendorBindingUntouched()
        {
            System.Type feature = VendorType("SRS_RenderDepthWithReplacement");
            FieldInfo field = feature.GetField("rtHandles", BindingFlags.Static | BindingFlags.Public);
            object previous = field.GetValue(null);
            var targets = (System.Collections.IDictionary)System.Activator.CreateInstance(field.FieldType);
            field.SetValue(null, targets);
            var cameraObject = new GameObject("SRS_TraceSurfCam", typeof(Camera));
            cameraObject.transform.SetParent(root.transform);
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.enabled = false;
            var texture = new RenderTexture(16, 16, 16, RenderTextureFormat.Depth);
            owned.Add(texture);
            camera.targetTexture = texture;
            object vendorHandle = AllocateDepthHandle(texture);
            try
            {
                typeof(TruckTaxiSnowSurface).GetMethod("BindDepthTarget", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(surface, new object[] { camera });
                object repaired = targets[camera];
                targets[camera] = vendorHandle;
                typeof(TruckTaxiSnowSurface).GetMethod("ReleaseDepthTargets", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(surface, null);
                Assert.AreSame(vendorHandle, targets[camera]);
                Assert.AreSame(texture, HandleTexture(vendorHandle));
                Assert.IsNull(HandleTexture(repaired));
                Assert.Zero(Field<System.Collections.IDictionary>("ownedDepthTargets").Count);
            }
            finally
            {
                typeof(TruckTaxiSnowSurface).GetMethod("ReleaseDepthTargets", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(surface, null);
                vendorHandle.GetType().GetMethod("Release").Invoke(vendorHandle, null);
                field.SetValue(null, previous);
            }
        }

        [TestCase(90f, 512, 1.0546875f)]
        [TestCase(45f, 256, 1.0546875f)]
        [TestCase(90f, 1024, .52734375f)]
        public void ContactDepthUsesThreeSurfaceTexelsWithoutWideningTheTrace(float size, int resolution, float expectedSpan)
        {
            var cameraObject = new GameObject("Surface depth footprint", typeof(Camera));
            cameraObject.transform.SetParent(root.transform);
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.enabled = false;
            camera.orthographic = true; camera.orthographicSize = size; camera.aspect = 1;
            var texture = new RenderTexture(resolution, resolution, 16, RenderTextureFormat.Depth);
            owned.Add(texture); camera.targetTexture = texture;
            typeof(TruckTaxiSnowSurface).GetMethod("CacheContactDepthFootprint", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(surface, new object[] { camera });
            float span = Field<float>("minimumContactDepthSpan");
            Assert.AreEqual(expectedSpan, span, .00001f);

            MeshRenderer depth = Renderer("Contact depth", Material("Universal Render Pipeline/Lit", "Depth"));
            MeshRenderer tracer = Renderer("Wheel trace", depth.sharedMaterial);
            MethodInfo placeDepth = typeof(TruckTaxiSnowSurface).GetMethod("PlaceContactDepth", BindingFlags.Static | BindingFlags.NonPublic);
            MethodInfo placeTrace = typeof(TruckTaxiSnowSurface).GetMethod("PlaceTrace", BindingFlags.Static | BindingFlags.NonPublic);
            Vector3 from = Vector3.zero, to = new Vector3(0, .03f, .06f);
            const float width = .3185f;
            placeDepth.Invoke(null, new object[] { depth, from, to, width, span });
            placeTrace.Invoke(null, new object[] { tracer, from, to, width, .4f, .03f });
            Assert.IsTrue(depth.enabled);
            Assert.GreaterOrEqual(depth.transform.localScale.x, expectedSpan);
            float horizontal = Vector3.ProjectOnPlane(depth.transform.forward, Vector3.up).magnitude;
            Assert.GreaterOrEqual(depth.transform.localScale.z * horizontal + .00001f, expectedSpan);
            Assert.AreEqual(.04f, depth.transform.localScale.y, .00001f);
            Assert.Less(Vector3.Angle(depth.transform.forward, to - from), .01f);
            Assert.Less(Vector3.Distance(depth.transform.position, (from + to) * .5f - Vector3.up * .02f), .00001f);
            Assert.AreEqual(width, tracer.transform.localScale.x, .00001f);
            Assert.AreEqual(Vector3.Distance(from, to) + width * .25f, tracer.transform.localScale.z, .00001f);

            to = new Vector3(0, 1, 3);
            placeDepth.Invoke(null, new object[] { depth, from, to, width, span });
            Assert.AreEqual(to.magnitude + width * 1.6f * .25f, depth.transform.localScale.z, .00001f,
                "Long contact patches must not be shortened by the minimum footprint.");
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

        private static System.Type VendorType(string name) => (System.Type)typeof(TruckTaxiSnowSurface)
            .GetMethod("ResolveType", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { name });

        private static object AllocateDepthHandle(RenderTexture texture) => VendorType("UnityEngine.Rendering.RTHandles")
            .GetMethod("Alloc", new[] { typeof(RenderTexture), typeof(bool) }).Invoke(null, new object[] { texture, false });

        private static RenderTexture HandleTexture(object handle) => (RenderTexture)handle.GetType().GetProperty("rt").GetValue(handle);

        private void NotifySceneUnloaded(Scene unloaded) => typeof(TruckTaxiSnowSurface).GetMethod("OnSceneUnloaded",
            BindingFlags.Instance | BindingFlags.NonPublic).Invoke(surface, new object[] { unloaded });

        private T Field<T>(string name) => (T)typeof(TruckTaxiSnowSurface).GetField(name,
            BindingFlags.Instance | BindingFlags.NonPublic).GetValue(surface);

        private Terrain CreateTerrain(string name, Material source)
        {
            var data = new TerrainData { heightmapResolution = 33, size = new Vector3(32, 10, 32) };
            owned.Add(data);
            var item = new GameObject(name, typeof(Terrain));
            item.transform.SetParent(root.transform);
            Terrain terrain = item.GetComponent<Terrain>();
            terrain.terrainData = data;
            terrain.materialTemplate = source;
            terrain.detailObjectDensity = .75f;
            return terrain;
        }

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

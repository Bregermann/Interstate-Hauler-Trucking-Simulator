using System;
using System.Collections.Generic;
using System.Linq;
using LWS.TruckTaxi.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace LWS.TruckTaxi.Tests.EditMode
{
    public sealed class TruckTaxiWorldGeometryAuditTests
    {
        private const string SceneSeed = "Packages/com.unity.render-pipelines.universal/Editor/SceneTemplates/Basic.unity";
        private const string ScenePrefix = "Assets/LWS/TruckTaxi/Tests/EditMode/__GeometryAudit_";
        private Scene scene, previous;
        private string sceneAssetPath, sceneAssetGuid;
        private (Scene scene, string path, bool dirty, int[] roots)[] initialScenes;
        private readonly List<Object> temporary = new List<Object>();

        [SetUp]
        public void SetUp()
        {
            scene = default;
            sceneAssetPath = sceneAssetGuid = null;
            previous = SceneManager.GetActiveScene();
            initialScenes = Enumerable.Range(0, SceneManager.sceneCount).Select(SceneManager.GetSceneAt)
                .Where(value => value.isLoaded)
                .Select(value => (scene: value, path: value.path, dirty: value.isDirty,
                    roots: value.GetRootGameObjects().Select(root => root.GetInstanceID()).ToArray())).ToArray();
            try
            {
                // NewScene(Additive) rejects an unsaved runner scene; preview scenes cannot be repaired.
                // Copy/open a saved seed instead, never saving or replacing any existing scene.
                Assert.NotNull(AssetDatabase.LoadAssetAtPath<SceneAsset>(SceneSeed), "Installed URP Basic scene template is required.");
                sceneAssetPath = ScenePrefix + Guid.NewGuid().ToString("N") + ".unity";
                Assert.IsEmpty(AssetDatabase.AssetPathToGUID(sceneAssetPath));
                Assert.IsTrue(AssetDatabase.CopyAsset(SceneSeed, sceneAssetPath));
                sceneAssetGuid = AssetDatabase.AssetPathToGUID(sceneAssetPath);
                Assert.IsNotEmpty(sceneAssetGuid);
                scene = EditorSceneManager.OpenScene(sceneAssetPath, OpenSceneMode.Additive);
                Assert.IsTrue(scene.IsValid() && scene.isLoaded && !EditorSceneManager.IsPreviewScene(scene));
                Assert.IsTrue(SceneManager.SetActiveScene(scene));
                foreach (var root in scene.GetRootGameObjects()) Object.DestroyImmediate(root);
                Assert.Zero(scene.rootCount);
                Undo.IncrementCurrentGroup();
            }
            catch
            {
                CleanupFixture();
                throw;
            }
        }

        [TearDown]
        public void TearDown() => CleanupFixture();

        private void CleanupFixture()
        {
            try
            {
                foreach (var value in temporary) if (value != null) Object.DestroyImmediate(value);
                temporary.Clear();
            }
            finally
            {
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                if (!string.IsNullOrEmpty(sceneAssetGuid))
                {
                    Assert.IsTrue(sceneAssetPath.StartsWith(ScenePrefix, StringComparison.Ordinal));
                    Assert.AreEqual(sceneAssetGuid, AssetDatabase.AssetPathToGUID(sceneAssetPath, AssetPathToGUIDOptions.OnlyExistingAssets),
                        "Refusing to clean up an asset no longer owned by this fixture.");
                    var ownedScene = SceneManager.GetSceneByPath(sceneAssetPath);
                    if (ownedScene.IsValid() && ownedScene.isLoaded)
                        Assert.IsTrue(EditorSceneManager.CloseScene(ownedScene, true));
                    Assert.IsTrue(AssetDatabase.DeleteAsset(sceneAssetPath), "Could not delete the fixture-owned scene asset.");
                    sceneAssetPath = sceneAssetGuid = null;
                }
                scene = default;
                Undo.IncrementCurrentGroup();
            }
        }

        [Test]
        public void FixtureCleanup_PreservesPreviouslyLoadedScenesAndDeletesOnlyItsOwnedAsset()
        {
            string ownedPath = sceneAssetPath;
            var probe = Cube("Editable fixture probe", Vector3.zero, Vector3.one);
            Assert.AreEqual(ownedPath, probe.scene.path);
            Assert.DoesNotThrow(() => TruckTaxiWorldGeometryAuthoring.RequireEditableSceneObject(probe));
            CleanupFixture();
            Assert.IsFalse(SceneManager.GetSceneByPath(ownedPath).isLoaded);
            Assert.IsNull(AssetDatabase.LoadAssetAtPath<SceneAsset>(ownedPath));
            Assert.AreEqual(previous, SceneManager.GetActiveScene());
            foreach (var before in initialScenes)
            {
                Assert.IsTrue(before.scene.IsValid() && before.scene.isLoaded);
                Assert.AreEqual(before.path, before.scene.path);
                Assert.AreEqual(before.dirty, before.scene.isDirty);
                CollectionAssert.AreEquivalent(before.roots,
                    before.scene.GetRootGameObjects().Select(root => root.GetInstanceID()).ToArray());
            }
        }

        [Test]
        public void DuplicateRepair_RemovesOnlyRenderer_IsIdempotent_AndRequiresResidency()
        {
            var keep = Cube("Keep", Vector3.zero, new Vector3(20, 1, 20));
            var duplicate = Cube("Obsolete", Vector3.zero, new Vector3(20, 1, 20));
            var renderer = duplicate.GetComponent<MeshRenderer>();
            var collider = duplicate.GetComponent<BoxCollider>();
            var unrelated = Cube("Unrelated", Vector3.right * 30, Vector3.one);
            Assert.Throws<InvalidOperationException>(() => TruckTaxiWorldGeometryAuthoring.RetireExactDuplicateRenderer(
                keep.GetComponent<MeshRenderer>(), renderer, false));
            Assert.AreEqual(1, TruckTaxiWorldGeometryAuthoring.RetireExactDuplicateRenderer(keep.GetComponent<MeshRenderer>(), renderer, true));
            Assert.IsTrue(collider != null && collider.enabled);
            Assert.NotNull(duplicate.GetComponent<MeshFilter>());
            Assert.NotNull(unrelated.GetComponent<MeshRenderer>());
            Assert.AreEqual(0, TruckTaxiWorldGeometryAuthoring.RetireExactDuplicateRenderer(keep.GetComponent<MeshRenderer>(), renderer, true));
        }

        [Test]
        public void EqualBoundsWithDifferentMesh_IsNeverAutomaticallyRetired()
        {
            var keep = Cube("Keep", Vector3.zero, Vector3.one * 20);
            var other = Cube("Other", Vector3.zero, Vector3.one * 20);
            var clone = Object.Instantiate(other.GetComponent<MeshFilter>().sharedMesh);
            temporary.Add(clone);
            other.GetComponent<MeshFilter>().sharedMesh = clone;
            Assert.IsFalse(TruckTaxiWorldGeometryAuthoring.ExactDuplicate(keep.GetComponent<MeshRenderer>(), other.GetComponent<MeshRenderer>()));
            Assert.Throws<InvalidOperationException>(() => TruckTaxiWorldGeometryAuthoring.RetireExactDuplicateRenderer(
                keep.GetComponent<MeshRenderer>(), other.GetComponent<MeshRenderer>(), true));
        }

        [Test]
        public void GroundTrim_RemovesOnlyFortyMeterOverlap_PreservesUnionAndIsIdempotent()
        {
            var keep = Cube("Regional ground", new Vector3(680, -.55f, 0), new Vector3(640, 1, 1000));
            var trim = Cube("Regional ground", new Vector3(1230, -.55f, 0), new Vector3(540, 1, 1000));
            var a = keep.GetComponent<MeshRenderer>(); var b = trim.GetComponent<MeshRenderer>();
            Assert.IsTrue(TruckTaxiWorldGeometryAuthoring.CoplanarTopOverlap(a.bounds, b.bounds, out float area));
            Assert.That(area, Is.EqualTo(40000).Within(.1f));
            Assert.AreEqual(1, TruckTaxiWorldGeometryAuthoring.TrimRegionalGroundOverlap(a, b, true));
            Assert.That(b.bounds.min.x, Is.EqualTo(a.bounds.max.x).Within(.001f));
            Assert.That(b.bounds.max.x, Is.EqualTo(1500).Within(.001f));
            Assert.That(trim.GetComponent<BoxCollider>().bounds.min.x, Is.EqualTo(1000).Within(.001f));
            Assert.AreEqual(0, TruckTaxiWorldGeometryAuthoring.TrimRegionalGroundOverlap(a, b, true));
            Assert.That(trim.transform.position.y, Is.EqualTo(-.55f));
        }

        [Test]
        public void GroundTrim_UndoAndRedoRetainColliderCoverage()
        {
            var keep = Cube("Regional ground", Vector3.zero, new Vector3(20, 1, 30));
            var trim = Cube("Regional ground", Vector3.right * 15, new Vector3(20, 1, 30));
            Undo.IncrementCurrentGroup();
            TruckTaxiWorldGeometryAuthoring.TrimRegionalGroundOverlap(keep.GetComponent<MeshRenderer>(), trim.GetComponent<MeshRenderer>(), true);
            Undo.FlushUndoRecordObjects();
            Undo.PerformUndo();
            Assert.AreEqual(Vector3.right * 15, trim.transform.position);
            Assert.AreEqual(new Vector3(20, 1, 30), trim.transform.localScale);
            Undo.PerformRedo();
            Physics.SyncTransforms();
            Assert.That(trim.GetComponent<BoxCollider>().bounds.min.x, Is.EqualTo(10).Within(.001f));
        }

        [Test]
        public void GroundTrim_RefusesRoadsAndInteriorOverlapWithoutMutation()
        {
            var keep = Cube("Regional ground", Vector3.zero, new Vector3(10, 1, 100));
            var trim = Cube("EasyRoads baked road", Vector3.zero, new Vector3(30, 1, 100));
            Assert.Throws<InvalidOperationException>(() => TruckTaxiWorldGeometryAuthoring.TrimRegionalGroundOverlap(
                keep.GetComponent<MeshRenderer>(), trim.GetComponent<MeshRenderer>(), true));
            trim.name = "Regional ground";
            Assert.Throws<InvalidOperationException>(() => TruckTaxiWorldGeometryAuthoring.TrimRegionalGroundOverlap(
                keep.GetComponent<MeshRenderer>(), trim.GetComponent<MeshRenderer>(), true));
            Assert.AreEqual(new Vector3(30, 1, 100), trim.transform.localScale);
        }

        [Test]
        public void CoplanarCandidates_RejectSeparatedHeightsAndEdgeTouch()
        {
            var bounds = new Bounds(Vector3.zero, new Vector3(40, 1, 1000));
            Assert.IsFalse(TruckTaxiWorldGeometryAuthoring.CoplanarTopOverlap(bounds, new Bounds(Vector3.up * .01f, bounds.size), out _));
            Assert.IsFalse(TruckTaxiWorldGeometryAuthoring.CoplanarTopOverlap(bounds, new Bounds(Vector3.right * 40, bounds.size), out _));
        }

        [Test]
        public void VehicleAudit_DoesNotCountTriggersDisabledOrWheelColliders()
        {
            var truck = Cube("Visible truck", new Vector3(0, 2, 0), new Vector3(3, 4, 7));
            var collider = truck.GetComponent<BoxCollider>();
            collider.enabled = false;
            var trigger = truck.AddComponent<BoxCollider>(); trigger.isTrigger = true;
            truck.AddComponent<WheelCollider>();
            Physics.SyncTransforms();
            var problems = TruckTaxiWorldColliderReview.VehicleProblems(truck.transform, null);
            Assert.IsTrue(problems.Any(p => p.Contains("No enabled non-trigger")));
            Assert.IsTrue(problems.Any(p => p.Contains("UNVERIFIED")));
        }

        [Test]
        public void VehicleAudit_ChassisOnlyAndIgnoredPlayerPairsAreFlagged()
        {
            var truck = Cube("Visible truck", new Vector3(0, 2, 0), new Vector3(3, 4, 7));
            var collider = truck.GetComponent<BoxCollider>();
            collider.size = new Vector3(1, .15f, 1); collider.center = new Vector3(0, -.4f, 0);
            var player = Cube("Player", Vector3.right * 20, Vector3.one).GetComponent<BoxCollider>();
            player.gameObject.AddComponent<Rigidbody>().isKinematic = true;
            Physics.SyncTransforms();
            Physics.IgnoreCollision(collider, player, true);
            try
            {
                var problems = TruckTaxiWorldColliderReview.VehicleProblems(truck.transform, new[] { player });
                Assert.IsTrue(problems.Any(p => p.Contains("cab/chassis coverage")));
                Assert.IsTrue(problems.Any(p => p.Contains("collision ignored")));
            }
            finally { Physics.IgnoreCollision(collider, player, false); }
        }

        [Test]
        public void LoadedAudit_IsReadOnly_ReportsBothSourcesAndDoesNotClaimRootCause()
        {
            Cube("Large A", Vector3.zero, new Vector3(30, 1, 30));
            Cube("Large B", Vector3.zero, new Vector3(30, 1, 30));
            int count = scene.rootCount;
            var report = TruckTaxiWorldColliderValidator.AuditLoadedWorld();
            Assert.AreEqual(count, scene.rootCount);
            Assert.IsTrue(report.findings.Any(f => f.Contains("Large A") && f.Contains("Large B")));
            StringAssert.Contains("not a confirmed flicker cause", report.limitation);
            StringAssert.Contains("Candidates", report.ToJson());
        }

        [Test]
        public void InstalledParkedUtsPrefab_RepairIsIdempotentAndKeepsVendorBehavioursOff()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TruckTaxiWorldGeometryAuthoring.UtsTruckPath);
            Assert.NotNull(prefab, "Installed UTS source must be present.");
            var depot = new GameObject("Depot"); depot.AddComponent<TruckTaxiRivalDepot>();
            var parked = (GameObject)PrefabUtility.InstantiatePrefab(prefab, depot.transform);
            parked.name = "Parked UTS taxi truck 1";
            parked.transform.rotation = Quaternion.Euler(0, 37, 0);
            foreach (var behaviour in parked.GetComponentsInChildren<MonoBehaviour>(true)) behaviour.enabled = false;
            foreach (var collider in parked.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            var player = Cube("Player body", Vector3.right * 30, Vector3.one).GetComponent<BoxCollider>();
            // Select an already-compatible layer; the repair itself must never mutate the matrix.
            int layer = Enumerable.Range(0, 32).First(i => !Physics.GetIgnoreLayerCollision(i, 9) && !Physics.GetIgnoreLayerCollision(i, 0));
            int children = parked.transform.childCount;
            player.excludeLayers = 1 << layer;
            Assert.Throws<InvalidOperationException>(() => TruckTaxiWorldGeometryAuthoring.RepairParkedUtsTruck(parked, new[] { player }, layer));
            Assert.AreEqual(children, parked.transform.childCount);
            Assert.IsFalse(parked.transform.Find("Box_Collider").GetComponent<BoxCollider>().enabled);
            player.excludeLayers = 0;
            Assert.Throws<InvalidOperationException>(() => TruckTaxiWorldGeometryAuthoring.RequireEditableSceneObject(prefab));
            Assert.Greater(TruckTaxiWorldGeometryAuthoring.RepairParkedUtsTruck(parked, new[] { player }, layer), 0);
            Assert.AreEqual(0, TruckTaxiWorldGeometryAuthoring.RepairParkedUtsTruck(parked, new[] { player }, layer));
            Assert.AreEqual(2, parked.GetComponentsInChildren<Collider>(true).Count(TruckTaxiWorldColliderReview.IsSolid));
            Assert.IsTrue(parked.GetComponentsInChildren<MonoBehaviour>(true).All(b => !b.enabled));
            Assert.IsTrue(parked.GetComponentsInChildren<WheelCollider>(true).All(c => !c.enabled));
            Assert.IsTrue(parked.GetComponent<Rigidbody>().isKinematic);
            Assert.IsTrue(parked.GetComponent<Rigidbody>().detectCollisions);
            var cab = parked.transform.Find(TruckTaxiWorldGeometryAuthoring.CabColliderName).GetComponent<BoxCollider>();
            Assert.Less(cab.size.z, 5, "Cab must not become a whole-truck invisible box.");
            Assert.IsTrue(prefab.transform.Find("Box_Collider").GetComponent<BoxCollider>().enabled);
        }

        private static GameObject Cube(string name, Vector3 position, Vector3 scale)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name; go.transform.position = position; go.transform.localScale = scale;
            Physics.SyncTransforms();
            return go;
        }
    }
}

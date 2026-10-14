using System;
using LWS.TruckTaxi.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using Authoring = LWS.TruckTaxi.Editor.TruckTaxiEdenboroughTerrainAuthoring;

namespace LWS.TruckTaxi.Tests
{
    [Category("TaxiEdenboroughTerrain")]
    public sealed class TruckTaxiEdenboroughTerrainAuthoringTests
    {
        private static TruckTaxiEdenboroughLayout.Road Road(Vector3 a, Vector3 b) =>
            new TruckTaxiEdenboroughLayout.Road("fixture", "Fixture road", "a", "b", "CityStreet", new[] { a, b });

        [Test] public void SixTilesCoverExactLayoutBoundsWithoutGaps()
        {
            Bounds world = TruckTaxiEdenboroughLayout.WorldBounds;
            Assert.AreEqual(1025, Authoring.HeightmapResolution);
            Assert.AreEqual(new Vector3(4000, 600, 5000), Authoring.TileSize);
            var union = Authoring.TileBounds(0, 0); float area = 0;
            for (int row = 0; row < Authoring.Rows; row++) for (int column = 0; column < Authoring.Columns; column++)
            {
                var bounds = Authoring.TileBounds(column, row); union.Encapsulate(bounds);
                area += bounds.size.x * bounds.size.z;
                Assert.AreEqual(world.min.y, bounds.min.y); Assert.AreEqual(world.max.y, bounds.max.y);
                if (column > 0) Assert.AreEqual(Authoring.TileBounds(column - 1, row).max.x, bounds.min.x);
                if (row > 0) Assert.AreEqual(Authoring.TileBounds(column, row - 1).max.z, bounds.min.z);
            }
            Assert.AreEqual(world.min, union.min); Assert.AreEqual(world.max, union.max);
            Assert.AreEqual(world.size.x * world.size.z, area);
        }
        [Test] public void NoRoadsPreserveMeaningfulAuthoredHillAndCoastProfile()
        {
            var field = new Authoring.RoadHeightfield(Array.Empty<TruckTaxiEdenboroughLayout.Road>());
            var hill = field.Sample(-2950, -3800); var coast = field.Sample(1300, 3800);
            Assert.AreEqual(TruckTaxiEdenboroughLayout.Height(-2950, -3800), hill.Height);
            Assert.AreEqual(TruckTaxiEdenboroughLayout.Height(1300, 3800), coast.Height);
            Assert.That(hill.Height - coast.Height, Is.GreaterThan(150)); Assert.AreEqual(0, hill.RoadBlend);
        }
        [Test] public void RoadBedUsesInterpolatedSampleElevationNotNaturalHeight()
        {
            var field = new Authoring.RoadHeightfield(new[] { Road(new Vector3(0, 20, 0), new Vector3(100, 40, 0)) });
            var sample = field.Sample(25, 0);
            Assert.That(sample.Height, Is.EqualTo(25 - Authoring.RoadInset).Within(.0001f));
            Assert.AreEqual(1, sample.RoadBlend);
        }
        [Test] public void ActualBakedRoadSurfaceOverridesHigherPlannedGradeWithoutChangingRoad()
        {
            var go = new GameObject("Baked road grading fixture");
            try
            {
                go.transform.position = new Vector3(50, 17.9f, 0);
                var collider = go.AddComponent<BoxCollider>(); collider.size = new Vector3(120, .2f, 20);
                Physics.SyncTransforms();
                var field = new Authoring.RoadHeightfield(new[] { Road(new Vector3(0, 20, 0), new Vector3(100, 20, 0)) },
                    null, new Collider[] { collider });
                Assert.That(field.Sample(25, 0).Height, Is.EqualTo(18 - Authoring.RoadInset).Within(.0001f));
                Assert.That(field.Sample(25, 4).Height, Is.EqualTo(18 - Authoring.RoadInset).Within(.0001f));
                Assert.AreEqual(new Vector3(50, 17.9f, 0), go.transform.position);
            }
            finally { Object.DestroyImmediate(go); }
        }
        [Test] public void ShoulderAndEmbankmentBlendBackIntoAuthoredTerrain()
        {
            var road = Road(new Vector3(0, 20, 0), new Vector3(100, 20, 0));
            var field = new Authoring.RoadHeightfield(new[] { road });
            float core = road.Width * .5f + Authoring.RoadSamplePadding;
            var halfway = field.Sample(50, core + Authoring.EmbankmentBlend * .5f);
            Assert.That(halfway.RoadBlend, Is.EqualTo(.5f).Within(.0001f));
            float natural = TruckTaxiEdenboroughLayout.Height(50, core + Authoring.EmbankmentBlend * .5f);
            Assert.That(halfway.Height, Is.EqualTo(Mathf.Lerp(natural, 20 - Authoring.RoadInset, .5f)).Within(.0001f));
            var outside = field.Sample(50, core + Authoring.EmbankmentBlend + 1);
            Assert.AreEqual(0, outside.RoadBlend);
            Assert.AreEqual(TruckTaxiEdenboroughLayout.Height(50, core + Authoring.EmbankmentBlend + 1), outside.Height);
        }
        [Test] public void GridQueriesWorkAcrossNegativeCellBoundaries()
        {
            var field = new Authoring.RoadHeightfield(new[] { Road(new Vector3(-128, 60, -64), new Vector3(0, 80, -64)) });
            Assert.That(field.Sample(-64, -64).Height, Is.EqualTo(70 - Authoring.RoadInset).Within(.0001f));
            Assert.That(field.Sample(-96, -64).Height, Is.EqualTo(65 - Authoring.RoadInset).Within(.0001f));
        }
        [Test] public void CrossingRoadCoresKeepTerrainBelowBothColliders()
        {
            var field = new Authoring.RoadHeightfield(new[]
            {
                Road(new Vector3(-100, 100, 0), new Vector3(100, 100, 0)),
                Road(new Vector3(0, 110, -100), new Vector3(0, 110, 100))
            });
            Assert.That(field.Sample(0, 0).Height, Is.EqualTo(100 - Authoring.RoadInset).Within(.0001f));
        }
        [Test] public void DuplicateSamplesDoNotDivideByZeroOrCreateSegments()
        {
            var point = new Vector3(0, 40, 0);
            var field = new Authoring.RoadHeightfield(new[] { Road(point, point) });
            Assert.AreEqual(0, field.SegmentCount); Assert.AreEqual(TruckTaxiEdenboroughLayout.Height(0, 0), field.Sample(0, 0).Height);
        }
        [Test] public void NonFiniteRoadSamplesAreRejectedBeforeBake()
        {
            Assert.Throws<ArgumentException>(() => new Authoring.RoadHeightfield(new[]
                { Road(new Vector3(0, float.NaN, 0), new Vector3(100, 40, 0)) }));
        }
        [Test] public void PaddedEndpointGradeKeepsBilinearTerrainBelowCollider()
        {
            var start = new Vector3(.9f, 80, .9f);
            var field = new Authoring.RoadHeightfield(new[] { Road(start, new Vector3(100.9f, 100, .9f)) });
            float dx = Authoring.TileSize.x / (Authoring.HeightmapResolution - 1);
            float dz = Authoring.TileSize.z / (Authoring.HeightmapResolution - 1);
            float tx = start.x / dx, tz = start.z / dz;
            float low = Mathf.Lerp(field.Sample(0, 0).Height, field.Sample(dx, 0).Height, tx);
            float high = Mathf.Lerp(field.Sample(0, dz).Height, field.Sample(dx, dz).Height, tx);
            Assert.That(Mathf.Lerp(low, high, tz), Is.EqualTo(start.y - Authoring.RoadInset).Within(.0001f));
        }
        [Test] public void ServicePadPreservesSavedSurfaceHeightAcrossIts44By32Footprint()
        {
            var pad = new Bounds(new Vector3(-100, 80, -64), new Vector3(44, 0, 32));
            var field = new Authoring.RoadHeightfield(Array.Empty<TruckTaxiEdenboroughLayout.Road>(), new[] { pad });
            foreach (var point in new[] { pad.center, pad.min, pad.max, new Vector3(pad.min.x, 80, pad.max.z) })
            {
                var sample = field.Sample(point.x, point.z);
                Assert.AreEqual(80, sample.Height, "Saved surface Y must not be resampled from Layout.Height or reduced by RoadInset.");
                Assert.AreEqual(1, sample.PadBlend);
            }
        }
        [Test] public void PadShoulderBlendsBackToExistingHeightfield()
        {
            var pad = new Bounds(new Vector3(-100, 80, -64), new Vector3(44, 0, 32));
            var field = new Authoring.RoadHeightfield(Array.Empty<TruckTaxiEdenboroughLayout.Road>(), new[] { pad });
            float x = pad.max.x + Authoring.RoadSamplePadding + Authoring.PadShoulderBlend * .5f;
            var halfway = field.Sample(x, pad.center.z);
            Assert.That(halfway.PadBlend, Is.EqualTo(.5f).Within(.0001f));
            Assert.That(halfway.Height, Is.EqualTo(Mathf.Lerp(TruckTaxiEdenboroughLayout.Height(x, pad.center.z), 80, .5f)).Within(.0001f));
            x = pad.max.x + Authoring.RoadSamplePadding + Authoring.PadShoulderBlend + 1;
            var outside = field.Sample(x, pad.center.z);
            Assert.AreEqual(0, outside.PadBlend); Assert.AreEqual(TruckTaxiEdenboroughLayout.Height(x, pad.center.z), outside.Height);
        }
        [Test] public void PadPaddingKeepsBilinearFootprintCornersFlat()
        {
            var pad = new Bounds(new Vector3(.9f, 80, .9f), new Vector3(44, 0, 32));
            var field = new Authoring.RoadHeightfield(Array.Empty<TruckTaxiEdenboroughLayout.Road>(), new[] { pad });
            float dx = Authoring.TileSize.x / (Authoring.HeightmapResolution - 1), dz = Authoring.TileSize.z / (Authoring.HeightmapResolution - 1);
            float x = pad.max.x, z = pad.max.z;
            float lowX = Mathf.Floor(x / dx) * dx, lowZ = Mathf.Floor(z / dz) * dz;
            float tx = (x - lowX) / dx, tz = (z - lowZ) / dz;
            float low = Mathf.Lerp(field.Sample(lowX, lowZ).Height, field.Sample(lowX + dx, lowZ).Height, tx);
            float high = Mathf.Lerp(field.Sample(lowX, lowZ + dz).Height, field.Sample(lowX + dx, lowZ + dz).Height, tx);
            Assert.That(Mathf.Lerp(low, high, tz), Is.EqualTo(80).Within(.0001f));
        }
        [Test] public void RaisedGaragePadCannotOccludeExistingRoadCore()
        {
            var road = Road(new Vector3(0, 40, 0), new Vector3(100, 40, 0));
            var pad = new Bounds(new Vector3(50, 80, 0), new Vector3(44, 0, 32));
            var field = new Authoring.RoadHeightfield(new[] { road }, new[] { pad });
            Assert.That(field.Sample(50, 0).Height, Is.EqualTo(40 - Authoring.RoadInset).Within(.0001f));
            Assert.AreEqual(80, field.Sample(50, 16).Height, "Away from the protected road core the pad retains its saved height.");
        }
        [Test] public void PadInputsAreCopiedAndOverlapsResolveDeterministically()
        {
            var lower = new Bounds(new Vector3(-100, 60, -64), new Vector3(44, 0, 32));
            var upper = new Bounds(new Vector3(-100, 80, -64), new Vector3(44, 0, 32));
            var pads = new[] { lower, upper };
            var field = new Authoring.RoadHeightfield(Array.Empty<TruckTaxiEdenboroughLayout.Road>(), pads);
            pads[0] = new Bounds(new Vector3(-100, 120, -64), new Vector3(44, 0, 32));
            Assert.AreEqual(60, field.Sample(-100, -64).Height);
            var reverse = new Authoring.RoadHeightfield(Array.Empty<TruckTaxiEdenboroughLayout.Road>(), new[] { upper, lower });
            Assert.AreEqual(field.Sample(-100, -64).Height, reverse.Sample(-100, -64).Height);
        }
        [Test] public void InvalidPadIsRejectedBeforeAnyTerrainAssetsAreWritten()
        {
            var pad = new Bounds(new Vector3(0, float.NaN, 0), new Vector3(44, 0, 32));
            Assert.Throws<ArgumentException>(() => new Authoring.RoadHeightfield(Array.Empty<TruckTaxiEdenboroughLayout.Road>(), new[] { pad }));
            pad = new Bounds(new Vector3(0, 80, 0), new Vector3(0, 0, 32));
            Assert.Throws<ArgumentException>(() => new Authoring.RoadHeightfield(Array.Empty<TruckTaxiEdenboroughLayout.Road>(), new[] { pad }));
        }
        [Test] public void OmittedAndEmptyPadsRetainExistingRoadHeightfield()
        {
            var roads = new[] { Road(new Vector3(0, 40, 0), new Vector3(100, 60, 0)) };
            var original = new Authoring.RoadHeightfield(roads);
            var empty = new Authoring.RoadHeightfield(roads, Array.Empty<Bounds>());
            foreach (var point in new[] { Vector2.zero, new Vector2(50, 0), new Vector2(-100, -100) })
            {
                Assert.AreEqual(original.Sample(point.x, point.y).Height, empty.Sample(point.x, point.y).Height);
                Assert.AreEqual(0, empty.Sample(point.x, point.y).PadBlend);
            }
        }
        [TestCase("Assets/NWH/Terrain")]
        [TestCase("Assets/LWS/TruckTaxi/../Other")]
        [TestCase("Assets/LWS/TruckTaxi")]
        [TestCase("Assets/LWS/TruckTaxi//Terrain")]
        [TestCase("F:/OtherTerrain")]
        public void OutputFolderCannotTargetVendorRootOrEscapeDedicatedOwnership(string path)
        { Assert.Throws<ArgumentException>(() => Authoring.NormalizeAssetFolder(path)); }

        [Test, Explicit("Native Terrain/asset bake integration: run only during main-owned Unity validation.")]
        public void BuildReusesSixAssetsAndObjectsWithReciprocalNeighborsAndSeamHeights()
        {
            var previous = SceneManager.GetActiveScene();
            bool previousDirty = previous.isDirty;
            var scene = SceneManager.CreateScene("Edenborough Terrain Test"); SceneManager.SetActiveScene(scene);
            var parent = new GameObject("Terrain fixture");
            string folder = "Assets/LWS/TruckTaxi/__TerrainFixture_" + Guid.NewGuid().ToString("N");
            try
            {
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                var first = Authoring.Build(parent.transform, folder);
                Assert.AreEqual(6, first.Length); var guids = new string[6];
                for (int i = 0; i < first.Length; i++) guids[i] = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(first[i].terrainData));
                var second = Authoring.Build(parent.transform, folder); Assert.AreEqual(6, parent.transform.childCount);
                for (int row = 0; row < Authoring.Rows; row++) for (int column = 0; column < Authoring.Columns; column++)
                {
                    int index = row * Authoring.Columns + column; var tile = second[index];
                    Assert.AreSame(first[index], tile);
                    Assert.AreEqual(guids[index], AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(tile.terrainData)));
                    Assert.AreEqual(1025, tile.terrainData.heightmapResolution); Assert.AreEqual(4, tile.terrainData.terrainLayers.Length);
                    Assert.AreEqual("Universal Render Pipeline/Terrain/Lit", tile.materialTemplate.shader.name);
                    Assert.IsTrue(tile.allowAutoConnect); Assert.AreEqual(91417, tile.groupingID);
                    Assert.AreEqual(0, tile.terrainData.treeInstanceCount);
                    var splat = tile.terrainData.GetAlphamaps(0, 0, 1, 1);
                    float sum = 0; for (int i = 0; i < 4; i++) sum += splat[0, 0, i];
                    Assert.That(sum, Is.EqualTo(1).Within(.0001f));
                    if (column + 1 < Authoring.Columns)
                    {
                        Assert.AreSame(second[index + 1], tile.rightNeighbor); Assert.AreSame(tile, second[index + 1].leftNeighbor);
                        var right = tile.terrainData.GetHeights(1024, 0, 1, 1025);
                        var left = second[index + 1].terrainData.GetHeights(0, 0, 1, 1025);
                        for (int z = 0; z < 1025; z++) Assert.AreEqual(right[z, 0], left[z, 0]);
                    }
                    if (row + 1 < Authoring.Rows)
                    {
                        Assert.AreSame(second[index + Authoring.Columns], tile.topNeighbor);
                        Assert.AreSame(tile, second[index + Authoring.Columns].bottomNeighbor);
                        var top = tile.terrainData.GetHeights(0, 1024, 1025, 1);
                        var bottom = second[index + Authoring.Columns].terrainData.GetHeights(0, 0, 1025, 1);
                        for (int x = 0; x < 1025; x++) Assert.AreEqual(top[0, x], bottom[0, x]);
                    }
                }
                if (previous.IsValid()) Assert.AreEqual(previousDirty, previous.isDirty, "Building additive Edenborough must not dirty the previously active scene.");
                foreach (var road in TruckTaxiEdenboroughLayout.Roads)
                    for (int i = 1; i < road.Points.Length; i++)
                        for (int k = 0; k <= 4; k++)
                        {
                            Vector3 point = Vector3.Lerp(road.Points[i - 1], road.Points[i], k / 4f);
                            var world = TruckTaxiEdenboroughLayout.WorldBounds;
                            int column = Mathf.Clamp(Mathf.FloorToInt((point.x - world.min.x) / Authoring.TileSize.x), 0, Authoring.Columns - 1);
                            int row = Mathf.Clamp(Mathf.FloorToInt((point.z - world.min.z) / Authoring.TileSize.z), 0, Authoring.Rows - 1);
                            var tile = second[row * Authoring.Columns + column];
                            float u = (point.x - tile.transform.position.x) / Authoring.TileSize.x;
                            float v = (point.z - tile.transform.position.z) / Authoring.TileSize.z;
                            float height = tile.terrainData.GetInterpolatedHeight(u, v) + tile.transform.position.y;
                            Assert.LessOrEqual(height, point.y - .02f, "Terrain must remain below roadway: " + road.Id + " " + point);
                        }
            }
            finally
            {
                Object.DestroyImmediate(parent);
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                EditorSceneManager.CloseScene(scene, true);
                // Only the unique, builder-owned test folder is removed; source scenes/assets are never saved.
                if (AssetDatabase.IsValidFolder(folder)) AssetDatabase.DeleteAsset(folder);
            }
        }
    }
}

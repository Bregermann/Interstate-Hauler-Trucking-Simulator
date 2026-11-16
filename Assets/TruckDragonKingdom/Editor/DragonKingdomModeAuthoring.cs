#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using CompassNavigatorPro;
using LWS.TruckMechanicalBull.Editor;
using LWS.TruckSports;
using LWS.TruckSports.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LWS.TruckDragonKingdom.Editor
{
    // Explicit new-scene composition over the established Sports/NWH/Heat/
    // Cinemachine/Compass/Weather Maker adapters. No normal build regeneration.
    public static class DragonKingdomModeAuthoring
    {
        public static T Begin<T>(string folder, int dragonCount = 2, string[] speciesIds = null) where T : DragonKingdomGroundMatch
        {
            string scene = "Assets/" + folder + "/Scenes/" + folder + ".unity";
            ImpossibleBalanceArt.GuardNew(scene);
            var shared = AssetDatabase.LoadAssetAtPath<GameObject>(DragonKingdomWorldAuthoring.PrefabPath);
            var catalog = AssetDatabase.LoadAssetAtPath<DragonSpeciesCatalog>(DragonSpeciesAuthoring.CatalogPath);
            if (shared == null || catalog == null) throw new InvalidOperationException("Author and validate the actual shared kingdom and dragon catalog before composing its games.");
            if (dragonCount < 1 || dragonCount > 6 || speciesIds != null && speciesIds.Length != dragonCount)
                throw new ArgumentException("One to six actual scene actors with matching explicit species IDs are required.");
            var match = SportsEventAuthoring.Begin<T>(folder);
            match.setup.count = 4; match.option = 0; match.NormalizeSetup(); match.species = catalog;
            match.world = ((GameObject)PrefabUtility.InstantiatePrefab(shared)).GetComponent<DragonKingdomWorld>();
            match.world.ValidateReferences();
            match.trucks = new SportsEventTruck[4]; match.conditions = new DragonTruckCondition[4];
            var starts = match.world.Region("village").truckStarts;
            for (int i = 0; i < 4; i++)
            {
                match.trucks[i] = ImpossibleBalanceArt.Tractor(match, i, starts[i].position);
                match.conditions[i] = match.trucks[i].gameObject.AddComponent<DragonTruckCondition>();
                match.conditions[i].truck = match.trucks[i];
            }
            match.dragons = new DragonCharacter[dragonCount]; match.dragonSpeciesIds = new string[dragonCount];
            for (int i = 0; i < dragonCount; i++)
            {
                string id = speciesIds != null ? speciesIds[i] : i % 2 == 0 ? "dragon-inferno" : "dragon-fire";
                if (!catalog.TryGet(id, out _)) throw new InvalidOperationException("Unknown configured species: " + id);
                string path = "Assets/TruckDragonKingdom/Prefabs/Dragons/" + id + "/" + id + " Dragon Actor.prefab";
                var actor = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (actor == null) throw new InvalidOperationException("Actual configured Generic dragon actor is required: " + path);
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(actor);
                instance.transform.position = match.world.Region(i % 2 == 0 ? "caverns" : "mountains").root.position + new Vector3(60, i % 2 == 0 ? 6 : 90, -40);
                match.dragons[i] = instance.GetComponent<DragonCharacter>(); match.dragonSpeciesIds[i] = id;
            }
            match.broadcast = SportsEventAuthoring.Camera(folder + " action broadcast");
            match.broadcast.view.farClipPlane = 6000;
            match.broadcast.Pose(starts[0].position, new Vector3(0, 24, -42), 64, true);
            match.cameras = new[] { match.broadcast.view };
            ImpossibleBalanceArt.UI(match);
            match.drivingViews = SportsDrivingViewsAuthoring.Install(match, match.trucks, match.broadcast, folder);
            match.drivingViews.defaultMode = 1; match.drivingViews.independentViewModes = true;
            foreach (var camera in match.drivingViews.rally.views) camera.view.farClipPlane = 6000;
            var spectators = match.gameObject.AddComponent<SportsExpansionSpectators>(); spectators.match = match; spectators.views = match.drivingViews;
            match.drivingViews.rally.retiredViews = spectators;
            match.feedback.clips = (AudioClip[])LWS.TruckHockey.Editor.HockeyAudioAuthoring.Create().Clone();
            match.presentationWeather = SportsChaosAuthoring.Weather(match, folder);
            match.presentationWeather.presets = SportsWeatherPresetsAuthoring.EnsureCatalog();
            match.drivingViews.rally.weather = match.presentationWeather;
            var allCameras = match.drivingViews.rally.views.Select(v => v.view).Append(match.broadcast.view).ToArray();
            SportsWeatherRenderingAuthoring.Install(match.gameObject, allCameras, folder);
            InstallNavigation(match);
            foreach (var control in match.menus.replayButtons) control.SetActive(false);
            return match;
        }

        public static void Finish(DragonKingdomGroundMatch match, string folder)
        {
            if (match == null || match.world == null || match.trucks.Length != 4) throw new InvalidOperationException("Complete new mode composition required before saving.");
            LWS.TruckDragonKingdom.Ambience.DragonKingdomAmbienceAuthoring.Install(match.world);
            if (PrefabUtility.IsPartOfPrefabInstance(match.world)) PrefabUtility.RecordPrefabInstancePropertyModifications(match.world);
            DragonKingdomBroadcastAuthoring.Install(match,match.trucks,match.broadcast);
            SportsEventAuthoring.Save(folder);
            var lamps = match.world.GetComponentsInChildren<Light>(true).Where(l => l.type != LightType.Directional).ToArray();
            SportsEnvironmentAuthoring.BindAuthoredOutdoorSession(match, folder, lamps);
            SportsTruckAppearanceHostAuthoring.BindNewImpossibleScene(match, match.trucks);
            DragonKingdomArtworkAuthoring.Install(match, match.world, folder);
            SportsBroadcastAuthoring.ApplyScene();
            ConfigureKingdomSnow(match);
            EditorSceneManager.MarkSceneDirty(match.gameObject.scene); EditorSceneManager.SaveScene(match.gameObject.scene); AssetDatabase.SaveAssets();
            Debug.Log("DRAGON_KINGDOM_GAME_AUTHORED " + folder + " · Native match/rendering acceptance remains required.");
        }

        public static void ConfigureKingdomSnow(SportsEventSession match)
        {
            var snow = match.GetComponent<SportsSnowSurface>();
            if (snow != null)
            {
                snow.traceExposedRoads = true;
                var settings = new SerializedObject(snow.adapter);
                settings.FindProperty("areaSizeMeters").floatValue = 3200;
                settings.FindProperty("areaDepthMeters").floatValue = 1600;
                settings.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(snow);
                DragonKingdomRoadSnowAuthoring.BindSceneRoads(match);
            }
        }

        static void InstallNavigation(DragonKingdomGroundMatch match)
        {
            var owner = match.gameObject.AddComponent<DragonKingdomNavigation>(); owner.match = match;
            owner.gps = new CompassPro[4]; owner.markers = new CompassProPOI[4];
            var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Plugins/Kronnect/CompassNavigatorPro/Resources/CNPro/Prefabs/CompassNavigatorPro.prefab");
            if (source == null) throw new InvalidOperationException("Installed Compass Navigator Pro prefab required.");
            for (int view = 0; view < 4; view++)
            {
                var root = (GameObject)PrefabUtility.InstantiatePrefab(source); root.SetActive(false);
                root.name = "Independent kingdom navigation owner " + (view + 1);
                var original = root.GetComponentInChildren<CompassPro>(true);
                var compass = original.gameObject.AddComponent<SportsBroadcastCompass>();
                EditorUtility.CopySerializedManagedFieldsOnly(original, compass); UnityEngine.Object.DestroyImmediate(original);
                owner.gps[view] = compass;
                var canvas = compass.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = match.drivingViews.rally.views[view].view; canvas.planeDistance = .5f;
                // Inactive vendor prefabs have not initialized their runtime
                // POI caches. Author serialized settings instead of invoking
                // runtime setters which immediately refresh those caches.
                var so = new SerializedObject(compass);
                so.FindProperty("_cameraMain").objectReferenceValue = canvas.worldCamera; so.FindProperty("_follow").objectReferenceValue = match.trucks[view].transform;
                so.FindProperty("_showCompassBar").boolValue = false; so.FindProperty("_showMiniMap").boolValue = true;
                so.FindProperty("_miniMapPositionAndSize").intValue = (int)MiniMapPositionAndScaleMode.UserDefined; so.FindProperty("_miniMapStyle").intValue = (int)MiniMapStyle.SolidBox;
                so.FindProperty("_miniMapOrientation").intValue = (int)MiniMapOrientation.Follow; so.FindProperty("_miniMapContents").intValue = (int)MiniMapContents.TopDownWorldView;
                so.FindProperty("_miniMapResolution").intValue = (int)MiniMapResolution._256;
                so.FindProperty("_miniMapCameraSnapshotFrequency").intValue = (int)MiniMapCameraSnapshotFrequency.TimeInterval; so.FindProperty("_miniMapSnapshotInterval").floatValue = .4f;
                so.FindProperty("_miniMapLayerMask").intValue = 1; so.FindProperty("_miniMapCaptureSize").floatValue = 150; so.FindProperty("_miniMapCameraHeightVSFollow").floatValue = 350; so.FindProperty("_miniMapCameraDepth").floatValue = 700;
                so.FindProperty("_miniMapShowMaximizeButton").boolValue = so.FindProperty("_miniMapShowZoomInOutButtons").boolValue = false;
                so.FindProperty("_routeShowOnMiniMap").boolValue = true; so.FindProperty("_routeShowInWorld").boolValue = so.FindProperty("_routeShowOnCompassBar").boolValue = false;
                so.FindProperty("_routeWidth").floatValue = 3; so.FindProperty("_routeColor").colorValue = new Color(.15f, 1, .55f);
                compass.iconPixels = 44; compass.labelFontSize = 18; compass.labelWidth = 190;
                so.FindProperty("_compassGroup").intValue = view + 1;
                so.FindProperty("_showOnScreenIndicators").boolValue = so.FindProperty("_showOffScreenIndicators").boolValue = true;
                so.FindProperty("_offScreenIndicatorShowDistance").boolValue = so.FindProperty("_onScreenIndicatorShowDistance").boolValue = false;
                so.ApplyModifiedPropertiesWithoutUndo();
                foreach (var rect in root.GetComponentsInChildren<RectTransform>(true)) if (rect.name == "MiniMap Root")
                {
                    rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1, 0);
                    rect.anchoredPosition = new Vector2(-14, 18); rect.sizeDelta = new Vector2(190, 145);
                }
                foreach (var child in root.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 26 + view;
                var marker = new GameObject("Independent kingdom objective " + (view + 1)); marker.transform.SetParent(owner.transform, false); marker.SetActive(false);
                var poi = marker.AddComponent<CompassProPOI>(); poi.canBeVisited = poi.hideWhenVisited = poi.dontDestroyOnLoad = false;
                poi.showOnScreenIndicator = poi.showOffScreenIndicator = poi.ignoreAreaOfInterest = true; poi.radius = 0;
                poi.compassGroupMask = 1 << view; poi.iconNonVisited = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
                compass.permittedTargets = new[] { poi }; owner.markers[view] = poi;
            }
        }
    }
}
#endif

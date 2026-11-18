using System;
using System.IO;
using System.Linq;
using UnityEditor;

namespace LWS.TruckSports.Editor
{
    public static class SportsHitchesAuthoring
    {
        public static void InstallSavedRescueRampAndCreativeWorkshop()
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/TruckExtremeHauling/Scenes/IceRoadReckoning.unity");
            LWS.TruckExtremeHauling.Editor.ThinIceRescueAuthoring.RepairActiveExtractionRamp();
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/TruckExtremeHauling/Scenes/DominoDerby.unity");
            LWS.TruckExtremeHauling.Editor.DominoCreativeWorkshopAuthoring.InstallActiveWorkshop();
            AssetDatabase.SaveAssets();
        }

        public static void RestoreSavedSiegeReceivingField()
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/TruckHitchSports/Scenes/TrailerTrebuchet.unity");
            LWS.TruckHitchSports.Editor.TrebuchetTargetsAuthoring.SetActiveTrebuchetTargetDistance(50);
            AssetDatabase.SaveAssets();
        }
        public static void FinishSavedGateLeafBinding()
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/TruckHitchSports/Scenes/JackknifeHighwire.unity");
            LWS.TruckHitchSports.Editor.HighwireVariantsAuthoring.BindActiveHighwireGateLeaves();
            AssetDatabase.SaveAssets();
        }
        public static void InstallSavedCurrentsAndCounterplay()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode before explicit saved additions.");
            var water = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/TruckWaterSports/Scenes/WhitewaterWipeout.unity");
            LWS.TruckWaterSports.Editor.RiverWhirlpoolCurrentAuthoring.UpgradeActiveScene();
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(water);
            var horror = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/TruckHorrorSports/ScarecrowSeason/Scenes/ScarecrowSeason.unity");
            LWS.TruckHorrorSports.Editor.ScarecrowLureAuthoring.UpgradeActiveVenue();
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(horror);
            var highwire = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/TruckHitchSports/Scenes/JackknifeHighwire.unity");
            LWS.TruckHitchSports.Editor.HighwireVariantsAuthoring.BindActiveHighwireGateLeaves();
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(highwire);
            AssetDatabase.SaveAssets();
            UnityEngine.Debug.Log("HH10_CURRENTS_COUNTERPLAY_SAVED bounded current adapter, existing physical stalker and real crossing leaves; runtime acceptance separate.");
        }
        // Explicit authoring only. Normal play/build never regenerates venues.
        [MenuItem("Truck Sports/Alpha 0.10/Create missing expansion scenes")]
        public static void CreateMissingScenes()
        {
            RepairInterruptedDominoSnow();
            LWS.TruckWaterSports.Editor.WaterSportsAuthoring.CreateAll();
            LWS.TruckHorrorSports.Editor.HorrorAuthoring.CreateAll();
            LWS.TruckHitchSports.Editor.CoreHitchSportsAuthoring.CreateAll();
            LWS.TruckExtremeHauling.Editor.ExtremeHaulingAuthoring.CreateAll();
            int count = 0;
            foreach (var entry in SportsHitchesGames.All)
            {
                if (!File.Exists(entry.ScenePath)) throw new FileNotFoundException("New expansion venue not authored: " + entry.id, entry.ScenePath);
                count++;
            }
            if (count != 22) throw new InvalidOperationException("Alpha 0.10 requires exactly 22 distinct sports.");
            AssetDatabase.SaveAssets();
            UnityEngine.Debug.Log("HH10_SAVED_SCENES=22; gameplay, art and Windows acceptance remain separately required.");
        }
        [MenuItem("Truck Sports/Alpha 0.10/Install authored candidate scenery and equipment")]
        public static void InstallSavedCandidateContent()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode before explicit candidate authoring.");
            foreach(string path in new[]{LWS.TruckWaterSports.Editor.WaterSportsAuthoring.PaddleScene,LWS.TruckWaterSports.Editor.WaterSportsAuthoring.KayakScene,LWS.TruckWaterSports.Editor.WaterSportsAuthoring.BlackwaterScene})
            {
                var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path);
                LWS.TruckWaterSports.Editor.WaterSportsAuthoring.UpgradeActiveTruckScale();
                LWS.TruckWaterSports.Editor.WaterVenueLandscapeAuthoring.UpgradeActiveVenue();
                LWS.TruckWaterSports.Editor.WaterEffectsAuthoring.InstallActiveScene();
                UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
            }
            foreach(var entry in SportsHitchesGames.All)if(entry.ScenePath.StartsWith("Assets/TruckExtremeHauling/Scenes/",StringComparison.Ordinal))
            {
                var venue=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(entry.ScenePath);
                LWS.TruckExtremeHauling.Editor.HaulingVenueLandscapeAuthoring.UpgradeActiveVenue();
                UnityEditor.SceneManagement.EditorSceneManager.SaveScene(venue);
            }
            foreach(var entry in SportsHitchesGames.All)if(entry.ScenePath.StartsWith("Assets/TruckHitchSports/Scenes/",StringComparison.Ordinal))
            {
                var venue=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(entry.ScenePath);
                LWS.TruckHitchSports.Editor.CoreHitchVenueLandscapeAuthoring.InstallActiveVenue();
                UnityEditor.SceneManagement.EditorSceneManager.SaveScene(venue);
            }
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/TruckExtremeHauling/Scenes/HitchRingToss.unity");
            LWS.TruckExtremeHauling.Editor.HaulingVenuePresentation.UpgradeActiveRingTossLaunchRamps();
            var cargo=UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/TruckExtremeHauling/Scenes/CargoCrisis.unity");
            LWS.TruckExtremeHauling.Editor.CargoLandingGearAuthoring.UpgradeActiveCargoLandingGear();
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(cargo);
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/TruckExtremeHauling/Scenes/SkyhookSprint.unity");
            LWS.TruckExtremeHauling.Editor.SkyhookSlalomAuthoring.UpgradeActiveSkyhookSlalom();
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/TruckHitchSports/Scenes/JackknifeHighwire.unity");
            LWS.TruckHitchSports.Editor.CoreHitchSetupRepairs.RepairActiveHighwireReceivingSurfaces();
            var trebuchet=UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/TruckHitchSports/Scenes/TrailerTrebuchet.unity");
            LWS.TruckHitchSports.Editor.CoreHitchSetupRepairs.RepairActiveTrebuchetApparatus();
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(trebuchet);
            AssetDatabase.SaveAssets();UnityEngine.Debug.Log("HH10_CANDIDATE_CONTENT_INSTALLED explicit new-venue additions only.");
        }
        // Explicit targeted content upgrade. Never invoked by build, load or Play Mode.
        public static void InstallSavedMechanicsExpansion()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode before upgrading saved mechanics.");
            foreach(string path in new[]{LWS.TruckWaterSports.Editor.WaterSportsAuthoring.PaddleScene,LWS.TruckWaterSports.Editor.WaterSportsAuthoring.KayakScene,LWS.TruckWaterSports.Editor.WaterSportsAuthoring.BlackwaterScene})
            {
                var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path);
                LWS.TruckWaterSports.Editor.WaterMechanicsAuthoring.UpgradeActiveScene();
                UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
            }
            foreach(var sport in new[]{LWS.TruckHorrorSports.HorrorSport.GraveyardShift,LWS.TruckHorrorSports.HorrorSport.ScarecrowSeason})
            {
                var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(LWS.TruckHorrorSports.Editor.HorrorAuthoring.Scene(sport));
                LWS.TruckHorrorSports.Editor.HorrorPhysicalObjectivesAuthoring.UpgradeActiveVenue();
                UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
            }
            var unhitched=UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/TruckHitchSports/Scenes/Unhitched.unity");
            LWS.TruckHitchSports.Editor.UnhitchedRollingSupportAuthoring.UpgradeActiveUnhitchedRollingSupport();
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(unhitched);
            var ownership=UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/TruckHitchSports/Scenes/HitchAndRun.unity");
            LWS.TruckHitchSports.Editor.HitchRunOwnershipAuthoring.UpgradeActiveHitchRunOwnershipBadges();
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(ownership);
            var orbit=UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/TruckExtremeHauling/Scenes/ZeroGHitchUp.unity");
            LWS.TruckExtremeHauling.Editor.OrbitalRecoveryAuthoring.UpgradeActiveScene();
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(orbit);
            var skyhook=UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/TruckExtremeHauling/Scenes/SkyhookSprint.unity");
            LWS.TruckExtremeHauling.Editor.SkyhookTransferAuthoring.UpgradeActiveVenue();
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(skyhook);
            var rescue=UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/TruckExtremeHauling/Scenes/IceRoadReckoning.unity");
            LWS.TruckExtremeHauling.Editor.ThinIceRescueAuthoring.UpgradeActiveScene();
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(rescue);
            foreach(string path in new[]{"Assets/TruckHitchSports/Scenes/Unhitched.unity","Assets/TruckHitchSports/Scenes/TrailerTrebuchet.unity","Assets/TruckHitchSports/Scenes/HitchAndRun.unity","Assets/TruckHitchSports/Scenes/HitchHandoff.unity","Assets/TruckExtremeHauling/Scenes/ZeroGHitchUp.unity","Assets/TruckExtremeHauling/Scenes/JackknifeOnIce.unity","Assets/TruckExtremeHauling/Scenes/HitchRingToss.unity","Assets/TruckExtremeHauling/Scenes/TrailerPlinko.unity","Assets/TruckExtremeHauling/Scenes/SkyhookSprint.unity","Assets/TruckExtremeHauling/Scenes/IceRoadReckoning.unity",LWS.TruckWaterSports.Editor.WaterSportsAuthoring.PaddleScene,LWS.TruckWaterSports.Editor.WaterSportsAuthoring.KayakScene,LWS.TruckWaterSports.Editor.WaterSportsAuthoring.BlackwaterScene,LWS.TruckHorrorSports.Editor.HorrorAuthoring.Scene(LWS.TruckHorrorSports.HorrorSport.GraveyardShift),LWS.TruckHorrorSports.Editor.HorrorAuthoring.Scene(LWS.TruckHorrorSports.HorrorSport.ScarecrowSeason)})
            {
                var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path);
                var authority=UnityEngine.Object.FindAnyObjectByType<SportsEventSession>();
                if(authority==null)throw new InvalidOperationException("Existing sport authority required: "+path);
                foreach(var label in authority.menus.GetComponentsInChildren<TMPro.TMP_Text>(true))
                    if(label.name=="Rules and controls"){label.text=authority.Help;EditorUtility.SetDirty(label);}
                UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();UnityEngine.Debug.Log("HH10_MECHANICS_EXPANSION_SAVED targeted existing venue upgrades; runtime acceptance remains separate.");
        }
        public static void InstallSavedRemainingMechanics()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode before these explicit additions.");
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/TruckHitchSports/Scenes/HitchHandoff.unity");
            LWS.TruckHitchSports.Editor.CoreHitchSetupRepairs.RepairActiveHandoffLandingGear();
            var rocket=UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/TruckHitchSports/Scenes/Trailerblazers.unity");
            LWS.TruckHitchSports.Editor.RocketFlightPresentationAuthoring.UpgradeActiveRocketFlightPresentation();
            SaveCurrentHelp(rocket);
            var road=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(LWS.TruckHorrorSports.Editor.HorrorAuthoring.Scene(LWS.TruckHorrorSports.HorrorSport.DeadEnd));
            LWS.TruckHorrorSports.Editor.DeadEndJunctionAuthoring.UpgradeActiveVenue();SaveCurrentHelp(road);
            var lake=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(LWS.TruckWaterSports.Editor.WaterSportsAuthoring.BlackwaterScene);
            LWS.TruckWaterSports.Editor.BlackwaterEnvironmentAuthoring.InstallActiveVenue();SaveCurrentHelp(lake);
            AssetDatabase.SaveAssets();UnityEngine.Debug.Log("HH10_REMAINING_MECHANICS_SAVED explicit rocket, branching road and lake additions; gameplay acceptance remains separate.");
        }
        static void SaveCurrentHelp(UnityEngine.SceneManagement.Scene scene)
        {
            var authority=UnityEngine.Object.FindAnyObjectByType<SportsEventSession>();
            if(authority==null)throw new InvalidOperationException("Preserve the existing sport authority.");
            foreach(var label in authority.menus.GetComponentsInChildren<TMPro.TMP_Text>(true))
                if(label.name=="Rules and controls"){label.text=authority.Help;EditorUtility.SetDirty(label);}
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
        }
        public static void FinishSavedGameplayCorrections()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode before explicit saved corrections.");
            var road=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(LWS.TruckHorrorSports.Editor.HorrorAuthoring.Scene(LWS.TruckHorrorSports.HorrorSport.DeadEnd));
            LWS.TruckHorrorSports.Editor.DeadEndJunctionAuthoring.RepairExitRoadForestClearance();SaveCurrentHelp(road);
            foreach(string path in new[]{"Assets/TruckHitchSports/Scenes/TrailerTrebuchet.unity","Assets/TruckHitchSports/Scenes/HitchHandoff.unity","Assets/TruckHitchSports/Scenes/HitchAndRun.unity","Assets/TruckExtremeHauling/Scenes/CargoCrisis.unity","Assets/TruckExtremeHauling/Scenes/IceRoadReckoning.unity",LWS.TruckWaterSports.Editor.WaterSportsAuthoring.KayakScene,LWS.TruckWaterSports.Editor.WaterSportsAuthoring.BlackwaterScene,LWS.TruckHorrorSports.Editor.HorrorAuthoring.Scene(LWS.TruckHorrorSports.HorrorSport.GraveyardShift),LWS.TruckHorrorSports.Editor.HorrorAuthoring.Scene(LWS.TruckHorrorSports.HorrorSport.CursedCargo)})
            {var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path);SaveCurrentHelp(scene);}
            AssetDatabase.SaveAssets();UnityEngine.Debug.Log("HH10_SAVED_GAMEPLAY_CORRECTIONS_COMPLETE explicit route clearance and changed control instructions only; natural acceptance remains separate.");
        }
        public static void InstallSavedSiegeLoadingBridges()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode before this explicit equipment upgrade.");
            var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/TruckHitchSports/Scenes/TrailerTrebuchet.unity");
            var match=UnityEngine.Object.FindAnyObjectByType<LWS.TruckHitchSports.TrebuchetMatch>();
            var ramps=scene.GetRootGameObjects().Where(g=>g.name=="Physical siege loading ramp").ToArray();
            if(match==null||ramps.Length!=4)throw new InvalidOperationException("Preserve the four original siege loading bridges.");
            for(int lane=0;lane<match.engines.Length;lane++)
            {
                var engine=match.engines[lane];
                if(engine.LoadingBridges!=null&&engine.LoadingBridges.Length>0){UnityEngine.Debug.Log("Preserved existing editable siege bridge configuration "+lane);continue;}
                var loading=ramps.OrderBy(g=>UnityEngine.Mathf.Abs(g.transform.position.x-engine.LoadingCradle.bounds.center.x)).First();
                var unloading=match.transform.Find("Physical siege unloading ramp "+lane);
                if(unloading==null||UnityEngine.Mathf.Abs(loading.transform.position.x-engine.LoadingCradle.bounds.center.x)>1)throw new InvalidOperationException("The original aligned loading and exit bridge are required.");
                var bridgeObjects=new[]{loading,unloading.gameObject};
                var bodies=new UnityEngine.Rigidbody[2];
                for(int index=0;index<2;index++)
                {
                    var body=bridgeObjects[index].GetComponent<UnityEngine.Rigidbody>();
                    if(body==null)body=bridgeObjects[index].AddComponent<UnityEngine.Rigidbody>();
                    body.isKinematic=true;body.useGravity=false;body.mass=10000;body.interpolation=UnityEngine.RigidbodyInterpolation.Interpolate;
                    body.collisionDetectionMode=UnityEngine.CollisionDetectionMode.ContinuousSpeculative;bodies[index]=body;EditorUtility.SetDirty(body);
                }
                engine.LoadingBridges=bodies;engine.LoadingBridgeRetractionOffsets=new[]{UnityEngine.Vector3.back*6,UnityEngine.Vector3.forward*6};
                engine.BridgeRetractionMetres=6;engine.BridgeRetractionSpeed=2;EditorUtility.SetDirty(engine);
            }
            UnityEngine.Physics.SyncTransforms();SaveCurrentHelp(scene);AssetDatabase.SaveAssets();
            UnityEngine.Debug.Log("HH10_SIEGE_REAL_LOADING_BRIDGES_SAVED original physical ramps retract axially after tractor clearance, then release mechanical lock. No actor poses or motor forces changed.");
        }
        public static void InstallSavedConvoyAndCraft()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode before these explicit additions.");
            var convoy=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(LWS.TruckHorrorSports.Editor.HorrorAuthoring.Scene(LWS.TruckHorrorSports.HorrorSport.CursedCargo));
            LWS.TruckHorrorSports.Editor.CursedConvoyRoadAuthoring.UpgradeActiveVenue();SaveCurrentHelp(convoy);
            var road=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(LWS.TruckHorrorSports.Editor.HorrorAuthoring.Scene(LWS.TruckHorrorSports.HorrorSport.DeadEnd));
            LWS.TruckHorrorSports.Editor.DeadEndJunctionAuthoring.RepairFinalSignClearance();SaveCurrentHelp(road);
            var lake=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(LWS.TruckWaterSports.Editor.WaterSportsAuthoring.BlackwaterScene);
            LWS.TruckWaterSports.Editor.BlackwaterAbandonedCraftAuthoring.UpgradeActiveVenue();SaveCurrentHelp(lake);
            InstallSavedSiegeLoadingBridges();
            AssetDatabase.SaveAssets();UnityEngine.Debug.Log("HH10_CONVOY_AND_OWNED_CRAFT_SAVED; gameplay/rendering acceptance remains separate.");
        }
        public static void FinishPendingRelayHazards()
        {
            InstallSavedRelayStations();
            foreach(string path in new[]{"Assets/TruckExtremeHauling/Scenes/IceRoadReckoning.unity","Assets/TruckExtremeHauling/Scenes/CargoCrisis.unity"})
            {
                var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path);
                LWS.TruckExtremeHauling.Editor.HaulingHazardPresentationAuthoring.UpgradeActiveScene();SaveCurrentHelp(scene);
            }
            AssetDatabase.SaveAssets();
        }
        public static void FinishSavedPhysicalVariants()
        {
            RepairSavedPhysicalRescueAndSupplies();InstallSavedRelayStations();
            foreach(string path in new[]{"Assets/TruckExtremeHauling/Scenes/IceRoadReckoning.unity","Assets/TruckExtremeHauling/Scenes/CargoCrisis.unity"})
            {
                var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path);
                LWS.TruckExtremeHauling.Editor.HaulingHazardPresentationAuthoring.UpgradeActiveScene();SaveCurrentHelp(scene);
            }
            AssetDatabase.SaveAssets();
        }
        public static void InstallSavedRelayStations()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode before this explicit addition.");
            var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/TruckHitchSports/Scenes/HitchHandoff.unity");
            LWS.TruckHitchSports.Editor.HandoffVariantsAuthoring.UpgradeActiveHandoffStationsAndRoutes();SaveCurrentHelp(scene);
            AssetDatabase.SaveAssets();UnityEngine.Debug.Log("HH10_RELAY_VARIANT_STATIONS_SAVED; actual relay acceptance remains required.");
        }
        public static void RepairSavedPhysicalRescueAndSupplies()
        {
            RepairSavedSupplyNavigation();
            var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/TruckExtremeHauling/Scenes/IceRoadReckoning.unity");
            LWS.TruckExtremeHauling.Editor.ThinIceRescueAuthoring.UpgradeActiveScene();SaveCurrentHelp(scene);
            AssetDatabase.SaveAssets();UnityEngine.Debug.Log("HH10_RESCUE_EXACT_HULLS_SAVED; natural rescue acceptance remains separate.");
        }
        public static void RepairSavedSupplyNavigation()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode before this explicit repair.");
            var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(LWS.TruckHorrorSports.Editor.HorrorAuthoring.Scene(LWS.TruckHorrorSports.HorrorSport.GraveyardShift));
            LWS.TruckHorrorSports.Editor.HorrorPhysicalObjectivesAuthoring.UpgradeActiveVenue();
            LWS.TruckHorrorSports.Editor.GraveyardServicesAuthoring.UpgradeActiveVenue();SaveCurrentHelp(scene);
            AssetDatabase.SaveAssets();UnityEngine.Debug.Log("HH10_SUPPLY_RECEIVER_NAVIGATION_SAVED");
        }
        public static void InstallSavedSiegeTargets()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode before this explicit upgrade.");
            var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/TruckHitchSports/Scenes/TrailerTrebuchet.unity");
            LWS.TruckHitchSports.Editor.TrebuchetTargetsAuthoring.UpgradeActiveTrebuchetTargets();SaveCurrentHelp(scene);
            AssetDatabase.SaveAssets();UnityEngine.Debug.Log("HH10_SIEGE_TARGETS_SAVED; actual downrange acceptance remains required.");
        }
        public static void InstallSavedAdvancedRoutesAndReplay()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode before these explicit additions.");
            var highwire=UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/TruckHitchSports/Scenes/JackknifeHighwire.unity");
            LWS.TruckHitchSports.Editor.HighwireVariantsAuthoring.UpgradeActiveHighwireVariants();SaveCurrentHelp(highwire);
            var domino=UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/TruckExtremeHauling/Scenes/DominoDerby.unity");
            LWS.TruckExtremeHauling.Editor.DominoReplayAuthoring.UpgradeActiveVenue();SaveCurrentHelp(domino);
            var roadtrain=UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/TruckExtremeHauling/Scenes/RoadTrainWreck.unity");
            LWS.TruckExtremeHauling.Editor.RoadTrainCapacityAuthoring.UpgradeActiveRoadTrainCapacityToEight();SaveCurrentHelp(roadtrain);
            var cargo=UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/TruckExtremeHauling/Scenes/CargoCrisis.unity");
            LWS.TruckExtremeHauling.Editor.CargoEmergencyAuthoring.UpgradeActiveScene();SaveCurrentHelp(cargo);
            AssetDatabase.SaveAssets();UnityEngine.Debug.Log("HH10_ADVANCED_ROUTES_REPLAY_SAVED; focused acceptance remains required.");
        }
        public static void InstallSavedTechnicalRiverForks()
        {
            foreach(string path in new[]{LWS.TruckWaterSports.Editor.WaterSportsAuthoring.PaddleScene,LWS.TruckWaterSports.Editor.WaterSportsAuthoring.KayakScene})
            {
                var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path);
                LWS.TruckWaterSports.Editor.RiverRouteAuthoring.InstallActiveScene();
                UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();
        }
        public static void RepairSavedUnhitchedLanes()
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/TruckHitchSports/Scenes/Unhitched.unity");
            LWS.TruckHitchSports.Editor.CoreHitchSetupRepairs.RepairActiveUnhitchedLanes();AssetDatabase.SaveAssets();
        }
        public static void RepairSavedHitchRunLandingGear()
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/TruckHitchSports/Scenes/HitchAndRun.unity");
            LWS.TruckHitchSports.Editor.CoreHitchSetupRepairs.RepairActiveHitchRunLandingGear();AssetDatabase.SaveAssets();
        }
        public static void RepairSavedTrebuchetLoadingLane()
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/TruckHitchSports/Scenes/TrailerTrebuchet.unity");
            LWS.TruckHitchSports.Editor.CoreHitchSetupRepairs.RepairActiveTrebuchetApparatus();
            AssetDatabase.SaveAssets();
        }
        static void RepairInterruptedDominoSnow()
        {
            const string path = "Assets/TruckExtremeHauling/Scenes/DominoDerby.unity";
            if (!File.Exists(path)) return;
            var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path);
            if (UnityEngine.Object.FindAnyObjectByType<SportsSnowSurface>() != null) return;
            foreach (var trailer in UnityEngine.Object.FindObjectsByType<LWS.TruckHitchSports.TrailerBody>(UnityEngine.FindObjectsSortMode.None))
                if (trailer.GetComponent<SportsSnowTrackExclusion>() == null) trailer.gameObject.AddComponent<SportsSnowTrackExclusion>();
            SportsProductionAuthoring.InstallScene(); SportsFocusPolicy.InstallScene();
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
            UnityEngine.Debug.Log("HH10_DOMINO_SNOW_REPAIRED non-rolling loads excluded; physical trailer components retained.");
        }
    }
}

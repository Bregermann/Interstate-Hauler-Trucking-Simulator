using System;
using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace LWS.TruckKickball.Editor
{
    [InitializeOnLoad]
    public static class KickballEditorJobs
    {
        public const string Folder = "Logs/TruckSports/EssentialRelease";
        static TestRunnerApi runner;
        static KickballTestResults results;
        const string ActiveTest = "Sports.Essential.ActiveTest";
        static KickballEditorJobs() { EditorApplication.update += Tick; EditorApplication.delayCall += RestoreTestCallbacks; }
        static void RestoreTestCallbacks()
        {
            string stem = SessionState.GetString(ActiveTest, "");
            if (stem.Length == 0) return;
            // Unity Test Framework's callback holder is not serialized across
            // the domain reload used by PlayMode tests. Re-register explicitly.
            runner = ScriptableObject.CreateInstance<TestRunnerApi>();
            runner.hideFlags = HideFlags.HideAndDontSave;
            results = ScriptableObject.CreateInstance<KickballTestResults>();
            results.hideFlags = HideFlags.HideAndDontSave;
            results.stem = stem; results.owner = runner; runner.RegisterCallbacks(results);
        }
        static void Tick()
        {
            string path = Folder + "/kickball.request";
            if (runner != null && SessionState.GetString(ActiveTest, "").Length == 0 &&
                !EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling && !EditorApplication.isUpdating)
                ReleaseFinishedRun(runner, results);
            if (runner != null || EditorUtility.scriptCompilationFailed || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isPlaying || !File.Exists(path)) return;
            string command;try { command=File.ReadAllText(path).Trim();File.Delete(path); } catch(IOException) { return; }
            try
            {
                if(command=="race-countdowns"){LWS.TruckTerrierRacing.Editor.TerrierCountdownAuthoring.UpgradeRaces();File.WriteAllText(Folder+"/race-countdown-job.txt","PASS saved "+DateTime.UtcNow.ToString("o"));}
                else if(command=="race-countdown-tests")RunTests("LWS.TruckSports.GoddessPlayModeTests","race-countdown",TestMode.PlayMode,"LWS.TruckSports.Tests.GoddessPhysicsTests.RaceStartingLightsUseAuthoritativeTimersAndSurvivePauseAndRematch");
                else if(command=="combined-validate"){LWS.TruckSports.Editor.SportsBuilder.Validate();File.WriteAllText(Folder+"/combined-validation-job.txt","PASS saved scenes "+DateTime.UtcNow.ToString("o"));}
                else if(command=="kickball-stadium"){KickballAuthoring.UpgradeStadiumDisplay();File.WriteAllText(Folder+"/kickball-stadium-job.txt","PASS saved "+DateTime.UtcNow.ToString("o"));}
                else if(command=="combined-build"){File.WriteAllText(Folder+"/combined-build-job.txt","RUNNING "+DateTime.UtcNow.ToString("o"));LWS.TruckSports.Editor.SportsBuilder.Build();File.WriteAllText(Folder+"/combined-build-job.txt","PASS "+DateTime.UtcNow.ToString("o"));}
                else if(command.StartsWith("derby-traverse ")){UnityEditor.SceneManagement.EditorSceneManager.OpenScene(LWS.TruckDerby.Editor.DerbyAuthoring.Scene);SessionState.SetBool("Sports.Derby.Traverse",true);SessionState.SetString("Sports.Derby.TraverseMode",command.Substring(15));EditorApplication.isPlaying=true;}
                else if(command=="buz-names-play"){UnityEditor.SceneManagement.EditorSceneManager.OpenScene(LWS.TruckBuzkashi.Editor.BuzAuthoring.Scene);SessionState.SetBool("Sports.Buz.Names",true);EditorApplication.isPlaying=true;}
                else if(command=="derby-goddess"){LWS.TruckDerby.Editor.DerbyGoddessAuthoring.DamageFixtures();LWS.TruckDerby.DerbyFixtures.Run();LWS.TruckDerby.Editor.DerbyGoddessAuthoring.Upgrade();File.WriteAllText(Folder+"/derby-goddess-job.txt","PASS saved "+DateTime.UtcNow.ToString("o"));}
                else if(command=="derby-hard-play"){UnityEditor.SceneManagement.EditorSceneManager.OpenScene(LWS.TruckDerby.Editor.DerbyAuthoring.Scene);SessionState.SetBool("LWS.DerbyProbe",true);SessionState.SetBool("LWS.DerbyHardOnly",true);EditorApplication.isPlaying=true;}
                else if(command=="horn-assets"){LWS.TruckSports.Editor.SportsHornAuthoring.Ensure();File.WriteAllText(Folder+"/horn-assets-job.txt","PASS saved "+DateTime.UtcNow.ToString("o"));}
                else if(command=="horn-play"){LWS.TruckSports.Editor.SportsHornAuthoring.Ensure();UnityEditor.SceneManagement.EditorSceneManager.OpenScene(LWS.TruckSports.Editor.SportsBuilder.FrontScene);SessionState.SetBool("Sports.Horns.Probe",true);EditorApplication.isPlaying=true;}
                else if(command=="bowling-regression-play"){UnityEditor.SceneManagement.EditorSceneManager.OpenScene(LWS.TruckBowling.Editor.BowlingAuthoring.Scene);SessionState.SetBool("LWS.BowlingReboundFocused",true);EditorApplication.isPlaying=true;}
                else if(command=="bowling-wheel-audit"){LWS.TruckBowling.Editor.BowlingWheelAudit.Run();File.WriteAllText(Folder+"/bowling-wheel-audit-job.txt","PASS audit "+DateTime.UtcNow.ToString("o"));}
                else if(command=="hockey-series-play"){UnityEditor.SceneManagement.EditorSceneManager.OpenScene(LWS.TruckHockey.Editor.HockeyAuthoring.Scene);SessionState.SetBool("Sports.Hockey.Series",true);EditorApplication.isPlaying=true;}
                else if(command=="derby-audit"){LWS.TruckDerby.Editor.DerbySceneAudit.Run();File.WriteAllText(Folder+"/derby-audit-job.txt","PASS audit "+DateTime.UtcNow.ToString("o"));}
                else if(command=="buz-labels"){LWS.TruckBuzkashi.Editor.BuzNameplateAuthoring.Upgrade();File.WriteAllText(Folder+"/buz-label-job.txt","PASS saved "+DateTime.UtcNow.ToString("o"));}
                else if(command=="fieldgoal-board"){LWS.TruckFieldGoal.Editor.FieldGoalBoardAuthoring.Upgrade();File.WriteAllText(Folder+"/fieldgoal-board-job.txt","PASS saved "+DateTime.UtcNow.ToString("o"));}
                else if(command=="fieldgoal-board-play"){UnityEditor.SceneManagement.EditorSceneManager.OpenScene(LWS.TruckFieldGoal.Editor.FieldGoalAuthoring.Scene);SessionState.SetBool("Sports.FieldGoal.Board",true);EditorApplication.isPlaying=true;}
                else if(command=="fieldgoal-crowd-play"){UnityEditor.SceneManagement.EditorSceneManager.OpenScene(LWS.TruckFieldGoal.Editor.FieldGoalAuthoring.Scene);SessionState.SetBool("Sports.FieldGoal.Crowd",true);EditorApplication.isPlaying=true;}
                else if(command=="goddess-stage"){LWS.TruckFieldGoal.Editor.FieldGoalCrowdAuthoring.Upgrade();LWS.TruckMinefield.Editor.MinefieldGoddessAuthoring.Upgrade();LWS.TruckLimbo.Editor.LimboReviewAuthoring.Upgrade();LWS.TruckWifeCarrying.Editor.WifeGoddessAuthoring.Upgrade();LWS.TruckCheeseRolling.Editor.CheeseIndicatorAuthoring.Upgrade();File.WriteAllText(Folder+"/goddess-stage-job.txt","PASS saved "+DateTime.UtcNow.ToString("o"));}
                else if(command=="minefield-flags"){var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(LWS.TruckMinefield.Editor.MinefieldAuthoring.Scene);LWS.TruckMinefield.Editor.MinefieldGoddessAuthoring.Flags(UnityEngine.Object.FindAnyObjectByType<LWS.TruckMinefield.MinefieldMatch>());UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();File.WriteAllText(Folder+"/minefield-flags-job.txt","PASS saved "+DateTime.UtcNow.ToString("o"));}
                else if(command=="minefield-goddess"){LWS.TruckMinefield.Editor.MinefieldGoddessAuthoring.Upgrade();File.WriteAllText(Folder+"/minefield-goddess-job.txt","PASS saved "+DateTime.UtcNow.ToString("o"));}
                else if(command=="dock-goddess"){LWS.TruckDockDiving.Editor.DockSlangAuthoring.Upgrade();File.WriteAllText(Folder+"/dock-goddess-job.txt","PASS saved "+DateTime.UtcNow.ToString("o"));}
                else if(command=="dock-goddess-play"){UnityEditor.SceneManagement.EditorSceneManager.OpenScene(LWS.TruckDockDiving.Editor.DockAuthoring.Scene);SessionState.SetBool("Sports.Dock.Slang",true);EditorApplication.isPlaying=true;}
                else if(command=="fieldgoal-crowd"){LWS.TruckFieldGoal.Editor.FieldGoalCrowdAuthoring.Upgrade();File.WriteAllText(Folder+"/fieldgoal-crowd-job.txt","PASS saved "+DateTime.UtcNow.ToString("o"));}
                else if(command=="fieldgoal-play"){UnityEditor.SceneManagement.EditorSceneManager.OpenScene(LWS.TruckFieldGoal.Editor.FieldGoalAuthoring.Scene);SessionState.SetBool("LWS.FieldGoal",true);EditorApplication.isPlaying=true;}
                else if(command=="poker-venue"){LWS.TruckPoker.Editor.PokerVenueAuthoring.Upgrade();File.WriteAllText(Folder+"/poker-venue-job.txt","PASS saved "+DateTime.UtcNow.ToString("o"));}
                else if(command=="poker-focused-play"){UnityEditor.SceneManagement.EditorSceneManager.OpenScene(LWS.TruckPoker.Editor.PokerAuthoring.Scene);SessionState.SetBool("LWS.PokerFocused",true);EditorApplication.isPlaying=true;}
                else if (command == "dance-create") { LWS.TruckDanceOff.Editor.DanceAuthoring.Create(); File.WriteAllText(Folder+"/dance-job.txt", "PASS create "+DateTime.UtcNow.ToString("o")); }
                else if(command=="dance-presentation"){LWS.TruckDanceOff.Editor.DancePresentationAuthoring.Upgrade();File.WriteAllText(Folder+"/dance-job.txt","PASS presentation "+DateTime.UtcNow.ToString("o"));}
                else if(command=="dance-venue"){LWS.TruckDanceOff.Editor.DanceVenueAuthoring.Upgrade();File.WriteAllText(Folder+"/dance-job.txt","PASS venue "+DateTime.UtcNow.ToString("o"));}
                else if(command=="pong-pacing-play"){UnityEditor.SceneManagement.EditorSceneManager.OpenScene(LWS.TruckBeerPong.Editor.PongAuthoring.Scene);SessionState.SetBool("Sports.Pong.Pacing",true);EditorApplication.isPlaying=true;}
                else if(command=="pong-pacing"){LWS.TruckBeerPong.Editor.PongPacingAuthoring.Upgrade();File.WriteAllText(Folder+"/pong-pacing-job.txt","PASS saved "+DateTime.UtcNow.ToString("o"));}
                else if(command=="bog-hard-play"){UnityEditor.SceneManagement.EditorSceneManager.OpenScene(LWS.TruckBogSnorkelling.Editor.BogAuthoring.Scene);SessionState.SetBool("Sports.Bog.Hard",true);EditorApplication.isPlaying=true;}
                else if(command=="bog-hard-create"){LWS.TruckBogSnorkelling.Editor.BogHardCourseAuthoring.Upgrade();File.WriteAllText(Folder+"/bog-hard-job.txt","PASS saved "+DateTime.UtcNow.ToString("o"));}
                else if(command=="bowling-highlights-play"){UnityEditor.SceneManagement.EditorSceneManager.OpenScene(LWS.TruckBowling.Editor.BowlingAuthoring.Scene);SessionState.SetBool("Sports.Bowling.Highlights",true);EditorApplication.isPlaying=true;}
                else if(command=="bowling-highlights"){LWS.TruckBowling.Editor.BowlingHighlightsAuthoring.Upgrade();File.WriteAllText(Folder+"/bowling-highlights-job.txt","PASS saved "+DateTime.UtcNow.ToString("o"));}
                else if(command=="bowling-upgrade"){LWS.TruckBowling.Editor.BowlingGoddessAuthoring.Upgrade();File.WriteAllText(Folder+"/bowling-job.txt","PASS upgrade "+DateTime.UtcNow.ToString("o"));}
                else if(command=="mattress-upgrade"){LWS.TruckMattressRacing.Editor.MattressGoddessAuthoring.Upgrade();File.WriteAllText(Folder+"/mattress-job.txt","PASS upgrade "+DateTime.UtcNow.ToString("o"));}
                else if(command=="limbo-review"){LWS.TruckLimbo.Editor.LimboReviewAuthoring.Upgrade();File.WriteAllText(Folder+"/limbo-review-job.txt","PASS saved "+DateTime.UtcNow.ToString("o"));}
                else if(command=="limbo-create"){LWS.TruckLimbo.Editor.LimboAuthoring.Create();File.WriteAllText(Folder+"/limbo-job.txt","PASS create "+DateTime.UtcNow.ToString("o"));}
                else if(command=="limbo-dynamics"){LWS.TruckLimbo.Editor.LimboAuthoring.UpgradeDynamics();File.WriteAllText(Folder+"/limbo-job.txt","PASS dynamics "+DateTime.UtcNow.ToString("o"));}
                else if(command=="curling-upgrade"){LWS.TruckCurling.Editor.CurlingAuthoring.UpgradeGoddess();File.WriteAllText(Folder+"/curling-job.txt","PASS upgrade "+DateTime.UtcNow.ToString("o"));}
                else if(command=="parking-cameras"){LWS.TruckMusicalParking.Editor.ParkingAuthoring.UpgradeCameras();File.WriteAllText(Folder+"/parking-job.txt","PASS cameras "+DateTime.UtcNow.ToString("o"));}
                else if(command=="skytow-create"){LWS.TruckSkyTow.Editor.SkyTowAuthoring.Create();File.WriteAllText(Folder+"/skytow-job.txt","PASS create "+DateTime.UtcNow.ToString("o"));}
                else if(command=="sumo-goddess"){LWS.TruckSumo.Editor.SumoGoddessAuthoring.Upgrade();File.WriteAllText(Folder+"/sumo-job.txt","PASS upgrade "+DateTime.UtcNow.ToString("o"));}
                else if(command=="skytow-lanes"){LWS.TruckSkyTow.Editor.SkyTowAuthoring.UpgradeLanes();File.WriteAllText(Folder+"/skytow-job.txt","PASS lanes "+DateTime.UtcNow.ToString("o"));}
                else if(command=="cornhole-weather"){LWS.TruckCornhole.Editor.CornholeWeatherAuthoring.Upgrade();File.WriteAllText(Folder+"/cornhole-weather-job.txt","PASS upgrade "+DateTime.UtcNow.ToString("o"));}
                else if(command=="skytow-presentation"){LWS.TruckSkyTow.Editor.SkyTowPresentationAuthoring.Upgrade();File.WriteAllText(Folder+"/skytow-job.txt","PASS presentation "+DateTime.UtcNow.ToString("o"));}
                else if(command=="trailerplug-presentation"){LWS.TruckTrailerPlug.Editor.TrailerPlugAuthoring.UpgradePresentation();File.WriteAllText(Folder+"/trailerplug-job.txt","PASS presentation "+DateTime.UtcNow.ToString("o"));}
                else if(command=="trailerplug-create"){LWS.TruckTrailerPlug.Editor.TrailerPlugAuthoring.Create();File.WriteAllText(Folder+"/trailerplug-job.txt","PASS create "+DateTime.UtcNow.ToString("o"));}
                else if(command=="farefight-create"){LWS.TruckFareFight.Editor.FareFightAuthoring.Create();File.WriteAllText(Folder+"/farefight-job.txt","PASS create "+DateTime.UtcNow.ToString("o"));}
                else if(command=="farefight-presentation"){LWS.TruckFareFight.Editor.FareFightPresentationAuthoring.Upgrade();File.WriteAllText(Folder+"/farefight-job.txt","PASS presentation "+DateTime.UtcNow.ToString("o"));}
                else if(command=="farefight-vehicle"){LWS.TruckFareFight.Editor.FareFightVehicleAuthoring.EnsurePrefab();AssetDatabase.SaveAssets();File.WriteAllText(Folder+"/farefight-job.txt","PASS vehicle prefab "+DateTime.UtcNow.ToString("o"));}
                else if(command=="farefight-district-audit"){LWS.TruckFareFight.Editor.FareFightDistrictAudit.Run();File.WriteAllText(Folder+"/farefight-job.txt","PASS district audit "+DateTime.UtcNow.ToString("o"));}
                else if(command=="snake-create"){LWS.TruckSnake.Editor.SnakeAuthoring.Create();File.WriteAllText(Folder+"/snake-job.txt","GENERATING "+DateTime.UtcNow.ToString("o"));}
                else if(command=="poker-dealer-views"){UnityEditor.SceneManagement.EditorSceneManager.OpenScene(LWS.TruckPoker.Editor.PokerAuthoring.Scene);SessionState.SetBool("Sports.Poker.Dealer",true);SessionState.SetString("Sports.Poker.DealerMode","views");EditorApplication.isPlaying=true;}
                else if(command=="poker-dealer-play"){UnityEditor.SceneManagement.EditorSceneManager.OpenScene(LWS.TruckPoker.Editor.PokerAuthoring.Scene);SessionState.SetBool("Sports.Poker.Dealer",true);EditorApplication.isPlaying=true;}
                else if(command=="poker-dealer"){LWS.TruckPoker.Editor.PokerMobileDealerAuthoring.Upgrade();File.WriteAllText(Folder+"/poker-dealer-job.txt","PASS saved "+DateTime.UtcNow.ToString("o"));}
                else if(command=="snake-growth"){LWS.TruckSnake.Editor.SnakeGrowthAuthoring.Upgrade();}
                else if(command=="snake-grade"){LWS.TruckSnake.Editor.SnakeRoadGradeAuthoring.Upgrade();File.WriteAllText(Folder+"/snake-grade-job.txt","PASS saved "+DateTime.UtcNow.ToString("o"));}
                else if(command=="snake-presentation"){LWS.TruckSnake.Editor.SnakePresentationAuthoring.Upgrade();File.WriteAllText(Folder+"/snake-presentation-job.txt","PASS saved "+DateTime.UtcNow.ToString("o"));}
                else if(command=="snake-menu"){LWS.TruckSnake.Editor.SnakePresentationAuthoring.Menu();File.WriteAllText(Folder+"/snake-menu-job.txt","PASS saved "+DateTime.UtcNow.ToString("o"));}
                else if(command=="snake-rules")RunTests("LWS.TruckSnake.EditModeTests","snake",TestMode.EditMode);
                else if(command=="farefight-rules")RunTests("LWS.TruckFareFight.EditModeTests","farefight",TestMode.EditMode);
                else if(command=="trailerplug-rules")RunTests("LWS.TruckTrailerPlug.EditModeTests","trailerplug",TestMode.EditMode);
                else if(command=="skytow-rules")RunTests("LWS.TruckSkyTow.EditModeTests","skytow",TestMode.EditMode);
                else if(command=="limbo-rules")RunTests("LWS.TruckLimbo.EditModeTests","limbo",TestMode.EditMode);
                else if (command == "rules") RunRules();
                else if(command=="fishing-goddess-rules")RunTests("LWS.TruckFishing.GoddessEditModeTests","fishing-goddess",TestMode.EditMode);
                else if(command=="fishing-goddess"){LWS.TruckFishing.Editor.FishingGoddessAuthoring.Upgrade();File.WriteAllText(Folder+"/fishing-goddess-job.txt","PASS saved "+DateTime.UtcNow.ToString("o"));}
                else if(command=="fishing-goddess-play"){UnityEditor.SceneManagement.EditorSceneManager.OpenScene(LWS.TruckFishing.Editor.FishingAuthoring.Scene);SessionState.SetBool("Sports.Fishing.Goddess",true);EditorApplication.isPlaying=true;}
                else if(command=="wife-goddess"){LWS.TruckWifeCarrying.Editor.WifeGoddessAuthoring.Upgrade();File.WriteAllText(Folder+"/wife-goddess-job.txt","PASS saved "+DateTime.UtcNow.ToString("o"));}
                else if(command.StartsWith("wife-goddess-play ")){UnityEditor.SceneManagement.EditorSceneManager.OpenScene(LWS.TruckWifeCarrying.Editor.WifeAuthoring.Scene);SessionState.SetString("Sports.Wife.Goddess",command.Substring(18));EditorApplication.isPlaying=true;}
                else if(command=="terrier-dessert-play"){UnityEditor.SceneManagement.EditorSceneManager.OpenScene(LWS.TruckTerrierRacing.Editor.TerrierAuthoring.Scene);SessionState.SetBool("LWS.TerrierDessertProbe",true);EditorApplication.isPlaying=true;}
                else if(command=="terrier-finish-play"){UnityEditor.SceneManagement.EditorSceneManager.OpenScene(LWS.TruckTerrierRacing.Editor.TerrierAuthoring.Scene);SessionState.SetBool("LWS.TerrierFinishProbe",true);EditorApplication.isPlaying=true;}
                else if(command=="terrier-goddess"){LWS.TruckTerrierRacing.Editor.TerrierGoddessAuthoring.Upgrade();File.WriteAllText(Folder+"/terrier-goddess-job.txt","PASS saved "+DateTime.UtcNow.ToString("o"));}
                else if(command.StartsWith("terrier-goddess-play ")){UnityEditor.SceneManagement.EditorSceneManager.OpenScene(LWS.TruckTerrierRacing.Editor.TerrierAuthoring.Scene);SessionState.SetString("Sports.Terrier.Goddess",command.Substring(21));EditorApplication.isPlaying=true;}
                else if(command=="cornhole-replay-test")RunTests("LWS.TruckSports.GoddessPlayModeTests","cornhole-replay",TestMode.PlayMode,"LWS.TruckSports.Tests.GoddessPhysicsTests.CornholeReplayRewindCannotConsumeTimeFromBeforeTheAction");
                else if(command=="goddess-physics-tests")RunTests("LWS.TruckSports.GoddessPlayModeTests","goddess-physics",TestMode.PlayMode);
                else if(command=="goddess-fare-ui")RunTests("LWS.TruckSports.GoddessPlayModeTests","goddess-fare-ui",TestMode.PlayMode,"LWS.TruckSports.Tests.GoddessPhysicsTests.FareFightHumanAndAICameraOwnership");
                else if (command == "rhythm-rules") RunTests("LWS.TruckDanceOff.EditModeTests", "rhythm", TestMode.EditMode);
                else if (command == "playmode-tests") RunTests("LWS.TruckKickball.PlayModeTests", "kickball-playmode", TestMode.PlayMode);
                else if (command == "create")
                {
                    KickballAuthoring.Create();
                    File.WriteAllText(Folder + "/kickball-job.txt", "PASS create " + DateTime.UtcNow.ToString("o"));
                }
                else if(command.StartsWith("cornhole-play ")){UnityEditor.SceneManagement.EditorSceneManager.OpenScene(LWS.TruckSports.Editor.SportsBuilder.FrontScene);SessionState.SetString("Sports.Cornhole.Smoke",command.Substring(14));EditorApplication.isPlaying=true;}
                else if(command=="menu-play"){UnityEditor.SceneManagement.EditorSceneManager.OpenScene(LWS.TruckSports.Editor.SportsBuilder.FrontScene);SessionState.SetBool("Sports.MenuPresentation.Probe",true);EditorApplication.isPlaying=true;}
                else if(command=="menu-layout"){LWS.TruckSports.Editor.SportsMenuArtAuthoring.Reflow();File.WriteAllText(Folder+"/menu-art-job.txt","PASS layout "+DateTime.UtcNow.ToString("o"));}
                else if(command=="menu-art"){LWS.TruckSports.Editor.SportsMenuArtAuthoring.Upgrade();File.WriteAllText(Folder+"/menu-art-job.txt","PASS upgrade "+DateTime.UtcNow.ToString("o"));}
                else if(command=="collection-billiards"){UnityEditor.SceneManagement.EditorSceneManager.OpenScene(LWS.TruckSports.Editor.SportsBuilder.FrontScene);SessionState.SetString("Sports.MenuCapture.Batch","CollectionOpenings/Retry-02");SessionState.SetString("Sports.MenuCapture.Paths",JsonUtility.ToJson(new LWS.TruckSports.SportsMenuCaptureProbe.Manifest{paths=new[]{LWS.TruckBilliards.Editor.BilliardsBuilder.ScenePath}}));SessionState.SetBool("Sports.MenuCapture.Run",true);EditorApplication.isPlaying=true;}
                else if(command=="collection-retry"){UnityEditor.SceneManagement.EditorSceneManager.OpenScene(LWS.TruckSports.Editor.SportsBuilder.FrontScene);SessionState.SetString("Sports.MenuCapture.Batch","CollectionOpenings/Retry-01");SessionState.SetString("Sports.MenuCapture.Paths",JsonUtility.ToJson(new LWS.TruckSports.SportsMenuCaptureProbe.Manifest{paths=new[]{LWS.TruckBilliards.Editor.BilliardsBuilder.ScenePath,LWS.TruckSumo.Editor.SumoAuthoring.Scene}}));SessionState.SetBool("Sports.MenuCapture.Run",true);EditorApplication.isPlaying=true;}
                else if(command=="collection-openings"){SessionState.SetString("Sports.MenuCapture.Batch","CollectionOpenings");UnityEditor.SceneManagement.EditorSceneManager.OpenScene(LWS.TruckSports.Editor.SportsBuilder.FrontScene);SessionState.SetString("Sports.MenuCapture.Paths",JsonUtility.ToJson(new LWS.TruckSports.SportsMenuCaptureProbe.Manifest{paths=LWS.TruckSports.Editor.SportsBuilder.SceneList()}));SessionState.SetBool("Sports.MenuCapture.Run",true);EditorApplication.isPlaying=true;}
                else if(command.StartsWith("plow-play ")){UnityEditor.SceneManagement.EditorSceneManager.OpenScene(LWS.TruckSports.Editor.SportsBuilder.FrontScene);SessionState.SetBool("LWS.PlowProbe",true);SessionState.SetString("LWS.PlowMode",command.Substring(10));SessionState.SetBool("Sports.PlowOwnEditorRun",true);EditorApplication.isPlaying=true;}
                else if(command=="snake-cadence"){UnityEditor.SceneManagement.EditorSceneManager.OpenScene(LWS.TruckSnake.Editor.SnakeAuthoring.Scene);SessionState.SetBool("Sports.Snake.Cadence",true);EditorApplication.isPlaying=true;}
                else if(command=="snake-convoy"){UnityEditor.SceneManagement.EditorSceneManager.OpenScene(LWS.TruckSnake.Editor.SnakeAuthoring.Scene);SessionState.SetBool("Sports.Snake.Convoy",true);EditorApplication.isPlaying=true;}
                else if(command=="snake-roads")LWS.TruckSnake.Editor.SnakeAuthoring.RepairRoads();
                else if(command.StartsWith("snake-play ")){UnityEditor.SceneManagement.EditorSceneManager.OpenScene(LWS.TruckSnake.Editor.SnakeAuthoring.Scene);SessionState.SetBool("Sports.Snake.Probe",true);SessionState.SetString("Sports.Snake.Mode",command.Substring(11));EditorApplication.isPlaying=true;}
                else if(command=="farefight-devices"){UnityEditor.SceneManagement.EditorSceneManager.OpenScene(LWS.TruckFareFight.Editor.FareFightAuthoring.Scene);SessionState.SetBool("Sports.FareDevices.Probe",true);EditorApplication.isPlaying=true;}
                else if(command.StartsWith("farefight-play ")){UnityEditor.SceneManagement.EditorSceneManager.OpenScene(LWS.TruckFareFight.Editor.FareFightAuthoring.Scene);SessionState.SetBool("Sports.FareFight.Probe",true);SessionState.SetString("Sports.FareFight.Mode",command.Substring(15));EditorApplication.isPlaying=true;}
                else if(command.StartsWith("trailerplug-play ")){UnityEditor.SceneManagement.EditorSceneManager.OpenScene(LWS.TruckTrailerPlug.Editor.TrailerPlugAuthoring.Scene);SessionState.SetBool("Sports.TrailerPlug.Probe",true);SessionState.SetString("Sports.TrailerPlug.Mode",command.Substring(17));EditorApplication.isPlaying=true;}
                else if(command.StartsWith("skytow-play ")){UnityEditor.SceneManagement.EditorSceneManager.OpenScene(LWS.TruckSkyTow.Editor.SkyTowAuthoring.Scene);SessionState.SetBool("Sports.SkyTow.Probe",true);SessionState.SetString("Sports.SkyTow.Mode",command.Substring(12));EditorApplication.isPlaying=true;}
                else if(command.StartsWith("sumo-play ")){UnityEditor.SceneManagement.EditorSceneManager.OpenScene(LWS.TruckSumo.Editor.SumoAuthoring.Scene);SessionState.SetBool("LWS.SumoProbe",true);SessionState.SetInt("LWS.SumoDifficulty",int.Parse(command.Substring(10)));EditorApplication.isPlaying=true;}
                else if(command.StartsWith("limbo-play ")){UnityEditor.SceneManagement.EditorSceneManager.OpenScene(LWS.TruckLimbo.Editor.LimboAuthoring.Scene);SessionState.SetBool("Sports.Limbo.Probe",true);SessionState.SetString("Sports.Limbo.Mode",command.Substring(11));EditorApplication.isPlaying=true;}
                else if(command.StartsWith("dance-play ")){UnityEditor.SceneManagement.EditorSceneManager.OpenScene(LWS.TruckDanceOff.Editor.DanceAuthoring.Scene);SessionState.SetBool("Sports.Dance.Probe",true);SessionState.SetString("Sports.Dance.Mode",command.Substring(11));EditorApplication.isPlaying=true;}
                else if (command.StartsWith("play "))
                {
                    UnityEditor.SceneManagement.EditorSceneManager.OpenScene(KickballAuthoring.Scene);
                    SessionState.SetBool("Sports.Kickball.Probe", true);
                    SessionState.SetString("Sports.Kickball.Mode", command.Substring(5));
                    EditorApplication.isPlaying = true;
                }
                else if (command == "front")
                {
                    LWS.TruckSports.Editor.SportsNewEntriesAuthoring.Install();
                    File.WriteAllText(Folder + "/kickball-job.txt", "PASS front " + DateTime.UtcNow.ToString("o"));
                }
                else if (command == "upgrade")
                {
                    KickballAuthoring.UpgradePresentation();
                    File.WriteAllText(Folder + "/kickball-job.txt", "PASS upgrade " + DateTime.UtcNow.ToString("o"));
                }
                else throw new ArgumentException("Unknown explicit Kickball job: " + command);
            }
            catch (Exception e) { File.WriteAllText(Folder + "/kickball-job.txt", "FAIL " + e); Debug.LogException(e); }
        }
        [MenuItem("Truck Sports/Kickball/Run EditMode rules")]
        public static void RunRules()
        {
            RunTests("LWS.TruckKickball.EditModeTests", "kickball", TestMode.EditMode);
        }
        static void RunTests(string assembly, string stem, TestMode mode,string testName=null)
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode before rule tests.");
            if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Test launch requires a clean saved scene. Preserve/review unsaved changes and open the saved front end first; do not silently save runtime vendor state.");
            Directory.CreateDirectory(Folder);
            File.WriteAllText(Folder + "/" + stem + "-rules.txt", "RUNNING " + DateTime.UtcNow.ToString("o"));
            SessionState.SetString(ActiveTest, stem);
            RestoreTestCallbacks();
            var filter=new Filter { testMode = mode, assemblyNames = new[] { assembly } };if(!string.IsNullOrEmpty(testName))filter.testNames=new[]{testName};runner.Execute(new ExecutionSettings(filter));
        }
        internal static void ReleaseFinishedRun(TestRunnerApi completedOwner, KickballTestResults completedCallback)
        {
            // Domain reload can replace the callback owner. Release this job's
            // stale static handle too, otherwise the next queued job waits forever.
            // A newly started run must keep its own owner and callbacks.
            if (SessionState.GetString(ActiveTest, "").Length == 0)
            {
                if (runner != null && runner != completedOwner)
                {
                    if (results != null) runner.UnregisterCallbacks(results);
                    UnityEngine.Object.DestroyImmediate(runner);
                }
                if (results != null && results != completedCallback) UnityEngine.Object.DestroyImmediate(results);
                runner = null;
                results = null;
            }
            if (completedOwner != null)
            {
                if (completedCallback != null) completedOwner.UnregisterCallbacks(completedCallback);
                UnityEngine.Object.DestroyImmediate(completedOwner);
            }
            if (completedCallback != null) UnityEngine.Object.DestroyImmediate(completedCallback);
        }
    }
    public sealed class KickballTestResults : ScriptableObject, ICallbacks
    {
        public string stem = "kickball";
        public TestRunnerApi owner;
        public void RunStarted(ITestAdaptor testsToRun) { }
        public void TestStarted(ITestAdaptor test) { File.AppendAllText(KickballEditorJobs.Folder+"/"+stem+"-progress.txt",DateTime.UtcNow.ToString("o")+" START "+test.FullName+Environment.NewLine); }
        public void TestFinished(ITestResultAdaptor result) { File.AppendAllText(KickballEditorJobs.Folder+"/"+stem+"-progress.txt",DateTime.UtcNow.ToString("o")+" FINISH "+result.ResultState+Environment.NewLine); }
        public void RunFinished(ITestResultAdaptor result)
        {
            SessionState.EraseString("Sports.Essential.ActiveTest");
            TestRunnerApi.SaveResultToFile(result, KickballEditorJobs.Folder + "/" + stem + "-tests.xml");
            File.WriteAllText(KickballEditorJobs.Folder + "/" + stem + "-rules.txt", (result.FailCount == 0 && result.PassCount > 0 ? "PASS " : "FAIL ") + "passed=" + result.PassCount + " failed=" + result.FailCount + " skipped=" + result.SkipCount + " " + DateTime.UtcNow.ToString("o"));
            var completedOwner = owner;
            var completedCallback = this;
            EditorApplication.delayCall += () => KickballEditorJobs.ReleaseFinishedRun(completedOwner, completedCallback);
        }
    }
}

using System;
using UnityEditor;
using UnityEngine;

namespace LWS.TruckSports.Editor
{
    // Explicit integration of separately reviewed, non-destructive authoring
    // operations. No build/load/PlayMode callback invokes this tool.
    public static class SportsCarnivalAuthoring
    {
        public static void InstallNextAppearanceBoundaries()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stopped Editor required for explicit appearance integration.");
            SportsTruckAppearanceHostAuthoring.BindMissingLogAndWallballHosts();
            foreach(string mode in new[]{"TruckSkyTow","TruckGiantSwing","TruckFieldGoal","TruckBeerPong"})
            {
                var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/"+mode+"/Scenes/"+mode+".unity");
                SportsTruckAppearanceHostAuthoring.InstallOpenLaunchFourAppearance();
                if(scene.isDirty)UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
            }
            var corn=UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/TruckCornhole/Scenes/TruckCornhole.unity");
            SportsTruckAppearanceHostAuthoring.InstallOpenCornholeAppearance();
            if(corn.isDirty)UnityEditor.SceneManagement.EditorSceneManager.SaveScene(corn);
            AssetDatabase.SaveAssets();
            Debug.Log("CARNIVAL_EXPLICIT_APPEARANCE_BOUNDARIES_SAVED Log4,Wallball8,Sky4,Swing1,FieldGoal1,Pong1; native acceptance remains separate.");
        }
        public static void InstallPrivateBurstAppearance()
        {
            SportsTruckFragmentRecipeAuthoring.CreateMissingRecipes();
            SportsTruckAppearanceHostAuthoring.BindMissingBurstFamilyHosts();
        }
        public static void InstallCurlingAppearanceBoundary()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stopped Editor required for explicit retained-stone appearance integration.");
            var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/TruckCurling/Scenes/TruckCurling.unity");
            SportsTruckAppearanceHostAuthoring.InstallOpenCurlingAppearance();
            if(scene.isDirty)UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
        }
        public static void InstallStaticAppearanceAndCompatibility()
        {
            SportsTruckAppearanceHostAuthoring.BindMissingStaticSportHosts();
            SportsTruckAppearanceHostAuthoring.BindMissingWifeRoleHosts();
            var target=UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/TruckTargetLaunch/Scenes/TruckTargetLaunch.unity");
            SportsTruckAppearanceHostAuthoring.InstallOpenTargetAppearance();
            if(target.isDirty)UnityEditor.SceneManagement.EditorSceneManager.SaveScene(target);
            SportsTruckCompatibilityAuthoring.InstallMissingRegisteredScenes();
        }
        public static void RefineKickballSnowPresentation()
        {
            LWS.TruckKickball.Editor.KickballShoveledSnowAuthoring.RefineOwnedSnowBanks();
            LWS.TruckKickball.Editor.KickballShoveledSnowAuthoring.RefineOwnedSnowBankDetailMaterial();
        }
        public static void InstallTugAppearanceAndFit()
        {
            SportsTruckAppearanceHostAuthoring.BindMissingTugAppearanceHosts();
            SportsTruckCompatibilityAuthoring.InstallMissingRegisteredScenes();
        }
        public static void InstallInitialSlice()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before explicit Carnival authoring.");
            SportsTruckAppearanceContentAuthoring.CreateMissingContent();
            SportsTruckCustomizerAuthoring.CreateReusablePrefab();
            SportsTruckCustomizerEntryAuthoring.Install();
            SportsTruckAppearanceHostAuthoring.BindMissingHosts();
            LWS.TruckHungryHaulers.Editor.HaulerPhysicalDumpAuthoring.Install();
            AssetDatabase.SaveAssets();
            Debug.Log("CARNIVAL_INITIAL_SLICE_SAVED owned appearance catalog/shop/crew entries, Parking/Bowling visual hosts and Haulers physical bank holes. Runtime and visual acceptance remain separate.");
        }
        public static void InstallSeventhSlice()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before explicit Carnival authoring.");
            InstallSixthSlice();
            LWS.TruckFieldGoal.Editor.FieldGoalCarnivalAuthoring.RetireSupersededAudience();
            CarnivalArenaHudAuthoring.Install();
            SportsTruckAppearanceHostAuthoring.BindMissingBogAndSumoHosts();
            var jumping = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/TruckBaseJumping/Scenes/TruckBaseJumping.unity");
            LWS.TruckBaseJumping.Editor.BaseBirdAuthoring.InstallOpenScene();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(jumping);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(jumping);
            AssetDatabase.SaveAssets();
            Debug.Log("CARNIVAL_SEVENTH_SLICE_SAVED five targeted HUD scenes, protected Bog/Sumo appearance hosts, recorded Field Goal board/stadium, repaired humanoid audience and genuine animated CC0 birds. Native/Windows acceptance remains separate.");
        }
        public static void InstallEighthSlice()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before explicit Carnival authoring.");
            SportsTruckAppearanceHostAuthoring.BindMissingBaseAndBungeeHosts();
            var cornhole = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/TruckCornhole/Scenes/TruckCornhole.unity");
            LWS.TruckCornhole.Editor.CornholeCarnivalAuthoring.InstallOpenScene();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(cornhole);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(cornhole);
            AssetDatabase.SaveAssets();
            Debug.Log("CARNIVAL_EIGHTH_SLICE_SAVED protected suspension/canopy appearance hosts and four original Cornhole approaches into one shared garden board. Native/Windows acceptance remains separate.");
        }
        public static void InstallCornholeGardenOnly()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before explicit Carnival authoring.");
            var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/TruckCornhole/Scenes/TruckCornhole.unity");
            LWS.TruckCornhole.Editor.CornholeCarnivalAuthoring.InstallOpenScene();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }
        public static void InstallNinthSlice()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before explicit Carnival authoring.");
            SportsTruckAppearanceHostAuthoring.BindMissingFixedOwnerFacadeHosts();
            SportsRaceFinishAuthoring.Install();
            var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/TruckCornhole/Scenes/TruckCornhole.unity");
            LWS.TruckCornhole.Editor.CornholeHudAuthoring.InstallOpenScene();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("CARNIVAL_NINTH_SLICE_SAVED thirteen explicit fixed-owner appearance venues, eight native-authority finish presentations and Cornhole compact HUD. Actual native and Windows acceptance remains separate.");
        }
        public static void InstallPresentationOnly()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before explicit Carnival authoring.");
            var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/TruckCornhole/Scenes/TruckCornhole.unity");
            LWS.TruckCornhole.Editor.CornholeHudAuthoring.InstallOpenScene();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
            SportsRaceFinishAuthoring.Install();
            AssetDatabase.SaveAssets();
        }
        public static void InstallLaunchContextsAndFinishes()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before explicit Carnival authoring.");
            foreach(var sport in new[]{"TruckDockDiving","TruckGiantSwing","TruckTargetLaunch","TruckBeerPong","TruckSkyTow"})
            {
                var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/"+sport+"/Scenes/"+sport+".unity");
                if(sport=="TruckDockDiving")LWS.TruckDockDiving.Editor.DockCompactHudAuthoring.InstallOpenScene();
                else if(sport=="TruckGiantSwing")LWS.TruckGiantSwing.Editor.SwingCompactHudAuthoring.InstallOpenScene();
                else if(sport=="TruckTargetLaunch")LWS.TruckTargetLaunch.Editor.TargetCompactHudAuthoring.InstallOpenScene();
                else if(sport=="TruckBeerPong")LWS.TruckBeerPong.Editor.PongCompactHudAuthoring.InstallOpenScene();
                else LWS.TruckSkyTow.Editor.SkyTowCompactHudAuthoring.InstallOpenScene();
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
            }
            SportsRaceFinishAuthoring.Install();AssetDatabase.SaveAssets();
        }
        public static void InstallSixthSlice()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before explicit Carnival authoring.");
            var pong = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/TruckBeerPong/Scenes/TruckBeerPong.unity");
            LWS.TruckBeerPong.Editor.PongTailgateAuthoring.InstallOpenScene();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(pong);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(pong);
            LWS.TruckFieldGoal.Editor.FieldGoalCarnivalAuthoring.InstallMissingPresentation();
            AssetDatabase.SaveAssets();
            Debug.Log("CARNIVAL_SIXTH_SLICE_SAVED verified humanoid tailgate foundation, curved two-deck Field Goal bowl and actual recorded-pose scoreboard video. Native and Windows acceptance remain separate.");
        }
        public static void RepairFifthSlice()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before explicit Carnival authoring.");
            SportsTruckAppearanceContentAuthoring.RepairInitialLiveryWinding();
            var pong = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/TruckBeerPong/Scenes/TruckBeerPong.unity");
            LWS.TruckBeerPong.Editor.PongTailgateAuthoring.InstallOpenScene();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(pong);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(pong);
            AssetDatabase.SaveAssets();
            Debug.Log("CARNIVAL_FIFTH_REPAIR_SAVED mirrored source livery normals/winding and genuine compatible humanoid tailgate assets; no existing field, physics, scoring or vendor source changed.");
        }
        public static void InstallFourthSlice()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before explicit Carnival authoring.");
            SportsTruckAppearanceContentAuthoring.AddMissingAtlasLivery();
            SportsTruckAppearanceHostAuthoring.BindMissingCleanModelHosts();
            SportsTruckAppearanceHostAuthoring.RefreshOwnedLiveryPaintSlots();
            var pong = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/TruckBeerPong/Scenes/TruckBeerPong.unity");
            LWS.TruckBeerPong.Editor.PongTailgateAuthoring.InstallOpenScene();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(pong);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(pong);
            AssetDatabase.SaveAssets();
            Debug.Log("CARNIVAL_FOURTH_SLICE_SAVED owned atlas livery, fourteen clean-model appearance hosts and Beer Pong tailgate. Native gameplay, rendering and preservation acceptance remain separate.");
        }
        public static void InstallThirdSlice()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before explicit Carnival authoring.");
            LWS.TruckHungryHaulers.Editor.HaulerCargoHudAuthoring.InstallEssentialClocks();
            LWS.TruckFishing.Editor.FishingCarnivalPresentationAuthoring.InstallMissingPresentation();
            LWS.TruckNinjaWarrior.Editor.NinjaCarnivalEnvironmentAuthoring.InstallMissingEnvironment();
            SportsCarnivalHudAuthoring.Install();
            var skee = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/TruckSkeeBall/Scenes/TruckSkeeBall.unity");
            LWS.TruckSkeeBall.Editor.SkeeCarnivalAuthoring.InstallOpenScene();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(skee);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(skee);
            AssetDatabase.SaveAssets();
            Debug.Log("CARNIVAL_THIRD_SLICE_SAVED Hauler essential clocks, repaired real wildlife controller, Ninja spectator/fire/water presentation, seven typed leaderboards and Skee arcade presentation. Runtime and visual acceptance remain separate.");
        }
        public static void InstallSecondSlice()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before explicit Carnival authoring.");
            LWS.TruckVolleyball.Editor.VolleyScorebugAuthoring.Install();
            LWS.TruckHungryHaulers.Editor.HaulerCargoHudAuthoring.Install();
            SportsCarnivalLavaAuthoring.InstallMissingPresentations();
            LWS.TruckKickball.Editor.KickballShoveledSnowAuthoring.Install();
            SportsScrapHeapAuthoring.Upgrade();
            var red = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/TruckRedLightGreenLight/Scenes/TruckRedLightGreenLight.unity");
            LWS.TruckRedLightGreenLight.Editor.RedLightCarnivalAuthoring.InstallOpenScene();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(red);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(red);
            LWS.TruckFishing.Editor.FishingCarnivalWildlifeIntake.ImportMissingOwnedClips();
            LWS.TruckFishing.Editor.FishingCarnivalPresentationAuthoring.InstallMissingPresentation();
            AssetDatabase.SaveAssets();
            Debug.Log("CARNIVAL_SECOND_SLICE_SAVED compact Volleyball/Hauler HUDs, molten surfaces, maintained Kickball bases, preserved Scrap Heap sports, Red Light world presentation and Fishing water/wildlife. Bear remains missing. Runtime and visual acceptance remain separate.");
        }
    }
}

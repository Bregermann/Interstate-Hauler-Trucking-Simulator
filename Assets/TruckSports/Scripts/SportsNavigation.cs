using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using LWS.TruckBilliards;
namespace LWS.TruckSports {
public static class SportsNavigation {
 public const string Snake="TruckSnake",FareFight="TruckFareFight",TrailerPlug="TruckTrailerPlug",SkyTow="TruckSkyTow",DanceOff="TruckDanceOff",Limbo="TruckLimbo",Kickball="TruckKickball",Front="TruckSports",PedestrianPlowing="TruckPedestrianPlowing",PedestrianPlowingCab="TruckPedestrianPlowingFirstPerson",Billiards="TruckBilliards",Hockey="TruckHockey",Bowling="TruckBowling",AirHockey="TruckAirHockey",Wallball="TruckWallball",Cornhole="TruckCornhole",Cheese="TruckCheeseRolling",Dock="TruckDockDiving",Keepaway="TruckKeepaway",Curling="TruckCurling",BaseJump="TruckBaseJumping",KeepawayCab="TruckKeepawayFirstPerson",Terrier="TruckTerrierRacing",Minefield="TruckMinefield",Mattress="TruckMattressRacing",Wife="TruckWifeCarrying",Parking="TruckMusicalParking",Swing="TruckGiantSwing",Buzkashi="TruckBuzkashi",Bog="TruckBogSnorkelling",BeerPong="TruckBeerPong",FieldGoal="TruckFieldGoal",TargetLaunch="TruckTargetLaunch",Sumo="TruckSumo",Fishing="TruckFishing",Tug="TruckTugOfWar",Skee="TruckSkeeBall",LogRolling="TruckLogRolling",Dodgeball="TruckTrampolineDodgeball",Derby="TruckDemolitionDerby",Poker="TruckPoker",Bungee="TruckBungeeDemolition";
 public static bool Collection{get;private set;} public static bool OpenSelection,BilliardsPractice;
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void Reset(){Collection=false;OpenSelection=false;BilliardsPractice=false;SceneManager.sceneLoaded-=Loaded;SceneManager.sceneLoaded+=Loaded;}
 static void Loaded(Scene scene,LoadSceneMode mode){if(Collection&&scene.name==Billiards){var s=Object.FindAnyObjectByType<BilliardsSession>();if(s!=null)s.gameObject.AddComponent<SportsBilliardsEntry>();}}
 static void Load(string scene){
#if UNITY_EDITOR
string folder=scene==Snake?"TruckSnake":scene==FareFight?"TruckFareFight":scene==TrailerPlug?"TruckTrailerPlug":scene==SkyTow?"TruckSkyTow":scene==DanceOff?"TruckDanceOff":scene==Limbo?"TruckLimbo":scene==Kickball?"TruckKickball":scene==PedestrianPlowing||scene==PedestrianPlowingCab?"TruckPedestrianPlowing":scene==Front?"TruckSports":scene==Hockey?"TruckHockey":scene==Bowling?"TruckBowling":scene==AirHockey?"TruckAirHockey":scene==Wallball?"TruckWallball":scene==Cornhole?"TruckCornhole":scene==Cheese?"TruckCheeseRolling":scene==Dock?"TruckDockDiving":scene==Keepaway||scene==KeepawayCab?"TruckKeepaway":scene==BeerPong?"TruckBeerPong":scene==FieldGoal?"TruckFieldGoal":scene==TargetLaunch?"TruckTargetLaunch":scene==Sumo?"TruckSumo":scene==Fishing?"TruckFishing":scene==Tug?"TruckTugOfWar":scene==Skee?"TruckSkeeBall":scene==LogRolling?"TruckLogRolling":scene==Dodgeball?"TruckTrampolineDodgeball":scene==Derby?"TruckDemolitionDerby":scene==Poker?"TruckPoker":scene==Bungee?"TruckBungeeDemolition":scene==Bog?"TruckBogSnorkelling":scene==Buzkashi?"TruckBuzkashi":scene==Swing?"TruckGiantSwing":scene==Parking?"TruckMusicalParking":scene==Wife?"TruckWifeCarrying":scene==Mattress?"TruckMattressRacing":scene==Minefield?"TruckMinefield":scene==Terrier?"TruckTerrierRacing":scene==Curling?"TruckCurling":scene==BaseJump?"TruckBaseJumping":"TruckBilliards";UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode("Assets/"+folder+"/Scenes/"+scene+".unity",new LoadSceneParameters(LoadSceneMode.Single));
#else
SceneManager.LoadScene(scene,LoadSceneMode.Single);
#endif
}
public static void EnterCollection(){Collection=true;}
#if UNITY_EDITOR || DEVELOPMENT_BUILD
 internal static void StandaloneValidation(){Collection=false;}
#endif
 public static void LoadSport(string scene){Time.timeScale=1;Load(scene);}
 public static void FrontEnd(bool selection=true){OpenSelection=selection;Time.timeScale=1;Load(Front);}
}
}

using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using LWS.TruckBilliards;
namespace LWS.TruckSports {
public static class SportsNavigation {
 public const string Front="TruckSports",Billiards="TruckBilliards",Hockey="TruckHockey",Bowling="TruckBowling",AirHockey="TruckAirHockey",Wallball="TruckWallball",Cornhole="TruckCornhole",Cheese="TruckCheeseRolling",Dock="TruckDockDiving",Keepaway="TruckKeepaway",Curling="TruckCurling",BaseJump="TruckBaseJumping",KeepawayCab="TruckKeepawayFirstPerson",Terrier="TruckTerrierRacing",Minefield="TruckMinefield",Mattress="TruckMattressRacing",Wife="TruckWifeCarrying",Parking="TruckMusicalParking",Swing="TruckGiantSwing",Buzkashi="TruckBuzkashi",Bog="TruckBogSnorkelling",Bungee="TruckBungeeDemolition";
 public static bool Collection{get;private set;} public static bool OpenSelection,BilliardsPractice;
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void Reset(){Collection=false;OpenSelection=false;BilliardsPractice=false;SceneManager.sceneLoaded-=Loaded;SceneManager.sceneLoaded+=Loaded;}
 static void Loaded(Scene scene,LoadSceneMode mode){if(Collection&&scene.name==Billiards){var s=Object.FindAnyObjectByType<BilliardsSession>();if(s!=null)s.gameObject.AddComponent<SportsBilliardsEntry>();}}
 static void Load(string scene){
#if UNITY_EDITOR
string folder=scene==Front?"TruckSports":scene==Hockey?"TruckHockey":scene==Bowling?"TruckBowling":scene==AirHockey?"TruckAirHockey":scene==Wallball?"TruckWallball":scene==Cornhole?"TruckCornhole":scene==Cheese?"TruckCheeseRolling":scene==Dock?"TruckDockDiving":scene==Keepaway||scene==KeepawayCab?"TruckKeepaway":scene==Bungee?"TruckBungeeDemolition":scene==Bog?"TruckBogSnorkelling":scene==Buzkashi?"TruckBuzkashi":scene==Swing?"TruckGiantSwing":scene==Parking?"TruckMusicalParking":scene==Wife?"TruckWifeCarrying":scene==Mattress?"TruckMattressRacing":scene==Minefield?"TruckMinefield":scene==Terrier?"TruckTerrierRacing":scene==Curling?"TruckCurling":scene==BaseJump?"TruckBaseJumping":"TruckBilliards";UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode("Assets/"+folder+"/Scenes/"+scene+".unity",new LoadSceneParameters(LoadSceneMode.Single));
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

using System;
using System.Collections;
using UnityEngine;
using LWS.TruckBilliards;
namespace LWS.TruckSports {
// Billiards regression compatibility entry. Six-player collection tests live in SportsHockeySmoke.
public sealed class SportsSmoke:MonoBehaviour {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Launch(){var args=Environment.GetCommandLineArgs();if(Array.IndexOf(args,"-tsBilliardsFeature")<0&&Array.IndexOf(args,"-tsBilliardsRegression")<0)return;var g=new GameObject("Billiards collection validation");DontDestroyOnLoad(g);g.AddComponent<SportsSmoke>();}
 IEnumerator Start(){yield return null;SportsNavigation.LoadSport(SportsNavigation.Billiards);yield return null;yield return null;SportsNavigation.StandaloneValidation();var bill=FindAnyObjectByType<BilliardsMatch>();bill.session.Show(BilliardsScreen.Main);bill.gameObject.AddComponent<BilliardsSmoke>();Destroy(gameObject);}
#endif
}
}
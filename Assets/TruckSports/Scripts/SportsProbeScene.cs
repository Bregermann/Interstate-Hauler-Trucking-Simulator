#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;using System.Collections;using UnityEngine;
namespace LWS.TruckSports {
// Explicit development probes enter their saved sport from the combined build's normal front end.
public static class SportsProbeScene {
 public static IEnumerator Ensure<T>(string scene) where T:Component {if(UnityEngine.Object.FindAnyObjectByType<T>()!=null)yield break;SportsNavigation.LoadSport(scene);yield return null;yield return null;if(UnityEngine.Object.FindAnyObjectByType<T>()==null)throw new InvalidOperationException("Development probe could not load saved scene "+scene);}
}
}
#endif

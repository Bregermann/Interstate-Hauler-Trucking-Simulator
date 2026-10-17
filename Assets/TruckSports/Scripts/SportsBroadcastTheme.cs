using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
namespace LWS.TruckSports {
// A saved theme plus a regeneration safety net for legacy sport-specific menu authors.
public sealed class SportsBroadcastTheme:ScriptableObject {
 public TMP_FontAsset headingFont,bodyFont;public Sprite controllerGlyph,leftStickGlyph,rightStickGlyph;
 static SportsBroadcastTheme current;
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void ResetScope(){SceneManager.sceneLoaded-=Loaded;current=null;}
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]static void Install(){current=Resources.Load<SportsBroadcastTheme>("SportsBroadcastTheme");if(current!=null)SceneManager.sceneLoaded+=Loaded;}
 static void Loaded(Scene scene,LoadSceneMode mode){if(current==null)return;foreach(var root in scene.GetRootGameObjects())foreach(var canvas in root.GetComponentsInChildren<Canvas>(true)){bool sports=false;foreach(var component in canvas.GetComponents<MonoBehaviour>())if(component!=null&&component.GetType().Namespace!=null&&component.GetType().Namespace.StartsWith("LWS.Truck")){sports=true;break;}if(!sports)continue;var theme=canvas.GetComponent<SportsBroadcastCanvas>();if(theme==null)theme=canvas.gameObject.AddComponent<SportsBroadcastCanvas>();theme.headingFont=current.headingFont;theme.bodyFont=current.bodyFont;theme.eventMenus=canvas.GetComponent<SportsEventMenus>();theme.Apply();}}
}
}

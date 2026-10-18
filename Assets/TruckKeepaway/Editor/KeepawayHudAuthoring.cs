using System;
using CompassNavigatorPro;
using LWS.SportsPresentation;
using LWS.TruckSports;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
namespace LWS.TruckKeepaway.Editor {
public static class KeepawayHudAuthoring {
 const string CompassPath="Assets/TruckKeepaway/Prefabs/Keepaway ball compass.prefab";
 public static void UpgradeBoth(){
  if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play Mode before authoring Keepaway.");
  for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Save the open scene before the additive Keepaway HUD upgrade.");
  var previous=EditorSceneManager.GetSceneManagerSetup();
  try{foreach(var path in new[]{KeepawayAuthoring.Scene,KeepawayChaosAuthoring.CabScene}){
   var scene=EditorSceneManager.OpenScene(path);Install(UnityEngine.Object.FindAnyObjectByType<KeepawayMatch>());EditorSceneManager.SaveScene(scene);
  }AssetDatabase.SaveAssets();}finally{if(previous.Length>0&&Array.Exists(previous,x=>!string.IsNullOrEmpty(x.path)&&x.isLoaded))EditorSceneManager.RestoreSceneManagerSetup(previous);else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);}
 }
 static GameObject CompassPrefab(){
  var existing=AssetDatabase.LoadAssetAtPath<GameObject>(CompassPath);if(existing!=null)return existing;
  var original=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Plugins/Kronnect/CompassNavigatorPro/Resources/CNPro/Prefabs/CompassNavigatorPro.prefab");
  if(original==null)throw new InvalidOperationException("Compass Navigator Pro prefab is not installed.");
  var copy=UnityEngine.Object.Instantiate(original);copy.name="Keepaway ball compass";copy.SetActive(false);
  try{
   var old=copy.GetComponent<CompassPro>();var adapter=copy.AddComponent<KeepawayBallCompass>();
   EditorUtility.CopySerializedManagedFieldsOnly(old,adapter);UnityEngine.Object.DestroyImmediate(old);
   adapter.dontDestroyOnLoad=false;
   // Configure the serialized public settings before first OnEnable. Several CNP
   // property setters redraw an already-initialized compass and cannot be used yet.
   var config=new SerializedObject(adapter);
   foreach(var name in new[]{"_showCompassBar","_showMiniMap","_showOnScreenIndicators","_offScreenIndicatorShowDistance"})config.FindProperty(name).boolValue=false;
   config.FindProperty("_showOffScreenIndicators").boolValue=true;
   config.FindProperty("_offScreenIndicatorMargin").floatValue=0;
   config.FindProperty("_offScreenIndicatorAlpha").floatValue=1;
   config.FindProperty("_offScreenIndicatorScale").floatValue=1;
   config.FindProperty("_use3Ddistance").boolValue=true;
   config.FindProperty("_onScreenIndicatorFarDistance").floatValue=10000;
   config.FindProperty("_compassBarOrientation").intValue=(int)CompassBarOrientation.Follow;
   config.ApplyModifiedPropertiesWithoutUndo();
   foreach(var scaler in copy.GetComponentsInChildren<CanvasScaler>(true)){scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;}
   foreach(var canvas in copy.GetComponentsInChildren<Canvas>(true)){canvas.overrideSorting=true;canvas.sortingOrder=35;}
   foreach(var graphic in copy.GetComponentsInChildren<Graphic>(true))graphic.raycastTarget=false;
   return PrefabUtility.SaveAsPrefabAsset(copy,CompassPath);
  }finally{UnityEngine.Object.DestroyImmediate(copy);}
 }
 public static void Install(KeepawayMatch match){
  if(match==null)throw new InvalidOperationException("No Keepaway authority in the scene.");
  var fit=match.broadcast.GetComponent<SportsCameraFit>();fit.arenaBounds=new Bounds(new Vector3(0,1,0),new Vector3(match.arenaX*2+2,3,match.arenaZ*2+2));fit.topInset=.012f;fit.bottomInset=.10f;fit.sideInset=.012f;fit.Apply();
  match.menus.header.transform.parent.gameObject.SetActive(false);match.menus.status.gameObject.SetActive(false);
  var backdrop=match.menus.status.transform.parent.Find("Broadcast status backplate");if(backdrop!=null)UnityEngine.Object.DestroyImmediate(backdrop.gameObject);
  var guidance=match.GetComponent<KeepawayBallGuidance>();
  if(guidance==null){
   var prefab=CompassPrefab();guidance=match.gameObject.AddComponent<KeepawayBallGuidance>();guidance.match=match;guidance.views=new KeepawayBallCompass[4];
   // All vendor instances exist before registering the single shared read-only POI.
   for(int i=0;i<4;i++){var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab);go.name="Keepaway human ball compass "+(i+1);go.transform.SetParent(match.transform,false);go.SetActive(false);var view=go.GetComponent<KeepawayBallCompass>();var settings=new SerializedObject(view);settings.FindProperty("_compassGroup").intValue=i+1;settings.FindProperty("_cameraMain").objectReferenceValue=match.cabViews[i].view;settings.FindProperty("_follow").objectReferenceValue=match.cabViews[i].view.transform;settings.ApplyModifiedPropertiesWithoutUndo();guidance.views[i]=view;}
   var proxy=new GameObject("Keepaway ball POI · read-only");proxy.transform.SetParent(match.transform,false);
   var poi=proxy.AddComponent<CompassProPOI>();poi.canBeVisited=false;poi.hideWhenVisited=false;poi.dontDestroyOnLoad=false;poi.showOnScreenIndicator=false;poi.showOffScreenIndicator=true;poi.ignoreAreaOfInterest=true;poi.playAudioClipWhenVisited=false;poi.title="BALL";poi.radius=0;poi.positionOffset=Vector3.zero;poi.iconNonVisited=AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");poi.tintColor=new Color(1,.82f,.12f);guidance.ballPOI=poi;
   foreach(var view in guidance.views)view.ballPOI=poi;
  }
  foreach(var t in match.menus.pages[(int)SportsEventScreen.Help].GetComponentsInChildren<TMPro.TMP_Text>(true))if(t.name=="Rules and controls")t.text=match.Help;
  EditorUtility.SetDirty(match);EditorUtility.SetDirty(guidance);
  Debug.Log("KEEP_HUD_SAVED "+match.gameObject.scene.path+" bottom typed cards, compact framing, four CNP groups");
 }
}
}

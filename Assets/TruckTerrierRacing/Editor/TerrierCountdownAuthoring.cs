using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using LWS.TruckSports.Editor;
namespace LWS.TruckTerrierRacing.Editor {
public static class TerrierCountdownAuthoring {
 [MenuItem("Truck Sports/Upgrade shared race starting lights")]
 public static void UpgradeRaces(){
  if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new System.InvalidOperationException("Preserve current scene edits before applying the targeted race display upgrade.");
  foreach(string scene in new[]{LWS.TruckMattressRacing.Editor.MattressAuthoring.Scene,LWS.TruckWifeCarrying.Editor.WifeAuthoring.Scene,LWS.TruckBogSnorkelling.Editor.BogAuthoring.Scene,LWS.TruckSkyTow.Editor.SkyTowAuthoring.Scene}){
   var opened=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scene);var race=UnityEngine.Object.FindAnyObjectByType<LWS.TruckSports.SportsEventSession>();
   if(race.GetComponent<TerrierCountdown>()==null)Install(race);
   UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(opened);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(opened);
  }
  AssetDatabase.SaveAssets();
 }
 public static void Install(TerrierMatch m)=>Install((LWS.TruckSports.SportsEventSession)m);
 public static void Install(LWS.TruckSports.SportsEventSession m){
 if(!m.SupportsRaceCountdown)throw new System.InvalidOperationException("Starting lights are only authored for a registered race.");
 bool terrier=m is TerrierMatch;string name=terrier?"Terrier starting lights":"Shared race starting lights";
 var old=m.menus.hud.transform.Find(name);if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);
 var root=SportsUIAuthoring.Rect(name,m.menus.hud.transform,Vector2.zero,Vector2.zero);var stretch=root.GetComponent<RectTransform>();stretch.anchorMin=Vector2.zero;stretch.anchorMax=Vector2.one;stretch.offsetMin=stretch.offsetMax=Vector2.zero;
 var c=m.GetComponent<TerrierCountdown>();if(c==null)c=m.gameObject.AddComponent<TerrierCountdown>();
 c.session=m;c.match=m as TerrierMatch;if(c.match!=null)c.match.countdown=c;
 c.panels=new RectTransform[4];c.badges=new RectTransform[4];c.labels=new TMP_Text[4];c.rings=new Image[4];c.fades=new CanvasGroup[4];if(c.beatAudio==null)c.beatAudio=m.gameObject.AddComponent<AudioSource>();c.beatAudio.playOnAwake=false;c.beatAudio.spatialBlend=0;
 for(int i=0;i<4;i++){var panel=SportsUIAuthoring.Rect("Countdown view "+i,root.transform,Vector2.zero,Vector2.zero);c.panels[i]=panel.GetComponent<RectTransform>();var badge=SportsUIAuthoring.Rect("Starting medallion",panel.transform,new Vector2(176,176),new Vector2(0,35));c.badges[i]=badge.GetComponent<RectTransform>();c.fades[i]=badge.AddComponent<CanvasGroup>();c.fades[i].blocksRaycasts=false;var shadow=Image("Halo",badge.transform,226,"Shadows/Radial/Radial Inner Filled Shadow.png");shadow.color=new Color(0,0,0,.75f);var plate=Image("Smoked plate",badge.transform,166,"Borders/Radial/256px/Radial Filled 256px.png");plate.color=new Color(.01f,.025f,.035f,.9f);var ring=Image("Tension sweep",badge.transform,176,"Borders/Radial/256px/Radial Outline 256px - 2x.png");ring.type=UnityEngine.UI.Image.Type.Filled;ring.fillMethod=UnityEngine.UI.Image.FillMethod.Radial360;ring.fillOrigin=2;c.rings[i]=ring;c.labels[i]=SportsUIAuthoring.Text("Starting call",badge.transform,"3",new Vector2(145,115),new Vector2(-2,3),88);c.labels[i].font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Heat - Complete Modern UI/Fonts/RobotoCondensed-BoldItalic SDF.asset");c.labels[i].enableAutoSizing=false;c.labels[i].fontStyle=FontStyles.Bold;panel.SetActive(false);}EditorUtility.SetDirty(c);EditorUtility.SetDirty(m);}
 static Image Image(string name,Transform parent,float size,string relative){var image=SportsUIAuthoring.Rect(name,parent,new Vector2(size,size),Vector2.zero).AddComponent<Image>();image.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Heat - Complete Modern UI/Textures/"+relative);image.raycastTarget=false;return image;}
}
}

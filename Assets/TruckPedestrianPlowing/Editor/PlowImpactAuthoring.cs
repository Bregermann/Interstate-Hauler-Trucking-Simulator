using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using LWS.TruckSports.Editor;
namespace LWS.TruckPedestrianPlowing.Editor {
 public static class PlowImpactAuthoring {
  [MenuItem("Truck Sports/Pedestrian Plowing/Update Impact Options (preserve layout)")]
  public static void Install(){foreach(var path in new[]{PlowAuthoring.Scene,PlowCabAuthoring.Scene}){var scene=EditorSceneManager.OpenScene(path);InstallCurrent(Object.FindAnyObjectByType<PlowMatch>());EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);}AssetDatabase.SaveAssets();}
  public static void InstallCurrent(PlowMatch match){
   var options=match.GetComponent<PlowImpactPresentation>();if(options==null)options=match.gameObject.AddComponent<PlowImpactPresentation>();options.match=match;match.impactPresentation=options;
   var menu=match.menus;var page=menu.pages[(int)LWS.TruckSports.SportsEventScreen.Settings].transform;
   MoveSlider(menu.master,"MASTER VOLUME",180);MoveSlider(menu.sfx,"SFX VOLUME",80);MoveSlider(menu.music,"MUSIC VOLUME",-20);MoveSlider(menu.steering,"CONTROLLER STEERING",-120);
   MoveButton(menu.displayText,180);MoveButton(menu.resolutionText,80);MoveButton(menu.vibrationText,-20);MoveButton(menu.lookSensitivityText,-120);MoveButton(menu.lookInvertText,-220);
   var old=page.Find("BLOOD AMOUNT Slider");options.slider=old!=null?old.GetComponent<UnityEngine.UI.Slider>():SportsUIAuthoring.Slider(page,"BLOOD AMOUNT",new Vector2(-330,-220),0,100,match.crowdSettings.bloodAmount*100);
   var info=page.Find("Info");if(info!=null){var text=info.GetComponent<TMPro.TMP_Text>();text.text="Blood Amount: small stylized impact splashes · 0 turns them off";text.rectTransform.anchoredPosition=new Vector2(0,-285);text.rectTransform.sizeDelta=new Vector2(1250,40);}
   if(options.splashes==null||options.splashes.Length==0){
    string path=PlowAuthoring.Root+"/Materials/Stylized impact splash.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
    if(material==null){material=new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));material.SetFloat("_Surface",1);material.SetFloat("_Blend",0);material.SetFloat("_SrcBlend",5);material.SetFloat("_DstBlend",10);material.SetFloat("_ZWrite",0);material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");material.renderQueue=3000;material.SetTexture("_BaseMap",AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd").texture);AssetDatabase.CreateAsset(material,path);}
    options.splashes=new ParticleSystem[8];
    for(int i=0;i<8;i++){
     var g=new GameObject("Pooled stylized impact splash "+i);g.transform.SetParent(match.transform,false);var ps=g.AddComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
     var main=ps.main;main.playOnAwake=false;main.loop=false;main.simulationSpace=ParticleSystemSimulationSpace.World;main.maxParticles=24;main.startLifetime=new ParticleSystem.MinMaxCurve(.22f,.42f);main.startSize=new ParticleSystem.MinMaxCurve(.025f,.06f);main.startSpeed=new ParticleSystem.MinMaxCurve(.9f,2);main.gravityModifier=.9f;main.startColor=new ParticleSystem.MinMaxGradient(new Color(.4f,.03f,.04f,.8f),new Color(.65f,.05f,.05f,.8f));
     var emission=ps.emission;emission.enabled=false;var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Hemisphere;shape.radius=.08f;shape.rotation=new Vector3(-90,0,0);
     var renderer=g.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;options.splashes[i]=ps;
    }
   }
   EditorUtility.SetDirty(options);EditorUtility.SetDirty(match);EditorUtility.SetDirty(match.crowdSettings);
  }
  static void MoveSlider(UnityEngine.UI.Slider slider,string label,float y){var r=slider.GetComponent<RectTransform>();r.anchoredPosition=new Vector2(-330,y);var text=slider.transform.parent.Find(label);if(text!=null)text.GetComponent<RectTransform>().anchoredPosition=new Vector2(-330,y+48);}
  static void MoveButton(TMPro.TMP_Text text,float y){if(text==null)return;var button=text.GetComponentInParent<Michsky.UI.Heat.ButtonManager>(true);button.GetComponent<RectTransform>().anchoredPosition=new Vector2(350,y);}
 }
}

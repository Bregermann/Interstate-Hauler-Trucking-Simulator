using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
namespace LWS.TruckSports.Editor {
// Explicit additive layout upgrade; never captures old screenshots or rebuilds
// scenes. Existing buttons, callbacks, crew and controller ownership survive.
public static class SportsRetroMenuAuthoring {
 public const string LogoFolder="Assets/TruckSports/Art/Logos/";
 [MenuItem("Truck Sports/Menu/Upgrade retro logo showcase")]
 public static void Upgrade(){EditorSceneManager.OpenScene(SportsBuilder.FrontScene);Install(UnityEngine.Object.FindAnyObjectByType<SportsFrontEnd>());EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());AssetDatabase.SaveAssets();}
 public static void Install(SportsFrontEnd front){
  if(front==null||front.catalogue==null)throw new InvalidOperationException("Saved catalogue required");
  var information=System.IO.File.Exists(SportsSportLogoAuthoring.InfoPath)?JsonUtility.FromJson<SportsSportLogoAuthoring.Catalogue>(System.IO.File.ReadAllText(SportsSportLogoAuthoring.InfoPath)).entries:null;
  var page=front.pages[1].transform;var existing=page.Find("Selected sport showcase");
  var root=existing!=null?existing.gameObject:SportsUIAuthoring.Rect("Selected sport showcase",page,new Vector2(620,610),new Vector2(390,0));
  var bg=Ensure<Image>(root);bg.color=SportsBroadcastCanvas.Ink;bg.raycastTarget=false;
  var plate=Ensure<SportsBroadcastPlate>(root);plate.cut=24;plate.accent=true;
  var hero=Ensure<SportsCatalogueHero>(root);
  var content=Child(root.transform,"Artwork and details",new Vector2(620,610),Vector2.zero);
  hero.content=Ensure<CanvasGroup>(content.gameObject);hero.content.blocksRaycasts=false;
  hero.logo=Picture(content,"Original sport logo",new Vector2(570,220),new Vector2(0,175));
  hero.preview=Picture(content,"Current gameplay",new Vector2(570,245),new Vector2(0,-105));
  hero.title=Label(content,"Accessible sport title",new Vector2(560,24),new Vector2(0,44),20);
  hero.description=Label(content,"Sport description",new Vector2(560,65),new Vector2(0,-269),20);
  for(int i=0;i<front.catalogue.cards.Length;i++){
   var card=front.catalogue.cards[i];var rect=card.GetComponent<RectTransform>();Place(rect,new Vector2(324,138),new Vector2(-525+(i%2)*337,222-((i%8)/2)*151));
   foreach(Transform child in card.transform)if(child.GetComponent<SportsMenuAction>()==null)child.gameObject.SetActive(false);
   var art=Picture(card.transform,"Original sport logo",new Vector2(302,94),new Vector2(0,18));art.gameObject.SetActive(true);
   var actions=card.GetComponentsInChildren<SportsMenuAction>(true);var description=card.transform.Find("Description")?.GetComponent<TMP_Text>()?.text??"";
   for(int n=0;n<actions.Length;n++){
    var action=actions[n];string scene=SportsMenuArtAuthoring.SceneFor(action.action);var logo=LoadLogo(scene);if(n==0)art.sprite=logo;
    float width=(304-(actions.Length-1)*6f)/actions.Length;Place(action.GetComponent<RectTransform>(),new Vector2(width,32),new Vector2(-152+width*.5f+n*(width+6),-48));action.transform.SetAsLastSibling();
    var choice=Ensure<SportsCatalogueChoice>(action.gameObject);choice.hero=hero;choice.scene=scene;choice.logo=logo;choice.preview=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/TruckSports/Art/Previews/"+scene+".png");choice.title=card.transform.Find("Sport")?.GetComponent<TMP_Text>()?.text??scene;choice.description=description;
    var info=information?.FirstOrDefault(entry=>entry.scene==scene);if(info!=null){choice.title=info.title;choice.description=description+"\n"+info.players;}
    if(choice.preview==null)throw new InvalidOperationException("Current gameplay preview missing for "+scene);EditorUtility.SetDirty(choice);
   }
   EditorUtility.SetDirty(card);
  }
  Place(front.catalogue.indicator.rectTransform,new Vector2(260,40),new Vector2(-355,-337));
  Place(front.catalogue.previous.GetComponent<RectTransform>(),new Vector2(176,42),new Vector2(-590,-337));Place(front.catalogue.next.GetComponent<RectTransform>(),new Vector2(176,42),new Vector2(-118,-337));
  var subtitle=page.Find("Subtitle")?.GetComponent<TMP_Text>();if(subtitle!=null){subtitle.text="CHOOSE YOUR NEXT BAD IDEA · LOCAL PLAY · YOUR CREW, YOUR RULES";Place(subtitle.rectTransform,new Vector2(1400,32),new Vector2(0,330));}
  if(!page.GetComponentsInChildren<SportsMenuAction>(true).Any(a=>a.action=="participants"))SportsUIAuthoring.Button(page,"YOUR CREW","participants",new Vector2(-555,-417),new Vector2(280,43));
  var scrap=page.GetComponentsInChildren<SportsMenuAction>(true).FirstOrDefault(a=>a.action=="scrapheap");if(scrap!=null)Place(scrap.GetComponent<RectTransform>(),new Vector2(280,43),new Vector2(555,-417));
  var wall=front.pages[4].transform.Find("WALLBALL Sport Card");if(wall!=null)InstallLegacyCard(wall,hero);
  hero.Select(front.catalogue.cards[0].GetComponentInChildren<SportsCatalogueChoice>(true),true);EditorUtility.SetDirty(hero);EditorUtility.SetDirty(front);
 }
 static void InstallLegacyCard(Transform card,SportsCatalogueHero hero){var image=Picture(card,"Original sport logo",new Vector2(300,100),new Vector2(0,90));image.sprite=LoadLogo("TruckWallball");var title=card.Find("Sport");if(title!=null)title.gameObject.SetActive(false);foreach(var action in card.GetComponentsInChildren<SportsMenuAction>(true)){var choice=Ensure<SportsCatalogueChoice>(action.gameObject);choice.hero=hero;choice.scene="TruckWallball";choice.title="TRUCK WALLBALL";choice.description="Experimental · Manual play";choice.logo=image.sprite;choice.preview=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/TruckSports/Art/Previews/TruckWallball.png");}}
 public static Sprite LoadLogo(string scene){string path=LogoFolder+scene+".png";var importer=AssetImporter.GetAtPath(path) as TextureImporter;if(importer==null)throw new InvalidOperationException("Original logo artwork missing: "+path);if(importer.textureType!=TextureImporterType.Sprite||importer.maxTextureSize!=1024||importer.mipmapEnabled){importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.maxTextureSize=1024;importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Bilinear;importer.textureCompression=TextureImporterCompression.CompressedHQ;importer.SaveAndReimport();}return AssetDatabase.LoadAssetAtPath<Sprite>(path);}
 static T Ensure<T>(GameObject gameObject)where T:Component{var component=gameObject.GetComponent<T>();return component!=null?component:gameObject.AddComponent<T>();}
 static RectTransform Child(Transform parent,string name,Vector2 size,Vector2 position){var found=parent.Find(name) as RectTransform;var rect=found!=null?found:SportsUIAuthoring.Rect(name,parent,size,position).GetComponent<RectTransform>();Place(rect,size,position);return rect;}
 static Image Picture(Transform parent,string name,Vector2 size,Vector2 position){var rect=Child(parent,name,size,position);var image=Ensure<Image>(rect.gameObject);image.color=Color.white;image.preserveAspect=true;image.raycastTarget=false;return image;}
 static TMP_Text Label(Transform parent,string name,Vector2 size,Vector2 position,int font){var found=parent.Find(name)?.GetComponent<TMP_Text>();var text=found!=null?found:SportsUIAuthoring.Text(name,parent,"",size,position,font);Place(text.rectTransform,size,position);text.fontSizeMax=font;text.fontSizeMin=font-3;text.enableAutoSizing=true;text.alignment=TextAlignmentOptions.Center;text.raycastTarget=false;return text;}
 static void Place(RectTransform rect,Vector2 size,Vector2 position){rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);rect.sizeDelta=size;rect.anchoredPosition=position;}
}
}

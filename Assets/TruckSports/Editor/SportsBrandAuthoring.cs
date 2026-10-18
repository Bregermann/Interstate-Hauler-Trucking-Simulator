using UnityEditor;
using UnityEngine;

namespace LWS.TruckSports.Editor {
public static class SportsBrandAuthoring {
 public const string WordmarkPath="Assets/TruckSports/Art/Brand/Truck Sports Party wordmark.png";
 public static Sprite Wordmark(){
  var importer=AssetImporter.GetAtPath(WordmarkPath) as TextureImporter;
  if(importer==null)throw new System.InvalidOperationException("Original Truck Sports: Party! wordmark is missing.");
  if(importer.textureType!=TextureImporterType.Sprite||!importer.alphaIsTransparency||importer.textureCompression!=TextureImporterCompression.Uncompressed){importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.wrapMode=TextureWrapMode.Clamp;importer.maxTextureSize=2048;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();}
  return AssetDatabase.LoadAssetAtPath<Sprite>(WordmarkPath);
 }
 // Targeted saved-page upgrade; preserves buttons, their actions, participants and navigation.
 public static void InstallTitle(SportsFrontEnd front){
  var page=front.pages[0].transform;var title=page.Find("Title");if(title!=null)title.gameObject.SetActive(false);
  var existing=page.Find("Truck Sports Party original wordmark");
  var root=existing!=null?existing.gameObject:SportsUIAuthoring.Rect("Truck Sports Party original wordmark",page,new Vector2(760,507),new Vector2(-325,40));
  var image=root.GetComponent<UnityEngine.UI.Image>()??root.AddComponent<UnityEngine.UI.Image>();image.sprite=Wordmark();image.preserveAspect=true;image.raycastTarget=false;image.color=Color.white;
  var rect=root.GetComponent<RectTransform>();rect.anchoredPosition=new Vector2(-325,40);rect.sizeDelta=new Vector2(760,507);
  var subtitle=page.Find("Subtitle").GetComponent<RectTransform>();subtitle.anchoredPosition=new Vector2(0,355);
  string[] keys={"play","help","settings","quit"};for(int i=0;i<keys.Length;i++){var button=page.Find(keys[i]+" Button").GetComponent<RectTransform>();button.anchoredPosition=new Vector2(430,150-i*100);button.sizeDelta=new Vector2(420,64);}
  EditorUtility.SetDirty(image);EditorUtility.SetDirty(front);
 }
}
}

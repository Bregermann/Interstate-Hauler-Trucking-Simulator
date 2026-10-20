using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
namespace LWS.TruckSports.Editor {
public static class SportsSportLogoAuthoring {
 [Serializable]public sealed class Information{public string scene,title,players;}
 [Serializable]public sealed class Catalogue{public Information[] entries;}
 public const string InfoPath="Assets/TruckSports/Art/Logos/Catalogue.json";
 public static void UpgradeAll(){
  if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play Mode before the explicit branding upgrade.");
  int scenes=0,headers=0,signs=0;var information=new System.Collections.Generic.List<Information>();
  foreach(string path in SportsBuilder.SceneList().Where(p=>p!=SportsBuilder.FrontScene)){
   var scene=EditorSceneManager.OpenScene(path);string name=Path.GetFileNameWithoutExtension(path);var logo=SportsRetroMenuAuthoring.LoadLogo(name);var authority=UnityEngine.Object.FindAnyObjectByType<SportsEventSession>();string sportTitle=authority!=null?authority.Title:SceneTitle(name);string players=authority!=null?(authority.MinimumPlayers==authority.MaximumPlayers?authority.MaximumPlayers.ToString():authority.MinimumPlayers+"–"+authority.MaximumPlayers)+" COMPETITORS · LOCAL / AI":LegacyPlayers(name);information.Add(new Information{scene=name,title=sportTitle,players=players});
   foreach(var menu in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include)){
    if(menu==null||menu.GetType().Namespace==null||!menu.GetType().Namespace.StartsWith("LWS.Truck"))continue;
    var field=menu.GetType().GetField("pages",BindingFlags.Public|BindingFlags.Instance);if(field==null||field.FieldType!=typeof(GameObject[]))continue;
    foreach(var page in (GameObject[])field.GetValue(menu)){
     if(page==null||!(page.name=="Setup"||page.name=="Main"||page.name=="Help"||page.name=="Setup Screen"||page.name=="Main Screen"||page.name=="Help Screen"))continue;
     var title=page.transform.Find("Title")?.GetComponent<TMP_Text>();if(title==null)continue;
     var existing=page.transform.Find("Original sport title artwork");var art=existing!=null?existing.gameObject:SportsUIAuthoring.Rect("Original sport title artwork",page.transform,new Vector2(250,125),new Vector2(-545,372));
     var image=art.GetComponent<Image>()??art.AddComponent<Image>();image.sprite=logo;image.preserveAspect=true;image.raycastTarget=false;image.color=Color.white;
     title.rectTransform.anchoredPosition=new Vector2(140,title.rectTransform.anchoredPosition.y);title.rectTransform.sizeDelta=new Vector2(1020,title.rectTransform.sizeDelta.y);headers++;
    }
   }
   // Replace only existing sport-title signage, with depth-tested transparent
   // artwork. Never invent a billboard in a course or change its colliders.
   foreach(var label in UnityEngine.Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include).Where(t=>t.gameObject.activeSelf&&t.text.Trim().Replace(":","").ToUpperInvariant()==sportTitle.ToUpperInvariant()&&(t.GetComponentInParent<Canvas>()==null||t.GetComponentInParent<Canvas>().renderMode==RenderMode.WorldSpace))) {
    if(label.transform.parent!=null&&label.transform.parent.Find("Original sport venue artwork")!=null)continue;
    var worldCanvas=label.GetComponentInParent<Canvas>();if(worldCanvas!=null){var rect=label.rectTransform;float h=Mathf.Min(rect.rect.height,rect.rect.width*logo.rect.height/logo.rect.width);var image=SportsUIAuthoring.Rect("Original sport venue artwork",label.transform.parent,new Vector2(h*logo.rect.width/logo.rect.height,h),rect.anchoredPosition).AddComponent<Image>();image.sprite=logo;image.preserveAspect=true;image.raycastTarget=false;image.transform.localRotation=rect.localRotation;label.gameObject.SetActive(false);signs++;continue;}
    var art=GameObject.CreatePrimitive(PrimitiveType.Quad);art.name="Original sport venue artwork";UnityEngine.Object.DestroyImmediate(art.GetComponent<Collider>());art.transform.SetParent(label.transform.parent,false);art.transform.SetPositionAndRotation(label.transform.position+label.transform.forward*.02f,label.transform.rotation);
    float height=Mathf.Clamp(label.rectTransform.rect.height,2,8);art.transform.localScale=new Vector3(height*logo.rect.width/logo.rect.height,height,1);
    string materialPath="Assets/TruckSports/Art/Logos/"+name+" venue.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);if(material==null){material=new Material(Shader.Find("Universal Render Pipeline/Unlit"));material.SetFloat("_Surface",1);material.SetFloat("_SrcBlend",(float)UnityEngine.Rendering.BlendMode.SrcAlpha);material.SetFloat("_DstBlend",(float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);material.SetFloat("_ZWrite",0);material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");material.renderQueue=3000;AssetDatabase.CreateAsset(material,materialPath);}material.mainTexture=logo.texture;art.GetComponent<MeshRenderer>().sharedMaterial=material;label.gameObject.SetActive(false);signs++;
   }
   EditorSceneManager.SaveScene(scene);scenes++;
  }
  File.WriteAllText(InfoPath,JsonUtility.ToJson(new Catalogue{entries=information.ToArray()},true));AssetDatabase.ImportAsset(InfoPath);SportsRetroMenuAuthoring.Upgrade();AssetDatabase.SaveAssets();Directory.CreateDirectory("Logs/TruckSports/PresentationSprint");File.WriteAllText("Logs/TruckSports/PresentationSprint/branding-integration.txt","SCENES="+scenes+" HEADERS="+headers+" EXISTING_VENUE_SIGNS="+signs+". Explicit additive artwork only; saved physics and game rules preserved.");
 }
 // Legacy modes have different roster authorities. These captions follow
 // their actual saved setup/rules, rather than borrowing Hockey's capacity.
 public static string LegacyPlayers(string scene){switch(scene){case "TruckBilliards":return "SOLO PRACTICE · 2-PLAYER MATCHES · AI";case "TruckBowling":return "1–4 SIMULTANEOUS · UP TO 8 HOT SEAT";case "TruckHockey":return "2–6 COMPETITORS · LOCAL / AI";case "TruckAirHockey":return "2 COMPETITORS · LOCAL / AI";case "TruckWallball":return "2–8 COMPETITORS · LOCAL / AI";case "TruckCornhole":case "TruckCheeseRolling":return "1–4 COMPETITORS · LOCAL / AI";default:throw new InvalidOperationException("Inspect the legacy participant authority before adding its menu caption: "+scene);}}
 public static void RefreshInformation(){var data=JsonUtility.FromJson<Catalogue>(File.ReadAllText(InfoPath));foreach(var entry in data.entries)if(new[]{"TruckBilliards","TruckBowling","TruckHockey","TruckAirHockey","TruckWallball","TruckCornhole","TruckCheeseRolling"}.Contains(entry.scene))entry.players=LegacyPlayers(entry.scene);File.WriteAllText(InfoPath,JsonUtility.ToJson(data,true));AssetDatabase.ImportAsset(InfoPath);SportsRetroMenuAuthoring.Upgrade();}
 static string SceneTitle(string name){var text=System.Text.RegularExpressions.Regex.Replace(name,"(?<=[a-z])(?=[A-Z])"," ").ToUpperInvariant();if(name=="TruckDockDiving")return "TRUCK DOCK DIVING";if(name=="TruckPoker")return "TRUCK POKER";return text;}
}
}

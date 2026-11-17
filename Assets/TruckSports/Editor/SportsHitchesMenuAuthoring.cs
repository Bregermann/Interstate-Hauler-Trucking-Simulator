using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LWS.TruckSports.Editor
{
    public static partial class SportsNewEntriesAuthoring
    {
        [MenuItem("Truck Sports/Alpha 0.10/Install expansion artwork and menu entries")]
        public static void InstallHitchesEntries()
        {
            if (EditorApplication.isPlaying || EditorApplication.isCompiling) throw new InvalidOperationException("Stopped compiled Editor required.");
            foreach (var entry in SportsHitchesGames.All)
            {
                if (!File.Exists(entry.ScenePath)) throw new FileNotFoundException("Saved expansion scene required",entry.ScenePath);
                foreach (string section in new[] { "Logos","Previews" })
                {
                    string path = "Assets/TruckSports/Art/" + section + "/" + entry.scene + ".png";
                    if (!File.Exists(path)) throw new FileNotFoundException("Original logo and actual gameplay image required",path);
                    AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
                    var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                    importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
                    importer.alphaIsTransparency = section == "Logos"; importer.mipmapEnabled = false; importer.maxTextureSize = 1024;
                    importer.wrapMode = TextureWrapMode.Clamp; importer.filterMode = FilterMode.Bilinear; importer.textureCompression = TextureImporterCompression.CompressedHQ; importer.SaveAndReimport();
                }
            }
            var information = JsonUtility.FromJson<SportsSportLogoAuthoring.Catalogue>(File.ReadAllText(SportsSportLogoAuthoring.InfoPath));
            var items = information.entries.ToList();
            foreach (var entry in SportsHitchesGames.All)
            {
                var scene = EditorSceneManager.OpenScene(entry.ScenePath);
                var authority = UnityEngine.Object.FindAnyObjectByType<SportsEventSession>();
                if (authority == null) throw new InvalidOperationException("Missing sport authority: " + entry.scene);
                if (!items.Any(item=>item.scene==entry.scene)) items.Add(new SportsSportLogoAuthoring.Information { scene=entry.scene,title=authority.Title,
                    players=(authority.MinimumPlayers==authority.MaximumPlayers?authority.MaximumPlayers.ToString():authority.MinimumPlayers+"–"+authority.MaximumPlayers)+" PARTICIPANTS · UP TO 4 LOCAL HUMANS · AI" });
                var logo = SportsRetroMenuAuthoring.LoadLogo(entry.scene);
                foreach (var page in authority.menus.pages)
                {
                    if (page == null || !(page.name == "Setup" || page.name == "Help")) continue;
                    foreach(var rules in page.GetComponentsInChildren<TMPro.TMP_Text>(true))
                        if(rules.name=="Rules and controls"){rules.text=authority.Help;EditorUtility.SetDirty(rules);}
                    var existing = page.transform.Find("Original sport title artwork");
                    var art = existing != null ? existing.gameObject : SportsUIAuthoring.Rect("Original sport title artwork",page.transform,new Vector2(250,125),new Vector2(-545,372));
                    var image = art.GetComponent<UnityEngine.UI.Image>() ?? art.AddComponent<UnityEngine.UI.Image>(); image.sprite=logo; image.preserveAspect=true; image.raycastTarget=false;
                    var title=page.transform.Find("Title")?.GetComponent<TMPro.TMP_Text>();
                    if(title!=null){title.rectTransform.anchoredPosition=new Vector2(140,title.rectTransform.anchoredPosition.y);title.rectTransform.sizeDelta=new Vector2(1020,title.rectTransform.sizeDelta.y);}
                }
                EditorSceneManager.SaveScene(scene);
            }
            if (items.Count != information.entries.Length)
            { File.WriteAllText(SportsSportLogoAuthoring.InfoPath,JsonUtility.ToJson(new SportsSportLogoAuthoring.Catalogue { entries=items.ToArray() },true)); AssetDatabase.ImportAsset(SportsSportLogoAuthoring.InfoPath); }
            var menuScene = EditorSceneManager.OpenScene(SportsBuilder.FrontScene);
            var front = UnityEngine.Object.FindAnyObjectByType<SportsFrontEnd>();
            foreach (var entry in SportsHitchesGames.All) Add(front,entry.key,entry.title,entry.title,entry.description,entry.scene,entry.ScenePath);
            var collection=front.pages[0].transform.Find("Collection")?.GetComponent<TMPro.TMP_Text>();
            if(collection!=null)collection.text=front.catalogue.cards.Length+" TRUCK SPORTS. ONE COLLECTION.\nLocal humans, shared controllers and autonomous AI.";
            SportsMenuArtAuthoring.InstallInto(front); EditorSceneManager.SaveScene(menuScene);
            SportsRetroMenuAuthoring.Upgrade(); AssetDatabase.SaveAssets();
            Debug.Log("HH10_MENU_ENTRIES=22; original artwork and captured gameplay previews assigned. Windows navigation acceptance remains required.");
        }
    }
}

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
namespace LWS.TruckSports.Editor
{
    // Explicit additive presentation upgrade. Normal builds never regenerate
    // screenshots, overwrite descriptions or replace authored card actions.
    public static class SportsMenuArtAuthoring
    {
        const string PreviewFolder = "Assets/TruckSports/Art/Previews";
        static readonly Dictionary<string, string> aliases = new Dictionary<string, string> {
            {"basejump","base"},{"targetlaunch","target"},{"beerpong","pong"},{"logrolling","log"},
            {"dodgeball","dodge"},{"pedestrianplowing","plowing"},{"pedestrianplowingcab","cabchaos"},{"keepawayfp","keepaway"}
        };
        [MenuItem("Truck Sports/Menu/Install original icons and verified gameplay captures")]
        public static void Upgrade()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop the explicit capture run before importing its images.");
            ImportCapturedImages(); EditorSceneManager.OpenScene(SportsBuilder.FrontScene);
            var front = UnityEngine.Object.FindAnyObjectByType<SportsFrontEnd>(); InstallInto(front);
            EditorSceneManager.SaveScene(front.gameObject.scene); AssetDatabase.SaveAssets();
        }
        public static void Reflow(){EditorSceneManager.OpenScene(SportsBuilder.FrontScene);var front=UnityEngine.Object.FindAnyObjectByType<SportsFrontEnd>();InstallInto(front);EditorSceneManager.SaveScene(front.gameObject.scene);AssetDatabase.SaveAssets();}
        public static void ImportCapturedImages()
        {
            Directory.CreateDirectory(PreviewFolder); AssetDatabase.Refresh();
            string root = "Logs/TruckSports/EssentialRelease/CollectionOpenings";
            foreach (string receipt in Directory.GetFiles(root, "receipt.json", SearchOption.AllDirectories).OrderBy(File.GetLastWriteTimeUtc))
            {
                var batch = JsonUtility.FromJson<SportsMenuCaptureProbe.Receipt>(File.ReadAllText(receipt));
                foreach (var entry in batch.scenes)
                {
                    if (entry.result != "OPENING CAPTURED" || entry.exceptions != 0 || !File.Exists(entry.screenshot720)) continue;
                    string path = PreviewFolder + "/" + entry.scene + ".png"; File.Copy(entry.screenshot720, path, true);
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                    var importer = (TextureImporter)AssetImporter.GetAtPath(path); importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single; importer.mipmapEnabled = false; importer.maxTextureSize = 1024;
                    importer.wrapMode = TextureWrapMode.Clamp; importer.SaveAndReimport();
                }
            }
        }
        public static void InstallInto(SportsFrontEnd front)
        {
            if (front == null || front.catalogue == null) throw new InvalidOperationException("Saved collection catalogue is required.");
            var icons = new Dictionary<string, Sprite>();
            foreach (string sheet in new[] { "Classics", "Events", "Newcomers" })
                foreach (var sprite in AssetDatabase.LoadAllAssetsAtPath("Assets/TruckSports/Art/SportIcons/" + sheet + ".png").OfType<Sprite>()) icons[sprite.name] = sprite;
            var snakeIcon=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/TruckSnake/Art/Truck Snake Icon.png");if(snakeIcon!=null)icons["sport-snake"]=snakeIcon;
            var subtitle=front.pages[1].transform.Find("Subtitle")?.GetComponent<TMP_Text>();if(subtitle!=null){Position(subtitle.rectTransform,new Vector2(1400,32),new Vector2(0,330));subtitle.fontSizeMax=22;subtitle.fontSizeMin=19;subtitle.color=new Color(.66f,.75f,.82f);}
            int missing = 0, updated = 0;
            var cards = front.catalogue.cards.ToList();
            var wallball = front.pages[4].transform.Find("WALLBALL Sport Card"); if (wallball != null) cards.Add(wallball.gameObject);
            foreach (var card in cards)
            {
                var action = card.GetComponentsInChildren<SportsMenuAction>(true).FirstOrDefault(a => a.action != "practice");
                if (action == null) continue;
                string key = action.action, iconKey = aliases.TryGetValue(key, out string alias) ? alias : key;
                string scene = SceneFor(key);
                var screenshot = AssetDatabase.LoadAssetAtPath<Sprite>(PreviewFolder + "/" + scene + ".png");
                if (!icons.TryGetValue("sport-" + iconKey, out var icon) || screenshot == null) { missing++; Debug.LogWarning("Menu card awaits verified art: " + key); continue; }
                int index = Array.IndexOf(front.catalogue.cards, card);
                var rect = card.GetComponent<RectTransform>(); rect.sizeDelta = new Vector2(330, 286);
                if (index >= 0) rect.anchoredPosition = new Vector2((index % 4 - 1.5f) * 345, index % 8 / 4 == 0 ? 148 : -148);
                var background = card.GetComponent<Image>(); background.color = SportsBroadcastCanvas.Ink;
                var plate = card.GetComponent<SportsBroadcastPlate>() ?? card.AddComponent<SportsBroadcastPlate>(); plate.cut = 18; plate.accent = true;
                var motion = card.GetComponent<SportsCardMotion>() ?? card.AddComponent<SportsCardMotion>();
                var title = card.transform.Find("Sport").GetComponent<TMP_Text>(); Position(title.rectTransform, new Vector2(242, 46), new Vector2(30, 112));
                title.enableAutoSizing = true; title.fontSizeMax = 25; title.fontSizeMin = 18; title.alignment = TextAlignmentOptions.Left;
                var description = card.transform.Find("Description").GetComponent<TMP_Text>(); Position(description.rectTransform, new Vector2(300, 38), new Vector2(0, -112));
                description.enableAutoSizing = true; description.fontSizeMax = 18; description.fontSizeMin = 15; description.alignment = TextAlignmentOptions.Center;
                foreach (Transform child in card.transform) if (child.name == "Rink icon" || child.name.StartsWith("Pool ball icon")) child.gameObject.SetActive(false);
                var iconRoot = card.transform.Find("Custom sport icon")?.GetComponent<RectTransform>();
                if (iconRoot == null) iconRoot = SportsUIAuthoring.Rect("Custom sport icon", card.transform, new Vector2(48,48), new Vector2(-130,112)).GetComponent<RectTransform>();
                var iconImage = iconRoot.GetComponent<Image>() ?? iconRoot.gameObject.AddComponent<Image>(); iconImage.sprite = icon; iconImage.preserveAspect = true; iconImage.raycastTarget = false;
                var frame = card.transform.Find("Rounded gameplay frame")?.GetComponent<RectTransform>();
                if (frame == null)
                {
                    frame = SportsUIAuthoring.Rect("Rounded gameplay frame", card.transform, new Vector2(300,168.75f), new Vector2(0,-2)).GetComponent<RectTransform>();
                    var maskImage = frame.gameObject.AddComponent<Image>(); maskImage.color = Color.white; maskImage.raycastTarget = false;
                    frame.gameObject.AddComponent<SportsBroadcastPlate>().cut = 12; frame.gameObject.AddComponent<Mask>().showMaskGraphic = false;
                }
                var picture = frame.Find("Sport preview"); if(picture==null)picture=card.transform.Find("Sport preview");
                if (picture == null) picture = SportsUIAuthoring.Rect("Sport preview", frame, new Vector2(300,168.75f), Vector2.zero).transform;
                picture.SetParent(frame, false); var image = picture.GetComponent<Image>() ?? picture.gameObject.AddComponent<Image>();
                SportsBroadcastCanvas.Stretch(picture.GetComponent<RectTransform>()); image.sprite = screenshot; image.preserveAspect = true; image.raycastTarget = false; picture.gameObject.SetActive(true);
                var buttons=card.GetComponentsInChildren<SportsMenuAction>(true);
                for(int n=0;n<buttons.Length;n++)
                {
                    var button=buttons[n];var buttonRect=button.GetComponent<RectTransform>();
                    float width=buttons.Length==1?100:(294-(buttons.Length-1)*8f)/buttons.Length;
                    float x=buttons.Length==1?92:-147+width*.5f+n*(width+8);
                    Position(buttonRect,new Vector2(width,36),new Vector2(x,-64));buttonRect.SetAsLastSibling();
                    var oldSelection=button.GetComponent<SportsCardSelection>();if(oldSelection!=null)UnityEngine.Object.DestroyImmediate(oldSelection);
                }
                EditorUtility.SetDirty(card); updated++;
            }
            front.catalogue.Step(0);
            if(Directory.Exists(SportsRetroMenuAuthoring.LogoFolder))SportsRetroMenuAuthoring.Install(front);
            File.WriteAllText("Logs/TruckSports/EssentialRelease/menu-art.txt", "UPDATED=" + updated + " MISSING=" + missing + " · original native sprite icons and real saved-game opening captures; descriptions/actions preserved.");
        }
        static void Position(RectTransform rect, Vector2 size, Vector2 position)
        { rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f,.5f); rect.sizeDelta = size; rect.anchoredPosition = position; }
        public static string SceneFor(string key)
        {
            var map = new Dictionary<string,string> {
                {"basejump","TruckBaseJumping"},{"cheese","TruckCheeseRolling"},{"dock","TruckDockDiving"},{"terrier","TruckTerrierRacing"},
                {"mattress","TruckMattressRacing"},{"wife","TruckWifeCarrying"},{"parking","TruckMusicalParking"},{"swing","TruckGiantSwing"},
                {"bog","TruckBogSnorkelling"},{"bungee","TruckBungeeDemolition"},{"targetlaunch","TruckTargetLaunch"},{"tug","TruckTugOfWar"},
                {"skee","TruckSkeeBall"},{"logrolling","TruckLogRolling"},{"dodgeball","TruckTrampolineDodgeball"},{"derby","TruckDemolitionDerby"},
                {"pedestrianplowing","TruckPedestrianPlowing"},{"pedestrianplowingcab","TruckPedestrianPlowingFirstPerson"},{"keepawayfp","TruckKeepawayFirstPerson"},
                {"airhockey","TruckAirHockey"},{"danceoff","TruckDanceOff"},{"skytow","TruckSkyTow"},{"trailerplug","TruckTrailerPlug"},
                {"fieldgoal","TruckFieldGoal"},{"beerpong","TruckBeerPong"},{"kickball","TruckKickball"},{"limbo","TruckLimbo"},{"farefight","TruckFareFight"},{"balloon","TruckBalloonBattle"},{"hotpotato","TruckHotPotato"},{"redlight","TruckRedLightGreenLight"},{"snake","TruckSnake"},{"practice","TruckBilliards"}
            };
            return map.TryGetValue(key, out string scene) ? scene : "Truck" + char.ToUpperInvariant(key[0]) + key.Substring(1);
        }
    }
}

using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using LWS.TruckKeepaway;
using LWS.TruckHotPotato;
using LWS.TruckBalloonBattle;
using LWS.TruckLimbo;

namespace LWS.TruckSports.Editor
{
    // The five-scene allowlist is intentional. No generic installation or protected-HUD migration.
    public static class CarnivalArenaHudAuthoring
    {
        public static readonly string[] Scenes={"Assets/TruckKeepaway/Scenes/TruckKeepaway.unity","Assets/TruckKeepaway/Scenes/TruckKeepawayFirstPerson.unity","Assets/TruckHotPotato/Scenes/TruckHotPotato.unity","Assets/TruckBalloonBattle/Scenes/TruckBalloonBattle.unity","Assets/TruckLimbo/Scenes/TruckLimbo.unity"};
        [MenuItem("Truck Sports/Carnival/Install five explicit arena HUD updates")]
        public static void Install()
        {
            foreach(string path in Scenes){EditorSceneManager.OpenScene(path);Install(UnityEngine.Object.FindAnyObjectByType<SportsEventSession>(),true);}
            Debug.Log("CARNIVAL_ARENA_HUD_AUTHORED Keepaway arena/cab, HotPotato split only, Balloon bottom eight, Limbo relevant physical state; protected arena and unrelated modes unchanged");
        }
        // Native tests may explicitly build the same saved-author hierarchy in memory, without saving assets.
        public static Component Install(SportsEventSession match,bool save)
        {
            if(match==null||!Scenes.Contains(match.gameObject.scene.path))throw new ArgumentException("Only the five reviewed Carnival arena HUD scenes are permitted.");
            Type type=match is KeepawayMatch?typeof(KeepawayCarnivalHud):match is HotPotatoMatch?typeof(HotPotatoSplitHud):match is BalloonMatch?typeof(BalloonCarnivalHud):match is LimboMatch?typeof(LimboCarnivalHud):null;
            if(type==null)throw new InvalidOperationException("The scene must retain its original authoritative match.");
            var existing=match.menus.GetComponent(type);if(existing!=null)return existing;
            var theme=Resources.Load<SportsBroadcastTheme>("SportsBroadcastTheme");var portrait=Resources.Load<Sprite>("Broadcast truck portrait");
            if(theme==null||theme.headingFont==null||theme.bodyFont==null||portrait==null)throw new InvalidOperationException("Existing Heat broadcast fonts and truck portrait are required.");
            var graphics=match.menus.GetComponentsInChildren<Graphic>(true);var preserved=graphics.Select(graphic=>EditorJsonUtility.ToJson(graphic)).ToArray();
            var retired=match.menus.header.transform.parent.GetComponentsInChildren<Graphic>(true).Concat(new Graphic[]{match.menus.status,match.menus.standings}).Concat(match.menus.viewTexts).Distinct().ToArray();
            var root=Rect(match.menus.hud.transform,"Carnival saved "+type.Name,Vector2.zero);SportsBroadcastCanvas.Stretch(root);var group=root.gameObject.AddComponent<CanvasGroup>();group.interactable=group.blocksRaycasts=false;
            var row=Rect(root,"Single shared compact participant row",Vector2.zero);int capacity=match is KeepawayMatch?4:8;
            var cards=new CarnivalArenaHudCard[capacity];
            for(int slot=0;slot<capacity;slot++)
            {
                var card=new CarnivalArenaHudCard{root=Plate(row,"Stable scored participant "+slot,new Vector2(230,82))};
                card.name=Text(card.root,"Actual participant name",theme.headingFont,20);card.value=Text(card.root,"Actual sport score",theme.headingFont,23);card.detail=Text(card.root,"Relevant authoritative state",theme.bodyFont,17);
                card.portrait=Image(card.root,"Existing truck portrait",new Vector2(32,32));card.portrait.sprite=portrait;card.portrait.preserveAspect=true;
                card.stripe=Image(card.root,"Actual match color",new Vector2(4,70));card.progress=Image(card.root,"Actual target progress",new Vector2(180,3));
                if(match is BalloonMatch){card.pips=new Image[3];for(int p=0;p<3;p++){card.pips[p]=Image(card.root,"Authoritative balloon "+p,new Vector2(11,11));card.pips[p].sprite=AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");}}
                cards[slot]=card;
            }
            Component result;
            if(match is KeepawayMatch keep)
            {
                var hud=match.menus.gameObject.AddComponent<KeepawayCarnivalHud>();hud.match=keep;hud.root=root;hud.row=row;hud.cards=cards;hud.legacyGraphics=retired;hud.originalNames=keep.identities.Select(identity=>identity.GetComponent<Renderer>()).ToArray();hud.icons=new SpriteRenderer[4];
                var world=new GameObject("Keepaway portrait identity markers");world.transform.SetParent(keep.transform,false);
                for(int slot=0;slot<4;slot++)
                {
                    var icon=new GameObject("Actual truck icon "+slot);icon.transform.SetParent(world.transform,false);icon.layer=22+slot;icon.transform.localScale=Vector3.one*(1.8f/Mathf.Max(.01f,portrait.bounds.size.x));
                    hud.icons[slot]=icon.AddComponent<SpriteRenderer>();hud.icons[slot].sprite=portrait;
                    var billboard=icon.AddComponent<SportsPerCameraBillboard>();billboard.distanceSizing=true;billboard.referenceDistance=30;billboard.maximumPixelHeight=32;billboard.maximumPixelWidth=44;icon.SetActive(false);
                }
                result=hud;
            }
            else if(match is HotPotatoMatch hot)
            {
                var hud=match.menus.gameObject.AddComponent<HotPotatoSplitHud>();hud.match=hot;hud.root=root;hud.row=row;hud.cards=cards;hud.legacyGraphics=retired;hud.contextRoot=Context(root,theme,out var text);hud.context=text;result=hud;
            }
            else if(match is BalloonMatch balloon)
            {
                var hud=match.menus.gameObject.AddComponent<BalloonCarnivalHud>();hud.match=balloon;hud.root=root;hud.row=row;hud.cards=cards;hud.legacyGraphics=retired;hud.countdownRoot=Context(root,theme,out var text);hud.countdown=text;result=hud;
            }
            else
            {
                var hud=match.menus.gameObject.AddComponent<LimboCarnivalHud>();hud.match=(LimboMatch)match;hud.root=root;hud.row=row;hud.cards=cards;hud.legacyGraphics=retired;hud.contextRoot=Context(root,theme,out var text);hud.context=text;result=hud;
            }
            root.gameObject.SetActive(false);
            for(int i=0;i<graphics.Length;i++)if(EditorJsonUtility.ToJson(graphics[i])!=preserved[i])throw new InvalidOperationException("Original or protected graphic changed during authoring: "+graphics[i].name);
            EditorUtility.SetDirty(result);
            if(save){if(EditorApplication.isPlaying)throw new InvalidOperationException("Save authoring is editor-only.");EditorSceneManager.MarkSceneDirty(match.gameObject.scene);EditorSceneManager.SaveScene(match.gameObject.scene);AssetDatabase.SaveAssets();}
            return result;
        }
        static RectTransform Rect(Transform parent,string name,Vector2 size)=>SportsUIAuthoring.Rect(name,parent,size,Vector2.zero).GetComponent<RectTransform>();
        static RectTransform Plate(Transform parent,string name,Vector2 size){var root=Rect(parent,name,size);var image=root.gameObject.AddComponent<Image>();image.color=SportsBroadcastCanvas.Ink;image.raycastTarget=false;root.gameObject.AddComponent<SportsBroadcastPlate>().cut=7;return root;}
        static Image Image(Transform parent,string name,Vector2 size){var image=Rect(parent,name,size).gameObject.AddComponent<Image>();image.raycastTarget=false;image.color=SportsBroadcastCanvas.Cream;return image;}
        static TMP_Text Text(Transform parent,string name,TMP_FontAsset font,int size){var text=SportsUIAuthoring.Text(name,parent,"",new Vector2(150,30),Vector2.zero,size);text.font=font;text.richText=false;text.fontSizeMax=size;text.fontSizeMin=size-2;text.overflowMode=TextOverflowModes.Ellipsis;text.alignment=TextAlignmentOptions.MidlineLeft;text.raycastTarget=false;return text;}
        static RectTransform Context(Transform parent,SportsBroadcastTheme theme,out TMP_Text text){var root=Plate(parent,"Essential round clock or objective",new Vector2(720,40));root.anchorMin=root.anchorMax=root.pivot=new Vector2(.5f,1);root.anchoredPosition=new Vector2(0,-12);text=Text(root,"Actual current objective",theme.headingFont,22);SportsBroadcastCanvas.Stretch(text.rectTransform,10,3);text.alignment=TextAlignmentOptions.Center;return root;}
    }
}

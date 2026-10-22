using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using LWS.TruckSports;
using LWS.TruckSports.Editor;

namespace LWS.TruckHockey.Editor
{
    public static class HockeyPresentationAuthoring
    {
        static TMP_FontAsset heading,body;
        static RectTransform Rect(string name,Transform parent,Vector2 size,Vector2 anchor,Vector2 at)
        {
            var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);var rect=(RectTransform)go.transform;
            rect.anchorMin=rect.anchorMax=rect.pivot=anchor;rect.anchoredPosition=at;rect.sizeDelta=size;return rect;
        }
        static Image Plate(string name,Transform parent,Vector2 size,Vector2 anchor,Vector2 at,Color color)
        {
            var rect=Rect(name,parent,size,anchor,at);var image=rect.gameObject.AddComponent<Image>();image.color=color;image.raycastTarget=false;
            rect.gameObject.AddComponent<SportsBroadcastPlate>().cut=12;return image;
        }
        static TMP_Text Label(string name,Transform parent,string value,Vector2 size,Vector2 at,int font,bool bold=false)
        {
            var rect=Rect(name,parent,size,new Vector2(.5f,.5f),at);var text=rect.gameObject.AddComponent<TextMeshProUGUI>();text.text=value;text.font=bold?heading:body;text.fontSizeMax=font;text.fontSizeMin=font-3;text.enableAutoSizing=true;text.color=SportsBroadcastCanvas.Cream;text.raycastTarget=false;text.alignment=TextAlignmentOptions.Center;text.overflowMode=TextOverflowModes.Ellipsis;return text;
        }
        static void Move(TMP_Text text,Transform parent,Vector2 size,Vector2 at,int font)
        {
            text.transform.SetParent(parent,false);var rect=text.rectTransform;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);rect.anchoredPosition=at;rect.sizeDelta=size;
            text.font=heading;text.fontSizeMax=font;text.fontSizeMin=font-3;text.enableAutoSizing=true;text.alignment=TextAlignmentOptions.Center;text.overflowMode=TextOverflowModes.Ellipsis;
        }
        public static void Upgrade()
        {
            var scene=EditorSceneManager.OpenScene(HockeyAuthoring.Scene);var menus=UnityEngine.Object.FindAnyObjectByType<HockeyMenus>();
            if(menus==null||menus.names?.Length!=6)throw new InvalidOperationException("Saved six-participant Hockey UI required");
            if(menus.presentation!=null)
            {
                bool changed=RetireLegacyOverlay(menus);changed|=ImproveContrast(menus);
                if(changed){EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);}
                Debug.Log("HOCKEY_PRESENTATION preserved existing authored presentation");return;
            }
            var theme=AssetDatabase.LoadAssetAtPath<SportsBroadcastTheme>("Assets/TruckSports/Resources/SportsBroadcastTheme.asset");heading=theme.headingFont;body=theme.bodyFont;
            var p=menus.gameObject.AddComponent<HockeyPresentation>();p.menus=menus;menus.presentation=p;
            var hud=(RectTransform)menus.hud.transform;SportsBroadcastCanvas.Stretch(hud);
            // Move existing referenced text, then retire only the empty legacy docks.
            var legacy=new[]{menus.scoreText.transform.parent,menus.clockText.transform.parent,menus.teamText.transform.parent,menus.orangeTeamText.transform.parent,menus.messageText.transform.parent};
            var bug=Plate("Hockey score bug",hud,new Vector2(830,98),new Vector2(.5f,1),new Vector2(0,-18),SportsBroadcastCanvas.Ink).rectTransform;p.scoreBug=bug;
            p.blueTitle=Label("Blue team title",bug,"BLUE",new Vector2(200,38),new Vector2(-269,13),30,true);p.blueTitle.color=HockeyPresentation.Blue;
            p.orangeTitle=Label("Orange team title",bug,"ORANGE",new Vector2(200,38),new Vector2(269,13),30,true);p.orangeTitle.color=HockeyPresentation.Orange;
            Move(menus.scoreText,bug,new Vector2(220,58),new Vector2(0,10),45);Move(menus.clockText,bug,new Vector2(190,28),new Vector2(0,-30),23);
            p.matchType=Label("Match type",bug,"TRUCK HOCKEY · 3v3",new Vector2(230,25),new Vector2(-269,-24),20);
            Label("Hockey scoring rule",bug,"DRIVE · HIT · SCORE",new Vector2(230,25),new Vector2(269,-24),20);
            var notice=Plate("Hockey phase notice",hud,new Vector2(1020,54),new Vector2(.5f,1),new Vector2(0,-126),SportsBroadcastCanvas.Ink).rectTransform;p.noticePlate=notice;
            Move(menus.messageText,notice,new Vector2(984,46),Vector2.zero,27);
            var roster=Rect("Hockey participant row",hud,new Vector2(1860,96),new Vector2(.5f,0),new Vector2(0,42));
            p.participantNames=new TMP_Text[6];p.participantSources=new TMP_Text[6];p.portraits=new Image[6];p.stripes=new Image[6];p.setupSlots=new TMP_Text[6];p.setupPortraits=new Image[6];
            var teamBlue=Rect("Blue rink end",hud,new Vector2(900,26),new Vector2(.25f,0),new Vector2(0,119));Move(menus.teamText,teamBlue,new Vector2(850,26),Vector2.zero,20);
            var teamOrange=Rect("Orange rink end",hud,new Vector2(900,26),new Vector2(.75f,0),new Vector2(0,119));Move(menus.orangeTeamText,teamOrange,new Vector2(850,26),Vector2.zero,20);
            var sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/TruckSports/Resources/Broadcast truck portrait.png");
            for(int i=0;i<6;i++)
            {
                var card=Plate("Hockey participant "+i,roster,new Vector2(300,70),new Vector2(0,0),new Vector2(i*310,0),SportsBroadcastCanvas.Ink).rectTransform;
                p.stripes[i]=Plate("Crew color",card,new Vector2(4,54),new Vector2(0,.5f),new Vector2(0,0),Color.white);
                var portrait=Rect("Truck portrait",card,new Vector2(44,44),new Vector2(0,.5f),new Vector2(12,0)).gameObject.AddComponent<Image>();portrait.sprite=sprite;portrait.preserveAspect=true;portrait.raycastTarget=false;p.portraits[i]=portrait;
                p.participantNames[i]=Label("Participant name",card,"Player",new Vector2(222,29),new Vector2(29,13),24,true);p.participantNames[i].alignment=TextAlignmentOptions.MidlineLeft;
                p.participantSources[i]=Label("Assigned source",card,"",new Vector2(222,26),new Vector2(29,-16),19);p.participantSources[i].alignment=TextAlignmentOptions.MidlineLeft;
                // Keep all Heat/input references and actions. Only add a backing and portrait.
                var input=menus.names[i];var setup=menus.pages[(int)HockeyScreen.Setup].transform;float x=i<3?-335:335,y=132-(i%3)*130;
                var backing=Plate("Hockey setup participant "+i,setup,new Vector2(588,124),new Vector2(.5f,.5f),new Vector2(x,y),SportsBroadcastCanvas.Raised).rectTransform;backing.SetSiblingIndex(setup.Find("Card").GetSiblingIndex()+1);
                var slot=setup.Find((i<3?"BLUE":"ORANGE")+" SLOT "+(i%3+1));p.setupSlots[i]=slot.GetComponent<TMP_Text>();p.setupSlots[i].text=(i<3?"BLUE":"ORANGE")+" · SLOT "+(i%3+1);p.setupSlots[i].font=heading;p.setupSlots[i].fontSizeMax=20;p.setupSlots[i].fontSizeMin=18;
                var sr=(RectTransform)slot;sr.anchoredPosition=new Vector2(x+24,y+43);sr.sizeDelta=new Vector2(460,26);
                var icon=Rect("Crew truck portrait",backing,new Vector2(38,38),new Vector2(0,1),new Vector2(14,-5)).gameObject.AddComponent<Image>();icon.sprite=sprite;icon.preserveAspect=true;icon.raycastTarget=false;p.setupPortraits[i]=icon;
                input.image.color=new Color(.025f,.035f,.065f,1);input.textComponent.font=body;input.textComponent.color=SportsBroadcastCanvas.Cream;
                ((RectTransform)input.transform).anchoredPosition=new Vector2(x,y+9);((RectTransform)input.transform).sizeDelta=new Vector2(550,38);
                foreach(var label in new[]{menus.sourceLabels[i],menus.levelLabels[i]})
                {
                    var button=label.GetComponentInParent<Michsky.UI.Heat.ButtonManager>();var rect=(RectTransform)button.transform;rect.anchoredPosition=new Vector2(rect.anchoredPosition.x,y-42);rect.sizeDelta=new Vector2(rect.sizeDelta.x,34);button.textSize=20;label.fontSizeMax=20;label.fontSizeMin=17;label.rectTransform.sizeDelta=new Vector2(rect.sizeDelta.x-18,28);EditorUtility.SetDirty(button);
                }
            }
            foreach(var old in legacy)if(old!=hud)old.gameObject.SetActive(false);
            var footer=Rect("Hockey compact footer",hud,new Vector2(1780,26),new Vector2(.5f,0),new Vector2(0,10));Move(menus.controlsText,footer,new Vector2(1780,26),Vector2.zero,18);
            RetireLegacyOverlay(menus);
            var pause=menus.pages[(int)HockeyScreen.Pause];p.pauseSummary=Label("Current match",pause.transform,"",new Vector2(1200,44),new Vector2(0,255),28,true);
            // Keep existing results/rematch actions; present scores and teams as cards.
            var result=menus.pages[(int)HockeyScreen.Result].transform;Move(menus.resultText,result,new Vector2(1200,188),new Vector2(0,147),36);
            var rematch=result.GetComponentsInChildren<SportsMenuAction>(true).Single(a=>a.action=="rematch");((RectTransform)rematch.transform).anchoredPosition=new Vector2(0,-140);
            p.resultTeams=new TMP_Text[2];for(int team=0;team<2;team++){var plate=Plate("Result team "+team,result,new Vector2(610,88),new Vector2(.5f,.5f),new Vector2(team==0?-320:320,-14),SportsBroadcastCanvas.Raised).rectTransform; p.resultTeams[team]=Label("Team roster",plate,"",new Vector2(580,76),Vector2.zero,27,true);p.resultTeams[team].color=HockeyPresentation.TeamColor(team);}
            ImproveContrast(menus);
            EditorUtility.SetDirty(menus);EditorUtility.SetDirty(p);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            Directory.CreateDirectory("Logs/TruckSports/PlaytestRepair");File.WriteAllText("Logs/TruckSports/PlaytestRepair/hockey-presentation-authoring.txt","PASS additive Hockey presentation; preserved six-slot Heat actions, input ownership, rules, cameras and physics.\n");
        }
        static bool RetireLegacyOverlay(HockeyMenus menus)
        {
            bool changed=false;var old=menus.hud.GetComponent<SportsArenaOverlay>();
            if(old!=null)
            {
                if(old.enabled){old.enabled=false;EditorUtility.SetDirty(old);changed=true;}
                foreach(var dock in old.docks)if(dock.plate!=null&&dock.target!=null&&dock.target.parent!=dock.plate&&dock.plate.gameObject.activeSelf){dock.plate.gameObject.SetActive(false);changed=true;}
            }
            if(!menus.controlsText.gameObject.activeSelf){menus.controlsText.gameObject.SetActive(true);changed=true;}
            return changed;
        }
        static bool ImproveContrast(HockeyMenus menus)
        {
            bool changed=false;
            foreach(var text in new[]{menus.teamText,menus.orangeTeamText,menus.controlsText})
            {
                var rect=text.transform.parent as RectTransform;
                if(rect==null||rect.GetComponent<Image>()!=null)continue;
                // Migrate only the known presentation roots made by this tool.
                bool footer=rect.name=="Hockey compact footer";
                if(!footer&&rect.name!="Blue rink end"&&rect.name!="Orange rink end")continue;
                var image=rect.gameObject.AddComponent<Image>();image.color=SportsBroadcastCanvas.Ink;image.raycastTarget=false;rect.gameObject.AddComponent<SportsBroadcastPlate>().cut=8;
                rect.sizeDelta=footer?new Vector2(960,30):new Vector2(280,30);text.rectTransform.sizeDelta=footer?new Vector2(940,28):new Vector2(260,26);changed=true;
            }
            return changed;
        }
    }
}

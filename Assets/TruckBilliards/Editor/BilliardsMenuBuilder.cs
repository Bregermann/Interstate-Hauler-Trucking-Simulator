using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using TMPro;
using Michsky.UI.Heat;
namespace LWS.TruckBilliards.Editor
{
 public static class BilliardsMenuBuilder
 {
  static BilliardsMenus menus;
  static GameObject Rect(string name,Transform parent,Vector2 size,Vector2 pos){
   var g=new GameObject(name,typeof(RectTransform));g.transform.SetParent(parent,false);var r=g.GetComponent<RectTransform>();r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.sizeDelta=size;r.anchoredPosition=pos;return g;
  }
  static TextMeshProUGUI Text(string name,Transform parent,string value,Vector2 size,Vector2 pos,int font=28){
   var g=Rect(name,parent,size,pos);var t=g.AddComponent<TextMeshProUGUI>();t.font=TMP_Settings.defaultFontAsset;t.text=value;t.fontSize=font;t.alignment=TextAlignmentOptions.Center;t.color=Color.white;t.raycastTarget=false;t.overflowMode=TextOverflowModes.Overflow;return t;
  }
  static ButtonManager Button(Transform parent,string caption,string action,Vector2 pos,Vector2? size=null){
   var g=Rect(action+" Button",parent,size??new Vector2(480,64),pos);var image=g.AddComponent<UnityEngine.UI.Image>();image.color=new Color(.035f,.19f,.19f);
   var native=g.AddComponent<UnityEngine.UI.Button>();native.transition=UnityEngine.UI.Selectable.Transition.None;
   var text=Text("Caption",g.transform,caption,(size??new Vector2(480,64))-new Vector2(20,6),Vector2.zero,26);
   var button=g.AddComponent<ButtonManager>();button.autoFitContent=false;button.useLocalization=false;button.useSounds=false;button.checkForDoubleClick=false;button.normalTextObj=text;button.textSize=26;button.buttonText=caption;button.useUINavigation=true;button.navigationMode=UnityEngine.UI.Navigation.Mode.Explicit;
   var actionComponent=g.AddComponent<BilliardsMenuAction>();actionComponent.action=action;actionComponent.button=button;
   var appearance=g.AddComponent<BilliardsButtonVisual>();appearance.surface=image;return button;
  }
  static GameObject Page(BilliardsScreen screen,string title,string subtitle=""){
   var root=Rect(screen+" Screen",menus.transform,new Vector2(1920,1080),Vector2.zero);
   var backdrop=root.AddComponent<UnityEngine.UI.Image>();backdrop.color=new Color(.004f,.012f,.019f,.82f);
   var card=Rect("Card",root.transform,new Vector2(1460,870),new Vector2(0,0));card.AddComponent<UnityEngine.UI.Image>().color=new Color(.014f,.038f,.048f,.97f);
   var accent=Rect("Accent",root.transform,new Vector2(1460,6),new Vector2(0,431));accent.AddComponent<UnityEngine.UI.Image>().color=new Color(.15f,.8f,.7f);
   Text("Title",root.transform,title,new Vector2(1400,80),new Vector2(0,350),48);
   Text("Subtitle",root.transform,subtitle,new Vector2(1380,70),new Vector2(0,275),25).color=new Color(.6f,.78f,.79f);
   menus.pages[(int)screen]=root;return root;
  }
  static TMP_InputField NameField(Transform parent,string label,string value,Vector2 pos){
   Text(label,parent,label,new Vector2(520,40),pos+new Vector2(0,42),24);
   var g=Rect(label+" Input",parent,new Vector2(520,60),pos);g.AddComponent<UnityEngine.UI.Image>().color=new Color(.05f,.11f,.14f);
   var viewport=Rect("Viewport",g.transform,new Vector2(496,56),Vector2.zero);viewport.AddComponent<UnityEngine.UI.RectMask2D>();
   var text=Text("Name",viewport.transform,value,new Vector2(480,56),Vector2.zero,27);text.alignment=TextAlignmentOptions.MidlineLeft;
   var placeholder=Text("Placeholder",viewport.transform,"Enter player name",new Vector2(480,56),Vector2.zero,25);placeholder.color=new Color(.5f,.6f,.65f);
   var input=g.AddComponent<TMP_InputField>();input.textViewport=viewport.GetComponent<RectTransform>();input.textComponent=text;input.placeholder=placeholder;input.characterLimit=20;input.text=value;input.lineType=TMP_InputField.LineType.SingleLine;
   var heat=g.AddComponent<InputFieldManager>();heat.inputText=input;heat.processSubmit=false;return input;
  }
  static UnityEngine.UI.Slider Slider(Transform parent,string label,Vector2 pos,float min,float max,float value){
   Text(label,parent,label,new Vector2(600,40),pos+new Vector2(0,48),25);
   var g=Rect(label+" Slider",parent,new Vector2(600,48),pos);
   var bg=Rect("Track",g.transform,new Vector2(600,12),Vector2.zero);bg.AddComponent<UnityEngine.UI.Image>().color=new Color(.05f,.18f,.2f);
   var handle=Rect("Handle",g.transform,new Vector2(24,0),Vector2.zero);var handleImage=handle.AddComponent<UnityEngine.UI.Image>();handleImage.color=new Color(.15f,.8f,.7f);
   var slider=g.AddComponent<UnityEngine.UI.Slider>();slider.minValue=min;slider.maxValue=max;slider.value=value;slider.handleRect=handle.GetComponent<RectTransform>();slider.targetGraphic=handleImage;
   var heat=g.AddComponent<SliderManager>();heat.mainSlider=slider;heat.saveValue=false;heat.invokeOnAwake=false;heat.useSounds=false;heat.showValue=true;heat.showPopupValue=false;heat.usePercent=max==100;heat.useRoundValue=max==100;
   var readout=Text("Value",g.transform,max==100?value.ToString("0")+"%":value.ToString("0.0"),new Vector2(75,40),new Vector2(355,0),24);
   var serialized=new SerializedObject(heat);serialized.FindProperty("valueText").objectReferenceValue=readout;serialized.ApplyModifiedPropertiesWithoutUndo();
   return slider;
  }
  static void Navigation(GameObject page){
   var items=page.GetComponentsInChildren<UnityEngine.UI.Selectable>(true);
   for(int i=0;i<items.Length;i++){
    var nav=new UnityEngine.UI.Navigation{mode=UnityEngine.UI.Navigation.Mode.Explicit,selectOnUp=items[(i+items.Length-1)%items.Length],selectOnDown=items[(i+1)%items.Length]};
    nav.selectOnLeft=nav.selectOnUp;nav.selectOnRight=nav.selectOnDown;items[i].navigation=nav;
   }
  }
  [MenuItem("Truck Billiards/Upgrade Local Multiplayer Menus")]
  public static void Upgrade(){
   var scene=EditorSceneManager.OpenScene(BilliardsBuilder.ScenePath);
   var match=UnityEngine.Object.FindAnyObjectByType<BilliardsMatch>();var hud=UnityEngine.Object.FindAnyObjectByType<BilliardsHUD>();
   if(match==null||hud==null)throw new Exception("Existing TruckBilliards scene missing");
   var oldMenu=hud.transform.Find("Standalone Menus");if(oldMenu!=null)UnityEngine.Object.DestroyImmediate(oldMenu.gameObject);
   var session=match.GetComponent<BilliardsSession>()??match.gameObject.AddComponent<BilliardsSession>();
   var router=match.GetComponent<BilliardsInputRouter>()??match.gameObject.AddComponent<BilliardsInputRouter>();
   var settings=match.GetComponent<BilliardsSettings>()??match.gameObject.AddComponent<BilliardsSettings>();
   var feedback=match.GetComponent<BilliardsFeedback>()??match.gameObject.AddComponent<BilliardsFeedback>();session.feedback=feedback;feedback.session=session;feedback.clips=BilliardsAudioAuthoring.Generate();
   match.session=session;session.match=match;session.hud=hud;session.input=router;session.settings=settings;router.session=session;settings.session=session;
   if(session.players==null||session.players.Length!=2||session.players[0]==null)session.players=new[]{new BilliardsPlayerSlot{name="Player 1",color=new Color(.15f,.8f,1),truck=match.player},new BilliardsPlayerSlot{name="Player 2",color=new Color(1,.55f,.13f),truck=match.ai}};
   var overlay=Rect("Standalone Menus",hud.transform,new Vector2(1920,1080),Vector2.zero);menus=overlay.AddComponent<BilliardsMenus>();menus.session=session;session.menus=menus;
   if(hud.overlay!=null)hud.overlay.SetActive(false);
   hud.controls=hud.transform.Find("Controls").GetComponent<TMP_Text>();hud.controls.fontSize=20;
   hud.controls.rectTransform.sizeDelta=new Vector2(1050,100);hud.controls.rectTransform.anchoredPosition=new Vector2(-15,8);
   if(hud.scoreboard!=null)UnityEngine.Object.DestroyImmediate(hud.scoreboard.gameObject);hud.scoreboard=Text("Series Score",hud.transform,"",new Vector2(1240,45),new Vector2(0,375),22);hud.scoreboard.enableAutoSizing=true;hud.scoreboard.fontSizeMin=18;hud.scoreboard.fontSizeMax=22;
   var main=Page(BilliardsScreen.Main,"TRUCK BILLIARDS","Tiny trucks. Big shots. • Local 8-ball");
   Button(main.transform,"PLAY","play",new Vector2(0,170));Button(main.transform,"PRACTICE","practice",new Vector2(0,80));Button(main.transform,"HOW TO PLAY","help",new Vector2(0,-10));Button(main.transform,"SETTINGS","settings",new Vector2(0,-100));Button(main.transform,"QUIT","quit",new Vector2(0,-190));
   Text("Footer",main.transform,"Keyboard + mouse • Local multiplayer\nChoose a mode and drive the break.",new Vector2(1250,120),new Vector2(0,-300),26);
   main.GetComponent<UnityEngine.UI.Image>().color=new Color(.004f,.012f,.019f,.35f);
   var mainCard=main.transform.Find("Card").GetComponent<RectTransform>();mainCard.sizeDelta=new Vector2(710,870);mainCard.anchoredPosition=new Vector2(-450,0);
   foreach(Transform child in main.transform){
    if(child.name=="Card")continue;
    var rect=child.GetComponent<RectTransform>();rect.anchoredPosition=new Vector2(-450,rect.anchoredPosition.y);
    if(child.name=="Title"){rect.sizeDelta=new Vector2(670,80);child.GetComponent<TMP_Text>().fontSize=40;}
    else if(child.name=="Accent")rect.sizeDelta=new Vector2(710,6);
    else if(child.name=="Subtitle"||child.name=="Footer")rect.sizeDelta=new Vector2(660,120);
   }
   Text("Table identity",main.transform,"8 BALL\n2 TRUCKS\nONE TABLE",new Vector2(600,250),new Vector2(490,250),54).color=new Color(.75f,.95f,.9f);
   var setup=Page(BilliardsScreen.Setup,"MATCH SETUP","Choose a mode and players. Confirm your devices before starting.");
   menus.modeText=Button(setup.transform,"SINGLE PLAYER vs AI","mode",new Vector2(0,210),new Vector2(1160,65)).normalTextObj;
   menus.name1=NameField(setup.transform,"PLAYER 1 NAME","Player 1",new Vector2(-320,95));
   menus.name2=NameField(setup.transform,"PLAYER 2 NAME","Player 2",new Vector2(320,95));
   menus.color1Text=Button(setup.transform,"Truck color: BLUE","color1",new Vector2(-320,15),new Vector2(520,60)).normalTextObj;
   menus.color2Text=Button(setup.transform,"Truck color: ORANGE","color2",new Vector2(320,15),new Vector2(520,60)).normalTextObj;
   menus.breakText=Button(setup.transform,"Opening break: Player 1","break",new Vector2(-320,-65),new Vector2(520,60)).normalTextObj;
   menus.hotDeviceText=Button(setup.transform,"Hot-seat device: KEYBOARD","hotdevice",new Vector2(320,-65),new Vector2(520,60)).normalTextObj;
   menus.formatText=Button(setup.transform,"Length: SINGLE RACK","format",new Vector2(-320,-140),new Vector2(520,55)).normalTextObj;menus.difficultyText=Button(setup.transform,"AI: NORMAL","difficulty",new Vector2(320,-140),new Vector2(520,55)).normalTextObj;
   menus.setupInfo=Text("Requirements",setup.transform,"",new Vector2(1180,170),new Vector2(0,-265),21);menus.setupInfo.enableAutoSizing=true;menus.setupInfo.fontSizeMin=18;menus.setupInfo.fontSizeMax=21;
   Button(setup.transform,"BACK","back",new Vector2(-320,-390),new Vector2(520,50));Button(setup.transform,"CONTINUE / START","start",new Vector2(320,-390),new Vector2(520,50));
   var join=Page(BilliardsScreen.Join,"JOIN CONTROLLERS","North / Y / Triangle joins • South / A / Cross confirms menu selections");
   menus.joinInfo=Text("Body",join.transform,"",new Vector2(1300,300),new Vector2(0,60),27);
   Button(join.transform,"REASSIGN PLAYER 1","reassign1",new Vector2(-320,-170),new Vector2(520,60));Button(join.transform,"REASSIGN PLAYER 2","reassign2",new Vector2(320,-170),new Vector2(520,60));
   Button(join.transform,"BACK","back",new Vector2(-320,-330),new Vector2(520,60));menus.joinStart=Button(join.transform,"START MATCH","joinedstart",new Vector2(320,-330),new Vector2(520,60));
   var help=Page(BilliardsScreen.Help,"HOW TO PLAY / CONTROLS");
   menus.helpText=Text("Body",help.transform,"",new Vector2(1280,605),new Vector2(0,-30),22);
   Button(help.transform,"BACK","back",new Vector2(0,-365));
   var settingsPage=Page(BilliardsScreen.Settings,"SETTINGS","Applies to Truck Billiards • Audio changes preview immediately");
   menus.masterSlider=Slider(settingsPage.transform,"MASTER VOLUME",new Vector2(-325,140),0,100,100);
   menus.sfxSlider=Slider(settingsPage.transform,"SFX VOLUME",new Vector2(-325,30),0,100,65);
   menus.sensitivitySlider=Slider(settingsPage.transform,"CONTROLLER STEERING",new Vector2(-325,-80),.4f,2,1);
   menus.fullscreenText=Button(settingsPage.transform,"Display: WINDOWED","fullscreen",new Vector2(350,135),new Vector2(560,60)).normalTextObj;
   menus.resolutionText=Button(settingsPage.transform,"Resolution","resolution",new Vector2(350,40),new Vector2(560,60)).normalTextObj;
   menus.vibrationText=Button(settingsPage.transform,"Vibration: ON","vibration",new Vector2(350,-65),new Vector2(560,60)).normalTextObj;
   Text("Music note",settingsPage.transform,"Impacts, pockets and menu clicks.\nSave / Apply stores your preferences.",new Vector2(580,90),new Vector2(350,-165),23);
   Button(settingsPage.transform,"BACK","back",new Vector2(-320,-330),new Vector2(520,60));Button(settingsPage.transform,"SAVE / APPLY","save",new Vector2(320,-330),new Vector2(520,60));
   var pause=Page(BilliardsScreen.Pause,"PAUSED","Your match state is preserved");
   string[] labels={"RESUME","CONTROLS","SETTINGS","RESTART RACK","RESTART SERIES","MAIN MENU"};string[] actions={"resume","help","settings","restart","restartseries","main"};
   for(int i=0;i<6;i++)Button(pause.transform,labels[i],actions[i],new Vector2(0,190-i*75),new Vector2(560,56));
   Button(pause.transform,"RESET PRACTICE RACK","practicereset",new Vector2(0,-35),new Vector2(560,56));Button(pause.transform,"REPOSITION CUE • WHEN SETTLED","reposition",new Vector2(0,-110),new Vector2(760,56));
   var confirm=Page(BilliardsScreen.Confirm,"ARE YOU SURE?");menus.confirmText=Text("Body",confirm.transform,"",new Vector2(1250,230),new Vector2(0,80),32);
   Button(confirm.transform,"CANCEL","back",new Vector2(-320,-180),new Vector2(520,70));Button(confirm.transform,"CONFIRM","yes",new Vector2(320,-180),new Vector2(520,70));
   var handoff=Page(BilliardsScreen.Handoff,"HOT SEAT");menus.handoffText=Text("Body",handoff.transform,"",new Vector2(1250,400),new Vector2(0,35),34);
   Button(handoff.transform,"I'M READY","ready",new Vector2(0,-270),new Vector2(650,75));
   var disconnect=Page(BilliardsScreen.Disconnect,"CONTROLLER DISCONNECTED","The match is frozen. Nothing has been reset.");
   Text("Body",disconnect.transform,"",new Vector2(1300,350),new Vector2(0,50),26);
   Button(disconnect.transform,"REASSIGN PLAYER 1","reassign1",new Vector2(-320,-185),new Vector2(520,60));Button(disconnect.transform,"REASSIGN PLAYER 2","reassign2",new Vector2(320,-185),new Vector2(520,60));
   Button(disconnect.transform,"MAIN MENU","main",new Vector2(-320,-330),new Vector2(520,60));menus.disconnectResume=Button(disconnect.transform,"RESUME MATCH","resume",new Vector2(320,-330),new Vector2(520,60));
   var result=Page(BilliardsScreen.Result,"MATCH COMPLETE");menus.resultText=Text("Body",result.transform,"",new Vector2(1250,250),new Vector2(0,50),40);
   menus.nextRackButton=Button(result.transform,"NEXT RACK • ALTERNATE BREAK","nextrack",new Vector2(0,-160),new Vector2(800,70));menus.rematchButton=Button(result.transform,"REMATCH • ALTERNATE BREAK","rematch",new Vector2(0,-160),new Vector2(800,70));Button(result.transform,"MAIN MENU","resultmain",new Vector2(0,-270),new Vector2(800,70));
   var winVfx=Rect("Win Feedback",result.transform,new Vector2(1460,870),Vector2.zero);var celebration=winVfx.AddComponent<BilliardsCelebration>();celebration.session=session;session.celebration=celebration;
   foreach(var page in menus.pages)if(page!=null){Navigation(page);page.SetActive(page==main);}
   var eventSystem=match.GetComponent<BilliardsHostIsolation>().modeEventSystem;
   var oldInput=eventSystem.GetComponent<StandaloneInputModule>();if(oldInput!=null)oldInput.enabled=false;
   var uiInput=eventSystem.GetComponent<InputSystemUIInputModule>()??eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();uiInput.AssignDefaultActions();
   EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
   Debug.Log("TB_LOCAL_MENUS_UPGRADED existing scene preserved");
  }
  public static void UpgradeBuild(){Upgrade();BilliardsBuilder.Build();}
 }
}








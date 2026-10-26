#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Michsky.UI.Heat;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using LWS.TruckBowling;
using LWS.TruckCornhole;
using LWS.TruckCheeseRolling;
using LWS.TruckHockey;
using LWS.TruckAirHockey;
using LWS.TruckWallball;
using LWS.TruckBilliards;

namespace LWS.TruckSports.Editor
{
    public static class SportsTruckCompatibilityAuthoring
    {
        public const string PrefabPath="Assets/TruckSports/Resources/TruckSports/SportsTruckCompatibilityModal.prefab";
        const string HeatPath="Assets/Heat - Complete Modern UI/Prefabs/UI Elements/Modal Window/Modal Window (Custom Content).prefab";
        static SportsBroadcastTheme Theme=>Resources.Load<SportsBroadcastTheme>("SportsBroadcastTheme");
        static void Guard(){if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode before explicit compatibility authoring.");if(Theme==null)throw new InvalidOperationException("The existing Sports broadcast theme is required.");}
        public static bool IsExcludedScene(string path)=>string.Equals(path?.Replace('\\','/'),LWS.TruckPedestrianPlowing.Editor.PlowAuthoring.Scene,StringComparison.OrdinalIgnoreCase)||string.Equals(path?.Replace('\\','/'),LWS.TruckPedestrianPlowing.Editor.PlowCabAuthoring.Scene,StringComparison.OrdinalIgnoreCase);
        public static string[] RegisteredTargets()=>SportsBuilder.SceneList().Skip(1).Where(path=>!IsExcludedScene(path)).ToArray();

        [MenuItem("Truck Sports/Customizer/Create Missing Compatibility Heat Modal")]
        public static void CreateMissingModal()
        {
            Guard();if(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath)!=null){Debug.Log("TRUCK_FIT_MODAL_PRESERVED "+PrefabPath);return;}
            var template=AssetDatabase.LoadAssetAtPath<GameObject>(HeatPath);if(template==null)throw new InvalidOperationException("Installed Heat custom-content modal is required.");
            Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));AssetDatabase.Refresh();var scene=EditorSceneManager.NewPreviewScene();GameObject root=null;
            try
            {
                root=new GameObject("Truck Sports compatibility",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));root.SetActive(false);SceneManager.MoveGameObjectToScene(root,scene);root.layer=5;
                var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=65;
                var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
                var copy=(GameObject)PrefabUtility.InstantiatePrefab(template,root.transform);copy.SetActive(true);var window=copy.GetComponent<ModalWindowManager>();window.useCustomContent=true;window.useLocalization=false;window.showCancelButton=window.showConfirmButton=false;window.closeOnConfirm=window.closeOnCancel=false;window.startBehaviour=ModalWindowManager.StartBehaviour.Enable;window.closeBehaviour=ModalWindowManager.CloseBehaviour.Disable;window.inputType=ModalWindowManager.InputType.Free;
                copy.GetComponent<Animator>().updateMode=AnimatorUpdateMode.UnscaledTime;
                var content=copy.transform.Find("Content");if(content==null)throw new InvalidOperationException("Installed Heat content transform changed.");
                foreach(var layout in content.GetComponents<LayoutGroup>())layout.enabled=false;
                foreach(Transform child in content)child.gameObject.SetActive(false);
                foreach(var hotkey in copy.GetComponentsInChildren<HotkeyEvent>(true))hotkey.enabled=false;
                var modal=root.AddComponent<SportsTruckCompatibilityModal>();modal.window=window;
                var card=SportsUIAuthoring.Rect("Truck fit broadcast card",content,new Vector2(1460,900),Vector2.zero);Plate(card,SportsBroadcastCanvas.Ink);
                modal.heading=Text(card.transform,"Heading","TRUCK FIT",new Vector2(1320,66),new Vector2(0,385),40,true);
                modal.roster=SportsUIAuthoring.Rect("Four driver reports",card.transform,new Vector2(1340,660),new Vector2(0,5));
                modal.rows=new SportsTruckCompatibilityModal.Row[4];
                for(int i=0;i<4;i++)
                {
                    var row=new SportsTruckCompatibilityModal.Row();modal.rows[i]=row;row.root=SportsUIAuthoring.Rect("Driver "+i,modal.roster.transform,new Vector2(1300,145),new Vector2(0,240-i*160));Plate(row.root,SportsBroadcastCanvas.Raised);
                    row.name=Text(row.root.transform,"Driver and stable ID","",new Vector2(940,34),new Vector2(-140,49),25,true);
                    row.fit=Text(row.root.transform,"Requested and effective body","",new Vector2(940,66),new Vector2(-140,-4),23);
                    row.status=Text(row.root.transform,"Retained choices notice","",new Vector2(940,29),new Vector2(-140,-51),20);
                    row.details=Button(row.root.transform,"DETAILS",new Vector2(510,33),new Vector2(225,48));row.customize=Button(row.root.transform,"CUSTOMIZE",new Vector2(510,-33),new Vector2(225,48));
                }
                modal.details=SportsUIAuthoring.Rect("Full fit reasons",card.transform,new Vector2(1300,620),new Vector2(0,5));
                modal.detailText=Text(modal.details.transform,"Paged complete reasons","",new Vector2(1240,570),Vector2.zero,27);modal.detailText.alignment=TextAlignmentOptions.TopLeft;modal.detailText.fontSizeMin=24;
                modal.pageCaption=Text(card.transform,"Page caption","",new Vector2(600,36),new Vector2(0,-341),22);modal.pageCaption.alignment=TextAlignmentOptions.Center;
                modal.previous=Button(card.transform,"PREVIOUS",new Vector2(-515,-341),new Vector2(260,48));modal.next=Button(card.transform,"NEXT",new Vector2(515,-341),new Vector2(260,48));
                modal.back=Button(card.transform,"DRIVER LIST",new Vector2(-320,-409),new Vector2(480,52));modal.close=Button(card.transform,"CLOSE",new Vector2(320,-409),new Vector2(480,52));
                foreach(var node in root.GetComponentsInChildren<Transform>(true))node.gameObject.layer=5;
                PrefabUtility.SaveAsPrefabAsset(root,PrefabPath);Debug.Log("TRUCK_FIT_MODAL_CREATED "+PrefabPath+"; installed Heat animation, four-driver pages, complete paged reasons, no EventSystem/input owner.");
            }
            finally{if(root!=null)UnityEngine.Object.DestroyImmediate(root);EditorSceneManager.ClosePreviewScene(scene);}
        }
        static void Plate(GameObject root,Color color){var image=root.AddComponent<Image>();image.color=color;image.raycastTarget=false;root.AddComponent<SportsBroadcastPlate>().cut=14;}
        static TMP_Text Text(Transform parent,string name,string value,Vector2 size,Vector2 position,int font,bool heading=false)
        {
            var text=SportsUIAuthoring.Text(name,parent,value,size,position,font);text.font=heading?Theme.headingFont:Theme.bodyFont;text.color=SportsBroadcastCanvas.Cream;text.richText=false;text.alignment=TextAlignmentOptions.MidlineLeft;text.fontSizeMin=font;text.fontSizeMax=font;return text;
        }
        static ButtonManager Button(Transform parent,string label,Vector2 position,Vector2 size)
        {
            var button=SportsUIAuthoring.Button(parent,label,"truckfit",position,size);UnityEngine.Object.DestroyImmediate(button.GetComponent<SportsMenuAction>());button.normalTextObj.font=Theme.bodyFont;button.normalTextObj.fontSizeMin=20;button.normalTextObj.fontSizeMax=23;button.gameObject.AddComponent<SportsBroadcastPlate>().cut=10;return button;
        }
        [MenuItem("Truck Sports/Customizer/Add Missing Sport Fit Shop Entry")]
        public static void AddShopContextEntry()
        {
            Guard();string path=SportsTruckCustomizerAuthoring.PrefabPath;var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(prefab==null)throw new InvalidOperationException("Author the existing reusable Heat shop first.");
            var contents=PrefabUtility.LoadPrefabContents(path);
            try
            {
                var shop=contents.GetComponent<SportsTruckCustomizerMenu>();if(shop.sportFit!=null){Debug.Log("TRUCK_FIT_SHOP_ENTRY_PRESERVED");return;}
                var caption=shop.previewCaption.rectTransform;
                if(caption.sizeDelta!=new Vector2(630,50)||caption.anchoredPosition!=new Vector2(-300,340))throw new InvalidOperationException("Customizer caption was edited; preserve it and author the optional SPORT FIT entry explicitly.");
                caption.sizeDelta=new Vector2(430,50);caption.anchoredPosition=new Vector2(-400,340);
                shop.sportFit=Button(contents.transform,"SPORT FIT",new Vector2(-80,340),new Vector2(180,44));shop.sportFit.gameObject.SetActive(false);
                shop.previewControls=contents.transform.Find("Preview controls")?.GetComponent<TMP_Text>();EditorUtility.SetDirty(shop);PrefabUtility.SaveAsPrefabAsset(contents,path);Debug.Log("TRUCK_FIT_SHOP_ENTRY_AUTHORED; original global Open remains context-free.");
            }
            finally{PrefabUtility.UnloadPrefabContents(contents);}
        }
        [MenuItem("Truck Sports/Customizer/Install Missing Fit Reports In Registered Scenes")]
        public static void InstallMissingRegisteredScenes()
        {
            Guard();if(Enumerable.Range(0,SceneManager.sceneCount).Any(index=>SceneManager.GetSceneAt(index).isDirty))throw new InvalidOperationException("Save current work before explicit registered-scene authoring.");CreateMissingModal();AddShopContextEntry();var previous=EditorSceneManager.GetSceneManagerSetup();
            try{foreach(string path in RegisteredTargets()){var scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Single);if(InstallOpenScene())EditorSceneManager.SaveScene(scene);}}
            finally{RestorePreviousSceneSetup(previous);}
        }
        public static bool ShouldRestoreSceneSetup(SceneSetup[] previous,bool batchMode)
        {
            if(previous!=null&&previous.Any(scene=>scene.isLoaded&&scene.isActive))return true;
            if(batchMode)return false;
            throw new InvalidOperationException("The previous interactive scene setup has no loaded active scene; preserve current saved work for explicit review.");
        }
        public static void RestorePreviousSceneSetup(SceneSetup[] previous)
        {
            if(ShouldRestoreSceneSetup(previous,Application.isBatchMode))EditorSceneManager.RestoreSceneManagerSetup(previous);
            else Debug.Log("TRUCK_FIT_BATCH_PRIOR_SETUP_EMPTY_PRESERVED; all authored scenes remain saved; leave current loaded scene.");
        }
        static T Find<T>()where T:Component=>SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<T>(true)).SingleOrDefault();
        public static bool InstallOpenScene()
        {
            Guard();if(IsExcludedScene(SceneManager.GetActiveScene().path)){Debug.Log("TRUCK_FIT_PROTECTED_PLOW_SKIPPED "+SceneManager.GetActiveScene().path);return false;}var existing=Find<SportsTruckCompatibilitySource>();if(existing!=null){if(existing.cornhole!=null)return SportsTruckCompatibilityCornholeAuthoring.Upgrade(existing);if(SportsTruckCompatibilityLocalOwnerAuthoring.Applies(existing.session))return SportsTruckCompatibilityLocalOwnerAuthoring.Upgrade(existing);if(existing.session is LWS.TruckSkeeBall.SkeeMatch)return SportsTruckCompatibilitySkeeAuthoring.Upgrade(existing);if(existing.cheese!=null)return SportsTruckCompatibilityCheeseAuthoring.Upgrade(existing);if(SportsTruckCompatibilityFixedOwnerAuthoring.Applies(existing.session))return SportsTruckCompatibilityFixedOwnerAuthoring.Upgrade(existing);if(existing.session is LWS.TruckCurling.CurlingMatch)return UpdateCurlingSetupTemplate(existing);if(existing.session is LWS.TruckTugOfWar.TugMatch)return UpgradeOwnedTugSetupBindings(existing);if(StaticFamily(existing))return UpgradeOwnedStaticSetupBindings(existing);Debug.Log("TRUCK_FIT_BINDING_PRESERVED "+SceneManager.GetActiveScene().path);return false;}
            var session=Find<SportsEventSession>();var bowling=Find<BowlingMatch>();var corn=Find<CornholeMatch>();var cheese=Find<CheeseMatch>();var hockey=Find<HockeyMatch>();var air=Find<AirHockeyMatch>();var wall=Find<WallballMatch>();var billiards=Find<BilliardsSession>();
            MonoBehaviour menu=session!=null?session.menus:bowling!=null?bowling.menus:corn!=null?corn.menus:cheese!=null?cheese.menus:hockey!=null?hockey.menus:air!=null?air.menus:wall!=null?wall.menus:billiards!=null?billiards.menus:null;
            if(menu==null)throw new InvalidOperationException("No proven Session/seven-legacy menu binding in "+SceneManager.GetActiveScene().path);
            // Curling has sixteen retained delivery actors, not sixteen setup
            // owners. Prove all fits before selecting the first original actor
            // as a read-only template, and before changing any scene object.
            var curlingTemplate=session is LWS.TruckCurling.CurlingMatch curling?CurlingSetupTemplate(curling):null;
            var staticMap=StaticSetupMap(session,hockey,air,billiards);
            var tugMap=session is LWS.TruckTugOfWar.TugMatch tug?ProvenTugSetupMap(tug):null;
            var fixedOwnerMap=SportsTruckCompatibilityFixedOwnerAuthoring.Applies(session)?SportsTruckCompatibilityFixedOwnerAuthoring.ProvenMap(session):null;
            var cheeseMap=cheese!=null?SportsTruckCompatibilityCheeseAuthoring.ProvenMap(cheese):null;
            var cornholeMap=corn!=null?SportsTruckCompatibilityCornholeAuthoring.ProvenMap(corn):null;
            var skeeTemplate=session is LWS.TruckSkeeBall.SkeeMatch skee?SportsTruckCompatibilitySkeeAuthoring.ProvenTemplate(skee):null;
            var terrierMap=session is LWS.TruckTerrierRacing.TerrierMatch terrier?SportsTruckCompatibilityLocalOwnerAuthoring.ProvenTerrierMap(terrier):null;
            var dockTemplate=session is LWS.TruckDockDiving.DockMatch dock?SportsTruckCompatibilityLocalOwnerAuthoring.ProvenDockTemplate(dock):null;
            var pages=menu is SportsMenuView common?common.pages:((BilliardsMenus)menu).pages;var page=pages[billiards!=null?(int)BilliardsScreen.Setup:0];
            // Own a new brief row above the existing card, leaving every original
            // subtitle, setup-info/rules writer and artwork rectangle untouched.
            PickSetupRow(page.transform,out Rect summaryRect,out Rect buttonRect);
            var owner=new GameObject("Truck fit setup adapter");SceneManager.MoveGameObjectToScene(owner,SceneManager.GetActiveScene());var source=owner.AddComponent<SportsTruckCompatibilitySource>();source.session=session;source.bowling=bowling;source.cornhole=corn;source.cheese=cheese;source.hockey=hockey;source.airHockey=air;source.wallball=wall;source.billiards=billiards;source.menu=menu;source.setupPage=page;source.setupInteraction=page.GetComponent<CanvasGroup>();if(source.setupInteraction==null)source.setupInteraction=page.AddComponent<CanvasGroup>();source.sportName=session!=null?session.Title:page.transform.Find("Title").GetComponent<TMP_Text>().text;source.catalog=Resources.Load<SportsTruckAppearanceCatalog>("SportsTruckAppearanceCatalog");
            source.participantHosts=new SportsTruckAppearanceHost[source.Participants.Length];
            for(int i=0;i<source.participantHosts.Length;i++)
            {
                if(staticMap!=null){source.participantHosts[i]=staticMap[i];continue;}
                if(tugMap!=null){source.participantHosts[i]=tugMap[i];continue;}
                if(fixedOwnerMap!=null){source.participantHosts[i]=fixedOwnerMap[i];continue;}
                if(cheeseMap!=null){source.participantHosts[i]=cheeseMap[i];continue;}
                if(cornholeMap!=null){source.participantHosts[i]=cornholeMap[i];continue;}
                if(terrierMap!=null){source.participantHosts[i]=terrierMap[i];continue;}
                if(dockTemplate!=null)continue;
                if(session is LWS.TruckCurling.CurlingMatch)continue;
                if(session!=null){var mapped=(session.appearanceHosts??Array.Empty<SportsTruckAppearanceHost>()).Where(host=>host!=null&&host.participantSlot==i).ToArray();source.participantHosts[i]=mapped.Length==1?mapped[0]:null;}
                else if(bowling!=null&&i<bowling.lanes.Length&&bowling.lanes[i]!=null&&bowling.lanes[i].truck!=null)source.participantHosts[i]=bowling.lanes[i].truck.appearanceHost;
                else if(cheese!=null&&i<cheese.trucks.Length)source.participantHosts[i]=OwnHost(cheese.trucks[i]);
                else if(hockey!=null&&i<hockey.trucks.Length)source.participantHosts[i]=OwnHost(hockey.trucks[i]);
                else if(air!=null&&i<air.trucks.Length)source.participantHosts[i]=OwnHost(air.trucks[i]);
                else if(wall!=null&&i<wall.appearanceHosts.Length&&i<wall.trucks.Length){var host=wall.appearanceHosts[i];source.participantHosts[i]=host!=null&&host.participantSlot==i&&wall.trucks[i]!=null&&host.transform==wall.trucks[i].transform?host:null;}
                else if(billiards!=null)source.participantHosts[i]=OwnHost(billiards.players[i].truck);
            }
            if(session is LWS.TruckTrailerPlug.TrailerPlugMatch trailer&&trailer.rig!=null)source.sharedTurnHost=TurnHost(trailer.rig.truck,session);
            else if(session is LWS.TruckGiantSwing.SwingMatch swing)source.sharedTurnHost=TurnHost(swing.truck,session);
            else if(session is LWS.TruckFieldGoal.FieldGoalMatch field)source.sharedTurnHost=TurnHost(field.truck,session);
            else if(session is LWS.TruckBeerPong.PongMatch pong)source.sharedTurnHost=TurnHost(pong.truck,session);
            else if(curlingTemplate!=null)source.sharedTurnHost=curlingTemplate;
            else if(skeeTemplate!=null)source.sharedTurnHost=skeeTemplate;
            else if(dockTemplate!=null)source.sharedTurnHost=dockTemplate;
            var presenter=owner.AddComponent<SportsTruckCompatibilityPresenter>();presenter.source=source;presenter.subtitle=Text(page.transform,"Truck fit setup subtitle","",summaryRect.size,summaryRect.center,21);presenter.originalSubtitle="";presenter.reportButton=Button(page.transform,"TRUCK FIT",buttonRect.center,buttonRect.size);SportsTruckCompatibilitySummaryBackingAuthoring.Upgrade(source);EditorUtility.SetDirty(source);EditorUtility.SetDirty(presenter);EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());Debug.Log("TRUCK_FIT_SETUP_AUTHORED "+SceneManager.GetActiveScene().path+"; explicit hosts="+source.participantHosts.Count(host=>host!=null)+", sharedTurn="+(source.sharedTurnHost!=null)+"; unmapped positions honestly retain original appearance.");return true;
        }
        static SportsTruckAppearanceHost OwnHost(Component actor)=>actor!=null?actor.GetComponent<SportsTruckAppearanceHost>():null;
        static SportsTruckAppearanceHost TurnHost(SportsEventTruck truck,SportsEventSession owner)=>truck!=null&&truck.session==owner&&truck.appearanceHost!=null&&truck.appearanceHost.transform==truck.transform?truck.appearanceHost:null;
        public static SportsTruckAppearanceHost[] ProvenTugSetupMap(LWS.TruckTugOfWar.TugMatch match)
        {
            if(match==null||match.trucks==null||match.trucks.Length!=8||match.trucks.Any(truck=>truck==null)||match.trucks.Distinct().Count()!=8||match.fragments==null||match.fragments.Length!=8||match.tugAppearanceHosts==null||match.tugAppearanceHosts.Length!=8||(match.appearanceHosts?.Length??0)!=0||match.setup.players.Length<8)throw new InvalidOperationException("Run the explicit Tug8 host author first; local8 and original actor/fragment arrays are required, inherited Session hosts stay empty.");
            var result=new SportsTruckAppearanceHost[match.setup.players.Length];
            for(int i=0;i<8;i++)
            {
                var truck=match.trucks[i];var host=match.tugAppearanceHosts[i];var rig=match.fragments[i];var fragment=host!=null?host.fragmentPresentation:null;var wheels=truck.GetComponent<SportsWheelMeshBinding>();
                if(truck.session!=match||truck.slot!=i||host==null||host!=truck.appearanceHost||host.transform!=truck.transform||host.participantSlot!=i||host.presentationParent!=truck.visuals||host.catalog==null||!host.catalog.Validate(out _)||host.compatibleBodies==null||host.compatibleBodies.Length<7||host.compatibleBodies.Any(fit=>fit==null||!fit.Valid)||host.wheels==null||host.wheels.Length!=4||!host.wheels.SequenceEqual(truck.wheels)||rig==null||rig.truck!=truck||rig.sourceBody!=null||rig.appearanceHost!=host||rig.fragmentPresentation!=fragment||fragment==null||fragment.host!=host||fragment.rig!=rig||rig.pieces==null||rig.pieces.Length!=12||wheels==null||wheels.truck!=truck||wheels.meshes==null||wheels.meshes.Length!=4||fragment.eventWheelPieceSources==null||!fragment.eventWheelPieceSources.SequenceEqual(wheels.meshes)||wheels.meshes.Any(source=>source==null)||wheels.meshes.Distinct().Count()!=4)throw new InvalidOperationException("Preserve unproven Tug physical root/slot/fragment/wheel ownership at "+i+".");
                foreach(var source in wheels.meshes)if(rig.pieces.Count(piece=>piece!=null&&piece.source==source&&piece.body!=null&&piece.collider!=null&&piece.renderer!=null&&piece.source.GetComponent<MeshFilter>()!=null&&piece.renderer.GetComponent<MeshFilter>()!=null&&piece.source.GetComponent<MeshFilter>().sharedMesh==piece.renderer.GetComponent<MeshFilter>().sharedMesh)!=1)throw new InvalidOperationException("Preserve custom Tug wheel-piece mapping at "+i+".");
                foreach(var body in host.catalog.bodies){var report=host.EvaluateDraft(new SportsTruckAppearanceData{bodyId=body.id});if(!fragment.Supports(body,out _)||!report.canApply||report.effectiveBodyId!=body.id)throw new InvalidOperationException("Tug setup cannot promise the actual body/recipe fit "+body.id+" at "+i+".");}
                // EvaluateDraft reads authored fits, even after original Burst
                // zeroes visuals scale. Never reset or prepare an actor here.
                result[i]=host;
            }
            return result;
        }
        public static bool CanUpgradeOwnedTugSetupBindings(SportsTruckCompatibilitySource source,SportsTruckAppearanceHost[] target)
        {
            if(source==null||!(source.session is LWS.TruckTugOfWar.TugMatch match)||target==null||target.Length!=source.Participants.Length||source.name!="Truck fit setup adapter"||source.menu!=match.menus||source.setupPage!=match.menus.pages[(int)SportsEventScreen.Setup]||source.sharedTurnHost!=null||source.participantHosts!=null&&source.participantHosts.Length!=0&&source.participantHosts.Length!=target.Length)throw new InvalidOperationException("Preserve custom Tug setup report ownership/menu/page/map shape.");
            if(source.participantHosts!=null&&source.participantHosts.SequenceEqual(target))return false;
            if(source.participantHosts!=null&&source.participantHosts.Any(host=>host!=null))throw new InvalidOperationException("Preserve custom/nonempty partial Tug setup maps; only an owned all-null map can upgrade.");return true;
        }
        static bool UpgradeOwnedTugSetupBindings(SportsTruckCompatibilitySource source)
        {
            var target=ProvenTugSetupMap((LWS.TruckTugOfWar.TugMatch)source.session);if(!CanUpgradeOwnedTugSetupBindings(source,target))return false;source.participantHosts=target;EditorUtility.SetDirty(source);EditorSceneManager.MarkSceneDirty(source.gameObject.scene);Debug.Log("TRUCK_FIT_TUG_LOCAL8_MAP_UPGRADED; exact local actors/wheel pieces and all body recipes; no preparation, profiles or layout changes.");return true;
        }
        [MenuItem("Truck Sports/Customizer/Upgrade Open Owned Unbound Tug Setup Report")]
        public static void UpgradeOpenSceneTugSetupBindings()
        {
            Guard();var source=Find<SportsTruckCompatibilitySource>();if(source==null||!(source.session is LWS.TruckTugOfWar.TugMatch))throw new InvalidOperationException("Open an already-authored original Tug setup report explicitly.");UpgradeOwnedTugSetupBindings(source);
        }
        static bool StaticFamily(SportsTruckCompatibilitySource source)=>source.hockey!=null||source.airHockey!=null||source.billiards!=null||source.session is LWS.TruckPoker.PokerMatch;
        public static SportsTruckAppearanceHost[] ProvenStaticSetupMap(SportsTruckCompatibilitySource source)=>StaticSetupMap(source.session,source.hockey,source.airHockey,source.billiards);
        static SportsTruckAppearanceHost[] StaticSetupMap(SportsEventSession session,HockeyMatch hockey,AirHockeyMatch air,BilliardsSession billiards)
        {
            SportsTruckAppearanceHost[] hosts;Transform[] actors;int count,slots;
            if(hockey!=null){count=slots=6;hosts=hockey.appearanceHosts;if(hockey.players.Length!=count||hockey.trucks.Length!=count||hockey.trucks.Any(truck=>truck==null))throw new InvalidOperationException("Original six Hockey participants/trucks required.");actors=hockey.trucks.Select(truck=>truck.transform).ToArray();}
            else if(air!=null){count=slots=2;hosts=air.appearanceHosts;if(air.players.Length!=count||air.trucks.Length!=count||air.trucks.Any(truck=>truck==null))throw new InvalidOperationException("Original two Air Hockey participants/trucks required.");actors=air.trucks.Select(truck=>truck.transform).ToArray();}
            else if(billiards!=null){count=slots=2;hosts=billiards.appearanceHosts;if(billiards.players.Length!=count||billiards.match==null||billiards.match.player==null||billiards.match.ai==null||billiards.players[0].truck!=billiards.match.player||billiards.players[1].truck!=billiards.match.ai)throw new InvalidOperationException("Billiards setup must retain its exact physical side0/1 rigs, including HotSeat.");actors=billiards.players.Select(player=>player.truck.transform).ToArray();}
            else if(session is LWS.TruckPoker.PokerMatch poker){count=8;slots=poker.setup.players.Length;hosts=poker.appearanceHosts;actors=poker.avatars;if(actors==null||actors.Length!=count||slots<count)throw new InvalidOperationException("Original eight Poker participant avatars required.");}
            else return null;
            if(hosts==null||hosts.Length!=count||actors.Any(actor=>actor==null)||actors.Distinct().Count()!=count)throw new InvalidOperationException("Run the explicit static18 host author first; exact owner array required.");
            var result=new SportsTruckAppearanceHost[slots];
            for(int i=0;i<count;i++)
            {
                var host=hosts[i];var actor=actors[i];if(host==null||host.transform!=actor||host.participantSlot!=i||actor.GetComponents<SportsTruckAppearanceHost>().Length!=1||host.presentationParent==null||host.presentationParent==actor||!host.presentationParent.IsChildOf(actor)||host.catalog==null||!host.catalog.Validate(out _)||host.fragmentPresentation!=null||host.wheels==null||host.wheels.Length!=0||host.staticWheelPoses==null||host.staticWheelPoses.Length!=4||host.staticWheelPoses.Any(pose=>pose==null||!pose.ValidFor(actor))||host.compatibleBodies==null||host.compatibleBodies.Length==0||host.compatibleBodies.Any(fit=>fit==null||!fit.Valid)||!host.EvaluateDraft(new SportsTruckAppearanceData()).canApply)throw new InvalidOperationException("Preserve custom/unproven static setup host root, slot, wheel or fit at "+i+".");
                result[i]=host;
            }
            return result;
        }
        public static bool CanUpgradeOwnedStaticSetupBindings(SportsTruckCompatibilitySource source,SportsTruckAppearanceHost[] target)
        {
            if(source==null||!StaticFamily(source)||target==null||target.Length!=source.Participants.Length||source.name!="Truck fit setup adapter"||source.sharedTurnHost!=null)throw new InvalidOperationException("Preserve custom setup report ownership.");
            MonoBehaviour menu=source.hockey!=null?source.hockey.menus:source.airHockey!=null?source.airHockey.menus:source.billiards!=null?source.billiards.menus:source.session.menus;var pages=menu is SportsMenuView common?common.pages:((BilliardsMenus)menu).pages;int page=source.billiards!=null?(int)BilliardsScreen.Setup:0;
            if(source.menu!=menu||source.setupPage!=pages[page]||source.participantHosts!=null&&source.participantHosts.Length!=0&&source.participantHosts.Length!=target.Length)throw new InvalidOperationException("Preserve custom setup report menu/page/map shape.");
            var current=source.participantHosts;if(current!=null&&current.SequenceEqual(target))return false;
            if(current!=null&&current.Any(host=>host!=null))throw new InvalidOperationException("Preserve custom/nonempty partial setup mappings; only an all-null owned map can upgrade.");return true;
        }
        static bool UpgradeOwnedStaticSetupBindings(SportsTruckCompatibilitySource source)
        {
            var target=ProvenStaticSetupMap(source);if(!CanUpgradeOwnedStaticSetupBindings(source,target))return false;source.participantHosts=target;EditorUtility.SetDirty(source);EditorSceneManager.MarkSceneDirty(source.gameObject.scene);Debug.Log("TRUCK_FIT_STATIC_MAP_UPGRADED "+source.gameObject.scene.path+"; explicit physical slots/roots and pure fit authority; layout/profiles/models untouched.");return true;
        }
        [MenuItem("Truck Sports/Customizer/Upgrade Open Owned Unbound Static Setup Report")]
        public static void UpgradeOpenSceneStaticSetupBindings()
        {
            Guard();if(IsExcludedScene(SceneManager.GetActiveScene().path))return;var source=Find<SportsTruckCompatibilitySource>();if(source==null||!StaticFamily(source))throw new InvalidOperationException("Open an already-authored Hockey/Air/Billiards/Poker setup report explicitly.");UpgradeOwnedStaticSetupBindings(source);
        }
        public static SportsTruckAppearanceHost CurlingSetupTemplate(LWS.TruckCurling.CurlingMatch match)
        {
            if(match==null||match.stones==null||match.stones.Length!=16||match.stones.Any(stone=>stone==null||stone.match!=match||stone.truck==null)||match.stones.Select(stone=>stone.truck).Distinct().Count()!=16)throw new InvalidOperationException("Curling setup fit requires the exact sixteen original delivery actors.");
            var hosts=match.stones.Select(stone=>TurnHost(stone.truck,match)).ToArray();var template=hosts[0];
            if(template==null||hosts.Any(host=>host==null||!match.appearanceHosts.Contains(host)||!EquivalentSetupFits(template,host)))throw new InvalidOperationException("Curling delivery hosts are missing or have different setup fits. Preserve actual delivery bindings; a common setup appearance cannot be promised.");
            return template;
        }
        static bool UpdateCurlingSetupTemplate(SportsTruckCompatibilitySource source)
        {
            var match=source.session as LWS.TruckCurling.CurlingMatch;var template=CurlingSetupTemplate(match);
            if(source.name!="Truck fit setup adapter"||source.menu!=match.menus||source.participantHosts==null||source.participantHosts.Any(host=>host!=null)||source.sharedTurnHost!=null&&source.sharedTurnHost!=template)throw new InvalidOperationException("Preserve custom Curling setup report bindings for explicit review.");
            if(source.sharedTurnHost==template)return false;source.sharedTurnHost=template;EditorUtility.SetDirty(source);EditorSceneManager.MarkSceneDirty(source.gameObject.scene);Debug.Log("TRUCK_FIT_CURLING_TEMPLATE_UPDATED equivalent16 original delivery hosts; no actor preparation or UI layout changes.");return true;
        }
        public static bool EquivalentSetupFits(SportsTruckAppearanceHost first,SportsTruckAppearanceHost other)
        {
            if(first==null||other==null||first.catalog==null||first.catalog!=other.catalog||first.fragmentPresentation!=null||other.fragmentPresentation!=null||first.wheels.Length!=4||other.wheels.Length!=4||first.wheels.Any(wheel=>wheel==null||wheel.wheel.rotatingContainer==null)||other.wheels.Any(wheel=>wheel==null||wheel.wheel.rotatingContainer==null)||first.originalBodyRenderers.Length!=other.originalBodyRenderers.Length||first.originalWheelRenderers.Length!=other.originalWheelRenderers.Length)return false;
            if(!first.catalog.Validate(out _))return false;for(int i=0;i<4;i++)if(!Near(first.wheels[i].wheel.radius,other.wheels[i].wheel.radius)||!Near(first.wheels[i].wheel.width,other.wheels[i].wheel.width))return false;
            if(!(first.modeOwnedSlots??Array.Empty<string>()).OrderBy(value=>value,StringComparer.Ordinal).SequenceEqual((other.modeOwnedSlots??Array.Empty<string>()).OrderBy(value=>value,StringComparer.Ordinal)))return false;
            var a=first.compatibleBodies;var b=other.compatibleBodies;if(a==null||b==null||a.Length!=b.Length||a.Any(fit=>fit==null||!fit.Valid)||b.Any(fit=>fit==null||!fit.Valid)||a.Select(fit=>fit.bodyId).Distinct().Count()!=a.Length||b.Select(fit=>fit.bodyId).Distinct().Count()!=b.Length)return false;
            foreach(var fit in a)
            {
                var compare=b.SingleOrDefault(item=>item.bodyId==fit.bodyId);if(compare==null||fit.notice!=compare.notice||!Near(fit.position,compare.position)||!Near(fit.euler,compare.euler)||!Near(fit.scale,compare.scale))return false;
                var paint=fit.paintSlots??Array.Empty<SportsTruckAppearanceCompatibility.PaintSlot>();var paintOther=compare.paintSlots??Array.Empty<SportsTruckAppearanceCompatibility.PaintSlot>();if(paint.Length!=paintOther.Length)return false;for(int i=0;i<paint.Length;i++)if(paint[i].rendererIndex!=paintOther[i].rendererIndex||paint[i].materialIndex!=paintOther[i].materialIndex)return false;
                var wheels=fit.wheelGeometry??Array.Empty<SportsTruckAppearanceCompatibility.WheelGeometry>();var wheelsOther=compare.wheelGeometry??Array.Empty<SportsTruckAppearanceCompatibility.WheelGeometry>();if(wheels.Length!=wheelsOther.Length)return false;for(int i=0;i<wheels.Length;i++){var wheel=wheels[i];var compareWheel=wheelsOther[i];if(wheel==null||compareWheel==null||wheel.physicalWheel!=compareWheel.physicalWheel||!Near(wheel.center,compareWheel.center)||!Near(wheel.euler,compareWheel.euler)||!Near(wheel.diameter,compareWheel.diameter)||!Near(wheel.visualDiameter,compareWheel.visualDiameter)||!(wheel.rendererIndices??Array.Empty<int>()).SequenceEqual(compareWheel.rendererIndices??Array.Empty<int>()))return false;}
            }
            // Public pure authority also proves the actual safe renderer/wheel
            // bindings. No profile lookup, preparation or model creation occurs.
            foreach(var body in first.catalog.bodies){var draft=new SportsTruckAppearanceData{bodyId=body.id};if(!SameReport(first.EvaluateDraft(draft),other.EvaluateDraft(draft)))return false;}
            foreach(var item in first.catalog.cosmetics){var draft=new SportsTruckAppearanceData{bodyId=first.catalog.defaultBodyId};draft.selections.Add(new SportsTruckCosmeticSelection{slotId=item.slotId,itemId=item.id});if(!SameReport(first.EvaluateDraft(draft),other.EvaluateDraft(draft)))return false;}
            return first.EvaluateDraft(new SportsTruckAppearanceData()).canApply;
        }
        static bool SameReport(SportsTruckAppearanceCompatibilityReport a,SportsTruckAppearanceCompatibilityReport b)=>a.canApply==b.canApply&&a.effectiveBodyId==b.effectiveBodyId&&a.notice==b.notice&&(a.unavailableSlots??Array.Empty<string>()).SequenceEqual(b.unavailableSlots??Array.Empty<string>());
        // Native world-to-local bounds introduce small float roundoff across
        // otherwise identical retained actors; .1mm/.0001deg is the whole guard.
        static bool Near(float a,float b)=>float.IsFinite(a)&&float.IsFinite(b)&&Mathf.Abs(a-b)<=.0001f;
        static bool Near(Vector3 a,Vector3 b)=>Near(a.x,b.x)&&Near(a.y,b.y)&&Near(a.z,b.z);
        public static void PickSetupRow(Transform page,out Rect summary,out Rect button)
        {
            // Reviewed strips only, within the1080 reference screen. Hockey's
            // saved title reaches y459; y492 leaves an11px button/title gap.
            // The bottom strip is a fallback for authored high headings/logos.
            var failures=new List<string>();foreach(float y in new[]{480f,492f,-492f})
            {
                summary=new Rect(-560,y-21,1040,42);button=new Rect(520,y-22,180,44);string obstruction=RowObstruction(page,summary,button);
                if(obstruction==null){Debug.Log("TRUCK_FIT_SAFE_ROW page="+page.name+" y="+y+" summary="+summary+" button="+button+"; original rectangles preserved;8px clearance.");return;}failures.Add("y"+y+": "+obstruction);
            }
            throw new InvalidOperationException("Preserve authored setup layout: all reviewed fit strips are obstructed. "+string.Join("; ",failures));
        }
        static string RowObstruction(Transform page,Rect summary,Rect button)
        {
            if(summary.xMin< -952||summary.xMax>952||summary.yMin< -532||summary.yMax>532||button.xMin< -952||button.xMax>952||button.yMin< -532||button.yMax>532)return "outside safe screen bounds";
            foreach(var graphic in page.GetComponentsInChildren<Graphic>(true))
            {
                if(!graphic.gameObject.activeSelf||graphic.transform==page||graphic.name=="Card"||graphic.name=="Accent")continue;
                var corners=new Vector3[4];graphic.rectTransform.GetWorldCorners(corners);var points=corners.Select(page.InverseTransformPoint).ToArray();var rect=Rect.MinMaxRect(points.Min(point=>point.x)-8,points.Min(point=>point.y)-8,points.Max(point=>point.x)+8,points.Max(point=>point.y)+8);
                if(rect.Overlaps(summary)||rect.Overlaps(button))return graphic.name+" with clearance "+rect;
            }
            return null;
        }
    }
}
#endif

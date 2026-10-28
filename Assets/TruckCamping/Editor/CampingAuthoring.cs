using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using PixelCrushers;
using LWS.TruckSports;
using LWS.TruckSports.Editor;
using LWS.TruckMechanicalBull.Editor;
namespace LWS.TruckCamping.Editor
{
    public static class CampingAuthoring
    {
        public const string Scene="Assets/TruckCamping/Scenes/TruckCamping.unity";
        [MenuItem("Truck Sports/Impossible Games/Create NEW Truck Camping Shared Wilderness")]
        public static void Create()
        {
            var match=Begin<CampingMatch>("TruckCamping",Scene);Finish(match,"TruckCamping");
        }
        // PetPals uses this EXACT scene composition and the same world prefab.
        public static T Begin<T>(string folder,string scene) where T:CampingMatch
        {
            ImpossibleBalanceArt.GuardNew(scene);
            var match=SportsEventAuthoring.Begin<T>(folder);match.setup.count=4;match.NormalizeSetup();
            var shared=CampingWildernessAuthoring.RequireSharedPrefab();match.world=((GameObject)PrefabUtility.InstantiatePrefab(shared)).GetComponent<CampingWilderness>();match.world.session=match;
            match.explorers=new CampExplorer[4];
            for(int i=0;i<4;i++)
            {
                var planar=new Vector2((i-1.5f)*14,-34);var truck=ImpossibleBalanceArt.Tractor(match,i,new Vector3(planar.x,0,planar.y));
                float low=truck.wheels.Min(w=>truck.transform.InverseTransformPoint(w.transform.position).y-w.wheel.radius-w.spring.maxLength);
                var explorer=truck.gameObject.AddComponent<CampExplorer>();explorer.match=match;explorer.truck=truck;explorer.slot=i;explorer.spawn=new Vector3(planar.x,CampingWildernessAuthoring.HeightAt(planar)+.2f-low,planar.y);truck.transform.position=explorer.spawn;
                explorer.attachedBodies=truck.GetComponentsInChildren<Rigidbody>(true).Where(b=>b!=truck.body).ToArray();FishingTools(explorer,match.world.transform,i);match.explorers[i]=explorer;
                var sleep=truck.gameObject.AddComponent<CampSleepPresentation>();sleep.explorer=explorer;sleep.letters=new TextMesh[3];for(int z=0;z<3;z++){sleep.letters[z]=SportsEventAuthoring.Label("Owner "+i+" sleepy floating Z "+z,"Z",explorer.spawn,.1f,Color.white);sleep.letters[z].transform.SetParent(match.world.transform,true);sleep.letters[z].gameObject.AddComponent<SportsPerCameraBillboard>();sleep.letters[z].gameObject.SetActive(false);}
            }
            match.broadcast=SportsEventAuthoring.Camera("Shared Camp Haul woodland exploration broadcast");match.broadcast.view.farClipPlane=2500;match.broadcast.Pose(match.explorers[0].spawn,new Vector3(0,19,-30),60,true);match.cameras=new[]{match.broadcast.view};
            ImpossibleBalanceArt.UI(match);match.menus.standings.gameObject.SetActive(false);
            match.drivingViews=SportsDrivingViewsAuthoring.Install(match,match.explorers.Select(e=>e.truck).ToArray(),match.broadcast,folder);match.drivingViews.defaultMode=1;match.drivingViews.independentViewModes=true;
            foreach(var view in match.drivingViews.rally.views)view.view.farClipPlane=2500;
            match.feedback.clips=(AudioClip[])LWS.TruckHockey.Editor.HockeyAudioAuthoring.Create().Clone();
            Storage(match,folder);UI(match);Help(match);
            return match;
        }
        public static void Finish(CampingMatch match,string folder)
        {
            SportsEventAuthoring.Save(folder);SportsTruckAppearanceHostAuthoring.BindNewImpossibleScene(match,match.explorers.Select(e=>e.truck).ToArray());
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(match.gameObject.scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(match.gameObject.scene);AssetDatabase.SaveAssets();
        }
        static void FishingTools(CampExplorer explorer,Transform parent,int index)
        {
            var wood=CampingWildernessAuthoring.Wood;var rod=CampingWildernessAuthoring.Part(parent,"Owner "+index+" cedar fishing pole",Vector3.zero,new Vector3(.08f,.08f,5.2f),wood);explorer.rod=rod.transform;
            var bobber=ImpossibleBalanceArt.Part(parent,"Owner "+index+" actual bite bobber",CampingWildernessAuthoring.Round,ImpossibleBalanceArt.Material("TruckCamping","Camp Haul cream red bobber",new Color(.85f,.23f,.13f)),Vector3.zero,new Vector3(.3f,.4f,.3f));explorer.bobber=bobber.transform;
            var line=new GameObject("Owner "+index+" fishing line").AddComponent<LineRenderer>();line.transform.SetParent(parent,false);line.sharedMaterial=wood;line.widthMultiplier=.025f;line.positionCount=2;explorer.fishingLine=line;
            explorer.catchSplash=CampingWildernessAuthoring.Particles(bobber.transform,"Actual bobber catch splash",new Color(.6f,.83f,.87f,.7f),.08f,0,.6f);var emission=explorer.catchSplash.emission;emission.SetBursts(new[]{new ParticleSystem.Burst(0,18)});
            rod.SetActive(false);bobber.SetActive(false);line.enabled=false;
        }
        static void Storage(CampingMatch match,string folder)
        {
            var store=match.gameObject.AddComponent<DiskSavedGameDataStorer>();store.storeSaveFilesIn=DiskSavedGameDataStorer.BasePath.Custom;store.customPath="";store.encrypt=false;
            var adapter=match.gameObject.AddComponent<SportsWorkshopPersistence>();adapter.stateProvider=match;adapter.storage=store;adapter.modeId=folder;match.persistence=adapter;
        }
        static void UI(CampingMatch match)
        {
            var pause=match.menus.pages[(int)SportsEventScreen.Pause].transform;
            foreach(var action in pause.GetComponentsInChildren<SportsMenuAction>(true))if(new[]{"resume","help","settings","restart","front"}.Contains(action.action))action.GetComponent<RectTransform>().anchoredPosition+=Vector2.left*350;
            SportsUIAuthoring.Button(pause,"SAVE EXPEDITION","camp-save",new Vector2(355,175),new Vector2(570,54));SportsUIAuthoring.Button(pause,"RESTORE EXPEDITION","camp-load",new Vector2(355,100),new Vector2(570,54));SportsUIAuthoring.Button(pause,"FINISH TOGETHER","camp-finish",new Vector2(355,25),new Vector2(570,54));
            match.persistence.statusLabel=SportsUIAuthoring.Text("Mode-local expedition save status",pause,"Pause outside care interactions to save the real expedition. Pet identity also autosaves separately.",new Vector2(1300,70),new Vector2(0,-340),22);
            match.photoExit=SportsUIAuthoring.Button(match.menus.hud.transform,"RETURN TO WALK","camp-photo-exit",new Vector2(0,-365),new Vector2(520,48)).gameObject;match.photoExit.SetActive(false);
            foreach(var replay in match.menus.replayButtons)replay.SetActive(false);
            var label=SportsUIAuthoring.Text("Cooperative wilderness activity reminder",match.menus.hud.transform,"STOP · ACTION FOR CAMP ACTIVITIES",new Vector2(800,35),new Vector2(0,445),21);label.color=new Color(.8f,.92f,.81f);
        }
        static void Help(CampingMatch match)
        {
            var page=match.menus.pages[(int)SportsEventScreen.Help];var text=page.transform.Find("Rules and controls").GetComponent<TMPro.TMP_Text>();var pages=new List<string>();string current="";
            foreach(var paragraph in match.Help.Split(new[]{"\n\n"},StringSplitOptions.None)){if(current.Length+paragraph.Length>1000&&current.Length>0){pages.Add(current);current="";}current+=(current.Length==0?"":"\n\n")+paragraph;}if(current.Length>0)pages.Add(current);
            var view=page.AddComponent<CampHelpPages>();view.pages=pages.ToArray();view.text=text;text.fontSizeMin=22;text.fontSizeMax=25;text.rectTransform.sizeDelta=new Vector2(1290,575);text.rectTransform.anchoredPosition=new Vector2(0,20);text.alignment=TMPro.TextAlignmentOptions.TopLeft;
            view.previous=SportsUIAuthoring.Button(page.transform,"PREVIOUS","camp-owned-help-prev",new Vector2(-490,-330),new Vector2(300,42));view.next=SportsUIAuthoring.Button(page.transform,"NEXT","camp-owned-help-next",new Vector2(490,-330),new Vector2(300,42));
            UnityEngine.Object.DestroyImmediate(view.previous.GetComponent<SportsMenuAction>());UnityEngine.Object.DestroyImmediate(view.next.GetComponent<SportsMenuAction>());view.number=SportsUIAuthoring.Text("Help page number",page.transform,"1 / "+pages.Count,new Vector2(180,38),new Vector2(0,-330),23);
        }
    }
}

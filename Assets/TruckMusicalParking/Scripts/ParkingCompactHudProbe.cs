#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using LWS.TruckSports;
using LWS.TruckBowling;

namespace LWS.TruckMusicalParking
{
    public sealed class ParkingCompactHudProbe:MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-tsParkingCompactHud")<0)return;var root=new GameObject("Explicit compact Parking Windows acceptance");DontDestroyOnLoad(root);root.AddComponent<SportsProbeInputIsolation>();root.AddComponent<ParkingCompactHudProbe>();}
        ParkingMatch match;string folder;int checks,errors;IDisposable preferences;float deadline;bool done,uiFixtures;InputSettings.BackgroundBehavior background;Gamepad[] pads=new Gamepad[4];readonly StringBuilder log=new StringBuilder();
        void Check(bool value,string message){checks++;if(!value)errors++;log.AppendLine((value?"PASS ":"FAIL ")+message);File.WriteAllText(Path.Combine(folder,"progress.txt"),log.ToString());}
        void Error(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception){errors++;log.AppendLine("ERROR "+message+"\n"+stack);}}
        IEnumerator Start()
        {
            var args=Environment.GetCommandLineArgs();int output=Array.IndexOf(args,"-tsParkingOutput");folder=output>=0?args[output+1]:SportsEvidencePaths.Folder("PlaytestRepair/ParkingCompact");Directory.CreateDirectory(folder);deadline=Time.realtimeSinceStartup+400;preferences=SportsPrefs.IsolateValidation("ParkingCompactWindows");Application.logMessageReceived+=Error;
            background=InputSystem.settings.backgroundBehavior;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            for(int i=0;i<4;i++){pads[i]=InputSystem.AddDevice<Gamepad>();InputSystem.EnableDevice(pads[i]);}
            yield return SportsProbeScene.Ensure<ParkingMatch>(SportsNavigation.Parking);yield return null;match=FindAnyObjectByType<ParkingMatch>();
            uiFixtures=true;
            foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080)})
            {
                Screen.SetResolution(size.x,size.y,false);yield return new WaitForSecondsRealtime(.3f);
                for(int humans=0;humans<=4;humans++)foreach(bool ai in new[]{false,true})
                {
                    match.SetupScreen();match.AllBots();SportsCameraOptions.Set(ai);for(int i=0;i<humans;i++){match.setup.players[i].source=BowlingSource.WholeController;match.setup.players[i].pad=pads[i];}
                    match.drivingViews.Select(humans==0?0:1);Check(match.StartMatch(),"saved scene starts humans="+humans+" AI views="+ai+" focus="+Application.isFocused+" devicesReady="+match.DevicesReady);Time.timeScale=0;yield return new WaitForSecondsRealtime(.3f);CheckCards();
                    // Explicit eliminated-state UI fixture, separate from the autonomous match below.
                    match.active[1]=false;yield return new WaitForSecondsRealtime(.2f);CheckCards();yield return Capture("icons-"+size.x+"-humans"+humans+"-ai"+ai);
                }
            }
            yield return BayGraphics();
            uiFixtures=false;match.SetupScreen();match.AllBots();match.drivingViews.Select(0);foreach(var p in match.setup.players)p.difficulty=BowlingDifficulty.Hard;
            Check(match.StartMatch(),"actual eight-AI match starts without presentation fixtures");Time.timeScale=4;float until=Time.realtimeSinceStartup+220;int last=0;
            while(!match.Finished&&Time.realtimeSinceStartup<until){if(last!=match.Round){last=match.Round;yield return new WaitForEndOfFrame();CheckCards();}yield return null;}
            Check(match.Finished&&match.Winner>=0&&match.Remaining==1&&match.TotalCaptures>0,"actual physical captures and eliminations produce one winner");log.AppendLine("MATCH rounds="+match.Round+" captures="+match.TotalCaptures+" winner="+match.Winner+" seed="+match.Seed);
            match.SetupScreen();Check(match.StartMatch(),"rematch starts");Time.timeScale=0;yield return new WaitForSecondsRealtime(.3f);Check(match.active.All(a=>a),"rematch restores all eight active competitors");CheckCards();match.SetupScreen();SportsNavigation.FrontEnd();yield return null;yield return null;Check(FindAnyObjectByType<ParkingMatch>()==null,"return to menu cleans up scene presentation");Finish();
        }
        void CheckCards()
        {
            Canvas.ForceUpdateCanvases();var root=match.menus.hud.transform.Find("Broadcast participant cards");Check(root!=null&&root.gameObject.activeInHierarchy,"one competition icon group visible in current camera mode");Check(!match.menus.header.transform.parent.gameObject.activeSelf&&!match.menus.status.gameObject.activeSelf,"obsolete top and bottom bars stay hidden");
            Check(root.Find("Elimination phase backing")!=null,"compact phase text remains contrasted in bright weather");
            var rect=(RectTransform)root;var corners=new Vector3[4];rect.GetWorldCorners(corners);Check(corners.All(c=>c.x>=0&&c.y>=0&&c.x<=Screen.width&&c.y<=Screen.height),"compact group stays inside actual screen at"+Screen.width);
            Check(rect.anchorMin==new Vector2(.5f,match.cameras.Count(c=>c!=null&&c.enabled)>1?.5f:0),"single views use bottom icons; multiple views use the central intersection");
            for(int i=0;i<8;i++)
            {
                var card=root.Find("Participant card "+i);var label=card.Find("Name").GetComponent<TMP_Text>();var image=card.Find("Actual truck portrait").GetComponent<UnityEngine.UI.Image>();label.ForceMeshUpdate();
                Check(card.gameObject.activeInHierarchy&&label.text==match.setup.players[i].name&&image.sprite!=null,"actual competitor icon/name association "+i);
                Check(match.active[i]?image.color.a>.9f:image.color.r==image.color.g&&image.color.g==image.color.b&&image.color.a<.7f,"gray elimination status matches actual competitor "+i);
            }
            Check(match.menus.viewTexts.All(t=>!t.gameObject.activeInHierarchy),"no duplicated player-text blocks in split views");
        }
        IEnumerator Capture(string name){yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(folder,name+".png"));yield return new WaitForSecondsRealtime(.1f);}
        IEnumerator BayGraphics()
        {
            Check(match.bays.All(b=>b.GetComponent<ParkingBayAppearance>()!=null),"actual saved scene binds all eight numbered bay graphics");
            if(match.bays.Any(b=>b.GetComponent<ParkingBayAppearance>()==null))yield break;
            Screen.SetResolution(1920,1080,false);match.SetupScreen();match.AllBots();match.drivingViews.Select(0);Check(match.StartMatch(),"world bay presentation fixture starts");Time.timeScale=0;
            // These direct bay states exercise graphics, not competitor captures.
            match.bays[0].SetOpen(true);match.bays[1].Secure(2,match.setup.players[2].color,match.setup.players[2].name);yield return new WaitForSecondsRealtime(.3f);
            Check(match.bays[0].GetComponent<ParkingBayAppearance>().stateText.text=="OPEN"&&match.bays[1].GetComponent<ParkingBayAppearance>().stateText.text=="SECURED"&&match.bays[2].GetComponent<ParkingBayAppearance>().stateText.text=="CLOSED","world open/secured/closed graphics follow authoritative bay states");
            Check(match.transform.Find("Parking world bay graphics").GetComponentsInChildren<Canvas>(true).Length==1,"all eight world markers share one canvas");
            Check(match.bays.All(b=>b.GetComponentsInChildren<Collider>(true).Length==0),"visual bay upgrades add no collisions or capture obstacles");yield return Capture("bay-shared-normal-1920");
            var framing=match.arenaCamera.broadcastFraming;bool framed=framing!=null&&framing.enabled;bool fitted=match.framing!=null&&match.framing.enabled;bool enabled=match.enabled,ortho=match.arenaCamera.view.orthographic;
            try
            {
                match.enabled=false;if(framing!=null)framing.enabled=false;if(match.framing!=null)match.framing.enabled=false;
                match.arenaCamera.view.orthographic=false;match.arenaCamera.PoseRaw(new Vector3(0,.12f,-11.45f),new Vector3(0,18,-18),50,true);yield return new WaitForSecondsRealtime(.2f);
                foreach(var bay in match.bays.Take(3))
                {
                    var p=bay.GetComponent<ParkingBayAppearance>();var corners=new Vector3[4];p.plate.GetWorldCorners(corners);var projected=corners.Select(v=>match.arenaCamera.view.WorldToViewportPoint(v)).ToArray();float width=projected.Max(v=>v.x)-projected.Min(v=>v.x);
                    Check(projected.All(v=>v.z>0&&v.x>=0&&v.x<=1&&v.y>=0&&v.y<=1)&&width*Screen.width>=80,"explicit close-up camera frames readable world bay "+p.number);
                }
                yield return Capture("bay-world-graphics-camera-fixture-1920");
            }
            finally{match.arenaCamera.view.orthographic=ortho;if(match.framing!=null)match.framing.enabled=fitted;if(framing!=null)framing.enabled=framed;match.enabled=enabled;}
            match.SetupScreen();match.AllBots();match.setup.players[0].source=BowlingSource.WholeController;match.setup.players[0].pad=pads[0];SportsCameraOptions.Set(false);match.drivingViews.Select(2);Check(match.StartMatch(),"actual cab camera starts bay-visibility fixture");Time.timeScale=0;
            var first=match.bays[0];first.SetOpen(true);match.trucks[0].Prepare(first.transform.position-first.transform.forward*18+Vector3.up*.55f,Quaternion.LookRotation(first.transform.forward),false);match.trucks[0].body.isKinematic=true;Physics.SyncTransforms();match.drivingViews.Tick(1);yield return new WaitForSecondsRealtime(.3f);
            var point=match.cameras[0].WorldToViewportPoint(first.GetComponent<ParkingBayAppearance>().plate.position);Check(point.z>0&&point.x>0&&point.x<1&&point.y>0&&point.y<1,"numbered bay marker projects inside the actual owned cab view");yield return Capture("bay-cab-approach-camera-fixture-1920");
            match.SetupScreen();Check(match.StartMatch(),"bay graphics rematch starts");Time.timeScale=0;yield return new WaitForSecondsRealtime(.2f);Check(match.bays.Where(b=>b.gameObject.activeSelf).All(b=>b.Owner<0&&!b.Open&&b.GetComponent<ParkingBayAppearance>().stateText.text=="CLOSED"),"rematch restores all live bay graphics and clears prior ownership");
        }
        void Update(){
            // These explicitly frozen software-human fixtures inspect rendering,
            // not focus-loss behavior. Keep real gameplay focus policy intact;
            // resume only this probe's valid synthetic roster through its API.
            if(uiFixtures&&match!=null&&match.Running&&match.Paused&&match.DevicesReady){log.AppendLine("FIXTURE resumed valid software roster after focus pause; focused="+Application.isFocused);match.Resume();Time.timeScale=0;}
            if(!done&&deadline>0&&Time.realtimeSinceStartup>deadline){Check(false,"Parking compact watchdog expired");Finish();}
        }
        void Finish(){if(done)return;done=true;log.AppendLine("CHECKS="+checks+" ERRORS="+errors+"; software-human UI fixtures plus actual AI match, no hardware/human acceptance");File.WriteAllText(Path.Combine(folder,"result.txt"),log.ToString());Application.Quit(errors==0?0:1);}
        void OnDestroy(){Application.logMessageReceived-=Error;preferences?.Dispose();foreach(var pad in pads)if(pad!=null&&pad.added)InputSystem.RemoveDevice(pad);InputSystem.settings.backgroundBehavior=background;Time.timeScale=1;}
    }
}
#endif

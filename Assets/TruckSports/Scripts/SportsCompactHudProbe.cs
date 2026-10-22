#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using LWS.TruckBowling;
using LWS.TruckBaseJumping;
using LWS.TruckMattressRacing;
using LWS.TruckWifeCarrying;
using LWS.TruckSnake;

namespace LWS.TruckSports
{
    // Explicit development-only rendering fixtures. These do not award gameplay
    // points, claim human input acceptance, or alter saved match settings.
    public sealed class SportsCompactHudProbe : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-tsCompactHud")<0)return;
            var root=new GameObject("Explicit compact HUD Windows acceptance");DontDestroyOnLoad(root);
            if(!SportsProbeInputIsolation.Active)root.AddComponent<SportsProbeInputIsolation>();
            root.AddComponent<SportsCompactHudProbe>();
        }
        readonly Gamepad[] pads=new Gamepad[4];readonly StringBuilder report=new StringBuilder();
        SportsEventSession match;string folder;int checks,errors;bool freeze;IDisposable preferences;
        InputSettings.BackgroundBehavior background;
        void Check(bool ok,string message){checks++;if(!ok)errors++;report.AppendLine((ok?"PASS ":"FAIL ")+message);File.WriteAllText(Path.Combine(folder,"progress.txt"),report.ToString());}
        void Error(string message,string stack,LogType type){if(type!=LogType.Error&&type!=LogType.Exception)return;errors++;report.AppendLine("ERROR "+message+"\n"+stack);File.WriteAllText(Path.Combine(folder,"progress.txt"),report.ToString());}
        static Rect Bounds(RectTransform rect){var corners=new Vector3[4];rect.GetWorldCorners(corners);var low=RectTransformUtility.WorldToScreenPoint(null,corners[0]);var high=RectTransformUtility.WorldToScreenPoint(null,corners[2]);return Rect.MinMaxRect(low.x,low.y,high.x,high.y);}
        IEnumerator Start()
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsBroadcastOutput");folder=at>=0?Path.GetFullPath(args[at+1]):SportsEvidencePaths.Folder("PlaytestRepair/CompactHud");Directory.CreateDirectory(folder);
            preferences=SportsPrefs.IsolateValidation("CompactHudWindows");background=InputSystem.settings.backgroundBehavior;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            Application.logMessageReceived+=Error;for(int i=0;i<4;i++){pads[i]=InputSystem.AddDevice<Gamepad>();InputSystem.EnableDevice(pads[i]);}
            File.WriteAllText(Path.Combine(folder,"result.txt"),"RUNNING "+DateTime.UtcNow.ToString("o"));
            foreach(string scene in new[]{"TruckBaseJumping","TruckMattressRacing","TruckTargetLaunch","TruckWifeCarrying","TruckSnake"})
            {
                freeze=false;Time.timeScale=1;SportsNavigation.LoadSport(scene);yield return new WaitForSecondsRealtime(.8f);match=FindAnyObjectByType<SportsEventSession>();
                Check(match!=null,scene+" actual saved scene loads");if(match==null)continue;
                for(int pass=0;pass<5;pass++)
                {
                    freeze=false;Time.timeScale=1;match.SetupScreen();match.setup.count=match.MaximumPlayers;match.AllBots();int humans=pass==4?1:pass+1;bool ai=pass==4;SportsCameraOptions.Set(ai);
                    for(int i=0;i<humans;i++){if(!pads[i].enabled)InputSystem.EnableDevice(pads[i]);match.setup.players[i].source=BowlingSource.WholeController;match.setup.players[i].pad=pads[i];}
                    foreach(var catalog in FindObjectsByType<SportsWeatherPresets>(FindObjectsSortMode.None)){var clear=Array.Find(catalog.entries,e=>e.displayName=="Clear");if(clear!=null)catalog.Select(clear.id);}
                    bool started=match.StartMatch();Check(started,scene+" starts humans="+humans+" AI cameras="+ai);if(!started)continue;
                    yield return new WaitForSecondsRealtime(.4f);freeze=true;Time.timeScale=0;
                    var visible=match.cameras.Select((camera,index)=>new{camera,index}).Where(v=>v.camera!=null&&v.camera.enabled).ToArray();Check(visible.Length==(ai?4:humans),scene+" correct camera count");
                    foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080)})
                    {
                        Screen.SetResolution(size.x,size.y,false);yield return new WaitForSecondsRealtime(.4f);yield return new WaitForEndOfFrame();Canvas.ForceUpdateCanvases();
                        Check(Screen.width==size.x&&Screen.height==size.y,scene+" actual window "+size);bool valid=true;var occupied=new Rect[visible.Length];
                        for(int k=0;k<visible.Length;k++)
                        {
                            var view=visible[k];var card=match.menus.viewPanels[view.index].Find("Compact owned viewport score") as RectTransform;
                            if(card==null||!card.gameObject.activeInHierarchy){valid=false;continue;}
                            valid&=match.TryReadViewportCard(view.index,out int owner,out string score,out string detail);
                            var viewport=view.camera.pixelRect;var bounds=Bounds(card);occupied[k]=bounds;
                            valid&=bounds.xMin>=viewport.xMin-1&&bounds.xMax<=viewport.xMax+1&&bounds.yMin>=viewport.yMin-1&&bounds.yMax<=viewport.yMax+1;
                            valid&=card.Find("Participant name").GetComponent<TMP_Text>().text==match.ViewportCardLabel(view.index,owner)&&card.Find("Owned score").GetComponent<TMP_Text>().text==score;
                            valid&=!match.menus.viewTexts[view.index].gameObject.activeSelf&&!match.menus.header.transform.parent.gameObject.activeSelf;
                            for(int other=0;other<k;other++)valid&=!bounds.Overlaps(occupied[other]);
                        }
                        Check(valid,scene+" compact owned groups within viewports humans="+humans+" AI="+ai+" "+size);
                        string file=Path.Combine(folder,scene+"-"+humans+"-human-ai-"+ai+"-"+size.x+".png");ScreenCapture.CaptureScreenshot(file);yield return new WaitForSecondsRealtime(.2f);
                        Check(File.Exists(file),scene+" real player-window screenshot written");
                    }
                }
                freeze=false;Time.timeScale=1;match.SetupScreen();
                if(match is BaseJumpMatch baseMatch)
                {
                    baseMatch.AllBots();Check(baseMatch.StartMatch(),"BASE smoke rematch starts");var flares=baseMatch.skyField.bodies.Where(b=>b.kind==2).ToArray();yield return new WaitForSecondsRealtime(15);
                    Check(flares.Length==4&&flares.All(f=>f.smoke!=null),"all four existing flares bind owned NWH smoke");
                    Check(flares.Any(f=>f.gameObject.activeInHierarchy&&f.smoke.particleCount>0),"actual Windows native particles emit from active flares");
                    var flare=flares.FirstOrDefault(f=>f.gameObject.activeInHierarchy&&f.smoke.particleCount>0);
                    if(flare!=null)
                    {
                        // A clearly labelled presentation fixture: real pooled
                        // emitter/body/particles, existing camera at 20m range.
                        // No claim that this is the ordinary flight framing.
                        var selected=baseMatch.cameras.First(c=>c.enabled);var rectangles=baseMatch.cameras.Select(c=>c.rect).ToArray();var enabled=baseMatch.cameras.Select(c=>c.enabled).ToArray();
                        baseMatch.enabled=false;foreach(var camera in baseMatch.cameras)camera.enabled=camera==selected;selected.rect=new Rect(0,0,1,1);
                        var rig=baseMatch.views.First(v=>v.view==selected);rig.Pose(flare.body.position, new Vector3(15,6,-12),60,true);yield return new WaitForSecondsRealtime(.25f);yield return new WaitForEndOfFrame();
                        string smokeFile=Path.Combine(folder,"BASE-native-smoke-camera-fixture.png");ScreenCapture.CaptureScreenshot(smokeFile);yield return new WaitForSecondsRealtime(.2f);Check(File.Exists(smokeFile),"real native smoke presentation fixture captured at useful range");
                        for(int view=0;view<baseMatch.cameras.Length;view++){baseMatch.cameras[view].enabled=enabled[view];baseMatch.cameras[view].rect=rectangles[view];}baseMatch.enabled=true;
                    }
                    baseMatch.SetupScreen();Check(flares.All(f=>f.smoke.particleCount==0),"scene cleanup clears all pooled smoke");
                }
                else if(match is MattressMatch mattress)yield return ManualReset(mattress);
                else if(match is WifeMatch wife)yield return WifeGuidance(wife);
                else if(match is SnakeMatch snake)yield return SnakeGuidance(snake);
            }
            match=null;freeze=false;Time.timeScale=1;SportsNavigation.FrontEnd();yield return new WaitForSecondsRealtime(.5f);Check(FindAnyObjectByType<SportsEventSession>()==null,"menu releases scene authorities");
            report.AppendLine("CHECKS="+checks+" ERRORS="+errors+". Actual Windows rendering and software-controller fixtures; no human, listening, hardware or performance acceptance claim.");File.WriteAllText(Path.Combine(folder,"result.txt"),report.ToString());Application.logMessageReceived-=Error;Application.Quit(errors==0?0:2);
        }
        IEnumerator WifeGuidance(WifeMatch wife)
        {
            wife.AllBots();SportsCameraOptions.Set(false);for(int slot=0;slot<4;slot++){wife.setup.players[slot].source=BowlingSource.WholeController;wife.setup.players[slot].pad=pads[slot];}
            Check(wife.StartMatch(),"four human-pair checkpoint fixture starts");yield return new WaitForSecondsRealtime(3.5f);freeze=true;Time.timeScale=0;
            var guide=wife.GetComponent<WifeCheckpointGuidance>();Check(guide!=null,"Wife saved scene binds checkpoint vendor adapter");if(guide==null)yield break;
            foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080)})
            {
                Screen.SetResolution(size.x,size.y,false);yield return new WaitForSecondsRealtime(.4f);yield return new WaitForEndOfFrame();bool valid=wife.Phase==WifePhase.Race;
                for(int view=0;view<4;view++)
                {
                    var compass=guide.compasses[view];var marker=guide.targets[view];var widget=marker.GetState(compass.compassGroup).indicatorRT;
                    valid&=compass.isActiveAndEnabled&&marker.isActiveAndEnabled&&compass.cameraMain==wife.cameras[view]&&compass.viewportRect==wife.cameras[view].rect&&widget!=null&&widget.gameObject.activeInHierarchy;
                    if(widget!=null){var point=RectTransformUtility.WorldToScreenPoint(null,widget.position);var viewport=wife.cameras[view].pixelRect;valid&=point.x>=viewport.xMin&&point.x<=viewport.xMax&&point.y>=viewport.yMin&&point.y<=viewport.yMax;}
                }
                Check(valid,"four native checkpoint widgets retain correct owner/viewport "+size);ScreenCapture.CaptureScreenshot(Path.Combine(folder,"Wife-checkpoint-cues-"+size.x+".png"));yield return new WaitForSecondsRealtime(.2f);
            }
            freeze=false;Time.timeScale=1;wife.SetupScreen();yield return null;Check(guide.compasses.All(c=>!c.gameObject.activeSelf),"Wife checkpoint widgets clear after leaving race");
        }
        IEnumerator SnakeGuidance(SnakeMatch snake)
        {
            snake.AllBots();SportsCameraOptions.Set(false);for(int slot=0;slot<4;slot++){snake.setup.players[slot].source=BowlingSource.WholeController;snake.setup.players[slot].pad=pads[slot];}
            snake.drivingViews.Select(1);Check(snake.StartMatch(),"four human Snake objective fixture starts");yield return new WaitForSecondsRealtime(3.5f);freeze=true;Time.timeScale=0;
            var guide=snake.GetComponent<SnakePickupGuidance>();Check(guide!=null,"Snake actual saved scene binds existing CNP prefab");if(guide==null)yield break;
            foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080)})
            {
                Screen.SetResolution(size.x,size.y,false);yield return new WaitForSecondsRealtime(.4f);yield return new WaitForEndOfFrame();bool valid=true;
                for(int view=0;view<4;view++)
                {
                    var selected=guide.Objective(view);var compass=guide.compasses[view];var marker=guide.targets[view];var widget=marker.GetState(compass.compassGroup).indicatorRT;
                    valid&=selected!=null&&selected.Live&&compass.isActiveAndEnabled&&compass.cameraMain==snake.cameras[view]&&compass.viewportRect==snake.cameras[view].rect&&widget!=null&&widget.gameObject.activeInHierarchy;
                    if(widget!=null){var point=RectTransformUtility.WorldToScreenPoint(null,widget.position);var viewport=snake.cameras[view].pixelRect;valid&=point.x>=viewport.xMin&&point.x<=viewport.xMax&&point.y>=viewport.yMin&&point.y<=viewport.yMax;}
                    var card=snake.menus.viewPanels[view].Find("Compact owned viewport score");var detail=card.Find("Race status").GetComponent<TMP_Text>();var telemetry=card.Find("Driving telemetry").GetComponent<TMP_Text>();valid&=detail.text.Contains("TRAILER")&&detail.text.Contains("MPH")&&telemetry.text.Contains("BEST")&&!detail.isTextTruncated&&!telemetry.isTextTruncated;
                }
                Check(valid,"Snake four CNP objectives and complete telemetry stay in owned views "+size);ScreenCapture.CaptureScreenshot(Path.Combine(folder,"Snake-objective-cues-"+size.x+".png"));yield return new WaitForSecondsRealtime(.2f);
            }
            var prior=guide.Objective(0);Check(prior!=null,"Snake marker has a real selected gate before pickup retirement");if(prior!=null){prior.Dormant();yield return new WaitForSecondsRealtime(.25f);Check(guide.Objective(0)!=prior,"Snake retired pickup immediately releases its objective marker");}
            report.AppendLine("OBSERVED Snake maximum throttled route-selection group="+guide.MaxQueryMilliseconds.ToString("F2")+"ms queries="+guide.RouteQueries+". This is a targeted measurement, not GPU or dense-world performance acceptance.");
            report.AppendLine("OBSERVED existing UTS traffic logical="+snake.traffic.LogicalCount+" live="+snake.traffic.ActiveCount+" fullPhysics="+snake.traffic.FullPhysicsCount+" target="+snake.traffic.TargetCount+"; pedestrians logical="+snake.pedestrians.LogicalCount+" live="+snake.pedestrians.ActiveCount+" fullPhysics="+snake.pedestrians.FullPhysicsCount+" target="+snake.pedestrians.TargetCount);
            Check(snake.traffic.LogicalCount>0&&snake.traffic.ActiveCount>0,"Snake actual native traffic exists; a configured population target alone is insufficient");
            var infill=GameObject.Find("Owned Snake roadside town infill");Check(infill!=null&&infill.transform.childCount==44,"actual Snake saved scene includes32owned trees and12road-clear properties");
            if(infill!=null)
            {
                var property=infill.transform.Cast<Transform>().First(t=>t.name.StartsWith("Connected roadside property"));var selected=snake.cameras[0];snake.enabled=false;guide.enabled=false;foreach(var camera in snake.cameras)camera.enabled=camera==selected;selected.rect=new Rect(0,0,1,1);
                // Aim outside the front facade: targeting the building's solid
                // center made Cinemachine's occlusion solver pull this test
                // camera indoors. This is a scenery fixture, not gameplay.
                var rig=snake.drivingViews.rally.views[0];var framing=rig.broadcastFraming;bool framed=framing!=null&&framing.enabled;if(framing!=null)framing.enabled=false;
                rig.PoseRaw(property.position+property.forward*18+Vector3.up*5,property.forward*40+property.right*25+Vector3.up*24,70,true);yield return new WaitForSecondsRealtime(.25f);yield return new WaitForEndOfFrame();Check(Vector3.Distance(selected.transform.position,property.position)>24,"explicit scenery camera remains outside the property");string file=Path.Combine(folder,"Snake-town-infill-camera-fixture.png");ScreenCapture.CaptureScreenshot(file);yield return new WaitForSecondsRealtime(.2f);Check(File.Exists(file),"actual owned roadside models captured with an explicitly positioned scenery camera");if(framing!=null)framing.enabled=framed;guide.enabled=true;snake.enabled=true;
            }
            freeze=false;Time.timeScale=1;snake.SetupScreen();yield return null;Check(guide.compasses.All(c=>!c.gameObject.activeSelf),"Snake objective widgets clear on menu/rematch setup");
        }
        IEnumerator ManualReset(MattressMatch mattress)
        {
            mattress.AllBots();SportsCameraOptions.Set(false);mattress.setup.players[0].source=BowlingSource.WholeController;mattress.setup.players[0].pad=pads[0];
            InputSystem.QueueStateEvent(pads[0],new GamepadState());Check(mattress.StartMatch(),"manual reset physical Windows fixture starts");
            yield return new WaitForSecondsRealtime(3.5f);for(int i=1;i<mattress.racers.Length;i++)mattress.racers[i].Fail();var racer=mattress.racers[0];var body=racer.truck.body;
            InputSystem.QueueStateEvent(pads[0],new GamepadState().WithButton(GamepadButton.South));yield return new WaitForSecondsRealtime(.2f);
            body.position=mattress.course.Spawn(0,0)+Vector3.up*3;body.rotation=Quaternion.Euler(0,0,180);body.linearVelocity=body.angularVelocity=Vector3.zero;Physics.SyncTransforms();
            yield return new WaitForSecondsRealtime(5.5f);
            Check(racer.NeedsReset&&racer.Recoveries==0&&!body.isKinematic&&racer.Phase==MattressAttempt.Racing,"overturned human keeps real physics and does not reset from an already-held action");
            freeze=true;Time.timeScale=0;
            foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080)})
            {
                Screen.SetResolution(size.x,size.y,false);yield return new WaitForSecondsRealtime(.4f);yield return new WaitForEndOfFrame();Canvas.ForceUpdateCanvases();
                var popup=mattress.menus.viewPanels[0].Find("Compact owned viewport score/Contextual action") as RectTransform;
                var bounds=popup!=null?Bounds(popup):default;var viewport=mattress.cameras[0].pixelRect;
                Check(popup!=null&&popup.gameObject.activeInHierarchy&&bounds.xMin>=viewport.xMin&&bounds.xMax<=viewport.xMax&&bounds.yMin>=viewport.yMin&&bounds.yMax<=viewport.yMax,"small assigned reset prompt remains in owned viewport "+size);
                ScreenCapture.CaptureScreenshot(Path.Combine(folder,"Mattress-reset-prompt-"+size.x+".png"));yield return new WaitForSecondsRealtime(.2f);
            }
            freeze=false;Time.timeScale=1;InputSystem.QueueStateEvent(pads[0],new GamepadState());yield return new WaitForSecondsRealtime(.2f);
            InputSystem.QueueStateEvent(pads[0],new GamepadState().WithButton(GamepadButton.South));yield return new WaitForSecondsRealtime(.2f);
            Check(racer.Recoveries==1&&racer.Phase==MattressAttempt.Recovery&&body.isKinematic,"fresh assigned input confirms one existing five-second recovery");
            yield return new WaitForSecondsRealtime(.3f);Check(racer.Recoveries==1,"continued hold never confirms another recovery");
            InputSystem.QueueStateEvent(pads[0],new GamepadState());mattress.SetupScreen();Check(mattress.StartMatch()&&racer.Recoveries==0&&!racer.NeedsReset,"rematch resets recovery counter and contextual prompt");mattress.SetupScreen();
        }
        void Update(){if(match!=null&&match.Paused&&match.DevicesReady){match.Resume();if(freeze)Time.timeScale=0;}}
        void OnDestroy(){Application.logMessageReceived-=Error;Time.timeScale=1;foreach(var pad in pads)if(pad!=null&&pad.added)InputSystem.RemoveDevice(pad);InputSystem.settings.backgroundBehavior=background;preferences?.Dispose();}
    }
}
#endif

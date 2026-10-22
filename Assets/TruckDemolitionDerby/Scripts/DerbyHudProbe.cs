#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using LWS.TruckBowling;
using LWS.TruckSports;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LWS.TruckDerby
{
    [DefaultExecutionOrder(1200)]
    public sealed class DerbyHudProbe : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-tsDerbyHud")<0)return;var root=new GameObject("Explicit Derby damage HUD acceptance");DontDestroyOnLoad(root);root.AddComponent<SportsProbeInputIsolation>();root.AddComponent<DerbyHudProbe>();}
        DerbyMatch match;string folder;int checks,errors;bool freeze;IDisposable prefs;InputSettings previous,owned;Gamepad[] pads=new Gamepad[4];readonly StringBuilder log=new StringBuilder();
        void Check(bool okay,string text){checks++;if(!okay)errors++;log.AppendLine((okay?"PASS ":"FAIL ")+text);File.WriteAllText(Path.Combine(folder,"progress.txt"),log.ToString());}
        void Error(string text,string stack,LogType type){if(type!=LogType.Error&&type!=LogType.Exception)return;errors++;log.AppendLine("ERROR "+text+"\n"+stack);}
        void LateUpdate(){if(freeze)Time.timeScale=0;}
        static Rect Bounds(RectTransform rect){var corners=new Vector3[4];rect.GetWorldCorners(corners);return Rect.MinMaxRect(corners[0].x,corners[0].y,corners[2].x,corners[2].y);}
        bool Row(Transform root,int slot,Rect viewport)
        {
            if(root==null||!root.gameObject.activeInHierarchy)return false;var r=Bounds((RectTransform)root);bool valid=r.xMin>=viewport.xMin-1&&r.xMax<=viewport.xMax+1&&r.yMin>=viewport.yMin-1&&r.yMax<=viewport.yMax+1;
            var engine=root.Find("Engine remaining percent").GetComponent<TMP_Text>();var body=root.Find("Body remaining percent").GetComponent<TMP_Text>();engine.ForceMeshUpdate();body.ForceMeshUpdate();
            valid&=engine.text==DerbyDamageHud.RemainingPercent(match.fighters[slot].damage.Engine)+"%"&&body.text==DerbyDamageHud.RemainingPercent(match.fighters[slot].damage.Structure)+"%";
            valid&=engine.GetPreferredValues(engine.text).x<=engine.rectTransform.rect.width&&body.GetPreferredValues(body.text).x<=body.rectTransform.rect.width;
            valid&=root.Find("NWH engine health icon").GetComponent<UnityEngine.UI.Image>().sprite!=null&&root.Find("Heat body health icon").GetComponent<UnityEngine.UI.Image>().sprite!=null;return valid;
        }
        IEnumerator Start()
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDerbyHudOutput");folder=at>=0?args[at+1]:SportsEvidencePaths.Folder("DerbyHud");Directory.CreateDirectory(folder);
            prefs=SportsPrefs.IsolateValidation("DerbyHudWindows");previous=InputSystem.settings;owned=Instantiate(previous);owned.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;InputSystem.settings=owned;Application.logMessageReceived+=Error;
            for(int i=0;i<4;i++){pads[i]=InputSystem.AddDevice<Gamepad>();InputSystem.EnableDevice(pads[i]);}
            SportsNavigation.LoadSport("TruckDemolitionDerby");yield return null;yield return null;match=FindAnyObjectByType<DerbyMatch>();
            Check(match!=null&&match.GetComponent<DerbyDamageHud>()!=null,"actual saved Derby scene has owned damage adapter");
            var signs=FindObjectsByType<TMP_Text>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(t=>t.name=="Event screen readable rules").ToArray();Check(signs.Length==2&&signs.All(t=>!t.enabled),"both exact redundant side texts remain disabled");
            var art=GameObject.Find("Original Derby artwork · no collision");Check(art!=null&&art.GetComponentsInChildren<Renderer>().Length==2&&art.GetComponentsInChildren<Collider>().Length==0,"original PNG appears on two render-only venue signs");
            for(int pass=0;pass<10;pass++)
            {
                freeze=false;Time.timeScale=1;match.SetupScreen();match.setup.count=8;match.AllBots();int humans=pass<2?0:(pass-2)/2+1;bool show=pass%2!=0;SportsCameraOptions.Set(show);match.option=pass==0?1:0;
                for(int i=0;i<humans;i++){int slot=7-i;match.setup.players[slot].source=BowlingSource.WholeController;match.setup.players[slot].pad=pads[i];}
                Check(match.StartMatch(),"existing rules start humans="+humans+" showAI="+show);yield return new WaitForSecondsRealtime(.3f);freeze=true;Time.timeScale=0;yield return null;yield return null;
                // Explicit presentation fixture, not collision or scoring proof:
                // use real damage-state API to check varied 0..100 readings.
                for(int i=0;i<8;i++)match.fighters[i].damage.Apply(DerbyZone.Engine,.1f*(i+1));yield return null;yield return null;
                foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080),new Vector2Int(2560,1080),new Vector2Int(1024,768)})
                {
                    Screen.SetResolution(size.x,size.y,false);yield return new WaitForSecondsRealtime(.3f);yield return new WaitForEndOfFrame();Canvas.ForceUpdateCanvases();bool valid=Screen.width==size.x&&Screen.height==size.y;
                    if(match.MultiViewHUD)
                    {
                        int count=0;for(int v=0;v<match.cameras.Length;v++)if(match.cameras[v].enabled){count++;int slot=match.ViewParticipant(v);valid&=slot>=0&&Row(match.menus.viewPanels[v].Find("Compact owned viewport score/Derby actual engine and body health"),slot,match.cameras[v].pixelRect)&&!match.menus.viewTexts[v].gameObject.activeSelf;}
                        valid&=count==(show?4:humans);
                    }
                    else
                    {
                        var root=match.menus.hud.transform.Find("Broadcast participant cards");float scale=match.menus.GetComponent<Canvas>().scaleFactor;for(int i=0;i<8;i++){var card=(RectTransform)root.Find("Participant card "+i);valid&=Row(card.Find("Derby actual engine and body health"),i,new Rect(0,0,Screen.width,Screen.height));var cardBounds=Bounds(card);valid&=cardBounds.yMax<Screen.height*.22f&&cardBounds.yMin>64*scale+4;}
                    }
                    Check(valid,"actual icon/percent visibility and owner bounds humans="+humans+" showAI="+show+" "+size);
                    ScreenCapture.CaptureScreenshot(Path.Combine(folder,"health-"+humans+"-human-ai-"+show+"-"+size.x+".png"));yield return new WaitForSecondsRealtime(.12f);
                }
            }
            freeze=false;Time.timeScale=1;match.SetupScreen();match.AllBots();match.option=1;SportsCameraOptions.Set(false);Check(match.StartMatch(),"original autonomous rematch starts");yield return null;yield return null;Check(match.fighters.All(f=>f.damage.Engine==0&&f.damage.Structure==0),"rematch repairs every original damage state");match.Pause();yield return null;Check(match.Paused,"existing pause remains functional");match.Resume();match.SetupScreen();
            SportsNavigation.FrontEnd();yield return null;yield return null;Check(FindAnyObjectByType<DerbyDamageHud>()==null&&FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length==1,"menu return removes the adapter and retains one listener");
            log.AppendLine("CHECKS="+checks+" ERRORS="+errors+"; actual composed Windows health fixtures and software ownership. Full physical collision/match evidence is separate; no human, physical hardware, listening or profiling claim.");File.WriteAllText(Path.Combine(folder,"result.txt"),log.ToString());Application.Quit(errors==0?0:2);
        }
        void OnDestroy(){Application.logMessageReceived-=Error;foreach(var pad in pads)if(pad!=null&&pad.added)InputSystem.RemoveDevice(pad);if(previous!=null)InputSystem.settings=previous;if(owned!=null)Destroy(owned);prefs?.Dispose();Time.timeScale=1;}
    }
}
#endif

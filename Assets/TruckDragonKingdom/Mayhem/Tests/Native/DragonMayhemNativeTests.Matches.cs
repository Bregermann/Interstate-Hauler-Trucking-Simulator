#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

// Complete ordinary AI matches, rather than an injected finish or a short
// contact demonstration. The original sport remains the scoring authority.
public sealed partial class DragonMayhemNativeTests
{
    [Serializable] sealed class CompleteMatchFact
    {
        public string sport, phase, matchId, originalScoreState;
        public bool enabled, finished;
        public int run, physicalDrops, physicalAttacks, sheepMeals;
        public int[] sideGold;
        public float realSeconds;
    }
    [Serializable] sealed class ResultTextFact
    {
        public string path,text;
        public bool enabled;
        public Vector3 bottomLeft,topRight;
    }
    [Serializable] sealed class ResultTextFacts { public ResultTextFact[] texts; }
    static object Member(object instance,string name)
    {
        const BindingFlags flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
        var type=instance.GetType();
        var field=type.GetField(name,flags);if(field!=null)return field.GetValue(instance);
        var property=type.GetProperty(name,flags);if(property!=null)return property.GetValue(instance);
        throw new MissingMemberException(type.FullName,name);
    }
    static string BytesHash(string path)
    {using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path)));}
    string OriginalScoreState(string mode,bool requireComplete)
    {
        if(mode=="TruckKeepaway")
        {
            var values=(float[])Field(match,"possession");int winner=(int)Property(match,"Winner");float target=(float)Property(match,"Target");
            if(requireComplete){Assert.That(winner,Is.InRange(0,3));Assert.That(values[winner],Is.EqualTo(target).Within(.001f));}
            return "winner="+winner+" target="+target+" possession="+string.Join(",",values.Select(v=>v.ToString("R",System.Globalization.CultureInfo.InvariantCulture)));
        }
        if(mode=="TruckBalloonBattle")
        {
            var rules=Field(match,"rules");int winner=(int)Property(rules,"Winner"),pops=(int)Property(rules,"Pops");
            if(requireComplete){Assert.That((bool)Property(rules,"Complete"),Is.True);Assert.That(pops,Is.GreaterThan(0));if(winner>=0)Assert.That((bool)Call(rules,"Alive",winner),Is.True);}
            return "winner="+winner+" pops="+pops+" scores="+string.Join(",",(int[])Field(rules,"scores"));
        }
        var totals=(int[])Field(match,"totals");
        if(mode=="TruckCornhole")
        {
            int[] winners=(int[])Field(match,"winners");int count=(int)Property(match,"RoundFinalizations");
            if(requireComplete){Assert.That(count,Is.GreaterThan(0));Assert.That(winners,Is.Not.Empty);int best=totals.Take(4).Max();foreach(int winner in winners)Assert.That(totals[winner],Is.EqualTo(best));}
            return "winners="+string.Join(",",winners)+" finalized="+count+" totals="+string.Join(",",totals);
        }
        int cheeseWinner=(int)Property(match,"Winner");
        if(requireComplete){Assert.That(cheeseWinner,Is.InRange(0,3));Assert.That(totals[cheeseWinner],Is.GreaterThanOrEqualTo(5));Assert.That((int)Property(match,"Catches"),Is.GreaterThanOrEqualTo(5));}
        return "winner="+cheeseWinner+" round="+Property(match,"Round")+" catches="+Property(match,"Catches")+" totals="+string.Join(",",totals);
    }
    string CompleteEvidenceFolder(string mode)
    {
        string[] args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonOutput");
        Assert.That(at>=0&&at+1<args.Length,Is.True,"Use the guarded native validation runner to retain actual match evidence.");
        string folder=Path.Combine(args[at+1],"MayhemCompleteMatches",mode);Directory.CreateDirectory(folder);return folder;
    }
    void CompleteObservation(string mode,string phase,bool enabled,int run,float start,List<CompleteMatchFact> facts)
    {
        var side=((int[])Property(director,"SideGold")).ToArray();int cap=(int)Field(Field(director,"configuration"),"maximumSideGold");
        foreach(int value in side)Assert.That(value,Is.InRange(0,enabled?cap:0),"Only capped optional side gold; original scores stay separate.");
        var fact=new CompleteMatchFact{sport=mode,phase=phase,enabled=enabled,run=run,matchId=(string)Property(match,"MatchId"),finished=(bool)Property(match,"Finished"),originalScoreState=OriginalScoreState(mode,false),sideGold=side,physicalDrops=(int)Property(director,"PhysicalGoldDrops"),physicalAttacks=(int)Property(director,"PhysicalAttackContacts"),sheepMeals=(int)Property(director,"SheepMeals"),realSeconds=Time.realtimeSinceStartup-start};facts.Add(fact);
        File.WriteAllText(Path.Combine(CompleteEvidenceFolder(mode),"run-"+run+"-"+phase+".json"),JsonUtility.ToJson(fact,true));
    }
    void CaptureActualMatch(string mode,string phase,int run,int width,int height)
    {
        var menu=(Component)Field(match,"menus");var canvas=menu.GetComponent<Canvas>();Assert.That(canvas,Is.Not.Null);
        Camera[] cameras=Camera.allCameras.Where(camera=>camera.gameObject.scene==match.gameObject.scene&&camera.isActiveAndEnabled&&camera.cameraType!=CameraType.Preview).ToArray();
        Assert.That(cameras.Any(camera=>camera.enabled),Is.True,"Actual original match must retain a rendered view.");
        var capture=Runtime("LWS.TruckSports.SportsControlledCapture").GetMethods().Single(method=>method.Name=="Save"&&method.GetParameters().Length==6&&method.GetParameters()[0].ParameterType==typeof(Canvas));
        capture.Invoke(null,new object[]{canvas,cameras,width,height,Path.Combine(CompleteEvidenceFolder(mode),"run-"+run+"-"+phase+"-"+width+"x"+height+".png"),null});
        if(phase=="actual-results")
        {
            // Keep the actual rendered text/rect observations alongside the
            // image. A correct score alone cannot certify overlapping UI.
            var values=new List<ResultTextFact>();
            foreach(var component in canvas.GetComponentsInChildren<Component>(false))
            {
                if(component==null||component.GetType().FullName!="TMPro.TextMeshProUGUI")continue;
                var rect=component.transform as RectTransform;if(rect==null)continue;
                var corners=new Vector3[4];rect.GetWorldCorners(corners);
                string path=component.name;for(var parent=component.transform.parent;parent!=null&&parent!=canvas.transform;parent=parent.parent)path=parent.name+"/"+path;
                values.Add(new ResultTextFact{path=path,text=(string)Member(component,"text"),enabled=((Behaviour)component).isActiveAndEnabled,bottomLeft=corners[0],topRight=corners[2]});
            }
            File.WriteAllText(Path.Combine(CompleteEvidenceFolder(mode),"run-"+run+"-actual-results-texts.json"),JsonUtility.ToJson(new ResultTextFacts{texts=values.ToArray()},true));
            var tally=values.SingleOrDefault(value=>value.enabled&&value.path.Contains("Dragon Mayhem side tally/"));
            if(tally!=null)
            {
                var bounds=Rect.MinMaxRect(tally.bottomLeft.x,tally.bottomLeft.y,tally.topRight.x,tally.topRight.y);
                foreach(var button in values.Where(value=>value.enabled&&value.path.Contains(" Button/Caption")))
                {
                    var other=Rect.MinMaxRect(button.bottomLeft.x,button.bottomLeft.y,button.topRight.x,button.topRight.y);
                    Assert.That(bounds.Overlaps(other),Is.False,"Actual Mayhem tally must not cover a replay/navigation button: "+button.path);
                }
            }
        }
    }
    IEnumerator CompleteOriginalMatches(string mode)
    {
        string scene="Assets/"+mode+"/Scenes/"+mode+".unity";var sources=new[]{scene,scene+".meta"}.ToDictionary(path=>path,BytesHash);
        var isolation=new GameObject("Mayhem complete-match native input isolation");UnityEngine.Object.DontDestroyOnLoad(isolation);isolation.AddComponent(Runtime("LWS.TruckSports.SportsProbeInputIsolation"));
        var facts=new List<CompleteMatchFact>();
        try
        {
            // Same loaded original scene: OFF match, ON match, and ON rematch.
            // No result, contact, score, rules clock or vehicle pose is injected.
            yield return Begin(mode,false);
            string previousId=null;
            for(int run=0;run<3;run++)
            {
                bool enabled=run>0;
                if(run>0)
                {
                    var setup=match.GetType().GetMethod("SetupScreen")??match.GetType().GetMethod("ChangeSetup");setup.Invoke(match,null);yield return null;yield return null;
                    Assert.That((int)Property(director,"RuntimeActorCount"),Is.Zero,"Normal setup reclaims the prior optional runtime.");
                    Assert.That((bool)Call(director,"SetEnabled",true),Is.True);Assert.That((bool)Call(match,"StartMatch"),Is.True);
                }
                string id=(string)Property(match,"MatchId");Assert.That(id,Is.Not.Empty);Assert.That(id,Is.Not.EqualTo(previousId));previousId=id;
                yield return null;yield return null;Time.timeScale=4;
                float start=Time.realtimeSinceStartup,next=start+15,limit=start+600;
                CompleteObservation(mode,"start",enabled,run,start,facts);CaptureActualMatch(mode,"start",run,1280,720);
                while(!(bool)Property(match,"Finished")&&Time.realtimeSinceStartup<limit)
                {
                    if(Time.realtimeSinceStartup>=next){CompleteObservation(mode,"running-"+facts.Count,enabled,run,start,facts);next=Time.realtimeSinceStartup+15;}
                    if(!enabled)Assert.That((int)Property(director,"RuntimeActorCount"),Is.Zero);
                    yield return null;
                }
                CompleteObservation(mode,"terminal",enabled,run,start,facts);
                Assert.That((bool)Property(match,"Finished"),Is.True,"Actual original AI match did not finish within600real seconds; no forced result permitted. Inspect retained live facts.");
                string actual=OriginalScoreState(mode,true);int[] gold=((int[])Property(director,"SideGold")).ToArray();
                // Allow the ordinary result tween/update and runtime cleanup.
                yield return new WaitForSecondsRealtime(1.2f);
                Assert.That(OriginalScoreState(mode,true),Is.EqualTo(actual));Assert.That((int[])Property(director,"SideGold"),Is.EqualTo(gold));Assert.That((int)Property(director,"RuntimeActorCount"),Is.Zero);
                CaptureActualMatch(mode,"actual-results",run,1280,720);CaptureActualMatch(mode,"actual-results",run,1920,1080);
            }
        }
        finally
        {
            Time.timeScale=1;if(isolation!=null)UnityEngine.Object.DestroyImmediate(isolation);
            foreach(var pair in sources)Assert.That(BytesHash(pair.Key),Is.EqualTo(pair.Value),"Actual gameplay cannot rewrite authored scene or GUID: "+pair.Key);
        }
    }
    [UnityTest,Timeout(1900000)]public IEnumerator KeepawayCompletesOriginalOffOnAndOnRematchWithUnchangedWinnerAuthority(){yield return CompleteOriginalMatches("TruckKeepaway");}
    [UnityTest,Timeout(1900000)]public IEnumerator BalloonBattleCompletesOriginalOffOnAndOnRematchWithUnchangedWinnerAuthority(){yield return CompleteOriginalMatches("TruckBalloonBattle");}
    [UnityTest,Timeout(1900000)]public IEnumerator CornholeCompletesOriginalOffOnAndOnRematchWithUnchangedWinnerAuthority(){yield return CompleteOriginalMatches("TruckCornhole");}
    [UnityTest,Timeout(1900000)]public IEnumerator CheeseCompletesOriginalOffOnAndOnRematchWithUnchangedWinnerAuthority(){yield return CompleteOriginalMatches("TruckCheeseRolling");}
}
#endif

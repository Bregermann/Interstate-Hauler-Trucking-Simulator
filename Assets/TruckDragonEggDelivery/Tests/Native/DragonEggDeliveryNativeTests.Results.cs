#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using LWS.TruckDragonEggDelivery;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

// Read-only presentation assertions added AFTER the original genuine physical
// delivery gates. Never supplies final values, finishes, callbacks or UI state.
public sealed partial class DragonEggDeliveryNativeTests
{
    readonly Dictionary<string,string> resultsSourceBytes=new Dictionary<string,string>();
    string previousResultsMatch;
    static string ResultsHash(string path)
    {
        string absolute=Path.GetFullPath(path);if(Environment.OSVersion.Platform==PlatformID.Win32NT&&!absolute.StartsWith(@"\\?\",StringComparison.Ordinal))absolute=@"\\?\"+absolute;
        using(var sha=SHA256.Create())using(var file=File.OpenRead(absolute))return string.Concat(sha.ComputeHash(file).Select(value=>value.ToString("x2")));
    }
    void RememberResultsSourceBytes()
    {
        resultsSourceBytes.Clear();previousResultsMatch=null;
        foreach(string path in new[]{Scene,Scene+".meta"})resultsSourceBytes.Add(path,ResultsHash(path));
    }
    void AssertResultsSourceBytes(){foreach(var pair in resultsSourceBytes)Assert.That(ResultsHash(pair.Key),Is.EqualTo(pair.Value),"Real results observation cannot save source scene/metadata: "+pair.Key);}
    Component EggBoard()
    {
        var menus=(Component)Get(match,"menus");var boards=menus.GetComponentsInChildren(Runtime("LWS.TruckSports.SportsResultsPresentation"),true).Cast<Component>().ToArray();
        Assert.That(boards.Length,Is.EqualTo(1),"Explicit saved results-board migration is a prerequisite; this fixture never authors one.");return boards[0];
    }
    void AssertEggResultsReset()
    {
        var board=EggBoard();Assert.That(Get(board,"Snapshot"),Is.Null,"Ordinary Setup/Start retires the prior real round's results.");Assert.That(Get(Get(board,"source"),"ResultReady"),Is.EqualTo(false));Assert.That(Get(Get(Get(board,"source"),"OriginalResult"),"enabled"),Is.EqualTo(true));
    }
    static Rect ResultPixels(RectTransform rect,Camera camera)
    {
        var corners=new Vector3[4];rect.GetWorldCorners(corners);var points=corners.Select(point=>RectTransformUtility.WorldToScreenPoint(camera,point)).ToArray();return Rect.MinMaxRect(points.Min(point=>point.x),points.Min(point=>point.y),points.Max(point=>point.x),points.Max(point=>point.y));
    }
    static void WithinCapture(Rect rect,Vector2Int size,string label)
    {
        Assert.That(rect.width,Is.GreaterThan(0),label);Assert.That(rect.height,Is.GreaterThan(0),label);Assert.That(rect.xMin,Is.GreaterThanOrEqualTo(-2),label);Assert.That(rect.yMin,Is.GreaterThanOrEqualTo(-2),label);Assert.That(rect.xMax,Is.LessThanOrEqualTo(size.x+2),label);Assert.That(rect.yMax,Is.LessThanOrEqualTo(size.y+2),label);
    }
    static string EscapeResult(string value)=>(value??"").Replace("<","‹").Replace(">","›");
    static object ParticipantResult(object player,string property)=>Runtime("LWS.TruckSports.ISportsParticipantSlot").GetProperty(property).GetValue(player);
    IEnumerator ObserveActualEggResultsBoard(DragonEggDeliveryRules rules,int run,bool requireAllDeliveries=true)
    {
        Assert.That(Get(match,"Finished"),Is.EqualTo(true));Assert.That(rules.Complete,Is.True);Assert.That(Get(match,"PhysicalDeliveries"),requireAllDeliveries?Is.EqualTo(4):Is.GreaterThan(0));
        var board=EggBoard();var source=Get(board,"source");Assert.That(source.GetType().FullName,Is.EqualTo("LWS.TruckDragonKingdom.DragonFinalResultsSource"));Assert.That(Get(source,"session"),Is.SameAs(match));Assert.That(Get(source,"ResultReady"),Is.EqualTo(true));
        string identity=(string)Get(match,"MatchId");Assert.That(identity,Is.Not.EqualTo(previousResultsMatch));previousResultsMatch=identity;
        int delivered=(int)Get(match,"PhysicalDeliveries");Assert.That(rules.Cargo.Count(cargo=>cargo.Stage==DragonEggStage.Delivered),Is.EqualTo(delivered));
        var scores=rules.Cargo.Select(cargo=>cargo.Score).ToArray();var cargoFacts=rules.Cargo.Select(cargo=>cargo.Id+"|"+cargo.Stage+"|"+cargo.Condition.ToString("R",CultureInfo.InvariantCulture)+"|"+cargo.RoughImpacts+"|"+cargo.DeliverySeconds.ToString("R",CultureInfo.InvariantCulture)+"|"+cargo.DeliveredBaby).ToArray();var winners=rules.Winners();int journal=(int)Get(Get(match,"ScoreJournal"),"Count");
        float deadline=Time.realtimeSinceStartup+3;while(!(bool)Get(board,"Displaying")&&Time.realtimeSinceStartup<deadline)yield return null;
        Assert.That(Get(board,"Displaying"),Is.EqualTo(true));yield return new WaitForSecondsRealtime(1.2f); // Allow the real existing unscaled DOTween row/value animations.
        var snapshot=Get(board,"Snapshot");var rows=((IEnumerable)Get(snapshot,"Rows")).Cast<object>().ToArray();Assert.That(rows.Length,Is.EqualTo(4));Assert.That(rows.Select(row=>(string)Get(row,"ParticipantId")).Distinct().Count(),Is.EqualTo(4));
        var players=(Array)Get(Get(match,"setup"),"players");var views=(Array)Get(board,"rows");var menus=(Component)Get(match,"menus");var pages=(GameObject[])Get(menus,"pages");var page=pages.Single(item=>item.activeInHierarchy&&board.transform.IsChildOf(item.transform));
        Assert.That(Get(menus,"Screen").ToString(),Is.EqualTo("Result"));Assert.That(Get(Get(source,"OriginalResult"),"enabled"),Is.EqualTo(false));var backing=page.transform.Find("Results readability backing");Assert.That(backing,Is.Not.Null);
        var backingImage=(Behaviour)backing.GetComponent(Type.GetType("UnityEngine.UI.Image, UnityEngine.UI",true));Assert.That(backingImage,Is.Not.Null);Assert.That(backingImage.isActiveAndEnabled,Is.True);Assert.That(((Color)Get(backingImage,"color")).a,Is.GreaterThanOrEqualTo(.9f));Assert.That(Get(backingImage,"raycastTarget"),Is.EqualTo(false));
        var visibility=(CanvasGroup)Get(board,"visibility");Assert.That(visibility.alpha,Is.GreaterThan(.99f));Assert.That(visibility.interactable||visibility.blocksRaycasts,Is.False);
        var resultActions=page.GetComponentsInChildren(Runtime("LWS.TruckSports.SportsMenuAction"),true).Cast<Component>().Where(item=>item.gameObject.activeInHierarchy).ToArray();
        foreach(string required in new[]{"rematch","setup","front"})
        {
            var actions=resultActions.Where(item=>(string)Get(item,"action")==required).ToArray();Assert.That(actions.Length,Is.EqualTo(1),"Exactly one original result action must remain available: "+required);
            var heat=(Component)Get(actions[0],"button");Assert.That(heat,Is.Not.Null,"Original Heat result button binding: "+required);
            var button=(Behaviour)heat.GetComponent(Type.GetType("UnityEngine.UI.Button, UnityEngine.UI",true));Assert.That(button,Is.Not.Null,"Original result button component: "+required);
            Assert.That(button.isActiveAndEnabled&&(bool)Get(button,"interactable"),Is.True,"Original result action must be usable: "+required);
        }
        var controls=resultActions.Select(item=>item.GetComponent<RectTransform>()).ToArray();
        for(int index=0;index<rows.Length;index++)
        {
            var row=rows[index];int slot=(int)Get(row,"Slot");Assert.That(slot,Is.InRange(0,3));var cargo=rules.Cargo[slot];var view=views.GetValue(index);
            Assert.That(Get(row,"ParticipantId"),Is.EqualTo(ParticipantResult(players.GetValue(slot),"ParticipantId")));Assert.That(Get(row,"DisplayName"),Is.EqualTo(ParticipantResult(players.GetValue(slot),"DisplayName")));Assert.That(Get(row,"Measurement"),Is.EqualTo((double)cargo.Score));Assert.That(Get(row,"Unit"),Is.EqualTo("PTS"));Assert.That(Get(row,"Winner"),Is.EqualTo(winners.Contains(slot)));Assert.That((int)Get(row,"Place"),cargo.Stage==DragonEggStage.Delivered?Is.GreaterThan(0):Is.EqualTo(0));
            string detail=(string)Get(row,"Detail");StringAssert.Contains(cargo.Stage==DragonEggStage.Delivered?(cargo.DeliveredBaby?"BABY SAFE":"EGG SAFE"):cargo.Stage==DragonEggStage.Lost?"DNF · CARGO LOST":"DNF · NOT DELIVERED",detail);StringAssert.Contains((cargo.Condition*100).ToString("0",CultureInfo.InvariantCulture)+"% CONDITION",detail);StringAssert.Contains(cargo.RoughImpacts+" ROUGH IMPACT",detail);if(cargo.Stage==DragonEggStage.Delivered)StringAssert.Contains(cargo.DeliverySeconds.ToString("0.0",CultureInfo.InvariantCulture)+" SEC",detail);
            Assert.That(Get(Get(view,"value"),"text"),Is.EqualTo(Get(row,"Value")));Assert.That(Get(Get(view,"detail"),"text"),Is.EqualTo(EscapeResult(detail)));Assert.That(Get(Get(view,"name"),"text"),Is.EqualTo(EscapeResult((string)Get(row,"DisplayName"))));Assert.That(((CanvasGroup)Get(view,"visibility")).alpha,Is.GreaterThan(.99f));
        }
        var save=Runtime("LWS.TruckSports.SportsControlledCapture").GetMethods().Single(method=>method.Name=="Save"&&method.GetParameters().Length==5);string folder=Path.Combine(output,"ResultsBoards",identity);Directory.CreateDirectory(folder);
        foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080)})
        {
            Action<Camera> inspect=ui=>
            {
                Canvas.ForceUpdateCanvases();var controlRects=controls.Select(control=>ResultPixels(control,ui)).ToArray();foreach(var rect in controlRects)WithinCapture(rect,size,"Original result control");
                var occupied=new List<Rect>();
                for(int index=0;index<rows.Length;index++)
                {
                    var view=views.GetValue(index);var rect=ResultPixels((RectTransform)Get(view,"rect"),ui);WithinCapture(rect,size,"Real ranked result row");foreach(var control in controlRects)Assert.That(rect.Overlaps(control),Is.False,"Ranked result cards cannot obscure original replay/navigation controls.");foreach(var other in occupied)Assert.That(rect.Overlaps(other),Is.False);occupied.Add(rect);
                    foreach(string field in new[]{"place","value","detail"})
                    {
                        var text=Get(view,field);Call(text,"ForceMeshUpdate",false,false);Assert.That(Get(text,"isTextOverflowing"),Is.EqualTo(false),"Actual final "+field+" is readable without truncation.");Assert.That((float)Get(text,"fontSize"),Is.GreaterThanOrEqualTo(14));Assert.That((int)Get(Get(text,"textInfo"),"characterCount"),Is.GreaterThan(0));WithinCapture(ResultPixels(((Component)text).GetComponent<RectTransform>(),ui),size,field);
                    }
                }
                foreach(string field in new[]{"heading","summary"}){var text=Get(board,field);Call(text,"ForceMeshUpdate",false,false);Assert.That(Get(text,"isTextOverflowing"),Is.EqualTo(false));Assert.That((int)Get(Get(text,"textInfo"),"characterCount"),Is.GreaterThan(0));WithinCapture(ResultPixels(((Component)text).GetComponent<RectTransform>(),ui),size,field);}
            };
            string file=Path.Combine(folder,"actual-physical-round"+run+"-results-"+size.y+".png");save.Invoke(null,new object[]{match,size.x,size.y,file,inspect});Assert.That(File.Exists(file)&&new FileInfo(file).Length>1000,Is.True,"Actual PNG must be written; capture failure is not swallowed.");
        }
        Assert.That(Get(match,"Rules"),Is.SameAs(rules));CollectionAssert.AreEqual(scores,rules.Cargo.Select(cargo=>cargo.Score));CollectionAssert.AreEqual(cargoFacts,rules.Cargo.Select(cargo=>cargo.Id+"|"+cargo.Stage+"|"+cargo.Condition.ToString("R",CultureInfo.InvariantCulture)+"|"+cargo.RoughImpacts+"|"+cargo.DeliverySeconds.ToString("R",CultureInfo.InvariantCulture)+"|"+cargo.DeliveredBaby));Assert.That(Get(Get(match,"ScoreJournal"),"Count"),Is.EqualTo(journal));Assert.That(Get(match,"PhysicalDeliveries"),Is.EqualTo(delivered));AssertResultsSourceBytes();
        evidence.AppendLine("GENUINE_EGG_RESULTS run="+run+" match="+identity+" actualScores="+string.Join(",",scores)+" winners="+string.Join(",",winners)+" native720p1080p=true ordinaryTweenPlayback=true sourceAndRulesExact=true noSuppliedResults=true");Snapshot("real-results-board-verified-run"+run);
    }
}
#endif

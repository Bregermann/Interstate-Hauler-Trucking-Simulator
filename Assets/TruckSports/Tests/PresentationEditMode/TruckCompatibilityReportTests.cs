#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class TruckCompatibilityReportTests
{
    const BindingFlags Any=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
    static Type Sports(string name)=>Type.GetType("LWS.TruckSports."+name+", Assembly-CSharp",true);
    static object Get(object owner,string name)=>owner.GetType().GetField(name,Any) is FieldInfo field?field.GetValue(owner):owner.GetType().GetProperty(name,Any).GetValue(owner);
    static void Set(object owner,string name,object value)=>owner.GetType().GetField(name,Any).SetValue(owner,value);
    static object Call(object owner,string name,params object[] args)=>owner.GetType().GetMethods(Any).Single(method=>method.Name==name&&method.GetParameters().Length==args.Length).Invoke(owner,args);
    static void RestoreScenes(SceneSetup[] setup)=>Type.GetType("LWS.TruckSports.Editor.SportsTruckCompatibilityAuthoring, Assembly-CSharp-Editor",true).GetMethod("RestorePreviousSceneSetup").Invoke(null,new object[]{setup});
    static object Draft(string body){var draft=Activator.CreateInstance(Sports("SportsTruckAppearanceData"));Set(draft,"bodyId",body);return draft;}
    static void Select(object draft,string slot,string item,string variant=""){var selected=Activator.CreateInstance(Sports("SportsTruckCosmeticSelection"));Set(selected,"slotId",slot);Set(selected,"itemId",item);Set(selected,"variantId",variant);((IList)Get(draft,"selections")).Add(selected);}
    static string AllPages(object report)=>string.Join("",Enumerable.Range(0,(int)Get(report,"DetailPages")).Select(page=>((string)Call(report,"Details",page)).Replace("\n","")));
    [Test]public void UnboundOriginalFallbackRetainsEveryLongStableIdAndSavedVariantAcrossPages()
    {
        var draft=Draft("body."+new string('W',120));for(int i=0;i<16;i++)Select(draft,"future.slot."+i,"item."+i+new string('W',115),"variant."+new string('V',115));string before=JsonUtility.ToJson(draft);Assert.That(Call(draft,"Validate"),Is.EqualTo(true));
        var method=Sports("SportsTruckCompatibilitySource").GetMethod("Original");var report=method.Invoke(null,new object[]{"stable-profile-a","Same Name",draft,null,"This sport uses its original truck appearance. Your saved body and accessories are kept for other sports."});Assert.That(Get(report,"canApply"),Is.EqualTo(false));Assert.That(Get(report,"hasHost"),Is.EqualTo(false));Assert.That(Get(report,"unavailable"),Is.EqualTo(16));StringAssert.Contains("ORIGINAL SPORT TRUCK",(string)Get(report,"effective"));Assert.That((int)Get(report,"DetailPages"),Is.GreaterThan(4));
        string complete=AllPages(report);StringAssert.Contains((string)Get(draft,"bodyId"),complete);foreach(var selection in (IList)Get(draft,"selections")){StringAssert.Contains((string)Get(selection,"itemId"),complete);StringAssert.Contains((string)Get(selection,"variantId"),complete);}StringAssert.Contains("stable-profile-a",complete);Assert.That(JsonUtility.ToJson(draft),Is.EqualTo(before));
        var other=method.Invoke(null,new object[]{"stable-profile-b","Same Name",draft,null,"This sport uses its original truck appearance. Your saved body and accessories are kept for other sports."});Assert.That(Get(other,"name"),Is.EqualTo(Get(report,"name")));Assert.That(Get(other,"profileId"),Is.Not.EqualTo(Get(report,"profileId")));StringAssert.Contains("stable-profile-b",AllPages(other));
    }
    [Test]public void WordBoundariesRetainWholeProseAndEveryOriginalWhitespaceCharacter()
    {
        var parts=new[]{"USED HERE: ORIGINAL SPORT TRUCK","This sport uses its original truck appearance. Your saved body and accessories are kept for other sports.","  deliberate  spaces remain  "};Func<string,float> measure=value=>new System.Globalization.StringInfo(value).LengthInTextElements;var wrap=Sports("SportsTruckCompatibilitySource").GetMethod("WrapDetails");var lines=(string[])wrap.Invoke(null,new object[]{parts,measure,44f});Assert.That(string.Concat(lines),Is.EqualTo(string.Concat(parts)));foreach(var line in lines)Assert.That(measure(line),Is.LessThanOrEqualTo(44));foreach(string word in new[]{"ORIGINAL","appearance","accessories"})Assert.That(lines.Any(line=>line.Contains(word)),Is.True,"Ordinary words must remain whole: "+word);
    }
    [Test]public void LongIdsAndUnicodeGraphemesSurviveFallbackWrappingAndPaginationExactly()
    {
        string token=string.Concat(Enumerable.Repeat("e\u0301\U0001F69A",80));var parts=new[]{"PROFILE ID: "+new string('W',250),"SAVED VARIANT: "+token};Func<string,float> measure=value=>new System.Globalization.StringInfo(value).LengthInTextElements;var lines=(string[])Sports("SportsTruckCompatibilitySource").GetMethod("WrapDetails").Invoke(null,new object[]{parts,measure,32f});Assert.That(string.Concat(lines),Is.EqualTo(string.Concat(parts)));foreach(var line in lines){Assert.That(measure(line),Is.LessThanOrEqualTo(32));Assert.That(char.IsHighSurrogate(line[line.Length-1]),Is.False);Assert.That(line[0],Is.Not.EqualTo('\u0301'));}var report=Activator.CreateInstance(Sports("SportsTruckCompatibilityView"));Set(report,"lines",lines);Assert.That((int)Get(report,"DetailPages"),Is.GreaterThan(1));Assert.That(AllPages(report),Is.EqualTo(string.Concat(parts)));
    }
    [Test]public void AuthoredTmpWidthFitsTheOriginalFallbackOnOnePageAtTheUnchangedFontSize()
    {
        var prefab=UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TruckSports/Resources/TruckSports/SportsTruckCompatibilityModal.prefab");Assert.That(prefab,Is.Not.Null,"Author the existing Heat modal first.");var preview=EditorSceneManager.NewPreviewScene();var root=UnityEngine.Object.Instantiate(prefab);SceneManager.MoveGameObjectToScene(root,preview);
        try
        {
            var modal=root.GetComponent(Sports("SportsTruckCompatibilityModal"));var text=(Component)Get(modal,"detailText");var rect=(RectTransform)text.transform;Assert.That(rect.sizeDelta,Is.EqualTo(new Vector2(1240,570)));Assert.That(Get(text,"fontSizeMax"),Is.EqualTo(27f));Call(text,"ForceMeshUpdate",true,true);var margin=(Vector4)Get(text,"margin");float width=rect.rect.width-margin.x-margin.z-12;Func<string,float> measure=value=>((Vector2)Call(text,"GetPreferredValues",value)).x;
            var report=Sports("SportsTruckCompatibilitySource").GetMethod("Original").Invoke(null,new object[]{"cf45e01e9f5946bbb2ca770464d651fb","Player 1",Draft("truck.monster"),null,"This sport uses its original truck appearance. Your saved body and accessories are kept for other sports."});var raw=(string[])Get(report,"paragraphs");Call(report,"FitDetails",measure,width);var lines=(string[])Get(report,"lines");Assert.That(lines.Any(line=>new System.Globalization.StringInfo(line).LengthInTextElements>44),Is.True,"Use actual available width rather than retaining the old character cap.");Assert.That(Get(report,"DetailPages"),Is.EqualTo(1));Assert.That(AllPages(report),Is.EqualTo(string.Concat(raw)));foreach(var line in lines)Assert.That(measure(line),Is.LessThanOrEqualTo(width+.01f));Assert.That(lines.Any(line=>line.Contains("ORIGINAL SPORT TRUCK")),Is.True);text.GetType().GetProperty("text",Any).SetValue(text,Call(report,"Details",0));Call(text,"ForceMeshUpdate",true,true);Assert.That(Get(text,"isTextOverflowing"),Is.EqualTo(false));Assert.That(Get(text,"fontSizeMax"),Is.EqualTo(27f));Assert.That((float)Get(text,"fontSize"),Is.EqualTo(27f).Within(.001f));TestContext.WriteLine("FIT_DETAIL_CAPACITY actualRect="+rect.rect+" fontMax27 unchanged; usedWidth="+width+" lines="+lines.Length+" page1; longestMeasured="+lines.Max(measure));
        }
        finally{UnityEngine.Object.DestroyImmediate(root);EditorSceneManager.ClosePreviewScene(preview);}
    }
    [Test]public void SavedHostEvaluationMatchesAuthorityWithoutPreparingModelsOrChangingPreferences()
    {
        var prefs=(IDisposable)Sports("SportsPrefs").GetMethod("IsolateValidation").Invoke(null,new object[]{"FitReport"+Guid.NewGuid().ToString("N")});Sports("SportsParticipantRegistry").GetMethod("ForgetCache").Invoke(null,null);var setup=EditorSceneManager.GetSceneManagerSetup();
        try
        {
            var scene=EditorSceneManager.OpenScene("Assets/TruckSumo/Scenes/TruckSumo.unity",OpenSceneMode.Single);var source=scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren(Sports("SportsTruckCompatibilitySource"),true)).Cast<Component>().Single();var host=Call(source,"Host",0);Assert.That(host,Is.Not.Null,"Explicit authored fixed-owner host required.");
            var draft=Draft("saved.unavailable.body");Select(draft,"hat","saved.missing.hat","saved.future.variant");Select(draft,"mirrors","saved.missing.mirrors");string input=JsonUtility.ToJson(draft);int revision=(int)Get(host,"AppearanceRevision");var root=Get(host,"AppearanceRoot");string profile=(string)Get(host,"ProfileId");var renderers=((Component)host).GetComponentsInChildren<Renderer>(true);var rendererStates=renderers.Select(renderer=>renderer.enabled).ToArray();int transforms=scene.GetRootGameObjects().Sum(item=>item.GetComponentsInChildren<Transform>(true).Length);
            // Warm registry load before recording serialized preference bytes.
            Sports("SportsParticipantRegistry").GetProperty("Current").GetValue(null);string bytes=(string)Sports("SportsPrefs").GetMethod("GetString").Invoke(null,new object[]{"TruckSports.Participants.v1",""});
            var authority=Call(host,"EvaluateDraft",draft);Assert.That(Get(authority,"canApply"),Is.EqualTo(true),"Saved default body must have a proven compatibility binding.");
            for(int repeat=0;repeat<25;repeat++)
            {
                var view=Call(source,"Evaluate",0,"stable-profile-fixture",draft);Assert.That(Get(view,"canApply"),Is.EqualTo(Get(authority,"canApply")));Assert.That(Get(view,"bodyFallback"),Is.EqualTo(true));StringAssert.Contains((string)Get(authority,"effectiveBodyId"),(string)Get(view,"effective"));Assert.That(Get(view,"unavailable"),Is.EqualTo(((Array)Get(authority,"unavailableSlots")).Length));StringAssert.Contains("saved.future.variant",AllPages(view));StringAssert.Contains(((string)Get(authority,"notice")).Replace("\n",""),AllPages(view));
            }
            Assert.That(JsonUtility.ToJson(draft),Is.EqualTo(input));Assert.That(Get(host,"AppearanceRevision"),Is.EqualTo(revision));Assert.That(Get(host,"AppearanceRoot"),Is.SameAs(root));Assert.That(Get(host,"ProfileId"),Is.EqualTo(profile));CollectionAssert.AreEqual(rendererStates,renderers.Select(renderer=>renderer.enabled));Assert.That(scene.GetRootGameObjects().Sum(item=>item.GetComponentsInChildren<Transform>(true).Length),Is.EqualTo(transforms));Assert.That((string)Sports("SportsPrefs").GetMethod("GetString").Invoke(null,new object[]{"TruckSports.Participants.v1",""}),Is.EqualTo(bytes));Assert.That(scene.isDirty,Is.False);
        }
        finally{RestoreScenes(setup);Sports("SportsParticipantRegistry").GetMethod("ForgetCache").Invoke(null,null);prefs.Dispose();}
    }
    [Test]public void ExistingCustomizerOpenStaysUniqueAndSportEntryHasDistinctName()
    {
        var type=Sports("SportsTruckCustomizerMenu");Assert.That(type.GetMethods().Count(method=>method.Name=="Open"),Is.EqualTo(1));Assert.That(type.GetMethod("Open").GetParameters().Length,Is.EqualTo(2));Assert.That(type.GetMethod("OpenForSport").GetParameters().Length,Is.EqualTo(3));
    }
    [Test]public void RegisteredAuthorTargetsExcludeBothProtectedPlowingScenesBeforeOpening()
    {
        var author=Type.GetType("LWS.TruckSports.Editor.SportsTruckCompatibilityAuthoring, Assembly-CSharp-Editor",true);var builder=Type.GetType("LWS.TruckSports.Editor.SportsBuilder, Assembly-CSharp-Editor",true);var registered=(string[])builder.GetMethod("SceneList").Invoke(null,null);var targets=(string[])author.GetMethod("RegisteredTargets").Invoke(null,null);
        var plow=Type.GetType("LWS.TruckPedestrianPlowing.Editor.PlowAuthoring, Assembly-CSharp-Editor",true);var cab=Type.GetType("LWS.TruckPedestrianPlowing.Editor.PlowCabAuthoring, Assembly-CSharp-Editor",true);string normal=(string)plow.GetField("Scene").GetRawConstantValue(),firstPerson=(string)cab.GetField("Scene").GetRawConstantValue();
        CollectionAssert.Contains(registered,normal);CollectionAssert.Contains(registered,firstPerson);CollectionAssert.DoesNotContain(targets,normal);CollectionAssert.DoesNotContain(targets,firstPerson);CollectionAssert.AreEqual(registered.Skip(1).Where(path=>path!=normal&&path!=firstPerson),targets);Assert.That(author.GetMethod("IsExcludedScene").Invoke(null,new object[]{firstPerson.Replace('/','\\')}),Is.EqualTo(true));
    }
    [TestCase("TruckHockey"),TestCase("TruckAirHockey"),TestCase("TruckBilliards"),TestCase("TruckPoker")]
    public void StaticOwnerMapUpgradeProvesActualHostsAndRefusesPartialCustomSlotOrFitChanges(string mode)
    {
        var previous=EditorSceneManager.GetSceneManagerSetup();
        try
        {
            var scene=EditorSceneManager.OpenScene("Assets/"+mode+"/Scenes/"+mode+".unity",OpenSceneMode.Single);var source=scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren(Sports("SportsTruckCompatibilitySource"),true)).Cast<Component>().Single();var author=Type.GetType("LWS.TruckSports.Editor.SportsTruckCompatibilityAuthoring, Assembly-CSharp-Editor",true);var prove=author.GetMethod("ProvenStaticSetupMap");var can=author.GetMethod("CanUpgradeOwnedStaticSetupBindings");var original=(Array)Get(source,"participantHosts");var target=(Array)prove.Invoke(null,new object[]{source});var hosts=target.Cast<object>().Where(host=>host!=null).ToArray();Assert.That(hosts.Length,Is.EqualTo(mode=="TruckHockey"?6:mode=="TruckPoker"?8:2));var revisions=hosts.Select(host=>Get(host,"AppearanceRevision")).ToArray();var roots=hosts.Select(host=>Get(host,"AppearanceRoot")).ToArray();
            try
            {
                Set(source,"participantHosts",target);Assert.That(can.Invoke(null,new object[]{source,target}),Is.EqualTo(false),"Exact established mapping is preserved idempotently.");var empty=Array.CreateInstance(target.GetType().GetElementType(),target.Length);Set(source,"participantHosts",empty);Assert.That(can.Invoke(null,new object[]{source,target}),Is.EqualTo(true),"Only an owned entirely unmapped source can upgrade.");empty.SetValue(hosts[0],0);Assert.That(Assert.Throws<TargetInvocationException>(()=>can.Invoke(null,new object[]{source,target})).InnerException,Is.TypeOf<InvalidOperationException>(),"Even a nonempty partial correct map is preserved/refused.");empty.SetValue(null,0);
                int slot=(int)Get(hosts[0],"participantSlot");try{Set(hosts[0],"participantSlot",slot+1);Assert.That(Assert.Throws<TargetInvocationException>(()=>prove.Invoke(null,new object[]{source})).InnerException,Is.TypeOf<InvalidOperationException>());}finally{Set(hosts[0],"participantSlot",slot);}
                var owner=Get(source,"hockey")??Get(source,"airHockey")??Get(source,"billiards")??Get(source,"session");var authored=(Array)Get(owner,"appearanceHosts");var first=authored.GetValue(0);var second=authored.GetValue(1);try{authored.SetValue(second,0);authored.SetValue(first,1);Assert.That(Assert.Throws<TargetInvocationException>(()=>prove.Invoke(null,new object[]{source})).InnerException,Is.TypeOf<InvalidOperationException>(),"A real host bound to another original physical root must be refused.");}finally{authored.SetValue(first,0);authored.SetValue(second,1);}
                var fits=(Array)Get(hosts[0],"compatibleBodies");var fit=fits.GetValue(0);var scale=(Vector3)Get(fit,"scale");try{Set(fit,"scale",Vector3.zero);Assert.That(Assert.Throws<TargetInvocationException>(()=>prove.Invoke(null,new object[]{source})).InnerException,Is.TypeOf<InvalidOperationException>());}finally{Set(fit,"scale",scale);}
                var valid=(Array)prove.Invoke(null,new object[]{source});CollectionAssert.AreEqual(target,valid);CollectionAssert.AreEqual(revisions,hosts.Select(host=>Get(host,"AppearanceRevision")));CollectionAssert.AreEqual(roots,hosts.Select(host=>Get(host,"AppearanceRoot")));
            }
            finally{Set(source,"participantHosts",original);}
        }
        finally{RestoreScenes(previous);}
    }
    [Test]public void HighTitleUsesReviewedUpperStripAndFullyObstructedLayoutsRefuseWithoutMovingAnything()
    {
        var scene=EditorSceneManager.NewPreviewScene();var root=new GameObject("Reviewed setup layout",typeof(RectTransform));SceneManager.MoveGameObjectToScene(root,scene);var page=root.GetComponent<RectTransform>();page.sizeDelta=new Vector2(1920,1080);var author=Type.GetType("LWS.TruckSports.Editor.SportsTruckCompatibilityAuthoring, Assembly-CSharp-Editor",true);var pick=author.GetMethod("PickSetupRow");
        RectTransform Graphic(string name,Vector2 size,Vector2 at){var child=new GameObject(name,typeof(RectTransform));child.transform.SetParent(root.transform,false);var rect=child.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);rect.sizeDelta=size;rect.anchoredPosition=at;child.AddComponent(Type.GetType("UnityEngine.UI.Image, UnityEngine.UI",true));return rect;}
        try
        {
            var title=Graphic("Title",new Vector2(1350,58),new Vector2(0,430));var logo=Graphic("Original sport title artwork",new Vector2(250,125),new Vector2(-545,372));object[] args={page,null,null};pick.Invoke(null,args);Assert.That(((Rect)args[1]).center.y,Is.EqualTo(492));Assert.That(((Rect)args[2]).yMin-459,Is.GreaterThanOrEqualTo(8));Assert.That(((Rect)args[2]).yMax,Is.LessThanOrEqualTo(532));Assert.That(title.anchoredPosition,Is.EqualTo(new Vector2(0,430)));Assert.That(title.sizeDelta,Is.EqualTo(new Vector2(1350,58)));Assert.That(logo.anchoredPosition,Is.EqualTo(new Vector2(-545,372)));Assert.That(title.gameObject.activeSelf,Is.True);Assert.That(logo.gameObject.activeSelf,Is.True);
            Graphic("Designer upper content",new Vector2(1460,88),new Vector2(0,492));Graphic("Designer lower content",new Vector2(1460,80),new Vector2(0,-492));Assert.That(Assert.Throws<TargetInvocationException>(()=>pick.Invoke(null,new object[]{page,null,null})).InnerException,Is.TypeOf<InvalidOperationException>(),"Do not overlap, hide or move original content when all reviewed strips are occupied.");Assert.That(title.gameObject.activeSelf,Is.True);Assert.That(title.anchoredPosition,Is.EqualTo(new Vector2(0,430)));
        }
        finally{UnityEngine.Object.DestroyImmediate(root);EditorSceneManager.ClosePreviewScene(scene);}
    }
    [Test]public void SavedHockeyFitRowClearsTheOriginalHighTitleAndLogo()
    {
        var previous=EditorSceneManager.GetSceneManagerSetup();
        try
        {
            var scene=EditorSceneManager.OpenScene("Assets/TruckHockey/Scenes/TruckHockey.unity",OpenSceneMode.Single);var source=scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren(Sports("SportsTruckCompatibilitySource"),true)).Cast<Component>().Single();var page=((GameObject)Get(source,"setupPage")).transform;var title=(RectTransform)page.Find("Title");var logo=(RectTransform)page.Find("Original sport title artwork");Assert.That(title.anchoredPosition,Is.EqualTo(new Vector2(0,430)));Assert.That(title.sizeDelta,Is.EqualTo(new Vector2(1350,58)));Assert.That(title.gameObject.activeSelf,Is.True);Assert.That(logo.anchoredPosition,Is.EqualTo(new Vector2(-545,372)));Assert.That(logo.sizeDelta,Is.EqualTo(new Vector2(250,125)));Assert.That(logo.gameObject.activeSelf,Is.True);
            var presenter=source.GetComponent(Sports("SportsTruckCompatibilityPresenter"));var summary=(RectTransform)((Component)Get(presenter,"subtitle")).transform;var button=(RectTransform)((Component)Get(presenter,"reportButton")).transform;Assert.That(summary.anchoredPosition.y,Is.EqualTo(492));Assert.That(button.anchoredPosition.y,Is.EqualTo(492));Assert.That(button.anchoredPosition.y-button.sizeDelta.y*.5f-459,Is.GreaterThanOrEqualTo(8));Assert.That(summary.anchoredPosition.y+summary.sizeDelta.y*.5f,Is.LessThanOrEqualTo(532));
        }
        finally{RestoreScenes(previous);}
    }
    [Test]public void HeadlessEmptyPriorSetupSkipsRestoreAndValidInteractiveSetupStillRestores()
    {
        var author=Type.GetType("LWS.TruckSports.Editor.SportsTruckCompatibilityAuthoring, Assembly-CSharp-Editor",true);var decision=author.GetMethod("ShouldRestoreSceneSetup");var empty=Array.Empty<SceneSetup>();Assert.That(decision.Invoke(null,new object[]{empty,true}),Is.EqualTo(false));var unloaded=new[]{new SceneSetup{path="Assets/TruckHockey/Scenes/TruckHockey.unity",isLoaded=false,isActive=false}};Assert.That(decision.Invoke(null,new object[]{unloaded,true}),Is.EqualTo(false));Assert.That(Assert.Throws<TargetInvocationException>(()=>decision.Invoke(null,new object[]{empty,false})).InnerException,Is.TypeOf<InvalidOperationException>(),"Invalid interactive setup is not silently discarded.");
        var valid=new[]{new SceneSetup{path="Assets/TruckHockey/Scenes/TruckHockey.unity",isLoaded=true,isActive=true},new SceneSetup{path="Assets/TruckAirHockey/Scenes/TruckAirHockey.unity",isLoaded=true,isActive=false}};Assert.That(decision.Invoke(null,new object[]{valid,false}),Is.EqualTo(true));Assert.That(decision.Invoke(null,new object[]{valid,true}),Is.EqualTo(true));Assert.That(valid[0].isActive&&valid[0].isLoaded&&valid[1].isLoaded,Is.True);
    }
    [Test]public void TugLocalMapChecksOriginalWheelPiecesAndRefusesPartialOrBorrowedOwnership()
    {
        var previous=EditorSceneManager.GetSceneManagerSetup();
        try
        {
            var scene=EditorSceneManager.OpenScene("Assets/TruckTugOfWar/Scenes/TruckTugOfWar.unity",OpenSceneMode.Single);var source=scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren(Sports("SportsTruckCompatibilitySource"),true)).Cast<Component>().Single();var match=Get(source,"session");var author=Type.GetType("LWS.TruckSports.Editor.SportsTruckCompatibilityAuthoring, Assembly-CSharp-Editor",true);var prove=author.GetMethod("ProvenTugSetupMap");var can=author.GetMethod("CanUpgradeOwnedTugSetupBindings");var target=(Array)prove.Invoke(null,new[]{match});var original=(Array)Get(source,"participantHosts");var local=(Array)Get(match,"tugAppearanceHosts");Assert.That(local.Length,Is.EqualTo(8));Assert.That(((Array)Get(match,"appearanceHosts")).Length,Is.Zero);var revisions=local.Cast<object>().Select(host=>Get(host,"AppearanceRevision")).ToArray();
            try
            {
                Set(source,"participantHosts",target);Assert.That(can.Invoke(null,new object[]{source,target}),Is.EqualTo(false));var empty=Array.CreateInstance(target.GetType().GetElementType(),target.Length);Set(source,"participantHosts",empty);Assert.That(can.Invoke(null,new object[]{source,target}),Is.EqualTo(true));empty.SetValue(local.GetValue(0),0);Assert.That(Assert.Throws<TargetInvocationException>(()=>can.Invoke(null,new object[]{source,target})).InnerException,Is.TypeOf<InvalidOperationException>());empty.SetValue(null,0);
                var first=local.GetValue(0);var second=local.GetValue(1);try{local.SetValue(second,0);local.SetValue(first,1);Assert.That(Assert.Throws<TargetInvocationException>(()=>prove.Invoke(null,new[]{match})).InnerException,Is.TypeOf<InvalidOperationException>());}finally{local.SetValue(first,0);local.SetValue(second,1);}
                var fragment=Get(first,"fragmentPresentation");var mapped=(Transform[])Get(fragment,"eventWheelPieceSources");var wheel=mapped[0];try{mapped[0]=mapped[1];Assert.That(Assert.Throws<TargetInvocationException>(()=>prove.Invoke(null,new[]{match})).InnerException,Is.TypeOf<InvalidOperationException>(),"Actual original wheel-piece mapping is required; four guessed wheel names are insufficient.");}finally{mapped[0]=wheel;}
                CollectionAssert.AreEqual(target,(Array)prove.Invoke(null,new[]{match}));CollectionAssert.AreEqual(revisions,local.Cast<object>().Select(host=>Get(host,"AppearanceRevision")));
            }
            finally{Set(source,"participantHosts",original);}
        }
        finally{RestoreScenes(previous);}
    }
}
#endif

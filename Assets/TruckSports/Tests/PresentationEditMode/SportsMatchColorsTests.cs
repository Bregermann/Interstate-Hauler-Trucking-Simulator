#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class SportsMatchColorsTests
{
    const BindingFlags Any=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static Type Runtime(string name)=>Type.GetType("LWS.TruckSports."+name+", Assembly-CSharp",true);
    static Type Player=>Type.GetType("LWS.TruckBowling.BowlingPlayer, Assembly-CSharp",true);
    static object Get(object owner,string name)=>owner.GetType().GetField(name,Any) is FieldInfo field?field.GetValue(owner):owner.GetType().GetProperty(name,Any).GetValue(owner);
    static void Set(object owner,string name,object value)=>owner.GetType().GetField(name,Any).SetValue(owner,value);
    static object Run(object owner,string name,params object[] args)=>owner.GetType().GetMethods(Any).Single(method=>method.Name==name&&method.GetParameters().Length==args.Length).Invoke(owner,args);
    static object Registry(string name,params object[] args)=>Runtime("SportsParticipantRegistry").GetMethod(name).Invoke(null,args);
    static object Prefs(string name,params object[] args)=>Runtime("SportsPrefs").GetMethod(name).Invoke(null,args);
    static object Assignment(Array slots,int count)=>Activator.CreateInstance(Runtime("SportsMatchColors"),new object[]{slots,count});
    static Color[] Colors(Array slots)=>slots.Cast<object>().Select(slot=>(Color)Get(slot,"color")).ToArray();
    static Array Slots(int count)
    {
        var slots=Array.CreateInstance(Player,count);for(int i=0;i<count;i++){var slot=Activator.CreateInstance(Player);Set(slot,"participantId","stable-"+i.ToString("D2"));Set(slot,"name","Same Driver");Set(slot,"color",new Color(.04f+i*.0005f,.28f,.92f));slots.SetValue(slot,i);}return slots;
    }
    static void Distinct(Color[] colors)
    {
        foreach(var color in colors){Assert.That(float.IsFinite(color.r)&&float.IsFinite(color.g)&&float.IsFinite(color.b),Is.True);Assert.That(color.a,Is.EqualTo(1));}
        for(int i=0;i<colors.Length;i++)for(int j=i+1;j<colors.Length;j++)Assert.That((float)Runtime("SportsMatchColors").GetMethod("Distance").Invoke(null,new object[]{colors[i],colors[j]}),Is.GreaterThanOrEqualTo(22f-.001f));
    }
    IDisposable prefs;string production;
    [SetUp]public void Setup(){production=PlayerPrefs.GetString("TruckSports.Participants.v1","");prefs=(IDisposable)Prefs("IsolateValidation","MatchColors"+Guid.NewGuid().ToString("N"));Registry("ForgetCache");}
    [TearDown]public void Cleanup(){foreach(string key in new[]{"TruckSports.Participants.v1","TruckSports.Participants.v1.Backup"})Prefs("DeleteKey",key);Registry("ForgetCache");prefs.Dispose();Assert.That(PlayerPrefs.GetString("TruckSports.Participants.v1",""),Is.EqualTo(production));}

    [Test]public void TwelveNearDuplicatePreferredColorsResolveWithStableIdsAndUnchangedDuplicateNames()
    {
        for(int count=1;count<=12;count++)
        {
            var slots=Slots(count);var preferred=Colors(slots);var assignment=Assignment(slots,count);try{Distinct(Colors(slots));for(int i=0;i<count;i++){Assert.That(Get(slots.GetValue(i),"participantId"),Is.EqualTo("stable-"+i.ToString("D2")));Assert.That(Get(slots.GetValue(i),"name"),Is.EqualTo("Same Driver"));Assert.That(Run(assignment,"PreferredColor",Get(slots.GetValue(i),"participantId")),Is.EqualTo(preferred[i]));}}finally{((IDisposable)assignment).Dispose();}Assert.That(Colors(slots),Is.EqualTo(preferred));
        }
    }
    [Test]public void DistinctPreferencesAndCompletePaletteStayDistinctAndRematchIsDeterministicByIdentity()
    {
        var slots=Slots(12);for(int i=0;i<12;i++)Set(slots.GetValue(i),"color",Runtime("SportsMatchColors").GetMethod("PaletteColor").Invoke(null,new object[]{i}));var preferred=Colors(slots);Distinct(preferred);var assignment=Assignment(slots,12);Assert.That(Colors(slots),Is.EqualTo(preferred));((IDisposable)assignment).Dispose();
        slots=Slots(12);assignment=Assignment(slots,12);var byId=slots.Cast<object>().ToDictionary(slot=>(string)Get(slot,"participantId"),slot=>(Color)Get(slot,"color"));((IDisposable)assignment).Dispose();var reversed=slots.Cast<object>().Reverse().ToArray();for(int i=0;i<12;i++)slots.SetValue(reversed[i],i);assignment=Assignment(slots,12);try{foreach(var slot in slots)Assert.That(Get(slot,"color"),Is.EqualTo(byId[(string)Get(slot,"participantId")]));}finally{((IDisposable)assignment).Dispose();}
    }
    [Test]public void NestedSuspendAndThrowCaptureActualPreferenceEditsButRestoreTheSameEffectiveMatchColors()
    {
        var slots=Slots(12);var preferred=Colors(slots);var assignment=Assignment(slots,12);var effective=Colors(slots);string id=(string)Get(slots.GetValue(0),"participantId");
        Assert.Throws<InvalidOperationException>(()=>
        {
            using((IDisposable)Run(assignment,"Suspend"))
            {
                Assert.That(Colors(slots),Is.EqualTo(preferred));using((IDisposable)Run(assignment,"Suspend")){Set(slots.GetValue(0),"color",Color.magenta);}Assert.That(Get(slots.GetValue(0),"color"),Is.EqualTo(Color.magenta));throw new InvalidOperationException("deliberate scoped save failure");
            }
        });
        Assert.That(Colors(slots),Is.EqualTo(effective));Assert.That(Run(assignment,"PreferredColor",id),Is.EqualTo(Color.magenta));var outer=(IDisposable)Run(assignment,"Suspend");var inner=(IDisposable)Run(assignment,"Suspend");Run(assignment,"Restore");inner.Dispose();outer.Dispose();Assert.That(Get(slots.GetValue(0),"color"),Is.EqualTo(Color.magenta));Assert.That(Get(assignment,"Active"),Is.False);((IDisposable)assignment).Dispose();using((IDisposable)Run(assignment,"Suspend"))Assert.That(Get(slots.GetValue(0),"color"),Is.EqualTo(Color.magenta));
    }
    [Test]public void RealProfileBindingSaveAndReloadKeepPreferredRegistryBytesAndRuntimeColorsSeparate()
    {
        for(int i=0;i<12;i++){var profile=Registry("Default",i);Registry("Rename",Get(profile,"id"),"Same Driver",new Color(.04f+i*.0005f,.28f,.92f),true);}var slots=Slots(12);var binding=Activator.CreateInstance(Runtime("SportsParticipantBinding"),new object[]{slots});var preferred=Colors(slots);string primary=(string)Prefs("GetString","TruckSports.Participants.v1",""),backup=(string)Prefs("GetString","TruckSports.Participants.v1.Backup","");var assignment=Assignment(slots,12);var effective=Colors(slots);
        using((IDisposable)Run(assignment,"Suspend")){using((IDisposable)Run(assignment,"Suspend"))Run(binding,"Refresh",true);Assert.That(Colors(slots),Is.EqualTo(preferred));}Assert.That(Colors(slots),Is.EqualTo(effective));Assert.That(Prefs("GetString","TruckSports.Participants.v1",""),Is.EqualTo(primary));Assert.That(Prefs("GetString","TruckSports.Participants.v1.Backup",""),Is.EqualTo(backup));Registry("ForgetCache");using((IDisposable)Run(assignment,"Suspend"))Run(binding,"Refresh",false);Assert.That(Colors(slots),Is.EqualTo(effective));((IDisposable)assignment).Dispose();Assert.That(Colors(slots),Is.EqualTo(preferred));Assert.That(Prefs("GetString","TruckSports.Participants.v1",""),Is.EqualTo(primary));
    }
    [Test]public void ReorderedSeatsWithinSuspensionKeepTheirExistingStableIdentityColors()
    {
        var slots=Slots(12);var assignment=Assignment(slots,12);var effective=slots.Cast<object>().ToDictionary(slot=>(string)Get(slot,"participantId"),slot=>(Color)Get(slot,"color"));using((IDisposable)Run(assignment,"Suspend")){var first=slots.GetValue(0);slots.SetValue(slots.GetValue(11),0);slots.SetValue(first,11);}foreach(var slot in slots)Assert.That(Get(slot,"color"),Is.EqualTo(effective[(string)Get(slot,"participantId")]));((IDisposable)assignment).Dispose();
    }
    [Test]public void InvalidIdentityRosterRefusesBeforeAnyTemporaryColorWrites()
    {
        var slots=Slots(12);Set(slots.GetValue(1),"participantId",Get(slots.GetValue(0),"participantId"));var before=Colors(slots);Assert.Throws<TargetInvocationException>(()=>Assignment(slots,12));Assert.That(Colors(slots),Is.EqualTo(before));
    }
}
#endif

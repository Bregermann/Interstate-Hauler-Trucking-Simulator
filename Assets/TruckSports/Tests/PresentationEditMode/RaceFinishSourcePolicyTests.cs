#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class RaceFinishSourcePolicyTests
{
    const BindingFlags Any=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static Type Runtime(string name)=>Type.GetType(name+", Assembly-CSharp",true);
    static object Get(object owner,string name)=>Field(owner.GetType(),name) is FieldInfo field?field.GetValue(owner):owner.GetType().GetProperty(name,Any).GetValue(owner);
    static FieldInfo Field(Type type,string name){while(type!=null){var field=type.GetField(name,Any|BindingFlags.DeclaredOnly);if(field!=null)return field;type=type.BaseType;}return null;}
    static void Set(object owner,string name,object value)=>Field(owner.GetType(),name).SetValue(owner,value);
    static object Call(object owner,string name,params object[] args)=>owner.GetType().GetMethods(Any).Single(method=>method.Name==name&&method.GetParameters().Length==args.Length).Invoke(owner,args);
    static IList List()=> (IList)Activator.CreateInstance(typeof(System.Collections.Generic.List<>).MakeGenericType(Runtime("LWS.TruckSports.SportsRaceFinishReceipt")));
    static object Receipt(string id,float time){var row=Activator.CreateInstance(Runtime("LWS.TruckSports.SportsRaceFinishReceipt"));Set(row,"id",id);Set(row,"seconds",time);return row;}
    static Component Match(GameObject root,string type,int count)
    {
        var match=root.AddComponent(Runtime(type));Set(Get(match,"setup"),"count",count);Set(match,"<Running>k__BackingField",true);Set(match,"participantMatchId","pure-finish-policy");
        var players=(Array)Get(Get(match,"setup"),"players");for(int i=0;i<count;i++)Set(players.GetValue(i),"participantId","fixture-profile-"+i);return match;
    }
    static Component Truck(Transform parent)
    {
        var go=new GameObject("Inactive pure fixture physical shape");go.SetActive(false);go.transform.SetParent(parent);var body=go.AddComponent<Rigidbody>();body.isKinematic=true;var shape=go.AddComponent<BoxCollider>();var truck=go.AddComponent(Runtime("LWS.TruckSports.SportsEventTruck"));Set(truck,"body",body);Set(truck,"shape",shape);Set(truck,"visuals",go.transform);return truck;
    }
    [Test] public void ExactEightFinishLineTypesExcludeCountdownAndNonFinishSports()
    {
        var source=Runtime("LWS.TruckSports.SportsRaceFinishSource");string[] yes={"TruckMattressRacing.MattressMatch","TruckWifeCarrying.WifeMatch","TruckBogSnorkelling.BogMatch","TruckTerrierRacing.TerrierMatch","TruckAvalancheEscape.AvalancheMatch","TruckRedLightGreenLight.RedLightMatch","TruckMinefield.MinefieldMatch","TruckFrogger.FrogMatch"};
        string[] no={"TruckBaseJumping.BaseJumpMatch","TruckCheeseRolling.CheeseMatch","TruckSkyTow.SkyTowMatch","TruckNinjaWarrior.NinjaMatch","TruckTetherball.TetherMatch","TruckVolleyball.VolleyMatch","TruckLimbo.LimboMatch","TruckPedestrianPlowing.PlowMatch"};
        foreach(string name in yes.Concat(no))Assert.That(source.GetMethod("Supports").Invoke(null,new object[]{Runtime("LWS."+name)}),Is.EqualTo(yes.Contains(name)),name);
    }
    [Test] public void NativeSeriesTolerancePreservesSharedFirstRatherThanCallbackOrder()
    {
        var rows=List();rows.Add(Receipt("later-array-slot",12f));rows.Add(Receipt("earlier-array-slot",12.00005f));rows.Add(Receipt("real-third",12.00025f));
        Runtime("LWS.TruckSports.SportsRaceFinishSource").GetMethod("FirstTimes",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{rows,.0001f});
        Assert.That(Get(rows[0],"winner"),Is.True);Assert.That(Get(rows[1],"winner"),Is.True);Assert.That(Get(rows[2],"winner"),Is.False);Assert.That(Get(rows[0],"place"),Is.EqualTo(1));Assert.That(Get(rows[1],"place"),Is.EqualTo(1));Assert.That(Get(rows[2],"place"),Is.EqualTo(3));
        Assert.That(Get(rows[0],"seconds"),Is.EqualTo(12f));Assert.That(Get(rows[1],"seconds"),Is.EqualTo(12.00005f));
    }
    [Test] public void SequentialFirstClearCannotBecomeWinnerBeforeNativeComparison()
    {
        var root=new GameObject("Inactive Frog rules/read-only-source fixture");root.SetActive(false);
        try
        {
            var match=Match(root,"LWS.TruckFrogger.FrogMatch",2);var source=root.AddComponent(Runtime("LWS.TruckSports.SportsRaceFinishSource"));Set(source,"session",match);var trucks=Array.CreateInstance(Runtime("LWS.TruckSports.SportsEventTruck"),2);for(int i=0;i<2;i++)trucks.SetValue(Truck(root.transform),i);Set(match,"trucks",trucks);
            var rules=Get(match,"rules");Call(rules,"Reset",2,1,8,60f);Call(rules,"Begin");Call(rules,"Tick",3.1f);Assert.That(Call(rules,"Finish",0,.05f),Is.True);var rows=List();Call(source,"Read",rows);Assert.That(rows.Count,Is.Zero,"A successful first attempt is not the official winner.");
            Call(rules,"Tick",5.6f);Assert.That(Call(rules,"Finish",1,.08f),Is.True);Call(source,"Read",rows);Assert.That(rows.Count,Is.Zero);Call(rules,"Tick",2.5f);Assert.That(Get(rules,"Complete"),Is.True);Assert.That(Get(rules,"Winner"),Is.EqualTo(0));
            string before=JsonUtility.ToJson(rules);Call(source,"Read",rows);Assert.That(rows.Count,Is.EqualTo(2));Assert.That(rows.Cast<object>().Single(row=>(bool)Get(row,"winner")).GetType(),Is.EqualTo(Runtime("LWS.TruckSports.SportsRaceFinishReceipt")));Assert.That(Get(rows.Cast<object>().Single(row=>(bool)Get(row,"winner")),"slot"),Is.EqualTo(0));Assert.That(JsonUtility.ToJson(rules),Is.EqualTo(before));
        }
        finally{UnityEngine.Object.DestroyImmediate(root);}
    }
    [Test] public void TerrierTieAndMinefieldRestWinnerCannotInventFinishCelebrations()
    {
        foreach(bool terrier in new[]{true,false})
        {
            var root=new GameObject("Inactive unique-finish exception fixture");root.SetActive(false);
            try
            {
                var match=Match(root,terrier?"LWS.TruckTerrierRacing.TerrierMatch":"LWS.TruckMinefield.MinefieldMatch",2);var source=root.AddComponent(Runtime("LWS.TruckSports.SportsRaceFinishSource"));Set(source,"session",match);var rows=List();
                if(terrier){Set(match,"<Heat>k__BackingField",1);var times=(float[])Get(match,"finishTimes");Array.Fill(times,float.PositiveInfinity);times[0]=times[1]=8f;Set(match,"<HeatWinner>k__BackingField",-1);Call(source,"Read",rows);Assert.That(rows.Count,Is.Zero);Assert.That(Get(source,"TieWithoutWinner"),Is.True);}
                else
                {
                    var racerType=Runtime("LWS.TruckMinefield.MinefieldRacer");var go=new GameObject("Inactive landed racer");go.SetActive(false);go.transform.SetParent(root.transform);var racer=go.AddComponent(racerType);var array=Array.CreateInstance(racerType,2);array.SetValue(racer,0);Set(match,"racers",array);Set(match,"<Winner>k__BackingField",0);Set(racer,"truck",Truck(root.transform));var phase=Get(racer,"Phase").GetType();Set(racer,"<Phase>k__BackingField",Enum.Parse(phase,"Landed"));Set(racer,"<FinishTime>k__BackingField",12f);Call(source,"Read",rows);Assert.That(rows.Count,Is.Zero,"A farthest-rest winner did not cross the mile while driving.");
                    Set(racer,"<Phase>k__BackingField",Enum.Parse(phase,"DrivenFinish"));Call(source,"Read",rows);Assert.That(rows.Count,Is.EqualTo(1));Assert.That(Get(rows[0],"winner"),Is.True);Assert.That(Get(rows[0],"gag"),Is.False);
                }
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
        }
    }
}
#endif

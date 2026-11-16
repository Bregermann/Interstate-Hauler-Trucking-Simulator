#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class DragonKingdomNativeRoadCrossingTests
{
    static Type Provider=>Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomNativeRoadCrossings, Assembly-CSharp-Editor",true);
    static Type Author=>Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomWorldAuthoring, Assembly-CSharp-Editor",true);
    static object Field(object row,string name)=>row.GetType().GetField(name).GetValue(row);
    static object Road(string id,float width,params Vector3[] points)=>Activator.CreateInstance(Author.GetNestedType("RoadSpec"),new object[]{id,"","",width,points});
    static object[] Find(Vector3[] creek,params object[] roads)
    {
        var array=Array.CreateInstance(Author.GetNestedType("RoadSpec"),roads.Length);for(int i=0;i<roads.Length;i++)array.SetValue(roads[i],i);
        return ((IEnumerable)Provider.GetMethod("Find").Invoke(null,new object[]{array,creek,3.125f})).Cast<object>().ToArray();
    }
    [Test]
    public void OrthogonalCrossingUsesActualCorridorAndBodyMarginRatherThanVisualArchAperture()
    {
        var rows=Find(null,Road("upper",20,new Vector3(-200,24,0),new Vector3(200,24,0)),Road("lower",18,new Vector3(0,0,-200),new Vector3(0,0,200)));
        Assert.That(rows.Length,Is.EqualTo(1));var r=rows[0];
        Assert.That((float)Field(r,"lowerCorridorHalfWidth"),Is.EqualTo(9));Assert.That((float)Field(r,"upperCorridorHalfWidth"),Is.EqualTo(10));
        Assert.That((float)Field(r,"start"),Is.GreaterThan(180));Assert.That((float)Field(r,"end"),Is.LessThan(220));
        // The original +/- .35*18 wheel positions and full body width
        // fit inside the actual road corridor, including its edge cases.
        foreach(float lateral in new[]{-.35f*18,0,.35f*18})
            Assert.That(Mathf.Abs(lateral)+2.2f,Is.LessThanOrEqualTo((float)Field(r,"lowerCorridorHalfWidth")));
    }
    [Test]
    public void ObliqueStripEnvelopeContainsEveryActualUpperAndLowerRoadFootprintIntersection()
    {
        float angle=30*Mathf.Deg2Rad;var direction=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
        var row=Find(null,Road("upper",20,new Vector3(-250,24,0),new Vector3(250,24,0)),Road("lower",18,-direction*250,direction*250)).Single();
        float start=(float)Field(row,"start")-250,end=(float)Field(row,"end")-250;int intersections=0;
        for(float x=-70;x<=70;x+=.5f)for(float z=-10;z<=10;z+=.5f)
            if(Mathf.Abs(-Mathf.Sin(angle)*x+Mathf.Cos(angle)*z)<=9)
            {Assert.That(x,Is.InRange(start,end));intersections++;}
        Assert.That(intersections,Is.GreaterThan(1000));Assert.That(end-start,Is.LessThan(100));
    }
    [Test]
    public void AtGradeJunctionAndParallelRoadsDoNotBecomeBridgesAndLowOverpassFailsExplicitly()
    {
        var upper=Road("upper",20,new Vector3(-100,0,0),new Vector3(100,0,0));
        Assert.That(Find(null,upper,Road("junction",20,new Vector3(0,0,-100),new Vector3(0,0,100))),Is.Empty);
        Assert.That(Find(null,upper,Road("parallel",20,new Vector3(-100,20,50),new Vector3(100,20,50))),Is.Empty);
        var error=Assert.Throws<TargetInvocationException>(()=>Find(null,upper,Road("low",20,new Vector3(0,8,-100),new Vector3(0,8,100))));
        Assert.That(error.InnerException,Is.TypeOf<InvalidOperationException>());Assert.That(error.InnerException.Message,Does.Contain(">=9m"));
    }
    [Test]
    public void SourceStreamUsesTrue34MetreCreekCoreNotTheOld45MetreVisualProxy()
    {
        var row=Find(new[]{new Vector3(0,0,-200),new Vector3(0,0,200)},Road("forest-nests",16,new Vector3(-200,24,0),new Vector3(200,24,0))).Single();
        Assert.That((bool)Field(row,"lowerIsWater"),Is.True);Assert.That((float)Field(row,"lowerCorridorHalfWidth"),Is.EqualTo(34));
        Assert.That((float)Field(row,"end")-(float)Field(row,"start"),Is.InRange(88,89));
    }
    [Test]
    public void OriginalRoadsRetainAllSemanticPosesAndForestStartIsOutsideTrueStreamExclusion()
    {
        var roads=(Array)Author.GetMethod("Roads").Invoke(null,null);string before=string.Join("\n",roads.Cast<object>().Select(JsonUtility.ToJson));
        var rows=((IEnumerable)Provider.GetMethod("Find").Invoke(null,new object[]{roads,Author.GetMethod("CanyonWaterPolyline").Invoke(null,null),3.125f})).Cast<object>().ToArray();
        Assert.That(rows.Length,Is.GreaterThan(0));var creek=rows.Where(r=>(string)Field(r,"upper")=="forest-nests"&&(bool)Field(r,"lowerIsWater")).ToArray();
        Assert.That(creek.Length,Is.GreaterThan(0));Assert.That(creek.All(r=>(float)Field(r,"start")>20),Is.True,"The original grounded forest junction must not be reclassified by the far visual arch.");
        Assert.That(string.Join("\n",roads.Cast<object>().Select(JsonUtility.ToJson)),Is.EqualTo(before));
        foreach(var row in rows){Assert.That((float)Field(row,"end"),Is.GreaterThan((float)Field(row,"start")));Assert.That((float)Field(row,"start"),Is.GreaterThanOrEqualTo(0));Assert.That((float)Field(row,"end"),Is.LessThanOrEqualTo((float)Field(row,"upperLength")));}
        string[] args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonOutput");if(at>=0&&at+1<args.Length){Directory.CreateDirectory(args[at+1]);File.WriteAllText(Path.Combine(args[at+1],"actual-original-native-crossing-envelopes.json"),"[\n"+string.Join(",\n",rows.Select(r=>JsonUtility.ToJson(r,true)))+"\n]");}
    }
    [TestCase(23.92f,24f)]
    [TestCase(23.92f,float.PositiveInfinity)]
    [TestCase(23.92f,23.7f)]
    public void CompatibleUpperClearanceCannotSilentlyLowerActualGroundCore(float ground,float cap)
        =>Assert.That((float)Provider.GetMethod("RequireCompatibleGroundCore").Invoke(null,new object[]{ground,cap,"lower road","upper bridge"}),Is.EqualTo(ground));
    [Test]
    public void GenuineIncompatibleUpperEnvelopeRejectsInsteadOfHidingGroundConflictWithMinimum()
    {
        var error=Assert.Throws<TargetInvocationException>(()=>Provider.GetMethod("RequireCompatibleGroundCore").Invoke(null,new object[]{23.92f,14f,"forest junction","old broad bridge cap"}));
        Assert.That(error.InnerException,Is.TypeOf<InvalidOperationException>());Assert.That(error.InnerException.Message,Does.Contain("forest junction"));Assert.That(error.InnerException.Message,Does.Contain("old broad bridge cap"));
    }
    [TestCase(1.1139980554580688f,174.74197387695312f,290.8236999511719f,174.74197387695312f,290.8236999511719f,false)]
    [TestCase(0f,174.74197387695312f,290.8236999511719f,174.74197387695312f,290.8236999511719f,true)]
    [TestCase(1f,174.74197387695312f,290.8236999511719f,174.74197387695312f,290.8236999511719f,true)]
    [TestCase(.5f,174.74197387695312f,290.8236999511719f,174.74197387695312f,290.8236999511719f,true)]
    [TestCase(-.01f,174.74197387695312f,290.8236999511719f,174.74197387695312f,290.8236999511719f,false)]
    [TestCase(1.5f,25,50,0,100,true)]
    [TestCase(3.5f,25,50,0,100,false)]
    public void ActualV335EndpointBleedIsExcludedWhileTrueSpanAndInternalSegmentJoinsRemainEligible(float projection,float from,float to,float spanFrom,float spanTo,bool expected)
        =>Assert.That((bool)Provider.GetMethod("PhysicalBridgeSpanIncludesProjection").Invoke(null,new object[]{projection,from,to,spanFrom,spanTo}),Is.EqualTo(expected));
}
#endif

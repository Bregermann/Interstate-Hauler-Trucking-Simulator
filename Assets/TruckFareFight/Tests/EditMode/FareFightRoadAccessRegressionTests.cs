using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;

public sealed class FareFightRoadAccessRegressionTests
{
    [Test]public void EveryAuthoredPickupSupportsFourSeparatedEqualRoadDistanceStarts()
    {
        var district=AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets/TruckFareFight/Configuration/Original Taxi District.asset");
        Assert.That(district,Is.Not.Null);var districtType=district.GetType();var graph=districtType.GetField("graph").GetValue(district);
        var stops=(Array)districtType.GetField("stops").GetValue(district);
        var gridType=Type.GetType("LWS.TruckFareFight.FareFightStartingGrid, Assembly-CSharp",true);var grid=Activator.CreateInstance(gridType);
        foreach(var stop in stops)
        {
            var point=(Vector3)stop.GetType().GetField("position").GetValue(stop);var positions=new Vector3[4];var forwards=new Vector3[4];var distances=new float[4];
            bool valid=(bool)gridType.GetMethod("TryBuild").Invoke(grid,new object[]{graph,point,4,12f,0,positions,forwards,distances,null});
            Assert.That(valid,Is.True,"Four fair starts for "+point+" "+gridType.GetProperty("Diagnostic").GetValue(grid));
            Assert.That(distances.Max()-distances.Min(),Is.LessThan(24),"Driving-distance spread for "+point);
            for(int i=0;i<4;i++){Assert.That(forwards[i].magnitude,Is.EqualTo(1).Within(.01f));for(int j=0;j<i;j++)Assert.That(Vector3.Distance(positions[i],positions[j]),Is.GreaterThanOrEqualTo(12));}
        }
    }
    [Test]public void OffRoadTruckJoinsTheNearestRoadBeforeFollowingItsNetwork()
    {
        var graphType=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("LWS.InterstateHauler.LwsRoadGraph")).First(t=>t!=null);
        const string json=@"{""graphId"":""L-road-connector-fixture"",""nodes"":[{""nodeId"":""A"",""position"":{""x"":0,""y"":0,""z"":0}},{""nodeId"":""B"",""position"":{""x"":0,""y"":0,""z"":100}},{""nodeId"":""C"",""position"":{""x"":100,""y"":0,""z"":100}}],""edges"":[{""edgeId"":""AB"",""roadId"":""AB"",""segmentId"":""AB"",""fromNodeId"":""A"",""toNodeId"":""B"",""distanceMeters"":100,""samples"":[{""position"":{""x"":0,""z"":0},""forward"":{""z"":1},""distanceFromStartMeters"":0},{""position"":{""x"":0,""z"":100},""forward"":{""z"":1},""distanceFromStartMeters"":100}]},{""edgeId"":""BC"",""roadId"":""BC"",""segmentId"":""BC"",""fromNodeId"":""B"",""toNodeId"":""C"",""distanceMeters"":100,""samples"":[{""position"":{""x"":0,""z"":100},""forward"":{""x"":1},""distanceFromStartMeters"":0},{""position"":{""x"":100,""z"":100},""forward"":{""x"":1},""distanceFromStartMeters"":100}]}]}";
        var graph=JsonUtility.FromJson(json,graphType);
        var type=Type.GetType("LWS.TruckFareFight.FareFightRoute, Assembly-CSharp",true);var route=Activator.CreateInstance(type);
        type.GetField("useRoadConnectors")?.SetValue(route,true);
        var update=type.GetMethods().Single(m=>m.Name=="Update"&&m.GetParameters()[0].ParameterType==graphType);
        var origin=new Vector3(20,0,40);update.Invoke(route,new object[]{graph,origin,new Vector3(100,0,100),1,0f,true,Vector3.right});
        var points=(List<Vector3>)type.GetField("points").GetValue(route);
        Assert.That(points.Count,Is.GreaterThan(1));var first=points.First(p=>Vector3.Distance(p,origin)>1);
        Assert.That(first.x,Is.EqualTo(0).Within(.01f));
        Assert.That(first.z,Is.EqualTo(40).Within(.01f),"Joining at the nearest intersection cuts diagonally through the city block; join the actual nearest road segment first.");
        Assert.That(points.Last(),Is.EqualTo(new Vector3(100,0,100)));
    }
}

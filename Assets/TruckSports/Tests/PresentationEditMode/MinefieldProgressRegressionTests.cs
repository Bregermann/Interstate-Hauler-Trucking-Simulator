using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public sealed class MinefieldProgressRegressionTests
{
    [Test]public void DrivingDistanceAppearsBeforeTheFirstExplosion()
    {
        var root=new GameObject("Inactive Minefield standings fixture");root.SetActive(false);
        var physicalBodies=new List<GameObject>();
        try
        {
            var matchType=Type.GetType("LWS.TruckMinefield.MinefieldMatch, Assembly-CSharp",true);var racerType=Type.GetType("LWS.TruckMinefield.MinefieldRacer, Assembly-CSharp",true);var truckType=Type.GetType("LWS.TruckSports.SportsEventTruck, Assembly-CSharp",true);
            var match=root.AddComponent(matchType);var racers=Array.CreateInstance(racerType,8);
            for(int i=0;i<8;i++)
            {
                var go=new GameObject("Driving competitor "+i);go.SetActive(false);go.transform.SetParent(root.transform);
                // Inactive rigidbodies have no initialized native center of
                // mass. Keep the actual body active, the session facade inert.
                var physical=new GameObject("Active authoritative body "+i);physicalBodies.Add(physical);var body=physical.AddComponent<Rigidbody>();body.position=new Vector3(i*5,1,100+i);body.centerOfMass=Vector3.zero;
                var truck=go.AddComponent(truckType);truckType.GetField("body").SetValue(truck,body);var racer=go.AddComponent(racerType);racerType.GetField("truck").SetValue(racer,truck);racers.SetValue(racer,i);
                Assert.That((int)racerType.GetProperty("Launches").GetValue(racer),Is.Zero);
            }
            matchType.GetField("racers").SetValue(match,racers);
            var text=(string)matchType.GetProperty("Standings").GetValue(match);
            Assert.That(text,Does.Contain("328"),"A truck100metres from the starting line must already show its328feet of progress before any mine explosion.");
        }
        finally{UnityEngine.Object.DestroyImmediate(root);foreach(var body in physicalBodies)UnityEngine.Object.DestroyImmediate(body);}
    }
}

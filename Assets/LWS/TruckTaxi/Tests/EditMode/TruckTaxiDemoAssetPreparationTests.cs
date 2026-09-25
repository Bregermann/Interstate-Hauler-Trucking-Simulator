using System;
using System.Collections;
using System.Linq;
using LWS.TruckTaxi.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.TestTools;

namespace LWS.TruckTaxi.Tests
{
    public sealed class TruckTaxiDemoAssetPreparationTests
    {
        [UnityTest, Timeout(900000)] public IEnumerator PrepareAndValidateExplicitNeutralDemoAudition()
        {
            if(Environment.GetEnvironmentVariable("TRUCK_TAXI_PREPARE_DEMO_ASSETS")!="1") Assert.Ignore("Opt-in persistent demo asset preparation.");
            TruckTaxiDemoPassengerPreparation.GenerateAudition();
            double deadline=EditorApplication.timeSinceStartup+600;
            while(TruckTaxiChatterboxQueue.IsRunning && EditorApplication.timeSinceStartup<deadline) yield return null;
            Assert.IsFalse(TruckTaxiChatterboxQueue.IsRunning,"Voice preparation timed out.");
            var p=TruckTaxiPassengerDialogueAuthoring.AllPassengers().Single(x=>x.passengerId==TruckTaxiDemoPassengerPreparation.AuditionPassenger);
            foreach(var line in p.authoredDialogue.lines) Assert.IsNotNull(line.generatedAudio,line.lineId+" / "+TruckTaxiChatterboxQueue.Log);
            Assert.AreEqual(0,TruckTaxiChatterboxQueue.BuildJob(new[]{p},true,false).items.Count);
            TruckTaxiPassengerFactoryBuilder.WriteReports();
        }
    }
}

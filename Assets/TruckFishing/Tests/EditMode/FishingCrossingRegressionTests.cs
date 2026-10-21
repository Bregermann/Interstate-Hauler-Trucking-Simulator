using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class FishingCrossingRegressionTests
{
    static Type Runtime(string name)=>Type.GetType("LWS.TruckFishing."+name+", Assembly-CSharp",true);
    [TestCase(0,2f)][TestCase(30,2.5f)][TestCase(60,3f)][TestCase(90,3.5f)][TestCase(120,4f)]
    public void RealMovementBaselineFollowsRequestedProgression(double seconds,float expected)
    {
        float multiplier=(float)Runtime("FishingDirector").GetMethod("BaselineMultiplier").Invoke(null,new object[]{seconds,2f,.5f,6f});
        Assert.That(multiplier,Is.EqualTo(expected).Within(.0001f));
    }
    [TestCase(0f)][TestCase(1.5f)][TestCase(3.8f)]
    public void IndependentSpeedVariationNeverReversesOrMovesTheNetCrossing(float phase)
    {
        var plan=Activator.CreateInstance(Runtime("FishPlan"));var type=plan.GetType();
        type.GetField("travel").SetValue(plan,2f);type.GetField("arrival").SetValue(plan,10d);type.GetField("entryLead").SetValue(plan,8f);type.GetField("tempo").SetValue(plan,.11f);type.GetField("tempoPhase").SetValue(plan,phase);
        var progress=Runtime("FishingStream").GetMethod("Progress");float last=-1;
        for(int i=0;i<=100;i++){float u=(float)progress.Invoke(null,new object[]{plan,8d+i*.02});Assert.That(u,Is.GreaterThan(last));last=u;}
        Assert.That(last,Is.EqualTo(1).Within(.0001f),"Spikes and slowdowns do not desynchronize actual net arrival.");
    }
    [TestCase(.25f,.35f)]
    [TestCase(8f,8f)]
    public void RenderedFishAndCatchEventShareTheSameArrival(float travel,float lead)
    {
        var root=new GameObject("Isolated fishing crossing fixture");root.SetActive(false);
        var prefab=new GameObject("Fixture pooled model");prefab.SetActive(false);
        try
        {
            var streamType=Runtime("FishingStream");var stream=root.AddComponent(streamType);
            streamType.GetField("fishPrefabs").SetValue(stream,new[]{prefab,prefab,prefab});
            streamType.GetField("debrisPrefabs").SetValue(stream,new[]{prefab});
            streamType.GetMethod("Initialize").Invoke(stream,null);
            var match=root.AddComponent(Runtime("FishingMatch"));
            var plan=Activator.CreateInstance(Runtime("FishPlan"));
            foreach(var pair in new[]{("travel",travel),("entryLead",lead),("x",0f),("curve",0f)})plan.GetType().GetField(pair.Item1).SetValue(plan,pair.Item2);
            double arrival=travel+lead;plan.GetType().GetField("arrival").SetValue(plan,arrival);
            streamType.GetMethod("Spawn").Invoke(stream,new[]{plan});
            var models=(Transform[])streamType.GetField("models",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(stream);
            var model=models[0];float dt=.02f;
            // At the old erroneous crossing time the fish must still be upstream,
            // not already through a net whose score event waits until arrival.
            double sample=travel;
            streamType.GetMethod("Advance").Invoke(stream,new object[]{sample,dt,match});
            Assert.That(model.localPosition.z,Is.GreaterThan(.1f),"Entry lead must affect visible motion as well as scoring.");
            streamType.GetMethod("Advance").Invoke(stream,new object[]{arrival,dt,match});
            Assert.That(model.localPosition.z,Is.EqualTo(0).Within(.001f),"The scored fish must be at the visible net plane.");
            Assert.That(model.gameObject.activeSelf,Is.False,"Resolved fish returns to its pool once.");
            Assert.That((int)streamType.GetProperty("Resolutions").GetValue(stream),Is.EqualTo(1));
            streamType.GetMethod("Advance").Invoke(stream,new object[]{arrival+dt,dt,match});
            Assert.That((int)streamType.GetProperty("Resolutions").GetValue(stream),Is.EqualTo(1));
        }
        finally{UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(prefab);}
    }
}

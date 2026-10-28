#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

// Pure synthetic ledger controls only. No actual-earned or disk acceptance.
public sealed class ArchaeologyEarnedCheckpointPolicyTests
{
    static Type Runtime(string name)=>Type.GetType("LWS.TruckArchaeology."+name+", Assembly-CSharp",true);
    static readonly string[] Ids=Enumerable.Range(0,12).Select(i=>"actual.stable.artifact."+i).ToArray();
    static readonly int[] Values=Enumerable.Range(0,12).Select(i=>new[]{180,140,220}[i%3]).ToArray();
    static object State()
    {
        string[] items=Enumerable.Range(0,12).Select(i=>"{\"id\":\""+Ids[i]+"\",\"stage\":"+(i==0||i==10?5:0)+",\"owner\":"+(i==0?0:i==10?3:-1)+",\"preservation\":"+(i==0?"0.5":i==10?"0.875":"1")+",\"body\":{\"id\":\""+Ids[i]+"\",\"kinematic\":true}}").ToArray();
        return JsonUtility.FromJson("{\"players\":4,\"cooperative\":false,\"scores\":[90,0,0,123],\"artifacts\":["+string.Join(",",items)+"]}",Runtime("ArchaeologyMatch+WorkshopState"));
    }
    static object[] Audit(object data,string[] ids=null)
    {
        object[] args={data,ids??Ids,Values,null,null,null};bool accepted=(bool)Runtime("ArchaeologyNonzeroPersistenceWindowsProbe").GetMethod("TryReadEarnedValues",BindingFlags.Public|BindingFlags.Static).Invoke(null,args);
        return new[]{(object)accepted,args[3],args[4],args[5]};
    }
    static Array Artifacts(object data)=>(Array)data.GetType().GetField("artifacts").GetValue(data);
    [Test]public void IndependentlyRecomputedPerOwnerActualValuesUseAwayFromZeroAndNeverMutateSnapshot()
    {
        var data=State();string before=JsonUtility.ToJson(data);var audit=Audit(data);Assert.That(audit[0],Is.True);CollectionAssert.AreEqual(new[]{90,0,0,123},(int[])audit[1]);CollectionAssert.AreEqual(new[]{0,10},(int[])audit[2]);Assert.That(JsonUtility.ToJson(data),Is.EqualTo(before),"Observer must not create score, ownership, delivery or model state.");
    }
    [Test]public void SamePositiveGrandTotalOnWrongOwnersCannotPassNonzeroCheckpoint()
    {
        var data=State();data.GetType().GetField("scores").SetValue(data,new[]{123,0,0,90});var result=Audit(data);Assert.That(result[0],Is.False);Assert.That((string)result[3],Does.Contain("Per-owner"));
    }
    [Test]public void UnknownIdentityOwnerAndUnsupportedDeliveredBodyRefuseWithoutMutation()
    {
        foreach(string mutation in new[]{"identity","owner","support","nonfinite"})
        {
            var data=State();var artifact=Artifacts(data).GetValue(10);if(mutation=="identity")artifact.GetType().GetField("id").SetValue(artifact,"unknown");
            else if(mutation=="owner")artifact.GetType().GetField("owner").SetValue(artifact,0);
            else if(mutation=="nonfinite")artifact.GetType().GetField("preservation").SetValue(artifact,double.NaN);
            else{var body=artifact.GetType().GetField("body").GetValue(artifact);body.GetType().GetField("kinematic").SetValue(body,false);}
            string before=mutation=="nonfinite"?null:JsonUtility.ToJson(data);Assert.That(Audit(data)[0],Is.False,mutation);if(before!=null)Assert.That(JsonUtility.ToJson(data),Is.EqualTo(before));else Assert.That(double.IsNaN((double)artifact.GetType().GetField("preservation").GetValue(artifact)),Is.True);
        }
    }
    [Test]public void ZeroDeliveryOrCargoOnlyCannotStandInForActuallyEarnedPositiveValue()
    {
        foreach(int stage in new[]{0,4})
        {
            var data=State();foreach(var artifact in Artifacts(data))artifact.GetType().GetField("stage").SetValue(artifact,stage);data.GetType().GetField("scores").SetValue(data,new int[4]);Assert.That(Audit(data)[0],Is.False);
        }
        var valid=State();var duplicate=(string[])Ids.Clone();duplicate[1]=duplicate[0];Assert.That(Audit(valid,duplicate)[0],Is.False);
    }
}
#endif

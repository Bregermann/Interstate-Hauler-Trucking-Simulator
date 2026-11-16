#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine.TestTools;
public sealed class DragonKingdomTowerMaskResponseTests
{
    static Type Owner=>Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomRegionalWorldIntegration, Assembly-CSharp-Editor",true);
    static object Call(string method,params object[] args)=>Owner.GetMethod(method).Invoke(null,args);
    static object Field(object value,string name)=>value.GetType().GetField(name).GetValue(value);
    static string Source()=>File.ReadAllText((string)Owner.GetField("TowerDetailVendorGraph").GetValue(null));
    [Test]
    public void ActualVendorGraphRepairChangesExactlyTwoDisconnectedScalarValuesAndNoReferenceOrOtherByte()
    {
        string source=Source(),changed=(string)Call("CorrectDisconnectedTowerMaskInputs",source);Assert.That(changed,Is.Not.EqualTo(source));int differences=0;Assert.That(changed.Length,Is.EqualTo(source.Length));for(int i=0;i<source.Length;i++)if(source[i]!=changed[i]){Assert.That(source[i],Is.EqualTo('0'));Assert.That(changed[i],Is.EqualTo('1'));differences++;}Assert.That(differences,Is.EqualTo(2));Assert.That(Source(),Is.EqualTo(source));
    }
    [TestCase("already-corrected")][TestCase("missing-slot")][TestCase("customized-value")][TestCase("connected-input")]
    public void RepairRejectsChangedOrConnectedVendorInputsInsteadOfOverwritingDesignerAuthorship(string fault)
    {
        string source=Source();switch(fault){case "already-corrected":source=(string)Call("CorrectDisconnectedTowerMaskInputs",source);break;case "missing-slot":source=source.Replace("c3f6552eb2eb418580fe0dbfbe058d09","11111111111111111111111111111111");break;case "customized-value":int at=source.IndexOf("\"m_ObjectId\": \"c3f6552eb2eb418580fe0dbfbe058d09\"",StringComparison.Ordinal),value=source.IndexOf("\"m_Value\": 0.0,",at,StringComparison.Ordinal);source=source.Substring(0,value)+source.Substring(value).Replace("\"m_Value\": 0.0,","\"m_Value\": 0.5,");break;case "connected-input":int edges=source.IndexOf("\"m_Edges\": [",StringComparison.Ordinal)+"\"m_Edges\": [".Length;source=source.Insert(edges,"{\"m_OutputSlot\":{\"m_Node\":{\"m_Id\":\"test-source\"},\"m_SlotId\":0},\"m_InputSlot\":{\"m_Node\":{\"m_Id\":\"240416e0d5bb4e21934ffc6e19c5457e\"},\"m_SlotId\":1043644200}},");break;}
        var error=Assert.Throws<TargetInvocationException>(()=>Call("CorrectDisconnectedTowerMaskInputs",source));Assert.That(error.InnerException,Is.TypeOf<InvalidOperationException>());
    }
}
#endif

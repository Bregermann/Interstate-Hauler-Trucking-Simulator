#if UNITY_EDITOR
using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class DragonKingdomNativeTypeSettingsTests
{
    [Test]
    public void EveryLiveDestroyedAndManagedNullUnitySettingRetainsAnExplicitIdentityWithoutReadingDestroyedNames()
    {
        var type=Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomNativeRoadTypeProbe, Assembly-CSharp-Editor",true);var method=type.GetMethod("PublicSettingValue",BindingFlags.Static|BindingFlags.NonPublic);
        string Value(object item)=>(string)method.Invoke(null,new object[]{item,0});Assert.That(Value(null),Is.EqualTo("NULL"));
        var mesh=new Mesh{name="Temporary SDK field identity regression"};int id=mesh.GetInstanceID();object managed=mesh;
        try
        {
            string live=Value(managed);Assert.That(live,Does.Contain("UnityEngine.Mesh"));Assert.That(live,Does.Contain("INSTANCE="+id));Assert.That(live,Does.Not.Contain("NAME="));
            UnityEngine.Object.DestroyImmediate(mesh);Assert.That(ReferenceEquals(managed,null),Is.False);Assert.That((bool)(UnityEngine.Object)managed,Is.False);
            string destroyed=Value(managed);Assert.That(destroyed,Does.Contain("UNITY_NULL_OR_DESTROYED"));Assert.That(destroyed,Does.Contain("INSTANCE="+id));Assert.That(destroyed,Does.Not.Contain("NAME="));
        }
        finally{if(mesh)UnityEngine.Object.DestroyImmediate(mesh);}
    }
}
#endif

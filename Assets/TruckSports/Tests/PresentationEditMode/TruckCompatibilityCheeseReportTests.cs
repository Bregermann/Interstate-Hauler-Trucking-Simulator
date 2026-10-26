#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
public sealed class TruckCompatibilityCheeseReportTests
{
    const BindingFlags Any=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
    static Type Sports(string name)=>Type.GetType("LWS.TruckSports."+name+", Assembly-CSharp",true);
    static object Get(object owner,string name)=>owner.GetType().GetField(name,Any) is FieldInfo f?f.GetValue(owner):owner.GetType().GetProperty(name,Any).GetValue(owner);
    static void Set(object owner,string name,object value)=>owner.GetType().GetField(name,Any).SetValue(owner,value);
    [Test]public void OnlyFourRegisteredCheeseOwnersCanFillAnOwnedAllNullMap()
    {
        var previous=EditorSceneManager.GetSceneManagerSetup();
        try
        {
            var scene=EditorSceneManager.OpenScene("Assets/TruckCheeseRolling/Scenes/TruckCheeseRolling.unity",OpenSceneMode.Single);var source=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren(Sports("SportsTruckCompatibilitySource"),true)).Cast<Component>().Single();var match=Get(source,"cheese");var author=Type.GetType("LWS.TruckSports.Editor.SportsTruckCompatibilityCheeseAuthoring, Assembly-CSharp-Editor",true);var prove=author.GetMethod("ProvenMap");var can=author.GetMethod("CanUpgrade");var target=(Array)prove.Invoke(null,new[]{match});var original=(Array)Get(source,"participantHosts");var hosts=((Array)Get(match,"appearanceHosts")).Cast<object>().ToArray();var trucks=((Array)Get(match,"trucks")).Cast<Component>().ToArray();var revisions=hosts.Select(h=>Get(h,"AppearanceRevision")).ToArray();var models=hosts.Select(h=>Get(h,"AppearanceRoot")).ToArray();bool dirty=scene.isDirty;
            Assert.That(target.Length,Is.EqualTo(8));Assert.That(hosts.Length,Is.EqualTo(4));for(int i=0;i<4;i++){Assert.That(target.GetValue(i),Is.SameAs(hosts[i]));Assert.That(((Component)hosts[i]).transform,Is.SameAs(trucks[i].transform));}for(int i=4;i<8;i++)Assert.That(target.GetValue(i),Is.Null);foreach(var truck in trucks.Skip(4))Assert.That(truck.GetComponent(Sports("SportsTruckAppearanceHost")),Is.Null);
            try
            {
                Set(source,"participantHosts",target);Assert.That(can.Invoke(null,new object[]{source,target}),Is.EqualTo(false));var empty=Array.CreateInstance(target.GetType().GetElementType(),8);Set(source,"participantHosts",empty);Assert.That(can.Invoke(null,new object[]{source,target}),Is.EqualTo(true));empty.SetValue(hosts[0],0);Assert.That(Assert.Throws<TargetInvocationException>(()=>can.Invoke(null,new object[]{source,target})).InnerException,Is.TypeOf<InvalidOperationException>());empty.SetValue(null,0);var background=(Array)target.Clone();background.SetValue(hosts[0],4);Assert.That(Assert.Throws<TargetInvocationException>(()=>can.Invoke(null,new object[]{source,background})).InnerException,Is.TypeOf<InvalidOperationException>());
                int slot=(int)Get(hosts[0],"participantSlot");try{Set(hosts[0],"participantSlot",4);Assert.That(Assert.Throws<TargetInvocationException>(()=>prove.Invoke(null,new[]{match})).InnerException,Is.TypeOf<InvalidOperationException>());}finally{Set(hosts[0],"participantSlot",slot);}var bindings=(Array)Get(match,"appearanceHosts");try{bindings.SetValue(hosts[1],0);bindings.SetValue(hosts[0],1);Assert.That(Assert.Throws<TargetInvocationException>(()=>prove.Invoke(null,new[]{match})).InnerException,Is.TypeOf<InvalidOperationException>());}finally{bindings.SetValue(hosts[0],0);bindings.SetValue(hosts[1],1);}
                Assert.That(can.Invoke(null,new object[]{source,target}),Is.EqualTo(true));CollectionAssert.AreEqual(revisions,hosts.Select(h=>Get(h,"AppearanceRevision")));CollectionAssert.AreEqual(models,hosts.Select(h=>Get(h,"AppearanceRoot")));Assert.That(scene.isDirty,Is.EqualTo(dirty));
            }
            finally{Set(source,"participantHosts",original);}
        }
        finally{Type.GetType("LWS.TruckSports.Editor.SportsTruckCompatibilityAuthoring, Assembly-CSharp-Editor",true).GetMethod("RestorePreviousSceneSetup").Invoke(null,new object[]{previous});}
    }
}
#endif

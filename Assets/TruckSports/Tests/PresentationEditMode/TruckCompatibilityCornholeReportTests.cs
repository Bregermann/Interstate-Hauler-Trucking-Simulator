#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
public sealed class TruckCompatibilityCornholeReportTests
{
    const BindingFlags Any=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
    static Type Sports(string name)=>Type.GetType("LWS.TruckSports."+name+", Assembly-CSharp",true);
    static object Get(object owner,string name)=>owner.GetType().GetField(name,Any) is FieldInfo f?f.GetValue(owner):owner.GetType().GetProperty(name,Any).GetValue(owner);
    static void Set(object owner,string name,object value)=>owner.GetType().GetField(name,Any).SetValue(owner,value);
    [Test]public void FourCoursesMayUseOnlyTheirOwnProvenEquivalentDeliveryTemplateAndRefusePartialOrUnequalMaps()
    {
        var previous=EditorSceneManager.GetSceneManagerSetup();
        try
        {
            var scene=EditorSceneManager.OpenScene("Assets/TruckCornhole/Scenes/TruckCornhole.unity",OpenSceneMode.Single);var source=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren(Sports("SportsTruckCompatibilitySource"),true)).Cast<Component>().Single();var match=Get(source,"cornhole");var courses=((Array)Get(match,"courses")).Cast<object>().ToArray();var author=Type.GetType("LWS.TruckSports.Editor.SportsTruckCompatibilityCornholeAuthoring, Assembly-CSharp-Editor",true);var prove=author.GetMethod("ProvenMap");var can=author.GetMethod("CanUpgrade");var target=(Array)prove.Invoke(null,new[]{match});var original=(Array)Get(source,"participantHosts");var originalShared=Get(source,"sharedTurnHost");var hosts=courses.SelectMany(c=>((Array)Get(c,"trucks")).Cast<object>().Select(t=>Get(t,"appearanceHost"))).ToArray();var revisions=hosts.Select(h=>Get(h,"AppearanceRevision")).ToArray();var models=hosts.Select(h=>Get(h,"AppearanceRoot")).ToArray();bool dirty=scene.isDirty;Assert.That(hosts.Length,Is.EqualTo(16));Assert.That(target.Length,Is.EqualTo(8));for(int i=0;i<4;i++)Assert.That(target.GetValue(i),Is.SameAs(hosts[i*4]));for(int i=4;i<8;i++)Assert.That(target.GetValue(i),Is.Null);
            try
            {
                var empty=Array.CreateInstance(target.GetType().GetElementType(),8);Set(source,"participantHosts",empty);Set(source,"sharedTurnHost",null);Assert.That(can.Invoke(null,new object[]{source,target}),Is.EqualTo(true));empty.SetValue(hosts[0],0);Assert.That(Assert.Throws<TargetInvocationException>(()=>can.Invoke(null,new object[]{source,target})).InnerException,Is.TypeOf<InvalidOperationException>());empty.SetValue(null,0);Set(source,"participantHosts",target);Assert.That(can.Invoke(null,new object[]{source,target}),Is.EqualTo(false));Set(source,"participantHosts",empty);Set(source,"sharedTurnHost",hosts[0]);Assert.That(Assert.Throws<TargetInvocationException>(()=>can.Invoke(null,new object[]{source,target})).InnerException,Is.TypeOf<InvalidOperationException>());Set(source,"sharedTurnHost",null);
                var fit=((Array)Get(hosts[3],"compatibleBodies")).GetValue(0);var at=(Vector3)Get(fit,"position");try{Set(fit,"position",at+Vector3.right*.01f);Assert.That(Assert.Throws<TargetInvocationException>(()=>prove.Invoke(null,new[]{match})).InnerException,Is.TypeOf<InvalidOperationException>(),"A different fourth delivery must not borrow delivery0's fit.");}finally{Set(fit,"position",at);}int slot=(int)Get(hosts[7],"participantSlot");try{Set(hosts[7],"participantSlot",0);Assert.That(Assert.Throws<TargetInvocationException>(()=>prove.Invoke(null,new[]{match})).InnerException,Is.TypeOf<InvalidOperationException>());}finally{Set(hosts[7],"participantSlot",slot);}var trucks=(Array)Get(courses[0],"trucks");var first=trucks.GetValue(0);try{trucks.SetValue(((Array)Get(courses[1],"trucks")).GetValue(0),0);Assert.That(Assert.Throws<TargetInvocationException>(()=>prove.Invoke(null,new[]{match})).InnerException,Is.TypeOf<InvalidOperationException>());}finally{trucks.SetValue(first,0);}CollectionAssert.AreEqual(target,(Array)prove.Invoke(null,new[]{match}));CollectionAssert.AreEqual(revisions,hosts.Select(h=>Get(h,"AppearanceRevision")));CollectionAssert.AreEqual(models,hosts.Select(h=>Get(h,"AppearanceRoot")));Assert.That(scene.isDirty,Is.EqualTo(dirty));
            }
            finally{Set(source,"participantHosts",original);Set(source,"sharedTurnHost",originalShared);}
        }
        finally{Type.GetType("LWS.TruckSports.Editor.SportsTruckCompatibilityAuthoring, Assembly-CSharp-Editor",true).GetMethod("RestorePreviousSceneSetup").Invoke(null,new object[]{previous});}
    }
}
#endif

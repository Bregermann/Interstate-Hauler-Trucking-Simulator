#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
public sealed class TruckCompatibilitySkeeReportTests
{
    const BindingFlags Any=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
    static Type Sports(string name)=>Type.GetType("LWS.TruckSports."+name+", Assembly-CSharp",true);
    static object Get(object owner,string name)=>owner.GetType().GetField(name,Any) is FieldInfo f?f.GetValue(owner):owner.GetType().GetProperty(name,Any).GetValue(owner);
    static void Set(object owner,string name,object value)=>owner.GetType().GetField(name,Any).SetValue(owner,value);
    [Test]public void FourEquivalentOriginalLanesMaySupplyOnlyAnOwnedReadOnlySkeeTemplate()
    {
        var previous=EditorSceneManager.GetSceneManagerSetup();
        try
        {
            var scene=EditorSceneManager.OpenScene("Assets/TruckSkeeBall/Scenes/TruckSkeeBall.unity",OpenSceneMode.Single);var source=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren(Sports("SportsTruckCompatibilitySource"),true)).Cast<Component>().Single();var match=Get(source,"session");var author=Type.GetType("LWS.TruckSports.Editor.SportsTruckCompatibilitySkeeAuthoring, Assembly-CSharp-Editor",true);var prove=author.GetMethod("ProvenTemplate");var can=author.GetMethod("CanUpgrade");var template=prove.Invoke(null,new[]{match});var courses=((Array)Get(match,"courses")).Cast<object>().ToArray();var hosts=courses.Select(c=>Get(Get(c,"truck"),"appearanceHost")).ToArray();var original=Get(source,"sharedTurnHost");var map=(Array)Get(source,"participantHosts");var revisions=hosts.Select(h=>Get(h,"AppearanceRevision")).ToArray();var models=hosts.Select(h=>Get(h,"AppearanceRoot")).ToArray();var profiles=hosts.Select(h=>Get(h,"ProfileId")).ToArray();bool dirty=scene.isDirty;Assert.That(template,Is.SameAs(hosts[0]));Assert.That(((Array)Get(match,"appearanceHosts")).Length,Is.Zero);
            try
            {
                Set(source,"participantHosts",Array.CreateInstance(map.GetType().GetElementType(),map.Length));Set(source,"sharedTurnHost",null);Assert.That(can.Invoke(null,new[]{source,template}),Is.EqualTo(true));Set(source,"sharedTurnHost",template);Assert.That(can.Invoke(null,new[]{source,template}),Is.EqualTo(false));Set(source,"sharedTurnHost",hosts[1]);Assert.That(Assert.Throws<TargetInvocationException>(()=>can.Invoke(null,new[]{source,template})).InnerException,Is.TypeOf<InvalidOperationException>());Set(source,"sharedTurnHost",null);
                var partial=(Array)Get(source,"participantHosts");partial.SetValue(hosts[0],0);Assert.That(Assert.Throws<TargetInvocationException>(()=>can.Invoke(null,new[]{source,template})).InnerException,Is.TypeOf<InvalidOperationException>());partial.SetValue(null,0);
                var fit=((Array)Get(hosts[3],"compatibleBodies")).GetValue(0);var position=(Vector3)Get(fit,"position");try{Set(fit,"position",position+Vector3.right*.01f);Assert.That(Assert.Throws<TargetInvocationException>(()=>prove.Invoke(null,new[]{match})).InnerException,Is.TypeOf<InvalidOperationException>(),"A real1cm fit difference must not borrow lane0.");}finally{Set(fit,"position",position);}int slot=(int)Get(hosts[3],"participantSlot");try{Set(hosts[3],"participantSlot",3);Assert.That(Assert.Throws<TargetInvocationException>(()=>prove.Invoke(null,new[]{match})).InnerException,Is.TypeOf<InvalidOperationException>());}finally{Set(hosts[3],"participantSlot",slot);}
                var inherited=(Array)Get(match,"appearanceHosts");try{Set(match,"appearanceHosts",Array.CreateInstance(inherited.GetType().GetElementType(),4));Assert.That(Assert.Throws<TargetInvocationException>(()=>prove.Invoke(null,new[]{match})).InnerException,Is.TypeOf<InvalidOperationException>());}finally{Set(match,"appearanceHosts",inherited);}Assert.That(prove.Invoke(null,new[]{match}),Is.SameAs(template));CollectionAssert.AreEqual(revisions,hosts.Select(h=>Get(h,"AppearanceRevision")));CollectionAssert.AreEqual(models,hosts.Select(h=>Get(h,"AppearanceRoot")));CollectionAssert.AreEqual(profiles,hosts.Select(h=>Get(h,"ProfileId")));Assert.That(scene.isDirty,Is.EqualTo(dirty));
            }
            finally{Set(source,"sharedTurnHost",original);Set(source,"participantHosts",map);}
        }
        finally{Type.GetType("LWS.TruckSports.Editor.SportsTruckCompatibilityAuthoring, Assembly-CSharp-Editor",true).GetMethod("RestorePreviousSceneSetup").Invoke(null,new object[]{previous});}
    }
}
#endif

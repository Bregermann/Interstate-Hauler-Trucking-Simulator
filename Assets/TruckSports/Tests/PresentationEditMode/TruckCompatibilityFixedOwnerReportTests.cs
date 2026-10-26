#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

public sealed class TruckCompatibilityFixedOwnerReportTests
{
    const BindingFlags Any=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
    static Type Sports(string name)=>Type.GetType("LWS.TruckSports."+name+", Assembly-CSharp",true);
    static Type Author=>Type.GetType("LWS.TruckSports.Editor.SportsTruckCompatibilityFixedOwnerAuthoring, Assembly-CSharp-Editor",true);
    static object Get(object owner,string name)=>owner.GetType().GetField(name,Any) is FieldInfo field?field.GetValue(owner):owner.GetType().GetProperty(name,Any).GetValue(owner);
    static void Set(object owner,string name,object value)=>owner.GetType().GetField(name,Any).SetValue(owner,value);
    static object Invoke(string method,params object[] args)=>Author.GetMethod(method).Invoke(null,args);
    static string PathFor(string mode)=>"Assets/"+(mode=="TruckKeepawayFirstPerson"?"TruckKeepaway":mode)+"/Scenes/"+mode+".unity";
    [TestCase("TruckKeepaway",4),TestCase("TruckKeepawayFirstPerson",4),TestCase("TruckMattressRacing",8),TestCase("TruckMinefield",8),TestCase("TruckTrampolineDodgeball",8),TestCase("TruckLimbo",8),TestCase("TruckKickball",12)]
    public void OnlyOwnedAllNullMapsUpgradeAndEveryFixedPhysicalOwnerIsProven(string mode,int count)
    {
        var previous=EditorSceneManager.GetSceneManagerSetup();
        try
        {
            var scene=EditorSceneManager.OpenScene(PathFor(mode),OpenSceneMode.Single);var source=scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren(Sports("SportsTruckCompatibilitySource"),true)).Cast<Component>().Single();var match=Get(source,"session");var original=(Array)Get(source,"participantHosts");var target=(Array)Invoke("ProvenMap",match);var hosts=target.Cast<object>().Where(host=>host!=null).ToArray();Assert.That(hosts.Length,Is.EqualTo(count));var revisions=hosts.Select(h=>Get(h,"AppearanceRevision")).ToArray();var models=hosts.Select(h=>Get(h,"AppearanceRoot")).ToArray();var profiles=hosts.Select(h=>Get(h,"ProfileId")).ToArray();var scales=hosts.Select(h=>((Component)h).transform.localScale).ToArray();var bodies=scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<Rigidbody>(true)).Select(body=>(body,body.position,body.rotation,body.linearVelocity,body.angularVelocity,body.isKinematic,body.detectCollisions)).ToArray();var layout=scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<RectTransform>(true)).Select(rect=>(rect,rect.anchoredPosition,rect.sizeDelta,rect.anchorMin,rect.anchorMax)).ToArray();int transforms=scene.GetRootGameObjects().Sum(root=>root.GetComponentsInChildren<Transform>(true).Length);bool dirty=scene.isDirty;
            void Refuses(string method,object[] args)=>Assert.That(Assert.Throws<TargetInvocationException>(()=>Invoke(method,args)).InnerException,Is.TypeOf<InvalidOperationException>());
            try
            {
                Set(source,"participantHosts",target);Assert.That(Invoke("CanUpgrade",source,target),Is.EqualTo(false));var empty=Array.CreateInstance(target.GetType().GetElementType(),target.Length);Set(source,"participantHosts",empty);Assert.That(Invoke("CanUpgrade",source,target),Is.EqualTo(true));empty.SetValue(hosts[0],0);Refuses("CanUpgrade",new object[]{source,target});empty.SetValue(null,0);
                var badTarget=(Array)target.Clone();badTarget.SetValue(hosts[1],0);Refuses("CanUpgrade",new object[]{source,badTarget});Set(source,"participantHosts",Array.CreateInstance(target.GetType().GetElementType(),1));Refuses("CanUpgrade",new object[]{source,target});Set(source,"participantHosts",empty);
                int slot=(int)Get(hosts[0],"participantSlot");try{Set(hosts[0],"participantSlot",slot+1);Refuses("ProvenMap",new[]{match});}finally{Set(hosts[0],"participantSlot",slot);}
                var authored=(Array)Get(match,mode=="TruckLimbo"?"limboAppearanceHosts":"appearanceHosts");var first=authored.GetValue(0);var second=authored.GetValue(1);try{authored.SetValue(second,0);authored.SetValue(first,1);Refuses("ProvenMap",new[]{match});}finally{authored.SetValue(first,0);authored.SetValue(second,1);}
                var fit=((Array)Get(hosts[0],"compatibleBodies")).GetValue(0);var scale=(Vector3)Get(fit,"scale");try{Set(fit,"scale",Vector3.zero);Refuses("ProvenMap",new[]{match});}finally{Set(fit,"scale",scale);}
                var menu=Get(source,"menu");try{Set(source,"menu",null);Refuses("CanUpgrade",new object[]{source,target});}finally{Set(source,"menu",menu);}var page=Get(source,"setupPage");try{Set(source,"setupPage",source.gameObject);Refuses("CanUpgrade",new object[]{source,target});}finally{Set(source,"setupPage",page);}var catalog=Get(source,"catalog");try{Set(source,"catalog",null);Refuses("CanUpgrade",new object[]{source,target});}finally{Set(source,"catalog",catalog);}
                if(mode=="TruckLimbo"){var inherited=(Array)Get(match,"appearanceHosts");Assert.That(inherited.Length,Is.Zero);try{Set(match,"appearanceHosts",target);Refuses("ProvenMap",new[]{match});}finally{Set(match,"appearanceHosts",inherited);}}
                Assert.That(Invoke("CanUpgrade",source,target),Is.EqualTo(true));CollectionAssert.AreEqual(target,(Array)Invoke("ProvenMap",match));
                CollectionAssert.AreEqual(revisions,hosts.Select(h=>Get(h,"AppearanceRevision")));CollectionAssert.AreEqual(models,hosts.Select(h=>Get(h,"AppearanceRoot")));CollectionAssert.AreEqual(profiles,hosts.Select(h=>Get(h,"ProfileId")));CollectionAssert.AreEqual(scales,hosts.Select(h=>((Component)h).transform.localScale));Assert.That(scene.GetRootGameObjects().Sum(root=>root.GetComponentsInChildren<Transform>(true).Length),Is.EqualTo(transforms));Assert.That(scene.isDirty,Is.EqualTo(dirty));
                foreach(var entry in bodies){Assert.That(entry.body.position,Is.EqualTo(entry.position));Assert.That(entry.body.rotation,Is.EqualTo(entry.rotation));Assert.That(entry.body.linearVelocity,Is.EqualTo(entry.linearVelocity));Assert.That(entry.body.angularVelocity,Is.EqualTo(entry.angularVelocity));Assert.That(entry.body.isKinematic,Is.EqualTo(entry.isKinematic));Assert.That(entry.body.detectCollisions,Is.EqualTo(entry.detectCollisions));}foreach(var entry in layout){Assert.That(entry.rect.anchoredPosition,Is.EqualTo(entry.anchoredPosition));Assert.That(entry.rect.sizeDelta,Is.EqualTo(entry.sizeDelta));Assert.That(entry.rect.anchorMin,Is.EqualTo(entry.anchorMin));Assert.That(entry.rect.anchorMax,Is.EqualTo(entry.anchorMax));}
            }
            finally{Set(source,"participantHosts",original);}
        }
        finally{Type.GetType("LWS.TruckSports.Editor.SportsTruckCompatibilityAuthoring, Assembly-CSharp-Editor",true).GetMethod("RestorePreviousSceneSetup").Invoke(null,new object[]{previous});}
    }
}
#endif

#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
public sealed class TruckCompatibilityLocalOwnerReportTests
{
    const BindingFlags Any=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
    static Type Sports(string name)=>Type.GetType("LWS.TruckSports."+name+", Assembly-CSharp",true);
    static object Get(object owner,string name)=>owner.GetType().GetField(name,Any) is FieldInfo f?f.GetValue(owner):owner.GetType().GetProperty(name,Any).GetValue(owner);
    static void Set(object owner,string name,object value)=>owner.GetType().GetField(name,Any).SetValue(owner,value);
    [TestCase("TruckTerrierRacing"),TestCase("TruckDockDiving")]
    public void LocalOwnersAndRealRecipesPermitOnlyTheOwnedDefaultSetupBinding(string mode)
    {
        var previous=EditorSceneManager.GetSceneManagerSetup();
        try
        {
            var scene=EditorSceneManager.OpenScene("Assets/"+mode+"/Scenes/"+mode+".unity",OpenSceneMode.Single);var source=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren(Sports("SportsTruckCompatibilitySource"),true)).Cast<Component>().Single();var match=Get(source,"session");bool terrier=mode=="TruckTerrierRacing";var author=Type.GetType("LWS.TruckSports.Editor.SportsTruckCompatibilityLocalOwnerAuthoring, Assembly-CSharp-Editor",true);var prove=author.GetMethod(terrier?"ProvenTerrierMap":"ProvenDockTemplate");var can=author.GetMethod("CanUpgrade");var value=prove.Invoke(null,new[]{match});var hosts=terrier?((Array)value).Cast<object>().ToArray():new[]{Get(Get(match,"truck"),"appearanceHost")}.Concat(((Array)Get(match,"staged")).Cast<object>().Select(t=>Get(t,"appearanceHost"))).ToArray();var originalMap=(Array)Get(source,"participantHosts");var originalTemplate=Get(source,"sharedTurnHost");var revisions=hosts.Select(h=>Get(h,"AppearanceRevision")).ToArray();var models=hosts.Select(h=>Get(h,"AppearanceRoot")).ToArray();bool dirty=scene.isDirty;Assert.That(hosts.Length,Is.EqualTo(terrier?12:9));
            try
            {
                var empty=Array.CreateInstance(originalMap.GetType().GetElementType(),originalMap.Length);Set(source,"participantHosts",empty);Set(source,"sharedTurnHost",null);Assert.That(can.Invoke(null,new object[]{source}),Is.EqualTo(true));if(terrier)Set(source,"participantHosts",value);else Set(source,"sharedTurnHost",value);Assert.That(can.Invoke(null,new object[]{source}),Is.EqualTo(false));Set(source,"participantHosts",empty);Set(source,"sharedTurnHost",null);empty.SetValue(hosts[0],0);Assert.That(Assert.Throws<TargetInvocationException>(()=>can.Invoke(null,new object[]{source})).InnerException,Is.TypeOf<InvalidOperationException>());empty.SetValue(null,0);
                if(terrier){var host=hosts[11];int slot=(int)Get(host,"participantSlot");try{Set(host,"participantSlot",10);Assert.That(Assert.Throws<TargetInvocationException>(()=>prove.Invoke(null,new[]{match})).InnerException,Is.TypeOf<InvalidOperationException>());}finally{Set(host,"participantSlot",slot);}Assert.That(((Array)Get(match,"appearanceHosts")).Length,Is.Zero);}
                else{Set(source,"sharedTurnHost",hosts[1]);Assert.That(Assert.Throws<TargetInvocationException>(()=>can.Invoke(null,new object[]{source})).InnerException,Is.TypeOf<InvalidOperationException>());Set(source,"sharedTurnHost",null);var fit=((Array)Get(hosts[8],"compatibleBodies")).GetValue(0);var position=(Vector3)Get(fit,"position");try{Set(fit,"position",position+Vector3.right*.01f);Assert.That(Assert.Throws<TargetInvocationException>(()=>prove.Invoke(null,new[]{match})).InnerException,Is.TypeOf<InvalidOperationException>());}finally{Set(fit,"position",position);}}
                var presenter=Get(match,terrier?"appearanceBridge":"appearanceSplit");var recipes=(Array)Get(presenter,"recipes");var first=recipes.GetValue(0);try{recipes.SetValue(null,0);Assert.That(Assert.Throws<TargetInvocationException>(()=>prove.Invoke(null,new[]{match})).InnerException,Is.TypeOf<InvalidOperationException>(),"Do not promise selected physical split/wreck rendering without its actual recipe.");}finally{recipes.SetValue(first,0);}Assert.That(can.Invoke(null,new object[]{source}),Is.EqualTo(true));CollectionAssert.AreEqual(revisions,hosts.Select(h=>Get(h,"AppearanceRevision")));CollectionAssert.AreEqual(models,hosts.Select(h=>Get(h,"AppearanceRoot")));Assert.That(scene.isDirty,Is.EqualTo(dirty));
            }
            finally{Set(source,"participantHosts",originalMap);Set(source,"sharedTurnHost",originalTemplate);}
        }
        finally{Type.GetType("LWS.TruckSports.Editor.SportsTruckCompatibilityAuthoring, Assembly-CSharp-Editor",true).GetMethod("RestorePreviousSceneSetup").Invoke(null,new object[]{previous});}
    }
}
#endif

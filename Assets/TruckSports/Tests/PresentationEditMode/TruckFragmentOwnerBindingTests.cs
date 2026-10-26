#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class TruckFragmentOwnerBindingTests
{
    const BindingFlags Any=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static Type Sports(string name)=>Type.GetType("LWS.TruckSports."+name+", Assembly-CSharp",true);
    static object Get(object owner,string name)=>owner.GetType().GetField(name,Any) is FieldInfo field?field.GetValue(owner):owner.GetType().GetProperty(name,Any).GetValue(owner);
    [TestCase(false)][TestCase(true)]public void ActualOriginalSourceBodyAndExplicitEventTruckOwnersKeepDistinctValidatedWheelAuthorities(bool eventTruck)
    {
        string mode=eventTruck?"TruckTugOfWar":"TruckRedLightGreenLight",path="Assets/"+mode+"/Scenes/"+mode+".unity";
        var previous=EditorSceneManager.GetSceneManagerSetup();
        // Never close unsaved interactive work. Native validation may begin
        // without a restorable active scene; use the existing explicit guard.
        if(!Application.isBatchMode)
        {
            for(int i=0;i<SceneManager.sceneCount;i++)
            {
                var prior=SceneManager.GetSceneAt(i);
                if(prior.isLoaded&&(prior.isDirty||string.IsNullOrEmpty(prior.path)))throw new InvalidOperationException("Save interactive scenes before this single-scene fixture: "+prior.name);
            }
        }
        var saved=previous.Where(entry=>!string.IsNullOrEmpty(entry.path)).ToArray();
        var author=Type.GetType("LWS.TruckSports.Editor.SportsTruckCompatibilityAuthoring, Assembly-CSharp-Editor",true);
        author.GetMethod("ShouldRestoreSceneSetup").Invoke(null,new object[]{saved,Application.isBatchMode});
        try
        {
            // Weather Maker owns one singleton; loading the actual venue
            // alongside a previous weather venue is an invalid test setup.
            var scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Single);
            var presenter=scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren(Sports("SportsTruckFragmentPresentation"),true)).First();var rig=Get(presenter,"rig");var pieces=((Array)Get(rig,"pieces")).Cast<object>().ToArray();var bodies=pieces.Select(piece=>(Rigidbody)Get(piece,"body")).ToArray();var shapes=pieces.Select(piece=>(Collider)Get(piece,"collider")).ToArray();var host=Get(presenter,"host");var catalog=Get(host,"catalog");var body=catalog.GetType().GetMethod("FindBody").Invoke(catalog,new object[]{"semi.standard"});var args=new object[]{body,null};Assert.That(presenter.GetType().GetMethod("Supports").Invoke(presenter,args),Is.EqualTo(true),args[1] as string);
            var sources=(Transform[])Get(presenter,"eventWheelPieceSources");if(eventTruck){Assert.That(Get(rig,"sourceBody"),Is.Null);var truck=(Component)Get(rig,"truck");var binding=truck.GetComponent(Sports("SportsWheelMeshBinding"));CollectionAssert.AreEqual((Transform[])Get(binding,"meshes"),sources);for(int i=0;i<4;i++)Assert.That(pieces.Count(piece=>(Transform)Get(piece,"source")==sources[i]),Is.EqualTo(1));var old=sources[0];try{sources[0]=sources[1];args=new object[]{body,null};Assert.That(presenter.GetType().GetMethod("Supports").Invoke(presenter,args),Is.EqualTo(false),"An ambiguous event wheel source may never be guessed.");}finally{sources[0]=old;}}
            else{Assert.That(Get(rig,"truck"),Is.Null);Assert.That(Get(rig,"sourceBody"),Is.Not.Null);var field=presenter.GetType().GetField("eventWheelPieceSources");try{field.SetValue(presenter,new Transform[]{null});args=new object[]{body,null};Assert.That(presenter.GetType().GetMethod("Supports").Invoke(presenter,args),Is.EqualTo(true),"Original sourceBody support does not depend on EventTruck-only data.");}finally{field.SetValue(presenter,sources);}}
            CollectionAssert.AreEqual(bodies,pieces.Select(piece=>(Rigidbody)Get(piece,"body")));CollectionAssert.AreEqual(shapes,pieces.Select(piece=>(Collider)Get(piece,"collider")));Assert.That(Get(host,"AppearanceRoot"),Is.Null,"Compatibility validation must not compose or allocate a body.");
        }
        finally{author.GetMethod("RestorePreviousSceneSetup").Invoke(null,new object[]{saved});}
    }
}
#endif

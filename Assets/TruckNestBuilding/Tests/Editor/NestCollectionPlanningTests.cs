#if UNITY_EDITOR
using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class NestCollectionPlanningTests
{
    const BindingFlags Any=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
    static Type Runtime(string name)=>Type.GetType("LWS.TruckNestBuilding."+name+", Assembly-CSharp",true);
    static object Read(object o,string n)=>o.GetType().GetField(n,Any) is FieldInfo f?f.GetValue(o):o.GetType().GetProperty(n,Any).GetValue(o);
    static void Write(object o,string n,object value){var field=o.GetType().GetField(n,Any);if(field!=null)field.SetValue(o,value);else o.GetType().GetProperty(n,Any).SetValue(o,value);}
    static object Call(object o,string n,params object[] args)=>o.GetType().GetMethod(n,Any).Invoke(o,args);
    sealed class PolicyFixture:IDisposable
    {
        public readonly GameObject root=new GameObject("Synthetic collection dependency policy only");public readonly Component site;public readonly Array builders;
        public PolicyFixture()
        {
            site=root.AddComponent(Runtime("NestBuildSite"));Call(site,"Clear");builders=Array.CreateInstance(Runtime("NestBuilder"),4);
            for(int i=0;i<4;i++){var go=new GameObject("Synthetic planner "+i);go.transform.SetParent(root.transform,false);var builder=go.AddComponent(Runtime("NestBuilder"));Write(builder,"slot",i);builders.SetValue(builder,i);}
        }
        public int Claim(int owner,object kind=null)
        {
            var builder=builders.GetValue(owner);Write(builder,"GoalSite",site);int socket=(int)Call(site,"ClaimNextForCollection",owner,builders,4,kind);Write(builder,"GoalSocket",socket);return socket;
        }
        public void Dispose(){UnityEngine.Object.DestroyImmediate(root);}
    }
    [Test]public void FutureCoreCollectionRequiresEveryActualPrerequisiteAssignmentAndCannotUseOrphanClaims()
    {
        using(var f=new PolicyFixture())
        {
            Assert.That(f.Claim(0),Is.EqualTo(0));Assert.That(f.Claim(1),Is.EqualTo(1));Assert.That(f.Claim(2),Is.EqualTo(2));Assert.That(f.Claim(3),Is.EqualTo(3));
            Assert.That((bool)Call(f.site,"Prerequisites",2),Is.False,"Reservation is not physical support.");Assert.That((bool)Call(f.site,"Prerequisites",3),Is.False);
            var materials=Array.CreateInstance(Runtime("NestMaterial"),0);Call(f.site,"Tick",materials,.02f);
            Assert.That((int)Read(f.site,"RootedCount"),Is.Zero);Assert.That((bool)Read(f.site,"CoreReady"),Is.False,"Collection cannot interlock or score an absent structure.");
            // Owner0 abandons a claim without touching the stale claim integer.
            // Its real builder assignment must invalidate future dependency work.
            Write(f.builders.GetValue(0),"GoalSocket",-1);
            var kind=Enum.Parse(Type.GetType("LWS.TruckNestBuilding.NestMaterialKind, LWS.TruckNestBuilding.Rules",true),"Stick");
            Assert.That(f.Claim(0,kind),Is.EqualTo(-1),"An unclaimed/orphan ancestor cannot be skipped for future sticks.");
            Assert.That(f.Claim(0),Is.EqualTo(0),"The physically ready missing ancestor takes priority.");
            Assert.That((bool)Call(f.site,"CollectionAncestorsCovered",2,f.builders,4),Is.True);
            ((Component)f.builders.GetValue(1)).gameObject.SetActive(false);
            Assert.That((bool)Call(f.site,"CollectionAncestorsCovered",2,f.builders,4),Is.False,"Inactive owner assignments do not qualify as inflight work.");
        }
    }
    [Test]public void AllWaitingOnMissingCoreSelectsOnlyOnePhysicalStagingWorkerAndPreservesReadyWork()
    {
        using(var f=new PolicyFixture())
        {
            var matchRoot=new GameObject("Inactive synthetic match policy only");matchRoot.SetActive(false);
            try
            {
                var match=matchRoot.AddComponent(Runtime("NestMatch"));Write(match,"builders",f.builders);var setup=Read(match,"setup");Write(setup,"count",4);var players=(Array)Read(setup,"players");
                for(int i=0;i<4;i++)
                {
                    var player=players.GetValue(i);var source=player.GetType().GetField("source");source.SetValue(player,Enum.Parse(source.FieldType,"AI"));
                    var builder=f.builders.GetValue(i);Write(builder,"GoalSite",f.site);Write(builder,"GoalSocket",2+i);
                    var piece=new GameObject("Synthetic finite cargo identity "+i);piece.transform.SetParent(f.root.transform,false);Write(builder,"Carried",piece.AddComponent(Runtime("NestMaterial")));
                }
                for(int i=0;i<4;i++)Assert.That((bool)Call(match,"ShouldRecoverMissingCore",f.builders.GetValue(i),f.site),Is.EqualTo(i==0),"Only one real worker stages its cargo to unblock an unassigned ancestor.");
                Write(f.builders.GetValue(0),"GoalSocket",0);
                for(int i=0;i<4;i++)Assert.That((bool)Call(match,"ShouldRecoverMissingCore",f.builders.GetValue(i),f.site),Is.False,"A worker already completing ready essential support prevents unnecessary staging.");
                Write(f.builders.GetValue(0),"GoalSocket",2);Write(f.builders.GetValue(0),"Carried",null);
                Assert.That((bool)Call(match,"ShouldRecoverMissingCore",f.builders.GetValue(1),f.site),Is.False,"The now free worker may retrieve support; companions retain their held cargo.");
            }
            finally{UnityEngine.Object.DestroyImmediate(matchRoot);}
        }
    }
}
#endif

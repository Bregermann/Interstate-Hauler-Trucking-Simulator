#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;
using LWS.TruckCamping;
using static WildernessNativeScope;
public sealed class CampingAiReadOnlyDiagnostics
{
    [Serializable]sealed class Candidate{public string id,kind;public bool available;public Vector3 home,actual,velocity;public float distance,homeError,regrowth;}
    [Serializable]sealed class Obstacle{public string name,path;public float distance,mass;public Vector3 point;}
    [Serializable]sealed class ActualWheel{public string name,hit;public bool grounded;public float load,spring;}
    static string Hierarchy(Transform t){string value=t.name;while(t.parent!=null){t=t.parent;value=t.name+"/"+value;}return value;}
    [Serializable]sealed class ExplorerRow{public int owner,task,collections,cooked;public string activity,pathStatus,hint;public int[] supplies;public Vector3 position,euler,velocity,target;public float speed;public Obstacle[] frontSweep;public ActualWheel[] wheels;public Vector3[] corners;public Vector2 command;public Candidate[] nearest;}
    [Serializable]sealed class FireRow{public bool built;public float fuel;public int meals;public Vector3 parking;}
    [Serializable]sealed class Row{public string evidence="READ_ONLY_AI_DIAGNOSTIC_NOT_COMPLETION";public int frame;public float real,clock,timeScale,maxMaterialSpeed;public ExplorerRow[] explorers;public FireRow[] fires;public Candidate[] fastMaterials;}
    static Candidate ReadPiece(Component p,Vector3 from){var b=(Rigidbody)Get(p,"body");return new Candidate{id=(string)Get(p,"id"),kind=Get(p,"kind").ToString(),available=(bool)Get(p,"Available"),home=(Vector3)Get(p,"home"),actual=b.position,velocity=b.linearVelocity,distance=Vector3.Distance(from,b.position),homeError=Vector3.Distance((Vector3)Get(p,"home"),b.position),regrowth=(float)Get(p,"Regrowth")};}
    [UnityTest,Timeout(35000)]public IEnumerator DiagnosticOnlyOriginalFourAiNavigationResourcePositionsStocksAndPublicWheelContactsWithinTwentyFiveRealSeconds()
    {
        using(var scope=new WildernessNativeScope("camp-original-ai-path-stock"))
        {
            yield return scope.Load("TruckCamping","LWS.TruckCamping.CampingMatch");scope.Configure(4,true);Assert.That((bool)Call(scope.match,"StartMatch"),Is.True);Time.timeScale=4;
            var world=(Component)Get(scope.match,"world");var explorers=Components(Get(scope.match,"explorers"));var resources=Components(Get(world,"resources"));string output=Path.ChangeExtension(scope.progress,".navigation.jsonl");Directory.CreateDirectory(Path.GetDirectoryName(output));
            var root=new GameObject("Test-owned read-only actual campsite wheel observer");root.transform.SetParent(scope.match.transform,false);var observer=root.AddComponent<CampingCargoWheelProbe>();observer.Configure(scope.match,resources,output+".wheel.jsonl");
            try
            {
                float end=Time.realtimeSinceStartup+25,next=0;int samples=0;
                while(Time.realtimeSinceStartup<end)
                {
                    if(Time.realtimeSinceStartup>=next)
                    {
                        next=Time.realtimeSinceStartup+2;var row=new Row{frame=Time.frameCount,real=Time.realtimeSinceStartup,clock=(float)Get(scope.match,"WildernessSeconds"),timeScale=Time.timeScale,maxMaterialSpeed=resources.Max(p=>((Rigidbody)Get(p,"body")).linearVelocity.magnitude),explorers=explorers.Select(e=>
                        {
                            var truck=Get(e,"truck");var body=(Rigidbody)Get(truck,"body");var journal=(CampJournal)Get(e,"Journal");var path=(NavMeshPath)Get(e,"path");var needed=journal.supplies[0]<3?CampSupply.Sticks:journal.supplies[1]<2?CampSupply.Firewood:CampSupply.Forage;
                            var destination=(Vector3)Get(e,"target");var delta=Vector3.ProjectOnPlane(destination-body.position,Vector3.up);var hits=delta.sqrMagnitude>.001f?body.SweepTestAll(delta.normalized,16,QueryTriggerInteraction.Ignore):Array.Empty<RaycastHit>();
                            return new ExplorerRow{euler=body.rotation.eulerAngles,speed=body.linearVelocity.magnitude,frontSweep=hits.Where(h=>h.collider!=null&&!h.collider.transform.IsChildOf(((Component)truck).transform)).OrderBy(h=>h.distance).Take(12).Select(h=>new Obstacle{name=h.collider.name,path=Hierarchy(h.collider.transform),distance=h.distance,point=h.point,mass=h.rigidbody==null?0:h.rigidbody.mass}).ToArray(),wheels=Components(Get(truck,"wheels")).Select(w=>new ActualWheel{name=w.name,hit=((Collider)Get(w,"HitCollider"))==null?"NONE":Hierarchy(((Collider)Get(w,"HitCollider")).transform),grounded=(bool)Get(w,"IsGrounded"),load=(float)Get(w,"Load"),spring=(float)Get(w,"SpringForce")}).ToArray(),owner=(int)Get(e,"slot"),task=(int)Get(e,"aiTask"),activity=Get(e,"Activity").ToString(),hint=(string)Get(e,"Hint"),collections=(int)Get(e,"ActualCollections"),cooked=journal.mealsCooked,supplies=(int[])journal.supplies.Clone(),position=body.position,velocity=body.linearVelocity,target=(Vector3)Get(e,"target"),pathStatus=path==null?"NULL":path.status.ToString(),corners=path==null?Array.Empty<Vector3>():path.corners,command=(Vector2)Get(truck,"command"),nearest=resources.Where(p=>(bool)Get(p,"Available")&&(CampSupply)Get(p,"kind")==needed).OrderBy(p=>(((Rigidbody)Get(p,"body")).position-body.position).sqrMagnitude).Take(3).Select(p=>ReadPiece(p,body.position)).ToArray()};
                        }).ToArray(),fires=Components(Get(world,"campsites")).Select(s=>{var f=(CampfireState)Get(s,"state");return new FireRow{built=f.built,fuel=f.fuel,meals=f.meals,parking=((Transform)Get(Get(s,"landmark"),"parking")).position};}).ToArray(),fastMaterials=resources.Where(p=>((Rigidbody)Get(p,"body")).linearVelocity.magnitude>10).Take(8).Select(p=>ReadPiece(p,Vector3.zero)).ToArray()};
                        File.AppendAllText(output,JsonUtility.ToJson(row)+"\n");samples++;TestContext.WriteLine("CAMP_AI_DIAGNOSTIC clock="+row.clock.ToString("R")+" collections="+string.Join(",",row.explorers.Select(e=>e.collections))+" maxCargoSpeed="+row.maxMaterialSpeed.ToString("R"));
                    }
                    yield return null;
                }
                Assert.That(samples,Is.GreaterThan(5));TestContext.WriteLine("CAMP_AI_DIAGNOSTIC_ONLY output="+output+" actual publicWheelContacts="+observer.contacts+"; no completion claim or source-state mutation.");
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
        }
    }
}
[DefaultExecutionOrder(32000)]public sealed class CampingCargoWheelProbe:MonoBehaviour
{
    [Serializable]sealed class Contact{public int frame,owner;public float clock,load,springForce,mass,speed;public string wheel,resource;public Vector3 position;}
    Component match;Component[] wheels;int[] owners;readonly Dictionary<Rigidbody,Component> cargo=new Dictionary<Rigidbody,Component>();readonly Dictionary<Component,Collider> previous=new Dictionary<Component,Collider>();string output;public int contacts;
    public void Configure(Component actual,Component[] resources,string path){match=actual;output=path;Directory.CreateDirectory(Path.GetDirectoryName(output));foreach(var p in resources)cargo.Add((Rigidbody)Get(p,"body"),p);var ws=new List<Component>();var os=new List<int>();foreach(var explorer in Components(Get(match,"explorers")))foreach(var wheel in Components(Get(Get(explorer,"truck"),"wheels"))){ws.Add(wheel);os.Add((int)Get(explorer,"slot"));}wheels=ws.ToArray();owners=os.ToArray();}
    void FixedUpdate()
    {
        if(match==null||!(bool)Get(match,"Live"))return;
        for(int i=0;i<wheels.Length;i++)
        {
            var w=wheels[i];var hit=(Collider)Get(w,"HitCollider");if((bool)Get(w,"IsGrounded")&&hit!=null&&hit.attachedRigidbody!=null&&cargo.TryGetValue(hit.attachedRigidbody,out var resource)&&(!previous.TryGetValue(w,out var old)||old!=hit))
            {contacts++;if(contacts<=100){var b=hit.attachedRigidbody;File.AppendAllText(output,JsonUtility.ToJson(new Contact{frame=Time.frameCount,owner=owners[i],clock=(float)Get(match,"WildernessSeconds"),wheel=w.name,resource=(string)Get(resource,"id"),load=(float)Get(w,"Load"),springForce=(float)Get(w,"SpringForce"),mass=b.mass,speed=b.linearVelocity.magnitude,position=b.position})+"\n");}}
            previous[w]=hit;
        }
    }
}
#endif

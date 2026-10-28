#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
public sealed partial class NestNativeTests
{
    [Serializable]sealed class EggContactFact{public int colliderId,materialSocket,eventFrame;public float eventFixedTime;public string name,materialId,materialSite;public bool exactRootedLining;public Vector3 point,normal;public float separation;}
    [Serializable]sealed class NearBroodMaterial{public string id,kind;public int colliderId,socket,lastOwner;public bool rooted,gripped,available,sleeping;public Vector3 position,velocity,boundsCenter,boundsSize,boxSize;public Matrix4x4 colliderMatrix;}
    [Serializable]sealed class EggContactFacts
    {
        public int site,rooted,frame;public float real;public bool live,paused,finished,core,present,stable,hatched,received,sleeping,kinematic;
        public float clock,incubation,settled,contactAge,capsuleRadius,capsuleHeight;public Vector3 position,velocity,angular,boundsCenter,boundsSize,capsuleCenter,liningPosition,liningBoundsCenter,liningBoundsSize;public string liningId,cachedContact;public int liningColliderId;
        public Matrix4x4 eggMatrix;public EggContactFact[] contacts;public NearBroodMaterial[] extraMaterials;
    }
    static EggContactFacts ObserveEgg(Component site)
    {
        var family=(Component)Get(site,"family");var match=(Component)Get(family,"match");var rules=Get(match,"rules");int index=(int)Get(site,"index");var body=(Rigidbody)Get(family,"eggBody");var shape=(Collider)Get(family,"eggCollider");var lining=Components(Get(site,"slots"))[9];var liningShape=lining==null?null:(Collider)Get(lining,"shape");var cached=(Collider)Get(family,"liningContact");
        var row=new EggContactFacts{real=Time.realtimeSinceStartup,frame=Time.frameCount,live=(bool)Get(match,"Live"),paused=(bool)Get(match,"Paused"),finished=(bool)Get(match,"Finished"),site=index,rooted=(int)Get(site,"RootedCount"),core=(bool)Get(site,"CoreReady"),present=(bool)Get(family,"EggPresent"),stable=(bool)Get(family,"EggStable"),hatched=(bool)Get(family,"Hatched"),received=((bool[])Get(rules,"received"))[index],incubation=((float[])Get(rules,"incubation"))[index],settled=((float[])Get(rules,"settled"))[index],clock=(float)Get(match,"Clock"),sleeping=body.IsSleeping(),kinematic=body.isKinematic,position=body.position,velocity=body.linearVelocity,angular=body.angularVelocity,boundsCenter=shape.bounds.center,boundsSize=shape.bounds.size,eggMatrix=shape.transform.localToWorldMatrix,contactAge=(float)Get(family,"contactAge"),cachedContact=cached==null?"NONE":cached.name,liningId=lining==null?"":(string)Get(lining,"pieceId"),liningColliderId=liningShape==null?0:liningShape.GetInstanceID(),liningPosition=lining==null?Vector3.zero:((Rigidbody)Get(lining,"body")).position,liningBoundsCenter=liningShape==null?Vector3.zero:liningShape.bounds.center,liningBoundsSize=liningShape==null?Vector3.zero:liningShape.bounds.size};
        if(shape is CapsuleCollider capsule){row.capsuleRadius=capsule.radius;row.capsuleHeight=capsule.height;row.capsuleCenter=capsule.center;}
        var probe=shape.GetComponent<NestEggCollisionReadOnlyProbe>();var contacts=probe==null?Array.Empty<NestEggCollisionReadOnlyProbe.Record>():probe.Snapshot();
        row.contacts=contacts.Select(record=>
        {
            var point=record.point;
            var other=point.thisCollider==shape?point.otherCollider:point.thisCollider;var material=other==null?null:other.GetComponentInParent(Type.GetType("LWS.TruckNestBuilding.NestMaterial, Assembly-CSharp",true));var owner=material==null?null:(Component)Get(material,"Site");
            return new EggContactFact{eventFrame=record.frame,eventFixedTime=record.fixedTime,colliderId=other==null?0:other.GetInstanceID(),name=other==null?"NONE":other.name,materialId=material==null?"":(string)Get(material,"pieceId"),materialSocket=material==null?-1:(int)Get(material,"Socket"),materialSite=owner==null?"":owner.name,exactRootedLining=other==liningShape&&lining!=null&&(bool)Call(site,"Rooted",lining),point=point.point,normal=point.normal,separation=point.separation};
        }).ToArray();
        row.extraMaterials=Components(Get(match,"materials")).Where(item=>item!=lining&&Get(item,"Site")==null&&Vector3.Distance(((Rigidbody)Get(item,"body")).position,row.liningPosition)<4).Select(item=>
        {
            var b=(Rigidbody)Get(item,"body");var c=(Collider)Get(item,"shape");return new NearBroodMaterial{id=(string)Get(item,"pieceId"),kind=Get(item,"kind").ToString(),colliderId=c.GetInstanceID(),socket=(int)Get(item,"Socket"),lastOwner=(int)Get(item,"LastOwner"),rooted=(bool)Call(site,"Rooted",item),gripped=(FixedJoint)Get(item,"Grip")!=null,available=(bool)Get(item,"Available"),sleeping=b.IsSleeping(),position=b.position,velocity=b.linearVelocity,boundsCenter=c.bounds.center,boundsSize=c.bounds.size,boxSize=c is BoxCollider box?box.size:Vector3.zero,colliderMatrix=c.transform.localToWorldMatrix};
        }).ToArray();return row;
    }
    [Serializable]sealed class EggDiagnosticPhase{public string stage;public int frame;public float real,clock;public bool live,finished;}
    [UnityTest,Timeout(180000)]public IEnumerator DiagnosticOnlyOrdinaryFourAiTwelveRootedCoreActualEggContactsAndPausedWorkshopWithin155Seconds()
    {
        using(var scope=new Scope("actual-egg-lining-contact"))
        {
            string phasePath=Path.Combine(Path.GetDirectoryName(scope.progress),"egg-"+Guid.NewGuid().ToString("N")+".phases.jsonl");
            Action<string> phase=stage=>File.AppendAllText(phasePath,JsonUtility.ToJson(new EggDiagnosticPhase{stage=stage,real=Time.realtimeSinceStartup,frame=Time.frameCount,clock=scope.match==null?0:(float)Get(scope.match,"Clock"),live=scope.match!=null&&(bool)Get(scope.match,"Live"),finished=scope.match!=null&&(bool)Get(scope.match,"Finished")})+"\n");
            phase("before-scene-load");yield return scope.Load();phase("after-scene-load");var match=scope.match;Set(Get(match,"setup"),"count",4);Set(match,"option",0);Call(match,"AllBots");
            var players=(Array)Get(Get(match,"setup"),"players");var bodies=new[]{"novelty.hotdog","truck.monster","truck.fire","novelty.icecream"};
            for(int i=0;i<4;i++){string id=(string)Get(players.GetValue(i),"participantId");var draft=Static("SportsParticipantRegistry","GetAppearance",id);Set(draft,"bodyId",bodies[i]);Static("SportsParticipantRegistry","TrySetAppearance",id,draft);}
            phase("before-original-start");Assert.That((bool)Call(match,"StartMatch"),Is.True);Time.timeScale=4;phase("after-original-start-before-loop");var site=Components(Get(match,"sites"))[0];string folder=Path.GetDirectoryName(scope.progress),stem="egg-"+Guid.NewGuid().ToString("N"),path=Path.Combine(folder,stem+".contacts.jsonl");Directory.CreateDirectory(folder);float end=Time.realtimeSinceStartup+155,next=0,settledAt=-1;int samples=0;
            while(Time.realtimeSinceStartup<end)
            {
                var family=(Component)Get(site,"family");bool unheld=Components(Get(match,"builders")).All(b=>Get(b,"Selected")==null&&(Get(b,"Carried")==null||(FixedJoint)Get(Get(b,"Carried"),"Grip")==null));
                if(settledAt<0&&unheld&&(((bool)Get(family,"Hatched"))||((bool)Get(match,"Finished"))||((int)Get(site,"RootedCount")==12&&(bool)Get(family,"EggPresent"))))settledAt=Time.realtimeSinceStartup;
                if(Time.realtimeSinceStartup>=next){File.AppendAllText(path,JsonUtility.ToJson(ObserveEgg(site))+"\n");samples++;next=Time.realtimeSinceStartup+2;}
                if(settledAt>=0&&Time.realtimeSinceStartup-settledAt>=3)break;yield return null;
            }
            phase("after-loop-before-end-trace");File.AppendAllText(path,JsonUtility.ToJson(ObserveEgg(site))+"\n");scope.Trace("actual-egg-contact-diagnostic-end");phase("after-end-trace");
            var builders=Components(Get(match,"builders"));bool safe=builders.All(b=>Get(b,"Selected")==null&&(Get(b,"Carried")==null||(FixedJoint)Get(Get(b,"Carried"),"Grip")==null));
            if(safe)
            {
                phase("before-original-pause");Call(match,"Pause",0,false);yield return null;yield return null;phase("after-pause-before-snapshot");string snapshot=(string)Call(match,"CaptureWorkshopJson");phase("after-snapshot-capture-before-write");File.WriteAllText(Path.Combine(folder,stem+".workshop.json"),snapshot);scope.Trace("actual-paused-unheld-workshop-captured");phase("after-snapshot-write-and-trace");
                TestContext.WriteLine("NEST_EGG_DIAGNOSTIC genuinepausedofficialsnapshot="+Path.Combine(folder,stem+".workshop.json")+"; no live pose/state injection.");
            }
            else TestContext.WriteLine("NEST_EGG_DIAGNOSTIC snapshotNOT_CAPTURED_actualHeldMaterials; guardpreserved.");
            Assert.That(samples,Is.GreaterThan(2));Assert.That((float)Get(match,"Clock"),Is.GreaterThan(50));phase("before-controlled-capture");Capture(scope,"diagnostic-only-actual-egg-contacts",1920,1080);phase("after-controlled-capture");
            TestContext.WriteLine("NEST_EGG_DIAGNOSTIC_ONLY path="+path+" rooted="+Get(site,"RootedCount")+"; same4x actualAI155real maximum; genuineHatched/Finished mayendmeasurementearly; no fullfamily acceptance/no rulecontactgateposeforcechange.");phase("complete-before-scope-dispose");
        }
    }
}
// Independent read-only observer of actual Collision.GetContacts callbacks.
// It never supplies family support or changes any collision/rule state.
public sealed class NestEggCollisionReadOnlyProbe:MonoBehaviour
{
    public struct Record{public ContactPoint point;public int frame;public float fixedTime;}
    readonly System.Collections.Generic.Dictionary<Collider,Record[]> pairs=new System.Collections.Generic.Dictionary<Collider,Record[]>();Collider shape;
    void Awake(){shape=GetComponent<Collider>();}
    void Remember(Collision collision){var points=new ContactPoint[32];int count=collision.GetContacts(points);pairs[collision.collider]=points.Take(count).Select(p=>new Record{point=p,frame=Time.frameCount,fixedTime=Time.fixedTime}).ToArray();}
    void OnCollisionEnter(Collision collision){Remember(collision);}
    void OnCollisionStay(Collision collision){Remember(collision);}
    void OnCollisionExit(Collision collision){pairs.Remove(collision.collider);}
    void FixedUpdate(){if(shape==null||!shape.enabled)pairs.Clear();}
    public Record[] Snapshot()=>pairs.Where(pair=>pair.Key!=null&&pair.Key.enabled&&pair.Key.gameObject.activeInHierarchy).SelectMany(pair=>pair.Value).ToArray();
}
#endif

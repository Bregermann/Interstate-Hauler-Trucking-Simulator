#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class BaseBirdPlayModeTests
{
 const BindingFlags Any=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
 static Type Runtime(string name)=>Type.GetType(name+", Assembly-CSharp",true);
 static object Get(object owner,string name)=>owner.GetType().GetField(name,Any) is FieldInfo field?field.GetValue(owner):owner.GetType().GetProperty(name,Any).GetValue(owner);
 static void Set(object owner,string name,object value)=>owner.GetType().GetField(name,Any).SetValue(owner,value);
 static object Call(object owner,string name,params object[] args)=>owner.GetType().GetMethods(Any).Single(method=>method.Name==name&&method.GetParameters().Length==args.Length).Invoke(owner,args);
 sealed class Fixture:IDisposable
 {
  readonly IDisposable prefs;readonly GameObject isolation;public Behaviour match;public Component flock;
  public object Bird(int i)=>((Array)Get(flock,"birds")).GetValue(i);public Rigidbody BirdBody(int i)=>(Rigidbody)Get(Bird(i),"body");public object Jumper(int i)=>((Array)Get(match,"jumpers")).GetValue(i);public Rigidbody Body(int i)=>(Rigidbody)Get(Get(Jumper(i),"truck"),"body");
  public Fixture(string suffix){prefs=(IDisposable)Runtime("LWS.TruckSports.SportsPrefs").GetMethod("IsolateValidation").Invoke(null,new object[]{"BaseBird"+suffix});isolation=new GameObject("Base bird native source isolation");UnityEngine.Object.DontDestroyOnLoad(isolation);isolation.AddComponent(Runtime("LWS.TruckSports.SportsProbeInputIsolation"));}
  public IEnumerator Load(bool earlyAiDeployment=false)
  {
   EditorSceneManager.LoadSceneInPlayMode("Assets/TruckBaseJumping/Scenes/TruckBaseJumping.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;match=UnityEngine.Object.FindObjectsByType(Runtime("LWS.TruckBaseJumping.BaseJumpMatch")).Cast<Behaviour>().Single();var author=Type.GetType("LWS.TruckBaseJumping.Editor.BaseBirdAuthoring, Assembly-CSharp-Editor",true);flock=(Component)author.GetMethod("Install").Invoke(null,new object[]{match,false});Assert.That(author.GetMethod("Install").Invoke(null,new object[]{match,false}),Is.SameAs(flock));Call(match,"AllBots");Runtime("LWS.TruckSports.SportsCameraOptions").GetMethod("Set").Invoke(null,new object[]{true});Set(match,"option",0);if(earlyAiDeployment){var player=((Array)Get(Get(match,"setup"),"players")).GetValue(0);Set(player,"difficulty",Enum.Parse(Get(player,"difficulty").GetType(),"Easy"));}Assert.That((bool)Call(match,"StartMatch"),Is.True);Set(flock,"physicalCrossingChance",0f);
  }
  public void Dispose(){Time.timeScale=1;if(match!=null)Call(match,"SetupScreen");if(flock!=null)Call(flock,"Clear");UnityEngine.Object.DestroyImmediate(isolation);prefs.Dispose();Time.timeScale=1;}
 }
 static void Capture(Fixture f,string label)
 {
  string folder=Path.GetFullPath("Logs/TruckSports/PlaytestRepair/BaseBirdNative");Directory.CreateDirectory(folder);var save=Runtime("LWS.TruckSports.SportsControlledCapture").GetMethods().Single(method=>method.Name=="Save"&&method.GetParameters().Length==6);var canvas=((Component)Get(f.match,"menus")).GetComponent<Canvas>();var cameras=(Camera[])Get(f.match,"cameras");foreach(int width in new[]{1280,1920})save.Invoke(null,new object[]{canvas,cameras,width,width==1280?720:1080,Path.Combine(folder,label+"-"+width+"-controlled.png"),null});
 }
 [UnityTest,Timeout(90000)]
 public IEnumerator OriginalGenericWingClipsChangeRealSkinnedPoseWhileTwelveBirdsFlyPauseAndRematch()
 {
  using(var f=new Fixture("Animation"))
  {
   yield return f.Load();Assert.That((bool)Call(f.flock,"TryCrossing",0,true),Is.False,"No crossing/contact before released open-canopy flight.");float deadline=Time.realtimeSinceStartup+12;while(!(bool)Get(f.flock,"Active")&&Time.realtimeSinceStartup<deadline)yield return null;Assert.That((bool)Get(f.flock,"Active"),Is.True);Assert.That(((Array)Get(f.flock,"birds")).Length,Is.EqualTo(12));Assert.That(((Array)Get(f.flock,"paths")).Length,Is.EqualTo(3));var ids=Enumerable.Range(0,12).Select(i=>((Component)f.Bird(i)).GetInstanceID()).ToArray();
   yield return new WaitForSeconds(.08f);for(int i=0;i<2;i++)
   {
    var animator=(Animator)Get(f.Bird(i),"animator");Assert.That(animator.avatar,Is.Not.Null);Assert.That(animator.avatar.isValid,Is.True);Assert.That(animator.avatar.isHuman,Is.False);Assert.That(animator.applyRootMotion,Is.False);var clip=animator.GetCurrentAnimatorClipInfo(0).Single().clip;Assert.That(AssetDatabase.GetAssetPath(clip),Does.StartWith("Assets/TruckBaseJumping/ThirdParty/QuaterniusBirds/"));Assert.That(clip.length,Is.GreaterThan(.2f));Assert.That(clip.isLooping,Is.True);
    var skin=animator.GetComponentInChildren<SkinnedMeshRenderer>(true);Assert.That(skin,Is.Not.Null);Assert.That(skin.bones.Length,Is.GreaterThanOrEqualTo(5));var bones=skin.bones.Where(bone=>bone!=null).ToArray();var rotations=bones.Select(bone=>bone.localRotation).ToArray();var baked=new Mesh();try{skin.BakeMesh(baked);var before=baked.vertices;yield return new WaitForSeconds(.19f);skin.BakeMesh(baked);var after=baked.vertices;float difference=before.Select((point,index)=>Vector3.Distance(point,after[index])).Max();int changed=bones.Where((bone,index)=>Quaternion.Angle(rotations[index],bone.localRotation)>.1f).Count();TestContext.WriteLine("BASE_BIRD_NATIVE_POSE actor="+i+" clip="+clip.name+" bones="+bones.Length+" changingBones="+changed+" bakedDeviation="+difference);Assert.That(changed,Is.GreaterThanOrEqualTo(2),"Multiple real artist rig bones must animate; moving the whole rigidbody is insufficient.");Assert.That(difference,Is.GreaterThan(skin.sharedMesh.bounds.size.magnitude*.001f),"The original wing clip must visibly deform the skinned mesh.");}finally{UnityEngine.Object.Destroy(baked);}
   }
   var origin=f.BirdBody(0).position;yield return new WaitForSeconds(.4f);Assert.That(Vector3.Distance(origin,f.BirdBody(0).position),Is.InRange(2f,8f));Assert.That(f.BirdBody(0).linearVelocity.magnitude,Is.LessThanOrEqualTo(16.2f));Assert.That(f.flock.GetComponentsInChildren<Camera>(true).Length,Is.Zero);Assert.That(f.flock.GetComponentsInChildren<Light>(true).Length,Is.Zero);Assert.That((int)Get(f.flock,"Contacts"),Is.Zero);Capture(f,"actual-animated-flock");
   Call(f.match,"Pause",0,false);yield return null;yield return null;var paused=f.BirdBody(0).position;var animation=(Animator)Get(f.Bird(0),"animator");float poseTime=animation.GetCurrentAnimatorStateInfo(0).normalizedTime;yield return new WaitForSecondsRealtime(.25f);Assert.That(f.BirdBody(0).position,Is.EqualTo(paused));Assert.That(animation.GetCurrentAnimatorStateInfo(0).normalizedTime,Is.EqualTo(poseTime).Within(.00001f));Call(f.match,"Resume");yield return new WaitForFixedUpdate();Assert.That(f.BirdBody(0).isKinematic,Is.False);
   Call(f.match,"SetupScreen");yield return null;Assert.That((bool)Get(f.flock,"Active"),Is.False);Assert.That(Enumerable.Range(0,12).All(i=>!((Component)f.Bird(i)).gameObject.activeSelf&&!((SphereCollider)Get(f.Bird(i),"shape")).enabled),Is.True);Assert.That((bool)Call(f.match,"StartMatch"),Is.True);yield return null;CollectionAssert.AreEqual(ids,Enumerable.Range(0,12).Select(i=>((Component)f.Bird(i)).GetInstanceID()).ToArray());Assert.That((int)Get(f.flock,"Contacts"),Is.Zero);
   TestContext.WriteLine("Original Quaternius Generic flight clips animated actual skins/bones; three UnitySpline formations,12fixed dynamic actors, no imported cameras/lights, pause and reused rematch pool verified. Initial scripted physical encounters are separate; normal flocks carry no hidden truck forces or score callbacks.");
  }
 }
 [UnityTest,Timeout(90000)]
 public IEnumerator PermittedTinyMassPhysicalBirdContactGivesModestDeviationOnceAndNeverFailsOrScores()
 {
  using(var f=new Fixture("PhysicalContact"))
  {
   yield return f.Load(true);float deadline=Time.realtimeSinceStartup+35;while((!(bool)Get(f.Jumper(0),"Released")||(float)Get(f.Jumper(0),"Opening")<=.99f||f.Body(0).position.y<55)&&Time.realtimeSinceStartup<deadline)yield return new WaitForFixedUpdate();TestContext.WriteLine("BASE_BIRD_CONTACT_PRECONDITION originalAi=Easy round="+Get(f.match,"Round")+" released="+Get(f.Jumper(0),"Released")+" deployed="+Get(f.Jumper(0),"Deployed")+" opening="+Get(f.Jumper(0),"Opening")+" flightTime="+Get(f.Jumper(0),"FlightTime")+" altitude="+f.Body(0).position.y+" landed="+Get(f.Jumper(0),"Landed"));Assert.That((bool)Get(f.Jumper(0),"Landed"),Is.False);Assert.That(f.Body(0).position.y,Is.GreaterThanOrEqualTo(55));Assert.That((float)Get(f.Jumper(0),"Opening"),Is.GreaterThan(.99f));Assert.That((float)Get(f.Jumper(0),"FlightTime"),Is.GreaterThan(4));f.match.enabled=false;Call(Get(f.match,"skyField"),"StopField");Set(f.flock,"crossingGroup",-1);Set(f.flock,"crossingParticipant",-1);
   var body=f.Body(0);var shape=(BoxCollider)Get(Get(f.Jumper(0),"truck"),"shape");var bird=f.Bird(0);var birdBody=f.BirdBody(0);var birdShape=(SphereCollider)Get(bird,"shape");var simulation=Physics.simulationMode;bool gravity=body.useGravity;float damping=body.linearDamping;var constraints=body.constraints;var interpolation=birdBody.interpolation;
   try
   {
    body.position=new Vector3(0,30,0);body.transform.SetPositionAndRotation(body.position,body.rotation);Physics.SyncTransforms();Assert.That((bool)Call(f.flock,"TryCrossing",0,true),Is.False,"The same deployed truck is protected near landing.");body.position=new Vector3(0,100,0);body.rotation=Quaternion.identity;body.transform.SetPositionAndRotation(body.position,body.rotation);var motor=Get(Get(f.Jumper(0),"truck"),"tractorMotor");if(motor is Component motorComponent&&motorComponent!=null)Call(motor,"ResetAttachedBodies");body.linearVelocity=body.angularVelocity=Vector3.zero;body.useGravity=false;body.linearDamping=0;body.constraints=RigidbodyConstraints.FreezePositionY|RigidbodyConstraints.FreezePositionZ|RigidbodyConstraints.FreezeRotation;Physics.SyncTransforms();Assert.That((bool)Call(f.flock,"TryCrossing",0,true),Is.True);
    for(int i=1;i<12;i++){((SphereCollider)Get(f.Bird(i),"shape")).enabled=false;f.BirdBody(i).isKinematic=true;}bool matrix=Physics.GetIgnoreLayerCollision(birdShape.gameObject.layer,shape.gameObject.layer);Call(f.flock,"Permissions",bird);Assert.That(birdShape.enabled,Is.True);Assert.That(Physics.GetIgnoreCollision(birdShape,shape),Is.False);Assert.That(Physics.GetIgnoreLayerCollision(birdShape.gameObject.layer,shape.gameObject.layer),Is.EqualTo(matrix),"Only this bird's collider override/pairs change; the shared traffic collision matrix stays unchanged.");for(int i=1;i<4;i++)Assert.That(Physics.GetIgnoreCollision(birdShape,(Collider)Get(Get(f.Jumper(i),"truck"),"shape")),Is.True);Assert.That(birdBody.mass/body.mass,Is.InRange(.002f,.0101f));
    // Controlled upright whole-assembly encounter: strike the actual +X chassis face normally.
    var face=shape.transform.TransformPoint(shape.center+Vector3.right*shape.size.x*.5f);var normal=shape.transform.right;var start=face+normal*(birdShape.radius+.35f);birdBody.interpolation=RigidbodyInterpolation.None;birdBody.position=start;birdBody.rotation=Quaternion.LookRotation(-normal);birdBody.transform.SetPositionAndRotation(start,birdBody.rotation);birdBody.linearVelocity=-normal*8;birdBody.angularVelocity=Vector3.zero;Physics.SyncTransforms();Physics.simulationMode=SimulationMode.Script;
    int steps=0;while((int)Get(f.flock,"Contacts")==0&&steps++<25)Physics.Simulate(Time.fixedDeltaTime);float deviation=Mathf.Abs(body.linearVelocity.x);TestContext.WriteLine("BASE_BIRD_REAL_CONTACT controlledUprightWholeAssembly=true steps="+steps+" birdMass="+birdBody.mass+" chassisMass="+body.mass+" assemblyBodies="+body.GetComponentsInChildren<Rigidbody>(true).Length+" massRatio="+(birdBody.mass/body.mass)+" chassisVelocityX="+body.linearVelocity.x.ToString("R")+" normal="+normal+" birdVelocity="+birdBody.linearVelocity+" truckVelocity="+body.linearVelocity+" impulse="+Get(f.flock,"LastImpulse")+" contacts="+Get(f.flock,"Contacts"));Assert.That((int)Get(f.flock,"Contacts"),Is.EqualTo(1),"Actual enabled sphere/chassis PhysX collision must produce the original collision callback.");Assert.That(deviation,Is.InRange(.005f,.45f));Assert.That((float)Get(f.flock,"LastImpulse"),Is.GreaterThan(0));Assert.That(birdShape.enabled,Is.False);Assert.That((bool)Get(f.Jumper(0),"Landed"),Is.False);Assert.That((int)Get(f.match,"LandingLocks"),Is.Zero);Assert.That(((int[])Get(f.match,"totals")),Is.All.Zero);Assert.That((int)Get(Get(f.match,"ScoreJournal"),"Count"),Is.Zero);
    Set(f.flock,"crossingGroup",-1);Assert.That((bool)Call(f.flock,"TryCrossing",0,true),Is.False,"One actual bird collision consumes this participant's permission for this round.");for(int i=0;i<5;i++)Physics.Simulate(Time.fixedDeltaTime);Assert.That((int)Get(f.flock,"Contacts"),Is.EqualTo(1));
   }
   finally{Physics.simulationMode=simulation;body.useGravity=gravity;body.linearDamping=damping;body.constraints=constraints;birdBody.interpolation=interpolation;}
   f.match.enabled=true;Call(f.match,"SetupScreen");yield return null;Assert.That((bool)Call(f.match,"StartMatch"),Is.True);yield return null;Assert.That((int)Get(f.flock,"Contacts"),Is.Zero);Assert.That(Enumerable.Range(0,12).All(i=>!((SphereCollider)Get(f.Bird(i),"shape")).enabled),Is.True);
   TestContext.WriteLine("Actual Easy AI first deployed, then explicitly controlled upright whole-assembly poses via existing ResetAttachedBodies and normal-incidence8m/s bird velocity isolated real PhysX contact; this is not a natural flock encounter. No direct truck force/velocity change after fixture initialization, no synthetic contact/score/failure call. Actual chassis layer permission, protected30maltitude, tiny mass, bounded deviation, once-per-round and rematch reset verified.");
  }
 }
}
#endif

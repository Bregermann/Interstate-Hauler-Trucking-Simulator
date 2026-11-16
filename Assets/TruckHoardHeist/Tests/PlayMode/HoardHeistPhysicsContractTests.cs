using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.IO;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif
using LWS.TruckHoardHeist;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

// Native adapter evidence only. This does not claim a complete saved-scene
// Heist match, NWH driving, dragon behavior or rendered acceptance.
public sealed class HoardHeistPhysicsContractTests
{
    Scene scene;
    Type Runtime(string name) => AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name,false)).First(t => t != null);
    GameObject Cube(string name,Vector3 position,Vector3 size)
    {
        var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;SceneManager.MoveGameObjectToScene(go,scene);go.transform.position=position;go.transform.localScale=size;return go;
    }
    object Invoke(object instance,string method,params object[] arguments) => instance.GetType().GetMethod(method,BindingFlags.Public|BindingFlags.Instance).Invoke(instance,arguments);

    [UnityTest]
    public IEnumerator ActualSharedHoistMovesTheOriginalDynamicBodyBeforeAnyPhysicalDeliveryValue()
    {
        scene=SceneManager.CreateScene("Heist isolated physical hoist "+Guid.NewGuid(),new CreateSceneParameters(LocalPhysicsMode.Physics3D));
        var physics=scene.GetPhysicsScene();
        Cube("Native support floor",new Vector3(0,-.25f,0),new Vector3(30,.5f,30));
        var receiving=Cube("Actual receiving rack",new Vector3(4,.25f,0),new Vector3(4,.5f,4));var receiver=receiving.AddComponent<Rigidbody>();receiver.isKinematic=true;
        var cargo=Cube("Original finite treasure",new Vector3(0,.52f,0),Vector3.one);var body=cargo.AddComponent<Rigidbody>();body.mass=10;body.solverIterations=20;body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
        int identity=body.GetInstanceID();
        var component=cargo.AddComponent(Runtime("LWS.TruckHoardHeist.HoardTreasure"));component.GetType().GetField("body").SetValue(component,body);component.GetType().GetField("shape").SetValue(component,cargo.GetComponent<BoxCollider>());
        var machine=new GameObject("Original shared physical Jenga crane");SceneManager.MoveGameObjectToScene(machine,scene);var crane=machine.AddComponent(Runtime("LWS.TruckJenga.JengaCrane"));
        var spreader=Cube("Real kinematic spreader",new Vector3(0,4,0),new Vector3(.2f,.1f,.2f)).AddComponent<Rigidbody>();spreader.isKinematic=true;crane.GetType().GetField("spreader").SetValue(crane,spreader);
        var rules=new HoardHeistRules(1,1,100,new[]{125},new[]{10f},new[]{identity});Assert.That(rules.ReserveLift(0,0,true),Is.True);
        var goal=new Vector3(4,1,0);Assert.That((bool)Invoke(crane,"Begin",body,goal,Quaternion.identity,1f),Is.True);
        Physics.SyncTransforms();
        bool rest=false;float furthest=0;Vector3 previous=body.position;
        for(int step=0;step<1000;step++)
        {
            Invoke(crane,"Step",.02f,.5f);physics.Simulate(.02f);
            furthest=Mathf.Max(furthest,Vector3.Distance(previous,body.position));previous=body.position;
            Assert.That(body.isKinematic,Is.False);Assert.That(rules.Scores[0],Is.Zero);
            rest=(bool)Invoke(crane,"AtRest",goal,1f);
            if(rest)break;
            yield return null;
        }
        Assert.That(rest,Is.True,"Real approach/lower/lift/traverse/release/settling must finish.");
        Assert.That(body.GetInstanceID(),Is.EqualTo(identity));Assert.That(Vector3.Distance(body.position,goal),Is.LessThan(.5f));Assert.That(furthest,Is.LessThan(1),"The original load may not teleport between fixtures.");
        Assert.That((int)crane.GetType().GetProperty("Pickups").GetValue(crane),Is.EqualTo(1));Assert.That((int)crane.GetType().GetProperty("Releases").GetValue(crane),Is.EqualTo(1));
        Assert.That((bool)Invoke(component,"Secure",0,receiver),Is.True);Assert.That(rules.CompleteLoad(0,0,true),Is.True);
        var zone=Cube("Real inner extraction",new Vector3(4,2,0),new Vector3(6,5,6)).GetComponent<BoxCollider>();zone.isTrigger=true;physics.Simulate(.02f);
        var fullyInside=Runtime("LWS.TruckHoardHeist.HoardHeistMatch").GetMethod("FullyInside",BindingFlags.Public|BindingFlags.Static);
        bool inside=(bool)fullyInside.Invoke(null,new object[]{cargo.GetComponent<BoxCollider>(),zone});
        var joint=(FixedJoint)component.GetType().GetProperty("CargoJoint").GetValue(component);
        var proof=new HoardDeliveryEvidence(identity,0,inside,!body.isKinematic,joint!=null&&joint.connectedBody==receiver,receiver.linearVelocity.magnitude<.7f,body.linearVelocity.magnitude<.8f);
        Assert.That(rules.Deliver(0,0,proof),Is.EqualTo(125));Assert.That(rules.Deliver(0,0,proof),Is.EqualTo(-1));
    }

    [UnityTest]
    public IEnumerator RestrainedOriginalCargoSeparatesOwnDeckRestFromARealExternalBreakLoad()
    {
        scene=SceneManager.CreateScene("Heist restraint cause "+Guid.NewGuid(),new CreateSceneParameters(LocalPhysicsMode.Physics3D));
        var physics=scene.GetPhysicsScene();Cube("Supported test floor",new Vector3(0,-.25f,0),new Vector3(60,.5f,60));
        var deck=Cube("Own finite rack deck",new Vector3(0,.11f,0),new Vector3(2.6f,.22f,4.8f));var truck=deck.AddComponent<Rigidbody>();truck.mass=6000;
        var cargo=Cube("Original 65kg cargo",new Vector3(0,.72f,0),Vector3.one);var body=cargo.AddComponent<Rigidbody>();body.mass=65;body.solverIterations=16;body.maxDepenetrationVelocity=3;body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
        var item=cargo.AddComponent(Runtime("LWS.TruckHoardHeist.HoardTreasure"));item.GetType().GetField("body").SetValue(item,body);item.GetType().GetField("shape").SetValue(item,cargo.GetComponent<BoxCollider>());
        int identity=body.GetInstanceID();Assert.That((bool)Invoke(item,"Secure",0,truck),Is.True);
        var joint=(FixedJoint)item.GetType().GetProperty("CargoJoint").GetValue(item);Assert.That(joint.breakForce,Is.EqualTo(2275));Assert.That(joint.enableCollision,Is.False);
        var observer=cargo.AddComponent<HoardCargoOwnContactWitness>();observer.truck=truck;
        Vector3 local=truck.transform.InverseTransformPoint(body.position);Physics.SyncTransforms();
        for(int i=0;i<150;i++)
        {
            if(i>=50)truck.AddForce(Vector3.forward*2,ForceMode.Acceleration);physics.Simulate(.02f);
            Assert.That(item.GetType().GetProperty("CargoJoint").GetValue(item),Is.Not.Null,"Resting on the own deck and modest physical acceleration must not exhaust the unchanged restraint.");
            Assert.That(Vector3.Distance(truck.transform.TransformPoint(local),body.position),Is.LessThan(.15f));
            yield return null;
        }
        Assert.That(observer.ownContacts,Is.Zero,"Own-deck contacts while restrained are a separate causal witness, not assumed from the later break snapshot.");
        body.AddForce(Vector3.right*joint.breakForce*10,ForceMode.Impulse);
        for(int i=0;i<10&&item.GetType().GetProperty("CargoJoint").GetValue(item)!=null;i++){physics.Simulate(.02f);yield return null;}
        Assert.That(item.GetType().GetProperty("CargoJoint").GetValue(item),Is.Null,"A genuine external load must still break the finite original restraint.");
        Assert.That((int)item.GetType().GetProperty("Carrier").GetValue(item),Is.EqualTo(-1));Assert.That(body.GetInstanceID(),Is.EqualTo(identity));Assert.That(body.isKinematic,Is.False);
    }

    [UnityTest]
    public IEnumerator ACenterInsideRotatedReceptionDoesNotCountPartlyOutsideCargo()
    {
        scene=SceneManager.CreateScene("Heist isolated reception "+Guid.NewGuid(),new CreateSceneParameters(LocalPhysicsMode.Physics3D));
        var zone=Cube("Rotated inner reception",Vector3.zero,new Vector3(3,3,3)).GetComponent<BoxCollider>();zone.isTrigger=true;zone.transform.rotation=Quaternion.Euler(0,45,0);
        var cargo=Cube("Actual wide treasure",Vector3.zero,new Vector3(2.4f,1,2.4f)).GetComponent<BoxCollider>();
        Physics.SyncTransforms();scene.GetPhysicsScene().Simulate(.02f);
        var test=Runtime("LWS.TruckHoardHeist.HoardHeistMatch").GetMethod("FullyInside",BindingFlags.Public|BindingFlags.Static);
        Assert.That(zone.bounds.Contains(cargo.bounds.center),Is.True);
        Assert.That((bool)test.Invoke(null,new object[]{cargo,zone}),Is.False,"AABB/center-only proof would award cargo crossing the real rotated boundary.");
        cargo.transform.localScale=new Vector3(1,1,1);Physics.SyncTransforms();scene.GetPhysicsScene().Simulate(.02f);
        Assert.That((bool)test.Invoke(null,new object[]{cargo,zone}),Is.True);
        yield return null;
    }

#if UNITY_EDITOR
    [Serializable] public sealed class ReleaseCauseRow
    {
        public string phase,stage,breakForce,breakTorque,interpolation,collisionMode,constraints,sceneName,lastJointBreakSnapshot;
        public int originalBodyId,jointId,connectedBodyId,step,renderFrame,simulationStep;
        public bool jointAlive,worldConnection,bodyKinematic,detectCollisions,shapeEnabled,bodySleeping,physicsSceneValid,autoConfigureConnectedAnchor,jointCollisionEnabled;
        public float mass,anchorGap,physicsAnchorGap,globalFixedTime,maximumAngularVelocity,jointMassScale,connectedMassScale;
        public int solverIterations,solverVelocityIterations,attachedJointCount,colliderAttachedBodyId;
        public Vector3 colliderWorldCenter,colliderWorldExtents;
        public bool rawBreakOverlapQueryUsesDefaultPhysicsSceneNotLocal;
        public Vector3 transformPosition,localCenterOfMass,inertiaTensor,physicsAnchorWorld,connectedPhysicsAnchorWorld,connectedPosition,connectedTransformPosition;
        public Quaternion transformRotation,inertiaTensorRotation,connectedRotation,connectedTransformRotation;
        public Matrix4x4 transformMatrix,physicsMatrix,connectedTransformMatrix,connectedPhysicsMatrix;
        public Vector3 shapeSize,shapeCenter,scale,position,velocity,angularVelocity,worldCenterOfMass,anchor,connectedAnchor,worldAnchor,connectedWorldAnchor,currentForce,currentTorque,momentumDelta;
        public Quaternion rotation;
    }
    [Serializable] public sealed class ReleaseCauseReport
    {
        public string scope,status,cleanupError;
        public Vector3 worldOrigin;
        public int[] originalCargoIds;
        public ReleaseCauseRow[] rows;
        public ReleaseCausePhase[] phases;
        public bool originalIdentitiesPreserved,originalRulesAndScoresUnchanged,kinematicReceiverIsNotNwh,gameplayAccepted,fixAccepted;
    }
    [Serializable] public sealed class ReleaseCausePhase
    {
        public string phase,reason;
        public float mass;
        public bool preconditionEligible,earlyJointBreak,windowMeasured;
    }
    static object CauseRead(object target,string name)
    {
        const BindingFlags flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
        for(Type type=target.GetType();type!=null;type=type.BaseType)
        {var f=type.GetField(name,flags|BindingFlags.DeclaredOnly);if(f!=null)return f.GetValue(target);var p=type.GetProperty(name,flags|BindingFlags.DeclaredOnly);if(p!=null)return p.GetValue(target);}
        throw new MissingMemberException(target.GetType().Name,name);
    }
    static object CauseCall(object target,string name,params object[] args)=>target.GetType().GetMethods(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).Single(m=>m.Name==name&&m.GetParameters().Length==args.Length).Invoke(target,args);
    static void CauseRecord(List<ReleaseCauseRow> rows,string phase,int step,int simulationStep,Rigidbody body,BoxCollider shape,FixedJoint joint,Vector3 initialVelocity,Component item=null,string stage=null)
    {
        bool alive=joint!=null;var connected=alive?joint.connectedBody:null;var a=alive?body.transform.TransformPoint(joint.anchor):body.position;var b=alive?(connected!=null?connected.transform.TransformPoint(joint.connectedAnchor):joint.connectedAnchor):body.position;
        var physicsMatrix=Matrix4x4.TRS(body.position,body.rotation,body.transform.lossyScale);var connectedPhysicsMatrix=connected!=null?Matrix4x4.TRS(connected.position,connected.rotation,connected.transform.lossyScale):Matrix4x4.identity;var pa=alive?physicsMatrix.MultiplyPoint3x4(joint.anchor):body.position;var pb=alive?(connected!=null?connectedPhysicsMatrix.MultiplyPoint3x4(joint.connectedAnchor):joint.connectedAnchor):body.position;
        var snapshot=item!=null?CauseRead(item,"LastJointBreakSnapshot"):null;
        rows.Add(new ReleaseCauseRow{phase=phase,stage=stage,interpolation=body.interpolation.ToString(),collisionMode=body.collisionDetectionMode.ToString(),constraints=body.constraints.ToString(),sceneName=body.gameObject.scene.name,physicsSceneValid=body.gameObject.scene.GetPhysicsScene().IsValid(),bodyKinematic=body.isKinematic,detectCollisions=body.detectCollisions,shapeEnabled=shape.enabled,bodySleeping=body.IsSleeping(),solverIterations=body.solverIterations,solverVelocityIterations=body.solverVelocityIterations,attachedJointCount=body.GetComponents<Joint>().Length,colliderAttachedBodyId=shape.attachedRigidbody!=null?shape.attachedRigidbody.GetInstanceID():0,colliderWorldCenter=shape.bounds.center,colliderWorldExtents=shape.bounds.extents,rawBreakOverlapQueryUsesDefaultPhysicsSceneNotLocal=true,maximumAngularVelocity=body.maxAngularVelocity,transformPosition=body.transform.position,transformRotation=body.transform.rotation,transformMatrix=body.transform.localToWorldMatrix,physicsMatrix=physicsMatrix,localCenterOfMass=body.centerOfMass,inertiaTensor=body.inertiaTensor,inertiaTensorRotation=body.inertiaTensorRotation,physicsAnchorWorld=pa,connectedPhysicsAnchorWorld=pb,physicsAnchorGap=Vector3.Distance(pa,pb),connectedPosition=connected!=null?connected.position:Vector3.zero,connectedRotation=connected!=null?connected.rotation:Quaternion.identity,connectedTransformPosition=connected!=null?connected.transform.position:Vector3.zero,connectedTransformRotation=connected!=null?connected.transform.rotation:Quaternion.identity,connectedTransformMatrix=connected!=null?connected.transform.localToWorldMatrix:Matrix4x4.identity,connectedPhysicsMatrix=connectedPhysicsMatrix,autoConfigureConnectedAnchor=alive&&joint.autoConfigureConnectedAnchor,jointCollisionEnabled=alive&&joint.enableCollision,jointMassScale=alive?joint.massScale:0,connectedMassScale=alive?joint.connectedMassScale:0,lastJointBreakSnapshot=snapshot!=null?JsonUtility.ToJson(snapshot):null,step=step,simulationStep=simulationStep,renderFrame=Time.frameCount,globalFixedTime=Time.fixedTime,originalBodyId=body.GetInstanceID(),mass=body.mass,shapeSize=shape.size,shapeCenter=shape.center,scale=shape.transform.lossyScale,position=body.position,rotation=body.rotation,velocity=body.linearVelocity,angularVelocity=body.angularVelocity,worldCenterOfMass=body.worldCenterOfMass,jointAlive=alive,jointId=alive?joint.GetInstanceID():0,connectedBodyId=connected!=null?connected.GetInstanceID():0,worldConnection=alive&&connected==null,anchor=alive?joint.anchor:Vector3.zero,connectedAnchor=alive?joint.connectedAnchor:Vector3.zero,worldAnchor=a,connectedWorldAnchor=b,anchorGap=Vector3.Distance(a,b),currentForce=alive?joint.currentForce:Vector3.zero,currentTorque=alive?joint.currentTorque:Vector3.zero,breakForce=alive?joint.breakForce.ToString("R",System.Globalization.CultureInfo.InvariantCulture):null,breakTorque=alive?joint.breakTorque.ToString("R",System.Globalization.CultureInfo.InvariantCulture):null,momentumDelta=(body.linearVelocity-initialVelocity)*body.mass});
    }

    [UnityTest,Timeout(240000)]
    public IEnumerator OriginalSavedCargoDestroyOnlyReleaseAndStopKeepCoherentSameFrameConnections()
    {
        var records=new List<ReleaseCauseRow>();var phases=new List<ReleaseCausePhase>();var origin=new Vector3(520,105,780);Component match=null;GameObject isolation=null;IDisposable preferences=null;bool completed=false,restored=false,rulesExact=false;string cleanupError=null;int[] identities=null;
        string[] args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonOutput");string output=Path.Combine(at>=0&&at+1<args.Length?args[at+1]:Application.persistentDataPath,"hoard-release-cause.json");Directory.CreateDirectory(Path.GetDirectoryName(output));
        try
        {
            preferences=(IDisposable)Runtime("LWS.TruckSports.SportsPrefs").GetMethod("IsolateValidation").Invoke(null,new object[]{"HoardReleaseCauseOnly"});Runtime("LWS.TruckSports.SportsParticipantRegistry").GetMethod("ForgetCache").Invoke(null,null);
            isolation=new GameObject("Release cause input isolation");UnityEngine.Object.DontDestroyOnLoad(isolation);isolation.AddComponent(Runtime("LWS.TruckSports.SportsProbeInputIsolation"));
            EditorSceneManager.LoadSceneInPlayMode("Assets/TruckHoardHeist/Scenes/TruckHoardHeist.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;
            match=(Component)UnityEngine.Object.FindAnyObjectByType(Runtime("LWS.TruckHoardHeist.HoardHeistMatch"));Assert.That(match,Is.Not.Null);CauseRead(match,"setup").GetType().GetField("count").SetValue(CauseRead(match,"setup"),4);CauseCall(match,"AllBots");Assert.That(CauseCall(match,"StartMatch"),Is.EqualTo(true));CauseCall(match,"Pause",0,false);Assert.That(CauseRead(match,"Paused"),Is.EqualTo(true));Assert.That(Time.timeScale,Is.Zero);
            var items=((Array)CauseRead(match,"treasure")).Cast<Component>().ToArray();identities=items.Select(v=>((Rigidbody)CauseRead(v,"body")).GetInstanceID()).ToArray();var rules=(HoardHeistRules)CauseRead(match,"Rules");var originalStates=rules.States.ToArray();var originalOwners=rules.Owners.ToArray();
            var machines=(Array)CauseRead(match,"hoists");object originalMachine=machines.GetValue(0);int simulationStep=0;
            scene=SceneManager.CreateScene("Actual saved Hoard release cause "+Guid.NewGuid(),new CreateSceneParameters(LocalPhysicsMode.Physics3D));var physics=scene.GetPhysicsScene();
            Cube("Local physical support only",origin+Vector3.down*.25f,new Vector3(30,.5f,30));
            foreach(float mass in new[]{28f,32f,65f})
            {
                var item=items.First(v=>Mathf.Abs(((Rigidbody)CauseRead(v,"body")).mass-mass)<.01f);var body=(Rigidbody)CauseRead(item,"body");var shape=(BoxCollider)CauseRead(item,"shape");var parent=item.transform.parent;int sibling=item.transform.GetSiblingIndex();var originalScene=item.gameObject.scene;Vector3 originalPosition=body.position,originalVelocity=body.linearVelocity,originalAngular=body.angularVelocity;Quaternion originalRotation=body.rotation;object originalMatch=CauseRead(item,"match");
                CauseRecord(records,mass.ToString("0")+"kg-original-global-registration",-1000,simulationStep,body,shape,null,body.linearVelocity,item,"original-global-before-detach");Assert.That(body.GetComponents<Joint>(),Is.Empty);item.transform.SetParent(null,true);SceneManager.MoveGameObjectToScene(item.gameObject,scene);item.GetType().GetField("match").SetValue(item,null);
                try
                {
                    foreach(string phase in new[]{"Release-current","Release-destroy-only-control","StopHoist-current","StopCrane-destroy-only-control"})
                    {
                        CauseCall(item,"Release");body.linearVelocity=body.angularVelocity=Vector3.zero;body.rotation=Quaternion.identity;float height=(float)CauseRead(item,"Height");FixedJoint joint=null;GameObject receiver=null,machine=null;Vector3 initialVelocity;string label=mass.ToString("0",System.Globalization.CultureInfo.InvariantCulture)+"kg-"+phase;
                        if(phase.StartsWith("Release"))
                        {
                            receiver=new GameObject("Controlled kinematic receiver NOT NWH");SceneManager.MoveGameObjectToScene(receiver,scene);receiver.transform.position=origin;var truck=receiver.AddComponent<Rigidbody>();truck.isKinematic=true;var deck=receiver.AddComponent<BoxCollider>();deck.center=new Vector3(0,3.5f,-2.5f);deck.size=new Vector3(2.6f,.22f,4.8f);body.position=origin+deck.center+Vector3.up*(.11f+height*.5f+.06f);item.transform.SetPositionAndRotation(body.position,body.rotation);Physics.SyncTransforms();Assert.That(Vector3.Distance(body.position,item.transform.position),Is.LessThan(.001f),"Explicit test calibration removes V620 scene-migration interpolation artifact; production Secure is unchanged.");CauseRecord(records,label,-201,simulationStep,body,shape,null,body.linearVelocity,item,"post-migration-pose-sync-PRE-Secure");Assert.That(CauseCall(item,"Secure",0,truck),Is.EqualTo(true));joint=(FixedJoint)CauseRead(item,"CargoJoint");CauseRecord(records,label,-200,simulationStep,body,shape,joint,body.linearVelocity,item,"immediate-POST-Secure");Assert.That(records.Last().physicsAnchorGap,Is.LessThan(.001f),"Calibrated test starts with coherent physics anchors.");
                            Vector3 beforeNormal=body.linearVelocity;for(int i=0;i<8;i++){physics.Simulate(.02f);simulationStep++;CauseRecord(records,label,-108+i,simulationStep,body,shape,joint,beforeNormal,item,"normal-precondition-step-"+(i+1));}
                        }
                        else
                        {
                            body.position=origin+Vector3.up*(height*.5f+.02f);item.transform.SetPositionAndRotation(body.position,body.rotation);Physics.SyncTransforms();machine=new GameObject("Actual shared Jenga crane control");SceneManager.MoveGameObjectToScene(machine,scene);var crane=machine.AddComponent(Runtime("LWS.TruckJenga.JengaCrane"));var spreader=Cube("Actual-size kinematic spreader",origin+Vector3.up*4,new Vector3(.32f,.1f,.32f)).AddComponent<Rigidbody>();spreader.isKinematic=true;crane.GetType().GetField("spreader").SetValue(crane,spreader);crane.GetType().GetField("travelSpeed").SetValue(crane,5f);crane.GetType().GetField("liftSpeed").SetValue(crane,3f);crane.GetType().GetField("turnSpeed").SetValue(crane,75f);Physics.SyncTransforms();Assert.That(CauseCall(crane,"Begin",body,origin+Vector3.right*3+Vector3.up*(height*.5f+.02f),Quaternion.identity,height),Is.EqualTo(true));
                            int steps=0;while((int)CauseRead(crane,"Pickups")==0&&steps++<800){CauseCall(crane,"Step",.02f,origin.y);physics.Simulate(.02f);simulationStep++;}Assert.That((int)CauseRead(crane,"Pickups"),Is.EqualTo(1),"Actual shared crane must physically grip the saved original body.");
                            for(int i=0;i<5;i++){CauseCall(crane,"Step",.02f,origin.y);physics.Simulate(.02f);simulationStep++;}joint=body.GetComponents<FixedJoint>().Single();machines.SetValue(crane,0);
                        }
                        bool eligible=joint!=null&&(phase.StartsWith("Release")?CauseRead(item,"CargoJoint")!=null:body.GetComponents<FixedJoint>().Any(j=>j==joint));phases.Add(new ReleaseCausePhase{phase=label,mass=mass,preconditionEligible=eligible,earlyJointBreak=!eligible,windowMeasured=eligible,reason=eligible?"LIVE_ORIGINAL_RESTRAINT_REACHES_COMMAND_WINDOW":"EARLY_RESTRAINT_BREAK_NO_RELEASE_WINDOW_CLAIM"});
                        Assert.That(eligible,Is.True,"All calibrated regression phases require a live original restraint before testing teardown.");
                        if(eligible)
                        {
                        var originalConnected=joint.connectedBody;float originalBreakForce=joint.breakForce,originalBreakTorque=joint.breakTorque;Assert.That(originalConnected,Is.Not.Null);
                        initialVelocity=body.linearVelocity;CauseRecord(records,label,-1,simulationStep,body,shape,joint,initialVelocity,item,"pre-command-eligible");int frame=Time.frameCount;
                        if(phase=="Release-current")CauseCall(item,"Release");
                        else if(phase=="Release-destroy-only-control")UnityEngine.Object.Destroy(joint);
                        else if(phase=="StopHoist-current")CauseCall(match,"StopHoist",0);
                        else CauseCall(machines.GetValue(0),"StopCrane");
                        CauseRecord(records,label,0,simulationStep,body,shape,joint,initialVelocity,item,"immediate-post-command");Assert.That(joint.connectedBody,Is.SameAs(originalConnected),"Pending deferred Destroy must not create a world restraint.");
                        // Exactly two genuine LOCAL PhysX steps before yielding.
                        // No external force, score, Clock or default-world step.
                        for(int step=1;step<=2;step++){physics.Simulate(.02f);simulationStep++;CauseRecord(records,label,step,simulationStep,body,shape,joint,initialVelocity,item,"post-command-local-step");Assert.That(joint.connectedBody,Is.SameAs(originalConnected));Assert.That(joint.breakForce,Is.EqualTo(originalBreakForce));Assert.That(joint.breakTorque,Is.EqualTo(originalBreakTorque));Assert.That(records.Last().physicsAnchorGap,Is.LessThan(.02f));}
                        Assert.That(Time.frameCount,Is.EqualTo(frame));
                        }
                        else CauseRecord(records,label,-300,simulationStep,body,shape,joint,body.linearVelocity,item,"ineligible-primary-early-break-retained");
                        yield return null;Assert.That(joint==null,Is.True,"Original deferred destruction completes at the render boundary.");
                        CauseCall(item,"Release");machines.SetValue(originalMachine,0);if(receiver!=null)UnityEngine.Object.Destroy(receiver);if(machine!=null)UnityEngine.Object.Destroy(machine);foreach(var go in scene.GetRootGameObjects())if(go.name=="Actual-size kinematic spreader")UnityEngine.Object.Destroy(go);yield return null;
                    }
                }
                finally
                {
                    machines.SetValue(originalMachine,0);foreach(var joint in body.GetComponents<Joint>())UnityEngine.Object.DestroyImmediate(joint);CauseCall(item,"Release");item.GetType().GetField("match").SetValue(item,originalMatch);SceneManager.MoveGameObjectToScene(item.gameObject,originalScene);item.transform.SetParent(parent,true);item.transform.SetSiblingIndex(sibling);body.position=originalPosition;body.rotation=originalRotation;item.transform.SetPositionAndRotation(originalPosition,originalRotation);body.linearVelocity=originalVelocity;body.angularVelocity=originalAngular;
                }
            }
            restored=identities.SequenceEqual(items.Select(v=>((Rigidbody)CauseRead(v,"body")).GetInstanceID()));rulesExact=originalStates.SequenceEqual(rules.States)&&originalOwners.SequenceEqual(rules.Owners)&&rules.Scores.All(v=>v==0)&&rules.DeliveredCount==0;Assert.That(restored&&rulesExact,Is.True);completed=true;
        }
        finally
        {
            try{if(match!=null)CauseCall(match,"SetupScreen");if(isolation!=null)UnityEngine.Object.DestroyImmediate(isolation);preferences?.Dispose();Runtime("LWS.TruckSports.SportsParticipantRegistry").GetMethod("ForgetCache").Invoke(null,null);}
            catch(Exception error){cleanupError=error.ToString();}
            File.WriteAllText(output,JsonUtility.ToJson(new ReleaseCauseReport{scope="CONTROLLED_ACTUAL_SAVED_28_32_65KG_CARGO_PRODUCTION_RELEASE_AND_STOPHOIST_VS_DESTROY_ONLY_TWO_LOCAL_PHYSX_STEPS_SAME_RENDER_FRAME_NOT_NWH_DRIVING_OR_GAMEPLAY",status=completed?"MEASURED_PRECONDITIONS_AND_ELIGIBLE_WINDOWS_REQUIRES_CAUSAL_REVIEW_NOT_FIX_ACCEPTANCE":"INCOMPLETE_OR_NUNIT_FAILURE_RETAINED",cleanupError=cleanupError,worldOrigin=origin,originalCargoIds=identities,rows=records.ToArray(),phases=phases.ToArray(),originalIdentitiesPreserved=restored,originalRulesAndScoresUnchanged=rulesExact,kinematicReceiverIsNotNwh=true,gameplayAccepted=false,fixAccepted=false},true));
            Assert.That(cleanupError,Is.Null,"Preserve the primary NUnit result and recorded cleanup error; no gameplay acceptance.");
        }
    }

    [Serializable] public sealed class RigTrafficReport
    {
        public string scope,status,cleanupError,mastName,railName;public int[] originalCargoIds,originalTruckIds;public float actualMastRailPenetration;
        public Bounds actualMast,actualRail,wholeRig;public int lowSensorOtherCrewHits,laterConflictEpisodes;public bool highRigConflictDetected,separatingInitialContactAccepted,increasingOverlapRejected,unsupportedShapeRejected,nearWorkspacesRejected,disjointWorkspacesAccepted,loadedCargoIncluded,humanCommandExact,heldRouteProgressUnchanged,identitiesAndRulesUnchanged,blockedRearUsesForward,higherWaitsForLower,higherFallbackAfterNoCourse,onlyOnePairYield,tangentialClearanceAccepted,gameplayAccepted;
    }
    [UnityTest,Timeout(180000)]
    public IEnumerator OriginalSavedCompoundRigsReserveDisjointWorkspacesAndSeparateWithoutDriverStallOrHumanOverride()
    {
        Component match=null;GameObject isolation=null,unsupported=null,rearBarrier=null,frontBarrier=null;IDisposable prefs=null;Component[] trucks=null;Vector3[] positions=null,velocities=null,angular=null;Quaternion[] rotations=null;
        var proof=new RigTrafficReport{scope="CONTROLLED_ORIGINAL_SAVED_NWH_COMPOUND_MAST_RAIL_WORKSPACE_AND_BOUNDED_YIELD_REGRESSION_NOT_FULL_MATCH_ACCEPTANCE",status="INCOMPLETE_NUNIT_FAILURE_RETAINED"};
        var args=Environment.GetCommandLineArgs();int arg=Array.IndexOf(args,"-tsDragonOutput");var output=Path.Combine(arg>=0&&arg+1<args.Length?args[arg+1]:Application.persistentDataPath,"hoard-compound-rig-regression.json");Directory.CreateDirectory(Path.GetDirectoryName(output));
        try
        {
            prefs=(IDisposable)Runtime("LWS.TruckSports.SportsPrefs").GetMethod("IsolateValidation").Invoke(null,new object[]{"OriginalCompoundRigRegression"});Runtime("LWS.TruckSports.SportsParticipantRegistry").GetMethod("ForgetCache").Invoke(null,null);isolation=new GameObject("Original compound rig input isolation");UnityEngine.Object.DontDestroyOnLoad(isolation);isolation.AddComponent(Runtime("LWS.TruckSports.SportsProbeInputIsolation"));
            EditorSceneManager.LoadSceneInPlayMode("Assets/TruckHoardHeist/Scenes/TruckHoardHeist.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;match=(Component)UnityEngine.Object.FindAnyObjectByType(Runtime("LWS.TruckHoardHeist.HoardHeistMatch"));Assert.That(match,Is.Not.Null);var setup=CauseRead(match,"setup");setup.GetType().GetField("count").SetValue(setup,4);CauseCall(match,"AllBots");Assert.That(CauseCall(match,"StartMatch"),Is.EqualTo(true));
            float deadline=Time.realtimeSinceStartup+20;while((float)CauseRead(match,"Intro")>0&&Time.realtimeSinceStartup<deadline)yield return null;Assert.That((float)CauseRead(match,"Intro"),Is.Zero,"Wait for genuine NWH-supported countdown, never inject Clock/Intro.");CauseCall(match,"Pause",0,false);Assert.That(Time.timeScale,Is.Zero);
            trucks=((Array)CauseRead(match,"trucks")).Cast<Component>().ToArray();var items=((Array)CauseRead(match,"treasure")).Cast<Component>().ToArray();var bodies=trucks.Select(v=>(Rigidbody)CauseRead(v,"body")).ToArray();proof.originalTruckIds=bodies.Select(v=>v.GetInstanceID()).ToArray();proof.originalCargoIds=items.Select(v=>((Rigidbody)CauseRead(v,"body")).GetInstanceID()).ToArray();positions=bodies.Select(v=>v.position).ToArray();rotations=bodies.Select(v=>v.rotation).ToArray();velocities=bodies.Select(v=>v.linearVelocity).ToArray();angular=bodies.Select(v=>v.angularVelocity).ToArray();
            void Move(int slot,Vector3 point){CauseCall(trucks[slot],"Prepare",point,Quaternion.identity,true);CauseCall(match,"ParkHoist",slot);Physics.SyncTransforms();match.GetType().GetField("trafficBoundsAt",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(match,-1f);}
            Move(0,new Vector3(450,105.12f,730));Move(1,new Vector3(570,105.12f,730));Move(2,new Vector3(520,105.12f,780));Move(3,new Vector3(522.8f,105.12f,780));
            var mast=trucks[2].GetComponentsInChildren<Collider>().Single(v=>v.name=="Actual mounted crane mast");var rail=trucks[3].GetComponentsInChildren<Collider>().Where(v=>v.name=="Physical cargo rail").OrderBy(v=>v.bounds.center.x).First();proof.mastName=mast.name;proof.railName=rail.name;proof.actualMast=mast.bounds;proof.actualRail=rail.bounds;
            Assert.That(Physics.ComputePenetration(mast,mast.transform.position,mast.transform.rotation,rail,rail.transform.position,rail.transform.rotation,out _,out proof.actualMastRailPenetration),Is.True,"Controlled actual original mast and rail must reproduce high contact.");Assert.That(mast.bounds.min.y,Is.GreaterThan(bodies[2].position.y+3));
            var lowHits=Physics.SphereCastAll(bodies[2].position+Vector3.up*1.1f,.8f,Vector3.forward,8,~0,QueryTriggerInteraction.Ignore);proof.lowSensorOtherCrewHits=lowHits.Count(v=>v.collider.transform.IsChildOf(trucks[3].transform));Assert.That(proof.lowSensorOtherCrewHits,Is.Zero,"The original lower-front sensor cannot represent this high side contact.");
            object[] conflict={2,new Vector2(.8f,1),-1,false};proof.highRigConflictDetected=(bool)CauseCall(match,"RigTrafficConflict",conflict);Assert.That(proof.highRigConflictDetected,Is.True);Assert.That(conflict[2],Is.EqualTo(3));proof.wholeRig=((Bounds[])CauseRead(match,"trafficRigBounds"))[2];Assert.That(proof.wholeRig.SqrDistance(mast.bounds.max),Is.LessThan(.000001f));
            var initial=new Pose(bodies[2].position,bodies[2].rotation);var depths=new Dictionary<(int,int),float>();proof.separatingInitialContactAccepted=(bool)CauseCall(match,"RigSeparatingPose",2,3,initial,initial,depths,false);Assert.That(proof.separatingInitialContactAccepted,Is.True);Assert.That(depths.Values.Any(v=>v>0),Is.True);
            foreach(float distance in new[]{.25f,.5f,1f,3f})Assert.That((bool)CauseCall(match,"RigSeparatingPose",2,3,initial,new Pose(initial.position+Vector3.left*distance,initial.rotation),depths,distance==3),Is.True,"Existing contacts must separate without any new or increasing physical penetration.");
            var tangentialDepths=new Dictionary<(int,int),float>();foreach(float distance in new[]{0f,.25f,1f,3f,5.5f,6f})Assert.That((bool)CauseCall(match,"RigSeparatingPose",2,3,initial,new Pose(initial.position+Vector3.forward*distance,initial.rotation),tangentialDepths,distance==6),Is.True,"Actual mast/rail tangential separation must never increase penetration and must completely exit at the endpoint.");Assert.That(tangentialDepths.Values.All(v=>v==0),Is.True,"The complete 6m tangential exit must leave zero actual shape penetration.");
            proof.increasingOverlapRejected=!(bool)CauseCall(match,"RigSeparatingPose",2,3,initial,new Pose(initial.position+Vector3.right*.1f,initial.rotation),new Dictionary<(int,int),float>(),false);Assert.That(proof.increasingOverlapRejected,Is.True);
            unsupported=new GameObject("Owned unresolved shape regression");var unresolved=unsupported.AddComponent<MeshCollider>();var resolved=match.GetType().GetMethod("RigShapeResolved",BindingFlags.NonPublic|BindingFlags.Static);proof.unsupportedShapeRejected=!(bool)resolved.Invoke(null,new object[]{unresolved});Assert.That(proof.unsupportedShapeRejected,Is.True);
            // Include a real original restrained load outside the empty rig envelope.
            var item=items.First(v=>Mathf.Abs(((Rigidbody)CauseRead(v,"body")).mass-28)<.01f);var cargo=(Rigidbody)CauseRead(item,"body");var shape=(BoxCollider)CauseRead(item,"shape");var cargoPosition=cargo.position;var cargoRotation=cargo.rotation;
            try{cargo.position=proof.wholeRig.max+Vector3.right*2;item.transform.SetPositionAndRotation(cargo.position,cargo.rotation);Physics.SyncTransforms();Assert.That(CauseCall(item,"Secure",2,bodies[2]),Is.EqualTo(true));match.GetType().GetField("trafficBoundsAt",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(match,-1f);CauseCall(match,"RefreshRigBounds");var loaded=((Bounds[])CauseRead(match,"trafficRigBounds"))[2];proof.loadedCargoIncluded=loaded.SqrDistance(shape.bounds.max)<.000001f;Assert.That(proof.loadedCargoIncluded,Is.True);CauseCall(item,"Release");yield return null;}
            finally{CauseCall(item,"Release");cargo.position=cargoPosition;cargo.rotation=cargoRotation;item.transform.SetPositionAndRotation(cargoPosition,cargoRotation);Physics.SyncTransforms();}
            CauseCall(match,"Resume");Assert.That(CauseRead(match,"Live"),Is.EqualTo(true));Assert.That(CauseCall(match,"IsDriving",0),Is.EqualTo(true),"Workspace claims require the live public session. Remaining assertions run synchronously before cleanup, without yielding.");
            for(int slot=0;slot<4;slot++)Move(slot,new Vector3(430+slot*35,105.12f,720));
            var targets=(int[])CauseRead(match,"Targets");for(int i=0;i<4;i++)targets[i]=-1;targets[0]=3;var valid=(bool[,])CauseRead(match,"treasureAccessValid");var parking=(Vector3[,])CauseRead(match,"treasureAccessParking");valid[0,3]=valid[1,7]=valid[1,12]=true;parking[0,3]=new Vector3(539.5f,105.12f,775.5f);parking[1,7]=new Vector3(539.5f,105.12f,782.5f);parking[1,12]=new Vector3(500.5f,105.12f,784.5f);
            proof.nearWorkspacesRejected=!(bool)CauseCall(match,"HoistWorkspaceClear",1,7);proof.disjointWorkspacesAccepted=(bool)CauseCall(match,"HoistWorkspaceClear",1,12);Assert.That(proof.nearWorkspacesRejected&&proof.disjointWorkspacesAccepted,Is.True,"Different cargo IDs cannot reserve overlapping hoists; disjoint real jobs remain concurrent.");
            var starts=(int[])CauseRead(match,"trafficYieldStarts");
            for(int episode=0;episode<5;episode++){Move(2,new Vector3(500+episode*7,105.12f,780));CauseCall(match,"RigConflictEpisode",2,3);Assert.That(starts[2],Is.Zero,"Meaningful progress opens a new bounded conflict episode, including after four earlier conflicts.");starts[2]=4;CauseCall(match,"RigConflictEpisode",2,3);Assert.That(starts[2],Is.EqualTo(4),"One unchanged conflict keeps its finite attempt bound.");proof.laterConflictEpisodes++;}
            // Real saved compound rigs on the measured flat original road. Only
            // disposable static barriers are authored for this controlled test.
            for(int slot=0;slot<4;slot++){Move(slot,new Vector3(430+slot*35,105.12f,720));CauseCall(match,"ClearRigYield",slot);}
            int lower=(int)CauseCall(match,"RigTrafficRank",2)>(int)CauseCall(match,"RigTrafficRank",3)?2:3,higher=lower==2?3:2;
            Move(lower,new Vector3(635,105.12f,780));Move(higher,new Vector3(638.2f,105.12f,780));
            rearBarrier=new GameObject("Owned blocked rear traffic regression");var rearShape=rearBarrier.AddComponent<BoxCollider>();rearShape.size=new Vector3(2.6f,8,1);rearBarrier.transform.position=new Vector3(635,109,774);Physics.SyncTransforms();
            Assert.That(CauseCall(match,"RigYieldCorridor",lower,bodies[lower].position,bodies[lower].position-Vector3.forward*3,Quaternion.identity,Quaternion.identity),Is.EqualTo(false),"Real static rear barrier must reject the original rear-only course.");
            proof.tangentialClearanceAccepted=(bool)CauseCall(match,"RigSeparatingPose",lower,higher,new Pose(bodies[lower].position,Quaternion.identity),new Pose(bodies[lower].position+Vector3.forward,Quaternion.identity),new Dictionary<(int,int),float>(),false);Assert.That(proof.tangentialClearanceAccepted,Is.True,"Tangential movement cannot create or increase any physical penetration.");
            proof.blockedRearUsesForward=(bool)CauseCall(match,"BeginRigYield",lower,higher)&&!((bool[])CauseRead(match,"trafficYieldBackwards"))[lower];Assert.That(proof.blockedRearUsesForward,Is.True,"Blocked rear must choose an actually guard-validated forward/side withdrawal.");
            CauseCall(match,"ClearRigYield",lower);CauseCall(match,"ClearRigYield",higher);
            frontBarrier=new GameObject("Owned blocked forward traffic regression");var frontShape=frontBarrier.AddComponent<BoxCollider>();frontShape.size=new Vector3(2.6f,8,1);frontBarrier.transform.position=new Vector3(635,109,784);Physics.SyncTransforms();
            proof.higherWaitsForLower=!(bool)CauseCall(match,"BeginRigYield",higher,lower);Assert.That(proof.higherWaitsForLower,Is.True,"Priority partner cannot pre-empt the lower rig's first real course search.");
            Assert.That(CauseCall(match,"BeginRigYield",lower,higher),Is.EqualTo(false),"Both actual static barriers must exhaust this lower rig's finite guarded courses.");Assert.That(((int[])CauseRead(match,"trafficNoCourseOther"))[lower],Is.EqualTo(higher));
            proof.higherFallbackAfterNoCourse=(bool)CauseCall(match,"BeginRigYield",higher,lower);Assert.That(proof.higherFallbackAfterNoCourse,Is.True,"Only the freshly proven blocked lower rig permits its partner's actually clear course.");
            proof.onlyOnePairYield=!(bool)CauseCall(match,"BeginRigYield",lower,higher)&&((int[])CauseRead(match,"trafficYieldPhase"))[higher]!=0&&((int[])CauseRead(match,"trafficYieldPhase"))[lower]==0;Assert.That(proof.onlyOnePairYield,Is.True);
            UnityEngine.Object.DestroyImmediate(rearBarrier);rearBarrier=null;UnityEngine.Object.DestroyImmediate(frontBarrier);frontBarrier=null;CauseCall(match,"ClearRigYield",lower);CauseCall(match,"ClearRigYield",higher);
            Move(2,new Vector3(520,105.12f,780));Move(3,new Vector3(529,105.12f,780));CauseCall(match,"ClearRigYield",2);((int[])CauseRead(match,"trafficYieldPhase"))[2]=3;((int[])CauseRead(match,"trafficYieldTo"))[2]=3;((float[])CauseRead(match,"trafficYieldUntil"))[2]=(float)CauseRead(match,"Clock")+20;var route=CauseCall(match,"NavigationRoute",2);CauseCall(route,"Reset");float stalled=(float)CauseRead(route,"SecondsWithoutProgress");
            for(int n=0;n<5;n++)Assert.That(CauseCall(match,"CanFollowRoute",2,.02f),Is.EqualTo(false));proof.heldRouteProgressUnchanged=(float)CauseRead(route,"SecondsWithoutProgress")==stalled&&(int)CauseRead(route,"RecoveryStarts")==0;Assert.That(proof.heldRouteProgressUnchanged,Is.True);
            var players=(Array)CauseRead(setup,"players");var player=players.GetValue(2);var source=player.GetType().GetField("source");var oldSource=source.GetValue(player);source.SetValue(player,Enum.Parse(source.FieldType,"Keyboard"));
            try{Assert.That(CauseCall(match,"CanFollowRoute",2,.02f),Is.EqualTo(true));var command=new Vector2(.4f,.35f);proof.humanCommandExact=(Vector2)CauseCall(match,"FilterDriveCommand",2,command,.02f)==command&&((int[])CauseRead(match,"trafficYieldPhase"))[2]==0;Assert.That(proof.humanCommandExact,Is.True);}finally{source.SetValue(player,oldSource);}
            var rules=(HoardHeistRules)CauseRead(match,"Rules");proof.identitiesAndRulesUnchanged=proof.originalTruckIds.SequenceEqual(bodies.Select(v=>v.GetInstanceID()))&&proof.originalCargoIds.SequenceEqual(items.Select(v=>((Rigidbody)CauseRead(v,"body")).GetInstanceID()))&&rules.States.All(v=>v==HoardCargoState.Loose)&&rules.Scores.All(v=>v==0)&&rules.DeliveredCount==0&&(int)CauseRead(match,"PhysicalLoads")==0;Assert.That(proof.identitiesAndRulesUnchanged,Is.True);proof.status="CONTROLLED_SAVED_RIG_REGRESSION_PASSED_REQUIRES_ORDINARY_FULL_MATCH";
        }
        finally
        {
            try{if(trucks!=null&&positions!=null)for(int i=0;i<trucks.Length;i++){CauseCall(trucks[i],"Prepare",positions[i],rotations[i],true);var body=(Rigidbody)CauseRead(trucks[i],"body");body.linearVelocity=velocities[i];body.angularVelocity=angular[i];}if(match!=null)CauseCall(match,"SetupScreen");if(unsupported!=null)UnityEngine.Object.DestroyImmediate(unsupported);if(rearBarrier!=null)UnityEngine.Object.DestroyImmediate(rearBarrier);if(frontBarrier!=null)UnityEngine.Object.DestroyImmediate(frontBarrier);if(isolation!=null)UnityEngine.Object.DestroyImmediate(isolation);prefs?.Dispose();Runtime("LWS.TruckSports.SportsParticipantRegistry").GetMethod("ForgetCache").Invoke(null,null);}catch(Exception e){proof.cleanupError=e.ToString();}
            File.WriteAllText(output,JsonUtility.ToJson(proof,true));Assert.That(proof.cleanupError,Is.Null);
        }
    }

#endif

    [UnityTearDown]
    public IEnumerator Cleanup()
    { if(scene.IsValid()&&scene.isLoaded)yield return SceneManager.UnloadSceneAsync(scene); }
}

// Event-time control for the isolated restraint fixture; never a shipping adapter.
public sealed class HoardCargoOwnContactWitness : MonoBehaviour
{
    public Rigidbody truck;public int ownContacts;
    void OnCollisionEnter(Collision collision){if(collision.rigidbody==truck)ownContacts++;}
    void OnCollisionStay(Collision collision){if(collision.rigidbody==truck)ownContacts++;}
}

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using TMPro;

namespace LWS.TruckSports.Editor
{
    public static class SportsRaceFinishAuthoring
    {
        const string Folder="Assets/TruckSports/FinishPresentation";
        public static readonly string[] Scenes={"TruckMattressRacing","TruckWifeCarrying","TruckBogSnorkelling","TruckTerrierRacing","TruckAvalancheEscape","TruckRedLightGreenLight","TruckMinefield","TruckFrogger"};
        [MenuItem("Truck Sports/Carnival/Install eight explicit racing finish presentations")]
        public static void Install()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Finish authoring must run outside Play Mode.");for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Save open scenes before the explicit finish migration.");
            Directory.CreateDirectory(Folder);AssetDatabase.Refresh();EnsureConfetti();
            foreach(string name in Scenes){EditorSceneManager.OpenScene("Assets/"+name+"/Scenes/"+name+".unity");InstallOpenScene();}
            AssetDatabase.SaveAssets();Debug.Log("RACE_FINISH_PRESENTATIONS_AUTHORED eight typed native authorities; Wife checkers, particle-only CoreCollapse, Balloon paper, UTS audience, RoadProps jugs in isolated local PhysX; original cameras/HUD/gameplay unchanged");
        }
        static void EnsureConfetti()
        {
            string path=Folder+"/Existing Balloon paper confetti.prefab";if(AssetDatabase.LoadAssetAtPath<GameObject>(path)!=null)return;
            var scene=EditorSceneManager.OpenPreviewScene("Assets/TruckBalloonBattle/Scenes/TruckBalloonBattle.unity");
            try
            {
                var match=scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<LWS.TruckBalloonBattle.BalloonMatch>(true)).Single();PrefabUtility.SaveAsPrefabAsset(match.confetti[0].gameObject,path);
            }
            finally{EditorSceneManager.ClosePreviewScene(scene);}
        }
        public static SportsRaceFinishPresentation InstallOpenScene()
        {
            var scene=SceneManager.GetActiveScene();string name=scene.name;if(!Scenes.Contains(name)||scene.path!="Assets/"+name+"/Scenes/"+name+".unity")throw new InvalidOperationException("Open one of the eight reviewed finish-line scenes.");
            var session=UnityEngine.Object.FindAnyObjectByType<SportsEventSession>();if(session==null||session.gameObject.scene!=scene||!SportsRaceFinishSource.Supports(session.GetType()))throw new InvalidOperationException("Original sport authority is required.");
            var existing=session.GetComponent<SportsRaceFinishPresentation>();if(existing!=null)return existing;
            var original=session.menus.GetComponentsInChildren<UnityEngine.UI.Graphic>(true);var preserved=original.Select(graphic=>EditorJsonUtility.ToJson(graphic)).ToArray();
            var source=session.gameObject.AddComponent<SportsRaceFinishSource>();source.session=session;var presentation=session.gameObject.AddComponent<SportsRaceFinishPresentation>();presentation.source=source;
            var visual=new GameObject("Read-only racing finish presentation").transform;visual.SetParent(session.transform,false);
            int gates=session is LWS.TruckFrogger.FrogMatch frog?frog.world.courses.Length:1;source.gates=new SportsRaceFinishGate[gates];
            for(int gate=0;gate<gates;gate++)source.gates[gate]=Gate(source,visual,gate,name);
            var material=SportsChaosAuthoring.Flare("TruckSports/FinishPresentation");presentation.fireworks=new ParticleSystem[4];for(int i=0;i<4;i++)presentation.fireworks[i]=Firework(visual,material,i);
            var paper=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/Existing Balloon paper confetti.prefab");if(paper==null)throw new InvalidOperationException("Run the explicit Install entry to capture the existing Balloon paper foundation first.");
            presentation.confetti=new ParticleSystem[2];for(int i=0;i<2;i++){var copy=(GameObject)PrefabUtility.InstantiatePrefab(paper,visual);presentation.confetti[i]=copy.GetComponent<ParticleSystem>();foreach(var effect in copy.GetComponentsInChildren<ParticleSystem>(true)){effect.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);var main=effect.main;main.playOnAwake=false;main.useUnscaledTime=true;var collision=effect.collision;collision.enabled=false;var trigger=effect.trigger;trigger.enabled=false;}}
            presentation.victoryLights=new Light[2];for(int i=0;i<2;i++){var light=new GameObject("Pooled local finish celebration light "+i).AddComponent<Light>();light.transform.SetParent(visual,false);light.type=LightType.Point;light.range=38;light.intensity=0;light.shadows=LightShadows.None;presentation.victoryLights[i]=light;}
            var crowd=new GameObject("Owned finish crowd response").AddComponent<AudioSource>();crowd.transform.SetParent(visual,false);crowd.playOnAwake=false;crowd.spatialBlend=.65f;crowd.minDistance=20;crowd.maxDistance=350;presentation.crowd=crowd;presentation.cheer=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/TruckDockDiving/Audio/Exceptional cheer.wav");
            presentation.jugs=Jugs(source,visual,material);
            for(int i=0;i<original.Length;i++)if(EditorJsonUtility.ToJson(original[i])!=preserved[i])throw new InvalidOperationException("Protected original HUD graphic changed: "+original[i].name);
            if(presentation.cheer==null)throw new InvalidOperationException("Existing owned crowd cheer is required.");
            EditorUtility.SetDirty(source);EditorUtility.SetDirty(presentation);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);return presentation;
        }
        static SportsRaceFinishGate Gate(SportsRaceFinishSource source,Transform parent,int index,string name)
        {
            source.GatePose(index,out var point,out var rotation,out var width);var root=new GameObject("Finish presentation anchor "+index).transform;root.SetParent(parent,false);root.SetPositionAndRotation(point,rotation);var gate=new SportsRaceFinishGate{root=root,width=width};
            var dark=AssetDatabase.LoadAssetAtPath<Material>("Assets/TruckWifeCarrying/Materials/Finish charcoal.mat");var ivory=AssetDatabase.LoadAssetAtPath<Material>("Assets/TruckWifeCarrying/Materials/Finish warm ivory.mat");
            if(dark==null||ivory==null)throw new InvalidOperationException("The existing Wife checkered materials are required.");
            if(!(source.session is LWS.TruckWifeCarrying.WifeMatch))
            {
                var banner=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/TruckWifeCarrying/Models/Country checkered finish banner.asset");if(banner==null)throw new InvalidOperationException("Existing Wife local checkered banner mesh is required.");
                var arch=new GameObject("Reused Wife checkered banner",typeof(MeshFilter),typeof(MeshRenderer));arch.transform.SetParent(root,false);arch.transform.localScale=new Vector3(width/48,1,1);arch.GetComponent<MeshFilter>().sharedMesh=banner;arch.GetComponent<MeshRenderer>().sharedMaterials=new[]{dark,ivory};
                var timber=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ConstructionPropsBundle/URP/Prefabs/SM_Planks01 Variant.prefab");if(timber==null)throw new InvalidOperationException("Installed timber finish-post model is required.");
                for(int side=-1;side<=1;side+=2){var post=(GameObject)PrefabUtility.InstantiatePrefab(timber,root);post.name="Render-only installed finish timber";StripGameplay(post);Fit(post.transform,new Vector3(.5f,6.8f,.5f));post.transform.localPosition=new Vector3(side*(width*.5f+.6f),3.4f,0);}
                var strip=new GameObject("Wife-pattern terrain-fitted finish checks",typeof(MeshFilter),typeof(MeshRenderer));strip.transform.SetParent(root,false);gate.checks=strip.GetComponent<MeshFilter>();gate.checks.sharedMesh=Checks(name+"-"+index,width);strip.GetComponent<MeshRenderer>().sharedMaterials=new[]{dark,ivory};
            }
            var board=new GameObject("Depth-tested official finish announcement",typeof(RectTransform)).AddComponent<TextMeshPro>();board.transform.SetParent(root,false);board.transform.localPosition=new Vector3(0,8.8f,0);board.font=Resources.Load<SportsBroadcastTheme>("SportsBroadcastTheme").headingFont;board.fontSize=28;board.enableAutoSizing=true;board.fontSizeMin=12;board.fontSizeMax=28;board.alignment=TextAlignmentOptions.Center;board.color=SportsBroadcastCanvas.Cream;board.rectTransform.sizeDelta=new Vector2(Mathf.Min(width,48),5);board.isOverlay=false;board.text="";var billboard=board.gameObject.AddComponent<SportsPerCameraBillboard>();billboard.maximumPixelHeight=90;billboard.maximumPixelWidth=900;billboard.maximumLabelDistance=500;gate.announcement=board.transform;gate.caption=board;return gate;
        }
        static Mesh Checks(string name,float width)
        {
            string path=Folder+"/"+name+" Wife-pattern finish checks.asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(saved!=null)return saved;
            var vertices=new List<Vector3>();var triangles=new[]{new List<int>(),new List<int>()};const int columns=32,rows=3;
            for(int row=0;row<rows;row++)for(int col=0;col<columns;col++){float x=-width*.5f+col*width/columns,z=-1.5f+row;int at=vertices.Count;vertices.AddRange(new[]{new Vector3(x,.045f,z),new Vector3(x+width/columns,.045f,z),new Vector3(x+width/columns,.045f,z+1),new Vector3(x,.045f,z+1)});triangles[(row+col)%2].AddRange(new[]{at,at+2,at+1,at,at+3,at+2});}
            var mesh=new Mesh{name="Adapted Wife finish checks "+name};mesh.SetVertices(vertices);mesh.subMeshCount=2;for(int i=0;i<2;i++)mesh.SetTriangles(triangles[i],i);mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);return mesh;
        }
        static ParticleSystem Firework(Transform parent,Material material,int index)
        {
            var template=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/CoreCollapse/Demo/Prefabs/Particles/Explosion.prefab");if(template==null)throw new InvalidOperationException("Installed CoreCollapse particle foundation is required.");
            var copy=(GameObject)PrefabUtility.InstantiatePrefab(template,parent);copy.name="CoreCollapse particle-only finish firework "+index;StripGameplay(copy);foreach(var light in copy.GetComponentsInChildren<Light>(true))light.enabled=false;
            foreach(var effect in copy.GetComponentsInChildren<ParticleSystem>(true)){effect.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);var main=effect.main;main.loop=false;main.playOnAwake=false;main.useUnscaledTime=true;main.simulationSpace=ParticleSystemSimulationSpace.World;main.startLifetime=new ParticleSystem.MinMaxCurve(1.1f,2.3f);main.startSpeed=new ParticleSystem.MinMaxCurve(10,26);main.startSize=new ParticleSystem.MinMaxCurve(.15f,.6f);main.maxParticles=96;var shape=effect.shape;shape.enabled=true;shape.shapeType=ParticleSystemShapeType.Sphere;shape.radius=.15f;var colors=effect.colorOverLifetime;colors.enabled=false;var emission=effect.emission;emission.rateOverTime=0;emission.SetBursts(new[]{new ParticleSystem.Burst(0,48)});var collision=effect.collision;collision.enabled=false;var trigger=effect.trigger;trigger.enabled=false;var lights=effect.lights;lights.enabled=false;var renderer=effect.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;}
            // Unity's missing-component wrapper can compare null through its
            // overloaded operator while remaining non-null to C# ?? semantics.
            // The installed Explosion root has scripts and three child effects,
            // so add only its particle coordinator when no real root PS exists.
            var root=copy.GetComponent<ParticleSystem>();if(root==null)root=copy.AddComponent<ParticleSystem>();if(root==null)throw new InvalidOperationException("The installed particle-only finish instance needs a valid root ParticleSystem.");root.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);var config=root.main;config.playOnAwake=false;config.useUnscaledTime=true;config.loop=false;var rootEmission=root.emission;rootEmission.rateOverTime=0;rootEmission.SetBursts(Array.Empty<ParticleSystem.Burst>());return root;
        }
        static RaceFinishJugPresentation Jugs(SportsRaceFinishSource source,Transform visual,Material flare)
        {
            var owner=source.gameObject.AddComponent<RaceFinishJugPresentation>();owner.source=source;var pool=new GameObject("Authored local-PhysX finish props only").transform;pool.SetParent(visual,false);owner.poolRoot=pool;owner.slots=new RaceFinishJugPresentation.Slot[3];
            var canister=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/RoadPropsPack/URP/Prefabs/Canister_URP.prefab");var amber=AssetDatabase.LoadAssetAtPath<Material>("Assets/TruckFareFight/Materials/Reusable Taxi jug translucent amber.mat");var character=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TruckPedestrianPlowing/Prefabs/glam-fire-fighter Parade.prefab");var controller=AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/TruckFieldGoal/Crowd/Stadium reactions.controller");
            if(canister==null||amber==null||character==null||controller==null)throw new InvalidOperationException("Existing modeled jug/amber and Taxi/UTS stadium actor/controller are required.");
            string contactPath=Folder+"/Existing Taxi jug bounce.asset";var contact=AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(contactPath);if(contact==null){contact=new PhysicsMaterial("Existing Taxi jug bounce"){bounciness=.32f,bounceCombine=PhysicsMaterialCombine.Maximum,dynamicFriction=.3f,staticFriction=.3f};AssetDatabase.CreateAsset(contact,contactPath);}
            for(int i=0;i<3;i++)
            {
                var slot=new RaceFinishJugPresentation.Slot();owner.slots[i]=slot;var jug=new GameObject("Pooled modeled finish jug "+i);jug.transform.SetParent(pool,false);slot.jug=jug.AddComponent<Rigidbody>();slot.jug.mass=.35f;slot.jug.isKinematic=true;slot.jug.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;slot.jug.angularDamping=.05f;slot.jugCollider=jug.AddComponent<CapsuleCollider>();((CapsuleCollider)slot.jugCollider).radius=.18f;((CapsuleCollider)slot.jugCollider).height=.8f;slot.jugCollider.sharedMaterial=contact;
                var model=(GameObject)PrefabUtility.InstantiatePrefab(canister,jug.transform);StripGameplay(model);Fit(model.transform,new Vector3(.45f,.8f,.45f));foreach(var renderer in model.GetComponentsInChildren<Renderer>(true))renderer.sharedMaterial=amber;
                var hit=jug.AddComponent<RaceFinishJugContact>();hit.owner=owner;hit.slot=i;jug.SetActive(false);
                var hull=new GameObject("Finished hull collision proxy "+i);hull.transform.SetParent(pool,false);slot.proxy=hull.AddComponent<Rigidbody>();slot.proxy.isKinematic=true;slot.proxy.useGravity=false;slot.hull=hull.AddComponent<BoxCollider>();hull.SetActive(false);
                var floor=new GameObject("Isolated prop receiving floor "+i);floor.transform.SetParent(pool,false);var floorBody=floor.AddComponent<Rigidbody>();floorBody.isKinematic=true;floorBody.useGravity=false;slot.floor=floor.AddComponent<BoxCollider>();slot.floor.sharedMaterial=contact;floor.SetActive(false);
                slot.ghost=new GameObject("Recorded finished appearance "+i).transform;slot.ghost.SetParent(pool,false);slot.ghost.gameObject.SetActive(false);
                slot.actor=SportsAudienceAuthoring.Spawn(character,visual,Vector3.zero,Vector3.forward,controller,1);slot.actor.updateMode=AnimatorUpdateMode.UnscaledTime;slot.actorRoot=slot.actor.transform;while(slot.actorRoot.parent!=visual)slot.actorRoot=slot.actorRoot.parent;foreach(var voice in slot.actorRoot.GetComponentsInChildren<AudioSource>(true))UnityEngine.Object.DestroyImmediate(voice);slot.actorRoot.gameObject.SetActive(false);
                slot.splash=new GameObject("Pooled amber comedy splat "+i).AddComponent<ParticleSystem>();slot.splash.transform.SetParent(visual,false);SportsChaosAuthoring.FixBlast(slot.splash,flare);var main=slot.splash.main;main.useUnscaledTime=true;main.startColor=new Color(.95f,.76f,.12f,.8f);main.startSize=new ParticleSystem.MinMaxCurve(.08f,.24f);main.startSpeed=new ParticleSystem.MinMaxCurve(1,4);main.maxParticles=24;
                slot.impact=slot.splash.gameObject.AddComponent<AudioSource>();slot.impact.playOnAwake=false;slot.impact.spatialBlend=.8f;slot.impact.maxDistance=100;slot.impact.clip=source.session.feedback.clips[1];
            }
            EditorUtility.SetDirty(owner);return owner;
        }
        static void Fit(Transform root,Vector3 size){var renderers=root.GetComponentsInChildren<Renderer>(true);var bounds=new Bounds(root.position,Vector3.zero);foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);root.localScale=Vector3.Scale(root.localScale,new Vector3(size.x/Mathf.Max(.01f,bounds.size.x),size.y/Mathf.Max(.01f,bounds.size.y),size.z/Mathf.Max(.01f,bounds.size.z)));bounds=new Bounds(root.position,Vector3.zero);foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);root.position-=bounds.center-root.position;}
        static void StripGameplay(GameObject root){foreach(var script in root.GetComponentsInChildren<MonoBehaviour>(true))UnityEngine.Object.DestroyImmediate(script);foreach(var collider in root.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(collider);foreach(var joint in root.GetComponentsInChildren<Joint>(true))UnityEngine.Object.DestroyImmediate(joint);foreach(var body in root.GetComponentsInChildren<Rigidbody>(true))UnityEngine.Object.DestroyImmediate(body);foreach(var audio in root.GetComponentsInChildren<AudioSource>(true))UnityEngine.Object.DestroyImmediate(audio);}
    }
}

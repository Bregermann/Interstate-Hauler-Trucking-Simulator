using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using LWS.TruckSports;
using LWS.TruckSports.Editor;
namespace LWS.TruckFashionShow.Editor
{
    public static class FashionAuthoring
    {
        public const string Folder="TruckFashionShow",Scene="Assets/TruckFashionShow/Scenes/TruckFashionShow.unity";
        const string Human="Assets/LWS/TruckTaxi/Passengers/Animations/Humanoid/";
        [MenuItem("Truck Sports/Alpha 0.7/Create NEW Fashion Show")]
        public static void CreateNewScene()
        {
            if(File.Exists(Scene))throw new InvalidOperationException("New Fashion author refuses overwrite: "+Scene);
            if(AssetDatabase.LoadAssetAtPath<GameObject>(SportsTruckCustomizerAuthoring.PrefabPath)==null)throw new InvalidOperationException("Existing authored Heat free customizer required; never regenerate global content.");
            var match=SportsEventAuthoring.Begin<FashionMatch>(Folder);match.setup.count=4;match.option=0;match.NormalizeSetup();match.feedback.clips=Audio();Install(match);SportsEventAuthoring.UI(match);
            for(int i=0;i<4;i++){SportsUIAuthoring.Button(match.menus.hud.transform,"READY / "+(i+1),"fashion.ready."+i,new Vector2(-630+i*420,-425),new Vector2(390,48));}
            FashionGalleryAuthoring.InstallNewOrExactOriginal(match);
            SportsEventAuthoring.Save(Folder);
            Debug.Log("FASHION_NEW_SCENE: lead binds fixed4 existing appearance hosts after firstSave. Shared independent runway camera deliberately opts out of tractor framing; indoor artificial light must stay controlled. Existing free customizer/profile DB and Dance input/rules own those responsibilities.");
        }
        public static void Install(FashionMatch match)
        {
            if(EditorApplication.isPlaying||EditorApplication.isCompiling||match==null||(!string.IsNullOrEmpty(match.gameObject.scene.path)&&match.gameObject.scene.path!=Scene)||match.transform.Find("Totally Ridiculous Runway fashion hub")!=null)throw new InvalidOperationException("Stopped new unauthored Fashion scene required.");
            match.catalog=AssetDatabase.LoadAssetAtPath<SportsTruckAppearanceCatalog>("Assets/TruckSports/Resources/SportsTruckAppearanceCatalog.asset");if(match.catalog==null||!match.catalog.Validate(out string reason))throw new InvalidOperationException("Actual validated owned catalog required.");
            var root=new GameObject("Totally Ridiculous Runway fashion hub").transform;root.SetParent(match.transform,false);
            Material cream=SportsEventAuthoring.Mat(Folder,"Warm boutique plaster",new Color(.84f,.8f,.71f),.25f),rose=SportsEventAuthoring.Mat(Folder,"Runway raspberry velvet",new Color(.42f,.035f,.15f),.12f),dark=SportsEventAuthoring.Mat(Folder,"Glossy charcoal terrazzo",new Color(.04f,.055f,.07f),.65f),gold=SportsEventAuthoring.Mat(Folder,"Warm brushed brass",new Color(.8f,.57f,.18f),.75f),mint=SportsEventAuthoring.Mat(Folder,"Boutique mint accent",new Color(.1f,.59f,.53f),.45f);gold.SetFloat("_Metallic",.8f);
            var physical=SportsEventAuthoring.Contact(Folder,"Fashion safe stage contact",.6f,0);
            SportsEventAuthoring.Box("Indoor fashion hub actual floor",new Vector3(0,-.2f,-3),new Vector3(124,.4f,112),dark,physical,root);
            foreach(float x in new[]{-61f,61f})SportsEventAuthoring.Box("Gallery perimeter physical wall",new Vector3(x,6,-3),new Vector3(1,12,112),cream,physical,root);
            foreach(float z in new[]{-58f,52f})SportsEventAuthoring.Box("Gallery back wall",new Vector3(0,6,z),new Vector3(122,12,1),cream,physical,root);
            // Raised color only: no truck/collider rescale, platform teleport or hidden scoring trigger.
            SportsEventAuthoring.Box("Long actual runway carpet",new Vector3(0,.025f,15),new Vector3(14,.05f,58),rose,null,root);
            foreach(float x in new[]{-7.4f,7.4f}){var trim=SportsEventAuthoring.Box("Continuous brass runway edge",new Vector3(x,.08f,15),new Vector3(.15f,.1f,58),gold,null,root);for(int n=0;n<15;n+=5)Light(root,new Vector3(x,.35f,-12+n*4),new Color(1,.65f,.8f),.6f,2);}
            match.entrance=Marker(root,"Actual runway entrance",new Vector3(0,.7f,-10));match.finalMark=Marker(root,"Actual final pose mark",new Vector3(0,.7f,40));match.dressing=Marker(root,"Actual dressing room entrance",new Vector3(-36,.7f,38));
            match.shops=new Transform[4];Vector3[] shopPositions={new Vector3(-38,.7f,-22),new Vector3(-38,.7f,12),new Vector3(38,.7f,-22),new Vector3(38,.7f,12)};string[] names={"HAT BOUTIQUE","FACES / EXPRESSIONS","WHEEL ATELIER","MIRRORS / HITCH ACCESSORIES"};string[][] slots={new[]{"hat"},new[]{"face.eyes","face.brows","face.mouth","face.mustache","face.lashes"},new[]{"wheels"},new[]{"mirrors","rear.ornament","dashboard"}};
            for(int i=0;i<4;i++)
            {
                Vector3 c=shopPositions[i];float side=c.x<0?-1:1;match.shops[i]=Marker(root,names[i]+" actual browse gate",c);SportsEventAuthoring.Box(names[i]+" accent floor",new Vector3(side*47,.02f,c.z),new Vector3(22,.04f,27),i%2==0?mint:rose,null,root);
                Label(root,names[i]+" / FREE",new Vector3(side*47,8,c.z+11),.6f,Color.white);
                Prop(root,"Assets/Hivemind/ShopStore/URP/Art/Prefabs/SM_Counter_1.prefab",new Vector3(side*47,0,c.z+6),new Vector3(8,2.5f,3));
                Prop(root,"Assets/Hivemind/ShopStore/URP/Art/Prefabs/SM_Register.prefab",new Vector3(side*48,2.5f,c.z+6),new Vector3(1.2f,1.1f,1.3f));
                Prop(root,"Assets/Hivemind/ShopStore/URP/Art/Prefabs/SM_Shelf_Unit_Reg.prefab",new Vector3(side*56,0,c.z),new Vector3(3,6,18));
                var items=match.catalog.cosmetics.Where(item=>item!=null&&item.visualPrefab!=null&&slots[i].Contains(item.slotId)).ToArray();if(items.Length==0)throw new InvalidOperationException("Every Fashion store needs real equippable catalog items.");
                for(int n=0;n<items.Length;n++)
                {
                    Vector3 at=new Vector3(side*(43+n%2*9),1.2f,c.z-8+n/2*5);SportsEventAuthoring.Box("Actual catalog display plinth / "+items[n].id,at-Vector3.up*.65f,new Vector3(2.5f,1.1f,2.5f),gold,null,root);var model=(GameObject)PrefabUtility.InstantiatePrefab(items[n].visualPrefab);model.name="Real equippable display / "+items[n].id;model.transform.SetParent(root,false);Fit(model,at,new Vector3(2.2f,2,2.2f));Label(root,items[n].label+" / FREE",at+Vector3.down*1.1f,.17f,Color.white);
                }
                Light(root,new Vector3(side*46,9,c.z),new Color(1,.86f,.68f),7,20);
            }
            SportsEventAuthoring.Box("Dressing room velvet back",new Vector3(-48,5,47),new Vector3(24,10,.2f),rose,null,root);Label(root,"DRESSING ROOM / SAVE YOUR LOOK",new Vector3(-46,8,39),.4f,Color.white);
            Prop(root,"Assets/Modern Resaturant - building and interior/Models/Prefabs/Restaurant_chair.prefab",new Vector3(-50,0,42),new Vector3(2,3,2));
            for(int n=0;n<3;n++){var mirror=SportsEventAuthoring.Box("Dressing room polished mirror "+n,new Vector3(-54+n*7,4,46.5f),new Vector3(5,7,.1f),gold,null,root);}
            Label(root,"TOTALLY RIDICULOUS RUNWAY",new Vector3(0,10,48),.8f,Color.white);Label(root,"FREE FASHION / REAL TRUCK PERSONALITY",new Vector3(0,7,48),.4f,new Color(1,.73f,.3f));
            // Installed upholstered chairs and real human rigs establish scale; stage architecture is mode-owned.
            for(int side=-1;side<=1;side+=2)for(int row=0;row<2;row++)for(int n=0;n<8;n++)Prop(root,"Assets/Modern Resaturant - building and interior/Models/Prefabs/Restaurant_chair.prefab",new Vector3(side*(15+row*5),0,-5+n*5),new Vector3(1.2f,1.8f,1.2f));
            Prop(root,"Assets/Modern Resaturant - building and interior/Models/Prefabs/Restaurant_Table_Rectangular_1.prefab",new Vector3(18,0,37),new Vector3(13,2.4f,4));
            match.trucks=new SportsEventTruck[4];match.walkers=new FashionStrut[4];for(int i=0;i<4;i++){match.trucks[i]=Rig(match,i);match.walkers[i]=match.trucks[i].gameObject.AddComponent<FashionStrut>();match.walkers[i].truck=match.trucks[i];}
            match.people=People(match,root);for(int z=-30;z<=30;z+=60)for(int x=-30;x<=30;x+=30)Light(root,new Vector3(x,11,z),new Color(1,.83f,.69f),5,30);
            foreach(var sun in UnityEngine.Object.FindObjectsByType<Light>())if(sun.type==LightType.Directional)sun.intensity=.2f;RenderSettings.ambientLight=new Color(.22f,.19f,.24f);RenderSettings.skybox=null;
            match.broadcast=SportsEventAuthoring.Camera("Fashion original shared Cinemachine runway camera");match.cameras=new[]{match.broadcast.view};match.broadcast.view.backgroundColor=new Color(.025f,.02f,.035f);match.broadcast.view.clearFlags=CameraClearFlags.SolidColor;match.broadcast.Pose(new Vector3(0,0,-7),new Vector3(0,65,-73),60,true);
            match.music=match.gameObject.AddComponent<AudioSource>();match.music.clip=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/TruckDanceOff/Audio/Music/Diesel Disco.wav");if(match.music.clip==null)throw new InvalidOperationException("Existing original Dance-Off disco track required.");match.music.loop=true;match.music.playOnAwake=false;match.music.spatialBlend=0;
        }
        static SportsEventTruck Rig(FashionMatch match,int owner)
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(LWS.TruckFareFight.Editor.FareFightVehicleAuthoring.PrefabPath);if(prefab==null)throw new InvalidOperationException("Existing full original tractor required.");var g=(GameObject)PrefabUtility.InstantiatePrefab(prefab);g.name="Fashion original full tractor "+owner;g.transform.SetPositionAndRotation(match.Queue(owner),Quaternion.identity);var t=g.GetComponent<SportsEventTruck>();if(t==null||t.tractorMotor==null)throw new InvalidOperationException("Original full NWH facade required.");t.session=match;t.slot=owner;t.tractorMotor.settings=match.settings;EditorUtility.SetDirty(t);EditorUtility.SetDirty(t.tractorMotor);return t;
        }
        static FashionPeople People(FashionMatch match,Transform root)
        {
            string path="Assets/TruckFashionShow/Materials/Actual fashion humans.controller";var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);if(controller==null)controller=AnimatorController.CreateAnimatorControllerAtPath(path);var machine=controller.layers[0].stateMachine;
            foreach(var pair in new[]{("Idle","idle1"),("Walk","walk"),("Cheer","cheer"),("Shout","shout")})
            {
                var clip=AssetDatabase.LoadAllAssetsAtPath(Human+pair.Item2+".FBX").OfType<AnimationClip>().FirstOrDefault(c=>c.humanMotion&&!c.name.StartsWith("__"));if(clip==null)throw new InvalidOperationException("Actual owned Human clip required: "+pair.Item2);var state=machine.states.FirstOrDefault(v=>v.state.name==pair.Item1).state;if(state==null)state=machine.AddState(pair.Item1);if(state.motion==null)state.motion=clip;if(pair.Item1=="Idle")machine.defaultState=state;EditorUtility.SetDirty(state);
            }
            EditorUtility.SetDirty(machine);EditorUtility.SetDirty(controller);AssetDatabase.SaveAssets();var p=match.gameObject.AddComponent<FashionPeople>();p.match=match;p.judges=Enumerable.Range(0,3).Select(i=>HumanActor(root,controller,new Vector3(13+i*5,0,40),i)).ToArray();p.shoppers=Enumerable.Range(0,20).Select(i=>HumanActor(root,controller,new Vector3((i%2==0?-1:1)*(14+i%3*5),0,-5+i/2*4),i)).ToArray();p.shopkeepers=Enumerable.Range(0,4).Select(i=>HumanActor(root,controller,new Vector3(match.shops[i].position.x<0?-47:47,0,match.shops[i].position.z+9),i)).ToArray();return p;
        }
        static Animator HumanActor(Transform root,AnimatorController controller,Vector3 p,int i)
        {
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(Human+(i%2==0?"business01_m":"business01_f")+".FBX");if(source==null)throw new InvalidOperationException("Owned actual human model required.");var model=UnityEngine.Object.Instantiate(source,root);model.name="Actual animated fashion Human "+i;var a=model.GetComponent<Animator>();if(a==null||a.avatar==null||!a.avatar.isHuman)throw new InvalidOperationException("Real Human avatar required.");a.runtimeAnimatorController=controller;a.applyRootMotion=false;a.cullingMode=AnimatorCullingMode.AlwaysAnimate;Fit(model,p+Vector3.up*1.1f,new Vector3(1.2f,2.2f,1.2f));return a;
        }
        static Transform Marker(Transform root,string name,Vector3 p){var g=new GameObject(name).transform;g.SetParent(root,false);g.position=p;return g;}
        static void Label(Transform root,string text,Vector3 p,float size,Color color){var t=SportsEventAuthoring.Label(text,text,p,size,color);t.transform.SetParent(root,true);}
        static void Light(Transform root,Vector3 p,Color color,float intensity,float range){var l=new GameObject("Indoor boutique practical light").AddComponent<Light>();l.transform.SetParent(root,false);l.transform.position=p;l.type=LightType.Point;l.color=color;l.intensity=intensity;l.range=range;l.shadows=LightShadows.None;}
        static void Prop(Transform root,string path,Vector3 p,Vector3 size)
        {
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(source==null)throw new InvalidOperationException("Owned actual venue asset required: "+path);var g=(GameObject)PrefabUtility.InstantiatePrefab(source);g.transform.SetParent(root,false);foreach(var c in g.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(c);Fit(g,p+Vector3.up*size.y*.5f,size);
        }
        static void Fit(GameObject g,Vector3 center,Vector3 maximum)
        {
            var r=g.GetComponentsInChildren<Renderer>(true);if(r.Length==0)throw new InvalidOperationException("Actual visible model required: "+g.name);var b=r[0].bounds;foreach(var v in r)b.Encapsulate(v.bounds);float s=Mathf.Min(maximum.x/Mathf.Max(.01f,b.size.x),maximum.y/Mathf.Max(.01f,b.size.y),maximum.z/Mathf.Max(.01f,b.size.z));g.transform.localScale*=s;var next=r[0].bounds;foreach(var v in r)next.Encapsulate(v.bounds);g.transform.position+=center-next.center;
        }
        static AudioClip[] Audio(){string[] paths={"Assets/Heat - Complete Modern UI/Audio/Click.wav","Assets/TruckBilliards/Audio/Ball.wav","Assets/NWH/Vehicle Physics 2/Sound/Horn/Horn.wav","Assets/TruckBilliards/Audio/Strike.wav","Assets/TruckHockey/Audio/Faceoff.wav","Assets/TruckHockey/Audio/GoalHorn.wav","Assets/TruckHockey/Audio/FinalWhistle.wav"};return paths.Select(path=>{var clip=AssetDatabase.LoadAssetAtPath<AudioClip>(path);if(clip==null)throw new InvalidOperationException("Actual audio required: "+path);return clip;}).ToArray();}
    }
}

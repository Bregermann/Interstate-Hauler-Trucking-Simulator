using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using LWS.TruckSports;
using LWS.TruckSports.Editor;
using LWS.TruckMechanicalBull.Editor;
using LWS.TruckFieldGoal;
namespace LWS.TruckBallet.Editor
{
    public static class BalletAuthoring
    {
        public const string Folder="TruckBallet",Scene="Assets/TruckBallet/Scenes/TruckBallet.unity";
        static Material Velvet=>ImpossibleBalanceArt.Material(Folder,"Grand theater burgundy velvet",new Color(.22f,.025f,.045f),.28f);
        static Material Gold=>ImpossibleBalanceArt.Material(Folder,"Grand theater warm gilt",new Color(.62f,.37f,.11f),.62f,.7f);
        static Material Wood=>ImpossibleBalanceArt.Material(Folder,"Grand theater warm maple stage",new Color(.42f,.25f,.1f),.38f);
        static Material Dark=>ImpossibleBalanceArt.Material(Folder,"Grand theater midnight blue",new Color(.015f,.025f,.06f),.2f);
        static Mesh Block=>ImpossibleBalanceArt.Mesh(Folder,"Grand theater bound rounded joinery",ImpossibleBalanceArt.RoundedBlock);
        static GameObject Part(Transform parent,string name,Vector3 p,Vector3 scale,Material material,Quaternion rotation=default)=>ImpossibleBalanceArt.Part(parent,name,Block,material,p,scale,rotation);
        [MenuItem("Truck Sports/Impossible Games/Create NEW Truck Ballet Grand Theater")]
        public static void Create()
        {
            ImpossibleBalanceArt.GuardNew(Scene);var m=SportsEventAuthoring.Begin<BalletMatch>(Folder);m.setup.count=4;m.option=0;m.NormalizeSetup();
            var theater=m.gameObject.AddComponent<BalletTheater>();theater.match=m;m.theater=theater;
            Stage(m);m.rigs=new BalletTruckRig[4];var required=new List<Transform>();
            for(int i=0;i<4;i++)
            {
                var truck=ImpossibleBalanceArt.Tractor(m,i,new Vector3((i-1.5f)*17,0,0));float lowest=truck.wheels.Min(w=>truck.transform.InverseTransformPoint(w.transform.position).y-w.wheel.radius-w.spring.maxLength);
                var rig=truck.gameObject.AddComponent<BalletTruckRig>();rig.truck=truck;rig.slot=i;rig.home=new Vector3((i-1.5f)*17,1.32f-lowest,0);truck.transform.position=rig.home;rig.attachedBodies=truck.GetComponentsInChildren<Rigidbody>(true).Where(b=>b!=truck.body).ToArray();m.rigs[i]=rig;var costume=Costume(rig);required.Add(costume.transform);
            }
            AudienceAndOrchestra(m);m.orchestra=m.gameObject.AddComponent<AudioSource>();m.orchestra.playOnAwake=false;m.orchestra.loop=true;m.orchestra.spatialBlend=0;m.orchestra.clip=BalletAudioAuthoring.Create();m.orchestra.priority=25;
            m.broadcast=SportsEventAuthoring.Camera("Grand Diesel theater graceful broadcast");m.broadcast.Pose(m.rigs[0].truck.body.worldCenterOfMass,new Vector3(0,15,-30),55,true);m.cameras=new[]{m.broadcast.view};
            m.feedback.clips=(AudioClip[])LWS.TruckHockey.Editor.HockeyAudioAuthoring.Create().Clone();ImpossibleBalanceArt.UI(m);HUD(m);
            var light=UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None).First(l=>l.type==LightType.Directional);light.intensity=.4f;light.color=new Color(.68f,.73f,.9f);RenderSettings.ambientLight=new Color(.16f,.14f,.2f);
            SportsEventAuthoring.Save(Folder);SportsTruckAppearanceHostAuthoring.BindNewImpossibleScene(m,m.rigs.Select(r=>r.truck).ToArray(),required.ToArray());UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(m.gameObject.scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(m.gameObject.scene);AssetDatabase.SaveAssets();
        }
        static void Stage(BalletMatch match)
        {
            var root=new GameObject("Finished Grand Diesel theater shell and stage").transform;var floor=Part(root,"Maple proscenium stage",new Vector3(0,.3f,1),new Vector3(80,1.8f,28),Wood);floor.AddComponent<BoxCollider>();
            for(int board=0;board<40;board++)Part(root,"Inlaid maple stage plank seam",new Vector3((board-19.5f)*1.95f,1.215f,1),new Vector3(.025f,.014f,27.8f),Gold);
            Part(root,"Theater velvet back wall",new Vector3(0,14,19),new Vector3(87,28,.6f),Dark);Part(root,"Grand theater ceiling",new Vector3(0,30,-12),new Vector3(92,.8f,65),Dark);
            for(int side=-1;side<=1;side+=2)
            {
                var wall=Part(root,"Tall theater side wall",new Vector3(side*45,12,-13),new Vector3(1,27,66),Velvet);wall.AddComponent<BoxCollider>();
                for(int z=0;z<5;z++){Part(root,"Gilt fluted proscenium column",new Vector3(side*41,11,16-z*12),new Vector3(1.2f,23,1.3f),Gold);for(int flute=0;flute<5;flute++)Part(root,"Column carved recessed flute",new Vector3(side*40.3f,11,15.5f-z*12+flute*.22f),new Vector3(.08f,21,.08f),Dark);}
                for(int tier=0;tier<2;tier++){Part(root,"Ornate side balcony fascia",new Vector3(side*40,6+tier*8,-20),new Vector3(7,1,49),Gold);Part(root,"Velvet balcony parapet",new Vector3(side*36.5f,7+tier*8,-20),new Vector3(.5f,2,49),Velvet);}
            }
            Velvet.SetFloat("_Cull",0);var curtainMesh=ImpossibleBalanceArt.Mesh(Folder,"Dense stitched theater curtain folds",()=>Curtain(72,18));match.theater.curtains=new Transform[2];match.theater.curtainWings=new Transform[4];
            for(int side=0;side<2;side++)
            {
                var curtain=ImpossibleBalanceArt.Part(root,"Opening grand velvet curtain "+side,curtainMesh,Velvet,new Vector3(side==0?-18:18,1.2f,-11.5f),new Vector3(36,24,1));match.theater.curtains[side]=curtain.transform;
                for(int wing=0;wing<2;wing++){var w=ImpossibleBalanceArt.Part(root,"Layered stitched velvet stage wing",curtainMesh,Velvet,new Vector3(side==0?-39:39,1.2f,-5+wing*11),new Vector3(5,24,1));match.theater.curtainWings[side*2+wing]=w.transform;}
            }
            Part(root,"Gilt proscenium crown",new Vector3(0,26,-12),new Vector3(86,2.4f,3),Gold);var swag=ImpossibleBalanceArt.Part(root,"Scalloped velvet proscenium valance",curtainMesh,Velvet,new Vector3(0,21.5f,-13),new Vector3(80,5,1));
            var sign=SportsEventAuthoring.Label("Grand theater stage title","THE GRAND DIESEL GALA",new Vector3(0,23.2f,18.5f),.3f,new Color(1,.83f,.47f));sign.transform.SetParent(root,true);
            match.theater.followSpots=new Light[4];for(int i=0;i<4;i++){var spot=new GameObject("Graceful original owner follow spotlight "+i).AddComponent<Light>();spot.transform.SetParent(root,false);spot.transform.localPosition=new Vector3((i-1.5f)*17,22,-14);spot.type=LightType.Spot;spot.spotAngle=32;spot.innerSpotAngle=21;spot.range=55;spot.intensity=8;spot.color=new Color(1,.86f,.68f);spot.shadows=i==0?LightShadows.Soft:LightShadows.None;match.theater.followSpots[i]=spot;}
            var audienceFloor=Part(root,"Raised theater audience floor",new Vector3(0,-.25f,-45),new Vector3(83,.5f,54),Dark);audienceFloor.AddComponent<BoxCollider>();
            var pit=Part(root,"Recessed orchestra pit velvet fascia",new Vector3(0,.5f,-18),new Vector3(75,1,1),Velvet);Part(root,"Actual orchestra pit floor",new Vector3(0,-.3f,-17),new Vector3(75,.5f,9),Wood);
        }
        public static Mesh Curtain(int columns,int rows)
        {
            var vertices=new Vector3[(columns+1)*(rows+1)];var uv=new Vector2[vertices.Length];var triangles=new int[columns*rows*6];
            for(int y=0;y<=rows;y++)for(int x=0;x<=columns;x++){float u=x/(float)columns,v=y/(float)rows;int k=y*(columns+1)+x;vertices[k]=new Vector3(u-.5f,v,Mathf.Sin(u*Mathf.PI*24)*.25f+Mathf.Sin(v*Mathf.PI)*.03f);uv[k]=new Vector2(u*8,v*4);}
            for(int y=0;y<rows;y++)for(int x=0;x<columns;x++){int a=y*(columns+1)+x,b=a+columns+1,k=(y*columns+x)*6;int[] t={a,b,a+1,a+1,b,b+1};Array.Copy(t,0,triangles,k,6);}
            var mesh=new Mesh();mesh.vertices=vertices;mesh.uv=uv;mesh.triangles=triangles;return mesh;
        }
        static BalletCostume Costume(BalletTruckRig rig)
        {
            var root=new GameObject("Owned giant ballet tutu and matching bow").transform;root.SetParent(rig.truck.transform,false);root.localPosition=rig.truck.shape.center+Vector3.up*rig.truck.shape.size.y*.12f;
            var costume=root.gameObject.AddComponent<BalletCostume>();costume.rig=rig;costume.stageFloor=1.2f;rig.costume=costume;float x=rig.truck.shape.size.x,z=rig.truck.shape.size.z;
            var material=ImpossibleBalanceArt.Material(Folder,"Soft woven participant ballet tulle",new Color(.8f,.57f,.68f),.15f);material.SetFloat("_Cull",0);var skirt=ImpossibleBalanceArt.Mesh(Folder,"Original pleated elliptical giant tutu",()=>Tutu(x*.5f,z*.32f,x*1.4f,z*.59f));var art=ImpossibleBalanceArt.Part(root,"Layered pleated giant tulle ruffles",skirt,material,Vector3.zero,Vector3.one);costume.ruffles=art.transform;costume.cloth=art.GetComponent<MeshFilter>();costume.tintable=new[]{art.GetComponent<Renderer>()};
            var belt=ImpossibleBalanceArt.Mesh(Folder,"Rounded elliptical tutu waist ribbon",()=>ImpossibleBalanceArt.Lathe(new[]{new Vector2(.48f,-.1f),new Vector2(.52f,-.1f),new Vector2(.52f,.1f),new Vector2(.48f,.1f),new Vector2(.48f,-.1f)},64));for(int ring=0;ring<2;ring++)ImpossibleBalanceArt.Part(root,"Gilt stitched hollow waist ribbon",belt,Gold,new Vector3(0,ring*.12f,0),new Vector3(x,.45f,z*.64f));
            var bow=ImpossibleBalanceArt.Mesh(Folder,"Rounded original ballet bow loops",ImpossibleBalanceArt.Ovoid);for(int side=-1;side<=1;side+=2)ImpossibleBalanceArt.Part(root,"Fitted rear tutu bow loop",bow,material,new Vector3(side*.7f,.15f,-z*.34f),new Vector3(1.4f,.7f,.4f),Quaternion.Euler(0,0,side*20));return costume;
        }
        public static Mesh Tutu(float innerX,float innerZ,float outerX,float outerZ)
        {
            const int segments=96,rings=12;var v=new Vector3[(segments+1)*(rings+1)];var uv=new Vector2[v.Length];var indices=new int[segments*rings*6];
            for(int ring=0;ring<=rings;ring++)for(int s=0;s<=segments;s++){float t=ring/(float)rings,a=s*Mathf.PI*2/segments;int k=ring*(segments+1)+s;float pleat=Mathf.Sin(a*24)*.11f*t;v[k]=new Vector3(Mathf.Sin(a)*(Mathf.Lerp(innerX,outerX,t)+pleat),-.68f*t+Mathf.Sin(a*24)*.09f*t,Mathf.Cos(a)*(Mathf.Lerp(innerZ,outerZ,t)+pleat));uv[k]=new Vector2(s/12f,t*3);}
            for(int ring=0;ring<rings;ring++)for(int s=0;s<segments;s++){int a=ring*(segments+1)+s,b=a+segments+1,k=(ring*segments+s)*6;int[] t={a,b,a+1,a+1,b,b+1};Array.Copy(t,0,indices,k,6);}var mesh=new Mesh();mesh.vertices=v;mesh.uv=uv;mesh.triangles=indices;return mesh;
        }
        static RuntimeAnimatorController Controller()
        {
            System.IO.Directory.CreateDirectory("Assets/TruckBallet/Animation");AssetDatabase.Refresh();string path="Assets/TruckBallet/Animation/Theater seated audience.controller";var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);if(controller!=null){if(controller.layers.Length==0)throw new InvalidOperationException("Existing custom/incomplete theater controller retained.");return controller;}
            controller=AnimatorController.CreateAnimatorControllerAtPath(path);foreach(var pair in new[]{("Idle","sitidle_f"),("Cheer","claphands_f"),("Disappointed","sitidle_f")}){var clip=AssetDatabase.LoadAllAssetsAtPath("Assets/UTS_FullPack/Models/People/Animations/Girl no Heel/"+pair.Item2+".FBX").OfType<AnimationClip>().FirstOrDefault(c=>!c.name.StartsWith("__"));if(clip==null)throw new InvalidOperationException("Actual compatible UTS audience clip missing: "+pair.Item2);var state=controller.AddMotion(clip);state.name=pair.Item1;if(pair.Item1=="Idle")controller.layers[0].stateMachine.defaultState=state;EditorUtility.SetDirty(state);}EditorUtility.SetDirty(controller.layers[0].stateMachine);EditorUtility.SetDirty(controller);AssetDatabase.SaveAssets();return controller;
        }
        static void AudienceAndOrchestra(BalletMatch match)
        {
            var root=new GameObject("Real animated theater audience and orchestra").transform;var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UTS_FullPack/Models/People/RagDoll Prefabs/Girl_44.prefab");if(prefab==null)throw new InvalidOperationException("Actual fully clothed UTS adult rig required.");var controller=Controller();var actors=new List<Animator>();
            for(int row=0;row<6;row++)for(int seat=0;seat<12;seat++)
            {
                var p=new Vector3((seat-5.5f)*5.2f,.2f+row*.48f,-29-row*5);Part(root,"Burgundy upholstered theater seat",p+new Vector3(0,.35f,0),new Vector3(1.6f,.55f,1.6f),Velvet);Part(root,"Gilt backed theater seat",p+new Vector3(0,1.25f,-.65f),new Vector3(1.65f,1.5f,.24f),Gold);
                var actor=SportsAudienceAuthoring.Spawn(prefab,root,p,new Vector3(0,p.y,0),controller,1.1f);actor.runtimeAnimatorController=controller;actor.cullingMode=AnimatorCullingMode.CullCompletely;actors.Add(actor);
            }
            var crowd=match.gameObject.AddComponent<FieldGoalCrowd>();crowd.eventSession=match;crowd.batches=Array.Empty<FieldGoalCrowd.Batch>();crowd.nearFans=actors.ToArray();crowd.runnerChance=0;crowd.authoredVersion=6;crowd.response=match.gameObject.AddComponent<AudioSource>();crowd.response.playOnAwake=false;crowd.response.spatialBlend=0;crowd.cheer=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/TruckDockDiving/Audio/Exceptional cheer.wav");match.audience=crowd;
            for(int i=0;i<8;i++)Musician(match,root,prefab,i);
        }
        static void Musician(BalletMatch match,Transform parent,GameObject prefab,int index)
        {
            var p=new Vector3((index-3.5f)*8,-.05f,-18);var actor=SportsAudienceAuthoring.Spawn(prefab,parent,p,p+Vector3.forward,null,1.2f);actor.runtimeAnimatorController=null;actor.enabled=false;var rig=actor.gameObject.AddComponent<BalletOrchestraPlayer>();rig.match=match;rig.actor=actor;rig.cello=index%3==0;rig.phase=index*.4f;
            var all=actor.GetComponentsInChildren<Transform>(true);Transform Bone(string n)=>all.Single(t=>t.name==n);rig.leftUpper=Bone("Bip01 L UpperArm");rig.rightUpper=Bone("Bip01 R UpperArm");rig.leftForearm=Bone("Bip01 L Forearm");rig.rightForearm=Bone("Bip01 R Forearm");rig.head=Bone("Bip01 Head");var leftHand=Bone("Bip01 L Hand");var rightHand=Bone("Bip01 R Hand");
            var instrument=new GameObject(rig.cello?"Original cello with strings and bridge":"Original violin with strings and bridge").transform;instrument.SetParent(leftHand,false);instrument.localPosition=new Vector3(0,.02f,.08f);instrument.localRotation=Quaternion.Euler(0,0,rig.cello?0:70);instrument.localScale=Vector3.one*(rig.cello?1.2f:.55f);
            var mesh=ImpossibleBalanceArt.Mesh(Folder,"Original waisted bowed-string instrument",Instrument);ImpossibleBalanceArt.Part(instrument,"Carved curved instrument body",mesh,Wood,Vector3.zero,Vector3.one);Part(instrument,"Fingerboard neck",new Vector3(0,.9f,0),new Vector3(.12f,1.1f,.1f),Dark);Part(instrument,"Curved instrument bridge",new Vector3(0,-.1f,-.11f),new Vector3(.38f,.14f,.12f),Gold);
            for(int s=0;s<4;s++){Part(instrument,"Actual narrow instrument string",new Vector3((s-1.5f)*.032f,.37f,-.13f),new Vector3(.008f,1.7f,.008f),Gold);Part(instrument,"Instrument tuning peg",new Vector3(s%2==0?-.13f:.13f,1.2f+s*.06f,0),new Vector3(.2f,.04f,.04f),Dark);}
            var bow=Part(rightHand,"Real moving wooden musician bow",new Vector3(0,.45f,0),new Vector3(.035f,1.25f,.035f),Wood);Part(rightHand,"Bowed horsehair ribbon",new Vector3(.035f,.45f,0),new Vector3(.012f,1.2f,.012f),Gold);
            var stand=Part(parent,"Gilt orchestra music stand",p+new Vector3(0,.7f,1.2f),new Vector3(.06f,1.4f,.06f),Gold);Part(parent,"Ivory visible music folio",p+new Vector3(0,1.4f,1.2f),new Vector3(.9f,.6f,.08f),ImpossibleBalanceArt.Material(Folder,"Ivory orchestral music paper",new Color(.85f,.82f,.68f)),Quaternion.Euler(20,0,0));
        }
        public static Mesh Instrument()
        {
            const int sides=48;var vertices=new Vector3[sides*2+2];var uv=new Vector2[vertices.Length];var triangles=new List<int>();
            for(int face=0;face<2;face++)for(int i=0;i<sides;i++){float a=i*Mathf.PI*2/sides;float width=.38f*(1-.4f*Mathf.Cos(a*2));int k=face*sides+i;vertices[k]=new Vector3(Mathf.Sin(a)*width,Mathf.Cos(a)*.63f,face==0?-.08f:.08f);uv[k]=new Vector2(vertices[k].x+.5f,vertices[k].y+.65f);}
            vertices[sides*2]=Vector3.back*.08f;vertices[sides*2+1]=Vector3.forward*.08f;
            for(int i=0;i<sides;i++){int n=(i+1)%sides;triangles.AddRange(new[]{sides*2,i,n,sides*2+1,sides+n,sides+i,i,sides+i,n,n,sides+i,sides+n});}var mesh=new Mesh();mesh.vertices=vertices;mesh.uv=uv;mesh.triangles=triangles.ToArray();return mesh;
        }
        static void HUD(BalletMatch match)
        {
            match.menus.standings.gameObject.SetActive(false);var hud=match.menus.hud.AddComponent<BalletHud>();hud.match=match;hud.cue=SportsUIAuthoring.Text("Current actual choreography cue",match.menus.hud.transform,"",new Vector2(1740,40),new Vector2(0,445),23);hud.cards=new TMPro.TMP_Text[4];
            for(int i=0;i<4;i++){var panel=SportsUIAuthoring.Rect("Owned deliberate ballet pose card "+i,match.menus.hud.transform,new Vector2(438,148),new Vector2((i-1.5f)*456,-345));panel.AddComponent<UnityEngine.UI.Image>().color=new Color(.025f,.008f,.035f,.9f);hud.cards[i]=SportsUIAuthoring.Text("Actual target and pose",panel.transform,"",new Vector2(414,132),Vector2.zero,21);hud.cards[i].fontSizeMin=18;hud.cards[i].fontSizeMax=21;}
            var text=match.menus.pages[(int)SportsEventScreen.Help].transform.Find("Rules and controls").GetComponent<TMPro.TMP_Text>();text.fontSizeMin=20;text.fontSizeMax=22;
        }
    }
}

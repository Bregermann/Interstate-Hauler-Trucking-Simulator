using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using Michsky.UI.Heat;
using LWS.TruckCamping;
using LWS.TruckCamping.Editor;
using LWS.TruckSports;
using LWS.TruckSports.Editor;
using LWS.TruckMechanicalBull.Editor;
namespace LWS.TruckPetPals.Editor
{
    public static class PetPalsAuthoring
    {
        public const string Scene="Assets/TruckPetPals/Scenes/TruckPetPals.unity";
        [MenuItem("Truck Sports/Impossible Games/Create NEW Truck Pet Pals Using Shared Camp Forest")]
        public static void Create()
        {
            var match=CampingAuthoring.Begin<PetPalsMatch>("TruckPetPals",Scene);
            match.petProfiles=match.gameObject.AddComponent<PetProfileRepository>();match.petProfiles.workshop=match.persistence;
            var templates=Enum.GetValues(typeof(PetKind)).Cast<PetKind>().Select(PetVisualAuthoring.Template).ToArray();
            Digging(match);Toys(match);WashStations(match);match.companions=new PetCompanion[4];
            for(int i=0;i<4;i++)match.companions[i]=Companion(match,i,templates);
            Adoption(match,templates);HUD(match);CampingAuthoring.Finish(match,"TruckPetPals");
        }
        static PetCompanion Companion(PetPalsMatch match,int owner,GameObject[] templates)
        {
            var desired=match.explorers[owner].spawn+new Vector3(4,0,-5);desired.y=match.world.Height(desired)+.1f;
            if(!match.world.WalkingPoint(desired,out var point))throw new InvalidOperationException("Actual owner pet spawn must lie on the shared woodland NavMesh.");
            var root=new GameObject("Stable owner "+(owner+1)+" animated companion");root.transform.position=point;root.SetActive(false);
            var pet=root.AddComponent<PetCompanion>();pet.match=match;pet.owner=match.explorers[owner];pet.repository=match.petProfiles;pet.digging=match.digPatches;pet.toys=match.petToys;
            pet.physicalBody=root.AddComponent<Rigidbody>();pet.physicalBody.isKinematic=true;pet.physicalBody.useGravity=false;pet.physicalBody.mass=35;
            // Nonblocking semantic capsule: the NavMesh avoids real world walls.
            // A full-sized owner can never kick a kinematic pet into an impulse
            // or be launched by it. Dynamic fetch toys retain genuine joints.
            var contact=root.AddComponent<CapsuleCollider>();contact.radius=.55f;contact.height=1.5f;contact.center=Vector3.up*.75f;contact.isTrigger=true;
            pet.motor=root.AddComponent<NavMeshAgent>();pet.motor.radius=.6f;pet.motor.height=1.8f;pet.motor.speed=7.5f;pet.motor.acceleration=12;pet.motor.angularSpeed=160;pet.motor.obstacleAvoidanceType=ObstacleAvoidanceType.HighQualityObstacleAvoidance;pet.motor.avoidancePriority=35+owner*5;pet.motor.autoTraverseOffMeshLink=true;
            pet.ownerTether=CampingWildernessAuthoring.Point("Safe cosmetic owner leash anchor",match.explorers[owner].truck.transform,match.explorers[owner].truck.transform.TransformPoint(new Vector3(1.6f,2,-2)));
            pet.leash=root.AddComponent<LineRenderer>();pet.leash.sharedMaterial=CampingWildernessAuthoring.Cloth;pet.leash.widthMultiplier=.045f;pet.leash.positionCount=13;pet.leash.useWorldSpace=true;
            pet.visualTypes=new PetVisual[3];
            for(int type=0;type<3;type++)
            {
                var art=(GameObject)PrefabUtility.InstantiatePrefab(templates[type],root.transform);art.transform.localPosition=Vector3.zero;art.transform.localRotation=Quaternion.identity;
                pet.visualTypes[type]=new PetVisual{kind=(PetKind)type,root=art,living=art.GetComponent<CampLivingArt>(),miniature=art.GetComponent<PetTruckArt>(),collar=art.GetComponent<PetAccessory>().transform,peeAnchor=art.transform.Find("Actual non-graphic leg effect mount"),kickAnchor=art.transform.Find("Actual grass-kick effect mount"),dirtSurfaces=art.GetComponentsInChildren<Renderer>(true)};art.SetActive(false);
            }
            pet.peeStream=CampingWildernessAuthoring.Particles(root.transform,"Stylized non-graphic grass stream",new Color(.85f,.78f,.29f,.5f),.025f,35,.32f);var main=pet.peeStream.main;main.startSpeed=2;main.gravityModifier=1.1f;var shape=pet.peeStream.shape;shape.angle=2;shape.radius=.025f;pet.peeStream.transform.rotation=Quaternion.Euler(65,130,0);
            pet.grassKicks=CampingWildernessAuthoring.Particles(root.transform,"Actual backward grass scrape",new Color(.32f,.35f,.12f),.06f,18,.5f);main=pet.grassKicks.main;main.startSpeed=2.2f;main.gravityModifier=1.4f;pet.grassKicks.transform.rotation=Quaternion.Euler(130,0,0);
            pet.bathWater=CampingWildernessAuthoring.Particles(root.transform,"Pet wash clear water",new Color(.5f,.8f,.9f,.6f),.055f,50,.5f);pet.bathWater.transform.localPosition=new Vector3(0,2,0);pet.bathWater.transform.localRotation=Quaternion.Euler(90,0,0);
            pet.soap=CampingWildernessAuthoring.Particles(root.transform,"Pet wash soap bubbles",new Color(.8f,.92f,.97f,.6f),.12f,18,1.2f);pet.soap.transform.localPosition=Vector3.up*.5f;
            pet.splash=CampingWildernessAuthoring.Particles(root.transform,"Pet bath shake splash",new Color(.6f,.88f,.95f,.7f),.07f,0,.6f);var emission=pet.splash.emission;emission.SetBursts(new[]{new ParticleSystem.Burst(0,28)});
            return pet;
        }
        static void WashStations(PetPalsMatch match)
        {
            match.washStations=new Transform[4];var parent=new GameObject("Shared camp pet wash and rest clearing").transform;
            for(int i=0;i<4;i++)
            {
                var p=new Vector3((i-1.5f)*14,0,-8);p.y=match.world.Height(p);var root=CampingWildernessAuthoring.Point("Owner-accessible wash station "+(i+1),parent,p);
                var deck=CampingWildernessAuthoring.Part(root,"Grooved cedar wash deck",Vector3.zero,new Vector3(5,.1f,5),CampingWildernessAuthoring.Wood);CampingWildernessAuthoring.Collision(deck);
                for(int rail=0;rail<2;rail++)CampingWildernessAuthoring.Part(root,"Rounded wash rail",new Vector3(rail==0?-2.1f:2.1f,.45f,0),new Vector3(.18f,.9f,4.8f),CampingWildernessAuthoring.Cloth);
                CampingWildernessAuthoring.Part(root,"Clean towels on wash rail",new Vector3(-2.1f,.8f,.8f),new Vector3(.25f,.6f,.9f),CampingWildernessAuthoring.Stone);
                var arch=CampingWildernessAuthoring.Point("Curved overhead wash nozzle",root,p+new Vector3(1.8f,2.4f,-1.8f));CampingWildernessAuthoring.Part(arch,"Wash water hose riser",Vector3.down*1.1f,new Vector3(.12f,2.4f,.12f),CampingWildernessAuthoring.Stone);CampingWildernessAuthoring.Part(arch,"Wash nozzle arm",Vector3.left*.9f,new Vector3(1.9f,.12f,.12f),CampingWildernessAuthoring.Stone);
                match.washStations[i]=CampingWildernessAuthoring.Point("Actual companion wash target",root,p+Vector3.up*.15f);
                var bed=CampingWildernessAuthoring.Part(root,"Stitched soft truck-sized pet cushion",new Vector3(0,.18f,-6.5f),new Vector3(4.8f,.35f,4),CampingWildernessAuthoring.Cloth);
                for(int seam=0;seam<4;seam++)CampingWildernessAuthoring.Part(root,"Visible cushion quilting",new Vector3((seam-1.5f),.37f,-6.5f),new Vector3(.025f,.025f,3.7f),CampingWildernessAuthoring.Stone);
            }
        }
        static void Digging(PetPalsMatch match)
        {
            match.digGrounds=match.world.terrains.Select(t=>{var g=t.gameObject.AddComponent<PetDigGround>();g.terrain=t;g.physicalTerrain=t.GetComponent<TerrainCollider>();return g;}).ToArray();match.digPatches=new PetDigPatch[6];
            var root=new GameObject("Designated soft-soil dig patches · no original terrain edits").transform;
            for(int i=0;i<6;i++)
            {
                var desired=new Vector3(24+(i%3)*11,0,-6+i/3*16);var ground=match.digGrounds.Single(g=>desired.x>=g.terrain.transform.position.x&&desired.z>=g.terrain.transform.position.z&&desired.x<g.terrain.transform.position.x+1000&&desired.z<g.terrain.transform.position.z+1000);
                float cell=ground.terrain.terrainData.size.x/ground.terrain.terrainData.holesResolution;desired.x=ground.terrain.transform.position.x+(Mathf.Floor((desired.x-ground.terrain.transform.position.x)/cell)+.5f)*cell;desired.z=ground.terrain.transform.position.z+(Mathf.Floor((desired.z-ground.terrain.transform.position.z)/cell)+.5f)*cell;desired.y=match.world.Height(desired);
                var patchRoot=CampingWildernessAuthoring.Point("Actual soft-soil patch "+i,root,desired);var patch=patchRoot.gameObject.AddComponent<PetDigPatch>();patch.id="pet.dig."+i;patch.ground=ground;
                var soil=new GameObject("Runtime visible excavated bowl");soil.transform.SetParent(patchRoot,false);patch.soil=soil.AddComponent<MeshFilter>();soil.AddComponent<MeshRenderer>().sharedMaterial=ImpossibleBalanceArt.Material("TruckPetPals","Freshly dug soft woodland soil",new Color(.23f,.14f,.065f));patch.collision=soil.AddComponent<MeshCollider>();soil.SetActive(false);
                patch.dirt=CampingWildernessAuthoring.Particles(patchRoot,"Actual excavated earth motes",new Color(.3f,.2f,.08f),.06f,18,.6f);var main=patch.dirt.main;main.gravityModifier=1;
                patch.discovery=CampingWildernessAuthoring.Part(patchRoot,"Unearthed little cedar toy treasure",new Vector3(0,-.2f,0),new Vector3(.5f,.25f,.85f),CampingWildernessAuthoring.Wood);patch.discovery.SetActive(false);
                for(int marker=0;marker<8;marker++){float a=marker*Mathf.PI/4;CampingWildernessAuthoring.Part(patchRoot,"Soft-soil boundary river stone",new Vector3(Mathf.Sin(a)*3.9f,.08f,Mathf.Cos(a)*3.9f),new Vector3(.3f,.16f,.3f),CampingWildernessAuthoring.Stone);}
                match.digPatches[i]=patch;
            }
        }
        static void Toys(PetPalsMatch match)
        {
            var root=new GameObject("Physical companion toys").transform;match.petToys=new PetToy[8];
            var rubber=ImpossibleBalanceArt.Material("TruckPetPals","Toy sunset soft rubber",new Color(.91f,.45f,.12f),.35f);var teal=CampingWildernessAuthoring.Cloth;
            for(int i=0;i<8;i++)
            {
                var p=new Vector3((i%4-1.5f)*14,0,-17-i/4*7);p.y=match.world.Height(p)+.5f;var actor=CampingWildernessAuthoring.Point("Physical "+(i%3==0?"squeaky ball":i%3==1?"flying disc":"novelty little trailer"),root,p);
                var toy=actor.gameObject.AddComponent<PetToy>();toy.id="pet.toy."+i;toy.body=actor.gameObject.AddComponent<Rigidbody>();toy.body.mass=i%3==2?3:.6f;toy.body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;toy.body.interpolation=RigidbodyInterpolation.Interpolate;
                if(i%3==0){ImpossibleBalanceArt.Part(actor,"Bound colorful squeaky ball",CampingWildernessAuthoring.Round,rubber,Vector3.zero,Vector3.one*.6f);actor.gameObject.AddComponent<SphereCollider>().radius=.3f;}
                else if(i%3==1){var mesh=ImpossibleBalanceArt.Mesh("TruckPetPals","Rounded hollow flying disc",()=>ImpossibleBalanceArt.Lathe(new[]{new Vector2(0,.08f),new Vector2(.32f,.09f),new Vector2(.48f,.04f),new Vector2(.5f,-.03f),new Vector2(.46f,-.08f),new Vector2(.35f,-.02f),new Vector2(0,-.02f)}));ImpossibleBalanceArt.Part(actor,"Finished rolled-rim flying disc",mesh,teal,Vector3.zero,Vector3.one);var box=actor.gameObject.AddComponent<BoxCollider>();box.size=new Vector3(1,.15f,1);}
                else{CampingWildernessAuthoring.Part(actor,"Toy trailer carved cedar body",Vector3.up*.1f,new Vector3(.65f,.55f,1.2f),teal);for(int side=-1;side<=1;side+=2)ImpossibleBalanceArt.Part(actor,"Toy trailer rubber wheel",CampingWildernessAuthoring.Round,rubber,new Vector3(side*.37f,-.1f,-.2f),new Vector3(.12f,.3f,.3f));var box=actor.gameObject.AddComponent<BoxCollider>();box.size=new Vector3(.85f,.6f,1.3f);}
                toy.home=p;toy.homeRotation=Quaternion.identity;match.petToys[i]=toy;
            }
        }
        static ButtonManager Direct(Transform parent,string title,Vector2 position,Vector2 size)
        {var button=SportsUIAuthoring.Button(parent,title,"pet-owned-modal",position,size);UnityEngine.Object.DestroyImmediate(button.GetComponent<SportsMenuAction>());return button;}
        static void Adoption(PetPalsMatch match,GameObject[] templates)
        {
            var controller=match.menus.gameObject.AddComponent<PetAdoptionPanel>();controller.match=match;controller.renderTemplates=templates;
            var panel=SportsUIAuthoring.Page("Choose your real animated companion",match.menus.transform,"A LITTLE FRIEND FOR THE BIG ADVENTURE","Adoption keeps your other companions, care and learned tricks. CANCEL discards this draft.");controller.panel=panel;
            var preview=SportsUIAuthoring.Rect("Isolated rotating actual pet preview",panel.transform,new Vector2(610,525),new Vector2(-330,-5));controller.preview=preview.AddComponent<RawImage>();controller.preview.color=Color.white;
            controller.title=SportsUIAuthoring.Text("Selected real pet type",panel.transform,"",new Vector2(630,58),new Vector2(335,220),34);
            controller.description=SportsUIAuthoring.Text("Actual pet identity and animation description",panel.transform,"",new Vector2(600,120),new Vector2(335,135),24);
            controller.petName=SportsUIAuthoring.Name(panel.transform,"COMPANION NAME","Pip",new Vector2(335,20));
            controller.bodyLabel=SportsUIAuthoring.Text("Selected truck body",panel.transform,"",new Vector2(590,45),new Vector2(335,-60),24);controller.accessoryLabel=SportsUIAuthoring.Text("Selected actual accessory",panel.transform,"",new Vector2(590,42),new Vector2(335,-110),24);
            controller.nextType=Direct(panel.transform,"PET TYPE",new Vector2(335,-175),new Vector2(190,48));controller.nextBody=Direct(panel.transform,"TRUCK BODY",new Vector2(130,-235),new Vector2(190,48));controller.nextAccessory=Direct(panel.transform,"ACCESSORY",new Vector2(450,-235),new Vector2(235,48));
            controller.message=SportsUIAuthoring.Text("Adoption data status",panel.transform,"",new Vector2(1240,60),new Vector2(0,-305),22);
            controller.cancel=Direct(panel.transform,"CANCEL",new Vector2(-320,-382),new Vector2(430,52));controller.apply=Direct(panel.transform,"ADOPT / KEEP",new Vector2(320,-382),new Vector2(430,52));panel.SetActive(false);
            for(int i=0;i<4;i++)
            {
                SportsUIAuthoring.Button(match.menus.pages[0].transform,"COMPANION "+(i+1),"pet-adopt"+i,new Vector2(375,178-i*106),new Vector2(570,48));
                SportsUIAuthoring.Button(match.menus.pages[3].transform,"COMPANION "+(i+1),"pet-adopt"+i,new Vector2(355,-55-i*57),new Vector2(570,46));
            }
        }
        static void HUD(PetPalsMatch match)
        {
            var hud=match.menus.hud.AddComponent<PetCareHUD>();hud.match=match;hud.cards=new PetCareCard[4];string[] labels={"FOOD","CLEAN","HAPPY","REST","LOVE"};Color[] colors={new Color(.85f,.61f,.26f),new Color(.24f,.75f,.9f),new Color(.95f,.75f,.2f),new Color(.51f,.65f,.92f),new Color(.92f,.4f,.56f)};
            for(int i=0;i<4;i++)
            {
                var panel=SportsUIAuthoring.Rect("Owned pet needs card "+i,match.menus.hud.transform,new Vector2(438,148),Vector2.zero);panel.AddComponent<Image>().color=new Color(.012f,.033f,.029f,.88f);
                var card=new PetCareCard{root=panel.GetComponent<RectTransform>(),fills=new Image[5]};card.identity=SportsUIAuthoring.Text("Stable pet name and bond",panel.transform,"",new Vector2(410,32),new Vector2(0,52),23);card.detail=SportsUIAuthoring.Text("Actual care or training cue",panel.transform,"",new Vector2(410,42),new Vector2(0,12),20);
                for(int n=0;n<5;n++){float x=(n-2)*79;SportsUIAuthoring.Text(labels[n],panel.transform,labels[n],new Vector2(72,22),new Vector2(x,-24),15);var back=SportsUIAuthoring.Rect(labels[n]+" needs track",panel.transform,new Vector2(66,9),new Vector2(x,-45));back.AddComponent<Image>().color=new Color(.08f,.12f,.12f);var fill=SportsUIAuthoring.Rect(labels[n]+" needs fill",back.transform,new Vector2(66,9),Vector2.zero).AddComponent<Image>();fill.color=colors[n];fill.type=Image.Type.Filled;fill.fillMethod=Image.FillMethod.Horizontal;fill.fillOrigin=0;card.fills[n]=fill;}
                hud.cards[i]=card;
            }
            hud.saveNotice=SportsUIAuthoring.Text("Unsupported pet save preservation warning",match.menus.hud.transform,"",new Vector2(1500,40),new Vector2(0,405),23);hud.saveNotice.color=new Color(1,.7f,.32f);
        }
    }
}

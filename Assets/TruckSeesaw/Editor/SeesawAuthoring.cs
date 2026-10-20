using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using LWS.TruckSports;
using LWS.TruckSports.Editor;
using LWS.TruckFareFight.Editor;
namespace LWS.TruckSeesaw.Editor
{
    public static class SeesawAuthoring
    {
        public const string Folder="TruckSeesaw",Scene="Assets/TruckSeesaw/Scenes/TruckSeesaw.unity";
        public static void RepairInitialLocalGeometry()
        {
            var scene=EditorSceneManager.OpenScene(Scene);var m=UnityEngine.Object.FindAnyObjectByType<SeesawMatch>();
            var deck=m.platform.transform.Find("Physical weight-responsive suspended wobble deck");
            if(deck.localPosition.y>40)deck.localPosition=Vector3.zero;
            foreach(Transform child in m.platform.transform)if(child.name.StartsWith("Painted")&&child.localPosition.y>40)child.localPosition-=m.platformOrigin;
            var label=m.menus.pages[4].transform.Find("Rules and controls");if(label!=null)label.GetComponent<TMP_Text>().text=m.Help;
            m.platform.ResetCenterOfMass();m.platform.ResetInertiaTensor();EditorUtility.SetDirty(m.platform);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        }
        public static void Create()
        {
            if(File.Exists(Scene))throw new InvalidOperationException("Preserve the authored quarry and pivot; apply targeted upgrades.");
            var m=SportsEventAuthoring.Begin<SeesawMatch>(Folder);m.setup.count=4;m.option=1;m.NormalizeSetup();m.feedback.clips=LWS.TruckHockey.Editor.HockeyAudioAuthoring.Create();
            var metal=SportsEventAuthoring.Mat(Folder,"Scuffed quarry platform steel",new Color(.26f,.30f,.32f),.38f);
            var original=AssetDatabase.LoadAssetAtPath<Material>("Assets/Kirkley Entertainment Inc/North American Speed Signs/Art/Materials/GalvanisedSteel.mat");if(original==null)throw new InvalidOperationException("Installed galvanised-steel material required.");metal.mainTexture=original.mainTexture;metal.mainTextureScale=Vector2.one*15;
            var yellow=SportsEventAuthoring.Mat(Folder,"Chipped construction yellow",new Color(.95f,.55f,.025f),.3f);var charcoal=SportsEventAuthoring.Mat(Folder,"Hazard stripe charcoal",new Color(.025f,.03f,.035f),.18f);
            var contact=SportsEventAuthoring.Contact(Folder,"NWH moving board contact",.85f,.02f);
            Quarry(m,yellow,charcoal);
            var platformRoot=new GameObject("Physical weight-responsive suspended wobble platform");platformRoot.transform.position=m.platformOrigin;
            var deck=SportsEventAuthoring.Box("Physical weight-responsive suspended wobble deck",Vector3.zero,new Vector3(82,1.8f,82),metal,contact,platformRoot.transform);
            m.platform=platformRoot.AddComponent<Rigidbody>();m.platform.mass=35000;m.platform.useGravity=true;m.platform.isKinematic=true;m.platform.interpolation=RigidbodyInterpolation.Interpolate;m.platform.collisionDetectionMode=CollisionDetectionMode.ContinuousSpeculative;m.platform.solverIterations=24;m.platform.solverVelocityIterations=12;m.platform.angularDamping=.15f;m.platform.maxAngularVelocity=1.2f;
            var marker=deck.AddComponent<LWS.TruckTaxi.TruckTaxiSurface>();marker.isRoad=false;
            m.pivot=platformRoot.AddComponent<ConfigurableJoint>();m.pivot.autoConfigureConnectedAnchor=false;m.pivot.anchor=Vector3.zero;m.pivot.connectedAnchor=m.platformOrigin;m.pivot.xMotion=m.pivot.yMotion=m.pivot.zMotion=ConfigurableJointMotion.Locked;
            m.pivot.angularXMotion=m.pivot.angularZMotion=ConfigurableJointMotion.Limited;m.pivot.angularYMotion=ConfigurableJointMotion.Locked;m.pivot.lowAngularXLimit=new SoftJointLimit{limit=-28};m.pivot.highAngularXLimit=new SoftJointLimit{limit=28};m.pivot.angularZLimit=new SoftJointLimit{limit=28};m.pivot.rotationDriveMode=RotationDriveMode.XYAndZ;m.pivot.targetRotation=Quaternion.identity;m.pivot.enablePreprocessing=true;
            for(int side=-1;side<=1;side+=2)for(int segment=0;segment<28;segment++)
            {
                float along=-39.5f+segment*2.9f;var material=segment%2==0?yellow:charcoal;
                var a=SportsEventAuthoring.Box("Painted edge distance and hazard marking",new Vector3(side*39.5f,.93f,along),new Vector3(2,.025f,2.85f),material,null,platformRoot.transform);UnityEngine.Object.DestroyImmediate(a.GetComponent<Collider>());
                var b=SportsEventAuthoring.Box("Painted transverse hazard marking",new Vector3(along,.93f,side*39.5f),new Vector3(2.85f,.025f,2),material,null,platformRoot.transform);UnityEngine.Object.DestroyImmediate(b.GetComponent<Collider>());
            }
            var socket=GameObject.CreatePrimitive(PrimitiveType.Sphere);socket.name="Visible central universal-joint socket";socket.transform.SetParent(platformRoot.transform,false);socket.transform.localPosition=Vector3.down*2;socket.transform.localScale=new Vector3(9,6,9);UnityEngine.Object.DestroyImmediate(socket.GetComponent<Collider>());socket.GetComponent<Renderer>().sharedMaterial=yellow;
            Suspension(m,yellow);
            m.trucks=new SportsEventTruck[4];m.fragments=new SportsFragmentRig[4];m.impacts=new ParticleSystem[4];float totalMass=0;
            for(int i=0;i<4;i++)
            {
                var truck=FareFightVehicleAuthoring.Spawn(m,i,m.Grid(i),Quaternion.LookRotation(Vector3.ProjectOnPlane(m.platformOrigin-m.Grid(i),Vector3.up)));m.trucks[i]=truck;totalMass+=truck.body.mass;
                foreach(var wheel in truck.wheels)wheel.otherBodyForceScale=1;
                var hit=truck.gameObject.AddComponent<SeesawContacts>();hit.match=m;hit.slot=i;m.fragments[i]=SportsFragmentAuthoring.InstallRigid(truck.body,truck.visuals.GetComponentsInChildren<MeshFilter>(true),Folder);
                var effect=new GameObject("Pooled actual quarry impact fireball "+i).AddComponent<ParticleSystem>();effect.transform.SetParent(m.transform);SportsChaosAuthoring.FixBlast(effect,SportsChaosAuthoring.Flare(Folder));var main=effect.main;main.startLifetime=new ParticleSystem.MinMaxCurve(.6f,1.3f);main.startSize=new ParticleSystem.MinMaxCurve(5,12);main.startSpeed=new ParticleSystem.MinMaxCurve(10,22);main.simulationSpace=ParticleSystemSimulationSpace.World;m.impacts[i]=effect;
            }
            m.platform.mass=totalMass*1.4f;float spring=totalMass*9.81f*18*.65f;var drive=new JointDrive{positionSpring=spring,positionDamper=Mathf.Sqrt(spring*m.platform.mass*82*82/12)*.8f,maximumForce=float.MaxValue};m.pivot.angularXDrive=m.pivot.angularYZDrive=drive;
            m.broadcast=SportsEventAuthoring.Camera("Quarry full-board and falling truck broadcast");m.broadcast.view.orthographic=true;m.broadcast.view.farClipPlane=2200;m.framing=m.broadcast.gameObject.AddComponent<SportsCameraFit>();m.framing.cameraTarget=m.broadcast.view;m.framing.arenaFit=true;m.framing.topInset=.1f;m.framing.bottomInset=.22f;m.cameras=new[]{m.broadcast.view};
            SportsEventAuthoring.UI(m);SportsRallyAuthoring.CompactUI(m);m.drivingViews=SportsDrivingViewsAuthoring.Install(m,m.trucks,m.broadcast,Folder);m.drivingViews.independentViewModes=true;
            m.atmosphere=SportsChaosAuthoring.Weather(m,Folder);m.atmosphere.presets=SportsWeatherPresetsAuthoring.EnsureCatalog();m.drivingViews.rally.weather=m.atmosphere;SportsWeatherPickerAuthoring.Install(m,m.atmosphere.presets,new Vector2(350,-180));
            var hud=m.gameObject.AddComponent<SportsExpansionViewHud>();hud.match=m;hud.views=m.drivingViews;var spectator=m.gameObject.AddComponent<SportsExpansionSpectators>();spectator.match=m;spectator.views=m.drivingViews;
            SportsUIAuthoring.Button(m.menus.pages[6].transform,"WATCH THE WOBBLE","watchseesaw",new Vector2(0,-245),new Vector2(520,48));Artwork(m);SportsBroadcastAuthoring.ApplyScene();SportsEventAuthoring.Save(Folder);SeesawPolishAuthoring.Upgrade();SeesawPolishAuthoring.OpenGantry();SeesawPolishAuthoring.SupportBalconies();
            Debug.Log("SEESAW_AUTHORED physical two-axis ConfigurableJoint and NWH wheel reaction forces; no scripted tilt. tractor mass total="+totalMass+" platform mass="+m.platform.mass+" spring="+spring);
        }
        static void Quarry(SeesawMatch m,Material yellow,Material dark)
        {
            var data=AssetDatabase.LoadAssetAtPath<TerrainData>("Assets/TruckTrailerPlug/Models/Industrial Quarry Terrain.asset");if(data==null)throw new InvalidOperationException("Existing finished quarry terrain required.");
            var terrain=Terrain.CreateTerrainGameObject(data);terrain.name="Reused complete rocky quarry basin and cliffs";terrain.transform.position=new Vector3(-300,-30,-300);
            var stone=AssetDatabase.LoadAssetAtPath<Material>("Assets/TruckTrailerPlug/Materials/Rocky quarry floor.mat");var contact=SportsEventAuthoring.Contact(Folder,"Quarry impact stone",.7f,.05f);
            var floor=new GameObject("Existing modeled deep quarry impact basin");floor.AddComponent<MeshFilter>().sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/TruckTrailerPlug/Models/Open Quarry Floor.asset");floor.AddComponent<MeshRenderer>().sharedMaterial=stone;var collider=floor.AddComponent<MeshCollider>();collider.sharedMesh=floor.GetComponent<MeshFilter>().sharedMesh;collider.sharedMaterial=contact;
            SportsEventAuthoring.Box("Visible lowered quarry floor at central depression",new Vector3(0,-2,0),new Vector3(25,1,25),stone,contact);
            var root=new GameObject("Owned industrial quarry scenery").transform;
            for(int i=0;i<32;i++){float a=i*Mathf.PI*2/32;var p=new Vector3(Mathf.Sin(a)*87,0,Mathf.Cos(a)*87);SportsRallyAuthoring.Prop(root,"Assets/PinwheelStudio/Vista/Personal/Samples/Prefabs/VistaSample_DesertRock.prefab",p,new Vector3(8,8+i%4*3,12));}
            for(int i=0;i<6;i++)
            {
                float a=(i+1)*Mathf.PI*2/7;var p=new Vector3(Mathf.Sin(a)*74,0,Mathf.Cos(a)*74);SportsRallyAuthoring.Prop(root,"Assets/JC_LP_MegaCity/Prefabs/Buildings/SM_Buildings_Warehouse_01.prefab",p,new Vector3(20,15,22));SportsRallyAuthoring.Prop(root,"Assets/ShipContainerPack/URP/Prefabs/ShipContainerRed_URP.prefab",p*.8f,new Vector3(11,3,3));
            }
            var craneMesh=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/TruckBungeeDemolition/Models/Lattice Crane.asset");if(craneMesh==null)throw new InvalidOperationException("Existing detailed lattice crane required.");
            for(int i=0;i<4;i++){var crane=new GameObject("Existing detailed quarry lattice crane "+i);crane.transform.SetParent(root);crane.AddComponent<MeshFilter>().sharedMesh=craneMesh;crane.AddComponent<MeshRenderer>().sharedMaterial=yellow;float scale=105/craneMesh.bounds.size.y;crane.transform.localScale=Vector3.one*scale;crane.transform.position=new Vector3(i%2==0?-83:83,-craneMesh.bounds.min.y*scale,i<2?-70:70);crane.transform.rotation=Quaternion.Euler(0,i*90,0);}
            for(int side=-1;side<=1;side+=2)
            {
                for(int row=0;row<4;row++)SportsEventAuthoring.Box("Protected quarry spectator terrace",new Vector3(side*(76+row*2),86+row,0),new Vector3(2.5f,1.5f,64),dark,null,root);
                for(int k=0;k<3;k++){var light=new GameObject("Industrial quarry event floodlight").AddComponent<Light>();light.transform.SetParent(root);light.transform.position=new Vector3(side*65,110,-35+k*35);light.transform.LookAt(m.platformOrigin);light.type=LightType.Spot;light.spotAngle=85;light.range=150;light.intensity=9;}
            }
            var target=SceneManager.GetActiveScene();var source=EditorSceneManager.OpenScene(LWS.TruckHotPotato.Editor.HotPotatoAuthoring.Scene,OpenSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(target);var variants=source.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<LWS.TruckHotPotato.HotPotatoMatch>(true)).First().crowd.batches;
                for(int side=-1;side<=1;side+=2)for(int row=0;row<4;row++)for(int k=0;k<28;k++){var original=variants[(k+row)%variants.Length];var fan=new GameObject("Reused posed quarry spectator");fan.transform.SetParent(root);fan.transform.SetPositionAndRotation(new Vector3(side*(76+row*2),86.75f+row,-30+k*2.2f),Quaternion.Euler(0,side<0?90:-90,0));fan.AddComponent<MeshFilter>().sharedMesh=original.idle;fan.AddComponent<MeshRenderer>().sharedMaterials=original.materials;}
            }
            finally{EditorSceneManager.CloseScene(source,true);SceneManager.SetActiveScene(target);}
        }
        static void Suspension(SeesawMatch m,Material steel)
        {
            foreach(int side in new[]{-1,1})SportsEventAuthoring.Box("Suspension gantry upright",new Vector3(side*56,62,0),new Vector3(3,124,3),steel);
            SportsEventAuthoring.Box("Suspension gantry crossbeam",new Vector3(0,124,0),new Vector3(115,3,4),steel);
            var cable=new GameObject("Visible central suspension cable").AddComponent<LineRenderer>();cable.positionCount=2;cable.SetPosition(0,new Vector3(0,122,0));cable.SetPosition(1,m.platformOrigin);cable.widthMultiplier=.65f;cable.sharedMaterial=steel;
            var hub=GameObject.CreatePrimitive(PrimitiveType.Sphere);hub.name="Visible suspended universal pivot hub";hub.transform.position=m.platformOrigin-Vector3.up*4;hub.transform.localScale=new Vector3(5,4,5);UnityEngine.Object.DestroyImmediate(hub.GetComponent<Collider>());hub.GetComponent<Renderer>().sharedMaterial=steel;
        }
        static void Artwork(SeesawMatch m)
        {
            var logo=SportsRetroMenuAuthoring.LoadLogo(Folder);
            foreach(int index in new[]{0,4}){var page=m.menus.pages[index];var art=SportsUIAuthoring.Rect("Original sport title artwork",page.transform,new Vector2(260,160),new Vector2(-560,365)).AddComponent<UnityEngine.UI.Image>();art.sprite=logo;art.preserveAspect=true;art.raycastTarget=false;var title=page.transform.Find("Title").GetComponent<TMP_Text>();title.rectTransform.anchoredPosition+=Vector2.right*140;title.rectTransform.sizeDelta=new Vector2(1000,title.rectTransform.sizeDelta.y);}
            var material=SportsEventAuthoring.Mat(Folder,"Original quarry seesaw artwork",Color.white);material.shader=Shader.Find("Universal Render Pipeline/Unlit");material.mainTexture=logo.texture;material.SetFloat("_Surface",1);material.SetFloat("_SrcBlend",5);material.SetFloat("_DstBlend",10);material.SetFloat("_ZWrite",0);material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");material.renderQueue=3000;
            var artBoard=GameObject.CreatePrimitive(PrimitiveType.Quad);artBoard.name="Original seesaw artwork painted on actual moving deck";UnityEngine.Object.DestroyImmediate(artBoard.GetComponent<Collider>());artBoard.transform.SetParent(m.platform.transform,false);artBoard.transform.localPosition=Vector3.up*(m.halfThickness+.05f);artBoard.transform.localRotation=Quaternion.Euler(90,0,0);artBoard.transform.localScale=new Vector3(27,17,1);artBoard.GetComponent<Renderer>().sharedMaterial=material;
        }
    }
}

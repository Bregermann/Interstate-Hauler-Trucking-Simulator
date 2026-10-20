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
namespace LWS.TruckTetherball.Editor
{
    public static class TetherAuthoring
    {
        public const string Folder="TruckTetherball",Scene="Assets/TruckTetherball/Scenes/TruckTetherball.unity";
        public static void Create()
        {
            if(File.Exists(Scene))throw new InvalidOperationException("Preserve the authored tether court; use targeted upgrades.");
            var m=SportsEventAuthoring.Begin<TetherMatch>(Folder);m.setup.count=2;m.option=0;m.NormalizeSetup();m.feedback.clips=LWS.TruckHockey.Editor.HockeyAudioAuthoring.Create();
            var ground=SportsEventAuthoring.Mat(Folder,"Sunworn schoolyard court",new Color(.17f,.23f,.25f),.12f);var original=AssetDatabase.LoadAssetAtPath<Material>("Assets/TruckHotPotato/Materials/Festival scorched clay.mat");
            if(original==null)original=AssetDatabase.LoadAssetAtPath<Material>("Assets/TruckKeepaway/Materials/Rocky mud terrain.mat");ground.mainTexture=original.mainTexture;ground.mainTextureScale=Vector2.one*24;
            var physics=SportsEventAuthoring.Contact(Folder,"Schoolyard NWH driving contact",.85f,.02f);var baseFloor=SportsEventAuthoring.Box("Continuous detailed oversized tether court",new Vector3(0,-.5f,0),new Vector3(230,1,230),ground,physics);SportsDrivingSurfaceAuthoring.MarkHierarchy(baseFloor.transform);
            var teal=SportsEventAuthoring.Mat(Folder,"Worn turquoise event paint",new Color(.015f,.55f,.62f),.25f);var orange=SportsEventAuthoring.Mat(Folder,"Sun-faded golden orange",new Color(.95f,.42f,.025f),.2f);var chalk=SportsEventAuthoring.Mat(Folder,"Scuffed ivory court chalk",new Color(.95f,.89f,.67f),.1f);
            for(int k=0;k<80;k++){float a=k*2*Mathf.PI/80;var p=new Vector3(Mathf.Sin(a)*89,1.8f,Mathf.Cos(a)*89);var fence=SportsEventAuthoring.Box("Solid curved schoolyard perimeter",p,new Vector3(7.2f,3.6f,.7f),k%10<5?teal:orange,physics);fence.transform.rotation=Quaternion.Euler(0,a*Mathf.Rad2Deg,0);}
            var steel=SportsEventAuthoring.Mat(Folder,"Galvanised tether mast",new Color(.57f,.61f,.61f),.5f);var galvanized=AssetDatabase.LoadAssetAtPath<Material>("Assets/Kirkley Entertainment Inc/North American Speed Signs/Art/Materials/GalvanisedSteel.mat");steel.mainTexture=galvanized.mainTexture;
            var pole=GameObject.CreatePrimitive(PrimitiveType.Cylinder);pole.name="Giant physical schoolyard tether pole";pole.transform.position=Vector3.up*10;pole.transform.localScale=new Vector3(1.6f,10,1.6f);pole.GetComponent<Renderer>().sharedMaterial=steel;pole.GetComponent<Collider>().sharedMaterial=physics;
            var baseRing=GameObject.CreatePrimitive(PrimitiveType.Cylinder);baseRing.name="Modeled broad bolted pole foundation";baseRing.transform.position=Vector3.up*.22f;baseRing.transform.localScale=new Vector3(4,.22f,4);baseRing.GetComponent<Renderer>().sharedMaterial=orange;baseRing.GetComponent<Collider>().sharedMaterial=physics;
            var top=new GameObject("Actual fixed tether anchor at pole top").transform;top.position=Vector3.up*20;
            var ball=new GameObject("Massive physical pebbled rubber tetherball");var shell=GameObject.CreatePrimitive(PrimitiveType.Sphere);shell.name="Owned pebbled rubber sphere visual";shell.transform.SetParent(ball.transform,false);shell.transform.localScale=Vector3.one*6;UnityEngine.Object.DestroyImmediate(shell.GetComponent<Collider>());ball.AddComponent<SphereCollider>().radius=3;
            var ballMaterial=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<Material>("Assets/TruckKickball/Materials/Red pebbled rubber kickball.mat"));ballMaterial.name="Owned orange pebbled rubber tetherball";ballMaterial.color=new Color(1,.44f,.035f);ballMaterial.enableInstancing=true;AssetDatabase.CreateAsset(ballMaterial,"Assets/"+Folder+"/Materials/Orange pebbled rubber tetherball.mat");shell.GetComponent<Renderer>().sharedMaterial=ballMaterial;ball.GetComponent<Collider>().sharedMaterial=SportsEventAuthoring.Contact(Folder,"Bounded giant tetherball bounce",.15f,.3f);
            m.ball=ball.AddComponent<Rigidbody>();m.ball.mass=1200;m.ball.linearDamping=.02f;m.ball.angularDamping=.08f;m.ball.maxAngularVelocity=25;m.ball.interpolation=RigidbodyInterpolation.Interpolate;m.ball.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;m.ball.solverIterations=24;m.ball.solverVelocityIterations=12;m.ball.isKinematic=true;
            var joint=ball.AddComponent<ConfigurableJoint>();joint.autoConfigureConnectedAnchor=false;joint.xMotion=joint.yMotion=joint.zMotion=ConfigurableJointMotion.Limited;joint.angularXMotion=joint.angularYMotion=joint.angularZMotion=ConfigurableJointMotion.Free;joint.linearLimitSpring=new SoftJointLimitSpring{spring=800000,damper=55000};joint.projectionMode=JointProjectionMode.PositionAndRotation;joint.projectionDistance=.1f;joint.enablePreprocessing=true;
            var braid=AssetDatabase.LoadAssetAtPath<Material>("Assets/TruckBungeeDemolition/Materials/Bungee braided safety cable.mat");m.ropeGroup=new GameObject("Pooled replay-recordable actual winding rope segments").transform;
            m.cable=m.gameObject.AddComponent<TetherCable>();m.cable.ball=m.ball;m.cable.constraint=joint;m.cable.poleTop=top;m.cable.segments=new Transform[64];
            for(int i=0;i<64;i++){var segment=GameObject.CreatePrimitive(PrimitiveType.Cylinder);segment.name="Pooled braided tether segment "+i;segment.transform.SetParent(m.ropeGroup,false);UnityEngine.Object.DestroyImmediate(segment.GetComponent<Collider>());segment.GetComponent<Renderer>().sharedMaterial=braid;segment.GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;m.cable.segments[i]=segment.transform;}
            m.cable.ResetCable(m.requiredTurns);m.ball.position=m.cable.StartPosition;ball.transform.position=m.cable.StartPosition;m.cable.Draw(0);ball.AddComponent<TetherBallContact>().match=m;
            CourtLines(chalk,teal,orange);Venue();m.trucks=new SportsEventTruck[2];m.fragments=new SportsFragmentRig[2];
            for(int i=0;i<2;i++){var p=m.cable.StartPosition+new Vector3(i==0?-3:3,.65f-m.cable.StartPosition.y,i==0?-24:24);var truck=FareFightVehicleAuthoring.Spawn(m,i,p,Quaternion.Euler(0,i==0?0:180,0));m.trucks[i]=truck;m.fragments[i]=SportsFragmentAuthoring.InstallRigid(truck.body,truck.visuals.GetComponentsInChildren<MeshFilter>(true),Folder);}
            m.broadcast=SportsEventAuthoring.Camera("Schoolyard ball and both tractors broadcast");m.broadcast.view.farClipPlane=2200;m.broadcast.view.orthographic=true;m.framing=m.broadcast.gameObject.AddComponent<SportsCameraFit>();m.framing.cameraTarget=m.broadcast.view;m.framing.arenaFit=true;m.framing.topInset=.1f;m.framing.bottomInset=.22f;m.cameras=new[]{m.broadcast.view};
            SportsEventAuthoring.UI(m);SportsRallyAuthoring.CompactUI(m);for(int i=0;i<2;i++)((RectTransform)m.menus.rows[i].transform).anchoredPosition=new Vector2(i==0?-350:350,100);m.drivingViews=SportsDrivingViewsAuthoring.Install(m,m.trucks,m.broadcast,Folder);m.drivingViews.independentViewModes=true;
            m.atmosphere=SportsChaosAuthoring.Weather(m,Folder);m.atmosphere.presets=SportsWeatherPresetsAuthoring.EnsureCatalog();m.drivingViews.rally.weather=m.atmosphere;SportsWeatherPickerAuthoring.Install(m,m.atmosphere.presets,new Vector2(350,-180));
            var hud=m.gameObject.AddComponent<SportsExpansionViewHud>();hud.match=m;hud.views=m.drivingViews;SportsUIAuthoring.Button(m.menus.pages[6].transform,"WATCH THE WINNING WIND","watchtether",new Vector2(0,-245),new Vector2(520,48));Artwork(m);SportsBroadcastAuthoring.ApplyScene();SportsEventAuthoring.Save(Folder);Debug.Log("TETHER_AUTHORED genuine PhysX constrained ball, finite winding rope and full NWH tractors");
        }
        public static void UpgradeVenue()
        {
            EditorSceneManager.OpenScene(Scene);
            if(GameObject.Find("Tether festival meadow surround")==null)
            {
                var grass=SportsEventAuthoring.Mat(Folder,"Vista festival meadow surround",new Color(.42f,.64f,.24f),.05f);
                grass.mainTexture=AssetDatabase.LoadAssetAtPath<TerrainLayer>("Assets/PinwheelStudio/Vista/Personal/Samples/TerrainLayers/Grass Light/Grass_Light_VistaSample.terrainlayer").diffuseTexture;
                grass.mainTextureScale=Vector2.one*45;
                var surround=SportsEventAuthoring.Box("Tether festival meadow surround",new Vector3(0,-1.1f,0),new Vector3(600,1,600),grass,SportsEventAuthoring.Contact(Folder,"Surround meadow contact",.8f,.05f));SportsDrivingSurfaceAuthoring.MarkHierarchy(surround.transform);
            }
            foreach(var go in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())if(go.name.StartsWith("Reused owned tether festival"))
            {
                var renderers=go.GetComponentsInChildren<Renderer>(true);var active=renderers.Where(x=>x.enabled&&x.gameObject.activeInHierarchy).ToArray();
                if(active.Length>0){var bounds=active[0].bounds;foreach(var renderer in active)bounds.Encapsulate(renderer.bounds);Debug.Log("TETHER_VENUE "+go.name+" renderers="+renderers.Length+" active="+active.Length+" bounds="+bounds);}
            }
            foreach(var go in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())if(go.name=="Painted assigned winding direction"||go.name=="Painted direction arrowhead")UnityEngine.Object.DestroyImmediate(go);
            var chalk=AssetDatabase.LoadAssetAtPath<Material>("Assets/"+Folder+"/Materials/Scuffed ivory court chalk.mat");
            var teal=AssetDatabase.LoadAssetAtPath<Material>("Assets/"+Folder+"/Materials/Worn turquoise event paint.mat");var orange=AssetDatabase.LoadAssetAtPath<Material>("Assets/"+Folder+"/Materials/Sun-faded golden orange.mat");
            DirectionArrows(teal,orange);
            var match=UnityEngine.Object.FindAnyObjectByType<TetherMatch>();
            SportsDrivingSurfaceAuthoring.MarkHierarchy(GameObject.Find("Tether festival meadow surround").transform);
            SportsScenicGradeAuthoring.Install(match.gameObject,new[]{match.broadcast.view}.Concat(match.drivingViews.rally.views.Select(v=>v.view)).ToArray(),Folder,-.4f);
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());AssetDatabase.SaveAssets();
        }
        static void DirectionArrows(Material teal,Material orange)
        {
            for(int side=0;side<2;side++)for(int i=0;i<12;i++)
            {
                var material=side==0?teal:orange;float direction=side==0?1:-1;float a=(side*Mathf.PI+i*Mathf.PI/12)*direction;
                var p=new Vector3(Mathf.Cos(a)*43,.055f,Mathf.Sin(a)*43);var forward=new Vector3(-Mathf.Sin(a),0,Mathf.Cos(a))*direction;
                var mark=SportsEventAuthoring.Box("Painted assigned winding direction",p,new Vector3(.6f,.035f,5),material);mark.transform.rotation=Quaternion.LookRotation(forward);
                var tip=p+forward*2.5f;
                for(int arm=-1;arm<=1;arm+=2){var directionToTip=Quaternion.AngleAxis(arm*42,Vector3.up)*forward;var fin=SportsEventAuthoring.Box("Painted direction arrowhead",tip-directionToTip*1.15f,new Vector3(.55f,.035f,2.3f),material);fin.transform.rotation=Quaternion.LookRotation(directionToTip);}
            }
        }
        static void CourtLines(Material chalk,Material teal,Material orange)
        {
            for(int ring=0;ring<3;ring++)
            {
                float radius=new[]{12f,32f,55f}[ring];var line=new GameObject("Schoolyard circular play marking "+ring).AddComponent<LineRenderer>();line.useWorldSpace=true;line.positionCount=129;line.widthMultiplier=.23f;line.sharedMaterial=chalk;for(int i=0;i<=128;i++){float a=i*2*Mathf.PI/128;line.SetPosition(i,new Vector3(Mathf.Sin(a)*radius,.035f,Mathf.Cos(a)*radius));}
            }
            DirectionArrows(teal,orange);
        }
        static void Venue()
        {
            var destination=SceneManager.GetActiveScene();var source=EditorSceneManager.OpenScene(LWS.TruckHotPotato.Editor.HotPotatoAuthoring.Scene,OpenSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(destination);
                foreach(var root in source.GetRootGameObjects())if(root.name.Contains("Union Field grandstands")||root.name.Contains("spectators"))
                {var copy=SportsReplayVisuals.Copy(root.transform);copy.name="Reused owned tether festival "+root.name;SceneManager.MoveGameObjectToScene(copy,destination);}
            }
            finally{EditorSceneManager.CloseScene(source,true);SceneManager.SetActiveScene(destination);}
        }
        static void Artwork(TetherMatch m)
        {
            var logo=SportsRetroMenuAuthoring.LoadLogo(Folder);if(logo==null)throw new InvalidOperationException("Import original Tetherball artwork first.");
            foreach(int index in new[]{0,4}){var page=m.menus.pages[index];var art=SportsUIAuthoring.Rect("Original sport title artwork",page.transform,new Vector2(260,160),new Vector2(-560,365)).AddComponent<UnityEngine.UI.Image>();art.sprite=logo;art.preserveAspect=true;art.raycastTarget=false;var title=page.transform.Find("Title").GetComponent<TMP_Text>();title.rectTransform.anchoredPosition+=Vector2.right*140;title.rectTransform.sizeDelta=new Vector2(1000,title.rectTransform.sizeDelta.y);}
            var material=SportsEventAuthoring.Mat(Folder,"Original grungy tether festival signage",Color.white);material.shader=Shader.Find("Universal Render Pipeline/Unlit");material.mainTexture=logo.texture;material.SetFloat("_Surface",1);material.SetFloat("_SrcBlend",5);material.SetFloat("_DstBlend",10);material.SetFloat("_ZWrite",0);material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");material.renderQueue=3000;
            foreach(int side in new[]{-1,1}){var sign=GameObject.CreatePrimitive(PrimitiveType.Quad);sign.name="Original tether festival venue artwork";UnityEngine.Object.DestroyImmediate(sign.GetComponent<Collider>());sign.transform.position=new Vector3(side*55,12,80);sign.transform.localScale=new Vector3(28,16,1);sign.GetComponent<Renderer>().sharedMaterial=material;for(int leg=-1;leg<=1;leg+=2)SportsEventAuthoring.Box("Tether venue sign support",new Vector3(side*55+leg*12,6,80.4f),new Vector3(.6f,12,.6f),AssetDatabase.LoadAssetAtPath<Material>("Assets/"+Folder+"/Materials/Galvanised tether mast.mat"));}
        }
    }
}

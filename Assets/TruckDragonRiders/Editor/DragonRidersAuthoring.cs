#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using CompassNavigatorPro;
using LWS.TruckDragonKingdom;
using LWS.TruckDragonKingdom.Editor;
using LWS.TruckMechanicalBull.Editor;
using LWS.TruckSports;
using LWS.TruckSports.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LWS.TruckDragonRiders.Editor
{
    public static class DragonRidersAuthoring
    {
        public const string Folder="TruckDragonRiders";
        public const string ScenePath="Assets/TruckDragonRiders/Scenes/TruckDragonRiders.unity";
        [MenuItem("Truck Sports/Dragon Kingdom/Create NEW Dragon Riders Scene")]
        public static void Create()
        {
            ImpossibleBalanceArt.GuardNew(ScenePath);
            var shared=AssetDatabase.LoadAssetAtPath<GameObject>(DragonKingdomWorldAuthoring.PrefabPath);
            var catalog=AssetDatabase.LoadAssetAtPath<DragonSpeciesCatalog>(DragonSpeciesAuthoring.CatalogPath);
            if(shared==null||catalog==null)throw new InvalidOperationException("Native-authored shared kingdom and configured dragon catalog are required first");
            var rideable=catalog.species.Where(s=>s!=null&&s.Supports(DragonSpeciesRole.RiderMount)).ToArray();
            if(rideable.Length<1)throw new InvalidOperationException("No actual configured rideable species is available");
            var match=SportsEventAuthoring.Begin<DragonRidersMatch>(Folder);match.catalog=catalog;match.setup.count=4;match.option=1;match.NormalizeSetup();
            match.world=((GameObject)PrefabUtility.InstantiatePrefab(shared)).GetComponent<DragonKingdomWorld>();match.world.ValidateReferences();
            match.trucks=new SportsEventTruck[4];match.racers=new DragonRiderRacer[4];
            var mounts=new GameObject("Configured rideable dragon choices").transform;
            for(int i=0;i<4;i++)
            {
                var truck=ImpossibleBalanceArt.Tractor(match,i,match.world.riderStarts[i].position);match.trucks[i]=truck;
                var racer=truck.gameObject.AddComponent<DragonRiderRacer>();racer.match=match;racer.truck=truck;racer.slot=i;match.racers[i]=racer;
                racer.choices=new DragonCharacter[rideable.Length];racer.choiceIds=rideable.Select(s=>s.id).ToArray();
                match.selectedSpeciesIds[i]=rideable[i%rideable.Length].id;
                for(int k=0;k<rideable.Length;k++)
                {
                    string id=rideable[k].id,path="Assets/TruckDragonKingdom/Prefabs/Dragons/"+id+"/"+id+" Dragon Actor.prefab";
                    var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(prefab==null)throw new InvalidOperationException("Configured owned mount actor missing: "+path);
                    var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab,mounts);instance.name="Crew "+(i+1)+" · "+rideable[k].displayName;
                    instance.transform.position=match.world.riderStarts[i].position;
                    var actor=instance.GetComponent<DragonCharacter>();racer.choices[k]=actor;
                    if(actor==null||actor.saddle==null||actor.leftGrip==null||actor.motor==null)throw new InvalidOperationException("Actual motor/saddle/rescue grip required on "+id);
                    foreach(var child in actor.GetComponentsInChildren<Transform>(true))child.gameObject.layer=2;
                    Saddle(actor,truck);DragonRiderSaddleAuthoring.InstallForNewChoice(actor,truck,rideable[k]);instance.SetActive(false);
                }
                Parachute(racer);
            }
            GateCourse(match);
            var broadcast=SportsEventAuthoring.Camera("Skyfire aerial broadcast");broadcast.view.farClipPlane=6500;
            broadcast.Pose(match.world.riderStarts[0].position,new Vector3(-30,18,-45),70,true);match.cameras=new[]{broadcast.view};
            ImpossibleBalanceArt.UI(match);
            match.drivingViews=SportsDrivingViewsAuthoring.Install(match,match.trucks,broadcast,Folder);
            match.drivingViews.defaultMode=1;match.drivingViews.independentViewModes=true;
            foreach(var camera in match.drivingViews.rally.views)camera.view.farClipPlane=6500;
            match.speciesLabels=new TMPro.TMP_Text[4];
            for(int i=0;i<4;i++)match.speciesLabels[i]=SportsUIAuthoring.Button(match.menus.pages[0].transform,"MOUNT "+(i+1),"mount:"+i,new Vector2(380,155-i*104),new Vector2(560,52)).normalTextObj;
            match.RefreshExtraUI();
            match.feedback.clips=(AudioClip[])LWS.TruckHockey.Editor.HockeyAudioAuthoring.Create().Clone();
            match.presentationWeather=SportsChaosAuthoring.Weather(match,Folder);match.presentationWeather.presets=SportsWeatherPresetsAuthoring.EnsureCatalog();
            match.drivingViews.rally.weather=match.presentationWeather;
            var cameras=match.drivingViews.rally.views.Select(c=>c.view).Append(broadcast.view).ToArray();
            SportsWeatherRenderingAuthoring.Install(match.gameObject,cameras,Folder);
            Navigation(match);
            foreach(var replay in match.menus.replayButtons)replay.SetActive(false);
            LWS.TruckDragonKingdom.Ambience.DragonKingdomAmbienceAuthoring.Install(match.world);
            if(PrefabUtility.IsPartOfPrefabInstance(match.world))PrefabUtility.RecordPrefabInstancePropertyModifications(match.world);
            DragonKingdomBroadcastAuthoring.Install(match,match.trucks,broadcast);
            SportsEventAuthoring.Save(Folder);
            SportsEnvironmentAuthoring.BindAuthoredOutdoorSession(match,Folder,match.world.GetComponentsInChildren<Light>(true).Where(l=>l.type!=LightType.Directional).ToArray());
            DragonKingdomArtworkAuthoring.Install(match,match.world,Folder);
            SportsTruckAppearanceHostAuthoring.BindNewImpossibleScene(match,match.trucks);
            SportsBroadcastAuthoring.ApplyScene();
            DragonKingdomModeAuthoring.ConfigureKingdomSnow(match);
            EditorSceneManager.MarkSceneDirty(match.gameObject.scene);
            if(!EditorSceneManager.SaveScene(match.gameObject.scene))throw new InvalidOperationException("The authored Riders scene did not save.");
            AssetDatabase.SaveAssets();
            // V87's old post-import object graph failed its aggregate guard,
            // while V90 proved every actual saved binding/mount/listener valid.
            // Validate the durable saved object graph after authoring/imports,
            // rather than retaining potentially stale pre-import wrappers.
            var current=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(ScenePath);
            if(current.IsValid()&&current.isDirty)throw new InvalidOperationException("Preserve newly unsaved Riders edits before validating the saved scene.");
            var saved=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
            ValidateScene(saved.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<DragonRidersMatch>(true)).Single());
            Debug.Log("DRAGON_RIDERS_AUTHORED · Actual flight, contact rescues, AI races and rendered split-screen acceptance remain required");
        }

        static void GateCourse(DragonRidersMatch match)
        {
            var points=match.world.aerialCheckpoints;
            if(points.Length<8)throw new InvalidOperationException("The shared kingdom must provide its real aerial loop");
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/TruckSkyTow/Art/Arctic standard gate body.asset");
            if(mesh==null)throw new InvalidOperationException("Existing authored physical ring mesh is required");
            var material=ImpossibleBalanceArt.Material(Folder,"Royal skyfire gate gold",new Color(1,.62f,.1f),.55f,.4f);
            material.EnableKeyword("_EMISSION");material.SetColor("_EmissionColor",new Color(1,.3f,.02f)*2);
            var root=new GameObject("Ordered kingdom aerial gate loop").transform;
            match.gates=new DragonRiderGateDefinition[points.Length];
            float half=Mathf.Max(mesh.bounds.extents.x,mesh.bounds.extents.y);if(half<.1f)throw new InvalidOperationException("Existing ring bounds are invalid");
            for(int i=0;i<points.Length;i++)
            {
                Vector3 before=i==0?match.world.riderStarts[0].position:points[i-1].position;
                var rotation=Quaternion.LookRotation((points[i].position-before).normalized,Vector3.up);
                var gate=new GameObject("Aerial gate "+(i+1));gate.transform.SetParent(root,false);gate.transform.SetPositionAndRotation(points[i].position,rotation);
                gate.transform.localScale=Vector3.one*(37/half);gate.AddComponent<MeshFilter>().sharedMesh=mesh;gate.AddComponent<MeshRenderer>().sharedMaterial=material;
                gate.AddComponent<MeshCollider>().sharedMesh=mesh;
                match.gates[i]=new DragonRiderGateDefinition{center=points[i].position,rotation=rotation,clearRadius=32};
            }
            EditorUtility.SetDirty(material);
        }
        static void Saddle(DragonCharacter actor,SportsEventTruck truck)
        {
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(DragonKingdomWorldAuthoring.Dock+"DockNoRailingConnector.prefab");
            if(source==null)throw new InvalidOperationException("Owned timber saddle support module required");
            var deck=(GameObject)PrefabUtility.InstantiatePrefab(source,actor.saddle);
            deck.name="Actual timber truck saddle support";deck.transform.rotation=actor.motor.transform.rotation;
            var bounds=DragonKingdomWorldAuthoring.MeasureOwnedModelBounds(deck.transform);
            var scale=deck.transform.localScale;scale.x*=(truck.shape.size.x+.5f)/bounds.size.x;scale.y*=.2f/bounds.size.y;scale.z*=(truck.shape.size.z+.4f)/bounds.size.z;deck.transform.localScale=scale;
            bounds=DragonKingdomWorldAuthoring.MeasureOwnedModelBounds(deck.transform);
            deck.transform.position+=actor.saddle.position-new Vector3(bounds.center.x,bounds.max.y,bounds.center.z);
            foreach(var collider in deck.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(collider);
        }
        static void Parachute(DragonRiderRacer racer)
        {
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/TruckBaseJumping/Materials/Parachute canopy.asset");
            var cloth=AssetDatabase.LoadAssetAtPath<Material>("Assets/TruckBaseJumping/Materials/Canopy ivory.mat");
            if(mesh==null||cloth==null)throw new InvalidOperationException("Existing authored parachute mesh/material required");
            var canopy=new GameObject("Crew "+(racer.slot+1)+" physical rescue parachute");canopy.AddComponent<MeshFilter>().sharedMesh=mesh;canopy.AddComponent<MeshRenderer>().sharedMaterials=new[]{cloth,cloth};
            racer.canopy=canopy.transform;racer.chuteLines=new LineRenderer[4];
            for(int i=0;i<4;i++){var line=new GameObject("Crew chute line "+racer.slot+" "+i).AddComponent<LineRenderer>();line.positionCount=2;line.startWidth=line.endWidth=.04f;line.sharedMaterial=cloth;line.enabled=false;racer.chuteLines[i]=line;}
            canopy.SetActive(false);
        }
        static void Navigation(DragonRidersMatch match)
        {
            var navigation=match.gameObject.AddComponent<DragonRiderNavigation>();navigation.match=match;navigation.compasses=new CompassPro[4];navigation.objectives=new CompassProPOI[4];
            var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Plugins/Kronnect/CompassNavigatorPro/Resources/CNPro/Prefabs/CompassNavigatorPro.prefab");
            if(source==null)throw new InvalidOperationException("Installed Compass Navigator Pro presentation required");
            for(int view=0;view<4;view++)
            {
                var root=(GameObject)PrefabUtility.InstantiatePrefab(source);root.SetActive(false);root.name="Skyfire objective compass "+view;
                var original=root.GetComponentInChildren<CompassPro>(true);var compass=original.gameObject.AddComponent<SportsBroadcastCompass>();EditorUtility.CopySerializedManagedFieldsOnly(original,compass);UnityEngine.Object.DestroyImmediate(original);
                navigation.compasses[view]=compass;var camera=match.drivingViews.rally.views[view].view;
                var canvas=compass.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=.5f;
                var serialized=new SerializedObject(compass);serialized.FindProperty("_compassGroup").intValue=view+1;
                serialized.FindProperty("_cameraMain").objectReferenceValue=camera;serialized.FindProperty("_follow").objectReferenceValue=match.trucks[view].transform;
                serialized.FindProperty("_showMiniMap").boolValue=false;serialized.FindProperty("_showCompassBar").boolValue=false;
                serialized.FindProperty("_showOnScreenIndicators").boolValue=true;serialized.FindProperty("_showOffScreenIndicators").boolValue=true;
                serialized.FindProperty("_offScreenIndicatorShowDistance").boolValue=false;serialized.FindProperty("_onScreenIndicatorShowDistance").boolValue=false;serialized.ApplyModifiedPropertiesWithoutUndo();
                foreach(var child in root.GetComponentsInChildren<Transform>(true))child.gameObject.layer=26+view;
                var marker=new GameObject("Ordered aerial objective "+view);marker.SetActive(false);marker.transform.SetParent(navigation.transform,false);
                var poi=marker.AddComponent<CompassProPOI>();poi.canBeVisited=poi.hideWhenVisited=poi.dontDestroyOnLoad=false;
                poi.showOnScreenIndicator=poi.showOffScreenIndicator=poi.ignoreAreaOfInterest=true;poi.radius=0;poi.compassGroupMask=1<<view;
                poi.iconNonVisited=AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");compass.permittedTargets=new[]{poi};navigation.objectives[view]=poi;
            }
        }
        public static void ValidateScene(DragonRidersMatch match)
        {
            if(match==null)throw new InvalidOperationException("Riders match reference is missing or stale; inspect the actual saved scene before changing its bindings.");
            var missing=new[]{match.world==null?"sharedWorld":null,match.catalog==null?"catalog":null,
                match.trucks==null||match.trucks.Length!=4?"trucks="+(match.trucks==null?-1:match.trucks.Length):null,
                match.racers==null||match.racers.Length!=4?"racers="+(match.racers==null?-1:match.racers.Length):null,
                match.gates==null||match.gates.Length<8?"gates="+(match.gates==null?-1:match.gates.Length):null,
                match.presentationWeather==null?"presentationWeather":null,match.drivingViews==null?"drivingViews":null}.Where(x=>x!=null).ToArray();
            if(missing.Length>0)throw new InvalidOperationException("Riders actual binding guard failed: "+string.Join(", ",missing)+" scene="+match.gameObject.scene.path);
            foreach(var racer in match.racers)
                if(racer.truck.tractorMotor==null||racer.truck.tractorMotor.vehicle==null||racer.choices.Length<1||racer.choices.Any(c=>c==null||c.motor==null||c.saddle==null||c.leftGrip==null)||racer.canopy==null||racer.chuteLines.Length!=4)
                    throw new InvalidOperationException("Incomplete actual NWH mount/rescue crew");
            if(UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Include).Length!=1)throw new InvalidOperationException("One original listener is required");
        }
    }
}
#endif

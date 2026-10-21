using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using LWS.TruckSports;
using CompassNavigatorPro;
using Cinemachine;

namespace LWS.TruckSports.Editor
{
    public static class SportsBroadcastCameraAuthoring
    {
        const string CompassPath="Assets/TruckSports/Resources/Broadcast objective compass.prefab";
        const string ProfilePath="Assets/TruckSports/Settings/Keepaway broadcast framing.asset";
        static SportsBroadcastProfile Profile()
        {
            var profile=AssetDatabase.LoadAssetAtPath<SportsBroadcastProfile>(ProfilePath);if(profile!=null)return profile;
            profile=ScriptableObject.CreateInstance<SportsBroadcastProfile>();AssetDatabase.CreateAsset(profile,ProfilePath);return profile;
        }
        static GameObject CompassPrefab()
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(CompassPath);if(prefab!=null)return prefab;
            var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Plugins/Kronnect/CompassNavigatorPro/Resources/CNPro/Prefabs/CompassNavigatorPro.prefab");
            if(source==null)throw new InvalidOperationException("Installed Compass Navigator Pro prefab missing");
            var copy=UnityEngine.Object.Instantiate(source);copy.SetActive(false);copy.name="Broadcast objective compass";
            try
            {
                var prior=copy.GetComponent<CompassPro>();var compass=copy.AddComponent<SportsBroadcastCompass>();EditorUtility.CopySerializedManagedFieldsOnly(prior,compass);UnityEngine.Object.DestroyImmediate(prior);compass.dontDestroyOnLoad=false;
                var settings=new SerializedObject(compass);
                foreach(string field in new[]{"_showCompassBar","_showMiniMap","_showOnScreenIndicators","_offScreenIndicatorShowDistance"})settings.FindProperty(field).boolValue=false;
                settings.FindProperty("_showOffScreenIndicators").boolValue=true;settings.FindProperty("_compassGroup").intValue=4;
                settings.FindProperty("_offScreenIndicatorMargin").floatValue=.075f;settings.FindProperty("_offScreenIndicatorAlpha").floatValue=.9f;
                settings.FindProperty("_offScreenIndicatorScale").floatValue=.8f;settings.ApplyModifiedPropertiesWithoutUndo();
                foreach(var scaler in copy.GetComponentsInChildren<UnityEngine.UI.CanvasScaler>(true)){scaler.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.screenMatchMode=UnityEngine.UI.CanvasScaler.ScreenMatchMode.Expand;}
                foreach(var canvas in copy.GetComponentsInChildren<Canvas>(true)){canvas.overrideSorting=true;canvas.sortingOrder=34;}
                return PrefabUtility.SaveAsPrefabAsset(copy,CompassPath);
            }
            finally{UnityEngine.Object.DestroyImmediate(copy);}
        }
        static void Guidance(SportsBroadcastFraming framing)
        {
            var guide=framing.GetComponent<SportsBroadcastGuidance>();
            int capacity=Mathf.Min(7,framing.SubjectCount+1);
            if(guide!=null&&guide.targets!=null&&guide.targets.Length>=capacity)return;
            if(guide==null)guide=framing.gameObject.AddComponent<SportsBroadcastGuidance>();guide.framing=framing;
            var owner=framing.session!=null?framing.session.transform:framing.source.transform;
            if(guide.compass==null){var root=(GameObject)PrefabUtility.InstantiatePrefab(CompassPrefab());root.transform.SetParent(owner,false);root.SetActive(false);guide.compass=root.GetComponent<SportsBroadcastCompass>();}
            var prior=guide.targets??Array.Empty<CompassProPOI>();guide.targets=new CompassProPOI[capacity];Array.Copy(prior,guide.targets,Mathf.Min(prior.Length,capacity));
            for(int i=0;i<capacity;i++)
            {
                if(guide.targets[i]!=null)continue;
                var marker=new GameObject(i==0?"Broadcast objective POI":"Broadcast human POI "+i);marker.transform.SetParent(owner,false);marker.SetActive(false);
                var poi=marker.AddComponent<CompassProPOI>();poi.canBeVisited=false;poi.hideWhenVisited=false;poi.dontDestroyOnLoad=false;poi.showOnScreenIndicator=false;poi.showOffScreenIndicator=true;poi.ignoreAreaOfInterest=true;poi.playAudioClipWhenVisited=false;poi.radius=0;poi.compassGroupMask=1<<3;
                poi.iconNonVisited=i==0?AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd"):AssetDatabase.LoadAssetAtPath<Sprite>("Assets/TruckSports/Resources/Broadcast truck portrait.png");guide.targets[i]=poi;
            }
            guide.compass.permittedTargets=guide.targets;EditorUtility.SetDirty(guide);
        }
        static void Hockey()
        {
            var match=UnityEngine.Object.FindAnyObjectByType<LWS.TruckHockey.HockeyMatch>();var camera=Camera.main;
            if(match==null||camera==null)throw new InvalidOperationException("Hockey match/camera missing");
            var adapter=camera.GetComponent<SportsEventCamera>();if(adapter==null)
            {
                adapter=camera.gameObject.AddComponent<SportsEventCamera>();adapter.view=camera;
                adapter.target=new GameObject("Hockey stable broadcast target").transform;adapter.target.SetParent(match.transform,false);
                adapter.aim=new GameObject("Hockey stable broadcast aim").transform;adapter.aim.SetParent(match.transform,false);
                adapter.rig=new GameObject("Hockey existing-camera Cinemachine rig").AddComponent<CinemachineVirtualCamera>();adapter.rig.transform.SetParent(match.transform,false);adapter.rig.Follow=adapter.target;adapter.rig.LookAt=adapter.aim;adapter.rig.Priority=-100;
                adapter.follow=adapter.rig.AddCinemachineComponent<CinemachineTransposer>();adapter.follow.m_BindingMode=CinemachineTransposer.BindingMode.WorldSpace;adapter.rig.AddCinemachineComponent<CinemachineComposer>();adapter.rig.enabled=false;
            }
            var source=match.GetComponent<LWS.TruckHockey.HockeyBroadcastSource>();if(source==null)source=match.gameObject.AddComponent<LWS.TruckHockey.HockeyBroadcastSource>();source.match=match;
            var framing=camera.GetComponent<SportsBroadcastFraming>();if(framing==null)framing=camera.gameObject.AddComponent<SportsBroadcastFraming>();framing.cameraRig=adapter;adapter.broadcastFraming=framing;framing.source=source;framing.useOrthographic=true;
            framing.bodies=match.trucks.Select(t=>t.body).ToArray();framing.modelRoots=match.trucks.Select(t=>t.transform).ToArray();framing.bodyShapes=match.trucks.Select(t=>t.GetComponentsInChildren<Collider>().First(c=>!c.isTrigger&&c.attachedRigidbody==t.body)).ToArray();
            if(framing.authoringVersion<2){framing.sharedDefaults=Profile();framing.useSharedDefaults=true;framing.authoringVersion=2;}
            var fit=camera.GetComponent<SportsCameraFit>();fit.broadcastFraming=framing;Guidance(framing);EditorUtility.SetDirty(framing);EditorUtility.SetDirty(fit);EditorUtility.SetDirty(source);
        }
        static readonly string[] ApplyTo={"TruckLogRolling","TruckTrampolineDodgeball","TruckBungeeDemolition","TruckTerrierRacing","TruckKickball","TruckMagnetMayhem","TruckKeepaway","TruckKeepawayFirstPerson","TruckHotPotato","TruckVolleyball","TruckKingOfTheHill","TruckBalloonBattle","TruckRedLightGreenLight","TruckWhackAMole","TruckSeesaw","TruckFloorIsLava","TruckTetherball","TruckJousting","TruckFreezeTag","TruckHungryHaulers","TruckSheepHerding","TruckFrogger","TruckAvalancheEscape","TruckNinjaWarrior","TruckFareFight","TruckSnake","TruckMusicalParking","TruckSumo","TruckDemolitionDerby","TruckBuzkashi","TruckPedestrianPlowing","TruckPedestrianPlowingFirstPerson"};
        public static void Bind()
        {
            var report=new StringBuilder("Targeted camera bindings; no environment regeneration.\n");
            foreach(var path in SportsBuilder.SceneList())
            {
                string sceneName=Path.GetFileNameWithoutExtension(path);
                if(sceneName=="TruckHockey")
                {
                    var hockeyScene=EditorSceneManager.OpenScene(path,OpenSceneMode.Single);Hockey();EditorSceneManager.MarkSceneDirty(hockeyScene);EditorSceneManager.SaveScene(hockeyScene);report.AppendLine(sceneName+" | BOUND | legacy Hockey read-only source; existing camera reused");continue;
                }
                if(!ApplyTo.Contains(sceneName)){report.AppendLine(sceneName+" | NOT MODIFIED | precision, protected, already local follow or separate camera architecture; audit retained");continue;}
                var scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Single);
                var match=UnityEngine.Object.FindAnyObjectByType<SportsEventSession>();
                if(match==null)throw new InvalidOperationException(sceneName+" missing event session");
                SportsEventCamera camera=null;
                var field=match.GetType().GetField("broadcast")??match.GetType().GetField("arenaCamera");
                if(field!=null)camera=field.GetValue(match) as SportsEventCamera;
                if(match is LWS.TruckKickball.KickballMatch kick)camera=kick.views.broadcast;
                if(match is LWS.TruckDodgeball.DodgeMatch dodge)camera=dodge.views.broadcast;
                if(match is LWS.TruckPedestrianPlowing.PlowMatch plow)camera=plow.views.rally.broadcast;
                if(camera==null)throw new InvalidOperationException(sceneName+" explicit shared camera missing");
                var framing=camera.GetComponent<SportsBroadcastFraming>();
                bool created=framing==null;if(created)framing=camera.gameObject.AddComponent<SportsBroadcastFraming>();
                framing.session=match;framing.cameraRig=camera;camera.broadcastFraming=framing;
                var all=UnityEngine.Object.FindObjectsByType<SportsEventTruck>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(t=>t.session==match).ToArray();
                var roster=match.GetType().GetField("trucks")?.GetValue(match) as SportsEventTruck[];
                if(match is LWS.TruckKickball.KickballMatch k)roster=k.agents.Select(a=>a.truck).ToArray();
                if(match is LWS.TruckLogRolling.LogMatch logMatch)roster=logMatch.fighters.Select(f=>f.truck).ToArray();
                if(match is LWS.TruckDodgeball.DodgeMatch dodgeMatch)roster=dodgeMatch.fighters.Select(f=>f.truck).ToArray();
                if(match is LWS.TruckBungeeDemolition.BungeeMatch bungee)roster=bungee.bays.Select(b=>b.truck).ToArray();
                if(match is LWS.TruckTerrierRacing.TerrierMatch terrier)roster=terrier.racers.Select(r=>r.truck).ToArray();
                framing.trucks=roster??all.OrderBy(t=>t.slot).ToArray();
                if(match is LWS.TruckJousting.JoustMatch joust)framing.excludedSubjectRoots=joust.mounts.Concat(joust.shields.Select(s=>s.transform)).ToArray();
                if(match is LWS.TruckDerby.DerbyMatch derby){framing.trucks=Array.Empty<SportsEventTruck>();framing.bodies=derby.fighters.Select(f=>f.body).ToArray();framing.modelRoots=derby.fighters.Select(f=>f.intact).ToArray();framing.bodyShapes=derby.fighters.Select(f=>f.body.GetComponentsInChildren<Collider>().First(c=>!c.isTrigger&&c.attachedRigidbody==f.body)).ToArray();}
                if(framing.SubjectCount==0)throw new InvalidOperationException(sceneName+" no explicit truck roster");
                if(created)
                {
                    framing.useOrthographic=camera.view.orthographic;
                    // Keepaway's established elevated angle is the collection reference.
                    framing.settings.elevation=58;framing.settings.azimuth=0;
                    if(sceneName=="TruckKickball"){framing.useOrthographic=false;framing.settings.azimuth=-90;framing.settings.elevation=42;framing.settings.nearbyTruckLengths=4;}
                    if(sceneName=="TruckVolleyball")framing.settings.nearbyTruckLengths=5.5f;
                }
                if(framing.authoringVersion==0){if(sceneName=="TruckKickball")framing.settings.azimuth=-90;}
                if(framing.authoringVersion<2){framing.sharedDefaults=Profile();framing.useSharedDefaults=sceneName!="TruckKickball"&&sceneName!="TruckVolleyball";framing.authoringVersion=2;}
                if(framing.authoringVersion<3){if(sceneName=="TruckKingOfTheHill"){framing.useSharedDefaults=false;framing.settings.objectiveWeight=6;framing.settings.primaryWeight=1;framing.settings.elevation=68;framing.settings.minimumTruckHeight=.07f;framing.settings.preferredTruckHeight=.10f;}framing.authoringVersion=3;}
                if(framing.authoringVersion<4){if(sceneName=="TruckKingOfTheHill")framing.settings.primaryWeight=6;framing.authoringVersion=4;}
                if(framing.authoringVersion<5){if(sceneName=="TruckKingOfTheHill"){framing.settings.groupFramingSize=.70f;framing.settings.screenY=.55f;}framing.authoringVersion=5;}
                if(framing.authoringVersion<6){if(sceneName=="TruckKingOfTheHill")framing.settings.maximumOrthoInTruckLengths=7.5f;framing.authoringVersion=6;}
                if(framing.authoringVersion<7){if(sceneName=="TruckKingOfTheHill")framing.settings.screenY=.43f;framing.authoringVersion=7;}
                var fit=camera.GetComponent<SportsCameraFit>();if(fit!=null)fit.broadcastFraming=framing;
                Guidance(framing);
                var driving=match.GetComponent<SportsDrivingViews>();
                if(driving!=null){var spectator=match.GetComponent<SportsExpansionSpectators>();if(spectator==null)spectator=match.gameObject.AddComponent<SportsExpansionSpectators>();spectator.match=match;spectator.views=driving;driving.rally.retiredViews=spectator;EditorUtility.SetDirty(spectator);EditorUtility.SetDirty(driving.rally);}
                EditorUtility.SetDirty(camera);EditorUtility.SetDirty(framing);if(fit!=null)EditorUtility.SetDirty(fit);
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
                report.AppendLine(sceneName+" | BOUND | "+framing.SubjectCount+" trucks | "+(framing.useOrthographic?"orthographic":"perspective")+" | primary minimum="+framing.settings.minimumTruckHeight+" preferred="+framing.settings.preferredTruckHeight);
            }
            Directory.CreateDirectory("Logs/TruckSports/PlaytestRepair");File.WriteAllText("Logs/TruckSports/PlaytestRepair/camera-scene-bindings.txt",report.ToString());
            AssetDatabase.SaveAssets();Debug.Log("SPORTS_BROADCAST_BINDINGS_COMPLETE");
        }
    }
}

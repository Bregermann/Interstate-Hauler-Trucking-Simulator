#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TMPro;
using LWS.TruckSports;
namespace LWS.TruckHorrorSports.Editor
{
    public static class GraveyardSecurityAuditAuthoring
    {
        const string RootName="Optional live CCTV missing-rig inspection";
        public static void AuthorSavedGraveyardSecurityAudit()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before saving the CCTV inspection.");
            var scene = EditorSceneManager.OpenScene(HorrorAuthoring.Scene(HorrorSport.GraveyardShift), OpenSceneMode.Single);
            UpgradeActiveVenue();
            if (scene.isDirty && !EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException("Could not save the existing Graveyard Shift venue.");
        }

        [MenuItem("Truck Sports/Alpha 0.10/Upgrade active Graveyard live security audit")]
        public static void UpgradeActiveVenue()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode before this explicit saved-scene addition.");
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.path!=HorrorAuthoring.Scene(HorrorSport.GraveyardShift))throw new InvalidOperationException("Open saved Graveyard Shift.");
            var match=UnityEngine.Object.FindObjectsByType<HorrorMatch>(FindObjectsSortMode.None).Single(m=>m.gameObject.scene==scene);var services=match.nightServices;var monitor=match.objectives[4].securityMonitor;
            if(services==null||monitor==null||monitor.securityCamera==null)throw new InvalidOperationException("Retain the original night services and live CCTV renderer.");
            var existing=scene.GetRootGameObjects().FirstOrDefault(go=>go.name==RootName);
            if(existing!=null){if(services.securityAudit!=existing.GetComponent<GraveyardSecurityAudit>())throw new InvalidOperationException("Preserve edited audit equipment; restore its original service binding.");Debug.Log("GRAVEYARD_SECURITY_AUDIT_PRESERVED");return;}
            var bays=match.anomalies.Where(a=>a!=null&&a.kind==HorrorAnomalyKind.DisappearingTruck&&a.subject!=null).Select(a=>a.subject).OrderBy(go=>go.transform.position.x).ToArray();
            if(bays.Length!=3||bays.Any(go=>go.GetComponentsInChildren<Renderer>(true).Length==0))throw new InvalidOperationException("Inspect the three original physical parked semis; do not substitute fake evidence.");
            var grip=Need<Mesh>("Assets/TruckHorrorSports/GraveyardShift/Models/Original service-control grip.asset");var metal=Need<Material>("Assets/TruckHorrorSports/GraveyardShift/Materials/Night service brushed metal.mat");var click=Need<AudioClip>("Assets/Heat - Complete Modern UI/Audio/Click.wav");var theme=Need<SportsBroadcastTheme>("Assets/TruckSports/Resources/SportsBroadcastTheme.asset");
            var root=new GameObject(RootName);var audit=root.AddComponent<GraveyardSecurityAudit>();audit.match=match;audit.monitor=monitor;audit.console=match.objectives[4].transform;audit.parkedBays=bays;
            audit.views=new Transform[3];audit.fieldOfViews=new[]{monitor.securityCamera.fieldOfView,80f,80f};audit.viewNames=new[]{"FUEL COURT","PARKED TRUCK BAYS","SERVICE ROAD"};
            audit.views[0]=Child(root.transform,"Original fuel-court CCTV pose",monitor.securityCamera.transform.position);audit.views[0].rotation=monitor.securityCamera.transform.rotation;
            Vector3 mean=Vector3.zero;foreach(var bay in bays)mean+=bay.transform.position;mean/=bays.Length;
            audit.views[1]=Child(root.transform,"Owned parked-bay CCTV pose",mean+new Vector3(0,14,26));audit.views[1].LookAt(mean+Vector3.up*2);
            Vector3 arrival=services.visitor.road[services.visitor.arrivalPoint].position;
            audit.views[2]=Child(root.transform,"Physical service-road CCTV pose",arrival+new Vector3(0,12,25));audit.views[2].LookAt(arrival+Vector3.up);
            var control=Child(root.transform,"Owned CCTV camera-selector control",audit.console.position+new Vector3(1.2f,1.2f,0));control.gameObject.AddComponent<MeshFilter>().sharedMesh=grip;control.gameObject.AddComponent<MeshRenderer>().sharedMaterial=metal;control.localScale=Vector3.one*.45f;audit.control=control;
            var sound=control.gameObject.AddComponent<AudioSource>();sound.playOnAwake=false;sound.clip=click;sound.spatialBlend=1;sound.minDistance=1;sound.maxDistance=15;sound.outputAudioMixerGroup=match.encounterAudio!=null?match.encounterAudio.outputAudioMixerGroup:null;audit.switchSound=sound;
            var label=Child(root.transform,"Current live feed and contextual inspection controls",monitor.transform.position+Vector3.down*1.25f).gameObject.AddComponent<TextMeshPro>();label.font=theme.headingFont;label.fontSize=1.5f;label.alignment=TextAlignmentOptions.Center;label.richText=false;label.rectTransform.sizeDelta=new Vector2(8,2);label.text="LIVE CCTV · FUEL COURT\nTAP ACTION: NEXT CAMERA · HOLD: REPORT MISSING RIG";var billboard=label.gameObject.AddComponent<SportsPerCameraBillboard>();billboard.distanceSizing=true;billboard.referenceDistance=10;billboard.maximumPixelWidth=380;billboard.maximumPixelHeight=75;billboard.maximumLabelDistance=24;audit.label=label;
            services.securityAudit=audit;Undo.RegisterCreatedObjectUndo(root,"Add contextual missing-rig CCTV inspection");EditorUtility.SetDirty(services);EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("GRAVEYARD_SECURITY_AUDIT_AUTHORED one existing real camera/RT, three pose presets, original parked subjects, owned control/audio/TMP. Six objectives/rigs/roads/nav data untouched. Caller saves and runs focused acceptance.");
        }
        static Transform Child(Transform parent,string name,Vector3 world){var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.position=world;return go.transform;}
        static T Need<T>(string path)where T:UnityEngine.Object{var asset=AssetDatabase.LoadAssetAtPath<T>(path);if(asset==null)throw new InvalidOperationException("Existing owned security-console asset missing: "+path);return asset;}
    }
}
#endif

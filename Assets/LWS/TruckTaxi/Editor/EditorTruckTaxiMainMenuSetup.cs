using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace LWS.TruckTaxi.Editor
{
    public static class EditorTruckTaxiMainMenuSetup
    {
        public const string ScenePath = "Assets/LWS/TruckTaxi/Scenes/TruckTaxi_MainMenu.unity";
        private const string CityPath = "Assets/LWS/TruckTaxi/Scenes/TruckTaxi_DemoCity.unity";
        private const string TractorPath = "Assets/LWS/TruckTaxi/Prefabs/TruckTaxi_InterstateTractor.prefab";
        private const string AudioPath = "Assets/LWS/TruckTaxi/Audio/TruckTaxiAudioRouting.asset";
        private const string ManagedRootName = "Truck Taxi Menu Generated";

        [MenuItem("Truck Taxi/Configure Main Menu")]
        public static void Configure()
        {
            for(int i=0;i<SceneManager.sceneCount;i++)
                if(SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("Save open scene changes before configuring the main menu.");
            var references=ReadCityHudReferences();
            var scene=AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath)!=null
                ? EditorSceneManager.OpenScene(ScenePath)
                : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            foreach(var root in scene.GetRootGameObjects())
                if(root.name==ManagedRootName) Object.DestroyImmediate(root);

            var managed=new GameObject(ManagedRootName);
            Stage(managed.transform);
            var cameraObject=new GameObject("Menu camera",typeof(Camera),typeof(AudioListener));
            cameraObject.tag="MainCamera";
            cameraObject.transform.SetParent(managed.transform,false);
            // Aim left of the display so the truck remains clear of the left-hand menu.
            cameraObject.transform.position=new Vector3(8,4.5f,10);
            cameraObject.transform.LookAt(new Vector3(4,1.5f,-1));
            var camera=cameraObject.GetComponent<Camera>();
            camera.clearFlags=CameraClearFlags.SolidColor;
            camera.backgroundColor=new Color(.08f,.16f,.18f);
            camera.fieldOfView=42;
            camera.nearClipPlane=.1f;
            camera.farClipPlane=80;
            camera.allowHDR=false;
            foreach(var root in scene.GetRootGameObjects())
                foreach(var listener in root.GetComponentsInChildren<AudioListener>(true))
                    if(listener!=cameraObject.GetComponent<AudioListener>()) listener.enabled=false;

            var lightObject=new GameObject("Menu key light",typeof(Light));
            lightObject.transform.SetParent(managed.transform,false);
            lightObject.transform.rotation=Quaternion.Euler(40,-40,0);
            var light=lightObject.GetComponent<Light>();
            light.type=LightType.Directional; light.intensity=1.6f;

            var canvasObject=new GameObject("Menu canvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(managed.transform,false);
            var canvas=canvasObject.GetComponent<Canvas>();
            canvas.renderMode=RenderMode.ScreenSpaceOverlay;
            var scaler=canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution=new Vector2(1920,1080);
            scaler.matchWidthOrHeight=.5f;
            var eventObject=new GameObject("Menu EventSystem",typeof(EventSystem));
            eventObject.transform.SetParent(managed.transform,false);

            var controllerObject=new GameObject("Truck Taxi Main Menu");
            controllerObject.transform.SetParent(managed.transform,false);
            var hud=controllerObject.AddComponent<TruckTaxiHud>();
            hud.heatButtonPrefab=references.button;
            hud.heatSliderPrefab=references.slider;
            hud.heatSwitchPrefab=references.toggle;
            hud.font=references.font;
            var controller=controllerObject.AddComponent<TruckTaxiMainMenu>();
            var audio=AssetDatabase.LoadAssetAtPath<TruckTaxiAudioConfiguration>(AudioPath);
            if(audio==null) throw new InvalidOperationException("Truck Taxi audio routing asset is missing.");
            var serialized=new SerializedObject(controller);
            serialized.FindProperty("viewRoot").objectReferenceValue=canvasObject.GetComponent<RectTransform>();
            serialized.FindProperty("view").objectReferenceValue=hud;
            serialized.FindProperty("audioConfiguration").objectReferenceValue=audio;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            if(scene.path==string.Empty) EditorSceneManager.SaveScene(scene,ScenePath);
            else { EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); }
            Debug.Log("Truck Taxi main menu configured. Add this scene before TruckTaxi_DemoCity in Build Settings.");
        }

        private static (GameObject button,GameObject slider,GameObject toggle,TMP_FontAsset font) ReadCityHudReferences()
        {
            var preview=EditorSceneManager.OpenPreviewScene(CityPath);
            try
            {
                var hud=preview.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<TruckTaxiHud>(true)).FirstOrDefault();
                if(hud==null || hud.heatButtonPrefab==null || hud.heatSliderPrefab==null || hud.heatSwitchPrefab==null)
                    throw new InvalidOperationException("Demo City HUD must have its serialized Heat references configured first.");
                return (hud.heatButtonPrefab,hud.heatSliderPrefab,hud.heatSwitchPrefab,hud.font);
            }
            finally { EditorSceneManager.ClosePreviewScene(preview); }
        }

        private static void Stage(Transform parent)
        {
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name="Display plinth";
            floor.transform.SetParent(parent,false);
            floor.transform.position=new Vector3(0,-.4f,0);
            floor.transform.localScale=new Vector3(18,.5f,14);
            var concrete=AssetDatabase.LoadAssetAtPath<Material>("Assets/LWS/TruckTaxi/Materials/Concrete.mat");
            if(concrete!=null) floor.GetComponent<Renderer>().sharedMaterial=concrete;
            Object.DestroyImmediate(floor.GetComponent<Collider>());

            var truck=AssetDatabase.LoadAssetAtPath<GameObject>(TractorPath);
            if(truck==null) throw new InvalidOperationException("Truck Taxi tractor prefab is missing.");
            var display=(GameObject)PrefabUtility.InstantiatePrefab(truck);
            display.name="Tractor display only";
            display.transform.SetParent(parent,false);
            display.transform.localPosition=new Vector3(1,0,0);
            display.transform.localRotation=Quaternion.Euler(0,-18,0);
            StripToVisual(display);

            var profiles=AssetDatabase.FindAssets("t:PassengerProfile",new[]{"Assets/LWS/TruckTaxi/Passengers/Profiles"})
                .Select(g=>AssetDatabase.LoadAssetAtPath<PassengerProfile>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(p=>p!=null && p.runtimePrefab!=null && p.appearance!=null &&
                    p.appearance.visualStyle==TruckTaxiVisualStyle.WobblePeople)
                .OrderBy(p=>p.passengerId).Take(4).ToArray();
            if(profiles.Length<3) throw new InvalidOperationException("Build at least three Wobble passenger runtime prefabs before configuring the menu.");
            var positions=new[]{new Vector3(1,0,4),new Vector3(3,0,3.4f),
                new Vector3(6,0,1.5f),new Vector3(5,0,-2)};
            var actions=new[]{TruckTaxiPassengerAnimation.Idle_Normal,TruckTaxiPassengerAnimation.Idle_Impatient,
                TruckTaxiPassengerAnimation.Idle_Excited,TruckTaxiPassengerAnimation.Idle_Nervous};
            for(int i=0;i<profiles.Length;i++)
            {
                var person=new GameObject();
                person.name="Display "+profiles[i].passengerName;
                person.transform.SetParent(parent,false);
                person.transform.localPosition=positions[i];
                person.transform.LookAt(new Vector3(8,person.transform.position.y,10));
                var model=(GameObject)PrefabUtility.InstantiatePrefab(profiles[i].runtimePrefab,person.transform);
                model.name=profiles[i].passengerName+" visual";
                StripToVisual(model,false);
                var wobble=model.transform.Find(TruckTaxiWobbleVisual.VisualName);
                var animator=model.GetComponentInChildren<Animator>(true);
                var visual=wobble!=null ? wobble : animator!=null ? animator.transform : model.transform;
                var idle=person.AddComponent<TruckTaxiMainMenuIdle>();
                idle.Configure(animator,actions[i].ToString(),i*.21f,visual);
            }
        }

        private static void StripToVisual(GameObject root,bool enableRenderers=true)
        {
            if(PrefabUtility.IsPartOfPrefabInstance(root))
                PrefabUtility.UnpackPrefabInstance(root,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
            var removable=root.GetComponentsInChildren<Component>(true).Where(c=>c!=null &&
                !(c is Transform) && !(c is Renderer) && !(c is MeshFilter) &&
                !(c is Animator) && !(c is LODGroup)).ToList();
            while(removable.Count>0)
            {
                // Strip dependants before Rigidbody/Collider components required by them.
                var component=removable.FirstOrDefault(candidate=>!removable.Any(other=>
                    other!=candidate && other.gameObject==candidate.gameObject &&
                    other.GetType().GetCustomAttributes(typeof(RequireComponent),true)
                        .Cast<RequireComponent>().Any(r=>
                            r.m_Type0?.IsAssignableFrom(candidate.GetType())==true ||
                            r.m_Type1?.IsAssignableFrom(candidate.GetType())==true ||
                            r.m_Type2?.IsAssignableFrom(candidate.GetType())==true)));
                if(component==null) throw new InvalidOperationException("Cyclic component dependency in menu visual: "+root.name);
                removable.Remove(component); Object.DestroyImmediate(component);
            }
            foreach(var animator in root.GetComponentsInChildren<Animator>(true))
            { animator.enabled=true; animator.applyRootMotion=false; }
            if(enableRenderers)
                foreach(var renderer in root.GetComponentsInChildren<Renderer>(true)) renderer.enabled=true;
            root.SetActive(true);
        }
    }
}

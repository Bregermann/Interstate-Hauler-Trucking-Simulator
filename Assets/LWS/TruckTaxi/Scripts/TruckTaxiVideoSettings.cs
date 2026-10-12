using System;
using System.Collections.Generic;
using LWS.InterstateHauler;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace LWS.TruckTaxi
{
    // Machine-local presentation preferences, never career/progression save data.
    [Serializable]
    public sealed class TruckTaxiVideoSettings
    {
        public const string PreferenceKey="ih.truckTaxi.video.v1";
        public int width=1920,height=1080,mode=1,quality=2,frameCap=60,msaa=2,textureLimit;
        public bool vsync=true,shadows=true,reflections=true,postProcessing=true;
        public float renderScale=1,shadowDistance=150;
        public int weatherQuality=1;
        private static readonly Dictionary<(int,UniversalRenderPipelineAsset),UniversalRenderPipelineAsset> ownedPipelines =
            new Dictionary<(int,UniversalRenderPipelineAsset),UniversalRenderPipelineAsset>();
        private static readonly Dictionary<int,RenderPipelineAsset> originalPipelines = new Dictionary<int,RenderPipelineAsset>();
        private struct CameraState
        {
            public UniversalAdditionalCameraData data;
            public bool created, previous, applied;
        }
        private static readonly Dictionary<Camera,CameraState> cameraStates = new Dictionary<Camera,CameraState>();
        private static TruckTaxiVideoSettings appliedSettings;
        private static int gameplayScene = -1;
        private static bool startupApplied;
        private static bool retiring;
        public static bool PostProcessingAllowed => !retiring && (appliedSettings ?? Load()).postProcessing;
        public TruckTaxiVideoSettings Copy() => (TruckTaxiVideoSettings)MemberwiseClone();
        public FullScreenMode WindowMode => mode==0 ? FullScreenMode.Windowed : mode==1 ? FullScreenMode.FullScreenWindow : FullScreenMode.ExclusiveFullScreen;

        public void Validate()
        {
            width=Mathf.Clamp(width,640,7680);height=Mathf.Clamp(height,360,4320);mode=Mathf.Clamp(mode,0,2);
            quality=Mathf.Clamp(quality,0,Mathf.Max(0,QualitySettings.names.Length-1));
            frameCap=Mathf.Clamp(frameCap,30,240);textureLimit=Mathf.Clamp(textureLimit,0,3);
            if(msaa!=1 && msaa!=2 && msaa!=4 && msaa!=8) msaa=2;
            renderScale=float.IsFinite(renderScale) ? Mathf.Clamp(renderScale,.5f,1.5f) : 1;
            shadowDistance=float.IsFinite(shadowDistance) ? Mathf.Clamp(shadowDistance,0,400) : 150;
            weatherQuality=Mathf.Clamp(weatherQuality,0,3);
        }
        public static TruckTaxiVideoSettings Capture()
        {
            var pipeline=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            return new TruckTaxiVideoSettings{width=Screen.width,height=Screen.height,
                mode=Screen.fullScreenMode==FullScreenMode.Windowed ? 0 : Screen.fullScreenMode==FullScreenMode.FullScreenWindow ? 1 : 2,
                quality=QualitySettings.GetQualityLevel(),frameCap=Application.targetFrameRate>0 ? Application.targetFrameRate : 60,
                vsync=QualitySettings.vSyncCount>0,msaa=pipeline!=null ? pipeline.msaaSampleCount : 1,
                renderScale=pipeline!=null ? pipeline.renderScale : 1,shadows=pipeline==null || pipeline.supportsMainLightShadows && pipeline.shadowDistance>0,
                shadowDistance=pipeline!=null && pipeline.shadowDistance>0 ? pipeline.shadowDistance : Load().shadowDistance,
                reflections=QualitySettings.realtimeReflectionProbes,textureLimit=QualitySettings.globalTextureMipmapLimit,
                postProcessing=PostProcessingAllowed,weatherQuality=(appliedSettings ?? Load()).weatherQuality};
        }
        public static TruckTaxiVideoSettings Load()
        {
            TruckTaxiVideoSettings value=null;
            try { if(PlayerPrefs.HasKey(PreferenceKey)) value=JsonUtility.FromJson<TruckTaxiVideoSettings>(PlayerPrefs.GetString(PreferenceKey)); }
            catch(ArgumentException) { }
            if(value==null)
            {
                var desktop=Screen.currentResolution;
                value=new TruckTaxiVideoSettings{width=desktop.width>=640 ? desktop.width : 1920,
                    height=desktop.height>=360 ? desktop.height : 1080,quality=QualitySettings.GetQualityLevel()};
            }
            value.Validate();return value;
        }
        public void Save() { Validate();PlayerPrefs.SetString(PreferenceKey,JsonUtility.ToJson(this));PlayerPrefs.Save(); }
        public static void ApplyStartup(TruckTaxiBootstrap host=null)
        {
            var preferences=Load();
            if(!startupApplied)
            {
                startupApplied=true;
                // Explicit launch dimensions belong to test/launcher callers, not saved preferences.
                string[] args=Environment.GetCommandLineArgs();
                bool explicitSize=Array.IndexOf(args,"-screen-width")>=0 || Array.IndexOf(args,"-screen-height")>=0;
                preferences.Apply(host,!Application.isEditor && !explicitSize);
            }
            else preferences.Apply(host,false);
        }
        public void Apply(TruckTaxiBootstrap host,bool display=true)
        {
            Validate();
            if(display) Screen.SetResolution(width,height,WindowMode);
            QualitySettings.SetQualityLevel(quality,true);
            if(!originalPipelines.ContainsKey(quality)) originalPipelines.Add(quality,QualitySettings.renderPipeline);
            var weatherSource=host!=null && host.Environment!=null && host.Environment.settings!=null
                ? host.Environment.settings.weatherRenderPipeline as UniversalRenderPipelineAsset : null;
            var source=weatherSource!=null ? weatherSource :
                (originalPipelines[quality]!=null ? originalPipelines[quality] : GraphicsSettings.defaultRenderPipeline) as UniversalRenderPipelineAsset;
            if(source!=null)
            {
                var ownedPipeline=ConfigurePipeline(quality,source);
                QualitySettings.renderPipeline=ownedPipeline;
            }
            QualitySettings.vSyncCount=vsync ? 1 : 0;Application.targetFrameRate=frameCap;
            QualitySettings.globalTextureMipmapLimit=textureLimit;QualitySettings.realtimeReflectionProbes=reflections;
            ApplyScene(host);
        }
        public void ApplyScene(TruckTaxiBootstrap host)
        {
            retiring=false;
            appliedSettings=Copy();
            gameplayScene=host!=null ? host.gameObject.scene.handle : -1;
            RenderPipelineManager.beginCameraRendering-=OnBeginCameraRendering;
            RenderPipelineManager.beginCameraRendering+=OnBeginCameraRendering;
            SceneManager.sceneUnloaded-=OnSceneUnloaded; SceneManager.sceneUnloaded+=OnSceneUnloaded;
            Application.quitting-=ResetStatics; Application.quitting+=ResetStatics;
            // A settings transaction/scene binding, not a per-frame camera search.
            foreach(var camera in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                ApplyCameraPreference(camera);
            host?.Environment?.weatherAdapter?.ApplyQualityTier((LwsRenderQualityTier)weatherQuality);
        }
        private UniversalRenderPipelineAsset ConfigurePipeline(int slot,UniversalRenderPipelineAsset source)
        {
            var key=(slot,source);
            if(!ownedPipelines.TryGetValue(key,out var clone) || clone==null)
            {
                clone=UnityEngine.Object.Instantiate(source);
                clone.name="Truck Taxi local video settings"; clone.hideFlags=HideFlags.DontSave;
                ownedPipelines[key]=clone;
            }
            clone.renderScale=renderScale; clone.msaaSampleCount=msaa;
            clone.shadowDistance=shadows ? shadowDistance : 0;
            return clone;
        }
        public static void ApplyCameraPreference(Camera camera)
        {
            if(retiring || camera==null || camera.targetTexture!=null || camera.cameraType!=CameraType.Game) return;
            var data=camera.GetComponent<UniversalAdditionalCameraData>();
            bool created=data==null;
            if(created) { data=camera.gameObject.AddComponent<UniversalAdditionalCameraData>(); data.hideFlags=HideFlags.DontSave; }
            if(!cameraStates.TryGetValue(camera,out var state) || state.data!=data)
                state=new CameraState { data=data, created=created, previous=data.renderPostProcessing };
            state.applied=PostProcessingAllowed;
            data.renderPostProcessing=state.applied;
            cameraStates[camera]=state;
        }
        private static void OnBeginCameraRendering(ScriptableRenderContext context,Camera camera) => ApplyCameraPreference(camera);
        private static void OnSceneUnloaded(Scene scene)
        {
            if(scene.handle==gameplayScene) ResetStatics();
            else
            {
                var removed=new List<Camera>();
                foreach(var pair in cameraStates) if(pair.Key==null) removed.Add(pair.Key);
                foreach(var camera in removed) cameraStates.Remove(camera);
            }
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            retiring=true;
            RenderPipelineManager.beginCameraRendering-=OnBeginCameraRendering;
            SceneManager.sceneUnloaded-=OnSceneUnloaded; Application.quitting-=ResetStatics;
            int selected=QualitySettings.GetQualityLevel();
            // Unbind every owned clone before destroying it, including inactive quality slots.
            foreach(var pair in originalPipelines)
            {
                if(pair.Key>=QualitySettings.names.Length) continue;
                var current=QualitySettings.GetRenderPipelineAssetAt(pair.Key);
                bool owned=false;
                foreach(var clone in ownedPipelines.Values) if(clone!=null && current==clone) { owned=true; break; }
                if(!owned) continue;
                QualitySettings.SetQualityLevel(pair.Key,false); QualitySettings.renderPipeline=pair.Value;
            }
            if(QualitySettings.GetQualityLevel()!=selected) QualitySettings.SetQualityLevel(selected,false);
            foreach(var pair in cameraStates)
            {
                var state=pair.Value;
                if(state.data==null) continue;
                if(state.created) UnityEngine.Object.DestroyImmediate(state.data);
                else if(state.data.renderPostProcessing==state.applied) state.data.renderPostProcessing=state.previous;
            }
            cameraStates.Clear();
            foreach(var clone in ownedPipelines.Values) if(clone!=null) UnityEngine.Object.DestroyImmediate(clone);
            ownedPipelines.Clear(); originalPipelines.Clear(); appliedSettings=null; gameplayScene=-1; startupApplied=false;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CaptureOriginalPipelines()
        {
            // Capture before Taxi Environment temporarily installs its authored weather pipeline.
            for(int slot=0;slot<QualitySettings.names.Length;slot++)
                if(!originalPipelines.ContainsKey(slot)) originalPipelines.Add(slot,QualitySettings.GetRenderPipelineAssetAt(slot));
        }
#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        private static void RegisterEditorCleanup()
        {
            UnityEditor.EditorApplication.playModeStateChanged-=OnPlayModeStateChanged;
            UnityEditor.EditorApplication.playModeStateChanged+=OnPlayModeStateChanged;
        }
        private static void OnPlayModeStateChanged(UnityEditor.PlayModeStateChange state)
        { if(state==UnityEditor.PlayModeStateChange.ExitingPlayMode) ResetStatics(); }
#endif
    }
}

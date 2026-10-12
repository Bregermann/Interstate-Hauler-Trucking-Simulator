using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace LWS.TruckTaxi.Tests
{
    [Category("TaxiIntegrated")]
    public sealed class TruckTaxiVideoOwnershipTests
    {
        private const BindingFlags StaticPrivate = BindingFlags.Static | BindingFlags.NonPublic;
        private const BindingFlags InstancePrivate = BindingFlags.Instance | BindingFlags.NonPublic;
        private readonly List<Object> objects = new List<Object>();
        private int quality, vsync, mipmaps, frameRate;
        private bool reflections;
        private Type cameraDataType;
        private static void Reset() => typeof(TruckTaxiVideoSettings).GetMethod("ResetStatics", StaticPrivate).Invoke(null, null);
        private static void SetApplied(bool enabled)
        {
            typeof(TruckTaxiVideoSettings).GetField("retiring", StaticPrivate).SetValue(null, false);
            typeof(TruckTaxiVideoSettings).GetField("appliedSettings", StaticPrivate)
                .SetValue(null, new TruckTaxiVideoSettings { postProcessing = enabled });
        }
        [SetUp]
        public void SetUp()
        {
            Reset();
            quality = QualitySettings.GetQualityLevel(); vsync = QualitySettings.vSyncCount;
            mipmaps = QualitySettings.globalTextureMipmapLimit; reflections = QualitySettings.realtimeReflectionProbes;
            frameRate = Application.targetFrameRate;
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                if ((cameraDataType = assembly.GetType("UnityEngine.Rendering.Universal.UniversalAdditionalCameraData")) != null) break;
            Assert.IsNotNull(cameraDataType, "Installed URP camera API must be available.");
            typeof(TruckTaxiVideoSettings).GetMethod("CaptureOriginalPipelines", StaticPrivate).Invoke(null, null);
        }
        [TearDown]
        public void TearDown()
        {
            foreach (var item in objects)
                if (item is GameObject go && go != null)
                    foreach (var camera in go.GetComponentsInChildren<Camera>(true)) camera.targetTexture = null;
            for (int i = objects.Count - 1; i >= 0; i--) if (objects[i] != null) Object.DestroyImmediate(objects[i]);
            objects.Clear(); Reset();
            QualitySettings.SetQualityLevel(quality, false); QualitySettings.vSyncCount = vsync;
            QualitySettings.globalTextureMipmapLimit = mipmaps; QualitySettings.realtimeReflectionProbes = reflections;
            Application.targetFrameRate = frameRate;
        }
        private Camera Camera(string name, bool active = true)
        {
            var go = new GameObject(name); objects.Add(go);
            var camera = go.AddComponent<Camera>(); go.SetActive(active); return camera;
        }
        private Component Data(Camera camera) => camera.GetComponent(cameraDataType);
        private bool Post(Component data) => (bool)cameraDataType.GetProperty("renderPostProcessing").GetValue(data);
        private void Post(Component data, bool value) => cameraDataType.GetProperty("renderPostProcessing").SetValue(data, value);
        private int Mask(Component data) => ((LayerMask)cameraDataType.GetProperty("volumeLayerMask").GetValue(data)).value;
        private void Mask(Component data, int value) => cameraDataType.GetProperty("volumeLayerMask").SetValue(data, (LayerMask)value);
        private static RenderPipelineAsset Pipeline() => AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(
            "Assets/LWS/TruckTaxi/Rendering/TruckTaxi_Weather_RPAsset.asset");
        private static RenderPipelineAsset Clone(TruckTaxiVideoSettings settings, int slot, RenderPipelineAsset source) =>
            (RenderPipelineAsset)typeof(TruckTaxiVideoSettings).GetMethod("ConfigurePipeline", InstancePrivate)
                .Invoke(settings, new object[] { slot, source });

        [Test]
        public void ClonesRetainWeatherRendererAndNeverModifySourceAsset()
        {
            var source = Pipeline(); Assert.IsNotNull(source);
            string before = EditorJsonUtility.ToJson(source);
            var settings = new TruckTaxiVideoSettings { renderScale = .65f, msaa = 4, shadows = false };
            var clone = Clone(settings, quality, source);
            Assert.AreNotSame(source, clone);
            Assert.AreEqual(before, EditorJsonUtility.ToJson(source));
            Assert.AreSame(clone, Clone(settings, quality, source));
            var originalData = new SerializedObject(source).FindProperty("m_RendererDataList");
            var clonedData = new SerializedObject(clone).FindProperty("m_RendererDataList");
            Assert.AreEqual(originalData.arraySize, clonedData.arraySize);
            for (int i = 0; i < originalData.arraySize; i++)
                Assert.AreSame(originalData.GetArrayElementAtIndex(i).objectReferenceValue,
                    clonedData.GetArrayElementAtIndex(i).objectReferenceValue);
            Assert.AreEqual(.65f, (float)clone.GetType().GetProperty("renderScale").GetValue(clone), .001f);
            Assert.Zero((float)clone.GetType().GetProperty("shadowDistance").GetValue(clone));
        }
        [Test]
        public void GameplayApplyUsesWeatherSourceEvenWhenMenuCloneAlreadyExists()
        {
            var settings = new TruckTaxiVideoSettings { quality = quality, renderScale = .7f, msaa = 4 };
            settings.Apply(null, false);
            var menuClone = QualitySettings.renderPipeline;
            var go = new GameObject("video host"); go.SetActive(false); objects.Add(go);
            var host = go.AddComponent<TruckTaxiBootstrap>();
            var environment = go.AddComponent<TruckTaxiEnvironmentCoordinator>();
            var configuration = ScriptableObject.CreateInstance<TruckTaxiEnvironmentSettings>(); objects.Add(configuration);
            configuration.weatherRenderPipeline = Pipeline(); environment.settings = configuration;
            typeof(TruckTaxiBootstrap).GetProperty("Environment").SetValue(host, environment);
            QualitySettings.renderPipeline = configuration.weatherRenderPipeline;
            settings.Apply(host, false);
            Assert.AreSame(Clone(settings, quality, Pipeline()), QualitySettings.renderPipeline);
            Assert.AreEqual(.7f, (float)QualitySettings.renderPipeline.GetType().GetProperty("renderScale").GetValue(QualitySettings.renderPipeline), .001f);
            Assert.IsTrue(menuClone != null, "Switching source must not destroy another slot/source's live clone.");
        }
        [Test]
        public void ResetUnbindsActiveAndInactiveQualitySlotsBeforeDestroyingClones()
        {
            var originals = new Dictionary<int, RenderPipelineAsset>();
            var clones = new List<RenderPipelineAsset>();
            for (int i = 0; i < Math.Min(2, QualitySettings.names.Length); i++)
            {
                originals[i] = QualitySettings.GetRenderPipelineAssetAt(i);
                var clone = Clone(new TruckTaxiVideoSettings(), i, Pipeline()); clones.Add(clone);
                QualitySettings.SetQualityLevel(i, false); QualitySettings.renderPipeline = clone;
            }
            int selected = QualitySettings.GetQualityLevel();
            Reset();
            Assert.AreEqual(selected, QualitySettings.GetQualityLevel());
            foreach (var pair in originals) Assert.AreSame(pair.Value, QualitySettings.GetRenderPipelineAssetAt(pair.Key));
            foreach (var clone in clones) Assert.IsTrue(clone == null);
            Reset(); // Disabled-domain-reload cleanup must also be idempotent.
        }
        [Test]
        public void InactiveAndLateCreatedScreenCamerasInheritAppliedPreferenceWithoutTouchingGps()
        {
            var inactive = Camera("inactive screen", false);
            var gps = Camera("GPS RTT"); var texture = new RenderTexture(16,16,0); objects.Add(texture);
            gps.targetTexture = texture;
            var gpsData = gps.gameObject.AddComponent(cameraDataType); Post(gpsData, false); int gpsMask = Mask(gpsData);
            new TruckTaxiVideoSettings { postProcessing = true }.ApplyScene(null);
            Assert.IsTrue(Post(Data(inactive))); Assert.IsFalse(Post(gpsData)); Assert.AreEqual(gpsMask, Mask(gpsData));
            var late = Camera("late screen");
            var callback = typeof(TruckTaxiVideoSettings).GetMethod("OnBeginCameraRendering", StaticPrivate);
            callback.Invoke(null, new[] { Activator.CreateInstance(callback.GetParameters()[0].ParameterType), (object)late });
            Assert.IsTrue(Post(Data(late)));
            SetApplied(false); callback.Invoke(null, new[] { Activator.CreateInstance(callback.GetParameters()[0].ParameterType), (object)late });
            Assert.IsFalse(Post(Data(late)));
            Assert.IsFalse(TruckTaxiVideoSettings.Capture().postProcessing, "Capture must use the live draft, not stale PlayerPrefs.");
        }
        [Test]
        public void EffectEndRespectsNewPreferenceAndPreservesOtherVolumeMaskEdits()
        {
            var camera = Camera("effect screen");
            var data = camera.gameObject.AddComponent(cameraDataType); Post(data, false); Mask(data, 1);
            var effect = camera.gameObject.AddComponent<TruckTaxiEffectPresentation>(); effect.Initialize(new TruckTaxiTemporaryEffects());
            SetApplied(true);
            typeof(TruckTaxiEffectPresentation).GetMethod("BindCamera", InstancePrivate).Invoke(effect, new object[] { camera });
            Assert.IsTrue(Post(data));
            Mask(data, Mask(data) | 4);
            SetApplied(false); TruckTaxiVideoSettings.ApplyCameraPreference(camera);
            typeof(TruckTaxiEffectPresentation).GetMethod("BindCamera", InstancePrivate).Invoke(effect, new object[] { camera });
            Assert.IsFalse(Post(data), "An active effect must not force the preference back on.");
            typeof(TruckTaxiEffectPresentation).GetMethod("RestoreCameras", InstancePrivate).Invoke(effect, null);
            Assert.IsFalse(Post(data)); Assert.AreEqual(5, Mask(data)); Assert.AreSame(data, Data(camera));
            SetApplied(true); TruckTaxiVideoSettings.ApplyCameraPreference(camera);
            typeof(TruckTaxiEffectPresentation).GetMethod("RestoreCameras", InstancePrivate).Invoke(effect, null);
            Assert.IsTrue(Post(data), "An old effect baseline must not undo a later preference.");
        }
        [Test]
        public void EffectsNeverBindRenderTextureCamera()
        {
            var camera = Camera("effect GPS"); var texture = new RenderTexture(16,16,0); objects.Add(texture); camera.targetTexture = texture;
            var effect = camera.gameObject.AddComponent<TruckTaxiEffectPresentation>(); effect.Initialize(new TruckTaxiTemporaryEffects());
            SetApplied(true);
            typeof(TruckTaxiEffectPresentation).GetMethod("BindCamera", InstancePrivate).Invoke(effect, new object[] { camera });
            Assert.IsNull(Data(camera));
        }
    }
}

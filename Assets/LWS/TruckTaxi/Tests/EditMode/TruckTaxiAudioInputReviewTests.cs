using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace LWS.TruckTaxi.Tests
{
    public sealed class TruckTaxiAudioInputReviewTests
    {
        [Test]
        public void InvalidMixerConfigurationDoesNotRerouteSourcesOrWritePreferences()
        {
            var mixer=AssetDatabase.LoadAssetAtPath<AudioMixer>("Assets/Heat - Complete Modern UI/Audio/_Mixer.mixer");
            Assert.IsNotNull(mixer);
            var groups=mixer.FindMatchingGroups("");
            var master=groups.First(g=>g.name=="Master");
            var sfx=groups.First(g=>g.name=="SFX");
            var configuration=ScriptableObject.CreateInstance<TruckTaxiAudioConfiguration>();
            configuration.buses=new[]{new TruckTaxiAudioBus {category=TruckTaxiAudioCategory.Master,group=master,volumeParameter="InvalidTaxiTestParameter"}};
            var go=new GameObject("Audio configuration safety fixture");
            string key=TruckTaxiAudioController.PreferencePrefix+TruckTaxiAudioCategory.Master;
            bool existed=PlayerPrefs.HasKey(key); float previous=PlayerPrefs.GetFloat(key);
            try
            {
                PlayerPrefs.SetFloat(key,.37f);
                var source=go.AddComponent<AudioSource>(); source.volume=.42f; source.outputAudioMixerGroup=sfx;
                var controller=go.AddComponent<TruckTaxiAudioController>();
                LogAssert.Expect(LogType.Error,"Truck Taxi audio bus is missing its group/exposed volume: Master");
                controller.Initialize(configuration);
                Assert.IsFalse(controller.Ready);
                controller.SetVolume(TruckTaxiAudioCategory.Master,.9f);
                controller.ResetDefaults();
                LogAssert.Expect(LogType.Warning,"Truck Taxi audio category is not routed yet: Master");
                Assert.IsFalse(controller.Route(source,TruckTaxiAudioCategory.Master));
                Assert.AreSame(sfx,source.outputAudioMixerGroup);
                Assert.AreEqual(.42f,source.volume);
                Assert.AreEqual(.37f,PlayerPrefs.GetFloat(key));
            }
            finally
            {
                Object.DestroyImmediate(go); Object.DestroyImmediate(configuration);
                if(existed) PlayerPrefs.SetFloat(key,previous); else PlayerPrefs.DeleteKey(key);
                PlayerPrefs.Save();
            }
        }

        [Test]
        public void MissingBusArrayIsPendingRatherThanAnException()
        {
            var configuration=ScriptableObject.CreateInstance<TruckTaxiAudioConfiguration>(); configuration.buses=null;
            var go=new GameObject("Pending mixer fixture");
            try
            {
                var controller=go.AddComponent<TruckTaxiAudioController>();
                LogAssert.Expect(LogType.Warning,"Truck Taxi audio routing awaits the project AudioMixer configuration.");
                controller.Initialize(configuration);
                Assert.IsFalse(controller.Ready); Assert.AreEqual(0,controller.Buses.Count);
                Assert.IsNull(configuration.Find(TruckTaxiAudioCategory.Master));
            }
            finally { Object.DestroyImmediate(go); Object.DestroyImmediate(configuration); }
        }

        [TestCase(float.NaN)] [TestCase(float.PositiveInfinity)] [TestCase(float.NegativeInfinity)]
        public void NonFiniteAudioValuesNeverReachMixer(float value)
        {
            Assert.AreEqual(-80,TruckTaxiAudioController.ToDecibels(value));
            Assert.AreEqual(0,TruckTaxiAudioController.FromDecibels(value));
        }

        [Test]
        public void ModalFocusSkipsDisabledControlsAndRestoresOriginalModuleActions()
        {
            var events=new GameObject("Modal input test",typeof(EventSystem),typeof(InputSystemUIInputModule));
            var panel=new GameObject("Modal input test panel",typeof(RectTransform));
            TruckTaxiUIInput input=null;
            try
            {
                // EditMode does not register EventSystem through OnEnable. Do not alter global registration.
                var eventSystem=events.GetComponent<EventSystem>();
                var module=events.GetComponent<InputSystemUIInputModule>(); module.AssignDefaultActions();
                var previousMove=module.move; var previousSubmit=module.submit; var previousCancel=module.cancel;
                var a=new GameObject("A",typeof(RectTransform),typeof(Button)).GetComponent<Button>(); a.transform.SetParent(panel.transform);
                var b=new GameObject("B",typeof(RectTransform),typeof(Button)).GetComponent<Button>(); b.transform.SetParent(panel.transform);
                var c=new GameObject("C",typeof(RectTransform),typeof(Button)).GetComponent<Button>(); c.transform.SetParent(panel.transform);
                input=new TruckTaxiUIInput(eventSystem); input.Focus(panel.transform);
                Assert.AreSame(a.gameObject,eventSystem.currentSelectedGameObject);
                b.interactable=false; input.Focus(panel.transform);
                Assert.AreSame(c,a.navigation.selectOnDown,"Disabled middle row must not trap D-pad focus.");
                b.interactable=true; input.Focus(panel.transform);
                Assert.AreSame(b,a.navigation.selectOnDown,"Re-enabled row must be reachable without reopening the menu.");
                a.enabled=false; input.Focus(panel.transform);
                Assert.AreSame(b.gameObject,eventSystem.currentSelectedGameObject);
                input.Focus(null); Assert.IsNull(eventSystem.currentSelectedGameObject);
                input.Dispose(); input=null;
                Assert.AreSame(previousMove,module.move); Assert.AreSame(previousSubmit,module.submit); Assert.AreSame(previousCancel,module.cancel);
            }
            finally
            {
                input?.Dispose(); Object.DestroyImmediate(panel); Object.DestroyImmediate(events);
            }
        }
    }
}

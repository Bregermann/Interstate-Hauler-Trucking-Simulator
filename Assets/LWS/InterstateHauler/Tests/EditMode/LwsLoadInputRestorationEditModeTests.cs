using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace LWS.InterstateHauler.Tests.EditMode
{
    public sealed class LwsLoadInputRestorationEditModeTests
    {
        private const string SaveLoadCoordinatorPath = "Assets/LWS/InterstateHauler/Save/LwsSaveLoadCoordinator.cs";

        [TestCase(LwsVehicleInputOwner.KeyboardMouse)]
        [TestCase(LwsVehicleInputOwner.Wheel)]
        public void CoordinatorPhasesPreserveDeviceOwnershipAndGateNwhInput(LwsVehicleInputOwner owner)
        {
            var registry=new LwsServiceRegistry();
            var gameplay=CreateInitializedService();
            var input=new LwsVehicleInputService();
            registry.Register<ILwsGameplayStateService>(gameplay);
            registry.Register<ILwsVehicleInputService>(input);
            var source=new LoadTestInputSource();
            Assert.IsTrue(input.SetInputSource(source,owner,true).Succeeded);
            Assert.IsTrue(gameplay.EnterFreeDrive("test ready").Succeeded);
            var coordinator=new LwsSaveLoadCoordinator(null,()=>registry);
            var setPhase=typeof(LwsSaveLoadCoordinator).GetMethod("SetPhase",
                System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
            var go=new GameObject("load input ownership test");
            try
            {
                Component provider=go.AddComponent(typeof(LwsNwhVehicleInputProvider));
                typeof(LwsNwhVehicleInputProvider).GetMethod("SetInputSource").Invoke(provider,new object[]{input});
                typeof(LwsNwhVehicleInputProvider).GetField("_gameplayStateService",
                    System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(provider,gameplay);
                typeof(LwsNwhVehicleInputProvider).GetField("logDriveInputDiagnostics",
                    System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(provider,false);
                typeof(LwsNwhVehicleInputProvider).GetField("truckControlController",
                    System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(provider,null);
                typeof(LwsNwhVehicleInputProvider).GetField("_truckControlLookupAttempted",
                    System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(provider,true);
                typeof(LwsNwhVehicleInputProvider).GetMethod("SetValidationGearMappingEnabled").Invoke(provider,new object[]{true});
                Assert.AreEqual(.75f,ReadThrottle(provider));
                AssertFallbackControls(provider,source,true);
                foreach(LwsSaveLoadCoordinatorPhase phase in Enum.GetValues(typeof(LwsSaveLoadCoordinatorPhase)))
                {
                    if(phase==LwsSaveLoadCoordinatorPhase.Idle || phase==LwsSaveLoadCoordinatorPhase.Complete ||
                        phase==LwsSaveLoadCoordinatorPhase.Failed) continue;
                    setPhase.Invoke(coordinator,new object[]{phase});
                    Assert.AreEqual(LwsGameplayState.LoadingWorld,gameplay.CurrentState);
                    Assert.IsFalse(gameplay.AllowsDrivingInput);
                    Assert.AreEqual(0,ReadThrottle(provider),phase.ToString());
                    AssertFallbackControls(provider,source,false);
                    Assert.AreEqual(owner,input.ActiveOwner); Assert.AreEqual(source.SourceId,input.ActiveSourceId);
                }
                setPhase.Invoke(coordinator,new object[]{LwsSaveLoadCoordinatorPhase.Complete});
                Assert.IsTrue(gameplay.AllowsDrivingInput); Assert.AreEqual(.75f,ReadThrottle(provider));
                AssertFallbackControls(provider,source,true);

                // Exercise the actual coordinator failure path, not just its phase mapping.
                setPhase.Invoke(coordinator,new object[]{LwsSaveLoadCoordinatorPhase.ReadingVendorSlot});
                Assert.AreEqual(LwsGameplayState.LoadingWorld,gameplay.CurrentState);
                Assert.IsFalse(coordinator.LoadPreparedVendorSlot(null).Succeeded);
                Assert.AreEqual(LwsGameplayState.RecoveryError,gameplay.CurrentState);
                Assert.IsFalse(gameplay.AllowsDrivingInput); Assert.AreEqual(0,ReadThrottle(provider));
                AssertFallbackControls(provider,source,false);
                Assert.AreEqual(owner,input.ActiveOwner); Assert.AreEqual(source.SourceId,input.ActiveSourceId);
                setPhase.Invoke(coordinator,new object[]{LwsSaveLoadCoordinatorPhase.PreparingGameplaySuppression});
                AssertFallbackControls(provider,source,false);
                setPhase.Invoke(coordinator,new object[]{LwsSaveLoadCoordinatorPhase.Complete});
                Assert.AreEqual(.75f,ReadThrottle(provider));
                AssertFallbackControls(provider,source,true);
                Assert.IsTrue(gameplay.Pause("post-load menu").Succeeded); Assert.AreEqual(0,ReadThrottle(provider));
                AssertFallbackControls(provider,source,false);
                Assert.IsTrue(gameplay.Resume("post-load resume").Succeeded); Assert.AreEqual(.75f,ReadThrottle(provider));
                AssertFallbackControls(provider,source,true);
                Assert.AreEqual(owner,input.ActiveOwner); Assert.AreEqual(source.SourceId,input.ActiveSourceId);
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }

        [Test] public void CoordinatorDoesNotReleaseInputOwnershipDuringTemporarySuppression()
        {
            StringAssert.DoesNotContain("NeutralizeInput",File.ReadAllText(SaveLoadCoordinatorPath));
        }

        [Test] public void DestroyedKeyboardOwnerReleasesServiceForReplacement()
        {
            var input=new LwsVehicleInputService();
            var oldObject=new GameObject("old input owner");
            var newObject=new GameObject("replacement input owner");
            try
            {
                var old=oldObject.AddComponent<LwsKeyboardGamepadTruckInputSource>();
                var replacement=newObject.AddComponent<LwsKeyboardGamepadTruckInputSource>();
                BindInputService(old,input); BindInputService(replacement,input);
                Assert.IsTrue(input.SetInputSource(old,LwsVehicleInputOwner.KeyboardMouse,true).Succeeded);
                DestroyInputSource(old);
                Assert.IsFalse(input.HasActiveSource);
                Assert.AreEqual(LwsVehicleInputOwner.None,input.ActiveOwner);
                Assert.AreEqual(0,input.ReadContinuousInput().throttle);
                // Start's fallback registration can now acquire the persistent service.
                Assert.IsTrue(input.SetInputSource(replacement,LwsVehicleInputOwner.KeyboardMouse,true).Succeeded);
                replacement.SetSimulatedFrameForTests(default,new LwsVehicleContinuousInput { throttle=.4f });
                Assert.IsTrue(input.HasActiveSource);
                Assert.AreEqual(.4f,input.ReadContinuousInput().throttle);
                DestroyInputSource(replacement);
                Assert.IsFalse(input.HasActiveSource);
            }
            finally { UnityEngine.Object.DestroyImmediate(oldObject); UnityEngine.Object.DestroyImmediate(newObject); }
        }

        [TestCase(LwsVehicleInputOwner.Wheel)]
        [TestCase(LwsVehicleInputOwner.KeyboardMouse)]
        public void DestroyingOldKeyboardSourceCannotReleaseNewOwner(LwsVehicleInputOwner owner)
        {
            var input=new LwsVehicleInputService();
            var oldObject=new GameObject("superseded input");
            var newObject=new GameObject("replacement input");
            try
            {
                var old=oldObject.AddComponent<LwsKeyboardGamepadTruckInputSource>();
                BindInputService(old,input);
                Assert.IsTrue(input.SetInputSource(old,LwsVehicleInputOwner.KeyboardMouse,true).Succeeded);
                ILwsVehicleInputSource replacement;
                if(owner==LwsVehicleInputOwner.Wheel) replacement=new LoadTestInputSource();
                else
                {
                    var keyboard=newObject.AddComponent<LwsKeyboardGamepadTruckInputSource>();
                    BindInputService(keyboard,input);
                    keyboard.SetSimulatedFrameForTests(default,new LwsVehicleContinuousInput { throttle=.75f });
                    replacement=keyboard;
                }
                Assert.IsTrue(input.SetInputSource(replacement,owner,true).Succeeded);
                DestroyInputSource(old);
                Assert.IsTrue(input.HasActiveSource);
                Assert.AreEqual(owner,input.ActiveOwner);
                Assert.AreEqual(replacement.SourceId,input.ActiveSourceId);
                Assert.AreEqual(.75f,input.ReadContinuousInput().throttle);
            }
            finally { UnityEngine.Object.DestroyImmediate(oldObject); UnityEngine.Object.DestroyImmediate(newObject); }
        }

        private static void BindInputService(LwsKeyboardGamepadTruckInputSource source,ILwsVehicleInputService service) =>
            typeof(LwsKeyboardGamepadTruckInputSource).GetField("_inputService",
                System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(source,service);

        private static void DestroyInputSource(LwsKeyboardGamepadTruckInputSource source)
        {
            // EditMode objects may never have entered Unity's runtime lifecycle.
            typeof(LwsKeyboardGamepadTruckInputSource).GetMethod("OnDestroy",
                System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(source,null);
            UnityEngine.Object.DestroyImmediate(source);
        }

        private sealed class LoadTestInputSource : ILwsVehicleInputSource
        {
            public int CommandReads, GearReads;
            public string SourceId => "test.load.device";
            public LwsVehicleContinuousInput ReadContinuousInput() => new LwsVehicleContinuousInput { throttle=.75f };
            public LwsVehicleCommandFrame ReadCommandFrame()
            {
                CommandReads++;
                return new LwsVehicleCommandFrame {
                    ignition=LwsMomentaryIntent.Pressed,horn=LwsMomentaryIntent.Pressed,
                    lowBeamLights=LwsMomentaryIntent.Pressed,highBeamLights=LwsMomentaryIntent.Pressed,
                    hazardLights=LwsMomentaryIntent.Pressed,leftIndicator=LwsMomentaryIntent.Pressed,
                    rightIndicator=LwsMomentaryIntent.Pressed,cruiseControl=LwsMomentaryIntent.Pressed,
                    trailerAttachDetach=LwsMomentaryIntent.Pressed
                };
            }
            public LwsTruckGearIntent ReadGearIntent()
            { GearReads++; return new LwsTruckGearIntent { requestedLogicalGear=2 }; }
        }

        private static void AssertFallbackControls(Component provider,LoadTestInputSource source,bool allowed)
        {
            int commands=source.CommandReads, gears=source.GearReads;
            foreach(string method in new[]{"EngineStartStop","Horn","LowBeamLights","HighBeamLights",
                "HazardLights","LeftBlinker","RightBlinker","CruiseControl","TrailerAttachDetach"})
                Assert.AreEqual(allowed,(bool)typeof(LwsNwhVehicleInputProvider).GetMethod(method).Invoke(provider,null),method);
            Assert.AreEqual(allowed ? 2 : -999,
                (int)typeof(LwsNwhVehicleInputProvider).GetMethod("ShiftInto").Invoke(provider,null));
            Assert.AreEqual(commands+(allowed ? 9 : 0),source.CommandReads,"Blocked commands must not consume the source frame.");
            Assert.AreEqual(gears+(allowed ? 1 : 0),source.GearReads,"Blocked shifting must not consume gear intent.");
        }

        private static float ReadThrottle(Component provider) =>
            (float)typeof(LwsNwhVehicleInputProvider).GetMethod("Throttle").Invoke(provider,null);

        private static LwsGameplayStateService CreateInitializedService()
        {
            var service = new LwsGameplayStateService();
            LwsServiceResult result = service.Initialize(new LwsServiceContext(new LwsServiceRegistry()));
            Assert.IsTrue(result.Succeeded, result.Message);
            return service;
        }
    }
}

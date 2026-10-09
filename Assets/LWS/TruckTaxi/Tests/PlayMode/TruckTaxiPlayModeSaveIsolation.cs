using System;
using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace LWS.TruckTaxi.Tests
{
    // Scene-starting fixtures must never discover or write the player's normal vendor slots.
    [SetUpFixture]
    public sealed class TruckTaxiPlayModeSaveIsolation
    {
        private Component owner;
        private string root;

        public static IEnumerator CompleteNewRunIntroduction(TruckTaxiBootstrap host)
        {
            Assert.IsNotNull(host);
            float deadline=Time.realtimeSinceStartup+60f;
            while((!host.StartupComplete || host.Persistence?.IsBusy==true) && Time.realtimeSinceStartup<deadline)
                yield return null;
            Assert.IsTrue(host.StartupComplete,"Startup did not complete.");
            Assert.IsTrue(host.CanResume,host.ResumeBlockReason);
            var profile=host.Session.CaptureCareerProfile();
            Assert.IsNotNull(profile,"New run must own a Career profile.");
            if(profile.firstGuideEligible && !profile.firstGuideClaimed)
            {
                Assert.IsTrue(host.hud.Career.IsOpen,"Eligible first-day guide must be presented.");
                Assert.IsTrue(host.hud.NormalHudSuppressed,"Career must suppress driving HUD elements.");
                Assert.IsFalse(host.hud.Root.Find("Paused").gameObject.activeSelf,"Pause controls must not show through Career.");
                TruckTaxiDemoPlayModeTests.Capture("CareerGuide1280",1280,720);
                TruckTaxiDemoPlayModeTests.Capture("CareerGuideUltrawide",3440,1440);
                host.hud.Career.Close();
                Assert.IsTrue(host.Session.CaptureCareerProfile().firstGuideClaimed);
            }
            host.StartShift();
            yield return null;
            deadline=Time.realtimeSinceStartup+5f;
            while(host.WorldLife.PointerInputBlocked && Time.realtimeSinceStartup<deadline) yield return null;
            Assert.IsFalse(host.WorldLife.PointerInputBlocked,"Guide input-release guard did not clear.");
        }

        [OneTimeSetUp]
        public void ConfigureFreshVendorRoot()
        {
            try { Configure(); }
            catch
            {
                if(owner!=null) UnityEngine.Object.DestroyImmediate(owner.gameObject);
                throw;
            }
        }
        private void Configure()
        {
            var saveType=FindType("PixelCrushers.SaveSystem");
            var diskType=FindType("PixelCrushers.DiskSavedGameDataStorer");
            var storerType=FindType("PixelCrushers.SavedGameDataStorer");
            foreach(var component in Resources.FindObjectsOfTypeAll(saveType))
                if(component is Component existing && existing.gameObject.scene.IsValid())
                    Assert.Fail("A SaveSystem already exists before test isolation. Refusing to redirect an existing session.");

            Assert.IsTrue(TruckTaxiRestartTestArguments.TryParse(new[]{"-truck-taxi-restart-test","seed","-truck-taxi-test-run",Guid.NewGuid().ToString("N")},
                Application.temporaryCachePath,out var request,out string failure),failure);
            request.VerifyFilesystemBoundary();
            root=request.Root;
            Assert.IsFalse(Directory.Exists(root),"A fresh test directory is required.");
            owner=saveType.GetProperty("instance",BindingFlags.Public|BindingFlags.Static)?.GetValue(null) as Component;
            Assert.IsNotNull(owner,"Installed SaveSystem.instance is required.");
            Assert.IsEmpty(owner.GetComponents(storerType),"Unexpected preconfigured storage; refusing normal-root access.");
            var disk=owner.gameObject.AddComponent(diskType);
            ((Behaviour)disk).enabled=false;
            var basePath=diskType.GetField("storeSaveFilesIn");
            var customPath=diskType.GetField("customPath");
            Assert.IsNotNull(basePath); Assert.IsNotNull(customPath);
            basePath.SetValue(disk,Enum.Parse(basePath.FieldType,"Custom"));
            customPath.SetValue(disk,root);
            request.VerifyFilesystemBoundary();
            Directory.CreateDirectory(root);
            Assert.AreSame(disk,saveType.GetProperty("storer",BindingFlags.Public|BindingFlags.Static)?.GetValue(null));
            ((Behaviour)disk).enabled=true;
            Debug.Log("Truck Taxi PlayMode isolated vendor root: "+root);
        }

        [OneTimeTearDown]
        public void RetireTestOwner()
        {
            // Leave isolated evidence on disk. Never restore a normal path into a still-running fixture world.
            if(owner!=null) UnityEngine.Object.DestroyImmediate(owner.gameObject);
        }

        private static Type FindType(string name)
        {
            foreach(var assembly in AppDomain.CurrentDomain.GetAssemblies())
            { var type=assembly.GetType(name,false); if(type!=null) return type; }
            throw new InvalidOperationException("Installed vendor type unavailable: "+name);
        }
    }
}

using System;
using System.Linq;
using LWS.InterstateHauler;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace LWS.TruckTaxi.Tests
{
    public sealed class TruckTaxiVibinSavePanelTests
    {
        private static LwsManualSaveSlotMetadata Manual() => new LwsManualSaveSlotMetadata
        { stableProfileId = "vibin-one", slotType = LwsSaveSlotType.Manual, slotNumber = 2,
            vendorSlotNumber = 16102, occupied = true, savedUtcTicks = 638900000000000000 };
        private static LwsSaveRecoveryOffer Backup() => new LwsSaveRecoveryOffer
        { available = true, profileId = "vibin-one", primarySlotNumber = 2, primaryVendorSlotNumber = 16102,
            primarySlotKind = LwsSaveLoadSlotKind.ManualPrimary, backupSlotKind = LwsSaveLoadSlotKind.ManualBackup,
            backupVendorSlotNumber = 16117, backupTimestampUtcTicks = 638899000000000000 };

        [Test] public void OverwriteRequiresTheSameActiveProfileAndExactSlot()
        {
            var slot = Manual(); var prompt = TruckTaxiVibinSavePanel.Confirmation.ForSlot(slot, true);
            Assert.IsTrue(prompt.Matches(slot, "vibin-one"));
            Assert.IsFalse(prompt.Matches(slot, "vibin-two"));
            Assert.IsFalse(prompt.Matches(null, "vibin-one"));
            slot.savedUtcTicks++; Assert.IsFalse(prompt.Matches(slot, "vibin-one"));
            slot.savedUtcTicks--; slot.occupied = false; Assert.IsFalse(prompt.Matches(slot, "vibin-one"));
            slot.occupied = true; slot.slotNumber = 1; Assert.IsFalse(prompt.Matches(slot, "vibin-one"));
            slot.slotNumber = 2; slot.vendorSlotNumber++; Assert.IsFalse(prompt.Matches(slot, "vibin-one"));
            Assert.AreEqual(638900000000000000, prompt.SavedTicks, "Prompt identity must not alias mutable metadata.");
        }

        [Test] public void ProfileBrowsingDoesNotRequireSwitchingTheActiveSaveProfileToLoad()
        {
            var slot = Manual(); var prompt = TruckTaxiVibinSavePanel.Confirmation.ForSlot(slot, false);
            Assert.IsTrue(prompt.Matches(slot, "another-active-run"));
            slot.stableProfileId = "different-save"; Assert.IsFalse(prompt.Matches(slot, "another-active-run"));
        }

        [Test] public void AutosaveCanLoadButCannotBeUsedAsAnOverwriteTarget()
        {
            var slot = Manual(); slot.slotType = LwsSaveSlotType.Autosave;
            Assert.IsTrue(TruckTaxiVibinSavePanel.Confirmation.ForSlot(slot, false).Matches(slot, "vibin-one"));
            Assert.IsFalse(TruckTaxiVibinSavePanel.Confirmation.ForSlot(slot, true).Matches(slot, "vibin-one"));
            var manualPrompt = TruckTaxiVibinSavePanel.Confirmation.ForSlot(Manual(), false);
            Assert.IsFalse(manualPrompt.Matches(slot, "vibin-one"));
        }

        [TestCase("profile")] [TestCase("timestamp")] [TestCase("primary-slot")] [TestCase("primary-vendor")]
        [TestCase("backup-vendor")] [TestCase("primary-kind")] [TestCase("backup-kind")] [TestCase("dismissed")]
        public void RecoveryRequiresTheExactOfferThePlayerConfirmed(string change)
        {
            var offer = Backup(); var prompt = TruckTaxiVibinSavePanel.Confirmation.ForRecovery(offer);
            Assert.IsTrue(prompt.Matches(offer));
            Assert.IsFalse(prompt.Matches((LwsSaveRecoveryOffer)null));
            Assert.IsFalse(prompt.Matches(Manual(), "vibin-one"));
            switch (change)
            {
                case "profile": offer.profileId = "other"; break;
                case "timestamp": offer.backupTimestampUtcTicks++; break;
                case "primary-slot": offer.primarySlotNumber++; break;
                case "primary-vendor": offer.primaryVendorSlotNumber++; break;
                case "backup-vendor": offer.backupVendorSlotNumber++; break;
                case "primary-kind": offer.primarySlotKind = LwsSaveLoadSlotKind.AutosavePrimary; break;
                case "backup-kind": offer.backupSlotKind = LwsSaveLoadSlotKind.AutosaveBackup; break;
                case "dismissed": offer.available = false; break;
            }
            Assert.IsFalse(prompt.Matches(offer));
        }

        [TestCase(0)] [TestCase(-1)] [TestCase(long.MaxValue)]
        public void InvalidTimestampsCannotBreakThePanel(long ticks)
        { Assert.AreEqual("UNKNOWN DATE", TruckTaxiVibinSavePanel.Timestamp(ticks)); }

        [Test] public void TimestampsAreExplicitUtc()
        { Assert.AreEqual("2026-09-30 12:45 UTC", TruckTaxiVibinSavePanel.Timestamp(new DateTime(2026, 9, 30, 12, 45, 0, DateTimeKind.Utc).Ticks)); }

        [Test] public void MissingOrLatePersistenceIsSafeAndTheViewCreatesNoCanvas()
        {
            var root = new GameObject("Save panel test", typeof(RectTransform));
            var events = new GameObject("Save panel events", typeof(EventSystem), typeof(InputSystemUIInputModule));
            TruckTaxiUIInput input = null;
            try
            {
                var hud = root.AddComponent<TruckTaxiHud>(); hud.InitializeViewRoot(root.GetComponent<RectTransform>());
                var panel = root.AddComponent<TruckTaxiVibinSavePanel>(); TruckTaxiVibinPersistence source = null;
                int pauses = 0; panel.InitializeView(hud, () => source, () => pauses++);
                panel.InitializeView(hud, () => source, () => pauses++);
                Assert.AreEqual(0, root.GetComponentsInChildren<Canvas>(true).Length);
                Assert.AreEqual(1, root.transform.childCount, "Repeated initialization must not build another view.");
                foreach (var control in root.GetComponentsInChildren<Selectable>(true))
                    Assert.IsTrue(control.GetComponent<CanvasGroup>() != null, control.name + " requires a real CanvasGroup before disabling.");
                panel.Open(); Assert.IsTrue(panel.IsOpen); Assert.IsFalse(panel.IsBusy);
                input = new TruckTaxiUIInput(events.GetComponent<EventSystem>()); input.Focus(panel.FocusRoot);
                Assert.AreEqual("BACK", events.GetComponent<EventSystem>().currentSelectedGameObject.name);
                Assert.AreEqual(1, panel.FocusRoot.GetComponentsInChildren<Selectable>().Count(s => s.IsInteractable()));
                foreach (var control in panel.FocusRoot.GetComponentsInChildren<Selectable>())
                {
                    var group = control.GetComponent<CanvasGroup>();
                    Assert.AreEqual(control.IsInteractable(), group.interactable, control.name);
                    Assert.AreEqual(control.IsInteractable(), group.blocksRaycasts, control.name);
                }
                panel.Back(); Assert.IsFalse(panel.IsOpen); Assert.AreEqual(2, pauses);
                source = root.AddComponent<TruckTaxiVibinPersistence>(); panel.Open();
                source.Save(); // A safe uninitialized-facade failure, with no storage access.
                Assert.IsTrue(panel.IsOpen); Assert.IsFalse(panel.IsBusy); Assert.IsFalse(source.LastResult.Succeeded);
                Assert.AreEqual(1, panel.FocusRoot.GetComponentsInChildren<Selectable>().Count(s => s.IsInteractable()));
                panel.Back(); Assert.IsFalse(panel.IsOpen);
            }
            finally { input?.Dispose(); Object.DestroyImmediate(root); Object.DestroyImmediate(events); }
        }

        [Test] public void BusyFacadePreventsClosingOrFocusingUnderlyingUi()
        {
            var root = new GameObject("Busy save panel test", typeof(RectTransform));
            try
            {
                var hud = root.AddComponent<TruckTaxiHud>(); hud.InitializeViewRoot(root.GetComponent<RectTransform>());
                var source = root.AddComponent<TruckTaxiVibinPersistence>();
                typeof(TruckTaxiVibinPersistence).GetProperty("IsBusy").SetValue(source, true);
                var panel = root.AddComponent<TruckTaxiVibinSavePanel>(); panel.InitializeView(hud, () => source);
                panel.Open(); panel.Back(); Assert.IsTrue(panel.IsOpen); Assert.IsNull(panel.FocusRoot);
                Assert.IsTrue(root.GetComponentsInChildren<Selectable>().All(s => !s.IsInteractable()));
                typeof(TruckTaxiVibinPersistence).GetProperty("IsBusy").SetValue(source, false);
                source.Save(); panel.Back(); Assert.IsFalse(panel.IsOpen);
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}

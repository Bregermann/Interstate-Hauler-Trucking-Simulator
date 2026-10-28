#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class ImpossibleWorkshopStorageTests
{
    const BindingFlags Any = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    static Type Sports(string name) => Type.GetType("LWS.TruckSports." + name + ", Assembly-CSharp", true);
    static object Get(object owner, string name) => owner.GetType().GetField(name, Any) is FieldInfo field ? field.GetValue(owner) : owner.GetType().GetProperty(name, Any).GetValue(owner);
    static void Set(object owner, string name, object value) => owner.GetType().GetField(name, Any).SetValue(owner, value);
    static object Call(object owner, string name, params object[] args) => owner.GetType().GetMethods(Any).Single(method => method.Name == name && method.GetParameters().Length == args.Length).Invoke(owner, args);
    static UnityEvent Control(Transform parent, string name)
    {
        var child = parent.Find(name); Assert.That(child, Is.Not.Null, "Saved production Heat control must exist: " + name);
        var button = child.GetComponents<Component>().Single(item => item.GetType().FullName == "Michsky.UI.Heat.ButtonManager");
        return (UnityEvent)Get(button, "onClick");
    }
    static void FreshPointerConfirm(Mouse mouse)
    {
        // A normal Button click releases its pointer before onClick. Exercise
        // that real confirmation contract rather than changing the input gate.
        InputSystem.QueueStateEvent(mouse, new MouseState { buttons = 1, position = new Vector2(-100, -100) }); InputSystem.Update();
        InputSystem.QueueStateEvent(mouse, new MouseState { buttons = 0, position = new Vector2(-100, -100) }); InputSystem.Update();
    }

    [UnityTest, Timeout(150000)]
    public IEnumerator BothRealPauseMenusSaveAndRestoreUsingVendorDiskStorageWithoutTouchingPersonalWorkshops()
    {
        var lease = (IDisposable)Sports("SportsPrefs").GetMethod("IsolateValidation").Invoke(null, new object[] { "WorkshopStorageNative" + Guid.NewGuid().ToString("N") });
        var originalSettings = InputSystem.settings; var scopedSettings = UnityEngine.Object.Instantiate(originalSettings);
        scopedSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        scopedSettings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        InputSystem.settings = scopedSettings;
        GameObject isolation = new GameObject("Workshop storage native physical input isolation");
        UnityEngine.Object.DontDestroyOnLoad(isolation); isolation.AddComponent(Sports("SportsProbeInputIsolation"));
        Component match = null; string validationPath = null; Mouse mouse = null;
        try
        {
            foreach (string scene in new[] { "TruckNestBuilding", "TruckSnowmanBuilding" })
            {
                EditorSceneManager.LoadSceneInPlayMode("Assets/" + scene + "/Scenes/" + scene + ".unity", new LoadSceneParameters(LoadSceneMode.Single));
                yield return null; yield return null;
                match = (Component)UnityEngine.Object.FindAnyObjectByType(Sports("SportsEventSession"));
                if (mouse == null) mouse = InputSystem.AddDevice<Mouse>(); InputSystem.EnableDevice(mouse); mouse.MakeCurrent();
                Assert.That(match, Is.Not.Null); Call(match, "AllBots"); Assert.That((bool)Call(match, "StartMatch"), Is.True);
                // Pause at the actual initial safe workshop boundary, before an
                // AI Update can acquire a material. The production save guard
                // deliberately refuses a held joint and must remain intact.
                Call(match, "Pause", 0, false); yield return null; yield return null;
                Assert.That((bool)Get(match, "Paused"), Is.True);
                var persistence = match.GetComponent(Sports("SportsWorkshopPersistence")); Assert.That(persistence, Is.Not.Null);
                var storage = Get(persistence, "storage"); Assert.That(storage.GetType().FullName, Is.EqualTo("PixelCrushers.DiskSavedGameDataStorer"));
                string validationId = "Validation" + Guid.NewGuid().ToString("N");
                validationPath = Path.GetFullPath(Path.Combine(Application.persistentDataPath, "TruckSports", "Workshops", validationId));
                Assert.That(Directory.Exists(validationPath), Is.False, "Each fixture must use a fresh isolated vendor directory.");
                Set(persistence, "modeId", validationId); Set(storage, "customPath", "");
                var menus = (Component)Get(match, "menus"); var pages = (GameObject[])Get(menus, "pages");
                var save = Control(pages[3].transform, "Save construction workshop");
                var load = Control(pages[3].transform, "Restore construction workshop");
                Assert.That(save.GetPersistentTarget(0), Is.SameAs(persistence)); Assert.That(load.GetPersistentTarget(0), Is.SameAs(persistence));
                int receipts = (int)Get(Get(match, "ScoreJournal"), "Count");
                var tractor = match.gameObject.scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren(Sports("SportsEventTruck"), true)).Cast<Component>().OrderBy(item => (int)Get(item, "slot")).First();
                var body = (Rigidbody)Get(tractor, "body"); Vector3 before = body.position;
                FreshPointerConfirm(mouse); Assert.That((bool)Call(match, "MenuConfirm"), Is.True, "Fresh pointer release must pass the original pause confirmation gate."); save.Invoke(); InputSystem.Update();
                Assert.That((bool)Get(persistence, "LastOperationSucceeded"), Is.True, (string)Get(persistence, "Status"));
                Assert.That(Path.GetFullPath((string)Get(storage, "customPath")), Is.EqualTo(validationPath));
                Assert.That(File.Exists(Path.Combine(validationPath, "save_0.dat")), Is.True);
                Assert.That(new FileInfo(Path.Combine(validationPath, "save_0.dat")).Length, Is.GreaterThan(100));
                // Controlled stopped-state perturbation tests persistence only;
                // it is not evidence of a natural gameplay delivery/score.
                body.position += Vector3.right * .7f; Physics.SyncTransforms();
                FreshPointerConfirm(mouse); Assert.That((bool)Call(match, "MenuConfirm"), Is.True); load.Invoke(); InputSystem.Update(); Assert.That((bool)Get(persistence, "LastOperationSucceeded"), Is.True, (string)Get(persistence, "Status"));
                Assert.That(Vector3.Distance(body.position, before), Is.LessThan(.0001f), "Actual vendor-stored semantic state must restore the physical tractor pose.");
                Assert.That((int)Get(Get(match, "ScoreJournal"), "Count"), Is.EqualTo(receipts), "Saving/loading cannot award construction points.");
                string folder = Path.GetFullPath("Logs/TruckSports/Impossible07/WorkshopStorageNative"); Directory.CreateDirectory(folder);
                foreach (int width in new[] { 1280, 1920 })
                    Sports("SportsControlledCapture").GetMethods().Single(method => method.Name == "Save" && method.GetParameters().Length == 5)
                        .Invoke(null, new object[] { match, width, width == 1280 ? 720 : 1080, Path.Combine(folder, scene + "-pause-storage-" + width + "-controlled.png"), null });
                TestContext.WriteLine("WORKSHOP_VENDOR_DISK_ROUNDTRIP " + scene + " real saved Heat callbacks, unique validation directory, physical stopped-state restore, unchanged score receipts; controlled native UI captures only.");
                Call(match, "SetupScreen"); match = null;
                RemoveOnlyValidationFiles(validationPath); validationPath = null;
            }
        }
        finally
        {
            Time.timeScale = 1; if (match != null) Call(match, "SetupScreen");
            if (mouse != null && mouse.added) InputSystem.RemoveDevice(mouse);
            UnityEngine.Object.DestroyImmediate(isolation); lease.Dispose();
            InputSystem.settings = originalSettings; UnityEngine.Object.DestroyImmediate(scopedSettings);
            if (validationPath != null) RemoveOnlyValidationFiles(validationPath);
        }
    }

    static void RemoveOnlyValidationFiles(string path)
    {
        string root = Path.GetFullPath(Path.Combine(Application.persistentDataPath, "TruckSports", "Workshops")) + Path.DirectorySeparatorChar;
        string absolute = Path.GetFullPath(path);
        Assert.That(absolute.StartsWith(root + "Validation", StringComparison.OrdinalIgnoreCase), Is.True);
        if (!Directory.Exists(absolute)) return;
        foreach (string name in new[] { "save_0.dat", "saveinfo.dat" })
        {
            string file = Path.Combine(absolute, name); if (File.Exists(file)) File.Delete(file);
        }
        if (Directory.GetFileSystemEntries(absolute).Length == 0) Directory.Delete(absolute, false);
    }
}
#endif

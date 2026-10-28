#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using LWS.TruckPetPals;

public sealed class PetProfileRepositoryNativeTests
{
    const BindingFlags Any = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    static Type Runtime(string name) => Type.GetType(name + ", Assembly-CSharp", true);
    static object Get(object owner, string name) => owner.GetType().GetField(name, Any) is FieldInfo f ? f.GetValue(owner) : owner.GetType().GetProperty(name, Any).GetValue(owner);
    static void Set(object owner, string name, object value) { var field = owner.GetType().GetField(name, Any); if (field != null) field.SetValue(owner, value); else owner.GetType().GetProperty(name, Any).SetValue(owner, value); }
    static object Call(object owner, string name, params object[] args) => owner.GetType().GetMethods(Any).Single(m => m.Name == name && m.GetParameters().Length == args.Length).Invoke(owner, args);
    static GameObject Create(string mode, out Component repository, out Component storer)
    {
        var root = new GameObject("Isolated real Pixel Crushers pet-profile validation"); root.SetActive(false);
        var workshopType = Runtime("LWS.TruckSports.SportsWorkshopPersistence"); storer = root.AddComponent(workshopType.GetField("storage", Any).FieldType);
        var workshop = root.AddComponent(workshopType); Set(workshop, "modeId", mode); Set(workshop, "storage", storer);
        repository = root.AddComponent(Runtime("LWS.TruckPetPals.PetProfileRepository")); Set(repository, "workshop", workshop); root.SetActive(true); return root;
    }
    static object Data(string payload)
    {
        // Vendor data lives in a plugin assembly. Resolve it from the actual
        // storer signature, without duplicating a persistence implementation.
        var type = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("PixelCrushers.SavedGameData")).First(t => t != null);
        var data = Activator.CreateInstance(type); Set(data, "version", 1); Set(data, "sceneName", "TruckPetPalsPetProfiles");
        Call(data, "SetData", "LWS.TruckSports.PetPals.Profiles.v1", -1, payload); return data;
    }
    [UnityTest] public IEnumerator RealVendorDiskRoundtripRetainsAbsentCrewNamesCareAndTricksWithoutTouchingWorkshopOrFutureData()
    {
        string mode = "PetPalsValidation_" + Guid.NewGuid().ToString("N"), path = Path.Combine(Application.persistentDataPath, "TruckSports", "Workshops", mode);
        Assert.That(Directory.Exists(path), Is.False, "Never read or clean an existing/personal save directory.");
        GameObject root = null; Component repo = null, storage = null;
        try
        {
            root = Create(mode, out repo, out storage); yield return null;
            Assert.That(Path.GetFullPath((string)Get(storage, "customPath")), Is.EqualTo(Path.GetFullPath(path)));
            var a = (PetRecord)Call(repo, "SelectOrAdopt", "person.alpha", PetKind.Dog, "semi.standard");
            var b = (PetRecord)Call(repo, "SelectOrAdopt", "person.beta", PetKind.ClothedHuman, "truck.monster");
            Assert.That((bool)Call(repo, "Rename", a.profileId, a.id, "Same Name"), Is.True); Assert.That((bool)Call(repo, "Rename", b.profileId, b.id, "Same Name"), Is.True);
            a.cleanliness = 42; a.energy = 64; a.affection = 76; a.Learn(PetTrick.RollOver, true, true); string aId = a.id, bId = b.id;
            Directory.CreateDirectory(path); Call(storage, "StoreSavedGameData", 0, Data("WORKSHOP_SENTINEL")); string slot0 = (string)Call(storage, "GetSaveGameFilename", 0); byte[] untouchedWorkshop = File.ReadAllBytes(slot0);
            Assert.That((bool)Call(repo, "SaveProfiles"), Is.True); string archive = JsonUtility.ToJson(Get(repo, "Archive"));
            CollectionAssert.AreEqual(untouchedWorkshop, File.ReadAllBytes(slot0));
            UnityEngine.Object.DestroyImmediate(root); root = Create(mode, out repo, out storage); yield return null;
            Assert.That(JsonUtility.ToJson(Get(repo, "Archive")), Is.EqualTo(archive), "Fresh component reconstruction reloads the actual disk record.");
            var onlyCurrentCrew = (PetProfileRecord)Call(repo, "ForProfile", "person.alpha", "semi.standard"); Assert.That(onlyCurrentCrew.pets.Single(p => p.id == aId).familiarity[(int)PetTrick.RollOver], Is.EqualTo(12));
            Assert.That((bool)Call(repo, "Rename", "person.alpha", aId, "Renamed"), Is.True); Assert.That((bool)Call(repo, "SaveProfiles"), Is.True);
            var retained = (PetArchive)Get(repo, "Archive"); Assert.That(retained.profiles.Single(p => p.profileId == "person.beta").selectedPetId, Is.EqualTo(bId), "Absent crew profiles survive a one-owner session.");
            foreach (string unsupported in new[] { archive.Replace("\"version\":1", "\"version\":999"), "{broken-json" })
            {
                Call(storage, "StoreSavedGameData", 1, Data(unsupported)); string slot1 = (string)Call(storage, "GetSaveGameFilename", 1); byte[] original = File.ReadAllBytes(slot1);
                UnityEngine.Object.DestroyImmediate(root); root = Create(mode, out repo, out storage); yield return null;
                Assert.That((bool)Get(repo, "ReadOnly"), Is.True); Call(repo, "ForProfile", "person.gamma", "semi.standard"); Assert.That((bool)Call(repo, "SaveProfiles"), Is.False);
                CollectionAssert.AreEqual(original, File.ReadAllBytes(slot1), "Unsupported/corrupt originals must not be replaced with defaults."); CollectionAssert.AreEqual(untouchedWorkshop, File.ReadAllBytes(slot0));
            }
            TestContext.WriteLine("PET_PROFILE_NATIVE actual existing Pixel Crushers same-storer slot1 roundtrip/reconstruction, stable pet/profile IDs despite duplicate names, retained absent crew/care/tricks, exact slot0 bytes and preserved future/corrupt record. Unique validation directory only; no personal/host saves.");
        }
        finally
        {
            if (root != null) UnityEngine.Object.DestroyImmediate(root);
            string expectedParent = Path.GetFullPath(Path.Combine(Application.persistentDataPath, "TruckSports", "Workshops")) + Path.DirectorySeparatorChar;
            string target = Path.GetFullPath(path);
            if (!target.StartsWith(expectedParent, StringComparison.OrdinalIgnoreCase) || Path.GetFileName(target) != mode || !mode.StartsWith("PetPalsValidation_", StringComparison.Ordinal)) throw new InvalidOperationException("Validation cleanup target refused.");
            if (Directory.Exists(target)) Directory.Delete(target, true);
        }
    }
}
#endif

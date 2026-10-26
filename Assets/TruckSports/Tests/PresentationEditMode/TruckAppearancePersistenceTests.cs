#if UNITY_EDITOR
using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

// Runtime Sports types live in Assembly-CSharp, matching the existing isolated
// EditMode tests. These fixtures use private preference scopes, never player data.
public sealed class TruckAppearancePersistenceTests
{
    static Type Runtime(string name) => Type.GetType("LWS.TruckSports." + name + ", Assembly-CSharp", true);
    static readonly Type Registry = Runtime("SportsParticipantRegistry"), Prefs = Runtime("SportsPrefs"),
        Appearance = Runtime("SportsTruckAppearanceData"), Selection = Runtime("SportsTruckCosmeticSelection");
    const string Key = "TruckSports.Participants.v1";
    const string Legacy = @"{""version"":1,""participants"":[{""id"":""driver-a"",""displayName"":""Twin"",""preferredTruck"":""unavailable/fleet:custom"",""origin"":""original-a"",""preferredAI"":false,""color"":{""r"":1,""g"":0,""b"":0,""a"":1}},{""id"":""driver-b"",""displayName"":""Twin"",""preferredTruck"":""truck.monster"",""origin"":""original-b"",""preferredAI"":true,""color"":{""r"":0,""g"":1,""b"":0,""a"":1}}],""roster"":[""driver-b"",""driver-a""],""legacy"":[{""key"":""Old.Setup"",""json"":""{\""unknown\"":42}""}],""results"":[{""matchId"":""finished-1"",""mode"":""old-mode"",""completedUtc"":""2026-10-22T00:00:00Z"",""summary"":""Twin=3; Twin=1"",""participantIds"":[""driver-a"",""driver-b""],""names"":[""Twin"",""Twin""]}],""foreignLegacyValue"":123}";
    IDisposable lease;
    string production;

    static object Call(Type type, string method, params object[] args) => type.GetMethod(method).Invoke(null, args);
    static object Run(string method, params object[] args) => Call(Registry, method, args);
    static object Field(object owner, string name) => owner.GetType().GetField(name).GetValue(owner);
    static void Field(object owner, string name, object value) => owner.GetType().GetField(name).SetValue(owner, value);
    static string Id(object participant) => (string)Field(participant, "id");
    static string Stored(string key = Key) => (string)Call(Prefs, "GetString", key, "");
    static void Store(string key, string value) => Call(Prefs, "SetString", key, value);
    static object Current => Registry.GetProperty("Current").GetValue(null);
    static object Get(string id) => Run("GetAppearance", id);
    static bool Apply(string id, object draft) => (bool)Run("TrySetAppearance", id, draft);
    static IList Items(object draft) => (IList)Field(draft, "selections");
    static object Draft(string body, params string[] items)
    {
        Assert.That(items.Length % 3, Is.Zero);
        var result = Activator.CreateInstance(Appearance); Field(result, "bodyId", body);
        for (int i = 0; i < items.Length; i += 3)
        {
            var item = Activator.CreateInstance(Selection);
            Field(item, "slotId", items[i]); Field(item, "itemId", items[i + 1]); Field(item, "variantId", items[i + 2]);
            Items(result).Add(item);
        }
        return result;
    }
    static string Item(object draft, string slot)
    {
        foreach (var item in Items(draft)) if ((string)Field(item, "slotId") == slot) return (string)Field(item, "itemId");
        return null;
    }
    static void ExpectFuture() => LogAssert.Expect(LogType.Warning, "Participant data is from a newer game version. Changes are temporary.");
    static void ExpectRecovery()
    {
        LogAssert.Expect(LogType.Warning, "Unreadable participant preferences were preserved; legacy settings remain available.");
        LogAssert.Expect(LogType.Warning, "Participant preferences recovered from their previous saved copy.");
    }

    [SetUp]
    public void Isolate()
    {
        production = PlayerPrefs.GetString(Key);
        lease = (IDisposable)Call(Prefs, "IsolateValidation", "Appearance" + Guid.NewGuid().ToString("N"));
        Run("ForgetCache");
    }

    [TearDown]
    public void Restore()
    {
        foreach (string key in new[] { Key, Key + ".Backup", Key + ".Migrated.v1" }) Call(Prefs, "DeleteKey", key);
        Call(Prefs, "Save"); Run("ForgetCache"); lease?.Dispose();
        Assert.That(PlayerPrefs.GetString(Key), Is.EqualTo(production), "Appearance fixtures must never change production participants.");
    }

    [Test]
    public void VersionOneMigrationRetainsIdentitiesPreferencesRosterRawLegacyAndResults()
    {
        Store(Key, Legacy);
        Assert.That((int)Field(Current, "version"), Is.EqualTo(2));
        Assert.That(Stored(Key + ".Migrated.v1"), Is.EqualTo(Legacy));
        Assert.That(Stored(Key + ".Backup"), Is.EqualTo(Legacy));
        Assert.That(Id(Run("Default", 0)), Is.EqualTo("driver-b"));
        Assert.That(Id(Run("Default", 1)), Is.EqualTo("driver-a"));
        var a = Run("Find", "driver-a"); var b = Run("Find", "driver-b");
        Assert.That((string)Field(a, "displayName"), Is.EqualTo("Twin"));
        Assert.That((string)Field(a, "preferredTruck"), Is.EqualTo("unavailable/fleet:custom"));
        Assert.That((string)Field(b, "preferredTruck"), Is.EqualTo("truck.monster"), "No unagreed legacy ID mapping.");
        Assert.That((string)Field(a, "origin"), Is.EqualTo("original-a"));
        Assert.That((Color)Field(a, "color"), Is.EqualTo(Color.red));
        Assert.That((bool)Field(b, "preferredAI"), Is.True);
        Assert.That((string)Field(((IList)Field(Current, "legacy"))[0], "json"), Is.EqualTo("{\"unknown\":42}"));
        Assert.That((string)Field(((IList)Field(Current, "results"))[0], "summary"), Is.EqualTo("Twin=3; Twin=1"));
        Assert.That((string)Field(Get("driver-a"), "bodyId"), Is.Empty);
        Run("ForgetCache");
        Assert.That(Id(Run("Default", 0)), Is.EqualTo("driver-b"));
    }

    [Test]
    public void DraftCopyApplyCancelAndFreshReadHaveNoMutableAliases()
    {
        string id = Id(Run("Create", "A driver"));
        var draft = Draft("truck.monster", "hat", "hat.cap", "red", "wheels", "wheel.chrome", "");
        Assert.That(Apply(id, draft), Is.True);
        string committed = Stored();
        Field(draft, "bodyId", "semi.lowrider"); Field(Items(draft)[0], "itemId", "hat.crown");
        Assert.That((string)Field(Get(id), "bodyId"), Is.EqualTo("truck.monster"));
        Assert.That(Item(Get(id), "hat"), Is.EqualTo("hat.cap"));
        var canceled = Get(id); Field(canceled, "bodyId", "truck.fire"); Items(canceled).Clear();
        Assert.That(Stored(), Is.EqualTo(committed), "Cancel is simply discarding a detached draft.");
        Run("ForgetCache");
        Assert.That((string)Field(Get(id), "bodyId"), Is.EqualTo("truck.monster"));
        Assert.That(Item(Get(id), "wheels"), Is.EqualTo("wheel.chrome"));
        Assert.That(Get("missing-profile"), Is.Null);
    }

    [Test]
    public void DuplicateNamesRenameSeatSwapAndAiPreferenceKeepIndependentAppearancesAndReceipts()
    {
        Store(Key, Legacy); _ = Current;
        Assert.That(Apply("driver-a", Draft("truck.monster", "hat", "hat.cap", "")), Is.True);
        Assert.That(Apply("driver-b", Draft("truck.fire", "hat", "hat.crown", "")), Is.True);
        Run("Rename", "driver-a", "Renamed", Color.magenta, false);
        Assert.That((bool)Run("UseInSeat", 0, "driver-a"), Is.True);
        Run("ForgetCache");
        Assert.That(Id(Run("Default", 0)), Is.EqualTo("driver-a"));
        Assert.That(Id(Run("Default", 1)), Is.EqualTo("driver-b"));
        Assert.That(Item(Get("driver-a"), "hat"), Is.EqualTo("hat.cap"));
        Assert.That(Item(Get("driver-b"), "hat"), Is.EqualTo("hat.crown"));
        Assert.That((string)Field(Run("Find", "driver-a"), "displayName"), Is.EqualTo("Renamed"));
        Assert.That((bool)Field(Run("Find", "driver-b"), "preferredAI"), Is.True);
        var receipt = ((IList)Field(Current, "results"))[0];
        Assert.That((string[])Field(receipt, "participantIds"), Is.EqualTo(new[] { "driver-a", "driver-b" }));
        Assert.That((string[])Field(receipt, "names"), Is.EqualTo(new[] { "Twin", "Twin" }));
        Assert.That(Stored(Key + ".Migrated.v1"), Is.EqualTo(Legacy), "Later saves retain the exact migration source.");
    }

    [Test]
    public void AppearanceEditsPreserveNewMatchReceiptsAndTheirCompletionDeduplication()
    {
        string id = Id(Run("Create", "Same name"));
        var slotType = Type.GetType("LWS.TruckBowling.BowlingPlayer, Assembly-CSharp", true);
        var slot = Activator.CreateInstance(slotType); Field(slot, "participantId", id); Field(slot, "name", "Same name");
        var slots = Array.CreateInstance(slotType, 1); slots.SetValue(slot, 0);
        Assert.That((bool)Run("Record", "actual-result", "Fixture", "Same name=7", slots, 1), Is.True);
        Assert.That(Apply(id, Draft("truck.monster", "dashboard", "toy.cat", "")), Is.True);
        Run("Rename", id, "New name", Color.yellow, true);
        Assert.That((bool)Run("Record", "actual-result", "Fixture", "duplicate", slots, 1), Is.False);
        Run("ForgetCache");
        var receipts = (IList)Field(Current, "results"); Assert.That(receipts.Count, Is.EqualTo(1));
        Assert.That((string[])Field(receipts[0], "participantIds"), Is.EqualTo(new[] { id }));
        Assert.That((string[])Field(receipts[0], "names"), Is.EqualTo(new[] { "Same name" }));
        Assert.That((string)Field(receipts[0], "summary"), Is.EqualTo("Same name=7"));
        Assert.That(Item(Get(id), "dashboard"), Is.EqualTo("toy.cat"));
    }

    [Test]
    public void AllCosmeticSlotsAndUnavailableCatalogIdsSurviveReloadWithoutLookup()
    {
        string id = Id(Run("Create", "Future content"));
        var draft = Draft("missing.body-2027", "hat", "missing.hat", "finish.gold", "face.eyes", "eyes.goofy", "",
            "face.brows", "brows.angry", "", "face.mouth", "mouth.smile", "", "face.mustache", "mustache.curly", "",
            "face.lashes", "lashes.long", "", "mirrors", "mirrors.chrome", "", "wheels", "wheels.lowrider", "",
            "rear.ornament", "ornament.balls", "purple", "dashboard", "toy.cat", "", "future.roof.flag", "flag.checkered", "");
        Assert.That(Apply(id, draft), Is.True); Run("ForgetCache");
        var loaded = Get(id);
        Assert.That((string)Field(loaded, "bodyId"), Is.EqualTo("missing.body-2027"));
        Assert.That(Items(loaded).Count, Is.EqualTo(11));
        Assert.That(Item(loaded, "future.roof.flag"), Is.EqualTo("flag.checkered"));
        Assert.That(Item(loaded, "rear.ornament"), Is.EqualTo("ornament.balls"));
        Assert.That((int)Field(loaded, "version"), Is.EqualTo(1));
    }

    [Test]
    public void InvalidOrUnchangedDraftsDoNotPartiallySaveOrEmitChanged()
    {
        string id = Id(Run("Create", "Atomic"));
        Assert.That(Apply(id, Draft("truck.monster", "hat", "hat.cap", "")), Is.True);
        string before = Stored(); int changes = 0; Action changed = () => changes++;
        Registry.GetEvent("Changed").AddEventHandler(null, changed);
        try
        {
            Assert.That(Apply(id, null), Is.False);
            Assert.That(Apply("absent", Draft("truck.fire")), Is.False);
            Assert.That(Apply(id, Draft("bad body", "hat", "hat.crown", "")), Is.False);
            Assert.That(Apply(id, Draft("truck.fire", "hat", "hat.cap", "", "hat", "hat.crown", "")), Is.False);
            Assert.That(Apply(id, Draft("truck.fire", "hat", "", "orphaned-variant")), Is.False);
            var future = Draft("truck.fire"); Field(future, "version", 99);
            Assert.That(Apply(id, future), Is.False);
            Assert.That(Apply(id, Get(id)), Is.False);
            Assert.That(Stored(), Is.EqualTo(before)); Assert.That(changes, Is.Zero);
            Assert.That(Apply(id, Draft("truck.fire")), Is.True); Assert.That(changes, Is.EqualTo(1));
        }
        finally { Registry.GetEvent("Changed").RemoveEventHandler(null, changed); }
    }

    [Test]
    public void CorruptNestedAppearanceIsSalvagedWithoutDiscardingTheParticipant()
    {
        var data = JsonUtility.FromJson(Legacy, Runtime("SportsParticipantData")); Field(data, "version", 2);
        var participant = ((IList)Field(data, "participants"))[0];
        var broken = Draft("bad body", "hat", "hat.cap", "", "hat", "hat.crown", "", "dashboard", "toy.cat", "");
        Items(broken).Add(null); Field(broken, "version", 0); Field(participant, "appearance", broken);
        Store(Key, JsonUtility.ToJson(data));
        LogAssert.Expect(LogType.Warning, "Invalid truck appearance preferences were preserved and safely repaired.");
        var repaired = Get("driver-a");
        Assert.That((string)Field(repaired, "bodyId"), Is.Empty);
        Assert.That(Item(repaired, "hat"), Is.EqualTo("hat.cap"));
        Assert.That(Item(repaired, "dashboard"), Is.EqualTo("toy.cat"));
        Assert.That(Items(repaired).Count, Is.EqualTo(2));
        Assert.That(Id(Run("Default", 0)), Is.EqualTo("driver-b"));
        Assert.That(((IList)Field(Current, "results")).Count, Is.EqualTo(1));
        Run("ForgetCache"); Assert.That(Item(Get("driver-a"), "hat"), Is.EqualTo("hat.cap"));
    }

    [Test]
    public void CorruptPrimaryRestoresTheValidPreviousAppearanceAndKeepsItsBackup()
    {
        string id = Id(Run("Create", "Recovery"));
        Assert.That(Apply(id, Draft("truck.monster", "hat", "hat.cap", "")), Is.True);
        string previous = Stored();
        Assert.That(Apply(id, Draft("truck.fire", "hat", "hat.crown", "")), Is.True);
        Store(Key, "broken json"); Run("ForgetCache"); ExpectRecovery();
        Assert.That(Item(Get(id), "hat"), Is.EqualTo("hat.cap"));
        Assert.That(Stored(Key + ".Backup"), Is.EqualTo(previous));
        Assert.That((string)Field(Get(id), "bodyId"), Is.EqualTo("truck.monster"));
    }

    [Test]
    public void ValidVersionOneBackupMigratesAfterPrimaryCorruption()
    {
        Store(Key + ".Backup", Legacy); Store(Key, "broken json"); ExpectRecovery();
        Assert.That(Item(Get("driver-a"), "hat"), Is.Null);
        Assert.That((int)Field(Current, "version"), Is.EqualTo(2));
        Assert.That(Stored(Key + ".Migrated.v1"), Is.EqualTo(Legacy));
        Assert.That(Id(Run("Default", 0)), Is.EqualTo("driver-b"));
    }

    [Test]
    public void FutureRegistryWithIncompatibleFieldsIsNeverReplaced()
    {
        const string future = @"{""version"":99,""participants"":""new representation"",""future"":{""secretShape"":42}}";
        Store(Key, future); Store(Key + ".Backup", "preserve backup"); ExpectFuture();
        string id = Id(Run("Default", 0));
        Assert.That(Apply(id, Draft("truck.monster")), Is.False);
        Run("Rename", id, "Temporary", Color.red, false);
        Assert.That(Stored(), Is.EqualTo(future));
        Assert.That(Stored(Key + ".Backup"), Is.EqualTo("preserve backup"));
    }

    [Test]
    public void FutureNestedAppearanceMakesAllRegistryWritesTemporary()
    {
        var data = JsonUtility.FromJson(Legacy, Runtime("SportsParticipantData")); Field(data, "version", 2);
        var futureAppearance = Draft("future.body"); Field(futureAppearance, "version", 99);
        Field(((IList)Field(data, "participants"))[0], "appearance", futureAppearance);
        string future = JsonUtility.ToJson(data); Store(Key, future); ExpectFuture();
        Assert.That((string)Field(Get("driver-a"), "bodyId"), Is.Empty, "An unsupported appearance resolves to a detached safe default.");
        Assert.That(Apply("driver-b", Draft("truck.fire")), Is.False);
        Run("Rename", "driver-b", "Temporary AI", Color.yellow, true);
        Assert.That(Stored(), Is.EqualTo(future));
    }

    [Test]
    public void FutureAppearanceHeaderIsProtectedWhenItsContentShapeHasChanged()
    {
        string future = Legacy.Replace("\"version\":1", "\"version\":2").Replace("\"preferredAI\":false",
            "\"preferredAI\":false,\"appearance\":{\"version\":99,\"selections\":\"new representation\",\"future\":42}");
        Store(Key, future); ExpectFuture(); _ = Current;
        Assert.That(Apply("driver-a", Draft("truck.monster")), Is.False);
        Run("Create", "Temporary");
        Assert.That(Stored(), Is.EqualTo(future));
    }

    [Test]
    public void NewlyWrittenFuturePrimaryIsNotOverwrittenByAnOlderRegistryCache()
    {
        string id = Id(Run("Create", "Cached"));
        const string future = @"{""version"":99,""participants"":""new representation"",""future"":42}";
        Store(Key, future); ExpectFuture();
        Assert.That(Apply(id, Draft("truck.monster")), Is.False);
        Assert.That((string)Field(Get(id), "bodyId"), Is.Empty);
        Assert.That(Stored(), Is.EqualTo(future));
    }

    [Test]
    public void FutureBackupIsProtectedEvenWhenThePrimaryIsDamagedOrCacheIsOlder()
    {
        string id = Id(Run("Create", "Cached")); string original = Stored();
        const string future = @"{""version"":99,""participants"":[],""roster"":[],""legacy"":[],""results"":[],""future"":42}";
        Store(Key + ".Backup", future); ExpectFuture();
        Assert.That(Apply(id, Draft("truck.monster")), Is.False);
        Assert.That(Stored(), Is.EqualTo(original));
        Assert.That((string)Field(Get(id), "bodyId"), Is.Empty, "Rejected persistence rolls back the live draft.");
        Store(Key, "broken json"); Run("ForgetCache"); ExpectFuture();
        _ = Run("Default", 0);
        Assert.That(Stored(), Is.EqualTo("broken json")); Assert.That(Stored(Key + ".Backup"), Is.EqualTo(future));
    }
}
#endif

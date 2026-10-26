#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.TestTools;

// Synthetic UI callbacks and render-only lifetime checks. These do not claim
// physical mouse/gamepad use, cross-sport appearance or visual art acceptance.
public sealed class TruckCustomizerSyntheticUiTests
{
    const BindingFlags Any = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    static Type Runtime(string name) => Type.GetType("LWS.TruckSports." + name + ", Assembly-CSharp", true);
    static object Get(object owner, string member) => owner.GetType().GetField(member, Any) is FieldInfo field ? field.GetValue(owner) : owner.GetType().GetProperty(member, Any).GetValue(owner);
    static void Set(object owner, string member, object value) => owner.GetType().GetField(member, Any).SetValue(owner, value);
    static object Call(object owner, string method, params object[] args) => owner.GetType().GetMethods(Any).Single(m => m.Name == method && m.GetParameters().Length == args.Length).Invoke(owner, args);
    static object Registry(string method, params object[] args) => Runtime("SportsParticipantRegistry").GetMethod(method).Invoke(null, args);
    static void Click(object button) => ((UnityEvent)Get(button, "onClick")).Invoke();
    static GameObject Prefab()
    {
        var prefab = Resources.Load<GameObject>("TruckSports/SportsTruckCustomizerMenu");
        Assert.That(prefab, Is.Not.Null, "Run the explicit Create Missing Reusable Heat Shop authoring command first."); return prefab;
    }

    [UnityTest, Timeout(90000)]
    public IEnumerator SyntheticHeatBodyCallbacksKeepCancelDetachedSaveByIdAndReleasePreview()
    {
        var lease = (IDisposable)Runtime("SportsPrefs").GetMethod("IsolateValidation").Invoke(null, new object[] { "CustomizerUi" + Guid.NewGuid().ToString("N") });
        Registry("ForgetCache"); GameObject root = null;
        try
        {
            var person = Registry("Create", "Same Name"); var other = Registry("Create", "Same Name"); string id = (string)Get(person, "id"), otherId = (string)Get(other, "id");
            root = UnityEngine.Object.Instantiate(Prefab()); var menu = root.GetComponent(Runtime("SportsTruckCustomizerMenu")); int closed = 0;
            Call(menu, "Open", id, (Action)(() => closed++)); yield return null;
            Assert.That(Get(menu, "IsOpen"), Is.True); StringAssert.Contains("Same Name", (string)Get(Get(menu, "identity"), "text"));
            var preview = Get(menu, "preview"); var stage = (GameObject)Get(preview, "stage"); var texture = (RenderTexture)Get(preview, "texture");
            Assert.That(stage.GetComponentsInChildren<Rigidbody>(true), Is.Empty); Assert.That(stage.GetComponentsInChildren<Collider>(true), Is.Empty);
            Assert.That(stage.GetComponentsInChildren<AudioListener>(true), Is.Empty);
            Assert.That(stage.GetComponentsInChildren<Camera>(true).Single().enabled, Is.False, "Only explicit render requests may show preview geometry.");
            Assert.That(stage.transform.Find("Customized truck preview").gameObject.activeSelf, Is.False, "Preview meshes remain hidden between camera draws.");
            var cards = ((Array)Get(menu, "cards")).Cast<object>().ToArray(); var monster = cards.Single(card => ((string)Get(card, "buttonTitle")) == "Monster Hauler");
            Click(monster); Assert.That((string)Get(Get(menu, "draft"), "bodyId"), Is.EqualTo("truck.monster"));
            Click(((Array)Get(menu, "categories")).GetValue(8));
            cards = ((Array)Get(menu, "cards")).Cast<object>().ToArray(); var wheel = cards.Single(card => ((string)Get(card, "buttonTitle")) == "Chrome Road Rims"); Click(wheel);
            var body = Call(Get(menu, "catalog"), "FindBody", "truck.monster"); var mounts = (Array)Get(body, "wheelMounts");
            Assert.That(mounts.Length, Is.EqualTo(4));
            var model = ((GameObject)Get(preview, "model")).transform;
            foreach (var mount in mounts) Assert.That(model.GetComponentsInChildren<Transform>(true).Count(node => node.name == "Cosmetic mount " + Get(mount, "slotId")), Is.EqualTo(1));
            var originals = (string[])Get(body, "wheelRendererNames"); var hidden = model.GetComponentsInChildren<Renderer>(true).Where(renderer => originals.Contains(renderer.name)).ToArray();
            Assert.That(hidden, Is.Not.Empty); Assert.That(hidden.All(renderer => !renderer.enabled), Is.True, "Authored wheel names hide only the original presentation wheels.");
            Assert.That((string)Get(Registry("GetAppearance", id), "bodyId"), Is.Empty);
            Click(Get(menu, "cancel")); Assert.That(Get(menu, "IsOpen"), Is.False); Assert.That(closed, Is.EqualTo(1));
            Assert.That(Get(Get(menu, "previewImage"), "texture"), Is.Null); Assert.That(texture == null || !texture.IsCreated(), Is.True);
            yield return null; Assert.That(stage == null, Is.True);
            Call(menu, "Open", id, (Action)(() => closed++)); yield return null;
            cards = ((Array)Get(menu, "cards")).Cast<object>().ToArray(); monster = cards.Single(card => ((string)Get(card, "buttonTitle")) == "Monster Hauler");
            Click(monster); Click(Get(menu, "apply"));
            Assert.That(closed, Is.EqualTo(2)); Assert.That((string)Get(Registry("GetAppearance", id), "bodyId"), Is.EqualTo("truck.monster"));
            Assert.That((string)Get(Registry("GetAppearance", otherId), "bodyId"), Is.Empty, "Identical display names never share edits.");
            Registry("ForgetCache"); Assert.That((string)Get(Registry("GetAppearance", id), "bodyId"), Is.EqualTo("truck.monster"));
            TestContext.WriteLine("SYNTHETIC_CUSTOMIZER_UI real Heat callbacks: detached cancel, stable-ID apply, independent duplicate names and released render-only preview. No physical-device or cross-sport acceptance.");
        }
        finally { if (root != null) UnityEngine.Object.DestroyImmediate(root); Registry("ForgetCache"); lease.Dispose(); }
    }

    [UnityTest, Timeout(90000)]
    public IEnumerator SyntheticEmptyHatCategoryHasNoEquippablePlaceholderAndPreservesMissingIds()
    {
        var lease = (IDisposable)Runtime("SportsPrefs").GetMethod("IsolateValidation").Invoke(null, new object[] { "CustomizerMissing" + Guid.NewGuid().ToString("N") });
        Registry("ForgetCache"); GameObject root = null; UnityEngine.Object catalog = null;
        try
        {
            string id = (string)Get(Registry("Create", "Missing content owner"), "id");
            var draft = Activator.CreateInstance(Runtime("SportsTruckAppearanceData")); Set(draft, "bodyId", "missing.saved.body");
            var selected = Activator.CreateInstance(Runtime("SportsTruckCosmeticSelection")); Set(selected, "slotId", "hat"); Set(selected, "itemId", "missing.saved.hat");
            ((IList)Get(draft, "selections")).Add(selected); Assert.That(Registry("TrySetAppearance", id, draft), Is.True);
            root = UnityEngine.Object.Instantiate(Prefab()); var menu = root.GetComponent(Runtime("SportsTruckCustomizerMenu"));
            var sourceCatalog = Resources.Load("SportsTruckAppearanceCatalog"); Assert.That(sourceCatalog, Is.Not.Null);
            catalog = UnityEngine.Object.Instantiate(sourceCatalog);
            var cosmetics = (Array)Get(catalog, "cosmetics"); Set(catalog, "cosmetics", Array.CreateInstance(cosmetics.GetType().GetElementType(), 0)); Set(menu, "catalog", catalog);
            Call(menu, "Open", id, (Action)(() => { })); yield return null;
            Click(((Array)Get(menu, "categories")).GetValue(1));
            Assert.That(((Array)Get(menu, "cards")).Cast<Component>().All(card => !card.gameObject.activeSelf), Is.True);
            StringAssert.Contains("NO COMPATIBLE ITEMS", (string)Get(Get(menu, "pageCaption"), "text"));
            StringAssert.Contains("retained", (string)Get(Get(menu, "notice"), "text"));
            Click(Get(menu, "apply")); var saved = Registry("GetAppearance", id);
            Assert.That((string)Get(saved, "bodyId"), Is.EqualTo("missing.saved.body"));
            Assert.That((string)Get(((IList)Get(saved, "selections"))[0], "itemId"), Is.EqualTo("missing.saved.hat"));
            Assert.That(Get(menu, "IsOpen"), Is.False);
        }
        finally { if (root != null) UnityEngine.Object.DestroyImmediate(root); if (catalog != null) UnityEngine.Object.DestroyImmediate(catalog); Registry("ForgetCache"); lease.Dispose(); }
    }
}
#endif

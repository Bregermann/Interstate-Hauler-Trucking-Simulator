#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

// Synthetic layout regression using the installed Heat notification and actual
// broadcast font. It does not simulate a match or establish gameplay acceptance.
public sealed class ResultNotificationSyntheticLayoutTests
{
    const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    static Type Sports(string name) => Type.GetType("LWS.TruckSports." + name + ", Assembly-CSharp", true);
    static object Get(object owner, string name) => owner.GetType().GetField(name, Any) is FieldInfo field
        ? field.GetValue(owner) : owner.GetType().GetProperty(name, Any).GetValue(owner);
    static void Set(object owner, string name, object value)
    {
        var field = owner.GetType().GetField(name, Any);
        if (field != null) field.SetValue(owner, value); else owner.GetType().GetProperty(name, Any).SetValue(owner, value);
    }
    static object Call(object owner, string name, params object[] values) => owner.GetType().GetMethods(Any)
        .Single(method => method.Name == name && method.GetParameters().Length == values.Length).Invoke(owner, values);
    static Rect Pixels(RectTransform rect, Camera camera)
    {
        var corners = new Vector3[4]; rect.GetWorldCorners(corners);
        var low = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
        var high = RectTransformUtility.WorldToScreenPoint(camera, corners[2]);
        return Rect.MinMaxRect(low.x, low.y, high.x, high.y);
    }
    static void Inside(Rect inner, Rect outer)
    {
        Assert.That(inner.xMin, Is.GreaterThanOrEqualTo(outer.xMin - .5f));
        Assert.That(inner.yMin, Is.GreaterThanOrEqualTo(outer.yMin - .5f));
        Assert.That(inner.xMax, Is.LessThanOrEqualTo(outer.xMax + .5f));
        Assert.That(inner.yMax, Is.LessThanOrEqualTo(outer.yMax + .5f));
    }
    static string State(Component notice, GameObject panel, Component text, Animator animator, string phase, string message)
    {
        var state = animator.GetCurrentAnimatorStateInfo(0);
        string report = "SYNTHETIC_HEAT_LIFECYCLE " + phase + " isOn=" + Get(notice, "isOn") + " defaultState=" + Get(notice, "defaultState")
            + " minimizeAfter=" + Get(notice, "minimizeAfter") + " panel=" + panel.activeSelf + "/" + panel.activeInHierarchy
            + " text=" + text.gameObject.activeSelf + "/" + text.gameObject.activeInHierarchy + " animator=" + animator.enabled
            + " stateHash=" + state.fullPathHash + " in=" + state.IsName("In") + " out=" + state.IsName("Out") + " normalized=" + state.normalizedTime
            + " timeScale=" + Time.timeScale + " scaledTime=" + Time.time + " realtime=" + Time.realtimeSinceStartup + " message=" + message;
        TestContext.WriteLine(report); return report;
    }

    [UnityTest, Timeout(120000)]
    public IEnumerator SyntheticHeatResultAwardsFit720And1080AndRestoreTheirPlayLayout()
    {
        var fixture = new GameObject("SYNTHETIC result notification layout regression"); fixture.SetActive(false);
        RenderTexture texture = null; float oldTimeScale = Time.timeScale;
        try
        {
            Time.timeScale = 1;
            var cameraRoot = new GameObject("Synthetic UI capture camera"); cameraRoot.transform.SetParent(fixture.transform);
            var camera = cameraRoot.AddComponent<Camera>(); camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black; camera.cullingMask = 1 << 5;
            var canvasRoot = new GameObject("Synthetic 1920x1080 reference Canvas", typeof(RectTransform), typeof(Canvas));
            canvasRoot.layer = 5; canvasRoot.transform.SetParent(fixture.transform, false);
            var canvas = canvasRoot.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera; canvas.planeDistance = 1;
            var scaler = canvasRoot.AddComponent(Type.GetType("UnityEngine.UI.CanvasScaler, UnityEngine.UI", true));
            Set(scaler, "uiScaleMode", Enum.Parse(Get(scaler, "uiScaleMode").GetType(), "ScaleWithScreenSize"));
            Set(scaler, "referenceResolution", new Vector2(1920, 1080)); Set(scaler, "matchWidthOrHeight", .5f);
            var root = new GameObject("Synthetic shared award rail", typeof(RectTransform), typeof(CanvasGroup));
            root.layer = 5; root.transform.SetParent(canvasRoot.transform, false);
            var rail = root.GetComponent<RectTransform>(); rail.anchorMin = rail.anchorMax = rail.pivot = new Vector2(.5f, 1);
            rail.anchoredPosition = new Vector2(0, -112); rail.sizeDelta = new Vector2(760, 132);
            var presenter = root.AddComponent(Sports("SportsScorePresentation"));
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Heat - Complete Modern UI/Prefabs/UI Elements/Notification/Notification.prefab");
            Assert.That(prefab, Is.Not.Null);
            var panel = UnityEngine.Object.Instantiate(prefab, root.transform, false);
            var panelRect = panel.GetComponent<RectTransform>(); panelRect.anchorMin = Vector2.zero; panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = panelRect.offsetMax = Vector2.zero;
            foreach (var component in panel.GetComponentsInChildren<Behaviour>(true))
            {
                string name = component.GetType().Name;
                if (name.EndsWith("LayoutGroup", StringComparison.Ordinal) || name == "ContentSizeFitter") component.enabled = false;
                if (name == "UIManagerText") { Set(component, "useCustomColor", true); Set(component, "useCustomFont", true); }
            }
            var notice = panel.GetComponent(Type.GetType("Michsky.UI.Heat.NotificationManager, Assembly-CSharp", true));
            Set(notice, "useLocalization", false); Set(notice, "minimizeAfter", 0f);
            // Match the actual Sports binding. The vendor prefab defaults to
            // Expanded, which would auto-start another In during Start.
            Set(notice, "defaultState", Enum.Parse(Get(notice, "defaultState").GetType(), "Minimized"));
            var text = (Component)Get(notice, "textObj"); var textRect = (RectTransform)Get(text, "rectTransform");
            textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(110, 12); textRect.offsetMax = new Vector2(-20, -12);
            var theme = Resources.Load("SportsBroadcastTheme"); Assert.That(theme, Is.Not.Null);
            Set(text, "font", Get(theme, "headingFont")); Set(text, "color", Color.white);
            Set(text, "enableAutoSizing", true); Set(text, "fontSizeMin", 20f); Set(text, "fontSizeMax", 28f);
            Set(text, "alignment", Enum.Parse(Get(text, "alignment").GetType(), "MidlineLeft"));
            var portrait = (Component)Get(notice, "iconObj"); var portraitRect = portrait.GetComponent<RectTransform>();
            portraitRect.anchorMin = portraitRect.anchorMax = portraitRect.pivot = new Vector2(0, .5f);
            portraitRect.pivot = new Vector2(.5f, .5f); portraitRect.anchoredPosition = new Vector2(53, 0); portraitRect.sizeDelta = new Vector2(76, 76);
            Set(notice, "icon", Resources.Load<Sprite>("Broadcast truck portrait")); Set(notice, "notificationText", "");
            var filled = panel.transform.Find("Filled"); if (filled != null) filled.gameObject.SetActive(false);
            var animator = (Animator)Get(notice, "itemAnimator");
            Set(presenter, "notification", notice); Set(presenter, "visibility", root.GetComponent<CanvasGroup>());
            Set(presenter, "portrait", portrait); Set(presenter, "notificationAnimator", animator);
            var playMargin = (Vector4)Get(text, "margin"); var playMin = textRect.offsetMin; var playMax = textRect.offsetMax;
            texture = new RenderTexture(1280, 720, 24); texture.Create(); camera.targetTexture = texture;
            fixture.SetActive(true); yield return null; yield return null;
            float initializedBy = Time.realtimeSinceStartup + 2;
            while (panel.activeSelf && Time.realtimeSinceStartup < initializedBy) yield return null;
            string initialized = State(notice, panel, text, animator, "after-vendor-Start", "");
            Assert.That(panel.activeSelf, Is.False, initialized); Assert.That(Get(notice, "isOn"), Is.False, initialized);
            string[] messages =
            {
                "<color=#33AAFF><b>Same Name ‹crew›</b></color>  ·  PARKING CHAMPION\n<size=140%><b>7.000</b></size> ROUNDS",
                "<color=#33AAFF><b>Alexandria Montgomery Smythe Truckington</b></color>  ·  MATCH CHAMPION\n<size=140%><b>123.456</b></size> SEC",
                "<color=#33AAFF><b>BLUE TEAM</b></color>  ·  FINAL POINT\n<size=140%><b>+1</b></size> TEAM PTS     15 TOTAL",
                "<color=#33AAFF><b>Same Name ‹crew›</b></color>  ·  LAST TRUCK PARKED\n<size=140%><b>3.000</b></size> TRUCKS LEFT"
            };
            float entrance = animator.runtimeAnimatorController.animationClips.Single(clip => clip.name == "Notification_In").length;
            float exit = animator.runtimeAnimatorController.animationClips.Single(clip => clip.name == "Notification_Out").length;
            foreach (var size in new[] { new Vector2Int(1280, 720), new Vector2Int(1920, 1080) })
            {
                camera.targetTexture = null; texture.Release(); UnityEngine.Object.DestroyImmediate(texture);
                texture = new RenderTexture(size.x, size.y, 24); texture.Create(); camera.targetTexture = texture;
                // Controlled render targets do not resize the Editor display.
                // Match the production 16:9 reference scaling explicitly.
                ((Behaviour)scaler).enabled = false; canvas.scaleFactor = size.y / 1080f;
                yield return null; Canvas.ForceUpdateCanvases();
                Assert.That(camera.pixelWidth, Is.EqualTo(size.x)); Assert.That(camera.pixelHeight, Is.EqualTo(size.y));
                Assert.That(((RectTransform)canvas.transform).rect.width, Is.EqualTo(1920).Within(1));
                Assert.That(((RectTransform)canvas.transform).rect.height, Is.EqualTo(1080).Within(1));
                foreach (string fullMessage in messages)
                {
                    string before = State(notice, panel, text, animator, "before-In " + size, fullMessage);
                    Assert.That(Get(notice, "isOn"), Is.False, "The preceding real Out must finish: " + before);
                    Call(presenter, "SetResultLayout", true);
                    Set(notice, "notificationText", fullMessage); Call(notice, "UpdateUI"); Call(notice, "ExpandNotification");
                    string expanding = State(notice, panel, text, animator, "after-Expand " + size, fullMessage);
                    Assert.That(Get(notice, "isOn"), Is.True, expanding); Assert.That(panel.activeInHierarchy, Is.True, expanding);
                    yield return new WaitForSecondsRealtime(entrance + .08f); yield return null;
                    // Heat's own completion coroutine uses scaled time. Native
                    // slow frames can advance realtime beyond an entire clip
                    // while scaled delta is capped; observe vendor completion.
                    float expandedBy = Time.realtimeSinceStartup + 15;
                    while (animator.enabled && panel.activeInHierarchy && Time.realtimeSinceStartup < expandedBy) yield return null;
                    Canvas.ForceUpdateCanvases(); Call(text, "ForceMeshUpdate", false, false);
                    string expanded = State(notice, panel, text, animator, "after-In-wait " + size, fullMessage);
                    Assert.That(panel.activeInHierarchy && text.gameObject.activeInHierarchy, Is.True, expanded);
                    Assert.That(animator.enabled, Is.False, "Heat's original In coroutine must finish: " + expanded);
                    Assert.That((string)Get(text, "text"), Is.EqualTo(fullMessage), "Full winner, reason, unit and 140% value remain intact.");
                    Assert.That(Get(text, "isTextOverflowing"), Is.False, "SYNTHETIC " + size + " " + fullMessage);
                    Assert.That((float)Get(text, "fontSize"), Is.GreaterThanOrEqualTo(16));
                    Assert.That(text.GetComponent<CanvasRenderer>().GetInheritedAlpha(), Is.GreaterThan(.95f));
                    Assert.That(((Color)Get(text, "color")).a, Is.GreaterThan(.95f));
                    Assert.That(textRect.rect.height, Is.EqualTo(68).Within(.1f));
                    var railPixels = Pixels(rail, camera); var textPixels = Pixels(textRect, camera); var portraitPixels = Pixels(portraitRect, camera);
                    Inside(railPixels, new Rect(0, 0, size.x, size.y)); Inside(textPixels, railPixels);
                    Assert.That(textPixels.xMin, Is.GreaterThan(portraitPixels.xMax), "Keep the complete existing portrait clear of text.");
                    TestContext.WriteLine("SYNTHETIC_HEAT_RESULT " + size.x + "x" + size.y + " font=" + Get(text, "fontSize") + " text=" + textPixels + " rail=" + railPixels);
                    Call(presenter, "SetResultLayout", false); Canvas.ForceUpdateCanvases();
                    Assert.That(rail.sizeDelta, Is.EqualTo(new Vector2(760, 132))); Assert.That(rail.anchoredPosition, Is.EqualTo(new Vector2(0, -112)));
                    Assert.That(textRect.offsetMin, Is.EqualTo(playMin)); Assert.That(textRect.offsetMax, Is.EqualTo(playMax));
                    Assert.That((Vector4)Get(text, "margin"), Is.EqualTo(playMargin));
                    Assert.That(Get(text, "enableAutoSizing"), Is.True); Assert.That(Get(text, "fontSizeMin"), Is.EqualTo(20f)); Assert.That(Get(text, "fontSizeMax"), Is.EqualTo(28f));
                    Call(notice, "MinimizeNotification"); yield return new WaitForSecondsRealtime(exit + .08f); yield return null;
                    float minimizedBy = Time.realtimeSinceStartup + 15;
                    while (((bool)Get(notice, "isOn") || panel.activeSelf) && Time.realtimeSinceStartup < minimizedBy) yield return null;
                    string minimized = State(notice, panel, text, animator, "after-Out-wait " + size, fullMessage);
                    Assert.That(Get(notice, "isOn"), Is.False, minimized); Assert.That(panel.activeSelf, Is.False, minimized);
                }
            }
            Call(presenter, "SetResultLayout", true); root.SetActive(false);
            Assert.That(textRect.offsetMin, Is.EqualTo(playMin)); Assert.That(textRect.offsetMax, Is.EqualTo(playMax));
            Assert.That(Get(text, "fontSizeMin"), Is.EqualTo(20f)); Assert.That(Get(text, "fontSizeMax"), Is.EqualTo(28f));
        }
        finally
        {
            Time.timeScale = oldTimeScale; UnityEngine.Object.DestroyImmediate(fixture);
            if (texture != null) { texture.Release(); UnityEngine.Object.DestroyImmediate(texture); }
        }
    }
}
#endif

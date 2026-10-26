using System;
using System.IO;
using System.Linq;
using Michsky.UI.Heat;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace LWS.TruckSports.Editor
{
    // Explicit prefab authoring in a preview scene. Never saves or rebuilds an
    // existing sport/front-end scene and never overwrites a designer's customizer.
    public static class SportsTruckCustomizerAuthoring
    {
        public const string PrefabPath = "Assets/TruckSports/Resources/TruckSports/SportsTruckCustomizerMenu.prefab";
        public const string ResourcePath = "TruckSports/SportsTruckCustomizerMenu";
        const string ShopPath = "Assets/Heat - Complete Modern UI/Prefabs/UI Elements/Button/Button (Shop).prefab";
        static readonly string[] Labels = { "TRUCK BODIES", "HAT SHOP", "EYES", "EYEBROWS", "MOUTHS", "MUSTACHES", "EYELASHES", "MIRRORS", "WHEELS", "TRUCK BALLS", "DASHBOARD" };

        [MenuItem("Truck Sports/Customizer/Create Missing Reusable Heat Shop")]
        public static void CreateReusablePrefab()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode before authoring the customizer prefab.");
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null) { Debug.Log("TRUCK_CUSTOMIZER_PREFAB_PRESERVED " + PrefabPath); return; }
            var shopPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ShopPath);
            var theme = Resources.Load<SportsBroadcastTheme>("SportsBroadcastTheme");
            if (shopPrefab == null || theme == null) throw new InvalidOperationException("Installed Heat Shop button and Sports broadcast theme are required.");
            Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath)); AssetDatabase.Refresh();
            var preview = EditorSceneManager.NewPreviewScene(); GameObject root = null;
            try
            {
                root = new GameObject("Truck Sports profile customizer", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster), typeof(CanvasGroup));
                root.SetActive(false); SceneManager.MoveGameObjectToScene(root, preview); root.layer = 5;
                var canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 50;
                var scaler = root.GetComponent<UnityEngine.UI.CanvasScaler>(); scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080); scaler.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.Expand;
                var shade = SportsUIAuthoring.Rect("Customizer background", root.transform, new Vector2(1920, 1080), Vector2.zero);
                shade.AddComponent<UnityEngine.UI.Image>().color = new Color(.012f, .02f, .04f, .99f);
                var menu = root.AddComponent<SportsTruckCustomizerMenu>(); root.AddComponent<SportsMenuMotion>();
                var logo = SportsUIAuthoring.Rect("Truck Sports Party wordmark", root.transform, new Vector2(300, 100), new Vector2(-770, 460)).AddComponent<UnityEngine.UI.Image>();
                logo.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/TruckSports/Art/Brand/Truck Sports Party wordmark.png"); logo.preserveAspect = true; logo.raycastTarget = false;
                Text(root.transform, "Title", "TRUCK CUSTOMIZER", new Vector2(1050, 72), new Vector2(100, 468), 54, theme.headingFont);
                menu.identity = Text(root.transform, "Profile identity", "", new Vector2(1550, 42), new Vector2(40, 405), 27, theme.bodyFont); menu.identity.richText = false;
                menu.categories = new ButtonManager[Labels.Length];
                for (int i = 0; i < Labels.Length; i++) menu.categories[i] = Button(root.transform, Labels[i], new Vector2(-775, 310 - i * 63), new Vector2(280, 52));
                var previewFrame = SportsUIAuthoring.Rect("Live truck preview", root.transform, new Vector2(640, 480), new Vector2(-300, 65));
                var previewSurface = SportsUIAuthoring.Rect("Render texture image", previewFrame.transform, new Vector2(640, 480), Vector2.zero);
                menu.previewImage = previewSurface.AddComponent<UnityEngine.UI.RawImage>(); menu.previewImage.color = Color.white; menu.previewImage.raycastTarget = true;
                previewSurface.AddComponent<EventTrigger>(); var aspect = previewSurface.AddComponent<UnityEngine.UI.AspectRatioFitter>(); aspect.aspectMode = UnityEngine.UI.AspectRatioFitter.AspectMode.FitInParent; aspect.aspectRatio = 4f / 3f;
                menu.previewCaption = Text(root.transform, "Preview identity", "", new Vector2(630, 50), new Vector2(-300, 340), 28, theme.headingFont);
                Text(root.transform, "Preview controls", "Drag to rotate  /  Q · E  /  right stick", new Vector2(630, 42), new Vector2(-300, -210), 20, theme.bodyFont);
                menu.rotateLeft = Button(root.transform, "ROTATE LEFT", new Vector2(-470, -275), new Vector2(270, 50));
                menu.rotateRight = Button(root.transform, "ROTATE RIGHT", new Vector2(-150, -275), new Vector2(270, 50));
                menu.autoRotate = Button(root.transform, "AUTO ROTATE: ON", new Vector2(-300, -340), new Vector2(420, 48));
                menu.categoryHeading = Text(root.transform, "Category", "TRUCK BODIES", new Vector2(720, 50), new Vector2(500, 350), 32, theme.headingFont);
                menu.bodyFilter = Button(root.transform, "ALL BODY STYLES", new Vector2(500, 300), new Vector2(680, 40));
                menu.cards = new ShopButtonManager[6];
                for (int i = 0; i < menu.cards.Length; i++)
                {
                    var item = (GameObject)PrefabUtility.InstantiatePrefab(shopPrefab, root.transform);
                    PrefabUtility.UnpackPrefabInstance(item, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                    item.name = "Cosmetic choice " + (i + 1); var rect = item.GetComponent<RectTransform>();
                    rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f); rect.sizeDelta = new Vector2(340, 170);
                    rect.anchoredPosition = new Vector2(310 + i % 2 * 380, 190 - i / 2 * 185);
                    var card = item.GetComponent<ShopButtonManager>(); menu.cards[i] = card; ConfigureCard(card, theme);
                }
                menu.previous = Button(root.transform, "PREVIOUS", new Vector2(290, -350), new Vector2(260, 46));
                menu.next = Button(root.transform, "NEXT", new Vector2(710, -350), new Vector2(260, 46));
                menu.pageCaption = Text(root.transform, "Item page", "", new Vector2(700, 35), new Vector2(500, -296), 21, theme.bodyFont);
                menu.remove = Button(root.transform, "REMOVE ITEM", new Vector2(500, -410), new Vector2(440, 46));
                menu.notice = Text(root.transform, "Save and availability notice", "", new Vector2(1440, 72), new Vector2(-100, -430), 21, theme.bodyFont);
                menu.notice.alignment = TextAlignmentOptions.MidlineLeft; menu.notice.rectTransform.sizeDelta = new Vector2(1060, 76); menu.notice.rectTransform.anchoredPosition = new Vector2(-340, -430);
                menu.cancel = Button(root.transform, "CANCEL", new Vector2(-320, -500), new Vector2(460, 58));
                menu.apply = Button(root.transform, "SAVE TRUCK", new Vector2(320, -500), new Vector2(460, 58));
                foreach (var text in root.GetComponentsInChildren<TMP_Text>(true)) { text.color = SportsBroadcastCanvas.Cream; text.raycastTarget = false; }
                foreach (var image in root.GetComponentsInChildren<UnityEngine.UI.Image>(true)) image.raycastTarget = image.GetComponent<UnityEngine.UI.Selectable>() != null;
                foreach (var node in root.GetComponentsInChildren<Transform>(true)) node.gameObject.layer = 5;
                shade.GetComponent<UnityEngine.UI.Image>().raycastTarget = true;
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath); AssetDatabase.SaveAssets();
                Debug.Log("TRUCK_CUSTOMIZER_PREFAB_CREATED " + PrefabPath + "; inactive reusable Heat shop, no new EventSystem/input/audio listener or scene save. Native/visual/controller verification pending.");
            }
            finally { if (root != null) UnityEngine.Object.DestroyImmediate(root); EditorSceneManager.ClosePreviewScene(preview); }
        }
        static TMP_Text Text(Transform parent, string name, string content, Vector2 size, Vector2 position, int fontSize, TMP_FontAsset font)
        {
            var text = SportsUIAuthoring.Text(name, parent, content, size, position, fontSize); text.font = font; text.fontSizeMin = Mathf.Max(16, fontSize * .8f); return text;
        }
        static ButtonManager Button(Transform parent, string label, Vector2 position, Vector2 size)
        {
            var button = SportsUIAuthoring.Button(parent, label, "customizer", position, size);
            UnityEngine.Object.DestroyImmediate(button.GetComponent<SportsMenuAction>()); button.name = label;
            button.normalTextObj.font = Resources.Load<SportsBroadcastTheme>("SportsBroadcastTheme").bodyFont;
            button.normalTextObj.fontSizeMax = 23; button.normalTextObj.fontSizeMin = 18; return button;
        }
        static void StretchParents(Transform target, Transform root)
        {
            for (var parent = target.parent; parent != null && parent != root; parent = parent.parent)
                if (parent is RectTransform rect) { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; }
        }
        static void Place(RectTransform rect, Vector2 size, Vector2 position) { rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f); rect.sizeDelta = size; rect.anchoredPosition = position; }
        static void ConfigureCard(ShopButtonManager card, SportsBroadcastTheme theme)
        {
            card.enablePrice = false; card.useModalWindow = false; card.useLocalization = false; card.useSounds = false; card.enableDescription = true; card.buttonDescription = "";
            card.useUINavigation = true; card.navigationMode = UnityEngine.UI.Navigation.Mode.Explicit; card.enableFilter = true;
            foreach (var layout in card.GetComponentsInChildren<UnityEngine.UI.LayoutGroup>(true)) layout.enabled = false;
            foreach (var fit in card.GetComponentsInChildren<UnityEngine.UI.ContentSizeFitter>(true)) fit.enabled = false;
            foreach (var style in card.GetComponentsInChildren<UIManagerText>(true)) { style.useCustomColor = true; style.useCustomFont = true; }
            foreach (var style in card.GetComponentsInChildren<UIManagerImage>(true)) style.useCustomColor = true;
            foreach (var target in new[] { card.iconObj.transform, card.titleObj.transform, card.descriptionObj.transform, card.purchaseButton.transform, card.purchasedButton.transform }) StretchParents(target, card.transform);
            Place(card.iconObj.rectTransform, new Vector2(110, 110), new Vector2(-105, 15)); card.iconObj.preserveAspect = true;
            Place(card.titleObj.rectTransform, new Vector2(195, 90), new Vector2(62, 30)); card.titleObj.font = theme.headingFont; card.titleObj.enableAutoSizing = true; card.titleObj.fontSizeMin = 18; card.titleObj.fontSizeMax = 27;
            Place(card.descriptionObj.rectTransform, new Vector2(180, 20), new Vector2(65, -15));
            foreach (var button in new[] { card.purchaseButton, card.purchasedButton })
            {
                Place(button.GetComponent<RectTransform>(), new Vector2(205, 40), new Vector2(50, -55)); button.autoFitContent = false; button.useSounds = false; button.useLocalization = false; button.useUINavigation = true; button.checkForDoubleClick = false;
                button.buttonText = button == card.purchaseButton ? "EQUIP" : "EQUIPPED"; button.UpdateUI();
                foreach (var text in button.GetComponentsInChildren<TMP_Text>(true)) { text.font = theme.bodyFont; text.enableAutoSizing = true; text.fontSizeMin = 16; text.fontSizeMax = 20; }
            }
            var indicator = card.purchasedIndicator.GetComponent<RectTransform>(); indicator.anchorMin = indicator.anchorMax = indicator.pivot = Vector2.one; indicator.sizeDelta = new Vector2(28, 28); indicator.anchoredPosition = new Vector2(-8, -8);
        }
    }
}

#if UNITY_EDITOR
using System;
using System.Linq;
using LWS.TruckBilliards;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LWS.TruckSports.Editor
{
    public static class SportsTruckCompatibilitySummaryBackingAuthoring
    {
        public const string BackingName="Truck fit summary readability backing";
        public static Sprite SharedSprite=>AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        static SportsTruckCompatibilityPresenter ValidateRow(SportsTruckCompatibilitySource source)
        {
            if(source==null||source.name!="Truck fit setup adapter"||SportsTruckCompatibilityAuthoring.IsExcludedScene(source.gameObject.scene.path))throw new InvalidOperationException("Only an owned unprotected setup report row can gain a backing.");
            var owners=new UnityEngine.Object[]{source.session,source.bowling,source.cornhole,source.cheese,source.hockey,source.airHockey,source.wallball,source.billiards};if(owners.Count(owner=>owner!=null)!=1)throw new InvalidOperationException("Preserve an unknown/mixed setup authority.");
            MonoBehaviour menu=source.session!=null?source.session.menus:source.bowling!=null?source.bowling.menus:source.cornhole!=null?source.cornhole.menus:source.cheese!=null?source.cheese.menus:source.hockey!=null?source.hockey.menus:source.airHockey!=null?source.airHockey.menus:source.wallball!=null?source.wallball.menus:source.billiards.menus;var pages=menu is SportsMenuView common?common.pages:menu is BilliardsMenus billiards?billiards.pages:null;int index=source.billiards!=null?(int)BilliardsScreen.Setup:0;
            var presenter=source.GetComponent<SportsTruckCompatibilityPresenter>();
            if(menu==null||source.menu!=menu||pages==null||index>=pages.Length||source.setupPage!=pages[index]||source.setupPage==null||source.setupInteraction!=source.setupPage.GetComponent<CanvasGroup>()||presenter==null||source.GetComponents<SportsTruckCompatibilityPresenter>().Length!=1||presenter.source!=source||presenter.subtitle==null||presenter.subtitle.name!="Truck fit setup subtitle"||presenter.subtitle.transform.parent!=source.setupPage.transform||presenter.reportButton==null||presenter.reportButton.transform.parent!=source.setupPage.transform||!string.IsNullOrEmpty(presenter.originalSubtitle))throw new InvalidOperationException("Preserve custom setup menu/page/summary/presenter ownership.");
            var summary=presenter.subtitle.rectTransform;var button=(RectTransform)presenter.reportButton.transform;
            bool Centered(RectTransform rect)=>rect.anchorMin==new Vector2(.5f,.5f)&&rect.anchorMax==rect.anchorMin&&rect.pivot==rect.anchorMin&&rect.localRotation==Quaternion.identity&&rect.localScale==Vector3.one;
            if(!Centered(summary)||!Centered(button)||summary.sizeDelta!=new Vector2(1040,42)||summary.anchoredPosition.x!=-40||!new[]{480f,492f,-492f}.Contains(summary.anchoredPosition.y)||button.sizeDelta!=new Vector2(180,44)||button.anchoredPosition!=new Vector2(610,summary.anchoredPosition.y)||presenter.subtitle.fontSizeMax!=21||presenter.subtitle.richText)throw new InvalidOperationException("Preserve custom summary/button geometry or text styling; no layout movement is authorized.");
            return presenter;
        }
        static Rect Bounds(RectTransform rect,Transform parent)
        {
            var corners=new Vector3[4];rect.GetWorldCorners(corners);var points=corners.Select(parent.InverseTransformPoint).ToArray();return Rect.MinMaxRect(points.Min(p=>p.x),points.Min(p=>p.y),points.Max(p=>p.x),points.Max(p=>p.y));
        }
        public static bool Upgrade(SportsTruckCompatibilitySource source)
        {
            var presenter=ValidateRow(source);var page=source.setupPage.transform;var summary=presenter.subtitle.rectTransform;var sprite=SharedSprite;if(sprite==null)throw new InvalidOperationException("Existing shared rounded UI sprite is required.");var candidates=page.Cast<Transform>().Where(child=>child.name==BackingName).ToArray();if(candidates.Length>1)throw new InvalidOperationException("Preserve duplicate/custom summary backings.");var existing=candidates.SingleOrDefault();var size=summary.sizeDelta+new Vector2(24,8);var footprint=new Rect(summary.anchoredPosition-size*.5f,size);
            if(footprint.xMin< -952||footprint.xMax>952||footprint.yMin< -532||footprint.yMax>532)throw new InvalidOperationException("Preserve the row outside reviewed screen margins.");
            foreach(var image in source.setupPage.GetComponentsInChildren<Image>(true))if(image.transform!=page&&image.transform!=existing&&image.gameObject.activeSelf&&Bounds(image.rectTransform,page).Overlaps(footprint))throw new InvalidOperationException("Preserve custom/unknown overlapping summary backing or original graphic: "+image.name);
            if(existing!=null)
            {
                var rect=existing as RectTransform;var image=existing.GetComponent<Image>();if(rect==null||image==null||existing.GetComponents<Component>().Length!=3||existing.childCount!=0||!existing.gameObject.activeSelf||existing.gameObject.layer!=summary.gameObject.layer||rect.anchorMin!=summary.anchorMin||rect.anchorMax!=summary.anchorMax||rect.pivot!=summary.pivot||rect.anchoredPosition!=summary.anchoredPosition||rect.sizeDelta!=size||rect.localRotation!=Quaternion.identity||rect.localScale!=Vector3.one||existing.GetSiblingIndex()!=summary.GetSiblingIndex()-1||image.sprite!=sprite||image.type!=Image.Type.Sliced||image.color!=SportsBroadcastCanvas.Ink||image.raycastTarget||!image.fillCenter||image.preserveAspect||image.material!=Graphic.defaultGraphicMaterial)throw new InvalidOperationException("Preserve an edited/custom summary backing; explicit review is required.");return false;
            }
            var root=SportsUIAuthoring.Rect(BackingName,page,size,summary.anchoredPosition);root.layer=summary.gameObject.layer;root.transform.SetSiblingIndex(summary.GetSiblingIndex());var backing=root.AddComponent<Image>();backing.sprite=sprite;backing.type=Image.Type.Sliced;backing.color=SportsBroadcastCanvas.Ink;backing.raycastTarget=false;EditorSceneManager.MarkSceneDirty(source.gameObject.scene);Debug.Log("TRUCK_FIT_SUMMARY_BACKING_AUTHORED "+source.gameObject.scene.path+"; existing rounded sprite,1064x50, no raycast or original rect/art/text changes.");return true;
        }
        [MenuItem("Truck Sports/Customizer/Add Missing Owned Fit Summary Backings In Registered Scenes")]
        public static void UpgradeRegisteredScenes()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||Enumerable.Range(0,SceneManager.sceneCount).Any(i=>SceneManager.GetSceneAt(i).isDirty))throw new InvalidOperationException("Stopped Editor with saved scenes required.");var previous=EditorSceneManager.GetSceneManagerSetup();
            try{foreach(var path in SportsTruckCompatibilityAuthoring.RegisteredTargets()){var scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Single);var source=scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<SportsTruckCompatibilitySource>(true)).Single();if(Upgrade(source))EditorSceneManager.SaveScene(scene);}}
            finally{SportsTruckCompatibilityAuthoring.RestorePreviousSceneSetup(previous);}
        }
        [MenuItem("Truck Sports/Customizer/Add Missing Owned Fit Summary Backing In Open Scene")]
        public static void UpgradeOpenScene()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stopped Editor required.");var source=SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<SportsTruckCompatibilitySource>(true)).Single();Upgrade(source);
        }
    }
}
#endif
